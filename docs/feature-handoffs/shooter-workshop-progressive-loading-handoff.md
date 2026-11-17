# Shooter Workshop Progressive Loading Handoff

## Outcome

Replace the protected Workshop routes' generic full-page "Loading workshop data..." state with progressive, component-local loading states. Navigation must render the authenticated shell, page title, subtitle, tabs, filters whose values come from the URL, and safe actions immediately; only regions that require server data may show loading treatment.

This is an Impeccable **Operate-mode refinement**, not a redesign. The implementation should use the `shape` direction established here and preserve the current dark graphite, white, and selective yellow Workshop identity. The user-provided Equipment reference is the acceptance example: the Equipment heading, description, tabs, and active-tab action area remain visible while the selected list or table loads.

Assumptions:

- "All pages" means every protected route with deferred server data: Dashboard, Equipment, Ammunition/Recipes, Shooting Sessions/Maintenance, History, Reconciliation, Firearm Detail, and Recipe Production.
- Account login, registration, profile, legal redirects, and compatibility redirects are outside this change because they do not use the generic protected-route deferred state.
- Initial account-session restoration remains an authentication gate. This request changes in-app route navigation, not the security behavior before the application knows whether a session is authenticated.
- No API, persistence, authentication, cache, or deployment contract change is required.

## Current Evidence

- `frontend/shooter-workshop.client/src/main.tsx` mounts a React Router 7 data router inside the existing authentication and localization providers.
- `frontend/shooter-workshop.client/src/routes/config.tsx` registers protected loaders for Dashboard, Equipment, Firearm Detail, Ammunition, Recipe Production, Shooting Sessions, History, and Reconciliation.
- `frontend/shooter-workshop.client/src/routes/shared.tsx` defines `DeferredRoute`; its single `Suspense` fallback always renders the generic `LoadingState`.
- `frontend/shooter-workshop.client/src/components/States.tsx` supplies that fallback as a paragraph with `role="status"` and the translated text "Loading workshop data...".
- Every protected route component currently wraps its complete page content in `DeferredRoute`. Consequently, titles, subtitles, tabs, toolbar actions, and data regions all disappear together while the promise resolves.
- Loaders already return raw promises immediately through `deferRouteData`, matching the React Router 7 deferred-data convention documented in `docs/architecture/shooter-workshop.md`. Router-owned loaders, actions, fetchers, and revalidation therefore remain the correct architecture.
- Dashboard currently combines dashboard summary, history, and analytics requests with one `Promise.all`, so the slowest request blocks every Dashboard region.
- Equipment currently combines firearms, component definitions, inventory, and installations with one `Promise.all`, despite the three tabs needing different subsets.
- Ammunition currently combines recipes and batches. Shooting Sessions combines firearms, recipes, sessions, definitions, and inventory. These combined promises prevent independent region reveal.
- History, Reconciliation, Firearm Detail, and Recipe Production each use one route-owned service result. Their static chrome can still render before the result, but their data-dependent content must remain unavailable until that result resolves.
- `frontend/shooter-workshop.client/src/main.scss` already owns Workshop tokens, panels, cards, tables, responsive breakpoints, and reduced-motion handling. It has no skeleton primitives.
- `frontend/shooter-workshop.client/src/routes/shared.test.tsx` explicitly expects the old generic loading sentence, while the Equipment and Ammunition route tests assert the current combined loader shapes.
- `docs/architecture/frontend.md` and `docs/architecture/shooter-workshop.md` require route-owned server reads, route actions/fetchers for mutations, app-local Workshop workflows, and reuse of shared layout primitives only where behavior is genuinely cross-application.
- No `PRODUCT.md`, `DESIGN.md`, or Workshop surface brief is present under the application. The implemented stylesheet and the Workshop architecture specification are therefore the incumbent visual truth. The Impeccable context launcher was unavailable in this session; no context directives were invented.

## Proposed Design

### Loading-state system

Add a small app-local loading-state family rather than one universal placeholder:

1. **Inline spinner status**
   - Use inside a table body as the first row.
   - The row spans every column and contains a compact spinner plus localized "Loading data...".
   - Keep the real table caption and column headings visible.
   - This is the default for produced batches, component inventory, component catalogue, and other true tables.

2. **Card-grid skeleton**
   - Use for firearms and recipes.
   - Render a stable responsive grid with two to four neutral placeholder cards matching the existing record-card height and radius.
   - Placeholder blocks are decorative; one visually hidden polite status communicates loading.

3. **Activity skeleton**
   - Use for Dashboard Recorded Activity, Dashboard Recent Activity, History, and shooting/maintenance timelines.
   - Preserve the section footprint with restrained rows, date blocks, and summary blocks that mirror the final content.
   - The Dashboard Recorded Activity panel may be a complete internal skeleton because its statistics and chart/table are all data-driven; the surrounding Dashboard title, hero, quick actions, and section position remain immediate.

4. **Metric skeleton**
   - Use for stat grids and compact status summaries.
   - Preserve labels when they are static; replace only values with fixed-width neutral bars.

5. **Detail skeleton**
   - Use for Firearm Detail and Recipe Production because their titles and forms depend on the fetched entity.
   - Render the back navigation immediately where available, then a title-line skeleton and panel-shaped placeholders matching the real layout.
   - Do not render enabled mutation controls before their required entity identifiers and revisions are known.

6. **Panel skeleton**
   - Use for Reconciliation and other single-card data regions.
   - Preserve the panel dimensions and heading position without inventing status values.

Skeletons use the current surface/elevated/border tokens. Yellow remains reserved for selected tabs and meaningful actions, not loading decoration. A subtle left-to-right luminance movement may be used at normal motion preference; `prefers-reduced-motion: reduce` must make placeholders static. Loading treatment must not flash error colors, simulate real values, or shift layout when content arrives.

### Deferred boundary design

Keep React Router loaders as the only owner of server reads. Refine `DeferredRoute` into a region-capable await boundary that accepts a required fallback and preserves the existing async error behavior. Page components render static chrome outside those boundaries.

For routes with independent service calls, return an object of named promises rather than one promise created by `Promise.all`. This keeps all requests parallel while allowing each visible region to resolve independently. Do not move reads into effects or components.

Errors should be scoped to the failed region when requests are independent. A failed analytics request must not remove Dashboard summary, quick actions, or successfully loaded recent activity. Authentication failures must continue through the existing login transition. Mutations and successful router revalidation remain unchanged; during revalidation, only the affected data regions return to their local loading treatment.

### Route-by-route behavior

| Route | Immediate content | Loading treatment | Promise boundaries |
|---|---|---|---|
| Dashboard `/` | Page title/subtitle, operational-overview hero, primary recording link, Quick Actions, Personal Goal heading and locally stored goal controls where safe | Metric skeleton for overview counts; full internal skeleton for Recorded Activity; compact activity skeleton for Recent Activity; goal progress value skeleton while analytics loads | Separate dashboard summary, analytics, and history promises |
| Equipment `/firearms` | Page title/subtitle, all three tabs, selected tab, and the selected tab's safe create action | Firearms card skeleton; Inventory table with loading row; Catalogue table with loading row | Separate firearms, definitions, inventory, and installations promises; combine only at the smallest tab region that needs multiple results |
| Ammunition `/ammunition` | Page title/subtitle, Ammunition/Recipes tabs; active-tab structure | Recipe-dependent production form placeholder with disabled controls until recipes resolve; produced-batches table loading row; recipe card skeleton on Recipes tab | Separate recipes and batches promises |
| Shooting `/sessions/shooting` | Page title/subtitle, Shooting/Maintenance tabs, selected tab, and section heading | Recent-session activity skeleton; Maintenance compact-list skeleton; forms/actions remain disabled or withheld until required select options resolve | Separate firearms, recipes, sessions, definitions, and inventory promises; group only per visible workflow |
| History `/history` | Page title/subtitle, CSV action, filter toolbar, and URL-derived current filter/page | Full activity-list skeleton in the results panel | One history-items promise plus synchronous page, page size, and type metadata derived from the request URL |
| Reconciliation `/integrity` | Page title/subtitle | Status panel skeleton; rebuild action withheld until status and revision context are available | One reconciliation promise |
| Firearm Detail `/firearms/:id` | Back link | Detail skeleton including title, metric cards, chart panel, installed-components panel, and history panels | One detail promise from the existing aggregate endpoint |
| Recipe Production `/recipes/:recipeId/produce` | Stable route container and any safe back navigation retained by the current design | Title, recipe card, QR panel, and production-form skeleton; form remains unavailable until the recipe ID is resolved | One recipe promise |

