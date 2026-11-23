# Shooting Range: consent cleanup and code-review handoff

**Status:** Implementation, validation and independent review complete.
Review outcome: Approved with minor comments; the verified mock lookup issue
was fixed. The user confirms that no database exists and no historical
friendship rows need migration. The consolidated migration has not been
applied or deployed.

Parent: [booking feedback handoff](./shooting-range-booking-feedback-handoff.md).

## Goal

Keep the accepted-friendship workflow and unlimited exact-slot sharing while
removing the legacy-unverified state and its migration/backfill path. Then
perform an independent review of the revised feature changes and record
findings before any first database migration or deployment.

The user-confirmed no-database premise is the planning basis. If deployment
evidence contradicts it before migration cleanup is complete, stop and retain
the current compatibility path until the database state is understood.

## Current Evidence

- [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs)
  defines only `Pending` and `Accepted`; every friendship has a required
  requester.
- [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs)
  handles request, acceptance, decline, cancellation and removal without a
  legacy-pair renewal branch.
- [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs)
  validates the two-state lifecycle and requires requester membership in the
  canonical pair.
- [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs)
  requires the requester and constrains status values to `0` and `1`.
- [20261121171929_AddShootingRangeFriendships.cs](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/20261121171929_AddShootingRangeFriendships.cs)
  creates the final table with required consent fields and no row backfill.
  Its down migration stops before dropping populated friendships.
- [ShootingRangeDbContextModelSnapshot.cs](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/ShootingRangeDbContextModelSnapshot.cs)
  reflects the final two-state schema.
- [ShootingRangeServiceTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs)
  covers request, acceptance, decline and cancellation without a
  legacy-renewal case.
- Current behavior is otherwise intended to remain: only accepted friends
  grant visibility or sharing; requests require recipient acceptance; every
  shared booking remains separately Pending for admin approval; unlimited
  exact same-bay/UTC-interval occupancy is allowed when each joining member
  is an accepted direct friend of an active participant.

Repository workflows use explicit EF migrations; normal API startup verifies
the schema rather than applying migrations. The current API workflow deploys
the API image and does not apply the schema migration. The user confirms no
database exists; this handoff does not independently verify remote
infrastructure.

## Planned Work And Order

### 1. Removing legacy friendship compatibility — Complete

Implementation owner: Shooting Range senior developer — complete.

- Remove `LegacyUnverified` from the status enum and every runtime branch,
  repository transition, SQL constraint, snapshot and active test.
- Make the requester required for every friendship row. Keep only Pending
  and Accepted states, and require the requester to be one of the canonical
  pair members. Preserve the existing unique tenant/pair index, tenant
  foreign keys and guarded mutation lifecycle.
- Since no database has been created or migrated, fold the final friendship
  schema into the initial friendship migration:
  `status`, required `requester_user_id`, requester index/FK, and constraints
  for valid states and pair membership. Remove the follow-up consent
  migration and designer, including the status-`2` backfill.
- Regenerate/update the EF model snapshot using the project’s established
  EF tooling. Do not leave a snapshot or migration that describes a state the
  runtime no longer supports.
- Keep the initial migration’s rollback explicit and data-safe: a down
  migration must not silently drop friendships after the table contains rows.
  If the project’s migration conventions cannot prevent this loss, document
  the operator-only recovery path instead of implying a safe rollback.
- Remove stale legacy-state claims from the
  [Range architecture](../shooting-range-architecture.md),
  [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md),
  [master handoff](./shooting-range-booking-feedback-handoff.md), and
  [friendship handoff](./shooting-range-04-friends-sharing-handoff.md).
  Preserve historical policy only when clearly marked as superseded, and
  correct the ADR amendment date if it is still future-dated.

Do not change the friendship request API or client interaction as part of this
cleanup unless review demonstrates a concrete contract mismatch.

### 2. Validation before review — Complete

Implementation owner: Shooting Range senior developer — complete.

