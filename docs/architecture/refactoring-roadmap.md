# Refactoring Roadmap

## Original Ten-Slice Backlog (2026-10-01)

Numbering follows the supplied Architect/Repository Expert handoff, not the separate TD identifiers. Current source wins over historical completion labels. This table records bounded authoring completion, not blanket backlog or release closure.

| Slice | Owning boundary | Authoring status | Release/remaining status |
|---|---|---|---|
| 1 | Docs, source/project inventories and instruction pointers | COMPLETE: maps, current/target/hold labels and delivery matrix reconciled | Deployed state UNKNOWN; historical samples are not production proof |
| 2 | ServerAPI containment; both API manifests/credential provisioning | Source IMPLEMENTED in existing worktree; focused evidence in security docs | Rotation, old artifacts and clean container execution BLOCKED |
| 3 | Project-local Range sessions/repositories and client restoration | Foundation IMPLEMENTED in existing worktree | Browser, Data Protection, target Mongo/data/index proof BLOCKED; does not implement slice 4 |
| 4 | Range anonymous-domain/onboarding cutover | NOT READY; deliberately UNTOUCHED | Product decision: administrator-created ordinary users versus protected registration; no invented onboarding or removed anonymous registration |
| 5 | Contracts/Domain Calendar service; API/client consumers | Bounded authoring COMPLETE; compatibility delegates retained | Remaining non-Calendar DataService consumers are separate slices; browser proof BLOCKED |
| 6 | Calendar-only revision/CAS across Mongo/mock and contracts | Bounded authoring COMPLETE; legacy revision compatibility documented | Real Mongo independent-writer evidence BLOCKED; no generic repository rewrite |
| 7 | Shared public/admin transport and active consumers | Bounded authoring COMPLETE; legacy shop exports retained | PARTIAL convergence: full admin typecheck, Ask SSR and public API-key lint fail; browser proof BLOCKED |
| 8 | Existing Windows hosts/main-window constructor DI | Bounded authoring COMPLETE; child facades retained | Local desktop suites PASS after test and snapshot repairs; existing-database upgrades beyond the compatibility fixture remain unproven |
| 9 | CI/deployment test gates and API container restore closure | COMPLETE authoring; actual runners collect nonempty suites; existing Docker fixes retained | Dedicated ShortLinks and ShootingRange backend test projects are now wired alongside the retained BackOffice project; local builds/tests are required, real Mongo and GitHub execution remain pending |
| 10 | Existing operational/recovery runbooks | COMPLETE authoring: evidence fields, expected results, owners, recovery and approvals | Operational closure BLOCKED; no Azure operations, rotation or deployment performed/approved |

Cache reliability is explicitly DEFERRED, not closed by the existing cache-tag/auth implementation. Shop is FROZEN. Source-complete slices do not close all TD items, Phase 4, Phase 5 or production release. Exact current delivery checks are in [deployment](deployment.md); operator records are in [activation/recovery](operations/phase5-activation-and-recovery.md).

## Phase 0: Containment

### Work

- Rotate and revoke exposed credentials.
- Remove secret-bearing source, migration seeds, and publish artifacts.
- Correct development flags and deployed CORS.
- Align Docker images with .NET 10.
- Fix CI so existing backend tests execute. (Completed 2026-08-03; backend build matrix includes ShortLinks.)

### Exit Criteria

- Secret scan passes with approved placeholders only.
- Old credentials no longer authenticate.
- Development enables only Dev and Swagger.
- Unsupported origins fail CORS tests.
- All supported projects build in CI and BackOffice.Tests runs.

## Phase 1: Explicit Boundaries

### Work

- Separate host-neutral controller behavior from authorization.
- Publish an endpoint authentication matrix.
- Remove API-key administration from the public frontend.
- Add authenticated BackOffice digital-artifact management. (Completed 2026-08-03)
- Deprecate duplicate BackOffice public shop controllers. (Completed 2026-08-03)

### Exit Criteria

- Public, admin, API-key, internal, and cart routes have executable authorization tests.
- No core administrative write is exposed by ServerAPI.
- No active consumer calls duplicate BackOffice shop routes.

## Phase 2: Contracts And Free Artifacts (On Hold)

The entire shop and digital-artifact phase is pre-production and unscheduled. Do not implement, migrate, test-expand, or deploy this phase until an explicit portfolio decision lifts the hold. The work and exit criteria remain design context for that future reassessment.

### Work

