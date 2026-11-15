# Shooter Workshop Firearm Detail Refinement Handoff

## Outcome

Refine the Shooter Workshop firearm detail route into the application's primary
equipment-status surface. A user opening `/firearms/:id` must be able to scan the
firearm's current record status, accumulated rounds, recent malfunction and maintenance
history, shooting-session history, round-count trend, and currently installed component
condition without navigating across separate Workshop routes. The same component
section must support definition-safe component swaps, creation of a same-definition
replacement, and detach without replacement.

This is an Impeccable **Operate** refinement of the incumbent black, white, and yellow
Workshop interface, not a redesign. The narrowest applicable workflow is `shape` before
implementation, followed by `polish` and `harden` during implementation. No
`PRODUCT.md` or `DESIGN.md` exists under `frontend/shooter-workshop.client`; current
source and `docs/architecture/shooter-workshop.md` are therefore the visual and product
authority for this handoff.

Assumptions:

- “Gun status” means the truthful persisted firearm state (`active` or `archived`) plus
  maintenance and malfunction evidence. The UI must not infer mechanical safety,
  shootability, or an unresolved fault from historical data.
- A malfunction is an append-only Workshop maintenance-event type. This keeps it in
  the existing historical-event model without introducing a separate incident system.
- The round graphic shows cumulative firearm rounds over time from authoritative
  operation deltas, including corrections, rather than reconstructing totals from
  editable UI state or parsing activity summaries.
- The component section shows current installations. Maintenance history also includes
  component events that occurred while a component was installed on this firearm.
- Interval colors follow the product specification thresholds: white below 70%, yellow
  at 70–89%, orange at 90–99%, and red at 100% or above. Values above the interval stay
  red; visual width is capped while numeric usage remains uncapped.
- The round graph is built from the complete authoritative firearm operation history.
  Maintenance and shooting logs initially show 15 entries each and expose an explicit
  load-more action backed by continuation metadata.
- Maintenance, malfunction, and shooting-session recording are available from the
  firearm detail and pre-targeted to that firearm.
- A richer user-managed operational status is not part of this delivery. Active and
  archived remain the only persisted firearm states; the future improvement is recorded
  in `docs/architecture/future-improvements.md`.
- Swapping replaces one current installation with another physical component of the
  exact same `definitionId`. It never resets, transfers, or merges either physical
  component's accumulated rounds.
- Detaching closes the installation and returns the physical component to inventory. It
  does not archive the firearm, create a malfunction, or change the firearm's active
  record state.

Completion stops after the firearm-detail read contract, detail-route UI, maintenance,
malfunction and shooting-session recording, definition-safe swap/create-replacement/
detach behavior, focused tests, localization, and directly related documentation are
implemented. A general Maintenance-route redesign, component-detail route, charting
library adoption, notification system, mandatory component-position model, or
mechanical safety model requires separate approval.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` currently
  loads the complete firearm list through `useResource(getWorkshopFirearms)`, finds the
  requested record in the browser, and renders only total rounds, revision, account
  record ID, edit/archive controls, and a shooting-session link.
- `frontend/shooter-workshop.client/src/routes/config.tsx` registers
  `firearms/:id` with an element only. It has no route loader or action, despite the
  current Workshop convention in `docs/architecture/frontend.md` requiring route-owned
  reads and mutations at the React Router boundary.
- `frontend/shooter-workshop.client/src/routes/Components/loader.ts` and
  `frontend/shooter-workshop.client/src/routes/Components/action.ts` are current local
  examples of concurrent typed route reads, fetcher mutations, and revalidation.
- `frontend/shooter-workshop.client/src/main.scss` provides the established dark
  visual language, responsive grids, panels, stats, badges, lists, focus-visible
  treatment, and status colors. It has no firearm-detail composition, progress-meter,
  timeline, or chart styles.
- `frontend/fe-packages/models/src/workshop.ts` exposes firearm totals, component
  totals, tracking mode, service interval, component status, active installation
  records, and maintenance-event fields. It exposes no firearm-detail aggregate or
  round-history point.
- `frontend/fe-packages/services/src/workshopService.ts` can list firearms,
  components, active installations, and definitions, and can record maintenance. It
  has no detail or maintenance-history read.
- `frontend/shooter-workshop.client/src/routes/Components/action.ts` changes an
  installation by calling remove and install sequentially from the browser. A failure
  after removal can leave the component detached, so this is not a safe swap primitive
  to reuse for the firearm detail.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` exposes list
  reads for firearms, components, and active installations; a maintenance write; and a
  generic account activity history. It has no account-scoped firearm-detail endpoint
  and no maintenance-event read endpoint.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` can list account-owned
  aggregates and activities. `GetComponentInstallationsAsync` intentionally returns
  only installations with no removal date, which is insufficient for historical
  event-to-firearm correlation.
- The Domain supports independent install and remove commands. Install rejects a
  component that already has an active installation; remove closes the installation
  and returns the component to `Inventory`. No command validates same-definition
  replacement or represents swap as one idempotent intent.
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` persists firearm and
  component totals, complete installation intervals, maintenance events, shooting
  sessions, immutable operation deltas, and activities. The maintenance enum has
  `Inspection`, `Cleaning`, `ComponentMaintenance`, `Replacement`,
  `CounterAdjustment`, and `General`, but no malfunction value.
