# Shooting Range: session focus and bay editor fixes

Status: implemented and locally validated; production migration/deployment
remain operator actions.

## Outcome

Keep the current view and unsaved work when the browser regains focus, fix the
bay form's bottom controls, make Pepper a nonnegative integer count like
plates, and let administrators create mechanism types for a fixed bay-editor
selection. Add realistic dummy bays, users, reservations and closing dates to
show the existing features. The user confirms there are no existing records to
preserve. Mechanism-type creation is in scope; rename and deletion are not.
The user clarified that sample data is the normal data shown whenever the
existing in-memory database is active; it is not a separate demo mode.

## Current Evidence

- `frontend/shooting-range.client/src/App.tsx`, `SessionGate`: window focus
  increments `revision`; the revision effect sets `restoring`, which replaces
  the protected subtree with a skeleton and unmounts it. This is a credible
  direct explanation for the reported full-view refresh and lost local state;
  it has not been reproduced in a browser during this analysis.
- `frontend/shooting-range.client/src/Admin.tsx`, `Admin` and `BayForm`: bay
  and configuration drafts are component state; mechanism type is free text,
  Pepper is a checkbox, and the mechanism and submit buttons lack the
  established `.button` classes. The mechanism row uses `mechanism-row`, for
  which `frontend/shooting-range.client/src/styles.css` defines no rule.
  The attached screenshot shows the resulting control layout issue.
- `frontend/shooting-range.client/src/api.ts` and `src/routeData.ts`: bay
  Pepper is a boolean and save action explicitly converts it to a boolean.
  Availability also uses the same bay type; `App.tsx` displays the existing
  mechanism quantities and names.
- `MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs`:
  `ShootingRangeBay.Pepper` is a `bool`; `Mechanism` contains a free-text
  `Type` and quantity. `Repositories/ShootingRangeDbContext.cs` maps mechanism
  lists to JSON; the initial EF migration stores Pepper as a SQL `bit`.
- `Repositories/IShootingRangeRepositories.cs` validates bay counts and
  mechanism strings; `Controllers/ShootingRangeController.cs` exposes
  authenticated admin bay reads and CSRF-protected admin writes.
  `Services/ShootingRangeService.cs` rechecks admin status within the shared
  mutation guard. Mock and SQL repository sets implement that guard.
- `docs/shooting-range-architecture.md` specifies independently owned Range
  contracts, explicit Azure SQL EF migrations and guarded writes. Startup
  refuses pending migrations. The user reports no existing records; deployed
  schema and independent client state have not been verified here.
- `MorWalPizVideo.ShootingRange/Program.cs` seeds only a local in-memory admin
  when its configured password exists, and forces a password change. In-memory
  and mock modes are restricted to Development/Test; no sample bays, bookings
  or closures are seeded. `ShootingRangeService` derives default sessions in
  the field time zone and checks booking admission and capacity. The current
  Development configuration contains a literal local admin password; do not
  reproduce it or use it as a pattern for new demo credentials.

## Proposed Design

1. Keep initial session restoration blocking as today. On focus, revalidate an
   existing session in the background without setting the initial restoration
   state or unmounting protected content. A confirmed 401 still performs the
   existing idempotent expiry transition; a successful response updates
   account/role/forced-change state. On a transient error retain the view and
   draft, surface a retryable session-check error, and continue to rely on
   server authorization for all operations. Prevent stale or aborted checks
   from restoring a logged-out/replaced session, and coalesce overlapping focus
   checks. Preserve normal route/action refresh and initial login behavior.
2. Replace Pepper with a required nonnegative integer end-to-end. Use `pepper`
   as the property name and change its JSON type from boolean to number in the
   coordinated release. Enforce integer and nonnegative validation on the
   server, not just in HTML. Default new bays to zero. Do not silently coerce
   malformed or missing client input to zero.
3. Add a Range-local persisted mechanism-type object with stable ID and
   immutable display name. Admins can create and list types, not rename/delete.
   Keep the existing bay mechanism `type` string as the immutable canonical
   name rather than changing the stored JSON shape or availability rendering.
   Server-side guarded bay writes require every type to match an existing
   canonical name and each quantity to be positive; reject duplicates within a
   bay. Creation trims names, rejects empty/oversized or case-insensitive
   duplicates, and reports clear errors. Resolve comparisons consistently in
   mock and SQL, including under concurrent creation.
4. Add an explicit EF migration for the SQL schema. It validates existing
   mechanism JSON, names, quantities and within-bay duplicates; it backfills
   distinct valid canonical mechanism names and converts Pepper `bit` to
   `int`, preserving legacy 0/1 values. It stops on malformed/ambiguous data.
   The user reports no existing records. No Pepper count is inferred beyond
   the exact boolean conversion.
