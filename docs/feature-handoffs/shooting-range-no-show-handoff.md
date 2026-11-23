# Shooting Range: attendance, cancellation and reporting

**Status:** Implemented locally; the additive SQL migration and deployed
browser/production verification remain pending. This handoff supersedes the
earlier single-rule and late-only cancellation proposals.

## Outcome

Provide the Range administrator with per-reservation no-show marking, multiple
configurable advisory levels, cancellation controls and a user reservation
report. Each booking defaults to unmarked/show. No-show advisories inform the
administrator but never block booking, approval or rejection.

**Confirmed decisions**

- Normal attendance marking applies only to Approved reservations; already
  removed/aborted reservations cannot subsequently be newly marked. An
  explicit no-show choice **during admin cancellation** is the exception:
  the Approved reservation becomes Cancelled and remains recorded as no-show.
- Multiple configurable rules may produce different alerts, for example after
  five occurrences and after ten.
- Today is eligible for marking throughout the field-local day, including
  before the reservation starts; previous days are also eligible.
- Members have a configurable date-based cancellation deadline.
  Administrators may cancel without that deadline. When the configured
  limit is at least one day, the admin cancellation modal asks whether to
  record a no-show, even when the booking date is in the future. With a
  zero-day limit, cancellation is recorded as a regular cancellation.
- All reservation-limit calculations use field-local dates, not hours.
- A no-show chosen during cancellation is attributed to the actual
  field-local date of cancellation, even when the reservation is for a
  future date.
- Administrators need a report filtered by user and show/no-show/all.

**Working assumptions for review**

- “Registration confirm page” is the existing reservation approval page, not
  account registration. One marked booking contributes one occurrence, even
  if a user has several bookings on the same day.
- Each alert rule has its own occurrence threshold, days and administrator-
  supplied short alert label. All matching rules are shown, so no severity
  ordering or hidden winner is needed. Rules are optional.
- Deadline days are field-local calendar days. With a setting of 2, the member
  may cancel through the whole local date two days before the reservation;
  starting the following local date the member must contact the owner. A
  setting of 0 permits cancellation throughout the reservation's local date,
  even after its start time, but not on subsequent dates. Ownership and
  active-status checks remain. This intentionally changes the existing
  future-start cutoff.
- “Remove” means set the existing Cancelled status, not physically delete
  booking history. Pending and Approved bookings can be cancelled by an admin;
  already Cancelled or Rejected bookings are not cancelled again.
- Only an Approved booking can receive a cancellation no-show; a Pending
  cancellation is regular, with the modal explaining why no-show is
  unavailable. Existing no-show marks may be explicitly cleared when
  correcting a mistake but cannot be newly added after cancellation.
- A no-show marked for an existing Approved reservation uses the reservation
  date. A cancellation no-show uses the actual local cancellation date. Each
  occurrence has one persisted date; one booking counts at most once.
- The report includes all reservation statuses. “Show” means **not flagged**
  rather than evidence of physical attendance; identify Cancelled, Rejected
  and Pending statuses explicitly so they are not represented as attendance
  facts. A Cancelled booking can be flagged only by the admin's cancellation
  choice, not by a later attendance operation.

## Current Evidence

- [Range architecture](../shooting-range-architecture.md) and
  [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md) describe
  the independently owned Range API/client, tenant isolation, SQL migration,
  authentication and booking workflows. Source is authoritative.
- [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs)
  has independent per-user bookings with immutable local dates and Pending,
  Approved, Rejected and Cancelled states; no attendance flag or deadline.
- [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs)
  cancels a member's own future Pending/Approved booking under the guarded
  transaction. Admin decisions can approve, reject or restore, but there is no
  separate administrator cancellation action. Member/admin booking lists show
  yesterday onward, not the complete history.
- [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs)
  returns booking entities to members and accepts full-replacement admin
  config. [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts)
  reconstructs the existing config payload, so old clients could erase new
  settings if they are added to that replacement contract without protection.