- `docs/architecture/shooter-workshop.md` already requires firearm details to include
  total rounds, installed components, installation history, maintenance history, and
  shooting sessions. It defines interval states at below 70%, 70–89%, 90–99%, and
  100% or above, and warns that they are display rules rather than technical safety
  assessments.
- The same specification defines historical operation facts and their deltas as the
  source of truth for replayable counters. A graph derived only from current totals,
  mutable sessions, or display summaries would violate that boundary.
- No repository source or specification currently defines malfunction behavior.
- No repository source currently enforces one installed component per definition on a
  firearm. This refinement must swap the specifically selected installation and must
  not introduce a new global cardinality rule.

## Proposed Design

### Read model and endpoint

Add one authenticated, account-scoped `GET /api/v1/workshop/firearms/{firearmId}/detail`
endpoint. It returns a presentation-neutral firearm-detail contract containing:

- the firearm record;
- current installed-component rows enriched with definition name and installation
  metadata;
- maintenance events associated directly with the firearm or with a component during
  an installation interval on that firearm;
- cumulative round-history points derived from effective operation deltas for the
  firearm;
- shooting-session rows associated with the firearm, including corrected-session
  identity needed to avoid presenting superseded records as current;
- active, uninstalled, non-archived inventory components needed by the swap chooser;
- concise summary counts, including maintenance-event count and malfunction-event
  count.

The Domain service owns correlation and projection. The controller must not join raw
collections or expose Mongo entities as an ad hoc anonymous object. Return `404` for a
missing or other-account firearm without revealing whether another account owns it.
Return all current component rows and all round-history points required for the
complete graph. Return maintenance and shooting logs in deterministic pages of 15,
newest first, with opaque continuation metadata. “Load more” appends the next page
without replacing or reordering visible entries. The contract must expose exhaustion
explicitly and must never silently truncate either log.

Round points come from immutable `WorkshopOperation` firearm deltas ordered by
effective time with a deterministic recorded-time/ID tie-breaker. Corrections and
negative deltas must be represented so the final point equals `firearm.totalRounds`.
Initial-round operations must appear as the first contribution. If a projection is
temporarily inconsistent, return the factual series and surface the existing
reconciliation state rather than fabricating a smooth chart.

### Malfunction semantics

Append `Malfunction` to `WorkshopMaintenanceEventType` and to the corresponding
TypeScript request union. Existing stored enum values remain readable because the new
value is additive and existing names/order are not changed. A malfunction record uses
the existing date, derived round count, description, and notes fields.

