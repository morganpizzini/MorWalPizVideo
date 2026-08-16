using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using System.Collections.Concurrent;

namespace MorWalPizVideo.Server.Services;

public sealed record YouTubeContentCacheIndexEntry(string EntityId, string Scope);

public interface IIndexedCacheStore
{
    bool IsEnabled { get; }
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, T?>> GetManyAsync<T>(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public interface IYouTubeContentIndexedCache
{
    Task<IReadOnlyList<YouTubeContent>> GetPublicForChannelAsync(string channelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<YouTubeContent>> GetGlobalAsync(CancellationToken cancellationToken = default);
    Task NotifyChangedAsync(string entityId);
    Task DrainAsync(CancellationToken cancellationToken = default);
}

public static class YouTubeContentCacheKeys
{
    public const string IndexPrefix = "youtubecontent:index";
    public const string EntryPrefix = "youtubecontent:entry";
    public const string PublicScopePrefix = "public-channel";
    public const string AdminScope = "admin-global";
    public const string PublicScopeRegistry = "youtubecontent:public-scopes";

    public static string PublicChannelScope(string channelId)
        => $"{PublicScopePrefix}:{Uri.EscapeDataString(channelId.Trim())}";

    public static string Index(string scope) => $"{IndexPrefix}:{NormalizeSegment(scope)}";

    public static string Entry(string scope, string entityId)
        => $"{EntryPrefix}:{NormalizeSegment(scope)}:{NormalizeSegment(entityId)}";

    private static string NormalizeSegment(string value)
        => Uri.EscapeDataString(value.Trim().ToLowerInvariant());
}

public sealed class YouTubeContentIndexedCache(
    IIndexedCacheStore store,
    IServiceScopeFactory scopeFactory,
    ILogger<YouTubeContentIndexedCache> logger) : IYouTubeContentIndexedCache
{
    private sealed record RefreshRequest(string EntityId, string? Scope, string QueueKey);

    private readonly ConcurrentQueue<RefreshRequest> refreshQueue = new();
    private readonly ConcurrentDictionary<string, byte> pendingKeys = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim drainLock = new(1, 1);
    private readonly object workerLock = new();
    private readonly SemaphoreSlim scopeRegistryLock = new(1, 1);
    private Task? workerTask;

    public int PendingRefreshCount => pendingKeys.Count;

    public async Task<IReadOnlyList<YouTubeContent>> GetPublicForChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default)
    {
        var scope = YouTubeContentCacheKeys.PublicChannelScope(channelId);
        await RegisterPublicScopeAsync(scope, cancellationToken);
        return await ReadScopeAsync(scope, cancellationToken);
    }

    public Task<IReadOnlyList<YouTubeContent>> GetGlobalAsync(CancellationToken cancellationToken = default)
        => ReadScopeAsync(YouTubeContentCacheKeys.AdminScope, cancellationToken);

    public Task NotifyChangedAsync(string entityId)
    {
        if (!store.IsEnabled || string.IsNullOrWhiteSpace(entityId))
            return Task.CompletedTask;

        Enqueue(entityId, scope: null);
        return Task.CompletedTask;
    }

    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Task? worker;
            lock (workerLock)
            {
                worker = workerTask;
            }

            if (worker is not null)
                await worker.WaitAsync(cancellationToken);

            if (pendingKeys.IsEmpty && refreshQueue.IsEmpty)
                return;

            StartWorker();
        }
    }

    private async Task<IReadOnlyList<YouTubeContent>> ReadScopeAsync(
        string scope,
        CancellationToken cancellationToken)
    {
        if (!store.IsEnabled)
            return await BuildEntriesAsync(scope, entityIds: null, cancellationToken);

        var index = await store.GetAsync<List<YouTubeContentCacheIndexEntry>>(
            YouTubeContentCacheKeys.Index(scope), cancellationToken);
        if (index is null)
            return await RebuildScopeAsync(scope, cancellationToken);

        var entryKeys = index
            .Where(entry => !string.IsNullOrWhiteSpace(entry.EntityId))
            .Select(entry => YouTubeContentCacheKeys.Entry(scope, entry.EntityId))
            .ToArray();
        var entries = await store.GetManyAsync<YouTubeContent>(entryKeys, cancellationToken);
        if (entries.Count != entryKeys.Length || entries.Values.Any(entry => entry is null))
            return await RebuildScopeAsync(scope, cancellationToken);

        var materialized = entries.Values
            .Where(entry => entry is not null)
            .Cast<YouTubeContent>()
            .ToList();
        if (materialized.Count != index.Count ||
            materialized.Any(entry => !index.Any(indexEntry => indexEntry.EntityId == entry.Id)))
            return await RebuildScopeAsync(scope, cancellationToken);

        return SortEntries(scope, materialized);
    }

