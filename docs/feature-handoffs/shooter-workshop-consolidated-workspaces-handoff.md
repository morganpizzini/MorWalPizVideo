# Shooter Workshop Consolidated Workspaces Handoff

## Outcome

Consolidate four existing Shooter Workshop routes into two user-visible, tabbed
workspaces while preserving their business components and route-owned data/mutations:

- **Ammunition**: Ammunition first, Recipes second.
- **Shooting sessions**: Shooting sessions first, Maintenance second.

Extract the successful Components-route tab and modal interaction behavior into neutral,
controlled primitives in `@morwalpiz/layout` so the pattern can be adopted by other
applications without importing Shooter Workshop models, services, translations, routes,
or styles.

This is an Impeccable **Operate** refinement of the incumbent black/white/yellow
interface, not a redesign. The narrow planning command is `shape`; implementation
finishes with `harden`, `adapt`, and `audit`.

Assumptions resolved for implementation:

- The canonical displayed and navigation label remains **Ammunition**. The user typo
  “ammonition” does not become product copy or a URL.
- `/ammunition` selects Ammunition and `/ammunition?tab=recipes` selects Recipes.
- `/sessions/shooting` selects Shooting sessions and
  `/sessions/shooting?tab=maintenance` selects Maintenance.
- Existing `/recipes` and `/maintenance` bookmarks redirect with replace semantics to
  the matching canonical tab.
- `/recipes/:recipeId/produce` remains an independent route.
- The shared components own semantics and interaction, not feature data, translation,
  routing, form submission, modal-success policy, or product-specific styling.
- Successful create operations close their modal after action success and route
  revalidation. Validation/transport failure keeps the modal and its draft open.
- “Repeat as draft” selects Shooting sessions, populates a fresh draft/idempotency
  identity, and opens the shooting modal.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/config.tsx` registers `/ammunition`,
  `/recipes`, `/sessions/shooting`, and `/maintenance` as separate protected routes
  with separate loaders/actions. It also preserves `/production` as a replace redirect
  to `/ammunition` and exposes `/recipes/:recipeId/produce` independently.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` currently exposes
  Maintenance, Ammunition, Recipes, and Shooting sessions as four navigation entries.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` contains the
  successful Inventory/Catalogue tab interaction: linked tab/panel semantics, roving
  focus, Arrow Left/Right, Home/End, and Inventory-first selection.
- The same Components file contains a local modal implementation with initial/return
  focus, focus containment, Escape and backdrop dismissal, and modal semantics.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx`
  is the stronger dialog precedent because it additionally applies `inert` and
  `aria-hidden` to background application content. Its tests verify focus restoration,
  background isolation, and failed-draft retention.
- `frontend/fe-packages/layout/src/index.ts` exposes no generic tabs or dialog.
  `frontend/fe-packages/layout/src/components/` has no equivalent implementation.
- `frontend/fe-packages/layout/package.json` publishes the main component barrel and
  one complete style bundle. It has no package-local test runner.
- Five applications declare `@morwalpiz/layout`: Shooter Workshop, BackOffice SPA,
  public video client, shop client, and Shooting ITA.
- Only the shop is verified to import `@morwalpiz/layout/styles`. Shooter Workshop
  applies local styling to shared component markup.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` renders an
  inline production form before the batch inventory. The global `.form-panel` cap in
  `src/main.scss` limits it to 760px.
- The Ammunition loader already loads recipes and batches, so it contains all read data
  required by both Ammunition and Recipes panels.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` renders New Recipe
  as an inline disclosure. Its recipe detail/edit experience already uses a modal.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` owns
  shooting draft state and an inline create form. Its `repeatSession` copies all draft
  values, creates a fresh idempotency key, shows a notice, and scrolls to the form.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` owns
  maintenance draft/idempotency state and an inline create form.
- Shooting Sessions loads firearms, recipes, and recent shooting sessions. Maintenance
  loads firearms, component definitions, and physical inventory. A naïve composition
  of both current loaders would request firearms twice.
- Existing route tests cover all four loaders/actions, but no affected component test
  covers tabs, create modals, or Repeat-as-draft modal behavior.
- No ServerAPI, DTO, model, persistence, authentication, cache, or configuration gap
  blocks the requested composition. Existing Workshop endpoints expose the required
  reads and mutations.
