using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Services;
using System.Security.Cryptography;
using System.Text;

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
    public async Task Ensure_video_short_link_uses_the_explicit_match_when_a_video_is_shared()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        var secondMatch = await matchRepository.AddItemAsync(source with
        {
            Id = "200000000000000000000099",
            ContentId = "second-content",
            ShortLinks = []
        });
        var linksService = factory.Services.GetRequiredService<ILinksService>();

        var shortLink = await linksService.EnsureVideoShortLinkAsync(
            secondMatch.Id,
            PrimaryScenario.VideoId,
            PrimaryScenario.ChannelId);

        Assert.Equal(secondMatch.Id, shortLink?.ContentId);
        Assert.Equal(PrimaryScenario.VideoId, shortLink?.Target);
    }

    [Fact]
    public async Task Failed_standalone_allocation_preserves_legacy_embedded_links()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        var legacyLink = new ShortLink("legacy-video", PrimaryScenario.VideoId, [])
        {
            Id = "400000000000000000000090",
            LinkType = LinkType.YouTubeVideo,
            ContentId = source.Id,
            ManagementChannelId = PrimaryScenario.ChannelId
        };
        await matchRepository.UpdateItemAsync(source with { ShortLinks = [legacyLink] });

        var shortLinkRepository = factory.Services.GetRequiredService<IShortLinkRepository>();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await shortLinkRepository.AddItemAsync(new ShortLink(CreateVideoCode(PrimaryScenario.VideoId, attempt), "occupied", []));
        }

        var linksService = factory.Services.GetRequiredService<ILinksService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            linksService.EnsureVideoShortLinkAsync(source.Id, PrimaryScenario.VideoId, PrimaryScenario.ChannelId));

        var persistedMatch = await matchRepository.GetItemAsync(source.Id);
        Assert.Contains(persistedMatch!.ShortLinks, link => link.Id == legacyLink.Id);
    }

    [Fact]
    public async Task Legacy_cleanup_does_not_replace_a_concurrent_video_reference()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        await matchRepository.UpdateItemAsync(source with
        {
            ShortLinks =
            [
                new ShortLink("legacy-video", PrimaryScenario.VideoId, [])
                {
                    LinkType = LinkType.YouTubeVideo,
                    ContentId = source.Id
                }
            ]
        });

        var linksService = factory.Services.GetRequiredService<ILinksService>();
        var appendTask = matchRepository.AddVideoReferenceAsync(
            source.Id,
            new VideoRef("concurrent-video", title: "Concurrent", channelIds: [PrimaryScenario.ChannelId]));
        var ensureTask = linksService.EnsureVideoShortLinkAsync(
            source.Id,
            PrimaryScenario.VideoId,
            PrimaryScenario.ChannelId);

        await Task.WhenAll(appendTask, ensureTask);

        var persistedMatch = await matchRepository.GetItemAsync(source.Id);
        Assert.Contains(persistedMatch!.VideoRefs, reference => reference.YoutubeId == "concurrent-video");
        Assert.Empty(persistedMatch.ShortLinks);
    }

    [Fact]
    public async Task Cleanup_failure_preserves_new_link_for_idempotent_retry()
    {
        var scenario = new EmptyScenario();
        var matchRepository = new FailingCleanupMatchRepository(scenario);
        var source = YouTubeContent.CreateSingleVideo(PrimaryScenario.VideoId, [])
            with
            {
                Id = PrimaryScenario.MatchId,
                VideoRefs =
                [
                    new VideoRef(PrimaryScenario.VideoId, channelIds: [PrimaryScenario.ChannelId])
                ],
                ShortLinks =
                [
                    new ShortLink("legacy-video", PrimaryScenario.VideoId, [])
                    {
                        LinkType = LinkType.YouTubeVideo,
                        ContentId = PrimaryScenario.MatchId
                    }
                ]
            };
        await matchRepository.AddItemAsync(source);

        var shortLinkRepository = new ShortLinkMockRepository(scenario);
        var linksService = new LinksService(
            shortLinkRepository,
            new QueryLinkMockRepository(scenario),
            matchRepository,
            new YTChannelMockRepository(scenario));

        var exception = await Assert.ThrowsAsync<ShortLinkCleanupPendingException>(() =>
            linksService.EnsureVideoShortLinkAsync(
                PrimaryScenario.MatchId,
                PrimaryScenario.VideoId,
                PrimaryScenario.ChannelId));

        Assert.Contains(
            (await matchRepository.GetItemAsync(PrimaryScenario.MatchId)).ShortLinks,
            link => link.Code == "legacy-video");
        Assert.Contains(
            await shortLinkRepository.GetItemsAsync(),
            link => link.Id == exception.Link.Id &&
                link.ContentId == PrimaryScenario.MatchId &&
                link.Target == PrimaryScenario.VideoId);
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

    private static string CreateVideoCode(string videoId, int attempt)
    {
        using var sha256 = SHA256.Create();
        var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes($"{videoId}:{attempt}")))
            .ToLowerInvariant();
        return hash[..5];
    }

    private sealed class FailingCleanupMatchRepository(IMockScenario scenario)
        : MatchMockRepository(scenario)
    {
        public override Task<bool> RemoveEmbeddedYouTubeLinksAsync(string matchId)
            => throw new InvalidOperationException("Simulated cleanup failure.");
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
