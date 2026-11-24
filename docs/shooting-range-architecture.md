# Shooting Range Booking

## Status and scope

Shooting Range is a pre-release proof of concept that is expected to become publicly reachable. It remains independently deployable through the GitHub `production` environment, but public exposure does not make it a feature-complete product.

The approved direction is an authorization-first, minimal user experience. Preserve the project-local API contracts, service, repository, authentication, and transport boundaries so later features can be added without coupling the POC to BackOffice, Domain, Models, or Workshop application code. The Range client may consume the consolidated `@morwalpiz/layout` presentation package to share UI components and its aligned visual system with Shooter Workshop. This supersedes ADR-016's earlier shared-frontend restriction, which reflected the old layout implementation; it does not share Workshop authentication, services, models, or domain behavior. Do not implement speculative workflows before observed use establishes a requirement.

The shop is unrelated to this runtime and remains pre-production and on hold.

## Runtime

`MorWalPizVideo.ShootingRange` is a dedicated ASP.NET Core API. Its models, request/response contracts, application service, repository interfaces, Azure SQL and mock repositories, and password hashing implementation are owned by the project itself. It references only ServiceDefaults among the solution's application projects and is registered with `MorWalPizVideo.AppHost` as `shooting-range`. `frontend/shooting-range.client` is a separate React/Vite application with the same SSR shape as `ask.client`, but no Ask-domain dependency.

The API is intentionally detached from `MorWalPizVideo.BackOffice`, `MorWalPizVideo.Domain`, `MorWalPizVideo.Models`, and `MorWalPiz.Contracts`. BackOffice is a pattern reference only; no BackOffice entities or shared ShootingRange types are used at runtime. The client uses `@morwalpiz/layout` only for presentation components; its Range API client, types, cookie/CSRF handling, and business workflows remain local.

## Tenant boundary

The Range API and Azure SQL database are shared by multiple shooting ranges. Each client deployment must provide its public `SHOOTING_RANGE` slug; the client server validates and safely serializes it into `window.ENV`, and every Range API request sends it in `X-Shooting-Range`, including CSRF and authentication requests. Missing or invalid client configuration fails closed. The slug selects a tenant; it is not an authentication credential.

Before cookie authentication, the API resolves exactly one active tenant and binds its internal ID to the request. The tenant registry binds each range to its exact HTTPS client origin. CORS preflight resolves that origin without relying on the tenant header, while actual browser requests must match both the origin and selected tenant. Tenant-owned rows have server-assigned `TenantId`; EF Core query filters and tenant-qualified indexes and foreign keys enforce scoped reads, uniqueness and relational ownership. Request payloads cannot set the tenant ID. Cookies and persisted sessions carry tenant identity and are rejected when the selected tenant differs.

Domain endpoints require authentication by fallback policy. CSRF acquisition, login, health probes and the separately authenticated management API are technical exceptions. Public registration has been removed; provisioning creates a tenant and its initial configuration, not an administrator. The existing manual first-administrator policy remains in effect and the administrator row must be created with the new tenant ID.

`POST /api/shooting-range/management/tenants` provisions a range using the `X-Shooting-Range-Management-Key` header. Configure `ShootingRange:ManagementKey` (App Service setting `ShootingRange__ManagementKey`) only on the API; production startup requires at least 32 characters. Swagger is controlled by `FeatureManagement:EnableSwagger`, disabled by default and enabled in Development settings. When enabled, its UI and OpenAPI document are accessible without application authentication and expose the management-key scheme. Keep production Swagger and the provisioning route behind trusted ingress, and never put the key in client configuration.

## Defaults and time

The initial configuration is one global field named Pepperbox, timezone `Europe/Rome`, opening Saturday/Sunday, morning `09:00-13:00`, afternoon `14:00-18:00`, and one-hour hourly slots. `ReservedReleaseDaysBefore` defaults to `2`. Local field dates and period keys are authoritative; the API returns `startUtc` and `endUtc` as UTC instants. The browser formats those instants for display.

