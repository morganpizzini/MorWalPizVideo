# Shooter Workshop Ammunition Container/Presentation Refactor

## Outcome

Refine the Shooter Workshop `/ammunition` workspace so changing between the Ammunition and Recipes tabs does not rerun the route loader or repeat recipe and batch API reads.

### Implementation status

Implemented on 2026-11-17.

- Added `AmmunitionPanel.tsx` as the presentation component for production and batch inventory.
- Kept `Ammunition/component.tsx` as the route container for loader data, URL tab state, and the production fetcher.
- Changed `Recipes/component.tsx` to receive `recipes` through typed props instead of reading route loader data.
- Added and registered `ammunitionShouldRevalidate`, which suppresses revalidation only for tab-only search changes and preserves normal action/default revalidation.
- Updated tab navigation to preserve unrelated search parameters.
- Added focused route-policy tests.
- Validation: `cd frontend/shooter-workshop.client && npm test` passed with 15 test files and 43 tests. `npm run build` passed after removing an unused container type.

Residual scope is unchanged: no route-aware component test was added because the existing test setup has no router/component harness for this route; the route policy tests directly cover the revalidation regression. Browser visual/network capture was not run in this environment.

The existing route component becomes the server-state and action container. The production/batch panel is extracted as an Ammunition presentation component, while the existing Recipes component becomes a presentation component that receives its recipe data from the container.

Assumptions:

- The URL remains the source of truth for the selected tab.
- Successful production and recipe mutations must continue to revalidate the route loader.
- Existing form behavior, action feedback, accessibility semantics, styling, and responsive behavior remain unchanged.
- This is an Impeccable Operate-mode refinement, not a redesign. The incumbent black, white, and yellow visual language is preserved.
- Tab changes continue to unmount the inactive panel. Preserving unfinished local form or dialog state across tab changes is outside this refactor.

The Impeccable context launcher could not be executed in the read-only architecture environment. No application-local `PRODUCT.md`, `DESIGN.md`, or surface brief was found. Current source, `src/main.scss`, the Shooter Workshop architecture document, and the accepted consolidated-workspaces handoff are therefore the applicable design evidence.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/config.tsx` registers `/ammunition` with the Ammunition loader and action. `/recipes` and `/production` are compatibility redirects rather than independent active data routes.
- `frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts` always loads both recipes and batches concurrently.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` currently combines route-container responsibilities with the complete production form and batch-list presentation. It also embeds the Recipes component.
- The same component derives the active tab from the URL and changes it with `setSearchParams`.
- `frontend/fe-packages/layout/src/components/TabbedWorkspace.tsx` is a controlled, domain-neutral tab component. It reports tab selection but does not own routing or data loading.
- A search-parameter update is a React Router navigation. Because the `/ammunition` route has no `shouldRevalidate` policy, switching `tab` uses default revalidation and reruns the combined loader.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` reads loader data directly even when embedded under `/ammunition`. This couples a presentation component to an assumed parent route-data shape.
- `frontend/shooter-workshop.client/src/routes/Ammunition/action.ts` is the canonical action boundary. It handles production submissions and dispatches recipe create/update intents to `frontend/shooter-workshop.client/src/routes/Recipes/action.ts`.
- Successful fetcher submissions currently rely on normal React Router revalidation to refresh recipes and batches. This behavior must not be disabled.
- `frontend/morwalpizvideo.client/src/routes/home/loader.ts` and its route registration provide an existing repository precedent for route-specific suppression of search-only revalidation. Ammunition needs a narrower policy that suppresses only tab-only transitions.
- `frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts` covers loader and production action behavior.
- `frontend/shooter-workshop.client/src/routes/Recipes/route.test.ts` covers recipe loading and recipe action behavior.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` verifies route inventory and Ammunition loader/action registration.
- There is no current route-aware component test proving that tab changes avoid loader calls.
- `docs/architecture/frontend.md` establishes React Router loaders, actions, and fetchers as the Workshop server-state boundary.
- `docs/architecture/shooter-workshop.md` documents the combined Ammunition workspace, URL-owned tab state, recipe intent dispatch, and post-action revalidation.
- `docs/feature-handoffs/shooter-workshop-consolidated-workspaces-handoff.md` requires tab updates to preserve unrelated query parameters. The current Ammunition callback replaces the complete query string and does not satisfy that requirement.

Current data flow:

