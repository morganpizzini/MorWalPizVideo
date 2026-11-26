# Mongo index startup alignment handoff

Status: implemented in source and focused tests; no live database changes or
deployment have been made.

## Outcome

BackOffice attempts every source-defined index in the shared platform Mongo
database on Mongo-mode startup. It applies entries sequentially in a background
service and audits the actual definition after every attempt. **Missing,
incompatible or uncreatable indexes do not block application startup, readiness
or traffic.** Failures are reported in structured logs; operators can inspect
actual state through the existing authenticated audit/apply endpoints.

Scope:

- "All indexes" means all application-defined indexes for the shared platform
  Mongo database, including Workshop and the two previously lazy-created blog
  indexes; MongoDB's built-in `_id` indexes are not application-managed.
- The FreezerAnalysis application database is explicitly excluded. No changes to
  FreezerAnalysis configuration, startup, indexes or deployment are planned.
- Mock-mode startup remains independent of MongoDB. No live database change,
  production deployment, or destructive operation is authorized by this plan.

## Source And History

- [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs)
  owns the BackOffice executable manifest, exact-definition audit and sequential
  approved apply. Its apply operation creates missing collections and indexes,
  skips exact matches, rechecks recognized concurrent-create conflicts and
  reports errors. Creation is not atomic across the whole manifest.
- [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs)
  defines Workshop indexes projected into that manifest. The source manifest
  is not evidence that any deployed database has those indexes.
- [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs) previously
  created two blog indexes lazily from repository calls; those definitions now
  live in the central manifest and repository calls do not create indexes.
- The previous BlogInteraction, YouTube and Feedback hosted index services are
  no longer registered by either API. Their feature definitions remain inputs
  to the BackOffice manifest; BackOffice registers only
  [MongoIndexStartupService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexStartupService.cs)
  in Mongo mode.
- [AppHost/Program.cs](../../MorWalPizVideo.AppHost/Program.cs) starts BackOffice
  and ServerAPI independently. The BackOffice, ServerAPI and ShortLinks
  deployment workflows are independent; their ZIP deployment jobs do not
  establish a cross-workflow startup dependency.
- [ServiceDefaults/Extensions.cs](../../MorWalPizVideo.ServiceDefaults/Extensions.cs)
  maps liveness and readiness endpoints, but an endpoint name alone does not
  prove that the deployed load balancer honors it.
- The [FreezerAnalysis registration](../../FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs)
  explicitly requires its application database to differ from the platform
  database. [FreezerAnalysisMongoRepository.cs](../../MorWalPizVideo.Domain/FreezerAnalysis/FreezerAnalysisMongoRepository.cs)
  declares separate application indexes. The shown repository method has no
  call site in the current C# source; do not claim those indexes run at startup.
- [ADR-029](../architecture/adr/ADR-029-nonblocking-mongo-index-startup.md)
  supersedes the shared-platform startup portions of ADR-008, ADR-022, and
  ADR-028. The separate FreezerAnalysis application database remains excluded.

## Implemented Design

### Desired state and ownership

1. Keep the existing BackOffice manifest and exact comparison as the source of
   truth, preserving all current manifest keys, index names, collection names,
   ordered key patterns, uniqueness, partial filters, collation and expiration
   options. BackOffice owns the shared-platform create operation; no API
   project references another API.
2. Add the two previously lazy-created blog indexes to the same manifest,
   using their existing names, BSON field names, key order and options. Remove
   repository-side creation only after BackOffice creation is in place and
   regression-tested.
3. Keep the existing authenticated BackOffice audit/apply/remove contracts
   compatible. The separate removal manifest stays manual and is never executed
   by startup. Startup never drops, renames, modifies, repairs, or replaces an
   existing index or directly changes documents; TTL expiry is a separate
   database-managed consequence.
4. In Mongo mode, BackOffice starts a non-blocking background initialization
   that runs the existing create/apply mechanism against **every entry of its
   canonical manifest**, not a duplicated key list. The current batch apply
   stops on its first error, so process entries independently and continue
   after an individual failure; do not report unattempted entries as checked.
   Audit actual database definitions afterward. A matching index is skipped; a
   missing collection/index is created. A mismatch, duplicate-key failure, permission
   error, provider limitation, timeout or database outage is logged with its
   index/collection and cause, without stopping the host. Mock mode does not
   contact MongoDB.
