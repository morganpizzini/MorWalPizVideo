# Shooting Range booking feedback: master handoff

**Status:** All four modules and consent cleanup are implemented and locally
validated. The consolidated friendship migration has not been applied. SQL
concurrency, migration rehearsal and authenticated browser verification
remain rollout gates; no deployment or production migration is authorized.
The user confirms no database or historical friendship rows exist. Independent
review found no Critical/Major issue; its verified mock lookup issue was
fixed. See the [consent cleanup handoff](./shooting-range-05-consent-cleanup-and-review-handoff.md).

## Outcome

Make reservation requests clearer for members and easier for administrators
to review and create; support an administrator-enabled choice between whole
split sessions and contiguous hourly reservations; and allow friends to
request the same bay and interval. Preserve tenant isolation, manual approval
of member requests, existing records, and the Range application's ownership.
Assume “stage” means the existing bay until the user confirms otherwise.

## Original Evidence Before Implementation

- [AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx)
  renders a flat list, one row per booking, from yesterday onward; it shows
  bay, local time, user, status and request text, with future-only decisions.
  It does not show a date/bay occupancy summary or create a booking.
- [App.tsx](../../frontend/shooting-range.client/src/App.tsx) immediately
  submits when a bay button is selected. [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts)
  sends an empty request string, although
  [ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs)
  already accepts up to 1,000 characters and the admin list displays them.
- [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx) displays
  “Durata slot orari” as a fixed output and forces 60 minutes on save.
  [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs)
  validates that duration and offers one of three mutually exclusive modes:
  whole periods, split single hours, or continuous single hours. It excludes
  incomplete hourly tails and invalid/ambiguous DST boundaries.
- Sessions are generated from configured local opening windows; UTC start/end
  are stored in [ShootingRangeModels.cs](../../MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs).
  Pending and Approved bookings block overlapping intervals, including when
  mode changes. Expected UTC endpoints detect a stale selection. Cancellation
  retains history; admin restoration requires current admission.
- [IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs)
  applies admission and overlap checks to generic booking writes too;
  [SqlServerShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs)
  serializes writes with a SQL transaction and the singleton guard.
  Tenant-filtered records and tenant-qualified booking foreign keys are mapped
  in [ShootingRangeDbContext.cs](../../MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs).
  No friendship model or separate stage entity was found in the Range source.
- Authenticated Range routes, admin-role checks, antiforgery, tenant-bound
  sessions and SQL deployment policy are documented in the
  [Range architecture](../shooting-range-architecture.md) and
  [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md).
  The earlier handoff's test results are historical, not evidence for this
  proposal or its deployment.

### Source-based UX critique

The target is the **Operate** admin booking view, a refinement rather than a
visual redesign. The flat, reverse-time list forces the administrator to
mentally group repeated dates and reconstruct occupancy across rows; one
user per row also cannot communicate a shared bay. The heading emphasizes
pending requests while the data includes other statuses. Request text exists
in the admin row but the member flow always sends it empty. The admin schedule
shows a duration label that looks like a setting but is not editable. Strengths:
the existing view retains statuses and clear per-booking decisions, and shows
loading, feedback and session errors.

This is a **source-only critique, not a completed `/impeccable critique`**:
the required independent assessments, deterministic detector and authenticated
browser inspection were not run in this architecture-only session. No visual,
responsive, heuristic-score or accessibility compliance claim is made. Neither
Range-local PRODUCT.md nor DESIGN.md exists in the checked-in tree; preserve
the incumbent Range and shared-layout identity. Implementation should use
Impeccable `shape` for the new operational grouping and `audit`/`harden` after
building; complete a full critique separately if requested.

## Original Implementation Decisions — Partially Superseded

The implementation proceeds on these conservative interpretations. They are
not claims that the user separately confirmed each policy:

- “Stage” is the existing bay; admin must select a bay.
- Keep hourly granularity administrator-configurable as 60 or 120 minutes,
  default 60. In the new Mixed mode, offer a whole morning/afternoon or any
  contiguous run of complete configured increments within one window. Retain
  all existing modes and continuous mode unchanged. No interval crosses the
  afternoon break; keep UTC bounds authoritative.
- **Superseded:** A member adding an approved same-tenant user by exact username immediately
  creates a reciprocal friendship; either party can remove it. Do not expose a
  general directory. Friend joins are exact bay/interval only, may use a
  Pending or Approved friend reservation, and one direct friendship with any
  existing participant is sufficient. This permits the described friend-of-a-
  friend chain by joining through the directly befriended participant.
- **Superseded:** Cap shared occupancy at two active users per exact bay/interval. This uses
  the two-person example as a conservative cap rather than assuming unlimited
  capacity. Any partial overlap, third occupant, or unrelated reservation is
  rejected. Each join is a separate Pending request and requires admin
  approval. Admin-created reservations are Approved immediately but obey the
  same account, bay, schedule, whitelist and sharing checks.
- Friendship removal does not silently alter existing bookings. Do not expose
  friends' notes. Admin history retains the existing yesterday-inclusive
  range; broader history needs a separate requirement.

The immediate-friendship and two-person-cap assumptions above are historical
only and must not be released. Do not reinterpret existing UTC bookings or
bypass server admission.

## Revised User Decisions — Implemented

- Friendship requires an explicit acceptance by the recipient before it
  becomes reciprocal or grants access to a friend's reservations.
- There is no application-defined cap on the number of participants in a
  shared stage. Each new requester must be an accepted direct friend of at
  least one active participant; a chain through any participant is allowed.
- Preserve the other established rules: exact same bay and UTC interval,
  Pending or Approved anchors, individual Pending requests and administrator
  decisions, no silent cancellation on friendship removal, and normal
  tenant/closure/whitelist/account validation. These are assumptions retained
  from the previous plan, not newly confirmed by this feedback.
- The original plan to retain canonical rows as non-authorizing
  `LegacyUnverified` records is superseded by the consent-cleanup amendment
  below. The user confirmed that no database or historical friendship rows
  exist.
- The client separates accepted friends from incoming/outgoing requests.
  Stale `POST /friends` clients receive `410 Gone`; request, acceptance,
  decline, cancellation and removal use separate antiforgery-protected routes.

## Delivered Implementation

All four modules are implemented across the Range API/data project and its
React client. Member booking confirmation and notes reuse the existing
`Request` property. `Mixed=3` and the existing `HourlyMinutes` setting provide
60/120-minute increments without a schedule migration. Admin bookings are
grouped by field date/bay/interval and direct admin creation inserts Approved
under the guard. Friendships are tenant-scoped canonical pairs with explicit
request state and required requester metadata. Only Pending and Accepted
states are supported; there is no legacy-row state or backfill. Accepted
direct friends can join an exact shared interval with no application
participant cap. Availability uses accepted direct friendships, and generic
repository writes enforce the same rule as the service. Each member join
remains independently Pending for admin approval.

### Consent cleanup amendment — 2026-11-21

The user confirmed that no database or historical friendship rows exist. The
runtime now supports only Pending/Accepted states with a required requester;
legacy-state handling and backfill logic are removed. Consent fields and
constraints are folded into the initial friendship migration, and its down
path refuses to drop populated friendship rows. The migration has not been
applied.

Changed backend files: `Contracts/ShootingRangeContracts.cs`,
`Controllers/ShootingRangeController.cs`, `Models/ShootingRangeModels.cs`,
`Repositories/IShootingRangeRepositories.cs`,
`Repositories/MockShootingRangeRepositories.cs`,
`Repositories/ShootingRangeDbContext.cs`,
`Repositories/SqlServerShootingRangeRepositories.cs`,
`Services/ShootingRangeService.cs`,
`Persistence/Migrations/ShootingRangeDbContextModelSnapshot.cs`,
and the migration
`20261121171929_AddShootingRangeFriendships`, with its updated designer.
Backend tests changed in `Features/ShootingRangeApiTests.cs`,
`Services/ShootingRangeServiceTests.cs`, and
`Services/ShootingRangeSqlServerTests.cs`.