The page may show “No malfunctions recorded,” a count, and the most recent malfunction.
It must not display “safe,” “unsafe,” “operational,” “resolved,” or equivalent claims,
because the current event schema has no resolution state. Introducing active/resolved
incident state is explicitly out of scope.

### Recording maintenance, malfunction, and shooting

The firearm detail exposes focused disclosures or dialogs for recording a maintenance
event, a malfunction, or a shooting session. Each form is pre-targeted to the route
firearm and does not ask the user to select it again. Existing validation, ammunition
consumption, component attribution, idempotency, and correction semantics remain
server-authoritative.

Maintenance and malfunction use the same route action with distinct allow-listed
intents. Shooting reuses the existing shooting request and shared service rather than
creating a detail-only write contract. Successful writes revalidate the detail loader,
updating the total, graph, logs, component counters, and threshold states together.
Expected validation errors remain attached to the submitting form; unexpected errors
reach the route error boundary.

### Component swap, replacement creation, and detach

Each current component row exposes `Swap` and `Detach` actions. `Swap` opens a focused
chooser tied to that specific installation:

- The inventory list contains only active, uninstalled, non-archived physical
  components owned by the account whose `definitionId` exactly equals the installed
  component's `definitionId`.
- The definition name is visible and fixed. Compatibility tags, manufacturer, model,
  tracking mode, or free-text name similarity never substitute for definition
  equality.
- The chooser offers a secondary “Add new physical component” path. The creation form
  locks the definition to the component being replaced, accepts the established
  physical-instance fields, and uses the newly created component as the replacement.
- Both existing-inventory and create-new paths submit one swap intent. The browser must
  not orchestrate separate detach and install calls.

Add one account-scoped, idempotent swap command under the current installation route.
Its request identifies the current installation and revision, effective timestamp,
idempotency key, and exactly one replacement source: either an existing physical
component ID or a new physical-component payload. The Domain validates all
preconditions before persisting intent:

- the current installation exists, is active, belongs to the route firearm, and has
  the expected revision;
- the route firearm is account-owned and not archived;
- the replacement is different from the installed component;
- an existing replacement is account-owned, active, uninstalled, non-archived, and not
  retired;
- the replacement definition exactly matches the installed component definition;
- a new replacement is created with that definition fixed and with normal
  physical-component validation;
- the swap time does not precede the current installation time.

The command closes the old installation, returns the old physical component to
`Inventory`, creates the replacement installation, and marks the replacement
`Installed`. It preserves both component counters and complete installation history.
Represent the user intent with an additive `ComponentSwapped` operation/activity and
stable IDs so a retry converges after any partial persistence failure. The existing
repository has no verified multi-document transaction boundary; implementation must
therefore use the established durable-operation/idempotent-convergence model and add
fault-injection tests for every persistence step rather than claiming unsupported
atomicity.

`Detach` remains a separate explicit intent using the existing removal semantics. It
requires confirmation that names the component and explains that it will return to
inventory without a replacement. After success, the firearm record remains active and
the component row disappears from current installations; history remains visible.

### Page hierarchy and interaction

The page uses a scan-first desktop layout that collapses to one column without changing
content order:

1. A compact identity header with a back link, nickname or
   manufacturer/model, caliber/category metadata, and active/archived badge.
2. A status summary band with total rounds as the dominant value, latest maintenance,
   malfunction summary, and installed-component attention count. Internal revision and
   record ID move to a low-priority details disclosure rather than occupying primary
   stats.
3. A round-history panel with a simple accessible cumulative line/area graphic,
   explicit current total, date range, and a textual empty state when no round events
   exist. Prefer a local SVG implementation because no chart dependency exists and the
   graphic is one bounded series.
4. A current-components panel ordered by severity first, then component name. Each row
   shows definition/name, identification note, installation date, tracking mode,
   current rounds, interval/remaining rounds where applicable, and persisted component
   lifecycle status, followed by clearly separated `Swap` and `Detach` actions.
