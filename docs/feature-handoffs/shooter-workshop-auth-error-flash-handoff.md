# Shooter Workshop Anonymous Route Error Flash Handoff

## Outcome

Make Shooter Workshop navigation commit immediately, including the transition after
login, while each retained loader starts its page fetch and the destination renders its
own loading state concurrently. At the same time, prevent the application from briefly
displaying React Router's built-in "Unhandled Application Error" when an anonymous
browser opens a protected route such as `/firearms`.

The accepted behavior is:

- Successful login keeps the submit state pending until the login route unmounts.
- Navigation is not blocked by dashboard, equipment, ammunition, history, integrity,
  or other Workshop page reads.
- The loader starts the request, returns its raw promise without awaiting it, and the
  destination page and shell render while `<Suspense>/<Await>` owns the local
  loading/error/content transition.
- Mutations continue to use the existing route actions and `useFetcher` patterns;
  React Router continues to revalidate the retained loaders after successful actions.
- The existing HttpOnly-cookie account flow, safe return URL, CSRF behavior, server
  authorization, and final navigation to `/account/login` are preserved.

Assumptions:

- The reported refresh that does not reproduce occurs after the browser has already
  reached the public login route. If the original protected URL itself is refreshed and
  no flash occurs, that timing difference should be captured during browser validation
  but does not change the missing error-boundary mechanism.
- This is a focused reliability refinement, not a redesign of authentication,
  navigation, or Workshop presentation.

## Implementation — 2026-11-17 deferred Workshop loaders

Implemented the retained-loader plan. Workshop page loaders now start their existing
API reads and return raw promises immediately through a shared deferred-data helper.
The destination route mounts while data is pending; data-dependent content is rendered
inside `Suspense`/`Await` with localized loading and error states.

Changed files:

- `frontend/shooter-workshop.client/src/routes/shared.tsx` — added
  `DeferredRouteData`, `deferRouteData`, `DeferredRoute`, and the shared async error
  boundary. Authentication failures preserve the current local return URL and redirect
  to login; other failures use the branded Workshop error state.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — added the protected-shell
  route error boundary without changing route topology or actions.
- `frontend/shooter-workshop.client/src/routes/{Dashboard,Firearms,FirearmDetail,Ammunition,RecipeProduction,ShootingSessions,History,Reconciliation}/loader.ts` —
  retained the existing request composition but return raw promises.
- Matching route components — render immediately and resolve typed loader data through
  the shared deferred boundary.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` — accepts
  resolved parent data when embedded under Shooting Sessions, avoiding a second read
  through `useLoaderData`.
- `frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx` — keeps
  successful login pending until replacement navigation unmounts the form.
- Focused route tests — await deferred loader payloads and cover pending/resolved/error
  deferred rendering.
- `docs/architecture/shooter-workshop.md` — recorded the deferred loader convention.

Validation:

- `cd frontend && yarn workspace shooter-workshop.client test --run` — 16 files,
  48 tests passed.
- `cd frontend && yarn workspace shooter-workshop.client build` — passed; Vite emitted
  the existing chunk-size warning for the main JavaScript bundle.

Residual risks:

- Exact-origin browser verification remains required to measure the visible login
  transition and confirm no default error frame paints during anonymous direct
  navigation.
- React Router's current revalidation behavior remains intentionally unchanged;
  mutations continue to rely on route actions and loader revalidation.

## Current Evidence

- `frontend/shooter-workshop.client/src/main.tsx` creates the browser router immediately
  and registers an account unauthorized handler that calls
  `window.location.replace(...)` for every non-login 401.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx` restores the account
  session in a post-render effect. Its loading state therefore does not delay React
  Router's matched route loaders.
- `frontend/shooter-workshop.client/src/routes/config.tsx` places `/firearms` and the
  other operational routes under `AppShell`, but that protected parent route has no
  `errorElement`. Only the recipe-production child has a local error element.
- `frontend/shooter-workshop.client/src/routes/Firearms/loader.ts` starts four protected
  Workshop reads as soon as React Router resolves the direct route.
