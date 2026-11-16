# Shooter Workshop Component Feedback Handoff

## Outcome

Refine the implemented Shooter Workshop Components route around three follow-up needs:

1. allow a physical component to start with a non-negative round count, defaulting to
   zero;
2. make `?firearmId=<id>` actually filter inventory and add an equivalent firearm
   filter to the inventory view;
3. replace the simultaneous Catalogue/Inventory columns with Inventory-first tabbed
   sections, move both create forms into accessible modals, and add independent text
   search to each section.

This is an Impeccable **Operate** refinement of the incumbent black/white/yellow
interface, not a redesign. The narrow Impeccable planning command is `shape`; eventual
implementation should also apply `harden`, `adapt`, and `audit` to this route.

The implementation boundary is the Components route, its directly required Workshop
contracts and Domain behavior for round initialization, focused tests, styles,
translations, and owning documentation. It excludes unrelated Workshop routes,
server-side list filtering, a general modal/tab framework, and changes to existing
component usage history.

Assumptions resolved for implementation:

- On create, the user-facing field is **Initial round count**, defaults to `0`, and
  establishes the component counter through the immutable Workshop ledger.
- On edit, the same control represents **Current round count**. A changed value is a
  ledger-backed correction to the requested total, not a rewrite of the historical
  initial event. Metadata-only edits leave the total unchanged.
- `?firearmId=<id>` means “show physical components currently installed on this
  firearm.” It selects the Inventory section, applies the firearm filter, and
  preselects that firearm if the add modal is later opened. It does not open the modal
  automatically.
- The inventory firearm filter offers all active firearms plus **Uninstalled**.
  “All firearms” clears `firearmId`; a selected firearm stores its ID in the query
  string so direct links and browser navigation remain meaningful.
- Catalogue search matches definition name, description, and notes. Inventory search
  matches definition name, identification note, manufacturer, model, compatibility
  tags, firearm name, and visible status labels.
- Archived records remain subject to the route's existing visibility policy; this
  change does not add a new archived-record toggle.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` reads
  `firearmId` through `useSearchParams`, but uses it only to open the physical form,
  preselect a firearm, adjust instructional copy, and change row-action wording.
  Inventory filtering currently uses compatibility tags only.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` already
  creates `/components?firearmId=<encoded id>`, so the broken behavior is in the
  Components route's interpretation rather than link generation.
- `frontend/shooter-workshop.client/src/routes/Components/loader.ts` loads definitions,
  components, firearms, and installations concurrently. It does not inspect the
  request URL.
- `frontend/fe-packages/services/src/workshopService.ts` and
  `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` expose an
  account-wide component list with no firearm query. Existing loaded installations
  already provide enough data for client-side firearm filtering, so an API filter is
  unnecessary.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` renders
  Catalogue and Inventory simultaneously in `.components-grid`. Both add forms are
  inline disclosure regions, both collections are compact lists, and neither has text
  search.
- `frontend/shooter-workshop.client/src/main.scss` provides the current component grid,
  compact-list, form, button, table, backdrop, and dialog styles. The grid collapses
  below 900px.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx` and
  `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` are the local
  modal precedents. They implement `role="dialog"`, modal labelling, initial and return
  focus, Escape handling, backdrop close, focus trapping, and inert background
  behavior.
- No local tab implementation exists. Bootstrap is installed, but this application
  primarily uses app-local React state and styles for equivalent interactions.
- `frontend/shooter-workshop.client/src/routes/History/component.tsx` is the nearest
  precedent for preserving and updating URL search parameters.
- `frontend/shooter-workshop.client/src/routes/Components/action.ts`,
  `frontend/fe-packages/models/src/workshop.ts`,
  `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`, and
  `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` do not accept an initial-round
  value for a physical component.
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` already persists
  non-nullable `ComponentInstance.TotalRounds`; omitted values currently become zero.
  No new MongoDB field is required.
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs` reconstructs component totals from
  immutable operation deltas. Writing a nonzero document total without a matching
  operation would be detected as inconsistent and may be overwritten.
- The firearm flow in `frontend/shooter-workshop.client/src/routes/Firearms/`,
  `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`, and
  `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` is the verified precedent:
  initial rounds default to zero, require a non-negative integer, and are recorded as
  a ledger delta.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` updates physical metadata through
  revision-checked replacement while preserving `TotalRounds`. A component round
  correction therefore needs explicit Domain orchestration rather than direct
  replacement.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` checks route
  registration only. No direct Components route UI test currently exists.
