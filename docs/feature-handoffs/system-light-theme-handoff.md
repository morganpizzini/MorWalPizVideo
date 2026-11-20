# System-aware appearance across public, Range and Workshop clients

## Status

Implemented in source. Shared and client builds and all three client test suites
pass. Browser screenshots, runtime hydration, deployed CSP, and PWA/offline
verification remain open; this handoff is not deployment approval.

## Outcome and scope

Add **System / Light / Dark** appearance choices to the public MorWalPizVideo
client, then extend the same capability to Shooting Range and Shooter Workshop.
System is the default and follows the device, including later device changes.
An explicit light or dark choice overrides the device and persists.

The theme is a **global shared asset**, not three independently maintained
palettes: all three clients consume the same centrally owned light and dark
semantic tokens and appearance behavior. This is a visual refinement, not a
redesign: preserve existing content, routes, actions, dark appearance, media,
branding and each application's layout. The proposed light direction is cool
light gray with slate-blue actions; retain small yellow brand accents. Allow
only documented app-specific exceptions needed to preserve existing behavior,
semantic status meaning or media readability. Do not extend the feature to
other applications.

**Assumption for review:** preferences are per browser origin and device, not
synced across applications or accounts. The public site and Workshop have
different documented origins; browser storage is origin-scoped. The same
capability and defaults apply in each app, but selecting a theme in one does not
change another. Cross-origin or account synchronization is outside this plan.

## Current evidence

- The public site's [design record](../../frontend/morwalpizvideo.client/DESIGN.md)
  describes a dark editorial identity. Its
  [palette and Bootstrap overrides](../../frontend/morwalpizvideo.client/src/main.scss)
  are dark; the [root](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx),
  [header](../../frontend/morwalpizvideo.client/src/layouts/title-header.tsx)
  and [footer](../../frontend/morwalpizvideo.client/src/layouts/footer.tsx)
  explicitly select Bootstrap dark. Header and homepage styles also contain
  hardcoded colors.
- [Public routes](../../frontend/morwalpizvideo.client/src/routes/config.tsx)
  place QuickLinks outside the root shell, and the root error page uses a
  reduced header. Theme application must not depend solely on the root route.
  Consent and donation widgets are mounted outside that route in
  [main.tsx](../../frontend/morwalpizvideo.client/src/main.tsx).
- Public [initial HTML](../../frontend/morwalpizvideo.client/index.html)
  contains a dark skeleton and browser theme color. It supports both client
  rendering and [SSR](../../frontend/morwalpizvideo.client/src/entry-server.tsx)
  followed by hydration. Preserve SSR, anonymous article HTML and metadata.
- [Range styles](../../frontend/shooting-range.client/src/styles.css) define
  dark/yellow tokens and force dark native controls. Its
  [shell](../../frontend/shooting-range.client/src/App.tsx) covers login and
  authenticated use; the client also has SSR and hydration.
- [Workshop styles](../../frontend/shooter-workshop.client/src/main.scss) use
  dark/yellow tokens and fixed colors; its authenticated
  [shell](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx) has
  a language control. Login and registration are outside that shell in
  [routes](../../frontend/shooter-workshop.client/src/routes/config.tsx).
  Workshop is client-rendered and has a static PWA manifest.
- [Shared layout](../../frontend/fe-packages/layout/src/index.ts) already
  exports presentation utilities and components and is a dependency of all
  three apps. Its existing [stylesheet](../../frontend/fe-packages/layout/src/styles/index.scss)
  is exported as a general component stylesheet, not as a shared theme; it
  contains unrelated fixed colors. It does not currently provide this
  appearance capability. Other consumers must remain unchanged.
- The [public feature record](../features/public-experience.md),
  [Range architecture](../shooting-range-architecture.md), and
  [Workshop architecture](../architecture/shooter-workshop.md) define existing
  visual, responsive and delivery constraints. Historic validation in those
  records is not fresh evidence for this change.

## Proposed design and behavior

