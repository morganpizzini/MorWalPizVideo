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