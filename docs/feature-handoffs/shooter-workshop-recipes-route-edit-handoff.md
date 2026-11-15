# Shooter Workshop Recipes Route And Edit Handoff

## Outcome

Refine the existing Shooter Workshop Recipes page so server state follows the same
React Router loader/action architecture as Production, the create form exposes every
user-editable recipe snapshot field, recipe details use a responsive two-column
presentation, and an existing recipe can be edited and submitted from the detail
modal.

Assumptions:

- "All available data" means every field in `CreateWorkshopRecipeRequest`: name,
  caliber, OAL, bullet weight, bullet form, drawing, primer, powder, powder weight,
  and notes.
- `status`, `version`, `updatedAt`, `id`, and `accountId` remain server-managed and
  must not become create inputs.
- Editing is enabled for active recipes. Archive, duplicate, search, filters,
  production history, and version-history UI remain outside this change.
- This is an Operate-mode refinement of the incumbent Workshop interface, not a
  redesign.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` loads recipes
  through `useResource(getWorkshopRecipes)` and creates them with a direct service
  call. It does not use route loader data, a router action, or a fetcher form.
- `frontend/shooter-workshop.client/src/routes/Production/loader.ts`,
  `Production/action.ts`, `Production/component.tsx`, and `Production/route.test.ts`
  establish the requested route pattern: server reads in a loader, mutations in an
  action, `useLoaderData` in the component, `useFetcher` for pending/result state, and
  focused tests of route data functions.
- `frontend/shooter-workshop.client/src/routes/config.tsx` registers Production's
  loader and action but registers Recipes with only an element.
- `frontend/fe-packages/models/src/workshop.ts` defines the complete
  `WorkshopRecipe` and derives `CreateWorkshopRecipeRequest` by omitting
  server-managed fields. The current Recipes create form omits `bulletWeight`,
  `bulletForm`, and `drawing`.
- `frontend/fe-packages/services/src/workshopService.ts` exposes recipe list and
  create operations, but no update operation.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` already owns
  `PUT api/workshop/recipes/{recipeId}`. It requires CSRF and accepts the complete
  recipe snapshot plus `Version` and `IdempotencyKey`.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` updates a recipe only when the
  expected version matches, increments the version, writes a recipe-version record,
  and returns a conflict when stale data is submitted. Production batches retain
  their embedded snapshot and are not rewritten.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` defines the server
  update contract as all recipe input fields plus a positive version and required
  idempotency key.
- `docs/architecture/shooter-workshop.md` requires route-owned reads and mutations to
  use colocated loaders/actions and identifies recipe editing as approved product
  behavior. Its current baseline explicitly records recipe editing as follow-up work.
- `frontend/shooter-workshop.client/src/main.scss` provides a two-column `.form-grid`
  that collapses below 560px, while the existing `.feedback-dialog` is globally
  shared and limited to 34rem. Recipe-specific layout must not alter every feedback
  dialog.

## Proposed Design

### Route data flow

Add a Recipes loader that returns the current account's recipe collection through
`getWorkshopRecipes`. Register it with the Recipes route and consume it through
`useLoaderData`. Keep only presentation state in the component: create disclosure,
selected recipe identifier, and detail/edit mode.

Add one Recipes action with explicit `create` and `update` intents:

1. Read and normalize the complete recipe form payload.
2. Validate required text and optional numeric parsing before transport.
3. For create, call the existing shared create service.
4. For update, require recipe ID and current version, generate a new idempotency key,
   and call the new shared update service.
5. Return a discriminated success/error result suitable for accessible fetcher
   feedback rather than performing transport inside the component.

React Router's successful mutation revalidation refreshes loader data. Store the
selected recipe ID rather than a copied recipe object so the modal resolves the
current recipe from loader data after an update and never displays a stale version.

### Shared contract and service

Add `UpdateWorkshopRecipeRequest` to the shared frontend model package as the complete
create payload plus `version` and `idempotencyKey`. Export it from the models package.
Add and export `updateWorkshopRecipe(recipeId, body)` in the shared services package,
using the existing account API client and URL-encoded recipe ID to call the existing
PUT route. Do not add an app-local fetch client or duplicate the endpoint constant.

### Create and edit forms

Use one reusable recipe-fields component for create and update so the two modes
cannot drift. It contains:

- Name and caliber as required text.
- OAL as the existing culture-neutral decimal string submitted to the server.
- Bullet weight and powder weight as optional numeric values.
- Bullet form, drawing, primer, and powder as optional text.
- Notes as optional multiline text.

The create form uses empty defaults. Edit mode uses the selected recipe values and
includes hidden intent, recipe ID, and version values. Server-managed status and
metadata remain visible only in detail mode.

Use separate fetchers for the create surface and modal edit surface so pending and
error states do not interfere. Disable the submitting form's controls, show a
localized pending label, and expose action errors with `role="alert"`. On create
success, close the create form. On update success, leave the modal open, return it to
detail mode, and show the revalidated values and incremented version.

### Detail modal and interaction

Keep the established modal visual language and add a recipe-specific dialog class
rather than widening `.feedback-dialog` globally. In read mode:

- Render semantic label/value pairs in a two-column field grid on wider screens.
- Each field remains one label/value unit; notes spans the full width.
- Collapse to one column on the existing narrow breakpoint.
- Show every recipe field plus status, version, and updated time. Account and record
  identifiers remain omitted because they do not help the task.

Add an Edit action for active recipes. Edit mode replaces the detail grid with the
shared complete recipe form and Save/Cancel controls. Cancel returns to unchanged
detail mode. Preserve backdrop close, Escape close, initial focus, and focus return.
Expand the focus-trap query to include the newly introduced form controls. Pending
submission must prevent duplicate saves without trapping the user in an
unrecoverable state.

### Impeccable direction

This is an Operate-mode `shape` refinement. Preserve the incumbent dark Workshop
palette, typography, controls, card list, and modal behavior. The narrow visual move
is a denser, aligned recipe specification grid that makes the record scannable; it is
not a new visual world. Implementation evidence should include the Recipes page,
complete create form, two-column read modal, edit modal, action error state, and the
single-column mobile layout at representative desktop and mobile widths.

## Impacted Projects

- `frontend/shooter-workshop.client`: route registration, loader/action ownership,
  complete create form, editable detail modal, responsive recipe layout,
  localization, and route/component tests.
- `frontend/fe-packages/models`: shared update request type.
- `frontend/fe-packages/services`: shared recipe update API operation.
- `MorWalPizVideo.ServerAPI`: verify only; the required authenticated, CSRF-protected
  update endpoint already exists.
- `MorWalPizVideo.Domain`: verify only; versioning, conflict handling, immutable
  historical snapshots, and update persistence already exist.
- `MorWalPiz.Contracts`: verify only; the backend request contract already exists.
- `MorWalPizVideo.BackOffice.Tests`: verify existing Workshop service coverage and add
  no backend test unless implementation reveals the existing update behavior lacks a
  focused regression test needed to prove UI assumptions.
- Deployment surfaces: no topology or configuration change.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — replace
  resource/direct-call ownership with loader data and fetcher forms; share complete
  create/edit fields; add editable responsive modal behavior.
- `frontend/shooter-workshop.client/src/routes/Recipes/index.ts` — export the loader
  and action with the component, following the route module convention.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — register the Recipes
  loader and action.
- `frontend/shooter-workshop.client/src/main.scss` — add recipe-specific dialog,
  two-column detail grid, full-width notes, action, and responsive styles without
  changing global feedback-dialog sizing.
- `frontend/shooter-workshop.client/src/i18n.tsx` — add any new recipe edit, feedback,
  metadata, and field labels to both supported cultures.
- `frontend/fe-packages/models/src/workshop.ts` — define the update recipe request.
- `frontend/fe-packages/models/src/index.ts` — export the update request.
- `frontend/fe-packages/services/src/workshopService.ts` — add the PUT operation.
- `frontend/fe-packages/services/src/index.ts` — export the update operation.
- `docs/architecture/shooter-workshop.md` — after implementation, move recipe editing
  and Recipes route data ownership from follow-up to current behavior without
  claiming the still-unimplemented recipe history/search/filter features.

### Add

- `frontend/shooter-workshop.client/src/routes/Recipes/loader.ts` — load current
  recipes and export its inferred data type.
- `frontend/shooter-workshop.client/src/routes/Recipes/action.ts` — own create/update
  parsing, validation, service delegation, idempotency, and action result shape.
- `frontend/shooter-workshop.client/src/routes/Recipes/route.test.ts` — focused loader
  and action contract tests.