    private async Task<IReadOnlyList<YouTubeContent>> RebuildScopeAsync(
        string scope,
        CancellationToken cancellationToken)
    {
        var entries = await BuildEntriesAsync(scope, entityIds: null, cancellationToken);
        await PersistScopeAsync(scope, entries, cancellationToken);
        return entries;
    }

    private async Task<IReadOnlyList<YouTubeContent>> BuildEntriesAsync(
        string scope,
        IReadOnlyCollection<string>? entityIds,
        CancellationToken cancellationToken)
    {
        await using var serviceScope = scopeFactory.CreateAsyncScope();
        var repository = serviceScope.ServiceProvider.GetRequiredService<IYouTubeContentRepository>();
        var shortLinkRepository = serviceScope.ServiceProvider.GetRequiredService<IShortLinkRepository>();
        var sourceEntries = entityIds is null
            ? await repository.GetItemsAsync()
            : await repository.GetByIdsAsync(entityIds.ToList(), includePrivate: true);

        IEnumerable<YouTubeContent> filteredEntries = sourceEntries;
        if (scope.StartsWith(YouTubeContentCacheKeys.PublicScopePrefix + ":", StringComparison.Ordinal))
        {
            var channelId = Uri.UnescapeDataString(scope[(YouTubeContentCacheKeys.PublicScopePrefix.Length + 1)..]);
            filteredEntries = sourceEntries.Where(match =>
                !match.IsPrivate && match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)));
        }

        var matches = filteredEntries.ToList();
        if (matches.Count == 0)
            return [];

        var contentIds = matches
            .Select(match => match.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var canonicalLinks = await shortLinkRepository.GetItemsAsync(link =>
            link.LinkType == LinkType.YouTubeVideo &&
            link.ContentId != null &&
            contentIds.Contains(link.ContentId));

        var enriched = matches.Select(match =>
        {
            var linksForMatch = canonicalLinks.Where(link => link.ContentId == match.Id &&
                match.VideoRefs.Any(video => video.YoutubeId == link.Target)).ToList();
            var legacyLinks = match.ShortLinks.Where(link => link.LinkType != LinkType.YouTubeVideo);
            return match with { ShortLinks = [.. legacyLinks, .. linksForMatch] };
        }).ToList();

        return SortEntries(scope, enriched);
    }

    private static IReadOnlyList<YouTubeContent> SortEntries(
        string scope,
        IEnumerable<YouTubeContent> entries)
        => scope.StartsWith(YouTubeContentCacheKeys.PublicScopePrefix + ":", StringComparison.Ordinal)
                ? entries.OrderByDescending(entry => entry.LatestPublishedAt == DateTime.MinValue
                    ? entry.CalculateLatestPublishedAt()
                    : entry.LatestPublishedAt)
                .ThenByDescending(entry => entry.CreationDateTime)
                .ToList()
            : entries.OrderByDescending(entry => entry.CreationDateTime).ToList();

    private async Task PersistScopeAsync(
        string scope,
        IReadOnlyList<YouTubeContent> entries,
        CancellationToken cancellationToken,
        IReadOnlyCollection<YouTubeContentCacheIndexEntry>? previousIndex = null)
    {
        previousIndex ??= await store.GetAsync<List<YouTubeContentCacheIndexEntry>>(
            YouTubeContentCacheKeys.Index(scope), cancellationToken);
        var currentIds = entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var oldEntry in previousIndex ?? [])
        {
            if (!currentIds.Contains(oldEntry.EntityId))
                await store.RemoveAsync(YouTubeContentCacheKeys.Entry(scope, oldEntry.EntityId), cancellationToken);
        }

        foreach (var entry in entries)
            await store.SetAsync(YouTubeContentCacheKeys.Entry(scope, entry.Id), entry, cancellationToken);

        var index = entries
            .Select(entry => new YouTubeContentCacheIndexEntry(entry.Id, scope))
            .ToList();
        await store.SetAsync(YouTubeContentCacheKeys.Index(scope), index, cancellationToken);
        await store.RemoveAsync(CacheKeys.Matches, cancellationToken);
    }

    private void Enqueue(string entityId, string? scope)
    {
        var normalizedId = entityId.Trim();
        var normalizedScope = scope?.Trim().ToLowerInvariant() ?? "*";
        var queueKey = $"{normalizedScope}::{normalizedId.ToLowerInvariant()}";
        if (!pendingKeys.TryAdd(queueKey, 0))
            return;

        refreshQueue.Enqueue(new RefreshRequest(normalizedId, scope, queueKey));
        StartWorker();
    }

    private void StartWorker()
    {
        lock (workerLock)
        {
            if (workerTask is null || workerTask.IsCompleted)
                workerTask = Task.Run(DrainQueueAsync);
        }
    }

    private async Task DrainQueueAsync()
    {
        await drainLock.WaitAsync();
        try
        {
            while (refreshQueue.TryDequeue(out var request))
            {
                try
                {
                    if (request.Scope is null)
                    {
                        await RefreshScopeAsync(YouTubeContentCacheKeys.AdminScope, request.EntityId, CancellationToken.None);
                        var publicScopes = await GetPublicScopesAsync(CancellationToken.None);
                        foreach (var scope in publicScopes)
                            await RefreshScopeAsync(scope, request.EntityId, CancellationToken.None);
                    }
                    else
                    {
                        await RefreshScopeAsync(request.Scope, request.EntityId, CancellationToken.None);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Indexed YouTubeContent cache refresh failed for {EntityId} in {Scope}",
                        request.EntityId, request.Scope ?? "*");
                }
                finally
                {
                    pendingKeys.TryRemove(request.QueueKey, out _);
                }
            }
        }
        finally
        {
            drainLock.Release();
            lock (workerLock)
            {
                workerTask = null;
                if (!refreshQueue.IsEmpty)
                    workerTask = Task.Run(DrainQueueAsync);
            }
        }
    }

    private async Task RefreshScopeAsync(string scope, string entityId, CancellationToken cancellationToken)
    {
        if (!store.IsEnabled)
            return;

        var index = await store.GetAsync<List<YouTubeContentCacheIndexEntry>>(
            YouTubeContentCacheKeys.Index(scope), cancellationToken);
        if (index is null)
            return;

        var current = await ReadScopeAsync(scope, cancellationToken);
        var oldEntries = current.Where(entry => entry.Id == entityId).ToList();
        var rebuilt = await BuildEntriesAsync(scope, [entityId], cancellationToken);
        var merged = SortEntries(scope, current.Where(entry => !oldEntries.Contains(entry)).Concat(rebuilt));
        await PersistScopeAsync(scope, merged, cancellationToken, index);
    }

    private async Task RegisterPublicScopeAsync(string scope, CancellationToken cancellationToken)
    {
        if (!store.IsEnabled)
            return;

        await scopeRegistryLock.WaitAsync(cancellationToken);
        try
        {
            var scopes = await GetPublicScopesAsync(cancellationToken);
            if (!scopes.Contains(scope, StringComparer.Ordinal))
            {
                scopes.Add(scope);
                await store.SetAsync(YouTubeContentCacheKeys.PublicScopeRegistry, scopes, cancellationToken);
            }
        }
        finally
        {
            scopeRegistryLock.Release();
        }
    }

    private async Task<List<string>> GetPublicScopesAsync(CancellationToken cancellationToken)
        => (await store.GetAsync<List<string>>(
            YouTubeContentCacheKeys.PublicScopeRegistry, cancellationToken))?.ToList() ?? [];
}