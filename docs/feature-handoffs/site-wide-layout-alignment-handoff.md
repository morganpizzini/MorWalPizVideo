# Public Site-Wide Layout Alignment Handoff

**Status:** Implemented; validation and residual limitations recorded below  
**Owning application:** `frontend/morwalpizvideo.client`  
**Direction:** Refinement of the implemented homepage identity, not a redesign

### Outcome

Apply the public homepage's dark editorial visual system and shared site chrome consistently to every route currently registered by MorWalPizVideo Client and to the existing public account UI planned for activation. Preserve each page's content hierarchy, route, data loading, form behavior, media, and external destinations; do not turn every page into a copy of the homepage.

The user confirmed that the scope is MorWalPizVideo only and that all public pages, including utility and currently standalone routes, should adopt the same visual system. They subsequently asked to include the existing account screens in the overall UI as well. Style the login, registration, and profile surfaces now, but do not activate account routes or change authentication rollout as part of this visual alignment.

### Overlay alignment follow-up (2026-11-05)

- `ConsentBanner` now uses an app-framed editorial surface, magazine tokens, responsive action wrapping, safe-area spacing, and yellow primary/outline actions instead of Bootstrap utility layout.
- The push opt-in prompt now uses the same magazine surface, text, border, and button tokens. Its opt-in, dismiss, completion, and error behavior is unchanged.
- The footer cookie-preferences action now uses the app's yellow link treatment.
- Updated the public-client `DESIGN.md` with the app-level notice rules.
- Validation: `npm --prefix frontend/morwalpizvideo.client run build` passed; the Impeccable layout detector returned no findings for the updated components and styles. The build still reports its existing `env-config.js` bundling notice and stale Browserslist data warning.
- Browser captures were unavailable in this session, so validation was limited to source inspection, the detector, and the production build.

### Baseline Evidence (Before Implementation)

- `frontend/morwalpizvideo.client/DESIGN.md` defines the current homepage identity: magazine black `#101010`, surface `#181818`, elevated surface `#222222`, signal yellow `#ffff36`, white and secondary-gray text, Manrope, and a 1540px content cap. It explicitly says routes outside the homepage retain the light shell.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx` renders the shared header, footer, and providers, but selects a dark homepage theme and a light legacy theme for every other child route.
- `frontend/morwalpizvideo.client/src/routes/config.tsx` registers the homepage and the standard routes under `Root`. Gallery, quick links, channel news, compilations, custom forms, and survey are separate top-level routes and therefore do not share its header/footer.
- The listed route styles confirm the split: matches, sponsors, accessories, blog, and calendar use light surfaces or Bootstrap-blue emphasis; quick links has its own pale gradient and rust accents. Static pages and route components also contain explicit `bg-white`, light text, or legacy Bootstrap state classes.
- `frontend/morwalpizvideo.client/src/layouts/title-header.scss` and `src/layouts/footer.scss` already use the homepage's dark-and-yellow shell. Preserve their behavior and use them as the shared chrome rather than creating another header or footer.
- `frontend/fe-packages/layout/src/styles/index.scss` styles the shared `PageTitle` in navy and gray. App-scoped overrides can align it without changing the shared package.
- `frontend/fe-packages/layout/src/components/QuickLinksRenderer.tsx` owns a `<main>` landmark and is used by both this client and Shooting ITA. Keep its semantics and avoid changing shared layout-package behavior for this task.
- Matches, sponsorships, accessories, and blog pages have distinct media, outbound-link, or form behavior. Their visual treatment can change without changing their loaders, services, contracts, or submission flows.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx` already implements login, registration, and profile edit screens; `src/routes/account/style.scss` currently gives the account card a light background.
- `src/routes/config.tsx` does not register account routes. `PublicAccountProvider` already wraps the current root, and the header profile action points to `/account/profile`. Treat the account surfaces as near-term visual targets, but keep route activation and account authentication behavior outside this handoff.

### Proposed Design