1. The `/ammunition` route loader reads recipes and batches.
2. The route component reads the combined loader result and the selected URL tab.
3. The Ammunition tab renders its production form and batch inventory inline.
4. The Recipes tab renders Recipes, which independently assumes access to recipe loader data.
5. A tab selection changes the URL search parameters.
6. Default React Router revalidation reruns the loader and both read APIs.
7. Production and recipe submissions use the canonical route action and intentionally rerun the loader after success.

## Proposed Design

### Route container

Keep `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` as the route container and make its responsibilities explicit:

- Read recipes and batches once from the Ammunition route loader.
- Read and update the URL-selected tab.
- Preserve every unrelated search parameter when changing only `tab`.
- Own the production fetcher and route-action feedback.
- Pass typed data, submission state, action feedback, and action-facing callbacks or form bindings to its presentation children.
- Compose the existing shared `TabbedWorkspace`.

The container must not copy loader data into component state. Loader data remains the authoritative server state and is refreshed through normal post-action revalidation.

### Ammunition presentation component

Extract the current production form and produced-batches section into a route-local Ammunition presentation component:

- Receive recipes, batches, pending state, and feedback through typed props.
- Render the production form, recipe selector, batch table, and existing empty/error states.
- Contain no loader, service, navigation, or URL-search ownership.
- Preserve current labels, fields, validation attributes, table structure, class names, and responsive behavior.

Because “Ammunition” is also the current route component name, use an unambiguous route-local name such as `AmmunitionPanel` for the presentation child unless the implementer performs a coherent container rename. Do not introduce a generic shared component: this panel has only one verified consumer.

### Recipes presentation component

Refine `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` so it receives `WorkshopRecipe[]` through typed props:

- Remove direct `useLoaderData` access and the cast to the Recipes loader-data shape.
- Keep presentation-local disclosure, selected-recipe, and edit state inside Recipes.
- Keep recipe fetcher forms, intent fields, pending UI, and structured action feedback aligned with the canonical `/ammunition` action.
- Do not introduce an effect or direct service read.

### Revalidation policy

Export an Ammunition-specific `shouldRevalidate` policy from the route-owned loader module and register it on `/ammunition`.

The policy must return false only when:

- current and next pathname are the Ammunition route;
- the only effective URL search change is the presentation-owned `tab` parameter; and
- React Router is not requesting revalidation for an action result or another explicit reason.

All other cases delegate to `defaultShouldRevalidate`. In particular:

- successful production actions revalidate;
- successful recipe create/update actions revalidate;
- explicit revalidation remains enabled;
- pathname changes remain enabled;
- changes to non-tab query parameters remain enabled.

Do not suppress all search-only revalidation. The homepage precedent is broader than this route’s requirement.

### Impeccable implementation lens

This surface is an Operate-mode refinement. During implementation, use the narrow `harden` lens for async, error, empty, and interaction states; use `adapt` only to confirm the extracted component preserves the existing narrow layout; and use `audit` as the completion check for accessibility and responsive regressions. No visual-world replacement, new design token, or new design-system artifact is required.

Minimum visual evidence:

- Ammunition tab at desktop width with populated and empty batch states.
- Recipes tab at desktop width.
- Both tabs at the existing narrow breakpoint.
- Keyboard tab selection and visible focus behavior.
- Pending and error feedback for both production and recipe actions.

## Impacted Projects

- `frontend/shooter-workshop.client`: Required. Owns the route, components, data loader, mutation action, route registration, styles, and tests.
- `frontend/fe-packages/layout`: Verify only. `TabbedWorkspace` remains unchanged and continues to supply controlled tab semantics and keyboard behavior.
- `frontend/fe-packages/models`: Verify only. Existing workshop recipe and ammunition batch contracts are sufficient.
- `frontend/fe-packages/services`: Verify only. Existing recipe/batch reads and production/recipe mutations remain unchanged.
- Backend .NET projects: Not applicable. No endpoint, contract, validation, authentication, or persistence behavior changes.
- Other frontend applications: Not applicable. No shared package API changes are planned.
- Shooter Workshop deployment image/workflow: Verify only. Existing build order and deployment topology remain unchanged.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx`
  - Retain route-container ownership, compose both presentation children, preserve query parameters, and remove inline production/batch markup.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx`
  - Accept recipes through typed props and remove route-loader coupling while preserving recipe interaction state and fetcher behavior.
- `frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts`
  - Add the route-specific tab-only revalidation policy beside the loader it governs.
- `frontend/shooter-workshop.client/src/routes/config.tsx`
  - Register the Ammunition `shouldRevalidate` policy.
