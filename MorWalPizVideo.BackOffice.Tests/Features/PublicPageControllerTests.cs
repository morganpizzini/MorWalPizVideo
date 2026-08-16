using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PublicPageControllerTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public PublicPageControllerTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Public_pages_exclude_drafts_and_do_not_expose_channel_identity()
    {
        var slug = $"public-page-{Guid.NewGuid():N}";
        var pages = factory.Services.GetRequiredService<IPageRepository>();
        await pages.AddItemAsync(new Page("", "Draft page", "<p>Draft</p>", slug, "")
        {
            Id = $"draft-{Guid.NewGuid():N}",
            ChannelId = PrimaryScenario.ChannelId,
            Status = PageStatus.Draft
        });

        using var client = factory.CreateClient();
        var draftResponse = await client.GetAsync($"/api/Pages/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, draftResponse.StatusCode);

        await pages.AddItemAsync(new Page("", "Published page", "<p>Published</p>", slug, "")
        {
            Id = $"published-{Guid.NewGuid():N}",
            ChannelId = PrimaryScenario.ChannelId,
            Status = PageStatus.Published
        });

        var publishedResponse = await client.GetAsync($"/api/Pages/{slug}");
        var body = JsonDocument.Parse(await publishedResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, publishedResponse.StatusCode);
        Assert.Equal("Published page", body.RootElement.GetProperty("title").GetString());
        Assert.False(body.RootElement.TryGetProperty("channelId", out _));
    }
}