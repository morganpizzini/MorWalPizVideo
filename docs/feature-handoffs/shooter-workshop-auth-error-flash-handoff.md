# Shooter Workshop Anonymous Route Error Flash Handoff

## Outcome

Prevent the Shooter Workshop from briefly displaying React Router's built-in
"Unhandled Application Error" when an anonymous browser opens a protected route such
as `/firearms`. Preserve the existing HttpOnly-cookie account flow, the requested
return URL, and the final navigation to `/account/login`.

Assumptions:

- The reported refresh that does not reproduce occurs after the browser has already
  reached the public login route. If the original protected URL itself is refreshed and
  no flash occurs, that timing difference should be captured during browser validation
  but does not change the missing error-boundary mechanism.
- This is a focused reliability refinement, not a redesign of authentication,
  navigation, or Workshop presentation.

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

Ranked hypotheses:

1. **Confirmed:** the protected loader's 401 rejection reaches React Router without a
   route error element while the hard redirect is still in flight.
2. **Contributing:** account restoration and matched loaders start independently, so
   the shell loading UI does not prevent the anonymous protected request.
3. **Not supported as the primary cause:** service-worker caching. The Workshop service
   worker is lifecycle-only and does not intercept account or API requests.

## Proposed Design

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

Keep the existing account transport and `window.location.replace` unauthorized handler
in this bounded fix. It remains the cross-request expiry fallback, while the route
error element makes matched loader failures deterministic and presentable. Removing
the hard redirect or re-architecting router bootstrap would expand the change into an
account-session coordination redesign and is not needed to correct the reported flash.

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

### Material Alternative

Delaying browser-router construction until `AuthProvider` completes session restoration
would prevent the initial protected requests entirely. It is not recommended for this
fix because it changes bootstrap ownership, public-route startup behavior, router
lifetime, and session coordination. Consider it separately only if request traces show
that avoiding the initial 401 traffic is an operational requirement after this defect
is fixed.

## Impacted Projects

- `frontend/shooter-workshop.client` — owns the route tree, account transition, UI
  states, and regression tests.
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

- `frontend/shooter-workshop.client/src/routes/config.tsx` — register the protected
  shell error element and ensure child-specific handling does not intercept 401
  incorrectly.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — require the protected
  shell to expose an error element.
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

### Generated/Migration

None. No generated output, package lock, database migration, or persisted data changes.

### Documentation

- `docs/feature-handoffs/shooter-workshop-auth-error-flash-handoff.md` — this
  implementation handoff; update after implementation with actual files, validation,
  and residual risks.

### Verify Only

- `frontend/shooter-workshop.client/src/main.tsx` — confirm the existing unauthorized
  handler still preserves the return URL and remains unchanged.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx` — confirm session restore
  behavior remains unchanged.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` — confirm authenticated
  and anonymous shell states remain unchanged.
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
| Frontend/UI | Replace the framework error flash with the existing Workshop transition state and deterministic login navigation. |
| Database/Data | Not applicable; no persistence or data-shape changes. |
| Contracts/Models | No contract changes; reuse exported `ApiResponseError`. |
| Authentication/Authorization | Presentation and routing only. Cookie validation and authorization remain server-authoritative. |
| Validation | Preserve safe local return-path handling; do not accept external destinations. |
| Cache | Not applicable; no cache policy or service-worker behavior changes. |
| Configuration/Secrets | Not applicable; no settings or secret changes. |
| Logging/Telemetry | Expected anonymous 401 should not be logged as an unhandled UI error. Existing transport/server telemetry remains authoritative. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Existing SPA test/build/image workflow is sufficient. Browser verification against the deployed exact origin remains required. |
| Backward Compatibility | Existing links, login return URLs, cookies, authenticated routes, and API responses remain compatible. |

## Reuse Opportunities

- Reuse `ApiResponseError` from `@morwalpizvideo/services`; do not inspect arbitrary
  message text.
- Reuse `LoadingState` and `ErrorState` from
  `frontend/shooter-workshop.client/src/components/States.tsx`.
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

- Session restoration and protected data loaders currently begin independently. This
  causes avoidable anonymous requests, but it does not block the focused presentation
  fix. Defer router/session bootstrap coordination unless request-volume or audit
  evidence makes it necessary.
- The Workshop has only one child-specific route error element and no protected-shell
  boundary. Add the parent boundary now because it is directly responsible for the
  defect; do not expand into a repository-wide error-component consolidation.
- The global unauthorized handler performs a hard browser navigation. Retain it for
  compatibility in this fix; evaluating SPA-native account-expiry coordination is a
  separate architectural change.

## Implementation Roadmap

1. **Create the Workshop route error boundary.**
   - Owner: `frontend/shooter-workshop.client/src/routes`.
   - Distinguish `ApiResponseError` status 401 from unexpected failures.
   - Preserve path, query, and hash in a local login return URL.
   - Use replacement navigation and existing Workshop state components.
   - Gate: focused component/router tests prove both 401 and non-401 behavior.

2. **Attach the boundary at the protected shell.**
   - Owner: `frontend/shooter-workshop.client/src/routes/config.tsx`.
   - Add the boundary to the parent containing every operational route.
   - Check the recipe-production child boundary and align only its 401 behavior if
     necessary.
   - Gate: route configuration test requires the shell error element and retains all
     existing routes, loaders, actions, and redirects.

3. **Add direct-navigation regression coverage.**
   - Owner: Workshop Vitest suite.
   - Start a memory router on representative protected URLs with a loader that rejects
     as unauthorized.
   - Assert replacement to the login route, exact encoded return URL, absence of
     framework error copy, and no redirect loop when already on login.
   - Include one representative route with query and hash and the recipe-production
     route because it has its own error element.
   - Gate: the focused tests fail against the current route tree and pass with the new
     boundary.

4. **Validate the unchanged compatibility surface.**
   - Run the Workshop test suite and production build through the existing workspace
     scripts.
   - Confirm shared services require no change by retaining their current account
     unauthorized tests.
   - Gate: no TypeScript errors, route test regressions, or changes to account API
     contracts.

5. **Perform exact-origin browser verification.**
   - Use a clean profile with no account cookie and open `/firearms` plus one other
     protected deep link directly.
   - Repeat at desktop and narrow mobile widths, then sign in and confirm the original
     URL is restored.
   - Gate: no generic framework error is painted, no redirect loop occurs, and
     authenticated navigation remains unchanged.

6. **Update this handoff after implementation.**
   - Record actual files, focused/full validation results, browser evidence, and any
     remaining timing risk.
   - Stop after this anonymous/error-transition defect is corrected; router-bootstrap
     redesign and unrelated account cleanup require separate approval.

Independent post-validation review is not required for this bounded UI routing fix if
the focused tests, full Workshop suite/build, and exact-origin browser evidence pass.
Request a separate MorWalPiz Code Reviewer pass only if implementation changes shared
account transport or router bootstrap ownership.

## Testing Strategy

### Unit

- Error classification recognizes exported `ApiResponseError` status 401 without
  relying on localized text.
- Return URL composition preserves local pathname, search, and hash.

### Integration/Behavior

- Not required in backend test projects because no API behavior changes.
- Preserve existing shared-service tests showing account 401 invokes the unauthorized
  handler and authentication endpoints do not.

### Component/Route

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
  ServerAPI and shared package deployment are unnecessary when implementation remains
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