- `frontend/fe-packages/services/src/workshopService.test.ts` covers component route
  selection and CSRF, but not round initialization.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` covers firearm
  initial-round ledger behavior and component lifecycle behavior, but not physical
  component initial rounds or edit-time correction.
- No app-local `PRODUCT.md`, `DESIGN.md`, or Impeccable surface brief exists beneath
  `frontend/shooter-workshop.client`; the shipped route and
  `docs/architecture/shooter-workshop.md` are the incumbent visual and product truth.

## Proposed Design

### Round-count contract and invariant

Extend physical-component creation with optional `initialRounds`, defaulting to zero.
Validate it as an integer in the inclusive range from zero to the platform's signed
64-bit maximum at the route action, API contract, and Domain boundary. The Domain
boundary remains authoritative.

Creation must persist the component and record a component-aggregate operation delta
for a nonzero initial value using the same idempotency envelope as the physical create.
The operation and projection must not be able to diverge on retry. Zero requires no
material delta but still produces a component whose projected total is zero.

Extend physical-component update with an optional requested `totalRounds`. The edit
form starts with the current total. If the submitted total differs, Domain calculates
the signed delta from the current persisted total and appends an immutable
component-aggregate correction operation while applying metadata changes under the
same expected revision and idempotency boundary. If the value is unchanged or omitted,
no round operation is created. Direct assignment of `TotalRounds` without a ledger
operation is prohibited.

Round-count correction does not rewrite, delete, or relabel historical maintenance,
installation, or usage events. The activity/operation description should distinguish
initialization from a later manual correction without logging free-form component
notes.

### Inventory firearm filtering

Keep the account-wide component, firearm, and installation reads unchanged. Build a
route-local component-to-current-installation lookup from loader data, then filter in
this order:

1. selected firearm or Uninstalled;
2. selected compatibility tags;
3. normalized text query.

Use the URL's `firearmId` as the selected firearm filter. Validate it against loaded
firearms:

- a valid ID applies the filter and selects Inventory;
- the reserved Uninstalled value applies the uninstalled predicate;
- a missing value means All firearms;
- an unknown or inaccessible ID must not silently produce a misleading empty view:
  clear the invalid filter from the URL and show the route-standard feedback message.

Changing the firearm control updates only the `firearmId` parameter while preserving
unrelated parameters. Clearing it preserves the user's text and compatibility filters,
which remain route-local UI state. Browser Back/Forward must restore the firearm
selection because it is URL-owned.

The add-physical-component modal uses the current valid firearm filter as its initial
firearm value. Changing the modal selection does not mutate the list filter. Direct
navigation with `firearmId` no longer opens the modal.

### Inventory-first sections

Replace the simultaneous two-column grid with a full-width section switcher:

- **Inventory** is first and selected by default;
- **Catalogue** is second;
- both controls are equal-width, visually prominent buttons at the top of the route;
- only the selected panel is rendered as visible content.

Implement this as an accessible tab set because the controls switch between two
peer panels in place. The tab list requires selected state, tab/panel relationships,
Left/Right arrow navigation, Home/End navigation, and logical focus management.
Mouse, touch, and keyboard activation must produce the same selected panel. A valid
`firearmId` always selects Inventory. Tab state does not need a URL parameter.

Each panel header contains its title, result count, and one primary action:
`+ Add physical component` for Inventory and `+ New component definition` for
Catalogue. Opening either action presents a modal using the established Feedback/
Recipes accessibility behavior. Create success closes and resets only that modal;
validation or transport failure keeps it open with values, errors, and focus context
preserved. Edit continues to use the same form in modal mode with create/edit-specific
title, submit label, and round-count label.

Use the existing table styles for wide screens and a deliberate stacked-row treatment
at narrow widths. Do not reproduce desktop columns as an overflowing miniature table.
Row actions remain keyboard reachable and retain the established archive/delete
confirmations.

### Search behavior

Give each panel an independently labelled search input directly above its table.
Search is immediate, client-side, case-insensitive, whitespace-trimmed, and
diacritic-tolerant. It must not issue network requests.

Catalogue search covers name, description, and notes. Inventory search covers the
fields listed in Outcome plus the resolved current firearm label. Search combines with
firearm and compatibility filters rather than replacing them.

Each panel distinguishes:

- no records exist;
- records exist but no row matches current filters;
- data is loading or an action is pending;
- a request failed.

The no-match state identifies active filters and offers a clear-filters action scoped
to that panel. Search terms survive route revalidation and modal open/close during the
current route lifetime.

### Impeccable workflow

This is an established **Operate** surface and a refinement. Preserve the incumbent
visual language and use `shape` to settle the tab, filter, table, and modal hierarchy
before implementation. Apply `harden` to invalid query values, validation failures,
long translated content, empty/no-match states, and destructive actions; `adapt` to
desktop and narrow layouts; and `audit` to tabs, dialogs, focus, labels, contrast, and
keyboard operation.

Minimum visual evidence is the route at desktop and mobile widths showing both
selected sections, each modal, create and edit modes, an active firearm deep-link
filter, combined filters, long content, a no-match state, and a server validation
error. This work must not introduce a replacement visual world.

## Impacted Projects

- **MorWalPizVideo.Models** — verify existing component totals and operation aggregate
  types; no persistence shape change is expected.
- **MorWalPizVideo.Domain** — ledger-backed initialization and edit-time correction,
  revision/idempotency handling, and replay consistency.
- **MorWalPiz.Contracts** — additive create/update round-count request properties.
- **MorWalPizVideo.ServerAPI** — map and validate the additive contract fields through
  existing authenticated, CSRF-protected routes.
- **MorWalPizVideo.BackOffice.Tests** — focused Domain/API behavior and replay tests.
- **frontend/fe-packages/models** — synchronized additive request properties.
- **frontend/fe-packages/services** — verify serialization of additive values; no new
  endpoint is required.
- **frontend/shooter-workshop.client** — tabbed layout, modals, searches, firearm
  query filtering, round-count controls, translations, styles, and component tests.
- **MorWalPizVideo.AppHost and deployment surfaces** — verify only; no topology,
  configuration, secret, or host change is expected.
- **Other APIs, SPAs, Shooting Range, VideoImporter, and BackOffice UI** — not
  impacted; no direct Workshop component-contract consumers were found.

## Impacted Files

### Modify

- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — add bounded
  create/update round-count inputs.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — create initial component
  delta and revision-safe update correction.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — map the
  additive request fields to Domain commands.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — initialization,
  correction, retry, and invalid-value coverage.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` — verify initialized
  and corrected component totals replay consistently if this is the existing replay
  test owner.