Changed client files: `src/App.tsx`, `src/Admin.tsx`,
`src/AdminBookings.tsx`, `src/Friends.tsx`, `src/api.ts`,
`src/routeData.ts`, `src/routes.tsx`, `src/styles.css` and focused tests.
The friendship migration has not been applied.

## Validation and Residual Risks

- `dotnet build MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` and `dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --no-build` — passed, 94 passed and 9 SQL Server tests skipped because no disposable host was configured.
- `dotnet ef migrations has-pending-model-changes` — no pending model changes. Generated forward SQL contains one friendship-table creation and no row backfill; generated down SQL checks for rows before dropping the table.
- The full client suite passed (84 tests). TypeScript checking, client production build and SSR build passed with build outputs directed to `/tmp`.
- No authenticated browser/responsive visual review or real SQL migration, concurrency, backup/restore or rollback test was performed.

The .NET tests reported existing `System.Security.Cryptography.Pkcs` 9.0.4
vulnerability warnings. SQL Server concurrency remains a release gate; skipped
integration tests are not evidence of SQL behavior. Apply and verify the
consolidated friendship migration against disposable SQL, drain old API writers before
enabling invitation or shared booking writes, deploy API before client, and
do not roll back to an old writer while accepted-only relationships or shared
active intervals exist. These local checks do not authorize deployment or
public exposure.

## Original Design

Implement four independently reviewable modules in this order:

1. [Confirmation and notes](./shooting-range-01-confirmation-notes-handoff.md):
   use the existing request field; deliver useful value without schema changes.
2. [Mixed-session intervals](./shooting-range-02-mixed-session-handoff.md):
   establish authoritative interval selection, compatibility, and overlap rules.
3. [Administrator occupancy and booking](./shooting-range-03-admin-bookings-handoff.md):
   show date/bay/time groupings and add a guarded auto-approved booking for a
   selected user using module 2's selection and admission rules.
4. [Friendship and shared occupancy](./shooting-range-04-friends-sharing-handoff.md):
   introduce mutual relationships and a narrow, concurrency-safe sharing
   exception. Extend module 3's recap to list every participant.

Modules 1 and 2 can technically be developed in parallel; the listed order
minimizes UI churn and makes module 2 the dependency for 3 and 4. Module 4
depends on the other three for a complete requested experience. No module
authorizes unrelated account onboarding, rescheduling, messaging, reporting,
or a separate stage-management product.

Core proposed invariants, subject to the gates below: all intervals are
half-open UTC; existing bookings keep their exact stored interval; Pending and
Approved reserve capacity; a whole period conflicts with any overlapping
hour on the same bay unless the explicit exact-interval sharing rule applies.
An administrator-created reservation is Approved atomically, not a Pending
request followed by a second write. Administrators do not bypass closure,
whitelist, approved-account, tenant, schedule or capacity rules by default.

## Impacted Projects

- `MorWalPizVideo.ShootingRange`: Range-local API, services, validation,
  repository and EF SQL model/migrations.
- `MorWalPizVideo.ShootingRange.Tests`: service, API, security and real SQL
  concurrency/migration coverage.
- `frontend/shooting-range.client`: Range-local React UI, loader/action,
  types, transport and component tests.
- `@morwalpiz/layout`: verify and reuse its existing Dialog; no shared package
  change proposed. Other frontend workspaces, BackOffice and shared domain/
  contract projects are not affected.
- Existing Range API/client deployment workflows, SQL operators, and AppHost
  are verify-only unless implementation proves a deployment change necessary.

## Impacted Files