5. A detailed maintenance timeline with event type, target, date, round count at event,
   description, and notes. Malfunctions receive a distinct but restrained error
   treatment. Long notes wrap and remain readable; missing optional fields are omitted,
   not rendered as placeholder noise.
6. A shooting-session timeline showing date, rounds, ammunition attribution summary,
   notes, and correction state, initially limited to 15 with load-more.
7. A quiet action area for record session, record maintenance/malfunction, edit
   details, and archive. Existing mutations move to a route action and fetcher flow;
   archive remains secondary and requires clear confirmation.

The main page must remain useful with zero components, zero maintenance events, zero
round-history points, archived firearms, over-threshold components, corrected negative
deltas, long localized values, and narrow mobile screens.

### Component interval meter

For `Interval` components with a positive service interval:

- percentage is `totalRounds / serviceInterval * 100`;
- remaining rounds are `max(serviceInterval - totalRounds, 0)`;
- semantic state and color are white below 70%, yellow from 70% through 89.999%,
  orange from 90% through 99.999%, and red at 100% or above;
- visual fill width is clamped from 0% to 100%, but the visible numeric value may
  exceed 100%;
- `aria-valuemin`, `aria-valuemax`, and a clamped `aria-valuenow` are paired with a
  readable label containing actual rounds, interval, percentage, and remaining/overdue
  rounds;
- color is reinforced with status text and iconography so color is never the only
  signal.

`Counter` components show accumulated rounds without a threshold meter. `Manual`
components show manual tracking status and last relevant maintenance information.
Missing or non-positive intervals must not produce division errors or a misleading
normal meter.

## Impacted Projects

- **MorWalPizVideo.Models** — additive maintenance-event enum value; no firearm,
  component, or operation persistence-shape change; additive swap operation type.
- **MorWalPiz.Domain** — account-scoped firearm-detail orchestration, installation-time
  correlation, authoritative round-series projection, and idempotent swap orchestration.
- **MorWalPiz.Contracts** — explicit response DTOs for firearm detail, component
  condition, maintenance/shooting history, graph points, and swap request/response.
- **MorWalPizVideo.ServerAPI** — authenticated detail endpoint, `404` behavior, and
  swap endpoint.
- **frontend/fe-packages/models** — matching TypeScript read contracts and additive
  malfunction request value.
- **frontend/fe-packages/services** — endpoint constant and typed detail read.
- **frontend/shooter-workshop.client** — route loader/action, page composition,
  interval presentation, chart, recording forms, swap/create/detach flow,
  localization, styles, and component tests.
- **MorWalPizVideo.BackOffice.Tests** — focused Workshop Domain/API behavior coverage.
- **Deployment surfaces** — no topology or configuration change; the existing
  ServerAPI and Workshop client artifacts deploy in normal dependency order.

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — append the malfunction
  maintenance-event value and `ComponentSwapped` operation type.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — add explicit
  firearm-detail response contracts and swap command contracts with mutually exclusive
  existing/new replacement input.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — compose the account-scoped
  detail projection and round series from authoritative records; validate and converge
  swap operations.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — expose the
  typed firearm-detail read and swap mutation, and map missing records to `404`.
- `frontend/fe-packages/models/src/workshop.ts` — mirror the new read contracts and
  malfunction union value.
- `frontend/fe-packages/models/src/index.ts` — export new Workshop contract types.
- `frontend/fe-packages/services/src/endpoints-frontend.ts` — add the firearm-detail
  endpoint template.
- `frontend/fe-packages/services/src/workshopService.ts` — add the typed detail call.
  Add the typed swap call without composing remove/install in the client.
- `frontend/fe-packages/services/src/index.ts` — export the detail service.
- `frontend/fe-packages/services/src/workshopService.test.ts` — verify URL encoding,
  account transport, and typed response handling.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/index.ts` — export route
  loader and action beside the component.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` — replace
  component-owned reads/mutations with the status-dashboard composition.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — register detail loader and
  action.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — assert route-boundary
  registration.
