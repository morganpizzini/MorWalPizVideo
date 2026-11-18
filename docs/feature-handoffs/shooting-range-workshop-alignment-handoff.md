# Shooting Range / Workshop UI alignment handoff

**Status:** Implemented and validated. Shared-presentation reuse was approved by the user on 2026-11-18.

## Outcome

Align the existing Shooting Range client with Shooter Workshop's operational UI: dark theme, navigation, shared presentation components where appropriate, route-owned reads and mutations, progressive skeleton loading, and tabs for multi-section administration. Preserve Range functionality, Italian copy, existing URLs, SSR, and its independent API and security model. This is a visual and interaction refinement using Workshop as the reference, not a transfer of Workshop product features.

Assumptions: all existing Range pages and actions remain in scope, including the current registration and messages UI; public-release policy changes, new booking workflows, localization, PWA features, and Workshop account integration are out of scope. User direction overrides the former ADR-016 restriction because the old layout implementation was the reason for isolation; the consolidated shared theme/layout is now the selected presentation baseline. This is not approval of deployment.

## Baseline Evidence (Before Implementation)

- [Range architecture](../shooting-range-architecture.md) and [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md) deliberately keep Range independent of shared frontend packages. The ADR's deny-by-default policy is an approved **target**, not current behavior; anonymous registration and some domain reads remain public-release blockers.
- [Range routes](../../frontend/shooting-range.client/src/routes.tsx) expose `/`, `/messages`, and `/admin`. [App.tsx](../../frontend/shooting-range.client/src/App.tsx) owns session restoration, login/register, forced password change, booking, history, logout, and expiry/focus handling. It requests session and booking data from effects. [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx) loads five resources together and performs existing admin operations via component callbacks. Messages currently fetch in an effect and silently render an empty list on read failure.
- [Range API client](../../frontend/shooting-range.client/src/api.ts) is local, uses credentialed requests and the Range CSRF endpoint, refreshes CSRF after login/password change, and emits a session-expiry event for non-auth 401s. [schedule.ts](../../frontend/shooting-range.client/src/schedule.ts) checks that the date, selected session, and UTC boundaries still match. Booking sends expected boundaries; a 409 refreshes the selection. These safeguards are not presentation details.
- [Range entry-server.tsx](../../frontend/shooting-range.client/src/entry-server.tsx) uses a static data router and `renderToString`; [server.mjs](../../frontend/shooting-range.client/server.mjs) creates a server request without forwarding browser cookies and serves SSR for direct paths. The current SSR output is a neutral session-restoration view, not authenticated content. [main.tsx](../../frontend/shooting-range.client/src/main.tsx) hydrates that output.
- Workshop's [AppShell.tsx](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx) and [main.scss](../../frontend/shooter-workshop.client/src/main.scss) implement its dark, yellow-accented responsive navigation and controls. Its [route configuration](../../frontend/shooter-workshop.client/src/routes/config.tsx), [deferred region wrapper](../../frontend/shooter-workshop.client/src/routes/shared.tsx), and [loading primitives](../../frontend/shooter-workshop.client/src/components/loading/LoadingPrimitives.tsx) demonstrate route loaders/actions, raw promises, Suspense/Await and shaped skeletons. Its [AuthProvider](../../frontend/shooter-workshop.client/src/auth/AuthProvider.tsx) uses a **different public-account session** and cannot be substituted for Range auth.
- Shared [TabbedWorkspace](../../frontend/fe-packages/layout/src/components/TabbedWorkspace.tsx) supports keyboard navigation and renders only the selected panel; [AccountForm](../../frontend/fe-packages/layout/src/components/AccountForm.tsx) and other presentation exports are available. Workshop owns their theme styling locally. [PepperboxTopBar](../../frontend/fe-packages/layout/src/components/PepperboxTopBar.tsx) is a placeholder with nonfunctional login/signup and is not an appropriate authenticated navbar.
- [Range tests](../../frontend/shooting-range.client/src/App.test.tsx) cover restoration, forced change, logout and expiry; [Admin tests](../../frontend/shooting-range.client/src/Admin.test.tsx) cover forms and direct-path SSR; [API tests](../../frontend/shooting-range.client/src/api.test.ts) cover credentials, CSRF, status errors and booking payload; [schedule tests](../../frontend/shooting-range.client/src/schedule.test.ts) cover stale sessions and time zones.

