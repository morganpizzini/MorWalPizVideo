using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class LinksServiceTests
{
    [Fact]
    public async Task Merge_queries_selected_aggregate_ids_and_preserves_canonical_selection_and_order()
    {
        var repository = new CountingShortLinkRepository();
        var longest = Link("longest", "match", "video-a", "utm_source=long");
        var winner = Link("winner", "match", "video-a", "a");
        var tied = Link("tied", "match", "video-a", "b");
        var second = Link("second", "match", "video-b");
        var unrelated = Link("unrelated", "other-match", "video-a");
        var wrongIdentity = Link("content-id-not-document-id", "business-id", "video-a");
        var nullContent = Link("null-content", "match", "video-a") with { ContentId = null };
        var blankContent = Link("blank-content", " ", "video-a");
        var nonYouTube = Link("embedded-other", "match", "https://example.test") with { LinkType = LinkType.CustomUrl };
        var legacy = Link("legacy", "match", "video-c");
        foreach (var link in new[] { longest, winner, tied, second, unrelated, wrongIdentity, nullContent, blankContent, nonYouTube })
            await repository.AddItemAsync(link);
        var match = YouTubeContent.CreateSingleVideo("business-id", []) with
        {
            Id = "match",
            VideoRefs =
            [
                new VideoRef("video-b"),
                new VideoRef("video-a"),
                new VideoRef("video-a"),
                new VideoRef("video-c")
            ],
            ShortLinks = [legacy, nonYouTube]
        };
        var emptyMatch = YouTubeContent.CreateSingleVideo("empty", []) with { Id = "no-links", ShortLinks = [legacy] };
        var service = new LinksService(repository, null!, null!, null!);

        var result = await service.MergeCanonicalVideoShortLinksAsync([match, emptyMatch]);

        Assert.Equal(["second", "winner", "winner", "embedded-other"], result[0].ShortLinks.Select(link => link.Code));
        Assert.Empty(result[1].ShortLinks);
        Assert.Equal(["legacy", "embedded-other"], match.ShortLinks.Select(link => link.Code));
        Assert.Equal(["match", "no-links"], result.Select(item => item.Id));
        Assert.Equal(1, repository.ReadCount);
        Assert.Equal(["longest", "winner", "tied", "second"], repository.ReturnedCodes);

        var predicate = Assert.IsAssignableFrom<Expression<Func<ShortLink, bool>>>(repository.Predicate);
        var rendered = new ExpressionFilterDefinition<ShortLink>(predicate).Render(
            new RenderArgs<ShortLink>(BsonSerializer.LookupSerializer<ShortLink>(), BsonSerializer.SerializerRegistry));
        var contentFilter = Assert.IsType<BsonDocument>(rendered["contentId"]);
        Assert.Equal(["match", "no-links"], contentFilter["$in"].AsBsonArray.Select(value => value.AsString).Order());
    }

    [Fact]
    public async Task Merge_refreshes_links_on_repeated_reads_of_the_same_cached_match()
    {
        var repository = new CountingShortLinkRepository();
        var match = YouTubeContent.CreateSingleVideo("business", []) with
        {
            Id = "match",
            VideoRefs = [new VideoRef("video")],
            ShortLinks = [Link("stale", "match", "video")]
        };
        var service = new LinksService(repository, null!, null!, null!);
        Assert.Empty((await service.MergeCanonicalVideoShortLinksAsync([match])).Single().ShortLinks);

        var created = await repository.AddItemAsync(Link("created", match.Id, "video"));
        Assert.Equal("created", Assert.Single((await service.MergeCanonicalVideoShortLinksAsync([match])).Single().ShortLinks).Code);
        await repository.UpdateItemAsync(created with { Code = "updated" });
        Assert.Equal("updated", Assert.Single((await service.MergeCanonicalVideoShortLinksAsync([match])).Single().ShortLinks).Code);
        await repository.DeleteItemAsync(created.Id);
        Assert.Empty((await service.MergeCanonicalVideoShortLinksAsync([match])).Single().ShortLinks);
        Assert.Equal("stale", Assert.Single(match.ShortLinks).Code);
    }

    [Fact]
    public async Task Empty_merge_does_not_query_links()
    {
        var service = new LinksService(null!, null!, null!, null!);

        Assert.Empty(await service.MergeCanonicalVideoShortLinksAsync([]));
    }

    [Fact]
    public async Task Code_availability_checks_legacy_embedded_links_case_insensitively()
    {
        var scenario = new EmptyScenario();
        var matchRepository = new MatchMockRepository(scenario);
        var channelRepository = new YTChannelMockRepository(scenario);
        await matchRepository.AddItemAsync(
            YouTubeContent.CreateSingleVideo("match", []) with
            {
                Id = "match",
                ShortLinks = [new ShortLink("LegacyVideo", "video", [])]
            });
        await channelRepository.AddItemAsync(
            new YTChannel("channel-id", "Channel")
            {
                ShortLinks = [new ShortLink("LegacyChannel", "channel", [])]
            });
        var service = new LinksService(
            new ShortLinkMockRepository(scenario),
            new QueryLinkMockRepository(scenario),
            matchRepository,
            channelRepository);

        Assert.False(await service.IsCodeAvailableAsync("legacyvideo"));
        Assert.False(await service.IsCodeAvailableAsync("LEGACYCHANNEL"));

        var predicate = (Expression<Func<YouTubeContent, bool>>)(match =>
            match.ShortLinks.Any(link => link.Code.ToLower() == "legacyvideo"));
        var rendered = new ExpressionFilterDefinition<YouTubeContent>(predicate).Render(
            new RenderArgs<YouTubeContent>(
                BsonSerializer.LookupSerializer<YouTubeContent>(),
                BsonSerializer.SerializerRegistry));
        Assert.NotEmpty(rendered);
    }

    [Fact]
    public async Task Merge_does_not_hide_repository_failures()
    {
        var repository = new CountingShortLinkRepository { Failure = new InvalidOperationException("Links unavailable") };
        var service = new LinksService(repository, null!, null!, null!);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MergeCanonicalVideoShortLinksAsync([YouTubeContent.CreateSingleVideo("match", [])]));

        Assert.Same(repository.Failure, exception);
    }

    private static ShortLink Link(string code, string contentId, string target, string? query = null)
        => new(code, target, query is null ? [] : [new QueryLink("query", query)])
        {
            Id = code,
            ContentId = contentId,
            CreationDateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    private sealed class CountingShortLinkRepository()
        : ShortLinkMockRepository(new EmptyScenario()), IShortLinkRepository
    {
        public int ReadCount { get; private set; }
        public Expression<Func<ShortLink, bool>>? Predicate { get; private set; }
        public string[] ReturnedCodes { get; private set; } = [];
        public Exception? Failure { get; init; }

        public new async Task<IList<ShortLink>> GetItemsAsync(Expression<Func<ShortLink, bool>> predicate)
        {
            ReadCount++;
            Predicate = predicate;
            if (Failure is not null)
                throw Failure;
            var links = await base.GetItemsAsync(predicate);
            ReturnedCodes = links.Select(link => link.Code).ToArray();
            return links;
        }
    }
}
