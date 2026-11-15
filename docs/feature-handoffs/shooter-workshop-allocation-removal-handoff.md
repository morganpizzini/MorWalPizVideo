# Shooter Workshop Allocation Removal Handoff

## Outcome

Remove ammunition allocations—the reservation of batch stock for a firearm—from the
Shooter Workshop product model. Ammunition is described by its recipe and physically
stored in production batches; it is not associated with a firearm before consumption.
All new stock availability therefore equals total batch on-hand quantity, and shooting
consumes selected recipe quantities from oldest batch to newest without consulting,
creating, consuming, releasing, or moving allocation records.

Correct a shooting session by editing its explicit per-recipe quantities. Do not ask
for a separate total and do not redistribute quantities proportionally. Preserve the
original untracked-ammunition quantity unchanged during a positive correction. The
server derives total rounds as:

`sum(corrected recipe quantities) + original untracked ammunition`.

Delete remains an explicit action, or the result of setting every recipe quantity to
zero when original untracked ammunition is also zero, and requires confirmation.

Assumptions:

- Existing immutable allocation operations may exist in deployed MongoDB and must
  remain deserializable, replayable, and auditable.
- Historical allocation projection documents are compatibility data, not current
  product state after cutover.
- “Remove from the codebase” means remove active allocation product behavior, routes,
  contracts, UI, current projections, and dashboard semantics. Legacy ledger fields and
  numeric enum positions remain in a quarantined compatibility reader until deployed
  data proves they can be retired safely.
- Positive correction does not edit firearm, effective date, notes, or untracked
  ammunition in this slice.
- The tagged firearm-detail refinement handoff does not depend on allocation behavior
  and remains otherwise unchanged.
- This is an Impeccable **Operate** refinement, not a visual redesign.

Completion stops after allocation-free current behavior, safe legacy interpretation,
explicit recipe correction, focused migration evidence, and documentation. Physical
deletion of historical ledger facts or the Mongo allocation collection is not
authorized by this handoff and requires a separate operational decision.

## Implementation status

The active cutover is implemented in the current working tree:

- dashboard availability now reports total batch on-hand quantity and no longer
  subtracts allocation projections;
- allocation mutation routes, shared allocation transport exports, and the
  allocation SPA route/form are retired;
- immutable allocation fields, enum values, replay digest inputs, and persisted
  allocation documents remain readable for historical data;
- reconciliation no longer repairs allocation projections as current product state;
- shooting correction edits each existing recipe quantity independently, keeps
  untracked ammunition read-only and unchanged, derives replacement total rounds
  server-side, and confirms deletion when all tracked and untracked quantities are
  zero;
- duplicate recipe requests are normalized before FIFO resolution, and correction
  session facts reuse the same resolved consumption used to commit ledger deltas.

The allocation collection and legacy allocation command implementation remain in
the domain only as compatibility infrastructure. Physical collection cleanup and
removal of legacy command code require deployed-client and data-retention evidence,
so they are intentionally outside this implementation.

The shooting form refinement also removes the editable total-round input. The
displayed total is calculated from the selected recipe quantities and untracked
quantity; recipe choices are populated from non-archived recipes, ammunition rows
keep removal inline with the quantity field, and the submit action is right-aligned
with deliberate top spacing.

## Current Evidence

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` defines
  `AllocationRecorded`, `AllocationReleased`, and `AllocationReallocated` operation
  types; `WorkshopAggregateKind.Allocation`; persisted `AmmoAllocation`; optional
  `ShootingConsumption.AllocationId`; and allocation delta fields in immutable
  `WorkshopDelta`.
- `MorWalPizVideo.Models/Constraints/DbCollections.cs` declares
  `workshopAmmoAllocations`.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` owns allocate, release, and
  reallocate commands and uses allocation projections to calculate dashboard
  availability.
