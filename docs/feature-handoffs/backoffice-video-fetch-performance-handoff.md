# BackOffice video fetch performance

Status: implemented and locally verified, 2026-11-10. Release build passed; focused backend regression selection passed 90/90. No deployment or real-provider latency benchmark performed. The original implementation contract is retained below; see Actual Delivery for executed changes and evidence.

## Outcome

Reduce the reported approximately three-second BackOffice video-list response through the user's three steps:

1. Add a bulk overload of the channel-read authorization operation.
2. Improve canonical short-link merging.
3. Streamline the existing fetch/cache path.

Assumption: step 3 means removing serial I/O and avoidable repeated work, not introducing pagination, a new response shape, or new cache scopes. Preserve authorization decisions, effective impersonated identity, ordering, private-content visibility, link selection, and error propagation. Measured latency attribution is not yet available; source proves expensive mechanisms, not their share of the observed delay.

This is backend-only work. The original plan below defines its scope; Actual Delivery distinguishes completed local implementation from outstanding operational measurements.

## Baseline Evidence (Before Implementation)

Paths below are repository-relative and refer to current source.

| Evidence | Verified behavior |
| --- | --- |
| [VideosController.cs](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs), Fetch and GetAuthorizedMatchesAsync | Fetch obtains the full authorized list, then converts it to the existing contracts. The helper resolves admin status, reads content, sequentially awaits CanReadInChannelAsync for every match, and merges canonical links. TagSuggestions and FindAuthorizedMatchAsync also use this helper; detail and mutation lookup paths therefore share its behavior. |
| [VideoAuthorizationService.cs](../../MorWalPizVideo.BackOffice/Services/VideoAuthorizationService.cs), IsAdminAsync and CanReadInChannelAsync | Each per-match check resolves the effective user's access profile. Non-admin, non-API-key callers also query active channel ownership. Admin/API-key access still requires owner-channel or attached-reference membership in the selected channel. |
| [UserAccessResolver.cs](../../MorWalPizVideo.Domain/Security/UserAccessResolver.cs), ResolveAsync | Queries the user and, if group IDs exist, groups. There is no request memoization. Uses the repository-compatible user-ID lookup. |
| [ChannelContext.cs](../../MorWalPizVideo.BackOffice/Services/ChannelContext.cs), ChannelContextResolver and ChannelScopeMiddleware | Validates X-Channel-Id before the action and records channel/admin/API-key context. Missing scope gives 400; unknown/inaccessible scope gives 404. |
| [FeatureServices.cs](../../MorWalPizVideo.Domain/FeatureServices.cs), ContentService.GetAuthorizedMatchesAsync | Reads the admin-global cache then filters owner/reference channel membership. Without an injected indexed cache it uses repository reads and canonical enrichment. With caching disabled the registered indexed cache itself falls back to repository materialization. |
| [FeatureServices.cs](../../MorWalPizVideo.Domain/FeatureServices.cs), LinksService.MergeCanonicalVideoShortLinksAsync | Queries canonical YouTube links across all content on every call, even for an empty list. Groups by aggregate Id plus YouTube target, picks shortest QueryString, emits canonical links in VideoRefs order, and appends embedded non-YouTube links. |
| [YouTubeContentIndexedCache.cs](../../MorWalPizVideo.Domain/YouTubeContentIndexedCache.cs), ReadScopeAsync and BuildEntriesAsync | Cache materialization already enriches canonical links. A warm read loads all indexed entries and validates membership with a nested index scan. A missing/inconsistent entry triggers a full rebuild. Global order is CreationDateTime descending. |
| [MorWalPizMemoryCache.cs](../../MorWalPizVideo.MvcHelpers/Services/MorWalPizMemoryCache.cs), MorWalPizIndexedCacheStore.GetManyAsync | Despite its name, retrieves keys with a sequential awaited loop. Each distributed-cache value is deserialized separately. |
| [Program.cs](../../MorWalPizVideo.BackOffice/Program.cs), cache registration | EnableCache selects enabled behavior. Redis uses IDistributedCache; absent Redis configuration uses distributed memory. The indexed cache is singleton; authorization resolution is scoped. |
| [Indexed cache specification](../architecture/indexed-entity-cache-specification.md), YouTubeContent Adaptation | Explicitly records that isolated short-link changes do not refresh the indexed content cache. This is supporting documentation; the live merge must not be deleted on the assumption that cached links are always fresh. |

