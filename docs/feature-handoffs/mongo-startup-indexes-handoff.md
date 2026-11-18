# Known Mongo indexes at startup

## Outcome

Introduce an explicit, reviewed set of known indexes that BackOffice verifies
or creates on startup, beginning with the feedback list sort index. The reported
Cosmos Mongo error occurs in the BackOffice feedback list. Assumption: "well
known" means explicitly selected safe indexes, not every index in the existing
operations manifest. The user also confirmed the public channel matches list
must return to Mongo-side ordering and pagination; see the additional module
below. Both modules are in scope.

Implementation completed for the source-level behavior described below.

Implemented source changes:

- Added Domain-owned `YouTubeContentMongoIndexes` and a Mongo-only hosted
  initializer in both API hosts.
- Added BackOffice startup application of `feedback.createdat_desc` through
  the existing allowlisted operations service.
- Projected the public match index into the BackOffice manifest from the
  Domain-owned definition.
- Changed the Mongo single-channel match repository to sort, skip and limit
  before materialization.
- Changed the single-channel content service list/count path to use the
  repository rather than the full indexed in-memory channel list.
- Added ADR-028 and updated Mongo/feedback operational documentation.

Validation completed: Release build of
`MorWalPizVideo.BackOffice.Tests` passed; focused backend tests for public
match ordering, feedback endpoints, Mongo index operations and indexed-cache
behavior passed (40 tests). Real Cosmos index readiness, legacy timestamp
backfill and production rollout evidence remain deployment gates.

## Current Evidence

- `MorWalPizVideo.Domain/Interfaces/Repository.cs`, `FeedbackRepository.ListAsync`,
  filters unexpired feedback, optionally filters status and application, and
  sorts by `creationDateTime` descending then `_id` descending.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs` already
  defines `feedback.createdat_desc`, a non-unique index on those two sort fields
  in that order. The same manifest includes unique and TTL entries, including
  a feedback TTL whose retention approval remains outstanding.
- `MorWalPizVideo.BackOffice/Program.cs` registers the operations service in
  Mongo mode. Registration does not apply the manifest. The authenticated
  `MongoIndexesController` provides the manual audit/apply routes.
- Both API hosts register `BlogInteractionMongoIndexStartupService` in Mongo
  mode. Its startup behavior is limited to blog interaction indexes; mock mode
  skips it. ADR-022 explicitly leaves other indexes to reviewed operations.
- The reported provider error confirms that the sort cannot currently be served
  by a usable composite index. Whether the declared index is missing, mismatched,
  still building, or unsupported in the deployed environment remains unverified.

## Proposed Design

Add a BackOffice-hosted startup service in the existing Mongo-only composition
branch. Use a fixed explicit startup allowlist whose sole initial entry is the
existing manifest key `feedback.createdat_desc`. Create a service scope and
reuse `IMongoIndexOperationsService.ApplyAsync`; do not duplicate the BSON key
definition or call the HTTP endpoint. Existing blog startup remains unchanged.

Await the apply operation and log the index key and created/skipped result.
Propagate failures with actionable context so a deployment does not silently
claim the index was ensured. Do not retry indefinitely, drop or modify existing
indexes, or automatically apply the full manifest. Reject invalid or duplicate
startup selections before database operations. Future additions require an
explicit review of data, provider behavior and operational impact.

Successful index creation acknowledgment is not proof that Cosmos has completed
index transformation or can serve the query. Verify readiness with the actual
provider and preserve an operational gate before relying on the deployed
endpoint. Do not claim the startup check proves Cosmos readiness without that
evidence.

## Impacted Projects

- `MorWalPizVideo.BackOffice`: Mongo-only hosted service and startup policy.
- `MorWalPizVideo.BackOffice.Tests`: focused hosted-service and manifest tests.
- `MorWalPizVideo.Domain`, `MorWalPizVideo.Models`, ServerAPI, frontend packages
  and deployment manifests: verify only; no source changes planned.

## Impacted Files

### Modify

- `MorWalPizVideo.BackOffice/Program.cs`: register the startup service only in
  the existing Mongo branch.
- `MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs`:
  focused startup-selection and operation behavior tests.

### Add

- A focused hosted-service file in `MorWalPizVideo.BackOffice/Services/`,
  implementing the explicit startup selection and scoped application.
- A new ADR in `docs/architecture/adr/` recording the narrow exception to
  ADR-008 and its relationship to ADR-022.

### Generated/Migration

None. Index creation is additive; no document migration is planned.

### Documentation

- `docs/architecture/mongo-operations.md`: document the exact startup key,
  manual exclusions, privileges, readiness caveat and recovery.
- `docs/features/global-feedback.md`: distinguish startup-managed sort index
  from manually reviewed unique/query indexes and the unapproved TTL.
- Update this handoff after implementation with actual files, checks and risks.

### Verify Only

- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs` and
  `MorWalPizVideo.BackOffice/Controllers/MongoIndexesController.cs`: existing
  index definitions, exact-definition checks and manual API remain unchanged.