Four administrator-selected modes are supported: whole morning/afternoon periods (`Periods=0`), split 60-minute windows (`Hourly=1`), continuous 60-minute opening (`HourlyContinuous=2`), and mixed whole-window or contiguous blocks (`Mixed=3`). Existing enum values, routes and legitimate response fields remain unchanged. Continuous opening uses explicit nullable `ContinuousStart`/`ContinuousEnd`, never inferred from split windows. Legacy modes require `HourlyMinutes=60`; Mixed accepts a 60- or 120-minute increment, defaulting to 60. It offers each full morning/afternoon window and every contiguous block of complete increments within those windows; no selection spans the afternoon break. Only complete increments are offered; ambiguous/invalid DST boundaries are excluded. Legacy non-60 configurations remain readable but must be corrected before new bookings are accepted.

A reserved bay is restricted to its whitelist while the local field date is more than two calendar days away. At T-2 it is available to all users when no active booking exists. Administrative closures, bay exceptions and conflicts take precedence. This is calculated per request; no job is required for correctness. Sessions come from `/api/shooting-range/sessions`; the client displays them in the configured field timezone and submits expected UTC boundaries to detect stale selections.

## Data and concurrency

Azure SQL Database is the production persistence store for all Range data, including its independent accounts and sessions. EF Core maps Range-owned data to `shooting_range_configs`, `shooting_range_bays`, `shooting_range_mechanism_types`, `shooting_range_exceptions`, `shooting_range_users`, `shooting_range_bookings`, `shooting_range_friendships`, `shooting_range_threads`, `shooting_range_local_sessions` and the singleton `shooting_range_mutation_guards` row. Booking attendance is stored on the individual booking as an optional no-show date plus a false-by-default marker. Tenant cancellation deadline and advisory rules are stored in the existing configuration row; rule values use its JSON text mapping. Friendships are one tenant-owned row per canonically ordered, unique pair with tenant-qualified restrictive foreign keys to both users; the required requester and status distinguish pending consent from accepted friendship. The schema permits only Pending and Accepted states, and the requester must be one of the canonical pair members. Mechanism types are immutable, administrator-created catalog entries; a bay stores its canonical mechanism name and quantity in the existing JSON list. Bay Pepper inventory is a nonnegative integer. Existing nested value lists/messages are stored as JSON text in `nvarchar(max)` columns; primary business records remain separate relational rows. UTC instants use `datetime2` with an EF conversion that restores `DateTimeKind.Utc`; local `CreationDateTime` uses `datetime2` without conversion. The initial schema and later changes are versioned EF migrations. Run the API assembly with `--migrate-database` as an explicit operator action; normal SQL startup never creates/upgrades the schema and fails closed unless all migrations and the guard row are present.

The mechanism-catalogue migration validates existing bay mechanism JSON and
quantities, rejects ambiguous names and repeated types within one bay, and
backfills distinct valid names before changing Pepper from `bit` to `int`.
The reverse migration refuses to discard Pepper counts above one. Coordinate
the schema, API and client rollout because the bay JSON Pepper value changes
from boolean to number; normal API startup still requires the explicit
`--migrate-database` operation before the new version is deployed.

The `FeatureManagement:EnableInMemoryDb` flag selects EF Core's in-memory provider for Development or Test only; it is enabled in `appsettings.Development.json` and disabled by default elsewhere. It uses the SQL repository implementation with a process-local write gate, but has no SQL transaction, cross-process concurrency, or persistence guarantees and must not be used for production data. When enabled it takes precedence over the legacy `EnableMock` repository flag. The in-memory provider automatically initializes sample mechanism types, bays, fictional users, bookings and closures for demonstrations and tests; these records are process-local and reset with the database/process. It does not seed SQL or the separate mock repository. The mock repository remains available in Development/Test when the in-memory database flag is off.

Every repository write participates in a SQL Server `ReadCommitted` transaction and acquires the singleton mutation-guard row using `UPDLOCK, ROWLOCK` before reading validation/admission state. The lock has a 15-second deadline. Every Range writer, including account, session, schedule, bay, closure, booking, and message changes, must use this guard. This serializes writes globally by design for the POC; no process-local lock is used for correctness. Cookie issuance follows successful transaction commit. The readiness check is read-only and verifies database connectivity, schema and guard row.