Current flow: authentication/permission checks -> selected-channel validation -> content/cache read -> serial per-match authorization -> global canonical-link query -> contract conversion/JSON.

Consumer inspection found one production caller of CanReadInChannelAsync and one of MergeCanonicalVideoShortLinksAsync, both in VideosController. Other authorization methods are used by channel, compilation, API-key, and short-link controllers and must remain compatible. ContentService and the indexed-cache infrastructure are shared with ServerAPI.

BackOffice, Domain, MvcHelpers, and the owning test project currently target .NET 10; their manifests confirm the existing dependency direction and xUnit/WebApplicationFactory infrastructure.

## Implementation Contract

### 1. Bulk channel-read authorization

- Add an **overload**, not an override, on IVideoAuthorizationService and its implementation. It accepts the principal, a materialized read-only collection of existing YouTubeContent records, and channel ID, and returns the readable records in input order.
- Prefer records over bare match IDs: the controller already has the data required for authorization. An ID-only operation would require reloading records or trusting insufficient input. No HTTP bulk endpoint is needed.
- For a nonempty collection, resolve effective user/admin access once per bulk invocation. For non-admin, non-API-key callers, load channel ownership at most once.
- Apply the existing per-record owner-channel OR attached-reference predicate in memory. An aggregate creator alone does not grant selected-channel read access. Admins and API keys must not bypass membership checks.
- Empty input returns an empty result without dependency calls. This is valid empty data, not an authorization-error fallback.
- Retain the scalar method and make it share the same authorization rules through the bulk implementation. Do not change CanAccessAsync, CanManageChannelAsync, or mutation semantics.
- Replace the controller's awaited per-match loop with one bulk call. Reuse the server-validated ChannelContext.IsAdmin for the content-service argument instead of resolving admin status a second time before content retrieval. The bulk service remains independently authoritative; it does not accept a caller-supplied trusted-admin Boolean.
- Keep authorization request-local. Do not add a process-wide authorization cache, change token claims, or parallelize the old authorization loop.

Result: authorization I/O in this helper becomes independent of match count. Existing middleware and permission checks may still resolve identity separately; this proposal does not claim one access lookup for the entire HTTP pipeline.

### 2. Narrow, fresh canonical-link merge

- Preserve the live LinksService merge so isolated short-link create/update/delete operations remain visible without waiting for content-cache refresh.
- Return immediately for empty matches. Build distinct, nonblank **aggregate document IDs from match.Id**, not ContentId or YouTube IDs.
- Query once for YouTubeVideo links with a nonnull ContentId contained in those IDs. Reuse the predicate approach already present in ContentService.AddCanonicalShortLinksAsync rather than adding a repository abstraction solely for this query.
- Group the bounded results by the existing ordinal aggregate/target identity. Preserve shortest-QueryString selection and existing first-result behavior for equal lengths; do not introduce a new tie-breaker in this refactor.
- Preserve VideoRefs order, repeated-reference behavior, canonical-first output, embedded non-YouTube links, and replacement/removal of embedded YouTube links even when no canonical result exists.
- Do not substitute ContentService's enrichment implementation wholesale: it currently has different duplicate-selection and ordering behavior.
- Preserve existing repository failure behavior. A database error must not silently return cached or empty links.

Warm fetch target: one bounded live canonical-link query, zero when there are no authorized matches. Cold/disabled-cache materialization can still perform its own enrichment query before this authoritative overlay. Removing that second cold-path query is deferred rather than weakening freshness or changing shared-service contracts.

### 3. Streamline the existing cache read

- Keep current keys, scopes, TTLs, provider selection, sort rules, and cache recovery behavior.
- Replace serial GetManyAsync retrieval with bounded asynchronous retrieval through the existing cache abstraction. Proposed internal cap: eight in-flight reads per invocation, with no new configuration or dependency. This is an initial engineering limit, not a measured provider-capacity guarantee.
- Preserve dictionary key association, ordinal comparison, duplicate-key last-assignment semantics, and input-key insertion order. Do not concurrently mutate a standard dictionary. These properties also avoid changing stable ordering when creation timestamps tie.
- Preserve failure propagation and wait for started work to finish before completing the operation. Do not convert errors into cache misses or partially successful dictionaries. Honor the existing cancellation contract without claiming that the current underlying IMorWalPizCache cancels in-flight provider calls.
- In ReadScopeAsync, replace repeated membership scans with an ordinal ID set while retaining current count/null checks and missing/inconsistent-entry rebuild decisions.
- Keep the global read followed by channel filtering for now. A private admin-channel index would require new scope membership and invalidation design; the existing public-channel scope is not a substitute because it excludes private/owner-only content.
- Compare cache retrieval, authorization, merge, and total request timings before and after. Use existing diagnostics/test instrumentation; add production timing spans only if current telemetry cannot distinguish phases, without logging principals, credentials, titles, or payloads.