- `MorWalPizVideo.Domain/BlogInteractions/BlogInteractionMongoIndexStartupService.cs`:
  preserve the existing separate blog startup behavior.
- `MorWalPizVideo.BackOffice.Tests/Features/FeedbackEndpointTests.cs`: preserve
  feedback pagination and filter behavior.
- `docs/architecture/operations/mongo-index-audits/README.md`: identify any
  instructions made inaccurate by the new exception; correct only affected text.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | BackOffice Mongo startup invokes existing index operations; no API contract changes. |
| Frontend/UI | None; feedback list behavior is unchanged. |
| Database/Data | One additive non-unique sort index; no document writes or deletion. |
| Contracts/Models | None. |
| Authentication/Authorization | Manual endpoint API-key protection remains unchanged; startup uses the configured database identity. |
| Validation | Startup selection must resolve to the intended manifest entry; failures are explicit. |
| Cache | None. |
| Configuration/Secrets | No new secret or flag; database identity needs index-list/create privileges. |
| Logging/Telemetry | Log key and result, not connection strings or feedback contents; surface startup failures. |
| Background Jobs | No Hangfire job; initialization is a hosted startup service. |
| Deployment/Operations | First startup may incur index-build load or fail for insufficient permissions/conflicts; Cosmos readiness must be checked. |
| Backward Compatibility | Existing manual operations and blog initializer remain supported; index remains on rollback. |

## Reuse Opportunities

Reuse `MongoIndexOperationsService` for definition matching, idempotent creation,
collection handling and concurrency-conflict re-audit. Reuse the blog hosted
service's scope lifetime and fail-visible startup pattern without moving its
Domain-owned definitions or broadening its manifest.

## Risks

- **High — host availability:** authorization failures, conflicting indexes or
  provider errors fail BackOffice startup. Mitigate with pre-deployment audit,
  verified privileges and recovery plan; signal: successful startup and audit.
- **Medium — Cosmos index readiness:** accepted creation may precede usable
  index transformation. Mitigate by checking provider status and retrying the
  real feedback request before release; signal: successful sorted, filtered and
  paged reads against the target provider.
- **Medium — unintended data effects:** automatic unique or TTL indexes can
  reject writes or delete data. Mitigate with an explicit one-key allowlist and
  tests excluding those categories; signal: only the reviewed key is passed.
- **Medium — conflicting definitions or concurrent replicas:** same-name or
  equivalent-key conflicts can interrupt startup. Reuse exact-definition
  matching, re-audit conflicts and never drop indexes automatically; signal:
  idempotent repeated startup and explicit conflict errors.
- **Low — resource use:** an initial index build can consume RU/storage.
  Schedule and monitor the rollout; signal: normal request latency and RU use.

## Technical Debt

- The operator-guide sample duplicates a historical manifest inventory and can
  drift. Correct statements directly affected by this change; broader inventory
  reconciliation is deferred.
- Production index state and Cosmos provider readiness are not established by
  source or mock tests. Deployment evidence is a release gate.

## Implementation Roadmap

1. In BackOffice, define the single reviewed startup key and add a scoped hosted
   service using the existing operations service. Ensure invalid selections and
   operation failures stop startup without exposing sensitive data.
