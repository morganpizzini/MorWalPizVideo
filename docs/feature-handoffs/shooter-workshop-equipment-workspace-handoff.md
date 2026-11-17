# Shooter Workshop Equipment Workspace

## Implementation status

Implemented on 2026-11-17.

- Consolidated the Firearms and Components route boundaries into the `/firearms` Equipment workspace.
- Added URL-selected Firearms, Inventory, and Catalogue tabs using the shared `TabbedWorkspace`.
- Combined firearm and component reads into one loader and delegated component mutations through the canonical Firearms action.
- Added tab/filter-only revalidation suppression while preserving post-action refresh.
- Retained `/components` as a query-preserving Inventory compatibility redirect.
- Updated navigation, detail links, onboarding, localization, and obsolete custom component-tab styles.
- Added focused combined-loader, revalidation, redirect, and route-registration tests.
- Validation: focused equipment/config tests passed; full Shooter Workshop test suite and production build passed.

Residual scope:

- Browser visual/network capture was not run in this environment.
- No new route-aware component test harness was added; loader/revalidation/redirect and existing UI tests cover the route policy, while the implementation preserves the existing panel markup and shared tab primitive.
- The legacy `Components/component.tsx` compatibility component remains only for the unused `pages.tsx` export; the active route is redirect-only.

## Outcome

Merge the current Shooter Workshop Firearms and Components pages into one canonical equipment workspace at `/firearms`, with three URL-selected tabs:

1. Firearms
2. Inventory
3. Catalogue

The `/firearms` route becomes the single container for loader data, tab/filter URL state, and mutation dispatch. Each tab receives a focused presentation component, following the implemented Ammunition container/presentation pattern.

Assumptions:

- The primary navigation and page title use the umbrella label **Equipment** because the workspace contains firearms, physical component inventory, and component definitions. The canonical URL remains `/firearms` to avoid unnecessary route churn.
- Firearm detail remains at `/firearms/:id` and is not converted into a workspace tab.
- `/components` remains a compatibility URL and redirects to `/firearms?tab=inventory`, preserving `firearmId` and other existing query parameters.
- Firearms is the default tab and therefore omits the `tab` query parameter. Inventory uses `tab=inventory`; Catalogue uses `tab=catalogue`.
- Existing forms, validation, services, authentication, copy semantics, visual language, and domain behavior remain unchanged except where labels must describe the merged workspace.
- This is an Impeccable Operate-mode refinement, not a redesign. The incumbent black, white, and yellow visual system remains authoritative.
- No application-local `PRODUCT.md`, `DESIGN.md`, or surface brief exists. Current source, `src/main.scss`, the shared `TabbedWorkspace`, and the Shooter Workshop architecture document are the applicable visual evidence.

Implementation boundary:

- Complete the route consolidation, presentation extraction, compatibility redirect, navigation/link updates, focused tests, and directly related documentation.
- Do not redesign firearm detail, change component domain rules, alter API contracts, or introduce new shared UI primitives.
- Stop after the merged workspace is behaviorally and visually verified. Broader component workflow changes require separate approval.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/config.tsx` currently registers `/firearms` and `/components` as separate data routes with separate loaders and actions.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` exposes Firearms and Components as separate primary-navigation entries.
- `frontend/shooter-workshop.client/src/routes/Firearms/component.tsx` currently combines route-container concerns, firearm creation state, form presentation, and firearm-card rendering.
- `frontend/shooter-workshop.client/src/routes/Firearms/loader.ts` loads only firearms.
- `frontend/shooter-workshop.client/src/routes/Firearms/action.ts` owns only firearm creation and currently has no explicit action intent.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` combines route-container concerns with:
  - `firearmId` URL validation and updates;
  - local Inventory/Catalogue tab state;
  - custom tab keyboard behavior;
  - inventory and catalogue filtering;
  - modal and form behavior;
  - component-definition and physical-component mutations.
- `frontend/shooter-workshop.client/src/routes/Components/loader.ts` loads definitions, physical inventory, firearms, and installations concurrently. This duplicates the firearm read when both areas are considered one equipment workspace.
- `frontend/shooter-workshop.client/src/routes/Components/action.ts` owns definition, physical-component, installation, archive, and guarded-delete intents.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` links component management to `/components?firearmId={id}`.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx` directs the component onboarding step to `/components`.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` currently asserts that `/components` has an active loader and action.
- There are no focused Firearms or Components route tests. Existing coverage does not prove combined loading, mutation dispatch, URL tab behavior, or compatibility redirects.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` is the implemented repository precedent:
  - one route container reads combined loader data;
  - selected tab is URL-owned;
  - shared `TabbedWorkspace` supplies accessible tab behavior;
  - route-local presentation children receive typed data;
  - unrelated query parameters are preserved during tab changes.
- `frontend/shooter-workshop.client/src/routes/Ammunition/action.ts` is the implemented precedent for one canonical route action dispatching child-workflow intents to an existing focused action.
- `frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts` is the implemented precedent for combined concurrent reads and tab-only revalidation suppression.
- `frontend/shooter-workshop.client/src/main.scss` contains both legacy `.component-tabs` styles and the shared `.mwp-tabbed-workspace__*` styles. The merged workspace should use the shared primitive and remove legacy tab styling only after no consumer remains.
- `docs/architecture/frontend.md` and `docs/architecture/shooter-workshop.md` require server reads and mutations to remain at React Router loader/action boundaries.
- `docs/architecture/shooter-workshop.md` currently documents Firearms and Components as separate main screens, so implementation changes the documented frontend route boundary but not the backend domain boundary.

Current route flows:

### Firearms

1. `/firearms` loads firearm records.
2. The route component renders firearm creation and firearm cards.
3. The creation form posts to the Firearms action.
4. Successful creation uses normal React Router revalidation.
5. A firearm card opens `/firearms/:id`.

### Components

1. `/components` loads definitions, physical inventory, firearms, and installations.
2. The route component selects Inventory or Catalogue through local component state.
3. `firearmId` is read from the URL and normalized against loaded firearms.
4. Inventory and Catalogue create their own fetchers but post to the Components route action.
5. Successful mutations use normal React Router revalidation.

## Proposed Design

### Canonical route container

Keep `frontend/shooter-workshop.client/src/routes/Firearms/component.tsx` as the route container and narrow it to orchestration:

- Read the combined equipment loader result.
- Derive the active tab from the URL.
- Own and normalize the presentation-only `firearmId` query parameter.
- Preserve unrelated query parameters when changing `tab` or `firearmId`.
- Compose the existing shared `TabbedWorkspace`.
- Pass typed data and URL-facing callbacks to the three presentation components.
- Keep all API reads out of effects and presentation components.

The active-tab rules are:

- absent, unknown, or `tab=firearms`: Firearms;
- `tab=inventory`: Inventory;
- `tab=catalogue`: Catalogue.

Unknown tab values should render Firearms and be normalized with replace-navigation so unsupported values do not remain shareable.

### Combined equipment loader

Expand `frontend/shooter-workshop.client/src/routes/Firearms/loader.ts` to load the complete workspace data concurrently:

- firearms;
- component definitions;
- physical component inventory;
- component installations.

The loader must request firearms once. It must not call the existing Firearms and Components loaders in a way that duplicates that read.

The resulting route-owned type becomes the prop source for all three panels. Existing shared model contracts remain authoritative; do not introduce duplicate app-local domain models.

### Firearms presentation

Extract the current Firearms content into a route-local `FirearmsPanel`:

- Receive firearms through typed props.
- Retain firearm creation disclosure state and its fetcher.
- Add an explicit `create-firearm` intent to make canonical-action dispatch deterministic.
- Preserve firearm-card links to `/firearms/:id`.
- Preserve populated, empty, pending, success-close, and error behavior.
- Contain no loader-data access, service read, tab selection, or search-parameter ownership.

### Inventory presentation

Extract the current Inventory section into a route-local `InventoryPanel`:

- Receive physical inventory, definitions, firearms, installations, the validated firearm filter, and a filter callback through typed props.
- Retain local search, compatibility-tag filters, modal state, fetcher state, and component form behavior.
- Preserve create, edit, installation move, archive, and guarded-delete payloads.
- Preserve `firearmId` as a shareable URL filter without owning React Router search hooks.
- Contain no loader-data access, API reads, or active-tab state.

### Catalogue presentation

Extract the current Catalogue section into a route-local `CataloguePanel`:

- Receive component definitions through typed props.
- Retain local search, modal state, fetcher state, and definition form behavior.
- Preserve create, edit, archive, and guarded-delete payloads.
- Contain no loader-data access, API reads, URL state, or active-tab state.

Route-local helpers used by more than one equipment panel, such as the modal, action error, search field, management action, and form types, may be placed in one focused route-local helper module. Do not move these feature-specific pieces into `@morwalpiz/layout`.

### Canonical action dispatch

Expand `frontend/shooter-workshop.client/src/routes/Firearms/action.ts` into the canonical equipment action:

- Dispatch `create-firearm` to the firearm-creation behavior.
- Delegate all existing component-definition and physical-component intents to `frontend/shooter-workshop.client/src/routes/Components/action.ts`.
- Preserve each existing request contract, idempotency behavior, validation, error behavior, and normal post-action revalidation.
- Reject unsupported or missing intents explicitly rather than treating them as firearm creation.

Keep `Components/action.ts` as the focused component mutation handler, matching the Ammunition-to-Recipes delegation pattern. It no longer needs to be registered as an independent route action.

### URL revalidation policy

Add a Firearms-specific `shouldRevalidate` policy beside the combined loader and register it on `/firearms`.

The policy may suppress revalidation only when all effective URL changes are presentation-owned:

- `tab`;
- `firearmId`.

It must delegate to `defaultShouldRevalidate` for:

- successful or failed action processing;
- explicit revalidation;
- pathname changes;
- changes to any other query parameter;
- any router case that is not a pure presentation-state transition.

This prevents repeated reads when switching tabs or changing the in-memory inventory filter while preserving refreshed data after firearm, definition, inventory, archive, delete, install, or move mutations.

### Compatibility route

Retain `/components` as a redirect-only compatibility route:

- Redirect to `/firearms?tab=inventory`.
- Preserve `firearmId` and every unrelated query parameter.
- Override any incoming `tab` value with `inventory`.
- Do not register an independent Components element or action.
- Do not issue component/firearm reads before redirecting.

This keeps old bookmarks and external links functional without preserving a second data owner.

### Navigation and direct links

- Replace the separate Firearms and Components primary-navigation entries with one Equipment entry targeting `/firearms`.
- Update the firearm-detail component-management link to `/firearms?tab=inventory&firearmId={id}`.
- Update the component onboarding step to `/firearms?tab=inventory`.
- Keep the firearm onboarding step at `/firearms`.
- Update directly related localized labels and descriptions so users understand that one workspace owns firearms, inventory, and catalogue.
- Do not rewrite historical event labels such as `FirearmCreated` or `ComponentCreated`; those describe domain events, not navigation.

### Impeccable implementation lens

This surface is an Operate-mode refinement. During implementation:

- use `shape` as represented by this handoff to preserve the task hierarchy;
- use `harden` for unsupported tab values, invalid firearm filters, async failures, destructive confirmations, and empty states;
- use `adapt` to verify the three-tab workspace and wide tables at current narrow breakpoints;
- use `audit` as the completion check for tab semantics, focus behavior, dialogs, forms, tables, and responsive overflow.

Minimum visual evidence:

- Firearms, Inventory, and Catalogue tabs at desktop width with populated data;
- all three tabs at the existing narrow breakpoint;
- empty Firearms, empty Inventory, empty Catalogue, and no-search-results states;
- firearm, physical-component, and definition forms in normal, pending, and error states;
- inventory deep-linked with a firearm filter;
- keyboard tab selection, visible focus, dialog focus containment/restoration, and table overflow.

No replacement visual world, new design token, or new shared design-system component is required.

## Impacted Projects

- `frontend/shooter-workshop.client`: Required. Owns route composition, loader/action boundaries, compatibility redirect, navigation, localization, styles, and tests.
- `frontend/fe-packages/layout`: Verify only. Existing `TabbedWorkspace`, `PageTitle`, and `EmptyState` remain sufficient.
- `frontend/fe-packages/models`: Verify only. Existing firearm, definition, physical-component, installation, and request contracts remain sufficient.
- `frontend/fe-packages/services`: Verify only. Existing reads and mutations remain canonical.
- `MorWalPizVideo.ServerAPI`: Not applicable. No endpoint, authorization, validation, or response contract changes.
- `MorWalPizVideo.Domain`: Not applicable. Domain ownership and component/firearm independence remain unchanged.
- `MorWalPizVideo.Models` and `MorWalPiz.Contracts`: Not applicable. No persisted or transport model changes.
- Other frontend applications: Not applicable. No shared package API changes are planned.
- Shooter Workshop deployment image and workflow: Verify only. Existing frontend build and deployment topology remain unchanged.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/src/routes/Firearms/component.tsx`
  - Retain route-container ownership and compose the three typed presentation panels with `TabbedWorkspace`.