## Impacted Projects

| Project/surface | Impact |
| --- | --- |
| MorWalPizVideo.BackOffice | Bulk authorization and controller orchestration. Deploy new application artifact. |
| MorWalPizVideo.Domain | Bounded live short-link query and linear cache membership validation. |
| MorWalPizVideo.MvcHelpers | Bounded multi-key cache retrieval; shared runtime behavior. |
| MorWalPizVideo.BackOffice.Tests | Authorization, merging, cache adapter, and endpoint regression coverage. |
| MorWalPizVideo.ServerAPI | Verify-only direct consumer of ContentService/indexed cache and the shared adapter; smoke-test public reads if its binary is updated. |
| Models / MorWalPiz.Contracts | Verify-only: existing entity and HTTP DTO shapes remain unchanged. |
| Frontend workspaces/packages, Windows tools, ShortLinks host, AppHost | No source or contract changes planned. BackOffice clients retain the same list/detail responses. No frontend or Impeccable work is required. |
| Deployment workflows/configuration | No workflow, provider, flag, secret, or topology changes. Shared-library changes take effect in each host when that host is rebuilt/deployed. |

## Impacted Files

Paths are relative to the repository root.

### Modify

- [MorWalPizVideo.BackOffice/Services/VideoAuthorizationService.cs](../../MorWalPizVideo.BackOffice/Services/VideoAuthorizationService.cs): additive bulk interface operation, shared scalar/bulk rules.
- [MorWalPizVideo.BackOffice/Controllers/VideosController.cs](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs): one bulk call and validated channel context reuse in the common helper.
- [MorWalPizVideo.Domain/FeatureServices.cs](../../MorWalPizVideo.Domain/FeatureServices.cs): LinksService query scope and empty-input handling only; do not refactor unrelated services in this large file.
- [MorWalPizVideo.Domain/YouTubeContentIndexedCache.cs](../../MorWalPizVideo.Domain/YouTubeContentIndexedCache.cs): linear membership validation, unchanged healing.
- [MorWalPizVideo.MvcHelpers/Services/MorWalPizMemoryCache.cs](../../MorWalPizVideo.MvcHelpers/Services/MorWalPizMemoryCache.cs): bounded retrieval preserving result semantics.
- [MorWalPizVideo.BackOffice.Tests/Services/YouTubeContentIndexedCacheTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/YouTubeContentIndexedCacheTests.cs): warm/recovery/order coverage.
- [MorWalPizVideo.BackOffice.Tests/Features/VideoPermissionAuthorizationTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/VideoPermissionAuthorizationTests.cs) and [ChannelTenancyAuthorizationTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs): endpoint authorization regression coverage.

### Add

In the existing [test Services directory](../../MorWalPizVideo.BackOffice.Tests/Services/), focused tests named for VideoAuthorizationService, LinksService, and MorWalPizIndexedCacheStore. Use counting/guarded fakes rather than new mocking dependencies.

### Generated/Migration

None. No generated feature tests, persistence schemas, indexes, or package manifests need changes.

### Documentation

- This handoff: update with actual files, measurements, validation results, and remaining gates after implementation.
- [Backend architecture](../architecture/backend.md): record bulk authorization on the list path when implemented.
- [Indexed cache specification](../architecture/indexed-entity-cache-specification.md): record bounded retrieval and linear validation; retain the isolated-short-link staleness warning.
- No new ADR is proposed: ownership, authorization policy, persistence, and deployment architecture remain unchanged.

### Verify Only

