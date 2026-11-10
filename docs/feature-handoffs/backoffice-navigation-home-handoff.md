# BackOffice Navigation And Home Refinement

## Outcome

**Status: Implemented and validated.**

Make the administrative sidebar manageable as modules grow, and make the home page useful for the user's most frequent tasks: managing videos and writing articles. The user confirmed that quick actions and existing analytics should remain together on one page. This is a refinement, not a visual redesign.

Preserve all existing destinations, authorization rules, channel selection, analytics meanings, and mobile navigation. Do not introduce dashboard tabs, new backend endpoints, personalized navigation storage, or a new design system.

User confirmed that video management and article writing are the most frequent tasks and that quick actions should remain on the same page as analytics. The proposed seven-section taxonomy below was implemented without changing the existing URLs or permissions.

## Current Evidence

Paths below are relative to the repository root; links resolve from this handoff.

| Evidence | Verified current behavior |
| --- | --- |
| [Architecture guide](../architecture/README.md), [frontend architecture](../architecture/frontend.md), [dashboard feature document](../architecture/admin-dashboard.md) | BackOffice owns administrative workflows; app-specific presentation stays local. Shop feature development is on hold. |
| [Navigation catalog](../../frontend/back-office-spa/src/layouts/adminMenu.ts), `adminMenuGroups` | One catalog contains 32 entries including Dashboard. Marketing has 15 entries; Content has eight. Groups carry labels, but no stable identifiers or expansion behavior. |
| [Sidebar](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx), `AdminSidebar` | Filters items by server-effective permissions and renders every remaining group expanded. Channel selection and configured external reports share the same navigation surface. One responsive React-Bootstrap Offcanvas serves mobile and desktop. |
| [Shell](../../frontend/back-office-spa/src/layouts/PrimaryLayout.tsx), `PrimaryLayout` | Owns mobile visibility, ChannelProvider, header, progress indicator, breadcrumbs, outlet, install invitation, and footer. |
| [Styles](../../frontend/back-office-spa/src/index.css) | Dark 248px sidebar, pale page background, white panels, teal-blue accents, and a 992px desktop breakpoint define the incumbent shell. Dashboard panels currently impose a 300px minimum height. |
| [Home](../../frontend/back-office-spa/src/routes/Home.tsx), `Home` | Requests summary and 21-day publications through shared services in a mount-only effect. Loading and errors replace the entire page. There are no quick actions. Chart clicks, table row clicks, and the video button navigate without checking video view permission locally. |
| [Route export](../../frontend/back-office-spa/src/router/routes/index.ts), [active lazy route catalog](../../frontend/back-office-spa/src/router/routes/lazy-index.ts) | The exported protected routes come from the lazy catalog. Existing destinations include `/videos`, `/videos/import`, `/blogposts`, and `/blogposts/create`. The current editorial route is `/editorialproductioncards`, not the dashed path in the feature document. |
| [Permission helpers](../../frontend/back-office-spa/src/authorization/permissions.ts), `hasPermission`, `getRoutePermissions` | Global administrator override is centralized. Video listing uses video view/manage; article creation uses pages create/manage. Article viewing and creation are distinct capabilities. |
| [Article form](../../frontend/back-office-spa/src/routes/blog/form.tsx), [video index](../../frontend/back-office-spa/src/routes/videos/index/Component.tsx) | Article creation already owns draft editing, explicit saving, and publishing checks. Video listing already owns import/translation controls. Home need only link to these workflows. |
| [Channel context](../../frontend/back-office-spa/src/contexts/ChannelContext.tsx), [shell store](../../frontend/back-office-spa/src/state/appStore.ts), [authentication loader](../../frontend/back-office-spa/src/router.ts) | Changing channels updates selected-channel state and shared transport context, then revalidates routes. Home's mount-only effect does not itself observe that change. |
| [Dashboard controller](../../MorWalPizVideo.BackOffice/Controllers/DashboardController.cs), [dashboard service](../../MorWalPizVideo.BackOffice/Services/DashboardService.cs) | Both dashboard endpoints require BackOffice access and channel scope. Links, publications, and insights use channel context; user/login metrics and form totals are not all channel-local. Metrics must not be relabeled as uniformly channel-specific. |
| [Sidebar tests](../../frontend/back-office-spa/src/layouts/AdminSidebar.test.tsx), [catalog tests](../../frontend/back-office-spa/src/layouts/adminMenu.test.ts), [home tests](../../frontend/back-office-spa/src/routes/Home.test.tsx), [shell tests](../../frontend/back-office-spa/src/layouts/PrimaryLayout.test.tsx) | Existing component tests cover basic permission filtering, shell composition, navigation dismissal, and dashboard data. They do not prove collapsed groups, shortcut permission combinations, or channel-change request races. |
| [Package manifest](../../frontend/back-office-spa/package.json), [Vitest configuration](../../frontend/back-office-spa/vitest.config.ts) | React 19, React Router 7, React-Bootstrap, Lucide, Recharts, Zustand, Vitest/Testing Library, and Playwright are already declared. No new dependency is required. |