- `dotnet build MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  and `dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --no-build`
  passed; 94 passed and 9 SQL Server tests skipped because no disposable SQL
  host was configured.
- `DOTNET_ROOT=/usr/lib/dotnet dotnet ef migrations has-pending-model-changes --project MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj --startup-project MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj`
  reported no pending model changes. Generated forward SQL contains one
  friendship-table creation with required requester/status constraints and
  no friendship-row insert/backfill. Generated down SQL checks for rows before
  dropping the table.
- `yarn --cwd frontend workspace shooting-range.client test --passWithNoTests=false`
  passed (84 tests). `frontend/node_modules/.bin/tsc --project frontend/shooting-range.client/tsconfig.app.json --pretty false`,
  the production Vite build, and the SSR Vite build passed; build outputs were
  directed to `/tmp`.
- No disposable SQL Server was available for applying the migration or
  verifying a live schema. Skipped SQL tests do not prove locking, migration
  behavior or multi-writer correctness; authenticated browser verification
  was not performed.

### 3. Independent code review — Complete

Reviewer: MorWalPiz Code Reviewer; review completed after implementation and
validation passed.

Review the friendship change as one feature, not only the cleanup diff:

- **Persistence and migration:** enum/model/snapshot/migration agreement,
  requester non-nullability, canonical pairs, tenant-qualified keys, check
  constraints, explicit empty-database forward path and data-safe rollback.
- **Authorization and lifecycle:** approved same-tenant accounts; antiforgery
  on every unsafe route; requester-only cancellation; recipient-only
  acceptance/decline; duplicate/retry behavior; accepted-only friend lists;
  no account enumeration or cross-tenant ID access.
- **Shared booking admission:** accepted direct friendship with any active
  participant, exact bay and UTC interval for every overlap, no duplicate
  user, no numeric occupancy cap, and consistent enforcement through service
  and generic SQL/mock repository writes and reactivation.
- **Client and contracts:** accepted/incoming/outgoing lists remain distinct;
  actions refresh state and report failures; stale `POST /friends` clients
  fail explicitly; availability exposes only accepted direct friends; all
  participant/status rows remain visible to admins.
- **Regression and rollout:** tests cover the lifecycle and failure paths;
  migrations are applied only by explicit operator action; no old API writer
  may create immediate friendships or reintroduce the two-person cap.

The review report should distinguish verified defects from optional
improvements, name exact paths/symbols and impacts, and list skipped SQL or
browser checks as verification gaps rather than assumed successes.

#### Review result

- **Critical/Major findings:** None. Approval status was
  **APPROVED WITH MINOR COMMENTS**.
- **Verified minor issue fixed:** `MockShootingRangeUserRepository.FindByUsernameAsync`
  previously searched all tenants. It now reuses tenant-filtered `GetAllAsync`;
  `Friend_request_does_not_reveal_cross_tenant_usernames` confirms that a
  cross-tenant username and an unknown username fail identically.
- **Optional improvement adopted:** requester membership uses explicit
  `Latin1_General_100_BIN2` comparisons in the DbContext, snapshot and initial
  migration so SQL equality matches the application's ordinal ID checks.
- **Remaining verification gaps:** SQL Server migration/concurrency rehearsal
  and authenticated browser verification remain rollout gates. An additional
  Friends UI interaction test for action refresh/error feedback was suggested;
  no UI behavior changed in this cleanup.

## Implementation Result

- Updated `Models/ShootingRangeModels.cs`,
  `Services/ShootingRangeService.cs`,
  `Repositories/IShootingRangeRepositories.cs`,
  `Repositories/MockShootingRangeRepositories.cs`, and
  `Repositories/ShootingRangeDbContext.cs` to support only Pending/Accepted
  friendships, require a requester, remove legacy renewal, and constrain mock
  username lookups to the active tenant.
- Consolidated the friendship schema into
  `Persistence/Migrations/20261121171929_AddShootingRangeFriendships.cs`;
  removed the later consent migration and its designer; regenerated the model
  snapshot; and guarded the initial migration's down path against dropping
  populated friendships.
- Removed the legacy renewal case and added the cross-tenant lookup regression
  case in `MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs`.
  No API or frontend contract changes were needed.
- Updated the Range architecture, ADR-016, friendship handoff, master
  handoff, and this handoff. No migration or deployment was performed.

## Acceptance Criteria

- No production model, service, repository, migration or active test supports
  a legacy-unverified friendship state or legacy-to-pending transition.
- The final friendship schema starts with the consent fields and admits only
  Pending/Accepted rows with a valid requester; no existing-pair backfill is
  present.
- Request/accept/decline/cancel/remove behavior and accepted-only,
  unlimited exact-slot sharing remain covered by backend and client tests.
- Architecture documentation describes the actual fresh-schema design and
  does not claim that existing relationships are being preserved or renewed.
- Focused validation passes, followed by an independent review with no
  unresolved Major/Critical finding. SQL concurrency and migration claims
  remain explicitly unverified unless disposable SQL evidence is produced.

## Pre-cleanup Validation Baseline

Before this cleanup, the Range API test project passed 93 tests and skipped 9
SQL Server tests. Earlier frontend validation covered focused tests and is
superseded by the full 84-test run recorded above.

This handoff records the implementation and validation completed after the
original plan was written. No database migration or deployment was performed.
