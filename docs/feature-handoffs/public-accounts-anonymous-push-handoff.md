# Public Accounts And Anonymous Web Push Handoff

## Outcome

Add self-service registration, login, logout, session restoration, and profile management to `frontend/morwalpizvideo.client`, backed by a dedicated public-account boundary in `MorWalPizVideo.ServerAPI`.

Anonymous Web Push must remain available without registration. The existing browser-held 256-bit subscription credential is sufficient authorization for settings changes and revocation. Public accounts are a separate identity feature and are not a prerequisite for correcting API-01.

Assumptions:

- Public accounts are distinct from BackOffice administrators and from the on-hold shop customer model.
- The first release uses a secure HttpOnly cookie, server-revalidated session, CSRF protection, exact credentialed CORS origins, and no browser-readable bearer token.
- Push subscriptions remain browser-scoped. Optional account linking is deferred until its cross-device and account-deletion behavior is approved.
- API-01 source remediation and focused regression coverage are complete. Final API-01/TD-027 closure remains pending production consumer and rollout evidence; this handoff does not claim that operational closure yet.

## Implementation Status (2026-10-26)

The public-account slice is implemented additively. ServerAPI now owns separate `PublicAccount` and `PublicAccountSession` persistence, Domain registration/login/session/profile commands, a dedicated cookie authentication scheme, CSRF validation, account-only credentialed CORS, rate limiting, and source-managed normalized identity/expiry indexes. The public client now has an isolated cookie account transport, session restoration, login, registration, profile update, logout, and account navigation. Existing public routes remain anonymous and unguarded; `/notifiche` and anonymous Web Push remain independent.

The account navigation refinement is now implemented in `frontend/morwalpizvideo.client/src/layouts/title-header.tsx` and its stylesheet. Anonymous visitors see `Accedi` and `Registrati`; authenticated visitors see `Profilo` and the action-based `Esci`; session restoration reserves the control space while loading, and logout failures remain visible without changing the displayed session state. The controls are top-right within the header on desktop and return to normal flow on narrow screens. The centered duplicate account link was removed.

The account form refinement is now implemented in `frontend/morwalpizvideo.client/src/routes/account/Components.tsx`. Registration and profile inputs mirror the server data annotations, including the 12-128 character password requirement, and browser validation blocks incomplete or out-of-range submissions. The shared account transport now preserves ASP.NET validation-problem field errors through `getApiFieldErrors`, allowing messages to render beside the relevant control instead of falling back to the generic account rejection message. Login, registration, and profile forms use a non-floating card treatment; header account actions are icon-only, borderless controls with accessible labels.

The first implementation slice addresses API-01 independently of public-account work. The legacy `POST /api/push/subscribe` and `DELETE /api/push/unsubscribe` routes now return `410 Gone` without resolving or mutating the shared subscription repository. The anonymous credential-protected `/api/push/subscriptions/*` routes remain unchanged and continue to support creation, settings, update, and revocation.

Validation: `PushSubscriptionEndpointTests` passes with 6 tests, including a regression that invokes both retired legacy mutation routes and confirms the protected subscription remains unchanged. The shared services suite passes 43 tests, the shared models/services TypeScript builds pass, `morwalpizvideo.client` production/PWA build passes, the ServerAPI build passes, and `PublicAccountEndpointTests` passes 2/2, including anonymous CSRF response/token coverage. The ServerAPI deployment workflow now gates release on an anonymous production CSRF probe from `https://morwalpiz.com` that checks status, token, cookie, and credentialed CORS headers. Production legacy-consumer traffic, live Mongo index deployment, and real-browser CORS/cookie evidence remain unverified until the workflow runs against the deployed app.

### Production Follow-up (2026-10-26)

Registration is blocked in the deployed browser flow because `GET https://morwalpiz-serverapi.azurewebsites.net/api/v1/account/csrf` returns `401`. Current source marks that exact action `[AllowAnonymous]`, `PublicAccountEndpointTests` obtains it anonymously before registering successfully, and the account transport resolves the expected URL with `credentials: include`. The checked-in controller path therefore does not explain the production response.

Treat this as a release mismatch or host/platform-policy incident until production evidence identifies the responder. The ServerAPI and public-client workflows deploy independently, and the ServerAPI workflow has no post-deployment account smoke test. Before changing controller authorization, verify the deployed revision/package and capture the unauthenticated response status, body, `Server`, `WWW-Authenticate`, CORS, and cookie headers. Then verify App Service Authentication/Easy Auth and gateway rules do not require authentication before ASP.NET Core receives the request. A current artifact reaching this controller action must return `200`, `PublicAccountCsrfContract`, and the CSRF cookie.

Release status: public-account registration is **BLOCKED** until the production CSRF probe passes from `https://morwalpiz.com`. Local/in-process endpoint tests are necessary but not sufficient release evidence.