No BackOffice-specific PRODUCT.md or DESIGN.md was found in the frontend tree. The public and FreezerAnalysis design documents are not authority for this admin surface. Current BackOffice styles and components were used as the visual baseline. A headless browser review was performed after implementation with mocked APIs.

## Proposed Design

### Navigation structure

Keep Dashboard as a direct, always-visible link outside the expandable sections. Keep channel selection above the section list and configured external reports after it. Each existing module appears exactly once in the catalog.

| Proposed section | Existing destinations, in display order |
| --- | --- |
| Publishing | Videos; Blog; Editorial production; Calendar; Compilations; Script Studio |
| Media library | Categories; Images; Galleries |
| Website | Pages; Navigation; QuickLinks |
| Audience | Forms; Surveys; Push notifications; Blog interactions |
| Growth & insights | Insights; Short links; Query links; Partners; Campaigns |
| Catalog | Product categories; Products; Sponsors |
| Administration | Channels; Users & access; Public accounts; API keys; Configuration; Diagnostics; AI workflows |

These are presentation categories only. Moving Sponsors or Channels does not change domain ownership, routes, channel scope, or permissions. Catalog destinations remain reachable as today, but frozen shop workflows receive no new functionality.

Give groups stable identifiers independent of display labels. Keep the existing item paths, labels, icons, and permission requirements. Do not add a recursive tree: one expandable level is sufficient, with at most seven items in a group.

Interaction contract:

1. Filter inaccessible items first; omit empty groups and their controls.
2. Open Publishing initially on Dashboard when it has visible items; otherwise start with all groups collapsed.
3. Use a single-open accordion: expanding a group closes the previous one; collapsing all groups is allowed.
4. On initial deep link and pathname changes, open the group containing the matching visible module. Match whole path segments, choosing the most specific catalog path; Dashboard matches only `/`.
5. A user may collapse the current group. Its header still indicates that it contains the current page. A later navigation to a module reopens that module's group.
6. Group controls only expand/collapse; they do not navigate or dismiss mobile navigation. Module links retain current mobile dismissal behavior.
7. Expansion is local component state only. Refresh recomputes from the current route; no localStorage, backend preferences, or new global store fields.
8. Use native buttons with expanded/control relationships, visible focus, and hidden collapsed links outside the tab order. Keep navigation semantics rather than application-menu roles.
9. Preserve one responsive Offcanvas/navigation tree. Ensure every item remains reachable at short viewport heights; group controls and channel selection must not be obscured by sticky or fixed additions.

### Home composition

Keep one page in this order:

1. Dashboard heading and short operational description.
2. Compact quick-action area with three independently permission-filtered links:
   - **Gestisci video** to `/videos`, matching video-list route permissions.
   - **Scrivi articolo** to `/blogposts/create`, matching article-create route permissions; this is the primary-styled action when available.
   - **Tutti gli articoli** to `/blogposts`, matching article-list route permissions.
3. Existing KPI values and generation timestamp.
4. Existing 21-day publication chart and operational values.
5. Existing recent publications, with native video links when authorized and an explicit empty state.

Use the existing permission helper with destination route permissions, not copied permission hierarchy logic. A user with create permission but no article-list permission can still see the create shortcut. Hide the quick-action block when none of its actions is available; do not promote unrelated administration tasks into its place.

Quick actions navigate only. They never create, import, save, generate, or publish anything automatically. Keep import and translation in the existing video workflow; do not invent `/videos/create`.

