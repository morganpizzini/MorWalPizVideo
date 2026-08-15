using MongoDB.Bson;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PublicMatchOrderingTests
{
    [Fact]
    public async Task Public_channel_matches_are_ordered_by_latest_video_publication_with_creation_fallback()
    {
        throw new InvalidOperationException(typeof(MatchMockRepository).Assembly.Location);
        var repository = new MatchMockRepository(new EmptyScenario());
        var baseDate = DateTime.UtcNow.AddDays(-30);
        var channelId = PrimaryScenario.ChannelId;

        var olderMatch = YouTubeContent.CreateSingleVideo("older-video", []) with
        {
            Id = "older-match",
            CreationDateTime = baseDate,
            VideoRefs = [new VideoRef("older-video", publishedAt: baseDate.AddDays(2), channelIds: [channelId])]
        };
        var fallbackMatch = YouTubeContent.CreateSingleVideo("fallback-video", []) with
        {
            Id = "fallback-match",
            CreationDateTime = baseDate.AddDays(5),
            VideoRefs = [new VideoRef("fallback-video", channelIds: [channelId])]
        };
        var newestMatch = YouTubeContent.CreateCollection("newest-match", "", "", "", "", []) with
        {
            Id = "newest-match",
            CreationDateTime = baseDate.AddDays(1),
            VideoRefs =
            [
                new VideoRef("old-video", publishedAt: baseDate.AddDays(3), channelIds: [channelId]),
                new VideoRef("new-video", publishedAt: baseDate.AddDays(10), channelIds: [channelId])
            ]
        };

        Assert.Equal(baseDate.AddDays(10), newestMatch.CalculateLatestPublishedAt());

        await repository.AddItemAsync(olderMatch);
        await repository.AddItemAsync(fallbackMatch);
        await repository.AddItemAsync(newestMatch);

        var storedNewestMatch = (await repository.GetItemsAsync(match => match.Id == newestMatch.Id)).Single();
        Assert.Equal(newestMatch.CalculateLatestPublishedAt(), storedNewestMatch.CalculateLatestPublishedAt());
        var storedFallbackMatch = (await repository.GetItemsAsync(match => match.Id == fallbackMatch.Id)).Single();
        Assert.True(
            storedNewestMatch.CalculateLatestPublishedAt() > storedFallbackMatch.CalculateLatestPublishedAt());

        var ordered = await repository.GetPublicOrderedForChannelAsync(channelId, 0, 200);

        Assert.Equal(new[] { newestMatch.Id, fallbackMatch.Id, olderMatch.Id }, ordered.Take(3).Select(match => match.Id));
        Assert.Equal(newestMatch.VideoRefs.Max(video => video.PublishedAt), ordered[0].CalculateLatestPublishedAt());
        Assert.Equal(fallbackMatch.CreationDateTime, ordered[1].CalculateLatestPublishedAt());
    }

    [Fact]
    public void Manifest_contains_the_cosmos_public_match_ordering_index()
    {
        var entry = MongoIndexOperationsService.Manifest.Single(item =>
            item.Key == "youtubecontent_isprivate_latestpublished_creation_desc");

        Assert.Equal(DbCollections.YouTubeContent, entry.Collection);
        Assert.Equal("ix_youtubecontent_isprivate_latestpublished_creation_desc", entry.Name);
        Assert.Equal(
            new BsonDocument
            {
                { "isPrivate", 1 },
                { "latestPublishedAt", -1 },
                { "creationDateTime", -1 }
            }.ToJson(),
            entry.Keys.ToJson());
    }
}