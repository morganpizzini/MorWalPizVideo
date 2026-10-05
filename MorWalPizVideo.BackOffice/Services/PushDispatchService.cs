using System.Text.Json;
using Hangfire;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

/// <summary>Raised when at least one endpoint in a batch failed transiently, so Hangfire retries the batch.</summary>
public sealed class PushTransientException(string message) : Exception(message);

public interface IPushDispatchService
{
    /// <summary>Snapshots the active consenting subscriptions and enqueues durable fanout. Safe to call twice.</summary>
    Task<bool> QueueAsync(string dispatchId, CancellationToken cancellationToken = default);
    Task ProcessBatchAsync(string dispatchId, CancellationToken cancellationToken = default);
    Task ReconcileAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Durable, idempotent Web Push fanout. Mirrors the newsletter dispatch pattern: claim the aggregate, snapshot
/// recipients behind a deterministic idempotency key, then drain leased batches from a Hangfire job.
/// </summary>
public sealed class PushDispatchService(
    IPushDispatchRepository dispatchRepository,
    IPushDispatchRecipientRepository recipientRepository,
    IPushSubscriptionRepository subscriptionRepository,
    IPushAudienceRepository audienceRepository,
    IWebPushSender webPushSender,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<PushDispatchService> logger) : IPushDispatchService
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The recipient snapshot is the durable queue: it is written before any background work is scheduled, so an
    /// unavailable Hangfire server delays delivery instead of losing it. The reconciliation job re-enqueues
    /// dispatches that were never picked up.
    /// </summary>
    public async Task<bool> QueueAsync(string dispatchId, CancellationToken cancellationToken = default)
    {
        var claimed = await dispatchRepository.ClaimForSendingAsync(dispatchId, PushDispatchState.Queued, DateTime.UtcNow, cancellationToken);
        if (claimed is null) return false;

        var recipientCount = await ProvisionRecipientsAsync(claimed, cancellationToken);
        await dispatchRepository.UpdateItemAsync(claimed with { RecipientCount = recipientCount });

        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        if (backgroundJobClient is null)
            logger.LogWarning("Push dispatch {DispatchId} snapshotted {RecipientCount} endpoint(s) but Hangfire is disabled; delivery is deferred", dispatchId, recipientCount);
        else
            backgroundJobClient.Enqueue<PushDispatchService>(service => service.ProcessBatchAsync(dispatchId, CancellationToken.None));

        logger.LogInformation("Push dispatch {DispatchId} queued for {RecipientCount} endpoint(s)", dispatchId, recipientCount);
        return true;
    }