- `frontend/fe-packages/services/src/apiTransport.ts` invokes the account client's
  unauthorized handler when a protected account request receives an error response,
  then returns a structured response containing the HTTP status.
- `frontend/fe-packages/services/src/workshopService.ts` passes that response through
  `requireSuccessfulResponse`, which throws `ApiResponseError` for status 401.
- The hard browser redirect and loader rejection therefore happen in the same failure
  path. Before the replacement navigation completes, React Router handles the
  unhandled loader exception with its default development-style route error UI. This
  exactly explains the transient message followed by the login page.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` already renders an
  account-restoration state and redirects an anonymous resolved session from an
  effect, but route loaders execute independently of that component render guard.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` verifies route
  registration but does not require an error boundary on the protected shell.
- The deployment workflow
  `.github/workflows/deploy-shooter-workshop-client.yml` already runs the Workshop
  Vitest suite and production build before image publication.
- `frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx` sets its
  local pending state before awaiting login, starts `navigate(...)` without awaiting
  the transition, and unconditionally clears pending in `finally`.
- React Router retains the currently committed login screen while the destination
  loader is pending. The login form therefore remains mounted briefly with its submit
  button enabled again.
- The tagged backend account tests prove that a successfully issued session can be
  restored immediately and account/session reads are non-cacheable. The backend session
  is not the cause of the post-login delay.
- Every rendered protected data route except profile is currently loader-backed:
  dashboard, firearms, firearm detail, ammunition, recipe production, shooting
  sessions, history, and integrity. Those loaders gate route commit.
- React Router 7.18.2 exports `<Await>` and documents raw promises returned from loaders
  as its deferred-data mechanism. The loader starts the request without awaiting it,
  returns the promise in its data object, and the route component renders that promise
  inside `<Suspense>` and `<Await>`.
- React Router 7 removed the old `defer()` helper. The implementation must return raw
  promises; it must not import or recreate the removed helper as if it were framework
  API.
- `frontend/back-office-spa/src/router/asyncData.tsx` is current repository prior art:
  `deferRouteData` wraps a raw promise, `AsyncRouteBoundary` renders it through
  `<Suspense>/<Await>`, and `useResolvedLoaderData` lets route content consume the
  resolved async value.
- Existing Workshop route actions rely on React Router's automatic loader revalidation.
  Retaining loaders preserves that freshness behavior and avoids adding manual
  component refresh wiring.

Ranked hypotheses:

1. **Confirmed:** the protected loader's 401 rejection reaches React Router without a
   route error element while the hard redirect is still in flight.
2. **Contributing:** account restoration and matched loaders start independently, so
   the shell loading UI does not prevent the anonymous protected request.
3. **Not supported as the primary cause:** service-worker caching. The Workshop service
   worker is lifecycle-only and does not intercept account or API requests.
4. **Confirmed for the post-login button behavior:** the login component clears its
   own pending state while the loader-gated destination navigation is still pending.

## Proposed Design

### Application-wide immediate navigation with retained loaders

Retain React Router data loaders, but make their page reads deferred. Each loader starts
its existing service call or `Promise.all(...)` group and returns the raw promise
without awaiting it. Returning the promise as data allows React Router to commit the
destination immediately instead of waiting for the network response.

Each route component must mount immediately, render its static page frame/title where
possible, and place only the data-dependent region inside React `<Suspense>` and React
Router `<Await>`. The Suspense fallback uses the existing Workshop loading presentation.
The Await error element handles rejected reads without exposing React Router's default
error screen.

Add a small Workshop-owned deferred-loader convention based on the existing BackOffice
prior art:

- A typed deferred data shape containing a raw `Promise<T>`.
- A helper that returns that shape without awaiting the promise.
- A typed resolved-data helper using `useAsyncValue` for content rendered by `<Await>`.
- A shared async error component using `useAsyncError`.
- A route/page boundary that renders the promise through `<Suspense>/<Await>` while
  allowing each page's static structure to render outside the suspended region.

