# Technical Debt Backlog

## Current Iteration Goal

Keep `MorWalPizVideo.BackOffice`, `MorWalPizVideo.ServerAPI`, `frontend/back-office-spa`, and
`frontend/morwalpizvideo.client` operationally reliable while preparing the independently owned
Shooting Range POC for public exposure. Shop-related debt is unscheduled and on hold; see
"On Hold: Shop" below. Pre-release Shooting Range contracts may still change where required
to establish the approved authorization and data-integrity boundary.

## Current Iteration Order (Historical Order; Status Reconciled 2026-10-01)

| Order | ID | Status | Debt | Notes |
|---|---|---|---|---|
| 1 | TD-009 | Partially addressed | Incomplete active delivery verification | Existing backend/AppHost/Windows builds and backend/YouTube tests are retained; slice 9 adds active frontend/shared transport/Windows tests and clean containers. Failed Ask/desktop/typecheck/lint gates and missing container/GitHub execution prevent closure; no shop expansion. See delivery matrix. |
| 2 | TD-007 | Closed | Dev flags and deployed CORS were fail-open | BackOffice and ServerAPI now use a permissive dev-only policy in `IsDevelopment()` and always fail closed to `MorWalPizPolicy` otherwise; the open `AllowAllOrigins` fallback was removed. |
| 3 | TD-008 | Source addressed; execution pending | Docker runtime versions did not match .NET 10 | Both API Dockerfiles use `aspnet`/`sdk` 10.0 and copy Contracts, Domain, Models, MvcHelpers, ServiceDefaults and transitive YouTubeUtilities before restore. Clean-container CI added; local execution NOT RUN without daemon. |
| 4 | TD-017 | Closed | BackOffice browser tokens remained in local storage | Login no longer returns the raw JWT in the response body; `back-office-spa` no longer stores or sends a bearer token. `/api/auth/validate` now reads the `auth_token` HttpOnly cookie server-side instead of accepting a client-supplied token. |
| 5 | TD-001 | Partially addressed; release blocking | Credential artifacts and unverified rotation | Both API manifests/context exclude credential JSON; production YouTube credentials require external provisioning. Sentinel metadata tests do not prove historical artifact invalidation, revocation or rotation. CI now adds a source-safe private-key pattern guard and runs the credential-artifact exclusion tests; operator history review and rotation remain blocked. |
| 6 | TD-018 | Source addressed; execution pending | Backend test ownership was concentrated in one mixed project | ShortLinks host redirect/resolution/click tests and all five ShootingRange tests now have dedicated projects; BackOffice retains ShortLinks management/Reqnroll coverage. CI and deployment gates are updated, but GitHub execution and the transaction-capable Mongo gate remain pending. |

## Prioritization

Priority combines security, correctness, production impact, architectural leverage, and implementation dependency. Status reflects the current iteration; items marked Closed above are Closed here too.

