# Indexed Entity Cache With Targeted Background Refresh

## Purpose

This specification describes a reusable cache pattern for read-heavy entity lists where:

- the complete list is expensive to build;
- individual entities need to be refreshed after writes;
- cache entries must be addressable by a compound business key; and
- refresh work must not use a request-scoped database context after the request finishes.

The pattern keeps one cache entry per entity and a separate index containing the keys of every entry. It supports targeted, deduplicated asynchronous refreshes and full-cache recovery when entries are missing or inconsistent.

## Terms

| Term | Meaning |
|---|---|
| Entity | The domain record being cached, such as a cluster, product, or tenant resource. |
| Entity key | The primary business identifier, such as `clusterId`. |
| Scope | An optional qualifier that makes an entity key unique, such as environment, region, or tenant. |
| Entry | One fully enriched cached entity. |
| Index | A cache value that lists every entry key currently in the cache. |
| Refresh request | A request to rebuild one entity in one scope, or every scope when scope is omitted. |

## Cache Contract

Use two cache-key families.

```text
entity-index
entity:{scope-or-wildcard}:{entity-id}
```

For example:

```text
cluster-cache-index
cluster-cache:production:eu-factory-01
cluster-cache:*:eu-factory-01
```

The index contains minimal records only:

```csharp
public sealed record YouTubeContentCacheIndexEntry(string EntityId, string Scope);
```

Each entry must contain its entity ID and scope so the application can rebuild the entry key. Entry keys must be deterministic and use consistent normalization, such as trimming whitespace and treating the scope as case-insensitive when that matches the data model.

Do not cache only a single serialized list when targeted refresh is required. Per-entity entries let readers retrieve one entity directly and let writers replace only affected entries. The index permits enumeration without a cache-provider-specific key scan.

## YouTubeContent Adaptation

The approved YouTubeContent implementation uses these deterministic keys:

```text
youtubecontent:index:{scope}
youtubecontent:entry:{scope}:{base-entity-id}
```

Its scopes are:

| Scope | Use |
|---|---|
| `public-channel:{escaped-channel-id}` | Public Matches reads for one configured channel. Private entries and entries without membership in that channel are excluded during materialization. |
| `admin-global` | BackOffice administrative reads across all channels. Existing authorization and channel visibility checks run after the cached data is read; the cache is never an authorization boundary. |

`BuildEntries` is also responsible for the existing canonical YouTube short-link enrichment. Public channel ordering is `LatestPublishedAt` descending, then `CreationDateTime` descending; global administrative ordering remains `CreationDateTime` descending. Public count and pagination are calculated from the same indexed channel population.

The implementation is registered behind the existing `EnableCache` flag. When disabled, indexed reads fall back to source repositories and no cache writes or refresh work are queued. When enabled, both ServerAPI and BackOffice use the configuration-driven `ConnectionStrings:Redis` provider through `IDistributedCache`; an absent Redis setting falls back to distributed memory for local development. The public-channel and `admin-global` scopes keep their deterministic keys distinct even when both hosts share a Redis instance. BackOffice keeps the existing legacy `CacheKeys.Matches` and output-cache purge flow alongside indexed-cache refresh notifications.

Short-link create/update/delete operations do not directly invalidate or refresh this cache in this iteration. A subsequent content rebuild or targeted YouTubeContent refresh re-applies canonical enrichment, so short-link enrichment can be briefly stale after an isolated short-link mutation.

## Building Entries

`BuildEntries(entityIds?, scope?)` is the single source of truth for constructing cache values.

1. Query active source entities, optionally limited by IDs and scope.
2. Bulk-load all relationships, metadata, and supplemental data needed by the response model.
3. Convert source entities to cache entries.
4. Enrich each entry using in-memory dictionaries keyed by ID, rather than querying within the loop.
5. Return entries in a stable order, such as name then scope.

The method must return an empty collection when an entity no longer exists. That behavior is how a targeted refresh removes deleted or soft-deleted entities from the cache.

## Full Populate And Persist

When no valid index exists, build the complete list and persist it:

```csharp
var entries = BuildEntries();
foreach (var entry in entries)
    cache.Set(EntryKey(entry.EntityId, entry.Scope), entry);

cache.Set(IndexKey, entries.Select(x => new CacheIndexEntry(x.EntityId, x.Scope)).ToList());
```

When persisting a complete replacement, remove any legacy single-list cache key from older versions of the application. This prevents stale reads while migrating between cache layouts.

## Read Rules

### Read all entities

1. Return an in-process list if it is already available for the service instance.
2. Otherwise, load the index and retrieve all entry keys with a batch `GetMany` operation.
3. If the index is absent, build and persist all entries.
4. If the index exists but one or more indexed entries are unavailable, rebuild and persist all entries. This heals partial eviction and index/entry inconsistency.

### Read selected entities

1. When the caller supplied both IDs and a scope, batch-get only those deterministic entry keys.
2. When scope is absent, use the index to locate all scopes belonging to those IDs, then batch-get those keys.
3. Apply ID and scope filters again to the returned collection. Cache data is an optimization, not an authorization or correctness boundary.
4. If requested IDs are missing and the service did not already have an in-process list, rebuild the full cache and reapply filters. This handles a cold cache, incomplete distributed cache, and recently added data.

An optional in-process list improves repeated reads in one service instance. It must never be the only source of truth because other instances can refresh the distributed cache.