Keep the heading and quick actions rendered while analytics load or fail. Scope the spinner/error message to analytics, with an explicit retry button that retries the two existing reads. Retain the current combined analytics load rather than introducing independent per-panel fetching or a data framework.

Because channel selection is part of the refined shell, make Home observe selected-channel changes. Fetch analytics for the current valid channel, clear the previous channel's values immediately, and ignore results from superseded requests or unmounted components. Retry uses the current channel. When there is no accessible selection, keep authorized shortcuts visible but show an explicit no-channel analytics state and issue no dashboard requests.

Preserve summary values, seven-day click semantics, the 21-day window, UTC server grouping, and provider-independent data sources. Do not add article counts or pretend all summary values are channel-specific.

For publication navigation, apply the same video-list visibility check used by the video shortcut. Keep analytics visible under the dashboard's existing access contract, but render non-navigable publication text for users without video-view access. For authorized users, use native links in the recent-publication table rather than click-only rows; retain the chart's current first-video drill-through as a convenience, with the table providing a keyboard-accessible route to every displayed recent video.

### Visual and Impeccable direction

- **Surface:** BackOffice protected shell and home page.
- **Mode:** Operate; task access and scanability outrank visual expression.
- **Workflow:** Continue the installed Impeccable `shape` direction for this refinement, then use a bounded `adapt` pass for responsive disclosure behavior and `harden` checks for permission, empty, loading, and error states during implementation.
- **Context:** No local product/design artifact is required just to refine these components. The implementer should load the skill's target context and craft-floor guidance before UI edits; this architect session does not execute its scripts or UI workflow.
- **Preserve:** Existing colors, font stack, Lucide icons, Bootstrap controls, shell width/breakpoint, install invitation, breadcrumbs, and account controls.
- **Improve:** Section-label contrast, control hierarchy, active/expanded distinction, spacing, focus visibility, link wrapping, and compact action sizing. Do not reuse the analytics panel's 300px minimum height for quick actions.
- **Proof:** Capture desktop and mobile before/after views. Inspect 375px, 768px, 991px, 992px, and 1440px widths, including a short-height viewport. Batch inspection, apply material fixes together, and perform at most one confirmation pass.

## Impacted Projects

- **BackOffice SPA:** Only application requiring implementation changes; owns navigation catalog, sidebar disclosure, dashboard shortcuts, analytics UI state, and component tests.
- **BackOffice API and backend tests:** Verify-only evidence for unchanged dashboard contracts and authorization. No controller, service, repository, or test implementation change is planned.
- **Shared frontend models/services/layout:** Verify-only consumers of existing behavior. No public API, transport, or shared presentation change; no cross-application rollout.
- **Other APIs, public clients, standalone tools, Windows applications, AppHost, database, and deployment configuration:** Not affected. Delivery uses the existing SPA artifact.

## Impacted Files

### Modify

- [Navigation catalog](../../frontend/back-office-spa/src/layouts/adminMenu.ts): reorganize all existing items, add stable group identifiers, and keep Dashboard directly addressable.
- [Sidebar](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx): permission-filtered disclosure, route-aware expansion, accessible controls, and preserved channel/external-link behavior.
- [Home](../../frontend/back-office-spa/src/routes/Home.tsx): quick actions, analytics-scoped states/retry, selected-channel refresh, race protection, native publication links, and empty-state copy.
- [Application styles](../../frontend/back-office-spa/src/index.css): narrowly scoped disclosure/action/focus/overflow styling; avoid global control restyling.
- [Catalog tests](../../frontend/back-office-spa/src/layouts/adminMenu.test.ts): update section expectations and prove complete, nonduplicated destination coverage, retained permission metadata, and Images/Galleries adjacency.
- [Sidebar tests](../../frontend/back-office-spa/src/layouts/AdminSidebar.test.tsx): replace assumptions that all groups are expanded and cover the interaction contract.
- [Home tests](../../frontend/back-office-spa/src/routes/Home.test.tsx): permission combinations, navigation targets, analytics states, retry, and channel transitions.

### Add

No new production file is required. Keep small action/disclosure metadata in the owning existing modules. New component extraction is not necessary for this scope.

### Generated/Migration

None. Do not change generated outputs, dependency manifests, lockfiles, or data schemas.

### Documentation