- `frontend/shooter-workshop.client/src/routes/Firearms/loader.ts`
  - Load the combined equipment data once and export the presentation-only revalidation policy.
- `frontend/shooter-workshop.client/src/routes/Firearms/action.ts`
  - Add explicit intent dispatch for firearm creation and existing component mutations.
- `frontend/shooter-workshop.client/src/routes/Components/action.ts`
  - Retain focused component mutation handling; adjust exported typing or error normalization only if required by delegation.
- `frontend/shooter-workshop.client/src/routes/Components/loader.ts`
  - Replace active data loading with the query-preserving `/components` compatibility redirect.
- `frontend/shooter-workshop.client/src/routes/config.tsx`
  - Register the combined `/firearms` loader/action/revalidation policy and make `/components` redirect-only.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx`
  - Replace separate Firearms and Components entries with one Equipment entry.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx`
  - Point component management to the Inventory tab with the current firearm filter.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx`
  - Point the component onboarding step to the Inventory tab.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.test.tsx`
  - Update the component-step destination assertion and retain the firearm-step assertion.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx`
  - Assert the canonical equipment route boundaries and redirect-only compatibility route.
- `frontend/shooter-workshop.client/src/i18n.tsx`
  - Add or update only the Equipment workspace, tab, subtitle, and directly related navigation translations.
- `frontend/shooter-workshop.client/src/main.scss`
  - Retain shared workspace styles and remove obsolete custom Components tab styles after confirming no remaining consumer.

### Add

- `frontend/shooter-workshop.client/src/routes/Firearms/FirearmsPanel.tsx`
  - Presentation component for firearm creation and firearm cards.
- `frontend/shooter-workshop.client/src/routes/Firearms/InventoryPanel.tsx`
  - Presentation component for physical component inventory, filters, modal, and form.
- `frontend/shooter-workshop.client/src/routes/Firearms/CataloguePanel.tsx`
  - Presentation component for definition search, modal, and management actions.
- `frontend/shooter-workshop.client/src/routes/Firearms/route.test.ts`
  - Focused combined-loader, action-dispatch, revalidation, validation, and redirect tests.
- `frontend/shooter-workshop.client/src/routes/Firearms/component.test.tsx`
  - Route-aware workspace tests for tab state, query preservation, panel rendering, and loader stability.

An optional route-local helper file under `src/routes/Firearms/` may be added only if it removes verified duplication shared by Inventory and Catalogue. It must not become a general-purpose abstraction.

### Delete

- `frontend/shooter-workshop.client/src/routes/Components/component.tsx`
  - Remove after Inventory and Catalogue presentation responsibilities are extracted and `/components` no longer renders an independent page.

If the Components barrel exports the deleted component, update or remove that export. Do not retain an unused compatibility component.

### Generated/Migration

None. No generated client, lockfile, database migration, build output, or deployment artifact should change.

### Documentation

- `docs/feature-handoffs/shooter-workshop-equipment-workspace-handoff.md`
  - Update after implementation with actual files changed, validation results, and residual risks.
- `docs/architecture/shooter-workshop.md`
  - Replace the separate Firearms/Components frontend route description with the canonical Equipment workspace while preserving the separate domain concepts.
- `docs/architecture/frontend.md`
  - Update the Workshop route-data paragraph if needed to name the Equipment workspace as another combined, URL-tabbed route.

No ADR is required because this consolidates frontend route ownership without changing domain ownership, external contracts, persistence, authentication, deployment topology, or operational policy.

### Verify Only

- `frontend/shooter-workshop.client/src/routes/Firearms/index.ts`
  - Confirm the existing route barrel still exports the canonical component, loader, and action as required.
- `frontend/shooter-workshop.client/src/routes/Components/index.ts`
  - Confirm it exports only what the redirect route still consumes, or remove unused exports.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/loader.ts`
  - Confirm detail loading is unaffected.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/action.ts`
  - Confirm maintenance/archive behavior is unaffected.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx`
  - Reuse its container/presentation and URL-tab pattern without modifying it.