    /// <summary>
    /// Resolves the dispatch targets to the set of active consenting subscriptions at this instant and writes the
    /// snapshot. Later subscription changes do not affect an already-queued dispatch.
    /// </summary>
    private async Task<int> ProvisionRecipientsAsync(PushDispatch dispatch, CancellationToken cancellationToken)
    {
        var channelIds = await ResolveChannelIdsAsync(dispatch, cancellationToken);

        var applications = dispatch.ApplicationKeys.Count > 0
            ? dispatch.ApplicationKeys
            : [dispatch.ApplicationKey];
        var channelSubscriptions = channelIds.Count == 0
            ? []
            : (await Task.WhenAll(applications
                .Where(application => !string.IsNullOrWhiteSpace(application))
                .Distinct(StringComparer.Ordinal)
                .Select(application => subscriptionRepository.GetActiveByChannelsAndApplicationAsync(channelIds, application, cancellationToken))))
            .SelectMany(items => items);
        var platformSubscriptions = dispatch.Scope == PushDispatchScope.Platform
            ? await subscriptionRepository.GetActivePlatformByApplicationAsync("backoffice", cancellationToken)
            : [];
        var subscriptions = channelSubscriptions.Concat(platformSubscriptions)
            .GroupBy(subscription => subscription.EndpointHash, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var now = DateTime.UtcNow;
        foreach (var subscription in subscriptions)
        {
            await recipientRepository.EnsurePendingAsync(new PushDispatchRecipient(
                dispatch.Id, subscription.Id, subscription.EndpointHash)
            {
                IdempotencyKey = $"{dispatch.Id}:{subscription.EndpointHash}",
                CreationDateTime = now
            }, cancellationToken);
        }
        return subscriptions.Length;
    }

    private async Task<IReadOnlyCollection<string>> ResolveChannelIdsAsync(PushDispatch dispatch, CancellationToken cancellationToken)
    {
        var channelIds = new HashSet<string>(dispatch.ChannelIds, StringComparer.Ordinal);
        if (dispatch.AudienceIds.Count > 0)
        {
            foreach (var audience in await audienceRepository.GetByIdsAsync(dispatch.AudienceIds, cancellationToken))
            {
                if (!audience.IsActive) continue;
                foreach (var channelId in audience.ChannelIds) channelIds.Add(channelId);
            }
        }
        channelIds.Remove(string.Empty);
        return channelIds;
    }

    [AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ProcessBatchAsync(string dispatchId, CancellationToken cancellationToken = default)
    {
        var dispatch = await dispatchRepository.GetItemAsync(dispatchId);
        if (dispatch is null || dispatch.State is PushDispatchState.Sent or PushDispatchState.Cancelled) return;

        var batchSize = Math.Clamp(configuration.GetValue("WebPush:BatchSize", 100), 1, 1000);
        var lease = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("WebPush:SendingLeaseMinutes", 15), 1, 240));
        var payload = BuildPayload(dispatch);
        var recipients = await recipientRepository.ClaimBatchAsync(dispatchId, batchSize, DateTime.UtcNow, lease, cancellationToken);
        var hasTransientFailure = false;

        foreach (var recipient in recipients)
        {
            var subscription = await subscriptionRepository.GetItemAsync(recipient.SubscriptionId);
            if (subscription is null || !subscription.IsActive)
            {
                await recipientRepository.MarkSuppressedAsync(recipient.Id, "Subscription is no longer active.", DateTime.UtcNow, cancellationToken);
                continue;
            }

            var result = await webPushSender.SendAsync(subscription, payload, cancellationToken);
            switch (result.Outcome)
            {
                case PushSendOutcome.Sent:
                    await recipientRepository.MarkSentAsync(recipient.Id, DateTime.UtcNow, cancellationToken);
                    break;
                case PushSendOutcome.Gone:
                    await subscriptionRepository.DeactivateAsync(subscription.EndpointHash, DateTime.UtcNow, cancellationToken);
                    await recipientRepository.MarkSuppressedAsync(recipient.Id, result.Reason ?? "Endpoint gone.", DateTime.UtcNow, cancellationToken);
                    break;
                case PushSendOutcome.Rejected:
                    await recipientRepository.MarkFailedAsync(recipient.Id, result.Reason ?? "Rejected.", false, DateTime.UtcNow, cancellationToken);
                    break;
                default:
                    await recipientRepository.MarkFailedAsync(recipient.Id, result.Reason ?? "Transient failure.", true, DateTime.UtcNow, cancellationToken);
                    hasTransientFailure = true;
                    break;
            }
        }

        if (hasTransientFailure)
            throw new PushTransientException($"Transient push delivery failure in dispatch {dispatchId}.");

        var outstanding = (await recipientRepository.GetItemsAsync(item => item.DispatchId == dispatchId))
            .Any(item => item.Status is PushDeliveryStatus.Pending or PushDeliveryStatus.Sending);
        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        if (outstanding && backgroundJobClient is not null)
            backgroundJobClient.Enqueue<PushDispatchService>(service => service.ProcessBatchAsync(dispatchId, CancellationToken.None));
        else if (!outstanding)
            await dispatchRepository.UpdateItemAsync(dispatch with { State = PushDispatchState.Sent, CompletedAt = DateTime.UtcNow });
    }

    /// <summary>Re-enqueues dispatches whose Hangfire job was lost (worker restart, storage failover).</summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var staleAfter = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("WebPush:StalledDispatchMinutes", 30), 5, 1440));
        var limit = Math.Clamp(configuration.GetValue("WebPush:ReconciliationBatchSize", 20), 1, 200);
        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        if (backgroundJobClient is null) return;

        foreach (var dispatch in await dispatchRepository.GetStalledAsync(DateTime.UtcNow - staleAfter, limit, cancellationToken))
        {
            logger.LogInformation("Re-enqueueing stalled push dispatch {DispatchId}", dispatch.Id);
            backgroundJobClient.Enqueue<PushDispatchService>(service => service.ProcessBatchAsync(dispatch.Id, CancellationToken.None));
        }
    }

    private static string BuildPayload(PushDispatch dispatch) => JsonSerializer.Serialize(new
    {
        title = dispatch.Title,
        body = dispatch.Body,
        tag = dispatch.Id,
        url = dispatch.Destination,
        actions = dispatch.Actions.Select(action => new
        {
            action = action.Action,
            title = action.Title,
            url = action.Destination ?? dispatch.Destination
        })
    }, PayloadOptions);
}