- A colocated Recipes component test only if route-function tests cannot prove the
  complete field rendering, modal mode transition, responsive semantics, and
  accessible feedback. Follow the existing route test naming convention.

### Generated/Migration

None. MongoDB recipe documents, version records, and production snapshots already
support the requested behavior.

### Documentation

- `docs/feature-handoffs/shooter-workshop-recipes-route-edit-handoff.md` — this
  implementation source of truth; update after delivery with actual files,
  validation, and residual risks.
- `docs/architecture/shooter-workshop.md` — current-state reconciliation after the
  feature ships.

### Verify Only

- `frontend/shooter-workshop.client/src/routes/Production/loader.ts`
- `frontend/shooter-workshop.client/src/routes/Production/action.ts`
- `frontend/shooter-workshop.client/src/routes/Production/component.tsx`
- `frontend/shooter-workshop.client/src/routes/Production/route.test.ts`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs`

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | No production change. Reuse existing GET, POST, and PUT recipe routes. |
| Frontend/UI | Route-driven list/create/update; complete fields; responsive two-column detail/edit modal. |
| Database/Data | No schema change. Existing update increments version and appends a version record. |
| Contracts/Models | Add only the missing shared TypeScript update request that mirrors the existing backend contract. |
| Authentication/Authorization | Unchanged account cookie/session boundary. Existing PUT continues to require authentication and CSRF. |
| Validation | Required name/caliber and safe optional numeric parsing at the action boundary; backend remains authoritative for length, OAL precision, version, and domain rules. |
| Cache | Not applicable. Workshop reads are account-owned and current source has no output-cache tag flow for this route. Router revalidation refreshes the client collection. |
| Configuration/Secrets | None. No new endpoint base, feature flag, or secret. |
| Logging/Telemetry | No new server path. Existing API/service behavior remains authoritative; client surfaces failures instead of swallowing them. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Shared frontend packages must build before the Shooter Workshop client. No service deployment ordering beyond normal compatible frontend delivery. |
| Backward Compatibility | Additive frontend type/service exports. Existing recipe documents and API clients remain compatible. |

## Reuse Opportunities

- Production's colocated loader/action and typed fetcher result pattern.
- Components' intent-based action and shared create/edit field approach.
- Existing `getWorkshopRecipes`, `createWorkshopRecipe`, account API client, endpoint
  constant, CSRF behavior, and shared `request` response handling.
- Existing `.form-grid`, buttons, alert styles, modal shell, page title, empty state,
  and Workshop localization function.
- Existing backend `UpdateRecipe`, optimistic version check, idempotent operation
  commit, recipe-version persistence, and production snapshot immutability.
- Existing Vitest service mocks and Production route data test structure.

Avoid duplicating recipe field definitions between create and edit, adding a second
HTTP client, keeping copied recipe objects in modal state, or changing the global
feedback dialog for a recipe-only layout need.

## Risks

### Medium — stale edit overwrites current data

- Cause: a modal can remain open while the same recipe changes elsewhere.
- Surface: recipe update and version history.
- Mitigation: submit the loader-provided version and preserve the backend optimistic
  conflict; never retry automatically with a newer version.
- Verification signal: a conflict response becomes visible action feedback, does not
  close the modal, and does not report success.

### Medium — numeric and OAL coercion changes stored values

- Cause: browser number parsing can turn empty input into zero or lose the
  culture-neutral OAL string behavior.
- Surface: bullet weight, powder weight, and OAL.
- Mitigation: map empty optional values to null, reject non-finite numbers, retain OAL
  as a string, and leave two-decimal canonicalization to the server.
- Verification signal: route tests cover empty and populated optional numeric values;
  existing backend OAL precision tests remain green.

### Medium — historical snapshot regression

- Cause: an implementation could bypass the existing versioned PUT route or mutate
  production snapshots.
- Surface: data integrity and historical batches.
- Mitigation: call only the existing shared PUT route and make no persistence changes.
- Verification signal: update returns the incremented recipe while existing Workshop
  service tests continue to prove snapshot/version behavior.

### Medium — modal accessibility regression

- Cause: adding inputs to a focus trap that currently discovers only buttons and
  explicit tab stops.
- Surface: keyboard and assistive-technology users.
- Mitigation: include form controls in the focusable set, preserve dialog labelling,
  Escape, backdrop close, focus return, and alert semantics.
- Verification signal: keyboard component test or manual evidence covers opening,
  edit entry, forward/reverse tab containment, cancel/save, close, and focus return.

### Low — action state leaks between create and edit

- Cause: sharing one fetcher across two independently visible forms.
- Surface: pending labels, errors, and form closure.
- Mitigation: use independent fetchers against the same route action and return the
  submitted intent in the discriminated result.
- Verification signal: create errors do not alter modal state and update errors do not
  close the create surface.

### Low — responsive density becomes cramped

- Cause: all recipe fields in a narrow modal.
- Surface: mobile and zoomed layouts.
- Mitigation: widen only the recipe dialog, use two columns only when space permits,
  collapse to one column, and allow vertical scrolling inside the viewport.
- Verification signal: no horizontal scrolling or clipped controls at mobile width
  and 200% zoom.

### Low — external-service or rollout risk

- Cause: none; no external provider or backend rollout is introduced.
- Mitigation: deploy additive shared package/client changes through the existing
  frontend pipeline.
- Verification signal: package builds and client tests pass.

## Technical Debt

- Recipe search, caliber filters, duplicate, archive UI, linked production batches,
  and version-history UI are approved product requirements but do not block these four
  requested fixes. Defer them as separate slices.
- The repository currently shares one hand-built feedback modal shell among unrelated
  surfaces. Do not extract a generalized dialog component during this bounded change;
  isolate recipe-specific styles and behavior.
- The backend update endpoint and domain behavior exist with limited directly located
  route-level coverage. This does not block a frontend-only contract exposure, but a
  focused backend regression test is warranted only if current test discovery shows
  version conflict or snapshot preservation is not already covered.
- Direct entity responses from the Workshop API are existing contract debt. Do not
  introduce a DTO migration in this feature.

## Implementation Roadmap

1. **Complete the shared frontend contract.** Add and export the update request type
   from the models package. Add and export the shared recipe PUT operation using the
   existing account client and endpoint. Gate: models and services type-check with no
   consumer break.
2. **Establish Recipes route ownership.** Add the collection loader and intent-based
   create/update action, export them through the route module, and register them in the
   route manifest. Gate: tests prove loader delegation, full create payload, full
   update payload, version/idempotency inclusion, invalid form rejection, and visible
   service failure results.
3. **Unify recipe fields.** Refactor the Recipes component around loader data and a
   reusable complete fields component. Keep transient disclosure and selected ID as
   local state; remove `useResource` and all direct mutation calls. Gate: both create
   and edit render every user-editable contract field with correct defaults and null
   handling.
4. **Add editable modal flow.** Resolve selection from current loader data, add
   read/edit modes and independent update fetcher state, preserve conflict errors, and
   return to revalidated details after success. Gate: no stale selected object remains
   after update and duplicate submissions are disabled.
5. **Refine layout and accessibility.** Add recipe-scoped dialog width and a semantic
   two-column field grid with full-width notes and mobile collapse. Update focus
   containment for controls, labels, status messages, and translations. Gate: desktop,
   mobile, keyboard, and zoom evidence meets the Operate-mode brief.
6. **Validate affected workspaces.** Build shared models then services, run focused
   Recipes tests, run the Shooter Workshop test suite, and build the Shooter Workshop
   client. Run the narrow existing backend Workshop tests only to confirm no contract
   assumption regressed; no backend production changes are expected.
7. **Reconcile documentation.** Update the Shooter Workshop architecture baseline and
   this handoff with actual files, validation evidence, and residual risks. Stop once
   the four requested fixes and their directly required contract/test work are
   complete.

An independent post-validation review is warranted because the change crosses shared
frontend contracts, router data ownership, optimistic concurrency, and modal
accessibility. The review should remain focused on those risks and must not expand
into deferred recipe-management features.

## Testing Strategy

### Unit and route data

Owner: `frontend/shooter-workshop.client`.

- Loader returns the shared recipe collection.
- Create intent maps every editable field and maps empty optional values to null.
- Update intent sends recipe ID, all editable fields, current version, and one
  generated idempotency key.
- Missing required fields, invalid optional numbers, missing update ID, and invalid
  version do not call a service.
- Service validation and version-conflict errors return visible non-success results.

### Component and accessibility

Owner: `frontend/shooter-workshop.client`.

- Create disclosure contains name, caliber, OAL, bullet weight, bullet form, drawing,
  primer, powder, powder weight, and notes.
- Detail mode renders complete recipe data, server-managed status/version/update time,
  and `Not set` for absent optional values.
- Edit mode is prepopulated, Cancel restores read mode, Save uses the update intent,
  pending state disables duplicate submission, and success returns to current details.
- Dialog is labelled, traps focus across buttons and fields, closes with Escape and
  backdrop interaction, returns focus, and announces action errors.
- Layout semantics support two columns without relying on DOM order that becomes
  confusing when collapsed.

### Shared contract/service

Owner: `frontend/fe-packages/models` and `frontend/fe-packages/services`.

- Type-check all exports and consumers.
- Add a focused service test only if the shared services package has an established
  workshop transport-test seam; otherwise prove URL, method, and payload through the
  typed route action mock and package build rather than testing a trivial wrapper.

### Backend integration/behavior

Owner: `MorWalPizVideo.BackOffice.Tests`.

- Existing authenticated Workshop tests remain green.
- Confirm stale version returns conflict and a valid update increments the version.
- Confirm a batch created before editing retains its original recipe snapshot.
- Confirm cross-account recipe identifiers cannot be updated.
- No new backend test is required if these exact behaviors are already covered by
  focused existing tests and no backend file changes.

### Security

- Existing authentication and CSRF requirements remain active for POST and PUT.
- Recipe ID is URL encoded by the shared service.
- Errors are rendered as text, not HTML.
- The client never silently retries an optimistic-concurrency failure.

### Migration

Not applicable; no schema, backfill, or dual-read scenario.

### End to end and visual evidence

- Create a recipe containing all optional fields and verify list/detail values.
- Edit that recipe, verify version increments, and verify a pre-existing production
  batch snapshot is unchanged.
- Capture desktop read and edit modal states in two columns.
- Capture narrow mobile create and edit states in one column with no overflow.
- Exercise validation error, conflict, pending, success, empty list, and absent
  optional-value states.

## Migration And Rollback

- **MongoDB:** no migration. Existing recipe and recipe-version documents already
  contain all fields, and update persistence is established.
- **API contract:** additive frontend exposure of an existing PUT contract. Deploying
  the client against the current API is compatible. No compatibility window is
  required.
- **Authentication:** unchanged cookie/session and CSRF flow.
- **Cache:** no server cache migration. Router action completion revalidates loader
  data.
- **Configuration:** no new settings, flags, or secrets.
- **Deployment order:** build models, services, then the Shooter Workshop client.
  Current backend can remain deployed unchanged.
- **Monitoring:** watch client-visible validation/conflict failures and the existing
  API error telemetry for recipe PUT requests after release.
- **Rollback:** revert the client and additive shared exports. Recipes created or
  updated while the feature is active remain valid current data and their version
  records must not be deleted. Rolling back the UI removes edit access but does not
  require data recovery.
- **Rollback limit:** historical recipe versions are intentional audit data; rollback
  must not decrement versions or overwrite snapshots.

## Open Questions

None blocking. Archived-recipe editing is intentionally excluded because archived
records represent a terminal state in current domain behavior; adding restore or
editing semantics would require a separate product and domain decision.

## Delivery Record

Implemented on 2026-11-15:

- Added the shared `UpdateWorkshopRecipeRequest` model and `updateWorkshopRecipe`
  service operation against the existing versioned PUT endpoint.
- Added the Recipes loader/action and registered both in the route manifest.
- Replaced component-owned recipe loading and mutation callbacks with loader data and
  independent fetcher forms.
- Added all create/edit snapshot fields, optimistic version submission, error feedback,
  active-recipe edit mode, and responsive two-column details.
- Added focused route tests for loading, complete create/update payloads, validation,
  idempotency, and service failures.

Validation:

- Shared models build: passed.
- Shared services build: passed.
- Recipes route tests: 5 passed.
- Existing Workshop frontend test suite: 21 passed.
- Full Workshop frontend build: blocked by unrelated pre-existing Components changes:
  `getWorkshopComponentInstallations` is not exported from the shared service index,
  causing additional implicit-`any` errors in `Components/component.tsx`.

Residual risks remain limited to the documented deferred recipe-management features,
the existing browser authentication proof gap, and the unrelated Components build
blocker.