- `frontend/shooter-workshop.client/src/routes/Ammunition/action.ts`
  - Reuse its action-delegation pattern without modifying it.
- `frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts`
  - Reuse its revalidation-policy pattern, narrowed to Equipment presentation parameters.
- `frontend/fe-packages/layout/src/components/TabbedWorkspace.tsx`
  - Confirm three-tab semantics, keyboard behavior, IDs, and panel rendering require no shared change.
- `frontend/fe-packages/models/src/workshop.ts`
  - Reuse existing firearm and component contracts.
- `frontend/fe-packages/services/src/workshopService.ts`
  - Confirm all required reads and mutations already exist.
- `.github/workflows/deploy-shooter-workshop-client.yml`
  - Confirm the existing test/build gate covers the changed workspace.
- `frontend/shooter-workshop.client/Dockerfile`
  - Confirm no build-order or runtime configuration change is required.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No API change. Existing firearm, definition, physical-component, installation, archive, and delete endpoints remain canonical. |
| Frontend/UI | Firearms and Components become one Equipment workspace with Firearms, Inventory, and Catalogue tabs. Firearm detail remains separate. |
| Database/Data | No schema, index, persisted-data, or ownership change. Firearms and components remain independent aggregates. |
| Contracts/Models | No contract change. Existing shared Workshop types are reused as loader results and panel props. |
| Authentication/Authorization | No change. The merged workspace remains under the authenticated shell and uses existing account-cookie/CSRF transport. |
| Validation | Existing route-action and server validation remain authoritative. Missing/unsupported action intents must fail explicitly. |
| Cache | No backend cache change. Client revalidation is suppressed only for presentation-owned URL changes; successful mutations still refresh the combined loader. |
| Configuration/Secrets | No environment variable, feature flag, secret, or runtime configuration change. |
| Logging/Telemetry | No new application logging is required. Browser/service telemetry should show one combined initial read set and no repeat reads for tab/filter-only navigation. |
| Background Jobs | Not applicable. |
| Deployment/Operations | No topology or deployment-order change. Existing Shooter Workshop test/build/deploy gates apply. |
| Backward Compatibility | `/firearms` and `/firearms/:id` remain stable. `/components` redirects to the Inventory tab and preserves query parameters. Existing action payloads and service contracts remain unchanged apart from the new route-local firearm intent field. |

