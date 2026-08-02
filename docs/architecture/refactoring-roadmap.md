# Refactoring Roadmap

## Phase 0: Containment

### Work

- Rotate and revoke exposed credentials.
- Remove secret-bearing source, migration seeds, and publish artifacts.
- Correct development flags and deployed CORS.
- Align Docker images with .NET 10.
- Fix CI so existing backend tests execute.

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
- Add authenticated BackOffice digital-artifact management.
- Deprecate duplicate BackOffice public shop controllers.

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

## Phase 3: Canonical Short Links

### Work

- Add canonical link and visit models.
- Implement destination safety validation.
- Backfill standalone and embedded links.
- Deploy canonical-first dual reads.
- Create the unique normalized-code index.
- Switch BackOffice management writes.
- Use atomic counters and optional retained visit events.

### Exit Criteria

- Every active code resolves from one canonical record.
- Duplicate codes are impossible.
- Redirect/query behavior and concurrent counts pass tests.
- Legacy embedded reads show zero use before removal.

## Phase 4: Persistence And Service Decomposition

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

## Rollback Discipline

Each phase ships one behavior slice at a time. Keep additive fields, old routes, old Blob locations, and legacy reads until executable validation and production telemetry confirm the new path. Never combine secret rotation, destructive data cleanup, and contract removal in one irreversible deployment.