**Modify:** each module lists its owning source and tests; shared touchpoints
are [ShootingRangeService.cs](../../MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs),
[ShootingRangeController.cs](../../MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs),
[ShootingRangeContracts.cs](../../MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs),
[IShootingRangeRepositories.cs](../../MorWalPizVideo.ShootingRange/Repositories/IShootingRangeRepositories.cs),
[App.tsx](../../frontend/shooting-range.client/src/App.tsx),
[AdminBookings.tsx](../../frontend/shooting-range.client/src/AdminBookings.tsx),
[Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx),
[api.ts](../../frontend/shooting-range.client/src/api.ts) and
[routeData.ts](../../frontend/shooting-range.client/src/routeData.ts), as
needed for the respective module.

**Add:** Range-local friendship/booking DTOs and UI only where warranted;
module 4 requires a tenant-owned relationship model and repository surface.
Choose names consistent with existing files. No cross-project contract.

**Generated/Migration:** module 4 needs an additive EF migration and
snapshot; module 2 needs one only if the confirmed scheduling design changes
the schema. Do not invent historical intervals or friendship data.

**Documentation:** update this handoff and affected module after each
implementation; update [Range architecture](../shooting-range-architecture.md)
and [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md)
when accepted behavior and operational policy change.

**Verify Only:** [Program.cs](../../MorWalPizVideo.ShootingRange/Program.cs),
[AppHost Program.cs](../../MorWalPizVideo.AppHost/Program.cs),
[API workflow](../../.github/workflows/deploy-shooting-range-api.yml),
[client workflow](../../.github/workflows/deploy-shooting-range-client.yml),
and [Dialog.tsx](../../frontend/fe-packages/layout/src/components/Dialog.tsx).

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Range-local endpoints, interval validation, guarded admin creation and shared admission. |
| Frontend/UI | Member confirmation/notes, interval chooser, friend management and join; grouped admin occupancy and creation. |
| Database/Data | Preserve stored bookings; additive tenant-scoped friendships and possibly schedule metadata; EF SQL migrations as needed. |
| Contracts/Models | Range-local additive/version-safe interval and sharing DTOs; retain legacy contracts for existing modes. |
| Authentication/Authorization | Existing tenant cookie, approved account, admin role, CSRF and forced-change checks; no anonymous user directory. |
| Validation | Server owns selection, expected endpoints, restrictions, membership, conflict checks, note bounds and approval transitions. |
| Cache | No booking output cache identified in this flow; refresh loaders and pending badge after mutations. |
| Configuration/Secrets | Tenant schedule settings change only as agreed; no new secrets. Mixed mode must be opt-in. |
| Logging/Telemetry | Record administrative creation and meaningful admission failures without notes, credentials or other sensitive content; avoid high-cardinality personal data. |
| Background Jobs | None; live guard and on-demand queries are authoritative. |
| Deployment/Operations | Explicit SQL migrations; stop/drain incompatible writers; separately deployed API/client and target SQL verification. |
| Backward Compatibility | Keep old enum values, routes, bookings and supported legacy mode behavior; new mode and shared data need guarded cutover and rollback. |

## Reuse Opportunities

Reuse `ShootingRangeService`, repository write guard, tenant filters and
foreign keys, `TimeProvider`, `BookingRequest.Request`, opening/closure and
whitelist checks, admin/user route actions, date picker, pending badge, and
shared [Dialog](../../frontend/fe-packages/layout/src/components/Dialog.tsx).
Reuse existing Range service/API/SQL test fixtures; do not copy Workshop
accounts or introduce another booking engine.

## Risks