1. Use the homepage's current tokens, typography, contrast, content width, borders, and yellow action/selection treatment as the site's visual authority. Keep page-specific composition: video-focused match details, sponsor logo grid and sponsor request form, grouped accessories, reading layouts, gallery controls, and utility forms remain purpose-built.
2. Apply one app-owned public shell to every route in the current manifest. Move the six top-level routes into the existing root route so they receive the same navigation, footer, navigation/account providers, loading behavior, and error handling. Preserve their URLs, route loaders/actions, and deep links. Remove the compilation page's extra local header once it is inside the shared shell.
3. Keep the shell's content frame as a non-landmark wrapper and maintain exactly one page-level `<main>` landmark per rendered page. The shared `QuickLinksRenderer` already supplies its own main landmark; do not wrap it in another main or alter the shared component. Add route-local main landmarks where needed, while retaining those already owned by a route or renderer. Keep the skip link's target usable after this structural change.
4. Put site-wide tokens, frame sizing, baseline type, and control defaults in the public client's `src/main.scss`. Add narrowly scoped app-level styles for shared `PageTitle` and shared layout-renderer classes. Keep page-specific styling in the owning route files, and avoid broad overrides that make Bootstrap states, embedded content, or third-party media unreadable.
5. Keep real sponsor assets legible on dark surfaces; use a neutral image frame where the source logo needs it. Keep forms, required/invalid feedback, success/error/loading states, and reCAPTCHA behavior clear and accessible in the dark theme.
6. Align account login, registration, and profile forms with the same surfaces, typography, focus, input, and action styles. Preserve field-level server errors, global errors, success feedback, legal acknowledgements, password/email inputs, and logout behavior. Give each account screen a proper page-level heading and one main landmark when its routes are activated.
7. Preserve layout responsiveness independently for each page: no horizontal page overflow, legible text and media, working filter/selection/action controls, and touch-safe targets. Do not force identical grids, card types, or page-title placement onto pages with different jobs.

**Impeccable planning lens:** The overall site is a public creator hub, but page mode varies: homepage/sponsors/accessories are primarily `Persuade`; articles, legal content, match and channel-news details are `Read`/`Experience`; gallery and forms are `Operate`. `DESIGN.md` exists and is the incumbent visual authority; there is no public-client `PRODUCT.md`. Use `audit` to inventory route-level drift, then the narrow `adapt` workflow for extending the existing visual system across routes, followed by `harden` and `polish`. This is a refinement, not a replacement visual world. Do not execute Impeccable during planning.

### Impacted Projects

- `frontend/morwalpizvideo.client`: Direct change. Owns the public route manifest, shared shell, route components, styles, design documentation, tests, and client/SSR artifacts.
- Public-account UI within `frontend/morwalpizvideo.client`: Direct visual change to existing login, registration, and profile components. Account endpoint, cookie, CSRF, activation, and authorization policy remain unchanged.
- `frontend/fe-packages/layout`: Verify only. Reuse `PageTitle`, `QuickLinksRenderer`, `CustomFormRenderer`, `BlogRenderer`, and `VideoPlayer`; avoid a shared-package change unless implementation evidence proves it unavoidable. In particular, `QuickLinksRenderer` is consumed by Shooting ITA.
- `frontend/shooting-ita-frontend`: Verify only for regressions if any shared layout-package behavior is changed. No visual restyling is in scope.
- `frontend/fe-packages/models` and `frontend/fe-packages/services`: No expected changes; existing route contracts and service owners remain.
- `MorWalPizVideo.ServerAPI` and all other .NET/API projects: No change expected.
- Public-client deployment on Aruba: Build/deployment output must continue to support the same routes, SSR/PWA behavior, and runtime API configuration. No deployment configuration change is planned.

### Impacted Files

#### Modify