## Implementation Delivered

- The Range app now consumes shared `@morwalpiz/layout` presentation primitives without importing Workshop authentication, services, models, or business logic. [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md) and [Range architecture](../shooting-range-architecture.md) record the user-approved exception.
- The Range shell and local styles align the navbar, dark/yellow theme, responsive controls, admin tabs, and loading/error states with Workshop while preserving the existing Italian content and URLs.
- Range-local [routes](../../frontend/shooting-range.client/src/routes.tsx) and [routeData](../../frontend/shooting-range.client/src/routeData.ts) now own guarded loaders/actions, progressive deferred reads, retryable failures, auth/role checks, and existing booking-boundary validation. Protected actions receiving 401 clear the session through the existing expiry event.
- Admin forms retain lifted drafts across tab changes. Loading/error UI is provided by [Loading.tsx](../../frontend/shooting-range.client/src/Loading.tsx); local API transport remains in [api.ts](../../frontend/shooting-range.client/src/api.ts).
- The shared layout workspace dependency and [Range Dockerfile](../../frontend/shooting-range.client/Dockerfile) build inputs are updated. No shared package source, backend endpoint, persisted data, or Range API contract changed.
- Focused test updates cover app/admin behavior, route authorization, deferred loader behavior, stale booking prevention, and protected-action 401 handling.

### Changed source and documentation

- [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md), [Range architecture](../shooting-range-architecture.md), and this handoff document the presentation-only exception and its boundaries.
- [package.json](../../frontend/shooting-range.client/package.json) and the [Range Dockerfile](../../frontend/shooting-range.client/Dockerfile) declare/build the shared layout dependency.
- [App.tsx](../../frontend/shooting-range.client/src/App.tsx), [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx), [routes.tsx](../../frontend/shooting-range.client/src/routes.tsx), [routeData.ts](../../frontend/shooting-range.client/src/routeData.ts), [Loading.tsx](../../frontend/shooting-range.client/src/Loading.tsx), [api.ts](../../frontend/shooting-range.client/src/api.ts), and [styles.css](../../frontend/shooting-range.client/src/styles.css) implement the UI and route behavior.
- [App.test.tsx](../../frontend/shooting-range.client/src/App.test.tsx), [Admin.test.tsx](../../frontend/shooting-range.client/src/Admin.test.tsx), and [routeData.test.ts](../../frontend/shooting-range.client/src/routeData.test.ts) cover the updated behavior.

### Validation performed

- `yarn --cwd frontend workspace shooting-range.client test --passWithNoTests=false` — passed, 32 tests across five files.
- `yarn --cwd frontend workspace shooting-range.client build:all` — passed, including TypeScript, client production build, and SSR build.
- `cd frontend && docker build -f shooting-range.client/Dockerfile -t shooting-range-alignment-check .` — passed from a clean container build context.
- Started the built SSR server and requested `/`, `/messages`, and `/admin` directly — all returned the neutral SSR shell without requiring a session. No real-browser screenshot or visual comparison was performed.
- Builds emit the existing Vite warning for non-module `/env-config.js`; Docker dependency installation reports peer-dependency warnings. Neither prevented validation.

### Residual risks and release gates

- A real-browser desktop/mobile visual pass, keyboard/screen-reader exercise, and authenticated end-to-end booking/admin run were not performed.
- Existing public-release authorization/onboarding and API security/deployment blockers remain unchanged; this UI alignment does not authorize deployment or relax ADR-016's remaining restrictions.

