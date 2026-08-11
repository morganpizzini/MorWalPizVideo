using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class ShortLinksMockScenarioTests
{
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
    public async Task Embedded_video_link_resolves_and_increments_the_content_link()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/{PrimaryScenario.MatchShortLinkCode}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"https://www.youtube.com/watch?v={PrimaryScenario.VideoId}", response.Headers.Location?.ToString());
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var match = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        Assert.Equal(1, match.ShortLinks.Single().ClicksCount);
    }

    [Fact]
    public async Task Embedded_video_link_outside_configured_channel_is_not_resolvable()
    {
        await using var factory = new ShortLinksWebApplicationFactory();
        var matchRepository = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matchRepository.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        await matchRepository.AddItemAsync(source with
        {
            Id = "200000000000000000000099",
            OwnerChannelId = "other-channel",
            VideoRefs = source.VideoRefs.Select(video => video with { ChannelIds = ["other-channel"] }).ToArray(),
            ShortLinks = [new ShortLink("other1", PrimaryScenario.VideoId, [])]
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/other1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
