# VideoImporter startup migration and schema gate

## Outcome

**Status: implemented; focused runtime tests still require a Windows host.**

Every normal VideoImporter launch must apply pending SQLite migrations and verify
the resulting physical schema before constructing or showing an interactive
window. Any migration or verification failure must produce one localized fatal
notification, stop/dispose initialized host resources, and terminate with a
nonzero exit code. There is no continue, degraded database mode, or automatic
database recreation.

Assumptions: this requirement concerns startup database readiness, not a new
global policy for every runtime/network exception. Existing remote-settings
degraded behavior remains valid only after the local database passes the gate.
The actual affected workstation database has not been inspected.

## Current Evidence

All paths below are relative to the repository root.

- [App.xaml.cs](../../MorWalPiz.VideoImporter/App.xaml.cs):
  `OnStartup` builds configuration, awaits `StartHostAsync`, and shows the returned
  window. There is no controlled startup failure notification/shutdown boundary.
  `StartHostAsync` starts the host, calls `DatabaseService.InitializeDatabase`,
  then resolves settings/services and finally `MainWindow`. Its optional
  initializer callback is used by tests.
- [DatabaseService.cs](../../MorWalPiz.VideoImporter/Services/DatabaseService.cs):
  `InitializeDatabase` calls EF Core `Migrate` only. It does not check physical
  schema or residual pending migrations. `_dbPath` is captured but never used
  to configure a context.
- [AppDbContext.cs](../../MorWalPiz.VideoImporter/Data/AppDbContext.cs):
  each context derives its database path anew from the current working
  directory. Seven mapped tables include `Tenants`, `Settings`, `Languages`,
  `Disclaimers`, `PublishSchedules`, `HashtagHistory`, and
  `SocialChannelConfigurations`; tenant filters protect six of them.
  Configured indexes include a tenant/language-default partial unique index
  and a tenant/channel/hashtag unique index; disclaimers reference languages.
- [tenant channel migration](../../MorWalPiz.VideoImporter/Migrations/20260918130000_tenantChannel.cs)
  now has migration/context metadata and adds `Tenants.ChannelId`.
  [social publishing migration](../../MorWalPiz.VideoImporter/Migrations/20260918120000_socialPublishing.cs)
  is likewise registered. Registration does not prove a workstation's schema.
- [DatabaseMigrationTests.cs](../../MorWalPiz.VideoImporter.Tests/DatabaseMigrationTests.cs)
  upgrades from the August migration and reads tenants. It does not test
  a database whose migration history is current but whose schema is damaged.
- [ImageGenerationTests.cs](../../MorWalPiz.VideoImporter.Tests/ImageGenerationTests.cs)
  contains STA dispatcher startup/lifecycle fixtures and a repeat-initialization
  compatibility test. The host fixture deliberately substitutes an initializer
  over an `EnsureCreated` database, so it is not real startup migration proof.
- The [application manifest](../../MorWalPiz.VideoImporter/MorWalPiz.VideoImporter.csproj)
  and [test manifest](../../MorWalPiz.VideoImporter.Tests/MorWalPiz.VideoImporter.Tests.csproj)
  target `net10.0-windows` with WPF. Tests use xUnit.
- [Windows architecture](../architecture/windows-apps.md) and
  [ADR-021](../architecture/adr/ADR-021-video-importer-channel-settings.md)
  preserve SQLite bootstrap/operational records while remote channel settings
  remain authoritative. Database replacement is not an acceptable repair.

## Proposed Design

### Single database identity

Resolve the absolute database path once when `DatabaseService` is constructed,
preserving the existing current-working-directory location. Pass that same path
to every context it creates, including initialization and verification. Retain
the parameterless EF design-time context and current tenant-context constructor
compatibility; application contexts must use the service-owned path. Do not move
existing files or add a new configuration setting.

### Ordered readiness gate

1. Open the selected database and apply registered pending migrations with the
   existing EF migration mechanism. Never replace this with `EnsureCreated`.
2. Verify there are no pending migrations. Reject migration-history entries
   unknown to this binary rather than accidentally running against a newer
   database. Do not edit history to make these checks pass.
3. Independently inspect SQLite's physical schema using provider metadata on the
   same database connection. Derive expected objects from EF's relational model,
   not a second hand-maintained list of entity property names.
4. Require every mapped table and mapped column, compatible SQLite type affinity,
   model-required nullability, primary-key shape, foreign-key definitions/delete
   behavior, and configured index column order/uniqueness/partial condition.
   Account for SQLite primary-key nullability reporting, implicit indexes and
   harmless provider identifier/whitespace differences. Read partial-index
   definitions only where needed; do not compare whole generated DDL strings.
   Additive, unmapped tables/columns/indexes are tolerated; incompatible required
   definitions fail. This is structural compatibility verification, not full
   row-data integrity validation or an automatic repair engine.