## Reuse Opportunities

- Reuse `TabbedWorkspace` instead of maintaining custom component-tab keyboard and ARIA logic.
- Reuse the Ammunition container/presentation composition pattern.
- Reuse the Ammunition action-dispatch pattern for component intents.
- Reuse the Ammunition `shouldRevalidate` approach, expanded only to the Equipment-owned `tab` and `firearmId` parameters.
- Reuse existing shared firearm, component-definition, physical-component, installation, tracking-mode, and request types.
- Reuse existing Workshop service methods; do not introduce direct `fetch`, new clients, or effect-driven reads.
- Reuse current modal, error, search, management-action, and form behavior when extracting Inventory and Catalogue.
- Reuse existing `PageTitle`, `EmptyState`, cards, tables, buttons, badges, form classes, and responsive table wrappers.
- Reuse current FirearmDetail and onboarding link patterns with updated destinations.

## Risks

### High: mutations do not refresh all dependent panels

- Cause: an over-broad `shouldRevalidate` policy suppresses action revalidation.
- Affected surface: firearm list, inventory, catalogue, installations, and filter options.
- Mitigation: suppress only pure changes to `tab` and `firearmId`; delegate every action/default case.
- Verification signal: every successful mutation triggers one combined loader refresh and the affected panel renders current data.

### High: action intent collision or accidental firearm creation

