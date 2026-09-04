using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ServerAskEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public ServerAskEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Public_campaign_hides_unpublished_and_cross_channel_campaigns()
    {
        var published = await AddCampaign("public", AskCampaignStatus.Published, PrimaryScenario.ChannelId);
        var draft = await AddCampaign("draft", AskCampaignStatus.Draft, PrimaryScenario.ChannelId);
        var foreign = await AddCampaign("foreign", AskCampaignStatus.Published, "missing-channel");
        using var client = factory.CreateClient();

        var channelName = Uri.EscapeDataString("Scenario channel");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/ask/{channelName}/{published.Slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/ask/{channelName}/{draft.Slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/ask/{channelName}/{foreign.Slug}")).StatusCode);
    }

    [Fact]
    public async Task Submission_maps_captcha_failure_duplicate_and_rate_limit_statuses()
    {
        var campaign = await AddCampaign($"submit-{Guid.NewGuid():N}", AskCampaignStatus.Published, PrimaryScenario.ChannelId,
            new AskPolicy { RecaptchaRequired = true, RateLimitPerHour = 1 });
        using var client = factory.CreateClient();
        var route = $"/api/ask/{Uri.EscapeDataString("Scenario channel")}/{campaign.Slug}/submissions";

        var captcha = await client.PostAsJsonAsync(route, new { text = "captcha", recaptchaToken = "invalid-token" });
        var created = await Post(client, route, "valid-token", "first", "one");
        var duplicate = await Post(client, route, "valid-token", "first", "two");
        var limited = await Post(client, route, "valid-token", "second", "three");

        Assert.Equal(HttpStatusCode.BadRequest, captcha.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal((HttpStatusCode)429, limited.StatusCode);
    }

    private async Task<AskCampaign> AddCampaign(string slug, AskCampaignStatus status, string channelId, AskPolicy? policy = null)
    {
        var campaign = new AskCampaign { ChannelId = channelId, Title = slug, Slug = slug, Status = status, Policy = policy ?? new AskPolicy { RecaptchaRequired = false } };
        return await factory.AskCampaignRepository!.AddItemAsync(campaign);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string route, string token, string text, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(new { text, recaptchaToken = token }) };
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
}