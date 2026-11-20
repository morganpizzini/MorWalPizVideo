# Shooting Range Booking

## Status and scope

Shooting Range is a pre-release proof of concept that is expected to become publicly reachable. It remains independently deployable through the GitHub `production` environment, but public exposure does not make it a feature-complete product.

The approved direction is an authorization-first, minimal user experience. Preserve the project-local API contracts, service, repository, authentication, and transport boundaries so later features can be added without coupling the POC to BackOffice, Domain, Models, or Workshop application code. The Range client may consume the consolidated `@morwalpiz/layout` presentation package to share UI components and its aligned visual system with Shooter Workshop. This supersedes ADR-016's earlier shared-frontend restriction, which reflected the old layout implementation; it does not share Workshop authentication, services, models, or domain behavior. Do not implement speculative workflows before observed use establishes a requirement.

The shop is unrelated to this runtime and remains pre-production and on hold.

## Runtime

`MorWalPizVideo.ShootingRange` is a dedicated ASP.NET Core API. Its models, request/response contracts, application service, repository interfaces, Azure SQL and mock repositories, and password hashing implementation are owned by the project itself. It references only ServiceDefaults among the solution's application projects and is registered with `MorWalPizVideo.AppHost` as `shooting-range`. `frontend/shooting-range.client` is a separate React/Vite application with the same SSR shape as `ask.client`, but no Ask-domain dependency.

The API is intentionally detached from `MorWalPizVideo.BackOffice`, `MorWalPizVideo.Domain`, `MorWalPizVideo.Models`, and `MorWalPiz.Contracts`. BackOffice is a pattern reference only; no BackOffice entities or shared ShootingRange types are used at runtime. The client uses `@morwalpiz/layout` only for presentation components; its Range API client, types, cookie/CSRF handling, and business workflows remain local.

## Defaults and time

The initial configuration is one global field named Pepperbox, timezone `Europe/Rome`, opening Saturday/Sunday, morning `09:00-13:00`, afternoon `14:00-18:00`, and one-hour hourly slots. `ReservedReleaseDaysBefore` defaults to `2`. Local field dates and period keys are authoritative; the API returns `startUtc` and `endUtc` as UTC instants. The browser formats those instants for display.

Three administrator-selected modes are supported: whole morning/afternoon periods (`Periods=0`), split 60-minute windows (`Hourly=1`), and continuous 60-minute opening (`HourlyContinuous=2`). Existing enum values, routes and legitimate response fields remain unchanged. Continuous opening uses explicit nullable `ContinuousStart`/`ContinuousEnd`, never inferred from split windows. Configuration writes require `HourlyMinutes=60`, valid minute-precision same-day windows, and a positive break for split modes. Only complete hourly slots are offered; ambiguous/invalid DST boundaries are excluded. Legacy non-60 configurations remain readable but must be corrected before new bookings are accepted.

A reserved bay is restricted to its whitelist while the local field date is more than two calendar days away. At T-2 it is available to all users when no active booking exists. Administrative closures, bay exceptions and conflicts take precedence. This is calculated per request; no job is required for correctness. Sessions come from `/api/shooting-range/sessions`; the client displays them in the configured field timezone and submits expected UTC boundaries to detect stale selections.

## Data and concurrency

Azure SQL Database is the production persistence store for all Range data, including its independent accounts and sessions. EF Core maps Range-owned data to `shooting_range_configs`, `shooting_range_bays`, `shooting_range_exceptions`, `shooting_range_users`, `shooting_range_bookings`, `shooting_range_threads`, `shooting_range_local_sessions` and the singleton `shooting_range_mutation_guards` row. Existing nested value lists/messages are stored as JSON text in `nvarchar(max)` columns; primary business records remain separate relational rows. UTC instants use `datetime2` with an EF conversion that restores `DateTimeKind.Utc`; local `CreationDateTime` uses `datetime2` without conversion. The initial schema and later changes are versioned EF migrations. Run the API assembly with `--migrate-database` as an explicit operator action; normal SQL startup never creates/upgrades the schema and fails closed unless all migrations and the guard row are present.

The `FeatureManagement:EnableInMemoryDb` flag selects EF Core's in-memory provider for Development or Test only; it is enabled in `appsettings.Development.json` and disabled by default elsewhere. It uses the SQL repository implementation with a process-local write gate, but has no SQL transaction, cross-process concurrency, or persistence guarantees and must not be used for production data. When enabled it takes precedence over the legacy `EnableMock` repository flag. The mock repository remains available in Development/Test when the in-memory database flag is off.

