# Tasks: Cache invalidation correctness and high-impact code-review fixes

**Input**: Design documents from `specs/001-cache-invalidation-fixes/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: INCLUDED — required by spec FR-017, FR-018 and [research.md §R7](./research.md).

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and shipped independently.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps to user story in [spec.md](./spec.md) (US1–US5). Setup/Foundational/Polish phases have no story label.
- Every task description includes a concrete file path.

## Path Conventions

This is an existing multi-project .NET 8 solution. Real paths:

- Back-office service: `MorWalPizVideo.BackOffice/`
- Back-office tests: `MorWalPizVideo.BackOffice.Tests/`
- Public API server: `MorWalPizVideo.ServerAPI/`
- Shared constants: `MorWalPizVideo.Models/`

No new projects are created.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the single shared constant introduced by this feature; no other setup needed (existing solution, no new dependencies).

- [X] T001 Add constant `Pinterest` to the `HttpClientNames` static class in [MorWalPizVideo.Models/Constraints/HttpClientNames.cs](MorWalPizVideo.Models/Constraints/HttpClientNames.cs) (value: `"pinterest"`). Search the workspace first to confirm the file path; if `HttpClientNames` lives elsewhere, add the constant alongside the existing `MorWalPiz`, `YouTube`, `Facebook` entries.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: One stub used by multiple contract tests in later phases. Must complete before any test task in Phase 3+ can compile.

**⚠️ CRITICAL**: No user story test task can begin until T002 is complete.

- [X] T002 Create test infrastructure stubs `StubHttpMessageHandler` and `HttpClientFactoryStub` in [MorWalPizVideo.BackOffice.Tests/Infrastructure/HttpStubs.cs](MorWalPizVideo.BackOffice.Tests/Infrastructure/HttpStubs.cs). `StubHttpMessageHandler` captures the last `HttpRequestMessage` and returns a configurable `HttpResponseMessage`. `HttpClientFactoryStub` implements `IHttpClientFactory` and returns a single named client. See pseudocode in [contracts/cache-invalidation.http.md](./contracts/cache-invalidation.http.md).

**Checkpoint**: Foundation ready — user-story tasks can begin in parallel.

---

## Phase 3: User Story 1 — Reliable admin mutations with cache invalidation (Priority: P1) 🎯 MVP

**Story Goal**: Admin mutations on BioLinks / ShortLinks / Videos return success without spurious error toasts, and public caches are actually invalidated.

**Independent Test**: From the admin SPA, mutate a BioLink. Verify the SPA receives a 2xx with no error toast, then fetch the corresponding public endpoint and confirm the new value is returned immediately. (Maps to spec [Acceptance Scenarios](./spec.md#user-story-1---reliable-admin-mutations-with-cache-invalidation-priority-p1) AS-1/2/3.)

### Tests for User Story 1 (write FIRST, must FAIL before implementation)

- [X] T003 [P] [US1] Contract test `PurgeCache_uses_query_string_contract` in [MorWalPizVideo.BackOffice.Tests/Infrastructure/CrossApiServiceContractTests.cs](MorWalPizVideo.BackOffice.Tests/Infrastructure/CrossApiServiceContractTests.cs) — asserts `CrossApiService.PurgeCache("tag-biolinks")` issues `GET https://example.test/api/cache/purge?k=tag-biolinks` (per [contracts/cache-invalidation.http.md](./contracts/cache-invalidation.http.md)).
- [X] T004 [P] [US1] Contract test `ResetCache_uses_query_string_contract` in the same file — asserts `ResetCache("matches")` issues `GET https://example.test/api/cache/reset?k=matches` (defensive regression guard; existing behavior).
- [X] T005 [P] [US1] Contract test `PurgeCache_url_encodes_key` in the same file — asserts a key containing reserved characters (`"a/b c"`) is URL-encoded in the outbound request URI.

### Implementation for User Story 1

- [X] T006 [US1] Fix the `PurgeCache` URL in [MorWalPizVideo.BackOffice/Services/CrossApiService.cs](MorWalPizVideo.BackOffice/Services/CrossApiService.cs) — change `client.GetStringAsync($"cache/purge/{key}")` to `client.GetStringAsync($"cache/purge?k={Uri.EscapeDataString(key)}")`. URL-encode the `ResetCache` key the same way.