- [UserAccessResolver.cs](../../MorWalPizVideo.Domain/Security/UserAccessResolver.cs), [ChannelContext.cs](../../MorWalPizVideo.BackOffice/Services/ChannelContext.cs), and [repository implementations](../../MorWalPizVideo.Domain/Interfaces/Repository.cs): identity, channel policy, Mongo predicate translation.
- [ContentServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/ContentServiceTests.cs), [FocusedServiceDependencyTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/FocusedServiceDependencyTests.cs), and [V1ShortLinkAndMigrationTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/V1ShortLinkAndMigrationTests.cs): compatibility/reuse evidence, not presumed bulk-test coverage.
- [BackOffice test factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs): existing endpoint fixture and isolated scenario infrastructure.
- [ContractUtils.cs](../../MorWalPiz.Contracts/ContractUtils.cs) and [ServerAPI MatchesController.cs](../../MorWalPizVideo.ServerAPI/Controllers/MatchesController.cs): unchanged response conversion and public-read behavior.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | Internal additive overload; same routes/statuses/payloads. Common helper consumers all receive the optimization. |
| Frontend/UI | Not applicable; no UI or loader changes, response stays an array. |
| Database/Data | Narrow existing short-link read; no writes, schema/index migration, or backfill. Verify Mongo translation. |
| Contracts/Models | No HTTP DTO/entity changes; internal service interface grows additively. |
| Authentication/Authorization | Preserve effective identity, active ownership, permission gates, API-key behavior, impersonation, and per-record membership. |
| Validation | Preserve header errors, compatible ID lookup, empty-list semantics, and lookup-not-found behavior. |
| Cache | Same global/public scopes and eviction/refresh rules; bounded reads and linear validation only. Keep live canonical overlay. |
| Configuration/Secrets | No additions or reads of deployed secret values. |
| Logging/Telemetry | Before/after phase timings with existing diagnostics; optional minimal spans only if necessary. No sensitive payload logging. |
| Background Jobs | No new jobs; existing refresh worker remains unchanged. |
| Deployment/Operations | Normal host rebuild/deploy. Observe provider pressure and request latency; no fleet-wide configuration switch. |
| Backward Compatibility | Same clients, scalar authorization operation, cache serialization, keys, ordering, and permission policy. |

## Reuse Opportunities

- Reuse IsAdminAsync/effective identity resolution and the existing owner repository, without cross-request caching.
- Reuse validated ChannelContext for the content read's existing admin argument.
- Reuse ContentService's content-ID-scoped canonical predicate, but preserve LinksService's own canonical-winner semantics.
- Reuse IIndexedCacheStore, IMorWalPizCache, existing DI, deterministic cache keys, and missing-entry rebuild path.
- Reuse xUnit, isolated mock scenarios, the BackOffice factory, existing permission/tenancy tests, and counting-fake patterns in ContentServiceTests.

## Risks

| Rank | Cause / surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| High | Bulk check accidentally grants whole-batch access, trusts actor instead of effective identity, or grants creator-only access | Preserve per-record membership and effective identity; keep permission middleware | Mixed-channel, impersonation, API-key, inactive-owner, and denied-user tests match scalar results |
| High | Removing live merge serves stale or deleted canonical links | Retain a fresh query scoped to authorized aggregate IDs | Warm cache followed by isolated link create/update/delete returns current links |
| Medium | Parallel cache reads overload a remote provider or reorder tied results | Bound to eight; preserve key/result order; validate under Redis and memory | In-flight counter stays at or below eight, outputs match, no error/timeout increase |
| Medium | Merge optimization changes duplicate winner or strips non-YouTube links | Preserve ordinal key and existing selection/output semantics | Golden cases cover shortest-query winner, ties, duplicate refs, absent links, legacy links |
| Medium | Cache validation optimization changes healing | Preserve counts/null/membership checks | Missing entry, wrong entity ID, and stale index still rebuild |
| Medium | Mongo/mock behavior differs for the bounded predicate | Reuse existing query pattern and run a real-Mongo read check | Query translates and excludes unrelated content |
| Low | End-to-end delay lies elsewhere or global materialization dominates after fixes | Measure before and after; retain honest attribution | Warm p50/p95 and phase timings reported with collection/payload sizes |

## Technical Debt

- Global content retrieval and unpaginated HTTP responses remain; defer new admin-channel indexes and pagination to separately approved work.
- Cold/disabled paths can enrich links before the live overlay; retain until shared-service freshness contracts are explicitly redesigned.
- Authorization is resolved independently by middleware/policies and the bulk service. This does not block eliminating per-video I/O; do not introduce a general authorization snapshot subsystem here.
- Generic cache APIs do not expose provider-native batch retrieval and the wrapper does not forward cancellation into underlying cache operations. Bounded concurrent reads are a contained improvement, not a claim of native Redis batching or full cancellation support.
- Existing enrichment implementations differ. Do not consolidate them during this performance change without a separate behavior decision.