The authenticated application header and navigation remain mounted throughout in-app navigation. Session restoration and unauthenticated redirection may continue to use the existing account-transition status because protected content must not be exposed before authentication is known.

## Impacted Projects

- **`frontend/shooter-workshop.client` — Modify.** Owns route loaders, route components, local loading primitives, localization, styling, and Vitest coverage.
- **`@morwalpiz/layout` — Verify only.** Continue using `PageTitle`, `TabbedWorkspace`, `Dialog`, and `EmptyState`; do not add Workshop-only skeletons to the shared package.
- **`@morwalpizvideo/services` — Verify only.** Existing service calls remain unchanged and continue to run from route loaders.
- **`@morwalpizvideo/models` — Verify only.** No DTO shape changes.
- **ServerAPI, Domain, Models, Contracts, and backend tests — Not applicable.** The existing endpoints already expose the required independent reads.
- **AppHost, Docker/nginx, and deployment workflows — Verify only.** No runtime topology or configuration change.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/src/routes/shared.tsx` — Replace the fixed full-page deferred fallback with a region-capable await boundary while preserving 401 and error handling.
- `frontend/shooter-workshop.client/src/components/States.tsx` — Retain account-transition/error/empty states and expose accessible loading-status behavior used by the new local skeletons.
- `frontend/shooter-workshop.client/src/main.scss` — Add skeleton, spinner, visually hidden status, table-loading-row, reduced-motion, responsive, and stable-dimension styles using existing Workshop tokens.
- `frontend/shooter-workshop.client/src/i18n.tsx` — Add English keys and Italian translations for concise region loading labels; remove the old generic key only after all consumers are migrated.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` and `loader.ts` — Render static Dashboard content immediately and split summary, analytics, and history promises.
- `frontend/shooter-workshop.client/src/routes/Firearms/component.tsx`, `loader.ts`, and `EquipmentPanels.tsx` — Keep Equipment chrome/tabs/actions immediate and add tab-specific local loading.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx`, `loader.ts`, and `AmmunitionPanel.tsx` — Split recipes from batches and use form/table-specific loading.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — Accept and render the card-grid loading treatment when embedded in Ammunition.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` and `loader.ts` — Split route resources and render tab-specific local loading.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` — Support its compact-list loading state without component-owned reads.
- `frontend/shooter-workshop.client/src/routes/History/component.tsx` and `loader.ts` — Keep URL-derived toolbar metadata synchronous and isolate the results promise.
- `frontend/shooter-workshop.client/src/routes/Reconciliation/component.tsx` and `loader.ts` — Keep the title immediate and await only the status panel.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` — Add immediate back navigation and a detail-shaped fallback around the existing aggregate promise.
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx` — Add an entity-detail fallback and keep production controls unavailable until recipe resolution.
- `frontend/shooter-workshop.client/src/routes/shared.test.tsx` — Replace the generic-sentence assertion with region-boundary, accessibility, resolve, and error tests.
- `frontend/shooter-workshop.client/src/routes/Firearms/route.test.ts` and `Ammunition/route.test.ts` — Update loader-shape assertions while preserving parallel call-count and revalidation checks.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/route.test.ts` — Add loader promise-shape and independent-resolution coverage alongside existing action tests.
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/route.test.ts`, `Maintenance/route.test.ts`, and `config.test.tsx` — Add or update focused route rendering assertions where their existing harness supports it.

### Add

- `frontend/shooter-workshop.client/src/components/loading/LoadingPrimitives.tsx` — App-local spinner, skeleton block, card-grid, table-row, metric, activity, panel, and detail primitives with accessible status ownership.
- `frontend/shooter-workshop.client/src/components/loading/LoadingPrimitives.test.tsx` — Focused accessibility and reduced-content semantic tests.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.test.tsx` — Verify immediate static Dashboard regions and independent data reveal.
- `frontend/shooter-workshop.client/src/routes/Firearms/component.test.tsx` — Verify the Equipment acceptance example: heading, tabs, and active action visible while list data is pending.
- `frontend/shooter-workshop.client/src/routes/History/component.test.tsx` — Verify toolbar persistence and activity skeleton during filter/page navigation.