2. Register it only in the Mongo branch. Keep the existing blog initializer,
   manual endpoints, ServerAPI, and feedback repository behavior unchanged.
3. Add focused tests for the exact selected key, scoped apply, result logging,
   failure propagation, and mock-mode exclusion. Verify no TTL or unique
   feedback index is included; retain existing manifest and endpoint tests.
4. Update operational documentation and ADR. Record the narrow exception,
   rollout prerequisites and unchanged TTL approval gate.
5. Run the focused BackOffice test/build gates serially according to the test
   runbook. Independently validate index creation and feedback list behavior on
   a disposable or approved Cosmos Mongo instance; do not mutate production as
   part of local implementation.

Completion boundary: the existing feedback sort index, the public match sort
index and reader described below, explicit narrowly scoped startup mechanisms,
targeted tests and documentation. Do not enroll unrelated indexes or redesign
the index subsystem without approval.
An independent post-validation review is warranted because startup availability
and production database operations are affected.

## Testing Strategy

- Unit/service tests in `MorWalPizVideo.BackOffice.Tests`: exact selection,
  single application, scoped resolution, success/skip, cancellation and error
  propagation; reject unknown or duplicate keys and exclude TTL/unique keys.
- Host tests: Mongo mode registers the service; mock mode does not attempt
  database access. Existing blog initializer remains registered as before.
- Regression tests: existing feedback endpoint pagination, filters, permissions
  and index-operation tests continue to pass.
- Real-provider integration: create on an absent index, repeat startup for
  idempotency, test an incompatible index and denied permissions, observe
  concurrent startups, then exercise the actual feedback list including optional
  filters and pagination. Check index build completion and exact definition.
- No UI, contract, schema-migration or new authentication tests are required;
  existing behavior remains unchanged.

## Migration And Rollback

Before rollout, confirm the BackOffice instance targets the intended database,
verify a restorable backup, inspect the existing index definition and secure
the required database privileges. Monitor startup, index build and RU usage.
Release BackOffice first and verify Cosmos completion and the feedback list.
If startup fails, inspect the index and permissions; do not remove or rebuild
indexes automatically. Roll back the application to the prior version if
necessary, retaining any created index. A manually applied index remains an
available operational recovery path subject to the existing review process.
No Mongo document, SQLite, API contract, auth, cache or configuration migration
is required.

## Open Questions

None identified for the selected scope. BackOffice initially ensures the
feedback sort index; the confirmed public match module adds its own ServerAPI
startup index. Further indexes require individual review. Automatically
applying the full manifest would include unique and destructive TTL indexes
and is not assumed.

## Confirmed Additional Module: Mongo-Side Public Match Ordering

### Outcome and Evidence

Restore Mongo-side ordering and pagination for the public single-channel match
list, without materializing the full matching collection before paging.
Preserve the existing response, channel/private filter, date fallback, sort
direction, pagination bounds and canonical short-link enrichment.

- `MorWalPizVideo.Domain/Interfaces/Repository.cs`,
  `GetPublicOrderedForChannelAsync`, currently applies the filter in Mongo but
  materializes all matches before ordering and paging. The separate
  `GetPublicOrderedAsync` already orders and pages in Mongo.
- `MorWalPizVideo.ServerAPI/Controllers/MatchesController.cs` and
  `V1VideosController.cs` use the single-channel service method.
  `MatchesController` separately requests the filtered total count.
- `MorWalPizVideo.Domain/FeatureServices.cs` normally uses
  `IYouTubeContentIndexedCache` for that list and count. Both host composition
  roots register the cache even when the backing cache is disabled.
  `YouTubeContentIndexedCache.BuildEntriesAsync` can load all content before
  filtering; fixing the repository alone therefore does not fix the real path.
- `YouTubeContent.LatestPublishedAt` is persisted but excluded from JSON.
  Writes through `YouTubeContentRepository` calculate it, and the existing mock
  derives the same fallback from video references. Legacy persisted documents
  may be missing, defaulted or stale; deployed data has not been inspected.