## Implementation Roadmap

1. **Establish preservation and timing baseline.** In BackOffice.Tests, characterize scalar authorization, merge output, and warm cache recovery/order. Record current dependency-call counts for 1, 50, and 200 matches. In the representative deployment capture phase and total timings using the same authenticated identity, channel, dataset, provider, and payload. Gate: document baseline and prove tests exercise the production adapter, not only the in-memory indexed-store fake.
2. **Bulk authorization and controller wiring.** Add the overload in BackOffice and update the helper. Reuse ChannelContext.IsAdmin for content retrieval; keep bulk authorization authoritative. Gate: scalar/bulk equivalence, constant dependency calls, unchanged permission/tenancy responses and detail/tag/mutation lookup behavior.
3. **Bound the canonical merge query.** Modify LinksService only. Gate: zero reads on empty input, one scoped query otherwise, unchanged canonical winner/order, and fresh links after isolated changes with a warm content cache.
4. **Improve indexed cache retrieval.** Update MvcHelpers retrieval and Domain validation independently of policy changes. Gate: bounded concurrency, stable dictionary/output association and ordering, unchanged error propagation, linear membership check, unchanged rebuild/refresh scenarios, and public/private scope isolation.
5. **Validate the integrated flow and document results.** Build the affected test project once serially; run one focused selection spanning the changed services, video permission/tenancy, link behavior, and relevant cache/public-read tests without rebuilding. Use the repository testing runbook. Escalate only for failures or additional affected behavior. Repeat warm timings against the same baseline; record counts, dataset sizes, sample count, p50/p95, and provider. Update this handoff and owning architecture documentation.
6. **Release and observe.** Deploy BackOffice normally; deploy ServerAPI only if updating its shared-library artifact. Monitor request duration/errors and Redis/Mongo pressure. Keep baseline measurements for rollback comparison.

Completion boundary: the three optimizations, their regression proof, and measured before/after evidence. No UI, pagination, new cache partition, provider replacement, broad FeatureServices extraction, or mutation-policy cleanup.

An independent post-validation review is recommended because authorization batching and shared cache concurrency are correctness-sensitive. The user may select MorWalPiz Code Reviewer; this plan does not assign that review to the architect.

## Testing Strategy

- **Unit/service, BackOffice.Tests:** compare scalar and bulk results for mixed readable/unreadable records, owner-only membership, collaborator-reference membership, unrelated creator, admin, API key, absent effective ID, inactive/missing user, inactive ownership, and impersonation. At 1, 50, and 200 records assert at most one access resolution and at most one ownership read per bulk call; empty input makes no calls. Ownership reads occur only for non-admin/non-API-key branches.
- **Link contracts, BackOffice.Tests:** capture predicate and call count, aggregate Id distinct from ContentId, unrelated aggregate exclusion, empty data, missing canonical links, duplicate targets/refs, shortest QueryString and ties, non-YouTube preservation, no mutation of input, and propagated repository failures.
- **Adapter tests, BackOffice.Tests:** controlled asynchronous fake with a release gate proves overlap without timing flakiness; in-flight maximum never exceeds eight. Cover empty input, duplicate keys, reversed completion order, null/missing values, and provider exceptions. Do not infer this behavior from tests that bypass MorWalPizIndexedCacheStore.
- **Cache tests, BackOffice.Tests:** warm reads avoid source materialization; null/missing/wrong-ID entries rebuild; global/private and public/channel filtering remain distinct; equal timestamp ordering, refresh retry/deletion behavior, and cache-disabled paths stay compatible.
- **Integration/behavior and security regression, BackOffice.Tests:** same list array and DTO fields, same ordering and read visibility for selected channels; unchanged 401/403/400/404 boundaries, tag suggestions, detail lookup, and mutation lookup restrictions. Verify isolated canonical changes over warm content. No vulnerability audit or authentication redesign is part of this task.
- **Real-provider validation:** verify the scoped Mongo predicate translates, and compare warm memory/Redis paths where available. Do not substitute mock-only timings for deployed evidence.
- **Performance acceptance:** dependency counts are constant with match count for bulk authorization; merge reads are zero/one on the warm helper path and exclude unrelated content; cache retrieval overlaps within its bound. For latency compare at least 30 warmed requests per representative case and report p50/p95 versus baseline, payload/count/provider, and error rates. No absolute sub-second guarantee is asserted without the environment baseline. A structural-only improvement must be reported as such, not as resolution of the observed three seconds.
- **Component/browser, migration:** not applicable; no frontend or stored-data change. A client smoke read verifies the unchanged API contract without a frontend redesign.

