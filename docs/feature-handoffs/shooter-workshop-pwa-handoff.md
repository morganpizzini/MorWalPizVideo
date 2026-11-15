# Shooter Workshop PWA Installation Handoff

## Outcome

Make `frontend/shooter-workshop.client` installable as a Progressive Web App and
offer a user-driven installation invitation equivalent to the BackOffice Admin SPA.

## Delivered Implementation

Implemented on 2026-11-14:

- Added the root-scoped [Workshop manifest](../../frontend/shooter-workshop.client/public/manifest.webmanifest),
  dedicated [192 icon](../../frontend/shooter-workshop.client/public/icon-192.svg),
  [512 icon](../../frontend/shooter-workshop.client/public/icon-512.svg), and
  lifecycle-only [service worker](../../frontend/shooter-workshop.client/public/workshop-sw.js).
- Linked the manifest and favicon from
  [index.html](../../frontend/shooter-workshop.client/index.html).
- Added the browser-event store and hook in
  [installPrompt.ts](../../frontend/shooter-workshop.client/src/services/installPrompt.ts),
  imported before render, with dismissal persistence, standalone detection,
  explicit prompt invocation, and installed-state handling.
- Registered the worker non-blockingly from
  [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx).
- Added the localized [install invitation](../../frontend/shooter-workshop.client/src/components/InstallPrompt.tsx)
  and authenticated-shell/header integration in
  [AppShell.tsx](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx).
- Added Workshop-local Italian translations and responsive styling.
- Added focused install-state tests in
  [installPrompt.test.ts](../../frontend/shooter-workshop.client/src/services/installPrompt.test.ts).
- Updated [frontend architecture](../architecture/frontend.md) and the
  [Workshop specification](../architecture/shooter-workshop.md) to record the
  shipped PWA and no-cache boundary.

Validation completed:

- `cd frontend && yarn workspace shooter-workshop.client test --run src/services/installPrompt.test.ts`
  — passed, 4 tests.
- `cd frontend && yarn workspace shooter-workshop.client test --passWithNoTests=false`
  — passed, 7 files and 17 tests.
- `cd frontend && yarn workspace shooter-workshop.client build`
  — passed; Vite emitted the manifest, icons, and worker in `dist`.

The generated `dist` and TypeScript build artifacts from validation were removed or
restored; unrelated pre-existing worktree edits were preserved.

Assumptions:

- The requested application is the tagged Shooter Workshop client.
- The installed product name is **MorWalPiz Workshop**, with **Workshop** as the
  short name.
- This is a refinement of the existing Workshop `Operate` surface, not a redesign.
- Installation support does not introduce offline Workshop data, API-response
  caching, background synchronization, or Web Push.
- The invitation remains inside the authenticated Workshop shell, while browser
  installability events are captured before authentication and router rendering.

## Current Evidence

- [Workshop main](../../frontend/shooter-workshop.client/src/main.tsx) mounts
  localization, public-account authentication, feedback, and the router, but does
  not capture `beforeinstallprompt` or register a service worker.
- [Workshop index](../../frontend/shooter-workshop.client/index.html) already sets
  the Workshop theme color, but has no manifest or application icon links.
- [Workshop AppShell](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx)
  owns the authenticated header and operational content boundary. Anonymous login
  routes sit outside this shell.
- [Workshop styles](../../frontend/shooter-workshop.client/src/main.scss) define the
  incumbent black, white, and yellow visual system, focus treatment, quiet and
  primary buttons, and responsive breakpoints.
- [Workshop localization](../../frontend/shooter-workshop.client/src/i18n.tsx)
  owns Italian and English presentation strings. New invitation and install-control
  text must use this provider.
- [Workshop package manifest](../../frontend/shooter-workshop.client/package.json)
  has React, Vitest, Testing Library, and jsdom available. It has no PWA plugin,
  React Bootstrap, or icon dependency.
- [Workshop Vitest configuration](../../frontend/shooter-workshop.client/vitest.config.ts)
  discovers colocated `src/**/*.test.{ts,tsx}` tests in jsdom.
