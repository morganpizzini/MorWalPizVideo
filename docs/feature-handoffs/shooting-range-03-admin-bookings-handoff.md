# Shooting Range 03: admin occupancy and direct booking

**Status:** Implemented and locally validated. Depends on:
[module 2](./shooting-range-02-mixed-session-handoff.md).
See [master handoff](./shooting-range-booking-feedback-handoff.md) for
implementation assumptions and deferred policy.

## Outcome

Give the administrator a concise, date-grouped view of booking requests and
bay occupancy by interval, with individual users, statuses, notes and decisions.
Allow an admin to select a user, date, session/interval and existing bay, then
create an automatically Approved reservation.

## Design and boundaries

- Group by field-local booking date, then bay and stored time interval. Show
  time, bay, participant count and status in text; expand to individual
  members, notes and applicable actions. Keep Pending, Approved, Rejected and
  Cancelled distinct; do not count rejected/cancelled as occupying capacity.
  Retain accessible pending badge and each individual decision/restore rule.
  When module 4 lands, multiple matching active intervals appear as one
  occupancy group with every participant and individual approval state.
- Current endpoint returns yesterday onward, not all historical records.
  Preserve that scope until the user defines a wider range; if wider history is
  required add explicit date bounds and bounded/paginated reads rather than
  promising complete history from the existing endpoint.
- An admin-only, antiforgery-protected create action takes a selected tenant
  user and the same offered selection and bay as a member. Inside one existing
  guarded transaction revalidate admin actor, target user's approved status
  and forced-change state, schedule, expected endpoints, bay, closures,
  whitelist, current capacity and tenant ownership; insert directly as
  Approved. Do not accept client-supplied status or tenant. A restriction
  override is out of scope unless explicitly approved. Return a normal conflict
  if availability changed and refresh the view.
- “Stage” is assumed to be the existing bay, not a new domain concept. A
  distinct stage entity or optional bay assignment requires product approval.
  Choose users from existing admin-only safe projections and do not expose the
  admin directory to members.

## Impacted files

- **Modify:** [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx),
  [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
  [api.ts](../../frontend/shooting-range.client/src/api.ts),
  [styles.css](../../frontend/shooting-range.client/src/styles.css);
  [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs),
  [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs),
  [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs)
  and repository validation only where needed.
- **Tests:** [Admin.test.tsx](../../frontend/shooting-range.client/src/Admin.test.tsx),
  [routeData.test.ts](../../frontend/shooting-range.client/src/routeData.test.ts),
  [api.test.ts](../../frontend/shooting-range.client/src/api.test.ts),
  [ShootingRangeServiceTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs),
  [ShootingRangeApiTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs)
  and [ShootingRangeSqlServerTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeSqlServerTests.cs).
- **Migration:** none anticipated. Update Range architecture, ADR and this
  handoff with actual behavior and verification.

## Verification and release

Test unsorted multiple dates, same bay/different intervals, statuses and notes,
past/future action availability, empty/error states, badge refresh and date
boundaries in field timezone. Test admin authorization/CSRF, cross-tenant user
and bay IDs, stale sessions, target account restrictions, closures, whitelist,
overlap and concurrent creation; verify atomic Approved insertion on real SQL.
Inspect desktop and mobile grouping, keyboard operation and screen-reader
labels. Deploy API before UI; old clients remain usable. Roll back the client
or a compatible API without deleting created bookings; avoid old writers if
module 2 or 4 has activated incompatible semantics.

## Implementation and validation

The admin page groups by local date and bay/interval, retaining each booking's
individual status, note and decision action. The creation form selects an
approved target account, date, offered interval and available bay, then calls
the antiforgery-protected admin endpoint. The service rechecks the current
administrator, target account and booking admission under the shared write
guard and inserts status Approved atomically.

Changed client files:
[AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx),
[routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
[api.ts](../../frontend/shooting-range.client/src/api.ts),
[styles.css](../../frontend/shooting-range.client/src/styles.css),
and related tests. Backend changes are in
[ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs),
[ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs)
and [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs).
No schema change was needed.

Validation passed: Range API suite (91 passed, 9 SQL Server tests skipped),
client suite (81 passed), and client production build. Authenticated browser and
target SQL tests remain release evidence.