Do not use the removed React Router `defer()` API. Do not introduce TanStack Query, SWR,
Redux, a component-effect resource hook, or a global cache.

Keep read composition in each existing loader. Combined pages continue to start one
coherent promise for their existing parallel request groups:

- Firearms owns firearms, definitions, physical inventory, and installations.
- Ammunition owns recipes and batches.
- Shooting sessions owns firearms, recipes, sessions, definitions, and inventory for
  both the shooting and embedded maintenance tabs.

Do not split those combined reads across children because that would duplicate requests
and produce inconsistent snapshots.

Keep existing route actions, `useFetcher`, `shouldRevalidate`, and automatic loader
revalidation behavior. Successful actions continue to refresh through the router;
validation failures retain current data. Route-specific requirements are:

- Dashboard: key dashboard/history/analytics reads by the normalized period query.
- Firearms: preserve automatic revalidation after firearm, component-definition,
  physical-component, install/remove, edit, and archive success.
- Firearm detail: preserve ID and both pagination inputs; revalidate after maintenance
  and edit success; keep archive's post-success navigation explicit.
- Ammunition: preserve revalidation after recipe or production success.
- Recipe production: key by recipe ID; keep not-found handling and begin QR generation
  only after recipe data succeeds.
- Shooting/maintenance: revalidate the single combined loader after record, correction,
  or maintenance success.
- History: resolve the effective saved/URL filter before fetching and key by page/type
  so an older unfiltered response cannot overwrite a newer filtered response.
- Integrity: revalidate reconciliation status after a successful rebuild.
- Account profile: continue reading the authenticated session from `AuthProvider`; it
  needs no page resource.

Keep the synchronous `/components` compatibility redirect loader. It performs no
network read and already returns immediately while preserving query parameters and
replacement semantics.

On successful login, treat success as terminal for the login component: start
replacement navigation and leave `pending` true so the form cannot become interactive
again. Clear pending only when login fails or navigation cannot be initiated. Because
the destination loader returns its raw data promise immediately, the destination route
commits and unmounts the login form while the destination `<Await>` remains pending.

Authentication restoration remains separate from page-data loading. A cold protected
deep link may still show the shell's account-session restoration state before protected
content is allowed to render. That is an intentional authorization gate, not a
page-data navigation delay.

### Protected-route error handling

Add one Workshop-owned route error component at the protected shell boundary and use
it as the parent route's `errorElement`.

The component must:

- Read the current route error using React Router's route-error API.
- Recognize `ApiResponseError` with status 401 as an authentication transition, not an
  application failure.
- Navigate with replacement to `/account/login`, preserving the current local path,
  query, and hash in the encoded `returnUrl`.
- Render only the existing localized redirect/loading state while that navigation is
  being committed; it must never expose the framework's default error page.
- Render the existing branded Workshop error state for non-authentication loader
  failures rather than allowing React Router's built-in error UI to appear.
- Avoid logging an expected anonymous 401 as an application exception.

Keep the existing account transport and `window.location.replace` unauthorized handler.
It remains the cross-request expiry fallback for loader promises and mutations. The
shared Await error element must recognize `ApiResponseError` status 401 as an
authentication transition so rejected deferred reads never expose a generic error.
The route error element remains the deterministic boundary for synchronous loader,
action, render, and unexpected router errors.

The error element belongs on the protected shell route, not independently on every
Workshop child. Child-specific error elements may remain where they provide more
specific treatment, but they must not turn a 401 into content-level error copy. If the
existing recipe-production error element intercepts 401 before the parent can handle
it, update that child to delegate or apply the same authentication transition.

Impeccable planning lens:

- Target surface: anonymous-to-login transition from protected Workshop routes.
- Mode: **Operate**.
- Direction: narrow reliability refinement; preserve the incumbent Workshop visual
  identity and existing loading/error components.