5. BackOffice and ServerAPI no longer register their previous subset index
   creators. BackOffice runs the single central initializer. No blocking index
   check or runtime BackOffice dependency is added to ServerAPI or ShortLinks.
   This intentionally removes the prior fail-fast gate for blog writes.
6. Preserve the existing authenticated audit/apply endpoints as the operator's
   way to inspect actual state and apply selected indexes after resolving an
   error. A successful process start or health response must not be presented
   as proof that all indexes exist.

### Startup, availability and recovery

- Schedule index work without awaiting it as a condition of starting the web
  host, including synchronous setup before its first asynchronous operation.
  Failure, slowness or cancellation of the index work must not stop
  BackOffice, ServerAPI or ShortLinks from starting or serving traffic. Do
  not attach index failure to a readiness check that removes traffic.
- Log the final initialization outcome at an actionable severity, including
  failed index key/collection where available; do not log documents or
  connection details. Catch failures at the background-work boundary, log
  explicitly, and keep the host alive; never silently treat failure as success.
  The existing authenticated audit endpoint remains available for diagnosis.
- Multiple BackOffice instances may start together. Keep the existing
  conflict-and-exact-recheck behavior; do not add a distributed lock unless
  actual provider behavior proves it necessary. Each attempt reports a real
  mismatch as a failure rather than claiming verification succeeded.
- Index application is sequential and can partially complete. Report individual
  results, audit again after a failure, and retry idempotently; never undo earlier
  indexes automatically. A timeout is an uncertain outcome until a fresh audit.
- The user manages failures: inspect logs and audit, resolve the underlying
  data/definition/permission issue, then use the existing approved apply
  operation or restart BackOffice to run the check again. No automatic retry
  loop or automatic cleanup is introduced.
- Until missing constraints exist, duplicate records or other incorrect writes
  may be accepted, and missing query indexes may hurt performance. This risk is
  explicitly accepted; later unique-index creation can fail if duplicates
  accumulated. An index initializer is not a guarantee of application-level
  correctness while it runs or after it reports an error.

### Operational safeguards

- Before the first rollout, inventory every desired index and its live definition,
  identify duplicate/malformed data for **each** missing unique index, assess
  collection sizes and provider index-build impact, verify a restorable backup,
  and resolve conflicts through approved operational procedures.
- Review every TTL definition and existing affected data before enabling this
  policy. Creating a TTL index may cause the database to delete expired records;
  source-defined TTL is not proof that existing data has been approved for
  automatic expiration. Explicitly approve this operational consequence.
- Deploy the BackOffice artifact containing the needed manifest and inspect its
  initialization result and audit against the intended database. This is an
  operational recommendation, **not** a startup, readiness or release gate.
  Independent service deployments and existing traffic remain available.
- Each future new or changed index remains source reviewed. Automatic startup
  creation is the approved policy;
  it does not authorize automatic data cleanup, index replacement or backfills.
  An incompatible index-definition change needs its own migration/rollout.

## Impacted Projects

- **Domain:** expose existing feature-owned and blog index definitions for
  the BackOffice manifest; preserve repository behavior.
- **BackOffice:** sole shared-platform automatic creator, existing operator API
  compatibility and non-blocking failure reporting.
- **ServerAPI:** no startup index creation or verification; remains available
  if BackOffice index initialization fails.
- **ShortLinks, ServiceDefaults and AppHost:** no intended changes or new
  dependencies; verify existing availability and health behavior.
- **BackOffice.Tests:** focused comparison, lifecycle, controller,
  compatibility and concurrency tests.
- **Deployment workflows:** no new index or readiness gate; verify the existing
  independent deployment behavior and document operational checks.
- **FreezerAnalysis:** excluded; do not change its application database or host.
- **Frontend, Windows clients and Shooting Range:** no intended behavior or
  contract changes. Operational availability can still affect API consumers.

## Impacted Files

### Modify

- [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs):
  add the blog index definitions while retaining existing manifest entries,
  exact comparison, operator contracts and safe concurrent-create handling.
- [MongoIndexStartupService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexStartupService.cs):
  run each manifest apply/audit attempt in a non-blocking BackOffice background
  service and log verified, failed and unverified outcomes.
- [BackOffice/Program.cs](../../MorWalPizVideo.BackOffice/Program.cs):
  register the single non-blocking full-manifest background initializer;
  retain mock mode.
- [ServerAPI/Program.cs](../../MorWalPizVideo.ServerAPI/Program.cs): remove
  the two blocking, index-creating hosted service registrations.