### Generated/Migration

None. No generated asset, database migration, or lockfile change is required.

### Documentation

- `docs/architecture/shooter-workshop.md` — After implementation, update the route-data implementation-status paragraph to describe region-level deferred rendering instead of one complete-page fallback.
- `docs/feature-handoffs/shooter-workshop-progressive-loading-handoff.md` — Update after implementation with actual files, validation evidence, deviations, and residual risks.

### Verify Only

- `frontend/shooter-workshop.client/src/main.tsx` — Router/provider ownership stays unchanged.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — Route registrations and URLs stay unchanged unless type-only loader shape updates require no manifest change.
- `frontend/fe-packages/layout` — Confirm no existing cross-application skeleton primitive is being duplicated; keep this Workshop-specific system local.
- `frontend/fe-packages/services` — Confirm the same service calls and account transport remain in use.
- `docs/architecture/frontend.md` — No boundary change is expected; verify the result still follows loader/action ownership.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Not applicable; endpoint behavior and request count remain the same. |
| Frontend/UI | Primary impact. Static page chrome renders immediately; data regions use task-specific skeletons or loading rows. |
| Database/Data | Not applicable; no schema, query, or migration change. |
| Contracts/Models | Loader-facing TypeScript shapes change from one aggregate promise to named resource promises; wire DTOs do not change. |
| Authentication/Authorization | Existing session restoration, 401 handling, cookie transport, and redirects remain unchanged. No protected data is rendered before resolution. |
| Validation | Existing action validation remains unchanged. Controls requiring server identifiers/options remain disabled or withheld while loading. |
| Cache | Not applicable; no PWA API caching or output-cache change. |
| Configuration/Secrets | Not applicable; no new configuration or secret. |
| Logging/Telemetry | No new backend logging. Frontend loading failures continue through explicit route-region error UI rather than silent fallback. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Static bundle only; no host, nginx, service-worker, or rollout topology change. |
| Backward Compatibility | URLs, actions, forms, DTOs, API calls, and successful revalidation remain compatible. The intentional visual change removes the generic navigation placeholder. |

## Reuse Opportunities

- Keep `deferRouteData`, React `Suspense`, and React Router `Await`; evolve their presentation boundary instead of introducing another data-fetching library.
- Reuse `DeferredRouteError` for consistent 401 and request-error behavior, but place it at the smallest meaningful region.
- Reuse `PageTitle`, `TabbedWorkspace`, `Dialog`, and `EmptyState` from `@morwalpiz/layout`.
- Reuse current Workshop CSS tokens, card dimensions, panel radius, table markup, and responsive grids so skeletons accurately predict final geometry.
- Reuse URL search parameters and current `shouldRevalidate` functions; tab-only navigation must remain request-free.
- Reuse existing route-test service mocks and `I18nProvider`/`FeedbackProvider` test setup.
- Keep all new loading primitives app-local until a second application demonstrates the same semantic and visual need.