- Introduce `/api/v1` and version-aware OpenAPI.
- Add public/admin artifact DTOs.
- Add server-owned anonymous cart identity.
- Persist idempotent permanent-free acquisitions.
- Separate public previews from private originals.
- Issue short-lived download SAS URLs after verification.
- Align shared TypeScript contracts and Aruba API-base configuration.

### Exit Criteria

- Public responses contain no storage keys.
- Cross-cart download attempts fail.
- Acquired artifacts download successfully from private storage.
- Old unversioned consumers continue through documented aliases.

## Phase 3: Canonical Short Links (Completed 2026-08-03)

### Implementation Note

The canonical short-link implementation is now in place and validated for the current behavior slice. The core feature is complete; remaining work is operational follow-up such as duplicate audit, idempotent legacy backfill, rollout monitoring, retention/cleanup tuning, and archival field cleanup.

### Work

- Add canonical link and visit models.
- Implement destination safety validation.
- Backfill legacy embedded links into standalone records.
- Deploy canonical-only reads and management.
- Create the unique normalized-code index.
- Switch BackOffice management writes.
- Use atomic counters and optional retained visit events.

### Exit Criteria

- Every active code resolves from one canonical record.
- Duplicate codes are impossible.
- Redirect/query behavior and concurrent counts pass tests.
- Every legacy embedded YouTube link is either reconciled into a standalone record or recorded as an approved archival exception.

## Phase 4: Persistence And Service Decomposition

### Status Note (2026-08-03)

The 2026-08-03 completion claim applies to the documented feature slices, not all service decomposition. Remaining `DataService` consumers still require independent migration slices; repository-wide Phase 4 closure is not established by those results.

Calendar slices 5 and 6 now have scoped contract/service/revision authoring and focused verification. See [Calendar compatibility and revisions](calendar-compatibility.md) for boundaries, commands, and outstanding Mongo/browser release evidence. Cache reliability remains deferred and shop work remains frozen.

Historical/local exit-criteria evidence (not target-environment or repository-wide closure):

- Representative local query-plan samples: committed audit/apply outputs and explain examples are recorded under `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-sample-audit-output.json`, `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-sample-apply-output.json`, and `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-explain-evidence.md`. These are historical samples, not deployed index/explain proof.
- Focused services have bounded dependencies and tests: high-impact BackOffice controllers use focused services instead of `DataService` and are covered by `MorWalPizVideo.BackOffice.Tests/Features/FocusedServiceDependencyTests.cs`.
- Response backfill counts reconcile exactly: form response migration safety coverage is green in `MorWalPizVideo.BackOffice.Tests/Features/FormsMigrationSafetyTests.cs`.
- No unbounded public query materializes full collections: public shop endpoints enforce bounded parameters and repository pushdown, covered by `MorWalPizVideo.BackOffice.Tests/Features/ShopCatalogQueryPushdownTests.cs`.

### Work

- Apply approved Mongo indexes after audits.
- Push filtering, sorting, projection, and limits into Mongo queries.
- Extract content, catalog, shop, forms, insights, and links services from `DataService`.
- Move custom-form responses into a separate collection.

### Exit Criteria

- Representative query plans use intended indexes.
- Focused services have bounded dependencies and tests.
- Response backfill counts reconcile exactly.
- No unbounded public query materializes full collections.

## Phase 5: Clients And Operations

### Transport Slice 7 (2026-10-01)

The bounded public/admin transport-isolation slice is implemented: shared per-client URL/credentials/CSRF/channel/recovery state, public omit without admin auth context, cookie-only admin, and independent frozen-shop legacy transport. Active BackOffice/public video/Ask/Shooting ITA callers and exports are covered without UI, route, Calendar contract/revision or Range session changes. See [transport isolation](transport-isolation.md) for exact files, fresh test/build results, and failed full-consumer gates. This is not repository-wide Phase 5 or TD-019 closure: admin full typecheck, Ask SSR error-path tests, legacy public API-key lint, shop Sass build and browser release evidence remain distinct gates. Cache reliability and shop feature work remain deferred; desktop/CI work is outside this slice.

### Desktop Slice 8 (2026-10-01)

Main-window constructor DI is source-complete for InsightScanner and VideoImporter within their existing Generic Hosts. Both resolve/show the registered window without `StartupUri`; Importer preserves database/tenant/upload initialization order and tenant-refresh/shutdown handling. Existing static `App` dependency facades remain for untouched child workflows; there is no architecture, visual, or schema redesign.

