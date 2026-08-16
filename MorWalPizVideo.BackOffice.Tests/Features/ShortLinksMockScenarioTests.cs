using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class ShortLinksMockScenarioTests
{
    [Fact]
    public async Task Ensure_video_short_link_creates_a_canonical_standalone_record()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var linksService = factory.Services.GetRequiredService<ILinksService>();

        var shortLink = await linksService.EnsureVideoShortLinkAsync(PrimaryScenario.VideoId, PrimaryScenario.ChannelId);

        Assert.NotNull(shortLink);
        Assert.Equal(PrimaryScenario.VideoId, shortLink.Target);
        Assert.Equal(PrimaryScenario.MatchId, shortLink.ContentId);
        var shortLinkRepository = factory.Services.GetRequiredService<IShortLinkRepository>();
        var persistedLink = (await shortLinkRepository.GetItemsAsync())
            .Single(link => link.Id == shortLink!.Id);
        Assert.Equal(LinkType.YouTubeVideo, persistedLink.LinkType);
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var match = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        Assert.Empty(match.ShortLinks);
    }

    [Fact]
    public async Task Standalone_link_resolves_from_the_code_initialized_scenario()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/{PrimaryScenario.StandaloneShortLinkCode}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.test/scenario", response.Headers.Location?.ToString());
        var repository = factory.Services.GetRequiredService<IShortLinkRepository>();
        var updatedLink = (await repository.GetItemsAsync())
            .Single(link => link.Code == PrimaryScenario.StandaloneShortLinkCode);
        Assert.Equal(1, updatedLink.ClicksCount);
    }

    [Fact]
    public async Task Canonical_video_link_outside_configured_channel_is_not_resolvable()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var shortLinkRepository = factory.Services.GetRequiredService<IShortLinkRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        const string otherMatchId = "200000000000000000000099";
        await matchRepository.AddItemAsync(source with
        {
            Id = otherMatchId,
            OwnerChannelId = "other-channel",
            VideoRefs = source.VideoRefs.Select(video => video with { ChannelIds = ["other-channel"] }).ToArray(),
            ShortLinks = []
        });
        await shortLinkRepository.AddItemAsync(new ShortLink("other1", PrimaryScenario.VideoId, [])
        {
            Id = "400000000000000000000099",
            LinkType = LinkType.YouTubeVideo,
            ContentId = otherMatchId,
            ManagementChannelId = "other-channel"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/other1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Canonical_video_link_resolves_without_embedded_data_and_counts_clicks_atomically()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        var repository = factory.Services.GetRequiredService<IShortLinkRepository>();
        await repository.AddItemAsync(new ShortLink("canonical1", PrimaryScenario.VideoId, [])
        {
            Id = "400000000000000000000099",
            LinkType = LinkType.YouTubeVideo,
            ContentId = PrimaryScenario.MatchId,
            ManagementChannelId = PrimaryScenario.ChannelId
        });
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        await matchRepository.UpdateItemAsync(source with { ShortLinks = [] });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var responses = await Task.WhenAll(client.GetAsync("/canonical1"), client.GetAsync("/canonical1"));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));
        Assert.All(responses, response => Assert.Equal(
            $"https://www.youtube.com/watch?v={PrimaryScenario.VideoId}",
            response.Headers.Location?.ToString()));
        var updatedLink = (await repository.GetItemsAsync()).Single(link => link.Code == "canonical1");
        Assert.Equal(2, updatedLink.ClicksCount);
        Assert.Empty((await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId).ShortLinks);
    }
}