| Rank | Risk / surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Two writers or an old API accept incompatible overlapping bookings. | Guard every relevant read/write including generic repository paths; drain old writers; run independent SQL writer races. |
| High | Unclear friend consent, capacity or privacy exposes members or permits unsafe occupancy. | Product gates in module 4; tenant-scoped minimal projections; authorization/CSRF and admission tests before enabling. |
| High | New schedule interpretation damages future/history or rollback. | Preserve stored UTC intervals, additive opt-in mode, SQL backup, migration rehearsal, block incompatible old writers and prove restoration. |
| Medium | Schedule edits invalidate open selections or historical reactivation. | Expected bounds plus guarded revalidation; explain conflicts; test edits, DST and cancellation/restore. |
| Medium | Admin-created reservations bypass user or bay policy. | Shared target-user admission with admin actor checked inside one transaction; negative API and SQL tests. |
| Medium | Frontend shows incomplete or misleading occupancy/status. | Group by stored intervals with explicit individual statuses, notes and counts; keyboard, responsive and concurrent-refresh tests. |
| Low | Global guard and list reads degrade as adoption grows. | Measure query/lock behavior; retain current guard initially, no speculative scaling redesign. |
| Low | No external-service dependency is added. | Not applicable; deployment and SQL remain operational risks. |

## Technical Debt

- Current admin list and availability read whole tenant sets before filtering;
  monitor volume, but query optimization is not a prerequisite without evidence.
- The fixed duration is intentional today, not a broken input. Do not imply
  arbitrary duration support unless the product gate approves it.
- The legacy `BookingDto` and entity-returning endpoints are inconsistent;
  add safe new projections where sharing requires them, without a broad API
  rewrite. Existing public-release response-safety gates remain open.
- Existing documents defer custom-duration sessions; amend only when the
  new policy is approved and implemented.

## Original Implementation Roadmap

1. Obtain the decisions in Open Questions; refine affected modules in place.
2. Implement and verify confirmation/notes (module 1); retain current APIs.
3. Implement and verify mixed interval contracts, authoritative admission,
   admin schedule settings and member chooser (module 2); test old modes first.
4. Implement and verify grouped admin view and atomic admin creation (module 3).
5. Implement and verify tenant-safe friendship and exact-slot sharing,
   including admin recap and SQL concurrency (module 4).
6. Update architecture/ADR and actual handoff results. Run focused backend,
   frontend and real SQL gates, then visually verify authenticated UI.
   Independent post-validation code/security review is warranted because
   authorization and concurrency invariants change; user selects the reviewer.

## Testing Strategy

Each module owns focused unit/service, API and component tests. End-to-end
acceptance covers member selection, notes, pending decision, admin-created
Approved booking, friend join, grouped counts and cancellations. Include
tenant/role/CSRF/forced-change negatives; stale selections, whole/hour
overlaps, DST, adjacent intervals and changed configuration; SQL concurrent
writers and transaction rollback. Browser evidence must include desktop and
mobile, keyboard/focus, zoom, labels, status not conveyed by color alone,
loading/error/empty states, and dialog focus/escape/restoration. Test both
light/dark when supported. Require real SQL migration and rollback rehearsal;
in-memory/mock results do not prove locking.

## Migration And Rollback

Take and verify a restorable SQL backup; inventory active bookings, settings
and data before changing schema or modes. Apply additive migrations explicitly
before compatible API, then deploy client and enable new settings per tenant.
Stop/drain incompatible API instances before enabling changed write behavior:
old writers cannot safely coexist with shared intervals or unknown mode values.
Do not roll back to an old API while new modes or shared active reservations
exist; disable new writes and reconcile, or restore a verified backup with
explicit acceptance of lost post-backup writes. Retain friendship and interval
data when disabling a UI. A schema down migration that drops relationships is
destructive and needs explicit operator approval. No MongoDB, SQLite, auth
migration, cache purge or new secrets are anticipated. Existing production
public-exposure and SQL evidence gates remain in force.

## Deferred Product Decisions

- Whether friend visibility should include more than username/display name,
  and whether removal should affect existing Pending requests.
- Whether stage should become a distinct entity, admins should override
  restrictions, or history should extend beyond the current date cutoff.

These do not change the revised acceptance and unlimited-sharing requirements.
