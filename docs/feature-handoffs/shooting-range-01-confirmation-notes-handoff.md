# Shooting Range 01: confirmation and reservation notes

**Status:** Implemented and locally validated. Depends on: none. Parent:
[master handoff](./shooting-range-booking-feedback-handoff.md).

## Outcome and boundaries

Before a member submits a reservation, show an accessible confirmation dialog
with local date, time range, bay and an optional note. Submit only on explicit
confirmation; display the note with the request in the admin view. Preserve
pending approval, existing error handling, and cancellation behavior.

## Evidence and design

[App.tsx](../../frontend/shooting-range.client/src/App.tsx) submits directly.
[routeData.ts](../../frontend/shooting-range.client/src/routeData.ts) sends an
empty `request`; the existing API accepts up to 1,000 characters, persists
that field and [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx)
renders it. No new note column or endpoint is needed. Use the existing
[Dialog](../../frontend/fe-packages/layout/src/components/Dialog.tsx) if its
export and behavior fit; verify focus placement, trapping, escape and focus
return. Label the note optional and indicate its maximum; preserve draft on
server error and prevent duplicate submissions. On stale interval or 409,
refresh availability and let the user reselect without silently losing text.
Text is rendered as text, never raw HTML.

## Impacted files

- **Modify:** [App.tsx](../../frontend/shooting-range.client/src/App.tsx),
  [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
  [App.test.tsx](../../frontend/shooting-range.client/src/App.test.tsx),
  [routeData.test.ts](../../frontend/shooting-range.client/src/routeData.test.ts);
  adjust [styles.css](../../frontend/shooting-range.client/src/styles.css) and
  admin formatting only if needed.
- **Verify only:** Range API request validation and existing
  [ShootingRangeApiTests.cs](../../MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs);
  [api.ts](../../frontend/shooting-range.client/src/api.ts) already has the
  request property. No schema or deployment configuration change.

## Verification and release

Test cancel versus confirm, selected local date/time and bay, empty and
1,000-character notes, too-long rejection, no duplicate submit, server conflict,
and visible admin note. Check keyboard focus, escape, error announcement,
mobile and dark/light appearance. Existing API validation remains authoritative.
Deploy compatible client after verifying the existing API; rollback client only.
Update this handoff with actual validation. Stop here; no time or friendship
rules are introduced.

## Implementation and validation

The member selects a bay to open an accessible confirmation dialog showing the
date, offered interval and bay. The dialog accepts an optional request note,
keeps it when a booking action fails, and only submits on confirmation. The
backend persists the existing request field; the admin booking view displays
it as text. No schema or request-field migration was needed.

Changed client files:
[App.tsx](../../frontend/shooting-range.client/src/App.tsx),
[api.ts](../../frontend/shooting-range.client/src/api.ts),
[routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
[App.test.tsx](../../frontend/shooting-range.client/src/App.test.tsx),
[api.test.ts](../../frontend/shooting-range.client/src/api.test.ts),
and [routeData.test.ts](../../frontend/shooting-range.client/src/routeData.test.ts).

`cd frontend && yarn workspace shooting-range.client test` passed (81 tests)
and `cd frontend && yarn workspace shooting-range.client build` passed.
Vite emitted its existing `/env-config.js` module-type warning. No authenticated
browser capture was available.