### User-facing contract

1. Provide an accessible, labeled three-option appearance selector in each
   application's existing header: **Sistema**, **Chiaro**, **Scuro**. Use
   Workshop's existing localization approach for all supported languages.
   Make it available during login and normal use; place Workshop's account-page
   selector beside its existing language control. Maintain focus, menu behavior,
   touch targets and narrow-screen usability.
2. With no valid saved choice, use System. In System, react to device changes
   while the page is open. An explicit selection remains in effect until changed
   or reset to System. Selecting System removes the explicit override.
3. Keep the preference in a dedicated, versioned, non-sensitive local-storage
   key per origin, independent of login, analytics consent and account-scoped
   Workshop preferences. Apply storage changes to other open tabs on the same
   origin. Do not send it to an API or synchronize it across origins.
4. If storage is unavailable or a write fails, keep the current selection for
   the open page and show an accessible, local explanation that it may not
   persist; log the failure without user data. An invalid saved value is not a
   valid override: report it diagnostically and use System. If the device
   preference API is unavailable, use dark as the explicit fallback; do not
   misrepresent this as a saved choice.
5. Apply the resolved theme to the document, not only to a router shell. This
   covers overlays, direct links, QuickLinks and error states. Preserve the
   immersive QuickLinks layout: it follows the preference but does not gain a
   full site header. The existing normal public header is where users change
   their choice.

### Palette direction

| Semantic role | Proposed light value |
| --- | --- |
| Page background | `#F2F4F6` |
| Card / panel | `#FFFFFF` |
| Subtle surface | `#E8EDF1` |
| Primary text | `#1C2B36` |
| Secondary text | `#536573` |
| Border | `#C9D3DA` |
| Primary action / link | `#38566D` |
| Action hover | `#263F52` |
| Text on action | `#FFFFFF` |
| Focus | `#205B82` |

These are a starting direction, not measured passing colors. Define the full
light and dark palettes once in the shared theme asset, including roles for
accent text, on-accent text, focus, selected states, borders and semantic
success/warning/error; do not reuse a page-background token as button text.
Use app-local aliases only to map existing component variables to these shared
roles, not as copies of the palettes. Keep yellow as a limited brand marker
rather than yellow body text on light surfaces. Retain intentionally dark image
overlays and QR foreground/background where needed for readability and
scanning; do not recolor them just to eliminate literals. Record and justify
any necessary per-app visual exception rather than silently forking a theme.
Adjust proposed values only as necessary to pass contrast and preserve the
approved visual direction.

### Shared boundary and startup

- Add an **opt-in global theme asset**, appearance utility and accessible
  selector to `@morwalpiz/layout`. Put the shared light/dark semantic tokens
  in a dedicated `src/styles/theme.scss` and expose its built CSS through a new
  `./theme` package subpath. The existing Sass styles build already emits
  styles from that directory; preserve the existing `./styles` export and
  unrelated component stylesheet unchanged. Each of the three target clients
  imports the shared theme asset before its local component styles. Existing
  layout consumers that do not import it must not be restyled.
- The shared asset is the sole source of truth for theme palette values.
  Target apps map existing variable names and Bootstrap variables to shared
  roles and own layout, placement, translations and component-specific rules.
  Explicitly document exceptional local values that are necessary to preserve
  existing dark status colors, brand media, overlays or QR readability; do not
  maintain separate copies of the common light or dark palette.
- Share preference validation, resolution, live device/storage subscriptions
  and selection semantics. The shared asset's system-preference fallback and
  explicit document-theme selectors must agree with the bootstrap and runtime:
  no app may silently replace the central palette when loading its own CSS.
- The first document paint must use a saved override or the system preference
  before React hydration. Use a small synchronous same-origin bootstrap in each
  app's initial HTML, before theme-sensitive styles; keep only the unavoidable
  early-boot logic there and verify that its storage key, valid values and
  resolution agree with the shared runtime. Shared runtime takes ownership
  after startup. Do not require an external service or a new cookie. Review the
  actual deployment content-security policy before choosing script packaging;
  do not weaken it.