- No app-local `PRODUCT.md`, `DESIGN.md`, or surface brief exists. Current source,
  `src/main.scss`, `docs/architecture/shooter-workshop.md`, and accepted handoffs are
  the visual/product authority.

## Proposed Design

### Shared interaction primitives

Add two controlled, neutral components to `@morwalpiz/layout`:

1. **`TabbedWorkspace`**
   - Receives a stable accessible label, selected key, ordered tab descriptors, panel
     content, and change callback.
   - Owns tablist/tab/tabpanel relationships, unique IDs, roving `tabIndex`, Arrow
     Left/Right wrapping, Home/End, focus movement, and disabled-tab behavior if needed.
   - Does not read or write URLs, translate labels, fetch data, or retain feature state.
   - Supports multiple instances on one document without ID collisions.

2. **`Dialog`**
   - Receives open state, accessible title, optional description, body, close callback,
     initial-focus target/ref, and dismissal options.
   - Owns modal semantics, unique title/description IDs, initial focus, Tab/Shift+Tab
     containment, focus restoration, Escape/backdrop dismissal, background
     `inert`/`aria-hidden` handling, and constrained viewport scrolling.
   - Uses a caller-supplied accessible close label; it does not render an unlabeled
     multiplication glyph as the only close name.
   - Avoids route, fetcher, form, validation, and pending-state assumptions.
   - Restricts `document`/DOM access to effects so SSR consumers can import it safely.

Export both components and their public prop types from
`frontend/fe-packages/layout/src/index.ts`.

Do not export a symbol named `Modal`, because BackOffice already imports React
Bootstrap `Modal` extensively. Do not promote the entire Components page or its
`ComponentSections` function.

Use package-namespaced structural classes and caller class-name hooks. Keep Workshop
visual tokens and exact active/toolbar/form styling in the Workshop application.
Do not require Shooter Workshop to import the package's complete stylesheet, because
that stylesheet includes visual defaults that differ from the Workshop world. Any
essential shared CSS must be isolated behind a new opt-in style subpath; otherwise
keep the primitives behavior-first and style them through consumer-supplied classes.

The first implementation adopts both primitives in Components and the two new merged
workspaces. This proves reuse across multiple route families in Workshop. The public
API remains suitable for the other four package consumers without requiring them to
migrate existing React Bootstrap or product-specific interfaces.

### Ammunition workspace

Make `/ammunition` the canonical route and page title. Its selected tab is URL-owned:

- absent/unknown `tab` value: Ammunition;
- `tab=recipes`: Recipes.

Tab changes update only `tab` while preserving unrelated search parameters. Browser
Back/Forward and refresh restore the selected panel.

The canonical loader reuses the Ammunition read contract: recipes and batches are
loaded once. Separate presentation components remain:

- Ammunition receives recipes, batches, and its production action result.
- Recipes receives recipes and its recipe action result.

The canonical page owns one `PageTitle` and the shared `TabbedWorkspace`. The panel
components no longer render duplicate page titles.

Preserve mutation ownership without a broad combined dispatcher:

- production forms continue posting to the Ammunition action;
- recipe forms continue posting to the Recipes action;
- the legacy `/recipes` route retains its action but redirects GET navigation to
  `/ammunition?tab=recipes`;
- successful fetcher actions revalidate the canonical Ammunition loader.

Namespaced action intents are preferred if the implementation consolidates actions,
but consolidation is not required for this feature. Unexpected failures must continue
to reach the route error boundary rather than being converted into success-shaped
results.

#### Ammunition production form

Keep the production form inline, but remove the route-specific width constraint. Do
not change global `.form-panel`, because other forms rely on its current measure.

At desktop width, the form spans the full panel and uses two columns:

- first column: Recipe, Quantity, Date;
- second column: Notes only.

The Notes textarea should use the available column height without forcing an
artificial fixed height. At the established narrow breakpoint, columns stack in the
same logical order. Validation and submit controls remain associated with the whole
form, not duplicated per column.

#### Recipes panel

Place a right-aligned `+ New recipe` trigger in the Recipes panel toolbar. Opening it
uses shared `Dialog` and the existing reusable recipe form in create mode.

