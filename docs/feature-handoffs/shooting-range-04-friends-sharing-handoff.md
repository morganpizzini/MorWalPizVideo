# Shooting Range 04: mutual friends and shared reservations

**Status:** Revised invitation and uncapped-sharing policy implemented and
locally validated. The SQL migration has not been applied; SQL concurrency
and authenticated browser verification remain release gates.
Parent: [master handoff](./shooting-range-booking-feedback-handoff.md).

### Frontend client implementation

The client now keeps accepted friends separate from incoming and outgoing
requests. It sends invitations through `POST /friend-requests`, supports
recipient accept/decline and requester cancellation, removes accepted
friendships through the existing friends route, reports pending-until-accepted
status, and reloads the relationship lists after each action. Booking
availability labels every listed direct friend on a shared slot and renders
the complete participant projection without a numeric display cap.

Changed client files: `frontend/shooting-range.client/src/api.ts`,
`api.test.ts`, `routeData.ts`, `routeData.test.ts`, `Friends.tsx`,
`Friends.test.tsx`, `App.tsx`, and `App.test.tsx`. The existing `/friends`
route registration and SSR route registry remain unchanged. Focused frontend
tests passed (65 tests); TypeScript checking, client production build, and SSR
build passed with outputs directed to `/tmp`. Vite reported its existing
`/env-config.js` non-module-script bundling notice. Backend behavior, SQL
migration, and rollout validation are outside this frontend implementation.

## Revised Outcome

The recipient must explicitly accept a friendship request before the relation
is mutual. A shared stage has no application-imposed participant limit:
each joining member must be an accepted direct friend of at least one active
participant. Every member booking remains separately Pending until approved
by an administrator. A chain such as A-B-C is allowed when C is accepted
friends with B, even if C is not friends with A.

## Pre-revision Evidence

- [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs)
  originally persisted only a canonical unordered pair. The model now stores
  only Pending or Accepted state and a required requester. The service lists
  only accepted pairs as friends.
- [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs)
  originally enforced a two-user limit. Generic SQL and mock writes now allow
  unlimited exact-interval sharing only when the joining user is an accepted
  direct friend of an active participant.
- [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs)
  now exposes request/list/accept/decline/cancel/remove operations and retires
  the old add route with `410 Gone`. [Friends.tsx](../../frontend/shooting-range.client/src/Friends.tsx)
  displays accepted friends and pending request directions separately.
- [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs)
  and the existing
  [friendship migration](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/20261121171929_AddShootingRangeFriendships.cs)
  enforce a tenant-qualified, unique canonical pair with required consent
  fields. The user confirmed that no database or historical friendship rows
  exist; the final schema is created without a backfill.
- [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx)
  groups individual rows by date, bay and stored interval. Revised SQL tests
  cover multi-member sharing, but were skipped without a configured SQL host.

## Revised Design and Boundaries

### Invitation lifecycle

- Extend the existing canonical pair with a state and requester identity.
  Pending means one approved user sent a request; only the other approved
  same-tenant user may accept or decline it. Acceptance makes the pair
  reciprocal. The requester can cancel a pending request; either participant
  can remove an accepted friendship. The requester cannot accept their own
  invitation. Invalid, stale or foreign IDs fail explicitly without exposing
  other users' data.
- Sending a request by exact username must never create an accepted friendship.
  Permit at most one pair per tenant. A repeated same-direction request is
  idempotent and does not create duplicates; a reverse request does **not**
  auto-accept it: the recipient must explicitly accept. Decline or cancellation
  removes the pending row so a later request can be sent. A retried decision
  must not change the result or turn a rejected request into an acceptance.
  Serialize all lifecycle mutations and account rechecks under the existing
  repository write guard.
- List accepted friends separately from incoming and outgoing pending
  requests. Expose only the pair participants' minimal identity, relationship
  state and request identifier; no general directory or booking notes.
  Continue exact-username lookup with generic non-enumerating failures.
  The UI must say “request sent” rather than “friend added,” expose
  accept/decline/cancel in context, refresh state after mutations, and show
  clear loading, empty and error states.