- `frontend/shooter-workshop.client/src/main.scss` — add page-local responsive,
  progress-meter, timeline, chart, and accessible status styling.
- `frontend/shooter-workshop.client/src/i18n.tsx` — add English/Italian copy for
  statuses, empty states, labels, and malfunction.
- `frontend/shooter-workshop.client/src/i18n.test.ts` — verify the new translation
  keys remain complete.
- `docs/architecture/shooter-workshop.md` — record the delivered firearm-detail
  behavior, malfunction event semantics, and definition-safe swap/detach rules after
  implementation.
- `docs/architecture/frontend.md` — record the route-owned detail data flow if the
  implementation adds a reusable Workshop route convention not already covered.

### Add

- `frontend/shooter-workshop.client/src/routes/FirearmDetail/loader.ts` — fetch the
  detail contract at the route boundary and preserve `404`/error behavior.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/action.ts` — own
  edit/archive, maintenance/malfunction, shooting, swap/create-replacement, and detach
  mutation conversion, idempotency, validation results, and revalidation.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.test.tsx` —
  focused responsive-independent component behavior, threshold boundaries, empty
  states, timeline details, and accessibility assertions.
- A focused Workshop API/detail test file under
  `MorWalPizVideo.BackOffice.Tests/Features/` following the current
  `WorkshopServiceTests.cs` and `WorkshopReplayTests.cs` patterns; the implementer
  should use the repository's established naming convention rather than inventing a
  parallel test project.

### Generated/Migration

None. The enum addition is serialized additively and introduces no new MongoDB fields,
collection, index, or backfill.

### Documentation

- `docs/feature-handoffs/shooter-workshop-firearm-detail-refinement-handoff.md` — this
  implementation handoff; update it after delivery with actual files, validation, and
  residual risks.

### Verify Only

- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — confirm existing
  account-scoped list/get operations support the projection without adding an
  unbounded or cross-account repository path.
- `frontend/shooter-workshop.client/package.json` — confirm no chart dependency is
  required.