- Cause: the current firearm form has no intent and the component action owns several string intents.
- Affected surface: all Equipment mutations.
- Mitigation: add explicit `create-firearm`; dispatch known component intents to the focused handler; reject every unsupported or missing intent.
- Verification signal: focused action tests prove each intent reaches exactly one service and unknown intents reach none.

### Medium: duplicate firearm reads remain after consolidation

- Cause: composing the old Firearms and Components loaders without removing their overlapping call.
- Affected surface: route latency and API request volume.
- Mitigation: make the Firearms loader own one concurrent read set with one firearm request.
- Verification signal: loader tests and browser network evidence show one call per required resource.

### Medium: old component deep links lose their firearm filter

- Cause: a static `/components` redirect discards the incoming query string.
- Affected surface: bookmarks and FirearmDetail-originated workflows.
- Mitigation: build the redirect from the incoming URL, preserve all parameters, and force only `tab=inventory`.
- Verification signal: `/components?firearmId=x&filter=y` resolves to `/firearms?tab=inventory&firearmId=x&filter=y`.

### Medium: presentation components remain route-coupled

- Cause: extracted panels continue using loader or search-parameter hooks.
- Affected surface: maintainability, focused tests, and ownership clarity.
- Mitigation: use typed props for loader data and URL-facing callbacks; allow fetchers only for route-action form submission.
- Verification signal: each panel renders from supplied data without a loader context or service-read mock.

### Medium: component mutation errors escape as route errors

- Cause: the existing component action does not normalize service failures like the firearm action.
- Affected surface: modal/form feedback after consolidation.
- Mitigation: preserve current behavior unless implementation confirms a directly coupled inaccessible failure; if normalization is needed, apply one consistent structured error contract across Equipment panels and test it.
- Verification signal: failed mutations produce visible, accessible feedback and no success-shaped result.

### Low: invalid tab or firearm query values create unstable URLs

- Cause: unsupported URL state is only interpreted, not normalized.
- Affected surface: shareable links and browser history.
- Mitigation: normalize unsupported tabs to Firearms and invalid firearm IDs by removing only the invalid parameter with replace-navigation.
- Verification signal: valid unrelated query parameters survive normalization.

### Low: visual or accessibility regression during extraction

- Cause: changed markup/classes, duplicated panel IDs, custom tabs remaining, or dialog focus behavior moving incorrectly.
- Affected surface: keyboard navigation, screen readers, narrow layouts, and tables.
- Mitigation: use shared `TabbedWorkspace`, preserve existing panel/form/table markup, and retain dialog focus behavior.
- Verification signal: component assertions plus desktop/narrow visual evidence show correct roles, labels, focus, and overflow.

### Low: umbrella naming is unclear

- Cause: canonical URL remains `/firearms` while navigation and title become Equipment.
- Affected surface: wayfinding only.
- Mitigation: use explicit tab labels and a subtitle naming firearms, inventory, and catalogue; preserve stable URLs.
- Verification signal: usability review confirms the destination and tabs are unambiguous.

Security, data-integrity, external-service, and rollout risks remain low because no authorization, persistence, endpoint, or deployment behavior changes.

## Technical Debt

- `Components/component.tsx` currently owns container, tabs, filters, dialogs, forms, and tables in one file. This directly motivates the requested refactor and must be resolved now through three presentation panels.
- The custom `.component-tabs` implementation duplicates the shared `TabbedWorkspace`. Remove it as part of consolidation after verifying there is no remaining consumer.
- Firearm creation has no explicit action intent. This blocks safe canonical-action dispatch and must be corrected now.
- Components has no focused route/action tests. Add only coverage needed for the merged boundary and existing mutation contracts.
- `Components/action.ts` has weaker structured error handling than `Firearms/action.ts`. Do not broaden this task into a general error-system redesign; normalize only if required to preserve accessible behavior through the merged action.
- The canonical `/firearms` path will represent a broader Equipment workspace. A route rename would add compatibility and navigation cost without domain benefit, so retain it.
- Firearm detail remains a large mixed-responsibility component. It is outside this workspace refactor and must not be cleaned up opportunistically.

## Implementation Roadmap

