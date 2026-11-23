# ADR-016: Authorization-First Public Posture For The Shooting Range POC

- **Status:** Accepted
- **Date:** 2026-09-11

## Context

`MorWalPizVideo.ShootingRange` and `frontend/shooting-range.client` form an independent booking proof of concept. They are not publicly exposed yet but are expected to be soon, and their deployment workflows remain bound to the GitHub `production` environment.

At the time this ADR was accepted, source allowed anonymous domain reads/registration and lacked a fallback authorization policy. The POC must remain simple without making public exposure unsafe or coupling it to the main publishing architecture. The implementation status below records the later authorization and tenancy changes; target-environment release evidence remains separate.

## Decision

Apply deny-by-default authorization to every Shooting Range domain endpoint. The only anonymous technical exceptions are login, CSRF token acquisition, and liveness/readiness probes. Availability requires authentication, and administrator operations additionally require the `admin` role.

The first administrator is inserted manually into the Shooting Range Azure SQL database using the current password-hashing implementation. No bootstrap-admin endpoint is introduced, and credentials or reusable password hashes are never stored in source or documentation.

Public self-registration is not part of the approved release posture. Administrator-created ordinary users are the working assumption, but ordinary-user onboarding remains a product decision that may be revised without changing the service boundary.

Keep models, contracts, services, repositories, authentication, and API transport locally owned by the Shooting Range projects. The Range client may consume `@morwalpiz/layout` for shared presentation components and align its visual system with Shooter Workshop. This supersedes the earlier prohibition on shared frontend packages: that restriction reflected the former layout implementation, while the consolidated layout and theme now provide the intended common presentation layer. Do not import Workshop application source, account/authentication providers, API services, or domain models. The UI remains limited to authentication and session restoration, forced password change, logout, availability and booking, the current user's bookings, and the existing minimal administrator workflows.

Production exposure is gated on explicit credentialed CORS, safe response DTOs, account-state revalidation, login throttling, Azure SQL uniqueness and readiness, authoritative booking validation, production rejection of mock mode, persistent Data Protection where required, and executable security/integrity tests.

## Alternatives

- Keep availability and registration anonymous: rejected because all domain functionality must be authorization-protected before public exposure.
- Add a public bootstrap-admin endpoint: rejected because the first administrator will be inserted manually.
- Complete every existing API workflow in the UI: rejected because the POC is intentionally minimal and observed use should drive expansion.
- Move Shooting Range backend/domain behavior or contracts into shared projects: rejected because no shared runtime consumer exists.
- Reuse the consolidated shared presentation package: accepted for UI components and consistent styling; it does not change Range ownership of authentication, transport, or domain behavior.

## Consequences

The implemented foundation protects login with pre-login CSRF and per-IP throttling (10 attempts per five minutes per process), refreshes CSRF after identity changes, and uses revocable eight-hour non-sliding sessions. Logout revokes the current session; password reset/change invalidates older sessions, while successful self-change renews the current session after the database transaction commits. Disabled accounts and demoted roles are enforced on each cookie request. Forced-change sessions permit only session inspection, password change, logout and technical CSRF acquisition. Production/Staging reject mock storage; Azure SQL readiness requires an applied schema and guard row, while an explicit migration command applies schema changes.

The deny-by-default fallback authorization policy is implemented and public registration has been removed. Ordinary-user onboarding remains an operational/product decision; tenant provisioning does not create an administrator. Do not interpret local implementation or tests as approval for public exposure or as evidence that all response DTOs have been reduced to safe projections.

Login, CSRF token acquisition, and health probes remain anonymous by necessity; this is not an exception for domain data. Direct links and browser refreshes require session restoration and protected client routing.

Schedule, bay and closure administration and booking decisions are accepted scope. Four modes preserve `Periods=0`, `Hourly=1`, and `HourlyContinuous=2`, adding opt-in `Mixed=3` for whole morning/afternoon or contiguous 60/120-minute increments. User booking requests include an optional note and remain Pending; administrators see requests grouped by field-local date, bay and interval, and can also create a direct Approved booking for an approved member. Friendship requests are sent by exact username and become reciprocal only after the recipient accepts; incoming and outgoing requests remain distinct, and accepted relationships can be removed. A member may request to join an exact bay/UTC interval held by an accepted direct friend; there is no application-defined participant cap, while every member booking still requires its own admin decision. Partial overlaps and non-friend sharing remain conflicts. Friendship removal does not cancel existing bookings. User cancellation is supported for a user's own future Pending or Approved booking, retaining its Cancelled record; administrators may restore future cancelled bookings only through guarded approval and normal admission. Booking lists are filtered from yesterday onward in field-local dates. The dedicated admin booking page shows an accessible badge for future Pending bookings, refreshed while visible; this is not push. Bays also store nonnegative integer targets and optional paired positive height/width numbers, unit-free and precise to two decimal places. The existing message page shows text, sent dates and request status; administrators also see the sender and can set or correct status. Broader messaging expansion, rescheduling, push notifications, waitlists and arbitrary-duration sessions remain deferred. Reporting was deferred at this decision point and is superseded by the bounded booking-report amendment below.

