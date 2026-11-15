# Shooter Workshop Ammunition And Production Merge Handoff

### Outcome

Merge the existing Production form into the Ammunition inventory page so the user can
record production and immediately review produced batches in one workflow. The
Ammunition route will adopt the established React Router loader/action/fetcher
architecture, with the production form above the produced-batches list.

Assumptions:

- `/ammunition` becomes the canonical production and batch-inventory screen.
- The separate Production navigation item is removed.
- `/production` remains as a replace redirect to `/ammunition` so bookmarks and
  previously shared links do not break.
- The current production request, batch response, authentication, CSRF, validation,
  idempotency, and API endpoints remain unchanged.
- This is an Operate-mode refinement of the incumbent Workshop interface, not a
  redesign.

### Current Evidence

- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` loads batches
  during component lifecycle with `useResource(getWorkshopBatches)` and wraps the
  result in `ResourceState`. It has no route loader or action.
- `frontend/shooter-workshop.client/src/routes/Ammunition/index.ts` exports only the
  component, and `frontend/shooter-workshop.client/src/routes/config.tsx` registers
  `/ammunition` without route data functions.
- `frontend/shooter-workshop.client/src/routes/Production/loader.ts` already loads
  recipes with `getWorkshopRecipes`.
- `frontend/shooter-workshop.client/src/routes/Production/action.ts` already converts
  `FormData`, performs basic production validation, creates the per-submission
  idempotency key, and delegates to `recordWorkshopProduction`.
- `frontend/shooter-workshop.client/src/routes/Production/component.tsx` already uses
  `useLoaderData`, `useFetcher`, and `fetcher.Form`, including pending and accessible
  action-feedback states.
- `frontend/shooter-workshop.client/src/routes/Production/route.test.ts` covers recipe
  loading, a valid production request, invalid form rejection, and service failure.
- `frontend/shooter-workshop.client/src/routes/config.tsx` currently registers
  `/production` with the Production loader and action. Its route configuration test
  asserts those functions are present.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` exposes separate
  Ammunition and Production navigation items.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` links the
  Record production quick action to `/production`.
- `frontend/fe-packages/services/src/workshopService.ts` already owns the canonical
  `getWorkshopBatches`, `getWorkshopRecipes`, and `recordWorkshopProduction`
  operations. No direct API client is needed in the application.
- `frontend/fe-packages/models/src/workshop.ts` already defines
  `WorkshopAmmunitionBatch`, `WorkshopRecipe`, and the production request shape.
- `docs/architecture/frontend.md` and
  `docs/architecture/shooter-workshop.md` require Workshop reads and mutations to be
  owned by colocated React Router loaders and actions, with fetchers and normal route
  revalidation.
- `docs/architecture/shooter-workshop.md` defines Production as the operation that
  creates a distinct batch and Ammunition as the batch inventory overview.
- No `PRODUCT.md` or `DESIGN.md` exists in the Shooter Workshop workspace. The current
  components, `main.scss`, and Workshop architecture document are the incumbent
  product and visual truth.

### Proposed Design

The `/ammunition` route becomes the single owner of the combined workflow:

1. Its loader fetches recipes and batches through the shared Workshop services and
   returns one typed route-data object.
2. Its action retains the existing Production form conversion, validation,
   idempotency, and mutation behavior.
3. Its component consumes loader data, renders one page title, places the production
   form first, and places a clearly labelled Produced batches section immediately
   below it.
4. The form submits through an Ammunition-owned fetcher. On success, React Router's
   normal action revalidation reruns the Ammunition loader, so the new batch appears
   without a component-owned reload or duplicate request path.
5. An empty batch collection affects only the list section. It must not replace or
   hide the production form.
6. The former `/production` route becomes a compatibility redirect. New navigation
   and dashboard links target `/ammunition` directly.

The loader should request recipes and batches concurrently because neither read
depends on the other. A failure in either read is a route-load failure rather than a
partially valid success-shaped page; the repository's router error handling remains
responsible for explicit retry/error presentation.

The production form should remain a route-local component or helper within the
Ammunition route unless extraction materially improves testability. It is
application-specific and is not a candidate for `@morwalpiz/layout`.

Material alternative rejected: retaining a second fully functional `/production`
route would duplicate route ownership and allow the two versions to drift. A redirect
preserves URL compatibility without preserving duplicate behavior.

### Impacted Projects

- `frontend/shooter-workshop.client` — route ownership, combined UI, navigation,
  localization, styling only if section spacing needs a narrow adjustment, and tests.
- `frontend/fe-packages/services` — verify only; existing reads and mutation are
  reused without transport changes.
- `frontend/fe-packages/models` — verify only; existing recipe, batch, and production
  request types remain authoritative.
- `MorWalPizVideo.ServerAPI` — verify only; no controller or endpoint change.
- `MorWalPiz.Contracts` — verify only; no wire-contract change.
- Backend test projects — not impacted because no backend behavior changes.
- Deployment surfaces — no topology, origin, container, or configuration change.

### Impacted Files

#### Modify

- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` — consume
  loader data, render the production form above the batch section, submit with a
  fetcher, and keep empty-state handling local to the list.