- `frontend/fe-packages/models/src/workshop.ts` — synchronize additive create/update
  request fields.
- `frontend/fe-packages/services/src/workshopService.test.ts` — assert zero, nonzero,
  and update payload serialization.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` — section
  tabs, modal forms, firearm/text filtering, tables, and round-count controls.
- `frontend/shooter-workshop.client/src/routes/Components/action.ts` — parse, default,
  validate, and submit create/update round values.
- `frontend/shooter-workshop.client/src/main.scss` — full-width switcher, panel,
  toolbar, modal, responsive table/row, and no-match presentation.
- `frontend/shooter-workshop.client/src/i18n.tsx` — labels, help, filter states,
  modal titles, counts, errors, and accessible names.
- `docs/architecture/shooter-workshop.md` — record ledger-backed physical component
  initialization/correction and Inventory/Catalogue interaction behavior.
- `docs/feature-handoffs/shooter-workshop-component-feedback-handoff.md` — keep actual
  implementation, validation, and residual risks current.

### Add

- `frontend/shooter-workshop.client/src/routes/Components/component.test.tsx` — direct
  route interaction coverage using the existing Vitest/Testing Library conventions.
- Add an action-focused test beside the Components route only if the existing
  component test cannot exercise form parsing and returned validation results without
  over-coupling to React Router internals.

### Generated/Migration

None. `ComponentInstance.TotalRounds` and component operation deltas already exist.
No EF migration, MongoDB backfill, generated client, or data rewrite is required.

### Documentation

- This handoff is the editable implementation source of truth for the follow-up.
- The Shooter Workshop architecture guide receives the durable behavior after
  implementation.

### Verify Only

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — existing total and
  operation types are sufficient.
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs` — existing component-delta replay
  remains authoritative.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — no new query is needed.
- `frontend/shooter-workshop.client/src/routes/Components/loader.ts` — existing four
  datasets are sufficient.
- `frontend/fe-packages/services/src/workshopService.ts` and
  `frontend/fe-packages/services/src/endpoints-frontend.ts` — existing routes remain
  sufficient.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx` and
  `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — behavioral
  modal references; do not couple route state to either feature.
