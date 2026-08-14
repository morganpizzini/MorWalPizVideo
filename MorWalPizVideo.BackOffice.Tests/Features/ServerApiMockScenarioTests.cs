using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class ServerApiMockScenarioTests
{
    [Fact]
    public async Task Matches_endpoint_reads_the_canonical_scenario()
    {
        await using var factory = new ServerApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var scenario = factory.Services.GetRequiredService<IMockScenario>();
    var expectedMatchId = scenario.Read<YouTubeContent>("matches").First().Id;

        var response = await client.GetAsync("/api/Matches?take=1000");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedMatchId, content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shooting_ita_endpoint_returns_only_isSHIT_channels_and_related_matches()
    {
        await using var factory = new ServerApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var channelId = $"shit-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, "Shooting test channel") { IsSHIT = true });
        var videoId = $"video-{Guid.NewGuid():N}";
        var matchId = $"match-{Guid.NewGuid():N}";
        await factory.MatchRepository!.AddItemAsync(YouTubeContent.CreateSingleVideo(videoId, []) with
        {
            Id = matchId,
            VideoRefs = [new VideoRef(videoId, channelIds: [channelId])]
        });

        var channelsResponse = await client.GetAsync("/api/shit/channels");
        var matchesResponse = await client.GetAsync("/api/shit/matches?take=100");
        var channelsContent = await channelsResponse.Content.ReadAsStringAsync();
        var matchesContent = await matchesResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, channelsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, matchesResponse.StatusCode);
        Assert.Contains(channelId, channelsContent, StringComparison.Ordinal);
        Assert.Contains(matchId, matchesContent, StringComparison.Ordinal);
        Assert.DoesNotContain(PrimaryScenario.ChannelId, channelsContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shooting_ita_quicklink_lookup_uses_isSHIT_channel_visibility()
    {
        await using var factory = new ServerApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var shootingChannelId = $"shit-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(shootingChannelId, "Shooting link channel")
        {
            IsSHIT = true
        });
        var shootingSlug = $"shooting-{Guid.NewGuid():N}";
        var regularSlug = $"regular-{Guid.NewGuid():N}";
        await factory.QuickLinksRepository!.AddItemAsync(new QuickLinks("Shooting links", null, shootingSlug, [])
        {
            ChannelId = shootingChannelId
        });
        await factory.QuickLinksRepository.AddItemAsync(new QuickLinks("Regular links", null, regularSlug, [])
        {
            ChannelId = PrimaryScenario.ChannelId
        });

        var shootingResponse = await client.GetAsync($"/api/shit/quicklinks/{shootingSlug}");
        var regularResponse = await client.GetAsync($"/api/shit/quicklinks/{regularSlug}");

        Assert.Equal(HttpStatusCode.OK, shootingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, regularResponse.StatusCode);
    }

    [Fact]
    public async Task Shooting_ita_channelnews_filters_status_and_non_shit_channels_and_uses_logo_fallback()
    {
        await using var factory = new ServerApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var publicChannelId = $"shit-news-{Guid.NewGuid():N}";
        var regularChannelId = $"regular-news-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(publicChannelId, "Shooting news") { IsSHIT = true });
        await factory.YTChannelRepository.AddItemAsync(new YTChannel(regularChannelId, "Regular news"));

        var now = DateTime.UtcNow;
        await factory.ChannelNewsRepository!.AddItemAsync(new ChannelNews { Id = "public-news", ChannelId = publicChannelId, Title = "Published", Status = ChannelNewsStatus.Published, Images = [new ChannelNewsImage { StorageKey = "private-key", PublicUrl = "https://cdn/image.jpg" }] });
        await factory.ChannelNewsRepository.AddItemAsync(new ChannelNews { Id = "past-news", ChannelId = publicChannelId, Title = "Past schedule", Status = ChannelNewsStatus.Scheduled, PublicationTimeUtc = now.AddMinutes(-1) });
        await factory.ChannelNewsRepository.AddItemAsync(new ChannelNews { Id = "future-news", ChannelId = publicChannelId, Title = "Future schedule", Status = ChannelNewsStatus.Scheduled, PublicationTimeUtc = now.AddMinutes(10) });
        await factory.ChannelNewsRepository.AddItemAsync(new ChannelNews { Id = "draft-news", ChannelId = publicChannelId, Title = "Draft", Status = ChannelNewsStatus.Draft });
        await factory.ChannelNewsRepository.AddItemAsync(new ChannelNews { Id = "regular-news", ChannelId = regularChannelId, Title = "Regular", Status = ChannelNewsStatus.Published });

        var response = await client.GetAsync("/api/shit/channelnews?take=100");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("public-news", content, StringComparison.Ordinal);
        Assert.Contains("past-news", content, StringComparison.Ordinal);
        Assert.DoesNotContain("future-news", content, StringComparison.Ordinal);
        Assert.DoesNotContain("draft-news", content, StringComparison.Ordinal);
        Assert.DoesNotContain("regular-news", content, StringComparison.Ordinal);
        Assert.Contains("/images/logo-150.png", content, StringComparison.Ordinal);
        Assert.DoesNotContain("private-key", content, StringComparison.Ordinal);
    }
}