- `frontend/shooter-workshop.client/src/routes/Ammunition/index.ts` — export the
  component, loader, and action.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — register the Ammunition
  loader/action and replace the former Production route with a compatibility
  redirect.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — assert Ammunition
  route ownership and the `/production` redirect contract.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` — remove the duplicate
  Production navigation item.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` — point the
  Record production quick action to `/ammunition`.
- `frontend/shooter-workshop.client/src/routes/pages.tsx` — remove the obsolete
  Production component export if no external consumer remains.
- `frontend/shooter-workshop.client/src/i18n.tsx` — add or align only the merged
  section and feedback strings needed for complete Italian and English presentation.
- `frontend/shooter-workshop.client/src/main.scss` — only if needed to preserve clear
  vertical hierarchy between the form and batch section at desktop and mobile sizes.
- `docs/architecture/frontend.md` — identify Ammunition as the canonical combined
  loader/action route.
- `docs/architecture/shooter-workshop.md` — update the current route-data description
  and Ammunition screen composition.

#### Add

- `frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts` — concurrently
  load recipes and batches and export the typed loader-data contract.
- `frontend/shooter-workshop.client/src/routes/Ammunition/action.ts` — own production
  form conversion, validation, idempotency, and mutation.
- `frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts` — cover the
  combined loader and production action.
- A focused Ammunition component test beside the route if the existing test harness
  can exercise loader/fetcher rendering without introducing broad new infrastructure.
  It should prove section order and the list-only empty state.

#### Generated/Migration

None.

#### Documentation

- `docs/feature-handoffs/shooter-workshop-ammunition-production-merge-handoff.md` —
  implementation source of truth and later completion record.
- `docs/architecture/frontend.md`
- `docs/architecture/shooter-workshop.md`

#### Verify Only

- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/fe-packages/services/src/workshopService.test.ts`
- `frontend/fe-packages/models/src/workshop.ts`
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `frontend/shooter-workshop.client/src/components/States.tsx`

#### Remove After References Are Migrated

- `frontend/shooter-workshop.client/src/routes/Production/component.tsx`
- `frontend/shooter-workshop.client/src/routes/Production/loader.ts`
- `frontend/shooter-workshop.client/src/routes/Production/action.ts`
- `frontend/shooter-workshop.client/src/routes/Production/index.ts`
- `frontend/shooter-workshop.client/src/routes/Production/route.test.ts`

The behavior and meaningful tests move to Ammunition before these files are removed.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | No change. Existing recipe, batch, and production endpoints remain authoritative. |
| Frontend/UI | Production form and batch inventory become one Ammunition page. Duplicate navigation is removed. |
| Database/Data | No schema or stored-data change. Successful production still creates the same operation, session, and batch. |
| Contracts/Models | No new or changed contract. The Ammunition loader composes existing typed results. |
| Authentication/Authorization | Unchanged authenticated public-account cookie flow, CSRF handling, and account scoping. |
| Validation | Existing HTML constraints, action checks, and server validation remain. Feedback stays accessible and localized. |
| Cache | Not applicable. This route does not use output-cache tags. Router action revalidation refreshes loader data. |
| Configuration/Secrets | None. |
| Logging/Telemetry | No new event required. Existing HTTP telemetry observes loader and action failures. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Frontend-only deployment; no environment or infrastructure sequencing requirement. |
| Backward Compatibility | `/production` remains reachable through a replace redirect; APIs and data remain compatible. |

### Reuse Opportunities

- Reuse the existing Production loader/action logic by moving ownership rather than
  rewriting behavior.
- Reuse `getWorkshopRecipes`, `getWorkshopBatches`, and
  `recordWorkshopProduction`; do not add direct `fetch`.
- Reuse the existing production form fields, input constraints, local date helper,
  fetcher pending behavior, and alert semantics.
- Reuse React Router automatic action revalidation to refresh both recipes and
  batches after success.
- Reuse the current `PageTitle`, `EmptyState`, panel, form, table, and responsive
  styling primitives.
- Reuse the existing route test cases under the Ammunition owner, extending them only
  for the combined loader result.

### Risks

- **Medium — stale batch list after production:** the mutation succeeds but the list
  is not refreshed if the form bypasses route action ownership. Mitigation: register
  the action on Ammunition and rely on normal fetcher revalidation. Verification
  signal: a successful action causes the loader to run and the newly returned batch
  is rendered.
- **Medium — route-load coupling:** concurrently loading recipes and batches means
  either failure blocks the combined page. Mitigation: surface an explicit route
  error rather than rendering incomplete, success-shaped data. Verification signal:
  rejected recipe and batch reads each reach the established error boundary.
- **Medium — broken legacy links:** deleting `/production` outright would break
  bookmarks and old dashboard links. Mitigation: retain a replace redirect and update
  all current links. Verification signal: direct navigation to `/production` lands on
  `/ammunition` without history-loop behavior.
- **Medium — duplicate or non-idempotent submission:** moving the form could lose its
  pending guard or idempotency generation. Mitigation: move the existing action and
  fetcher behavior intact. Verification signal: the valid action test asserts one
  generated key and pending UI disables the submit control.
- **Low — empty-state regression:** the current `ResourceState` can replace the whole
  page. After the merge, no batches must not hide the production form. Mitigation:
  scope `EmptyState` to the Produced batches section. Verification signal: empty
  loader data renders both form and empty list state.
- **Low — responsive density:** stacking a form and wide table can create excessive
  scroll or unclear hierarchy on mobile. Mitigation: preserve the single-column form
  breakpoint and horizontal table overflow, with clear section headings. Verification
  signal: narrow-mobile evidence shows all controls, feedback, and table scrolling
  remain usable.
- **Low — localization drift:** current production success and action error text are
  not consistently passed through the translation boundary. Mitigation: ensure all
  user-facing merged feedback has dictionary coverage and is translated at render.
  Verification signal: both supported cultures show no English-only merged feedback.
- **Low — external-service risk:** none; the workflow uses only existing ServerAPI
  operations.

### Technical Debt

- Other Workshop routes still use `useResource`. That drift does not block this
  request and must not be migrated in this change.
- The current action returns human-readable English error strings rather than stable
  error codes. The merged component may translate known local validation strings, but
  redesigning the action/API error contract is deferred.
- The batch table currently exposes raw identifiers and has no filtering despite the
  broader feature document describing future caliber and recipe filters. Those
  enhancements are outside this merge.
- The prior Production handoff remains useful historical evidence but should not be
  rewritten as if the standalone route still owns current behavior after completion.

### Implementation Roadmap

1. Create the Ammunition loader and action by transferring the established Production
   behavior and adding the existing batch read to the loader. Define one typed result
   containing recipes and batches.
2. Refactor the Ammunition component to use loader data. Render one Ammunition page
   title, the production panel first, then a labelled Produced batches section with
   its table or list-scoped empty state.
3. Preserve fetcher submission, pending disablement, accessible success/error
   feedback, field names, local date default, action validation, and one idempotency
   key per submission. Confirm successful actions use router revalidation rather than
   a manual reload.
4. Export and register the Ammunition loader/action. Convert `/production` to a
   replace redirect to `/ammunition`.
5. Remove Production from primary navigation, retarget the dashboard quick action,
   and remove obsolete page exports.
6. Move and extend route tests under Ammunition, then remove the superseded Production
   module only after all imports and test ownership have moved.
7. Align merged copy and translations. Make only the smallest style adjustment
   required for section hierarchy and responsive spacing.
8. Update the two owning architecture documents and record actual files, validation,
   and residual risks in this handoff.
9. Validate focused route/config/component behavior, then the Shooter Workshop test
   suite and production build. Stop without migrating unrelated routes or adding
   inventory filters.

An independent post-validation review is not required for this bounded frontend
ownership move if focused tests and the full application build pass. It becomes
warranted only if implementation changes shared services, API contracts, or
authentication behavior beyond this plan.

### Testing Strategy

- **Loader unit tests:** verify recipes and batches are both returned in the typed
  result; verify each rejected service read produces an explicit loader failure.
- **Action unit tests:** retain valid payload, numeric conversion, optional notes,
  idempotency key, invalid input rejection, and service-error cases from the existing
  Production route tests.
- **Component tests:** verify the production form appears before the Produced batches
  heading/list; recipes populate the select; batch rows use loader data; empty batches
  leave the form visible; action success/error uses an alert; pending submission
  disables the button.
- **Revalidation behavior:** use the route-aware harness where practical to prove a
  successful fetcher action refreshes loader-backed batches. If the local harness
  cannot observe revalidation reliably, retain this as a manual browser gate rather
  than testing React Router internals with brittle mocks.
- **Route configuration:** verify Ammunition has loader and action functions,
  Production is a replace redirect, and protected child paths remain complete.
- **Navigation:** verify the shell has only Ammunition, and the dashboard production
  quick action targets `/ammunition`.
- **Localization:** verify the new/relocated section and feedback keys resolve in
  `it-IT` and `en-GB`.
- **Shared service contract:** retain existing tests proving canonical account
  transport, CSRF, endpoints, and DTO names; no new service test is required if those
  functions are unchanged.
- **Security:** no new authorization test is required because transport and endpoints
  do not change. Confirm the merged route remains under the authenticated shell.
- **End to end/manual:** sign in, open Ammunition, submit a valid production record,
  observe pending and success feedback, and confirm the new batch appears below
  without a full-page reload.
- **Impeccable:** implementation should apply a narrow `harden` check to this
  Operate-mode refinement, preserving the incumbent visual system. Minimum visual
  evidence is one desktop and one narrow-mobile rendering in both populated and empty
  batch states. Accessibility checks must include keyboard field order, visible focus,
  labelled controls, screen-reader-readable feedback, disabled/pending semantics, and
  horizontally scrollable table access.

### Migration And Rollback

- **MongoDB/SQLite:** no migration or backfill. No persistence shape changes.
- **API contracts:** no compatibility window required. Existing endpoints and payloads
  remain unchanged.
- **Authentication:** no migration. The route stays inside the authenticated Workshop
  shell and uses the existing account transport.
- **Cache:** no cache migration or eviction.
- **Configuration:** no new values or secrets.
- **Deployment order:** frontend-only. Deploy the combined route and redirect in one
  artifact.
- **Monitoring:** watch frontend route-load failures and production mutation failures;
  confirm successful production is followed by successful Ammunition loader reads.
- **Rollback:** restore the standalone Production route, navigation item, and dashboard
  target together. Because no API or data changes occur, rollback requires no data
  recovery. Keep `/production` behavior and navigation consistent within a single
  deployed frontend version.

### Open Questions

None identified. The recommended redirect preserves existing URLs while establishing
`/ammunition` as the sole owner of the combined workflow.

### Implementation Completed

Implemented on 2026-11-15:

- Added `Ammunition/loader.ts`, which concurrently loads account recipes and produced
  batches through the existing shared Workshop services.
- Added `Ammunition/action.ts`, preserving the existing production form validation,
  request mapping, service error feedback, and per-submission idempotency key.
- Updated the Ammunition component to render the production form first and the
  Produced batches section below it. The form uses `useFetcher` and automatic route
  revalidation; an empty batch list no longer hides the form.
- Exported and registered the Ammunition loader/action.
- Removed the duplicate Production navigation item and retargeted the dashboard
  production quick action to `/ammunition`.
- Kept `/production` as a replace redirect to `/ammunition` and removed the obsolete
  standalone Production route modules.
- Moved the route data tests to Ammunition and extended them for the combined loader.
- Added the Italian translation for the new Produced batches section heading.

### Validation Completed

- `cd frontend/shooter-workshop.client && yarn test src/routes/Ammunition/route.test.ts src/routes/config.test.tsx`
  — passed, 5 tests.
- `cd frontend/shooter-workshop.client && yarn test` — passed, 21 tests across 8
  files.
- `cd frontend/shooter-workshop.client && yarn build` — blocked by pre-existing
  TypeScript errors in `routes/Components/component.tsx` (implicit `any` parameters)
  and `routes/Components/loader.ts` (missing
  `getWorkshopComponentInstallations` service export). No errors were reported in the
  changed Ammunition, navigation, dashboard, or route configuration files before the
  unrelated baseline errors stopped compilation.

Deferred to downstream automatic validation: broad formatting/lint checks and browser
visual verification. No backend, API, persistence, authentication, or deployment
changes were made.

### Residual Risks

- Production browser flow and the `/production` redirect still need authenticated
  desktop/mobile verification.
- The application build remains blocked by unrelated existing Components route/service
  type errors; those were not changed as part of this merge.
- The workspace contained unrelated pre-existing modifications, which were preserved.