- `docs/architecture/testing.md` — use the established focused backend/frontend
  validation gates.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` — verify the
  new malfunction event remains recordable through the existing flow or explicitly
  include its option if that flow is kept in scope.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Add one account-scoped detail read; keep existing list and mutation routes compatible. |
| Frontend/UI | Replace the sparse detail with a responsive operational dashboard, recording forms, and definition-safe swap/detach flow. |
| Database/Data | Read existing aggregates and histories; append malfunction and swap operation values while preserving component counters and installation history. |
| Contracts/Models | Add explicit response DTOs and TypeScript mirrors; append `Malfunction` and `ComponentSwapped`; add swap contracts. |
| Authentication/Authorization | Existing public-account cookie scheme remains mandatory; other-account IDs return indistinguishable `404`. |
| Validation | Validate route ID, interval divisors, deterministic event ordering, graph consistency, and action payloads. |
| Cache | Not applicable; authenticated account Workshop responses are not output-cached. |
| Configuration/Secrets | Not applicable; no new setting or secret. |
| Logging/Telemetry | Use existing request telemetry; log projection inconsistencies structurally without record notes or sensitive descriptions. |
| Background Jobs | Not applicable; detail projection is a synchronous read. |
| Deployment/Operations | Shared contracts/services, ServerAPI, and client must be deployed compatibly; no host or CORS change. |
| Backward Compatibility | Existing endpoints and documents remain valid; new response contract and enum value are additive. |

## Reuse Opportunities

- Reuse the current public-account transport and CSRF behavior in
  `@morwalpizvideo/services`; do not add direct `fetch`.
- Reuse account-scoped repository primitives and immutable operation deltas; do not
  introduce a second counter-history store.
- Reuse React Router loader/action/fetcher patterns from the Components, Ammunition,
  and Recipes routes.
- Reuse `PageTitle`, `EmptyState`, existing button/panel/list primitives, number/date
  formatters, and localization provider.
- Reuse component definitions for human-readable names and complete installation
  intervals for event correlation.
- Reuse the existing physical-component creation fields and removal semantics, but do
  not reuse the browser's sequential remove/install orchestration as a swap.
- Keep the chart app-local. A single firearm series does not justify a shared layout
  component or a new charting dependency.

## Risks

- **High — misleading mechanical status.** Historical maintenance and malfunction
  records do not prove current safety. Mitigation: factual labels only, no inferred
  safe/unsafe state, and explicit “recorded history” language. Verification: UI copy
  and contract contain no safety assertion.
- **High — inaccurate round graph after corrections.** Reading shooting-session rows
  directly can double-count corrected operations. Mitigation: derive points from
  effective immutable firearm deltas and verify the final cumulative value against the
  projected firearm total. Verification: correction and negative-delta tests.
- **Medium — cross-account data exposure.** A multi-collection detail join can
  accidentally include another account's component or event. Mitigation: every read
  remains account-scoped and relationship IDs are never fetched without account
  ownership. Verification: cross-account API tests return `404` and omit foreign data.
- **Medium — incorrect historical component maintenance attribution.** Current active
  installation reads cannot establish where a component was installed at event time.
  Mitigation: correlate against complete installation intervals in Domain.
  Verification: moved-component tests before, during, and after each interval.
- **Medium — large histories increase response cost.** Unbounded operations and events
  can make the core page slow. Mitigation: full server-built graph with an explicit
  aggregation/downsampling contract if needed, 15-entry paged logs, and no N+1
  lookups. Verification: response-shape and representative-volume timing checks.
- **High — partial swap persistence.** Closing the old installation and creating the
  new one spans multiple documents, while no transaction boundary is verified.
  Mitigation: one durable idempotent swap intent, stable IDs, complete prevalidation,
  retry convergence, and reconciliation/fault-injection coverage. Verification: every
  injected failure converges to exactly one closed old installation and one active new
  installation without counter loss.
- **Medium — invalid replacement choice.** Client filtering alone could be bypassed or
  stale. Mitigation: Domain revalidates account, active installation, inventory state,
  and exact definition equality. Verification: cross-definition, cross-account,
  archived, retired, already-installed, and stale-revision tests.
- **Medium — color-only communication.** White/yellow/orange/red can be ambiguous or
  inaccessible. Mitigation: pair color with labels, values, and progress semantics.
  Verification: keyboard, screen-reader, and contrast audit.
- **Low — enum rollout skew.** An older client may not localize `Malfunction`.
  Mitigation: deploy additive shared model/service and client support with the API;
  existing clients still receive a valid string only when such an event exists.
- **Low — visual overflow.** Long component names, notes, IDs, and localized labels can
  break dense cards. Mitigation: wrapping, semantic truncation only for secondary
  metadata, and narrow-screen evidence. Verification: long-content fixtures at mobile
  and desktop widths.

## Technical Debt

- The current firearm detail, firearms list, History route, and Maintenance route use
  `useResource` and direct mutation callbacks rather than the accepted route-boundary
  convention. Correct only FirearmDetail in this scope. Defer the other route
  migrations.
- The existing maintenance screen records events but does not display actual
  maintenance history. The firearm detail solves the requested per-firearm history;
  a consolidated Maintenance-route redesign remains separate.
- The Components action currently models a move as sequential remove/install calls.
  Do not broaden this feature into a Components-route refactor, but isolate the new
  server-owned swap command so that route can adopt it in a later bounded cleanup.
- Internal revision and database-style record ID currently appear as primary stats.
  Demote them on this page; do not remove revision concurrency from mutations.
- There is no durable `PRODUCT.md` or `DESIGN.md` for the Workshop client. Do not block
  this refinement on creating them. Offer Impeccable `init`/`document` as a separate
  follow-up after the shipped UI is visually verified.

## Implementation Roadmap

1. **Freeze the accepted semantics.** Use complete authoritative graph history,
   15-entry maintenance and shooting pages with load-more, factual malfunction copy,
   exact-definition swap eligibility, pre-targeted recording forms, and no inferred
   operational state. Gate: no implementer-owned product decisions remain.
2. **Add compatible contracts.** Append the malfunction enum and define explicit
   backend response/swap DTOs plus matching TypeScript interfaces; append the swap
   operation value. Gate: existing JSON shapes remain unchanged and new types build in
   dependency order.
3. **Build the Domain projection.** Resolve the account-owned firearm, current
   components, definitions, complete installation intervals, associated maintenance
   and shooting events, operation deltas, inventory candidates, and reconciliation
   state without N+1 queries. Gate: focused service tests cover corrections, initial
   rounds, moved components, pagination, malformed/missing intervals, and account
   isolation.
4. **Implement the swap command.** Add one idempotent Domain operation for inventory
   replacement or same-definition replacement creation, plus explicit detach reuse.
   Gate: validation, retry, concurrency, and injected partial-failure tests prove
   convergence and counter/history preservation.
5. **Expose the API surface.** Add the authenticated detail read and swap mutation with
   deterministic ordering and correct `404`/conflict behavior. Gate: request-level
   tests prove same-account success, other-account concealment, archived-record
   behavior, exact-definition enforcement, and 15-entry pagination.
6. **Extend shared frontend access.** Add endpoint composition and typed detail/swap
   call using the account transport. Gate: service tests verify encoded IDs and no
   transport-mode regression.
7. **Move FirearmDetail to route ownership.** Add loader/action exports, register them,
   and use separate fetchers for edit/archive, recording, pagination, swap, and detach
   with structured accessible feedback and normal revalidation. Gate: the component
   performs no service read or mutation directly.
8. **Implement the information hierarchy.** Build the identity/status summary,
   full-history accessible rounds graphic, severity-sorted components, 15-entry
   maintenance and shooting timelines, load-more controls, recording forms, and quiet
   action area using incumbent tokens and primitives. Gate: all defined empty,
   archived, overflow, corrected, and over-threshold states render truthfully.
9. **Implement swap/create/detach UX.** Add row-scoped actions, exact-definition
   inventory chooser, locked-definition new-component form, confirmations, pending
   states, and focus restoration. Gate: stale or rejected swaps preserve user input
   and announce the server error; success revalidates all dependent page data.
10. **Implement interval presentation.** Centralize the pure threshold calculation in
   the route feature, clamp visual width, preserve actual percentages, and provide
   textual state. Gate: exact boundary tests at 0, 69.99, 70, 89.99, 90, 99.99, 100,
   and greater than 100 percent.
11. **Localize and harden.** Add English/Italian strings, date/number formatting,
   keyboard/focus behavior, reduced-motion-safe visuals, wrapping, and screen-reader
   labels. Gate: localization completeness, accessibility assertions, and mobile
   layout checks pass.
12. **Validate visually and operationally.** Use Impeccable `polish` and `harden`
    against the rendered route at representative desktop and mobile widths with empty,
    normal, approaching, attention, overdue, malfunction, swap, detach, and long-log
    fixtures. Update this handoff and the architecture specification with actual
    delivery evidence.

An independent post-validation review is warranted because this apparently visual
change introduces a multi-collection account-scoped projection and safety-adjacent
language. The user may select MorWalPiz Code Reviewer after implementation.

## Testing Strategy

- **Domain/unit:** cumulative round-series ordering; initial rounds; corrected and
  negative deltas; final-total consistency; intervalless components; current
  installations; maintenance correlation across move/removal dates; malfunction
  summary; 15-entry maintenance/shooting pagination; archived records; other-account
  isolation; exact-definition swap; create-and-swap; detach; retry convergence.
- **API/integration:** authenticated success, unauthenticated rejection, same-account
  missing ID, other-account `404`, additive JSON shape, deterministic event order,
  continuation behavior, and no foreign component/event leakage. Swap cases include
  cross-definition, cross-account, archived/retired/already-installed replacement,
  stale installation revision, duplicate retry, and fault recovery.
- **Shared service:** encoded firearm ID, expected endpoint, account transport, normal
  failure propagation, and response typing.
- **Component:** loading/error/404 behavior through the router, zero-data states,
  identity hierarchy, archived state, maintenance details, malfunction treatment,
  shooting log/correction state, load-more append behavior, component sort order,
  inventory filtering, locked new-component definition, swap/detach confirmations,
  pending state, errors, and focus restoration.
- **Threshold contract:** white/yellow/orange/red boundaries, value above interval
  remains red, clamped visual fill, actual percentage text, no meter for Counter or
  Manual, and invalid/missing interval handling.
- **Accessibility:** heading order, landmark/section naming, progress semantics,
  non-color labels, focus visibility, keyboard actions, status announcements, chart
  text alternative, and contrast.
- **Responsive/visual:** at minimum narrow mobile, tablet, and wide desktop captures
  with realistic long English and Italian content; verify no hidden actions or
  horizontal page overflow.
- **Migration:** existing maintenance events deserialize unchanged; new malfunction
  values round-trip; no document rewrite.
- **End-to-end:** register firearm, record shooting, install interval component, record
  maintenance and malfunction, correct shooting, swap with an inventory component,
  create and swap a same-definition component, detach it, then verify totals, graph,
  logs, component counters, inventory membership, and installation history remain
  coherent after each route revalidation.

## Migration And Rollback

- **MongoDB:** no schema migration or backfill. `Malfunction` is an additive enum value
  stored in existing maintenance documents and `ComponentSwapped` is an additive
  operation value. Existing documents and indexes remain valid.
- **API contract:** deploy the additive ServerAPI contract before or with the updated
  client. Existing clients continue using list routes. The new client must not require
  a response field absent from the new detail endpoint's declared contract.
- **Frontend packages:** build and publish models, then services, then the Workshop
  client in existing workspace dependency order.
- **Auth/cache/configuration:** no migration. Existing account cookie, CSRF, exact
  origin, and no-authenticated-cache policies remain unchanged.
- **Rollback:** the client can roll back independently while the unused additive detail
  endpoint remains deployed. The API can roll back after the client if no new
  malfunction or swap operation has been written. Once new enum values exist, rolling
  back to a binary that cannot deserialize them may fail; retain forward-compatible
  enum support or temporarily block the new writes before API rollback. A swap already
  completed is ordinary installation history and must not be reversed by deployment
  rollback.
- **Monitoring:** watch detail endpoint latency, `404`/`5xx` rates, projection mismatch
  logs, and client route errors after deployment. A mismatch between the final graph
  point and firearm total is an integrity signal, not a UI condition to hide.

## Open Questions

None identified. The user resolved history scope, recording scope, and operational
status. Swap behavior is fixed to exact definition identity, supports existing
inventory or same-definition creation, and detach is allowed without replacement.

## Delivery update

Implemented the typed account-scoped firearm-detail read and route-owned Workshop
surface. The API now projects the firearm, current component/definition/install
rows, installation-correlated maintenance events, shooting sessions, immutable round
history, replacement candidates, malfunction count, and reconciliation state.
`Malfunction` and `ComponentSwapped` were appended additively to the existing enums.
The Workshop client now loads the detail contract through a route loader and provides
pre-targeted shooting, maintenance/malfunction, and archive actions through a route
action. The detail page renders factual status, an accessible SVG round trend,
component interval meters, and maintenance/shooting timelines.

Validation completed:

- `dotnet build MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Workshop"` (11 passed)
- `cd frontend && yarn build:shared && yarn workspace shooter-workshop.client build`

Residual scope: durable definition-safe swap/create-replacement/detach, truthful
continuation metadata for paged logs, focused detail API/service/frontend tests,
and visual browser capture remain outstanding. Generated frontend build artifacts
are not part of the implementation.
