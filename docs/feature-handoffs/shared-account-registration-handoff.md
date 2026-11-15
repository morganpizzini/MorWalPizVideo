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