5. Only after success may startup resolve database-consuming settings,
   upload/synchronization services, tenant subscriptions, or the main window.
   Keep the existing initializer test seam internal; tests substituting it do
   not count as production-gate coverage.

Keep the verifier private to the database service initially; do not introduce a
generic schema framework, shared package, or additional runtime dependency.
Use bounded read-only metadata queries with correctly quoted provider identifiers;
do not materialize tenant rows or log row values during schema inspection.

### Failure boundary and shutdown

Keep database validation failures explicit and propagating. At the application
startup boundary, handle failures terminally: record the failing stage, absolute
database path when available, migration IDs, and missing/incompatible object
names through structured `ILogger` diagnostics. Do not record configuration,
credentials, connection strings, SQL parameter values, or database contents.

Show one native WPF error notification in Italian explaining that initialization
failed, normal use is blocked, and the application will close. Include the
database file path and a safe migration/schema failure category, not an
unsanitized exception/stack trace. No retry/continue button or main window behind
the notification. A terminal catch here is acceptable only because it notifies
and exits; it must never convert failure into startup success.

Protect configuration/host creation as well as database startup so an early
startup failure cannot leave a partially running process. Use explicit WPF
shutdown mode while startup/the fatal notification is active, then restore normal
main-window-close semantics after successful startup. On failure, stop/dispose
any host that exists before explicit nonzero application shutdown; ensure
`OnExit` cannot double-dispose resources. Cleanup failure must be diagnosed
without losing the original failure or preventing termination.

No background work that consumes application data may start before readiness.
The current host registrations have no application hosted worker; preserve that
ordering if startup is reorganized. Do not add a disabled main-window state as a
substitute for withholding window construction.

## Impacted Projects

- **MorWalPiz.VideoImporter:** database identity, startup gate, fatal lifecycle.
- **MorWalPiz.VideoImporter.Tests:** SQLite upgrade/drift and STA startup tests.
- **Deployment:** Windows importer distribution/smoke verification only;
  no workflow, packaging, API, shared library, MongoDB, or frontend package changes.

## Impacted Files

### Modify

- `MorWalPiz.VideoImporter/App.xaml.cs`: startup failure boundary and lifecycle.
- `MorWalPiz.VideoImporter/Services/DatabaseService.cs`: one database identity,
  migration/post-migration structural checks, safe diagnostics.
- `MorWalPiz.VideoImporter/Data/AppDbContext.cs`: accept service-owned path while
  retaining current tenant filtering and design-time compatibility.
- `MorWalPiz.VideoImporter.Tests/DatabaseMigrationTests.cs`: drift and upgrade proof.
- `MorWalPiz.VideoImporter.Tests/ImageGenerationTests.cs`: fatal and successful
  startup lifecycle assertions; reuse its STA helper.

### Add

None required. Prefer private methods and existing fixtures over new abstractions.

### Generated/Migration

None expected: the current EF model and intended schema do not change. Do not
regenerate migrations merely to hide drift or manually alter migration history.

### Documentation

- This handoff: record actual files, validation results, and remaining risks.
- `docs/architecture/windows-apps.md`: document fail-closed startup after delivery.
- `docs/architecture/adr/ADR-021-video-importer-channel-settings.md`: clarify that
  remote-settings degraded mode requires a verified local schema.

### Verify Only

- `MorWalPiz.VideoImporter/App.xaml`: confirm no automatic startup window route.
- Both importer project manifests: retain existing dependencies and Windows target.
- The two September migration files and `Migrations/AppDbContextModelSnapshot.cs`:
  discovery and physical/model consistency.
- `MainWindow.xaml.cs`, `Services/TenantService.cs`,
  `Services/ChannelConfigurationSyncService.cs`: consumers remain behind the gate;
  no changes to channel selection or remote-first writes.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | None; no endpoints or server changes. |
| Frontend/UI | No web frontend change. WPF gains one fatal startup notification, not a redesign. Native dialog needs keyboard/focus and DPI smoke checks; Impeccable web workflow is not applicable. |
| Database/Data | Apply existing SQLite migrations, verify physical compatibility, preserve records, block on drift. MongoDB unaffected. |
| Contracts/Models | No shared DTO or entity changes. EF relational metadata is the expected schema source. |
| Authentication/Authorization | Unchanged; no credential reads/writes added by verification. |
| Validation | Startup is successful only after migration/history and physical checks pass. |
| Cache | Remote-settings/local-cache semantics unchanged after readiness; no HTTP cache changes. |
| Configuration/Secrets | Preserve database location/configuration precedence. Never expose secret values. |
| Logging/Telemetry | Structured startup stage/path/schema failure diagnostics; user-safe terminal notification. No new telemetry service or logging platform. |
| Background Jobs | No new jobs; prevent consumer initialization on failure. |
| Deployment/Operations | Windows verification and database backup/recovery guidance; nonzero fatal exit. |
| Backward Compatibility | Old local data upgraded in place. Unknown newer migrations and incompatible schema rejected, not downgraded. |