- `frontend/shooter-workshop.client/package.json`,
  `frontend/shooter-workshop.client/Dockerfile`, deployment workflows, and
  `MorWalPizVideo.AppHost/Program.cs` — no dependency or runtime wiring change.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Add optional round values to existing physical create/update requests. No list endpoint or route changes. |
| Frontend/UI | Inventory-first accessible tabs, modal create/edit forms, firearm and text filters, responsive tables, and count/empty states. |
| Database/Data | No schema or backfill. Nonzero initialization and corrections must be represented by immutable component operation deltas. |
| Contracts/Models | Additive nullable/optional request properties; existing response and persisted entity shapes remain unchanged. |
| Authentication/Authorization | Existing PublicAccount authentication, account scoping, CSRF, and cross-account not-found behavior remain unchanged. |
| Validation | Non-negative integer/64-bit bounds at client and contract boundaries; Domain verifies requested correction against the current revision and total. |
| Cache | Not applicable. Workshop reads are not output-cached; route state revalidates through React Router. |
| Configuration/Secrets | Not applicable. No setting, credential, connection string, or feature flag is added. |
| Logging/Telemetry | Record structured initialization/correction outcomes and conflicts without notes, cookies, or sensitive account data. |
| Background Jobs | Not applicable. No asynchronous migration or scheduled work is needed. |
| Deployment/Operations | Backend additive support must deploy before the SPA starts sending the fields; no infrastructure change. |
| Backward Compatibility | Old clients omit the optional fields and preserve current zero/create and unchanged/update behavior. Existing documents replay unchanged. |

## Reuse Opportunities

- Reuse the firearm initial-round validation and ledger-delta pattern for physical
  component initialization.
- Reuse existing component aggregate operations and replay rather than adding a second
  counter source.
- Reuse revision and idempotency behavior already present in physical component
  mutations.
- Reuse the loaded installations to resolve current firearm ownership; do not add a
  second API query.
- Reuse History's URL search-parameter update pattern.
- Reuse Feedback and Recipes modal accessibility behavior, extracting a shared
  route-local dialog helper only if duplication would otherwise be material.
- Reuse existing table, form, button, badge, empty-state, fetcher, and i18n patterns.
- Reuse account API transport and existing component endpoints.

## Risks

- **High — counter projection corruption:** directly replacing `TotalRounds` would
  disagree with replay. Mitigation: ledger-only initialization/correction and replay
  tests. Verification signal: persisted projection equals replay after create, edit,
  and retry.
- **High — duplicate correction on retry:** a retried update could append the same
  delta twice. Mitigation: include the requested total in the existing idempotency
  hash and commit correction once per operation key. Verification signal: repeated
  requests leave one effective delta and one final total.
- **Medium — concurrent usage versus edit correction:** rounds can change between form
  load and submit. Mitigation: expected revision/current-total validation and a
  conflict requiring refresh instead of applying a stale delta. Verification signal:
  stale edits return conflict and preserve the newer total.
- **Medium — misleading firearm filter:** unknown query IDs could look like a valid
  empty inventory. Mitigation: validate against loader-owned firearms, clear invalid
  values, and notify the user. Verification signal: invalid deep links recover to All
  firearms with feedback.
- **Medium — filter disagreement:** installed state can change after loader data is
  read. Mitigation: derive from current installation records and rely on action
  revalidation. Verification signal: install/remove actions immediately move rows to
  the correct filtered result.
- **Medium — modal data loss:** fetcher revalidation or validation failure could reset
  fields. Mitigation: close/reset only on confirmed success and test retained values.
  Verification signal: server errors keep modal, values, and focus context.
- **Low — tab/table accessibility regression:** custom controls can be mouse-only or
  overflow on mobile. Mitigation: ARIA tab keyboard model, modal focus tests, and
  responsive stacked rows. Verification signal: keyboard-only and narrow viewport
  evidence passes.
- **Low — external-service risk:** none; this flow uses only ServerAPI and MongoDB.

## Technical Debt

- The frontend type for create-and-install currently declares `installation` as
  non-nullable while the backend permits null for a spare. This is directly adjacent
  contract debt but not required for the requested behavior; correct it only if the
  touched create flow otherwise requires an unsafe assertion.
- `Components/action.ts` does not consistently translate expected transport problems
  into typed action errors. Modal value retention depends on predictable errors, so
  narrow correction for component intents is in scope; a cross-route error framework
  is deferred.