- Keep the existing accepted-friend read response limited to accepted pairs.
  Use a new request-specific endpoint for sending and explicit decision
  endpoints. Retire the old add endpoint with an explicit failure rather than
  returning success to an old client that assumes immediate friendship.
  All unsafe routes require antiforgery and current approved account/tenant
  checks; authorization comes from the session, never supplied user IDs.

### Joining a stage

- For a new or reactivated reservation, inspect **all** active overlaps under
  the guard. If there are none, retain ordinary exclusive admission. Otherwise
  permit sharing only if every overlap has the exact same bay and UTC bounds,
  every interval is valid, the requester has no existing active booking for
  that bay/interval, and at least one active participant is an accepted
  direct friend of the requester. Do not require friendship with every
  participant or a relationship with the original reserver. Remove the
  numeric cap from availability and every generic insert/replace path.
- Preserve half-open interval overlap, the exact-interval-only sharing
  assumption, Pending and Approved anchors, whitelist and closure rules, and
  individually Pending requests requiring admin approval. Existing active
  bookings remain approvable independently even if a friend removes the
  relationship or the original anchor cancels; rejected/cancelled bookings
  re-enter through current admission. No participant is silently cancelled.
  Admin-created bookings remain directly Approved and use the same target-user
  sharing checks.
- Availability must use the same predicate as writes, show eligible bays with
  minimal details of accepted direct friends only, and never leak unrelated
  participants. Admin recap must show all active participants and individual
  statuses/notes without truncating at two; rejected/cancelled rows do not
  count as active. Large groups must stay usable on mobile and keyboard.

### Original data-transition plan (superseded)

The prior plan was to retain migrated pairs in a non-authorizing
legacy-unverified state and roll out an additive consent migration. It is
historical only and must not be followed: the user confirmed that no database
or friendship rows exist. The cleanup amendment below folds the final schema
into the initial table-creation migration and removes all legacy-row handling.

## Revised Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Add guarded invitation lifecycle; revise availability and all booking write/transition paths. |
| Frontend/UI | Refine Range Operate-mode friends view for incoming/outgoing requests and decisions; support arbitrary participant lists in the admin recap. |
| Database/Data | Store pair state and required requester in the initial friendship schema; do not backfill nonexistent rows. |
| Contracts/Models | Range-local invitation DTOs; retain accepted-friend projection, explicitly retire unsafe old add behavior. |
| Authentication/Authorization | Recipient-only acceptance, participant-only visibility, tenant-bound sessions, approved accounts, forced-change and CSRF checks. |
| Validation | Require accepted direct friend of any active occupant; preserve exact intervals, independent status and closure/whitelist checks. |
| Cache | No booking output cache identified; refresh friend, availability and admin data after changes. |
| Configuration/Secrets | No new settings or secrets. |
| Logging/Telemetry | Log meaningful state failures without notes, credentials or sensitive personal data. |
| Background Jobs | None; invitations are visible when the member opens the page, not push notifications. |
| Deployment/Operations | Coordinated schema/API/client rollout; old writers must not coexist. |
| Compatibility | Existing bookings are preserved; there are no existing friendships to migrate, and old add clients fail explicitly. |

## Delivered Implementation

1. The initial friendship migration creates required requester/state columns,
   permits only Pending/Accepted states, validates requester membership in
   the canonical pair, and stops its down path if friendship rows would be
   lost. There is no legacy state or friendship-row backfill.
2. The API implements guarded request/list/accept/decline/cancel/remove
   transitions; only accepted pairs are returned as friends. Stale add clients
   receive `410 Gone`.
3. Availability and guarded SQL/mock repository writes enforce exact same
   bay/UTC intervals and an accepted direct friend among active participants,
   with no numeric participant cap. Existing rows, partial-overlap rejection,
   duplicate-participant rejection, independent Pending approval and
   friendship-removal behavior remain intact.
