# Shooter Workshop Capability Wizard Handoff

**Status:** Implemented. The account marker, authenticated Workshop endpoint,
shell-level modal, replay control, legacy compatibility behavior, and focused
validation are complete. Same-origin proxy deployment remains an environment
smoke-test responsibility.

### Delivery record

Implemented across Models, Contracts, Domain, ServerAPI, shared frontend models
and services, and `frontend/shooter-workshop.client`. The old dashboard-only
`OnboardingGuide` and its test were removed; `CapabilityWizard` now owns the
four-step presentation from `AppShell`. The modal traps focus, supports Escape
and backdrop dismissal, marks the shell content inert, links to canonical
Workshop routes, and reports marker-save failures without silently treating them
as success.

Validation completed:

- PublicAccount-focused backend tests: 11 passed.
- Shared models, services, and Workshop client production builds: passed.
- Workshop client test suite: 48 passed.
- CapabilityWizard focused test: 2 passed.

Not run in this environment: a deployed/container same-origin `/api` smoke test
covering cookies, CSRF, marker persistence, and refresh. Browser-sized visual
capture was also unavailable; the implementation uses the existing Workshop
visual system and responsive breakpoints.

### Outcome

Replace the existing dashboard-only setup guide with a wide, multi-step Shooter
Workshop capability wizard. It opens automatically once for a newly created
PublicAccount when that account first reaches the authenticated Workshop shell,
and it can always be reopened from a button in the top-right account controls.

Persist a versioned **Workshop capability guide seen marker** on PublicAccount.
Do not infer first use from a last-login timestamp: PublicAccount has no such
field today, registration immediately creates an authenticated session, and a
login timestamp would describe authentication history rather than whether the
Workshop guide was presented.

Accepted decisions:

- Existing account documents with no marker are interpreted as having seen
  version 1. They are not interrupted and can use the header replay button.
- New registrations explicitly start at version 0 and receive the automatic
  guide the first time they reach the Workshop shell.
- The server marker records that the automatic guide was presented, not that
  the user completed business setup.
- Skip, close, completion, and manual replay remain presentation behavior; none
  is treated as proof that the user created Workshop data.
- The current browser-local onboarding state is retained only during the
  compatibility transition and is no longer authoritative after activation.

### Current Evidence

- `MorWalPizVideo.Models/Models/PublicAccount.cs` owns shared account identity,
  legal acknowledgement, activation, and security-version fields. It has no
  last-login or onboarding field.
- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs` creates a session
  immediately after both registration and login. Login does not currently
  update PublicAccount, and session validation returns the current account.
- `MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs` and
  `frontend/fe-packages/models/src/publicAccount.ts` expose authenticated
  account summary data used by Workshop's `AuthProvider`.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` returns the
  same summary for registration, login, and session restoration. A
  login-only signal would therefore miss registration and would not remain
  available on restored sessions.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` is the persistent
  authenticated shell. It renders the top-right
  language/account/install/logout controls and primary Workshop navigation.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx`
  implements the current four-step inline setup guide.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` limits
  that guide to empty workspaces and offers replay only from the empty
  dashboard.
- `frontend/shooter-workshop.client/src/services/workshopPreferences.ts`
  stores `onboardingState` and `onboardingStep` under an account-keyed
  browser-local key. That state cannot roam, survive storage clearing, or
  reliably govern first presentation across devices.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx`
  establishes Workshop dialog behavior: backdrop, background inerting,
  initial focus, Escape, Tab containment, and focus restoration.
- `frontend/shooter-workshop.client/src/i18n.tsx` owns English source strings
  and Italian translations. `src/main.scss` owns the Workshop visual system,
  header controls, responsive behavior, and current onboarding/dialog styles.
- ServerAPI already owns authenticated, account-scoped Workshop routes under
  `/api/v1/workshop`; writes use the PublicAccount cookie identity and CSRF.
- `MorWalPizVideo.ServerAPI/Program.cs` already registers
  `IPublicAccountService`, PublicAccount Mongo/mock repositories,
  `WorkshopService`, the PublicAccount authentication scheme, controllers, and
  the credentialed Workshop origin. The marker does not require a new service
  registration, database connection, host, or authentication scheme.