## Reuse Opportunities

Reuse `DatabaseService`, `AppDbContext`, EF migrations/relational metadata and
SQLite provider metadata. Reuse `StartHostAsync`/`StopHostAsync`, WPF native
notification, existing `ILogger` DI infrastructure, xUnit desktop serialization,
STA dispatcher fixture, fake external services and unique temporary databases.
No API call or new schema checksum registry is needed.

## Risks

| Rank | Cause/surface | Mitigation and verification signal |
|---|---|---|
| High | Current history can conceal missing columns/tables and allow unsafe use. | Physical metadata checks; current-history/missing-channel fixture fails before main-window construction. |
| High | Destructive repair could lose bootstrap secrets and operational records. | No drop/recreate, repair or history rewrite; sentinel records survive upgrades and failed verification. |
| Medium | SQLite affinity, primary-key and index reporting differs from model representation. | Provider-aware normalization; fresh and upgraded real SQLite databases pass, altered definitions fail. |
| Medium | Shutdown races, partial host initialization or notification dismissal leave resources running. | Single terminal boundary, explicit startup shutdown mode, idempotent cleanup; STA tests assert no consumer construction and disposal once. |
| Medium | Wrong file selected after current directory changes. | Pin the original absolute path; change-directory regression proves subsequent contexts still target it. |
| Medium | Lock, permission or migration operation failure interrupts rollout. | Fail without interactions; retain original error category and path, test a deterministic failing migration operation, and verify recovery on a copied database. |
| Low | Startup messages or logs expose secrets. | Safe categories/object names/path only; test secret-like exception details are absent from user notification. |
| Low | Older binary opens newer migrated database. | Reject unknown history; do not automatically downgrade. |

External-service and cache coherence risks: no new behavior; gate must prevent
channel sync/uploads from running against an unverified local database.

## Technical Debt

- Unused `_dbPath` and per-context directory resolution are directly coupled to
  readiness; address within this slice.
- Existing test initializer bypass is intentional test infrastructure. Retain it,
  but add real-gate coverage instead of presenting it as migration proof.
- Broader MVVM work, persistent logging platform, database relocation,
  startup splash/progress UI and runtime-wide exception handling are deferred.
- Detection is not remediation: history/schema drift remains an explicit,
  blocked recovery case rather than an implicit manual repair.

## Implementation Roadmap

1. **Database identity — implemented:** database contexts share the resolved
   absolute path. Runtime regression execution is pending Windows.
2. **Readiness — implemented:** startup checks history, migrates, checks
   remaining migrations, then validates relational metadata against SQLite.
   Runtime regression execution is pending Windows.
3. **Fatal startup — implemented:** no main window is resolved before readiness;
   failures log safe diagnostics, notify once, clean up, and request nonzero
   shutdown.
4. **Validation and documentation — partially complete:** app and test projects
   compile; architecture/ADR/handoff are updated. Focused WPF test execution and
   manual Windows verification remain open because this host lacks the Windows
   Desktop runtime. Stop implementation here; no unrelated project changes.

Independent review after validation is recommended for schema-comparison edge
cases and WPF async shutdown reliability; the user may select MorWalPiz Code
Reviewer. It is not a new mandatory implementation phase.

## Testing Strategy

All automated cases belong in `MorWalPiz.VideoImporter.Tests`, use isolated
temporary SQLite files, and never touch the user's database.

- **Migration/integration:** fresh database; August-to-current upgrade preserving
  modified settings and tenant records; repeat initialization; no pending
  migrations after success; unknown newer history rejected.
- **Physical drift:** history current but `Tenants.ChannelId` missing; another
  required table missing; incompatible required column definition; removed or
  altered configured unique index/foreign key. Each fails with a useful safe
  diagnostic. Verify additive unrelated objects remain acceptable.
- **Database identity:** construct service, change current directory, initialize
  and open another context; both use the original file.
- **Failure lifecycle:** real validation failure plus injected initializer
  failure; main-window factory and upload/sync consumers never invoked; safe
  notification once; host cleanup once; nonzero shutdown requested. Use the
  narrowest internal presentation/shutdown test seam if needed rather than a
  new public notification framework or blocking automated tests on a real modal.
