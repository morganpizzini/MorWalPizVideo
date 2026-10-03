using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.ShortLinks.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Services;
namespace MorWalPizVideo.ShortLinks.Tests.Features;

[Trait("Category", "TestGroup:ShortLinks")]
public class ShortLinksMockScenarioTests
{

    [Fact]
    public async Task Ask_shortlink_redirects_to_the_current_slug_and_hides_inactive_or_foreign_campaigns()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        var campaigns = factory.Services.GetRequiredService<IAskCampaignRepository>();
        var channels = factory.Services.GetRequiredService<IYTChannelRepository>();
        var links = factory.Services.GetRequiredService<IShortLinkRepository>();
        var channel = (await channels.GetItemsAsync()).Single(item => item.ChannelId == PrimaryScenario.ChannelId);
        var campaign = await campaigns.AddItemAsync(new AskCampaign
        {
            Id = "ask-campaign-1",
            ChannelId = channel.ChannelId,
            Slug = "first-slug",
            Status = AskCampaignStatus.Published
        });
        var link = await links.AddItemAsync(new ShortLink("ask-test-1", campaign.Id, [])
        {
            Id = "ask-link-1",
            LinkType = LinkType.AskCampaign,
            CampaignId = campaign.Id,
            ChannelId = campaign.ChannelId
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var first = await client.GetAsync($"/{link.Code}");
        Assert.Equal("https://ask.morwalpiz.com/Scenario channel/first-slug", Uri.UnescapeDataString(first.Headers.Location?.ToString() ?? string.Empty));

        await campaigns.UpdateItemAsync(campaign with { Slug = "second-slug" });
        var renamed = await client.GetAsync($"/{link.Code}");
        Assert.Equal("https://ask.morwalpiz.com/Scenario channel/second-slug", Uri.UnescapeDataString(renamed.Headers.Location?.ToString() ?? string.Empty));

        await campaigns.UpdateItemAsync(campaign with { Status = AskCampaignStatus.Draft });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/{link.Code}")).StatusCode);

        await campaigns.UpdateItemAsync(campaign with { Status = AskCampaignStatus.Published, ChannelId = "other-channel" });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/{link.Code}")).StatusCode);
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

    [Fact]
    public async Task Video_reference_shortlink_redirects_to_the_referenced_video_for_single_video_content()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var shortLinkRepository = factory.Services.GetRequiredService<IShortLinkRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        const string referencedVideoId = "scenario-video-ref";
        await matchRepository.UpdateItemAsync(source with
        {
            ContentType = YoutubeContentType.SingleVideo,
            ThumbnailVideoId = "scenario-main-video",
            VideoRefs = [new VideoRef(referencedVideoId, [], "Referenced video", channelIds: [PrimaryScenario.ChannelId])]
        });
        await shortLinkRepository.AddItemAsync(new ShortLink("video-ref-link", referencedVideoId, [])
        {
            Id = "400000000000000000000098",
            LinkType = LinkType.YouTubeVideo,
            ContentId = PrimaryScenario.MatchId,
            ManagementChannelId = PrimaryScenario.ChannelId
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/video-ref-link");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"https://www.youtube.com/watch?v={referencedVideoId}", response.Headers.Location?.ToString());
    }

[Fact]
    public async Task Newsletter_redirect_aggregates_click_context_without_identity_data()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        var shortLinkRepository = factory.Services.GetRequiredService<IShortLinkRepository>();
        await shortLinkRepository.AddItemAsync(new ShortLink("newsletter-link", "@morwalpiz", [])
        {
            LinkType = LinkType.YouTubeChannel,
            Id = "newsletter-link-id"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/newsletter-link?newsletterId=newsletter-1&channelId=channel-a");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var events = factory.Services.GetRequiredService<INewsletterEventRepository>();
        var click = (await events.GetItemsAsync(item => item.Type == NewsletterEventType.Click)).Single();
        Assert.Equal("channel-a", click.ChannelId);
        Assert.Equal("newsletter-1", click.NewsletterId);
        Assert.Equal("newsletter-link", click.ShortLinkContext);
        Assert.Equal(1, click.Count);
        Assert.Null(click.ProviderMessageId);
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
}
