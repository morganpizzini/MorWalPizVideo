# Shooter Workshop Shooting Session FIFO And Inline Correction Handoff

> **Allocation and correction decisions superseded:** The accepted
> [Shooter Workshop allocation removal handoff](./shooter-workshop-allocation-removal-handoff.md)
> removes ammunition reservations from current product behavior. Inline correction
> edits explicit recipe quantities, does not ask for a total, and preserves untracked
> ammunition unchanged. Its migration and compatibility decisions replace this
> document's allocation policy and proportional correction behavior.

## Implementation status — 2026-11-15

Implemented the recipe-intent/FIFO and inline correction slice across Models, Contracts,
Domain, ServerAPI, shared frontend packages, Workshop UI, and focused tests.

Delivered:

- recipe ID plus quantity replaces browser batch/allocation ID entry;
- server-side deterministic FIFO resolution by production date and batch ID;
- resolved recipe metadata and ledger operation linkage on new shooting sessions;
- account-owned recent shooting-session read with corrected-row status;
- inline positive correction, explicit delete, and zero-quantity confirmation;
- immutable correction/delete behavior through the existing compensating ledger command;
- focused FIFO Domain coverage and existing service/client regression coverage.

Validation:

- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
- `dotnet test ... --no-build --filter "FullyQualifiedName~WorkshopServiceTests"` — 8 passed
- shared models/services builds — passed
- `yarn --cwd frontend workspace @morwalpizvideo/services test --run src/workshopService.test.ts` — 8 passed
- `yarn --cwd frontend workspace shooter-workshop.client test --run` — 29 passed
- Workshop client production build — passed

Residual risks:

- legacy shooting-session documents without an operation ID remain readable but are not
  backfilled by this slice;
- allocation removal and direct per-recipe correction are planned in the superseding
  allocation-removal handoff.

## Outcome

Refine the Shooter Workshop shooting-session workflow so users select an ammunition
recipe and quantity instead of entering batch or allocation IDs. ServerAPI and Domain
resolve that recipe quantity into concrete batch consumption from oldest production
date to newest, persist the resolved batch facts in the immutable ledger, and subtract
the corresponding recipe stock.

Replace the standalone raw-operation-ID correction form with an inline action on each
editable shooting-history row. The user can enter the corrected round count and save,
or remove the operation with a delete action. Entering zero follows the same deletion
path, but both zero-save and explicit delete require a confirmation that the shooting
record will be removed from the active history while its immutable audit facts remain.

Assumptions:

- Recipe consumption is resolved on the server at command execution; the browser never
  chooses or calculates batch IDs.
- FIFO means ascending `ProductionDate`, with batch ID as the deterministic tie-breaker.
- Batch availability and allocation integrity remain authoritative. The recommended
  eligibility policy consumes active allocations for the selected firearm first and
  then unallocated stock for the recipe; stock allocated to another firearm is not
  silently consumed.
- Inline correction edits the round quantity only, as requested. Firearm, effective
  date, notes, and recipe breakdown remain visible but unchanged in this bounded slice.
- A corrected positive quantity reuses the original session's recipe proportions only
  when there is exactly one recipe. Sessions with several recipes require explicit
  per-recipe corrected quantities rather than an invented distribution.
- Historical sessions that cannot be linked unambiguously to a ledger operation remain
  visible but read-only until repaired; ambiguous data is never guessed.
- This is an Impeccable **Operate** refinement of the incumbent Workshop interface, not
  a redesign.