Keep recipe details/editing as a separate dialog flow, migrated to the shared primitive
without changing recipe production links, update/version behavior, or QR workflow.
Create failure keeps values and the dialog open. Create success closes/reset the
create dialog after revalidation. The right-alignment requirement applies to the New
Recipe trigger and follows the established panel-header action placement.

### Shooting and maintenance workspace

Make `/sessions/shooting` canonical with URL-owned selection:

- absent/unknown `tab`: Shooting sessions;
- `tab=maintenance`: Maintenance.

Add a composite loader that loads each unique resource once:

- firearms;
- recipes;
- recent shooting sessions;
- component definitions;
- physical component inventory.

Return one typed route-data object and pass the relevant subsets into separate Shooting
Sessions and Maintenance presentation components. Do not call both current loaders
unchanged because that duplicates the firearms request.

Preserve separate mutation ownership:

- shooting record/correction forms use the Shooting Sessions action;
- maintenance forms use the Maintenance action;
- `/maintenance` keeps its action but redirects GET navigation to
  `/sessions/shooting?tab=maintenance`.

The canonical page owns one `PageTitle`, one `TabbedWorkspace`, and the selected-tab
URL synchronization.

#### Shooting Sessions panel

Move only the new shooting-session form into shared `Dialog`. Keep history and
correction behavior in the panel.

The primary Add operation opens an empty/fresh shooting draft. Repeat as draft:

1. copies firearm, notes, untracked rounds, and recipe quantities using existing
   behavior;
2. generates a fresh idempotency key;
3. selects the Shooting sessions tab;
4. opens the shooting dialog;
5. places initial focus at the first field needing review;
6. preserves the current “repeated session loaded as draft” notice.

Remove the obsolete `scrollIntoView` behavior. A failed create remains open with the
same idempotency key and values. Success closes, resets, and revalidates. Shooting
correction remains independent from the create dialog.

#### Maintenance panel

Move the maintenance create form into shared `Dialog`. Keep maintenance templates,
target rules, and current draft/idempotency behavior.

The Add operation opens a fresh draft. Failed or uncertain submission keeps the modal,
draft, and idempotency key. Success closes, resets, and revalidates. The panel remains
responsible for domain-specific target selection, date, event type, description,
notes, and template application.

### Navigation and compatibility

Update AppShell navigation to show:

- Ammunition only, not a separate Recipes item;
- Shooting sessions only, not a separate Maintenance item.

Update internal links:

- Dashboard and onboarding Recipes links target `/ammunition?tab=recipes`.
- Maintenance links target `/sessions/shooting?tab=maintenance`.
- “Back to recipes” from Recipe Production targets the canonical Recipes tab.
- Existing firearm-specific shooting links preserve `firearmId` while defaulting to
  Shooting sessions.

Legacy routes use replace redirects to avoid duplicate history entries. Preserve
`/production` and `/recipes/:recipeId/produce`. Redirects should retain unrelated
query parameters and fragments when practical, but must replace any legacy tab value
with the canonical selected tab.

### Impeccable workflow

This is an established **Operate** refinement. Use `shape` to confirm toolbar, tab,
modal, form, and panel hierarchy without changing the visual world. Apply:

- `harden` for invalid tab values, failed requests, pending states, long content,
  empty panels, and repeat-draft recovery;
- `adapt` for desktop two-column production layout, narrow stacking, long Italian tab
  labels, and viewport-constrained dialogs;
- `audit` for tabs, focus containment/restoration, background isolation, labels,
  status announcements, and keyboard-only operation.

Minimum visual evidence covers both workspaces at desktop and mobile widths, every
tab, all three create dialogs, recipe detail/edit dialog, Ammunition production form,
Repeat as draft, pending/error states, empty data, and long translated content.

## Impacted Projects

- **frontend/fe-packages/layout** — new generic controlled tabs and dialog exports,
  types, optional isolated styles, and accessibility behavior.
- **frontend/shooter-workshop.client** — canonical route composition, separate panel
  components, composite loader, modal forms, navigation/deep links, responsive styles,
  translations, and tests.
- **frontend/back-office-spa** — verify only; additive shared exports must not collide
  with React Bootstrap Tabs/Modal imports.
- **frontend/morwalpizvideo.client** — verify only; SSR-safe additive exports.
- **frontend/morwalpiz-shop.client** — verify only; it imports the shared complete
  stylesheet and is the consumer most exposed to accidental CSS changes.
