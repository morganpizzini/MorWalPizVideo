using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

[Trait("Category", "TestGroup:Shared")]
public sealed class ExternalDataServiceTests
{
    [Fact]
    public async Task Refresh_retains_all_attachments_and_unavailable_metadata_despite_legacy_single_type()
    {
        var (service, repository, youtube) = CreateService();
        var match = await repository.AddItemAsync(CreateEditorial() with
        {
            ContentType = YoutubeContentType.SingleVideo,
            VideoRefs = [CreateReference("one"), CreateReference("unavailable"), CreateReference("three")]
        });
        youtube.UnavailableIds.Add("unavailable");

        var updated = await service.RefreshMatch(match.Id);

        Assert.Equal(["one", "unavailable", "three"], Assert.Single(youtube.Requests));
        Assert.NotNull(updated);
        Assert.Equal(match.VideoRefs.Select(video => video.YoutubeId), updated.VideoRefs.Select(video => video.YoutubeId));
        Assert.Equivalent(match.VideoRefs[1], updated.VideoRefs[1], strict: true);
        Assert.Equal("Provider one", updated.VideoRefs[0].Title);
        Assert.Equal("Provider three", updated.VideoRefs[2].Title);
        Assert.Equal(match.Title, updated.Title);
        Assert.Equal(match.Description, updated.Description);
        Assert.Equal(match.Url, updated.Url);
        Assert.Equal(match.ThumbnailVideoId, updated.ThumbnailVideoId);
        Assert.Equal(match.ContentType, updated.ContentType);
        for (var index = 0; index < updated.VideoRefs.Length; index++)
        {
            Assert.Equal(match.VideoRefs[index].Categories, updated.VideoRefs[index].Categories);
            Assert.Equal(match.VideoRefs[index].ChannelIds, updated.VideoRefs[index].ChannelIds);
            Assert.Equal(match.VideoRefs[index].CreationDateTime, updated.VideoRefs[index].CreationDateTime);
        }
        Assert.Equal(youtube.PublishedAt, updated.CreationDateTime);
        Assert.Equal(3, (await repository.GetItemAsync(match.Id)).VideoRefs.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task One_reference_editorial_content_keeps_aggregate_text_during_refresh_and_fetch(bool fetch)
    {
        var (service, repository, youtube) = CreateService();
        var match = await repository.AddItemAsync(CreateEditorial() with
        {
            VideoRefs = [CreateReference("sole-video") with { Title = "", Description = "" }]
        });

        var updated = fetch ? Assert.Single(await service.FetchMatches()) : await service.RefreshMatch(match.Id);

        Assert.Equal(["sole-video"], Assert.Single(youtube.Requests));
        Assert.NotNull(updated);
        Assert.True(updated.IsLink);
        Assert.Equal(match.Title, updated.Title);
        Assert.Equal(match.Description, updated.Description);
        Assert.Equal(match.Url, updated.Url);
        Assert.Equal(match.ThumbnailVideoId, updated.ThumbnailVideoId);
        Assert.Equal("Provider sole-video", Assert.Single(updated.VideoRefs).Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Imported_root_refresh_uses_reference_identity_instead_of_thumbnail(bool fetch)
    {
        var (service, repository, youtube) = CreateService();
        var match = await repository.AddItemAsync(YouTubeContent.CreateSingleVideo("root-video", []) with
        {
            Id = "root-document",
            ThumbnailVideoId = "unattached-thumbnail",
            VideoRefs = [CreateReference("root-video")]
        });

        var updated = fetch ? Assert.Single(await service.FetchMatches()) : await service.RefreshMatch(match.Id);

        Assert.Equal(["root-video"], Assert.Single(youtube.Requests));
        Assert.NotNull(updated);
        Assert.Equal("Provider root-video", updated.Title);
        Assert.Equal("Provider description root-video", updated.Description);
        Assert.Equal("provider-root-video", updated.Url);
        Assert.Equal("unattached-thumbnail", updated.ThumbnailVideoId);
        Assert.Equal(youtube.PublishedAt, updated.CreationDateTime);
        Assert.Equal(match.VideoRefs[0].CreationDateTime, updated.VideoRefs[0].CreationDateTime);
    }

    [Fact]
    public async Task Empty_content_does_not_fetch_or_invent_a_reference()
    {
        var (service, repository, youtube) = CreateService();
        var match = await repository.AddItemAsync(CreateEditorial() with { ContentType = YoutubeContentType.SingleVideo });

        var refreshed = await service.RefreshMatch(match.Id);
        var fetched = Assert.Single(await service.FetchMatches());

        Assert.Empty(youtube.Requests);
        Assert.Empty(refreshed!.VideoRefs);
        Assert.Empty(fetched.VideoRefs);
        Assert.Equal(match.Title, fetched.Title);
    }

    [Fact]
    public async Task Provider_failure_propagates_without_persisting_partial_changes()
    {
        var (service, repository, youtube) = CreateService();
        var match = await repository.AddItemAsync(CreateEditorial() with { VideoRefs = [CreateReference("one")] });
        youtube.ShouldFail = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshMatch(match.Id));
        Assert.Equivalent(match, await repository.GetItemAsync(match.Id), strict: true);
    }

    private static (ExternalDataService Service, MatchMockRepository Repository, RecordingYouTubeService YouTube) CreateService()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var data = new MinimalDataService(repository, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var youtube = new RecordingYouTubeService();
        return (new ExternalDataService(data, youtube), repository, youtube);
    }

    private static YouTubeContent CreateEditorial() =>
        YouTubeContent.CreateCollection("editorial-content", "Editorial title", "Editorial description",
            "editorial-slug", "unattached-thumbnail", []) with
        {
            Id = "editorial-document",
            CreationDateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    private static VideoRef CreateReference(string id) => new(id, [new CategoryRef("category", "Category")],
        "Original title", "Original description", channelIds: ["channel"])
    {
        CreationDateTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
    };

    private sealed class RecordingYouTubeService : YTServiceMock
    {
        public List<string[]> Requests { get; } = [];
        public HashSet<string> UnavailableIds { get; } = [];
        public bool ShouldFail { get; set; }
        public DateTime PublishedAt { get; } = new(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc);

        public override Task<IList<Video>> FetchFromYoutube(IList<string> videoIds)
        {
            Requests.Add(videoIds.ToArray());
            if (ShouldFail) throw new InvalidOperationException("Simulated provider failure");
            return Task.FromResult<IList<Video>>(videoIds.Where(id => !UnavailableIds.Contains(id))
                .Select(id => new Video(id, $"Provider {id}", $"Provider description {id}",
                    1, 2, 3, PublishedAt, "thumbnail", "PT1M"))
                .ToList());
        }
    }
}