- `frontend/shooter-workshop.client/Dockerfile` builds the client with a
  relative API base, and `nginx.conf.template` proxies same-origin `/api/`
  requests to the configured ServerAPI origin. Production Workshop account
  cookies and CSRF therefore flow through the first-party Workshop origin;
  direct cross-origin CORS remains a controlled fallback/diagnostic path.

### Proposed Design

#### Account marker

Add an integer PublicAccount field conceptually named
`WorkshopCapabilityGuideSeenVersion`, serialized additively with a default of
one for missing legacy data. Registration explicitly writes zero for newly
created accounts.

Use a version rather than a boolean or timestamp:

- `0` means no server-recorded Workshop capability guide;
- `1` means version 1 has been presented;
- a missing field means a legacy account and is read as version 1;
- future materially changed onboarding may increment the current guide version
  without redefining historical timestamps or adding another flag.

The field is presentation metadata. It must not grant access, alter session
security, imply setup completion, or affect other applications that share
PublicAccount.

Expose the seen version additively in the authenticated PublicAccount summary
returned by registration, login, and session restoration. The public site and
other account consumers may ignore the optional additive field; Workshop uses
it only after authenticated session restoration.

#### Mark-seen endpoint

Add an authenticated Workshop-specific write endpoint under the existing
`/api/v1/workshop` boundary. It accepts the guide version, requires the
PublicAccount cookie scheme and CSRF, and updates the current account only.

The Domain account service owns the mutation because PublicAccount is its
aggregate. The operation is monotonic and idempotent:

- a requested version below the stored version does not downgrade it;
- the current version may be repeated safely;
- unsupported future versions are rejected rather than stored blindly;
- inactive, missing, or mismatched account identity fails explicitly.

Return the effective seen version so the client can update its in-memory session
snapshot. Do not create a Workshop domain event, ledger operation, background
job, or cache purge for this presentation marker.

The current PublicAccount repository's `UpdateAccountAsync` performs a full
document replacement. Do not use that method for the marker: add an atomic
field-level advance operation so concurrent requests cannot downgrade the seen
version. The existing profile update also performs a read/replace cycle; move
its mutable profile fields to a targeted repository update in the same bounded
change, or otherwise prove that a concurrent profile save cannot overwrite the
new marker. This is required correctness work tightly coupled to adding an
independently mutable account field.

#### Automatic launch lifecycle

Define one client-owned current guide version, initially 1.

When `AppShell` receives an authenticated session:

1. compare `session.account.workshopCapabilityGuideSeenVersion` with the current
   guide version;
2. if the stored version is lower, render the modal and request the idempotent
   mark-seen mutation;
3. after success, update the authenticated session snapshot to the returned
   version so route navigation or rerender cannot relaunch it;
4. if persistence fails, keep the modal available, show a clear retryable
   status, and do not manufacture a successful server state.

The automatic guide may be closed while a retry remains unavailable, but the
failure is session-local only and the server may show it again on a later
login. Do not fall back to localStorage as success-shaped persistence.

Manual replay ignores the seen comparison, never lowers or rewrites the server
version, and starts at step one. Changing authenticated account identity resets
all local modal state and reevaluates the new account's server marker.

#### Existing onboarding transition

Remove the empty-workspace condition, inline Dashboard guide, and dashboard
reopen banner. `AppShell` becomes the single owner because first authenticated
presentation and top-right replay are shell behavior.

During the rollout compatibility window:

- legacy account documents missing the field read as seen version 1 without a
  data rewrite;
- registration explicitly persists version 0 for new accounts;
- existing browser-local `onboardingState` and `onboardingStep` remain readable
  until the new client ships, but the new wizard does not use them as
  authoritative state;
- unrelated dashboard period, history filter, goal, and recipe preferences
  remain unchanged.

After the rollout is stable, obsolete onboarding fields may remain tolerated in
the version-1 browser preference object. Removing them is optional cleanup, not
part of this feature.

#### Shell launch control

Add a dedicated top-right icon button between the account link and
install/sign-out controls. It remains visible for every authenticated account,
uses the existing `button button-quiet icon-button` treatment, and has complete
English/Italian `aria-label` and `title` text such as **Open Workshop guide**.
Extend the local inline SVG set with a neutral guide/help symbol; add no icon
dependency.

The launcher opens the shell-owned modal from any authenticated route.
Navigating through a wizard action closes the modal and uses an existing route.
No wizard route is added.

