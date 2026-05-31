# Data Model: Cache invalidation correctness and high-impact code-review fixes

**Feature**: `001-cache-invalidation-fixes`
**Date**: 2026-05-31

This feature is primarily a correctness/perf fix; it does **not** introduce new persisted entities or modify existing storage schemas. This document captures the (small) semantic refinements to existing in-memory and operational concepts that the implementation relies on.

---

## Existing entities (no schema changes)

### `BioLink`
- **Storage**: MongoDB collection `bioLinks` (existing).
- **Fields used by this feature**: `Id`, `Title`, `Order` (`int`).
- **Invariant strengthened by this feature**: `Order` MUST be unique across the entire collection at all times.
  - *Before*: invariant existed in the domain model but was not enforced under concurrency.
  - *After*: enforced by atomic `UpdateMany($inc)` in the mutation paths (see [research.md R3](./research.md)).
- **No schema migration required**: existing documents that already have unique `Order` values remain valid. Any pre-existing duplicates from past races are NOT auto-repaired by this feature; a one-time repair script is out of scope (operator concern, can be done ad hoc).

### `ShortLink` / `Video` (`YouTubeContent`)
- **Storage**: existing MongoDB collections.
- **No structural change**. The feature only changes how their mutation handlers interact with the cache layer.

### `ApiKey`
- **Storage**: existing MongoDB collection.
- **Fields used**: `Id`, `LastUsedAt`.
- **Behavioral change**: `LastUsedAt` updates that previously failed silently now log on failure. Field semantics, type, and nullability are unchanged.

---

## Operational concepts (clarified, not stored)

### Output Cache Tag
- **Domain**: free-form string label declared in `[OutputCache(Tags=[...])]` attributes and passed to `IOutputCacheStore.EvictByTagAsync`.
- **Canonical form (new convention)**: lowercase, ASCII, hyphen-separated. Example: `tag-biolinks`, `tag-matches`, `tag-calendar-events`.
- **Comparison rule**: invalidation MUST normalize the supplied tag with `ToLowerInvariant` before calling `EvictByTagAsync`. New attribute declarations MUST already be lowercase; the normalization is a defensive guard.
- **Lifecycle**: ephemeral (per-response, per-process). No persistence.

### In-Memory Cache Key
- **Domain**: string keys defined in `MorWalPizVideo.Models.Constraints.CacheKeys` (existing static class).
- **Current state**: some keys are lowercase (`"matches"`), some are camelCase (`"calendarEvents"`). The purge endpoint already lowercases the supplied keys before calling `cache.Remove(key)`, which means camelCase keys are silently unreachable via the purge endpoint today.
- **Required action**: audit `CacheKeys` constants; rename any camelCase entries to lowercase OR remove the `ToLowerInvariant` step in the purge endpoint, whichever is less risky. Per [research.md R2](./research.md), lowercase convention wins.

### Cache Purge HTTP Contract
- **Endpoint**: `GET api/cache/purge?k={tag}` (existing on `MorWalPizVideo.ServerAPI`).
- **Caller**: `MorWalPizVideo.BackOffice/Services/CrossApiService.PurgeCache(string key)`.
- **Required change**: caller URL becomes `cache/purge?k={Uri.EscapeDataString(key)}` (was `cache/purge/{key}`).
- **Response**: `204 NoContent` on success; any non-2xx is a failure.

### Cache Reset HTTP Contract
- **Endpoint**: `GET api/cache/reset?k={comma-separated-keys}` (existing).
- **Caller**: `CrossApiService.ResetCache(string key)`.
- **No change** to the contract; the caller already uses the correct shape. URL-encoding the `k` parameter is a defensive improvement.

---

## State transitions

### BioLink create (after fix)

```
[Client request: create BioLink with target.Order = N]
  │
  ▼
UpdateManyAsync(Order >= N, $inc Order +1)   ← atomic shift
  │
  ▼
InsertOneAsync(target)                       ← atomic insert
  │
  ▼
[Cache invalidation: ResetCache + PurgeCache]
  │
  ▼
[204 NoContent]
```

### BioLink reorder (after fix; new order = M, old order = O)

```
if M < O:  UpdateMany(Order >= M & Order < O, $inc +1)
if M > O:  UpdateMany(Order >  O & Order <= M, $inc -1)
if M == O: no shift
  │
  ▼
ReplaceOneAsync(Id == target.Id, target with Order = M)
  │
  ▼
[Cache invalidation: ResetCache + PurgeCache]
  │
  ▼
[204 NoContent]
```

### API-key authentication telemetry (after fix)

```
[Authenticated request arrives]
  │
  ▼
Validate API key (existing)
  │
  ├─► UpdateLastUsedAsync(...)
  │       │
  │       ├─ success → silent
  │       └─ failure → ILogger.LogError(...)   ← new
  │
  ▼
[Response continues with unchanged status code]
```

---

## Validation rules

- `BioLink.Order` MUST be a non-negative integer. (Pre-existing rule; no change.)
- `Order` uniqueness is now an enforced invariant, not just a domain wish. Tests in `BackOffice.Tests` validate it under concurrency.
- Output cache tags written into `[OutputCache(Tags=[...])]` MUST be lowercase ASCII. Lint/audit enforced via a one-line `grep` in the implementation tasks.

---

## Out-of-scope data concerns (recorded for follow-up specs)

- Repairing any pre-existing `Order` duplicates already present in production data.
- Adding a unique MongoDB index on `BioLink.Order` (would conflict with the shift step's intermediate states).
- Schema versioning / migration framework.
- Cosmos DB migration evaluation.