### Booking workflow amendment — 2026-11-20

The client loads availability automatically after the date and offered session
are selected, and refreshes it when either changes. User cancellation follows
the future-only, owner-only guarded policy above. An additive SQL migration
adds target count and nullable dimensions; existing rows receive zero targets
and unspecified dimensions. The API requires new bay fields on full-replacement
requests so stale clients fail safely rather than erase new inventory data.

All local repository writes participate in a field-scoped SQL Server transaction guard using `UPDLOCK, ROWLOCK`; Pending/Approved capacity is checked using stored UTC overlap, not period-key uniqueness. All checks that authorize a mutation are performed inside the guarded transaction. The schema uses explicit EF Core migrations; startup never creates or upgrades tables. Disposable SQL Server independent-writer races, Azure SQL backup/restore, existing Range data migration and browser CORS/CSRF checks remain release gates; local mock tests cannot satisfy them.

Manual administrator insertion remains an operational responsibility. The procedure must use the current normalized username and password-hashing representation without recording secrets.

### Frontend presentation amendment — 2026-11-18

The original frontend isolation constraint is superseded. The Range client now uses the consolidated `@morwalpiz/layout` presentation layer where its components fit, with Range-local styling and workflows. This amendment does not make Workshop authentication, services, models, or application code shared, and it does not complete or alter the separate public-release authorization/onboarding decision.

### Message status amendment — 2026-11-20

The existing `/messages` page may display each thread's messages and sent
dates, and show its status to the owner. Administrators may see the sender and
set the status to InProgress, Completed or Refused; an administrator may
correct any earlier decision. Pending (`0`) is the default for existing and
new threads, not an administrator decision. The Range-local API enforces the
administrator role, current approved account and CSRF validation. Both status
changes and replies perform their thread read-modify-write inside the existing
repository mutation guard. An additive SQL migration preserves all existing
thread and message data and defaults old rows to Pending. This does not add
reply composition, notifications, or other deferred messaging capabilities.

### Multi-tenant amendment — 2026-11-20

The Range API and SQL database serve multiple shooting ranges. Each client
deployment sets the public `SHOOTING_RANGE` runtime slug and sends it as
`X-Shooting-Range` on every API request. The API resolves the slug before
authentication, binds the internal tenant ID to the request, and enforces
tenant-scoped EF query filters, server-assigned ownership, tenant-qualified
uniqueness/relationships, and tenant-bound cookie and server-side session
claims. CORS resolves exact origins from the active tenant registry and
requires actual requests to select the tenant bound to that origin.

`POST /api/shooting-range/management/tenants` is protected by the independent
`X-Shooting-Range-Management-Key` header and the API-only
`ShootingRange:ManagementKey` setting. Provisioning creates a tenant and its
initial configuration; it does not create an administrator or accept the key
from a client deployment. Swagger exposes the management scheme in Development
or when `ShootingRange:EnableSwagger` is enabled. Keep production ingress,
secret rotation and Swagger access under operator control.

The explicit `20261120233255_AddShootingRangeTenancy` migration adds the
tenant registry and ownership constraints without historical data backfill.
The user confirmed that no existing data needs preservation. Verify the target
database is empty before applying the migration; do not silently assign any
existing records to a tenant. The migration has not been applied to the
production database. Tests cover tenant selection, provisioning, session
mismatch, tenant-scoped uniqueness and foreign-ID isolation. The local
Shooting Range test run passed 81 tests; eight SQL Server integration tests
were skipped because no disposable SQL host was configured. This is not SQL
Server/Azure migration or concurrency evidence.

### Booking feedback amendment — 2026-11-21

The member UI confirms date, local interval and bay before submission and
accepts an optional 1,000-character reservation note. New `Mixed=3` schedule
mode preserves existing numeric modes; the administrator configures a 60- or
120-minute increment, while members may select a full morning/afternoon or a
contiguous run of complete increments. UTC interval resolution and expected
bounds remain authoritative. Continuous-hour mode and old stored booking
intervals are unchanged.

The original immediate-friendship and two-person limit described in this
amendment were superseded by the friendship-consent amendment below.

The admin page groups by field-local date and bay/interval while preserving
each participant's status and note. A new antiforgery-protected admin action
inserts a direct reservation as Approved inside the existing SQL mutation
guard, rechecking administrator and target status, schedule, closure,
whitelist, tenant and capacity.

The additive `20261121171929_AddShootingRangeFriendships` migration has been
generated but not applied. The original implementation's test results are
historical; see the friendship-consent amendment below for current validation
and release gates.

### Friendship consent amendment — 2026-11-21