## Migration And Rollback

No MongoDB/SQLite data migration, API transition window, auth migration, cache-key change, backfill, new flag, or configuration rollout is needed. The proposal only changes internal orchestration and reads. Existing cache contents remain usable.

Deploy one BackOffice artifact containing the additive service overload and caller update. The shared cache adapter also affects any ServerAPI artifact rebuilt with it; validate public reads before deploying that host. Hosts can otherwise roll independently with the same cache format.

Rollback to the prior application artifact if authorization output differs, cache-provider pressure/errors rise, or warm latency regresses. No cache purge or data recovery should be necessary because keys and writes are unchanged. Retain the live link overlay in both versions. Existing background cache staleness/rebuild behavior is not repaired by rollback.

## Open Questions

None blocking this proposal. Step 3 is explicitly scoped above. Deployed provider, request phase timings, collection sizes, and acceptable latency are measurement inputs still to collect, not reasons to assume the cache is local or promise a response-time threshold. If removing global materialization is required after these changes, it needs a separate scope/index/invalidation decision.

## Actual Delivery

### Changes

| Modified file | Delivered behavior |
| --- | --- |
| [VideoAuthorizationService.cs](../../MorWalPizVideo.BackOffice/Services/VideoAuthorizationService.cs) | Added a bulk overload accepting existing records and returning readable records in order. Resolves access once and ownership at most once per nonempty call. Scalar reads delegate to the same rules; effective identity, claim fallback, API-key behavior, and owner/reference membership remain intact. |
| [VideosController.cs](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs) | Reuses the validated ChannelContext.IsAdmin for content retrieval and invokes bulk authorization once instead of a sequential per-record authorization loop. Keeps live link merging after filtering and existing helper consumers. |
| [FeatureServices.cs](../../MorWalPizVideo.Domain/FeatureServices.cs) | LinksService skips empty input and scopes its single canonical-link query to distinct nonblank match.Id values. Canonical winner, reference/output order, duplicate references, non-YouTube links, and error propagation are unchanged. |
| [MorWalPizMemoryCache.cs](../../MorWalPizVideo.MvcHelpers/Services/MorWalPizMemoryCache.cs) | Batches multi-key reads eight at a time with Task.WhenAll. Combines results in original key order, preserving ordinal keys, duplicate last-value behavior, and nulls. Synchronous and asynchronous provider failures are captured as tasks so all started reads in the batch settle before propagation. |
| [YouTubeContentIndexedCache.cs](../../MorWalPizVideo.Domain/YouTubeContentIndexedCache.cs) | Replaced the nested index-membership scan with an ordinal HashSet. Preserved sorting, count/null validation, and healing. |
| [VideoAuthorizationServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/VideoAuthorizationServiceTests.cs) | Added scalar/bulk policy cases, effective target identity, admin/claim/API-key cases, inactive/unrelated ownership, empty input, failure propagation, and constant dependency-call checks at 1/50/200 matches for admins and owners. |
| [LinksServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/LinksServiceTests.cs) | Added scoped query/count checks, aggregate-versus-business-ID distinction, Mongo driver predicate rendering, duplicate/tie/order preservation, input immutability, repeated link create/update/delete reads, empty input, and error propagation. |
| [MorWalPizIndexedCacheStoreTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/MorWalPizIndexedCacheStoreTests.cs) | Gate-controlled tests prove eight overlapping reads, maximum eight in flight over 25 keys, deterministic key association despite reversed completion, duplicate/ordinal/null semantics, no empty-input reads, and synchronous/asynchronous failure draining. No elapsed-time assertions or new test dependencies. |
| [VideosFetchPerformanceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/VideosFetchPerformanceTests.cs) | HTTP coverage for owners/admins with cache enabled/disabled using the production indexed adapter and distributed-memory serialization. Confirms private owner-only and collaborator-reference visibility, exclusion of unrelated creator-owned content, ordering, warm reads, current links after isolated create/update/delete, unchanged detail lookup, and out-of-scope 404. |
| [YouTubeContentIndexedCacheTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/YouTubeContentIndexedCacheTests.cs) | Added tied-order/private-scope warm-read checks and wrong-entity-ID healing. Existing missing-entry/refresh/retry/delete coverage remains passing. |
| [Backend architecture](../architecture/backend.md), [cache specification](../architecture/indexed-entity-cache-specification.md), [content feature record](../features/content-import.md), and this handoff | Recorded delivered behavior, preservation boundaries, local evidence, and operational gaps. |