- [Workshop test setup](../../frontend/shooter-workshop.client/src/test/setup.ts)
  provides deterministic local storage for browser-state tests.
- [Workshop Nginx configuration](../../frontend/shooter-workshop.client/nginx.conf)
  serves built static files and falls back to `index.html` for client routes.
- [Workshop Dockerfile](../../frontend/shooter-workshop.client/Dockerfile) copies the
  complete Vite `dist` directory into Nginx, so Vite `public` assets require no
  container-specific copy step.
- [Workshop deployment workflow](../../.github/workflows/deploy-shooter-workshop-client.yml)
  already tests and builds the owning workspace before image publication.
- [BackOffice main](../../frontend/back-office-spa/src/main.tsx) imports its install
  prompt store before router bootstrap and directly registers its same-origin worker.
- [BackOffice install store](../../frontend/back-office-spa/src/services/installPrompt.ts)
  captures `beforeinstallprompt`, tracks standalone installation, persists a dismissed
  invitation, handles `appinstalled`, and exposes state through
  `useSyncExternalStore`.
- [BackOffice install component](../../frontend/back-office-spa/src/components/InstallPrompt.tsx)
  renders only when installation is available, not dismissed, and not already
  installed.
- [BackOffice authenticated layout](../../frontend/back-office-spa/src/layouts/PrimaryLayout.tsx)
  keeps the invitation out of anonymous routes.
- [BackOffice header](../../frontend/back-office-spa/src/layouts/Header.tsx) lets a
  user deliberately reopen a dismissed invitation while the deferred browser prompt
  remains available.
- [BackOffice manifest](../../frontend/back-office-spa/public/manifest.webmanifest)
  uses root scope/start URL, standalone display, theme colors, and 192/512 icons.
- [BackOffice worker](../../frontend/back-office-spa/public/push-sw.js) is
  BackOffice-specific Web Push infrastructure and must not be copied into Workshop.
- [BackOffice install tests](../../frontend/back-office-spa/src/services/installPrompt.test.ts)
  cover event capture, explicit prompting, dismissal/reopen, and installation.
- [Frontend architecture](../architecture/frontend.md) documents that BackOffice's
  installable shell does not add offline or authenticated-response caching and that
  Workshop is a separate private deployment unit at the root of
  `https://workshop.morwalpiz.com`.
- [Workshop specification](../architecture/shooter-workshop.md) fixes the existing
  MorWalPiz black/white/yellow identity and separate Workshop application boundary.

## Proposed Design

### Installability shell

Add a Workshop-owned web app manifest at the root scope. It identifies MorWalPiz
Workshop, uses the existing `#101010` background/theme color, requests standalone
display, and references dedicated 192 and 512 application icons. Link the manifest,
favicon, and existing theme color from the HTML document.

Add a Workshop-owned service worker under `public`. Register it directly from the
Workshop bootstrap with root scope. The worker exists only to establish the
installable application shell and lifecycle; it must not intercept fetches, populate
caches, store API responses, implement background synchronization, or handle push.
This preserves the authenticated public-account transport and avoids stale or
cross-account data.

Do not add `vite-plugin-pwa` or another dependency. The BackOffice precedent is a
small explicit manifest, worker registration, and browser-event store, and the
Workshop does not need generated precaching.

### Installation state

Create a Workshop-local install prompt service following the BackOffice state model:

- import it before router rendering so the one-shot `beforeinstallprompt` event is
  not missed;
- prevent the browser's automatic mini-infobar and retain the event until an explicit
  user action;
- expose `canInstall`, `dismissed`, and `installed`;
- detect standalone display mode and iOS standalone mode;
- persist only the invitation dismissal under a Workshop-specific versioned local
  storage key;
- clear availability and mark installation after `appinstalled`;
- consume a retained prompt at most once;
- return the browser's accepted/dismissed outcome without pretending an unavailable
  prompt succeeded;
- tolerate unavailable browser storage without breaking rendering.