#### Four-step capability sequence

1. **Welcome to Workshop** — explain private account-owned recordkeeping for
   equipment, ammunition, and activity history; state that the guide takes
   about one minute and can be replayed from the header.
2. **Equipment and maintenance records** — explain firearms, physical
   components, installation/history, and factual maintenance entries. Link to
   `/firearms`; do not imply registry, inspection, safety, or replacement
   authority.
3. **Ammunition and recipes** — explain user-authored recipes, production
   records, batches, and available stock. Link to `/ammunition`; do not provide
   load recommendations, ballistic calculations, or safety validation.
4. **Record and review activity** — explain shooting-session capture,
   correction-aware history, dashboard summaries, reports, and integrity
   review. Offer `/sessions/shooting` and `/history`, then allow Finish.

Each step has concise explanatory copy, capability bullets, and at most two
existing-route actions. Back and Next control progression. Skip is always
visible. Route actions never mutate Workshop data.

#### Modal and responsive behavior

This is an Impeccable **Operate**-mode `onboard` refinement. Preserve the
current black/white/yellow Workshop world. Do not add Bootstrap JavaScript,
React-Bootstrap, an external tour library, or a shared-package component.

Reuse the feedback dialog as the interaction reference:

- one full-viewport backdrop above shell and floating launchers;
- wide desktop dialog with bounded viewport height;
- independently scrollable step body;
- title, step count, close action, and persistent footer controls;
- background inerting and `aria-hidden`;
- initial focus, Tab containment, Escape/backdrop handling, and focus return;
- reduced-motion-safe transitions with no autoplay.

At narrow widths the dialog uses nearly the full viewport and stacked actions.
At short heights its controls remain reachable while content scrolls. The
feedback and wizard dialogs must not stack or leave the shell inert after
closing.

### Impacted Projects

- `MorWalPizVideo.Models`: additive PublicAccount seen-version field.
- `MorWalPizVideo.Domain`: monotonic account-service mutation and repository
  persistence.
- `MorWalPiz.Contracts`: additive session/summary field and mark-seen
  request/response contracts.
- `MorWalPizVideo.ServerAPI`: authenticated Workshop mark-seen endpoint.
- `MorWalPizVideo.BackOffice.Tests`: account contract, endpoint,
  authorization, CSRF, compatibility, and persistence coverage.
- `@morwalpizvideo/models`: additive PublicAccount summary field.
- `@morwalpizvideo/services`: typed mark-seen helper using account-mode
  credentials and CSRF.
- `frontend/shooter-workshop.client`: shell integration, modal, replay,
  localization, styling, and tests.
- `frontend/shooter-workshop.client` container/proxy: same-origin `/api`
  transport verification; no topology change.
- Documentation: Workshop, account/data compatibility, and rollout records.
- BackOffice SPA, public application UI, FreezerAnalysis UI, shared layout,
  AppHost topology, and deployment units are verify-only consumers or
  unaffected.

### Impacted Files

#### Modify

- `MorWalPizVideo.Models/Models/PublicAccount.cs`: add the BSON-compatible seen
  version with tolerant legacy default.
- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs`: expose a monotonic,
  account-owned mark-seen operation.
- The existing PublicAccount repository interface and Mongo/mock
  implementations under `MorWalPizVideo.Domain/Interfaces`: add an atomic,
  monotonic marker update and a targeted profile update that cannot overwrite
  independently mutable account fields.
- `MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs`: add the seen
  version to authenticated account summary and define the Workshop mark-seen
  request/response.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`: add the
  authenticated, CSRF-protected current-account endpoint while preserving all
  domain routes.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs`: map the
  additive version in registration, login, and restored-session summaries.
- PublicAccount mock/scenario seed data: explicitly represent existing-account
  and first-use version states.
- `frontend/fe-packages/models/src/publicAccount.ts` and its barrel export:
  expose the additive seen version and endpoint DTOs.
- `frontend/fe-packages/services/src/publicAccountService.ts` or the established
  Workshop service module: add the typed mark-seen call through account-mode
  transport and CSRF.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx`: provide a narrow
  way to update the in-memory authenticated account summary after successful
  marking without re-authenticating.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx`: own automatic
  launch/replay, persistence state, account switching, error/retry status, and
  the top-right guide button.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx`: remove
  empty-workspace onboarding and its reopen banner.
