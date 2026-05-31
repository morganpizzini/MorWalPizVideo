# Research: Cache invalidation correctness and high-impact code-review fixes

**Feature**: `001-cache-invalidation-fixes`
**Date**: 2026-05-31

This document resolves the open implementation questions left by [spec.md](./spec.md) so that Phase 1 design can proceed without `NEEDS CLARIFICATION` markers.

---

## R1. Direction of the cache-purge contract fix

**Question**: Spec assumption notes that "back-office and public API must agree on the cache-purge contract"; either side can move. Which side moves?

**Decision**: Fix the **back-office client** to match the existing **public-API server** contract (`GET api/cache/purge?k={tag}`). Do not change the server endpoint.

**Rationale**:
- The server contract is already deployed and may be called by other internal tools or scripts; changing the route shape is a breaking change with unknown blast radius.
- The client bug is a one-line URL fix, fully reversible, and localized to `MorWalPizVideo.BackOffice/Services/CrossApiService.cs`.
- The `ResetCache` endpoint on the same controller already uses the `?k=` query-parameter convention, so aligning `PurgeCache` keeps the two endpoints consistent.

**Alternatives considered**:
- *Change the server to accept `cache/purge/{tag}` as a path segment*: rejected — wider blast radius, breaks any external consumer, no functional benefit.
- *Support both shapes on the server*: rejected — added surface area and ongoing maintenance for zero benefit; the goal is consistency, not flexibility.

---

## R2. Output cache tag case-insensitivity strategy

**Question**: How do we guarantee tag invalidation works regardless of caller casing without breaking existing cached entries?

**Decision**: Adopt the **lowercase convention** for all `[OutputCache(Tags=[...])]` attribute values AND normalize the tag to lowercase (`ToLowerInvariant`) at the single point of invalidation in `CacheController.purge`. The runtime normalization is the safety net; the convention is the long-term guarantee.

**Rationale**:
- `IOutputCacheStore.EvictByTagAsync` matches tags by ordinal equality. The library does not provide a case-insensitive option, so normalization must happen at both write (attribute) and evict (purge endpoint) sites.
- Normalizing only on the evict side would still leak: a mutation requesting eviction of `"matches"` would not evict an entry tagged `"Matches"` because the stored tag is preserved verbatim. Therefore the attribute-side normalization (lowercase convention enforced in code) is the actual fix; the purge-side `ToLowerInvariant` is a defensive guard against future regressions.
- `ToLowerInvariant` (not `ToLower`) is required to avoid Turkish-`i` and other culture-dependent surprises in tag strings that may contain locale-specific characters.

**Alternatives considered**:
- *Custom `IOutputCacheStore` decorator that lowercases on both store and evict*: rejected — adds a moving part for a problem solvable by convention and a one-line normalization.
- *Force enum-typed tags*: rejected — large refactor, no proportionate benefit at current scale.

---

## R3. Atomic BioLink ordering scheme

**Question**: How to guarantee unique `Order` values under concurrent BioLink create and reorder operations without introducing a new dependency?

**Decision**: Replace the current read-modify-bulk-write pattern with an atomic MongoDB `UpdateMany` using the `$inc` operator, scoped by an `Order >= n` filter, executed **before** the insert/replace of the target document.

For create:
1. `await collection.UpdateManyAsync(Filter.Gte(x => x.Order, target.Order), Update.Inc(x => x.Order, 1));`
2. `await collection.InsertOneAsync(target);`

For reorder (update where `Order` changed):
1. If new order < old order: `UpdateMany(Gte(Order, newOrder) & Lt(Order, oldOrder), Inc(Order, +1))`
2. If new order > old order: `UpdateMany(Gt(Order, oldOrder) & Lte(Order, newOrder), Inc(Order, -1))`
3. `ReplaceOne(Id == target.Id, target with { Order = newOrder })`

**Rationale**:
- MongoDB's update operators are atomic at the document level; `UpdateMany` is atomic per matching document and avoids the lost-update class of bugs that the current `Find().ToList()` + in-memory `+1` + `BulkWrite` pattern exhibits.
- No new dependency; uses the existing `MongoDB.Driver`.
- The two-step (shift then insert/replace) is not transactionally atomic across documents in non-replica-set deployments, but the spec's success criterion (SC-003) is "no duplicates among 10 parallel creates", which the `$inc` shift guarantees because the increment is a single atomic operation per matching document. A duplicate would require two creates to both pass the same `Order` AND both `UpdateMany` calls to complete fully before either insert — impossible because each `$inc` is observable to the next reader.

**Alternatives considered**:
- *MongoDB multi-document transactions (`IClientSessionHandle`)*: rejected — requires replica set / Atlas configuration; the current deployment shape is not guaranteed to support it, and the atomic-update approach satisfies the requirement without it.
- *Order as a sparse float (gap-based ordering)*: rejected — larger refactor; changes the semantics of `Order` and requires a read-side rebalance.
- *Pessimistic lock via distributed mutex*: rejected — adds dependency (Redis), violates "no new external dependencies" in spec.

---

## R4. HttpClient lifecycle correction pattern

**Question**: What is the correct pattern for consuming `IHttpClientFactory` clients, and how do we audit the codebase for violations?

**Decision**:
- **Consumption pattern**: Inject `IHttpClientFactory`, call `CreateClient(name)`, use the returned `HttpClient` for the duration of the call **without** wrapping it in `using` and **without** storing it for the lifetime of a singleton service. Services that take `IHttpClientFactory` as a dependency MUST NOT implement `IDisposable` solely to dispose those clients.
- **Direct `new HttpClient()`**: replace with named-client registration in `Program.cs` and inject `IHttpClientFactory`. The Pinterest base URL becomes a named client `HttpClientNames.Pinterest`.
- **Audit**: a one-line `grep` (`new HttpClient(` and `using.*CreateClient(`) on the `MorWalPizVideo.BackOffice` tree, gating any future regression via the same check in the implementation tasks.