- [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs): remove lazy
  index creation while preserving the existing repository query and write paths.
- [BlogInteractionMongoIndexStartupService.cs](../../MorWalPizVideo.Domain/BlogInteractions/BlogInteractionMongoIndexStartupService.cs),
  [YouTubeContentMongoIndexStartupService.cs](../../MorWalPizVideo.Domain/YouTubeContent/YouTubeContentMongoIndexStartupService.cs)
  and [FeedbackMongoIndexStartupService.cs](../../MorWalPizVideo.BackOffice/Services/FeedbackMongoIndexStartupService.cs):
  their registrations were removed; existing feature-owned definitions remain
  manifest inputs.
- [MongoIndexOperationsTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs)
  and [BlogServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs):
  verify complete-manifest attempts, continuation after failure, non-blocking
  startup and blog index definitions.

### Generated/Migration

No automatic document migration, index drop or destructive script. The first
environment rollout requires an operator-owned index inventory, duplicate and
TTL review, backup, approved conflict resolution and recorded pre/post audits.
Database/index state is an operational migration even though no data backfill is
automatically performed.

### Documentation

- Added [ADR-029](../architecture/adr/ADR-029-nonblocking-mongo-index-startup.md)
  and marked ADR-008, ADR-022, and ADR-028's startup policies superseded.
- Updated [mongo-operations.md](../architecture/mongo-operations.md),
  [operator runbook](../architecture/operations/mongo-index-audits/README.md),
  backend/pages/admin architecture guidance, directly affected feature records,
  and this handoff with the implemented behavior and validation.

### Verify Only

- [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs):
  preserve all feature definitions and assert central inclusion.
- [MongoIndexesController.cs](../../MorWalPizVideo.BackOffice/Controllers/MongoIndexesController.cs):
  preserve API-key protection, explicit apply/remove approval and existing
  response behavior.
- [ServiceDefaults/Extensions.cs](../../MorWalPizVideo.ServiceDefaults/Extensions.cs):
  verify readiness and liveness remain independent of index status.
- [ShortLinks/Program.cs](../../MorWalPizVideo.ShortLinks/Program.cs),
  [AppHost/Program.cs](../../MorWalPizVideo.AppHost/Program.cs) and existing
  deployment workflows: verify no new startup or deployment dependency.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | BackOffice checks and creates shared-platform manifest indexes in the background; no host is gated on their state; existing operator endpoints remain compatible. |
| Frontend/UI | No source changes; operations can still fail if the database itself is unavailable. |
| Database/Data | Missing indexes and possibly collections are created. No document edits or index removals; TTL creation can cause later database-managed expiration. |
| Contracts/Models | Preserve manifest identities and definitions and existing public API responses; no shared API contract change. |
| Authentication/Authorization | No auth change; operator endpoints retain API-key authorization. BackOffice Mongo identity needs index/collection privileges; no new consumer privileges are required. |
| Validation | Reuse the existing manifest matcher for declared key patterns, uniqueness, collation, partial filters and TTL; report incompatible definitions and provider errors without gating startup. |
| Cache | Not applicable; no output-cache or offline-cache changes. |
| Configuration/Secrets | Reuse environment-managed Mongo settings; verify BackOffice targets the intended shared platform database without exposing credentials. No insecure defaults. |
| Logging/Telemetry | Structured startup outcome by manifest key and collection; distinguish verified, created, failed and unverified; exclude connection values and document contents. |
| Background Jobs | One non-blocking startup attempt; no scheduled job or automatic retry loop. |
| Deployment/Operations | Recommend preflight, backup, duplicate/TTL review and provider proof; no added startup, readiness or release gate. |
| Backward Compatibility | Preserve endpoint contracts and index names/options. Additive indexes remain on rollback; old/new manifest artifacts require tested overlap. |

## Reuse Opportunities

Reuse the current [Mongo index operations service](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs),
its approved manifest and exact matcher, the existing feature-owned index
definitions, Mongo-mode DI branches, existing health endpoints, and the
BackOffice test fixtures. Keep comparison in its existing owner rather than
introducing a new shared service or general migration framework.

## Risks

1. **High — missing integrity constraints:** failures or the initialization
   window allow writes before unique indexes exist, potentially introducing
   duplicates that also prevent later creation. The user accepts this risk.
   Log failures, audit and resolve duplicates manually; test writes can proceed
   and failures remain visible without claiming constraints are active.
