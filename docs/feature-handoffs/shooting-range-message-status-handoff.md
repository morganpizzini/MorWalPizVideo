# Shooting Range message display and status handoff

## Outcome

Replace the raw JSON on `/messages` with readable message text, sent date and
status. Administrators also see the sender and can mark a request as taken in
charge, completed or refused. This is a focused refinement of the existing
Range messaging surface, not a new conversation product.

Each existing thread represents one request; its status applies to the whole
thread. Existing and newly created requests start as **In attesa**;
administrators can change any current status to **Presa in carico**,
**Completata** or **Rifiutata**, including correcting an earlier decision.
Users see all four states, including refusal. Existing replies remain
readable, but reply composition, status notifications, deletion and reporting
are out of scope.

## Implementation Results

- Added Range-local `ShootingRangeThreadStatus` values Pending `0`, InProgress
  `1`, Completed `2` and Refused `3`, plus an additive SQL `status` column with
  a database default of Pending. The down migration refuses to drop recorded
  decisions.
- Added admin-only `POST /api/shooting-range/admin/messages/{id}/status`.
  It requires CSRF and a non-Pending, defined status; current administrator
  state, thread lookup and replacement are checked under the existing write
  guard. Any decision can be corrected.
- Moved the existing reply read, owner/admin authorization and replacement into
  that same guarded operation, preventing status and reply updates from
  overwriting each other.
- Replaced JSON rendering on `/messages` with typed thread cards showing text,
  dates in the device's local timezone, status, admin sender identity, and
  decision controls. Unknown statuses are explicitly unavailable and have no
  controls. Admin sender lookup failures use the existing retryable deferred
  error pattern. Successful sends and decisions reload the list.
- Updated [Shooting Range architecture](../shooting-range-architecture.md) and
  [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md) to record
  the limited workflow and migration/rollout constraints.

## Before-change Evidence

- `frontend/shooting-range.client/src/routes.tsx` maps loaded items to
  `JSON.stringify(item)`, which explains the screenshot. The page already
  supports message submission and deferred loading; `routeData.ts` provides
  the loader/action and `api.ts` currently types the list as `unknown[]`.
- `ShootingRangeController.Messages` returns all threads for an admin and
  only the caller's threads for other users. `OpenMessage` creates a thread
  containing an initial message with UTC creation time; `Reply` appends to the
  message list. Both routes require authentication and unsafe operations
  require antiforgery validation.
- `ShootingRangeThread` has an owner, subject and message list but no status.
  Each `ShootingRangeMessage` has text, author information and `CreatedUtc`.
  The base entity's `CreationDateTime` is local time, not the authoritative
  UTC instant for individual messages.
- `admin/users` returns safe `AdminUserDto` records containing username and
  names, only to administrators. Session validation rechecks current account
  and role. `ShootingRangeService.AdminMutationAsync` rechecks administrator
  state inside the repository write guard.
- Range's EF model stores each thread as a SQL row and its messages as JSON.
  Repository replacements are guarded, but `Reply` reads its thread before
  entering the repository write guard: concurrent read-modify-write operations
  can replace stale data. `docs/shooting-range-architecture.md` specifies SQL
  as the production store and explicit EF migration and guarded-write policy.
- Before this change, the architecture guide and ADR-016 deferred messaging
  expansion. The approved implementation remains limited to the requested
  status workflow and does not resolve other public-release gates.

## Implemented Design

1. Add a Range-local, explicitly valued status enum: pending, taken in charge,
   completed and refused. Add a non-null status property to the existing thread
   entity, defaulting to pending. Preserve existing message and thread fields
   and endpoints; status is an additive numeric field on the current thread
   response, consistent with existing Range enum serialization.
2. Add an admin-only, antiforgery-protected status decision endpoint with a
   required status request. Reject absent, malformed, undefined and pending
   decision values at the server boundary; do not silently interpret an
   omitted numeric value as pending. Return not-found for an unknown thread.
   Read the current thread and write only the requested status within
   `AdminMutationAsync`, preserving all text, author data and timestamps.