**Rationale**:
- Microsoft's official `IHttpClientFactory` guidance: the factory manages handler pooling and rotation; calling `Dispose` on a returned client is a no-op for the handler but signals intent incorrectly and will become a real leak if `HttpClient` ownership semantics ever change. Removing `using` prevents future regressions.
- `TelegramService`, `DiscordService`, `FacebookService` currently implement `IDisposable` to dispose their `HttpClient` field. Since those fields are factory-issued, the `Dispose()` body becomes a no-op and the `IDisposable` interface implementation should be removed entirely.

**Alternatives considered**:
- *Typed clients (`AddHttpClient<TService>()`)*: not adopted in this feature to keep diff small; recorded as a follow-up improvement.

---

## R5. Async Mongo audit scope

**Question**: Which controllers and methods need conversion from sync to async Mongo calls?

**Decision**: Scope is limited to `MorWalPizVideo.BackOffice/Controllers/*.cs` methods declared `async Task<IActionResult>` or `async Task`. Specifically:
- `BioLinksController.DeleteBioLink` — `DeleteOne` → `DeleteOneAsync`.
- `BioLinksController.CreateBioLink` / `UpdateBioLink` — `Find(...).ToList()` → `Find(...).ToListAsync()` (now superseded by R3's atomic update, but the audit still applies to any residual reads).
- Audit (read-only, not necessarily a fix in this PR) of all admin controllers for additional `.Find(...).ToList()`, `.FirstOrDefault()`, `.InsertOne(...)`, `.UpdateOne(...)` calls inside async methods.

**Rationale**: Spec FR-012 is scoped to admin controller actions; broader audits (ServerAPI, ShortLinks) are explicitly out of scope per the spec's "Out of scope" section.

**Alternatives considered**:
- *Roslyn analyzer to enforce going forward*: deferred; explicitly out of scope.

---

## R6. API-key "last used" telemetry hardening

**Question**: How do we replace the fire-and-forget `_ = _apiKeyService.UpdateLastUsedAsync(...)` so failures are logged without changing the response status?

**Decision**: Replace the discard pattern with a `ContinueWith` continuation that observes the antecedent task and logs faults via the existing `ILogger<ApiKeyAuthenticationHandler>`:

```csharp
_ = _apiKeyService.UpdateLastUsedAsync(apiKey.Id!)
    .ContinueWith(
        t => _logger.LogError(t.Exception, "Failed to update ApiKey LastUsed for {ApiKeyId}", apiKey.Id),
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);
```

The authentication path remains non-awaiting (so the request's status code is unaffected per FR-014), but unobserved exceptions are now observed.

Additionally, inside `ApiKeyService.UpdateLastUsedAsync` itself, the existing `catch (Exception) { return false; }` MUST log the exception before returning `false`.

**Rationale**:
- `ContinueWith` with `OnlyOnFaulted` is the canonical .NET pattern for "fire and observe" when you cannot block the caller; it avoids `async void` and does not change the request's response.
- Logging inside the service ensures the failure is captured even if a different caller someday awaits the result and ignores the `false` return.

**Alternatives considered**:
- *Wrap in `Task.Run` and await inside a background channel*: rejected — heavier-weight than needed for a single timestamp update.
- *Await inline*: rejected — would change request latency for every authenticated admin call and is unnecessary for telemetry.

---

## R7. Testing strategy

**Question**: What new tests are required and where do they live?

**Decision**:
- **Contract test** for the cache-purge URL agreement: a new xUnit test in `MorWalPizVideo.BackOffice.Tests/Infrastructure/` that uses an `HttpMessageHandler` test double to capture the outbound request and assert that `CrossApiService.PurgeCache("biolinks")` issues `GET cache/purge?k=biolinks`. This guards against regression of the original bug.
- **Concurrency test** for atomic BioLink ordering: a SpecFlow scenario or xUnit fact under `MorWalPizVideo.BackOffice.Tests/Features/` that exercises `CreateBioLink` with `Parallel.ForEachAsync` (10 concurrent calls against the mock repository or a test Mongo instance) and asserts distinct `Order` values.
- **Logging assertion** for API-key telemetry: an xUnit fact that injects a failing `IApiKeyService` and asserts the `ILogger<ApiKeyAuthenticationHandler>` receives an `Error`-level entry while the HTTP response is unchanged.

**Rationale**: These align directly with spec success criteria SC-001/SC-002 (purge URL), SC-003 (concurrency), and SC-006 (telemetry logging). The project already uses xUnit + SpecFlow with mock repositories (`MockCrossApiService`, `BioLinkMockRepository`), so no new test infrastructure is needed.

**Alternatives considered**:
- *Integration test against a live MongoDB instance*: deferred — the existing mock repositories satisfy the constitution's testing requirement and avoid CI infrastructure changes.

---

## Summary of decisions

| ID | Decision |
|----|----------|
| R1 | Move client to match server: `cache/purge?k={tag}` |
| R2 | Lowercase convention for output cache tags + `ToLowerInvariant` guard at purge |
| R3 | Atomic `UpdateMany`+`$inc` for BioLink ordering |
| R4 | Remove `using` on factory clients; remove `IDisposable` from social services; add named Pinterest client |
| R5 | Async Mongo audit limited to back-office admin controllers |
| R6 | `ContinueWith(OnlyOnFaulted)` + service-side logging for API-key telemetry |
| R7 | xUnit contract test + concurrency test + logger-assertion test in `BackOffice.Tests` |

All `NEEDS CLARIFICATION` markers from the spec are resolved.