- [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx)
  lists individual users and booking decisions. Its date selects a direct
  booking session, not a history filter. Its initial date is UTC-based.
  [App.tsx](../../frontend/shooting-range.client/src/App.tsx) currently offers
  member cancellation on future active bookings and confirms it in-browser.
  [routes.tsx](../../frontend/shooting-range.client/src/routes.tsx) has no
  administrative reservation report route.
- [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs)
  maps tenant-scoped SQL bookings and configuration.
  [SqlServerShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs)
  and [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs)
  enforce tenant scope and the guarded write path. The current generic
  repository loads all records; the report requires filtered SQL queries.
  Production startup requires explicitly applied EF migrations.

## Proposed Design

### Attendance and administrative cancellation

- Persist an exceptional no-show marker and its occurrence date on the
  reservation, with false/null for existing and new bookings. Require a date
  exactly when marked. Keep both independent of booking status and private
  to administrator projections; do not expose them on member booking,
  creation, friend or availability responses.
- An admin-only, CSRF-protected attendance operation sets or corrects the
  marker under the existing write guard after current admin, tenant, Approved
  status, and stored local date not later than field-local today are checked.
  The occurrence date is the booking's stored local date. Explicit true/false
  makes retries idempotent; clearing also clears the occurrence date.
  Shared-slot participants have independent controls.
- Add an admin-only guarded cancellation operation, separate from the member
  cancellation endpoint. It reads the actual booking and current policy
  within the transaction, permits Pending/Approved removal on any date, sets
  Cancelled, and never bypasses tenant, authentication or CSRF checks. It
  must not change another participant's reservation.
- With a configured limit of one or more days, the cancellation modal asks
  for an explicit yes/no decision on tracking a no-show for each Approved
  booking, regardless of how far away its date is; do not preselect “yes.”
  A Pending booking still receives a cancellation confirmation but cannot
  be classified as no-show. With a zero-day limit, show a normal cancellation
  confirmation and save a regular Cancelled booking, without no-show.
  Validate the setting and explicit choice on the server inside the guard,
  not only in the SPA. Missing/stale choice, changed policy or status returns
  a conflict and prompts the admin to refresh.
- Commit cancellation, the chosen marker and its occurrence date atomically.
  For a new cancellation no-show, set the occurrence date to the server's
  current field-local date, even for a future reservation. If a previously
  marked Approved booking is cancelled, require the modal to disclose the
  existing mark and its date and explicitly confirm whether to replace it
  with a cancellation no-show dated today; when the policy is zero, disclose
  and confirm its removal before regular cancellation.
  Permit an explicit correction that clears an existing mark on a Cancelled
  booking, but never newly mark an already Cancelled or Rejected booking.
  Do not silently retain or erase a mark.
- Preserve existing future approval/rejection/restore operations. A marked
  booking cannot transition away from Approved through the old decision
  route; correction or the explicit cancellation flow is required. A marked
  Cancelled booking cannot be restored until its mark is explicitly cleared;
  then existing future-start, capacity and admission checks apply. No
  participant's marker, capacity or state may be changed indirectly.

### Member deadline

- Add one tenant-configured nonnegative integer for minimum calendar days
  before the booking. Use a proposed bound of 0–3,650 days; show the meaning
  and inclusive boundary in the settings UI. Existing tenants default to 0.
- In the member cancellation transaction, load current tenant settings and
  calculate field-local today using the repository `TimeProvider` and the
  configured timezone. For an active booking with a non-past local date,
  allow cancellation when the difference between `booking.LocalDate` and
  today is at least the configured number of days. Reject later attempts
  explicitly with guidance to contact the range owner. Remove the existing
  UTC start-time check **for member cancellation only**: at zero days the
  whole reservation date is allowed, even if its session has started. Member
  cancellation is always regular; if an admin already marked that same-day
  booking no-show, clear the marker atomically as part of cancellation, and
  reflect the correction on the next admin read.