- The existing manifest index begins with `isPrivate`, while the requested sort
  starts with `latestPublishedAt`. Its suitability for Cosmos multi-field sort
  is unverified. Do not assert it serves the query merely because it exists.

### Proposed Design

1. Change the Mongo channel-list query to retain its current filter, sort by
   persisted `latestPublishedAt` descending and `creationDateTime` descending,
   then apply skip and bounded limit before materialization. Do not add a third
   tie-breaker: exact ties already have unspecified relative order.
2. Route single-channel list and count through the repository regardless of the
   indexed content cache. Enrich only the returned page with the existing
   `AddCanonicalShortLinksAsync` behavior. Retain the separate output-cache
   behavior and mutation/invalidation paths; do not remove or redesign the
   indexed cache used by other consumers. This deliberately exchanges
   single-channel indexed-cache hits for bounded database reads.
3. Keep the existing index intact. Define a new additive, non-unique
   feature-owned sort index on `latestPublishedAt` descending followed by
   `creationDateTime` descending, matching the actual sort fields. Project its
   definition into the BackOffice operations manifest from one Domain-owned
   definition, following the blog interaction ownership pattern.
   Register a narrowly scoped initializer for this index in ServerAPI's Mongo
   branch, because ServerAPI serves the affected reads and can start before
   BackOffice. Preserve mock mode and the existing blog initializer. Test the
   exact key pattern and query against the real Cosmos Mongo provider before
   relying on it; if provider evidence requires a different supported pattern,
   resolve that incompatibility before release, not with a silent memory
   fallback.
4. Existing persisted timestamps must be backfilled and verified before the
   database-sorted reader is deployed. Follow the existing public-match
   publication-ordering runbook with writes paused, a verified backup and
   resumable batches. Confirm every affected value matches the model's exact
   fallback semantics, not merely that the field exists. Reconcile any write
   paths that bypass the repository before lifting the write pause.

### Additional Impacted Projects and Files

**Modify**

- `MorWalPizVideo.Domain/Interfaces/Repository.cs`: server-side sort and
  pagination for the single-channel reader.
- `MorWalPizVideo.Domain/FeatureServices.cs`: repository-backed single-channel
  list/count and bounded short-link overlay.