- Public and Range SSR must render deterministic server markup without reading
  a real browser preference or leaking state between requests. The bootstrapped
  document theme and React's hydration snapshot must agree without hydration
  warnings; the visible selector must reconcile to the saved choice after
  hydration. Preserve existing status codes and route data.
- For JavaScript-disabled or blocked-bootstrap cases, provide a readable
  system-aware CSS baseline and usable existing content. Document any native
  Bootstrap-control limitation discovered during implementation rather than
  claiming unsupported behavior. Set native `color-scheme` and Bootstrap
  `data-bs-theme` consistently for the resolved theme when JavaScript runs.
- Scope `theme-color` changes to actual browser behavior. Static installed PWA
  splash/manifest colors cannot reliably follow a runtime preference; retain
  compatible static values unless a tested change is justified. Verify the
  public Workbox update/offline path and Workshop's existing service-worker
  lifecycle without adding caches or changing notification behavior.

## Impacted projects and files

Paths below are concrete owners; additional existing route styles may need
targeted changes after the required rendered-state audit. Avoid a blanket
replacement of every fixed color.

### Modify

- [Shared layout exports](../../frontend/fe-packages/layout/src/index.ts) and
  [package manifest](../../frontend/fe-packages/layout/package.json): export
  the opt-in theme API, control and dedicated CSS subpath without changing the
  existing shared component stylesheet or its consumers.
- Public [index.html](../../frontend/morwalpizvideo.client/index.html),
  [main.tsx](../../frontend/morwalpizvideo.client/src/main.tsx),
  [root.tsx](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx),
  [header](../../frontend/morwalpizvideo.client/src/layouts/title-header.tsx),
  [footer](../../frontend/morwalpizvideo.client/src/layouts/footer.tsx),
  [main.scss](../../frontend/morwalpizvideo.client/src/main.scss),
  [header styles](../../frontend/morwalpizvideo.client/src/layouts/title-header.scss),
  [footer styles](../../frontend/morwalpizvideo.client/src/layouts/footer.scss),
  [home styles](../../frontend/morwalpizvideo.client/src/routes/home/style.scss)
  and directly affected route/overlay styles: initial theme, controls, semantic
  tokens and explicit dark assumptions.
- Range [index.html](../../frontend/shooting-range.client/index.html),
  [main.tsx](../../frontend/shooting-range.client/src/main.tsx),
  [App.tsx](../../frontend/shooting-range.client/src/App.tsx) and
  [styles.css](../../frontend/shooting-range.client/src/styles.css): SSR-safe
  bootstrap, shared selector, theme roles and native controls.
- Workshop [index.html](../../frontend/shooter-workshop.client/index.html),
  [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx),
  [AppShell.tsx](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx),
  [login](../../frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx),
  [registration](../../frontend/shooter-workshop.client/src/routes/AccountRegister/component.tsx),
  [main.scss](../../frontend/shooter-workshop.client/src/main.scss) and
  [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx): startup,
  selectors, translations, tokens and affected application states.
- Focused existing tests:
  [public shell](../../frontend/morwalpizvideo.client/src/routes/layout/root.test.tsx),
  [public header](../../frontend/morwalpizvideo.client/src/layouts/title-header.test.tsx),
  [Range App](../../frontend/shooting-range.client/src/App.test.tsx), and
  Workshop's existing localization and route/component tests where appropriate.
  Replace the public dark-only assertion with assertions for the intended
  SSR/theme behavior; keep the landmark and navigation checks.

### Add

- The canonical shared `src/styles/theme.scss` under
  [layout source](../../frontend/fe-packages/layout/src/), plus a small shared
  appearance utility and control following existing module/export conventions.
  Include system fallback, light/dark roles and explicit theme selection.
- App-local first-paint bootstrap assets as necessary in the three clients,
  with parity coverage against shared resolution. No new dependencies are
  assumed; use existing test runners.