- The same service’s current recipe FIFO path creates batch deltas only. It does not
  consume allocation projections, so current shooting behavior already conflicts with
  dashboard reservation semantics.
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs` includes allocation values in
  replay and deterministic contribution digests; reconciliation compares and repairs
  existing allocation projections.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` recognizes allocation
  projections in the mock repository. No allocation-specific Mongo index exists, but
  operation delta indexes cover historical allocation aggregates.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` exposes allocate,
  release, and reallocate requests and `AllocatedRounds` on the dashboard response.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` exposes three
  authenticated, CSRF-protected allocation mutation routes.
- `frontend/fe-packages/models/src/workshop.ts`,
  `frontend/fe-packages/services/src/workshopService.ts`, and their barrel exports
  expose the matching allocation transport.
- `frontend/shooter-workshop.client/src/routes/Allocation/component.tsx` is a complete
  allocation form but asks users for allocation IDs and revisions because no allocation
  list/read API exists.
- `frontend/shooter-workshop.client/src/routes/config.tsx` registers
  `/ammunition/allocations`; the main navigation does not link to it.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` displays
  server-computed available stock. The server currently subtracts active allocations
  even though FIFO shooting can consume the same batch without changing them.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx`
  currently edits a separate total and proportionally redistributes multiple recipe
  quantities. It changes untracked ammunition to the lesser of the original value and
  corrected total.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/action.ts` records its
  quick shooting entry entirely as untracked ammunition, bypassing recipe entry.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` and
  `WorkshopServiceTests.cs` contain allocation replay and mutation coverage that must
  become legacy-compatibility coverage rather than be deleted indiscriminately.
- ADR-027 makes immutable operations authoritative. Historical allocation operations
  cannot be rewritten or deleted to simplify the new model.
- Actual deployed allocation volume, active allocation state, BSON enum
  representation, and old-client usage are not established by repository source.

## Proposed Design

### Current product model

The current product model has only:

- recipe: the ammunition specification;
- batch: a produced quantity of one recipe with production date and on-hand stock;
- shooting consumption: recipe quantities resolved server-side into FIFO batch facts;
- untracked ammunition: rounds intentionally outside recipe inventory.

There is no reservation, allocation, firearm-specific ammunition stock, allocated
dashboard count, or allocation workflow.

`AvailableRounds` becomes the sum of all batch `OnHandQuantity`. The API should
eventually rename it to `OnHandRounds` only if a broader dashboard contract change is
approved; this removal can preserve the existing field name while changing and
documenting its meaning.

### Legacy compatibility boundary

Do not remove or reorder persisted enum values. Do not remove legacy allocation fields
from the BSON operation reader while historical documents may contain them. Isolate
legacy interpretation behind replay/correction compatibility:

- allocation operations and deltas remain readable and retain their original digest;
- current dashboard and stock availability ignore allocation projections;
- current reconciliation does not require an allocation projection to declare current
  recipe/batch/firearm/component projections healthy;
- correction of a historical allocation-bearing shooting operation reverses its exact
  legacy deltas for audit/replay compatibility but never creates a new allocation;
- no new command emits allocation aggregates or allocation IDs;
- legacy allocation activity may remain readable as historical text but is not an
  actionable workflow.

Use the existing operation schema version as the compatibility discriminator where
necessary. Unknown versions must fail explicitly rather than silently dropping
allocation facts.

### Expand, migrate, contract

1. **Expand:** deploy readers/replay/correction that understand both legacy
   allocation-bearing facts and allocation-free current facts. Add explicit tests
   before disabling any route.
2. **Stop writes:** remove the allocation route from the current client and make
   allocate/release/reallocate API commands return an explicit retirement response
   during a bounded old-client compatibility window.
3. **Cut over semantics:** stop subtracting allocation projections from dashboard
   availability and stop reconciling them as current product projections. Existing
   batch on-hand stock is unchanged; all remaining on-hand stock becomes generally
   available.
4. **Contract transports/UI:** remove the allocation route, component, API actions,
   request DTOs, shared service methods, public exports, translations, and current
   activity labels. Preserve only historical display fallback where required.
5. **Archive projections:** stop registering `AmmoAllocation` as a current mock or
   reconciliation projection. Retain BSON compatibility and the collection constant
   until deployment evidence and backup policy authorize physical retirement.

