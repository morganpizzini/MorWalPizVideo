# Shooting Range booking workflow

Status: implementation complete; deployment and live SQL/browser verification
remain.

## Delivered behavior

- Availability loads when the date and an offered session are selected, and
  reloads when either changes. Existing session refresh and booking checks
  remain.
- Members can cancel their own future pending or approved reservations.
  Cancellation retains the record and displays its cancelled status. Admins
  can approve a cancelled reservation again only while it is future and passes
  the existing capacity/admission checks.
- Personal and admin booking lists include bookings dated yesterday or later
  in the configured field timezone. The admin navigation badge counts future
  pending bookings.
- Admin booking decisions are available at `/admin/bookings`; the former
  booking tab was removed. The coupled admin decision and account-approval
  form payload mismatch was corrected.
- Bay inventory displays Bersagli, Paratie, Piastre and Popper in that order.
  Targets are integers. Height and width are optional, paired positive
  decimals, stored to two decimal places without a unit. The editor keeps both
  inputs within one grid cell.
- Bay inventory and dimensions are included in availability details.

## User-confirmed policy

- Height and width are plain numbers; administrators know the unit.
- Cancellation is retained as history rather than physically deleting the
  booking.
- Both member and admin history use the yesterday-inclusive cutoff.
- Administrators may restore a cancelled future reservation by approving it.

## Changed files

- Backend behavior and contracts:
  [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs),
  [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs),
  [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs),
  [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs),
  [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs),
  [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs).
- Database migration:
  [20261120213000_AddBayTargetsAndDimensions.cs](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/20261120213000_AddBayTargetsAndDimensions.cs),
  its designer, and
  [ShootingRangeDbContextModelSnapshot.cs](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/ShootingRangeDbContextModelSnapshot.cs).
- Backend tests:
  [ShootingRangeServiceTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs)
  and
  [ShootingRangeApiTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs).
- Frontend:
  [App.tsx](../../frontend/shooting-range.client/src/App.tsx),
  [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx),
  [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx),
  [api.ts](../../frontend/shooting-range.client/src/api.ts),
  [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
  [routes.tsx](../../frontend/shooting-range.client/src/routes.tsx),
  and [styles.css](../../frontend/shooting-range.client/src/styles.css).
- Frontend tests:
  [App.test.tsx](../../frontend/shooting-range.client/src/App.test.tsx),
  [Admin.test.tsx](../../frontend/shooting-range.client/src/Admin.test.tsx),
  [api.test.ts](../../frontend/shooting-range.client/src/api.test.ts),
  and [routeData.test.ts](../../frontend/shooting-range.client/src/routeData.test.ts).
- Architecture and decision records:
  [shooting-range-architecture.md](../shooting-range-architecture.md)
  and [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md).

## Validation

- `cd frontend && yarn workspace shooting-range.client test` — passed,
  59 tests.
- `cd frontend && yarn workspace shooting-range.client build` — passed
  (`tsc -b` and Vite production build). Vite emitted the existing
  `/env-config.js` module-type warning.
- `DOTNET_ROOT=/usr/lib/dotnet dotnet build MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  — passed.
- `DOTNET_ROOT=/usr/lib/dotnet dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~ShootingRange"`
  — passed, 76 passed and 6 SQL Server tests skipped because no SQL Server
  test connection was configured.
- EF `migrations has-pending-model-changes` — passed; no model changes are
  missing from the migration snapshot. Generated SQL for the new migration
  adds `targets int NOT NULL DEFAULT 0` and nullable `height`/`width`
  `decimal(18,2)` columns.

## Deployment and residual verification

Apply the SQL migration before deploying the new API, then deploy the updated
client. New full-replacement bay requests require the new fields, so stale
clients fail safely instead of overwriting them. Existing bays receive zero
targets and unspecified dimensions; no measurements are inferred. Migration
application and rollback against a real database have not been performed.

SQL Server integration tests were skipped without a configured SQL Server.
No authenticated browser screenshot or visual check was available. The .NET
build/EF commands reported existing NU1902/NU1903 warnings for
`System.Security.Cryptography.Pkcs` 9.0.4. These warnings and the Vite warning
were not changed by this feature.