**Checkpoint**: T003–T005 now PASS. The original cache-reset error symptom is gone. The MVP can be shipped here.

---

## Phase 4: User Story 2 — Consistent output cache tag invalidation (Priority: P1)

**Story Goal**: Output cache tag eviction works regardless of letter casing of the declaring attribute, with a documented lowercase convention going forward.

**Independent Test**: Annotate two endpoints with the same tag in different casing, trigger eviction for that tag, confirm both return fresh data on next request. (Maps to spec [US2 AS-1/2](./spec.md#user-story-2---consistent-output-cache-tag-invalidation-priority-p1).)

### Tests for User Story 2

- [X] T007 [P] [US2] Unit test `Purge_normalizes_tag_to_lowercase_invariant` in [MorWalPizVideo.BackOffice.Tests/Features/OutputCachePurgeTests.cs](MorWalPizVideo.BackOffice.Tests/Features/OutputCachePurgeTests.cs) — uses a fake `IOutputCacheStore` to assert that calling the purge endpoint with `k=Tag-BioLinks` invokes `EvictByTagAsync("tag-biolinks", _)`.
- [X] T008 [P] [US2] Repository audit test `OutputCache_tag_attributes_are_lowercase` in the same file — uses reflection over `MorWalPizVideo.ServerAPI` assembly to enumerate `[OutputCacheAttribute]` declarations and assert every `Tags` entry equals its `ToLowerInvariant()`.

### Implementation for User Story 2

- [X] T009 [US2] In [MorWalPizVideo.ServerAPI/Controllers/CacheController.cs](MorWalPizVideo.ServerAPI/Controllers/CacheController.cs) line 21, normalize the incoming tag: `await cache.EvictByTagAsync(tag.ToLowerInvariant(), default);`.
- [X] T010 [P] [US2] Audit all `[OutputCache(Tags=[...])]` declarations across the solution (run `grep_search` for `OutputCache.*Tags`) and rename any non-lowercase tag literals to their lowercase form. Update both the attribute site and any corresponding constant in `ApiTagCacheKeys` (likely in [MorWalPizVideo.Models/Constraints/](MorWalPizVideo.Models/Constraints/)).
- [X] T011 [US2] Audit `CacheKeys` constants in [MorWalPizVideo.Models/Constraints/CacheKeys.cs](MorWalPizVideo.Models/Constraints/CacheKeys.cs) — rename any camelCase value (e.g., `"calendarEvents"`) to lowercase so the existing `keys.ToLower()` step in `CacheController.Reset` actually matches the stored cache keys. Per [data-model.md](./data-model.md) §"In-Memory Cache Key".

**Checkpoint**: T007 and T008 PASS. US1 + US2 both ship correctly.

---

## Phase 5: User Story 3 — No duplicate BioLink ordering under concurrency (Priority: P2)

**Story Goal**: 10 parallel BioLink create requests result in 10 distinct `Order` values (SC-003).

**Independent Test**: Issue 10 parallel `CreateBioLink` calls against an empty collection; assert no duplicate `Order`. (Maps to spec [US3 AS-1/2](./spec.md#user-story-3---no-duplicate-biolink-ordering-under-concurrency-priority-p2).)

### Tests for User Story 3

- [X] T012 [P] [US3] Concurrency test `CreateBioLink_under_parallel_load_produces_unique_orders` in [MorWalPizVideo.BackOffice.Tests/Features/BioLinkOrderingTests.cs](MorWalPizVideo.BackOffice.Tests/Features/BioLinkOrderingTests.cs) — pseudocode in [contracts/biolink-ordering.md](./contracts/biolink-ordering.md). Uses the existing `BioLinkMockRepository` or a per-test `IMongoCollection<BioLink>` fake that implements `UpdateManyAsync` atomically.
- [X] T013 [P] [US3] Reorder test `UpdateBioLink_shifts_orders_atomically` in the same file — verifies that moving a BioLink from `Order=2` to `Order=5` results in items previously at orders 3,4,5 being shifted to 2,3,4 with no duplicates.

### Implementation for User Story 3

- [X] T014 [US3] Rewrite `CreateBioLink` in [MorWalPizVideo.BackOffice/Controllers/BioLinksController.cs](MorWalPizVideo.BackOffice/Controllers/BioLinksController.cs) to use the atomic shift pattern per [contracts/biolink-ordering.md](./contracts/biolink-ordering.md) "Insert BioLink at a target order": `await collection.UpdateManyAsync(Builders<BioLink>.Filter.Gte(x => x.Order, entity.Order), Builders<BioLink>.Update.Inc(x => x.Order, 1));` then `await collection.InsertOneAsync(entity);`. Remove the `Find().ToList()` + in-memory loop + `BulkWriteAsync` pattern.
- [X] T015 [US3] Rewrite `UpdateBioLink` in the same file to use the atomic shift pattern per the contract's "Reorder existing BioLink" section. Replace the existing `Find().ToList()` + in-memory loop with the two cases (`M < O` shift up, `M > O` shift down), followed by `ReplaceOneAsync` of the target.
- [X] T016 [US3] Update `DeleteBioLink` in the same file: replace synchronous `collection.DeleteOne(...)` with `await collection.DeleteOneAsync(...)`. Compaction (`UpdateMany Order > K, $inc -1`) is OPTIONAL per the contract; if added, include it in the same method.

**Checkpoint**: T012, T013 PASS. US1 + US2 + US3 all green.

---

## Phase 6: User Story 4 — Reliable HttpClient and resource lifecycle (Priority: P2)

**Story Goal**: All outbound `HttpClient` instances come from `IHttpClientFactory`, none are wrapped in `using`, no service implements `IDisposable` solely for factory clients. (Maps to spec [US4](./spec.md#user-story-4---reliable-http-client-and-resource-lifecycle-in-back-office-priority-p2).)

**Independent Test**: `grep_search` for `new HttpClient(` and `using\s+var\s+\w+\s*=\s*[^;]*\.CreateClient\(` under `MorWalPizVideo.BackOffice/` returns zero matches (SC-004).

### Tests for User Story 4

- [X] T017 [P] [US4] Audit test `BackOffice_does_not_construct_HttpClient_directly` in [MorWalPizVideo.BackOffice.Tests/Infrastructure/HttpClientLifetimeAuditTests.cs](MorWalPizVideo.BackOffice.Tests/Infrastructure/HttpClientLifetimeAuditTests.cs) — scans every `.cs` file under `MorWalPizVideo.BackOffice/` (resolve path relative to test assembly), asserts zero regex matches for `new HttpClient\(` and `using\s+var\s+\w+\s*=\s*[^;]*\.CreateClient\(`.
- [X] T018 [P] [US4] Audit test `BackOffice_services_do_not_implement_IDisposable_for_factory_clients` in the same file — reflects over service types in `MorWalPizVideo.BackOffice` assembly that take `IHttpClientFactory` in their constructor; asserts none implement `IDisposable`.

### Implementation for User Story 4

- [X] T019 [US4] In [MorWalPizVideo.BackOffice/Services/CrossApiService.cs](MorWalPizVideo.BackOffice/Services/CrossApiService.cs), remove the `using` keyword from each of the three `using var client = this.client.CreateClient(...)` lines (in `ResetCache`, `PurgeCache`, `ReloadCache`). Field already named `client`; rename the local to `httpClient` to avoid shadowing.
- [X] T020 [P] [US4] In [MorWalPizVideo.BackOffice/Controllers/PinterestController.cs](MorWalPizVideo.BackOffice/Controllers/PinterestController.cs), remove the two `new HttpClient()` allocations at lines ~53 and ~73. Inject `IHttpClientFactory` in the constructor and call `CreateClient(HttpClientNames.Pinterest)` at each call site.
- [X] T021 [US4] Register the `Pinterest` named client in [MorWalPizVideo.BackOffice/Program.cs](MorWalPizVideo.BackOffice/Program.cs) alongside the existing `MorWalPiz` / `YouTube` / `Facebook` registrations. Use the Pinterest base URL currently hard-coded in `PinterestController` (move the URL into the registration, remove it from the controller). Depends on T001 and T020.
- [X] T022 [P] [US4] In [MorWalPizVideo.BackOffice/Services/TelegramService.cs](MorWalPizVideo.BackOffice/Services/TelegramService.cs), remove the `IDisposable` interface from the class declaration and delete the `Dispose()` method body that disposes the factory-issued `HttpClient`. Keep all other functionality.
- [X] T023 [P] [US4] Same change as T022 applied to [MorWalPizVideo.BackOffice/Services/DiscordService.cs](MorWalPizVideo.BackOffice/Services/DiscordService.cs).
- [X] T024 [P] [US4] Same change as T022 applied to [MorWalPizVideo.BackOffice/Services/FacebookService.cs](MorWalPizVideo.BackOffice/Services/FacebookService.cs).

**Checkpoint**: T017 and T018 PASS. All four stories so far are green.

---

## Phase 7: User Story 5 — Reliable async Mongo and observable API-key telemetry (Priority: P3)

**Story Goal**: No sync Mongo in async admin controllers; API-key "last used" failures are logged at error level without changing response status. (Maps to spec [US5](./spec.md#user-story-5---reliable-async-data-access-and-observable-api-key-telemetry-priority-p3).)

**Independent Test**: Static audit finds zero sync Mongo calls in admin controllers; forced failure of `UpdateLastUsedAsync` produces exactly one error log entry and the response status is unchanged.

### Tests for User Story 5

- [X] T025 [P] [US5] Audit test `AdminControllers_use_async_Mongo_in_async_methods` in [MorWalPizVideo.BackOffice.Tests/Infrastructure/AsyncMongoAuditTests.cs](MorWalPizVideo.BackOffice.Tests/Infrastructure/AsyncMongoAuditTests.cs) — text-scans all `.cs` files under `MorWalPizVideo.BackOffice/Controllers/` for the patterns `\.DeleteOne\(`, `\.InsertOne\(`, `\.UpdateOne\(`, `\.ReplaceOne\(`, `\.Find\([^)]*\)\.ToList\(\)`, `\.Find\([^)]*\)\.FirstOrDefault\(\)` and asserts none appear inside methods whose signature contains `async`.
- [X] T026 [P] [US5] Test `Authentication_logs_error_when_UpdateLastUsedAsync_throws` in [MorWalPizVideo.BackOffice.Tests/Features/ApiKeyTelemetryTests.cs](MorWalPizVideo.BackOffice.Tests/Features/ApiKeyTelemetryTests.cs) — replaces `IApiKeyService` with a stub whose `UpdateLastUsedAsync` returns a faulted task; asserts the captured `ILogger<ApiKeyAuthenticationHandler>` receives exactly one `LogLevel.Error` entry containing the API key id, AND the authentication result is still success.
- [X] T027 [P] [US5] Test `UpdateLastUsedAsync_logs_error_on_repository_failure` in the same file — forces the repository to throw, calls `ApiKeyService.UpdateLastUsedAsync`, asserts `ILogger<ApiKeyService>` receives one `LogLevel.Error` entry and the method returns `false`.

### Implementation for User Story 5

- [X] T028 [US5] In [MorWalPizVideo.BackOffice/Controllers/BioLinksController.cs](MorWalPizVideo.BackOffice/Controllers/BioLinksController.cs) (already touched by T014–T016), replace any remaining sync Mongo calls flagged by T025 with their `*Async` counterparts. In particular, replace `Find(...).FirstOrDefault()` with `await Find(...).FirstOrDefaultAsync()` in `UpdateBioLink`, `ToggleBioLink`, and `DeleteBioLink`.
- [X] T029 [P] [US5] Run the audit script from T025 against the rest of the controllers in [MorWalPizVideo.BackOffice/Controllers/](MorWalPizVideo.BackOffice/Controllers/) and fix any additional violations. Scope is limited to admin controllers per [research.md §R5](./research.md). Keep diff minimal — do not refactor unrelated logic.
- [X] T030 [US5] In [MorWalPizVideo.BackOffice/Authentication/ApiKeyAuthenticationHandler.cs](MorWalPizVideo.BackOffice/Authentication/ApiKeyAuthenticationHandler.cs) line ~84, replace `_ = _apiKeyService.UpdateLastUsedAsync(apiKey.Id!);` with the `ContinueWith(..., OnlyOnFaulted | ExecuteSynchronously, TaskScheduler.Default)` pattern from [research.md §R6](./research.md). Inject `ILogger<ApiKeyAuthenticationHandler>` if not already present.
- [X] T031 [US5] In [MorWalPizVideo.BackOffice/Services/ApiKeyService.cs](MorWalPizVideo.BackOffice/Services/ApiKeyService.cs) `UpdateLastUsedAsync`, inject `ILogger<ApiKeyService>` if not present and replace the existing `catch (Exception) { return false; }` with `catch (Exception ex) { _logger.LogError(ex, "Failed to update LastUsed for ApiKey {ApiKeyId}", apiKeyId); return false; }`.

**Checkpoint**: All five user stories implemented and tested.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T032 [P] Run the full back-office test suite (`dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj`) and confirm all pre-existing tests still pass (SC-007).
- [X] T033 [P] Manual smoke test per the spec's independent-test scripts: mutate one BioLink, one ShortLink, and one Video from the admin SPA; verify success toasts and updated public responses (covers SC-001, SC-002).
- [X] T034 Final audit pass: run `grep_search` for `new HttpClient\(` and `using\s+var\s+\w+\s*=\s*[^;]*\.CreateClient\(` over `MorWalPizVideo.BackOffice/**/*.cs` — must return zero matches (SC-004). Run the same for `\.DeleteOne\(`, `\.InsertOne\(`, `\.UpdateOne\(`, `\.Find\([^)]*\)\.ToList\(\)`, `\.Find\([^)]*\)\.FirstOrDefault\(\)` over `MorWalPizVideo.BackOffice/Controllers/**/*.cs` (SC-005).
- [X] T035 Document the lowercase output-cache-tag convention in [.github/copilot-instructions.md](.github/copilot-instructions.md) (single short line under a "Conventions" sub-section if one exists, otherwise add the section). Reference [contracts/cache-invalidation.http.md](specs/001-cache-invalidation-fixes/contracts/cache-invalidation.http.md).

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 (Setup)**: T001 only. No prerequisites.
- **Phase 2 (Foundational)**: T002. Depends on nothing in this feature; blocks every later test task.
- **Phase 3–7 (User stories)**: Each depends only on Phase 2. **Stories US1, US2, US4, US5 are independent of each other** and can be implemented in parallel by separate developers. **US3 (Phase 5) shares `BioLinksController.cs` with US5 task T028**, so US3 must complete before T028 begins — or, equivalently, T028 must merge after T014–T016.
- **Phase 8 (Polish)**: Depends on all desired user stories being merged.

### Within each user story

- Test tasks first (write FIRST, must FAIL before implementation).
- Implementation tasks next.
- Each story is complete when its checkpoint passes.

### Critical edge

- **T021 depends on T001 and T020** (named-client registration needs the constant and the controller refactor in the same PR).
- **T028 depends on T014–T016** (same file).
- **T030 depends on T026** (test exists before fix).
- **T031 depends on T027** (test exists before fix).

---

## Parallel Execution Examples

### Phase 3 (US1) — all three tests can run in parallel

T003, T004, T005 all live in the same test file but are independent fact methods — they parallelize naturally under xUnit.

### Once Phase 2 completes, multiple developers can fan out:

- Dev A → US1 (T003–T006)
- Dev B → US2 (T007–T011)
- Dev C → US4 (T017–T024) — none of these files overlap with US1/US2/US3
- Dev D → US5 tests (T025–T027) and `ApiKeyAuthenticationHandler` / `ApiKeyService` fixes (T030–T031)
- US3 (Dev A or E after US1 ships) → T012–T016

### Implementation tasks that can run truly in parallel within a single developer's branch

- T020 / T022 / T023 / T024 (Pinterest, Telegram, Discord, Facebook) — four distinct files, no shared symbols.
- T010 / T011 (output cache tag attribute audit vs. CacheKeys constants) — distinct files.

---

## Implementation Strategy

- **MVP scope**: Phase 1 + Phase 2 + Phase 3 (US1). This single PR resolves the visible-on-every-mutation error symptom described by the user. Total: 6 tasks.
- **Incremental delivery**:
  1. Ship MVP → user-visible bug fixed.
  2. Add US2 (Phase 4) → eliminates remaining stale-content risk.
  3. Add US3 (Phase 5) → eliminates ordering corruption.
  4. Add US4 (Phase 6) → hardens HttpClient lifecycle (latent bug).
  5. Add US5 (Phase 7) → observability + async correctness cleanup.
  6. Polish (Phase 8) → final audits and docs.
- Each phase ships behind the same admin endpoints; no feature flags or migrations required.
