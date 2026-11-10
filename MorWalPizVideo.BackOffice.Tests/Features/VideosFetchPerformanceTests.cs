using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class VideosFetchPerformanceTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Fetch_preserves_scoped_order_and_observes_link_changes_without_content_refresh(bool enableCache, bool admin)
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IIndexedCacheStore>(new MorWalPizIndexedCacheStore(
                    new MorWalPizMemoryCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))),
                    enableCache))));
        using var client = configuredFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", admin
            ? AuthorizationPermissionKeys.BackofficeManageAll
            : AuthorizationPermissionKeys.VideosView);
        const string channelId = "fetch-selected-channel";
        client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        using var scope = configuredFactory.Services.CreateScope();
        var services = scope.ServiceProvider;
        await services.GetRequiredService<IYTChannelRepository>().AddItemAsync(new YTChannel(channelId, "Selected"));
        await services.GetRequiredService<IUserChannelOwnerRepository>().AddItemAsync(
            new UserChannelOwner { UserId = "test-user-id", ChannelId = channelId });
        var contents = services.GetRequiredService<IYouTubeContentRepository>();
        var privateMatch = YouTubeContent.CreateSingleVideo("private-video", []) with
        {
            Id = "private-match",
            OwnerChannelId = channelId,
            IsPrivate = true,
            CreationDateTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            VideoRefs = [new VideoRef("private-video")]
        };
        var referenced = YouTubeContent.CreateSingleVideo("referenced-video", []) with
        {
            Id = "referenced-match",
            OwnerChannelId = "other-channel",
            CreationDateTime = privateMatch.CreationDateTime.AddDays(-1),
            VideoRefs = [new VideoRef("referenced-video", channelIds: [channelId])]
        };
        var unrelated = privateMatch with
        {
            Id = "unrelated-match",
            OwnerChannelId = "other-channel",
            CreatorUserId = "test-user-id",
            CreationDateTime = privateMatch.CreationDateTime.AddDays(1)
        };
        foreach (var match in new[] { referenced, unrelated, privateMatch })
            await contents.AddItemAsync(match);
        var links = services.GetRequiredService<IShortLinkRepository>();
        var original = await links.AddItemAsync(new ShortLink("original", "private-video", [])
        {
            ContentId = privateMatch.Id
        });

        var first = await FetchAsync(client);
        Assert.Equal(["private-match", "referenced-match"], first.Select(match => match.Id));
        Assert.True(first[0].IsPrivate);
        Assert.Equal("original", Assert.Single(first[0].ShortLinks).Code);
        var cachedSnapshot = await services.GetRequiredService<IYouTubeContentIndexedCache>().GetGlobalAsync();
        Assert.Equal("original", Assert.Single(cachedSnapshot.Single(match => match.Id == privateMatch.Id).ShortLinks).Code);
        var second = await FetchAsync(client);
        Assert.Equal(first.Select(match => match.Id), second.Select(match => match.Id));

        await links.UpdateItemAsync(original with { Code = "updated" });
        Assert.Equal("updated", Assert.Single((await FetchAsync(client))[0].ShortLinks).Code);
        await links.DeleteItemAsync(original.Id);
        Assert.Empty((await FetchAsync(client))[0].ShortLinks);
        await links.AddItemAsync(new ShortLink("replacement", "private-video", []) { ContentId = privateMatch.Id });
        Assert.Equal("replacement", Assert.Single((await FetchAsync(client))[0].ShortLinks).Code);

        var detail = await client.GetAsync($"/api/Videos/{privateMatch.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var payload = await detail.Content.ReadFromJsonAsync<YouTubeContentContract>();
        Assert.NotNull(payload);
        Assert.Equal("replacement", Assert.Single(payload.ShortLinks).Code);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Videos/{unrelated.Id}")).StatusCode);
        Assert.Equal("original", Assert.Single(cachedSnapshot.Single(match => match.Id == privateMatch.Id).ShortLinks).Code);
    }

    private static async Task<List<YouTubeContentContract>> FetchAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/Videos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<YouTubeContentContract>>();
        Assert.NotNull(payload);
        return payload;
    }
}