- `frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts`
  - Add focused policy coverage while retaining loader/action tests.

### Add

- `frontend/shooter-workshop.client/src/routes/Ammunition/AmmunitionPanel.tsx`
  - Route-local presentation component for the production form and batch inventory.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.test.tsx`
  - Route-aware component coverage for tab navigation, prop-driven rendering, query preservation, and loader-call stability.

The exact presentation filename may follow an existing route-local naming convention discovered during implementation. Do not create a shared package component.

### Generated/Migration

None. No generated client, lockfile, database migration, or build artifact should change.

### Documentation

- `docs/feature-handoffs/shooter-workshop-ammunition-container-presentation-handoff.md`
  - Update after implementation with actual files changed, validation results, and residual risks.
- `docs/architecture/shooter-workshop.md`
  - Update only if the final ownership description differs materially from its current combined-workspace documentation. Otherwise verify that it remains accurate.

### Verify Only

- `frontend/shooter-workshop.client/src/routes/Ammunition/action.ts`
  - Confirm production and recipe intent dispatch remain the sole action boundary.
- `frontend/shooter-workshop.client/src/routes/Recipes/action.ts`
  - Confirm recipe create/update behavior and structured feedback remain unchanged.
- `frontend/shooter-workshop.client/src/routes/Recipes/loader.ts`
  - Existing orphaned read boundary; no change is required for this refactor.
- `frontend/shooter-workshop.client/src/routes/Recipes/route.test.ts`
  - Existing recipe action coverage must continue to pass.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx`
  - Confirm route registration expectations remain accurate; extend only if the established test style asserts `shouldRevalidate`.
- `frontend/shooter-workshop.client/src/main.scss`
  - Confirm existing classes and breakpoints remain sufficient after extraction.
- `frontend/fe-packages/layout/src/components/TabbedWorkspace.tsx`
  - Confirm controlled selection, ARIA relationships, and keyboard behavior are preserved without modification.
- `frontend/fe-packages/models/src/workshop.ts`
  - Reuse `WorkshopRecipe` and `WorkshopAmmunitionBatch`.
- `frontend/fe-packages/services/src/workshopService.ts`
  - Confirm no new service method or endpoint is needed.
- `.github/workflows/deploy-shooter-workshop-client.yml`
  - Confirm the existing test/build gate covers the changed workspace.
- `frontend/shooter-workshop.client/Dockerfile`
  - Confirm no build-order or runtime change is required.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No API change. Existing recipe, batch, and production endpoints remain canonical. |
| Frontend/UI | Route component becomes a container; Ammunition and Recipes become presentation children. Visual behavior is preserved. |
| Database/Data | No schema or persisted-data change. Loader data remains authoritative. |
| Contracts/Models | No contract change. Existing shared workshop types are reused for props. |
| Authentication/Authorization | No change. Existing authenticated route and service credential behavior remain intact. |
| Validation | Existing native/form and route-action validation remain unchanged. Presentation extraction must not move server validation into the client. |
| Cache | No backend cache change. Client navigation avoids unnecessary reads only for tab-only transitions. |
| Configuration/Secrets | No configuration or secret impact. |
| Logging/Telemetry | No new logging is required. Existing request telemetry should show the absence of reads on tab-only navigation. |
| Background Jobs | Not applicable. |
| Deployment/Operations | No topology or deployment-order change. Existing frontend test and build gates apply. |
| Backward Compatibility | `/ammunition`, `/recipes`, and `/production` URLs retain their behavior. Existing action payloads and service calls remain unchanged. |

## Reuse Opportunities

- Reuse the controlled `TabbedWorkspace`; do not teach it React Router or Ammunition-specific behavior.
- Reuse `WorkshopRecipe` and `WorkshopAmmunitionBatch` from the shared model package for component props.
- Reuse the existing Ammunition loader as the sole combined read boundary.
- Reuse the existing Ammunition action and recipe intent delegation as the sole mutation boundary.
- Reuse existing fetcher forms and structured action-result rendering.
- Reuse the route-specific `shouldRevalidate` pattern demonstrated by the public-client home route, narrowed to the Ammunition tab parameter.
- Reuse current SCSS class names and responsive rules to make extraction behavior-neutral.
- Reuse existing Ammunition and Recipes route test fixtures and service mocks.

## Risks

### High: stale data after mutations

- Cause: an over-broad `shouldRevalidate` policy suppresses post-action revalidation.
- Surface: production batches and recipe lists.
- Mitigation: suppress only pure tab-only navigation and delegate every other case to `defaultShouldRevalidate`.
- Verification signal: successful production and recipe mutations each trigger one loader refresh and display updated data.