Do not append synthetic release operations merely to neutralize reservations. The
accepted new semantic is that allocations cease to affect current availability at the
cutover; rewriting every account’s immutable history would add misleading business
events.

### Recipe-only correction

The inline correction editor lists every recipe recorded on the session with its
current quantity. The user edits those quantities directly:

- no total-round input;
- no proportional redistribution;
- no batch or allocation IDs;
- original untracked ammunition is displayed read-only and sent unchanged;
- the browser may show the derived total, but the server calculates and validates it;
- duplicate rows for the same recipe are normalized into one recipe quantity before
  submission;
- each positive recipe quantity is independently resolved through FIFO after reversing
  the original operation;
- zero recipe quantities are omitted from the replacement request;
- if no recipe quantities remain and original untracked is zero, saving follows the
  confirmed delete path;
- if no recipe quantities remain but original untracked is positive, the corrected
  session remains valid with total equal to unchanged untracked ammunition;
- the explicit Delete button always submits a null replacement after confirmation.

Resolve replacement FIFO once and reuse the same resolved facts for both committed
deltas and the replacement `ShootingSession` read document. Do not perform a second
post-commit FIFO resolution.

### Impeccable direction

The target remains an established **Operate** surface using the incumbent Workshop
visual system. Apply `shape` to the per-recipe inline editor, `harden` to zero/delete,
legacy read-only, insufficient-stock, conflict, and pending states, `adapt` at desktop,
tablet, 480 px, and 360 px, and `audit` before release.

Minimum visual evidence includes a single-recipe correction, several-recipe correction,
read-only untracked ammunition, corrected/deleted status, confirmation, validation
error, conflict, keyboard operation, 200% zoom, and mobile layout without page-level
horizontal overflow.

## Impacted Projects

- **MorWalPizVideo.Models** — legacy allocation persistence compatibility and removal
  from the current domain model.
- **MorWalPizVideo.Domain** — commands, dashboard semantics, replay, reconciliation,
  correction compatibility, repository projection handling, and one-pass FIFO.
- **MorWalPiz.Contracts** — removal/deprecation of allocation mutations and
  `AllocatedRounds`; recipe-only correction request/read semantics.
- **MorWalPizVideo.ServerAPI** — retire allocation routes while retaining auth/CSRF
  conventions for remaining mutations.
- **MorWalPizVideo.BackOffice.Tests** — migration, replay, controller, correction,
  stock, and compatibility coverage.
- **frontend/fe-packages/models** — remove allocation request/current dashboard types.
- **frontend/fe-packages/services** — remove allocation methods and endpoint exports.
- **frontend/shooter-workshop.client** — remove allocation route/UI/copy and replace
  proportional correction with explicit recipe quantities.
- **MorWalPizVideo.BackOffice** — verify central index governance only; no product
  allocation behavior belongs there.
- **MorWalPizVideo.AppHost and deployment workflows** — verify-only; no topology,
  secret, or environment change.
- **Other frontend consumers** — verify shared-package compilation; no intended
  behavior change.

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — mark allocation members
  as legacy compatibility; preserve enum numeric positions and historical readers.
- `MorWalPizVideo.Models/Constraints/DbCollections.cs` — retain the collection constant
  through compatibility; remove only in a separately authorized physical contraction.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — remove active allocation
  commands/availability semantics, preserve historical correction, normalize recipe
  quantities, and resolve replacement FIFO once.
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs` — distinguish current projections
  from legacy allocation facts without changing historical digest interpretation.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — retire allocation as a
  current projection while preserving legacy reads during migration.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — retire allocation
  requests and dashboard allocation count; make corrected totals server-derived.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — compatibility
  retirement responses followed by route removal; updated dashboard/correction mapping.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — stock cutover,
  route retirement, explicit recipe corrections, duplicate recipe normalization, and
  one-pass FIFO.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` — legacy allocation
  replay/correction compatibility and allocation-free current reconciliation.
- `frontend/fe-packages/models/src/workshop.ts` and `src/index.ts` — remove active
  allocation types/exports and revise correction/dashboard shapes.
- `frontend/fe-packages/services/src/workshopService.ts`,
  `src/endpoints-frontend.ts`, `src/index.ts`, and `src/workshopService.test.ts` —
  remove active allocation transport and test retired/current behavior as applicable.
