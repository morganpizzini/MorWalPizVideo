using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

/// <summary>
/// Web Push composition. Two independent sections:
/// <list type="bullet">
/// <item>platform-wide sends, gated by <see cref="AuthorizationPermissionKeys.PushPlatformSend"/>, targeting named
/// audiences and/or explicit channels;</item>
/// <item>channel-owner broadcasts, scoped by <c>X-Channel-Id</c> ownership, reaching every subscriber of the owned
/// channel with no per-user selection.</item>
/// </list>
/// Recipients are snapshotted at queue time and delivered by a durable Hangfire fanout.
/// </summary>
[ApiController]
[Route("api/pushcampaigns")]
[Authorize]
public sealed class PushCampaignsController(
    IPushDispatchRepository dispatchRepository,
    IPushSubscriptionRepository subscriptionRepository,
    IPushAudienceRepository audienceRepository,
    IYTChannelRepository channelRepository,
    IPushDispatchService dispatchService,
    IConfiguration configuration) : ControllerBase
{
    private const int MaxActionsCeiling = 4;

    /// <summary>Channels and audiences available to the platform composer, with current consenting subscriber counts.</summary>
    [HttpGet("targets")]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<ActionResult<PushTargetsContract>> GetTargets(CancellationToken cancellationToken)
    {
        var channels = await channelRepository.GetItemsAsync();
        var channelIds = channels.Select(channel => channel.ChannelId).ToArray();
        var subscriptions = await subscriptionRepository.GetActiveByChannelsAsync(channelIds, cancellationToken);
        var counts = subscriptions
            .SelectMany(subscription => subscription.ChannelIds.Distinct(StringComparer.Ordinal))
            .GroupBy(channelId => channelId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var audiences = await audienceRepository.GetItemsAsync();
        return Ok(new PushTargetsContract
        {
            Channels = [.. channels
                .OrderBy(channel => channel.ChannelName, StringComparer.OrdinalIgnoreCase)
                .Select(channel => new PushTargetChannelContract
                {
                    ChannelId = channel.ChannelId,
                    ChannelName = channel.ChannelName,
                    ActiveSubscriberCount = counts.GetValueOrDefault(channel.ChannelId)
                })],
            Audiences = [.. audiences
                .OrderBy(audience => audience.Name, StringComparer.OrdinalIgnoreCase)
                .Select(audience => new PushAudienceContract
                {
                    Id = audience.Id,
                    Code = audience.Code,
                    Name = audience.Name,
                    Description = audience.Description,
                    ChannelIds = audience.ChannelIds,
                    IsActive = audience.IsActive,
                    CreationDateTime = audience.CreationDateTime,
                    UpdatedAt = audience.UpdatedAt
                })],
            MaxActions = MaxActions
        });
    }

    /// <summary>Platform-wide dispatch history.</summary>
    [HttpGet("platform")]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<ActionResult<IReadOnlyList<PushDispatchContract>>> GetPlatformDispatches()
    {
        var items = await dispatchRepository.GetItemsAsync(item => item.Scope == PushDispatchScope.Platform);
        return Ok(OrderedContracts(items));
    }

    /// <summary>Dispatch history for the channel resolved from <c>X-Channel-Id</c>.</summary>
    [HttpGet("channel")]
    [RequireChannelScope]
    [AllowUser(AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<ActionResult<IReadOnlyList<PushDispatchContract>>> GetChannelDispatches()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var items = await dispatchRepository.GetItemsAsync(item => item.OwnerChannelId == channelId);
        return Ok(OrderedContracts(items));
    }

    [HttpPost("platform")]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<ActionResult<PushDispatchContract>> SendPlatform(PushPlatformSendRequest request, CancellationToken cancellationToken)
    {
        var destination = PushDestination.Normalize(request.Destination);
        if (destination is null) return BadRequest("Destination must be a same-origin relative path.");

        var actions = NormalizeActions(request.Actions, out var invalidAction);
        if (invalidAction) return BadRequest("Action destinations must be same-origin relative paths.");

        var knownChannelIds = (await channelRepository.GetItemsAsync()).Select(channel => channel.ChannelId).ToHashSet(StringComparer.Ordinal);
        var channelIds = request.AllChannels
            ? knownChannelIds
            : request.ChannelIds.Where(knownChannelIds.Contains).ToHashSet(StringComparer.Ordinal);

        var audienceIds = await ResolveAudienceIdsAsync(request.AudienceIds, cancellationToken);
        if (channelIds.Count == 0 && audienceIds.Count == 0)
            return BadRequest("Select at least one channel or audience.");

        return await CreateAndQueueAsync(new PushDispatch(
            PushDispatchScope.Platform,
            request.Title.Trim(),
            request.Body.Trim(),
            [.. channelIds],
            PushDispatchState.Queued)
        {
            Destination = destination,
            Actions = actions,
            AudienceIds = audienceIds,
            CreatedBy = CurrentUserId,
            CreationDateTime = DateTime.UtcNow
        }, cancellationToken);
    }

    /// <summary>
    /// Broadcasts to every active consenting subscriber of the owned channel. Ownership is enforced by the channel
    /// scope middleware; there is intentionally no recipient picker.
    /// </summary>
    [HttpPost("channel")]
    [RequireChannelScope]
    [AllowUser(AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<ActionResult<PushDispatchContract>> SendChannel(PushChannelSendRequest request, CancellationToken cancellationToken)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var destination = PushDestination.Normalize(request.Destination);
        if (destination is null) return BadRequest("Destination must be a same-origin relative path.");

        var actions = NormalizeActions(request.Actions, out var invalidAction);
        if (invalidAction) return BadRequest("Action destinations must be same-origin relative paths.");

        return await CreateAndQueueAsync(new PushDispatch(
            PushDispatchScope.Channel,
            request.Title.Trim(),
            request.Body.Trim(),
            [channelId],
            PushDispatchState.Queued)
        {
            Destination = destination,
            Actions = actions,
            OwnerChannelId = channelId,
            CreatedBy = CurrentUserId,
            CreationDateTime = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task<ActionResult<PushDispatchContract>> CreateAndQueueAsync(PushDispatch dispatch, CancellationToken cancellationToken)
    {
        var created = await dispatchRepository.AddItemAsync(dispatch);
        if (!await dispatchService.QueueAsync(created.Id, cancellationToken))
        {
            await dispatchRepository.UpdateItemAsync(created with { State = PushDispatchState.Failed });
            return Conflict("The push dispatch could not be queued.");
        }

        var queued = await dispatchRepository.GetItemAsync(created.Id) ?? created;
        return Accepted(ToContract(queued));
    }

    private async Task<IReadOnlyList<string>> ResolveAudienceIdsAsync(IReadOnlyList<string> requested, CancellationToken cancellationToken)
    {
        var wanted = requested.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
        if (wanted.Length == 0) return [];
        var audiences = await audienceRepository.GetByIdsAsync(wanted, cancellationToken);
        return [.. audiences.Where(audience => audience.IsActive).Select(audience => audience.Id)];
    }

    /// <summary>
    /// Caps action buttons at the configured <c>WebPush:MaxActions</c>. Browsers expose their own
    /// <c>Notification.maxActions</c>; the service worker slices again at display time.
    /// </summary>
    private IReadOnlyList<PushNotificationAction> NormalizeActions(IReadOnlyList<PushNotificationActionRequest> requested, out bool invalid)
    {
        invalid = false;
        var normalized = new List<PushNotificationAction>();
        foreach (var action in requested.Take(MaxActions))
        {
            if (string.IsNullOrWhiteSpace(action.Action) || string.IsNullOrWhiteSpace(action.Title)) continue;
            var destination = PushDestination.Normalize(action.Destination);
            if (destination is null)
            {
                invalid = true;
                return [];
            }
            normalized.Add(new PushNotificationAction(action.Action.Trim(), action.Title.Trim(), destination));
        }
        return normalized;
    }

    private int MaxActions => Math.Clamp(configuration.GetValue("WebPush:MaxActions", 2), 0, MaxActionsCeiling);

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private static PushDispatchContract[] OrderedContracts(IEnumerable<PushDispatch> items) =>
        [.. items.OrderByDescending(item => item.CreationDateTime).Select(ToContract)];

    private static PushDispatchContract ToContract(PushDispatch dispatch) => new()
    {
        Id = dispatch.Id,
        Scope = dispatch.Scope,
        State = dispatch.State,
        Title = dispatch.Title,
        Body = dispatch.Body,
        Destination = dispatch.Destination,
        Actions = [.. dispatch.Actions.Select(action => new PushNotificationActionRequest
        {
            Action = action.Action,
            Title = action.Title,
            Destination = action.Destination
        })],
        ChannelIds = dispatch.ChannelIds,
        AudienceIds = dispatch.AudienceIds,
        OwnerChannelId = dispatch.OwnerChannelId,
        RecipientCount = dispatch.RecipientCount,
        CreationDateTime = dispatch.CreationDateTime,
        QueuedAt = dispatch.QueuedAt,
        CompletedAt = dispatch.CompletedAt
    };
}