The friendship-consent schema is part of the initial friendship-table migration; there is no legacy-pair state or backfill. The user confirmed that no database or historical friendship rows exist. Its down migration stops while any friendship row remains rather than silently dropping relationships. The migration has not been applied to a target database; verify target state before any operator applies it.

Pending and Approved consume capacity through half-open persisted UTC intervals per bay, regardless of period key or current mode. Adjacent intervals and different bays are allowed. The only overlap exception is an exact same bay and UTC interval shared by distinct users, where each joining requester is an accepted direct friend of at least one active participant; there is no application-defined participant cap. Pending reservations may be joined; each participant retains an independent Pending/Approved decision. Partial overlaps remain conflicts. Friendship removal does not cancel existing reservations. Generic booking insert/replace paths enforce the same rule; inactive-to-active transitions recheck admission, while Pending-to-Approved preserves the existing reservation. Historical intervals and booking identity cannot be rewritten. Malformed active intervals block the affected bay until operational reconciliation. A unique normalized-username index with binary collation is a database backstop. The mock repository serializes mutations and restores its snapshot on failure; mock tests do not prove SQL Server locking or transaction rollback.

Message threads persist a numeric `ShootingRangeThreadStatus`: Pending (`0`),
InProgress (`1`), Completed (`2`) or Refused (`3`). The SQL migration defaults
existing rows and inserts from an older API version to Pending. Administrators
may select any of the three decision states and correct a prior decision;
Pending is not a decision. Both status decisions and reply append operations
read and replace the thread inside the existing guarded write transaction, so
concurrent writes preserve the latest messages and status.

All account passwords are PBKDF2 hashes with per-account salts. New passwords must contain at least seven characters. Passwords must never be logged or returned. Only Approved users can authenticate. Admin reset marks `ForcePasswordChange`; the new password must be communicated out of band.

The first production administrator is inserted manually into `shooting_range_users`; there is no public or administrative bootstrap endpoint. The inserted row must use the normalized username and the password representation produced by the current password-hashing implementation. In Development/Test only, the EF in-memory provider can seed a local administrator when `ShootingRange:InMemoryAdminPassword` is supplied through environment-managed configuration. That account requires a password change at first login. No password or reusable hash is stored in source or documentation.

Ordinary-user onboarding is not yet a final product decision. Admin-created users are the working assumption because it keeps account creation behind authorization; public self-registration is not part of the approved public-release posture.

## Security policy

Authentication uses an HttpOnly, Secure, SameSite=None cookie with an eight-hour, non-sliding lifetime and a server-side revocable session. A deny-by-default fallback authorization policy protects every Shooting Range domain endpoint, including availability; admin routes require the `admin` role from revalidated account state. Public registration has been removed. Ordinary-user onboarding remains an operational/product decision and must be provided through a tenant-aware trusted process.

In the target policy, only these technical entry points remain anonymous:

- Login, because authentication cannot otherwise begin.
- CSRF token acquisition, because login and later unsafe cookie requests need a token.
- Liveness and readiness probes.

Login itself and unsafe cookie-authenticated requests require the `X-CSRF-TOKEN` token returned by `/api/shooting-range/csrf`. Acquire it before login and refresh it after successful login or password change. The API processes one `X-Forwarded-Proto` hop from the Azure App Service ingress before antiforgery and cookie middleware because TLS terminates at that ingress; it does not use forwarded host or client-IP headers. Keep the API behind the platform ingress, which must overwrite this header; do not expose the app directly or place it behind an untrusted proxy. Login is limited to 10 attempts per five minutes per remote IP per process; scaled deployments require reviewed gateway/distributed throttling and trusted proxy configuration. Credentialed CORS is resolved from the active tenant registry and permits only its exact HTTPS origin; the selected request tenant must match the origin's tenant.

The authentication/session tenant binding and management key are independent of CORS; neither the tenant slug nor an allowed origin grants access to domain data. SQL Server migration and target-environment verification remain operational gates before production rollout.