- Applicable implementation workflow: **harden**.
- `PRODUCT.md` and `DESIGN.md` are not present in the Workshop workspace; the
  implemented components, `main.scss`, and the Workshop architecture specification
  are the incumbent visual truth.
- Minimum visual evidence: no generic framework error text or flash during direct
  anonymous navigation at desktop and narrow mobile widths; the existing localized
  redirect state and login page remain visually unchanged.

### Material Alternatives

Delaying browser-router construction until `AuthProvider` completes session restoration
would prevent initial protected requests but changes router lifetime and public-route
startup. It is not required for immediate navigation when loaders return raw promises.

Awaiting the destination navigation before clearing the login pending flag would hide
the button symptom but retain loader-gated navigation across the rest of the
application. It is insufficient for the requested application-wide behavior.

Moving all reads into component effects would also make navigation immediate, but the
user explicitly chose to retain loaders. It would discard router revalidation and
duplicate existing React Router deferred-data capability.

## Impacted Projects

- `frontend/shooter-workshop.client` — owns the route tree, account transition, UI
  states, route data ownership, mutation refresh behavior, and regression tests.
- `frontend/fe-packages/services` — verify only; its account 401 and
  `ApiResponseError` behavior explains the transition and should remain unchanged.
- `MorWalPizVideo.ServerAPI` — verify only; no endpoint, cookie, CORS, CSRF, or
  authorization behavior needs to change.
- `.github/workflows` — verify only; the existing Workshop validation job already owns
  the required test and build gates.
- Other frontend workspaces and backend projects — not impacted because the proposed
  boundary is Workshop route configuration.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/src/routes/shared.tsx` — add typed raw-promise
  loader data, resolved async-value access, shared Suspense/Await presentation, and
  deferred error handling; retain unrelated activity helpers.
- `frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx` — keep
  successful login pending until immediate route unmount; clear pending on failure.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — register the protected
  shell error element while retaining page-data loaders, actions, revalidation hooks,
  and redirects.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — require the protected
  shell error element and deferred loaders on rendered data routes.
- Existing data loader modules for Dashboard, Firearms, FirearmDetail, Ammunition,
  RecipeProduction, ShootingSessions, History, and Reconciliation — return typed raw
  promises without awaiting page reads.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` — render the
  route immediately and resolve period-keyed dashboard/history/analytics data in Await.
- `frontend/shooter-workshop.client/src/routes/Firearms/component.tsx` and its equipment
  panels — own the combined equipment resource and refresh it after successful
  mutations.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` — own the
  ID/pagination-keyed detail resource and explicit post-mutation refresh/navigation.
- `frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx` and embedded
  recipe components — resolve deferred recipes/batches while preserving action
  revalidation.
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx` — resolve
  the deferred recipe, retain specific error behavior, and sequence QR generation
  after success.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` and
  `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` — share one
  route-owned resource and refresh after shooting/correction/maintenance success.
- `frontend/shooter-workshop.client/src/routes/History/component.tsx` — resolve effective
  filters before issuing a page/type-keyed resource read.
- `frontend/shooter-workshop.client/src/routes/Reconciliation/component.tsx` — own and
  render deferred reconciliation status.
- Existing route tests under `Dashboard`, `Firearms`, `FirearmDetail`, `Ammunition`,
  `RecipeProduction`, `ShootingSessions`, `History`, and `Reconciliation` — move
  assertions from loader functions to rendered loading/success/error/reload behavior.
- `frontend/shooter-workshop.client/src/i18n.tsx` — only if the implementation needs a
  new user-facing string; prefer the existing redirect and generic workspace-error
  strings.
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx` — only
  if its existing child boundary must pass 401 through the shared auth transition.

### Add

- `frontend/shooter-workshop.client/src/routes/WorkshopRouteError.tsx` — central
  protected-route error presentation and 401 login transition.
- A colocated Workshop route-error test file following the existing Vitest/Testing
  Library convention — prove anonymous 401 navigation and branded non-401 handling.
- Focused deferred-loader tests — prove immediate route commit, loading fallback,
  resolved content, rejected-promise handling, and action-driven revalidation.