## Risks

1. **Medium — Partial-data dependency errors.** Splitting aggregate promises can accidentally render a form before all required option lists exist. Affected surfaces: Equipment, Ammunition, and Shooting Sessions. Mitigation: document each region's complete dependency set and withhold or disable unsafe actions until all required promises resolve. Verification signal: component tests hold one dependency pending while resolving the others.
2. **Medium — Revalidation flicker.** Fast mutations could cause skeletons to flash during automatic route revalidation. Affected surfaces: all action-backed routes. Mitigation: preserve resolved content where React Router provides it and use skeletons for genuine pending replacement, not fetcher submission state; do not blank unrelated regions. Verification signal: mutation tests show button-level pending first and local region refresh only when loader data is replaced.
3. **Medium — Error-boundary regression.** Independent promises can produce multiple failures or bypass the current 401 redirect if boundaries are wired incorrectly. Affected surface: all protected routes. Mitigation: retain one tested region wrapper that delegates to `DeferredRouteError`. Verification signal: 401 still redirects and a failed analytics promise leaves other Dashboard regions intact.
4. **Low — Layout shift.** Generic placeholders may not match real card, row, or panel dimensions. Mitigation: variant-specific skeletons share existing grid and panel classes and set realistic minimum heights. Verification signal: visual comparison at desktop, tablet, and mobile widths shows no material jump.
5. **Low — Excessive motion.** Shimmer can distract or violate user preferences. Mitigation: restrained neutral animation and a static reduced-motion variant. Verification signal: computed styles under reduced-motion contain no skeleton animation.
6. **Low — Accessibility noise.** Every placeholder could announce separately. Mitigation: decorative blocks are `aria-hidden`; each region owns one concise `role="status"`/polite announcement. Verification signal: accessibility tests find one status per pending region and no fake values in the accessibility tree.
7. **Low — Visual-system drift.** A bright or glossy skeleton could compete with the yellow brand accent. Mitigation: skeletons use only graphite surface, elevated, border, and muted values. Verification signal: rendered review confirms yellow remains exclusive to actions/selection.
8. **Low — Test overfitting to animation markup.** Brittle tests could lock internal skeleton structure. Mitigation: assert immediate static content, status semantics, table column span, and final reveal rather than class counts.

Security, data-integrity, cache-coherence, external-service, and irreversible rollout risks are otherwise unchanged because this is a client presentation refactor over existing authenticated calls.

## Technical Debt

- The current `DeferredRoute` name implies a whole-route boundary even though the desired primitive is region-level. Rename or clarify it during this change; this is directly coupled, not unrelated cleanup.
- Dashboard and multi-tab loaders currently use all-or-nothing `Promise.all` shapes. Splitting them is required for the requested behavior.
- `useResource`/`ResourceState` in `routes/shared.tsx` use effect-driven loading and conflict with the documented route-loader convention. They have no identified current consumer in this investigation. Do not expand scope to remove them unless implementation search confirms they are dead and removal is necessary for type/lint cleanliness.
- The application has no durable `PRODUCT.md`/`DESIGN.md`. This does not block the refinement because the implemented tokens and architecture specification are authoritative. Creating design-system documentation is deferred; the final build may optionally run Impeccable `document` as a separate approved task.
- Some page titles are entity-derived and cannot be real text before data resolves. Use title-shaped skeletons rather than inventing generic copy.

## Implementation Roadmap