Keep this service local to Workshop. BackOffice and Workshop require different
storage ownership, application identity, shell placement, and future worker
capabilities; moving the service to a shared package would create coupling without
two identical consumers.

### Invitation and reopen behavior

Add a localized Workshop install invitation inside the authenticated `AppShell`
content, before the active route outlet. It appears only when:

- the browser supplied a deferred install prompt;
- the user has not dismissed the invitation; and
- the application is not running standalone or known to be installed.

The invitation contains a concise installation benefit, a primary install action,
and a quiet “not now” action. The install action is the only place that invokes the
browser prompt. Dismissal hides the invitation across browser sessions.

Add a compact localized “Install app” or “Show install invitation” control to the
authenticated header when the browser prompt is still available and the app is not
installed. Activating it clears only the invitation's dismissed presentation state;
it does not invoke the browser prompt directly. This preserves the BackOffice reopen
procedure and gives users a recoverable path after “not now.”

The component must use existing Workshop button classes and CSS variables rather than
adding React Bootstrap or an icon library. At narrow widths, actions wrap or stack
without competing with the existing horizontally scrollable navigation.

### Browser behavior

Chromium browsers that emit `beforeinstallprompt` receive the in-app invitation.
Browsers that do not emit it, including current iOS Safari, do not receive a broken or
nonfunctional custom prompt; their native browser installation affordance remains
available through the manifest. Standalone detection suppresses the invitation after
launch from an installed application.

Service-worker registration failure is logged explicitly but does not block the
authenticated application. Installation is enhancement-only; account restoration,
routing, and domain operations retain their current behavior.

### Impeccable planning lens

- Surface: authenticated Workshop application shell.
- Mode: `Operate`.
- Direction: refinement, preserving the incumbent black/white/yellow identity and
  existing responsive shell.
- Existing context: no Workshop-local `PRODUCT.md` or `DESIGN.md` is present; the
  shipped styles and Workshop architecture specification are the incumbent truth.
- Narrow workflow for implementation: `harden` the installation invitation for
  browser capability differences, localization, responsive layout, keyboard focus,
  dismissal, and installed-state edge cases.
- Minimum visual evidence: authenticated desktop and narrow-mobile captures with the
  invitation visible, dismissed/reopen state exercised, keyboard focus visible on
  both actions, and Italian plus English copy checked for wrapping.

## Impacted Projects

- **`frontend/shooter-workshop.client`** — manifest, icons, service worker,
  installation state, localized invitation, shell integration, styling, and tests.
- **`frontend/back-office-spa`** — verify-only reference implementation; no changes.
- **Shared frontend packages** — no change. The behavior remains application-owned.
- **MorWalPizVideo.ServerAPI** — no change. Authentication, CSRF, CORS, and API
  behavior are unaffected.
- **MorWalPizVideo.AppHost** — no change. Existing Workshop origin and development
  hosting remain valid.
- **Deployment image/workflow** — verify-only. Existing Vite public-asset copying and
  test/build gates should carry the new files.
- **Backend and desktop projects** — not applicable.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/index.html` — link the manifest and icon while
  retaining the existing theme color and language.
- `frontend/shooter-workshop.client/src/main.tsx` — load install event handling early
  and register the Workshop worker without blocking render.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` — place the invitation
  inside the authenticated shell and expose the deliberate reopen action.
- `frontend/shooter-workshop.client/src/i18n.tsx` — add Italian translations for all
  English source strings used by the installation UI.
- `frontend/shooter-workshop.client/src/main.scss` — style the invitation and
  responsive actions using existing tokens and focus behavior.
- `docs/architecture/frontend.md` — document Workshop installability and the explicit
  no-offline/no-authenticated-cache boundary.
- `docs/architecture/shooter-workshop.md` — record PWA installation as implemented
  application-shell behavior after delivery.
- `docs/feature-handoffs/shooter-workshop-pwa-handoff.md` — update after
  implementation with actual files, validation results, and residual risks.

### Add