5. Add an admin section for creating/listing types and a labelled native select
   for each bay mechanism. Show a clear empty state and disable adding a
   mechanism until a type exists. Use established button classes and a
   responsive mechanism-row layout with explicit spacing. Preserve the
   incumbent Italian copy and appearance tokens.
6. Automatically seed sample data when the existing EF in-memory provider is
   active in Development or Test, without a separate opt-in. The existing
   provider restrictions continue to forbid in-memory storage in Production
   and Staging. Never seed SQL or the separate mock provider. Keep sample data
   separate from production migrations and from the current local admin
   bootstrap. Create a small coherent dataset: at least
   three mechanism types; four bays including available, maintenance and
   whitelist-reserved examples, with varied plate/Pepper/partition counts; at
   least three clearly fictional users including approved and pending; pending
   and approved reservations on separate available bay/session combinations;
   and both a field-wide and a bay-specific closing date. Leave a future
   session and bay available for a live booking. Use the existing configured
   admin for administration and do not require a new credential for seeding.
   Sample users have unique generated, unexposed password material hashed via
   the existing password helper and are not intended for login; never log or
   commit it. Admin login continues to use the existing configured bootstrap
   credential and forced-change flow. To demonstrate personal booking history,
   the signed-in admin can create a booking during the demonstration.
7. Derive dates from the injected clock in the configured field time zone:
   select future valid open days and actual generated sessions, with the
   reserved example more than two local days away. Avoid conflicting bookings,
   closures or invalid/ambiguous DST times. Use existing guarded repository
   writes and admission rules, and seed only a fresh in-memory dataset; never
   overwrite existing accounts or application records. Make repeated setup
   idempotent and explicit about partial-failure recovery. Document that data
   resets on process restart. Tests use isolated in-memory stores and adapt
   fixtures/assertions to account for the automatic sample records; do not
   disable the behavior under Test or leak data between test factories.

This is an **Operate** surface refinement, not a redesign. No `PRODUCT.md` or
`DESIGN.md` was found for this client; its existing CSS, shared layout
components and the supplied screenshot are the incumbent visual context.
The installed Impeccable `layout` workflow was attempted, but the launcher
reported the subcommand as unknown, so no workflow ran. Source/screenshot
assessment and the layout detector were used instead. The detector reported
no findings. A browser screenshot could not be captured because Chromium is
not installed.

## Impacted Projects

- `MorWalPizVideo.ShootingRange`: Range-local model, guarded persistence,
  validation, API, explicit SQL schema migration and in-memory sample data.
- `frontend/shooting-range.client`: session gate, admin flow, local API types,
  form styling and focused tests.
- `MorWalPizVideo.ShootingRange.Tests`: API/security, repository and real SQL
  migration/integrity coverage.
- Azure SQL and independently deployed Range API/client: coordinated cutover
  required. AppHost, other APIs, shared frontend packages, MongoDB and Windows
  applications have no intended changes.

## Impacted Files

### Modify

- `frontend/shooting-range.client/src/App.tsx`: background focus revalidation
  without subtree replacement; preserve booking display.
- `frontend/shooting-range.client/src/Admin.tsx`: Pepper counter, type creation
  section and selection, draft handling and scoped button classes.
- `frontend/shooting-range.client/src/styles.css`: responsive mechanism rows
  and appropriately separated actions.
- `frontend/shooting-range.client/src/api.ts` and `src/routeData.ts`: local
  number/type contracts, admin type operations and strict payload handling.
- `frontend/shooting-range.client/src/App.test.tsx`, `src/Admin.test.tsx`,
  `src/routeData.test.ts` and `src/api.test.ts`: focused regression tests in
  the existing owner files as applicable.
- `MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs`: numeric Pepper
  and Range-owned mechanism-type entity.
- `MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs`: add
  narrowly scoped type-creation request/response contracts as appropriate.
- `MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs`,
  `MockShootingRangeRepositories.cs`, `SqlServerShootingRangeRepositories.cs`
  and `ShootingRangeDbContext.cs`: catalogue repository, guarded validation,
  rollback capture, EF mapping and uniqueness.
- `MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs` and
  `Services/ShootingRangeService.cs`: admin-only catalogue read/create with
  CSRF and authoritative validation within the guard.
- `MorWalPizVideo.ShootingRange/Program.cs`: initialize sample data only for
  the existing in-memory provider and keep the existing admin bootstrap intact.