- This handoff: preserve user review notes and append actual implementation decisions, changed files, validation results, and remaining gaps after implementation.
- [Dashboard feature document](../architecture/admin-dashboard.md): update sidebar taxonomy/disclosure and quick-action behavior; correct its editorial route to the source-verified path. Replace only validation claims affected by this work.
- No ADR is needed: ownership, persistence, authentication, runtime topology, and API contracts remain unchanged.

### Verify Only

- [PrimaryLayout](../../frontend/back-office-spa/src/layouts/PrimaryLayout.tsx), [shell tests](../../frontend/back-office-spa/src/layouts/PrimaryLayout.test.tsx), [Header](../../frontend/back-office-spa/src/layouts/Header.tsx): preserve shell composition and mobile opening/dismissal.
- [Active route catalog](../../frontend/back-office-spa/src/router/routes/lazy-index.ts), [route inventory tests](../../frontend/back-office-spa/src/router/routes/index.test.ts), [permissions](../../frontend/back-office-spa/src/authorization/permissions.ts): navigation/shortcut targets and authorization remain aligned.
- [Article form](../../frontend/back-office-spa/src/routes/blog/form.tsx), [article form tests](../../frontend/back-office-spa/src/routes/blog/form.test.tsx), [video index](../../frontend/back-office-spa/src/routes/videos/index/Component.tsx): shortcut destination workflows remain intact.
- [ChannelContext](../../frontend/back-office-spa/src/contexts/ChannelContext.tsx), [appStore](../../frontend/back-office-spa/src/state/appStore.ts), [router bootstrap](../../frontend/back-office-spa/src/router.ts): reuse existing channel/session context without changing it.
- [Dashboard controller](../../MorWalPizVideo.BackOffice/Controllers/DashboardController.cs), [service](../../MorWalPizVideo.BackOffice/Services/DashboardService.cs), [backend dashboard tests](../../MorWalPizVideo.BackOffice.Tests/Features/DashboardControllerTests.cs): unchanged read semantics and server-side authorization.
- [Package manifest](../../frontend/back-office-spa/package.json), [Vitest setup](../../frontend/back-office-spa/vitest.config.ts), [test helper](../../frontend/back-office-spa/src/test/test-utils.tsx): use existing tools. Deep-link tests can use a test-local memory router rather than broadening the shared helper unnecessarily.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | No changes; reuse both dashboard GET endpoints. |
| Frontend/UI | Seven collapsible groups, direct Dashboard link, three permission-filtered shortcuts, same-page analytics, explicit states. |
| Database/Data | No writes, collections, indexes, backfill, or personalized preferences. Preserve existing metric scope. |
| Contracts/Models | No wire changes or shared DTO additions. Existing local dashboard shapes remain sufficient. |
| Authentication/Authorization | Keep cookie/CSRF/session and server checks unchanged. Presentation visibility follows effective permissions and existing route helper. |
| Validation | UI state/route consistency and accessibility only; form/server validation untouched. |
| Cache | No new caching, tags, invalidation, or stored analytics. Clear stale view data on channel change. |
| Configuration/Secrets | None required; preserve existing configured external-report URLs and safe link attributes. |
| Logging/Telemetry | No new telemetry service. Show failures explicitly in analytics UI; do not convert failures into zero-valued success. |
| Background Jobs | Not applicable; shortcuts are navigation, not commands or scheduling. |
| Deployment/Operations | Existing SPA build/deployment only. No configuration or feature flag needed. |
| Backward Compatibility | All existing paths, route guards, workflow ownership, metric contracts, and module reachability preserved. |

## Reuse Opportunities

- Retain the single `adminMenuGroups` catalog and existing item metadata; avoid parallel desktop/mobile route lists.
- Reuse `hasPermission`, `getRoutePermissions`, and `useAppStore` effective permissions; do not introduce client-side permission expansion.
- Reuse React-Bootstrap disclosure/Offcanvas controls and React Router links/location; no menu library.
- Reuse the current channel provider and shared services transport; no direct fetch client or new channel header management.
- Keep existing KPI/chart components and article/video route modules.
- Extend Vitest/Testing Library route-aware tests. Reuse existing browser tooling if available rather than adding dependencies solely for screenshots.

## Risks