## Implementation Decisions

1. **Approved presentation boundary.** Range consumes the existing `@morwalpiz/layout` presentation package, especially `TabbedWorkspace`, without importing Workshop app source or moving Range models, transport, authorization, contracts or business behavior into shared packages. The user explicitly superseded the former ADR-016 restriction because it reflected the old layout implementation; the shared consolidated theme/layout is now the intended presentation baseline. Do not change shared component contracts unless a demonstrated common need requires it; verify all consumers if changed.
2. **Design direction.** Use Workshop's current source and rendered screens as visual authority: dark surfaces, yellow emphasis, typography, compact task-oriented shell, active navigation, and responsive controls. Keep the Range name, data, Italian content and actions. Treat the application as **Operate**; use Impeccable `shape` for the cross-page direction and `adapt`/`audit` during implementation. No `PRODUCT.md` or `DESIGN.md` exists in either app in the inspected tree; use incumbent source and visual evidence, not an invented design system. Compare authenticated, anonymous, error and loading views on desktop and narrow mobile; verify keyboard, contrast, focus and reduced motion. Do not create design artifacts merely to satisfy the workflow.
3. **Navigation and sections.** Preserve `/`, `/messages`, `/admin` and direct entry. Align a Range-owned shell with Workshop's header/navigation behavior and current-role visibility; keep logout pending/error behavior and never show protected links during forced change. On `/admin`, make the existing configuration, bays, closures, accounts, and booking decisions distinct labeled tabs, with a deterministic default and keyboard access. Use the shared tab primitive and Range-local styling. Because the primitive unmounts inactive panels, lift each editable form draft above the tab panel; preserve it across tab switches and unrelated revalidation, resetting only after its successful save or explicit cancellation. Preserve new-bay cancellation. Do not split ordinary short pages into unnecessary tabs.
4. **Route-owned flow.** Reorganize Range route reads into colocated loaders and mutations into actions/fetchers, retaining the local `api.ts` transport and the existing endpoint and request shapes. Use explicit action intents for login, registration, password change, booking, message creation and existing admin mutations; parse and validate form input at the route boundary, keep server validation authoritative, return accessible structured errors and disable duplicate submission. On successful writes use router revalidation rather than a second component reload; report a failed refresh separately from a successful mutation so retries cannot accidentally repeat writes. Keep range-specific UI state local where it is not route data.
5. **Session and SSR gate.** Keep Range session semantics, not Workshop's account provider. Server-render a neutral, deterministic restoration skeleton for all three existing paths without requesting private API data or serializing account data. Browser hydration must explicitly trigger the client loaders even though the SSR response supplies placeholder data; use the React Router hydration-loader/fallback mechanism and prove it with SSR + hydration tests. Each protected browser loader/action independently awaits current Range session validation before starting domain work, including admin role and forced-change checks: a parent loader or rendered shell alone is insufficient because matched data-router loaders can execute concurrently. A 401 clears protected UI and returns to login; a 403 shows authorization feedback; forced-change permits only password change/logout. Restoration transport failures retain retry UI rather than pretending the user is logged out. Retain focus revalidation, expiry handling and failed-logout behavior. The API remains the authorization authority.
6. **Progressive data.** After the per-loader session check, return raw promises for independent route regions and render each with Suspense/Await plus meaningful table/card/form-shaped skeletons. SSR must not create those private promises. Keep admin dependencies coherent: config, users and bays needed by related forms may resolve together; separate unrelated booking/closure regions only where their required references are ready. Each rejection has a visible retry/error state, never an empty-success fallback. Abort or ignore stale requests on navigation/date changes. Use router navigation/fetcher pending indicators for revalidation and mutations without erasing usable data or drafts; skeletons must announce loading and honor reduced-motion preferences.
7. **Booking integrity.** Route state for date and period must invalidate old availability immediately when either changes; only accept results matching the currently offered session and UTC boundaries. Pass the original expected UTC boundaries when booking, block stale or duplicate requests, and on 400/409 refresh sessions and display the actionable message. Preserve configured field time zone, empty states, history errors, and all current admin form fields and decision actions.