### Generated/Migration

None. No generated output, package lock, database migration, or persisted data changes.

No loader modules are removed. `Maintenance/loader.ts` and `Recipes/loader.ts` remain
outside this change because they are not registered by the current router.

### Documentation

- `docs/feature-handoffs/shooter-workshop-auth-error-flash-handoff.md` — this
  implementation handoff; update after implementation with actual files, validation,
  and residual risks.
- `docs/architecture/shooter-workshop.md` — after implementation, record the
  application-wide immediate-navigation/component-resource convention.

### Verify Only

- `frontend/shooter-workshop.client/src/main.tsx` — confirm the existing unauthorized
  handler still preserves the return URL and remains unchanged.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx` — confirm session restore
  behavior remains unchanged.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` — confirm authenticated
  and anonymous shell states remain unchanged.
- All existing route action modules — preserve parsing, validation, idempotency,
  mutation contracts, fetcher action results, and automatic loader revalidation.
- `frontend/fe-packages/services/src/apiTransport.ts` — confirm account 401 still
  invokes the configured handler and returns a status-bearing error response.
- `frontend/fe-packages/services/src/workshopService.ts` — confirm unsuccessful
  Workshop responses still become `ApiResponseError`.
- `.github/workflows/deploy-shooter-workshop-client.yml` — confirm the existing
  Workshop test and build gates cover the changed workspace.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Not applicable; no endpoint behavior changes. |
| Frontend/UI | Commit destinations immediately, render local loading/error/content states, keep login pending until unmount, and replace the framework error flash with the Workshop transition state. |
| Database/Data | Not applicable; no persistence or data-shape changes. |
| Contracts/Models | No contract changes; reuse exported `ApiResponseError`. |
| Authentication/Authorization | Presentation and routing only. Cookie validation and authorization remain server-authoritative. |
| Validation | Preserve safe local return-path handling; do not accept external destinations. |
| Cache | Not applicable; no cache policy or service-worker behavior changes. |
| Configuration/Secrets | Not applicable; no settings or secret changes. |
| Logging/Telemetry | Expected anonymous 401 should not be logged as an unhandled UI error. Stale/abandoned reads should not surface as user errors. Existing transport/server telemetry remains authoritative. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Existing SPA test/build/image workflow is sufficient. Browser verification against the deployed exact origin remains required. |
| Backward Compatibility | Existing links, login return URLs, cookies, authenticated routes, API responses, mutation actions, and query semantics remain compatible; only read timing/ownership changes. |

## Reuse Opportunities

- Reuse `ApiResponseError` from `@morwalpizvideo/services`; do not inspect arbitrary
  message text.
- Reuse `LoadingState` and `ErrorState` from
  `frontend/shooter-workshop.client/src/components/States.tsx`.
- Reuse the raw-promise `<Suspense>/<Await>` pattern already implemented in
  `frontend/back-office-spa/src/router/asyncData.tsx`, adapted to Workshop loading,
  error, and authentication states.
- Reuse each current loader's service-call composition inside the corresponding route
  component resource.
- Reuse existing route actions, `useFetcher` pending/error state, and idempotency keys;
  add explicit owner-resource refresh rather than rewriting mutations.
- Reuse the current localized strings for session restoration, login redirection, and
  workspace load failure where they fit.
- Reuse the safe local return-path behavior already tested by
  `routes/AccountLogin/component.test.tsx`.
- Follow the existing route error patterns in the repository, while keeping this
  component Workshop-owned because its account transition and return URL are
  application-specific.
- Reuse the existing Vitest, Testing Library, jsdom setup, and memory-router approach;
  do not add a test dependency.

## Risks

### High — accidentally awaiting deferred data

- Cause: an `async` loader may await `Promise.all(...)` before returning it.
- Surface: every migrated Workshop navigation.
- Mitigation: loaders return the raw promise immediately and tests keep it unresolved
  while asserting that the destination page frame and loading fallback have committed.