- `MorWalPizVideo.ServerAPI/Program.cs`: register the public-match initializer
  only in Mongo mode.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`: include
  the same feature-owned index definition in the operator manifest.
- `MorWalPizVideo.BackOffice.Tests/Features/PublicMatchOrderingTests.cs`: match
  ordering, filtering, fallback and pagination regression cases.
- `MorWalPizVideo.BackOffice.Tests/Services/ContentServiceTests.cs`: prove the
  actual service path uses bounded repository reads even with a cache injected,
  keeps the count filter and preserves page-level canonical links.
- `MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs`:
  feature-owned manifest projection, startup scope and safe index exclusions.
- `docs/architecture/mongo-operations.md` and the operator guide: make backfill
  and real-provider index verification mandatory rollout gates for this reader.

**Add**

- A feature-owned index definition and narrowly scoped startup initializer under
  `MorWalPizVideo.Domain/`, named consistently with existing feature startup
  patterns. Reuse existing index comparison/creation behavior where practical
  without duplicating definitions or expanding the blog initializer's scope.

**Verify only**

- `MorWalPizVideo.Domain/YouTubeContentIndexedCache.cs`: other cache consumers
  and mutation refresh behavior remain intact.
- `MorWalPizVideo.Domain/Interfaces/MockRepository.cs`: preserve the current
  logical ordering and fallback; change only if focused parity tests expose a
  genuine discrepancy.
- ServerAPI matches/video controllers, contracts, authentication and output
  cache configuration remain unchanged.

### Risks and Compatibility

- **High — legacy dates:** missing or stale persisted values change ordering
  when the calculation no longer happens on read. Backup, pause writes, backfill
  and compare computed values before release; check missing, default and stale
  data, empty references, missing dates and equal timestamps.
- **High — startup availability and sequencing:** ServerAPI needs Mongo index
  privileges, and index failure must be visible. Pre-audit and apply before
  deploy; do not assume BackOffice starts first. Cosmos index creation
  acknowledgment may precede readiness.
- **Medium — caching and load:** list/count now read Mongo on cache hits, and
  output caching can mask changes. Monitor request rate, RU, latency, and cache
  expiry/invalidation. Test with output cache both bypassed and enabled.
- **Medium — page correctness:** no extra tie-breaker is introduced; equal dates
  remain unspecified. Preserve count/filter semantics and canonical links.
- **Medium — simultaneous writes during backfill:** an uncontrolled writer can
  reintroduce stale values. Inventory and pause all affected writers, reconcile
  before resuming and validate subsequent repository write paths.

### Dependency-Ordered Roadmap and Verification

1. Add the feature-owned sort-index definition and operator-manifest projection,
   then the Mongo-only ServerAPI startup initializer. Keep feedback startup
   separately scoped to BackOffice. Test allowlists, existing definitions,
   idempotency, concurrent startup and failure propagation.
2. Validate the candidate index and exact filter/sort against disposable Cosmos
   Mongo, including optional filters relevant to the query and realistic data.
   Verify the exact index is accepted and serves the two-field sort. Do not
   equate a Mongo mock or an index-create response with this evidence.
3. In the approved environment, complete backup, paused-write legacy backfill,
   reconciliation and post-backfill verification. Deploy the BackOffice artifact
   containing the new operator-manifest entry before using its reviewed
   operations to pre-apply the validated index; wait for Cosmos readiness and
   preserve evidence. These rollout gates precede deploying the new ServerAPI
   reader.
4. Implement repository-side paging and route the service's single-channel
   list/count around the indexed cache; preserve bounded page enrichment.
   Add focused unit and behavior tests and build/test the affected backend
   project in the runbook's serial order.
5. Test real-provider first and later pages, filters, ties, legacy and missing
   dates, count, private/other-channel exclusion, and canonical links; confirm
   Mongo receives the sort, skip and limit before materialization. Compare
   expected ordering and monitor Cosmos RU and latency. Test the actual ServerAPI
   endpoints with indexed and output caches both enabled and disabled.
6. Update architecture/feature documentation, ADR and this handoff with
   implementation evidence. Recommend an independent review because startup,
   stored-data compatibility, caching and public pagination change together.

### Rollback and Open Questions

Deploy the prior reader if provider index readiness or data validation fails.
Retain the additive index and backfilled timestamps; do not delete data or
automatically drop an index. Flush or expire relevant output-cache entries
through existing operations as needed. Backfill is not reversed: preserve the
verified backup if recovery is needed. The existing public match index remains
available for other readers.

The exact Cosmos-supported index plan and deployed data completeness remain
unverified; both are release gates, not assumptions. The approved scope is the
single-channel public list, not the separate multi-channel or admin lists.

### Public Match Sort Index Correction (2026-11-18)

The production error confirmed Cosmos could not serve the public reader's
two-field sort from the existing three-field index whose leading key is
`isPrivate`. The implementation now adds the exact non-unique composite index
`{ latestPublishedAt: -1, creationDateTime: -1 }` to the Domain-owned
`YouTubeContentMongoIndexes.Manifest`. Because both API hosts already register
that Mongo-only initializer, they will verify/create the new index before
accepting traffic. The existing index is retained and still projected into the
BackOffice operations manifest; neither index is dropped.

`PublicMatchOrderingTests` now verifies the exact new index definition in the
operator manifest. Architecture documentation and ADR-028 identify the
two-field index as the one required for the public sort.

Validation: the Release build of `MorWalPizVideo.BackOffice.Tests` passed, and
the focused `Manifest_contains_an_exact_index_for_the_public_match_sort` test
passed. The build reported existing SixLabors.ImageSharp vulnerability
warnings.

The code change cannot confirm Cosmos index readiness or correct any existing
`latestPublishedAt` values. Deploy both API artifacts to ensure the new startup
initializer runs, verify the exact index in the live Cosmos database, and retry
the public endpoint after Cosmos reports the index ready. If startup reports an
existing same-name or same-key conflict, resolve it through the reviewed
operator workflow; startup intentionally does not drop or repair indexes.