- Focused appearance behavior tests in existing application test locations,
  including bootstrap/shared-contract parity and the control's failure state.

### Generated / migration

No database, API or generated migration. Do not edit built `dist` output or
generated PWA assets. Shared layout and app builds produce their normal assets.

### Documentation

- Update this handoff after implementation with actual files, deviations,
  validation results and residual risks.
- Update the public [DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md)
  from the shipped result, plus
  [public experience](../features/public-experience.md),
  [Range architecture](../shooting-range-architecture.md) and
  [Workshop architecture](../architecture/shooter-workshop.md) for affected
  appearance behavior and delivery constraints. Update
  [frontend architecture](../architecture/frontend.md) for the opt-in shared
  UI capability if it becomes a maintained cross-app contract. No new ADR is
  proposed for an additive presentation utility.

### Verify only

- Public [entry-server.tsx](../../frontend/morwalpizvideo.client/src/entry-server.tsx),
  [server.mjs](../../frontend/morwalpizvideo.client/server.mjs),
  [QuickLinks route](../../frontend/morwalpizvideo.client/src/routes/quickLinks/Component.tsx),
  [error page](../../frontend/morwalpizvideo.client/src/error-page-root.tsx),
  consent/push components, shared renderers and
  [Vite PWA configuration](../../frontend/morwalpizvideo.client/vite.config.ts).
- Range [entry-server.tsx](../../frontend/shooting-range.client/src/entry-server.tsx),
  [server.mjs](../../frontend/shooting-range.client/server.mjs), admin,
  loading and error states.
- Workshop [route configuration](../../frontend/shooter-workshop.client/src/routes/config.tsx),
  error/loading states,
  [manifest](../../frontend/shooter-workshop.client/public/manifest.webmanifest),
  [service worker](../../frontend/shooter-workshop.client/public/workshop-sw.js),
  QR output and existing local preferences.
- Other layout consumers: BackOffice, Shooting ITA and the on-hold shop. Verify
  additive exports and unchanged defaults; do not redesign or activate them.
  Review each target's build/container and workflow path coverage for shared
  layout and bootstrap assets.

## Impact matrix

| Area | Impact |
| --- | --- |
| Backend / API | None; no new endpoint or server preference. |
| Frontend / UI | One opt-in shared light/dark theme asset and three client shells, login states, direct links, overlays, forms and theme-sensitive states. |
| Database / data | No database changes. One origin-scoped browser preference only. |
| Contracts / models | Additive shared frontend theme CSS subpath and presentation API; no wire DTO changes. |
| Authentication / authorization | No changes; preferences are independent of identity and do not alter auth flows. |
| Validation | Validate preference values and storage errors; existing form/domain validation remains authoritative. |
| Cache | No API cache changes. Check existing PWA asset updates and offline behavior. |
| Configuration / secrets | No new secret, service or deployed configuration. Check actual CSP before bootstrapping. |
| Logging / telemetry | Diagnostic logging for storage failures and invalid stored values without personal data; no new telemetry service. |
| Background jobs | None. |
| Deployment / operations | Existing independent client artifacts and gates; no deployment authorized by this plan. |
| Backward compatibility | Existing dark styling preserved when Dark is selected; first-time default intentionally changes to device preference. Other layout consumers remain opt-in. |

## Reuse and design workflow

- Reuse the shared layout package, its existing Sass build and its
  [media-preference utility pattern](../../frontend/fe-packages/layout/src/utils/prefersReducedMotion.ts).
  Share the canonical semantic palette and selector behavior; keep component
  styling and selector placement app-owned.
- Map existing app-local variable names to the shared tokens, preserving
  justified exceptions, localization, account controls, route boundaries,
  SSR and test fixtures. Keep status meaning supported by labels or icons, not
  color alone.