- `frontend/morwalpizvideo.client/src/routes/config.tsx`: Place the currently standalone gallery, quick-links, channel-news, compilations, custom-form, and survey routes under the public shell while retaining route paths, loaders, actions, and appropriate error elements.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx`: Apply the same shell and theme to every registered route; keep one content frame and avoid duplicate header/footer or nested main landmarks.
- `frontend/morwalpizvideo.client/src/main.scss`: Define/apply the public-client-wide frame and homepage-aligned tokens, and remove the home-versus-legacy visual split.
- `frontend/morwalpizvideo.client/DESIGN.md`: Document the site-wide implemented system and page-specific layout rules after the source work is complete.
- `frontend/morwalpizvideo.client/src/routes/matches/Component.tsx` and `matches/style.scss`: Restyle match title, gallery, video details, and video panels while preserving player/gallery behavior.
- `frontend/morwalpizvideo.client/src/routes/sponsors/Component.tsx` and `sponsors/style.scss`: Align sponsor grid, logo frames, and sponsor-request form while preserving its existing form renderer, reCAPTCHA, and action.
- `frontend/morwalpizvideo.client/src/routes/accessories/Component.tsx` and `accessories/style.scss`: Align category/subcategory/product hierarchy and outbound actions without changing grouping/data behavior.
- `frontend/morwalpizvideo.client/src/routes/calendar/Component.tsx` and `calendar/style.scss`: Align cards, date/category states, and match links.
- `frontend/morwalpizvideo.client/src/routes/blog/index.tsx` and `blog/style.scss`: Align list/article surfaces, metadata, pagination, and rich article rendering.
- `frontend/morwalpizvideo.client/src/routes/pages/Component.tsx`: Align dynamic content, video, thumbnail, and optional form presentation.
- `frontend/morwalpizvideo.client/src/routes/compilations/Component.tsx`: Align video cards and remove the duplicated local header after shell integration.
- `frontend/morwalpizvideo.client/src/routes/quickLinks/Component.tsx` and `quickLinks/style.scss`: Apply the public visual system to this client while preserving renderer semantics and external destinations.
- `frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx`: Align gallery toolbar, cards, modal, selection, and download states; preserve download behavior.
- `frontend/morwalpizvideo.client/src/routes/channelNews/Component.tsx`: Align the article-like detail view and media.
- `frontend/morwalpizvideo.client/src/routes/customForms/Component.tsx` and `src/routes/survey/Component.tsx`: Align form cards, progress, validation, and completion states without changing submission behavior.
- `frontend/morwalpizvideo.client/src/routes/links/Component.tsx` and `links/style.scss`: Align the link landing page while retaining each outbound destination.
- `frontend/morwalpizvideo.client/src/routes/stream/Component.tsx`: Align live/offline and chat/player presentation without changing stream configuration or embeds.
- `frontend/morwalpizvideo.client/src/routes/system/bluetooth.tsx` and `system/sponsors-video.tsx`: Align static/support pages and remove legacy light-only page surfaces.
- `frontend/morwalpizvideo.client/src/routes/system/privacy.tsx` and `system/terms.tsx`: Align legal reading surfaces without changing policy copy.
- `frontend/morwalpizvideo.client/src/routes/newsletter/Component.tsx` and `pushSettings/Component.tsx`: Align forms and notification states while preserving token, push, and subscription behavior.
- `frontend/morwalpizvideo.client/src/analytics/ConsentBanner.tsx` and `src/analytics/consent-banner.scss`, plus `src/components/push-opt-in.scss`: Align global cookie and notification overlays with the public app layout without changing consent or push behavior.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx` and `account/style.scss`: Align login, registration, profile, loading, field/global error, success, and logout presentation; preserve account service calls and legal consent behavior.
- `frontend/morwalpizvideo.client/src/error-page.tsx`: Align routed error presentation and provide the correct page landmark under the shared shell.
- Add a focused public-client shell/route test under `frontend/morwalpizvideo.client/src/routes/layout/` (or extend the established route test) to cover shared chrome, route nesting, and landmark structure.
- Extend the relevant existing route/component tests, especially `src/routes/gallery-route.test.tsx`, to verify that moved routes still resolve and retain their route-specific behavior.

#### Add

- No new production component or shared design package is required by the recommended design.
- Add a colocated shell/route test only if existing tests cannot cover the route manifest and shared-shell integration cleanly.

#### Generated/Migration

- None. Do not edit `dist`, generated PWA/service-worker output, archives, or lockfiles.

#### Documentation

- `docs/feature-handoffs/site-wide-layout-alignment-handoff.md`: This approval handoff; update its implementation record, changed files, validation results, and remaining risks when the work is delivered.
- `frontend/morwalpizvideo.client/DESIGN.md`: Update to the resulting source truth as listed under Modify.
- `docs/feature-handoffs/new-ui-design-handoff.md`: Historical evidence for the earlier homepage-only scope; retain it as history, but ensure the new site-wide handoff is treated as the active scope.

#### Verify Only

