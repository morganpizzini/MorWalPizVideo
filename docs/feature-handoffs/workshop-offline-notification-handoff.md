# Workshop offline preparation notifications

Status: implemented and validated 2026-11-25. Browser screenshot verification
remains outstanding because no authenticated local Workshop runtime was
available.

## Outcome

Move the offline-readiness status and its preparation and manual-sync controls
from the all-page shell into the existing dashboard hero; keep its layout stable
when preparation completes. Show each preparation outcome in an
appearing/disappearing notification tile, and disable the preparation button
with a visible loader for the entire request and router revalidation.

Assumptions: “option” means the existing preparation control, not a new
user-configurable placement preference. Automatic foreground sync remains active
on all authenticated routes. Keep urgent blocked-command, sync-session and
sign-out errors visible across routes. Use a short-lived success notification
and manually dismissible failure and storage-persistence warnings.

## Current Evidence

- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` shares one fetcher
  between prepare and sync. It disables both controls whenever that fetcher is
  not idle, but shows “Preparing…” only while submitting. Successful,
  non-persistent, and error results are rendered inside the wrapping
  `.offline-status` flex container and change its layout. The same container
  also owns blocked-command, sync-authentication, and sign-out errors.
- `frontend/shooter-workshop.client/src/routes/offlineAction.ts` prepares
  account-partitioned datasets and returns either a success with
  `preparedAt`/`storagePersisted`, or an error. Sync has different results;
  its 401 result triggers the existing session refresh in `AppShell`.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx`
  renders the hero immediately, ahead of the deferred dashboard datasets; its
  existing shooting-session link is part of the hero. The dashboard is the
  index child of the authenticated shell in `src/routes/config.tsx`. The shell
  currently renders an `Outlet` without context, and its offline fetcher and
  sync effects remain mounted while navigating between child routes.
- `frontend/back-office-spa/src/components/ToastNotification/` already provides
  a React Bootstrap toast provider and hook, with configurable variant,
  autohide and delay. It is application-local, not an export of
  `@morwalpiz/layout`. Many BackOffice callers use its existing API and its
  provider tests check stable context identity and navigation.
- `frontend/fe-packages/layout/package.json` exports shared components and
  styles but has no notification export or React Bootstrap dependency.
  Workshop already imports Bootstrap in `src/main.scss`; BackOffice already
  depends on React Bootstrap. Workshop has a reusable, reduced-motion-aware
  `.loading-spinner` in `src/components/States.tsx` and `src/main.scss`.
- `docs/architecture/frontend.md` and `docs/architecture/shooter-workshop.md`
  describe the client-owned offline workflow and the persistence warning.
  Workshop has no local `PRODUCT.md` or `DESIGN.md`; the current shell, theme,
  and supplied screenshot are the visual authority.

## Proposed Design

Promote the existing BackOffice notification provider and hook into an opt-in
shared-layout notification entry point, retaining its current API and Bootstrap
presentation. Keep the existing BackOffice import paths as compatibility
re-exports, rather than editing its many consumers. Declare the actual shared
runtime and peer dependencies and stylesheet prerequisite; do not make other
SPAs initialize a provider or import styles implicitly. Give the Workshop
provider a viewport-fixed, responsive notification container that does not
participate in document flow. Preserve BackOffice's existing default placement.
Use semantic success, warning, and error variants with text as well as color;
provide a localized close label. Avoid timestamp-only IDs that can collide when
two notifications are created in the same millisecond.

In Workshop, track each actual prepare submission through the shared fetcher
lifecycle and emit one notification for its final result, including repeated
identical results and React Strict Mode. Do not treat retained fetcher data,
rerenders, sync results, or automatic reconnect sync as new preparation
outcomes. Process a completed preparation before any subsequent automatic
sync can replace its data. Preserve existing sync authentication refresh and
visible blocked-command and sign-out errors.