- **frontend/shooting-ita-frontend** — verify only; additive package exports and shared
  builds must preserve existing tests.
- **MorWalPizVideo.ServerAPI, Domain, Contracts, Models** — not impacted; current APIs
  and contracts are sufficient.
- **MorWalPizVideo.AppHost and deployment topology** — verify only; no route host,
  configuration, secret, or service relationship changes.

## Impacted Files

### Modify

- `frontend/fe-packages/layout/src/index.ts` — export `TabbedWorkspace`, `Dialog`, and
  public prop types.
- `frontend/fe-packages/layout/package.json` — modify only if an isolated opt-in style
  subpath is added.
- `frontend/fe-packages/layout/src/styles/index.scss` — avoid modifying unless styles
  can remain fully namespaced and backward compatible; prefer an isolated style entry.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — canonical workspaces,
  legacy redirects/actions, composite loader/action wiring, and preserved production
  route.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — canonical and legacy
  route assertions.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` — collapse navigation
  entries.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` — canonical
  page shell or Ammunition panel, full-width two-column production form.
- `frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts` — retain or expose
  the combined recipes/batches read contract.
- `frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts` — merged loader
  and production behavior.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — panel mode,
  modal create, shared dialog migration, canonical back links.
- `frontend/shooter-workshop.client/src/routes/Recipes/route.test.ts` — modal-facing
  action behavior and compatibility.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` —
  canonical page shell or panel, create dialog, Repeat-as-draft opening.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/action.ts` — preserve
  record/correct ownership; change only if canonical intent namespacing is needed.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/route.test.ts` —
  composite behavior and repeat draft.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` — panel mode
  and modal create.
- `frontend/shooter-workshop.client/src/routes/Maintenance/action.ts` — preserve
  maintenance ownership; change only if canonical intent namespacing is needed.
- `frontend/shooter-workshop.client/src/routes/Maintenance/route.test.ts` — retained
  idempotency and modal-facing errors.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` — canonical
  Recipes/Maintenance links.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx` —
  canonical tab links.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` —
  canonical Maintenance link while preserving firearm-specific shooting links.
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx` —
  canonical Back to Recipes link.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` — replace
  local tab/modal mechanics with shared primitives without changing filtering/forms.
- `frontend/shooter-workshop.client/src/main.scss` — workspace theming, full-width
  production layout, left toolbar alignment, dialog sizing, and responsive behavior.
- `frontend/shooter-workshop.client/src/i18n.tsx` — page/tab/dialog labels, statuses,
  validation/recovery copy, and exact existing subtitle gaps.
- `docs/architecture/shooter-workshop.md` — canonical navigation, route ownership,
  compatibility redirects, and shared-interaction boundary.
- `docs/architecture/frontend.md` — record the two additive layout primitives and
  clarify consumer-owned styling/routing.
- `docs/feature-handoffs/shooter-workshop-consolidated-workspaces-handoff.md` — actual
  implementation, validation, and residual risks.

### Add

- `frontend/fe-packages/layout/src/components/TabbedWorkspace.tsx` — controlled
  accessible tab interaction.
- `frontend/fe-packages/layout/src/components/Dialog.tsx` — controlled accessible
  modal interaction.
- An isolated namespaced Sass entry under
  `frontend/fe-packages/layout/src/styles/` only if essential primitive styles cannot
  remain consumer-owned.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/loader.ts` may be
  replaced or supplemented by a canonical composite loader colocated with the
  workspace. Use the established route-folder naming rather than inventing a generic
  global loader.
- Focused component tests for shared primitives through the Workshop consumer test
  harness, unless package-local test infrastructure is explicitly added.

### Generated/Migration

None. No database, API, generated client, lockfile, or persisted-data migration is
required. Do not check in regenerated `dist` artifacts unless current repository
policy explicitly requires them for the shared package release.

### Documentation

- This handoff is the implementation source of truth.
- Update the Shooter Workshop and frontend architecture guides after implementation.

### Verify Only

- `frontend/fe-packages/layout/README.md` — update only if its public component/style
  import guidance becomes incomplete.