4. The client presents accepted, incoming and outgoing relationships
   separately, supports each decision, and displays all direct friend
   participants in an available shared slot. Admin recap preserves all
   participant rows.
5. Backend and frontend tests and local build/type checks passed as listed
   below. The architecture, ADR and this handoff now describe the revised
   behavior. No SQL migration or deployment was performed.

## Validation and Remaining Release Gates

- `dotnet build MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` and `dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --no-build` passed (94 passed, 9 skipped). Skipped SQL Server tests were unavailable without a configured disposable SQL host and are not concurrency evidence.
- `dotnet ef migrations has-pending-model-changes` reported no pending changes. Generated forward SQL contains one friendship-table creation and no friendship-row backfill; generated down SQL checks for rows before dropping the table.
- The complete client suite passed (84 tests). TypeScript checking, client production build and SSR build passed with outputs directed to `/tmp`.
- Tests cover recipient-only consent, cross-tenant username privacy, friend-of-a-friend joins, uncapped shared occupancy, generic repository writes and client request actions. Migration has not been applied or exercised against SQL.
- The independent review found no Critical/Major issue; its verified minor mock lookup finding was fixed. Details and optional follow-up coverage are recorded in the [consent cleanup handoff](./shooting-range-05-consent-cleanup-and-review-handoff.md).
- Still required before rollout: disposable SQL forward/recovery rehearsal, independent SQL-writer concurrency tests, and authenticated desktop/mobile browser verification. Neither local mock tests nor skipped SQL tests prove SQL locking or migration behavior.

## Open Questions

- Should the exact-interval requirement or Pending-anchor eligibility change?
  They remain unchanged unless the user says otherwise.
- Are there independent range safety or legal occupancy restrictions outside
  this application's policy? The requested feature imposes no numeric cap;
  operators remain responsible for any external limit.

## Outcome

Let members establish two-way friendship only after recipient acceptance and
request the same bay/time as a friend, with a separate Pending reservation for
each participant and admin visibility of everyone occupying the slot. A
friend of any existing participant may request to join; joining is not tied
to the original reserver or capped by application policy.

## Previous Policy — Superseded

- An approved member may add an approved same-tenant user by exact username;
  that action immediately establishes reciprocal friendship and either party
  can remove it. Return only minimum identity needed and use generic
  non-enumerating failures. This is the chosen default; invitation acceptance
  is deferred. Do not expose notes, private account details or a general
  member directory.
- A new join is permitted only for an **exact** bay and stored UTC interval
  matching an active Pending or Approved reservation held by a current direct
  friend of the requester. A friend of a friend can join when directly
  connected to any participant, not merely to the first reserver. Transitive
  discovery without such an active direct friend is not sufficient. Matching
  existing participants may include different friendship edges; do not
  require every participant to be friends with every other. Reject duplicate
  active reservations for the same user, bay and interval.
- Cancellation, rejection, disabled account or
  friendship removal does not silently cancel another person's existing
  reservation. New joins and reactivation require a currently active direct
  friend; existing approved/pending rows remain independently managed. A new
  overlapping but non-identical interval is blocked. Pending joins reserve
  shared capacity and Pending anchors are joinable. Friendship removal does
  not cancel already-created Pending requests.
- Ordinary exclusive bookings remain exclusive unless the exact-match
  friendship predicate succeeds. Limit occupancy to two active users, using
  the request's two-person example conservatively. Apply the same policy to
  admin-created bookings with no override.
  Preserve closure, whitelist and account checks for every participant.

## Previous Data, Privacy and Concurrency Plan

Use one canonical tenant-owned relationship row per unordered pair, with a
state sufficient for invitation/acceptance if approved. Enforce distinct
members, canonical ordering, tenant-qualified unique pair and foreign keys
to both users; tenant is assigned server-side. Queries expose only the
requester's own relationships and minimal identity. Specify revocation and
re-invitation transitions explicitly; process them under the same write guard.
No existing users are automatically made friends and no booking data is
backfilled.

