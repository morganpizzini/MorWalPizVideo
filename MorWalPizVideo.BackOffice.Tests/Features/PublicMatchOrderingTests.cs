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
    public async Task Public_channel_matches_exclude_private_and_other_channels_and_apply_ordering_tie_break()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var baseDate = DateTime.UtcNow.AddDays(-30);
        var channelId = PrimaryScenario.ChannelId;

        var newestMatch = YouTubeContent.CreateSingleVideo("newest-video", []) with
        {
            Id = "newest-match",
            CreationDateTime = baseDate.AddDays(1),
            VideoRefs = [new VideoRef("newest-video", publishedAt: baseDate.AddDays(10), channelIds: [channelId])]
        };
        var tieOlderMatch = YouTubeContent.CreateSingleVideo("tie-older-video", []) with
        {
            Id = "tie-older-match",
            CreationDateTime = baseDate.AddDays(2),
            VideoRefs = [new VideoRef("tie-older-video", publishedAt: baseDate.AddDays(8), channelIds: [channelId])]
        };
        var tieNewerMatch = YouTubeContent.CreateSingleVideo("tie-newer-video", []) with
        {
            Id = "tie-newer-match",
            CreationDateTime = baseDate.AddDays(3),
            VideoRefs = [new VideoRef("tie-newer-video", publishedAt: baseDate.AddDays(8), channelIds: [channelId])]
        };
        var fallbackMatch = YouTubeContent.CreateSingleVideo("fallback-video", []) with
        {
            Id = "fallback-match",
            CreationDateTime = baseDate.AddDays(5),
            VideoRefs = [new VideoRef("fallback-video", channelIds: [channelId])]
        };
        var olderMatch = YouTubeContent.CreateSingleVideo("older-video", []) with
        {
            Id = "older-match",
            CreationDateTime = baseDate,
            VideoRefs = [new VideoRef("older-video", publishedAt: baseDate.AddDays(2), channelIds: [channelId])]
        };
        var privateMatch = YouTubeContent.CreateSingleVideo("private-video", []) with
        {
            Id = "private-match",
            IsPrivate = true,
            VideoRefs = [new VideoRef("private-video", publishedAt: baseDate.AddDays(20), channelIds: [channelId])]
        };
        var otherChannelMatch = YouTubeContent.CreateSingleVideo("other-channel-video", []) with
        {
            Id = "other-channel-match",
            VideoRefs = [new VideoRef("other-channel-video", publishedAt: baseDate.AddDays(30), channelIds: ["other-channel"])]
        };

        await repository.AddItemAsync(olderMatch);
        await repository.AddItemAsync(fallbackMatch);
        await repository.AddItemAsync(newestMatch);
        await repository.AddItemAsync(tieOlderMatch);
        await repository.AddItemAsync(tieNewerMatch);
        await repository.AddItemAsync(privateMatch);
        await repository.AddItemAsync(otherChannelMatch);

        var ordered = await repository.GetPublicOrderedForChannelAsync(channelId, 0, 200);

        Assert.Equal(
            new[] { newestMatch.Id, tieNewerMatch.Id, tieOlderMatch.Id, fallbackMatch.Id, olderMatch.Id },
            ordered.Select(match => match.Id));
        Assert.DoesNotContain(privateMatch.Id, ordered.Select(match => match.Id));
        Assert.DoesNotContain(otherChannelMatch.Id, ordered.Select(match => match.Id));
    }

    [Fact]
    public async Task Public_channel_matches_apply_skip_and_take_after_ordering()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var baseDate = DateTime.UtcNow.AddDays(-30);
        var channelId = PrimaryScenario.ChannelId;

        foreach (var (id, publishedAt) in new[]
        {
            ("first-match", baseDate.AddDays(3)),
            ("second-match", baseDate.AddDays(2)),
            ("third-match", baseDate.AddDays(1))
        })
        {
            await repository.AddItemAsync(YouTubeContent.CreateSingleVideo(id, []) with
            {
                Id = id,
                VideoRefs = [new VideoRef(id, publishedAt: publishedAt, channelIds: [channelId])]
            });
        }

        var page = await repository.GetPublicOrderedForChannelAsync(channelId, 1, 1);

        Assert.Equal(["second-match"], page.Select(match => match.Id));
    }

    [Fact]
    public async Task Adding_a_video_reference_updates_latest_published_at_and_ordering()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var channelId = PrimaryScenario.ChannelId;
        var originalPublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newerPublishedAt = originalPublishedAt.AddDays(10);
        var match = YouTubeContent.CreateCollection("ordering-content", "Ordering", string.Empty, string.Empty, "thumbnail", []) with
        {
            Id = "ordering-match",
            CreationDateTime = originalPublishedAt,
            VideoRefs = [new VideoRef("original", publishedAt: originalPublishedAt, channelIds: [channelId])],
            LatestPublishedAt = DateTime.MinValue
        };
        await repository.AddItemAsync(match);
        await repository.AddItemAsync(YouTubeContent.CreateSingleVideo("older-match-video", []) with
        {
            Id = "older-ordering-match",
            CreationDateTime = originalPublishedAt.AddDays(2),
            VideoRefs = [new VideoRef("older-match-video", publishedAt: originalPublishedAt.AddDays(1), channelIds: [channelId])]
        });

        var result = await repository.AddVideoReferenceAsync(
            match.Id,
            new VideoRef("newer", publishedAt: newerPublishedAt, channelIds: [channelId]));

        Assert.Equal(VideoReferenceAppendResult.Added, result);
        var updated = await repository.GetItemAsync(match.Id);
        Assert.Equal(newerPublishedAt, updated.LatestPublishedAt);
        Assert.Equal(match.Id, (await repository.GetPublicOrderedForChannelAsync(channelId, 0, 10)).First().Id);
    }

    [Fact]
    public async Task Adding_an_invalid_published_date_does_not_reset_latest_published_at()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var channelId = PrimaryScenario.ChannelId;
        var publishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var match = YouTubeContent.CreateCollection("legacy-content", "Legacy", string.Empty, string.Empty, "thumbnail", []) with
        {
            Id = "legacy-match",
            CreationDateTime = publishedAt,
            VideoRefs = [new VideoRef("original", publishedAt: publishedAt, channelIds: [channelId])],
            LatestPublishedAt = DateTime.MinValue
        };
        await repository.AddItemAsync(match);

        await repository.AddVideoReferenceAsync(
            match.Id,
            new VideoRef("missing-date", publishedAt: DateTime.MinValue, channelIds: [channelId]));

        var updated = await repository.GetItemAsync(match.Id);
        Assert.Equal(publishedAt, updated.LatestPublishedAt);
    }

    [Fact]
    public async Task Adding_an_older_reference_keeps_the_latest_date_for_a_legacy_match()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var channelId = PrimaryScenario.ChannelId;
        var latestPublishedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var match = YouTubeContent.CreateCollection("legacy-ordering-content", "Legacy", string.Empty, string.Empty, "thumbnail", []) with
        {
            Id = "legacy-ordering-match",
            CreationDateTime = latestPublishedAt,
            VideoRefs = [new VideoRef("latest", publishedAt: latestPublishedAt, channelIds: [channelId])],
            LatestPublishedAt = DateTime.MinValue
        };
        await repository.AddItemAsync(match);

        await repository.AddVideoReferenceAsync(
            match.Id,
            new VideoRef("older", publishedAt: latestPublishedAt.AddDays(-1), channelIds: [channelId]));

        Assert.Equal(latestPublishedAt, (await repository.GetItemAsync(match.Id)).LatestPublishedAt);
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