Every cookie request revalidates the session's expiry/revocation/security version and current Approved account status and role, including mine/messages. Disabled accounts are rejected; demotion removes administrator access immediately. Logout revokes the current session. Password reset/change rotates `SecurityVersion`, invalidating older sessions; successful self-change revokes and renews the current session. Forced-change sessions permit only session inspection, password change, logout and technical CSRF acquisition. Old cookies without a session claim require reauthentication. The client restores sessions on refresh/direct navigation, handles expiry/retry, and supports logout without persisting credentials or redesigning the existing UI. A 401 from anonymous session inspection is an expected signed-out state, not an expiry event; a protected-request 401 while signed in triggers the idempotent expiry transition. Failed login leaves the form available with generic credential feedback, while successful login opens booking or the existing forced-password-change screen. See the [login-loop handoff](./feature-handoffs/shooting-range-login-loop-handoff.md) for implementation validation and the remaining deployed-browser evidence gate.

Booking admission also rechecks current approved account status and `ForcePasswordChange`; touched administrator operations recheck current administrator state. Admin-user responses explicitly preserve `Id`, `CreationDateTime`, username, names, status, administrator and forced-password flags, excluding `PasswordHash`. Safe projections for other persistence entities remain a separate cutover requirement. The API remains the authoritative admission boundary.

Members may cancel their own Pending or Approved reservations through the inclusive field-local date deadline configured for their tenant (zero days permits cancellation throughout the reservation date). The guarded write rechecks current account, ownership, status, configuration and date; it clears any same-day attendance mark when the member cancels. Administrators may cancel Pending or Approved reservations on any date through a separate guarded, antiforgery-protected action. When the configured deadline is positive, an Approved admin cancellation requires an explicit no-show choice; a cancellation no-show is dated to the field-local cancellation date. Existing marks must be explicitly replaced or cleared. Administrators may restore an unmarked cancelled future reservation through the existing guarded approval and capacity checks. Members receive safe cancellation eligibility guidance but no attendance fields.

Normal admin booking review remains yesterday-inclusive and retains its pending count. A separate exact-field-local-date historical query permits attendance correction on arbitrary past dates. A separate admin report pages one selected user's full history/future bookings with status and all/unmarked/no-show filters. Both reads are tenant-scoped and bounded in SQL. Configurable no-show rules are advisory only: they count each marked booking once in inclusive field-local date windows, show every matching rule, and never prevent booking or admin decisions. The policy and attendance/report operations use the existing global mutation guard for writes; no cache or background job is introduced.

## Booking and friendship workflows

Members confirm the selected local date, time interval and bay before sending
a Pending reservation. The confirmation dialog accepts an optional request
note (up to 1,000 characters); notes are visible to administrators, not
friends. An approved member may send an exact-username friendship request to
another approved same-tenant member. The recipient must accept before the
pair is returned as friends or grants access to shared-slot availability.
Incoming and outgoing requests are listed separately; either member may
remove an accepted relationship. There is no general member directory.
Availability returns only bay-specific exact-interval reservations from the
current member's accepted direct friends; it reveals no unrelated
reservations or friend notes.

Administrators see future/current requests grouped by field-local date, bay
and interval, with each participant's status and note. They may create a
reservation for an approved member; the API inserts it as Approved in the same
guarded write that rechecks current administrator/target status, time bounds,
closures, whitelist, tenant, friendship and capacity. The admin interface
continues to show the yesterday-inclusive list range.

## Minimal UI scope

The initial public-facing POC is limited to:

- Login, session restoration, forced password change, and logout.
- Authenticated availability search, confirmation with optional notes, booking
  and cancellation.
- Booking dates use the field configuration's opening weekdays; closed weekdays are disabled in the calendar, and available sessions render only after the sessions response resolves.
- The current user's bookings.
- Accepted-friend management and exact-slot requests to join a slot occupied
  by an accepted direct friend; each join remains Pending until individually
  approved.
