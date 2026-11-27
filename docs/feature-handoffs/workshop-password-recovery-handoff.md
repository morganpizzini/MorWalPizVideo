# Shared public-account recovery and email confirmation handoff

Status: implemented and locally verified; SMTP/deployed release gates remain open.
Owner: implementation team. Scope ends at the
shared public-account recovery/email confirmation capability and its Workshop and public-client
integrations.

## Outcome

Let a public-account user request an email link, choose a new password, and
sign in again. The recovery components and transport are shared; integrate
them into both Workshop and the public site's existing public-account login.
These are two clients of ServerAPI's public-account capability, not a change
to BackOffice, the held shop, or Freezer's separate account API. Send email
through configuration, with credentials supplied by the deployment environment,
never committed.

Accepted decisions (2026-11-26): reset links return to the requesting site
(this supersedes the earlier Workshop-only destination);
best-effort delivery is acceptable; reset resend is available after a 60-second
client-only cooldown; existing offline behavior is preserved. New ServerAPI
accounts must confirm their email before login. Existing accounts may still
login but receive a confirmation notice and email. Their temporary unverified
access remains accepted; do not introduce a legacy-account lockout.

Acceptance: an active account with a working mailbox can recover without
knowing its old password; an unknown or inactive email gets the same public
request response; expired, superseded and previously used links cannot change
a password; existing server sessions cease to authenticate after a successful
reset. The UI supports Italian and English, mobile and keyboard use. No
password is emailed. Recovery and email confirmation require connectivity.
New registration issues no session and shows a check-your-email intermediate
step. An unconfirmed new account cannot login, even via direct API calls.
Existing unconfirmed accounts get an online-login confirmation prompt; after
confirmation the prompt disappears. Tokens for reset and email confirmation
are distinct and cannot be substituted for one another.

## Historical Baseline Evidence

This section records the pre-implementation investigation. The delivery record
below and current source supersede its descriptions of immediate registration
sessions and missing confirmation state.

- [Workshop entry](../../frontend/shooter-workshop.client/src/main.tsx) installs
  the isolated account transport and redirects unauthorized responses to login.
  [Login](../../frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx)
  accepts username or email and uses `AccountForm`; [routes](../../frontend/shooter-workshop.client/src/routes/config.tsx)
  put login and registration outside the protected shell.
- [PublicAccountController](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs)
  exposes anonymous registration/login, uses the `public-account` CORS policy
  and validates a CSRF cookie/header. The host has an authenticated fallback.
- [PublicAccountService](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs)
  normalizes email, hashes passwords, issues opaque sessions and checks each
  session's account security version. [PublicAccount](../../MorWalPizVideo.Models/Models/PublicAccount.cs)
  stores password hash, salt, active state and security version.
  Current registration creates an authenticated session immediately; both
  [Workshop registration](../../frontend/shooter-workshop.client/src/routes/AccountRegister/component.tsx)
  and the public client's registration refresh it before showing login.
  No email-confirmation state exists in the inspected account model.
- [PublicAccountRepository](../../MorWalPizVideo.Domain/Interfaces/Repository.cs)
  stores accounts and sessions in MongoDB; profile changes currently use a
  targeted update. [MockRepository](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs)
  supplies the corresponding mock. The existing full-document account replace
  must not be used for recovery mutations.
- [Shared account service](../../frontend/fe-packages/services/src/publicAccountService.ts)
  obtains CSRF before anonymous account mutations. Shared account types and
  endpoint constants live in the models and services packages.
- [Public site login](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx)
  also uses that account service and shared `AccountForm`; its
  [routes](../../frontend/morwalpizvideo.client/src/routes/config.tsx) are
  different from Workshop's. [Layout](../../frontend/fe-packages/layout/package.json)
  already depends on models, React and the router, but not on the services
  package. Keep shared form presentation independent of API and app routing.
- [Newsletter SMTP](../../MorWalPizVideo.Domain/NewsletterEmailService.cs)
  demonstrates configured `System.Net.Mail` delivery and a mock. It is
  newsletter-specific, including its message ID and logging; it is not itself a
  password-recovery interface. [ServerAPI composition](../../MorWalPizVideo.ServerAPI/Program.cs)
  binds `Newsletter:Smtp` and selects mock/real newsletter mail by repository
  mode. Do not silently use the mock for real recovery.
- [Workshop specification](../architecture/shooter-workshop.md) describes the
  separate app and its offline behavior. The [security guide](../architecture/security.md)
  records a still-unverified deployed account-CSRF gate. Current source, not
  that guide's deployment assumptions, determines implementation.
- [Freezer account controller](../../FreezerAnalysis.Api/Controllers/FreezerAccountController.cs)
  also consumes the Domain public-account service but has its own API and
  cookie. Its separate backing account database must not be assumed to be the
  ServerAPI database; no Freezer recovery UI/API is included.

## Approved Implementation Contract

### Flow and contract

