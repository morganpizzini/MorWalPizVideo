using System.Linq.Expressions;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class ContentServiceTests
{
    [Fact]
    public async Task FindMatch_uses_compatible_id_lookup_before_embedded_youtube_id()
    {
        const string youtubeId = "7NqjU6wmN2o";
        var thumbnailMatch = YouTubeContent.CreateSingleVideo("thumbnail-match", []) with
        {
            Id = "thumbnail-document-id",
            ThumbnailVideoId = youtubeId
        };
        var embeddedMatch = YouTubeContent.CreateSingleVideo("embedded-match", []) with
        {
            Id = "embedded-document-id",
            ThumbnailVideoId = "different-thumbnail",
            VideoRefs = [new VideoRef(youtubeId, [], channelIds: [])]
        };
        var repository = new GuardedYouTubeContentRepository([thumbnailMatch, embeddedMatch]);
        var service = new ContentService(repository, null!, null!, null!, null!);

        var thumbnailResult = await service.FindMatchAsync(youtubeId);
        var embeddedRepository = new GuardedYouTubeContentRepository([embeddedMatch]);
        var embeddedService = new ContentService(embeddedRepository, null!, null!, null!, null!);
        var embeddedResult = await embeddedService.FindMatchAsync(youtubeId);

        Assert.Same(thumbnailMatch, thumbnailResult);
        Assert.Same(embeddedMatch, embeddedResult);
        Assert.Equal(0, repository.GetItemCallCount);
        Assert.Equal(1, embeddedRepository.GetItemCallCount);
        Assert.DoesNotContain(repository.PredicateBodies, body => body.Contains(".Id ==", StringComparison.Ordinal));
        Assert.DoesNotContain(embeddedRepository.PredicateBodies, body => body.Contains(".Id ==", StringComparison.Ordinal));

        var objectIdRepository = new GuardedYouTubeContentRepository(
        [embeddedMatch with { Id = "507f1f77bcf86cd799439011", ThumbnailVideoId = "different-thumbnail" }]);
        var objectIdService = new ContentService(objectIdRepository, null!, null!, null!, null!);

        var objectIdResult = await objectIdService.FindMatchAsync("507f1f77bcf86cd799439011");

        Assert.Equal("507f1f77bcf86cd799439011", objectIdResult?.Id);
        Assert.Equal(1, objectIdRepository.GetItemCallCount);
    }

    private sealed class GuardedYouTubeContentRepository(IList<YouTubeContent> matches) : IYouTubeContentRepository
    {
        public int GetItemCallCount { get; private set; }
        public List<string> PredicateBodies { get; } = [];

        public Task<YouTubeContent> AddItemAsync(YouTubeContent item) => throw new NotSupportedException();

        public Task DeleteItemAsync(string id) => throw new NotSupportedException();

        public Task<YouTubeContent> GetItemAsync(string id)
        {
            GetItemCallCount++;
            return Task.FromResult(matches.FirstOrDefault(match => match.Id == id)!);
        }

        public Task<IList<YouTubeContent>> GetItemsAsync()
            => Task.FromResult<IList<YouTubeContent>>(matches);

        public Task<IList<YouTubeContent>> GetItemsAsync(Expression<Func<YouTubeContent, bool>> predicate)
        {
            var predicateBody = predicate.Body.ToString();
            PredicateBodies.Add(predicateBody);
            if (predicateBody.Contains(".Id ==", StringComparison.Ordinal))
            {
                throw new FormatException("Entity ID predicates must use the ObjectId-compatible repository lookup.");
            }

            return Task.FromResult<IList<YouTubeContent>>(matches.AsQueryable().Where(predicate).ToList());
        }

        public Task UpdateItemAsync(YouTubeContent item) => throw new NotSupportedException();

        public Task<IList<VideoPublication>> GetPublicationsAsync(DateTime fromInclusive, DateTime toExclusive, string? channelId = null)
            => throw new NotSupportedException();

        public Task<IList<YouTubeContent>> GetOwnedAsync(string userId, IList<string> channelIds)
            => throw new NotSupportedException();

        public Task<IList<YouTubeContent>> GetPublicOrderedAsync(bool includePrivate, int skip, int take)
            => throw new NotSupportedException();

        public Task<long> CountPublicAsync(bool includePrivate) => throw new NotSupportedException();

        public Task<IList<YouTubeContent>> GetPublicOrderedForChannelAsync(string channelId, int skip, int take)
            => throw new NotSupportedException();

        public Task<long> CountPublicForChannelAsync(string channelId) => throw new NotSupportedException();

        public Task<YouTubeContent?> GetByUrlAsync(string url, bool includePrivate)
            => throw new NotSupportedException();

        public Task<IList<YouTubeContent>> GetByIdsAsync(IList<string> ids, bool includePrivate)
            => throw new NotSupportedException();
    }
}