- All five `@morwalpiz/layout` consumer manifests and builds.
- `frontend/fe-packages/services/src/workshopService.ts` and
  `frontend/fe-packages/models/src/workshop.ts` — no contract change expected.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — no endpoint
  change expected.
- `frontend/shooter-workshop.client/Dockerfile`, deployment workflow, nginx template,
  and `MorWalPizVideo.AppHost/Program.cs` — no runtime wiring change.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Not applicable. Existing authenticated Workshop endpoints support all reads and mutations. |
| Frontend/UI | Two canonical tabbed workspaces, three create modals, shared dialog/tab primitives, full-width Ammunition form, collapsed navigation. |
| Database/Data | Not applicable. No persisted shape or data transition. |
| Contracts/Models | Not applicable. Shared primitives use generic React content/keys, not Workshop contracts. |
| Authentication/Authorization | Existing protected shell, account credentials, CSRF, and route actions remain authoritative. |
| Validation | Existing actions remain server-authoritative; modals retain values and errors on failure. Invalid tab values normalize to the default panel. |
| Cache | Not applicable. React Router revalidation refreshes route data; no output-cache tags are involved. |
| Configuration/Secrets | Not applicable. No settings or credentials. |
| Logging/Telemetry | No new backend telemetry. Frontend errors remain visible through established route/action surfaces. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Shared layout builds before every consumer. Backend deployment is unnecessary. |
| Backward Compatibility | `/recipes`, `/maintenance`, `/production`, and recipe-production deep links remain valid through redirects or unchanged routes. |

## Reuse Opportunities

- Reuse the Components tab behavior as the interaction specification, not as a
  Workshop-aware shared component.
- Reuse FeedbackProvider's background isolation and tested focus behavior in `Dialog`.
- Reuse the existing recipe form for create and edit modes.
- Reuse the Ammunition loader's recipes/batches result for both panels.
- Reuse route actions and shared Workshop services rather than adding component-owned
  API calls.
- Reuse existing `.form-grid` responsive behavior with route-specific production
  layout classes.
- Reuse History/Components query-parameter patterns for tab URL synchronization.
- Reuse existing fetcher success/failure transitions and React Router revalidation.

## Risks

- **High — shared dialog accessibility regression:** extracting duplicated logic can
  weaken focus/background behavior across multiple routes. Mitigate with the stronger
  FeedbackProvider contract and focused keyboard/focus tests. Signal: focus enters,
  stays inside, restores, and background is inert for every dialog.
- **High — shared package CSS regression:** the shop imports the complete shared
  stylesheet while Workshop does not. Mitigate with namespaced, opt-in or
  consumer-owned styles. Signal: all five consumer builds pass and shop visual
  selectors remain unchanged.
- **Medium — URL compatibility loss:** deleting standalone routes would break
  bookmarks and internal links. Mitigate with replace redirects and canonical link
  updates. Signal: route tests prove old and direct URLs select the correct tab.
- **Medium — loader failure coupling:** eager combined reads can hide both panels when
  one dependency fails. Mitigate by retaining current route error behavior initially
  and keeping each panel's data contract explicit; consider deferred isolation only
  with evidence. Signal: failure tests identify the failed dependency clearly.
- **Medium — duplicate resource requests:** composing current shooting and maintenance
  loaders unchanged fetches firearms twice. Mitigate with one canonical composite
  loader. Signal: service mocks assert one firearm request.
- **Medium — idempotency loss in modals:** closing/resetting on uncertain failure can
  duplicate writes. Mitigate by reset only on confirmed success. Signal: retry tests
  preserve the same key; Repeat as draft generates a new one.
- **Medium — cross-action revalidation gaps:** posting to preserved route actions may
  fail to refresh the canonical workspace. Mitigate with fetcher integration tests
  proving canonical loader revalidation. Signal: created recipe/maintenance/session is
  visible without manual reload.
- **Low — long translated tab labels:** equal-width controls may wrap poorly on narrow
  screens. Mitigate with responsive evidence and flexible tab sizing. Signal: Italian
  labels remain readable and operable without horizontal clipping.
- **Low — external services:** none are introduced.

## Technical Debt

- Components, Recipes, and FeedbackProvider duplicate modal mechanics. This extraction
  directly resolves the duplication and is in scope.
- The Components tab implementation lacks direct tests. Migrating it to the shared
  primitive adds consumer-level regression coverage.