3. Move the existing reply read, authorization check and replacement into a
   single guarded operation too. Recheck the actor's current approved account,
   forced-password state and admin/ownership inside the guard; preserve
   existing reply endpoint and behavior. This is necessary to prevent an
   in-flight reply from overwriting a newly saved status, or a stale decision
   from overwriting a reply.
4. On `/messages`, use a typed thread response. Display each message's text
   and its `CreatedUtc` date/time in Italian, in the browser's local timezone;
   include a visible timezone indicator. Keep all existing messages readable,
   identify admin replies where relevant, and show thread status as text, not
   color alone. Handle empty legacy threads and invalid dates explicitly.
   Never display object JSON. Use thread IDs as stable keys.
5. For admins, reuse `adminUsers` to resolve the owner to name and username;
   fetch it only for admins. If an owner is missing, display an explicit
   unavailable-identity label rather than exposing internal IDs or pretending
   identity is known. Keep that failure distinct from a failed users request,
   which must surface a retryable error. The same page offers the three
   decisions with accessible names, pending state, error feedback and refresh
   after success. Users never receive decision controls.
6. Explicitly refresh the message list after a successful send or decision,
   as in `AdminBookings`, and retain entered text after a failed send. An
   absent or unrecognized status from an older or unexpected API displays
   **Stato non disponibile** and disables decisions; do not silently label it
   pending. A failed refresh must not be presented as a fresh successful
   status.

### UI planning lens

Operate mode; refinement, not redesign. Preserve the existing Range shell,
Italian copy, shared appearance tokens, message cards and responsive controls.
No Range-local `PRODUCT.md` or `DESIGN.md` was found; the existing Range
implementation and the user's screenshot are the incumbent visual evidence.
The Impeccable `harden` workflow was used. Component render tests cover member
and admin states, unknown status, empty history, and invalid dates. Browser
captures were unavailable because no Chromium-based browser is installed;
desktop/mobile appearance and live keyboard/contrast behavior remain
unverified. Do not introduce a new visual identity or require new design
artifacts.

## Impacted Projects

- `MorWalPizVideo.ShootingRange`: Range-owned API, model, service and SQL mapping.
- `MorWalPizVideo.ShootingRange.Tests`: API, service and disposable SQL tests.
- `frontend/shooting-range.client`: user/admin message page, local API types,
  route data, styling and tests.
- Range API and client deployment surfaces: explicit SQL migration and
  coordinated independently deployed versions.
- Other .NET projects, frontend workspaces, BackOffice, Workshop and shared
  packages: not affected; no cross-project contracts are introduced.

## Impacted Files

### Modify

- `MorWalPizVideo.ShootingRange/Models/ShootingRangeModels.cs`: thread status.
- `MorWalPizVideo.ShootingRange/Contracts/ShootingRangeContracts.cs`: required,
  validated admin decision input.
- `MorWalPizVideo.ShootingRange/Repositories/ShootingRangeDbContext.cs`: SQL
  enum mapping and migration-consistent default.
- `MorWalPizVideo.ShootingRange/Services/ShootingRangeService.cs`: guarded
  decision and guarded reply orchestration and actor revalidation.
- `MorWalPizVideo.ShootingRange/Controllers/ShootingRangeController.cs`:
  status endpoint and delegation of existing reply behavior.
- `frontend/shooting-range.client/src/api.ts`: typed threads and decision call.
- `frontend/shooting-range.client/src/routeData.ts`: typed loading, admin
  identity lookup and decision action.
- `frontend/shooting-range.client/src/routes.tsx`: accessible rendering,
  admin decisions and refresh.
- `frontend/shooting-range.client/src/styles.css`: only necessary message
  layout and responsive refinements using existing tokens.
- `MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs`,
  `MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeServiceTests.cs`
  and `MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeSqlServerTests.cs`:
  targeted behavior, concurrency and migration verification.
- `frontend/shooting-range.client/src/routeData.test.ts` and
  `frontend/shooting-range.client/src/api.test.ts`: route and transport cases.