- `frontend/morwalpizvideo.client/src/routes/home/Component.tsx` and `home/style.scss`: Preserve the homepage exactly as the visual baseline, except for any minimal shared-shell adjustment required by the root landmark change.
- `frontend/morwalpizvideo.client/src/layouts/title-header.tsx`, `title-header.scss`, `layouts/footer.tsx`, and `footer.scss`: Confirm the existing navigation, skip link, social/account actions, footer destinations, responsive behavior, and dark identity remain correct on all routes.
- `frontend/morwalpizvideo.client/src/routes/layout/navigation.tsx` and `layout/account.tsx`: Preserve navigation/account provider behavior after route nesting.
- `frontend/morwalpizvideo.client/src/layouts/title-header.tsx`: Preserve the account profile action and its current session-dependent behavior.
- `frontend/morwalpizvideo.client/src/routes/home/loader.ts`, `home/loader.test.ts`, and `home/Component.test.tsx`: Preserve homepage fetch, failure, and destination behavior.
- `frontend/fe-packages/layout/src/components/PageTitle.tsx`, `QuickLinksRenderer.tsx`, and `src/styles/index.scss`: Reuse as-is; scope any presentation overrides to MorWalPizVideo Client.
- `frontend/morwalpizvideo.client/src/routes/ask/Component.tsx`: Not registered in the current public route manifest; do not register or redesign it as a side effect.
- `frontend/morwalpizvideo.client/package.json`: Use the existing test, lint, formatting, client-build, and SSR-build scripts; no dependency or test-tool change is needed.

### Impact Matrix

| Area | Impact | Decision |
|---|---|---|
| Backend/API | None | Preserve public endpoints, DTOs, data loaders, and submission contracts. |
| Frontend/UI | Direct, broad | Align every currently registered public route and unify shell presentation; preserve each page's task and content hierarchy. |
| Database/Data | None | No persisted data or query change. |
| Contracts/Models | None | Reuse current route and shared-model shapes. |
| Authentication/Authorization | Verify only | Include account UI styling, but preserve public-account auth/session/CSRF behavior and do not activate routes or change rollout. |
| Validation | UI-only | Preserve native and server validation, reCAPTCHA, error messaging, loading, success, empty, unsupported, and unavailable states. |
| Cache | None | No cache tags, data invalidation, or server cache behavior changes. |
| Configuration/Secrets | None | Preserve existing build/runtime API configuration; introduce no configuration values or secrets. |
| Logging/Telemetry | None | Preserve existing page-view and event tracking; no new instrumentation required. |
| Background Jobs | Not applicable | No job behavior. |
| Deployment/Operations | Verify | Keep Aruba route fallback, SSR, PWA, and runtime configuration behavior compatible. |
| Backward Compatibility | Required | Keep all current URLs, deep links, route loaders/actions, external destinations, forms, and page-specific interactions working. |

### Reuse Opportunities

- Reuse the current `Root`, `TitleComponent`, footer, navigation/account providers, and global token set rather than building another shell.
- Reuse `PageTitle`, `VideoPlayer`, `BlogRenderer`, `CustomFormRenderer`, and `QuickLinksRenderer` from `@morwalpiz/layout`; style them at the application boundary.
- Reuse the route-local Sass and Bootstrap patterns already used in the public client; do not add Tailwind or a second styling system.
- Preserve the existing loaders, services, form actions, recaptcha integration, gallery download helpers, and stream configuration.
- Keep brand tokens centralized in the public app; do not move homepage components into shared packages without a second demonstrated consumer.

### Risks

| Rank | Risk | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|---|
| High | A route escapes the shared shell or breaks on reparenting | Six dynamic routes are currently top-level and carry distinct loaders/actions/error boundaries. | Move them without changing their route path or data functions; test direct URL loads, navigation, and failure cases. | Every registered route renders the same header/footer, and direct deep links still resolve. |
| High | Landmark or layout nesting becomes invalid | Root currently provides `<main>`; some route components and `QuickLinksRenderer` also own `<main>`. | Use a non-landmark shell content wrapper; preserve existing route-owned main landmarks and add one only where absent. | Each page has exactly one main landmark and the skip link targets the content area. |
| Medium | Global styling damages specialized surfaces | Embedded rich text, forms, video players, gallery modal, and configurable quick links have special requirements. | Prefer app-scoped tokens and route-scoped styles; avoid global selectors that override renderer semantics or state colors. | All dynamic/content-heavy surfaces remain legible and usable at desktop and mobile widths. |
| Medium | Shared package change causes cross-app regression | `QuickLinksRenderer` is also consumed by Shooting ITA; shared `PageTitle` is not public-client-owned. | Do not modify the layout package for this task; add client-local styles. If a shared change proves necessary, expand impact tests and build order explicitly. | No shared package source change is needed, or all affected consumers pass their checks. |
| Medium | Visual consistency is achieved by erasing page purpose | A literal common template could make sponsor, match, legal, gallery, and form pages harder to use. | Align visual tokens and shell, not page information architecture; preserve task-specific composition. | Route-by-route visual review confirms clear page hierarchy and intact primary actions. |
| Medium | Existing behavior is lost during style or shell changes | Multiple pages have forms, downloads, embeds, filters, and external links. | Keep behavior changes out of scope; exercise existing route interactions during implementation and browser review. | Existing targeted tests and manual interaction checks remain green. |
| Low | Design documentation remains misleading | `DESIGN.md` explicitly records the old light-route exception. | Update it only after implementation matches source; distinguish global shell rules from route-specific composition. | Design documentation and source tokens/layout behavior agree. |