### Medium: non-tab query parameters are lost

- Cause: replacing the entire search-parameter object during tab selection.
- Surface: deep links and future query-owned workspace state.
- Mitigation: mutate only the `tab` parameter on a copy of current search parameters.
- Verification signal: unrelated query values survive Ammunition-to-Recipes and Recipes-to-Ammunition transitions.

### Medium: presentation component remains route-coupled

- Cause: Recipes or AmmunitionPanel continues to call `useLoaderData`, navigation hooks, or read services.
- Surface: component reuse, tests, and ownership clarity.
- Mitigation: define typed prop contracts and keep route reads/navigation in the container.
- Verification signal: both presentation children render in tests from supplied props without a route loader.

### Medium: tab test proves rendering but not network stability

- Cause: a shallow component test does not execute React Router navigation/revalidation.
- Surface: original regression.
- Mitigation: use a route-aware test with loader or service spies and perform real tab interactions.
- Verification signal: loader/read counts remain unchanged across both tab switches.

### Low: styling or accessibility regresses during extraction

- Cause: markup, IDs, tab-panel relationships, classes, or element order change unintentionally.
- Surface: responsive layout, keyboard use, screen-reader semantics.
- Mitigation: retain current markup/classes and continue using `TabbedWorkspace`.
- Verification signal: desktop/narrow visual evidence and keyboard/ARIA assertions remain correct.

### Low: local form or dialog state resets on tab change

- Cause: `TabbedWorkspace` renders only the selected panel.
- Surface: unfinished recipe or production interaction.
- Mitigation: preserve existing behavior and document it as outside scope.
- Verification signal: no claim or test is added for cross-tab draft persistence.

### Low: orphan Recipes loader creates future confusion

- Cause: `/recipes` now redirects, but a standalone Recipes loader remains.
- Surface: maintainability only.
- Mitigation: defer removal unless implementation proves it has no remaining consumer and cleanup stays isolated.
- Verification signal: active route configuration has one canonical read boundary.

Security, data-integrity, external-service, and rollout risk are otherwise low because no auth, contract, persistence, or endpoint behavior changes.

## Technical Debt

- Recipes currently depends on an assumed loader-data shape through a cast. This debt directly blocks the requested presentation boundary and must be fixed now.
- The tab callback currently drops unrelated query parameters. This is tightly coupled to the tab-navigation change and must be fixed now.
- `frontend/shooter-workshop.client/src/routes/Recipes/loader.ts` appears orphaned because `/recipes` is a redirect. It does not block the requested behavior; defer deletion to avoid widening scope.
- The inactive tab is unmounted and loses presentation-local state. This is pre-existing behavior and is explicitly deferred.
- The production panel currently shares a file and name context with the route component. Resolve naming clearly, but do not introduce a generic abstraction.

## Implementation Roadmap

1. **Lock the regression with focused route-policy tests**
   - Extend the Ammunition route tests with tab-only, non-tab-search, pathname, action-result, and default-delegation scenarios.
   - Gate: the new tab-only expectation fails against the current route behavior.

2. **Introduce the narrow Ammunition revalidation policy**
   - Add the policy beside the Ammunition loader and register it on the route.
   - Preserve normal loader behavior and post-action refresh.
   - Gate: tab-only transitions suppress revalidation; every other tested case delegates to React Router defaults.

3. **Define typed presentation boundaries**
   - Establish route-local typed props for recipes, batches, pending state, and feedback.
   - Keep shared wire/domain contracts in the existing model package; do not duplicate model shapes.
   - Gate: no presentation child needs loader-data casts or read-service access.

4. **Extract AmmunitionPanel**
   - Move the production form and batch inventory markup into the route-local presentation component.
   - Preserve DOM semantics, form submission contract, classes, labels, empty states, and action feedback.
   - Gate: populated, empty, pending, and error states render from props with no service call.

5. **Decouple Recipes from loader data**
   - Supply recipes from the route container.
   - Preserve local dialog/edit state and existing fetcher mutation behavior.
   - Gate: Recipes renders supplied data without a loader context and still posts the same action intents.

6. **Complete the container composition**
   - Keep loader data, URL tab ownership, production fetcher, and route-action coordination in the current route component.
   - Update only the `tab` search parameter while preserving unrelated values.
   - Gate: direct URLs, tab clicks, keyboard selection, and browser history reflect the correct selected tab.