- `MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs`:
  catalogue, seed and API coverage.

### Add

- A focused Range-local in-memory sample seeder, if needed to keep the startup
  composition root readable; use the existing `Services` ownership pattern.

### Generated/Migration

- Added an EF migration and designer under
  `MorWalPizVideo.ShootingRange/Persistence/Migrations/`; updated
  `ShootingRangeDbContextModelSnapshot.cs`. The migration backfills valid
  existing names, fails closed on malformed/ambiguous mechanism data and does
  not modify the initial migration.

### Documentation

- Update `docs/shooting-range-architecture.md` with catalogue ownership,
  migration semantics, automatic in-memory samples, cutover and recovery after implementation.
- Update this handoff with actual changes, checks and residual risks.

### Verify Only

- `MorWalPizVideo.ShootingRange/Program.cs`: also verify existing
  exception-to-status mapping, dependency registration and readiness.
- `MorWalPizVideo.ShootingRange/Security/ShootingRangeSessions.cs`: session
  revocation and role enforcement.
- Range API/client deployment workflows, `frontend/shooting-range.client`
  build and SSR entry points, and the initial SQL migration: verify ordering,
  SSR and existing storage assumptions.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Admin-only list/create catalogue; guarded bay validation and numeric Pepper. |
| Frontend/UI | Background session check, preserved drafts, corrected controls, catalogue form and fixed selection. |
| Database/Data | SQL bit-to-int conversion and valid legacy mechanism-name backfill; automatic in-memory sample dataset. |
| Contracts/Models | Range-local Pepper JSON type changes; mechanism names remain compatible strings in bay JSON. |
| Authentication/Authorization | Preserve session revalidation, role checks and forced password changes; catalogue reads/admin mutations restricted to admin, mutations require CSRF. |
| Validation | Reject invalid counts, unknown or duplicate bay types, duplicate/blank names and migration ambiguities; surface error responses. |
| Cache | No Range cache identified for these paths; no cache invalidation planned. |
| Configuration/Secrets | No new opt-in or configured demo-user credential. Sample-user password material is generated and never disclosed; existing admin bootstrap and migration connection policies remain. |
| Logging/Telemetry | Surface actionable migration, validation and background-check errors without logging sensitive session data. |
| Background Jobs | None; migration is explicitly operator-run. |
| Deployment/Operations | Coordinated API/client and schema cutover; startup refuses unapplied migrations. Sample data is automatic in the existing Development/Test in-memory provider only, never SQL or mock. |
| Backward Compatibility | Pepper boolean-to-number is breaking for old clients and API versions; legacy mechanism JSON remains readable. No mixed-version writers. |

## Reuse Opportunities

Reuse `SessionGate`, the existing session-expired event and cookie/CSRF client;
the Range `AdminMutationAsync` and repository mutation guard; existing local
repository and EF patterns; `TabbedWorkspace`, `.button` classes and theme
tokens; existing API host and disposable SQL test fixtures. Do not introduce
shared domain types, an additional global state library or speculative
catalogue editing features.

## Risks

1. **Medium — historical Pepper semantics:** legacy `true` converts to 1 and
   false to 0; this is the only faithful boolean conversion. The user reports
   no existing records. The migration does not invent larger counts.
2. **High — mixed-version writes:** old boolean clients/API and new numeric
   ones are incompatible. Suspend writes and avoid mixed API instances; verify
   both versions and schema before resuming.
3. **High — demo data or credentials in a real environment:** restrict sample
   seeding to the already environment-gated in-memory provider, forbid embedded
   credentials and verify SQL/production exclusion.
4. **Medium — stale session races:** a focus response could resurrect a logged
   out or demoted account. Cancel or sequence checks, verify expiry, role,
   forced-change, logout and network-error behavior.
5. **Medium — concurrent catalogue creation:** duplicate names could bypass a
   client-only check. Validate inside the global write guard and enforce
   consistent SQL uniqueness; test concurrent independent SQL writers.
6. **Low — layout/accessibility regression:** verify keyboard focus, labels,
   wrapping, light/dark themes and narrow/desktop captures when browser
   tooling is available.
7. **Medium — misleading demo:** hard-coded past dates or conflicting
   reservations make capabilities appear broken. Generate future valid
   sessions from the field clock and test actual availability and booking.
8. **Medium — existing tests assume an empty store:** automatic in-memory
   samples change fixture contents. Isolate factories and update only affected
   test setup/assertions while keeping sample behavior enabled.

## Technical Debt