- `frontend/shooter-workshop.client/src/routes/config.tsx`, `pages.tsx`, and
  `config.test.tsx` — remove the allocation route and export.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` — direct
  per-recipe correction and read-only unchanged untracked quantity.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` and
  `action.ts` — remove allocation language and route quick shooting into the canonical
  recipe-aware form.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` — preserve
  available/on-hand display with allocation-free meaning.
- `frontend/shooter-workshop.client/src/routes/shared.tsx` — keep safe historical
  allocation activity labels only if old activity remains visible; remove current
  action semantics.
- `frontend/shooter-workshop.client/src/i18n.tsx` — remove allocation workflow strings
  and add explicit correction/read-only untracked copy in English and Italian.
- `docs/architecture/shooter-workshop.md` and
  `docs/architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md`
  — current/target distinction, migration, replay, correction, and rollback.
- `docs/feature-handoffs/shooter-workshop-shooting-session-fifo-inline-correction-handoff.md`
  — supersession note and delivered/follow-up status.
- `docs/feature-handoffs/shooter-workshop-handoff.md` — link this active follow-up.

### Add

- Focused route/component tests for recipe correction if no established shooting-route
  test file exists.
- A bounded allocation inventory/report artifact only if deployed Mongo inspection
  confirms active allocation data requiring operational evidence.

### Generated/Migration

- No destructive Mongo migration is authorized.
- No synthetic allocation-release ledger migration is recommended.
- Optional read-only inventory report: accounts, allocation documents, active
  quantities, allocation-bearing shooting operations, and affected schema versions.

### Documentation

- This handoff plus the architecture, ADR, and predecessor handoffs listed above.

### Verify Only

- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`.
- `MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs`.
- `MorWalPizVideo.AppHost/Program.cs`.
- `frontend/package.json`.
- `.github/workflows/ci.yml`.
- `.github/workflows/deploy-shooter-workshop-client.yml`.
- Workshop container/nginx files.
- `docs/feature-handoffs/shooter-workshop-firearm-detail-refinement-handoff.md`.

### Delete

- `frontend/shooter-workshop.client/src/routes/Allocation/component.tsx` after the
  compatibility deployment no longer serves the current route.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Retire three allocation mutations; redefine dashboard availability; preserve legacy correction/replay. |
| Frontend/UI | Remove allocation route/form/copy; edit recipe quantities directly; show untracked as read-only. |
| Database/Data | Preserve immutable allocation facts; stop treating allocation projections as current state; no destructive collection deletion. |
| Contracts/Models | Remove active allocation DTOs and dashboard count after compatibility; retain legacy BSON shape. |
| Authentication/Authorization | No boundary change; remaining Workshop reads/mutations stay account-scoped with CSRF on mutations. |
| Validation | Positive recipe quantities, duplicate normalization, server-derived total, unchanged untracked quantity, sufficient FIFO stock, explicit delete. |
| Cache | Not applicable; private Workshop reads are not output-cached. |
| Configuration/Secrets | No new configuration or secrets. |
| Logging/Telemetry | Count retired-route calls, legacy allocation facts, stock conflicts, and compatibility failures without account PII or notes. |
| Background Jobs | None required; any allocation inventory is a bounded operational report, not a recurring job. |
| Deployment/Operations | Backend compatibility precedes client removal and contract contraction; physical data deletion remains separate. |
| Backward Compatibility | Historical enum values, deltas, digests, operations, corrections, and activity remain readable. |

## Reuse Opportunities

- Reuse immutable correction reversal and replacement rather than editing sessions.
- Reuse the server-owned FIFO resolver for create and correction.
- Reuse `WorkshopOperation.SchemaVersion` for compatibility interpretation.
- Reuse account authentication, CSRF, repository ownership filtering, idempotency, and
  conflict responses.
- Reuse recipe selectors, route-local loading/action patterns, `WorkshopForm`, panels,
  buttons, errors, localization, and formatting.
- Retain existing allocation replay tests as legacy-reader fixtures instead of deleting
  the only compatibility evidence.