Keep the existing offline state subscription, connectivity listeners, shared
fetcher, and automatic sync and notification effects in `AppShell`, so leaving
the dashboard cannot cancel an in-progress preparation or prevent its result
from being reported. Expose a narrow typed offline view model and callbacks to
the dashboard through the existing router outlet context. The dashboard hero
renders the offline readiness/workspace text, prepared state, pending count,
prepare button and conditional manual sync button, preserving their current
online/offline conditions. Keep the hero's existing copy and primary action.
Remove the ordinary readiness strip entirely from other pages. Render
blocked-command details, sync-session errors and sign-out errors as conditional
shell-level alerts on every route, not as an always-present readiness strip;
never hide an actionable conflict or session failure when leaving the dashboard.
No new preference, route, or second fetcher is required.

During prepare, show the existing spinner and localized “Preparing…” label,
with the control disabled until the fetcher is idle after revalidation. Both
actions remain disabled while either intent is active. Reserve sufficient button
space for both labels and the spinner at the supported locales and viewport
sizes so the controls do not jump. A storage-persistence denial remains a
warning, not an unqualified success.

This is a **refinement**, not a redesign: use Impeccable's **Operate** mode and
the narrow **harden** workflow during implementation. Preserve the existing
dark/light appearance and screen hierarchy. The context launcher and workflow
were not run during this architecture-only analysis.

## Impacted Projects

- `@morwalpiz/layout`: shared opt-in toast entry point and dependency metadata.
- `back-office-spa`: compatibility adapter; existing toast callers and behavior
  remain intact.
- `shooter-workshop.client`: provider placement, preparation feedback, spinner,
  dashboard hero, shell-to-dashboard state ownership, styling, localization,
  and focused UI tests.
- Public, Shooting Range, Shooting ITA, and frozen shop clients already consume
  the layout package: verify the additive package export and dependency
  compatibility; do not migrate them or change their behavior. Other SPAs may
  opt in later, installing the shared package and its documented prerequisites.
- Backend, database, deployment topology, and standalone tools: not affected.

## Impacted Files

### Modify

- `frontend/fe-packages/layout/package.json`: opt-in notification export and
  React Bootstrap dependency declaration.
- `frontend/fe-packages/layout/src/notifications.ts`: public provider, hook,
  context and types export.
- `frontend/fe-packages/layout/src/components/ToastNotification/index.tsx`
  and `ToastContext.tsx`: extracted shared provider, stable toast IDs,
  configurable placement, localized close label and accessible status output.
- `frontend/back-office-spa/src/components/ToastNotification/index.tsx` and
  `ToastContext.tsx`: compatibility re-exports preserving existing imports,
  public types, defaults, and stable context behavior.
- `frontend/shooter-workshop.client/src/main.tsx`: mount the shared provider
  within localization scope, with a localized dismissal label.
- `frontend/shooter-workshop.client/src/components/feedback/WorkshopToastProvider.tsx`:
  bind the common notification provider to Workshop's active culture and
  viewport styling.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx`: prepare-specific
  pending/result lifecycle, notification emission, typed outlet context and
  global conditional alerts; preserve sync behavior.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx`:
  consume the outlet context and place readiness and controls inside the
  existing hero, outside its deferred data section.
- `frontend/shooter-workshop.client/src/main.scss`: hero integration, stable
  action sizing and non-flow notification positioning, including small viewports.
- `frontend/shooter-workshop.client/src/i18n.tsx`: any new accessible
  notification labels in both supported cultures.
- `docs/architecture/frontend.md` and `docs/architecture/shooter-workshop.md`:
  document the shared opt-in provider and dashboard-only readiness controls.
- This handoff: implementation and validation record.

### Add

- `frontend/shooter-workshop.client/src/layouts/AppShell.test.tsx`: completion,
  busy state, repeated failures and persistent storage warning coverage.
- Focused dashboard placement/loading assertions in
  `frontend/shooter-workshop.client/src/routes/Dashboard/component.test.tsx`.
- Concurrent notification dismissal regression in
  `frontend/back-office-spa/src/components/ToastNotification/index.test.tsx`.

### Generated/Migration

No persisted-data migration. Shared package `dist` output is generated locally
by the existing build and must not be hand-edited. No `yarn.lock` change was
needed because the workspace already resolves `react-bootstrap@^2.10.9`.

### Documentation