- Member cancellation of Pending or Approved bookings through the tenant-configured field-local deadline, with the cancelled record retained in history.
- Administrator booking decisions on the dedicated `/admin/bookings` page, linked in the top navigation with an accessible count badge for future pending requests. The client refreshes this count on navigation/focus, after relevant booking changes and every 60 seconds while visible; this is not push notification.
- Member cancellation eligibility and deadline guidance, tenant policy editing, explicit admin cancellation/no-show choices, individual attendance correction, advisory alert retry, and field-local historical paging.
- The administrator `/admin/reports` page with selected-user and attendance filters, bounded paging and explicit state handling.

Schedule, bay, mechanism-type catalogue, closure management, grouped booking
review and direct approved booking are accepted administrator scope. In Mixed
mode the administrator selects a 60- or 120-minute increment. Bays have
nonnegative integer target, partition, plate and popper counts; optional
height and width are unit-free positive decimal values, entered to two decimal
places and supplied together. The client preserves `/`, `/messages` and
`/admin` and adds `/admin/bookings`, `/admin/reports` and `/friends`; this does
not authorize unrelated messaging expansion. Rescheduling, push notifications, waitlists,
arbitrary-duration sessions and notifications remain deferred. Reporting is
limited to the administrator booking report described above.

The existing `/messages` page displays request text and each message's sent
date. Users see their request status; administrators also see the sender and
may set it to InProgress, Completed or Refused, including correcting an earlier
decision. Status is visible as text and unknown values are not represented as
Pending. This is limited to the existing thread workflow; reply composition,
status notifications and other messaging expansion remain deferred.

## Appearance preference

The client uses the shared `@morwalpiz/layout` System/Light/Dark appearance
contract and opt-in theme stylesheet. System is the default; explicit choices
are stored per browser origin and do not alter Range authentication, booking,
or account state. The initial HTML bootstrap applies the resolved theme before
the client starts, while the React selector appears on the login and
authenticated shell. Range-specific status roles map to shared semantic tokens.

The consent cleanup passed the full Range API test project (**94 passed, 9
SQL Server tests skipped**); EF reports no pending model changes, and
generated forward/down SQL confirms one friendship-table creation, no row
backfill and a guarded table drop. The full client suite passed (84 tests);
TypeScript checking and production/SSR builds passed. Disposable SQL migration
and concurrency evidence, authenticated browser rendering, deployed
content-security policy and PWA/offline behavior remain unverified. Independent
review found no Critical/Major issue; its verified minor mock lookup finding
was fixed. Review details are tracked in the
[consent cleanup handoff](feature-handoffs/shooting-range-05-consent-cleanup-and-review-handoff.md).

## Future extension candidates

Reassess these only after real POC use establishes operational value:

1. Rescheduling with an explicit policy.
2. Further administration workflows beyond the accepted schedule, bay and closure controls.
3. Messaging capabilities beyond the existing thread view, replies and
   administrator status decisions.
4. Push notifications and waitlists.
5. Custom-duration sessions.
6. Attendance and utilization reporting.
7. Invite-based onboarding if administrator-created accounts become burdensome.

## Deployment and rollback

Development defaults to the EF Core in-memory provider through `FeatureManagement:EnableInMemoryDb`; this flag is allowed only in Development or Test and must remain disabled in production. The legacy mock repository remains available in Development/Test when the in-memory database flag is off; production rejects either non-SQL mode. Azure SQL uses external configuration key `ShootingRangeDatabase:ConnectionString`; when Key Vault is enabled, provide `ShootingRangeDatabase--ConnectionString` and grant the App Service managed identity read access. Prefer Azure SQL Microsoft Entra managed identity authentication where provisioned; otherwise keep the SQL credential in Key Vault. Require encryption and certificate validation for deployed connections. The API fails startup when the configured vault is unavailable or the required key is missing, and does not fall back to a direct connection value. A separately managed `ShootingRangeDatabase:MigrationConnectionString` may be supplied for explicit schema migration; it must not be used by normal runtime. Keep all connection values outside source, documentation and deployment workflows.