- `frontend/shooting-range.client/src/Messages.test.tsx`: member and admin
  rendering, decision correction, unavailable status/identity and bad dates.

### Add

- A focused message-page test alongside the existing Range client tests,
  covering both roles, rendering, decisions and refresh.

### Generated/Migration

- EF migration `20261120221832_AddShootingRangeMessageStatus` and designer in
  `MorWalPizVideo.ShootingRange/Persistence/Migrations/`, plus the model
  snapshot. The non-null integer status defaults existing rows and old API
  inserts to Pending; Down refuses to discard recorded decisions.

### Documentation

- `docs/shooting-range-architecture.md`: describe the accepted narrow message
  status workflow, guarded writes, data compatibility and release procedure.
- `docs/architecture/adr/ADR-016-shooting-range-public-poc.md`: amend the
  earlier messaging deferral to allow only this request/status slice.
- Update this handoff with implemented changes, validation and residual risks.

### Verify Only

- `MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs`
  and `MockShootingRangeRepositories.cs`: preserve existing guarded transaction
  and mock semantics.
- `frontend/shooting-range.client/src/AdminBookings.tsx`, `App.tsx` and
  `Loading.tsx`: reuse existing UX, session, refresh and deferred-error patterns.
- `.github/workflows/deploy-shooting-range-api.yml` and
  `.github/workflows/deploy-shooting-range-client.yml`: check independent
  deployment triggers and coordinate operations; do not assume automatic
  schema migration.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Add guarded admin status decision; preserve existing thread and reply routes. |
| Frontend/UI | Refine existing `/messages` for user and admin, with accessible Italian status labels. |
| Database/Data | Add one status column; existing messages and JSON unchanged, existing rows pending. |
| Contracts/Models | Range-local thread enum and decision input; additive response field, no shared model. |
| Authentication/Authorization | Existing session and CSRF preserved; server enforces current admin on decisions and ownership on replies. |
| Validation | Require an allowed decision; unknown thread is 404; invalid state is 400; keep existing send validation. |
| Cache | No message output cache is present; no cache invalidation added. |
| Configuration/Secrets | No new configuration or secrets. |
| Logging/Telemetry | Preserve existing error handling; do not log message bodies, identities or credentials. |
| Background Jobs | None; no polling or notification job. |
| Deployment/Operations | Explicit SQL migration, compatible API rollout, then client; public-release gates remain. |
| Backward Compatibility | Existing response fields and routes preserved; old clients ignore the additive field. Old API lacks status decisions and must not coexist with new writers during cutover. |

## Reuse Opportunities

Reuse `AdminMutationAsync`, the existing repository guard, `adminUsers`, the
Range API transport and its antiforgery handling, `RangeDeferred`, the
`AdminBookings` action/refresh pattern, current cards and appearance tokens,
and the existing API-host and SQL Server test fixtures. Keep business behavior
local to Range.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Stale reply or status writers overwrite thread data. | Complete read-modify-write is guarded; the independent SQL writer test passed with both status and reply preserved. Drain old API instances before enabling new writes because the prior reply path read outside its guard. |
| High | Other users can change status or read private requests. | Enforce role, current account and CSRF on server; test ordinary, demoted, disabled and forced-change accounts and cross-user visibility. |
| High | A migration or rollback loses decisions. | Preserve message JSON; default legacy rows to pending; test upgrade with populated data and rollback procedure on disposable SQL. Do not drop the column after real decisions without an approved loss/recovery decision. |
| Medium | API/client versions disagree about status. | Deploy compatible API before client; show unavailable rather than a false pending state; test missing/unknown status and mixed-version cutover. |
| Medium | Sender lookup or dates mislead administrators. | Explicit unavailable identity/date display, browser timezone label and tests for missing owner and invalid timestamps. |
| Medium | Deployment is mistaken for public-release approval. | Keep existing authorization, backup, browser and operational gates independently blocked until evidence exists. |
| Low | External services or cache coherence affect this slice. | No external integration or message cache identified; verify existing deployment and browser behavior. |

## Technical Debt