- `frontend/shooter-workshop.client/public/manifest.webmanifest` — Workshop identity,
  root scope, standalone display, colors, and icon metadata.
- `frontend/shooter-workshop.client/public/icon-192.svg` — Workshop-owned small
  application icon using the incumbent identity.
- `frontend/shooter-workshop.client/public/icon-512.svg` — Workshop-owned large
  application icon using the incumbent identity.
- `frontend/shooter-workshop.client/public/workshop-sw.js` — same-origin lifecycle
  worker with no fetch, cache, push, or synchronization behavior.
- `frontend/shooter-workshop.client/src/services/installPrompt.ts` — browser event
  store and React hook adapted to Workshop ownership.
- `frontend/shooter-workshop.client/src/services/installPrompt.test.ts` — state and
  browser-event behavior tests.
- `frontend/shooter-workshop.client/src/components/InstallPrompt.tsx` — localized
  invitation component.
- `frontend/shooter-workshop.client/src/components/InstallPrompt.test.tsx` — rendering,
  install, dismissal, and localization integration tests.

### Generated/Migration

None. Vite build output remains generated and must not be committed. No database
migration or backfill is involved.

### Documentation

- `docs/architecture/frontend.md`
- `docs/architecture/shooter-workshop.md`
- `docs/feature-handoffs/shooter-workshop-pwa-handoff.md`

### Verify Only

- `frontend/back-office-spa/src/main.tsx`
- `frontend/back-office-spa/src/services/installPrompt.ts`
- `frontend/back-office-spa/src/services/installPrompt.test.ts`
- `frontend/back-office-spa/src/components/InstallPrompt.tsx`
- `frontend/back-office-spa/src/layouts/PrimaryLayout.tsx`
- `frontend/back-office-spa/src/layouts/Header.tsx`
- `frontend/back-office-spa/public/manifest.webmanifest`
- `frontend/back-office-spa/public/push-sw.js`
- `frontend/shooter-workshop.client/package.json`
- `frontend/shooter-workshop.client/vitest.config.ts`
- `frontend/shooter-workshop.client/src/test/setup.ts`
- `frontend/shooter-workshop.client/nginx.conf`
- `frontend/shooter-workshop.client/Dockerfile`
- `.github/workflows/deploy-shooter-workshop-client.yml`

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Not applicable; no endpoint or transport change. |
| Frontend/UI | Add install invitation and header reopen affordance inside the authenticated Workshop shell. |
| Database/Data | No server persistence. One versioned, non-sensitive local dismissal flag is added. |
| Contracts/Models | No API or shared TypeScript contract change. Browser event typing remains local. |
| Authentication/Authorization | No policy change. Capture happens before render, but invitation rendering remains authenticated. |
| Validation | Browser capability and retained-prompt availability guard every install action. |
| Cache | No Cache Storage usage and no fetch interception. Authenticated/API responses remain network-owned. |
| Configuration/Secrets | No setting or secret is added. Manifest and worker are same-origin static assets. |
| Logging/Telemetry | Explicit console error only for worker registration failure; no new telemetry contract. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Existing HTTPS production host, Vite build, container copy, and Nginx serving must expose the manifest, icons, and worker at root paths. |
| Backward Compatibility | Additive enhancement. Unsupported browsers continue using the current web application with no invitation. |

## Reuse Opportunities

- Reuse the BackOffice install-state lifecycle and its unit-test scenario structure.
- Reuse Workshop's `I18nProvider` and `useI18n` rather than adding static bilingual
  markup.
- Reuse Workshop's primary/quiet button classes, panel geometry, CSS variables, and
  responsive breakpoints.
- Reuse `AppShell` as the authenticated presentation boundary.
- Reuse the existing Vite `public` asset pipeline, Docker `dist` copy, Nginx static
  root, workspace build, and CI test gate.

Do not reuse:

- the BackOffice storage key or product copy;
- the BackOffice Push worker;
- React Bootstrap or Lucide dependencies that Workshop does not carry;
- shared package ownership for an app-specific install shell;
- offline caching patterns from other applications.