The bounded Importer WPF fixture repair passes both host tests using the real application resources and main window on STA. Temporary SQLite checks cover data retention, tenant filters/switching, cancellation, pending refresh completion and host disposal, not all existing-database migration upgrades. Both Windows application builds pass. Full suites now pass: Importer 9/9 and InsightScanner 4/4. Source completion does not close TD-018 or the full desktop/Phase 5 gate. See [Windows applications](windows-apps.md) for exact commands, fixture boundaries and residual gates. CI expansion remains a separate slice.

### Work

- Standardize frontend calls through shared services (slice 7 transport isolation implemented; remaining route ownership/full-consumer gates are separate).
- Complete BackOffice cookie auth and CSRF protection.
- Adopt Generic Host/DI in WPF applications incrementally (slice 8 main windows source-complete; child facades and interactive validation remain).
- Add durable Hangfire configuration and dashboard protection.
- Add Blob health, metadata, lifecycle, and recovery controls.
- Retain Shooting ITA/WPF/AppHost builds and execute active frontend/shared transport/Windows suites (slice 9 authored); frontend typecheck/lint and clean-container/GitHub evidence remain release gates.
- Close the Shooting Range public-release gate without expanding its product scope: deny-by-default authorization, explicit production CORS, safe DTOs, current account-state enforcement, authoritative booking validation, MongoDB uniqueness/readiness, and focused tests.
- Keep the Shooting Range UI limited to authentication/session lifecycle, availability, booking, own bookings, and only the administrator operations required by the selected onboarding flow.

### Exit Criteria

- No unsupported direct client exists in maintained frontend paths.
- WPF network clients are factory-managed and testable.
- Jobs survive restart and expose usable telemetry.
- Storage recovery and credential rotation are tested.
- Shooting Range anonymous access is limited to login, CSRF token acquisition, and health probes; production deployment tests prove the authorization matrix and booking invariants.

## Phase 6: Deferred Capabilities

Only start when product need is confirmed:

- Shop customer accounts, acquisition claiming, download analytics, and transactional delivery remain on hold with the rest of the shop.
- Detailed short-link analytics.
- Shooting Range cancellation/rescheduling, richer messaging, notifications, waitlists, custom-duration sessions, reporting, and invite onboarding. Existing schedule/bay/closure administration and message routes are accepted current scope, not deferred removal targets. Further expansion requires observed POC need.

## Phase 7: Operational Verification And Convergence

### Purpose

Validate and converge the post-refactor platform once major structural work is done, with explicit closure of remaining Phase 4 evidence/testing/query-boundary gaps before final sign-off. This phase is operational, not feature-delivery, and can run even if Deferred Capabilities remain intentionally unstarted.

### Work

- Close and evidence all outstanding Phase 4 blockers, then mark Phase 4 completed.
- Publish a repeatable verification bundle per release candidate: query-plan evidence, index audit outcomes, focused-service dependency checks, and migration reconciliation results.
- Fix failing migration safety scenarios and confirm deterministic reconciliation for custom-form response separation.
- Eliminate any remaining unbounded public full-collection query paths and verify bounded-query behavior under load.
- Run production-like convergence checks across auth, cache eviction coherence, background jobs, and cross-service contracts after decomposition changes.
- Remove temporary compatibility reads/routes only after telemetry confirms non-usage for the defined stabilization window.
- Record a convergence sign-off that separates resolved refactor debt from still-deferred product capabilities.

### Exit Criteria

- Phase 4 is explicitly marked completed with objective evidence attached for each former blocker.
- Verification bundle is green for agreed critical flows across public, admin, and background-processing surfaces.
- No Sev1/Sev2 regressions are observed during the stabilization window after convergence release.
- Legacy compatibility paths targeted for retirement show zero required usage and are removed (or scheduled with a dated removal gate).
- Remaining open items are only Deferred Capabilities, not refactor correctness or operational safety gaps.

### Rollback Discipline Alignment

Apply one reversible behavior slice per deployment, keep additive compatibility until telemetry-backed validation passes, and never combine irreversible cleanup steps (contract removal, destructive data cleanup, credential rotation) in the same deployment unit.

## Rollback Discipline

Each phase ships one behavior slice at a time. Keep additive fields, old routes, old Blob locations, and legacy reads until executable validation and production telemetry confirm the new path. Never combine secret rotation, destructive data cleanup, and contract removal in one irreversible deployment.