The implementation boundary ends after recipe-based entry, server-owned FIFO
resolution, inline round correction/deletion, compatibility handling, focused tests,
and directly related documentation. It excludes bulk editing, undo, arbitrary
historical field editing, stock-adjustment redesign, and allocation-management
redesign.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` renders
  editable `Batch ID` and `Allocation ID` fields. These identifiers are internal
  persistence concepts and cannot reasonably be supplied by a user.
- The same component renders `Correct shooting operation` as a separate form requiring
  an `Original operation ID`; it does not load any shooting records and therefore
  cannot offer an inline correction.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` already
  displays a readable shooting history, but its rows have no update/delete actions.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/action.ts` records the
  entire firearm-detail shooting quantity as untracked ammunition. This second entry
  point would bypass recipe FIFO unless it is brought into the same command flow or
  redirected to the canonical shooting-session route.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` defines
  `WorkshopConsumptionRequest` with optional `BatchId`, quantity, and optional
  `AllocationId`. `RecordWorkshopShootingRequest` accepts these browser-authored
  records.
- `frontend/fe-packages/models/src/workshop.ts` mirrors that ID-based request and
  exposes shooting-session consumption as the same type, coupling write intent to
  resolved historical facts.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` maps browser
  consumption records directly into `ShootingConsumption` and exposes the existing
  correction endpoint under the authenticated, CSRF-protected Workshop controller.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` currently validates and consumes
  explicit batches. It checks each batch's `OnHandQuantity`, produces batch and optional
  allocation deltas, and persists the submitted records unchanged.
- `WorkshopService.CorrectShootingAsync` already implements immutable deletion when
  `Replacement` is null: it appends a `ShootingCorrected` operation containing reversal
  deltas. Positive corrections append reversal plus replacement deltas.
- `WorkshopService.BuildReplacementDeltasAsync` repeats the explicit-batch behavior for
  replacements, so FIFO resolution must be shared by record and correction paths.
- `AmmoBatch` persists `RecipeId`, `ProductionDate`, `OnHandQuantity`, and `Revision`;
  those fields are sufficient to identify deterministic recipe FIFO candidates.
- `AmmoAllocation` persists batch, firearm, active quantity, and revision. Current
  Domain behavior can reduce batch stock without reducing an allocation, so automatic
  recipe consumption must define allocation eligibility to prevent negative available
  stock or allocations exceeding remaining stock.
- `ShootingSession` stores resolved consumption records and an optional
  `CorrectionOfId`, but it does not store its own ledger operation ID.
- `WorkshopOperation` is the immutable authority and stores `OriginalOperationId`.
  The existing correction service accepts that operation ID, not a session ID.
- `WorkshopService.GetFirearmDetailAsync` calculates a `superseded` set but does not
  return it. `WorkshopController.FirearmDetail` currently constructs every
  `WorkshopShootingSessionContract` with `Superseded = false`.
- The current session/operation relationship is not recoverable with certainty from
  every persisted session because the original session document lacks an operation
  link. Firearm, date, and quantity matching can be ambiguous.
- ADR-027 requires immutable ledger facts, complete correction reversal, persisted
  accepted consumption attribution, optimistic projection updates, and explicit
  failures rather than guessed or partial results.
- `docs/feature-handoffs/shooter-workshop-ui-feedback-handoff.md` already states that
  correction must select a readable shooting-history record rather than require a raw
  operation ID.
- The Workshop has no local `PRODUCT.md` or `DESIGN.md`. The accepted handoffs identify
  `frontend/morwalpizvideo.client/DESIGN.md` as the incumbent visual authority.

## Proposed Design

### Command and fact separation

Introduce a recipe-consumption command DTO containing `RecipeId` and positive
`Quantity`. Use it in shooting create and replacement requests. Do not reuse the
persisted `ShootingConsumption` fact shape as a browser request.

Keep `ShootingConsumption` as the resolved historical fact with batch and optional
allocation IDs. The Domain service transforms recipe intent into one or more resolved
facts before calculating the payload hash, appending the operation, and persisting the
shooting-session read document. This preserves exact replay and correction evidence
without exposing internal identifiers in the UI.

For a transition window, ServerAPI may accept the existing ID-based fields only for
older clients if release sequencing requires it. New clients send recipe records.
The Domain command must receive one normalized representation so the legacy branch does
not duplicate ledger logic. Remove the legacy input only after deployed-client
compatibility is confirmed.