- Have the API return a member-safe per-booking cancellation eligibility and
  explanatory deadline in the existing member-history response projection;
  retain all current response fields. The UI offers cancellation only when
  eligible and otherwise explains that the member must contact the owner.
  Eligibility is advisory: the mutation rechecks authoritative current state
  and handles a stale UI or policy change as a visible conflict. Keep the
  existing messages page available; do not invent automatic cancellation
  requests or promise a notification workflow.

### Alert settings and calculations

- Store tenant-scoped rules with threshold, window in days and short label.
  A bounded list in the existing tenant configuration follows the project's
  existing JSON-mapped value-list pattern; no independent service or
  cross-tenant table is necessary. Validate the entire list, unique rule
  identities, nonempty bounded labels, positive integer thresholds and days,
  and a bounded rule count; proposed limits: 20 rules, 1–1,000,000
  occurrences, 1–3,650 days, and 1–80 characters per label. Empty list
  disables alerts. Settings must allow adding, editing and removing rules.
- Use a separate admin-only policy read/write contract for rules and deadline.
  Hide the rules from general configuration JSON; the existing full-replacement
  admin config endpoint must preserve stored policy on old or new clients,
  and the dedicated policy write must preserve all other configuration.
  Both writes use the shared guard. Never introduce a silent default after a
  malformed policy. Handle concurrent stale admin edits explicitly.
- For each rule of `N` days at field-local today `T`, count eligible marked
  occurrences whose persisted occurrence date is in the inclusive range
  `T - (N - 1)` through `T`. Count Approved marked reservations and
  Cancelled reservations marked by the cancellation decision. An alert
  matches at count **greater than or equal to** its threshold. Compute from
  current data; corrections and rule changes affect the next read. A
  cancellation no-show for a future booking counts on its actual cancellation
  date, while a normal attendance no-show counts on its reservation date.
- Fetch admin-only advisory data independently of booking decisions. Use a
  tenant-filtered database query bounded by the maximum configured window
  and aggregate per user/rule without per-row requests; return every matching
  label, count and window. A failure is an explicit unavailable/retry state,
  not zero matches; approval and rejection remain usable. Refresh on page
  load/focus and after relevant changes.

### History and report

- Retain the current recent/future approval list and pending counter. Add an
  admin-only, exact-field-local-date historical read with deterministic
  bounded paging. Default the new selector to field-local today; it is
  independent of the existing direct-booking date/session selector. Its
  per-person attendance controls allow access to arbitrary past dates.
- Add a separate administrator report page reachable from the existing
  navigation. Select one tenant user from the existing admin-only safe user
  list; require a selection before fetching. Provide All, Show/unmarked and
  No-show filters and display the booking date, interval, bay, status and
  attendance indicator and, when marked, the no-show occurrence date. Use all
  historical and future reservations, not the
  recent-list filter. Include explicit empty/loading/error states and bounded
  deterministic paging; show actual status for non-Approved records.
- Implement tenant/user/status/attendance filtering and paging in SQL, not by
  downloading all bookings to the browser or filtering an unbounded
  `GetAllAsync`. Provide equivalent mock/in-memory behavior and a composite
  index selected against the actual query. Return only admin-safe fields.

### UI direction

This is a **refinement**, not a redesign, across Range member cancellation,
admin settings, bookings and the new report; mode **Operate**. There is no
Range-local `PRODUCT.md` or `DESIGN.md` in the checked-in tree; the current
Range UI, Italian copy and shared layout are the incumbent visual authority.
The existing patterns were retained. Impeccable's static detector scanned the
booking component but reports parsed-page checks as unavailable for TSX; its
`harden` command is not exposed by the installed CLI. Desktop/mobile captures,
authenticated browser checks and an end-to-end keyboard/screen-reader pass
remain pending; no visual verification is claimed.

## Impacted Projects