1. **Lock route and mutation behavior with focused tests**
   - Add combined-loader call-count expectations, explicit firearm-intent validation, representative component-intent delegation, unknown-intent rejection, presentation-only revalidation cases, and compatibility redirect query preservation.
   - Gate: new consolidation expectations fail against the current separate-route implementation.

2. **Define the canonical Equipment loader contract**
   - Expand the Firearms loader to request firearms, definitions, physical inventory, and installations concurrently without duplicate reads.
   - Export one typed loader result used by the container and panels.
   - Gate: each service read occurs once and the result shape is complete.

3. **Define deterministic action dispatch**
   - Add `create-firearm` to the firearm form contract.
   - Route firearm creation locally and delegate known component intents to the focused Components action.
   - Reject missing and unsupported intents.
   - Gate: each tested intent invokes only its expected service behavior.

4. **Extract FirearmsPanel**
   - Move firearm disclosure, form, cards, empty state, pending/error feedback, and detail links out of the route container.
   - Gate: populated, empty, create, pending, success-close, and failure states render from props and fetcher state.

5. **Extract InventoryPanel and route-local shared helpers**
   - Move inventory search, tags, firearm filtering, table, modal, component form, and actions into the typed panel.
   - Keep `firearmId` ownership in the container.
   - Gate: filtering and mutations work without loader/search hooks in the panel.

6. **Extract CataloguePanel**
   - Move definition search, table, modal, form, and management actions into the typed panel.
   - Gate: populated, empty, no-results, create/edit, archive/delete, pending, and failure states remain intact.

7. **Compose the three-tab Equipment container**
   - Add shared `TabbedWorkspace`, URL-derived active tab, query-preserving tab/filter updates, and invalid-value normalization.
   - Gate: direct URLs, click/keyboard selection, and Back/Forward render the correct panel.

8. **Add the narrow revalidation policy**
   - Suppress only pure `tab`/`firearmId` transitions and register the policy on `/firearms`.
   - Gate: tab/filter navigation performs no reads; successful mutations perform one combined refresh.

9. **Convert `/components` to compatibility redirect**
   - Preserve incoming query parameters and force the Inventory tab without loading workspace data.
   - Remove the independent Components element/action registration and delete its obsolete page component.
   - Gate: old URLs arrive at the correct Inventory state with no pre-redirect API reads.

10. **Update navigation, deep links, onboarding, localization, and styles**
    - Consolidate the primary navigation, update FirearmDetail and onboarding destinations, add Equipment copy, and remove unused custom tab styles.
    - Gate: no maintained link targets the standalone Components page and no obsolete custom tab selector remains in use.

11. **Validate the Operate-mode refinement**
    - Check the three panels, direct links, browser history, dialogs, forms, tables, pending/errors, keyboard behavior, and narrow layouts.
    - Gate: the merged workspace preserves existing workflows while reducing route and component complexity.

12. **Update durable documentation**
    - Update the Shooter Workshop architecture route description and this handoff with actual changes, validation, and residual risks.
    - Stop at the defined implementation boundary.

An independent post-validation review is warranted because the change consolidates two mutation surfaces and several destructive actions behind one route. The reviewer should focus on intent dispatch, revalidation safety, compatibility links, accessible error behavior, and accidental API-call duplication.

## Testing Strategy

### Unit and route data

Owning file: `frontend/shooter-workshop.client/src/routes/Firearms/route.test.ts`.

- Combined loader returns firearms, definitions, inventory, and installations.
- Each read service is called exactly once.
- Firearm creation requires `create-firearm` and preserves current validation/request mapping.
- Representative definition and component intents delegate to the focused component handler.
- Every supported component intent remains reachable through `/firearms`.
- Missing and unsupported intents fail explicitly without calling a mutation service.
- Service failures retain accessible structured feedback according to the accepted action-result contract.
- Pure `tab` changes skip revalidation.
- Pure `firearmId` changes skip revalidation.
- Combined `tab` and `firearmId` changes skip revalidation.
- Non-presentation query changes, pathname changes, actions, and explicit/default revalidation remain enabled through delegation.
- `/components` redirects to Inventory without loading data.
- Compatibility redirect preserves `firearmId` and unrelated query parameters.

### Route-aware integration

Owning file: `frontend/shooter-workshop.client/src/routes/Firearms/component.test.tsx`.