Friendship requests do not grant friendship or shared-slot visibility until
the recipient accepts. The requester may cancel; the recipient may decline;
either participant may remove an accepted relationship. Old `POST /friends`
clients receive `410 Gone`. The initial
`20261121171929_AddShootingRangeFriendships` migration creates the final
consent schema: requester is required, only Pending/Accepted states are
allowed, and the requester must belong to the canonical pair. There is no
legacy-unverified state or existing-pair backfill. The user confirmed that no
database or historical friendship rows exist. The migration's down path
refuses to drop the friendship table while any row remains.

An accepted direct friend of any active participant may join an exact same
bay and UTC interval. There is no application-defined participant cap; all
participants retain separate Pending/Approved decisions. Partial overlaps,
duplicate participants and joins without an accepted direct friend remain
conflicts. Availability, SQL/mock generic write validation, and UI participant
lists follow this rule.

The local Range test project passed 93 tests; 9 SQL Server tests were skipped
without `SHOOTING_RANGE_TEST_SQL_HOST`. EF reported no pending model changes.
Focused client tests passed (65), App tests passed on rerun (16), and
TypeScript checking plus client and SSR builds passed. The migration has not
been applied or rehearsed against SQL; independent SQL concurrency and
authenticated browser checks remain rollout gates.

### Attendance, cancellation and reporting amendment — 2026-11-23

Each booking now stores a false-by-default no-show marker and an optional
occurrence date. Ordinary attendance marking is admin-only and applies to
Approved bookings on or before the current field-local date. A separate admin
cancellation action may cancel Pending or Approved bookings on any date.
When the tenant deadline is positive, cancelling an Approved booking requires
an explicit no-show choice; any resulting occurrence is dated to the actual
field-local cancellation date. Existing marks require an explicit correction
or removal. These writes remain in the tenant-scoped mutation guard.

Members may cancel their own Pending/Approved bookings through a configurable
inclusive field-local calendar-day deadline, defaulting to zero. Eligibility
is rechecked inside the guarded mutation. Member booking responses expose
cancellation guidance but never attendance data. The separate admin policy
contract stores the deadline and up to 20 advisory rules in tenant config;
legacy full-config writes preserve these values. Matching rules never block
booking or decisions.

The admin booking page retains recent decision work and adds independent
field-local history paging, attendance correction, cancellation confirmation
and retryable advisory alerts. `/admin/reports` is restricted to one selected
user and provides all/unmarked/no-show filters with deterministic paging.
History, reports and alert reads are tenant-filtered and bounded in repository
queries. The additive
`20261123110721_AddShootingRangeAttendanceAndPolicy` migration adds safe
defaults and supporting indexes; it has not been applied to a target database.

Local validation passed: 100 backend tests and 90 frontend tests; 9 SQL Server
integration tests skipped because no disposable SQL host was configured.
TypeScript checking, production and SSR builds, migration SQL generation/
inspection, and EF pending-model-change validation passed. No SQL migration
execution, authenticated browser capture, deployment, or backup/restore
rehearsal was performed. These remain release evidence gates.

## Migration And Rollback

Old cookies without the session tenant claim require reauthentication. Sessions are persisted in the Azure SQL `shooting_range_local_sessions` table; account `security_version`, `normalized_username` and tenant ownership are required, and unique indexes use tenant-qualified binary-collated keys. Before migration, confirm that the target database is empty, pause traffic, drain old writers, apply the explicit EF migration and deploy the compatible API before clients. Do not run old API writers against the tenant-aware schema.

For the message status migration, back up SQL, pause message writes, drain old
API instances, apply the explicit EF migration and deploy the guarded API
everywhere before deploying the client. Old API instances must not coexist as
writers because their reply path does not guard the preceding read. Retain the
status column when rolling back after decisions have been recorded; an old
client cannot show or change those values. Application rollback must use a
compatible guarded version; otherwise suspend mutations. Never restore
unrestricted credentialed CORS, password-hash responses, or production mock
repositories. See the Shooting Range architecture procedure for data preflight
and unresolved public-release controls.

## Validation

These are full release acceptance criteria, not a claim that every item passes in the current foundation. Local API/client validation has passed, but target Azure SQL migration, backup/restore, real-browser origin/session switching and Data Protection evidence remain required separately from local executable tests. Nine SQL Server tests were skipped without `SHOOTING_RANGE_TEST_SQL_HOST`.

- Anonymous domain requests return `401`; normal users receive `403` from administrator operations.
- Login, CSRF token acquisition, and health probes remain reachable anonymously.
- Missing or forged CSRF tokens fail for unsafe cookie-authenticated requests.
- Only the configured production client origin receives credentialed CORS responses.
- Disabled, demoted, and forced-password-change accounts are enforced from current server state.
- Responses never contain password hashes, whitelist identifiers, or persistence-only metadata.
- Duplicate normalized usernames and conflicting active bookings fail at the database boundary.
- Invalid dates, periods, closures, unavailable bays, and reservation restrictions fail during booking creation.
- A correctly inserted approved administrator can log in and perform the minimal administrator workflow.