The existing generic write validation currently rejects every overlap. Replace
that rule coherently for *all* insertion, activation and replacement paths,
including SQL and mock implementations; do not add an endpoint-only bypass.
Inside the guard atomically recheck actor/account, the friendship, active
anchor, exact interval, all overlapping records, duplicate user and capacity.
Do not change stored booking identity/interval. Reject conflicting unrelated
or partial overlaps. Preserve valid existing participant rows after later
relationship changes; reconcile malformed historical intervals fail-closed.
Use safe, requester-specific availability projections so non-friends and
other tenants never learn who reserved a bay. A join still uses the normal
confirmation and notes flow and remains Pending until individually approved.

## Original Impacted Files

- **Modify:** [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs),
  [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs),
  [ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs),
  [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs),
  [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs),
  [MockShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/MockShootingRangeRepositories.cs),
  [SqlServerShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs),
  [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs);
  [App.tsx](../../frontend/shooting-range.client/src/App.tsx),
  [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx),
  [routes.tsx](../../frontend/shooting-range.client/src/routes.tsx),
  [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts),
  [api.ts](../../frontend/shooting-range.client/src/api.ts) and relevant styles.
- **Add:** Range-local relationship DTO/model/repository and a focused member
  management surface as needed, following current organization.
- **Generated/migration:** additive tenant-owned relationship table,
  indexes, foreign keys and EF snapshot under
  [Migrations](../../MorWalPizVideo.ShootingRange/Persistence/Migrations).
  Update Range architecture, ADR-016 and this handoff after implementation.
- **Tests:** existing Range service, API, SQL, app, admin, route-data and API
  test files; add focused friend UI tests if the surface warrants them.

## Previous Verification and Rollout Plan

Test reciprocal add/removal and visibility; self, duplicate,
disabled and cross-tenant cases; forged IDs, anonymous/role/CSRF and
forced-change denials; no notes or non-friend booking data in member
projections. Test direct friends, chain join via any active participant,
non-friends, different interval, partial overlap, duplicate participant,
capacity boundary, pending/approved anchors, cancellation, rejection,
friend removal, restoration and schedule edits. Simultaneous SQL joins,
join-versus-cancel/friend-removal, unrelated concurrent booking and generic
repository bypass tests must prove the guard and rollback semantics.

Deploy additive schema and compatible API with old writers drained, then UI;
enable only after capacity and privacy policy is approved. A rollback to old
writers is unsafe while shared active intervals exist; pause mutations and
reconcile or recover from a tested backup. Do not silently drop friendships
or shared reservations. SQL tests, migration rehearsal and authenticated
desktop/mobile/keyboard evidence are release gates.

## Previous Immediate-Friendship Implementation and Validation

Added tenant-scoped canonical friendship pairs, exact-username add/list/remove
endpoints, and safe friend availability projections containing bay ID,
username and status only. Exact same-bay/same-UTC intervals may contain at
most two distinct active users; a joining account must be directly befriended
with an existing participant. Pending anchors are joinable, each request
remains Pending, and generic repository validation enforces the rule. Admin
recap lists each member separately. Friendship removal leaves existing
bookings unchanged.

Changed models, contracts, controller, service, SQL and mock repository
interfaces/implementations, DbContext, EF snapshot and API/service/SQL tests.
Added
[20261121171929_AddShootingRangeFriendships.cs](../../MorWalPizVideo.ShootingRange/Persistence/Migrations/20261121171929_AddShootingRangeFriendships.cs)
and its designer. The migration is additive and has not been applied.
Frontend changes include
[Friends.tsx](../../frontend/shooting-range.client/src/Friends.tsx),
[api.ts](../../frontend/shooting-range.client/src/api.ts),
[routes.tsx](../../frontend/shooting-range.client/src/routes.tsx),
[App.tsx](../../frontend/shooting-range.client/src/App.tsx),
[AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx)
and focused tests.

Range API tests passed (91 passed, 9 SQL Server tests skipped without a
configured disposable SQL host); frontend tests passed (81), and the client
production build passed. Real SQL concurrency/migration and authenticated
browser checks remain outstanding.