- Existing reply read outside its write guard is directly coupled to the
  change and must be fixed now.
- Current controller exposes persistence threads directly; preserve existing
  wire compatibility for this slice. A broader safe-DTO cutover is separate.
- Global write serialization and list-wide reads are established POC
  constraints; avoid pagination, new indexes or architectural redesign here.
- Anonymous Range domain reads and onboarding policy are separate known
  public-release blockers, not part of this change.

## Implementation Roadmap

1. Confirmed by the user: administrators may correct previous decisions.
2. Implemented the Range-local thread status, migration, guarded status/reply
   operations, authorization and request validation.
3. Implemented user/admin display, sender lookup, status controls, refresh,
   typed API handling, visual states and regression tests.
4. Updated architecture, ADR, and this handoff with rollout and rollback
   details.
5. Verified the client, API, migration and SQL concurrency paths. Browser
   captures remain unverified because Chromium is unavailable.
6. Stop after this workflow; further messaging features need separate
   approval.

## Testing Strategy

- **Unit/service:** all three decisions and corrections; reject missing,
  pending and undefined input; preserve thread contents and timestamps;
  concurrent reply/decision semantics; invalid identity and permissions.
- **API/contract:** create and list own message, admin list with sender,
  every status round-trip, unknown ID, anonymous and non-admin rejection,
  demotion/disable/forced-password behavior, missing CSRF, and no leakage of
  another user's threads or password hashes. Existing reply contract remains.
- **SQL migration/integration:** seed old-schema rows with multiple messages;
  migrate and verify default/status round-trip and message preservation.
  Test independent status/reply writers against disposable SQL Server, not
  solely EF in-memory or mock. Verify old-version inserts get pending.
- **Frontend/component:** user and admin display real text, dates, status and
  sender; multiline/long text, missing sender, empty thread, invalid date,
  missing/unknown status, no raw JSON; user has no decision controls.
  Exercise all decisions, busy state, send/decision success and error,
  refresh, retry, session expiration and keyboard access. Cover SSR/direct
  navigation using existing tests.
- **End-to-end/visual:** user sends a message; admin sees sender and updates
  each status; the user sees the persisted result after refresh. Test desktop
  and mobile, both appearance themes and cookie/CSRF behavior on the allowed
  origin. Local API/SQL tests exercise persistence and antiforgery paths; the
  browser capture and deployed-origin evidence remain outstanding.

## Migration And Rollback

Use the existing explicit EF migration procedure with a verified SQL backup.
The nullable-free column defaults all historical and old-version inserted
threads to pending; do not infer historical completion. First quiesce message
writes and drain all old API instances because their reply path reads before
the guard. Apply the migration, deploy and verify the new guarded API across
all instances, then deploy the compatible client and resume writes. Check
legacy records and decisions before declaring rollout complete.

For rollback after decisions exist, disable decision controls and quiesce
message writes; retain the status column and database backup. Only use a
tested compatible API recovery path, and drain mixed writer versions. An old
API/client cannot expose or change saved statuses; any down-migration that
drops the column loses them and requires explicit approval and verified
recovery. If safe compatibility cannot be established, leave writes suspended
and preserve data. MongoDB, SQLite, authentication configuration and cache
migrations are not applicable.

## Validation Performed

- `yarn --cwd frontend workspace shooting-range.client test src/Messages.test.tsx src/routeData.test.ts src/api.test.ts` — 38 passed.
- `yarn --cwd frontend workspace shooting-range.client build:all` — client and SSR builds passed.
- `dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ShootingRange"` — 78 passed, 7 SQL tests skipped without SQL configuration.
- Disposable SQL Server run of `ShootingRangeSqlServerTests` — 7 passed, including migration application and concurrent reply/status preservation.
- `dotnet ef migrations has-pending-model-changes` — no pending model changes.
- Browser/visual capture was not performed because no Chromium-based browser is available.

The backend commands emitted existing NU1902/NU1903 warnings for
`System.Security.Cryptography.Pkcs` 9.0.4. No dependency changes were made.