- `docs/architecture/frontend.md`: records the opt-in shared notification
  contract, Bootstrap stylesheet prerequisite and existing BackOffice adapter.
- `docs/architecture/shooter-workshop.md`: records preparation notification
  outcomes, dashboard-only readiness controls and cross-route sync without
  changing offline guarantees.
- This handoff now records actual files, validation and the remaining visual
  verification gap.

### Verify Only

- `frontend/back-office-spa/src/components/ToastNotification/index.test.tsx`
  and existing BackOffice toast call sites: compatibility and navigation.
- `frontend/shooter-workshop.client/src/routes/offlineAction.ts`,
  `src/routes/offlineAction.test.ts`, `src/routes/config.tsx`,
  `src/components/States.tsx`, and `src/services/offlineStore.ts`: unchanged
  data behavior, routing, spinner and sync semantics.
- Other existing `@morwalpiz/layout` consumer manifests and builds, including
  the public SSR client, for additive-export compatibility.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | None; no endpoint or response shape change. |
| Frontend/UI | Shared opt-in notification provider; dashboard-only readiness, Workshop feedback and busy affordance; conditional global errors. |
| Database/Data | None; IndexedDB snapshots and outbox unchanged. |
| Contracts/Models | No API contracts; additive shared UI export and preserved BackOffice adapter. |
| Authentication/Authorization | No changes; preserve sync session refresh and sign-out behavior. |
| Validation | Show the existing action error without hiding it; no changed domain validation. |
| Cache | No changes to snapshots, service worker, or server cache. |
| Configuration/Secrets | No configuration or secrets; package dependencies only. |
| Logging/Telemetry | Existing logging unchanged; do not swallow preparation errors. |
| Background Jobs | None; foreground synchronization unchanged. |
| Deployment/Operations | Rebuild shared package before consumers; static frontend redeployment only. |
| Backward Compatibility | Preserve BackOffice imports/defaults and other clients' opt-in behavior. |

## Reuse Opportunities

Reuse the BackOffice toast contract and provider, React Bootstrap rendering,
Workshop's spinner/reduced-motion CSS, translations, existing fetcher action,
appearance tokens, BackOffice toast tests, and Workshop Vitest/Testing Library
and memory-router patterns. Do not add a second toast implementation or global
store.

## Risks

- **High — data-readiness misunderstanding:** persistence may be denied despite
  successful preparation. A distinct non-autohiding warning is implemented and
  covered by the focused AppShell test; verify it in the browser as part of the
  outstanding visual check.
- **High — lost or duplicate result:** retained fetcher data, Strict Mode, or
  reconnect sync can misattribute or replace a preparation outcome. The shell
  tracks the active intent and previous action data, reports each result once,
  and tests repeated identical failure responses; a real browser check of
  reconnect interleaving remains outstanding.
- **High — lost offline warnings or sync across routes:** moving the whole
  offline owner to the dashboard would unmount sync and hide actionable
  problems. Keep ownership and conditional urgent alerts in the shell; test
  navigation during preparation and reconnect from a non-dashboard route.
- **Medium — BackOffice regression:** extraction affects many existing callers.
  Preserve the provider API/defaults and local import paths; run existing toast
  and representative navigation tests, plus its build.
- **Medium — package compatibility:** a shared React Bootstrap dependency and
  styling prerequisites affect package consumers. Keep the entry point opt-in,
  declare peers correctly, and verify shared build, Workshop, BackOffice and
  public SSR builds.
- **Medium — inaccessible or obstructive tiles:** timed messages, focus,
  contrast, viewport edges and overlays can hide information. Warnings and
  errors remain until dismissed, status text is announced, and close labels are
  localized. Keyboard, zoom, narrow-screen and theme behavior still need
  browser verification.
- **Low — layout movement:** translated loading text and the changing status
  description can reflow the hero. The prepare control reserves 12rem and
  responds to narrow widths; compare rendered positions at representative
  widths and both locales when browser verification is available.

## Technical Debt

- BackOffice's provider uses timestamp-only IDs; fix as part of extraction
  because concurrent notifications must remain independently dismissible.
- Existing sync, logout, and conflict alerts may still affect page layout when
  they arise. Their redesign is out of scope; preserve their visibility.
