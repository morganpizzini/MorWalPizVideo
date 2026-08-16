using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class YouTubeContentIndexedCacheTests
{
    [Fact]
    public void Cache_keys_use_distinct_global_and_public_channel_scopes()
    {
        var publicScope = YouTubeContentCacheKeys.PublicChannelScope("Channel/One");

        Assert.Equal("public-channel:Channel%2FOne", publicScope);
        Assert.NotEqual(
            YouTubeContentCacheKeys.Index(publicScope),
            YouTubeContentCacheKeys.Index(YouTubeContentCacheKeys.AdminScope));
        Assert.Equal(
            "youtubecontent:entry:admin-global:content-id",
            YouTubeContentCacheKeys.Entry(YouTubeContentCacheKeys.AdminScope, "Content-Id"));
    }

    [Fact]
    public async Task Initial_population_filters_public_entries_and_preserves_channel_order()
    {
        var channelId = PrimaryScenario.ChannelId;
        var baseDate = DateTime.UtcNow.AddDays(-10);
        var newest = CreateMatch("newest", channelId, baseDate.AddDays(3));
        var older = CreateMatch("older", channelId, baseDate.AddDays(1));
        var privateMatch = CreateMatch("private", channelId, baseDate.AddDays(5)) with { IsPrivate = true };
        var otherChannel = CreateMatch("other", "other-channel", baseDate.AddDays(6));
        var repository = new MatchMockRepository(new EmptyScenario());
        await repository.AddItemAsync(older);
        await repository.AddItemAsync(privateMatch);
        await repository.AddItemAsync(otherChannel);
        await repository.AddItemAsync(newest);
        var cache = CreateCache(repository, new InMemoryIndexedCacheStore());

        var matches = await cache.GetPublicForChannelAsync(channelId);

        Assert.Equal([newest.Id, older.Id], matches.Select(match => match.Id));
    }

    [Fact]
    public async Task Missing_entry_rebuilds_index_and_targeted_update_and_delete_are_applied()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var match = CreateMatch("match", PrimaryScenario.ChannelId, DateTime.UtcNow);
        await repository.AddItemAsync(match);
        var store = new InMemoryIndexedCacheStore();
        var cache = CreateCache(repository, store);
        var scope = YouTubeContentCacheKeys.PublicChannelScope(PrimaryScenario.ChannelId);

        await cache.GetPublicForChannelAsync(PrimaryScenario.ChannelId);
        await store.RemoveAsync(YouTubeContentCacheKeys.Entry(scope, match.Id));
        var recovered = await cache.GetPublicForChannelAsync(PrimaryScenario.ChannelId);
        Assert.Single(recovered);

        var updated = match with { Title = "updated" };
        await repository.UpdateItemAsync(updated);
        var writesBeforeRefresh = store.SetCount;
        store.BlockNextPublicIndexRead();
        await cache.NotifyChangedAsync(match.Id);
        await store.RefreshBlocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cache.NotifyChangedAsync(match.Id);
        Assert.Equal(1, cache.PendingRefreshCount);
        store.ReleaseBlockedRead();
        await cache.DrainAsync();
        Assert.Equal(2, store.SetCount - writesBeforeRefresh);
        Assert.Equal("updated", (await cache.GetPublicForChannelAsync(PrimaryScenario.ChannelId)).Single().Title);

        await repository.DeleteItemAsync(match.Id);
        await cache.NotifyChangedAsync(match.Id);
        await cache.DrainAsync();
        Assert.Empty(await cache.GetPublicForChannelAsync(PrimaryScenario.ChannelId));
        Assert.True(store.GetCount > 0);
    }

    [Fact]
    public async Task Content_service_notifies_cache_after_successful_mutations()
    {
        var repository = new MatchMockRepository(new EmptyScenario());
        var notifier = new RecordingIndexedCache();
        var service = new ContentService(
            repository,
            null!,
            null!,
            null!,
            new ShortLinkMockRepository(new EmptyScenario()),
            notifier);
        var match = CreateMatch("mutation", PrimaryScenario.ChannelId, DateTime.UtcNow);

        Assert.True(await service.SaveMatchAsync(match));
        await service.UpdateMatchAsync(match with { Title = "changed" });
        await service.DeleteMatchAsync(match.Id);

        Assert.Equal([match.Id, match.Id, match.Id], notifier.ChangedIds);
    }

    private static YouTubeContent CreateMatch(string id, string channelId, DateTime publishedAt)
        => YouTubeContent.CreateSingleVideo(id, []) with
        {
            Id = id,
            CreationDateTime = publishedAt,
            VideoRefs = [new VideoRef(id, publishedAt: publishedAt, channelIds: [channelId])]
        };

    private static YouTubeContentIndexedCache CreateCache(
        IYouTubeContentRepository repository,
        InMemoryIndexedCacheStore store)
    {
        var services = new ServiceCollection();
        services.AddScoped<IYouTubeContentRepository>(_ => repository);
        services.AddScoped<IShortLinkRepository>(_ => new ShortLinkMockRepository(new EmptyScenario()));
        var provider = services.BuildServiceProvider();
        return new YouTubeContentIndexedCache(
            store,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<YouTubeContentIndexedCache>.Instance);
    }

    private sealed class InMemoryIndexedCacheStore : IIndexedCacheStore
    {
        private readonly Dictionary<string, object> values = new(StringComparer.Ordinal);
        private TaskCompletionSource<bool>? blockedRead;
        private TaskCompletionSource<bool>? activeReadGate;
        public bool IsEnabled => true;
        public int GetCount { get; private set; }
        public int SetCount { get; private set; }
        public TaskCompletionSource<bool> RefreshBlocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void BlockNextPublicIndexRead()
            => blockedRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseBlockedRead()
            => activeReadGate?.TrySetResult(true);

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            GetCount++;
            var readGate = blockedRead;
            if (readGate is not null && key.StartsWith(
                    YouTubeContentCacheKeys.Index(YouTubeContentCacheKeys.PublicScopePrefix),
                    StringComparison.Ordinal) &&
                Interlocked.CompareExchange(ref blockedRead, null, readGate) == readGate)
            {
                activeReadGate = readGate;
                RefreshBlocked.TrySetResult(true);
                await readGate.Task.WaitAsync(cancellationToken);
            }

            return values.TryGetValue(key, out var value) && value is T typed ? typed : default;
        }

        public Task<IReadOnlyDictionary<string, T?>> GetManyAsync<T>(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, T?>>(
                keys.ToDictionary(key => key, key => values.TryGetValue(key, out var value) && value is T typed ? typed : default, StringComparer.Ordinal));

        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            values[key] = value!;
            SetCount++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            values.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingIndexedCache : IYouTubeContentIndexedCache
    {
        public List<string> ChangedIds { get; } = [];
        public Task<IReadOnlyList<YouTubeContent>> GetPublicForChannelAsync(string channelId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<YouTubeContent>>([]);
        public Task<IReadOnlyList<YouTubeContent>> GetGlobalAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<YouTubeContent>>([]);
        public Task NotifyChangedAsync(string entityId)
        {
            ChangedIds.Add(entityId);
            return Task.CompletedTask;
        }
        public Task DrainAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