## Impacted Projects

- `frontend/shooting-range.client`: primary UI, routing, tests and build/SSR integration.
- `frontend/fe-packages/layout`: existing runtime dependency for presentation; preferably verify-only. Models/services packages are transitive build inputs, not Range domain dependencies.
- `frontend/shooter-workshop.client`: visual and interaction reference and regression consumer if shared layout changes; no Workshop-specific feature changes.
- `MorWalPizVideo.ShootingRange` and its tests: verify-only security and contract authority; no backend changes intended.
- Range client Docker image, shared frontend workspace lockfile and deployment validation: update only as needed for the new package dependency and its clean image build. AppHost and the independent API deployment remain unchanged.

## Impacted Files

### Modify

- [Range package.json](../../frontend/shooting-range.client/package.json): declares the existing shared layout dependency. The workspace lockfile needed no change; the clean Docker build passed with `--frozen-lockfile`.
- [routes.tsx](../../frontend/shooting-range.client/src/routes.tsx), [App.tsx](../../frontend/shooting-range.client/src/App.tsx), [Admin.tsx](../../frontend/shooting-range.client/src/Admin.tsx): establish route boundaries, auth-aware shell, booking, messages and tabbed admin UI without changing domain behavior.
- [styles.css](../../frontend/shooting-range.client/src/styles.css): aligned theme, responsive shell, tabs, feedback and skeletons. Scope selectors to avoid shared-style regressions.
- [api.ts](../../frontend/shooting-range.client/src/api.ts): only the minimal cancellation or error plumbing required by the route implementation; retain URL, credential and CSRF semantics.
- [main.tsx](../../frontend/shooting-range.client/src/main.tsx) and [entry-server.tsx](../../frontend/shooting-range.client/src/entry-server.tsx): coordinate neutral SSR, hydration and client-only private loading.
- [Range Dockerfile](../../frontend/shooting-range.client/Dockerfile): include the shared package manifests/source and dependency-ordered build inputs needed for a clean standalone image. Preserve the existing SSR runtime and runtime API configuration.
- Existing Range test files: update affected expectations and add focused route, hydration, deferred-state, tab and security regressions.

### Add

- Route-owned loader/action/component and local loading primitive modules under `frontend/shooting-range.client/src/` as warranted by the decomposition; keep the existing three public URLs. Exact module names are implementation details, not new routes.

### Generated/Migration

- No database or generated-code migration. The dependency lockfile is updated by the package manager.

### Documentation

- Update [Range architecture](../shooting-range-architecture.md), [ADR-016](../architecture/adr/ADR-016-shooting-range-public-poc.md), and, if its documented ownership/SSR behavior changes, [frontend architecture](../architecture/frontend.md). Record the narrow shared-presentation exception and actual routing/loading behavior; do not mark release gates complete.
- After implementation, update this handoff with changed files, actual decisions, validation, and residual risks.

### Verify Only