1. **Establish the region-loading contract.** Add app-local loading primitives and region-aware deferred/error behavior. Preserve 401 redirection and expose one accessible status per region. Gate: primitive tests cover table row, decorative skeleton semantics, resolve, reject, and reduced-motion classes/behavior.
2. **Add visual styling and localization.** Implement neutral graphite skeleton variants, compact spinner, stable dimensions, responsive behavior, and Italian translations. Gate: no yellow loading decoration; no generic "Loading workshop data..." consumer remains on protected route navigation.
3. **Convert Dashboard first as the reference composition.** Split summary, analytics, and history promises; move title, hero, and quick actions outside data boundaries; apply metric/activity skeletons. Gate: independent promise-resolution test proves one slow request does not block unrelated regions.
4. **Convert Equipment as the user acceptance example.** Split four loader resources and place boundaries inside the selected tab. Render title, subtitle, tabs, and safe active-tab action immediately. Gate: pending Firearms shows card skeleton; pending Inventory/Catalogue shows real table headers and one loading row.
5. **Convert Ammunition and Recipes.** Split recipes and batches, keep tabs immediate, and prevent production submission before recipes resolve. Gate: batches can display while recipes remain pending and vice versa.
6. **Convert Shooting Sessions and Maintenance.** Split five route resources and define dependency groups for the shooting and maintenance tabs. Gate: static tab/action structure remains visible; no form exposes empty server-owned selectors as if ready.
7. **Convert History and Reconciliation.** Return History URL metadata synchronously, keep its toolbar immediate, and isolate result/status panels. Gate: filter/page navigation retains chrome and announces only the results region.
8. **Convert Firearm Detail and Recipe Production.** Add entity-detail skeletons while preserving safe navigation and preventing premature mutations. Gate: not-found and async error behavior remain unchanged after pending resolution.
9. **Remove obsolete generic navigation loading.** Retain account-session and redirect labels only where they describe real authentication transitions. Remove the generic translation after confirming no consumer remains.
10. **Update architecture and handoff documentation.** Record the implemented region-level deferred convention and actual validation evidence. Stop at progressive loading; do not redesign pages, add caching, change endpoints, or refactor unrelated forms.
11. **Run the Impeccable finish checks during implementation.** Apply the narrow `polish` lens to the completed Workshop loading states, then inspect rendered Dashboard, Equipment, Ammunition, Shooting, History, and both detail routes at representative desktop, tablet, and mobile widths. Verify reduced motion, keyboard focus continuity, no layout jump, and no loading flash on tab-only navigation.

An independent post-validation review is warranted because the change touches every protected data route and can regress authentication/error boundaries or action revalidation even though it is frontend-only. The user may select the MorWalPiz Code Reviewer after implementation.

## Testing Strategy

### Unit and component

- Loading primitives: one polite status per region, decorative blocks hidden from assistive technology, correct table `colSpan`, localized label, and static reduced-motion variant.
- Deferred region: pending fallback, successful reveal, scoped error, and 401 transition behavior.
- Dashboard: static title/hero/actions visible immediately; summary, analytics, and history reveal independently.
- Equipment: title/subtitle/tabs/action visible while pending; card skeleton for Firearms; header plus loading row for Inventory/Catalogue; tab-only navigation does not reload.
- Ammunition: tabs visible immediately; batches table and recipes resolve independently; production controls cannot submit before recipe options exist.
- Shooting/Maintenance: each tab waits only for its required resource set; repeat/edit/create behavior remains unchanged after resolution.
- History: title, download, and URL-derived filter remain visible while items are pending; pagination final state remains correct.
- Firearm Detail and Recipe Production: detail skeleton first, final entity UI second, existing not-found/error states preserved.

### Integration/behavior

- Existing route-loader tests continue to prove each service is called exactly once and in parallel.
- Existing action tests remain unchanged except for loader-shape types; successful mutations still trigger router revalidation.
- A controlled deferred test resolves promises in different orders and rejects one secondary Dashboard promise to prove partial resilience.

### Accessibility

- Status text is concise and localized; repeated visual bars do not become repeated announcements.
- Tables retain captions and headers while loading.
- Focus stays on navigation or the initiating control; skeletons are not focusable.
- No enabled action depends on unresolved identifiers, revisions, or option lists.
- Reduced-motion mode removes shimmer/continuous spinner animation or substitutes a static loading mark while retaining status text.
- Color contrast uses current text and muted tokens; loading is not communicated by color alone.

### Responsive and visual

