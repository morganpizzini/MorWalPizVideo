# Shooting Range 02: mixed whole-session and hourly reservations

**Status:** Implemented and locally validated under the assumptions in the
[master handoff](./shooting-range-booking-feedback-handoff.md). Depends on:
module 1 for the unified confirmation UX.

## Outcome

Allow a tenant administrator to enable a split-window mode in which a member
chooses the entire morning/afternoon or one or more contiguous hours inside
that window. A whole-session choice is unavailable on a bay if any active
booking overlaps; remaining non-overlapping hours remain selectable. Make
settings truthful rather than presenting fixed duration as editable.

## Proposed contract and policy

- Preserve existing numeric modes 0, 1 and 2 and their legacy requests. Add a
  distinct opt-in Mixed mode. Admin selects 60- or 120-minute increments
  (default 60); offer contiguous multiples of that increment and each complete
  morning/afternoon window, with no break crossing. Retain continuous mode
  unchanged. Validate and persist the selected increment in the existing
  config field.
- Offer whole-window selection even when the window has an incomplete hourly
  tail; only full hourly units can be selected individually. Reject invalid or
  ambiguous DST boundaries; apply current half-open UTC conflict semantics.
  Define multi-hour options without requiring the API to trust arbitrary UTC
  inputs: use an unambiguous server-offered selection identity and expected
  UTC boundaries, resolve both again from current config inside the write guard.
  Reject stale selections explicitly. No selected range may span a break.
- Availability and insertion must use the same interval resolver and compare
  stored active intervals, not labels. A conflict at any point blocks the
  proposed interval, but touching endpoints and different bays are allowed.
  Whole and hourly choices must both use the same overlap rule. Preserve
  historical UTC intervals and existing cancellation behavior; restoration
  continues to require current schedule and admission.
- Build the chooser into existing booking/confirmation flow. Make available,
  partially occupied and unavailable choices distinguishable in text; do not
  present a promise of availability before server admission.

## Impacted files

- **Modify:** [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs),
  [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs),
  [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs),
  [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs),
  [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs);
  [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx),
  [App.tsx](../../frontend/shooting-range.client/src/App.tsx),
  [api.ts](../../frontend/shooting-range.client/src/api.ts),
  [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
  [schedule.ts](../../frontend/shooting-range.client/src/schedule.ts) and
  relevant styles.
- **Tests:** [ShootingRangeServiceTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs),
  [ShootingRangeApiTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs),
  [ShootingRangeSqlServerTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeSqlServerTests.cs),
  [App.test.tsx](../../frontend/shooting-range.client/src/App.test.tsx),
  [Admin.test.tsx](../../frontend/shooting-range.client/src/Admin.test.tsx),
  [schedule.test.ts](../../frontend/shooting-range.client/src/schedule.test.ts)
  and route/API tests as affected.
- **Generated/migration:** no schema change is assumed for an additive enum
  value; inspect actual EF mapping and constraints. Add a migration if
  confirmed settings require new persisted fields. Update Range architecture
  and ADR-016 after acceptance and implementation.

## Validation and release

Test each legacy mode unchanged; whole versus one/multiple hours in both
directions, a partially occupied window, adjacent/different-bay cases, no
cross-break or incomplete unit, exact opening/closing times, DST, stale config,
closed day, whitelist, reactivation and concurrent whole/hour writers on SQL.
Include contract and UI tests, responsive and keyboard review. Opt in only
after updated API and client are deployed; drain old API writers before
activating new mode. Keep existing intervals untouched. Roll back by disabling
new writes and reconciling new-mode bookings before returning to an older API;
do not reinterpret or discard stored intervals.

## Implementation and validation

Added opt-in `ShootingRangeSessionMode.Mixed = 3`, preserving modes 0–2.
Mixed schedules offer whole morning/afternoon windows and all contiguous
blocks of complete 60- or 120-minute increments; the admin schedule editor
lets the administrator choose the increment. The existing config schema
already stores `HourlyMinutes`, so no schedule migration was needed. Member
and admin booking submissions resolve the offered period key and revalidate
expected UTC bounds in the guarded service.

Backend changes include
[ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs),
[ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs),
[ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs),
[IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs)
and related API/repository tests. Client changes include
[Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx),
[App.tsx](../../frontend/shooting-range.client/src/App.tsx),
[api.ts](../../frontend/shooting-range.client/src/api.ts),
[routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
[schedule.ts](../../frontend/shooting-range.client/src/schedule.ts)
and focused tests.

The Range API suite passed 91 tests; nine SQL Server tests were skipped without
`SHOOTING_RANGE_TEST_SQL_HOST`. The client suite passed 81 tests and its
production build passed. No target SQL/browser verification was performed.