- Impeccable: **refinement**, not redesign. Public home is Persuade/Read and
  article pages Read; Range and Workshop are Operate. The public client has an
  incumbent DESIGN.md; no PRODUCT.md was found for it. Inspect the other
  clients' actual design context before implementation. Use `colorize` for
  role-based palette work and `audit`/`adapt` for contrast, responsive and
  accessibility checks; preserve each incumbent identity. Capture real
  screenshots in both themes at phone and desktop sizes and compare the dark
  baseline before sign-off. The planning session did not run the Impeccable
  launcher or render the apps.

## Risks and technical debt

| Risk | Level | Mitigation and verification signal |
| --- | --- | --- |
| Light text, controls or focus become unreadable due to fixed colors or token reuse | High | Audit computed pairs and interactive states across all three clients; normal text >= 4.5:1, large text and relevant control/focus boundaries >= 3:1. |
| SSR flash, mismatched hydration or request-state leakage | High | First-paint bootstrap, deterministic server snapshot and direct-route SSR/hydration checks for both SSR clients; no hydration errors or cross-request state. |
| Bootstrap logic drifts from shared behavior | Medium | Keep bootstrap minimal and test the same valid, invalid, missing and unavailable-storage cases against shared behavior in each app. |
| Theme choice appears shared across different origins but is not | Medium | Label and document device/browser-local behavior; test each origin independently. Cross-origin sync requires a separately approved design. |
| Forms, status, QR, overlays or PWA states become misleading | Medium | Inspect status labels, validation, account pages, dialogs, loading/error/empty states, QR readability and installed/offline behavior in both themes. |
| First-time users see a different default | Medium | Explicitly document System as an intentional change; verify Dark restores the prior appearance and test rollback. |
| Shared theme unexpectedly restyles other consumers | Medium | Dedicated opt-in CSS subpath, no change to the existing component stylesheet, dependency-ordered build and consumer checks. |
| App styles override or duplicate the shared theme | Medium | Verify computed semantic roles and stylesheet ordering in all three clients; justify and document each local exception. |
| Existing deployment/CSP or browser constraints block early initialization | Medium | Verify actual CSP and first paint; do not weaken security policy. Treat missing evidence as a release blocker. |

Existing hardcoded dark styles, dark-only shell test, public SSR browser shims
and unrelated Workshop error-context concerns are technical debt. Change only
theme-coupled code and tests; investigate unrelated failures separately rather
than expanding this feature.

## Dependency-ordered implementation roadmap

The roadmap below records the approved sequence; the implementation evidence
and remaining gates are recorded in the status and delivery notes above and
below.

1. **Baseline and design gate:** capture current dark public, Range and
   Workshop surfaces and representative login/error states. Confirm the
   proposed palette against real content and brand assets; record actual
   viewport and contrast evidence. No layout redesign.
2. **Shared contract and global asset:** implement the dedicated shared
   light/dark semantic theme, CSS subpath, opt-in preference resolver,
   subscription and accessible control in shared layout, with focused tests for
   System, explicit overrides, external storage changes, invalid values and
   unavailable storage. Build shared packages in existing dependency order;
   verify the CSS export is present and other consumers are unaffected.
3. **Initial paint and SSR:** wire the minimal bootstrap and document-level
   resolved theme into each entry point. Align native controls and public
   Bootstrap state; establish client/server snapshot behavior. Validate direct
   SSR, hydration, first paint and no-JavaScript readability before route
   styling proceeds.
4. **Public client:** import the global theme, map its existing variables to
   shared roles and apply targeted style changes to shell, home, forms, content,
   overlays, QuickLinks, error and loading states. Add selector and ensure
   outside-root widgets use the document theme.
   Preserve existing yellow/dark look and site behavior. Gate on focused tests,
   client and SSR builds, and rendered evidence.
5. **Range and Workshop:** import the same global theme and integrate the
   contract into each shell, all login/registration states and local styles.
   Map existing variables to the shared roles and record justified exceptions.
   Preserve booking/admin workflows, Workshop localization, status semantics,
   QR and existing preferences. Gate each independently on focused tests,
   checked builds and browser evidence.