- `@morwalpiz/layout` has no test command. Do not add a broad package testing stack
  solely for this feature; test behavior through Shooter Workshop and keep package
  compilation as the shared-package gate. A package-local harness is deferred unless
  another consumer adopts the primitives immediately.
- Existing route barrels export loader/action inconsistently. Normalize only touched
  route folders when required by composition; do not perform a repository-wide barrel
  refactor.
- Some affected strings bypass i18n. Correct directly touched page/tab/modal/status
  copy now; a full localization audit is deferred.
- AppHost/Vite port mismatch and deployment workflow/template discrepancies are
  unrelated and must not enter this implementation.

## Implementation Roadmap

1. **Lock compatibility and interaction tests.** Add failing route/component tests for
   canonical/default tabs, legacy URLs, keyboard tabs, dialog focus, failed drafts,
   navigation collapse, and Repeat as draft. Gate: tests discriminate current separate
   routes/inline forms from the requested outcome.
2. **Extract shared primitives.** Add controlled `TabbedWorkspace` and `Dialog` with
   neutral props, unique IDs, SSR-safe effects, accessibility mechanics, and exports.
   Gate: layout package builds and Workshop consumer tests prove the public behavior.
3. **Migrate Components to the primitives.** Remove route-local tab/modal mechanics
   while preserving current inventory/catalogue behavior. Gate: existing component
   filters, forms, query handling, and modal success/failure remain unchanged.
4. **Compose Ammunition and Recipes.** Introduce one canonical page shell and URL-owned
   selection, keep separate panel components/actions, retain legacy redirect/action
   compatibility, and preserve recipe production routes. Gate: direct/legacy URLs,
   Back/Forward, create/update, and revalidation tests pass.
5. **Refine Ammunition form and Recipe modal.** Add the full-width two-column production
   layout, narrow stacking, right-aligned New Recipe trigger, and create dialog. Gate:
   desktop/mobile visual evidence and failed/successful form tests pass.
6. **Compose Shooting and Maintenance reads.** Add one typed loader that requests every
   unique dataset once and passes subsets to separate panels. Gate: one firearm call,
   correct panel data, and clear dependency failure behavior.
7. **Move operation forms into dialogs.** Add shooting and maintenance create dialogs;
   preserve corrections/templates/idempotency and implement Repeat-as-draft
   population/opening. Gate: failure retains draft/key, success resets, repeat uses a
   fresh key and opens the correct dialog.
8. **Update navigation and deep links.** Collapse AppShell entries and update Dashboard,
   onboarding, firearm detail, and recipe production links. Gate: no internal source
   link depends on obsolete standalone presentation routes.
9. **Complete i18n and styling.** Add exact English/Italian copy, Workshop theme hooks,
   responsive rules, and long-content states. Gate: no touched user-visible string is
   raw in Italian mode.
10. **Validate every consumer and document reality.** Build shared packages, test/build
    Workshop, type/build every direct layout consumer, run scoped diff hygiene, and
    update architecture/handoff with actual evidence. Stop after this workspace scope.

An independent post-validation review is warranted because this changes a shared
frontend package, route compatibility, modal accessibility, action revalidation, and
idempotent draft behavior. The user may select `MorWalPiz Code Reviewer` after
implementation.

## Testing Strategy

- **Shared primitive behavior — Workshop Vitest/Testing Library:** controlled
  selection, roving focus, Arrow/Home/End, unique relationships, dialog naming,
  initial/return focus, containment, Escape/backdrop policy, background isolation,
  scrollable content, and multiple instances.
- **Route configuration — Workshop:** canonical route loader/action ownership, legacy
  replace redirects, retained `/production`, retained recipe-production route, and
  collapsed route/navigation expectations.
- **Ammunition/Recipes loaders/actions:** retain current valid/invalid/service-failure
  cases; add merged loader data and canonical revalidation.
- **Ammunition/Recipes components:** default/tab URL selection, New Recipe modal,
  create failure retention, success close, recipe detail/edit dialog, production links,
  and two-column semantic field order.
- **Shooting/Maintenance loader:** one firearm request, all unique datasets, rejection
  propagation, and typed subsets.
- **Shooting/Maintenance actions:** existing record/correct/validation cases,
  maintenance idempotency retention, modal failure/success, and no action-intent
  collision.
