# Refactoring Roadmap

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

## Phase 2: Contracts And Free Artifacts

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

Phase 4 is complete (Completed 2026-08-03).

Exit-criteria evidence:

- Representative query plans use intended indexes: committed audit/apply outputs and explain evidence are recorded under `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-sample-audit-output.json`, `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-sample-apply-output.json`, and `docs/architecture/operations/mongo-index-audits/phase4-2026-08-03-explain-evidence.md`.
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

### Work

- Standardize frontend calls through shared services.
- Complete BackOffice cookie auth and CSRF protection.
- Adopt Generic Host/DI in WPF applications incrementally.
- Add durable Hangfire configuration and dashboard protection.
- Add Blob health, metadata, lifecycle, and recovery controls.
- Include Shooting ITA and WPF builds in CI.

### Exit Criteria

- No unsupported direct client exists in maintained frontend paths.
- WPF network clients are factory-managed and testable.
- Jobs survive restart and expose usable telemetry.
- Storage recovery and credential rotation are tested.

## Phase 6: Deferred Capabilities

Only start when product need is confirmed:

- Verified customer accounts and anonymous-acquisition claiming.
- Customer and download analytics with approved retention.
- Provider-neutral transactional email.
- Detailed short-link analytics.

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