- `frontend/shooter-workshop.client/src/services/workshopPreferences.ts`:
  preserve unrelated fields and tolerate obsolete onboarding fields during the
  compatibility window.
- `frontend/shooter-workshop.client/src/i18n.tsx` and `src/main.scss`: add
  translated wizard/endpoint-state copy and wide responsive presentation.
- Existing PublicAccount, Workshop route, AuthProvider, Dashboard,
  preferences, configuration, and feedback dialog tests: extend focused
  coverage without weakening current assertions.

Retire
`frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx`
after its factual content and route actions move to the shell-level component.
This retirement is now complete.

#### Add

- `frontend/shooter-workshop.client/src/components/CapabilityWizard.tsx`:
  controlled four-step accessible modal.
- `frontend/shooter-workshop.client/src/components/CapabilityWizard.test.tsx`:
  focused component, state, keyboard, and localization coverage.
- A focused backend endpoint test in
  `MorWalPizVideo.BackOffice.Tests/Features/` following current PublicAccount
  and Workshop authentication fixtures.

#### Generated/Migration

- No EF migration.
- No MongoDB data backfill. Missing fields must deserialize as legacy version 1,
  while the registration path explicitly persists version 0 for new accounts.
- No index is required for a field read and updated only by account ID.

#### Documentation

- `docs/architecture/shooter-workshop.md`: current shell onboarding behavior,
  account marker semantics, and rollout status after implementation.
- `docs/features/accounts-and-channels.md`: additive PublicAccount contract/data
  compatibility and shared-consumer impact.
- `docs/architecture/domain-model.md`: PublicAccount presentation marker and
  non-authorization semantics.
- `docs/feature-handoffs/shooter-workshop-nice-to-haves-handoff.md`: supersede
  the inline browser-local guide delivery record with actual wizard behavior.
- This handoff: append actual files, validation, migration evidence, and
  residual risks, then consolidate and retire.

#### Verify Only

- Public client and FreezerAnalysis consumers of the additive PublicAccount
  summary ignore the new field without behavior changes.
- `frontend/shooter-workshop.client/src/routes/config.tsx`: wizard remains
  inside authenticated `AppShell`, not login/register/legal routes.
- Feedback and install prompt components/services retain their current
  behavior.
- PublicAccount session cookie, CSRF, CORS, security version, legal
  acknowledgement, and expiry contracts remain unchanged.
- `MorWalPizVideo.ServerAPI/Program.cs`: existing account/Workshop DI,
  authentication, CORS, MongoDB, and controller registration remain sufficient.
- `frontend/shooter-workshop.client/Dockerfile`,
  `nginx.conf.template`, and `vite.config.ts`: preserve relative `/api`
  production proxy and local development proxy behavior.
- AppHost and deployment workflows require no topology or secret changes.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Additive account summary field and one authenticated, CSRF-protected Workshop mark-seen write. |
| Frontend/UI | Existing dashboard guide becomes a wide shell modal with top-right replay. |
| Database/Data | Additive version field; missing legacy field reads as 1 and new registration explicitly writes 0. No backfill or index. |
| Contracts/Models | Coordinated .NET and TypeScript additive account/endpoint contracts. |
| Authentication/Authorization | Existing PublicAccount cookie identity only; marker never authorizes. |
| Validation | Supported version bounds, monotonic update, current-account ownership, and explicit failure handling. |
| Cache | No output/application cache; session snapshot updates locally after mutation. |
| Configuration/Secrets | One source-owned current guide version; no secret or environment setting. |
| Logging/Telemetry | Structured mutation failure/outcome without account PII or modal content. |
| Background Jobs | Not applicable; synchronous idempotent account update. |
| Deployment/Operations | Backend/contracts before client; same-origin proxy smoke evidence required. |
| Backward Compatibility | Missing fields read as 1, new registrations persist 0, and other consumers ignore additive data. |

### Reuse Opportunities

- Reuse PublicAccount repository/service ownership and session-summary mapping.
- Reuse Workshop authenticated controller, account identity, CSRF, account-mode
  frontend transport, and the production same-origin `/api` reverse proxy.
- Reuse existing mock PublicAccount scenarios and WebApplicationFactory
  authentication tests.
- Reuse `AppShell`, header icon pattern, existing canonical routes, current
  onboarding copy boundaries, Workshop preferences for unrelated presentation
  settings, and English/Italian localization.