- Verification signal: controlled pending loader promises never keep the previous route
  or login form mounted.

### High — deferred rejection bypasses authentication/error UX

- Cause: promise rejection occurs inside `<Await>` rather than the route's synchronous
  error boundary.
- Surface: anonymous deep links, expired sessions, and API failures.
- Mitigation: provide a shared Await error element using `useAsyncError`; handle 401 as
  login transition and other failures with the Workshop error state.
- Verification signal: rejected 401 and 500 promises never render React Router's
  default error UI.

### Medium — combined pages fragment their data ownership

- Cause: embedded forms/panels may independently fetch after loaders are removed.
- Surface: firearms, ammunition, and shooting/maintenance.
- Mitigation: keep one route-owned combined resource and pass data/reload callbacks
  downward.
- Verification signal: each combined route performs only its current expected request
  set and refreshes all affected panels together.

### Medium — competing redirect mechanisms

- Cause: both the account transport handler and the route error element can initiate
  login navigation.
- Surface: browser history and preserved return URL.
- Mitigation: both paths must use replacement semantics and the same local return URL;
  do not create redirect loops on `/account/login`.
- Verification signal: one final login history entry and a successful post-login return
  to the original protected URL.

### Medium — child boundary intercepts authentication

- Cause: the recipe-production route already owns a local `errorElement`.
- Surface: direct anonymous navigation to `/recipes/:recipeId/produce`.
- Mitigation: ensure status 401 receives the same login transition rather than generic
  recipe error copy.
- Verification signal: every protected route family reaches login without content or
  framework error UI.

### Low — non-auth failures become too generic

- Cause: a parent boundary catches all unmatched loader errors.
- Surface: operational error diagnosis.
- Mitigation: preserve a branded retry-capable error state for non-401 failures and
  retain structured console/telemetry reporting only for unexpected errors.
- Verification signal: a simulated 500 displays the Workshop error state and remains
  distinguishable from an auth redirect.

### Low — return URL regression

- Cause: rebuilding the login target in a second location.
- Surface: deep links with query strings and hashes.
- Mitigation: centralize or reuse the current local-path composition and keep the login
  component's existing destination validation.
- Verification signal: `/firearms/example?tab=history#notes` survives the round trip,
  while external and protocol-relative destinations remain rejected.

### Low — transient behavior requires a real browser

- Cause: jsdom cannot prove paint timing during a hard navigation.
- Surface: deployed user experience.
- Mitigation: pair deterministic route tests with one clean-profile browser capture.
- Verification signal: no frame contains "Unhandled Application Error" or React
  Router's default error content.

Security, data integrity, cache coherence, and external-service risk are otherwise
unchanged because authorization remains server-side and no persisted data is touched.

## Technical Debt

- Session restoration and protected page loaders currently begin independently.
  Deferred loaders make the route transition immediate but do not stop the initial
  anonymous request; shared Await 401 handling and the protected-shell boundary must
  make that transition deterministic.
- The existing `useResource` helper is unused. Do not expand it for this change; the
  accepted design retains loaders and uses React Router's documented promise handling.
- Existing automatic loader revalidation remains the refresh owner. Do not replace it
  with a global event bus or manual cross-page invalidation layer.
- `Maintenance/loader.ts` and `Recipes/loader.ts` are not registered by the current
  router. They do not block the change and should not be removed as unrelated cleanup.
- The Workshop has only one child-specific route error element and no protected-shell
  boundary. Add the parent boundary now because it is directly responsible for the
  defect; do not expand into a repository-wide error-component consolidation.
- The global unauthorized handler performs a hard browser navigation. Retain it for
  compatibility in this fix; evaluating SPA-native account-expiry coordination is a
  separate architectural change.

## Implementation Roadmap

