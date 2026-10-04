using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

/// <summary>
/// BackOffice Web Push composition: platform section gated by <c>push.platform.send</c>, channel-owner broadcasts
/// scoped by channel ownership, and the durable snapshot/fanout contract.
/// </summary>
[Trait("Category", "TestGroup:BackOffice")]
public sealed class PushCampaignTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public PushCampaignTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Platform_section_requires_the_canonical_push_permission()
    {
        using var denied = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        using var allowed = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);

        var deniedTargets = await denied.GetAsync("/api/pushcampaigns/targets");
        var deniedAudiences = await denied.GetAsync("/api/pushaudiences");
        var allowedTargets = await allowed.GetAsync("/api/pushcampaigns/targets");

        Assert.Equal(HttpStatusCode.Forbidden, deniedTargets.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedAudiences.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowedTargets.StatusCode);
    }

    [Fact]
    public async Task Audiences_are_named_channel_collections_validated_against_existing_channels()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);
        var code = $"audience-{Guid.NewGuid():N}";

        var created = await client.PostAsJsonAsync("/api/pushaudiences", new
        {
            code = code.ToUpperInvariant(),
            name = "Italian creators",
            channelIds = new[] { PrimaryScenario.ChannelId },
            isActive = true
        });
        var audience = await created.Content.ReadFromJsonAsync<PushAudienceContract>();
        var duplicate = await client.PostAsJsonAsync("/api/pushaudiences", new
        {
            code,
            name = "Duplicate",
            channelIds = new[] { PrimaryScenario.ChannelId }
        });
        var unknownChannels = await client.PostAsJsonAsync("/api/pushaudiences", new
        {
            code = $"unknown-{Guid.NewGuid():N}",
            name = "Unknown",
            channelIds = new[] { "channel-that-does-not-exist" }
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(code, audience!.Code);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownChannels.StatusCode);
    }

    [Fact]
    public async Task Platform_send_snapshots_active_consenting_subscriptions_and_is_idempotent()
    {
        var channelId = await SeedChannelAsync();
        var endpointHash = await SeedSubscriptionAsync(channelId, isActive: true);
        await SeedSubscriptionAsync(channelId, isActive: false);
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);

        var response = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Platform announcement",
            body = "A new episode is live.",
            destination = "/videos/latest",
            channelIds = new[] { channelId }
        });
        var dispatch = await response.Content.ReadFromJsonAsync<PushDispatchContract>();

        using var scope = factory.Services.CreateScope();
        var recipientRepository = scope.ServiceProvider.GetRequiredService<IPushDispatchRecipientRepository>();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IPushDispatchService>();

        // Re-queueing an already claimed dispatch must not duplicate the snapshot.
        await dispatchService.QueueAsync(dispatch!.Id);
        var recipients = await recipientRepository.GetItemsAsync(item => item.DispatchId == dispatch.Id);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(PushDispatchScope.Platform, dispatch.Scope);
        Assert.Equal("/videos/latest", dispatch.Destination);
        Assert.Single(recipients);
        Assert.Equal(endpointHash, recipients[0].EndpointHash);
        Assert.Equal($"{dispatch.Id}:{endpointHash}", recipients[0].IdempotencyKey);
    }

    [Fact]
    public async Task Platform_send_resolves_named_audiences_and_rejects_empty_targets()
    {
        var channelId = await SeedChannelAsync();
        var endpointHash = await SeedSubscriptionAsync(channelId, isActive: true);
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);
        var audience = await (await client.PostAsJsonAsync("/api/pushaudiences", new
        {
            code = $"audience-{Guid.NewGuid():N}",
            name = "Primary only",
            channelIds = new[] { channelId }
        })).Content.ReadFromJsonAsync<PushAudienceContract>();

        var viaAudience = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Audience send",
            body = "Targeted at a named collection.",
            audienceIds = new[] { audience!.Id }
        });
        var dispatch = await viaAudience.Content.ReadFromJsonAsync<PushDispatchContract>();
        var noTargets = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "No targets",
            body = "Nothing selected."
        });

        using var scope = factory.Services.CreateScope();
        var recipients = await scope.ServiceProvider.GetRequiredService<IPushDispatchRecipientRepository>()
            .GetItemsAsync(item => item.DispatchId == dispatch!.Id);

        Assert.Equal(HttpStatusCode.Accepted, viaAudience.StatusCode);
        Assert.Contains(endpointHash, recipients.Select(item => item.EndpointHash));
        Assert.Equal(HttpStatusCode.BadRequest, noTargets.StatusCode);
    }

    [Fact]
    public async Task Destinations_and_actions_must_stay_same_origin_and_respect_max_actions()
    {
        var channelId = await SeedChannelAsync();
        await SeedSubscriptionAsync(channelId, isActive: true);
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);

        var absoluteDestination = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Off-origin",
            body = "Should be rejected.",
            destination = "https://evil.example/landing",
            channelIds = new[] { channelId }
        });
        var protocolRelativeAction = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Off-origin action",
            body = "Should be rejected.",
            channelIds = new[] { channelId },
            actions = new[] { new { action = "open", title = "Open", destination = "//evil.example" } }
        });
        var tooManyActions = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Many actions",
            body = "Only the configured maximum survives.",
            channelIds = new[] { channelId },
            actions = new[]
            {
                new { action = "a", title = "A", destination = "/a" },
                new { action = "b", title = "B", destination = "/b" },
                new { action = "c", title = "C", destination = "/c" }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, absoluteDestination.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, protocolRelativeAction.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooManyActions.StatusCode);
    }

    [Fact]
    public async Task Platform_send_includes_only_platform_scoped_backoffice_subscriptions_without_channels()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPushSubscriptionRepository>();
        var endpoint = $"https://push.example.com/platform/{Guid.NewGuid():N}";
        var platformHash = PushEndpointProtection.HashEndpoint(endpoint);
        await repository.UpsertAsync(new PushChannelSubscription(
            platformHash, endpoint, new PushSubscriptionKeys("BPublicKeyMaterial", "AuthSecret"),
            PushEndpointProtection.HashCredential(PushEndpointProtection.CreateCredential()), [])
        {
            ApplicationKey = "backoffice",
            Scope = PushSubscriptionScope.Platform,
            IsActive = true,
            ConsentedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreationDateTime = DateTime.UtcNow
        });
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);

        var response = await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Platform only",
            body = "Administrative alert.",
            destination = "/",
            actions = Array.Empty<object>()
        });
        var dispatch = await response.Content.ReadFromJsonAsync<PushDispatchContract>();
        var recipients = await scope.ServiceProvider.GetRequiredService<IPushDispatchRecipientRepository>()
            .GetItemsAsync(item => item.DispatchId == dispatch!.Id);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Single(recipients);
        Assert.Equal(platformHash, recipients[0].EndpointHash);
    }

    [Fact]
    public async Task Channel_section_broadcasts_to_the_owned_channel_only()
    {
        var foreignChannelId = await SeedChannelAsync();
        var ownedHash = await SeedSubscriptionAsync(PrimaryScenario.ChannelId, isActive: true);
        var foreignHash = await SeedSubscriptionAsync(foreignChannelId, isActive: true);

        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        var response = await client.PostAsJsonAsync("/api/pushcampaigns/channel", new
        {
            title = "Channel broadcast",
            body = "Only my subscribers.",
            destination = "/news"
        });
        var dispatch = await response.Content.ReadFromJsonAsync<PushDispatchContract>();

        using var scope = factory.Services.CreateScope();
        var recipients = await scope.ServiceProvider.GetRequiredService<IPushDispatchRecipientRepository>()
            .GetItemsAsync(item => item.DispatchId == dispatch!.Id);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(PushDispatchScope.Channel, dispatch!.Scope);
        Assert.Equal(PrimaryScenario.ChannelId, dispatch.OwnerChannelId);
        Assert.Contains(ownedHash, recipients.Select(item => item.EndpointHash));
        Assert.DoesNotContain(foreignHash, recipients.Select(item => item.EndpointHash));
    }

    [Fact]
    public async Task Processing_a_batch_marks_recipients_sent_and_completes_the_dispatch()
    {
        var channelId = await SeedChannelAsync();
        await SeedSubscriptionAsync(channelId, isActive: true);
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PushPlatformSend);
        var dispatch = await (await client.PostAsJsonAsync("/api/pushcampaigns/platform", new
        {
            title = "Delivered",
            body = "Processed by the fanout job.",
            channelIds = new[] { channelId }
        })).Content.ReadFromJsonAsync<PushDispatchContract>();

        using var scope = factory.Services.CreateScope();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IPushDispatchService>();
        await dispatchService.ProcessBatchAsync(dispatch!.Id);
        // Repeating the batch must be a no-op rather than a second delivery.
        await dispatchService.ProcessBatchAsync(dispatch.Id);

        var recipients = await scope.ServiceProvider.GetRequiredService<IPushDispatchRecipientRepository>()
            .GetItemsAsync(item => item.DispatchId == dispatch.Id);
        var stored = await scope.ServiceProvider.GetRequiredService<IPushDispatchRepository>().GetItemAsync(dispatch.Id);

        Assert.All(recipients, recipient => Assert.Equal(PushDeliveryStatus.Sent, recipient.Status));
        Assert.All(recipients, recipient => Assert.Equal(1, recipient.AttemptCount));
        Assert.Equal(PushDispatchState.Sent, stored.State);
    }

    private async Task<string> SeedChannelAsync()
    {
        var channelId = $"channel-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, $"Channel {channelId}"));
        return channelId;
    }

    private async Task<string> SeedSubscriptionAsync(string channelId, bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPushSubscriptionRepository>();
        var endpoint = $"https://push.example.com/send/{Guid.NewGuid():N}";
        var endpointHash = PushEndpointProtection.HashEndpoint(endpoint);
        await repository.UpsertAsync(new PushChannelSubscription(
            endpointHash,
            endpoint,
            new PushSubscriptionKeys("BPublicKeyMaterial", "AuthSecret"),
            PushEndpointProtection.HashCredential(PushEndpointProtection.CreateCredential()),
            [channelId])
        {
            ApplicationKey = "morwalpizvideo",
            IsActive = isActive,
            ConsentedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreationDateTime = DateTime.UtcNow
        });
        return endpointHash;
    }
}