No dependency, configuration, cache format/key, persisted data, frontend, route, HTTP DTO, or deployment-workflow changes. No new ADR, migration, or feature flag is required.

### Executed Validation

Working directory: repository root.

Baseline, before source edits:

```bash
dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter 'FullyQualifiedName~ChannelTenancyAuthorizationTests|FullyQualifiedName~VideoPermissionAuthorizationTests|FullyQualifiedName~YouTubeContentIndexedCacheTests|FullyQualifiedName~ContentServiceTests|FullyQualifiedName~V1ShortLinkAndMigrationTests'
```

Result: build passed; 45 passed, zero failed/skipped. Baseline compilation reported 93 existing warnings.

Final source/test verification, built serially and tested without rebuilding:

```bash
dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter 'FullyQualifiedName~VideoAuthorizationServiceTests|FullyQualifiedName~LinksServiceTests|FullyQualifiedName~MorWalPizIndexedCacheStoreTests|FullyQualifiedName~VideosFetchPerformanceTests|FullyQualifiedName~ChannelTenancyAuthorizationTests|FullyQualifiedName~VideoPermissionAuthorizationTests|FullyQualifiedName~YouTubeContentIndexedCacheTests|FullyQualifiedName~ContentServiceTests|FullyQualifiedName~V1ShortLinkAndMigrationTests|FullyQualifiedName~ContentTagTests|FullyQualifiedName~PublicVideoReferenceClassificationTests|FullyQualifiedName~FocusedServiceDependencyTests|FullyQualifiedName~ImpersonationTests'
```

Result: build passed with zero errors and 17 existing warnings in the incremental build; 90 passed, zero failed/skipped in approximately 17 seconds. Warnings include pre-existing ImageSharp advisories and nullable warnings in unrelated tests. This is not a full-suite result. The first new-test run exposed fixture ordering/channel-constructor mistakes; those fixtures were corrected before this passing run, without changing production behavior to accommodate them.

Structural performance evidence:

- Bulk service: exactly one access resolution at 1, 50, and 200 records; owners require one ownership read and admins zero. Empty input calls neither dependency. Middleware/policy access checks are separate.
- Warm live merge: one scoped repository query for nonempty records and none for empty input. Mongo driver successfully renders the scoped predicate to a contentId inclusion filter; no actual Mongo server was contacted.
- Production cache adapter: eight reads can be pending together; maximum concurrency is eight across 25 keys. All started reads settle on provider failure.
- Cached HTTP responses preserve current isolated link edits without a content-cache refresh, for both admin and owner readers. Cache-disabled reads also pass.

Full solution/workspace builds, broad lint/format/hygiene checks, and full test suites are deferred to the downstream automatic final procedure; focused validation is not duplicated. No independent review agent was invoked.

### Operational Evidence Still Required

No deployed Redis/Mongo credentials or production traffic were used. Therefore there is no before/after request p50/p95, provider-pressure measurement, or absolute latency promise. The original approximately three-second symptom is addressed by eliminating its identified serial mechanisms, but the actual response-time improvement must be measured after deployment.

Use the same identity/channel/dataset/provider/payload for at least 30 warmed requests before and after rollout, recording content/cache, authorization, merge, and total request durations plus errors and collection sizes. Verify real Mongo execution as well as the already-tested driver translation. Observe Redis pressure under concurrent requests: the eight-read limit is per invocation, not fleet-wide.

Global cache materialization, unpaginated payloads, independent middleware authorization reads, cold-path duplicate enrichment, and isolated-link staleness in cached paths without the BackOffice live overlay remain deliberately unchanged. Existing telemetry was not expanded; targeted phase tracing is an operational follow-up if current spans do not provide attribution. Normal artifact rollback remains sufficient.