- Most current physical form fields are uncontrolled. Do not convert unrelated forms,
  but the modal must own enough state to preserve values and distinguish create/edit
  round labels reliably.
- No reusable tab implementation exists. Keep the implementation local until a second
  equivalent surface proves shared ownership.
- Catalogue and Inventory are currently compact lists despite the user's table
  terminology. Converting only this route to responsive tables is in scope; broad
  list/table standardization is deferred.
- Archived-definition selection validation remains a known gap. It should not be
  hidden by this UI work, but fixing the Domain rule requires separate approval unless
  a new test proves it blocks the requested create behavior.

## Implementation Roadmap

1. **Establish regression tests for round invariants.** Add failing Domain tests for
   zero-default creation, nonzero initialization, replay, idempotent retry, unchanged
   edit, correction up/down, negative/overflow rejection, and stale revision conflict.
   Gate: tests distinguish ledger-backed behavior from direct total assignment.
2. **Add the compatible round contract.** Extend backend and shared TypeScript
   create/update requests without changing responses or persisted entities. Gate: old
   payloads remain valid and invalid numeric values fail at the owning boundary.
3. **Implement Domain initialization and correction.** Record nonzero initial and
   edit-time deltas atomically/idempotently with physical mutations. Gate: projected
   totals equal replay after retries and correction conflicts preserve newer data.
4. **Map existing API routes.** Pass the additive fields through the controller while
   retaining authentication, CSRF, account ownership, and current problem shapes.
   Gate: focused API behavior proves omitted, zero, nonzero, and stale values.
5. **Synchronize the route action and transport tests.** Parse create/edit values,
   default create to zero, preserve unchanged edits, and serialize exact payloads.
   Gate: action/service tests cover field omission, bounds, and expected errors.
6. **Shape the Inventory-first interaction.** Apply the Impeccable `shape` direction
   to tabs, toolbars, tables, filters, and modal hierarchy without changing the visual
   world. Gate: the agreed desktop and mobile structure resolves all controls and
   states before detailed styling.
7. **Implement query-owned firearm filtering.** Derive installation lookup from loader
   data, synchronize the firearm select with `firearmId`, handle Uninstalled/invalid
   values, preserve unrelated parameters, and stop auto-opening the add form. Gate:
   direct link and Back/Forward component tests select Inventory and show exact rows.
8. **Move create/edit forms into accessible modals.** Reuse the local dialog behavior,
   add create/edit round controls, and preserve values/errors/focus. Gate: keyboard,
   Escape, backdrop, initial/return focus, pending, success, and failure tests pass.
9. **Add independent searches and responsive tables.** Compose firearm, compatibility,
   and text predicates; add counts, empty/no-match states, clear actions, and mobile
   rows. Gate: combined filter tests and desktop/mobile visual evidence pass.
10. **Complete localization, accessibility, and documentation.** Add all translated
    copy, apply Impeccable `harden`, `adapt`, and `audit`, and update this handoff plus
    the architecture guide with actual behavior and evidence. Stop at this component
    scope.
11. **Deploy in compatibility order.** Release backend additive support before shared
    packages and the Workshop SPA, then smoke-test deep linking, create at zero and
    nonzero, edit correction, replay consistency, searches, and modal flows.

An independent post-validation review is warranted because the change touches an
immutable counter ledger, idempotency/concurrency, URL behavior, and custom accessible
tabs/dialogs. The user may select `MorWalPiz Code Reviewer` after implementation.

## Testing Strategy

- **Domain/unit — `MorWalPizVideo.BackOffice.Tests`:** default zero, maximum valid
  value, negative/overflow rejection, nonzero initialization, correction up/down,
  unchanged update, stale revision, idempotent retry, and operation description/type.
- **Replay — `MorWalPizVideo.BackOffice.Tests`:** create and correction deltas rebuild
  the same component total; metadata-only edits do not affect replay.
- **API/integration — `MorWalPizVideo.BackOffice.Tests`:** omitted/additive fields,
  model validation, account isolation, CSRF, stale update conflict, and existing
  clients without round values.
- **Shared service contract — `frontend/fe-packages/services`:** exact create/update
  payloads for zero, nonzero, unchanged, and corrected totals with existing CSRF
  behavior.
- **Route action — Workshop SPA:** defaulting, integer/bounds validation, typed server
  error propagation, retained values, and successful revalidation.