Every repository write participates in a SQL Server `ReadCommitted` transaction and acquires the singleton mutation-guard row using `UPDLOCK, ROWLOCK` before reading validation/admission state. The lock has a 15-second deadline. Every Range writer, including account, session, schedule, bay, closure, booking, and message changes, must use this guard. This serializes writes globally by design for the POC; no process-local lock is used for correctness. Cookie issuance follows successful transaction commit. The readiness check is read-only and verifies database connectivity, schema and guard row.

Pending and Approved consume capacity through half-open persisted UTC intervals per bay, regardless of period key or current mode. Adjacent intervals and different bays are allowed. Generic booking insert/replace paths also validate capacity; inactive-to-active transitions recheck admission, while Pending-to-Approved preserves the original reservation. Historical intervals and booking identity cannot be rewritten. Malformed active intervals block the affected bay until operational reconciliation. A unique normalized-username index with binary collation is a database backstop. The mock repository serializes mutations and restores its snapshot on failure; mock tests do not prove SQL Server locking or transaction rollback.

All account passwords are PBKDF2 hashes with per-account salts. New passwords must contain at least seven characters. Passwords must never be logged or returned. Only Approved users can authenticate. Admin reset marks `ForcePasswordChange`; the new password must be communicated out of band.

The first production administrator is inserted manually into `shooting_range_users`; there is no public or administrative bootstrap endpoint. The inserted row must use the normalized username and the password representation produced by the current password-hashing implementation. In Development/Test only, the EF in-memory provider can seed a local administrator when `ShootingRange:InMemoryAdminPassword` is supplied through environment-managed configuration. That account requires a password change at first login. No password or reusable hash is stored in source or documentation.

Ordinary-user onboarding is not yet a final product decision. Admin-created users are the working assumption because it keeps account creation behind authorization; public self-registration is not part of the approved public-release posture.

## Security policy

Authentication uses an HttpOnly, Secure, SameSite=None cookie with an eight-hour, non-sliding lifetime and a server-side revocable session. The target authorization policy is deny-by-default for every Shooting Range domain endpoint, including availability; that domain cutover is NOT READY until ordinary-user onboarding is approved. Current admin routes require the `admin` role from revalidated account state.

In the target policy, only these technical entry points remain anonymous:

- Login, because authentication cannot otherwise begin.
- CSRF token acquisition, because login and later unsafe cookie requests need a token.
- Liveness and readiness probes.

Login itself and unsafe cookie-authenticated requests require the `X-CSRF-TOKEN` token returned by `/api/shooting-range/csrf`. Acquire it before login and refresh it after successful login or password change. Login is limited to 10 attempts per five minutes per remote IP per process; scaled deployments require reviewed gateway/distributed throttling and trusted proxy configuration. Production credentialed CORS allows only the configured Shooting Range client origin.

Current source does not yet satisfy the full target: registration, configuration, sessions and availability are anonymous, and no fallback authorization policy exists. These remain public-release blockers outside the scoped schedule change. Credentialed CORS now permits exactly `https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net`; no suffix, trailing-slash or arbitrary-origin matching is enabled, including in Development.

Every cookie request revalidates the session's expiry/revocation/security version and current Approved account status and role, including mine/messages. Disabled accounts are rejected; demotion removes administrator access immediately. Logout revokes the current session. Password reset/change rotates `SecurityVersion`, invalidating older sessions; successful self-change revokes and renews the current session. Forced-change sessions permit only session inspection, password change, logout and technical CSRF acquisition. Old cookies without a session claim require reauthentication. The client restores sessions on refresh/direct navigation, handles expiry/retry, and supports logout without persisting credentials or redesigning the existing UI.

Booking admission also rechecks current approved account status and `ForcePasswordChange`; touched administrator operations recheck current administrator state. Admin-user responses explicitly preserve `Id`, `CreationDateTime`, username, names, status, administrator and forced-password flags, excluding `PasswordHash`. Safe projections for other persistence entities remain a separate cutover requirement. The API remains the authoritative admission boundary.

Cancellation/modification is intentionally not exposed in the POC. Admin rejection is the supported way to release a pending slot; this avoids ambiguous user-side policy while the operational cancellation policy is agreed.

## Minimal UI scope

The initial public-facing POC is limited to:

- Login, session restoration, forced password change, and logout.
- Authenticated availability search and booking.
- The current user's bookings.
- Minimal administrator user creation and booking approval or rejection, if admin-created ordinary users are confirmed.

Schedule, bay and closure management and booking decisions are now accepted administrator scope. The client preserves `/`, `/messages` and `/admin`; this refinement does not authorize unrelated messaging expansion. Cancellation, rescheduling, notifications, waitlists, custom-duration sessions, and reporting remain deferred.

## Appearance preference

