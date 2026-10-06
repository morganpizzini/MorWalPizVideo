using System.Net;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class DashboardControllerTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public DashboardControllerTests(BackOfficeWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Video_publications_includes_days_without_videos()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);

        var response = await client.GetAsync("/api/dashboard/video-publications?days=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var days = document.RootElement.EnumerateArray().ToArray();

        Assert.Equal(3, days.Length);
        Assert.Equal(3, days.Select(day => day.GetProperty("date").GetDateTime().Date)
            .Distinct()
            .Count());
        Assert.Contains(days, day => day.GetProperty("count").GetInt32() == 0 &&
            day.GetProperty("videos").GetArrayLength() == 0);
    }

    [Fact]
    public async Task Summary_counts_canonical_video_short_link_clicks_in_the_selected_channel()
    {
        await using var localFactory = new BackOfficeWebApplicationFactory();
        using var client = localFactory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeAccess);
        var before = await GetSummaryAsync(client);

        await localFactory.ShortLinkRepository!.AddItemAsync(new ShortLink(
            "dashboard-click-regression",
            PrimaryScenario.VideoId,
            [])
        {
            LinkType = LinkType.YouTubeVideo,
            ContentId = PrimaryScenario.MatchId,
            ClicksCount = 7
        });

        var after = await GetSummaryAsync(client);

        Assert.Equal(before.GetProperty("totalShortLinkClicks").GetInt64() + 7,
            after.GetProperty("totalShortLinkClicks").GetInt64());
    }

    [Fact]
    public async Task Short_link_clicks_returns_the_selected_range_as_buckets()
    {
        await using var localFactory = new BackOfficeWebApplicationFactory();
        using var client = localFactory.CreateClientWithPermissions(
            AuthorizationPermissionKeys.ShortLinksManage);
        var now = DateTime.UtcNow;
        await localFactory.ShortLinkRepository!.AddItemAsync(new ShortLink(
            "click-range-regression",
            "https://example.com",
            [])
        {
            LinkType = LinkType.YouTubeChannel,
            ChannelId = PrimaryScenario.ChannelId,
            ClicksCount = 2,
            ClickTimestamps = [now.AddHours(-2), now.AddHours(-25)]
        });

        var response = await client.GetAsync("/api/shortlinks/click-range-regression/clicks?range=24h");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var buckets = document.RootElement.EnumerateArray().ToArray();
        Assert.Equal(24, buckets.Length);
        Assert.Equal(1, buckets.Sum(bucket => bucket.GetProperty("count").GetInt32()));
    }

    private static async Task<JsonElement> GetSummaryAsync(HttpClient client)
    {
        using var document = JsonDocument.Parse(
            await (await client.GetAsync("/api/dashboard/summary")).Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