- **Success lifecycle:** database readiness precedes consumer resolution,
  window shown once, tenant switching/cancellation/disposal retained.
- **Security:** notification excludes raw secret-like exception content; metadata
  inspection does not read settings values. Auth contract tests are not required.
- **Unit:** only isolated normalization/comparison cases needed for real SQLite
  representation differences; do not test framework constants.
- **Contract/component:** no API contract change; native WPF lifecycle tests
  replace web component tests for this slice.
- **End-to-end/manual Windows:** launch against a copied valid older database,
  then a deliberately drifted copy. Confirm first upgrades and opens, second
  shows only fatal notification then exits with no actionable window or surviving
  app process. Check keyboard dismissal and normal/high-DPI notification layout.

Follow the test runbook: serialized Release build, then focused tests without
rebuild, then importer suite. A build or zero-test exit is not passing test
evidence. Linux cannot establish WPF runtime behavior.

## Migration And Rollback

No new schema migration is required because the intended schema/model is unchanged.
Existing registered migrations still run in order; successful checks are read-only.
There is no backfill, automatic repair, new feature flag or compatibility window.

Before upgrading an affected workstation, close the app and back up the local
database using a SQLite-consistent backup, accounting for WAL files if present.
Distribute the verified Windows binary; observe success or the safe failing
stage/path. Failed validation blocks use until a reviewed repair or compatible
backup is restored while the app is closed. Do not delete the database or modify
history as a startup workaround.

Application rollback does not roll back migrations. An older binary is safe only
if compatible with the migrated schema; otherwise restore a known-compatible
SQLite backup, acknowledging loss of changes after that backup. Unknown newer
history is intentionally blocked. No API, MongoDB, authentication, cache or
configuration migration/deployment sequencing is needed.

## Open Questions

None blocking this bounded design. The workstation's actual schema/history and
running-binary identity remain unknown; they affect diagnosis/recovery, not the
required fail-closed startup behavior.

## Implementation And Validation Record

Implemented in this slice:

- `MorWalPiz.VideoImporter/Data/AppDbContext.cs`: application contexts accept a
  service-owned database path; the parameterless and tenant-only constructors
  remain compatible.
- `MorWalPiz.VideoImporter/Services/DatabaseService.cs`: resolves one absolute
  path, rejects unknown history, migrates, checks for pending migrations, and
  compares physical SQLite tables, column affinities/nullability, primary keys,
  indexes/filters, and foreign keys against EF relational metadata. It fails
  without modifying migration history or recreating a damaged database.
- `MorWalPiz.VideoImporter/App.xaml.cs`: holds explicit shutdown mode until the
  host/database gate succeeds. Startup errors are logged without raw exception
  content, show one safe fatal notification, stop/dispose the host, and call
  nonzero shutdown; no main window is resolved before the gate.
- `MorWalPiz.VideoImporter.Tests/DatabaseMigrationTests.cs`: tests previous
  migration upgrade, current-history missing-column drift, unknown future
  migration history, and stable database-path resolution.
- `MorWalPiz.VideoImporter.Tests/ImageGenerationTests.cs`: tests failed-schema
  startup notification, host stop, nonzero exit request, and that the main
  window is never resolved.
- `docs/architecture/windows-apps.md` and this feature's
  `docs/architecture/adr/ADR-021-video-importer-channel-settings.md`: record the
  fail-closed gate and its relation to local-cache behavior.

Validation:

| Command | Result |
|---|---|
| `dotnet build MorWalPiz.VideoImporter/MorWalPiz.VideoImporter.csproj --configuration Release --no-restore -p:EnableWindowsTargeting=true -m:1 -p:BuildInParallel=false` | Passed before the final test build. |
| `dotnet build MorWalPiz.VideoImporter.Tests/MorWalPiz.VideoImporter.Tests.csproj --configuration Release --no-restore -p:EnableWindowsTargeting=true -m:1 -p:BuildInParallel=false` | Passed, 0 warnings and 0 errors on the final source build. |
| `dotnet test MorWalPiz.VideoImporter.Tests/MorWalPiz.VideoImporter.Tests.csproj --configuration Release --no-restore --no-build -p:EnableWindowsTargeting=true --filter "FullyQualifiedName~DatabaseMigrationTests|FullyQualifiedName~Startup_schema_failure_notifies_stops_and_never_resolves_main_window" --verbosity minimal` | Test discovery succeeded, but execution aborted because Linux lacks `Microsoft.WindowsDesktop.App` 10.0.0. Runtime pass/fail remains unverified. |

Residual risk: Windows execution of the migration/schema comparisons and WPF
failure lifecycle is required before release. The focused manual Windows
verification in this handoff remains open. The actual user's database was not
opened or modified.