## Risks

- **High — historical replay break:** removing enum values or delta fields can corrupt
  deserialization/digests. Mitigation: freeze numeric identities and retain legacy
  readers. Signal: archived allocation fixtures replay to the same contribution digest.
- **High — correction break:** historical shooting operations may contain allocation
  deltas. Mitigation: reverse exact legacy deltas but never emit new allocations.
  Signal: legacy correction restores batch/firearm/component values and remains
  replayable without an active allocation projection.
- **High — mixed-version clients:** an old client may call removed routes. Mitigation:
  backend retirement response before route deletion and telemetry for remaining calls.
  Signal: zero retired-route calls for the agreed window.
- **Medium — availability semantic change:** dashboard values increase when active
  reservations stop reducing availability. Mitigation: document cutover and verify
  on-hand totals against batches. Signal: dashboard available equals batch on-hand sum.
- **Medium — correction mismatch:** current code resolves replacement FIFO twice.
  Mitigation: resolve once and persist the committed facts. Signal: replacement session
  consumption exactly matches operation batch deltas.
- **Medium — duplicate recipe rows:** repeated recipe requests can overrun locally
  calculated stock. Mitigation: normalize by recipe before FIFO. Signal: duplicate-row
  insufficient-stock test fails atomically.
- **Medium — rollback semantics:** an older server may subtract retained allocation
  projections again. Mitigation: rollback only to a compatibility-aware version after
  cutover. Signal: rollback rehearsal produces the same availability.
- **Low — historical activity wording:** allocation activity remains visible after the
  workflow disappears. Mitigation: label it explicitly as historical. Signal: history
  has readable fallback without actionable controls.
- **Low — responsive correction density:** multiple recipe inputs may overflow.
  Mitigation: stacked rows and responsive evidence. Signal: 360 px and 200% zoom pass.
- **External-service risk:** Not applicable; no external service is involved.

## Technical Debt

- Allocation behavior is internally inconsistent: FIFO ignores reservations while the
  dashboard subtracts them. This blocks reliable stock reporting and is fixed now.
- Allocation UI has no read/list API and asks for internal IDs. Remove it rather than
  expanding a rejected feature.
- Allocation fields are embedded in immutable history. They cannot be completely
  erased from source safely without deployed-data evidence; quarantine them as legacy
  compatibility rather than claiming physical deletion.
- Current multi-recipe correction invents proportions and changes untracked
  ammunition. Replace it now because it contradicts the clarified requirement.
- Firearm-detail quick shooting bypasses recipe selection. Redirect it now; do not
  maintain a second semantic path.
- Reconciliation only compares existing projection documents and schema-version
  handling is incomplete. Address only the allocation compatibility needed here; track
  broader reconciliation hardening separately.

## Implementation Roadmap

1. **Inventory deployed compatibility.** Establish whether old clients call allocation
   routes and whether Mongo contains allocation documents or allocation-bearing
   shooting operations. Record counts, schema versions, and backup/retention policy.
2. **Freeze legacy identities.** Make enum numeric values explicit if they are not
   already, retain legacy BSON fields, and add fixtures proving unchanged replay and
   digest behavior.
3. **Expand replay/correction.** Separate current projection handling from legacy
   allocation interpretation. Prove historical corrections work without creating new
   allocations or requiring current allocation projections.
4. **Fix current stock semantics.** Define available stock as total batch on-hand,
   remove allocation subtraction, and verify dashboard/reconciliation consistency.
5. **Retire allocation commands.** Stop new allocate/release/reallocate writes with an
   explicit compatibility response while retaining old-client observability.
6. **Remove allocation UI and transport.** Delete the route/component and remove
   frontend services, contracts, exports, translations, tests, and firearm copy.
7. **Contract backend API/current models.** Remove active request DTOs, service
   commands, controller actions, dashboard allocation count, and current repository
   projection registration after the compatibility window.
8. **Implement direct recipe correction.** Replace total input with per-recipe
   quantities, keep untracked read-only/unchanged, derive total server-side, normalize
   duplicates, and preserve confirmed deletion.