- [Workshop shell](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx), [theme](../../frontend/shooter-workshop.client/src/main.scss), [loading primitives](../../frontend/shooter-workshop.client/src/components/loading/LoadingPrimitives.tsx), and shared [layout exports](../../frontend/fe-packages/layout/src/index.ts): reference and consumer compatibility.
- [Range API tests project](../../MorWalPizVideo.ShootingRange.Tests/), [Range deployment workflow](../../.github/workflows/deploy-shooting-range-client.yml), [AppHost](../../MorWalPizVideo.AppHost/Program.cs), [testing runbook](../architecture/testing.md), and [deployment guide](../architecture/deployment.md): verify contracts, build inputs, release gates and unchanged runtime topology.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No endpoint or business-rule changes; existing Range API remains authoritative. |
| Frontend/UI | Restyled shell, accessible admin tabs, route loaders/actions and progressive loading; existing content/actions retained. |
| Database/Data | No schema, write, index or backfill change. Preserve draft input, UTC booking identity and conflict handling. |
| Contracts/Models | No wire changes; Range's local types and endpoints remain local. |
| Authentication/Authorization | Preserve separate Range cookie/CSRF/revocable-session model, role checks and forced-change restrictions. No Workshop SSO or new authorization cutover. |
| Validation | Preserve native and server validation; show action, refresh and field failures explicitly. |
| Cache | No API cache/tag change; router revalidation governs displayed data. No authenticated-response cache. |
| Configuration/Secrets | Preserve runtime `API_BASE_URL`, configured CORS and Vite proxy; no secrets or new settings. |
| Logging/Telemetry | No backend telemetry change; visible client failures remain distinguishable from empty/success states. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Shared-package build order and clean SSR image are affected; no deployment or public-release approval implied. |
| Backward Compatibility | Preserve URLs, SSR/hydration, session/cookie contract, forms, Range API and current registration/messages behavior. |

## Reuse Opportunities

Reuse shared `TabbedWorkspace` and selectively `AccountForm`, `PageTitle` or `EmptyState` only where their semantics fit; retain local Range forms where shared interfaces would compromise behavior. Refer to Workshop's skeleton and route patterns without importing its app-local source. Reuse Range's API client, session rules, `availabilityMatches`, existing forms and fixtures. Avoid pulling in Workshop services, models, provider, feature flags, wizard or PWA.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | SSR/parallel loaders could request protected data before session/forced-change checks or fail hydration. | Neutral server output; independently guard each browser loader/action; direct-path SSR and hydration tests assert no private request or content before validation. |
| High | UI rewrite could lose reservation identity or replay a successful mutation when refresh fails. | Preserve boundary matching, expected UTC instants, conflict refresh and separate mutation/refresh feedback; test stale responses, 409 and refresh failure. |
| High | Public-release blockers could be mistaken for solved by a visual/auth alignment. | Keep current anonymous policy intact and existing release gates explicitly open; API authorization and real-browser checks remain separate. |
| Medium | Shared package dependency contradicts current ADR and could break clean Range image. | Approve/document presentation-only exception; build packages in dependency order and verify clean image and SSR. |
| Medium | Admin tabs unmount forms, losing unsaved input or replacing it on revalidation. | Lift drafts and preserve across tab changes/unrelated refresh; test switching while editing and pending. |
| Medium | Session expiry, role demotion, failed logout or CSRF behavior regresses. | Preserve independent Range transport and server authority; test expiry, 401/403, forced change, retries, CSRF renewal and direct navigation. |
| Medium | Regional loading failures or delayed requests appear empty or show stale records. | Independent error/retry and cancellation/version checks; test rejected/out-of-order promises and no false empty state. |
| Low | Shared styling, mobile overflow, inaccessible tabs or motion degrades usability. | Scoped CSS; keyboard/ARIA, contrast, responsive and reduced-motion checks; shared consumer regression if component contracts change. |

## Technical Debt

- Anonymous registration/domain reads and incomplete public-release authorization/DTO posture are existing blockers, not addressed here.
- Messages now surface read failures explicitly; their existing payload and feature scope remain unchanged.
- Range's current local effects and monolithic admin page are refactored only to the extent needed for routing, progressive loading and tabs. Do not undertake a broader domain or authentication rewrite.
- Workshop and Range have distinct authentication boundaries; apparent visual similarity is not justification for a shared session or account migration.

## Implementation Roadmap (Completed)