- Default `/firearms` renders Firearms.
- Direct `tab=inventory` and `tab=catalogue` URLs render the correct panel.
- Unknown tabs normalize to Firearms without dropping unrelated parameters.
- Selecting each tab updates the URL and preserves unrelated parameters.
- Browser Back/Forward restores the selected tab.
- Tab and firearm-filter transitions do not increment loader/read counts.
- Invalid `firearmId` is removed while valid unrelated parameters survive.
- A successful firearm mutation refreshes the combined route data.
- A successful definition mutation refreshes the combined route data.
- A successful physical-component/install mutation refreshes the combined route data.
- Failed mutations retain the active panel and show accessible feedback.

### Presentation components

- `FirearmsPanel` renders populated and empty states, opens/closes the form, preserves submitted values on failure, and links to details.
- `InventoryPanel` filters by search, compatibility tags, installed firearm, and uninstalled state; it renders empty/no-results states and preserves modal behavior.
- `CataloguePanel` filters definitions and preserves create/edit/archive/delete behavior.
- Dialog tests verify Escape, focus containment, and focus restoration if those remain route-local rather than moving to the shared Dialog.

### Navigation and localization

- `config.test.tsx` proves `/firearms` owns loader/action/revalidation and `/components` is redirect-only.
- `OnboardingGuide.test.tsx` proves the component step targets the Inventory tab.
- Verify one Equipment primary-navigation entry replaces the previous pair.
- Verify new user-facing strings have both Italian and English values.

### Contract

- No backend or shared-contract tests are required because endpoint and DTO shapes do not change.
- Existing service request mapping remains authoritative and should be asserted through route-action tests, not duplicated in presentation tests.

### Security

- Verify all forms still post through React Router actions and existing shared services.
- Verify no panel introduces direct API calls, bearer storage, manual cookie handling, or CSRF bypass.
- Verify destructive confirmations remain present and action intent cannot be altered into an unintended mutation without server-side validation.

### Migration

Not applicable. There is no schema or persisted-data transition.

### End-to-end/manual

- Open each tab directly and through primary navigation.
- Open the Inventory tab from a firearm detail and confirm the firearm filter.
- Open an old `/components` bookmark with and without `firearmId`.
- Switch tabs and filters repeatedly while observing zero repeat read requests.
- Perform one firearm create, definition mutation, and physical-component/install mutation and observe one refresh after each.
- Check desktop and narrow breakpoints, table overflow, modal sizing, focus visibility, keyboard tab selection, alerts, and empty states.

Validation should use the existing Shooter Workshop test and build gates. Shared package source is unchanged, so no new package-specific test suite is required.

## Migration And Rollback

### MongoDB and SQLite

Not applicable. No document, collection, index, serializer, query, or local database change.

### API contracts

No external migration. Existing API URLs and request/response shapes remain unchanged. The new `create-firearm` field is an internal browser-to-route-action discriminator and is not sent as an API contract field.

### Authentication

No migration. The workspace remains protected by the existing authenticated shell and account transport.

### Cache

No server cache migration. The only cache-like behavior is client route revalidation. Post-action refresh is the compatibility requirement.

### Configuration

No feature flag, environment variable, secret, or deployment setting is required.

### Deployment order

Deploy the Shooter Workshop frontend as one atomic application change. No backend coordination or compatibility window is required.

### Monitoring

Validation evidence should confirm:

- one initial call for each combined loader resource;
- zero additional calls for tab/filter-only navigation;
- one combined refresh after successful mutations;
- no increase in action or redirect errors;
- old `/components` links resolve to the intended Inventory state.

### Rollback

Rollback is source-only and must restore the route set coherently:

- restore separate Firearms and Components element/loader/action registrations;
- restore separate primary-navigation entries and old component links;
- restore the original Components component before removing panel imports;
- remove the Firearms dispatch/revalidation behavior together with the merged container.

There is no data rollback and no irreversible operation. Existing backend contracts remain compatible with either frontend version.

## Open Questions

None blocking.

The plan assumes **Equipment** is the desired umbrella navigation/page label while `/firearms` remains the stable canonical URL. If product wording must remain **Firearms**, only the navigation/title/localization copy changes; the route, tab, data, action, compatibility, and testing design remain the same.