- Desktop: 1440px-class Equipment screenshot matching the supplied reference composition.
- Tablet: tabs, metric grids, card grids, and detail placeholders preserve hierarchy without overflow.
- Mobile: card skeletons collapse with the final grid; loading table rows remain horizontally scrollable with their real tables; statuses do not force excessive width.
- Compare pending and resolved captures for cumulative layout shift, consistent panel height, and correct selected-tab yellow.

### Contract, security, migration, and end-to-end

- Contract tests: not required because API DTOs do not change.
- Security tests: retain existing unauthenticated redirect and 401 behavior coverage.
- Migration tests: not applicable.
- End-to-end/manual path: authenticated navigation through every main nav item, both tabs on multi-tab routes, one detail route, one recipe-production route, one failed request, and one successful mutation/revalidation.

## Migration And Rollback

- **MongoDB/SQLite:** No migration.
- **API contracts:** No compatibility window; requests and responses are unchanged.
- **Authentication:** No migration; HttpOnly cookie session restoration and account CSRF behavior are unchanged.
- **Cache:** No migration; the PWA continues not to cache authenticated API responses.
- **Configuration:** No new setting or secret.
- **Deployment order:** Ship as one Workshop static-client release after focused tests and visual checks. Backend coordination is unnecessary.
- **Feature flags:** Not required because the change is presentation-only and can be rolled back with the previous static bundle.
- **Monitoring:** Watch frontend request failures, repeated 401 redirects, and user reports of blank/disabled regions. Where browser telemetry is unavailable, release validation must explicitly exercise slow and failed requests.
- **Rollback:** Restore the previous Workshop bundle. No data written under the new UI is structurally different, so rollback has no data-recovery step.

## Open Questions

None blocking. The implementation should follow the route-by-route treatments above. Any proposal to keep stale resolved data visible during revalidation instead of showing a local skeleton is an optional follow-up optimization and must not introduce a second client cache or move reads out of React Router.

## Implementation Record

Implemented on 2026-11-17:

- Added app-local loading primitives in `frontend/shooter-workshop.client/src/components/loading/LoadingPrimitives.tsx` for status spinners, table loading rows, card grids, activity rows, metrics, panels, and detail layouts.
- Added token-based graphite skeleton styling, shimmer/spinner motion, responsive behavior, and reduced-motion fallbacks in `frontend/shooter-workshop.client/src/main.scss`.
- Extended `DeferredRoute` with a caller-provided fallback while preserving the existing async error and 401 transition path.
- Replaced the generic default route label with concise localized `Loading data…`, `Loading activity…`, `Loading summary…`, and `Loading details…` labels. Authentication transition labels remain unchanged.
- Converted Equipment and Ammunition loaders to return named promises so their visible regions can resolve independently. Equipment now keeps its title, tabs, and selected-workspace chrome visible while Firearms, Inventory, or Catalogue data resolves.
- Converted Dashboard to render the title, hero, primary action, and Quick Actions immediately, with a deferred data region and dashboard-specific skeletons.
- Added localized shells for History, Reconciliation, Firearm Detail, Recipe Production, and Shooting Sessions while their route-owned data is pending.
- Updated the Workshop architecture guide to document region-level deferred rendering.
- Updated affected loader and deferred-state tests for the named-promise contract.

Validation completed:

- `yarn --cwd frontend/shooter-workshop.client build` — passed; Vite emitted only the existing bundle-size warning.
- `yarn --cwd frontend/shooter-workshop.client test` — passed: 16 files, 48 tests.

Residual risk:

- Shooting Sessions retains its existing aggregate loader promise; its pending shell is localized and preserves the page title, tabs, disabled action, and activity/maintenance skeletons, but its five API calls do not yet reveal independently. Splitting that route further should be a follow-up focused refactor because the form state currently depends on several shared resolved collections.
- Visual browser captures were not available in this environment. The implementation uses the existing Workshop tokens and responsive rules; browser-level visual review remains recommended before release.