| Severity | Cause and affected surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| Medium | Reorganization accidentally drops a module or alters permissions | Inventory every current destination and retain its metadata; no route edits | All 32 current entries remain represented once, including direct Dashboard; restricted and administrator personas pass |
| Medium | Collapsing navigation conceals the active route or traps keyboard focus | Whole-segment route matching, visible active-group state, native disclosure controls, hidden collapsed content | Deep links and Back/Forward open the correct group; collapsed links are not focusable |
| Medium | Channel changes leave old analytics visible or a late request overwrites new data | Clear data on selection change; ignore stale completions; preserve existing transport context | Controlled A-to-B and rapid A-to-B-to-A request tests pass |
| Medium | New shortcuts imply unauthorized access or accidental article publication | Use destination permissions; links only; retain route/API enforcement | View-only, create-only, access-only, and manage-all cases show only allowed links; following create does not save |
| Low | Revised control styles reduce contrast or overflow on mobile | Preserve palette but verify text/control contrast, wrapping, focus, and 992px behavior | No horizontal shell overflow; all controls reachable at target widths and short height |
| Low | Dashboard failure blocks frequent work | Keep heading/actions independent of analytics load; retry explicitly | Both data rejection and loading tests retain allowed shortcuts |
| Low | Metric relabeling misrepresents global data as channel-local | Keep current labels/semantics and identify selected context without claiming universal scoping | Review copy against DashboardService source |
| Low | External report or rollout regression | Preserve link targets/attributes and current SPA deployment path | Configured links remain safe and reachable; previous SPA artifact is recoverable |

No new persistence/data-integrity or external-service integration risk is introduced. Existing editor concurrency, provider behavior, and server authorization remain outside the implementation boundary.

## Technical Debt

- **Address now, tightly coupled:** Home's mount-only analytics reads do not observe channel selection; explicit channel-aware UI state is necessary while refining this shell/home interaction.
- **Address now, tightly coupled:** Click-only recent-publication rows and unfiltered video affordances undermine the new dashboard action contract; replace with permission-aware links.
- **Address now, documentation only:** Dashboard feature documentation contains an outdated editorial URL and historical browser-tool availability. Update relevant claims using actual implementation evidence.
- **Defer:** The legacy route-definition block in the route export file is not the active catalog. Do not clean it up during this task.
- **Defer:** Mixed English navigation and Italian dashboard copy. Preserve the current language boundaries; do not introduce an i18n system or translate unrelated features.
- **Defer:** Globally scoped summary metrics, richer article/video analytics, customizable favorites, search/command palette, and permission-model cleanup. None blocks this refinement.

## Implementation Roadmap

1. **Establish baseline and visual context.** Owning area: BackOffice SPA and its existing targeted tests. Capture current shell/home views, load Impeccable context for this target, and check baseline navigation/home tests. Gate: document existing failures separately; confirm all current menu paths and destination permissions before edits.
2. **Reorganize the navigation catalog.** Depends on step 1. Owning files: navigation catalog and catalog tests. Add stable group identifiers, keep Dashboard direct, and apply the exact taxonomy above without modifying item authorization or URLs. Gate: 32 existing entries preserved once; no group exceeds seven items; Images precedes Galleries.
3. **Implement accessible disclosure.** Depends on step 2. Owning files: sidebar, scoped styles, sidebar tests. Implement one-open route-aware groups, preserve channel selection and external reports, and keep the existing responsive Offcanvas. Gate: deep-link activation, toggle/collapse, permission changes, single navigation tree, and dismissal tests pass.
4. **Make home action-first.** Depends on step 1; can be developed independently of steps 2-3. Owning files: Home, styles, Home tests. Add the three shortcuts, keep analytics below, scope load/error/retry to analytics, refresh by selected channel with stale-result protection, and use permission-aware native publication links. Gate: action targets, granular permissions, loading/error/empty states, retry, and channel-race tests pass; no editor/import/API behavior changes.
5. **Validate the integrated refinement.** Depends on steps 3-4. Run the combined catalog/sidebar/home/shell/route tests, relevant article-form regression tests, then the SPA's existing type-check/production build and appropriate lint/format checks. Respect shared-package dependency build order. Browser-check the target widths, keyboard paths, restricted persona, external reports, and high-permission longest menu. Gate: no new failures, overflow, hidden destinations, or broken focus/dismissal behavior.
6. **Record and hand back.** Depends on step 5. Update the dashboard feature document and this handoff with actual changes, validation outcomes, screenshots/evidence locations, baseline failures, and residual risks. Stop at this scope. An independent post-validation review is recommended because shared navigation affects every administrative workflow and permission persona; the user may select MorWalPiz Code Reviewer.

