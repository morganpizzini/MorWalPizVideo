# Contract: BioLink Atomic Ordering Operations

**Feature**: `001-cache-invalidation-fixes`
**Owner**: `MorWalPizVideo.BackOffice.Controllers.BioLinksController`
**Status**: Internal (Mongo) contract; not exposed over HTTP beyond existing admin endpoints.

---

## Pre-conditions

- `bioLinks` MongoDB collection accessible via the existing `IMongoDatabase` injection used by `BioLinksController`.
- `MongoDB.Driver` ≥ existing pinned version (no upgrade required).

---

## Operation: Insert BioLink at a target order

```
Inputs:
  target : BioLink            // newly constructed entity with Order = N
Effects on collection:
  1. UpdateMany({ Order >= N }, { $inc: { Order: 1 } })       // atomic shift
  2. InsertOne(target)                                          // atomic insert
Postconditions:
  - target is persisted with Order = N.
  - No document in the collection has an Order value duplicating another document's Order.
```

## Operation: Reorder existing BioLink

```
Inputs:
  target  : BioLink            // existing entity with NEW Order = M
  oldOrder: int                // its previous Order value
Effects on collection:
  if M == oldOrder:
    1. ReplaceOne({ _id: target.Id }, target with Order = M)
  elif M < oldOrder:
    1. UpdateMany({ Order >= M, Order < oldOrder }, { $inc: { Order: 1 } })
    2. ReplaceOne({ _id: target.Id }, target with Order = M)
  else: // M > oldOrder
    1. UpdateMany({ Order > oldOrder, Order <= M }, { $inc: { Order: -1 } })
    2. ReplaceOne({ _id: target.Id }, target with Order = M)
Postconditions:
  - target is persisted with Order = M.
  - No document in the collection has an Order value duplicating another document's Order.
```

## Operation: Delete BioLink

```
Inputs:
  target : BioLink             // existing entity with Order = K
Effects on collection:
  1. DeleteOneAsync({ _id: target.Id })
  2. UpdateMany({ Order > K }, { $inc: { Order: -1 } })       // atomic compaction
Postconditions:
  - target no longer present.
  - Remaining documents have contiguous Order values starting from 0 (assuming pre-state was contiguous).
  - No duplicates.
```

> Compaction step is included for completeness; if the existing delete path does not currently compact, the implementation MAY preserve current behavior (non-contiguous after delete) as long as the no-duplicates invariant holds.

---

## Invariants

- **I1 — Uniqueness**: At rest, no two `BioLink` documents share the same `Order`.
- **I2 — Atomicity per shift**: Each `UpdateMany` is a single MongoDB command and atomic per matching document, so concurrent shifts compose without lost updates.
- **I3 — Cache invalidation**: After any of the above operations, the controller MUST perform the existing `ResetCache(CacheKeys.BioLinks)` + `PurgeCache(ApiTagCacheKeys.BioLinks)` sequence (now using the corrected URL contract).

---

## Concurrency test (must exist in `MorWalPizVideo.BackOffice.Tests`)

Pseudocode:

```csharp
[Fact]
public async Task CreateBioLink_under_parallel_load_produces_unique_orders()
{
    // Arrange: empty in-memory repository or test Mongo instance
    var controller = BuildControllerWithEmptyCollection();

    // Act: 10 parallel creates, each requesting Order = i
    await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (i, _) =>
        await controller.CreateBioLink(new CreateBioLinkRequest(
            Title: $"link-{i}", Description: "", Url: "https://example.test",
            Icon: "", Order: i)));

    // Assert: no duplicate Order values
    var all = await ReadAllBioLinks();
    Assert.Equal(10, all.Count);
    Assert.Equal(10, all.Select(x => x.Order).Distinct().Count());
}
```

The actual test file name/location is finalized in [tasks.md](./tasks.md).

---

## Non-goals

- Order values being **contiguous** after concurrent operations (only uniqueness is guaranteed by this contract).
- Transactional atomicity across the shift+insert pair (requires replica-set transactions; explicitly out of scope per [research.md R3](../research.md)).