6. **Integration and documentation:** run target-app regression gates and
   verify additive shared-package consumption, PWA/update paths and delivery
   asset inclusion. Update the named documentation and this handoff with
   actual evidence. An independent post-validation review is recommended
   because the change spans shared code, two SSR surfaces and three visual
   systems; the user decides whether to request it.

**Stop boundary:** implementation ends after these three clients and their
direct dependencies pass the stated gates. Cross-origin synchronization,
account settings, new themes, additional clients and unrelated cleanup require
separate approval.

## Testing strategy and acceptance

- **Unit/contract:** missing, valid and invalid preferences; explicit and
  System resolution; device changes in System versus fixed override; reset;
  cross-tab updates; listener cleanup; storage read/write failure; early
  bootstrap parity. Verify all three clients resolve common roles from the same
  shared asset, including after reload and a theme switch; test documented
  exceptions separately. No fabricated success when persistence fails.
- **Components:** keyboard-operable labeled three-way selector, announced
  selected value, localized text, focus visibility, 44px coarse-pointer targets
  and meaningful persistence-failure feedback. Test public normal header,
  Range login/authenticated shell, Workshop login/register/authenticated shell.
- **SSR/integration:** direct public article and Range routes with device light
  and dark plus saved overrides; deterministic SSR, correct first paint,
  hydration without warnings, route metadata and navigation unchanged.
  Check no-JavaScript readability and bootstrap failure. Workshop remains
  client-rendered; test its initial paint and route-error path.
- **Visual/accessibility:** at least 320px and 390px mobile, 768px tablet,
  1440px desktop, 667x375 landscape and 200% zoom. Capture both themes for
  public home/article/QuickLinks/consent, Range login/booking/admin, Workshop
  login/operational workspace/form/dialog and loading/error/empty states.
  Verify no horizontal overflow, responsive controls, dark baseline, contrast
  thresholds above, reduced motion and keyboard navigation. Test text on media
  and native inputs as rendered rather than relying on token values alone.
- **Functional/security:** switching does not reset consent, login, booking,
  account state, existing preferences, route state or QR contents. Test blocked
  storage and a real deployed-equivalent CSP without weakening it.
- **Build/regression:** run the smallest owning Vitest selections first, then
  each target app's existing tests and checked build; include client and SSR
  builds for public and Range, and Workshop's checked build. Build shared
  packages in existing dependency order and verify affected layout consumers.
  Escalate unrelated baseline failures explicitly rather than suppressing them.
- **End-to-end/operations:** exercise same-origin tab changes, independent
  origins, reload, PWA update/offline behavior and representative deployed
  artifacts when authorized. CI and screenshots do not prove a deployed
  experience.

## Migration, rollout and rollback

No MongoDB, SQLite, API, authentication or server-cache migration is needed.
Existing users without a preference intentionally enter System mode. The local
key is additive and contains no account information; malformed values use the
documented fallback. Existing releases ignore the new key.

Build shared dependencies before each client; release each client through its
own existing pipeline after its gates, without requiring simultaneous deployment.
Public and Range client/SSR artifacts must be paired. Existing Range public
exposure and Workshop staging/activation gates remain unchanged; this feature
does not authorize release. Monitor client errors, unreadable states and asset
loading after any approved rollout.

Rollback the affected client and paired SSR artifacts to their prior version;
the leftover preference is harmless and must not be used to clear unrelated
storage or consent. Service-worker/CDN caches can retain old assets, so verify
update and rollback behavior on an existing installation. No account data or
server migration needs reversal.

## Out-of-scope review notes

Implementation follows the plan's origin-local preference and Slate & Mist
direction. Cross-origin/account synchronization remains out of scope. The
semantic token contrast calculation passed, but rendered/browser contrast and
layout still require a real-browser review.

## User review notes

Implementation was requested against this plan; no additional review notes
were provided.

## Real implementation notes and residual risks

### Delivered behavior