Set the client App Service's `SHOOTING_RANGE` value separately for each deployment. Set `ShootingRange__ManagementKey` only on the API App Service and configure the exact client origin when provisioning each tenant. Apply `20261120233255_AddShootingRangeTenancy` explicitly with the API's migration command before deploying the new runtime. The user confirmed there is no existing data to preserve; nevertheless, verify the target database contains no business rows before applying the migration. Its required tenant ownership constraints intentionally prevent silently assigning existing rows to a tenant.

Before deploying booking-feedback, back up SQL and verify the additive
`20261121171929_AddShootingRangeFriendships` migration against a disposable
SQL Server. Apply it explicitly before the compatible API. Drain old API
writers before deploying the client or enabling Mixed mode: old versions do
not recognize `SessionMode=3`, 120-minute increments, or shared exact-slot
admission. Do not roll back to an old writer while shared active bookings
exist; pause writes and use a compatible API or an explicitly approved
recovery. No migration or deployment has been run for this change.

The additive `20261123110721_AddShootingRangeAttendanceAndPolicy` migration
adds unmarked/no-date defaults for existing bookings, empty rules and a
zero-day cancellation deadline, plus tenant/date indexes for attendance,
history and user reports. It has not been applied or exercised against SQL.
Back up and rehearse it against a disposable SQL Server, apply it explicitly
before the compatible API, then deploy the compatible SPA. Do not roll back
its down migration after production attendance or policy writes without
explicitly accepting data loss and verifying recovery.

The API and client continue to deploy independently through workflows bound to the GitHub `production` environment. Before public exposure, validate the authorization matrix, configured tenant origins, CSRF/login flow, persistent Data Protection keys, Azure SQL readiness, tenant-scoped uniqueness and relationships, booking concurrency, backup/restore, and successful login by the manually inserted tenant-owned administrator.

Mock repositories are allowed only in Development or Test; enabling `FeatureManagement:UseMock` in Production or Staging fails startup before storage resolution. Shooting Range API host tests explicitly enable the in-memory database, while the separate SQL Server tests exercise provider-specific behavior. Azure SQL startup verifies connectivity, no pending schema migrations and the provisioned mutation guard row; it does not apply schema changes or replace target-environment transaction and backup/restore evidence. EF migrations create the unique `ux_shooting_range_users_normalized_username` index using `Latin1_General_100_BIN2`. Session expiry is checked during authorization; no TTL cleanup is required.

Real SQL Server tests use `SHOOTING_RANGE_TEST_SQL_HOST`, `SHOOTING_RANGE_TEST_SQL_PORT`, `SHOOTING_RANGE_TEST_SQL_DATABASE`, `SHOOTING_RANGE_TEST_SQL_USERNAME`, and optionally `SHOOTING_RANGE_TEST_SQL_PASSWORD` for an explicitly disposable server with permission to create/drop uniquely named test databases. Missing host configuration skips only those tests. The API deployment workflow provisions a temporary SQL Server 2022 container with a per-run generated password and requires nonzero selected and executed integration tests before publishing. Never point integration tests at production or shared customer data. CI test success does not replace Azure SQL backup/restore, migration and production approval evidence.

Browser verification must cover the exact allowed origin, denied lookalikes, cookie/CSRF flow, desktop/mobile controls, SSR and direct navigation. Missing target Azure SQL backup/restore, existing-data migration or browser evidence blocks release, not authoring; no deployment is implied by local checks.

Use R1-R8 and R14 in the existing [release evidence checklist](architecture/operations/phase5-activation-and-recovery.md#release-evidence-checklist-2026-10-01) for named owners, exact environment/UTC timestamps, redacted artifacts, independent approval and compatible recovery actions. All absent target proofs are BLOCKED. Slice 10 runbook authoring is complete; operational release closure is not. Ordinary-user onboarding remains a product/operations requirement; anonymous registration is removed and not a fallback.

Rollback before Azure SQL writes may use the old Mongo deployment only if its actual Cosmos transaction prerequisites have been proven; do not assume that it works. After new SQL writes, restore a compatible Azure SQL-backed API/database or use a separately tested and approved reverse import. Never restart an old Mongo writer against stale data. If no compatible recovery exists, suspend mutations and preserve both source and target data. Never restore permissive CORS, password-hash responses or production mock mode.