1. **Agree boundary and baseline:** the user approved the shared-presentation ADR exception; source and focused-test baselines informed the implementation. A desktop/mobile screenshot comparison was not performed.
2. **Package and SSR foundation:** add the layout dependency and clean-image build inputs; define deterministic server placeholder and client hydration behavior. Gate: existing three direct paths SSR without protected requests/content; hydration triggers restoration without mismatch.
3. **Session and route infrastructure:** implement Range-local, independently checked browser loaders/actions, forced-change and role handling, failure/retry and expiry/focus behavior. Gate: existing session/CSRF/logout tests plus loader/action concurrency and unauthorized tests pass.
4. **Shell and auth:** align navbar, theme, forms and visible states to Workshop while retaining Range routes, copy and cookie behavior. Gate: login/register, password change, logout and responsive navigation operate as before.
5. **Booking and messages:** move reads/mutations to route boundaries, add deferred region skeletons and explicit errors; retain date/session invalidation, UTC expectations, history and message behavior. Gate: success, empty, delayed, rejected, stale, conflict and retry tests.
6. **Admin workspace:** migrate existing operations and interdependent reads, introduce accessible tabs and preserved drafts; use action/fetcher pending state and route revalidation. Gate: all five sections and existing admin actions work, with draft and partial-load failure coverage.
7. **Integrated validation and documentation:** focused Range tests, client+SSR build, clean image build, direct-path SSR smoke tests, architecture/ADR and this handoff are complete. Real-browser visual/accessibility and authenticated end-to-end checks remain deferred as listed above. No shared component contract changed, so no additional Workshop consumer check was needed. Stop at this scope; new product capabilities require separate approval.

## Testing Strategy

- **Unit:** existing schedule comparisons and time-zone formatting; stale date/period/boundaries, invalid form intents and field conversion.
- **Component/router:** skeleton-to-content and rejection/retry transitions, no empty state during loading, 401/403, forced change, auth pending/error, failed logout, role-specific nav, keyboard tab selection, and draft retention across tabs/revalidation. Verify repeated submissions cannot duplicate mutations.
- **Contract/security:** retain [api.test.ts](../../frontend/shooting-range.client/src/api.test.ts) coverage for credential inclusion, pre-login and post-identity-change CSRF, status propagation and expected booking bounds. Test that loaders/actions do not initiate domain reads for unvalidated, forced-change or non-admin sessions; server authorization remains independently tested in the existing API project.
- **Integration/SSR:** direct server render and hydration of `/`, `/messages`, `/admin`, no private data in HTML, no cookie-forwarding assumption, route loader invocation after hydration, expiry/focus and concurrent navigation; client and SSR builds must both pass.
- **Visual/end-to-end:** compare Workshop and Range desktop and narrow mobile with real representative states; check focus order, tab keyboard navigation, status announcements, contrast, 200% zoom, reduced motion and overflow. In a suitable environment exercise booking, conflict, admin forms and session restoration with a disposable/test account. Before public exposure, independently satisfy the existing exact-origin CORS/CSRF, API authorization, persistent-key and operational release checks.
- **Migration:** not applicable to persistent data. Verify a clean dependency install and clean container build because workspace-local artifacts can conceal missing image inputs.

## Migration And Rollback

No MongoDB, SQLite, API, cache, authentication or persistent-data migration is proposed. Keep the existing API deployed and compatible; publish the client only after validation and retain its previous compatible immutable image. Roll back to that client image if necessary without weakening the guarded API, CORS or session security. The ADR exception applies only to frontend presentation; rollback requires no data reversal. If an implementation unexpectedly requires API or auth changes, stop and obtain a separate compatibility and rollout decision.

## Open Questions

- Approve the proposed exception to ADR-016 for `@morwalpiz/layout` presentation reuse? Without it, preserve the current isolation boundary and revisit the shared-component requirement before implementation. Response: override the ADR-016 and reuse the layout
- The approved public-release onboarding and anonymous-domain cutover remains undecided; this implementation intentionally does not resolve or implement it.