- **Component — Workshop SPA:** Inventory default, Catalogue selection, full keyboard
  tab behavior, valid/invalid/Uninstalled firearm query values, Back/Forward, combined
  firearm/tag/text filtering, per-section search scope, clear filters, result counts,
  and no-record/no-match distinctions.
- **Modal/accessibility — Workshop SPA:** accessible names/descriptions, focus trap,
  initial/return focus, Escape/backdrop behavior, create/edit labels, pending state,
  validation focus, retained data, and no background interaction.
- **Responsive/visual:** desktop and narrow mobile for both panels, long values,
  translated copy, active filters, stacked rows, both modals, errors, and empty states.
- **Security:** no cross-account firearm filtering or counter update, CSRF on create/
  update, no sensitive form content in logs, and invalid query IDs do not disclose
  firearm existence.
- **End-to-end:** enter from firearm detail, verify filtered inventory, open the add
  modal with that firearm preselected, create a nonzero component, correct its total,
  confirm replay-derived display, switch/search Catalogue, and hard reload the URL.

## Migration And Rollback

No MongoDB or SQLite migration is required. Existing `TotalRounds` and operation
deltas support the behavior, and old documents remain valid. There is no backfill:
historical components retain their current ledger-derived totals.

The API change is additive. Deploy Contracts/Domain/ServerAPI first, then shared
frontend models/services, then the Workshop SPA. Old clients omit the fields and keep
current behavior. New clients must not deploy before the backend accepts them.

Monitor component create/update validation failures, revision conflicts, idempotency
replays, and reconciliation mismatches. A reconciliation mismatch is a release blocker
because it indicates projection/ledger divergence.

Rollback the SPA first; the additive backend remains compatible. Backend rollback is
safe only after confirming no new client still sends the additive fields. Operations
already recorded for initialization/correction are valid immutable facts and must not
be deleted during rollback; reverting the UI does not revert those user-requested
totals. No cache, auth, secret, background-job, configuration, or deployment-topology
migration is involved.

## Open Questions

None blocking. The plan resolves the ambiguous phrases as follows:

- create uses **Initial round count**;
- edit uses **Current round count** and appends a ledger correction when changed;
- firearm filtering means currently installed on the selected firearm, with an
  explicit Uninstalled option;
- the two wide controls are accessible tabs, Inventory is default, and tab selection
  is not persisted in the URL;
- text matching uses the documented visible fields and combines with existing
  inventory filters.

Any request to rewrite historical counter events, filter by mere firearm
compatibility instead of current installation, or introduce server-side list queries
is a separate architectural decision.

## Implementation Status

Implemented in the current worktree:

- Added optional `InitialRounds` to physical-component creation and optional
  `TotalRounds` to updates across Contracts, Domain, ServerAPI, and shared frontend
  models.
- Recorded nonzero component initialization and edit-time round corrections as
  immutable component ledger deltas. Direct projection assignment is not used.
- Added focused Domain tests for component initialization and round correction.
- Reworked the Components route to use Inventory-first accessible tabs, modal create/
  edit forms, firearm filtering synchronized with `?firearmId=`, an Uninstalled option,
  per-section text search, compatibility-tag filtering, result counts, responsive
  tables, and no-result states.
- Added initial/current round controls to the physical-component form, defaulting create
  to zero and retaining edit values through fetcher failure.
- Added the required Italian labels and route-local responsive/modal/tab styles.
- Updated the Shooter Workshop architecture guide with the component counter invariant.

Validation completed:

- `dotnet build MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed.
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~WorkshopServiceTests"` — 14 passed.
- `yarn --cwd frontend workspace shooter-workshop.client build` — passed; Vite emitted the existing chunk-size warning.
- `yarn --cwd frontend workspace shooter-workshop.client test --run src/routes/config.test.tsx` — 2 passed.
- Scoped `git diff --check` — passed.

Residual risks:

- No browser screenshot/e2e run was available in this environment, so visual evidence
  for narrow tables, focus trapping, and direct-link interaction remains a follow-up.
- The route still uses the established account-wide loader and client-side filtering;
  no server-side firearm query was introduced.
- Backend test builds report pre-existing NuGet audit warnings for the pinned
  `SixLabors.ImageSharp` package and unrelated nullable warnings. They were not changed
  as part of this feature.
- Existing user changes in `Dashboard/component.tsx`, `ShootingSessions/component.tsx`,
  and the pre-existing Components worktree edits were preserved.
