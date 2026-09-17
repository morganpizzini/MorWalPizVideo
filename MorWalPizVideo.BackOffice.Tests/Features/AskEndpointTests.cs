using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AskEndpointTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public AskEndpointTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Ask_routes_enforce_authentication_permission_and_channel_scope()
    {
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        using var denied = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        using var allowed = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskView);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/Ask")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/api/Ask")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await allowed.GetAsync("/api/Ask")).StatusCode);
    }

    [Fact]
    public async Task Ask_create_update_and_read_are_channel_scoped_and_invalidate_cache()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskManage);
        var slug = $"endpoint-{Guid.NewGuid():N}";
        var request = new { title = "Endpoint campaign", description = "Description", slug, status = AskCampaignStatus.Published };

        var create = await client.PostAsJsonAsync("/api/Ask", request);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        var update = await client.PutAsJsonAsync($"/api/Ask/{id}", new { title = "Updated", request.description, request.slug, request.status });
        using var otherChannel = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskManage);
        otherChannel.DefaultRequestHeaders.Remove("X-Channel-Id");
        otherChannel.DefaultRequestHeaders.Add("X-Channel-Id", "missing-channel");
        var hidden = await otherChannel.GetAsync($"/api/Ask/{id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Contains(CacheKeys.Ask, factory.CrossApiService.ResetKeys);

        var link = (await factory.ShortLinkRepository!.GetItemsAsync())
            .Single(item => item.LinkType == LinkType.AskCampaign && item.CampaignId == id);
        var share = await client.GetAsync($"/api/Ask/{id}/share");
        var sharePayload = await share.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"https://shorts.morwalpiz.com/{link.Code}", sharePayload.GetProperty("shareUrl").GetString());

        var updatedSlug = $"updated-{Guid.NewGuid():N}";
        await client.PutAsJsonAsync($"/api/Ask/{id}", new { title = "Updated", request.description, slug = updatedSlug, request.status });
        var updatedShare = await client.GetAsync($"/api/Ask/{id}/share");
        var updatedSharePayload = await updatedShare.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(sharePayload.GetProperty("shareUrl").GetString(), updatedSharePayload.GetProperty("shareUrl").GetString());
    }

    [Fact]
    public async Task Ask_telegram_publish_requires_moderation_and_returns_server_built_url()
    {
        var channel = (await factory.YTChannelRepository!.GetItemsAsync()).Single(item => item.ChannelId == PrimaryScenario.ChannelId);
        var protector = new SocialPublishingSecretProtector(Options.Create(new SocialPublishingOptions
        {
            EncryptionKey = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE="
        }));
        await factory.YTChannelRepository.UpdateItemAsync(channel with
        {
            SocialPublishing = new SocialPublishingConfiguration
            {
                Telegram = new SocialPublishingProviderConfiguration
                {
                    DestinationId = "test-chat",
                    CredentialCiphertext = protector.Protect("test-token", channel.ChannelId, "telegram")
                }
            }
        });

        using var manage = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskManage);
        var create = await manage.PostAsJsonAsync("/api/Ask", new
        {
            title = "Telegram campaign",
            description = "Description",
            slug = $"telegram-{Guid.NewGuid():N}",
            status = AskCampaignStatus.Published
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        using var denied = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskView);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.PostAsync($"/api/Ask/{id}/publish-telegram", null)).StatusCode);

        using var moderator = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskModerate);
        var publish = await moderator.PostAsync($"/api/Ask/{id}/publish-telegram", null);
        var payload = await publish.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        var link = (await factory.ShortLinkRepository!.GetItemsAsync())
            .Single(item => item.LinkType == LinkType.AskCampaign && item.CampaignId == id);
        Assert.Equal("https://shorts.morwalpiz.com/" + link.Code, payload.GetProperty("url").GetString());
        Assert.Equal("Telegram campaign", payload.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Ask_telegram_publish_hides_cross_channel_campaigns()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AskManage);
        var create = await client.PostAsJsonAsync("/api/Ask", new
        {
            title = "Scoped campaign",
            description = "Description",
            slug = $"scoped-{Guid.NewGuid():N}",
            status = AskCampaignStatus.Published
        });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        client.DefaultRequestHeaders.Remove("X-Channel-Id");
        client.DefaultRequestHeaders.Add("X-Channel-Id", "missing-channel");

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/Ask/{id}/publish-telegram", null)).StatusCode);
    }
}