- Offline network and persistence validation on actual devices remains an
  existing release gate, not evidence supplied by UI tests.

## Implementation Roadmap

1. **Complete:** extracted shared notification provider/context while preserving
   BackOffice import paths, variants, defaults, and context identity. IDs are
   monotonic within the provider, close text and placement are configurable,
   and the shared export remains opt-in.
2. **Complete:** mounted the provider in Workshop localization scope. Kept the
   fetcher, connectivity handling and sync in `AppShell`; tracked preparation
   results once and exposed a typed dashboard outlet context. Conditional
   conflict and sync/session alerts remain visible on every protected route.
3. **Complete:** moved readiness and prepare/manual-sync controls into the
   existing dashboard hero. Added the spinner and disabled state through
   fetcher loading, persistent failure/warning toasts and responsive sizing.
4. **Complete:** updated frontend architecture, Workshop architecture and this
   implementation record.
5. **Outstanding:** browser screenshot and keyboard/reduced-motion review at
   desktop/mobile widths, in both cultures and themes, plus a real online
   preparation/persistence-denial check. No authenticated local Workshop
   runtime was available in this implementation session.

## Testing Strategy

- **Unit/component:** shared provider behavior is covered through its existing
  BackOffice compatibility tests, including context stability, variants,
  close-label customization and independent dismissal for concurrent
  notifications. Workshop AppShell tests verify exactly-once success, repeated
  identical failures, and dismissible persistent-storage warnings.
- **Integration/behavior:** Workshop AppShell tests drive the fetcher through
  submitting, loading and idle, verifying the disabled/busy state and result
  notification after completion. Dashboard tests verify controls and spinner
  placement in the hero. Existing offline-action tests pass. Route navigation
  during a real preparation and reconnect interleaving were not browser-tested.
  Confirm readiness and controls occur inside the dashboard hero only; on a
  different protected route the ordinary strip is absent, reconnect still
  syncs, and blocked/session/sign-out errors remain visible. Navigate away
  during preparation and verify the result is still reported exactly once.
- **Visual/accessibility:** capture before/loading/success/error at desktop and
  narrow mobile widths, both light/dark and English/Italian; compare hero,
  readiness and button positions, verify the existing shooting-session action,
  prevent clipping/overlap, check keyboard close, screen-reader announcement
  and reduced motion. Not performed: no authenticated local browser session was
  available.
- **Contract/security/migration:** verify the shared export and existing
  BackOffice compatibility; no new API, authorization, storage, or migration
  behavior. Confirm error text is rendered as text and not interpreted as HTML.
- **End-to-end:** browser-check a real online prepare, a denied persistence
  request and a failing request where feasible; offline device preparation,
  reconnect, and sign-out remain separately documented release gates.
- Passed: `yarn --cwd frontend build:models`,
  `yarn --cwd frontend build:services`, and
  `yarn --cwd frontend build:layout`.
- Passed: `yarn --cwd frontend workspace shooter-workshop.client test
  src/layouts/AppShell.test.tsx src/routes/Dashboard/component.test.tsx
  src/routes/offlineAction.test.ts` (10 tests).
- Passed: `yarn --cwd frontend workspace back-office-spa vitest run
  src/components/ToastNotification/index.test.tsx` (3 tests).
- Passed production builds for `shooter-workshop.client`, `back-office-spa`,
  and `morwalpizvideo.client`.
- Builds emitted their existing bundle-size warnings; no build or test failures
  remain. Screenshot/device validation is still required as described above.

## Migration And Rollback

No MongoDB, SQLite, API, authentication, cache, or configuration migration is
needed. Shared package and Workshop must be built and shipped together; the
BackOffice adapter preserves its legacy imports. Other SPAs remain unchanged.
Rollback the frontend deployment and compatible shared build together; prepared
IndexedDB data and queued operations remain untouched. Existing PWA cache/update
behavior applies; verify a refreshed client loads matching static assets.

## Open Questions

No implementation-blocking questions remain. Dashboard-only placement without
a user preference, the success timeout and persistent warning/error behavior
follow the approved assumptions. Browser verification remains outstanding.
