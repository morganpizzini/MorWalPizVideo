using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class CreatorPartnershipEndpointTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public CreatorPartnershipEndpointTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Partner_and_campaign_crud_is_channel_scoped_and_timeline_is_embedded()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PartnersManage, AuthorizationPermissionKeys.CampaignsManage);
        var partnerResponse = await client.PostAsJsonAsync("/api/Partners", new { name = $"Partner {Guid.NewGuid():N}", contactEmail = "creator@example.com", status = PartnerStatus.Active });
        var partner = await partnerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var partnerId = partner.GetProperty("id").GetString();

        var campaignResponse = await client.PostAsJsonAsync("/api/Campaigns", new { partnerId, title = "Launch", status = CampaignStatus.Active, contentReferences = new[] { "video-1" } });
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<JsonElement>();
        var campaignId = campaign.GetProperty("id").GetString();
        var timelineResponse = await client.PostAsJsonAsync($"/api/Campaigns/{campaignId}/timeline", new { kind = TimelineEntryKind.Deliverable, title = "Draft video", url = "https://example.com/draft" });
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, partnerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, campaignResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        Assert.NotEqual(string.Empty, timeline.GetProperty("timelineEntries")[0].GetProperty("id").GetString());

        client.DefaultRequestHeaders.Remove("X-Channel-Id");
        client.DefaultRequestHeaders.Add("X-Channel-Id", "missing-channel");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Campaigns/{campaignId}")).StatusCode);
    }

    [Fact]
    public async Task Campaign_collection_defaults_to_active_records()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PartnersManage, AuthorizationPermissionKeys.CampaignsManage);
        var partnerResponse = await client.PostAsJsonAsync("/api/Partners", new { name = $"Archived partner {Guid.NewGuid():N}" });
        var partner = await partnerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var partnerId = partner.GetProperty("id").GetString();
        await client.PostAsJsonAsync("/api/Campaigns", new { partnerId, title = "Completed", status = CampaignStatus.Completed });

        var active = await client.GetFromJsonAsync<JsonElement[]>("/api/Campaigns");
        var all = await client.GetFromJsonAsync<JsonElement[]>("/api/Campaigns?includeArchived=true");

        Assert.Empty(active!);
        Assert.Contains(all!, item => item.GetProperty("status").GetInt32() == (int)CampaignStatus.Completed);
    }
}