- Reuse feedback-dialog accessibility behavior and current responsive theme.

### Risks

- **High — legacy/default inversion:** the wrong serializer default could show
  the wizard to every existing account or suppress it for new registrations.
  Mitigation: missing BSON fields read as 1 and registration explicitly writes
  0, with serialization and HTTP tests for both paths. Signal: an old fixture
  returns 1 while a newly registered account returns 0.
- **High — marker set without presentation:** marking before the modal renders
  could permanently suppress onboarding after a client failure. Mitigation:
  render/open first, then submit mark-seen; report mutation failure explicitly.
  Signal: render-failure and endpoint-failure tests never manufacture version 1.
- **Medium — additive contract leakage:** Workshop metadata appears in a shared
  account summary. Mitigation: one optional numeric field, ignored by other
  consumers, documented as non-security presentation metadata. Signal: public
  and Freezer account tests remain unchanged/passing.
- **Medium — concurrent or stale clients downgrade state:** a stale client could
  overwrite a newer version. Mitigation: server uses monotonic max semantics
  and rejects unsupported future versions. Signal: repeat/lower/concurrent
  endpoint tests retain the highest supported version.
- **High — profile replacement loses the marker:** the existing profile
  read/replace path can race an atomic guide update and restore an older value.
  Mitigation: use targeted atomic updates for both the marker and mutable profile
  fields. Signal: concurrent profile/guide tests preserve both changes.
- **Medium — proxy-only deployment defect:** unit tests may pass while the
  same-origin nginx path fails to forward cookies, CSRF headers, or the new
  route. Mitigation: container/proxy browser smoke through `/api`, not only
  direct ServerAPI tests. Signal: registration/session, mark-seen, refresh, and
  replay succeed from the Workshop origin without direct cross-origin calls.
- **Medium — persistence failure repeats onboarding:** failed writes may show
  the wizard on a later login. Mitigation: explicit retry state and no silent
  local success. Signal: failure test keeps server/session version unchanged.
- **Medium — firearm/ammunition claims:** capability copy could imply registry,
  safety, or ballistic advice. Mitigation: factual recordkeeping copy and
  established product boundaries. Signal: copy review contains no prohibited
  identifiers, recommendations, or conclusions.
- **Medium — competing modal focus:** feedback and wizard may stack or leave
  content inert. Mitigation: focused coexistence tests and browser keyboard
  review. Signal: one active dialog and clean focus/inert restoration.
- **Low — header crowding:** another icon may overflow narrow screens.
  Mitigation: existing icon-only pattern and 320/375 px checks.

### Technical Debt

- PublicAccount is shared across several applications; the Workshop-specific
  field is an intentional small coupling chosen to satisfy account-roaming
  first-use behavior without a new preferences collection. Do not expand it
  into a general product-settings bag.
- Existing browser onboarding fields become legacy compatibility data. Leave
  them tolerated rather than broadening this feature into local-storage
  migration cleanup.
- Feedback modal focus management is local rather than a reusable primitive.
  Do not introduce a general modal framework unless the narrow wizard work
  cannot meet accessibility requirements without it.

### Implementation Roadmap

1. **Add the compatible account contract.** Introduce the version field in the
   model, .NET/TypeScript summaries, mappings, mocks, and consumer tests. Gate:
   missing-field accounts deserialize as one, new registrations persist zero,
   and existing consumers pass.
2. **Implement monotonic account persistence.** Add the Domain service/repository
   field-level operation, replace the conflicting profile full-document update
   with targeted mutable-field persistence, and test current, repeat, lower,
   future, missing, and concurrent updates. Gate: no downgrade, lost profile
   change, marker loss, or cross-account mutation is possible.
3. **Expose the Workshop mark-seen endpoint.** Reuse cookie authentication,
   account claims, CSRF, account-mode transport, no-store response behavior, and
   structured errors. Gate: authenticated success plus anonymous, CSRF,
   unsupported-version, inactive-account, and idempotency cases.
4. **Build the capability modal.** Move/refine the four-step content into a
   controlled global component with full localization, canonical route actions,
   and accessible dialog behavior. Apply Impeccable `onboard` in Operate mode.
   Gate: component tests cover progression, replay, dismissal, focus, error/retry,
   and copy boundaries.