### Deterministic FIFO resolution

Add one Domain-owned resolver used by both new shooting and replacement correction:

1. Validate that the recipe belongs to the authenticated account and is usable for
   consumption. Preserve recipe identity even if the recipe was later archived when
   correcting an existing session.
2. Load account-owned batches for the recipe with positive replay/current stock.
3. Order candidates by production date ascending, then stable batch ID ascending.
4. Within each batch, consume an active allocation belonging to the selected firearm
   before unallocated batch stock. Never consume stock reserved for another firearm.
5. Split a recipe quantity across as many eligible batches as necessary.
6. Fail the whole command with a validation response when aggregate eligible stock is
   insufficient; do not append a partial operation.
7. Emit concrete batch and allocation deltas plus resolved `ShootingConsumption`
   records. Coalesce identical batch/allocation pairs for deterministic payloads.

Availability must be evaluated from the replay state used for the command, not trusted
from a stale browser response. Replacement correction resolves FIFO after applying the
original operation's reversal, matching the existing correction-validation order.

The normalized idempotency payload is the user's stable recipe intent, not a result
that may change after concurrent stock updates. An identical retry must return the
original operation. A new command losing an optimistic race must return conflict and
require reload/retry with a new idempotency key; it must not silently choose a
different stock result under the same accepted operation.

### Shooting read model and operation linkage

Add an optional `OperationId` to `ShootingSession` and populate it for every newly
recorded or replacement session. Expose a dedicated shooting-session response contract
that includes:

- session ID and ledger operation ID;
- firearm ID and display label;
- effective date, total rounds, notes, and untracked quantity;
- user-facing recipe consumption summaries derived from resolved batch snapshots;
- correction/deletion status;
- whether the row is editable and, when not, a safe reason.

Do not return account IDs or require the client to reconstruct recipe totals from batch
IDs. Keep resolved IDs available only if needed for diagnostics, not as editable form
fields.

Add an account-owned, bounded recent-shooting read for the shooting-session route, or
extend an existing bounded read if it can return the same explicit DTO without loading
all account operations. Join sessions to operations by `OperationId`; use operation
correction links to determine whether a row is current, corrected, or deleted.

For legacy sessions with no `OperationId`, perform an idempotent compatibility backfill
only when exactly one account-owned `ShootingRecorded` operation matches the session's
firearm, effective date, round delta, and resolved stock facts. Leave zero-match or
multi-match rows unlinked and read-only, log only operation-safe correlation metadata,
and report counts through migration/operational evidence.

### Inline correction and deletion

On the Shooting Sessions route, replace the separate correction form with a recent
session list under the create form. Each current editable row has:

- an `Update` action that reveals an inline number input prefilled with total rounds;
- `Save` and `Cancel` actions with pending and error feedback confined to the row;
- a `Delete` action;
- a status treatment for corrected/deleted/read-only records.

A positive save submits a correction using the row's ledger operation ID and a complete
replacement command. The browser must preserve the original immutable fields and send
explicit recipe quantities. When the original contains one recipe, changing total
rounds changes that recipe quantity after accounting for unchanged untracked rounds.
For multiple recipes, the inline editor exposes one quantity per existing recipe so
the invariant remains explicit. The sum of recipe quantities plus untracked rounds
must equal the corrected total.

Saving `0` and pressing `Delete` both open the same accessible confirmation dialog.
The message states that the session will disappear from active counters and stock will
be restored, while the audit record remains. Confirmation submits the existing
correction contract with no replacement. Cancellation restores the row without a
request. A deleted/superseded row cannot be corrected again.

After success, reload the recent-session resource and affected firearm/dashboard data
through the existing router/resource conventions. Do not optimistically invent stock
or counter values.

### UI direction