1. **Add and prove the Workshop deferred-loader primitive.**
   - Owner: `frontend/shooter-workshop.client/src/routes/shared.tsx`.
   - Add typed raw-promise loader data, resolved async-value access, Suspense/Await
     presentation, and async error handling.
   - Keep static page structure outside the suspended data region.
   - Gate: focused tests prove immediate commit with an unresolved promise, loading
     fallback, resolved content, 401 transition, and branded non-401 failure.

2. **Create the Workshop route error boundary.**
   - Owner: `frontend/shooter-workshop.client/src/routes`.
   - Distinguish `ApiResponseError` status 401 from unexpected failures.
   - Preserve path, query, and hash in a local login return URL.
   - Use replacement navigation and existing Workshop state components.
   - Gate: focused component/router tests prove both 401 and non-401 behavior.

3. **Fix the login transition state.**
   - Owner: `routes/AccountLogin/component.tsx`.
   - Keep pending true after successful authentication and start replacement navigation.
   - Clear pending and expose existing feedback only for failed authentication or a
     navigation initiation failure.
   - Gate: a component test proves the button never re-enables on success and still
     re-enables on failure.

4. **Convert simple loaders to raw-promise deferred data.**
   - Owner: dashboard, recipe production, history, and reconciliation routes.
   - Keep existing service-call composition in loaders but return the unawaited promise.
   - Update route components to render their frame immediately and resolve data through
     Await.
   - Preserve period/page/type/ID semantics, recipe-specific not-found behavior, and
     automatic reconciliation revalidation after rebuild.
   - Gate: each route commits with pending data, then renders success/error.

5. **Convert combined mutation-heavy loaders atomically.**
   - Owner: firearms, firearm detail, ammunition/recipes, and
     shooting/maintenance.
   - Keep one deferred promise at each route loader for its existing parallel request
     group and pass resolved data to embedded components.
   - Preserve route actions, idempotency keys, validation feedback, correction semantics,
     automatic revalidation, and explicit archive navigation.
   - Gate: successful mutations revalidate to a new promise and refresh the affected
     page; rejected actions retain current data.

6. **Retain and verify the loader-based route tree.**
   - Owner: `frontend/shooter-workshop.client/src/routes/config.tsx`.
   - Attach the protected-shell error boundary.
   - Retain all page loaders, actions, `shouldRevalidate` hooks, and compatibility
     redirects.
   - Gate: configuration tests require deferred loaders and the protected error
     boundary without changing route topology.

7. **Add navigation and authentication regression coverage.**
   - Owner: Workshop Vitest suite.
   - Prove post-login destination elements commit without waiting for deferred loader
     promises and the login button remains pending until unmount.
   - Exercise representative protected URLs whose loader promises reject as
     unauthorized.
   - Assert replacement to the login route, exact encoded return URL, absence of
     framework error copy, and no redirect loop when already on login.
   - Include one representative route with query and hash and the recipe-production
     route because it has its own error element.
   - Gate: the focused tests fail against the current route tree and pass with the new
     boundary.

8. **Validate the unchanged compatibility surface.**
   - Run the Workshop test suite and production build through the existing workspace
     scripts.
   - Confirm shared services require no change by retaining their current account
     unauthorized tests.
   - Gate: no TypeScript errors, route test regressions, or changes to account API
     contracts.

9. **Perform exact-origin browser verification.**
   - Use a clean profile with no account cookie and open `/firearms` plus one other
     protected deep link directly.
   - Repeat at desktop and narrow mobile widths, then sign in and confirm the original
     URL is restored.
   - Throttle representative page reads and confirm navigation and destination loading
     UI appear immediately while data remains pending.
   - Gate: the login button never re-enables after success, no generic framework error
     is painted, no redirect loop occurs, and slow reads no longer hold the previous
     route on screen.

10. **Update architecture records and this handoff.**
   - Record raw-promise deferred loaders, Suspense/Await page data, and retained router
     revalidation in `docs/architecture/shooter-workshop.md`.
   - Record actual files, focused/full validation results, browser evidence, and any
     remaining timing risk.
   - Stop after all rendered Workshop page loaders return raw promises and the
     navigation, revalidation, authentication, and error gates pass. Router-bootstrap
     redesign, a global data cache, component-effect migration, and unrelated account
     cleanup require separate approval.