### Technical Debt

- **Intentional legacy theme split:** `Root` and `DESIGN.md` encode homepage-only dark styling. This is the direct cause of the mismatch and is addressed in scope.
- **Legacy route styling:** Several pages use hard-coded Bootstrap blue, white surfaces, or bespoke colors. Replace only the visual rules that conflict with the approved system; do not refactor route logic.
- **Shared `PageTitle` color:** The shared component has light-theme defaults. Handle with a public-client-scoped override; shared-package extraction or redesign is deferred.
- **Standalone dynamic routes:** Six routes lack global navigation/footer. Integrating them is required by the confirmed scope; preserve their URLs and custom renderer behavior.
- **Account route activation:** Login, registration, and profile UI exist but are not registered. Style these surfaces now as requested; route activation, authentication rollout, and endpoint availability remain separate release decisions.
- **Unregistered ask component:** Source exists, but it is not in the current route manifest and is not added to scope.

### Implementation Roadmap

1. **Establish the route and shell baseline.** Inventory the current `routes/config.tsx` tree and capture representative desktop/mobile evidence for all route families. Record dynamic route URLs and the current data/error states. No product behavior changes.
2. **Unify route ownership.** Move gallery, quick links, channel-news, compilations, forms, and survey under the existing public root. Preserve route paths, loaders/actions, URL parameters, and error handling. Remove the compilation page's duplicate local header.
3. **Normalize shell and landmarks.** Remove the home-versus-legacy theme split, use the public visual system for every route, and preserve a single main landmark per page. Retain `QuickLinksRenderer`'s existing main; provide route-local main landmarks only where absent. Verify skip-link targeting and loading state.
4. **Align shared route primitives and page styles.** Apply app-local styling to shared page titles, forms, article rendering, buttons, focus states, and surfaces. Keep responsive shell dimensions and page-specific layouts distinct.
5. **Restyle route families in functional priority order.** Matches/media; sponsors and accessories; calendar/blog/content pages; forms/survey/newsletter/push settings and account screens; gallery/quick links/channel news/compilations; legal, static, links, stream, and error pages. Preserve imagery, media ratios, copy, destinations, and all interaction states.
6. **Add focused automated coverage.** Test route matching after nesting, shared chrome on direct navigation, landmark count, selected representative theme/state behavior, and unchanged route-specific interactions. Extend only the relevant existing tests.
7. **Validate and review the full route set.** Run the public-client tests and lint/format/build gates, including client and SSR outputs. Complete a bounded browser evidence pass over every route at desktop and mobile sizes, then fix material issues in one batch and confirm once.
8. **Reconcile design records.** Update `DESIGN.md` to reflect shipped behavior and this handoff with actual files, validation evidence, and residual risks. Stop after the completion criteria below.

**Completion boundary:** All currently registered routes use the common dark editorial system and header/footer; account UI is styled consistently in preparation for activation without changing rollout; no route has unintended horizontal overflow or contrast/readability failures; each keeps its own information architecture and working behavior; direct route and error handling remain compatible; client tests and client/SSR builds pass; responsive and accessibility evidence is recorded. Do not activate account or ask routes, redesign page information architecture, change shared packages, introduce new APIs, or expand to other applications without separate approval.

An independent post-validation review is warranted because the change touches every public route, moves route boundaries, changes shared-shell coverage, and carries cross-route accessibility/regression risk. The user may select `MorWalPiz Code Reviewer` after implementation; review execution is not part of this handoff.

### Testing Strategy