9. **Resolve FIFO once.** Share the resolved replacement facts between operation
   commit and replacement-session persistence; add insufficient-stock and concurrency
   coverage.
10. **Close the firearm-detail bypass.** Link to the canonical shooting route with
    firearm preselection rather than recording all rounds as untracked.
11. **Run migration gates.** Validate legacy replay/correction, current Domain/API,
    shared packages, Workshop tests/build, and visual/accessibility states before
    contracting compatibility.
12. **Document actual cutover.** Update this handoff with deployed evidence, retained
    compatibility fields, validation, route-call telemetry, rollback version, and
    residual risks.

An independent post-validation review is warranted because the work changes immutable
history interpretation, dashboard stock semantics, API compatibility, and destructive
UI affordances. The user may select MorWalPiz Code Reviewer after implementation.

## Testing Strategy

- **Legacy model/replay:** deserialize allocation entities, enum values, operation
  deltas, and shooting allocation IDs; preserve digest and replay outcomes.
- **Domain migration:** dashboard on-hand totals ignore active allocations; no new
  operation contains allocation aggregates; reconciliation succeeds without current
  allocation projections.
- **Legacy correction:** correct/delete allocation-bearing shooting operations; exact
  reversal, no new allocation, idempotent retry, and missing projection behavior.
- **Recipe correction:** one and several recipes, quantity increase/decrease, unchanged
  untracked quantity, no recipe with positive untracked, all-zero delete, duplicate
  recipe normalization, insufficient stock, and one-pass resolved evidence.
- **API/security:** retired allocation routes during compatibility, removed routes
  after contraction, authentication, CSRF, claim-derived ownership, malformed recipe
  quantities, and cross-account references.
- **Frontend shared contracts:** no allocation exports/calls; dashboard and correction
  bodies match the contracted API.
- **Component:** no total input; each recipe is editable; untracked is read-only;
  derived total updates; delete confirmation; validation/conflict/pending/read-only
  states; success reload.
- **End to end:** produce batches, record recipe consumption, correct recipe quantities,
  verify FIFO stock/counters, delete, verify restoration/audit, and confirm no
  allocation concept appears.
- **Accessibility/responsive:** labels/errors, keyboard actions, confirmation focus,
  status announcement, touch targets, 200% zoom, 360/480/tablet/desktop.

## Migration And Rollback

### MongoDB and immutable ledger

Use expand/migrate/contract. Never rewrite or delete historical operations. Preserve
legacy allocation enum values, delta fields, IDs, and digest behavior. Stop treating
allocation documents as current projections only after compatibility readers and
legacy correction tests pass.

Do not drop `workshopAmmoAllocations` in this implementation. A later physical-retention
decision requires deployed counts, backup/restore evidence, and confirmation that no
supported rollback or correction depends on it.

### API and clients

Deploy compatibility-aware backend first. Then deploy the client without the allocation
route. Observe retired-route calls for the agreed window. Contract API routes and shared
DTOs only after old callers are absent.

### Correction and data

Existing shooting sessions retain resolved batch/allocation facts. New and corrected
sessions contain recipe/batch facts only. Legacy correction reverses historical
allocation deltas but replacement facts are allocation-free. No backfill of immutable
operations is required.

### Rollback

Before contract, client rollback is safe while retirement routes remain. After
availability cutover, server rollback is safe only to a version that also ignores
allocations for current availability and understands all written schema versions.
Keep a known compatibility build deployable through the observation window.

If replay or stock diverges, stop new shooting writes, reconcile firearm/component/batch
projections from immutable history, and restore the compatibility build. Do not repair
by editing ledger facts.

### Cache, auth, and configuration

No cache, authentication, authorization, CORS, secret, or environment migration is
required.

## Open Questions

1. What allocation documents and allocation-bearing shooting operations exist in the
   deployed database? Static source cannot determine the required compatibility window.
2. Are any old Workshop clients still deployed or cached that can call allocation
   routes?
3. What operational retention period is required before the allocation projection
   collection can be physically archived or dropped?

These questions affect migration timing and eventual physical cleanup, not the accepted
target behavior.