- **MorWalPizVideo.ShootingRange:** Range-local models, contracts, admin and
  member operations, guarded validation, SQL queries and migration.
- **MorWalPizVideo.ShootingRange.Tests:** focused service, host and disposable
  SQL tests, including migration and isolation.
- **frontend/shooting-range.client:** admin settings, booking and report
  surfaces; member cancellation feedback; transport, routes and tests.
- **Deployment:** independently deployed Range API and SPA, with explicit
  Azure SQL migration and coordinated rollout. No other API, shared frontend
  package or background worker is affected.

## Impacted Files

### Modify

- [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs):
  attendance marker and occurrence date, deadline and rule values.
- [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs):
  admin projections, policy, cancellation, report and advisory contracts;
  member-safe cancellation eligibility.
- [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs):
  admin endpoints, protected writes, member projections and compatibility for
  legacy full-config replacement.
- [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs):
  guarded state transitions, timezone/deadline validation, rule calculations.
- [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs),
  [SqlServerShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs),
  [MockShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/MockShootingRangeRepositories.cs),
  [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs):
  bounded tenant-scoped report/history/aggregate reads, mapping and index.
- [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx),
  [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx),
  [App.tsx](../../frontend/shooting-range.client/src/App.tsx),
  [api.ts](../../frontend/shooting-range.client/src/api.ts),
  [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
  [routes.tsx](../../frontend/shooting-range.client/src/routes.tsx), and if
  needed [styles.css](../../frontend/shooting-range.client/src/styles.css):
  deadline, rule editor, per-person actions, report navigation and warnings.
- [ShootingRangeServiceTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs),
  [ShootingRangeApiTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs),
  [Admin.test.tsx](../../frontend/shooting-range.client/src/Admin.test.tsx),
  [routeData.test.ts](../../frontend/shooting-range.client/src/routeData.test.ts)
  and [api.test.ts](../../frontend/shooting-range.client/src/api.test.ts):
  focused service, security, UI and compatibility coverage.

### Add

- [AdminReport.tsx](../../frontend/shooting-range.client/src/AdminReport.tsx):
  Range-local admin report surface, following existing route and test
  conventions. No shared application or package.

### Generated/Migration

An additive Range EF Core SQL Server migration and designer in
[Migrations/](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/),
and updated snapshot. Existing bookings become unmarked; existing policies
have no alert rules and a zero-day cancellation deadline.

### Documentation

Update [shooting-range-architecture.md](../shooting-range-architecture.md),
[ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md) and this
handoff with implemented decisions, files, tests and remaining release
evidence. Do not record secrets.

### Verify Only

[Program.cs](../../MorWalPizVideo.ShootingRange/Program.cs) for auth, CSRF and
migration startup;
[API deployment](../../.github/workflows/deploy-shooting-range-api.yml),
[client deployment](../../.github/workflows/deploy-shooting-range-client.yml),
and [CI](../../.github/workflows/ci.yml) for deployment/test gates. No workflow
change is assumed.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Add guarded admin actions and reports; enforce member cancellation deadline; alerts remain advisory. |
| Frontend/UI | Rule editor, deadline, individual attendance and cancellation, warnings, member guidance and report. |
| Database/Data | Add attendance marker/date, tenant rule list and deadline; add query index and migration. Cancellation records may carry a no-show chosen atomically at cancellation. |
| Contracts/Models | Range-local admin contracts; preserve existing member/admin fields and keep attendance private. |
| Authentication/Authorization | Current approved admin and tenant rechecked; CSRF for unsafe operations; members can only cancel their own eligible bookings. |
| Validation | Rule, date, integer bounds, eligibility, state transitions and page limits enforced server-side. |
| Cache | No new cache; compute from persisted values after each refresh. |
| Configuration/Secrets | Tenant business settings, not deployment secrets; no new secret. |
| Logging/Telemetry | Explicit operational errors without names, attendance details or alert counts in logs. |
| Background Jobs | None; no scheduled transition, notification or automated enforcement. |
| Deployment/Operations | Explicit SQL migration; API before client; disposable SQL and browser release gates. |
| Backward Compatibility | Old list/decision routes and fields remain; old config saves preserve new policy; old member clients receive explicit server conflicts when deadlines apply. |

## Reuse Opportunities

Reuse tenant-scoped repositories/query filters, guarded admin mutations,
`TimeProvider`, field timezone, existing status and booking rows, tenant JSON
value-list mapping, admin user projection, Range API/CSRF helpers, route
loader/action patterns and current test fixtures. Avoid a new shared service,
automatic penalties, separate show records, member-facing attendance data and
unbounded per-booking statistics requests.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | A cancellation no-show may refer to a future booking and Cancelled status, unlike a normal attendance no-show. | Store the actual local cancellation date separately from the booking date; test atomic transitions, correction, restoration, window boundaries and reports on SQL. |
| High | Incorrect attendance may influence human decisions, including same-day marks before start. | Reversible explicit action, date/status and neutral wording; test corrections and confirm final product policy. |
| High | Cross-tenant or member disclosure of attendance statistics. | Safe projections and tenant/admin checks; test foreign IDs, demotion, member responses and report access. |
| High | Old writers or stale config updates erase policy, marks or violate state invariants. | Preserve new settings in existing replacement, guard concurrent edits, drain old API writers before activation; test legacy payloads and mixed versions. |
| Medium | Deadline/window boundary or timezone produces wrong eligibility or count. | Define inclusive field-local date math; deterministic midnight, DST, same-day post-start and boundary tests. |
| Medium | Old member client cannot explain a newly enforced deadline. | Return clear conflict text; deploy compatible client before enabling a nonzero deadline and test stale sessions. |
| Medium | Statistics fail or large report queries degrade booking decisions. | Separate advisory fetch, explicit failure with retry, bounded SQL reports/aggregation and query/index evidence; retain decision controls. |
| Medium | Migration/rollback loses data. | Backup and migration tests; additive schema, controlled deployment and compatible-writer rollback. |
| Low | Cache coherence, external service and jobs. | No cache, external integration or job introduced; verify direct reads after writes. |

## Technical Debt

- Existing admin list and several repository reads materialize many rows;
  avoid broad cleanup, but new report/history/aggregate queries must be
  filtered and paged/aggregated in SQL.
- Booking entities currently appear in member responses. Hide attendance
  fields and use admin-specific projections; broader DTO migration is separate.
- The existing direct-booking date defaults from UTC. Do not change that
  unrelated behavior; new history defaults use field-local today.
- Retention and audit policy for sensitive attendance information is not
  established. Obtain operational/legal review before production; do not add
  unrelated audit or messaging systems in this slice.

## Implementation Roadmap

1. **Decisions confirmed:** the inclusive cutoff and editable alert labels
   were accepted as specified above; all matching rules are shown.
2. **Data/contracts and backend:** implemented additive attendance and
   tenant-policy fields, safe member/admin projections, guarded state
   transitions, current field-local deadline checks, advisory calculations,
   and bounded SQL/mock history and report reads.
3. **Frontend:** implemented the deadline guidance, policy editor, admin
   cancellation confirmation, attendance controls, advisory retry state,
   historical paging and user/filter report while preserving existing direct
   booking and decision flows.
4. **Validation/documentation:** local API and client checks passed; the
   migration SQL was generated and inspected, and the EF model reports no
   pending changes. Disposable SQL execution and authenticated browser/
   responsive evidence remain deployment gates, not completed local checks.
   Architecture, ADR and this handoff record the implementation and residual
   evidence.
5. **Stop:** no automated refusal, account penalties, new notification flow,
   predictive scoring or unrelated refactoring was introduced.

## Testing Strategy

- **Unit/service:** unmarked defaults and correction; only Approved and
  today/past for normal marking; count per booking; multiple rules, different
  windows/labels, threshold equality and overlaps; invalid/duplicate rules.
  Test 2-day cutoff at D-2/D-1, setting 0, same-day pre/post start, timezone
  midnight and DST; same-day after-start cancellation is valid at limit 0.
  Verify admin no time limit, cancellation no-show choice at limits 0/1+,
  future booking cancelled today counts today; occurrence date survives
  retries, and is cleared on correction. Test Pending exclusion, correction
  and restoration.
  Include member cancellation after an earlier same-day admin mark at
  limit 0, verifying the resulting cancellation is unmarked.
- **API/contract/security:** admin and CSRF requirements, current admin
  revocation and forced-change, tenant foreign IDs, owner-only member cancel,
  malformed input, cutoff conflict, stale policy, old config replacement,
  member/friend output privacy, report pagination and no automatic rejection.
- **Component/routes:** accessible settings, multiple alerts, date selection,
  per-person marking/correction, explicit cancellation choice when limit is
  positive and regular cancellation at zero, no silently selected yes,
  member deadline guidance, user/filter report with all statuses,
  empty/loading/error/retry and usable decisions during advisory failure.
- **SQL migration/concurrency:** existing rows/configs retain data with safe
  defaults, tenant-scoped indexed SQL filtering and paging, multiple rules,
  guarded concurrent cancellation/policy/attendance changes, correction/
  restore invariant and rollback behavior. In-memory tests cannot prove SQL
  locks or migration semantics.
- **End-to-end/release:** authenticated two-tenant browser validation of
  settings, a member cancellation near the deadline, admin removal, advisory
  and user report; verify desktop/mobile, SSR/direct navigation, field
  timezone and deployed origin/CSRF.

## Migration And Rollback

Use an additive SQL Server/EF migration. Existing reservations are unmarked,
existing tenant rules empty and the member deadline zero. There is no inferred
historical attendance backfill. Back up, test against representative existing
data and run the disposable SQL gate; apply explicitly before the compatible
API. Deploy the API before the SPA, then enable nonzero deadlines only after
the compatible SPA is live. Existing clients remain readable; a stale client
receives a clear API conflict if it attempts a now-late cancellation.

Rolling back the SPA while keeping the new API/schema preserves data, but
administrative controls become unavailable and stale members may encounter
deadline conflicts. For API rollback, drain incompatible writers; the old API
does not enforce deadlines, policy or new state invariants. Preserve the
additive schema/data, disable writes or explicitly accept the policy gap, and
reconcile any intervening changes before reactivating new code. Never run a
destructive down migration after production writes unless data loss is
explicitly accepted and recovery verified. MongoDB, SQLite, authentication
configuration and cache migrations are not applicable.

## Implementation and Validation

- The member response now includes cancellation eligibility and guidance but
  never includes attendance fields. Admin operations remain role/CSRF
  protected and use the existing tenant-scoped mutation guard.
- Historical reads use a field-local date and bounded deterministic paging.
  Reports require a selected user, support all/unmarked/no-show filters, and
  use bounded repository queries rather than browser-side filtering.
- Migration
  [20261123110721_AddShootingRangeAttendanceAndPolicy.cs](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/20261123110721_AddShootingRangeAttendanceAndPolicy.cs)
  adds no-show marker/date, zero-day/empty-rule policy defaults and the
  history, report and attendance indexes. It has not been applied to a
  database. Do not apply its destructive down migration after production
  writes.
- Backend tests: 100 passed; 9 SQL Server integration tests skipped because
  no disposable SQL Server was configured. Frontend tests: 90 passed.
  TypeScript checking, production client build to temporary output, SSR build
  to temporary output, generated migration SQL inspection and EF
  `has-pending-model-changes` all passed.
- No authenticated browser capture, responsive visual review, SQL migration
  execution, production deployment or backup/restore rehearsal was performed.
  The existing build emitted package vulnerability warnings and repository
  CS9107 warnings; those were not changed by this feature.