- **Repeat as draft:** every draft field copied, fresh idempotency key, Shooting tab
  selected, modal opened, and correction/original identity excluded.
- **Navigation/i18n:** only canonical nav items, canonical internal links, and Italian
  translations for all new/touched copy.
- **Responsive/accessibility:** desktop/mobile Ammunition layout, long Italian labels,
  dialog viewport scrolling, keyboard-only tabs/forms, labelled errors, pending state,
  and reduced-motion compatibility.
- **Cross-consumer qualification:** layout build plus builds/type-checks for Workshop,
  BackOffice SPA, public client, shop, and Shooting ITA according to their existing
  scripts.
- **End-to-end smoke:** old and new URLs, create production, create/edit recipe,
  recipe-production return, record/correct shooting, repeat draft, record maintenance,
  browser history, refresh, and authentication return URL.

## Migration And Rollback

No database, API, auth, cache, or configuration migration is required.

Deliver the additive `@morwalpiz/layout` exports first in the same source release, then
the Workshop consumer changes. Existing package consumers are unaffected unless they
import the new exports. Avoid modifying the existing default style bundle in a way that
changes current consumers.

The route compatibility window is indefinite: keep `/recipes`, `/maintenance`, and
`/production` redirects while external bookmarks may exist. Recipe production remains
unchanged.

Monitor client-side route errors, action failures, revalidation, and navigation to
legacy URLs. There is no backend rollout dependency.

Rollback the Workshop route/navigation changes first while retaining the additive
shared components; unused exports are backward compatible. If the shared primitives
have a defect, restore the previous app-local Components/Recipes mechanics while
keeping route redirects only if their canonical targets still exist. No user data
needs reversal, and successful operations performed through the new modals remain
valid domain records.

## Open Questions

None blocking. The implementation must not invent different answers:

- immediate shared-package extraction is approved because the user explicitly requires
  future cross-application reuse;
- query key is `tab` with values `recipes` and `maintenance`;
- canonical routes load both panels eagerly;
- old standalone URLs redirect with replace semantics;
- shared primitives are controlled and domain-neutral;
- feature styling remains in Shooter Workshop unless essential shared CSS is isolated
  behind an opt-in subpath;
- successful creates close their modal; failures retain modal, values, and idempotency
  identity.

## Implementation Status

The consolidated workspaces are implemented. The changed source files include:

- `frontend/fe-packages/layout/src/components/TabbedWorkspace.tsx` — controlled,
  roving-focus tabs with stable `useId` relationships, keyboard navigation, disabled
  tab handling, and caller-supplied classes.
- `frontend/fe-packages/layout/src/components/Dialog.tsx` — SSR-safe portal dialog
  with generated title/description IDs, initial-focus support, focus trapping and
  restoration, Escape/backdrop options, background `inert`/`aria-hidden` isolation,
  and constrained backdrop scrolling.
- `frontend/fe-packages/layout/src/index.ts` — additive component and public prop
  type exports.

- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` — canonical
  Ammunition/Recipes tabs and full-width two-column production form.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — right-aligned
  New Recipe action and shared Dialog integration for create and detail/edit flows.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` and
  `Maintenance/component.tsx` — canonical Shooting/Maintenance tabs, modal creation
  flows, and Repeat-as-draft modal opening.
- `frontend/shooter-workshop.client/src/routes/Ammunition/action.ts` and
  `ShootingSessions/action.ts` — route-local dispatch for the two tabbed workspaces.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/loader.ts` — combined
  eager read with a single firearms request.
- `frontend/shooter-workshop.client/src/routes/config.tsx`,
  `layouts/AppShell.tsx`, and internal links — canonical navigation plus compatible
  legacy redirects.
- `frontend/shooter-workshop.client/src/main.scss` — consumer styling for shared
  primitives and responsive production layout.

Validation passed:

- `cd frontend/fe-packages/layout && npm run build`
- `cd frontend/shooter-workshop.client && npm run build`
- `cd frontend/shooter-workshop.client && npm test` (15 files, 41 tests)

The production build retains the existing Vite chunk-size warning. Browser screenshot
and end-to-end validation were unavailable. Generated `dist` output was restored and
not included as a source change; existing unrelated Workshop worktree changes were
preserved.