2. **High — TTL data deletion:** a newly created TTL index can expire existing
   documents. Review TTL/data and retention effects before enabling the artifact
   in an environment; this operational review does not gate application startup.
3. **High — false confidence or wrong database:** a healthy host does not prove
   indexes exist, and a misconfigured BackOffice could manage a different
   database. Log the actual outcome and verify environment database identity
   operationally without exposing credentials; test audit failure reporting.
4. **Medium — partial or concurrent creation:** multiple instances and
   interrupted sequential writes can leave a mixed state. Re-audit, accept
   only exact matches, retry safely and test actual provider behavior.
5. **Medium — query or compatibility regression:** blog BSON serialization,
   older artifacts, missing backfills or changed query plans may differ.
   Preserve rendered definitions, test mixed versions, inspect representative
   explain plans and retain additive indexes on rollback.
6. **Medium — security/operational privilege:** startup now needs collection and
   index privileges. Give the BackOffice identity only necessary creation
   rights and retain existing protected operator endpoints.

## Technical Debt

- Previous split startup registrations and BlogRepository lazy index creation
  have been retired. The central manifest remains the source for both automatic
  startup attempts and explicit operator operations.
- The separate FreezerAnalysis application's index lifecycle is explicitly
  outside this delivery.
- Existing deployment workflows remain independent. Do not introduce
  cross-service ordering or a general CI/CD redesign.

## Implemented Work And Validation

1. Audited shared-platform index definitions and startup creation sites; the
   separate FreezerAnalysis application database remains excluded.
2. Added the two existing BlogRepository index definitions to the BackOffice
   manifest and removed repository-time index creation.
3. Added a BackOffice background initializer. It applies and then audits each
   manifest entry independently, logs failures, and continues to later entries.
4. Removed BackOffice and ServerAPI registrations for their previous subset
   startup creators. ServerAPI has no index startup gate.
5. Added ADR-029 and updated the applicable architecture, operator and feature
   documentation.
6. Focused validation passed:
   - `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
   - `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~MongoIndexOperationsTests|FullyQualifiedName~BlogServiceTests|FullyQualifiedName~HostSecurityContainmentTests"` (55 passed).

No disposable Mongo-compatible provider integration, live audit/apply,
deployment, or browser validation was run. No production database or
FreezerAnalysis database was changed. Duplicate/TTL review and provider
qualification remain recommended before environment rollout, but are not
application startup or readiness gates.

## Testing Strategy

- **Unit:** manifest parity and duplicate keys/names, exact key order and
  uniqueness/partial-filter/collation/TTL matching, serializer-rendered blog
  definitions, expected mismatch and no-removal behavior.
- **Focused lifecycle:** tests prove the background service returns while an
  index operation is pending and continues to later manifest entries after an
  individual failure. Actual deployed-host and Mongo-provider behavior was not
  tested. Audit/apply/remove authorization and response contracts remain
  unchanged.
- **Disposable real Mongo/provider:** absent collection, empty DB, existing
  matching indexes, conflicting name/pattern/options, duplicate data, permission
  denial, cancellation/interruption, parallel BackOffice starts and sequential
  partial completion followed by audit/retry. Validate TTL definition with
  disposable data only and verify provider behavior separately.
- **Deployment/compatibility:** old/new artifacts during additive rollout, wrong
  database, BackOffice unavailable, index missing, delayed initialization,
  unaffected readiness, rollback and operator recovery. Check query plans and
  relevant blog/Workshop/account behavior.
- **Not applicable:** frontend component, UI or offline behavior changes; no
  new schema migration or external service.

## Migration And Rollback

Recommend inventory, provider qualification, verified backup and duplicate and
TTL review before enabling the new BackOffice artifact in each environment.
Deploy independently and inspect initialization logs and actual audit results;
index discrepancies do not block service startup or deployment. Existing
writers can continue while initialization is in progress or after it fails.
No automatic cleanup, index replacement or backfill is authorized.

On code rollback, previous fail-fast startup creators and lazy blog creation may
return. Keep newly created indexes and collections; dropping indexes or
restoring TTL-deleted data requires separate reviewed recovery. A code rollback
cannot restore TTL-expired records. Before retrying interrupted initialization,
audit actual state because earlier index creations may have succeeded; resolve
failures, then use approved manual apply or restart BackOffice.

## Open Questions

Which production Mongo-compatible provider and shared-platform database are in
use? Repository source does not verify the deployed configuration; confirm
provider behavior and database identity during environment qualification.