7. **Add route-aware regression coverage**
   - Exercise both tab transitions through React Router.
   - Assert panel content changes without an additional loader or recipe/batch read.
   - Assert a successful mutation still refreshes the appropriate visible data.
   - Gate: tests distinguish tab navigation from mutation revalidation.

8. **Validate the Operate-mode refinement**
   - Check desktop and narrow layouts, focus visibility, tab semantics, forms, dialogs, tables, and async/error states.
   - Confirm no shared style or design-system change is necessary.
   - Gate: the extracted implementation is visually and behaviorally equivalent except for removed tab refetches.

9. **Update durable documentation**
   - Record actual files changed, test/build results, and residual risks in this handoff.
   - Update Shooter Workshop architecture only if final ownership differs from its current description.
   - Stop at this boundary; orphan-loader removal and cross-tab draft persistence require separate approval.

An independent post-validation review is not mandatory for this bounded frontend refactor if route-aware regression tests cover both loader stability and mutation revalidation. A MorWalPiz Code Reviewer is warranted if implementation broadens into route cleanup, shared layout changes, or altered mutation behavior.

## Testing Strategy

### Unit

Owning test file: `frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts`.

- Tab changes in either direction return false from the route policy.
- Adding, removing, or changing a non-tab query parameter delegates to the default decision.
- Path changes delegate to the default decision.
- Action-result and explicit/default revalidation remain enabled through delegation.
- Existing loader mapping and action validation tests remain unchanged.

### Integration/route behavior

Owning test file: `frontend/shooter-workshop.client/src/routes/Ammunition/component.test.tsx`.

- Initial navigation runs the combined loader once.
- Ammunition is selected by default.
- Selecting Recipes updates the URL and renders supplied recipes.
- Returning to Ammunition renders production and batch content.
- Neither tab transition increments loader or recipe/batch read counts.
- Browser Back/Forward restores selected tabs without unnecessary reads when the transition remains tab-only.
- Unrelated query parameters survive both transitions.
- Successful production submission revalidates and renders the new batch.
- Successful recipe create/update revalidates and renders refreshed recipes.
- Failed actions display structured feedback without a success-shaped fallback.

### Component/presentation

- AmmunitionPanel renders recipe options, populated batches, the empty state, pending state, and action feedback entirely from props.
- Recipes renders supplied recipes without loader context.
- Existing disclosure, edit, cancel, pending, and error behavior remains intact.

### Contract

- No new contract tests are required because payloads and endpoints do not change.
- Existing services and route-action tests must continue to prove production and recipe request mapping.

### Security

- Verify forms continue to use existing route actions and shared services, preserving current credentials and CSRF behavior.
- Verify no direct browser service call or new token handling is introduced by either presentation component.

### Migration

Not applicable. There is no schema or data transition.

### End-to-end/manual

- Confirm network activity shows no recipe or batch reads when switching tabs repeatedly.
- Confirm one appropriate refresh follows each successful mutation.
- Check desktop and narrow breakpoints.
- Check mouse and keyboard tab selection, visible focus, tab/panel ARIA relationships, form labels, pending indicators, alerts, and table overflow.

Validation should use the existing Shooter Workshop test and build gates. Shared packages require no rebuild beyond the application’s established dependency build order because their source is unchanged.

## Migration And Rollback

### MongoDB and SQLite

Not applicable. No persisted schema, query, index, or local database changes.

### API contracts

No migration. Request and response shapes, URLs, action intents, and compatibility redirects remain unchanged.

### Authentication

No migration. Existing route protection, cookie credentials, and CSRF behavior remain unchanged.

### Cache

No server cache migration. This is a client route-revalidation refinement. Post-action refresh remains the compatibility requirement.

### Configuration

No feature flag, environment variable, secret, or runtime configuration is required.

### Deployment order

The frontend can deploy through the existing Shooter Workshop pipeline with no backend coordination.

### Monitoring

During validation, use loader/service spies and browser network evidence to confirm:

- zero additional reads for tab-only navigation;
- one expected refresh after successful mutations;
- no increase in action errors.

### Rollback

Rollback is source-only:

- remove the route-specific `shouldRevalidate` registration to restore default loader behavior;
- revert the container/presentation composition together so Recipes does not expect props that the route no longer supplies.

There is no data rollback and no compatibility window because no external contract changes.

## Open Questions

None blocking.

The implementation may choose a route-local presentation filename other than `AmmunitionPanel.tsx` if an existing naming convention is found, but the ownership boundary and completion criteria must remain unchanged.