1. Add a login link in both apps to an anonymous request form accepting email.
   Implement request, reset, check-email and confirmation-notice forms as reusable components in
   `@morwalpiz/layout`, with typed callbacks, labels and state supplied by the
   host. App-local route wrappers invoke the shared account service, own
   navigation, and provide localized text and styling. Do not make layout
   depend on services or on either app. Submit through the shared account
   client, after acquiring CSRF. A valid
   request always displays a neutral message such as “If the account is
   eligible, check your email”; never promise actual delivery or disclose
   registration/active state.
   Include a bounded site identifier (`workshop` or `public-site`) in request
   DTOs; the server maps it to a fixed configured HTTPS origin and route.
   Reject unknown identifiers. Never accept a full redirect URL. Carry the
   identifier through registration, login mail dispatch and resend requests.
2. Add anonymous, CSRF-protected `POST /api/v1/account/password-recovery-requests`
   and `POST /api/v1/account/password-resets` under the existing account CORS
   policy. Both are no-store, bounded, and have dedicated per-IP fixed-window
   throttling that is not disabled by the existing optional login limiter.
   Invalid input may get normal validation errors independent of account
   existence; rate limiting and globally unavailable recovery return explicit,
   account-independent errors. Invalid/expired/used token returns a generic
   client error, not 401 (which would trigger Workshop's login redirect).
3. Decouple request response from account lookup and SMTP latency with a bounded
   in-process background queue. Enqueue normalized email, validated site,
   message purpose and correlation
   metadata; use a hosted worker and a new DI scope per item. Full/unavailable
   queue responds consistently with a service-unavailable error regardless of
   address. A worker looks up the account, creates a token only for active
   accounts, and attempts delivery. Do not persist the raw token. Log and
   count sanitized delivery/worker failures for operators, without telling the
   browser whether an account exists. Reuse this queue and sender for purpose-
   separated confirmation messages. This queue is explicitly best-effort:
   process termination can lose a request; the UI invites retry, and operators
   must monitor delivery. Durable delivery is outside this scope.
4. Reset email links use the configured HTTPS origin mapped to the validated
   requesting-site identifier and that site's fixed reset path. Both clients
   implement reset completion. Never use a
   request Host, Origin, return URL or other
   user-controlled address. Put an account identifier and 32-byte
   cryptographically random token in the URL fragment so normal HTTP requests
   and proxy access logs do not receive them. The SPA copies them to component
   memory and immediately removes the fragment with history replacement; no
   token in storage, analytics, logs, network URL or referrer. Link opening does
   not consume it; scanners must not reset accounts. Apply no-referrer and
   no-store protections to the recovery page and avoid third-party resources
   before fragment removal. Refresh after removal asks for a new link. A public
   site request finishes on the public site; a Workshop request finishes on
   Workshop. Only the enum-to-configured-origin mapping chooses the destination.
5. On explicit form submission, send identifier, token and new password in the
   POST body. Require the existing registration password bounds, confirmation
   in UI, and current password hashing helper. On success show sign-in, not an
   automatic session. Do not put an arbitrary return destination in email.
6. After an accepted reset request, disable resend for exactly 60 seconds with
   a shared client countdown based on a deadline, not interval ticks (background
   tabs must not extend the wait). At 60 seconds enable another request. Start
   the deadline only after acceptance; failed requests show errors. Do not
   persist the deadline or add per-account backend cooldown state. Reloading
   may reset it; independent backend anti-abuse rate limits remain mandatory.

### Email confirmation and registration

- Introduce optional confirmation state: confirmed UTC timestamp and normalized
  email binding, a requires-confirmation-before-login marker, plus confirmation
  token hash and expiry. Newly created ServerAPI accounts explicitly require
  confirmation. Missing marker means legacy, not confirmed; legacy login is
  permitted, but no false verified label is shown. Do not infer legacy status
  from dates or retroactively mark existing emails verified.
- Separate account creation from automatic session creation in Domain. Reuse
  registration validation, conflict behavior, hashing and legal acceptance;
  ServerAPI uses the new no-session creation path. Keep Freezer's existing
  registration/session semantics and constructors intact; it owns a different
  account store and is not being converted to this workflow. No API-to-API
  references or email dependency in the general account-service constructor.
- ServerAPI registration returns the existing session-shaped response with
  authenticated false and an additive confirmation-required status; no session
  cookie is created. Shared client registration must stop assuming it always
  authenticates. Both updated clients navigate to the check-email intermediate
  step, do not refresh/restore a session, clear password inputs, and offer
  resend and sign-in after confirmation. Preserve registration validation and
  duplicate/conflict handling.
- Confirmation issuance uses 32 random bytes, hash-only storage and a 24-hour
  expiry, separately named fields/purpose from reset. Bind issuance and
  redemption to the current normalized email and active state; last issued
  confirmation wins. Atomic redemption sets the confirmed timestamp/email
  binding and clears only confirmation fields. It never changes the password,
  creates a session or increments security version merely for confirmation.
- Add anonymous CSRF-protected, no-store request and redeem endpoints at
  `POST /api/v1/account/email-confirmation-requests` and
  `POST /api/v1/account/email-confirmations`, with dedicated anti-abuse policies.
  Request accepts email and always returns a neutral acknowledgment regardless
  of account existence/eligibility. Confirmation errors do not use 401.
  Confirmation links also land on the requesting site's fixed confirmation
  page with fragment-held tokens (consistent with the corrected reset routing),
  the same early scrubbing, and an explicit Confirm action (GET/navigation
  alone must not consume a token). After success show sign-in; do not poll.
- Check new-account confirmation after valid password verification and before
  issuing a session. Return a typed unconfirmed result without a session
  cookie, distinct from incorrect credentials. Both clients show check-email,
  not a generic credential error; ensure Workshop's AuthProvider does not
  swallow this result or send it through the global 401 redirect. Validate
  session eligibility as well, so an accidental old session cannot bypass the
  new-account policy.
- On successful online login for a legacy unconfirmed account, return the
  ordinary session and request a confirmation email. Queue admission failure
  must not fail an otherwise permitted legacy login; show a retry notice.
  Show the shared confirmation banner in both apps until session/profile
  refresh reports confirmation. Include a resend action. A current unexpired
  confirmation issuance can suppress repeated automatic mails; explicit
  resend replaces it. Do not send email from render, session restoration,
  offline restoration, or every protected request.
- A profile email change atomically clears the old confirmation and reset
  fields and confirmation binding; the new address needs confirmation and the
  same shared notice. Never confirm a changed address using an old token.
  Preserve legacy login permission; for new accounts the confirmation-before-
  login marker remains in force. Existing sessions need not be revoked solely
  for this notice, but current-state session validation must enforce that
  marker. Warn new-account users before a change that requires reconfirmation;
  return accurate confirmation/session state. Route gated users to check-email
  immediately after the successful update, request confirmation through the
  shared endpoint, and require sign-in after confirmation; do not refresh a
  now-ineligible session and silently lose the confirmation instructions.
- Password reset does not implicitly confirm email or bypass the new-account
  login requirement. If both are pending, show the separate confirmation step.
  Send notices truthfully: the intermediate step says the confirmation email
  has been requested and must be approved before login; best-effort queue
  acceptance does not prove SMTP delivery. Use “sent” only if actual SMTP
  acceptance is known, never on queue success or failure. Offer retry when
  nothing arrives; an incorrect/inaccessible mailbox still cannot self-recover.
- Reuse the 60-second client-only resend UI for confirmation as well; this is
  a UX convenience, not a substitute for the independent backend rate limits.

### Atomicity, identity and delivery

- Add optional reset-token hash, UTC expiry and issuance security-version
  fields to the existing public-account document. Store only SHA-256 of the
  random token; lifetime 30 minutes. Last issued request wins. Keep no TTL on
  account documents; expiry is checked during consumption. Existing records
  with no fields cannot be reset until a request issues them.
- Perform issuance with a targeted conditional account update requiring active
  state, the looked-up normalized email, and the same security version. If
  issuance loses a race, do not send a usable link. Profile email changes
  atomically clear outstanding recovery fields when normalized email changes.
  A successful reset is one Mongo single-document conditional update matching
  account ID, active status, hash, expiry and issuance/current security version;
  it replaces hash/salt, clears reset fields and increments security version in
  the same update. Only one concurrent redemption can succeed. Use equivalent
  synchronized mock semantics. Reset and profile updates must not overwrite
  other account fields.
- The version increment invalidates already-issued sessions at their next
  server validation, including a login racing with the reset using a previously
  read account version. It does not erase locally prepared Workshop offline
  snapshots on another device; do not claim remote offline revocation. This is
  not offline password login: Workshop currently restores a previously
  remembered identity on a network failure and permits prepared offline use.
  A reset made on a different device cannot reach a device that is offline;
  when it reconnects, server validation rejects the old session. The local
  snapshot can remain on the device. Changing offline behavior or erasing
  remote data is out of scope.
- Reuse existing SMTP configuration shape and transport patterns without
  treating newsletter delivery as a recovery-specific abstraction. Bind a
  dedicated `PublicAccount:Mail:Smtp` section for both account mail purposes,
  with host, port, TLS, username, password and sender, plus trusted origins for
  both sites. Validate
  configuration without printing secrets; missing/invalid settings disable
  request admission uniformly and produce an observable operational error.
  Do not admit new registrations when mail infrastructure is known unavailable;
  if account creation succeeded before a later enqueue/send failure, return
  an explicit pending-account retry state rather than silently repeating
  registration. The account remains unconfirmed and can resend after recovery.
  Require secure TLS and do not weaken certificate validation. Inject a fake
  sender in tests. No real email should be sent in tests.
- Recovery trusts the currently stored email as the mailbox authority. New
  registration confirmation and email-change invalidation are now in scope.
  Existing unconfirmed accounts remain permitted during the accepted transition.
  An unavailable mailbox means the address is wrong, inaccessible, or cannot
  receive the message. Self-service reset cannot work without access to it;
  provide no bypass endpoint. Support ownership/process is a separate
  operational decision, not a requirement to implement account recovery.

### Configuration and implementation-end reminder

Read through `builder.Configuration` after the existing Key Vault provider is
loaded; bind and validate typed mail options. The inspected ServerAPI uses
`AddAzureKeyVault` with `DefaultAzureCredential`. Key Vault hierarchical secret
names use double hyphens. No new credential provider or committed values.

| Configuration key | Key Vault secret name |
| --- | --- |
| `PublicAccount:Mail:Smtp:Host` | `PublicAccount--Mail--Smtp--Host` |
| `PublicAccount:Mail:Smtp:Port` | `PublicAccount--Mail--Smtp--Port` |
| `PublicAccount:Mail:Smtp:EnableSsl` | `PublicAccount--Mail--Smtp--EnableSsl` |
| `PublicAccount:Mail:Smtp:Username` | `PublicAccount--Mail--Smtp--Username` |
| `PublicAccount:Mail:Smtp:Password` | `PublicAccount--Mail--Smtp--Password` |
| `PublicAccount:Mail:Smtp:From` | `PublicAccount--Mail--Smtp--From` |
| `PublicAccount:Mail:WorkshopOrigin` | `PublicAccount--Mail--WorkshopOrigin` |
| `PublicAccount:Mail:PublicSiteOrigin` | `PublicAccount--Mail--PublicSiteOrigin` |

WorkshopOrigin must match the controlled HTTPS Workshop origin
`https://workshop.morwalpiz.com`; PublicSiteOrigin must match
`https://morwalpiz.com`. Paths are application-owned, not separately
configurable. Validate port/TLS against the provider; the existing SmtpClient
pattern supports STARTTLS, not implicit SMTPS. If the chosen provider requires
implicit SMTPS, resolve that compatibility before implementation rather than
silently disabling TLS. Key Vault activation still needs the existing
`FeatureManagement:EnableKeyVault` and `KeyVaultUrl` bootstrap settings plus
service-identity access. Implementation can use mocks without real values.
At implementation completion, explicitly remind the user of all eight mail
keys and Key Vault mappings, and identify deployment validation pending until
the user supplies them; do not claim real email delivery was verified.

## Impacted Projects

- `MorWalPizVideo.ServerAPI`: anonymous endpoints, configuration, admission,
  limiter, worker, SMTP registration, pending registration and confirmation
  login/session policy.
- `MorWalPizVideo.Domain` and `MorWalPizVideo.Models`: recovery and confirmation
  invariants, no-session account creation, storage updates, hashing and optional
  persisted fields.
- `MorWalPiz.Contracts`: request contracts and validation.
- `@morwalpizvideo/models` and `@morwalpizvideo/services`: shared typed request
  shapes, endpoints and isolated account transport.
- `@morwalpiz/layout`: reusable, presentation-only recovery/confirmation forms,
  notices and resend countdown with app-provided text, callbacks and state.
- `shooter-workshop.client` and `morwalpizvideo.client`: login links, anonymous
  route wrappers, localization and browser leak prevention. Freezer is a
  verify-only consumer of the Domain service.
- `MorWalPizVideo.BackOffice.Tests`: endpoint and repository behavior tests;
  shared frontend and Workshop tests cover transport and UI. Real Mongo
  concurrency proof and deployed browser/SMTP evidence remain release gates.
- Deployment: ServerAPI secure settings, both trusted site origins and
  coordinated registration cutover; no deployment topology change.

## Impacted Files

### Modify

- [PublicAccountController.cs](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs):
  four anonymous CSRF-protected request/redeem endpoints, no-session
  registration response, typed confirmation status and legacy login dispatch.
- [Program.cs](../../MorWalPizVideo.ServerAPI/Program.cs): recovery policy,
  configuration, scoped worker dependencies and sender selection.
- [PublicAccountService.cs](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs):
  extract/reuse account creation, add no-session registration and account-state
  eligibility to login/session validation. Mail orchestration stays outside
  Freezer's existing account-service constructor.
- [PublicAccount.cs](../../MorWalPizVideo.Models/Models/PublicAccount.cs):
  additive nullable reset/confirmation state and explicit new-account login
  confirmation marker.
- [IRepository.cs](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs),
  [Repository.cs](../../MorWalPizVideo.Domain/Interfaces/Repository.cs), and
  [MockRepository.cs](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs):
  atomic issue/consume and email-change invalidation.
- [PublicAccountContracts.cs](../../MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs):
  bounded request DTOs, validated site identifier, confirmation state and
  registration/login result extensions. Session/profile summaries report
  confirmation from current server state, never browser inference.
- [publicAccount.ts](../../frontend/fe-packages/models/src/publicAccount.ts),
  [endpoints-frontend.ts](../../frontend/fe-packages/services/src/endpoints-frontend.ts),
  [publicAccountService.ts](../../frontend/fe-packages/services/src/publicAccountService.ts),
  and relevant package exports: typed calls and CSRF.
- [layout exports](../../frontend/fe-packages/layout/src/index.ts): expose
  reusable, API-independent request/reset/check-email/confirmation components.
- [config.tsx](../../frontend/shooter-workshop.client/src/routes/config.tsx),
  [login](../../frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx),
  [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx), and
  [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx): anonymous
  navigation, early fragment removal, English/Italian labels and errors.
- [Workshop registration](../../frontend/shooter-workshop.client/src/routes/AccountRegister/component.tsx),
  [AuthProvider](../../frontend/shooter-workshop.client/src/auth/AuthProvider.tsx)
  [AppShell](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx)
  and [profile](../../frontend/shooter-workshop.client/src/routes/AccountProfile/component.tsx):
  pending-registration step, typed blocked-login result and legacy confirmation
  notice/resend, without offline mail dispatch.
- Public-client [account components](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx),
  [routes](../../frontend/morwalpizvideo.client/src/routes/config.tsx),
  [entry](../../frontend/morwalpizvideo.client/src/main.tsx) and
  [return URL helper](../../frontend/morwalpizvideo.client/src/routes/account/returnUrl.ts):
  link and route wrappers, early fragment handling, safe navigation and
  existing Italian presentation.
- [Public account context](../../frontend/morwalpizvideo.client/src/routes/layout/account.tsx)
  and [root layout](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx):
  current confirmation status/banner and refresh after
  confirmation/profile changes.
- Existing focused [endpoint tests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs),
  [repository tests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountRepositoryTests.cs),
  [service tests](../../frontend/fe-packages/services/src/publicAccountService.test.ts)
  and [login tests](../../frontend/shooter-workshop.client/src/routes/AccountLogin/component.test.tsx).

### Add

- Account-mail sender, bounded queue/worker and options in their owning
  ServerAPI/Domain directories; use the existing SMTP implementation as prior
  art, not newsletter-specific logging or identifiers.
- Workshop route modules and focused component/browser tests under
  `frontend/shooter-workshop.client/src/routes/` and its existing `e2e/` folder.
- Shared layout recovery components and focused tests, plus public-client route
  integration and tests under its existing account feature.
- Focused service/worker tests in the existing backend test project if not
  naturally covered by the existing endpoint and repository files.

### Generated/Migration

No generated migration or backfill. Missing confirmation-policy marker denotes
legacy; new ServerAPI accounts explicitly opt into confirmation-before-login.
No new collection/index. Conditional issuance/consume must be verified against
real MongoDB. This is a behavioral registration/login transition even though
the storage additions are compatible.

### Documentation

Update [security.md](../architecture/security.md),
[shooter-workshop.md](../architecture/shooter-workshop.md), and the relevant
operational configuration/release notes. Record the new recovery, confirmation
gate and legacy transition in an ADR: these now change identity policy.
Update this handoff after implementation with
actual decisions, files, results and residual risks.

### Verify Only

[FreezerAccountController.cs](../../FreezerAnalysis.Api/Controllers/FreezerAccountController.cs),
[workshop service worker](../../frontend/shooter-workshop.client/public/workshop-sw.js),
[Workshop deployment workflow](../../.github/workflows/deploy-shooter-workshop-client.yml),
ServerAPI ingress/logging and existing session validation. Confirm shared
consumers still compile and that the deployed SPA serves the new deep link.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Four anonymous CSRF-protected mail endpoints, registration/login policy and bounded worker. |
| Frontend/UI | Shared recovery/confirmation, pending-registration step and legacy notice in both clients; no redesign. |
| Database/Data | Optional account fields, new-account policy marker, atomic update, no backfill; best-effort queue is transient. |
| Contracts/Models | Additive fields/site identifier; registration no longer authenticates; typed unconfirmed login outcome. |
| Authentication/Authorization | New-account confirmation gate, legacy login retained, reset increments security version; no admin/shop change. |
| Validation | Email and password bounds, token format/expiry, active state, configured HTTPS origin. |
| Cache | No-store recovery responses; service worker never caches API data. No content tag changes. |
| Configuration/Secrets | Shared account SMTP and two trusted origins from Key Vault-backed configuration. |
| Logging/Telemetry | Sanitized status/counts only; no email, passwords, tokens, full links or SMTP secrets. |
| Background Jobs | Bounded in-process queue, no persistence or automatic durable retries; restart loss explicit. |
| Deployment/Operations | Coordinated registration cutover, secure SMTP provisioning, both-client link/confirmation proof. |
| Backward Compatibility | Legacy account access and Freezer preserved; old registration clients must upgrade. Offline data is not remotely erased. |

## Reuse Opportunities

Reuse `AccountForm`, current login patterns, localization, account CSRF client,
password hashing, session security-version validation, Mongo targeted updates,
fixed-window limiter patterns, SMTP framework and backend test factory. Do not
reuse newsletter message identifiers or force newsletter credentials to be
password-recovery credentials.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Recovery token replay or races could take over an account. | Atomic conditional consume/version increment; real Mongo parallel redemption proves one success, old sessions fail. |
| High | Unconfirmed new accounts bypass the gate or legacy users are locked out. | Explicit per-account marker, server-side login/session checks and tests for missing markers/new records; no blanket legacy backfill. |
| High | Email-change/token races falsely confirm an address. | Purpose-separated tokens and atomic normalized-email-bound redemption; test old links after profile edits and reset/confirm cross-use. |
| High | Old registration client/server bypasses the new policy or cannot display pending state. | Coordinated cutover, compatibility tests, do not roll back to an ungated server after new gated accounts exist. |
| Medium | Legacy mailbox remains unverified and offline data remains accessible locally. | Accepted temporary legacy access and unchanged offline behavior; test server revocation at reconnect. |
| High | Secret leakage through URL, telemetry or access logs. | Fragment-only link, early removal, no referrer and sanitized logging; inspect browser, proxy and telemetry evidence. |
| Medium | Enumeration and mail flooding through anonymous requests. | Uniform admission response and async lookup, per-IP rate limits, bounded queue; compare responses/timing and exercise throttling. Limiter is per instance; assess ingress limits for scale-out. |
| Medium | SMTP outage, missing settings or worker restart loses mail. | Fail admission uniformly when globally unavailable, log failures, monitor queue/send health and support retry; test failure and recovery. No delivery guarantee. |
| Medium | Deployment or origin mismatch breaks link, cookie or CSRF. | Server-first deployment and real-origin hard reload, CSRF and mailbox test; verify existing deployment blocker. |
| Medium | Wrong-site routing/open redirect leaks reset capability. | Closed site enum mapped to exact configured origins and fixed paths; test each site and reject arbitrary URLs/unknown identifiers. |
| Low | Old documents/other apps affected by shared-model changes. | Nullable additive fields, focused regression tests and cross-consumer build. |

## Technical Debt

- Legacy unverified accounts retain access while confirmation is introduced.
  New registration confirmation and email-change invalidation are required
  scope, not deferred debt. Device management and remote local-data deletion
  remain out of scope.
- Existing newsletter SMTP service is specialized; avoid making it a generic
  account-mail dependency merely to eliminate a few lines of transport setup.
- In-memory rate limiting and best-effort work do not provide distributed
  abuse protection or durable delivery. Escalate those separately if production
  scale or delivery SLO requires it.

## Implementation Roadmap

1. Establish both trusted site origins and shared SMTP settings in Key Vault,
   confirm STARTTLS compatibility and check the deployed CSRF blocker. Record
   the accepted best-effort/legacy/offline decisions and cutover window.
2. Add additive account state, conditional repository operations and mock
   equivalence for both token purposes, plus the explicit new-account marker.
   Gate on expiry/replay/email-change/session-version/concurrency and legacy
   deserialization tests.
3. Separate no-session registration from Freezer's existing registration,
   implement new-account login/session eligibility, add typed contracts and
   four endpoints. Add purpose/site-aware bounded worker, fake/real sender,
   no-store/CSRF/rate limits and sanitized observability. Gate on new vs legacy
   login, mail failure, pending-account retry and enumeration tests.
4. Add shared transport/types and layout forms/notices/countdown, then integrate
   both clients' request/reset/confirmation/check-email routes and login banners.
   Add the site identifier, remove token fragments early, and preserve each
   site's conventions. Gate on focused shared/per-app tests, including exact
   60-second timing and both link destinations.
5. Validate real Mongo atomicity, browser/deep-link behavior and a controlled
   SMTP delivery in a safe environment. Update architecture/operations docs
   and this handoff. Perform the coordinated registration cutover described
   below and prove both sites on their actual origins. Remind the user of the
   eight mail configuration keys and pending real-email verification.
6. Stop after the acceptance criteria. An independent security-aware code review
   is warranted because this adds anonymous credential-reset and mail surfaces;
   the user decides whether to select a reviewer.

## Testing Strategy

- Backend unit/repository: unknown, inactive, normalized email, issuance race,
  token expiry, supersession, email change, wrong token, replay, password
  bounds, concurrent reset and old/new password/session behavior.
- New-account tests: registration creates no session/cookie, login/session
  rejected until confirmation, legal acceptance preserved, queue failure after
  creation gives a pending retry result, confirmation does not login or reset
  a password. Legacy records missing fields can login and are not labeled
  verified. Repeated session/offline restore must not send mail.
- Confirmation tests: expiry, replay, supersession, email changes, distinct
  token purposes, inactive account, legacy login notice and resend, successful
  confirmation clears notice after refresh. Invalid credentials never disclose
  confirmation status. Retain Freezer registration regression coverage.
- Backend integration: anonymous metadata, CSRF missing/valid, CORS allow/deny,
  no-store, independent throttles, queue full, SMTP misconfiguration/failure,
  uniform eligible/ineligible responses and no sensitive logs. Mock SMTP only.
- Shared client: correct endpoint, credentials and CSRF, no admin bearer, no
  token in URL, pending registration/non-401 unconfirmed login, exact site
  mapping and rejected arbitrary redirect destinations.
- Shared components and both apps' component/browser tests: all account-mail
  forms, check-email/banner states, validation, password
  confirmation, pending/error/success, expired link, offline, Italian/English,
  focus/keyboard, small and desktop widths, deep-link reload and fragment
  removal; compare existing visual conventions. Check no external requests see
  the token and API calls bypass the service worker cache.
  Fake-time proof: resend is disabled at 59 seconds, enabled at 60, handles
  background-tab clock jumps and retries, and never requests backend cooldown
  tracking. No mail is sent on render or offline restoration.
- Release evidence: real Mongo atomicity, controlled mailbox delivery, SMTP
  error monitoring and actual deployed client origins; backend tests alone do
  not prove these. No production mail or database mutation during planning.
- Impeccable implementation lens: **Operate**, refinement of existing logins,
  not redesign. No Workshop PRODUCT.md or DESIGN.md was found; use its current
  login and specification as visual authority. The public client has a
  [DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md); consult it and
  the current public login before styling its integration. Apply the narrow
  `harden` workflow during implementation and capture desktop/mobile evidence
  including keyboard, zoom, labels, pending, failure and success states.

## Migration And Rollback

No Mongo/SQLite backfill, auth-cookie or cache-tag migration, or Freezer data
transition is needed. The additive marker treats existing accounts as legacy;
new ServerAPI writes explicitly require confirmation. Do not eventually
contract legacy access without separate approval.

Registration semantics change: older clients assume an authenticated response.
Use a short coordinated registration maintenance window rather than a mixed
fleet silently creating ungated accounts. Deploy the backend and both clients
as one release, drain old ServerAPI instances, and resume registration only
after pending-registration/confirm/login tests pass on both origins. A required
site identifier makes stale registration clients fail explicitly instead of
bypassing the new path. Preserve existing-account login during cutover, but
do not claim old clients show the new confirmation UI. Existing PWA clients
must receive the new build before completing the new registration flow.

Rollback may remove client mail affordances or stop new mail admission, but
must not re-enable login for unconfirmed new accounts. After gated accounts
exist, retain the new policy-aware backend or forward-fix; rolling back to the
old server would bypass that policy. Preserve password/security-version and
confirmation state, never restore stale account documents. Verify older
deserializers tolerate added BSON fields before any compatible rollback.
In-flight in-memory requests may be lost during restart; users can resend.

## Open Questions

None blocking local implementation. The latest direction resolves reset
routing: each requesting site handles its own reset. Confirmation links use
the same site mapping. Legacy login remains permitted with a confirmation
notice, while new-account login is gated; this is the interpretation of
"after login" for existing accounts.

Deployment prerequisite: the user provides the eight documented Key Vault mail
settings and a compatible SMTP provider. If that provider cannot use STARTTLS,
resolve the transport choice before deployment. Mailbox-support ownership
is a separate operational decision; never add an identity bypass.

## Actual Delivery

### Owning Changes

- [Account model](../../MorWalPizVideo.Models/Models/PublicAccount.cs) and
  [Contracts](../../MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs):
  additive gate/confirmation/token state, safe response fields and mail DTOs.
- [Account service](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs),
  [mail service](../../MorWalPizVideo.Domain/Accounts/PublicAccountMailService.cs),
  [repository contract](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs),
  [Mongo](../../MorWalPizVideo.Domain/Interfaces/Repository.cs) and
  [mock](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs): pending
  registration, login/session eligibility, atomic token consumption and email
  update invalidation. Freezer's existing registration method is retained.
- [Account controller](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs),
  [mail controller](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountMailController.cs),
  [delivery worker](../../MorWalPizVideo.ServerAPI/Services/AccountMailDelivery.cs),
  [composition](../../MorWalPizVideo.ServerAPI/Program.cs) and
  [configuration](../../MorWalPizVideo.ServerAPI/appsettings.json): four
  endpoints, mandatory throttling, 100-item best-effort queue, configured
  SMTP/mock sender, fixed site mapping and sanitized failures. No secrets added.
- [Shared models](../../frontend/fe-packages/models/src/publicAccount.ts),
  [transport](../../frontend/fe-packages/services/src/publicAccountService.ts),
  [early token capture](../../frontend/fe-packages/services/src/accountMailLink.ts),
  [mail UI](../../frontend/fe-packages/layout/src/components/AccountMail.tsx)
  and [AccountForm](../../frontend/fe-packages/layout/src/components/AccountForm.tsx):
  purpose/site contracts, isolated account-CSRF calls, memory-only tokens,
  shared localized pages/notices and deadline-based resend. Cooldown disabling
  is separate from the pending label.
- [Workshop routes](../../frontend/shooter-workshop.client/src/routes/AccountMail.tsx),
  registration/login, auth provider, shell, entry point, localization and nginx:
  anonymous recovery/check-email/confirmation routes and legacy-account notice.
- [Public-site routes](../../frontend/morwalpizvideo.client/src/routes/account/Mail.tsx),
  [notice](../../frontend/morwalpizvideo.client/src/routes/account/ConfirmationNotice.tsx),
  account components/context, route manifest, entry point, nginx and
  [SSR server](../../frontend/morwalpizvideo.client/server.mjs): the same
  capability using `public-site`, with sensitive-page no-store/no-referrer and
  no SSR hydration or third-party widget initialization.
- Backend mail/Mongo tests and updated account/feedback fixtures, shared
  transport/token tests, and both clients' focused mail tests establish the
  behavior. Architecture backend/security/frontend/Workshop guides and
  [ADR-031](../architecture/adr/ADR-031-public-account-mail-confirmation.md)
  record the delivered boundary.

### Compatibility And Delivery Details

The shared registration DTO leaves `Site` optional for Freezer; ServerAPI alone
requires a known site. Login site remains optional for old callers; automatic
confirmation dispatch requires a valid known site. Legacy login remains allowed,
while new ServerAPI accounts cannot login or validate sessions until confirmed.

Email text is bilingual. UI calls acceptance a request, not proof that mail was
sent. Automatic legacy-login confirmation does not supersede an unexpired
confirmation token; explicit resend does. Profile email changes invalidate both
mail purposes and confirmation binding atomically. No package dependencies,
cookie shapes, offline snapshots, or held-shop workflows were changed.

### Local Verification

- Release build of `MorWalPizVideo.BackOffice.Tests` with `--no-restore -m:1
  -p:BuildInParallel=false`, then `dotnet test --no-restore --no-build --filter
  'FullyQualifiedName~PublicAccount|FullyQualifiedName~FeedbackEndpointTests|FullyQualifiedName~HostSecurityContainmentTests'`:
  **43 passed, zero skipped**. `ACCOUNT_MAIL_TEST_MONGO_URI` selected a disposable
  local MongoDB 8 instance; tests used unique databases and removed them.
  Covers new/legacy login, token purpose/expiry/replay, profile invalidation,
  concurrent atomic reset, session revocation, CSRF/no-store, mandatory mail
  throttling, queue/config failures and sanitized SMTP failure/continuation.
- `yarn --cwd frontend workspace @morwalpizvideo/services test
  src/publicAccountService.test.ts src/accountMailLink.test.ts`: **10 passed**
  for mail transport and early fragment capture.
- Final `yarn --cwd frontend workspace shooter-workshop.client test
  src/routes/AccountMail.test.tsx`: **4 passed**, including disabled-at-59 and
  enabled-at-60-second proof, clock jumps, failure retry, explicit consumption,
  password mismatch and Workshop site routing.
- Final `yarn --cwd frontend workspace morwalpizvideo.client test
  src/routes/account/Mail.test.tsx`: **1 passed** for localized public-site
  routing. Earlier selected public account regression tests also passed.
- `yarn --cwd frontend workspace @morwalpiz/layout build`,
  `yarn --cwd frontend workspace shooter-workshop.client build` and
  `yarn --cwd frontend workspace morwalpizvideo.client build:all`:
  **passed**, including public client and SSR bundles.
- Freezer Release build with the same serial flags, then `dotnet test
  FreezerAnalysis.Api.Tests/FreezerAnalysis.Api.Tests.csproj --configuration
  Release --no-restore --no-build --filter
  'FullyQualifiedName~Development_mock_requires_login_and_issues_a_cookie_session'`:
  **1 passed, zero skipped**. The first build found an absent existing Playwright
  package; restoring the existing project dependencies resolved it without a
  dependency-manifest change.
- Controlled Chromium desktop/mobile exercises passed for **both sites**:
  recovery, correct site payload, resend disabling, fragment removal before
  requests, no automatic confirmation consumption, explicit confirmation and
  successful reset. API calls were intercepted; this is UI evidence, not real
  SMTP or deployed API/cookie evidence. Captures remain in session artifacts.
- Existing .NET dependency/nullable warnings and frontend Sass/bundle warnings
  are not repaired by this feature. Routine broad suites, lint/format and git
  hygiene checks are deferred to downstream automatic final validation.

Owned local frontend servers and the disposable Mongo container were stopped.
Build-only tracked output changes were removed; useful browser artifacts remain
in the session workspace.

### Remaining Release Gates

Provide the eight keys in the configuration table through the existing Key Vault
provider, then verify authenticated STARTTLS and controlled mailbox delivery.
No real SMTP, Key Vault, deployment, production database write or commit was
performed. Deployed CSRF/cookie/origin verification and the coordinated
registration cutover remain required. Keep a policy-aware backend during any
rollback; legacy access and previously prepared offline data remain intentionally
unchanged. Queue work can be lost on restart; users may request another email.
