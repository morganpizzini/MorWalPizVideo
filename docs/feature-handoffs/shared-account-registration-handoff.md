# Shared Account Registration Handoff

## Outcome

Add public-account registration to `shooter-workshop.client` and remove duplicated
login/registration presentation from the Workshop and public clients.

Assumptions:

- The existing public-account API, cookie session, CSRF flow, contracts, and legal
  evidence behavior remain authoritative and unchanged.
- "Unify the code" means extracting genuinely shared account-form presentation into
  `@morwalpiz/layout`, not moving application routing, authentication providers,
  localization, or navigation into the layout package.
- Workshop legal links must resolve safely. Anonymous Workshop routes for `/privacy`,
  `/terms`, and `/cookie-policy` will redirect to the existing canonical public legal
  pages rather than duplicating legal content.
- This is a refinement of the existing account experience, not a visual redesign.

Implementation boundary: deliver the shared account presentation, migrate both
clients' login and registration surfaces to it, add Workshop registration and legal
redirect routes, and add focused tests. Stop before changing backend account behavior,
introducing account recovery/verification, or redesigning either application's visual
system.

## Current Evidence

- The governing account feature requires server-owned legal versions/timestamps,
  retention of the registration-issued session, authoritative session refresh, safe
  local return URLs, no bearer/localStorage workaround, and no automatic registration
  retry (`docs/features/accounts-and-channels.md`, "Registration, legal evidence,
  session, and return URLs").
- Workshop is a separate SPA that must reuse shared packages and visual identity
  without importing public-client-local source
  (`docs/architecture/shooter-workshop.md`, "Architectural Direction and Delivery
  Baseline").
- `frontend/morwalpizvideo.client/DESIGN.md` establishes the incumbent visual
  authority: dark graphite surfaces, white Manrope text, and selective yellow action
  emphasis.
- `frontend/fe-packages/models/src/publicAccount.ts` already owns registration, login,
  session, profile, and CSRF TypeScript contracts.
- `frontend/fe-packages/services/src/publicAccountService.ts` already owns account
  registration/login/session operations, fresh CSRF acquisition, credential inclusion,
  and status-aware errors.
- `frontend/fe-packages/services/src/endpoints-frontend.ts` centralizes the public
  account endpoints.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx` already implements
  login and registration, including pending state, field errors, legal acknowledgement,
  session refresh, safe return handling, and one-shot post-registration state.
- `frontend/morwalpizvideo.client/src/routes/account/returnUrl.ts` rejects external,
  protocol-relative, backslash, and account-authentication-loop destinations.
- `frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx` implements
  Workshop login only. Its local return-path helper is less restrictive and its UI
  collapses service errors into a generic message.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx` owns Workshop session
  restore, login, logout, and refresh.
- `frontend/shooter-workshop.client/src/routes/config.tsx` exposes login anonymously
  but has no registration or legal routes.
- `frontend/fe-packages/layout/src/index.ts` exports no account presentation today.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` already exposes
  anonymous, CSRF-protected, rate-limited registration and sets the account session
  cookie.
- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs` already owns normalization,
  uniqueness checks, password hashing, legal evidence, persistence, and session
  issuance.
- `MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs` is the authoritative
  backend request/response and validation contract.
- `MorWalPizVideo.ServerAPI/Program.cs` admits credentialed account requests from the
  configured Workshop origin.

Current end-to-end registration flow:

1. The client obtains a no-store CSRF token.
2. The client posts the shared registration contract to `/api/v1/accounts`.
3. Domain logic normalizes and validates identity, checks uniqueness, hashes the
   password, records server-owned legal evidence, creates the account and session, and
   returns an authenticated session.
4. ServerAPI sets the HttpOnly session cookie.
5. The client refreshes authoritative session state and replaces navigation with the
   login route carrying one-shot success or session-warning state.

No backend registration capability is missing. The missing product behavior is the
Workshop route and shared frontend presentation.

## Proposed Design

### Shared presentation boundary

Add a controlled, presentational account form component to `@morwalpiz/layout`. It
should own:

- the account card/form structure;
- accessible labels, descriptions, field-error association, and invalid state;
- pending, form-error, success, and warning regions;
- common login/register cross-link placement;
- composable field and legal-content slots so login and registration share one shell
  without a mode-driven component accumulating application behavior.

The component must receive copy, links, field content, status content, submit content,
and event handlers through typed props. It must not:

- call account services;
- access either authentication provider;
- navigate or parse return URLs;
- embed Italian or Workshop-specific copy;
- own legal wording or canonical destinations;
- persist authentication state.

This keeps the package reusable across applications while avoiding a coupled
"universal auth component" that would have to know two providers and routing policies.

### Client orchestration

- Public client login and registration retain their current service calls, provider
  interaction, navigation behavior, field mapping, and localized copy, but render
  through the shared presentation.
- Workshop login adopts the shared presentation and the same return-URL policy as the
  public client. A shared pure return-URL helper may move into the layout package only
  if it has no router or application dependency; otherwise duplicate policy must be
  eliminated in an application-neutral utility package already consumed by both apps.
- Workshop registration is an anonymous route. It uses
  `registerPublicAccount`, both required legal acknowledgement values, Workshop
  `AuthProvider.refresh`, and the shared presentation.
- After successful registration, Workshop keeps the API-issued cookie session,
  performs one authoritative refresh, and replaces history with `/account/login`.
  Navigation state distinguishes full success from account-created/session-refresh
  warning. It never reposts registration automatically.
- Workshop `/privacy`, `/terms`, and `/cookie-policy` are anonymous redirect routes to
  the canonical public legal pages. Redirects must not accept or forward arbitrary
  return destinations.

### UX direction

Impeccable mode is **Operate**: the visitor's job is to complete authentication with
clear validation and recovery from correctable input errors. The narrowest applicable
Impeccable workflow is `shape`, followed during implementation by `harden` for account
errors, pending behavior, accessibility, and responsive edge cases.

This is refinement, not redesign. Preserve the incumbent dark account-card treatment,
Manrope typography, yellow primary action, and each application's surrounding layout.
The focal sequence is identity fields, password confirmation, explicit legal
acknowledgement, submit, then the alternate login/registration action. Minimum visual
evidence is one login and one registration capture in each application at desktop and
mobile widths, including one validation-error state and one pending state.

## Impacted Projects

- `frontend/fe-packages/layout`: add shared account presentation and styling.
- `frontend/morwalpizvideo.client`: consume the shared presentation without changing
  existing public-account behavior.
- `frontend/shooter-workshop.client`: add registration and legal redirects, align login
  behavior, copy, session refresh, and return-path handling.
- `MorWalPizVideo.BackOffice.Tests`: verify only; existing endpoint behavior remains the
  backend regression boundary.
- `MorWalPizVideo.ServerAPI`: verify only; no endpoint or composition change expected.
- `MorWalPizVideo.Domain`: verify only; no account-domain change expected.
- `MorWalPiz.Contracts`: verify only; no contract change expected.
- `MorWalPizVideo.Models`: verify only; no persistence change expected.
- `MorWalPizVideo.AppHost`: verify only; it already composes Workshop with ServerAPI.
- Other `@morwalpiz/layout` consumers: regression verification because the shared
  package export/style graph changes; no intended source changes.
- BackOffice SPA, shop client, Shooting ITA, TelePrompter, stage designer, VideoImporter,
  and other backend services: not applicable to product behavior.

## Impacted Files

### Modify

- `frontend/fe-packages/layout/src/index.ts` — export the shared account presentation.
- `frontend/fe-packages/layout/src/styles/index.scss` — include narrowly scoped account
  presentation styles.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx` — replace local
  account shell markup with the shared component while retaining orchestration.
- `frontend/morwalpizvideo.client/src/routes/account/style.scss` — remove only styles
  transferred to layout; retain app-specific page composition.
- `frontend/morwalpizvideo.client/src/routes/account/Components.test.tsx` — protect
  existing login/registration behavior through the shared rendering boundary.
- `frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx` — consume the
  shared presentation and aligned safe-return behavior.
- `frontend/shooter-workshop.client/src/routes/AccountLogin/component.test.tsx` — cover
  status, pending, service errors, cross-link, and return destinations.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — add anonymous registration
  and legal redirect routes.
- `frontend/shooter-workshop.client/src/routes/pages.tsx` — export the new route
  components according to current route organization.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx` — expose or reuse the
  minimum authoritative refresh behavior needed after registration; do not duplicate
  session state logic.
- `frontend/shooter-workshop.client/src/i18n.tsx` — add registration, legal,
  validation, status, and service-error copy.
- `frontend/shooter-workshop.client/src/main.scss` — remove duplicated account-card
  styling while preserving Workshop page-level composition.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — assert registration
  and legal routes are anonymous while protected routes remain protected.

### Add

- `frontend/fe-packages/layout/src/components/AccountForm/` — typed shared account form
  shell and component-scoped styles, following the package's existing component naming
  and export pattern.
- `frontend/shooter-workshop.client/src/routes/AccountRegister/component.tsx` — Workshop
  registration orchestration and controlled fields.
- `frontend/shooter-workshop.client/src/routes/AccountRegister/index.ts` — route export.
- `frontend/shooter-workshop.client/src/routes/AccountRegister/component.test.tsx` —
  focused registration behavior, validation, session refresh, and navigation tests.
- Workshop legal redirect route module under the existing routes organization — fixed
  canonical redirects for privacy, terms, and cookie policy.
- A shared safe-return helper and focused tests in the narrowest existing shared
  frontend package only if the two clients cannot consume one current implementation
  without an app-local import.

### Generated/Migration

None. No database, EF, MongoDB, code-generation, or lockfile change is expected.

### Documentation

- `docs/feature-handoffs/shared-account-registration-handoff.md` — this architecture
  handoff; update after implementation with actual files, validation, and residual
  risks.
- `docs/architecture/frontend.md` — update only if the implemented shared-component or
  anonymous Workshop route boundary materially differs from the documented target.
- `docs/features/accounts-and-channels.md` — update delivery evidence after verified
  implementation; do not rewrite governing account policy.

### Verify Only

- `frontend/fe-packages/models/src/publicAccount.ts`
- `frontend/fe-packages/services/src/publicAccountService.ts`
- `frontend/fe-packages/services/src/publicAccountService.test.ts`
- `frontend/fe-packages/services/src/apiTransport.ts`
- `frontend/fe-packages/services/src/endpoints-frontend.ts`
- `MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs`
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs`
- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs`
- `MorWalPizVideo.Models/Models/PublicAccount.cs`
- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs`
- `MorWalPizVideo.ServerAPI/Program.cs`
- `MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs`
- `MorWalPizVideo.AppHost/Program.cs`
- `.github/workflows/ci.yml`
- `.github/workflows/deploy-shooter-workshop-client.yml`
- `frontend/shooter-workshop.client/Dockerfile`

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | No behavior change. Existing anonymous CSRF-protected registration endpoint is reused. |
| Frontend/UI | Shared account presentation; Workshop gains registration and fixed legal redirects; public UI preserves behavior. |
| Database/Data | No schema or data change. Existing account/session documents and indexes remain authoritative. |
| Contracts/Models | No contract change. Reuse existing shared TypeScript and .NET contracts. |
| Authentication/Authorization | Preserve HttpOnly cookie session, credentialed requests, CSRF, anonymous account/legal routes, and protected Workshop shell. |
| Validation | Preserve server authority and field errors; add matching client constraints, password confirmation, legal checks, pending state, and status-aware errors. |
| Cache | Account CSRF/session reads remain no-store. No output-cache tag changes. |
| Configuration/Secrets | No new setting or secret. Existing exact Workshop origin remains required. |
| Logging/Telemetry | No new frontend logging of credentials or tokens. Existing API telemetry remains the operational signal. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Shared layout rebuild precedes both clients; Workshop image/workflow already builds shared packages. Browser origin/cookie behavior needs deployed verification. |
| Backward Compatibility | Existing public routes and API contracts remain stable. New Workshop routes are additive. Shared CSS must remain component-scoped. |

## Reuse Opportunities

- Reuse `PublicAccountRegistrationRequest`, `PublicAccountLoginRequest`, and
  `PublicAccountSession` from `@morwalpizvideo/models`.
- Reuse `registerPublicAccount`, `loginPublicAccount`, session refresh, and structured
  service errors from `@morwalpizvideo/services`.
- Reuse the public client's registration field mapping, legal acknowledgement
  behavior, post-registration state distinction, and safe-return policy.
- Reuse Workshop `AuthProvider.refresh` rather than adding a second session source.
- Follow the layout package's existing component/export/style conventions.
- Reuse existing route-aware Vitest/Testing Library helpers and service mocks in each
  client.
- Reuse existing ServerAPI endpoint behavior tests as the backend regression boundary.

Avoid:

- importing public-client-local components into Workshop;
- copying the existing registration form;
- embedding navigation/provider/service behavior in layout;
- adding another API wrapper or account DTO;
- storing the account token in browser-readable storage;
- introducing a repository-wide state store for this feature.

## Risks

| Rank | Cause and surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Registration succeeds but authoritative refresh fails, causing duplicate submission or false failure. | Treat account creation and refresh as separate outcomes; retain cookie; replace to login with warning; never retry registration automatically. | Test asserts one registration request and warning navigation after refresh failure. |
| High | Cross-origin cookie/CORS differences make Workshop registration appear successful but leave it anonymous. | Preserve account transport credentials/CSRF and exact origin; verify on deployed HTTPS Workshop origin. | Browser shows Secure HttpOnly cookie and authenticated session request after registration. |
| High | Legal links or acknowledgements are omitted or routed to the Workshop wildcard. | Submit both required booleans; add fixed anonymous legal redirects; retain server-owned legal evidence. | Route tests and registration request assertion; deployed links reach canonical content. |
| Medium | Shared component absorbs app behavior and couples two providers/routers. | Keep it controlled and presentational with slots/typed props. | Layout component has no service, router, provider, or localization imports. |
| Medium | Return URL permits external navigation or login/register loops. | Use one tested local-path policy for both clients and reject all auth-loop destinations. | Negative tests cover absolute, protocol-relative, backslash, login, and register paths. |
| Medium | Shared styles regress unrelated layout consumers. | Scope styles to the component and preserve app page wrappers. | Both target client suites/builds pass; smoke check another layout consumer. |
| Medium | Service errors become generic, hiding 409/429/validation recovery. | Preserve structured field/form/status errors and accessible announcements. | Tests exercise conflict, rate limit, validation, and unexpected server response. |
| Low | Localization additions are incomplete or hard-coded in layout. | Keep all user-facing copy app-owned and use Workshop i18n keys. | Search/test shows no Workshop/public copy embedded in shared component. |
| Low | Deployment workflow behavior differs from architecture documentation. | Treat current workflow as authoritative and validate its actual artifact/deploy path before release. | Workflow run identifies the deployed image/version and completes expected gates. |

## Technical Debt

- Workshop's return-path helper is weaker than the public helper. This directly affects
  the new route and must be resolved now, not deferred.
- Workshop login currently lacks pending, field-error, and status-aware service states.
  Resolve now because the shared presentation would otherwise preserve divergent and
  inaccessible behavior.
- The layout package has no dedicated test script. Do not add package-wide test
  infrastructure solely for this change; test shared behavior through both consuming
  application suites unless the component contains independent logic that cannot be
  covered there.
- Backend username annotations and Domain character policy differ. This does not block
  the frontend change; preserve the server error and track contract alignment
  separately rather than broadening this scope.
- Workshop deployment comments and active deploy steps conflict. This is operational
  debt, not a reason to alter the workflow in this feature; verify the active path and
  record evidence.
- Account recovery, verification, deletion, moderation, and global administration are
  explicitly out of scope.

## Implementation Roadmap

1. **Lock behavior and shared API**
   - Confirm the controlled layout component props against both existing login forms
     and the public registration form.
   - Confirm fixed canonical destinations for the three Workshop legal redirects.
   - Gate: no application service/provider/router dependency is required by the shared
     component.

2. **Extract shared account presentation**
   - Add the typed layout component and component-scoped styles.
   - Export it through the layout package.
   - Preserve semantic form structure, label association, `aria-invalid`,
     `aria-describedby`, live status regions, keyboard submission, and disabled/pending
     feedback.
   - Gate: layout package builds before either consumer changes.

3. **Migrate the public client without behavior change**
   - Replace its local `AccountForm` shell with the shared presentation.
   - Retain all existing service, refresh, navigation, one-shot state, legal, and
     return-URL behavior.
   - Remove only presentation styles made redundant by extraction.
   - Gate: focused public account tests pass and cover both login and registration
     rendering/submission.

4. **Unify return-path policy**
   - Establish one app-neutral safe-local-path helper or equivalent shared policy.
   - Reject external/protocol-relative/backslash destinations and exact login/register
     loops, including slash-normalized variants.
   - Gate: both clients pass the same positive and negative return-path cases.

5. **Harden Workshop login**
   - Render it with the shared component.
   - Add pending/disabled behavior, field/form errors, status-aware service messages,
     and a registration cross-link preserving only a validated return URL.
   - Gate: login tests prove one submission, safe navigation, accessible errors, and
     cross-link behavior.

6. **Add Workshop registration**
   - Add controlled username, email, password, confirmation, and legal
     acknowledgement fields using existing contracts/services.
   - On success, refresh authoritative Workshop session and replace to login with
     success or session-warning state.
   - Never place credentials/session tokens in URLs, navigation state, logs, or browser
     storage.
   - Gate: tests cover success, conflict, rate limit, field errors, legal rejection,
     password mismatch, refresh failure, and no duplicate registration.

7. **Add anonymous route integration**
   - Register `/account/register` outside the protected Workshop shell.
   - Add fixed anonymous legal redirect routes and ensure the wildcard does not capture
     them.
   - Keep all Workshop domain routes protected.
   - Gate: route-manifest tests distinguish anonymous and protected routes.

8. **Run visual and accessibility refinement**
   - Apply the Impeccable `harden` lens to the Operate surface without redesign.
   - Verify mobile/desktop layout, zoom, keyboard order, focus visibility, field-error
     announcements, pending state, long localized strings, and service-failure states.
   - Gate: capture the minimum visual evidence described in Proposed Design.

9. **Validate delivery and update documentation**
   - Build shared packages in models/services/layout order, then both clients.
   - Run focused and application test suites without weakening the nonempty-test gate.
   - Verify deployed HTTPS cookie/CORS/session behavior at the Workshop origin.
   - Update this handoff with actual files, results, visual evidence, and residual risk.
   - Gate: no backend/API/config change was needed; if one becomes necessary, stop for
     architecture review rather than expanding the patch implicitly.

Independent post-validation review is warranted because this changes shared UI consumed
by multiple applications and touches authentication, CSRF, cross-origin cookies, legal
acknowledgement, and safe redirects. The reviewer should focus on behavior preservation,
security boundaries, accessibility, and shared-package regression.

## Testing Strategy

### Shared/component behavior

- Render login and registration compositions through the shared component in both
  consuming applications.
- Assert semantic labels, descriptions, field errors, live regions, pending state,
  disabled submit, and alternate-action links.
- Verify no user-entered password appears in rendered status, URL, or navigation state.

### Public client regression

- Existing login success refreshes authoritative state before navigation.
- Existing registration sends both legal booleans, retains the issued session, and
  replaces to login with success.
- Registration refresh failure produces the distinct warning and does not repost.
- Existing field/status errors remain visible and associated.
- Safe return URLs preserve valid local destinations and reject unsafe/auth-loop paths.

### Workshop component and route tests

- Registration success, service validation error, duplicate identity conflict, rate
  limiting, unexpected server error, password mismatch, missing legal acknowledgement,
  pending double-submit prevention, and refresh failure.
- Login success/error/pending states and login-to-registration return URL.
- Registration-to-login return URL.
- `/account/login`, `/account/register`, `/privacy`, `/terms`, and `/cookie-policy`
  remain anonymous; Workshop domain routes remain guarded.
- Legal redirect destinations are fixed and cannot be influenced by query parameters.

### Service/contract regression

- Keep existing public-account service tests for credential mode, fresh CSRF, no-store
  reads, status/field error preservation, and logout CSRF.
- No new shared-service test is required unless service behavior changes unexpectedly.

### Backend behavior

- Existing endpoint tests remain the regression suite for CSRF-required registration,
  missing legal acknowledgement, cookie-backed session restore, no-store responses,
  and anonymous public pages.
- No new backend test is required when no backend behavior changes.

### End-to-end/deployed checks

- Register a disposable non-production account through the deployed Workshop origin.
- Confirm the browser sends the CSRF header, accepts the Secure HttpOnly cookie, and
  restores an authenticated session.
- Confirm duplicate username/email, rate-limit messaging, legal links, refresh failure
  handling where safely inducible, logout, and a protected-route return journey.
- Test at narrow mobile and desktop widths with keyboard-only navigation and browser
  zoom.

## Migration And Rollback

- **MongoDB:** no schema migration or backfill. Existing account and session collections
  and unique indexes are reused. Production index/audit state remains a release
  prerequisite, not a change in this feature.
- **SQLite:** not applicable.
- **API contracts:** additive frontend route/UI change only; no compatibility window is
  needed because existing request/response shapes are unchanged.
- **Authentication:** preserve current cookie, CSRF, SameSite/Secure, rate-limit, and
  exact-origin policies. Deploy ServerAPI only if configuration evidence shows the
  Workshop origin is absent in the target environment; do not broaden CORS.
- **Cache:** no migration. CSRF/session requests remain no-store.
- **Configuration:** no new values or secrets. Validate the existing Workshop origin,
  TLS, and API base URL in the target environment.
- **Deployment order:** publish the compatible shared layout package/workspace build as
  part of each client artifact; deploy the public client regression-preserving change,
  then Workshop registration. Because packages are workspace-built, artifacts must be
  produced from one coherent commit.
- **Monitoring:** watch registration 4xx/409/429/5xx rates, session-restore failures,
  browser CORS failures, and client error telemetry without logging credentials/tokens.
- **Rollback:** roll back both client artifacts to the previous shared-layout version.
  Existing accounts and sessions remain valid; do not delete registration-created data.
  If only Workshop must be disabled, remove/disable its registration route and link
  while leaving the backend endpoint available to the public client. Rollback must not
  weaken CSRF, cookie, legal, or CORS controls.

## Open Questions

- Confirm the canonical production URLs for Workshop privacy, terms, and cookie-policy
  redirects. The architecture recommendation is to use the existing public client
  legal pages; no legal copy should be duplicated.
- Confirm whether Workshop registration copy should use the current Workshop locale
  only or expose a language selector. The architecture defaults to the existing
  Workshop localization behavior and does not add a selector.

## Implementation Record

Implemented on 2026-11-15:

- Added the controlled shared `AccountForm` presentation to
  `frontend/fe-packages/layout/src/components/AccountForm.tsx` and exported it from
  `frontend/fe-packages/layout/src/index.ts`.
- Migrated the public client's account login, registration, and profile shell to the
  shared component without changing its account service, refresh, navigation, legal,
  or safe-return behavior.
- Updated Workshop login to use the shared shell, pending state, structured field
  errors, account-service status messages, registration cross-link, and authentication
  loop-safe return paths.
- Added Workshop registration at `/account/register` using the existing shared
  registration contract/service, both legal acknowledgement values, password
  confirmation, Workshop session refresh, and one-shot success/session-warning
  navigation state.
- Added anonymous Workshop legal redirect routes for `/privacy`, `/terms`, and
  `/cookie-policy`, with fixed destinations on `https://morwalpiz.com`.
- Added Workshop localization strings and account presentation styling for the shared
  form's footer and legal acknowledgement.
- Added focused Workshop registration and route coverage and retained the existing
  login/public account tests.

Validation completed:

- `yarn --cwd frontend build:shared` — passed.
- `yarn --cwd frontend workspace shooter-workshop.client test --run src/routes/AccountLogin/component.test.tsx src/routes/AccountRegister/component.test.tsx src/routes/config.test.tsx` — 3 files, 6 tests passed.
- `yarn --cwd frontend workspace shooter-workshop.client build` — passed.
- `yarn --cwd frontend workspace morwalpizvideo.client test --run src/routes/account/Components.test.tsx` — 2 tests passed.
- `yarn --cwd frontend workspace morwalpizvideo.client build` — passed.

The public build emitted existing non-blocking warnings for the runtime
`env-config.js` script, stale Browserslist data, and bundle size. No backend, contract,
database, cache, or secret/configuration changes were required.

Residual risks:

- A real HTTPS browser check against the deployed Workshop origin is still required
  for CORS, CSRF, Secure/HttpOnly cookie acceptance, session restoration, and fixed
  legal redirects. Static tests and local builds cannot prove deployed behavior.
- The Workshop auth provider still exposes login failures as a boolean, so Workshop
  login cannot display server field-specific errors in the same detail as the public
  client; registration preserves structured field errors because it calls the shared
  service directly. This is intentionally limited to the requested registration and
  shared presentation change.
- Existing unrelated dirty-worktree changes were preserved.

## Post-implementation defect investigation (2026-11-15)

### Outcome

Correct the two reported Workshop account-surface defects and harden public account
creation against automated account spam:

1. registration repeatedly returns the security-request-expired message;
2. the global Report an issue launcher appears on login and registration and its
   submission can fail with connection-oriented copy;
3. public registration has some security controls, but its current abuse protection is
   not sufficient for unrestricted production self-service registration.

The registration and feedback failures have a credible shared mechanism: both use the
account transport, both require the CSRF response token plus its matching cookie, and
the production Workshop artifact is configured to call a separate API site directly.
Modern third-party-cookie policy can therefore allow the CSRF response while
withholding the cookie required by the subsequent POST. Repeated page reloads cannot
repair a browser policy decision.

This is an Impeccable **Operate** hardening refinement, not a redesign. Preserve the
current account form, copy hierarchy, visual system, and public-account boundary.

Assumptions:

- `https://workshop.morwalpiz.com` is the browser-facing Workshop origin.
- ServerAPI remains the owner of public account registration and feedback intake.
- A production same-origin `/api` route is the accepted transport direction already
  recorded in the Workshop UI feedback handoff.
- Email verification, account recovery, and account deletion remain outside this
  defect slice unless the product owner separately approves them.

Implementation boundary: establish same-origin Workshop API transport, hide feedback
outside the authenticated Workshop shell, correct error classification, and add
registration-specific abuse controls using existing repository infrastructure. Stop
before redesigning account UI, changing Workshop domain behavior, or adding a new
identity system.

### Current Evidence

Verified facts:

- `frontend/shooter-workshop.client/src/routes/AccountRegister/component.tsx` calls
  `registerPublicAccount`. A `403` is mapped by
  `frontend/fe-packages/services/src/publicAccountService.ts` to the reported
  security-request-expired message.
- `registerPublicAccount` first gets `api/v1/account/csrf`, then posts
  `api/v1/accounts` with `X-CSRF-TOKEN`.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` returns `403` when
  `PublicAccountSecurity.HasValidCsrf` cannot match the header to the
  `public_account_csrf` cookie.
- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs` creates a
  host-only, HttpOnly, Secure, `SameSite=None` production cookie. The browser must
  still accept and return that cookie; CORS alone cannot override third-party-cookie
  blocking.
- `frontend/shooter-workshop.client/Dockerfile` bakes `VITE_API_BASE_URL` into the
  client image. The deployment workflow supplies `WORKSHOP_API_BASE_URL`, while
  `frontend/shooter-workshop.client/nginx.conf` serves only static SPA files and has no
  `/api` reverse proxy. Production therefore supports a direct cross-site browser/API
  topology.
- The existing Workshop UI feedback handoff already identifies third-party-cookie
  withholding as a high-risk session failure and accepts a same-origin Workshop
  `/api` route as the target correction.
- `frontend/shooter-workshop.client/src/main.tsx` places `FeedbackProvider` outside the
  router. `FeedbackProvider` always renders the launcher, so the button is
  unconditionally present on login, registration, and legal routes. This defect is
  confirmed from source and does not depend on deployment.
- Feedback submission uses `accountApiService` and
  `MorWalPizVideo.ServerAPI/Controllers/FeedbackController.cs` enforces the same CSRF
  header/cookie match. It can therefore fail through the same cookie path as
  registration.
- `FeedbackProvider` reports an explicit offline message only when
  `navigator.onLine` is false. Other fetch/CORS failures fall through to
  "check your connection" copy, which can incorrectly imply local connectivity is the
  cause.
- In-process account and feedback endpoint tests prove cookie/CSRF behavior with an
  HTTP client's cookie container. They do not exercise the deployed Workshop origin,
  browser third-party-cookie policy, production preflight, or the image's baked API
  base URL.
- No ServerAPI test currently proves the exact Workshop-origin credentialed preflight.

Evidence-ranked cause assessment:

1. **High confidence:** the direct cross-site API topology allows the CSRF cookie to be
   withheld, producing the repeatable registration `403`; the same topology affects
   feedback. This matches an already documented Workshop session risk. Exact deployed
   cookie rejection still requires browser evidence.
2. **Confirmed independent defect:** feedback is mounted globally rather than under
   the authenticated shell.
3. **Lower-probability deployment alternative:** stale ServerAPI/gateway authorization
   or CORS configuration can also block the CSRF flow. Architecture documentation
   records prior deployed/source skew for the anonymous CSRF endpoint. The release
   investigation must capture the actual CSRF GET, preflight, cookie storage reason,
   and POST before changing application authorization.

Current registration security posture:

- Positive controls:
  - CSRF header/cookie matching;
  - fixed-window rate limiting at 10 combined registration/login requests per five
    minutes per observed remote IP by default;
  - server-side validation and legal acknowledgement;
  - normalized username/email checks plus unique MongoDB indexes;
  - PBKDF2-SHA256 password hashing with a random 32-byte salt and constant-time
    verification;
  - HttpOnly session cookies and server-side hashed session tokens.
- Gaps:
  - CSRF prevents cross-site request forgery; it is not proof that the caller is human.
  - There is no CAPTCHA/challenge on account creation.
  - Registration and login share one limiter, allowing account creation traffic to
    consume login capacity.
  - The limiter is process-local and resets on restart; it is not coordinated across
    scaled ServerAPI instances.
  - ServerAPI does not explicitly configure trusted forwarded-header processing before
    rate limiting. A reverse proxy can therefore collapse users into one proxy IP, or
    an unsafe forwarding configuration could trust spoofed client addresses.
  - There is no email verification, disposable-email policy, account-creation
    monitoring, or cleanup/quarantine state for abusive inactive accounts.
  - No test proves a `429` registration boundary.

Conclusion: no direct account-takeover flaw was found in this trace, but registration
is only **partially protected** and should not be described as spam-resistant in its
current state.

### Proposed Design

#### Same-origin Workshop account transport

Expose ServerAPI through `https://workshop.morwalpiz.com/api/...` at the Workshop
ingress/nginx layer. Build the production SPA with a relative API base so account,
feedback, session, and Workshop requests are first-party from the browser's
perspective.

The `/api/` location must:

- proxy only to the environment-owned ServerAPI upstream;
- preserve method, body, status, and required response headers;
- forward host, scheme, and a trustworthy client-address chain;
- never fall back to `index.html`;
- never log cookie, CSRF, authorization, password, or feedback body values;
- expose a deployment/readiness failure when the upstream is absent rather than
  returning SPA content.

Keep the current direct ServerAPI origin temporarily available as a rollback target,
but do not use JavaScript-readable tokens or weaken CSRF/CORS as a workaround.

Before rollout, capture browser network evidence for:

- CSRF GET status and response origin;
- `Set-Cookie` acceptance or browser rejection reason;
- credentialed preflight headers;
- registration POST request cookie/header presence and response status.

This confirms the mechanism and distinguishes browser cookie policy from stale
gateway authorization.

#### Feedback visibility and failure semantics

Move `FeedbackProvider` from the application root to the authenticated route layout
that owns `AppShell`. Login, registration, and legal redirects stay outside that
layout, so neither the launcher nor feedback dialog can render there. Keep
`ErrorState` consumers inside the provider.

Feedback error copy must distinguish:

- browser-confirmed offline;
- rate limit;
- CSRF/security rejection;
- validation/conflict;
- API unavailable or request blocked.

Do not label every fetch/CORS/API failure as "no internet." Preserve the failed draft
for retry, but do not retry automatically.

#### Registration abuse controls

Use layered controls rather than treating any one mechanism as sufficient:

1. Split `public-account-register` from `public-account-login`. Give registration a
   lower, independently configurable limit. Preserve generic conflict responses so
   username/email availability is not disclosed more precisely.
2. Establish trusted client-IP resolution before the rate limiter. Trust only the
   configured Workshop/API proxy boundary; never accept arbitrary forwarded addresses.
3. Reuse the existing `IRecaptchaService` and named `HttpClient` for a dedicated
   `public_account_register` action. Add a short-lived challenge token to the shared
   registration request contract. Production fails closed when verification or its
   required configuration is unavailable; Development/Test use the existing fake.
4. Apply the challenge and registration limiter to the shared ServerAPI registration
   endpoint, so Workshop and the public client cannot bypass each other's policy.
5. Emit privacy-safe counters for accepted, validation-rejected, challenge-rejected,
   conflict, rate-limited, and server-failed registration attempts. Do not log email,
   username, password, challenge token, CSRF token, session cookie, or raw client IP.
6. Add an operator alert based on sustained challenge failures, `429` rates, and
   unusual successful-account volume. The exact threshold belongs to deployment
   configuration, not source.

Email verification would further limit useful spam accounts, but it changes account
state, login policy, email delivery, recovery, persistence, and user experience. It is
not required for this defect slice and needs a separate approved design if CAPTCHA
plus registration-specific throttling proves insufficient.

#### UX direction

Impeccable mode remains **Operate**. Use `harden` for truthful failure states and
route-appropriate controls; use `audit` for keyboard, focus, responsive, and error
announcement verification. This is refinement, not redesign.

Minimum visual evidence:

- login and registration at desktop and 360 px with no feedback launcher;
- authenticated shell at the same sizes with the launcher present;
- registration challenge unavailable, rate-limited, security-rejected, field-error,
  and successful pending states;
- feedback offline, API unavailable, `403`, and `429` states with distinct copy.

### Impacted Projects

- `frontend/shooter-workshop.client` — same-origin production transport, route-scoped
  feedback provider, registration challenge, error copy, and focused tests.
- `frontend/morwalpizvideo.client` — coordinated registration challenge because it
  uses the same ServerAPI contract; no visual redesign.
- `frontend/fe-packages/models` — additive registration challenge token.
- `frontend/fe-packages/services` — preserve account transport behavior and test the
  revised request; no new API client.
- `MorWalPiz.Contracts` — additive registration request field and validation.
- `MorWalPizVideo.ServerAPI` — registration-specific limiter, trusted proxy handling,
  challenge verification, telemetry, and exact-origin tests.
- `MorWalPizVideo.Domain` — verify only; account creation, normalization, hashing, and
  session issuance remain unchanged.
- `MorWalPizVideo.Models` — verify only; no account schema change.
- `MorWalPizVideo.MvcHelpers` — verify only; CSRF/cookie contract remains unchanged.
- `MorWalPizVideo.BackOffice.Tests` — ServerAPI registration, CAPTCHA, limiter, CORS,
  and feedback regression coverage.
- `MorWalPizVideo.AppHost` — verify local relative proxy behavior; no production
  topology ownership.
- Workshop deployment workflow/container — production same-origin proxy and public
  site-key/upstream configuration.
- BackOffice API/SPA, ShortLinks, Shooting Range, FreezerAnalysis, VideoImporter,
  InsightScanner, shop, Shooting ITA, TelePrompter, and stage designer — not
  applicable to product behavior.

### Impacted Files

#### Modify

- `frontend/shooter-workshop.client/src/main.tsx` — remove the unconditional global
  feedback mount and add the registration challenge provider at the narrowest shared
  client boundary.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — place
  `FeedbackProvider` around the authenticated `AppShell` route only.
- `frontend/shooter-workshop.client/src/routes/AccountRegister/component.tsx` — obtain
  one registration challenge token per explicit submission and preserve current
  pending/error/session behavior.
- `frontend/shooter-workshop.client/src/routes/AccountRegister/component.test.tsx` —
  prove challenge, single submission, and truthful error states.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx` —
  retain draft behavior while separating offline, API, security, validation, and
  throttling failures.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.test.tsx`
  — cover the revised status mapping.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — prove the launcher
  is absent on login/register/legal routes and available under the authenticated
  shell.
- `frontend/shooter-workshop.client/src/i18n.tsx` — route-appropriate challenge and
  failure copy.
- `frontend/shooter-workshop.client/package.json` and `frontend/yarn.lock` — add only
  the already established reCAPTCHA client integration needed by Workshop.
- `frontend/shooter-workshop.client/nginx.conf` or its container template — explicit
  `/api/` upstream proxy before SPA fallback.
- `frontend/shooter-workshop.client/Dockerfile` — production-relative API build and
  environment-owned proxy/site-key inputs.
- `.github/workflows/deploy-shooter-workshop-client.yml` — provide non-secret
  production build/runtime configuration and add a same-origin smoke gate.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx` — supply the same
  registration challenge to the shared endpoint.
- `frontend/morwalpizvideo.client/src/routes/account/Components.test.tsx` — preserve
  public registration behavior under the additive contract.
- `frontend/fe-packages/models/src/publicAccount.ts` — additive challenge token.
- `frontend/fe-packages/services/src/publicAccountService.ts` and its tests — transport
  the revised request without changing CSRF/session semantics.
- `MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs` — validate the challenge
  token as part of registration input.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` — verify the
  registration challenge before Domain account creation.
- `MorWalPizVideo.ServerAPI/Program.cs` — separate registration/login limiters,
  trusted forwarded-header policy, and privacy-safe registration metrics.
- `MorWalPizVideo.ServerAPI/appsettings.json` and environment-specific non-secret
  configuration — explicit limiter defaults and trusted-proxy configuration shape;
  never add deployed values or secrets.
- `MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs` — challenge,
  `429`, no account persistence on rejection, and existing CSRF/session behavior.
- `MorWalPizVideo.BackOffice.Tests/Features/FeedbackEndpointTests.cs` — regression
  coverage through the unchanged CSRF contract.

#### Add

- A focused ServerAPI production-CORS/preflight test for
  `https://workshop.morwalpiz.com` covering account registration and feedback headers.
- A production-like browser test in the established Workshop test area for CSRF GET,
  registration POST, cookie acceptance, session restoration, feedback submission, and
  denied origins through the same-origin route.

#### Generated/Migration

None. No MongoDB, EF, or persisted account document migration is required.

#### Documentation

- `docs/feature-handoffs/shared-account-registration-handoff.md` — this defect and
  abuse-hardening addendum.
- `docs/feature-handoffs/shooter-workshop-ui-feedback-handoff.md` — update actual
  same-origin delivery evidence after implementation.
- `docs/architecture/security.md` — record registration challenge/throttling and
  Workshop first-party cookie transport.
- `docs/architecture/deployment.md` — replace the direct Workshop credentialed-CORS
  browser edge with the same-origin `/api` proxy path.
- `docs/architecture/frontend.md` — distinguish Workshop's same-origin production
  account transport from the public client's deployment.
- `docs/architecture/adr/ADR-018-public-account-boundary.md` — record the shared
  registration abuse policy because it changes the account boundary contract.
- `docs/features/accounts-and-channels.md` — add delivered evidence and residual
  release gates.

#### Verify Only

- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs`
- `MorWalPizVideo.Domain/Security/PasswordHashing.cs`
- `MorWalPizVideo.Domain/Interfaces/Repository.cs`
- `MorWalPizVideo.Models/Models/PublicAccount.cs`
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`
- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs`
- `MorWalPizVideo.ServerAPI/Services/IRecaptchaService.cs`
- `MorWalPizVideo.ServerAPI/Services/RecaptchaService.cs`
- `MorWalPizVideo.AppHost/Program.cs`

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Registration adds a challenge token and dedicated limiter; feedback API contract remains unchanged. |
| Frontend/UI | Feedback launcher becomes authenticated-shell-only; registration adds invisible challenge acquisition and truthful errors. |
| Database/Data | No schema change. Rejected challenge/rate-limited attempts must create no account or session. |
| Contracts/Models | Additive registration request field in .NET and TypeScript; coordinated client/API release required. |
| Authentication/Authorization | Preserve HttpOnly session, double-submit CSRF, anonymous registration/feedback, and account-owned Workshop authorization. |
| Validation | CAPTCHA is server-authoritative; existing identity, password, legal, and field validation remains authoritative. |
| Cache | CSRF/session remain no-store. Proxy must not cache account, feedback, or Workshop API responses. |
| Configuration/Secrets | Reuse protected reCAPTCHA secret; add only non-secret site key, limits, proxy trust, and upstream settings through environment configuration. |
| Logging/Telemetry | Add outcome counters only; exclude identity, credentials, cookies, CSRF/challenge tokens, free text, and raw IP. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Same-origin proxy must precede switching the SPA to relative API paths; challenge config and browser smoke proof are release gates. |
| Backward Compatibility | Old clients cannot register after the API requires a challenge. Deploy both active registration clients in the same compatibility window or gate enforcement. |

### Reuse Opportunities

- Reuse `accountApiService`, existing CSRF acquisition, and status-aware
  `ApiResponseError`; do not add an application-local fetch wrapper.
- Reuse `IRecaptchaService`, `HttpClientNames.Recaptcha`, the mock implementation, and
  the public client's existing reCAPTCHA provider integration.
- Reuse the existing `public-feedback` limiter pattern while keeping registration and
  login in separate policies.
- Reuse `FeedbackProvider` and move its ownership boundary rather than duplicating the
  modal.
- Reuse the accepted Workshop same-origin proxy decision and existing local Vite
  `/api` proxy.
- Reuse unique normalized account indexes and current generic conflict response.

### Risks

| Rank | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Same-origin proxy incorrectly falls through to the SPA or drops cookies/headers. | Define `/api/` before SPA fallback, preserve status/headers, fail closed on missing upstream. | Browser sees API JSON/status, matching CSRF cookie/header, and restored session after reload. |
| High | CAPTCHA enforcement deploys before both clients can send a token. | Coordinate both clients and API; use a short explicit rollout gate if artifacts cannot deploy atomically. | Workshop and public registration pass; old payload is rejected only after both clients are live. |
| High | Forwarded IP trust is too broad, allowing spoofed partitions, or absent, collapsing users behind a proxy. | Trust only known proxy/network boundaries and test direct/spoofed/forwarded cases. | Limiter keys distinct clients correctly and ignores untrusted forwarding. |
| Medium | Process-local rate limits can be bypassed across instances/restarts. | Keep edge throttling as the production-wide control and app limiting as defense in depth; move to shared state only if scale requires it. | Multi-instance smoke/load evidence reaches the configured aggregate threshold. |
| Medium | reCAPTCHA outage blocks legitimate registration. | Fail closed with truthful retry copy and monitor provider failures; do not create accounts on uncertain verification. | Simulated provider failure creates no account/session and displays a non-connectivity-specific message. |
| Medium | Moving feedback provider breaks `useFeedback` consumers. | Place it around the complete authenticated `AppShell`, not individual pages. | Every authenticated ErrorState can open feedback; anonymous routes render without the launcher. |
| Medium | Error copy still misdiagnoses CORS/API failures as offline. | Base messages on browser offline state and structured status; use neutral unavailable copy for fetch failures. | Tests distinguish offline, fetch failure, `403`, `409`, and `429`. |
| Low | Challenge scripts affect account-page performance or privacy expectations. | Load only for registration-capable surfaces and retain required privacy disclosure. | No challenge script on authenticated Workshop routes; legal disclosure is visible on registration. |

Independent post-validation review is warranted because the slice changes production
routing, trusted proxy handling, registration abuse controls, CSRF/cookie transport,
and a shared public contract.

### Technical Debt

- Public account password hashing uses PBKDF2-SHA256 with 100,000 iterations. It uses
  strong random salts and constant-time verification, but the work factor should be
  reviewed against current organizational guidance. A versioned rehash-on-login
  migration is separate from the spam fix and does not block this slice.
- Email verification and compromised/disposable-email controls are absent. They do
  not block the immediate layered anti-automation fix; add them only through a
  separate account-state and email-delivery design.
- The public client still uses a direct cross-site ServerAPI topology. That broader
  account-cookie risk is documented and must not be silently folded into the Workshop
  proxy patch.
- The Workshop client has no dedicated `PRODUCT.md` or `DESIGN.md`. Continue using the
  documented public visual authority for this refinement; document the finished
  Workshop system separately after acceptance.

### Implementation Roadmap

1. Capture the failing production browser transaction and record CSRF GET, preflight,
   cookie acceptance/rejection, and registration POST evidence without recording token
   values.
2. Add regression tests for launcher visibility, feedback status mapping, exact
   Workshop-origin preflight, challenge rejection, and registration `429` behavior.
3. Establish the same-origin `/api` proxy and trusted forwarded-header boundary. Prove
   API failures never return the SPA shell.
4. Switch the Workshop production client to relative account/API paths while retaining
   the local Vite proxy and a controlled direct-origin rollback.
5. Move feedback ownership to the authenticated route layout and revise error copy.
6. Add the shared registration challenge contract in .NET and TypeScript, then update
   both active registration clients.
7. Verify the challenge in ServerAPI before Domain account/session creation and split
   login from registration rate limiting.
8. Add privacy-safe registration outcome metrics and production alerting.
9. Apply the Impeccable `harden` and `audit` checks to anonymous and authenticated
   route states without visual redesign.
10. Deploy in dependency order: proxy/trust readiness, compatible API rollout gate,
    both clients, enforcement activation, browser smoke proof, documentation update.

Stop when the two reported defects are fixed, registration has challenge plus
registration-specific throttling, and deployed browser evidence passes. Email
verification, password-hash migration, and broader public-client transport changes
require separate approval.

### Testing Strategy

- **Unit/component:** Workshop registration challenge unavailable/success, single
  submit, field errors, `403`, `409`, `429`, and provider failure; feedback visibility
  and error classifications.
- **Route:** login, registration, and legal routes have no feedback launcher;
  authenticated shell routes retain it.
- **Service/contract:** registration payload includes challenge token while preserving
  CSRF, credentials, no-store behavior, and structured errors.
- **Backend behavior:** missing/invalid challenge, missing CSRF, valid registration,
  duplicate identity, registration limit, login independence, and no persisted
  account/session on rejected attempts.
- **CORS/proxy security:** exact Workshop origin allowed where direct rollback is
  retained; unsupported origin denied; untrusted forwarded address ignored; trusted
  proxy address honored.
- **Integration:** mock reCAPTCHA success/failure/timeout and deterministic rate-limit
  windows.
- **Browser end to end:** Workshop registration, session restore, hard reload,
  authenticated feedback, anonymous-route launcher absence, blocked third-party
  cookies, narrow mobile layout, keyboard navigation, and browser zoom.
- **Operational:** verify registration outcome metrics contain no PII or secret/token
  material and alerts trigger from synthetic non-production traffic.

### Migration And Rollback

- **MongoDB:** no schema migration or backfill. Existing accounts, sessions, unique
  indexes, and TTL index remain valid.
- **SQLite:** not applicable.
- **API contract:** additive request field but behaviorally required once enforcement
  activates. Use a short compatibility gate or coordinated deployment so both clients
  can send it before rejection is enabled.
- **Authentication/CSRF:** same contract; browser routing changes from cross-site to
  first-party. Do not alter token storage, HttpOnly, Secure, revocation, or legal
  evidence.
- **Rate limiting:** introduce new policy names and limits without reusing shared
  login buckets. Rollback may restore previous policy selection but must not disable
  all throttling.
- **Configuration:** proxy upstream, trusted proxy/network, public site key, limiter
  values, and challenge secret availability are deployment prerequisites. Missing
  required production settings fail readiness/enforcement rather than silently
  accepting registration.
- **Deployment order:** provision and verify same-origin proxy; deploy API support;
  deploy both clients; activate required challenge; monitor; retire the direct
  Workshop browser/API path only after rollback evidence.
- **Rollback:** switch Workshop temporarily to the verified direct API origin and roll
  back client/API artifacts together. Keep CSRF and current account/session security.
  Accounts created during the rollout remain valid and must not be deleted as part of
  code rollback.
- **Recovery:** if abuse protection blocks legitimate traffic, adjust
  environment-owned limits or disable challenge enforcement through the approved
  rollout gate while retaining CSRF and throttling; never bypass validation with a
  client-only fallback.

### Open Questions

- What trusted proxy/network values and edge rate-limiting capability are available
  for the production Workshop ingress? This determines whether aggregate throttling
  is enforced at nginx, Azure ingress, or a shared limiter.
- Can the public client and Workshop client be deployed in the same compatibility
  window as the ServerAPI registration contract? If not, an explicit temporary
  enforcement gate is required.
- Is Google reCAPTCHA approved for the Workshop registration privacy posture, or must
  the existing provider be replaced through a separate architecture decision?

### Delivered implementation record (2026-11-15)

Implemented:

- Moved Workshop `FeedbackProvider` from the application root to the authenticated
  `AppShell` route element. Login, registration, and legal routes no longer render
  the Report an issue launcher.
- Distinguished feedback API-unavailable/validation failures from browser-confirmed
  offline failures without changing draft retention or automatic-retry behavior.
- Added a production Workshop nginx template that proxies `/api/` to the
  environment-owned ServerAPI upstream. The SPA build now uses relative API paths,
  while the upstream remains a Docker/workflow build input. The obsolete static-only
  nginx configuration was removed.
- Split the shared ServerAPI account throttle into independent registration and login
  policies. Registration defaults to five attempts per observed IP per ten minutes;
  login defaults to ten attempts per five minutes and retains the old
  `PublicAccount:RateLimit` configuration as a fallback.
- Updated security, frontend, deployment, and ADR documentation for the first-party
  Workshop cookie transport and separate account throttles.

Validation:

- `yarn --cwd frontend workspace shooter-workshop.client test --passWithNoTests=false --run src/components/feedback/FeedbackProvider.test.tsx src/routes/config.test.tsx src/routes/AccountRegister/component.test.tsx` — 6 tests passed.
- `yarn --cwd frontend workspace shooter-workshop.client build` — passed.
- `dotnet build MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed.
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PublicAccountEndpointTests|FullyQualifiedName~FeedbackEndpointTests"` — 9 tests passed.

Remaining release gates:

- The proxy must be built and deployed with a real environment-owned
  `VITE_API_BASE_URL` upstream; the client image intentionally fails its final stage
  when that value is absent.
- A real HTTPS browser test must confirm CSRF cookie acceptance, registration,
  hard-reload session restoration, authenticated feedback, and API error responses.
- CAPTCHA/human verification, distributed/edge throttling, and email verification were
  not introduced in this patch. Registration is better throttled but is not yet
  demonstrably spam-resistant across multiple instances or automated clients.
- Existing ImageSharp dependency vulnerability warnings were emitted by the backend
  test restore and are unrelated to this change.