## Targeted Refresh

After creating, updating, or deleting an entity, enqueue a targeted refresh using `(entityId, scope)`. For a change affecting every scope, use a null scope.

The refresh algorithm is:

1. Load current entries from the local list when available; otherwise load them through the index.
2. Identify entries matching the requested entity ID. If scope is supplied, match both ID and scope; otherwise match every scope for that ID.
3. Rebuild only the requested ID and scope using `BuildEntries([entityId], scope)`.
4. Remove matching old entry keys from the cache.
5. Write rebuilt entries. If rebuilding returns none, no replacement is written.
6. Merge unchanged and rebuilt entries, sort stably, and persist the replacement index.
7. Update the in-process list to the merged collection.

This makes delete and soft-delete behavior correct without a separate deletion path: the existing entry is removed and an empty rebuild leaves it absent.

## Background Queue And Concurrency

Use a process-wide queue, a set of pending queue keys, and one asynchronous drain lock.

```text
queue key = normalized(scope or "*") + "::" + normalized(entity ID)
```

### Enqueue

1. Ignore empty entity IDs.
2. Create the normalized queue key.
3. Add it to the pending-key set. If it already exists, do not enqueue duplicate work.
4. Enqueue the refresh request.
5. Start a background worker that creates a new dependency-injection scope and resolves a new instance of the owning service.

The new scope is mandatory for request-driven applications. Repositories and database contexts resolved from the original request scope can be disposed before a fire-and-forget task runs.

### Drain

1. Await the asynchronous drain lock.
2. Dequeue and process every pending request in a loop.
3. Remove the request key from the pending-key set in a `finally` block, including when a refresh fails.
4. Release the drain lock in a `finally` block.

The lock prevents competing workers from interleaving read-modify-write updates to the index. The pending-key set avoids repeated cache rebuilds when one entity is modified rapidly.

### Deterministic completion

For tests, jobs, or administrative commands, expose a method that waits for queued workers and drains remaining queued requests. The current YouTubeContent adapter waits for queued targeted refreshes; a subsequent read still performs the normal index/entry recovery path when a complete rebuild is needed.

Do not make normal mutation APIs wait for this completion method. They should enqueue work and return after the database write succeeds unless the caller explicitly requires read-after-write cache consistency.

## Invalidation

Provide a full invalidation method for changes that cannot be accurately expressed as a small set of entity keys.

The method must:

1. Remove every entry referenced by the local list, or by the distributed index when no local list exists.
2. Remove the index.
3. Remove any retired legacy list key.
4. Clear the in-process list.

Use full invalidation when a shared dependency changes, such as metadata or a relationship that enriches many cached entities. The next read will rebuild the complete cache.

## Failure And Recovery Behavior

| Situation | Required behavior |
|---|---|
| No index | Build and persist the complete cache. |
| Index present but no entries can be read | Build and persist the complete cache. |
| Targeted entry absent after a cold partial read | Rebuild the complete cache, then filter the result. |
| Entity deleted before refresh runs | Remove matching old entries; write no replacement. |
| Duplicate mutation events | Coalesce by queue key while a refresh is pending. |
| Refresh throws | Remove its pending key; allow a later mutation or explicit invalidation to retry. Log the failure in production. |
| Multiple application instances | The index and entries must live in a shared cache. In-process lists are instance-local accelerators only. |

## Pseudocode

```csharp
async Task DrainRefreshQueueAsync()
{
    await drainLock.WaitAsync();
    try
    {
        while (queue.TryDequeue(out var request))
        {
            try
            {
                RefreshEntries(request.EntityId, request.Scope);
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
    }
}

void RefreshEntries(string entityId, string? scope)
{
    var current = ReadCurrentEntriesOrReturn();
    var oldEntries = current.Where(x => Matches(x, entityId, scope)).ToList();
    var rebuilt = BuildEntries([entityId], scope);

    RemoveEntryKeys(oldEntries);
    SetEntryKeys(rebuilt);

    var merged = current.Except(oldEntries)
        .Concat(rebuilt)
        .OrderBy(x => x.DisplayName)
        .ThenBy(x => x.Scope)
        .ToList();

    PersistIndex(merged);
    localEntries = merged;
}
```

## Implementation Checklist

- Define deterministic index and entry keys.
- Make `BuildEntries` the only place that materializes and enriches cache values.
- Use batch cache reads for index-driven lookups.
- Rebuild the full cache when the index and entries disagree.
- Enqueue targeted refreshes after successful persistence.
- Create a new DI scope inside every background worker.
- Serialize drains and deduplicate pending work by normalized compound key.
- Remove pending keys and release locks in `finally` blocks.
- Treat an empty targeted rebuild as deletion.
- Provide full invalidation for shared dependency changes.
- Test cache population, targeted update, scoped update, deletion, duplicate enqueue, partial-cache recovery, and full invalidation.

## Adaptation Points

The portable elements are the index, entry storage, batching, queue, deduplication, and recovery rules. Replace only these application-specific pieces:

- entity ID and optional scope;
- entry-key format and normalization policy;
- source queries and enrichment data;
- stable sort order;
- cache provider interface;
- logging, metrics, retry, and error policy.

For high-volume systems, replace the in-memory static queue with a durable work queue and add distributed locking or optimistic versioning around index updates. The cache contract and refresh algorithm remain the same.