An independent post-validation MorWalPiz Code Reviewer pass is warranted because this
revision changes read ownership and mutation freshness across every protected Workshop
route. Review should focus on stale-response safety, missed post-mutation refreshes,
authentication transitions, and retained action/idempotency behavior.

## Testing Strategy

### Unit

- Error classification recognizes exported `ApiResponseError` status 401 without
  relying on localized text.
- Return URL composition preserves local pathname, search, and hash.
- Deferred-loader tests cover immediate commit, pending fallback, resolved data,
  async-error handling, and revalidation to a replacement promise.

### Integration/Behavior

- Not required in backend test projects because no API behavior changes.
- Preserve existing shared-service tests showing account 401 invokes the unauthorized
  handler and authentication endpoints do not.

### Component/Route

- Login success never re-enables submission before the route unmounts; login failure
  does.
- Every rendered data route commits its component before controlled service promises
  resolve and displays the existing loading state.
- Dashboard period, firearm-detail pagination, recipe ID, and history page/type changes
  ignore stale responses.
- Combined equipment, ammunition, and shooting/maintenance loaders issue their existing
  single parallel request groups.
- Every successful mutation triggers loader revalidation; validation/action failures do
  not.
- Anonymous 401 from a protected loader transitions to login with replacement.
- The encoded return URL exactly represents the original local deep link.
- The login route itself does not redirect back to itself.
- A non-401 loader failure renders the branded Workshop error state.
- Recipe-production 401 follows the same auth transition despite its child error
  element.
- Existing authenticated route configuration remains intact.

### Contract

- No contract changes. Verify the boundary consumes the existing
  `ApiResponseError.status` contract.

### Security

- External, protocol-relative, and backslash-ambiguous return destinations remain
  rejected by the login page.
- No session token becomes visible to JavaScript or browser storage.
- Server authorization remains required for every Workshop endpoint.

### Migration

- Not applicable; there is no data or schema transition.

### End-to-End

- Clean anonymous browser direct navigation to `/firearms`.
- Anonymous direct navigation to a deep route with query/hash.
- Login and return to the requested route.
- Throttled post-login destination reads: login disappears immediately, destination
  loading UI appears, button never re-enables.
- Throttled in-app navigation between two data routes: destination commits immediately
  and does not retain the previous page until data arrives.
- Representative successful mutations update equipment, ammunition,
  shooting/maintenance, and reconciliation views without a manual refresh.
- Session expiry while already inside a Workshop route.
- Desktop and narrow mobile checks for absence of the framework error flash.

## Migration And Rollback

- MongoDB: no migration.
- SQLite: not applicable.
- API contracts: no version or compatibility window required.
- Authentication: cookie and unauthorized response semantics remain unchanged.
- Cache: no purge or service-worker migration required.
- Configuration: no new values or secrets.
- Deployment order: deploy only the rebuilt Workshop SPA after its test/build gates.
  ServerAPI and shared-package deployment are unnecessary when implementation remains
  within the proposed boundary.
- Rollback: redeploy the previous Workshop SPA image. There is no data rollback and no
  server compatibility dependency.
- Monitoring: inspect client error telemetry and browser network/history behavior for
  repeated redirects or remaining default route errors after rollout.

## Open Questions

- During the non-reproducing refresh, was the browser refreshed on the final
  `/account/login?...` URL or was the original protected URL entered again? This affects
  timing documentation only and does not block the fix.
- Does production client telemetry currently capture React Router default boundary
  errors? If available, use it to confirm the affected route set and post-release
  disappearance; do not add a new telemetry system for this defect.
- During action-driven loader revalidation, should the current resolved content remain
  visible until the replacement promise resolves, or should the Await fallback appear?
  Prefer preserving resolved content with a localized updating indicator if React
  Router's revalidation behavior permits it without a custom cache; otherwise use the
  same loading fallback consistently and document the choice.