## Risks

| Rank | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | A worker that caches account or API responses could expose stale or cross-account Workshop data. | Keep the worker free of fetch/cache handlers and document the boundary. | Worker source has no request interception or Cache Storage access; online auth/API behavior is unchanged. |
| Medium | `beforeinstallprompt` is one-shot and can be missed if listeners attach after bootstrap. | Import the event store before asynchronous or router bootstrap work. | A pre-render dispatched event produces an available invitation in tests/browser evidence. |
| Medium | The SPA fallback could disguise missing root assets by returning HTML. | Verify response content types and successful root requests for manifest, icons, and worker in the built container. | Each asset returns its intended content and the browser Application panel reports a valid manifest and active worker. |
| Medium | Installed-state or dismissal bugs could repeatedly nag users or remove the recoverable install path. | Preserve separate `dismissed`, `canInstall`, and `installed` states with a deliberate reopen action. | Tests cover dismiss, reload state, reopen, accepted prompt, dismissed browser choice, and `appinstalled`. |
| Medium | Browser differences, especially iOS, could expose a button that cannot install. | Render only after a real `beforeinstallprompt`; rely on native browser UI otherwise. | No custom invitation appears when the event is absent; standalone mode suppresses it. |
| Low | Long Italian or English copy could overflow the dense header/mobile shell. | Keep the full invitation in content and the header affordance compact; wrap actions at narrow widths. | Desktop and mobile visual evidence in both cultures has no clipping or horizontal page overflow. |
| Low | Worker registration failure could prevent application startup if awaited as a hard dependency. | Register as an enhancement and log rejection without failing render. | A rejected registration still renders login/authenticated routes and emits an explicit error. |
| Low | Icon safe areas could crop the brand in maskable presentation. | Keep the brand mark within the maskable safe zone and inspect installed/icon previews. | Chromium manifest inspection shows legible 192 and 512 icons in normal and maskable previews. |

## Technical Debt

- BackOffice's install prompt service is structurally reusable but application-local.
  This does not block the change. Duplicate the small ownership-specific mechanism
  now; consider shared extraction only after a third consumer proves identical
  semantics.
- BackOffice uses its Push worker to satisfy its service-worker requirement. Workshop
  has no Push scope, so copying that worker would create misleading dead capability.
  Isolate with a Workshop-specific lifecycle worker.
- The repository has no dedicated cross-browser PWA end-to-end suite. Unit/component
  tests cannot prove production installability. Defer general PWA automation and
  require focused manual browser evidence for this delivery.
- No Workshop-local `PRODUCT.md` or `DESIGN.md` exists. This does not block a narrow
  refinement because the architecture specification and shipped UI define the
  incumbent direction.

## Implementation Roadmap

1. **Establish static installability assets.** Add the Workshop manifest, dedicated
   icons, and non-caching worker under the workspace `public` directory. Link manifest
   and icon metadata from the HTML document. Gate: built output contains all assets at
   root paths and manifest metadata resolves without fallback HTML.
2. **Capture browser installation state before rendering.** Add the Workshop-local
   external store/hook and import it at bootstrap time. Register the same-origin
   Workshop worker as a non-blocking enhancement with explicit failure logging. Gate:
   focused service tests prove event capture, one-shot prompt use, dismissal/reopen,
   standalone detection, and `appinstalled`.
3. **Integrate the localized authenticated invitation.** Add the component, Italian
   translations, AppShell placement, and compact header reopen affordance. Preserve
   anonymous route behavior and existing account restoration. Gate: component tests
   prove visibility rules and user actions in both cultures.
4. **Harden the responsive presentation.** Add only Workshop-local styles using
   existing tokens and button patterns. Check keyboard focus, semantic heading/order,
   action wrapping, and screen widths around the existing 900px and 560px breakpoints.
   Gate: desktop and mobile visual evidence shows no overlap or overflow.