## Testing Strategy

- **Unit/catalog:** Extend catalog tests for the complete destination inventory, unique paths, stable group IDs, retained permission metadata, maximum section size, and Images/Galleries ordering. Route matching must distinguish exact `/` and module boundaries.
- **Component/sidebar:** Administrator, restricted reader, access-only user, no visible group, and live permission changes. Verify initial Dashboard state, direct nested URLs, article creation/edit routes, Back/Forward navigation, manual active-group collapse, one-open policy, and no focusable hidden links. Preserve channel selector, configured/no external reports, and link-close versus group-toggle behavior.
- **Component/home:** Test video-view only; pages-view only; pages-create without pages-view; global override; and access-only. Assert exact shortcut targets and absence of unauthorized controls. Prove loading and either analytics request failing do not remove actions, retry rereads the current channel, missing channel causes no data read, zero publications has an explicit state, UTC dates remain formatted for display, and timestamps/metrics remain unchanged.
- **Request lifecycle:** Controlled deferred responses for channel A then B, late A completion, rapid reselection, unmount, and rejected retry. Previous-channel figures must clear immediately and never overwrite current figures.
- **Workflow/contract regression:** Existing route inventory, shell, and article-form tests remain relevant. Verify that opening article creation creates no persisted draft and uses the existing explicit save/publish flow. Verify video management still exposes its existing import/translation controls under their original permissions. Both dashboard endpoint URLs and the `days=21` query remain unchanged.
- **Accessibility/security presentation:** Tab/Shift+Tab, Enter/Space disclosure, expanded/control attributes, active-page indication, visible focus, Escape/backdrop dismissal and focus return on mobile. Check contrast and reduced motion. No new security mechanism is being implemented; server authorization remains authoritative.
- **End-to-end/visual:** Use a safe development/mock persona, not production writes. Open mobile navigation, change section, follow Videos, return Home, follow Write article without saving, and change channels while analytics load. Inspect 375/768/991/992/1440px and short height; ensure the chart has nonzero size and all navigation remains reachable.
- **Integration/backend:** No new backend tests or backend execution required for this frontend-only contract-preserving plan. Existing dashboard tests are verify-only evidence; escalate only if an actual API regression appears.
- **Migration:** Not applicable; no stored preferences, schema, API/auth transition, or configuration migration.

Implementation validation results are recorded below; no backend build or deployment was performed because the change does not affect backend/API behavior.

## Migration And Rollback

No MongoDB, SQLite, API-contract, authentication, cache-tag, or configuration migration is needed. There is no compatibility window, backfill, or dual-write phase because only the SPA's presentation/local state changes.

Deploy the SPA normally after its targeted checks and browser evidence pass. There is no backend deployment order and no new flag. Keep the prior SPA artifact available through the existing release process. Roll back by redeploying that artifact; no content recovery or preference cleanup is required.

Observe normal route errors, failed dashboard reads, broken links, and user reports of unreachable modules after rollout. Existing server logging remains unchanged; do not introduce a telemetry project or instrumentation scope just for this refinement.

## Open Questions

No blocking architectural questions remain. Video/article priority and same-page analytics were confirmed by the user. The seven-section taxonomy and disclosure defaults were implemented as specified in this handoff.

**User review notes:** The user confirmed that managing videos and writing articles are the top priorities and that actions and analytics should remain on the same page. This handoff remains user-editable.

**Implementation boundary:** Only the listed BackOffice navigation/home presentation, tightly coupled state/accessibility fixes, tests, and owning documentation. Search, favorites, new metrics, article/editor redesign, import redesign, tabs, permission changes, shop feature work, backend changes, and broader styling cleanup require separate approval.

## Real Implementation Notes

Implemented the approved behavior without backend, route, shared package, contract, dependency, persistence, authentication, or deployment changes.