| ID | Priority | Debt | Impact | Recommended action | Complexity | Status |
|---|---|---|---|---|---|---|
| TD-001 | Critical | Credential material and unconfirmed rotation | Credential compromise and unauthorized access | Revoke/rotate, review history/artifacts, enable scanning | Medium | PARTIAL: source exclusions/external provisioning implemented; historical artifact audit and rotation BLOCKED pending operator proof |
| TD-002 | Critical | Shop tokens are not persisted or validated; cart trusts caller IDs | Customer/cart impersonation | Replace caller identity with server-owned anonymous cart cookie; later add customer policy | High | On hold — pre-production shop |
| TD-003 | High | Shared controller base applies BackOffice authorization to public hosts | Public endpoint failures and unclear exposure | Split host-neutral base behavior from explicit authorization policies | Medium | Closed — verified in source (`ApplicationControllerBase` is host-neutral; regression test exists) |
| TD-004 | High | BackOffice duplicates shop controller behavior | Divergent contracts and ownership | Reassess authenticated management and compatibility surfaces when the hold is lifted | Medium | On hold — pre-production shop |
| TD-005 | High | Public product responses expose storage keys | Private artifact disclosure | Introduce public DTOs and private-original download contract | Medium | On hold — pre-production shop |
| TD-006 | High | Cache reliability beyond existing internal authentication/tags | Stale public data | Preserve current behavior; reassess reliability separately | Medium | DEFERRED explicitly: internal-service auth and lowercase tags are implemented, but do not prove end-to-end reliability closure |
| TD-007 | High | Development flags and deployed CORS are inconsistent | Insecure or nonfunctional environments | Enforce only Dev/Swagger locally and fail-closed explicit deployed origins | Low | Closed this iteration |
| TD-008 | High | Docker runtime/restore closure | Failed or misleading builds | Validate .NET 10 images and transitive manifests with clean builds | Low | Source addressed; clean-container execution NOT RUN locally, retained as CI gate |
| TD-009 | High | CI/delivery verification gaps | Regressions reach deployment | Execute active suites and checked builds without bypasses | Medium | PARTIAL: source gates now cover backend/AppHost/Windows/frontend/shared services/clean API containers. Ask and desktop tests fail; admin typecheck/public lint fail; GitHub/container execution remains pending. Shop remains excluded. See [delivery matrix](deployment.md#delivery-and-test-matrix). |
| TD-010 | High | Legacy embedded YouTube short-link fields remain in historical content documents | Archived fields can confuse migrations and reporting if treated as authoritative | Keep standalone records globally indexed, validate video references, and reconcile an idempotent backfill; do not add runtime embedded reads | High | Mitigated in the current slice — canonical standalone resolution and management are implemented; archival cleanup and production backfill evidence remain. |
| TD-011 | High | Mongo index governance lacked source-owned apply/audit flow | Unbounded latency and duplicate data | Audit, normalize, define and apply index manifest | Medium | Source operations addressed; manifest reconciliation and target execution pending — the executable allowlist in `MongoIndexOperationsService` is authoritative, while the checked-in JSON remains a phase-4 review snapshot and is not yet an apply authority. Sanitized samples do not prove target state; no target apply was performed. |
| TD-012 | High | Free checkout does not persist acquisition or produce download | Core shop workflow incomplete | Add permanent-free acquisition and private download delivery | High | On hold — pre-production shop |
| TD-013 | Medium | Broad `DataService` has excessive dependencies and responsibility | Coupling and difficult tests | Extract focused feature services incrementally | High | Partially addressed — focused services cover content, catalog, shop, forms, insights, links, and Calendar; three proven ServerAPI controllers no longer inherit unused `IGenericDataService`; other consumers remain separate migration slices. See [Calendar compatibility](calendar-compatibility.md). |
| TD-014 | Medium | APIs return persistence entities and inconsistent errors | Contract leakage and unsafe evolution | Adopt versioned DTOs and Problem Details feature by feature | High | Partially addressed — video import/update validation and social publishing errors use stable responses, and competitions now have additive `/api/v1/competitions` DTO/Problem Details routes while unversioned routes remain unchanged; broader migration remains open. |
| TD-015 | Medium | CustomForm embeds unbounded responses | Mongo document-size and contention risk | Move responses to separate collection with dual-write migration | High | Closed for migration-safety scope — `customFormResponses` collection, dual-write, backfill, and reconcile flows are implemented (`CustomFormResponseDocument`, `FormsService`, BackOffice backfill/reconcile endpoints), and focused migration safety tests are green in `MorWalPizVideo.BackOffice.Tests/Features/FormsMigrationSafetyTests.cs`. Embedded compatibility reads remain intentionally additive until telemetry-backed retirement. |
| TD-016 | Medium | Legacy embedded YouTube short-link fields remain in historical documents | Treating archived links as live data would bypass indexed lookup and atomic counting | Use standalone indexed lookup and atomic increment; reconcile legacy data operationally | Medium | Mitigated — runtime resolution and counting use standalone records only; legacy backfill and archival cleanup remain operational work. |
| TD-017 | Medium | BackOffice browser tokens remain in local storage | XSS token exposure | Complete HttpOnly cookie and CSRF design | Medium | Closed this iteration |
| TD-018 | Medium | WPF applications use static service location/direct HttpClient | Testability and connection-management issues | Adopt Generic Host, DI, typed clients incrementally | Medium | Partially addressed — slice 8 main-window constructor DI is source-complete in both existing hosts; static child-window facades remain compatible. Windows builds and Importer host/tenant/cancellation/shutdown checks pass. Full desktop gate FAILED: Importer migration model drift and unrelated InsightScanner fake DTO equality remain unsuppressed. Existing-database upgrades are not proven. See [Windows applications](windows-apps.md). |
| TD-019 | Medium | Frontends contain direct Fetch/Axios and route ownership leaks | Inconsistent auth/config and duplicate APIs | Consolidate in shared services; remove public management routes | Medium | Partially addressed: slice 7 public/admin per-client transport isolation and active caller migration are implemented; frozen shop retains legacy transport. Focused source/test/build evidence and failed full-consumer gates are recorded in [transport isolation](transport-isolation.md). Public management-route ownership and broader consumer/release convergence remain open; no route removal, shop feature work or cache refactor is authorized. |
| TD-020 | Medium | Blob abstraction loses metadata and swallows failures | Incorrect media responses and weak diagnostics | Return typed blob metadata/result and classify failures | Medium | Partially addressed — `BlobDownloadResult`, deterministic failure outcomes, and folder-prefix normalization now cover the touched paths; broader production/provider hardening remains open. |
| TD-021 | Medium | Private content authorization is coarse and inconsistent | Asset authorization bypass | Add visibility policies and enforce across metadata/images/downloads | High | Open |
| TD-022 | Medium | API-key throttling is process-local | Limits bypassed when scaled | Use distributed rate limiting or gateway enforcement | Medium | Open |
| TD-023 | Medium | Hangfire production durability/dashboard protection need verification | Lost/duplicated work and admin exposure | Durable storage, protected dashboard, idempotent jobs | Medium | Open |
| TD-024 | Low | Shared namespaces reference Server/BackOffice ownership | Boundary confusion | Rename after behavioral boundaries stabilize | Medium | Open |
| TD-025 | Low | Legacy domains and obsolete frontend modules remain | SEO and maintenance ambiguity | Correct canonical metadata; decide retain/remove per module | Low | Open |
| TD-027 | Medium | Two Web Push surfaces coexist: the legacy user-bound `api/push/*` with subscriptions embedded in `User.PushSubscriptions`, and the anonymous per-channel surface at `api/push/subscriptions/*` | Duplicate sending paths and an unqueryable embedded array | Decide whether the legacy surface still has a consumer; retire it or migrate it onto `pushSubscriptions` | Medium | Open — intentionally out of scope of [ADR-017](adr/ADR-017-anonymous-web-push.md), which preserved it for compatibility |
| TD-028 | Low | `MorWalPizVideo.ServerAPI/Services/WebPushService.cs` still constructs `new WebPushClient()` instead of using `IHttpClientFactory` | Socket exhaustion and no DNS refresh on the legacy path | Move it onto a named client as `WebPushSender` already does in BackOffice | Low | Open — pre-existing; the `HttpClientLifetimeAuditTests` source scan only covers BackOffice |
| TD-026 | High | Shooting Range public-release boundary is incomplete | Anonymous domain access, credentialed cross-origin abuse, sensitive response leakage, stale account claims, and invalid bookings | Add fallback authorization, narrow anonymous exceptions, explicit CORS, safe DTOs, account-state enforcement, MongoDB uniqueness/readiness, authoritative booking validation, and focused tests | Medium | Open — blocks public exposure; feature expansion remains deferred |

## Delivery Addendum (2026-10-01)

Slice 9 adds active frontend/shared-transport and Windows test execution, clean API container gates and checked frontend delivery builds. TD-009 remains PARTIAL, not closed: fresh Ask and desktop suites fail, admin full typecheck/public legacy API-key lint remain failed, and GitHub/container execution is pending. See [delivery matrix](deployment.md#delivery-and-test-matrix). TD-011's 2026-08-03 artifacts are representative local samples, not target-environment index/explain proof; operational sign-off remains BLOCKED. TD-026 now has implemented session/security and schedule foundations, but onboarding/domain authorization cutover is NOT READY and live evidence is BLOCKED. The [ten-slice table](refactoring-roadmap.md#original-ten-slice-backlog-2026-10-01) is the bounded authoring status, not a claim that this debt backlog is all complete.

## On Hold: Shop

TD-002, TD-004, TD-005, and TD-012 are shop-specific and do not block BackOffice, ServerAPI,
back-office-spa, morwalpizvideo.client, or Shooting Range. They are intentionally unscheduled
and must remain untouched until an explicit portfolio decision lifts the pre-production hold.
When resumed, they should be scoped together because identity, ownership, acquisition, and
delivery form one trust boundary.

## Shooting Range POC Gate

TD-026 is the only active Shooting Range expansion boundary. Close it before public exposure,
but do not treat closure as approval for richer product features. The first administrator is
inserted manually into MongoDB. Ordinary-user onboarding remains unresolved; administrator-created
users are the working assumption. See [ADR-016](adr/ADR-016-shooting-range-public-poc.md).

## Backlog Rules

- Security and data-integrity items block feature expansion in their area.
- A debt item closes only after executable validation and documentation update.
- Do not close an item merely because a plan or partial abstraction exists.
- Record pre-existing unrelated failures separately rather than hiding them.