5. **Validate the production package boundary.** Confirm the built container serves
   the manifest, worker, and icons directly; confirm a secure production-like origin
   reports installability; confirm authenticated API responses are absent from Cache
   Storage. Gate: browser Application-panel evidence and an install/launch/uninstall
   smoke cycle.
6. **Reconcile documentation.** Update the frontend architecture, Workshop
   specification status, and this handoff with actual implementation and validation.
   Stop after PWA installation support; offline domain data, Push, background sync,
   and broader shell redesign remain outside scope.

An independent post-validation review is proportionate because worker scope and
authenticated cache isolation are security-sensitive despite the small UI change. The
review should focus on cache absence, shell/auth boundaries, browser fallback, and
deployment asset serving rather than general Workshop functionality.

## Testing Strategy

### Unit

Owning workspace: `frontend/shooter-workshop.client`.

- Capture `beforeinstallprompt`, prevent default browser behavior, retain the event,
  and notify subscribers.
- Invoke the browser prompt only through explicit `install`.
- Consume a prompt once and return accepted or dismissed outcome accurately.
- Persist dismissal using the Workshop-specific key.
- Reopen a dismissed invitation without invoking installation.
- Clear availability and mark installed after `appinstalled`.
- Initialize installed state from standalone display mode and iOS standalone mode.
- Continue safely when local storage reads or writes throw.
- Return an unavailable result when no deferred prompt exists.

### Component

- Hide the invitation when unavailable, dismissed, or installed.
- Render localized title, body, install action, and “not now” action.
- Invoke install through the primary action and dismissal through the quiet action.
- Expose the header reopen affordance only while a browser prompt remains available.
- Keep anonymous login routes free of the invitation.

### Integration/Behavior

- Existing authentication restoration and protected route redirects remain unchanged.
- Existing Workshop route and feedback tests remain green.
- The owning workspace's complete Vitest suite passes with its nonempty-suite gate.
- The owning workspace's TypeScript/Vite production build passes.

### Contract

- Manifest `start_url` and `scope` remain `/`, matching the canonical deployment.
- Icon paths and worker registration use same-origin root URLs.
- No API, DTO, service package, cookie, CSRF, or CORS contract changes.

### Security

- Worker has no fetch interception, caches, push handling, or background sync.
- Cache Storage contains no authenticated HTML or API data after normal Workshop use.
- Dismissal persistence contains no account identifier or sensitive domain data.
- Switching or signing out of an account does not expose cached Workshop records.

### End-to-End/Manual Browser

- On an HTTPS Chromium profile with the application not installed, authenticate,
  observe the invitation, dismiss it, reopen it from the header, accept installation,
  launch standalone, and verify the invitation is absent.
- Repeat the visible states at desktop and narrow mobile viewport widths in Italian
  and English.
- Inspect manifest warnings, icon previews, service-worker scope, and standalone
  display in browser developer tools.
- Verify unsupported/no-event behavior has no custom install control and no runtime
  error.
- Verify a simulated worker registration rejection does not block the application.

## Migration And Rollback

- **MongoDB/SQLite:** no migration, backfill, or data compatibility work.
- **API contracts:** no migration or compatibility window.
- **Authentication:** no cookie, CSRF, CORS, or authorization change.
- **Cache:** no application cache is introduced. Browser HTTP caching remains owned
  by the existing static host.
- **Configuration:** no new environment variable or secret.
- **Deployment order:** deploy the updated static client as one image so HTML,
  manifest, icons, worker, and UI arrive atomically.
- **Monitoring:** observe client-side worker registration errors and browser
  installability warnings during release verification.
- **Rollback:** redeploy the prior client image. Already installed clients may retain
  the previous worker registration until browser update/unregister behavior completes,
  but the worker owns no cache or background behavior, so rollback has no data
  recovery step.
- **Recovery limit:** removing the invitation cannot programmatically uninstall an
  application users already installed; users retain normal browser/OS uninstall
  controls.

## Open Questions

None identified. The application identity, root deployment scope, authenticated-shell
placement, localization ownership, no-cache boundary, and validation path are resolved
from current source and architecture.
