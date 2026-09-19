# Technical Debt Backlog

## Current Iteration Goal

Keep `MorWalPizVideo.BackOffice`, `MorWalPizVideo.ServerAPI`, `frontend/back-office-spa`, and
`frontend/morwalpizvideo.client` operationally reliable while preparing the independently owned
Shooting Range POC for public exposure. Shop-related debt is unscheduled and on hold; see
"On Hold: Shop" below. Pre-release Shooting Range contracts may still change where required
to establish the approved authorization and data-integrity boundary.

## Current Iteration Order (Resolved: 2026-08-02)

| Order | ID | Status | Debt | Notes |
|---|---|---|---|---|
| 1 | TD-009 | Partially addressed | CI skipped active backend verification paths | Central CI builds AppHost and runs both `MorWalPizVideo.BackOffice.Tests` and `MorWalPizVideo.YouTubeUtilities.Tests`; BackOffice and ServerAPI deployment workflows include YouTubeUtilities path filters and require both test projects to pass before publish. Frontend tests are explicitly out of scope for this item, and shop coverage remains deferred while the shop is on hold. Closure awaits a green BackOffice baseline; the focused `CatalogAuthorizationTests` class now passes all 16 theory cases, while broader baseline status requires separate validation. |
| 2 | TD-007 | Closed | Dev flags and deployed CORS were fail-open | BackOffice and ServerAPI now use a permissive dev-only policy in `IsDevelopment()` and always fail closed to `MorWalPizPolicy` otherwise; the open `AllowAllOrigins` fallback was removed. |
| 3 | TD-008 | Closed | Docker runtime versions did not match .NET 10 | Both API Dockerfiles now use `aspnet`/`sdk` 10.0 images and copy every referenced project (`Contracts`, `Domain`, `Models`, `MvcHelpers`, `ServiceDefaults`) before restore. |
| 4 | TD-017 | Closed | BackOffice browser tokens remained in local storage | Login no longer returns the raw JWT in the response body; `back-office-spa` no longer stores or sends a bearer token. `/api/auth/validate` now reads the `auth_token` HttpOnly cookie server-side instead of accepting a client-supplied token. |
| 5 | TD-001 | Open (deferred, not urgent) | Tracked credential material still copied into build/publish output | Confirmed intentional offline-workflow use, excluded via `.gitignore`. Real residual risk: both API `.csproj` files still declare `credentials.json` as publishable `Content`, so it can leak into deploy artifacts if a real file is ever present at publish time. Revisit before any public/production deployment. |

## Prioritization

Priority combines security, correctness, production impact, architectural leverage, and implementation dependency. Status reflects the current iteration; items marked Closed above are Closed here too.