- Shared `@morwalpiz/layout` now owns the versioned
  `morwalpiz.appearance.v1` preference, System/Light/Dark resolution, system and
  cross-tab subscriptions, document theme attributes, and an accessible native
  selector. Invalid values use System with a generic diagnostic. Storage
  failures preserve the in-page choice and expose a localized status message.
- Shared `src/styles/theme.scss` owns the light/dark semantic palettes. The
  opt-in `./theme` package subpath is typed without changing the existing
  `./styles` export or nonparticipating consumers. Apps map their existing
  variables to shared roles.
- Identical synchronous bootstraps run before theme-sensitive styles in all
  three apps. The document `data-appearance-theme` and Bootstrap
  `data-bs-theme` use the resolved theme. SSR snapshots remain deterministic;
  no browser preference is read on the server.
- Public navigation, Range shell/login, and Workshop shell/login/registration
  expose the selector. Workshop labels are localized. Existing routes,
  authentication, account data, API contracts, consent, and QR output are
  unchanged. Static PWA/manifest colors remain unchanged.

### Changed files

- Shared layout: `frontend/fe-packages/layout/package.json`,
  `src/index.ts`, `src/appearance.ts`, `src/theme.ts`,
  `src/components/AppearanceSelector.tsx`, and `src/styles/theme.scss`.
- Public client: `index.html`, `public/appearance-bootstrap.js`,
  `src/main.tsx`, `src/main.scss`, `src/layouts/title-header.tsx`,
  `src/layouts/title-header.scss`, `src/layouts/footer.tsx`,
  `src/layouts/footer.scss`, `src/routes/layout/root.tsx`,
  `src/routes/layout/root.test.tsx`, `src/routes/home/style.scss`,
  `src/routes/accessories/style.scss`, `src/routes/calendar/style.scss`,
  and `src/appearance.test.tsx`.
- Shooting Range: `index.html`, `public/appearance-bootstrap.js`,
  `src/main.tsx`, `src/App.tsx`, `src/App.test.tsx`, and `src/styles.css`.
- Shooter Workshop: `index.html`, `public/appearance-bootstrap.js`,
  `src/main.tsx`, `src/layouts/AppShell.tsx`, account login/registration
  components, `src/main.scss`, `src/i18n.tsx`, and the account registration
  test.
- Documentation: public `DESIGN.md` and Impeccable sidecar, public experience,
  frontend architecture, Range architecture, Workshop architecture, and this
  handoff. No new ADR or migration was needed.

### Validation performed

- `yarn --cwd frontend workspace @morwalpiz/layout build` — passed.
- `yarn --cwd frontend workspace morwalpizvideo.client test` — 41 tests passed.
- `yarn --cwd frontend workspace shooting-range.client test` — 36 tests passed.
- `yarn --cwd frontend workspace shooter-workshop.client test` — 48 tests passed.
- All three workspace `build` commands passed; public and Range `build:ssr`
  commands passed.
- Focused tests cover system changes, explicit overrides, cross-tab storage,
  invalid values, unavailable storage, bootstrap parity, and selector presence.
- WCAG relative-luminance calculation on shared semantic text/status pairs:
  dark minimum 5.10:1; light minimum 5.48:1. This is token-level evidence,
  not a substitute for rendered-state measurement.

### Residual risks and release gates

- The Impeccable screenshot command was attempted for all three local previews,
  but no Chrome/Chromium/Edge/Brave browser is installed. No visual capture,
  rendered overflow/contrast check, or runtime hydration check is claimed.
- Deployed Content-Security-Policy compatibility is unknown; no policy was
  weakened. Confirm the production policy before release.
- Public PWA update/offline behavior and Workshop service-worker update/
  installation behavior remain unverified. No deployed artifact or device
  testing was performed.
- Public and Range builds emitted the existing non-module `env-config.js`
  bundling warning. Public also emitted a chunk-size warning and stale
  Browserslist data notice. Build commands regenerated their usual output;
  generated files were not edited by hand.