## Current Evidence

- `MorWalPizVideo.ServerAPI/Controllers/PushSubscriptionsController.cs` exposes the accepted anonymous surface at `api/push/subscriptions/*`, applies the `push-public` limiter, and delegates to `IPushSubscriptionService`.
- `MorWalPizVideo.Domain/Push/PushSubscriptionService.cs` mints a 256-bit credential once, persists only its hash, and requires a fixed-time hash match for endpoint retargeting, settings reads, and revocation.
- `MorWalPizVideo.Domain/Push/PushEndpointProtection.cs` refuses empty hash comparisons, so legacy records with an empty credential hash cannot silently pass the protected flow.
- `MorWalPizVideo.Models/Models/PushNotifications.cs` models `PushChannelSubscription` as anonymous and not bound to the administrative `User` aggregate.
- `frontend/fe-packages/services/src/pushNotifications.ts` stores the endpoint and minted credential locally, replays the credential for updates, and can manage a subscription without an account.
- `frontend/morwalpizvideo.client/src/routes/pushSettings/Component.tsx` already provides an anonymous notification-settings page at `/notifiche`.
- `MorWalPizVideo.ServerAPI/Controllers/PushController.cs` now retires the API-01 legacy subscribe and unsubscribe mutations with `410 Gone`; it no longer accesses the shared subscription repository. The public-key alias remains anonymous.
- `MorWalPizVideo.Domain/Interfaces/Repository.cs`, `PushSubscriptionRepository`, updates and deactivates by endpoint hash. Its general upsert preserves `CredentialHash` only on insert, so a secure legacy claim needs a dedicated atomic repository operation.
- `MorWalPizVideo.BackOffice/Controllers/PushSubscriptionsController.cs` already owns authenticated BackOffice browser registration through the same credential protocol.
- No maintained frontend source caller of the legacy `api/push/subscribe`, `api/push/unsubscribe`, or `api/push/public-key` routes was found. Deployed or external callers remain unknown and require telemetry/consumer confirmation before route removal.
- `MorWalPizVideo.Models/Models/User.cs`, BackOffice `AuthController`, and BackOffice `UserController` are administrative: they carry RBAC groups, direct permissions, BackOffice access, impersonation restrictions, quotas, and admin lifecycle operations. They are not an appropriate public self-registration aggregate.
- ServerAPI now has a dedicated public-account controller, account authentication scheme, server-side session store, account-only credentialed CORS policy, CSRF validation, and explicit anonymous metadata on registration, login, session restoration, and CSRF acquisition. Its general fallback policy remains authenticated.
- The shared public frontend client still strips credentials for ordinary public calls. The isolated account client uses `credentials: include`, strips BackOffice bearer/channel context, and obtains CSRF from `api/v1/account/csrf`.
- `MorWalPizVideo.ShootingRange` contains a repository-local precedent for an HttpOnly cross-origin cookie, CSRF, exact credentialed CORS, server-side session revalidation, fixed expiry, logout revocation, and non-redirecting API 401/403 responses. It is evidence for the security shape, not a library to reference from ServerAPI.
- `docs/architecture/adr/ADR-017-anonymous-web-push.md`, `docs/architecture/api-consistency-audit.md`, and TD-027 already state that anonymous push is intentional and that legacy credential bypass is the open deviation.
- No `PRODUCT.md` or `DESIGN.md` was found under `frontend/morwalpizvideo.client`; current routes, Bootstrap styling, shared layout components, and existing public pages are the incumbent design context.
- `frontend/morwalpizvideo.client/src/routes/layout/account.tsx` owns additive session restoration and logout state. It is composed by the public root and does not guard existing content routes.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx` provides login, registration, and profile presentation using the existing Bootstrap language.
- `frontend/morwalpizvideo.client/src/layouts/title-header.tsx` currently appends one account link to the centered content navigation. This makes `Accedi` read as a primary content-navigation item and provides no persistent registration or logout action.

## Proposed Design

### 1. Keep Push Anonymous

Treat each Web Push subscription as a capability-owned browser resource:

1. The browser creates a `PushSubscription` through its service worker.
2. It anonymously posts endpoint, encryption keys, application key, and selected channels to `api/push/subscriptions`.
3. ServerAPI creates the record, returns the management credential once, and stores only its hash.
4. The browser keeps endpoint plus credential in local storage and must present both for settings, updates, and revocation.
5. Rate limiting, HTTPS/SSRF endpoint validation, endpoint redaction, and application/channel validation remain mandatory.

This answers the core question: an anonymous visitor can register for push because the per-subscription random credential, not a user account, proves authority over later mutations.

### 2. Correct API-01 Independently

Retire the legacy mutation semantics. Do not add login as a substitute for subscription ownership and do not let the legacy controller call the repository directly.

- Maintained public clients continue using `api/push/subscriptions/*`.
- Legacy subscribe/unsubscribe routes stop mutating records and return the stable `410 Gone` retirement response. The public-key alias remains because it is read-only and its consumer status is still being verified.
- Credentialless records may be adopted once only when the caller proves possession of the exact endpoint and current Web Push key material. A dedicated atomic repository operation must match endpoint hash, empty credential hash, and existing keys before setting a newly minted credential hash. Endpoint knowledge alone must fail.
- BackOffice-stamped legacy records are claimed only through the authenticated BackOffice registration surface. Public ServerAPI must continue rejecting `applicationKey=backoffice`.
- Records that cannot be claimed remain readable for delivery until they are naturally replaced, permanently suppressed, or explicitly retired by an approved retention policy. Do not invent a credential or account owner for them.
- Cross-route tests must prove that legacy requests cannot overwrite keys, alter application/scope, reactivate, or revoke a protected record.

The one-time claim is preferred over endpoint-only migration because existing browsers can read their current endpoint and keys, while an attacker assumed to know only the endpoint cannot claim the record.

### 3. Introduce A Separate Public Account Boundary

Create public-account and public-session aggregates in separate MongoDB collections. Do not reuse `User`, `Customer`, shop sessions, BackOffice JWTs, groups, or permissions.

Recommended ServerAPI contract:

- `POST /api/v1/accounts`: anonymous registration.
- `POST /api/v1/account/sessions`: anonymous login and session creation.
- `DELETE /api/v1/account/sessions/current`: authenticated logout and revocation.
- `GET /api/v1/account/session`: session restoration with a safe account summary.
- `GET /api/v1/profile`: authenticated profile read.
- `PUT /api/v1/profile`: authenticated profile update.
- `GET /api/v1/account/csrf`: anonymous pre-auth CSRF token acquisition.

Use explicit Contracts DTOs and Problem Details. Registration and login receive dedicated rate-limit policies. Normalize username and email with invariant rules, enforce unique MongoDB indexes, use the existing Domain password hashing primitive, never return hashes/salts/session identifiers, and avoid account-existence disclosure in authentication/recovery responses.

Use a dedicated `PublicAccount` cookie authentication scheme and authorization policy. The cookie must be HttpOnly, Secure, `SameSite=None`, fixed-expiry, non-sliding, and server-revalidated against active account/session state. Unsafe cookie-backed operations, including registration, login, logout, and profile updates, require the CSRF header. API authentication failures return 401/403 rather than redirects.

Add an exact-origin credentialed CORS policy only for account endpoints. Keep existing anonymous content and push calls on `publicApiService` with `credentials: omit`; do not globally convert public traffic to credentialed requests.

In `@morwalpizvideo/services`, add an isolated account client mode that includes cookies and CSRF but never injects BackOffice channel context or bearer tokens. Account DTOs belong in `@morwalpizvideo/models`. The public application owns route state and presentation.

### 4. Optional Future Account-to-Push Linking

Do not make account linking part of API-01 or the initial account release. If later approved, linking must require both:

- a valid authenticated public-account session; and
- the existing endpoint plus subscription credential.

The server may then set an optional public-account reference on the subscription. It must never claim by endpoint, cookie, email, or login alone. Anonymous management remains valid so a signed-out browser can still revoke its own consent. Cross-device listing/revocation, unlink-on-logout, account deletion, retention, and privacy export behavior require a separate decision before this field is added.

### 5. Header Account Controls

Move account actions out of the centered content-navigation row into one account utility cluster owned by `TitleComponent`:

- Desktop: anchor the cluster to the top-right of the header container, inside normal header bounds. Do not make it viewport-fixed; it must scroll with the header and never cover content.
- Narrow screens: return the cluster to document flow as a compact, end-aligned row below or beside the brand according to available width. It must not overlap the logo, social buttons, or main navigation.
- Anonymous state: show `Accedi` and `Registrati`, using the existing icon/style system and Bootstrap language.
- Authenticated state: show `Profilo` (or the bounded display name when it fits) and an explicit `Esci` button. Logout is an action, not a navigation link.
- Loading state: reserve stable control dimensions and do not briefly render `Accedi` before session restoration completes.
- Remove the duplicate account link from the centered main-navigation list and its fallback account-only navigation branch.
- Keep account controls out of `hideLink` compact embeds unless product intent explicitly changes that reduced surface.
- Keyboard focus, accessible names, 44px minimum mobile targets, and visible logout failure are required. Logout failure must not falsely render the anonymous state.

This is an Impeccable `layout` refinement in `Operate` mode, not a redesign. Preserve the large brand, current Bootstrap behavior, social controls, dynamic content navigation, fonts, and palette.

### Alternatives

- Reuse administrative `User`: rejected because it couples public registration to BackOffice RBAC and privileged account lifecycle.
- Require login for all push subscriptions: rejected because it removes the accepted anonymous consent model and does not by itself prove ownership of existing endpoint-hash records.
- Delegate legacy routes to the protected service without changing their contract: rejected because the legacy DTO/response cannot safely capture and return the newly minted credential.
- Allow endpoint-only legacy adoption: rejected because it recreates API-01 under a migration name.
- Store browser bearer tokens in local storage: rejected because the repository has already moved browser authentication toward HttpOnly cookies and isolated transports.
- Reuse the on-hold shop customer/session model: rejected because the shop boundary is frozen and its identity contract serves a different product lifecycle.

## Impacted Projects

- `MorWalPizVideo.Models`: add public account/session persistence models and collection constants; adjust Push only if optional linking is separately approved.
- `MorWalPizVideo.Domain`: own public-account registration/authentication/profile services, repositories, mock parity, session validation, and the atomic legacy push claim.
- `MorWalPiz.Contracts`: own public account request/response DTOs and any additive push-claim response semantics.
- `MorWalPizVideo.ServerAPI`: own public account controllers, authentication policy/scheme, cookie/CSRF/CORS/rate-limit composition, DI, and API-01 route retirement.
- `MorWalPizVideo.BackOffice`: only affected if BackOffice legacy push records need the authenticated one-time claim path; no public account behavior belongs here.
- `MorWalPizVideo.BackOffice.Tests`: extend existing ServerAPI host tests for public accounts, account security, and cross-route push tampering/claim behavior.
- `frontend/fe-packages/models`: add public account/session/profile TypeScript contracts.
- `frontend/fe-packages/services`: add isolated account endpoints/client behavior and retain the existing public push transport.
- `frontend/morwalpizvideo.client`: add register, login, profile, logout/session restoration, and account-aware navigation while preserving anonymous `/notifiche`; route guards are intentionally deferred because no current page is restricted.
- `frontend/back-office-spa`, `frontend/shooting-ita-frontend`, and other shared-package consumers: verify-only for shared model/service compatibility; Shooting ITA anonymous push behavior must remain unchanged.
- `MorWalPizVideo.AppHost` and deployment workflows: verify-only unless public account configuration, persistent Data Protection keys, or new release checks require orchestration/deployment updates.
- Shop, Shooting Range domain behavior, ShortLinks, VideoImporter, InsightScanner, TelePrompter, and stage-designer: not applicable to implementation.

## Impacted Files

### Modify

- `MorWalPizVideo.ServerAPI/Controllers/PushController.cs`: remove direct repository mutation and implement the approved legacy retirement behavior.
- `MorWalPizVideo.ServerAPI/Controllers/PushSubscriptionsController.cs`: preserve anonymous capability behavior and expose only any approved credentialless-claim outcome.
- `MorWalPizVideo.BackOffice/Controllers/PushSubscriptionsController.cs`: support authenticated claim only if legacy BackOffice records require it.
- `MorWalPizVideo.ServerAPI/Program.cs`: register account policy/scheme, exact credentialed CORS, antiforgery, rate limits, services, repositories, and real/mock parity.
- `MorWalPizVideo.Domain/Push/PushSubscriptionService.cs`: centralize the secure one-time credentialless claim and retain credential checks for every later mutation.
- `MorWalPizVideo.Domain/Interfaces/IRepository.cs`, `Repository.cs`, and `MockRepository.cs`: add atomic push claim and public account/session persistence operations with Mongo/mock parity.
- `MorWalPizVideo.Models/Constraints/DbCollections.cs`: add dedicated public account/session collection names.
- `frontend/fe-packages/models/src/index.ts`: export public account contract types.
- `frontend/fe-packages/services/src/apiTransport.ts`: add an isolated account transport mode without weakening public/admin isolation.
- `frontend/fe-packages/services/src/endpoints-frontend.ts` and `src/index.ts`: expose versioned public account endpoints and functions.
- `frontend/morwalpizvideo.client/src/routes/config.tsx`: register login, registration, and profile routes.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx` and the owning header/navigation component: expose account state/actions without changing anonymous push availability.
- `docs/architecture/README.md`, `backend.md`, `frontend.md`, `security.md`, and `api-design.md`: document the implemented public-account and transport boundaries.
- `docs/architecture/adr/ADR-017-anonymous-web-push.md`: reconcile the implemented API-01 compatibility decision while preserving anonymous push.
- `docs/architecture/api-consistency-audit.md` and `technical-debt.md`: mark API-01/TD-027 complete only after focused executable evidence passes.

### Add

- A public account model and session model under `MorWalPizVideo.Models/Models/`.
- Focused public account repository interfaces and service/session abstractions under the Domain owning folders, plus Mongo and mock implementations following current repository conventions.
- Versioned public account contracts under `MorWalPiz.Contracts/Contracts/`.
- ServerAPI account/profile controllers under `MorWalPizVideo.ServerAPI/Controllers/` and project-local authentication/CSRF helpers under an owning security folder.
- TypeScript public account types under `frontend/fe-packages/models/src/` and account service functions under `frontend/fe-packages/services/src/`.
- Feature-oriented `login`, `register`, and `profile` routes with adjacent component/loader/action tests under `frontend/morwalpizvideo.client/src/routes/`.
- Focused public-account and API-01 tests under `MorWalPizVideo.BackOffice.Tests/Features/` using `ServerApiWebApplicationFactory`.
- A new ADR under `docs/architecture/adr/` for the public-account identity/session boundary.

### Generated/Migration

- No EF migration applies. MongoDB has no automatic migration.
- Add source-owned unique index definitions for normalized username and normalized email, plus a TTL or explicit expiry index for public sessions as selected by the repository's index governance.
- Verify and, if absent, add a unique `EndpointHash` index for `pushSubscriptions` before relying on atomic claim/upsert uniqueness. Apply/audit remains an explicit operational action, not startup mutation.
- No generated frontend or Reqnroll output should be edited manually.

### Documentation

- This handoff is the pre-implementation source of truth.
- After implementation, update this same file with actual decisions, changed files, validation results, rollout status, and residual risks.
- Do not change the audit finding to completed in advance of tests and compatibility evidence.

### Verify Only

- `frontend/shooting-ita-frontend` push settings and tests.
- `frontend/back-office-spa` authenticated push registration and shared transport tests.
- `MorWalPizVideo.AppHost` ServerAPI resource/configuration flow.
- ServerAPI deployment test filter and exact discovery of the new ServerAPI/security suites.
- Current production callers/telemetry for legacy push routes.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | New versioned account/session/profile endpoints and dedicated policy; legacy push mutations retired; protected anonymous routes retained. |
| Frontend/UI | Login, registration, profile, logout, session restoration, route guarding, and navigation state. `/notifiche` stays anonymous. |
| Database/Data | Additive public account/session collections and indexes; atomic credentialless push claim; no destructive push rewrite. |
| Contracts/Models | New public-safe account DTOs and separate persistence models; no reuse of administrative `UserContract`. |
| Authentication/Authorization | Dedicated public cookie scheme/policy; server-side session revocation/revalidation; no BackOffice RBAC inheritance. |
| Validation | Normalize and uniquely constrain username/email; password/profile rules; credential-and-key proof for one-time push claim. |
| Cache | Account/profile/session responses must not use shared public output cache. Push delivery targeting is unchanged. |
| Configuration/Secrets | Cookie name/lifetime, rate limits, Data Protection persistence, and optional transactional email settings; no committed secrets. |
| Logging/Telemetry | Structured registration/login/session outcomes without password, cookie, email verification token, endpoint, push keys, or raw credential. Add legacy-route usage telemetry before retirement. |
| Background Jobs | Not required for account/session core. Email delivery may use an existing durable mechanism only if verification/recovery joins scope. |
| Deployment/Operations | API before client; exact credentialed CORS; persistent Data Protection; apply/audit indexes; monitor auth failures, duplicate keys, legacy route use, and push claim outcomes. |
| Backward Compatibility | Anonymous push remains compatible. Legacy mutation routes receive an explicit compatibility window and retirement response. Public account additions are additive. |

## Reuse Opportunities

- Reuse `PushSubscriptionService`, `PushEndpointProtection`, shared Push contracts, `push-public` rate limiting, endpoint validation/redaction, and existing frontend push helpers.
- Reuse `PasswordHashing` in Domain; do not duplicate password cryptography in ServerAPI controllers.
- Reuse the ServerAPI real/mock repository branch and `ServerApiWebApplicationFactory` test infrastructure.
- Reuse the security shape established by Shooting Range and BackOffice: exact credentialed CORS, HttpOnly/Secure cookie, CSRF header, non-redirecting API failures, session revalidation, and logout revocation. Keep implementation ownership in ServerAPI/Domain.
- Extend `createApiClient` with an account mode so base URL, CSRF, errors, and SSR isolation remain centralized. Do not use the BackOffice admin client in the public app.
- Reuse React Router route loaders/actions or feature-local session provider patterns already accepted by the public application; no repository-wide state store is required for a small session summary.
- Impeccable planning lens: this is a refinement of `morwalpizvideo.client`, in `Operate` mode. Use `shape` before implementation and `harden` after behavior exists. Preserve the current Bootstrap/shared-layout language. Minimum evidence is desktop/mobile captures of login, registration, validation error, authenticated profile, expired session, and anonymous notification settings, plus keyboard/focus, label/error association, contrast, and narrow-width checks.

## Risks

- **High — Privilege boundary collision:** Reusing administrative `User` could let public lifecycle rules affect BackOffice authorization. Mitigation: separate aggregates, collections, scheme, policy, contracts, and controllers. Signal: a public account cannot authenticate to BackOffice or acquire admin claims.
- **High — API-01 remains reachable:** A forgotten legacy route could still mutate by endpoint hash. Mitigation: no controller-level repository mutation and cross-route state-invariance tests. Signal: overwrite/reactivate/revoke attempts leave keys, application, scope, active state, and timestamps unchanged.
- **High — Cross-origin cookie/CSRF failure:** `SameSite=None` without exact credentialed CORS and antiforgery can be nonfunctional or unsafe. Mitigation: endpoint-specific exact origins, CSRF on all unsafe cookie requests, and real-browser allowed/denied-origin evidence. Signal: legitimate session works; forged/missing-CSRF and unapproved origins fail.
- **High — Frontend/backend release skew:** The public client can expose account UI while the deployed ServerAPI lacks or blocks its anonymous bootstrap contract. Mitigation: backend-first release dependency and a post-deploy account smoke probe before frontend activation. Signal: production CSRF, anonymous session, registration, authenticated profile, and logout probes pass against the exact deployed origin.
- **High — Duplicate identities/racing registration:** In-memory existence checks are race-prone. Mitigation: normalized fields plus unique MongoDB indexes and stable 409 handling. Signal: concurrent same-email/username registration creates one account.
- **High — Credentialless push takeover during migration:** Endpoint-only adoption would repeat the vulnerability. Mitigation: atomic endpoint plus exact-current-key match and empty-hash precondition. Signal: endpoint-only/wrong-key claims fail; exactly one correct claim succeeds and returns a credential once.
- **Medium — Session invalidation gaps:** A valid cookie could outlive deactivation/password change. Mitigation: session record and account security version/state checked on every authenticated request. Signal: deactivation, password change, logout, and expiry invalidate existing cookies.
- **Medium — Data Protection loss:** App restart or scale-out can invalidate or inconsistently decrypt cookies. Mitigation: persist and protect Data Protection keys. Signal: approved restart/instance test preserves valid sessions and rejects tampered cookies.
- **Medium — Shared transport regression:** Adding an account mode could accidentally send cookies or CSRF context on public requests. Mitigation: interleaved-client tests and immutable per-client mode. Signal: public push/content remain `credentials: omit`; account calls include cookies; admin channel headers never appear on account calls.
- **Medium — Account enumeration/abuse:** Registration/login responses and rate limits can disclose or permit attack. Mitigation: generic login/recovery responses, per-IP/account throttling, bounded payloads, and audit metrics. Signal: equivalent observable responses and enforced 429 behavior.
- **Medium — Unverified legacy consumers:** Immediate route removal could break an old deployed client. Mitigation: telemetry/inventory, bounded deprecation, explicit retirement response, and coordinated client deployment. Signal: zero legacy mutation traffic over the approved window.
- **Low — Anonymous push credential loss:** Clearing local storage removes management capability. This is accepted current behavior. Mitigation: explain browser-local scope and allow a new browser subscription after unsubscribe/endpoint rotation; account linking remains optional future work.

## Technical Debt

- TD-027/API-01 blocks claiming push consistency but does not block designing public accounts. Address now and close only with regression evidence.
- The Push subscription collection has no source-evidenced unique endpoint-hash index in the reviewed index manifest. Treat uniqueness verification as part of API-01 rollout, not unrelated cleanup.
- Public client test coverage is sparse outside Blog/gallery/match loaders. Add feature-local auth/profile tests now; do not launch a broad test reorganization.
- `apiTransport.ts` contains compatibility and active transport logic in a large file. Add the smallest account mode and focused tests; do not refactor the transport wholesale.
- Existing `User` mixes credentials, administrative authorization, quotas, and legacy push subscriptions. Do not clean it up as part of public accounts; isolate the new boundary.
- Email verification and password recovery are not current shared capabilities. Their inclusion changes configuration, provider, abuse, and rollout scope and requires the decision below.

## Implementation Roadmap

1. **Approve boundaries and compatibility.** Confirm separate public accounts, anonymous push preservation, legacy route retirement window, email verification/recovery scope, session lifetime, and whether optional push linking is deferred. Gate: no unresolved choice affects collections, API contract, or deployment order.
2. **Correct API-01.** Add the atomic empty-credential/key-proven claim, route both public and authenticated BackOffice claim attempts through `IPushSubscriptionService`, and stop legacy direct mutations. Gate: focused cross-route tampering, one-time claim, wrong-key, concurrency, and legitimate protected-flow tests pass.
3. **Apply API-01 data/index safety.** Audit endpoint-hash uniqueness, resolve duplicates through an operator-approved policy if any exist, and apply the approved unique index. Gate: target audit evidence and rollback snapshot exist; no automatic destructive startup migration.
4. **Add public account contracts and persistence.** Introduce separate account/session models, normalized identity fields, repositories, mock parity, indexes, and focused Domain services. Gate: uniqueness, password verification, inactive account, expiry, revocation, and security-version behavior pass without controllers.
5. **Compose ServerAPI security.** Add the dedicated scheme/policy, exact account CORS, antiforgery, rate limits, safe cookie options, session validation, and public-account controllers. Gate: real and mock hosts resolve; anonymous/account/admin/internal principals remain isolated; API returns Problem Details and no redirects.
6. **Add shared frontend account transport.** Add account models, endpoints, and a cookie/CSRF account client while preserving public/admin isolation. Build order is models, services, then application. Gate: transport tests prove credentials/header separation under interleaved use and SSR-safe client construction.
7. **Add the public account UI.** Add registration, login, profile, logout, session restore, protected profile routing, accessible validation, and account navigation. Keep push prompt/settings available signed out. Apply the Impeccable `shape` refinement before implementation and `harden` review after. Gate: component/route tests and responsive/accessibility visual evidence cover all required states.
8. **Refine global account controls.** Move account state/actions to the responsive top-right header utility cluster, remove the centered duplicate, preserve stable loading dimensions, and expose logout failure without lying about session state. Gate: desktop/mobile captures and keyboard checks cover anonymous, loading, authenticated, long-name, and logout-failure states without overlap.
9. **Deploy compatibly.** Deploy ServerAPI/Domain/contracts and required configuration/indexes before the frontend. Require an unauthenticated production `account/csrf` smoke probe and authenticated lifecycle probe before activating the client UI. Observe account failures, CORS/CSRF, duplicate-key conflicts, session validation, legacy push traffic, and claim outcomes. Gate: rollback is rehearsed and no legacy consumer remains before final route retirement.
10. **Close documentation.** Update the architecture guide, new public-account ADR, ADR-017, this handoff, API consistency audit, and TD-027 with actual implementation and executable evidence. Only then mark API-01 complete.
11. **Stop.** Do not add account-to-push linking, cross-device subscription management, shop identity convergence, social login, or broader `User` cleanup without separate approval.

Independent post-validation review is warranted because the change introduces a new internet-facing authentication boundary and closes a security finding across legacy/current routes. The user should select `MorWalPiz Code Reviewer` after implementation and focused validation.

## Testing Strategy

- **Domain/unit:** username/email normalization, password hash verification, account state/security-version transitions, session expiry/revocation, credentialless push claim preconditions, wrong keys, fixed-time credential checks, and one-time credential emission.
- **Backend integration/behavior (`MorWalPizVideo.BackOffice.Tests`):** register/login/session/profile/logout happy paths; duplicate/racing registration; invalid/missing CSRF; allowed/denied CORS origins; cookie flags; rate limits; inactive account; expiry; tampered/revoked session; admin JWT/API-key/internal identity denied from public-account policy where inappropriate.
- **Push regression (`PushSubscriptionEndpointTests`):** create through protected route then attempt every legacy overwrite/reactivation/revocation without credential; assert all record fields remain unchanged. Claim credentialless record with correct keys exactly once; endpoint-only/wrong-key/concurrent claims fail; legitimate public and BackOffice registration/settings/revoke continue.
- **Mongo/index:** normalized username/email uniqueness, session expiry index behavior, endpoint-hash uniqueness, real/mock parity, and duplicate preflight reporting. No production data mutation is inferred from mock tests.
- **Shared frontend services:** account calls include credentials and CSRF; public calls omit cookies/auth/channel/CSRF; admin behavior is unchanged; 401 handling is client-local; concurrent SSR clients do not share account context.
- **Public client components/routes:** registration validation/server errors, login failure/rate limit, session restoration, profile read/update, logout, expired-session redirect, keyboard focus on errors, and anonymous `/notifiche` before/during/after account sessions.
- **Header component:** anonymous actions, loading placeholder, authenticated profile/logout, long display name, logout failure, `hideLink`, desktop alignment, narrow-screen flow, keyboard order, accessible names, and no duplicate center navigation item.
- **Security:** no password/hash/salt/session token/push endpoint/keys/raw credential in response or logs; account enumeration resistance; CSRF and CORS negative tests; cookie tampering; session fixation prevention; password-change invalidation if password management is in scope.
- **End to end/browser:** actual `morwalpiz.com` to ServerAPI cross-origin cookie and CSRF flow, denied lookalike origin, restart/scale session behavior, service-worker push opt-in while signed out, and push settings after login/logout. The release probe must prove unauthenticated `GET /api/v1/account/csrf` returns `200` with exact credentialed CORS and secure cookie headers before registration is considered available.
- **CI ownership:** new backend tests carry `TestGroup:ServerAPI` (and Shared/CrossApi only where behavior truly spans ownership) so the ServerAPI deployment filter discovers them exactly. Public client and shared services use their existing Vitest workspaces.

## Migration And Rollback

### MongoDB

Public account/session collections are additive. Before activation, apply audited unique normalized-username/email indexes and the selected session expiry index. If duplicate preflight fails, stop; do not auto-merge or delete records. API-01 uses an atomic additive credential hash claim only for records whose hash is empty and keys match. Existing protected records are unchanged.

### API Contracts

Versioned public-account endpoints are additive. Existing anonymous push endpoints remain. Legacy push mutation routes have a measured compatibility window and then a stable retirement response; do not silently reinterpret their payload. Rollback may restore the prior deployment only while legacy direct mutation remains disabled, otherwise API-01 reopens.

### Authentication

Deploy persistent Data Protection and cookie configuration before enabling account UI. A feature flag may hide account routes/UI during rollout, but disabled state must fail closed. Rollback disables new account entry points and clears/rejects account cookies; it must not alter BackOffice JWT, internal service auth, or anonymous push capability behavior.

### Cache

No cache migration. Mark account/profile/session responses non-cacheable and exclude them from shared output caching. Push targeting/cache tags are unchanged.

### Configuration

Add only non-secret defaults to source. Session/cookie/rate-limit and optional email provider configuration come from protected environment settings or Key Vault. Exact allowed origins remain explicit. Missing production security configuration must prevent account activation rather than fall back insecurely.

### Deployment Order

1. Audit data and indexes; provision Data Protection/configuration.
2. Deploy API-01 correction and observe legacy usage/claim outcomes.
3. Deploy public account backend with UI feature disabled.
4. Validate the anonymous CSRF/session bootstrap and full cross-origin browser/session behavior against the deployed ServerAPI.
5. Deploy shared packages/public client and enable account UI only after the production account smoke gate passes.
6. Retire legacy push mutation routes after the approved zero-use window.
7. Update documentation and close API-01/TD-027.

### Recovery Limits

- A credentialless legacy push record cannot be recovered from endpoint knowledge alone. Without matching browser keys it must remain unclaimed or be replaced through browser unsubscribe/resubscribe.
- Rolling back after accounts are created must preserve additive account/session data for forward recovery; do not drop collections.
- Rotating or losing Data Protection keys invalidates account cookies and requires login again.
- Optional future account-to-push links are outside this rollout and therefore need no migration now.

## Open Questions

1. Must the first public-account release require email verification and include forgotten-password/reset delivery, or is immediate username/password activation acceptable for the initial scope? This changes provider configuration, token storage, abuse controls, UI, and rollout readiness.
2. What fixed account session lifetime is required, and should users be able to list/revoke other sessions in the first release?
3. What telemetry window and response (`404`, `410`, or another stable Problem Details contract) should govern final retirement of legacy push mutation routes?
4. Are there known external or old deployed consumers of legacy `api/push/*` routes that are absent from current source?
5. Should account-to-push linking remain deferred as recommended, or is cross-device notification management an explicit first-release requirement? If required, account deletion/unlink/retention semantics must be decided before implementation.

## Production Incident Decision

No new architecture decision is required for the observed CSRF `401`: source already declares the endpoint anonymous and the account client already targets it correctly. The implementation owner should first prove which layer produced the deployed response.

1. Confirm the ServerAPI deployment containing `PublicAccountController` completed after the account commit; a green public-client deployment is not evidence of a green ServerAPI deployment.
2. Capture the exact production response headers/body without an account cookie and with `Origin: https://morwalpiz.com`. A JWT `WWW-Authenticate` response indicates ASP.NET authorization or revision mismatch; an App Service/platform signature indicates pre-application authentication.
3. Verify App Service Authentication/Easy Auth and any gateway rule allow this anonymous path. Do not weaken the ServerAPI fallback policy globally.
4. Add a post-deploy smoke gate for anonymous CSRF/session bootstrap and credentialed CORS headers. The gate must fail deployment or UI activation on any `401`, missing exact origin, missing `Access-Control-Allow-Credentials`, or missing CSRF cookie.
5. Only if the deployed current artifact reproduces the `401` inside ASP.NET should implementation change. At that point, inspect endpoint metadata discovery and authentication scheme selection with a production-equivalent host test before editing controller attributes.