| ID | Priority | Debt | Impact | Recommended action | Complexity | Status |
|---|---|---|---|---|---|---|
| TD-001 | Critical | Tracked credential material and unconfirmed rotation | Credential compromise and unauthorized access | Revoke/rotate, remove current files/seeds, review history/artifacts, enable scanning | Medium | Open — deferred behind current iteration (offline-only credentials; still copied to publish output) |
| TD-002 | Critical | Shop tokens are not persisted or validated; cart trusts caller IDs | Customer/cart impersonation | Replace caller identity with server-owned anonymous cart cookie; later add customer policy | High | On hold — pre-production shop |
| TD-003 | High | Shared controller base applies BackOffice authorization to public hosts | Public endpoint failures and unclear exposure | Split host-neutral base behavior from explicit authorization policies | Medium | Closed — verified in source (`ApplicationControllerBase` is host-neutral; regression test exists) |
| TD-004 | High | BackOffice duplicates shop controller behavior | Divergent contracts and ownership | Reassess authenticated management and compatibility surfaces when the hold is lifted | Medium | On hold — pre-production shop |
| TD-005 | High | Public product responses expose storage keys | Private artifact disclosure | Introduce public DTOs and private-original download contract | Medium | On hold — pre-production shop |
| TD-006 | High | Cache eviction lacks a reliable authenticated service contract | Stale public data | Replace maintenance GETs with authenticated internal commands and telemetry | Medium | Closed — `CacheController` requires internal-service auth, and producer/purge tags now use the same lowercase `CacheKeys` values for matches and QuickLinks. |
| TD-007 | High | Development flags and deployed CORS are inconsistent | Insecure or nonfunctional environments | Enforce only Dev/Swagger locally and fail-closed explicit deployed origins | Low | Closed this iteration |
| TD-008 | High | Docker runtime versions do not match .NET 10 | Failed or misleading builds | Align SDK/runtime images and referenced-project restore inputs | Low | Closed this iteration |
| TD-009 | High | CI coverage omitted accepted active backend verification paths | Regressions reach deployment | Add AppHost build verification, YouTubeUtilities tests, and deployment test gates | Medium | Partially addressed — AppHost build verification, YouTubeUtilities tests, preserved BackOffice coverage, and API deployment gates are implemented. The focused `CatalogAuthorizationTests` class now passes all 16 theory cases; broader BackOffice baseline status requires separate validation. Frontend test coverage is handled separately and is not part of this item; shop coverage remains intentionally deferred. |
| TD-010 | High | Legacy embedded YouTube short-link fields remain in historical content documents | Archived fields can confuse migrations and reporting if treated as authoritative | Keep standalone records globally indexed, validate video references, and reconcile an idempotent backfill; do not add runtime embedded reads | High | Mitigated in the current slice — canonical standalone resolution and management are implemented; archival cleanup and production backfill evidence remain. |
| TD-011 | High | Mongo index governance lacked source-owned apply/audit flow | Unbounded latency and duplicate data | Audit, normalize, define and apply index manifest | Medium | Closed — source-owned manifest and authenticated audit/apply operations are in place (`MongoIndexOperationsService`, `MongoIndexesController`, `docs/architecture/operations/mongo-index-manifest.phase4.json`) with committed operational evidence under `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-sample-audit-output.json`, `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-sample-apply-output.json`, and `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-explain-evidence.md`. |
| TD-012 | High | Free checkout does not persist acquisition or produce download | Core shop workflow incomplete | Add permanent-free acquisition and private download delivery | High | On hold — pre-production shop |
| TD-013 | Medium | Broad `DataService` has excessive dependencies and responsibility | Coupling and difficult tests | Extract focused feature services incrementally | High | Partially addressed — focused services cover content, catalog, shop, forms, insights, and links; `ExternalDataService` now consumes `IGenericDataService`, while remaining `DataService` consumers continue as separate migration slices. |
| TD-014 | Medium | APIs return persistence entities and inconsistent errors | Contract leakage and unsafe evolution | Adopt versioned DTOs and Problem Details feature by feature | High | Partially addressed — video import/update validation and social publishing errors now use stable responses; persistence-entity exposure and broader Problem Details migration remain open. |
| TD-015 | Medium | CustomForm embeds unbounded responses | Mongo document-size and contention risk | Move responses to separate collection with dual-write migration | High | Closed for migration-safety scope — `customFormResponses` collection, dual-write, backfill, and reconcile flows are implemented (`CustomFormResponseDocument`, `FormsService`, BackOffice backfill/reconcile endpoints), and focused migration safety tests are green in `MorWalPizVideo.BackOffice.Tests/Features/FormsMigrationSafetyTests.cs`. Embedded compatibility reads remain intentionally additive until telemetry-backed retirement. |
| TD-016 | Medium | Legacy embedded YouTube short-link fields remain in historical documents | Treating archived links as live data would bypass indexed lookup and atomic counting | Use standalone indexed lookup and atomic increment; reconcile legacy data operationally | Medium | Mitigated — runtime resolution and counting use standalone records only; legacy backfill and archival cleanup remain operational work. |
| TD-017 | Medium | BackOffice browser tokens remain in local storage | XSS token exposure | Complete HttpOnly cookie and CSRF design | Medium | Closed this iteration |
| TD-018 | Medium | WPF applications use static service location/direct HttpClient | Testability and connection-management issues | Adopt Generic Host, DI, typed clients incrementally | Medium | Open |
| TD-019 | Medium | Frontends contain direct Fetch/Axios and route ownership leaks | Inconsistent auth/config and duplicate APIs | Consolidate in shared services; remove public management routes | Medium | Open |
| TD-020 | Medium | Blob abstraction loses metadata and swallows failures | Incorrect media responses and weak diagnostics | Return typed blob metadata/result and classify failures | Medium | Partially addressed — `BlobDownloadResult`, deterministic failure outcomes, and folder-prefix normalization now cover the touched paths; broader production/provider hardening remains open. |
| TD-021 | Medium | Private content authorization is coarse and inconsistent | Asset authorization bypass | Add visibility policies and enforce across metadata/images/downloads | High | Open |
| TD-022 | Medium | API-key throttling is process-local | Limits bypassed when scaled | Use distributed rate limiting or gateway enforcement | Medium | Open |
| TD-023 | Medium | Hangfire production durability/dashboard protection need verification | Lost/duplicated work and admin exposure | Durable storage, protected dashboard, idempotent jobs | Medium | Open |
| TD-024 | Low | Shared namespaces reference Server/BackOffice ownership | Boundary confusion | Rename after behavioral boundaries stabilize | Medium | Open |
| TD-025 | Low | Legacy domains and obsolete frontend modules remain | SEO and maintenance ambiguity | Correct canonical metadata; decide retain/remove per module | Low | Open |
| TD-026 | High | Shooting Range public-release boundary is incomplete | Anonymous domain access, credentialed cross-origin abuse, sensitive response leakage, stale account claims, and invalid bookings | Add fallback authorization, narrow anonymous exceptions, explicit CORS, safe DTOs, account-state enforcement, MongoDB uniqueness/readiness, authoritative booking validation, and focused tests | Medium | Open — blocks public exposure; feature expansion remains deferred |

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