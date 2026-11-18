# Shooting Range Azure SQL migration handoff

Status: **implemented locally; Azure SQL cutover not performed**.

## Outcome

Shooting Range now targets Azure SQL Database through EF Core's SQL Server
provider. The former PostgreSQL provider and its unshipped schema migration
were replaced. Range-owned accounts and sessions remain in the standalone
application; BackOffice and ServerAPI identity/data are unaffected.

## Implementation

- Runtime persistence, explicit `--migrate-database` schema application, and
  read-only readiness use SQL Server. The shared mutation guard is acquired
  with `UPDLOCK, ROWLOCK` inside the existing 15-second transaction.
- Nested lists/messages are JSON text in `nvarchar(max)`. Local clock fields
  use SQL `time`; dates use `date`; UTC instants use `datetime2` with a value
  converter that stores UTC ticks and restores `DateTimeKind.Utc` on reads.
  The normalized-username index uses `Latin1_General_100_BIN2` so uniqueness
  matches the application's ordinal normalized values.
- The versioned initial migration is SQL Server-specific. Normal API startup
  still fails closed for missing connection configuration, pending schema
  migrations, or a missing guard row. Production credentials/TLS remain
  externally configured; prefer Azure SQL managed identity authentication
  where provisioned.
- The isolated operator importer still reads the seven legacy Range Mongo
  collections and now writes into an empty, migrated Azure SQL target. It
  remains explicitly gated by `--confirm-import`, preserves source records,
  rejects username conflicts/nonempty targets, and verifies destination
  counts in a single guarded transaction. Supply
  `SHOOTING_RANGE_IMPORT_SOURCE_MONGO`,
  `SHOOTING_RANGE_IMPORT_SOURCE_DATABASE`, and
  `SHOOTING_RANGE_IMPORT_SQLSERVER` through the process environment; use a
  read-only Mongo principal and do not put connection values in source or logs.
- SQL Server integration tests cover unique constraint behavior, locking,
  rollback, booking conflicts/reactivation, and independent slots. The API
  workflow starts SQL Server 2022 with a per-run generated test password; it
  does not store a reusable database password in the repository.

## Key modified files

- `MorWalPizVideo.ShootingRange/Program.cs`,
  `Repositories/SqlServerShootingRangeRepositories.cs`,
  `Repositories/ShootingRangeDbContext.cs`,
  `Repositories/ShootingRangeDbContextFactory.cs`, and
  `Security/ShootingRangeSqlServerReadiness.cs`
- `MorWalPizVideo.ShootingRange/Persistence/Migrations/` (SQL Server initial
  migration replaces the unshipped PostgreSQL migration)
- `MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj`
- `MorWalPizVideo.ShootingRange.Migration/Program.cs`
- `MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeSqlServerTests.cs`
- `.github/workflows/deploy-shooting-range-api.yml`
- `docs/shooting-range-architecture.md`,
  `docs/architecture/backend.md`, `docs/architecture/testing.md`,
  `docs/architecture/adr/ADR-016-shooting-range-public-poc.md`, and
  `docs/architecture/operations/phase5-activation-and-recovery.md`

## Validation performed

- Restored and built the Shooting Range tests project in Release.
- Ran the entire Shooting Range test project against a disposable local SQL
  Server 2022 container: **71 passed, 0 failed, 0 skipped**, including all six
  SQL Server transaction/concurrency integration tests.
- The migration tool is referenced by the test project and built as part of
  that build.
- Restore/build reports existing transitive `System.Security.Cryptography.Pkcs`
  9.0.4 NU1902/NU1903 advisories.

## Cutover gates and residual risks

- No Azure SQL server was provisioned or contacted; no deployment or Azure
  schema migration was run. Local SQL Server tests are not Azure SQL
  operational evidence.
- The Mongo importer has BSON mapping tests but has not been run against the
  actual legacy Range database. Before any cutover, pause old writes, take and
  restore-test backups, import into an isolated Azure SQL target, and verify
  row counts, account/session compatibility, timestamps, and active booking
  intervals.
- The prior implementation had no production PostgreSQL database or data.
  This change does not include PostgreSQL-to-Azure-SQL data transfer. If any
  PostgreSQL-only records were created outside that known state, stop before
  cutover and plan a separately reviewed transfer.
- Configure least-privilege Azure SQL runtime/migration identities, TLS,
  Key Vault or managed identity, backups, restore procedures, and target
  concurrency/recovery drills. Do not retry unknown transaction outcomes
  automatically; clients should refresh before repeating.
- Existing public authorization/onboarding, browser, CORS/CSRF, and Data
  Protection release gates remain open.