This is an established **Operate** surface and a functional refinement. Preserve the
incumbent Workshop panel, form, typography, button, focus, and responsive conventions.
The narrowest applicable Impeccable workflow is `shape` before implementation to
confirm inline row states, followed by `harden` for validation/conflict/deletion
states, `adapt` for responsive behavior, and `audit` before release.

Minimum visual evidence:

- desktop and 360 px captures of create form plus populated recent sessions;
- inline edit at rest, pending, validation error, conflict, and success;
- deletion confirmation opened by button and by zero-save;
- insufficient recipe stock and no-recipes empty states;
- keyboard focus order, dialog focus containment/restoration, visible focus, associated
  errors, 200% zoom, and no page-level horizontal overflow.

## Impacted Projects

- **MorWalPizVideo.Models** — additive shooting-session operation linkage and preserved
  resolved consumption facts.
- **MorWalPizVideo.Domain** — recipe FIFO resolution, allocation-safe availability,
  shared create/replacement logic, linkage/backfill, and shooting read projection.
- **MorWalPiz.Contracts** — separate recipe-intent requests from resolved shooting
  response contracts.
- **MorWalPizVideo.ServerAPI** — account-owned shooting reads and revised command
  mapping under existing auth/CSRF behavior.
- **MorWalPizVideo.BackOffice.Tests** — focused Domain, replay, compatibility,
  controller, security, and concurrency coverage for ServerAPI-owned behavior.
- **frontend/fe-packages/models** — matching request/read models.
- **frontend/fe-packages/services** — revised create/correct calls and recent-shooting
  read.
- **frontend/shooter-workshop.client** — recipe selection, recent-session rows, inline
  update/delete, confirmation, localization, and component tests.
- **MorWalPizVideo.BackOffice** — verify-only unless a new stable index is justified;
  its central Mongo index manifest is the operational governance surface.
- **MorWalPizVideo.AppHost** — verify-only for local client/API integration.
- **Workshop deployment workflow/container** — verify-only; no topology or secret
  change is expected.
- **Other frontend workspaces and standalone tools** — verify-only consumers of shared
  package builds; no behavior change is intended.

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — optional operation link
  on shooting-session read documents; keep resolved batch/allocation facts.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — recipe consumption
  request and explicit shooting-session response DTOs; compatibility input only if
  needed for staged deployment.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — shared FIFO resolver, command
  validation, correction reuse, session linkage, and bounded read projection.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — only if a bounded
  account/recipe/date query or conditional compatibility update is needed.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — revised
  request mapping, recent-session read, explicit DTO projection, and unchanged
  claim-derived ownership/CSRF enforcement.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — FIFO,
  multi-batch, allocation, correction, deletion, idempotency, and compatibility cases.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` — resolved batch and
  allocation reversal/replacement proof where existing replay coverage belongs.
- `frontend/fe-packages/models/src/workshop.ts` and
  `frontend/fe-packages/models/src/index.ts` — recipe-intent and explicit session-read
  types.
- `frontend/fe-packages/services/src/workshopService.ts`,
  `frontend/fe-packages/services/src/index.ts`, and
  `frontend/fe-packages/services/src/endpoints-frontend.ts` — revised mutations and
  bounded recent-session read.
- `frontend/fe-packages/services/src/workshopService.test.ts` — transport shape,
  CSRF, replacement, and delete request coverage.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` —
  canonical recipe-based entry and inline update/delete workflow.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/` route-local loader,
  action, and test files — add or modify according to the established Recipes and
  Ammunition route pattern.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — attach route data
  functions if the route moves from component-owned loading.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` and
  `action.ts` — remove the bypassing untracked-only quick form or route it into the
  canonical recipe-aware flow; render correction status consistently.
- `frontend/shooter-workshop.client/src/i18n.tsx` — English keys and complete Italian
  translations for recipe consumption, inline states, validation, and deletion
  confirmation.
- Existing Workshop style files owning panel, timeline, dialog, and responsive rules —
  add only the selectors needed for inline rows and confirmation behavior.