- Range currently returns persistence bay entities for both admin and
  availability; safe projection is an existing public-release concern.
  This task must not expand it or alter anonymous availability policy.
- Browser verification and deployed Azure SQL backup/restore evidence are
  existing release gates, not claims of this analysis.
- The checked-in Development configuration contains a local admin password
  literal despite the architecture document's stated external-configuration
  policy. Do not copy it or introduce another; credential cleanup is separate
  unless needed to safely enable the demo.

## Delivered Work

1. Session focus now revalidates in the background without unmounting protected
   content; successful state updates, 401 expiry and retryable transient errors
   are covered.
2. The admin editor uses numeric Pepper, a fixed type select and a new
   mechanism-type creation/list section. API and repository writes enforce
   administrator/CSRF checks, canonical types, unique names and valid counts.
3. The explicit EF migration and updated snapshot add the catalogue, safely
   backfill valid legacy types and convert Pepper to an integer.
4. The in-memory provider automatically seeds future valid demo users, bays,
   mechanism types, bookings and closures. Tests verify idempotence and that
   mock storage remains unseeded.
5. Architecture and this handoff now record migration and rollout behavior.

Stop after these fixes and their directly required migration and checks.
Mechanism rename/delete, unrelated POC security cutover, redesign and other
administration features require separate approval.

## Testing Strategy

- Frontend component/session tests: edit a bay and select an admin tab, trigger
  focus with a pending and then successful check, and assert the same mounted
  view, draft, tab and no restoration skeleton; exercise 401, transient failure,
  demotion, forced change, logout and late response races. Confirm type empty
  state, creation, selection, counts, validation and save feedback.
- Range API tests: admin creation/list and bay round-trip; reject anonymous or
  non-admin calls, missing CSRF, negative/fractional Pepper, invalid or unknown
  types, duplicate names and duplicate type entries; prove no partial writes.
- Real disposable SQL tests: migrate an empty old-schema database, verify the
  integer column and catalogue, backfill valid legacy names, reject malformed,
  ambiguous and repeated-within-bay values without data loss, and exercise
  concurrent duplicate creation. Mock/in-memory tests alone do not prove
  transactions or locking.
- Sample-seed tests: automatic for Development and Test in-memory; absent for
  mock and SQL, with in-memory forbidden in production and staging; repeated
  initialization never duplicates or overwrites; use a fixed clock near a
  weekend and DST boundaries. Verify actual future sessions, available and
  reserved bays, pending/approved bookings, closures, configured admin access,
  admin views and one successful new booking. Test factories remain isolated.
- Browser: compare the supplied admin scenario at desktop and narrow viewport
  sizes, light/dark, keyboard focus, action spacing and select usability;
  verify SSR/direct navigation, focus preservation and actual API round-trip.
- Validation completed: 45 focused client tests passed; client production and
  SSR builds passed; the Release test build and Range test suite passed (70
  passed, 6 SQL Server tests skipped because disposable SQL configuration was
  absent). EF model/snapshot comparison reported no pending changes. The final
  SQL script was generated and inspected for migration preflight guards,
  catalogue creation/backfill, uniqueness and Pepper conversion. The layout
  detector returned no findings.
- Deferred evidence: migration SQL was generated but not run against SQL
  Server; the six SQL Server integration tests were skipped; no browser visual
  capture was possible because Chromium was unavailable. Restore emitted
  existing moderate/high advisories for `System.Security.Cryptography.Pkcs`
  9.0.4; dependency updates were out of scope.

## Migration And Rollback

Take and verify an Azure SQL backup before changing the schema. Pause all
writes and mixed API instances. Run the migration's mechanism-data preflight;
it backfills valid distinct names and stops on malformed, ambiguous or
repeated-within-bay values. Verify the new schema, start only the matching new
API, deploy the matching client, then smoke-test authorization, CSRF,
selection, saving and booking before resuming. Sample data is created only
when the existing Development/Test in-memory provider is active and requires
no production data migration.

An old API/client cannot safely use the numeric contract. Before new writes,
rollback can restore the verified pre-migration backup and matching old
deployment. After new writes, restoring that backup loses those writes; values
above 1 cannot be represented in the old boolean schema. Suspend mutations and
restore a compatible new version, or use a separately designed and verified
recovery/export procedure with explicit data-loss approval. Do not assume EF
down migration can reverse new counts. No MongoDB, SQLite, authentication
schema or cache migration is intended.

## Operational Gate

The client bay JSON Pepper value changes from boolean to number, so deploy the
migration, API and client as a coordinated change. No compatibility alias is
provided for mixed-version clients. Mechanism type rename/delete remain out of
scope.