- `adminMenu.ts` now keeps Dashboard as a separate direct item and organizes all 31 module links into seven stable, one-level groups. A segment-aware helper selects the group for deep links.
- `AdminSidebar.tsx` filters empty groups with the existing server-effective permissions, opens Publishing at Dashboard or the matched group for a module route, and provides a single-open, keyboard-operable disclosure. Users can collapse the active section. Channel selection, configured external reports, the one responsive Offcanvas, and link dismissal are preserved.
- `Home.tsx` adds independently permission-filtered links for **Gestisci video**, **Scrivi articolo**, and **Tutti gli articoli** above the existing analytics. Analytics retain the existing API calls and metrics, refresh on selected-channel changes, ignore superseded requests, and show scoped loading, error/retry, and no-channel states. Recent video links and chart drill-through are limited to users allowed to access the video list; the table has a publication empty state.
- `index.css` adds scoped disclosure/action/status/chart styling and preserves the existing palette and responsive shell.
- Catalog, sidebar, and Home tests cover the complete route list, group matching and disclosure, permission-specific actions, retries, empty/no-channel states, and a late response from a previous channel.
- [Dashboard feature documentation](../architecture/admin-dashboard.md) now records the live taxonomy, route, action and analytics behavior, and current visual/test evidence.

### Actual files changed

| File | Implementation |
| --- | --- |
| [adminMenu.ts](../../frontend/back-office-spa/src/layouts/adminMenu.ts) | Stable seven-section catalog, standalone Dashboard item, deep-link group matching. |
| [AdminSidebar.tsx](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx) | Permission-filtered accordion and active-route behavior. |
| [Home.tsx](../../frontend/back-office-spa/src/routes/Home.tsx) | Quick actions, channel-aware analytics lifecycle, accessible publication links and states. |
| [index.css](../../frontend/back-office-spa/src/index.css) | Scoped sidebar and dashboard styles. |
| [adminMenu.test.ts](../../frontend/back-office-spa/src/layouts/adminMenu.test.ts) | Catalog inventory, uniqueness, group bounds, route matching, Images/Galleries order. |
| [AdminSidebar.test.tsx](../../frontend/back-office-spa/src/layouts/AdminSidebar.test.tsx) | Default expansion, active collapse, one-open behavior, permissions. |
| [Home.test.tsx](../../frontend/back-office-spa/src/routes/Home.test.tsx) | Action permissions, analytics recovery/states, channel race handling. |
| [admin-dashboard.md](../architecture/admin-dashboard.md) | Current feature and validation documentation. |
| This handoff | Implementation decisions, validation, and residual scope. |

### Validation performed

- Focused Vitest: 13 tests passed across the navigation catalog, sidebar, and Home.
- Regression Vitest: 54 tests passed across seven files (`AdminSidebar`, catalog, `PrimaryLayout`, protected route inventory, Home, Blog form, and video index).
- BackOffice SPA production build: `tsc -b && vite build` passed. Vite emitted the existing warning that some minified chunks exceed 500 kB.
- Prettier check passed for all seven changed application source/test/style files.
- Impeccable mechanical detector returned no findings on the four changed UI targets.
- Headless Playwright review with locally intercepted/mock API responses at 375, 768, 991, 992, and 1440px, plus 375x520. No horizontal overflow or browser page errors; the chart retained a 320px area. Desktop and mobile home/sidebar screenshots were stored as local session artifacts, not added to the repository.

No live API, authenticated deployment, production mutation, backend test, or cross-browser test was performed.

## Residual Risks

- The browser review used mocked responses; deployed authentication and dashboard API integration remain covered by existing backend/API contracts, not this UI validation.
- The headless viewport pass checked responsive sizing and page errors, not an exhaustive screen-reader or cross-browser audit. The disclosure uses native buttons, `aria-expanded`/`aria-controls`, and hidden collapsed links; mobile dismissal continues to use React-Bootstrap Offcanvas.
- Existing production build warning for JavaScript chunks larger than 500 kB was not introduced or addressed.

No migrations, feature flags, deployment ordering, or rollback data cleanup are required. The previous SPA artifact remains the rollback option.

## Final Documentation Notes

Dashboard quick actions are links only; article creation continues to use the existing unsaved editor and explicit save/publish flow. Permission filtering is presentation-only and the protected route/server checks remain authoritative. The sidebar taxonomy is navigational organization, not a change in domain ownership.