- `docs/architecture/shooter-workshop.md` — accepted recipe-intent/FIFO behavior,
  operation linkage, correction UX, and legacy compatibility.
- `docs/architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md` —
  clarify that user recipe intent is resolved to immutable FIFO batch facts and that
  delete remains a compensating correction.
- `docs/feature-handoffs/shooter-workshop-handoff.md` — link this focused follow-up and,
  after delivery, record actual validation and residual risks.

### Add

- A focused ServerAPI/controller test file only if existing Workshop test organization
  cannot clearly contain account isolation, CSRF, read projection, and malformed input
  scenarios.
- A feature-owned compatibility/backfill service only if startup/request-time bounded
  repair cannot meet operational safety; do not create an unbounded UI-triggered scan.

### Generated/Migration

- No destructive MongoDB migration.
- Optional idempotent operation-link backfill artifact or support operation, with dry
  run counts and ambiguous-row reporting, if existing data requires repair.
- A stable Mongo index definition only after the final query shape demonstrates need;
  if added, update the Domain manifest, BackOffice executable manifest, reviewed JSON
  snapshot, and index tests together.

### Documentation

- This handoff.
- The architecture and ADR files listed above.

### Verify Only

- `frontend/package.json` shared-package build order.
- `MorWalPizVideo.AppHost/Program.cs`.
- `.github/workflows/ci.yml`.
- `.github/workflows/deploy-shooter-workshop-client.yml`.
- `frontend/shooter-workshop.client/Dockerfile`.
- `frontend/shooter-workshop.client/nginx.conf`.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs` unless an index is
  added.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Revise shooting create/replacement inputs, add bounded account-owned shooting reads, and keep immutable correction semantics. |
| Frontend/UI | Replace ID fields and standalone correction form with recipe rows and inline update/delete. |
| Database/Data | Add optional operation linkage to shooting-session read documents; preserve resolved FIFO facts; optionally backfill unambiguous legacy rows. |
| Contracts/Models | Separate user recipe intent from resolved batch/allocation history in both .NET and TypeScript. |
| Authentication/Authorization | No boundary change: public-account scheme, claim-derived account ID, account-filtered IDs, and CSRF on every mutation. |
| Validation | Positive recipe quantities, exact total invariant, account-owned recipe/firearm, sufficient eligible stock, deterministic FIFO, and no second correction. |
| Cache | Not applicable; current private Workshop reads do not use output-cache tags. Client resources must reload after mutation. |
| Configuration/Secrets | Not applicable; no new configuration or secret is required. |
| Logging/Telemetry | Record operation-safe recipe/batch counts, conflicts, insufficient-stock failures, and legacy-link outcomes; exclude account identity and free-text notes. |
| Background Jobs | Not required for normal commands. A bounded support backfill may be operationally invoked but must be idempotent and observable. |
| Deployment/Operations | Deploy compatible backend/contracts before the new client; audit any new index; observe conflicts and unlinked legacy rows. |
| Backward Compatibility | Additive persisted field; tolerate missing operation links; stage request compatibility if old clients can remain active. |

## Reuse Opportunities

- Reuse `WorkshopService.CorrectShootingAsync` for both positive corrections and
  deletion; do not introduce physical operation deletion.
- Reuse its existing reversal-before-replacement replay state.
- Extract one Domain FIFO resolver used by `RecordShootingAsync` and
  `BuildReplacementDeltasAsync`; do not duplicate batch walking.
- Reuse `GetRecipesAsync`, recipe ownership checks, batch projections, allocation
  entities, optimistic projection revisions, and existing Workshop exceptions.
- Reuse the account-authenticated Workshop controller and `HasCsrf` mutation gate.
- Follow the route-local loader/action/fetcher conventions in Recipes and Ammunition
  for pending/error state rather than adding a global store.
- Reuse `WorkshopForm`, existing button/panel styles, `ResourceState`, localization,
  formatting helpers, and the existing accessible application shell.
- Reuse `MorWalPizVideo.BackOffice.Tests` mock repository and ServerAPI factory.

## Risks

- **High — allocation corruption:** FIFO could consume stock reserved for another
  firearm while leaving its allocation active. Mitigation: define eligible stock
  explicitly, consume same-firearm allocation first, exclude other allocations, and
  prove batch/allocation projections after replay. Signal: no allocation exceeds its
  batch's remaining stock and dashboard available stock never becomes negative.
- **High — ambiguous legacy linkage:** session rows lack operation IDs, so a guessed
  match could correct the wrong ledger fact. Mitigation: link only unique full matches;
  leave ambiguous rows read-only. Signal: dry-run matched/unmatched/ambiguous counts and
  a test with identical-date/quantity sessions.
- **High — concurrent FIFO races:** two commands can select the same old stock.
  Mitigation: retain optimistic projection revisions and explicit conflict; verify no
  partial append/projection success. Signal: concurrent test yields one accepted
  result and one conflict or a consistent retry outcome.
- **Medium — correction recipe ambiguity:** a single corrected total cannot determine
  how several recipes should change. Mitigation: expose per-recipe quantities for
  multi-recipe rows and validate the exact sum. Signal: no proportional or implicit
  redistribution occurs.
- **Medium — idempotency drift:** recomputing FIFO on retry after stock changes could
  produce a different payload. Mitigation: check accepted idempotency before new
  resolution where possible and hash stable user intent plus committed resolved facts
  consistently. Signal: identical retry returns the original operation; changed intent
  conflicts.
- **Medium — staged contract rollout:** a new client can fail against an old API, or an
  old client can send batch IDs to a new API. Mitigation: backend-first deployment and
  a bounded compatibility window. Signal: contract tests for both accepted shapes
  during the window.
- **Medium — delete misunderstanding:** users may expect audit history to be erased.
  Mitigation: confirmation copy states active counters/stock are reversed while audit
  facts remain. Signal: usability check and activity history still shows correction.
- **Low — responsive row overflow:** recipe summaries and inline controls can overflow
  mobile widths. Mitigation: stacked mobile topology and long-label tests. Signal:
  360 px and 200% zoom evidence without page-level horizontal scrolling.
- **Low — external services:** none are involved; no external-service risk identified.

## Technical Debt

- `WorkshopController.FirearmDetail` hardcodes `Superseded = false` despite Domain
  attempting to calculate superseded records. This directly blocks correct inline
  state and must be fixed in scope.
- Shooting-session persistence lacks an operation link. Address additively now because
  correction discovery cannot be reliable without it.
- Domain reads currently materialize complete account collections before filtering and
  paging. Keep this change bounded, but introduce repository-side recipe/date or recent
  session queries if account data can make FIFO or recent reads unbounded.
- The firearm-detail quick shooting form bypasses the structured ammunition workflow.
  Remove or redirect it in scope; do not maintain two inconsistent entry semantics.
- Request and persisted fact types are currently conflated. Separate them now because
  it is the root cause of browser-authored internal IDs.
- Workshop lacks its own `PRODUCT.md` and `DESIGN.md`. This does not block the
  refinement; use the incumbent authority and run Impeccable `document` only after the
  finished surface is accepted.
- Broader allocation-management usability and history pagination cleanup do not block
  this work and remain deferred.

## Implementation Roadmap

1. **Confirm command semantics and compatibility window.** Finalize allocation
   eligibility, multi-recipe correction behavior, and whether any deployed old client
   requires temporary ID-based input. Record the decisions in the architecture
   specification before contract implementation.
2. **Separate contracts.** Add recipe-consumption command DTOs and explicit shooting
   read DTOs in .NET, then mirror them in the shared TypeScript model package. Keep
   owner IDs claim-derived and keep resolved batch facts out of editable browser input.
3. **Add operation linkage compatibility.** Extend `ShootingSession` with an optional
   operation ID, populate it for new writes, and implement unique-only legacy linking
   with observable unmatched/ambiguous results. Prove old documents remain readable.
4. **Implement the shared FIFO resolver.** Resolve account-owned recipe quantities
   against replay/current batch and allocation state in stable order, emit complete
   deltas/facts, and fail atomically on insufficient eligible stock or ambiguity.
5. **Integrate create and correction.** Use the same resolver for recording and
   replacement after reversal. Preserve delete as a null replacement, existing
   idempotency behavior, correction-chain protection, and projection recovery.
6. **Expose the read model.** Add a bounded recent-session read with operation linkage,
   firearm/recipe display data, and corrected/deleted/editable status. Fix firearm
   detail to report real superseded state rather than hardcoded false.
7. **Update ServerAPI mappings and security tests.** Accept normalized recipe intent,
   project explicit responses, retain account authentication and CSRF, and reject
   cross-account firearm, recipe, batch, session, and operation references.
8. **Update shared frontend services.** Implement revised record/correct bodies and the
   recent-session read, with transport tests for CSRF, positive replacement, and null
   replacement.
9. **Shape and implement the create workflow.** Use recipe selectors and quantity
   fields, show available recipe-level stock as advisory only, preserve server
   authority, and remove all batch/allocation ID inputs.
10. **Implement inline correction/delete.** Render recent rows, row-local update state,
    positive save, zero/delete confirmation, pending/errors, success reload, corrected
    status, and read-only legacy state. Remove the standalone correction form.
11. **Close the second entry-point gap.** Redirect firearm-detail recording to the
    canonical structured route with firearm preselection, or give it the same recipe
    semantics without duplicating business logic.
12. **Harden and localize.** Add complete Italian copy, accessible confirmation
    behavior, keyboard/focus handling, conflict/insufficient-stock messaging, empty
    states, and responsive layouts.
13. **Run focused validation gates.** Prove Domain/replay/controller behavior first,
    then shared service/model builds, route component tests, Workshop build, and the
    minimum visual/responsive/accessibility evidence.
14. **Update persistent documentation and rollout evidence.** Record actual files,
    validation, compatibility/backfill counts, any index decision, and residual risks
    in this handoff and the broader Workshop handoff.

Completion requires recipe-only user input, deterministic and allocation-safe FIFO
facts, immutable positive/delete correction behavior, no raw operation-ID entry,
readable inline states, backward-readable stored data, and passing focused validation.
The implementer must stop there; further editing capabilities require separate
approval.

An independent post-validation review is warranted because this change modifies
inventory integrity, immutable correction chains, compatibility data, security-scoped
reads, and a multi-state destructive UI. The user may select the MorWalPiz Code
Reviewer after implementation.

## Testing Strategy

- **Domain/unit — MorWalPizVideo.BackOffice.Tests:** one recipe/one batch; exact
  depletion; one recipe split across several batches; equal-date stable ordering;
  insufficient aggregate stock; archived/missing/cross-account recipe; same-firearm
  allocation consumed before unallocated stock; other-firearm allocation excluded;
  untracked plus recipe sum invariant; several recipes; create idempotent retry and
  payload conflict.
- **Correction/replay — MorWalPizVideo.BackOffice.Tests:** positive increase and
  decrease; reversal restores original batch/allocation projections before replacement
  FIFO; delete restores counters and stock; zero maps to delete at the UI boundary;
  second correction rejected; corrected/deleted rows classified correctly; full replay
  matches projections.
- **Concurrency/faults — MorWalPizVideo.BackOffice.Tests:** competing FIFO consumers,
  projection revision conflict, failure after ledger append, retry recovery, and no
  partial success response.
- **Compatibility/migration — MorWalPizVideo.BackOffice.Tests:** missing optional
  operation ID reads safely; unique legacy match links; zero/multiple matches remain
  read-only; old ID-based request accepted only when compatibility is enabled; new
  writes always persist links and resolved facts.
- **Controller/security — ServerAPI test factory:** unauthenticated read/mutation,
  missing or invalid CSRF, claim-derived account ownership, cross-account IDs,
  malformed/zero/negative recipe quantities, missing operation, already-corrected
  operation, and null replacement deletion.
- **Shared service contracts — frontend/fe-packages/services:** recent read URL and
  shape, recipe request body, corrected replacement body, null replacement body, CSRF
  transport, and surfaced validation/conflict failures.
- **Route/component — shooter-workshop.client:** recipe add/remove rows; no internal ID
  inputs; total validation; inline open/cancel/save; multi-recipe editing; explicit
  delete; zero-save confirmation; cancel confirmation; pending lockout; server error
  retention; success reload; deleted/read-only status; localized accessible names.
- **Accessibility/responsive:** keyboard-only creation and correction, confirmation
  dialog focus trap/restoration, screen-reader status/error announcements, associated
  labels, visible focus, touch target sizing, 200% zoom, and 360 px through desktop.
- **End to end:** create two batches of one recipe on different dates, record a quantity
  spanning both, verify oldest-first stock; correct downward and verify restoration;
  correct upward and verify FIFO; delete and verify firearm/component/recipe inventory
  restoration plus retained audit activity; repeat with another firearm's allocation.
- **Performance:** verify bounded recent-session and FIFO batch queries at realistic
  personal-account maximums; add an index only if query evidence requires one.

## Migration And Rollback

### MongoDB

The schema change is additive and tolerant: old shooting-session documents omit
`OperationId`; new readers must accept that absence. New writes populate it. Any
backfill is idempotent, account-scoped, unique-match-only, and reports ambiguous rows
without changing them. No ledger fact is edited or deleted.

Deploy compatible readers and writers before backfill. If a new index is justified,
run duplicate/query audits before apply through existing Workshop index governance.

Rollback of the UI is independent while the compatibility request shape remains
accepted. Backend rollback is limited to versions that tolerate the optional persisted
field and all ledger schema versions written during rollout. Because resolved
consumption remains the existing persisted fact shape, no destructive down-migration
is required.

### API contracts

Deploy backend and shared contracts before the client. Retain old ID-based request
acceptance only for the measured compatibility window, while responses move additively
to explicit recipe summaries and operation status. Remove legacy input in a separate
approved cleanup after deployment evidence shows no old callers.

### Authentication and cache

Authentication, CSRF, CORS, and claim-derived ownership do not migrate. There is no
server output-cache migration. Client data is reloaded after mutation.

### Configuration

No feature flag or secret is required. If operational risk warrants staged activation,
an existing repository-standard feature flag may gate recipe FIFO acceptance, but it
must not create dual ledger meanings for the same request shape.

### Deployment and monitoring

Deploy in this order: compatible models/Domain/contracts/ServerAPI, optional index,
shared frontend packages and Workshop client, then compatibility backfill. Monitor
validation failures, optimistic conflicts, projection inconsistency, insufficient
stock, correction rejection, and linked/unlinked legacy counts.

Rollback the client first if UX defects occur. Disable new shooting writes if inventory
or replay signals diverge, reconcile projections from the immutable ledger, and repair
only read projections/link metadata. Never repair by editing ledger deltas.

## Open Questions

1. Confirm the allocation policy: should recipe FIFO consume same-firearm allocations
   first and exclude stock allocated to other firearms, as recommended, or should all
   recipe stock be consumable regardless of allocation?
2. For an original session containing several recipes, should inline correction expose
   each recipe quantity, or should that uncommon case remain read-only until a broader
   editor is approved? This handoff recommends explicit per-recipe quantities.
3. Are any older Workshop client versions concurrently deployed? The answer determines
   whether temporary ID-based request compatibility is necessary.
4. Does production contain shooting-session documents that require operation-link
   backfill, or can compatibility begin with no historical data? This changes rollout
   work but not the target architecture.