The client uses the shared `@morwalpiz/layout` System/Light/Dark appearance
contract and opt-in theme stylesheet. System is the default; explicit choices
are stored per browser origin and do not alter Range authentication, booking,
or account state. The initial HTML bootstrap applies the resolved theme before
the client starts, while the React selector appears on the login and
authenticated shell. Range-specific status roles map to shared semantic tokens.

The Range test suite (**36 passed**), checked client build, and SSR build passed.
Browser rendering, deployed content-security policy, and PWA/offline behavior
were not verified locally because no Chromium-based browser was installed.

## Future extension candidates

Reassess these only after real POC use establishes operational value:

1. Cancellation and rescheduling with an explicit policy.
2. Further administration workflows beyond the accepted schedule, bay and closure controls.
3. Message threads and replies.
4. Notifications and waitlists.
5. Custom-duration sessions.
6. Attendance and utilization reporting.
7. Invite-based onboarding if administrator-created accounts become burdensome.

## Deployment and rollback

Development defaults to the EF Core in-memory provider through `FeatureManagement:EnableInMemoryDb`; this flag is allowed only in Development or Test and must remain disabled in production. The legacy mock repository remains available in Development/Test when the in-memory database flag is off; production rejects either non-SQL mode. Azure SQL uses external configuration key `ShootingRangeDatabase:ConnectionString`; when Key Vault is enabled, provide `ShootingRangeDatabase--ConnectionString` and grant the App Service managed identity read access. Prefer Azure SQL Microsoft Entra managed identity authentication where provisioned; otherwise keep the SQL credential in Key Vault. Require encryption and certificate validation for deployed connections. The API fails startup when the configured vault is unavailable or the required key is missing, and does not fall back to a direct connection value. A separately managed `ShootingRangeDatabase:MigrationConnectionString` may be supplied for explicit schema migration; it must not be used by normal runtime. Keep all connection values outside source, documentation and deployment workflows.

The API and client continue to deploy independently through workflows bound to the GitHub `production` environment. Before public exposure, validate the authorization matrix, configured CORS origin, CSRF/login flow, persistent Data Protection keys, Azure SQL readiness, unique normalized usernames, booking concurrency, backup/restore, data migration if required, and successful login by the manually inserted administrator.

Mock repositories are allowed only in Development or Test; enabling `FeatureManagement:UseMock` in Production or Staging fails startup before storage resolution. Shooting Range API host tests explicitly enable the in-memory database, while the separate SQL Server tests exercise provider-specific behavior. Azure SQL startup verifies connectivity, no pending schema migrations and the provisioned mutation guard row; it does not apply schema changes or replace target-environment transaction and backup/restore evidence. EF migrations create the unique `ux_shooting_range_users_normalized_username` index using `Latin1_General_100_BIN2`. Session expiry is checked during authorization; no TTL cleanup is required.

Real SQL Server tests use `SHOOTING_RANGE_TEST_SQL_HOST`, `SHOOTING_RANGE_TEST_SQL_PORT`, `SHOOTING_RANGE_TEST_SQL_DATABASE`, `SHOOTING_RANGE_TEST_SQL_USERNAME`, and optionally `SHOOTING_RANGE_TEST_SQL_PASSWORD` for an explicitly disposable server with permission to create/drop uniquely named test databases. Missing host configuration skips only those tests. The API deployment workflow provisions a temporary SQL Server 2022 container with a per-run generated password and requires nonzero selected and executed integration tests before publishing. Never point integration tests at production or shared customer data. CI test success does not replace Azure SQL backup/restore, migration and production approval evidence.

Browser verification must cover the exact allowed origin, denied lookalikes, cookie/CSRF flow, desktop/mobile controls, SSR and direct navigation. Missing target Azure SQL backup/restore, existing-data migration or browser evidence blocks release, not authoring; no deployment is implied by local checks.

Use R1-R8 and R14 in the existing [release evidence checklist](architecture/operations/phase5-activation-and-recovery.md#release-evidence-checklist-2026-10-01) for named owners, exact environment/UTC timestamps, redacted artifacts, independent approval and compatible recovery actions. All absent target proofs are BLOCKED. Slice 10 runbook authoring is complete; operational release closure is not. Slice 4 remains NOT READY until ordinary-user onboarding is explicitly confirmed; current anonymous registration is not silently removed.

Rollback before Azure SQL writes may use the old Mongo deployment only if its actual Cosmos transaction prerequisites have been proven; do not assume that it works. After new SQL writes, restore a compatible Azure SQL-backed API/database or use a separately tested and approved reverse import. Never restart an old Mongo writer against stale data. If no compatible recovery exists, suspend mutations and preserve both source and target data. Never restore permissive CORS, password-hash responses or production mock mode.