5. **Integrate with AuthProvider and AppShell.** Auto-open from server version,
   mark only after presentation, update the in-memory session on success, reset
   on account change, and add top-right replay. Gate: registration, login, and
   restored sessions produce consistent behavior.
6. **Retire dashboard-only onboarding.** Remove the empty-workspace panel and
   browser-local reopen logic without changing dashboard data, goals, history,
   or recipe preferences.
7. **Validate transport, consumers, and UX.** Prove the new endpoint through
   the Workshop container's same-origin `/api` proxy, then run
   backend/shared/client focused tests, full
   Workshop and PublicAccount suites, shared package builds in dependency order,
   the Workshop production build, and browser evidence at 320, 375, 768, 1024,
   and 1440 px.
8. **Document actual rollout.** Record compatibility behavior, proxy evidence,
   real files, validation, deployed status, and residual risks. Stop without analytics,
   feature-specific tours, a generic settings bag, or unrelated account fields.

Because this change crosses shared identity, persistence, API contracts, and a
production proxy boundary, an independent post-validation review is warranted before
production activation.

### Testing Strategy

- **Model/serialization:** missing BSON field defaults to one; explicit new
  account version zero and version round-trip; no change to account
  security/legal fields.
- **Domain/repository:** current-account update, idempotent repeat, lower-version
  no-op, unsupported future rejection, profile/marker concurrency,
  cancellation, missing and inactive accounts.
- **HTTP/security:** registration/login/session expose the additive version;
  mark-seen requires authenticated PublicAccount and CSRF; anonymous,
  cross-account, malformed, and unsupported requests fail explicitly; response
  is no-store.
- **Shared frontend contracts/services:** typed parsing, CSRF transport,
  successful response, Problem Details, network failure, and no admin/channel
  header leakage.
- **AuthProvider/shell:** version-zero automatic presentation, version-one
  suppression, success snapshot update, failure/retry, registration, login,
  restored session, account switch, every protected route, and replay.
- **Wizard component:** four translated steps, progress, back/next, skip, close,
  Escape, backdrop, finish, replay, route actions, focus containment/return,
  and prohibited-claim review.
- **Dashboard/preferences regression:** inline guide removed; dashboard,
  analytics, goals, history filters, recipes, and existing preferences remain.
- **Dialog regression:** feedback and wizard never stack and leave no residual
  inert or `aria-hidden` state.
- **Compatibility:** legacy missing-field fixture returns version 1; newly
  registered account returns 0; no database rewrite is required.
- **Proxy/integration:** run registration, session restoration, mark-seen,
  CSRF failure, refresh, and manual replay through the container's same-origin
  `/api` proxy with no direct ServerAPI origin dependency.
- **Consumer/build:** PublicAccount endpoint suite, focused Workshop backend
  tests, shared models/services builds, full Workshop Vitest suite, and
  TypeScript/Vite production build.
- **Visual/accessibility:** authenticated captures at 320, 375, 768, 1024, and
  1440 px plus a short-height viewport; keyboard traversal, reduced motion,
  zoom/reflow, contrast, scrolling, and dialog naming.

### Migration And Rollback

MongoDB compatibility is additive and requires no data rewrite. Missing
seen-version fields on historical documents read as version 1. The registration
path explicitly writes version 0 for new accounts, which advances to version 1
only after the Workshop renders the guide and the mark-seen request succeeds.

Deployment order:

1. deploy model/domain/contract/API support while the old client ignores it;
2. verify legacy missing-field sessions return 1 and new registration returns 0;
3. deploy shared frontend packages and the Workshop client/container;
4. verify registration, restored existing account, mutation, refresh, and replay
   through the production-equivalent same-origin `/api` proxy;
5. monitor endpoint failures and unexpected version-zero accounts after guide
   presentation.

Rollback order is client first, then API/contracts if necessary. The additive
field and version-1 values may remain safely in MongoDB. Do not roll values back
to zero because that would re-trigger automatic onboarding. Preserve the
legacy-missing-as-one rule through any rolling rollback so old binaries and old
documents remain compatible.

No EF, cache, auth-cookie, service-worker, secret, or topology migration is
required.

### Open Questions

None identified. The user approved preserving existing skipped/completed users
and requested account-roaming first-use state; the versioned seen marker is the
recommended account-level implementation.