- **Unit/component:** Extend public-client Vitest/Testing Library coverage for the shell, theme application, one-main-landmark structure, and route-specific empty/error/success states. Preserve existing homepage and gallery tests.
- **Routing/integration:** Exercise every registered static and dynamic route by direct URL and internal navigation. Specifically cover the six moved routes, loader/action execution, query parameters, and each route's existing error element.
- **Feature behavior:** Verify match video/gallery navigation, sponsor submission/reCAPTCHA feedback, accessories outbound links, blog pagination/article render, calendar match links, gallery view/select/download/modal, quick-link external destinations, form/survey progression, newsletter token actions, push setting states, and stream on/off/chat behavior.
- **Account UI:** Render the unregistered login, registration, and profile components with their existing provider/test doubles. Verify input and legal-consent states, field/global errors, successful login/register/profile update, loading and unauthenticated profile states, and logout presentation without enabling production account routes or contacting live services.
- **Accessibility:** Verify one main landmark per route, skip-link target, heading order, keyboard focus, visible focus, modal/dialog controls, labels/errors, live status messages, reduced motion, contrast, and touch target sizing.
- **Responsive/visual:** Capture homepage baseline and every route family at wide desktop, tablet, common phone, and narrow 320px widths. Check no horizontal overflow, clipped text, obscured controls, awkward nested gutters, or media distortion; test 200% zoom and landscape on representative dense pages and forms.
- **SSR/build:** Confirm client and SSR rendering/hydration for homepage, representative dynamic page, and the shared QuickLinks renderer; ensure the PWA/deep-link output remains compatible.
- **Cross-app:** If no shared package changes occur, no other app suite is required. If a shared layout-package edit becomes necessary, verify Shooting ITA's QuickLinks use and its existing targeted test/build path before acceptance.
- **Backend/contract/security/migration:** No backend, API, authorization, contract, persistence, or migration tests are expected. Confirm no request shape, endpoint, auth behavior, or secret/config value changed.

### Migration And Rollback

No data, MongoDB, SQLite, API-contract, cache, authentication, or configuration migration is needed. The route-tree change is frontend-only and keeps each registered public URL and loader/action stable. Account routes remain inactive; styling their existing components does not activate account APIs or alter the cookie/CSRF boundary. Deploy the new public-client artifact as a normal application release after client/SSR and deep-link checks. If visual or route regressions appear, roll back the public-client artifact; no server or data rollback is required. Preserve existing runtime API configuration and PWA routing behavior.

### Open Questions

None for visual scope. The user confirmed the owning scope (MorWalPizVideo public client only), all public pages including standalone routes, and inclusion of the existing account UI. Account route activation remains intentionally out of scope until its separate enablement decision.

### Implementation Record

- Moved the six standalone public routes beneath the existing root shell while preserving their URLs, loaders, actions, and error handling. The shell uses a non-landmark content target, and route-owned `<main>` landmarks remain unique; account routes remain unregistered.
- Applied the dark editorial baseline, shared frame, accessible focus and control states, page-specific route styling, and account-form styling without changing the existing account rollout.
- Promoted the shared palette, type roles, radius choices, and layered shadows into the public client's app-level tokens and reused those tokens in the shell, homepage, calendar, accessories, links, quick links, and sponsor styles. Retained existing page-specific type and radius values as named design-system exceptions rather than shifting their visual hierarchy.
- Updated [DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md), [config.tsx](../../frontend/morwalpizvideo.client/src/routes/config.tsx), [root.tsx](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx), [main.scss](../../frontend/morwalpizvideo.client/src/main.scss), shared header/footer styles, route styles and landmarks, account styles, and shell/route tests. No shared package, backend, contract, deployment, or authentication behavior was changed.
- Validation: public-client tests passed (8 files, 23 tests); `build:all` passed for client and SSR. The design detector reports one pre-existing intentional section-heading marker documented in `DESIGN.md`. Full lint and format checks remain blocked by existing repository issues (generated-output ESLint configuration/source issues and 61 files needing formatting); targeted lint showed only pre-existing Bluetooth `any` errors/warnings in the touched source set. Browser screenshot review was unavailable, so desktop/mobile visual captures remain unverified.
- Residual risk: route visuals have no browser-based desktop/mobile screenshot review. Existing lint/format baseline issues are not caused or fixed by this work. No migration or deployment configuration change is required.
