# Shooter Workshop Capability Wizard Handoff

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

- Existing accounts that predate activation are marked as having seen version
  1 before the client is released. They are not interrupted and can use the
  header replay button.
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

### Proposed Design

#### Account marker

Add an integer PublicAccount field conceptually named
`WorkshopCapabilityGuideSeenVersion`, serialized additively with a default of
zero for missing legacy data.

Use a version rather than a boolean or timestamp:

- `0` means no server-recorded Workshop capability guide;
- `1` means version 1 has been presented;
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

- existing pre-activation accounts are backfilled to seen version 1;
- new accounts created after the agreed activation cutoff remain at version 0;
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
- MongoDB operations: idempotent pre-activation PublicAccount backfill.
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
  implementations under `MorWalPizVideo.Domain/Interfaces`: persist the
  account update using the established account write path.
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

#### Add

- `frontend/shooter-workshop.client/src/components/CapabilityWizard.tsx`:
  controlled four-step accessible modal.
- `frontend/shooter-workshop.client/src/components/CapabilityWizard.test.tsx`:
  focused component, state, keyboard, and localization coverage.
- A focused backend endpoint test in
  `MorWalPizVideo.BackOffice.Tests/Features/` following current PublicAccount
  and Workshop authentication fixtures.
- An operator-reviewed Mongo backfill record in the existing operations
  documentation; add a source-owned migration utility only if current
  operational tooling requires one.

#### Generated/Migration

- No EF migration.
- MongoDB requires an idempotent data backfill for pre-activation
  PublicAccounts. It sets seen version 1 only for accounts created before the
  agreed activation cutoff and must be safe to audit and rerun.
- Missing BSON fields remain readable as version 0 before backfill.
- No index is required for a field read and updated only by account ID.

#### Documentation

- `docs/architecture/shooter-workshop.md`: current shell onboarding behavior,
  account marker semantics, and rollout status after implementation.
- `docs/features/accounts-and-channels.md`: additive PublicAccount contract/data
  compatibility and shared-consumer impact.
- `docs/architecture/domain-model.md`: PublicAccount presentation marker and
  non-authorization semantics.
- `docs/architecture/mongo-operations.md`: cutoff, audit, apply, verification,
  and recovery procedure for the backfill.
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
- AppHost and deployment workflows require no topology or secret changes.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Additive account summary field and one authenticated, CSRF-protected Workshop mark-seen write. |
| Frontend/UI | Existing dashboard guide becomes a wide shell modal with top-right replay. |
| Database/Data | Additive version field on PublicAccount plus cutoff-based idempotent backfill. |
| Contracts/Models | Coordinated .NET and TypeScript additive account/endpoint contracts. |
| Authentication/Authorization | Existing PublicAccount cookie identity only; marker never authorizes. |
| Validation | Supported version bounds, monotonic update, current-account ownership, and explicit failure handling. |
| Cache | No output/application cache; session snapshot updates locally after mutation. |
| Configuration/Secrets | One source-owned current guide version; no secret or environment setting. |
| Logging/Telemetry | Structured mutation failure/outcome without account PII or modal content. |
| Background Jobs | Not applicable; synchronous idempotent account update and controlled backfill. |
| Deployment/Operations | Backend/contracts, cutoff backfill, then client in dependency order. |
| Backward Compatibility | Missing fields read as zero; pre-activation accounts are backfilled to one; consumers ignore additive data. |

### Reuse Opportunities

- Reuse PublicAccount repository/service ownership and session-summary mapping.
- Reuse Workshop authenticated controller, account identity, CSRF, CORS, and
  account-mode frontend transport.
- Reuse existing mock PublicAccount scenarios and WebApplicationFactory
  authentication tests.
- Reuse `AppShell`, header icon pattern, existing canonical routes, current
  onboarding copy boundaries, Workshop preferences for unrelated presentation
  settings, and English/Italian localization.
- Reuse feedback-dialog accessibility behavior and current responsive theme.

### Risks

- **High — rollout marks the wrong population:** an incorrect cutoff could hide
  the wizard from new accounts or interrupt old ones. Mitigation: record one UTC
  activation cutoff, dry-run counts, make the backfill idempotent, and verify
  samples on both sides. Signal: pre-cutoff accounts report version 1 and
  post-cutoff registrations remain 0 before first Workshop presentation.
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
   missing-field accounts deserialize as zero and existing consumers pass.
2. **Implement monotonic account persistence.** Add the Domain service/repository
   operation and focused tests for current, repeat, lower, future, missing, and
   concurrent updates. Gate: no downgrade or cross-account mutation is possible.
3. **Expose the Workshop mark-seen endpoint.** Reuse cookie authentication,
   account claims, CSRF, account-mode transport, no-store response behavior, and
   structured errors. Gate: authenticated success plus anonymous, CSRF,
   unsupported-version, inactive-account, and idempotency cases.
4. **Prepare the rollout population.** Approve one UTC activation cutoff, audit
   pre/post-cutoff counts, and apply the idempotent pre-activation version-1
   backfill. Gate: recorded audit/apply evidence before client activation.
5. **Build the capability modal.** Move/refine the four-step content into a
   controlled global component with full localization, canonical route actions,
   and accessible dialog behavior. Apply Impeccable `onboard` in Operate mode.
   Gate: component tests cover progression, replay, dismissal, focus, error/retry,
   and copy boundaries.
6. **Integrate with AuthProvider and AppShell.** Auto-open from server version,
   mark only after presentation, update the in-memory session on success, reset
   on account change, and add top-right replay. Gate: registration, login, and
   restored sessions produce consistent behavior.
7. **Retire dashboard-only onboarding.** Remove the empty-workspace panel and
   browser-local reopen logic without changing dashboard data, goals, history,
   or recipe preferences.
8. **Validate consumers and UX.** Run backend/shared/client focused tests, full
   Workshop and PublicAccount suites, shared package builds in dependency order,
   the Workshop production build, and browser evidence at 320, 375, 768, 1024,
   and 1440 px.
9. **Document actual rollout.** Record cutoff, migration evidence, real files,
   validation, deployed status, and residual risks. Stop without analytics,
   feature-specific tours, a generic settings bag, or unrelated account fields.

Because this change crosses shared identity, persistence, API contracts, and a
data backfill, an independent post-validation review is warranted before
production activation.

### Testing Strategy

- **Model/serialization:** missing BSON field defaults to zero; version
  round-trip; no change to account security/legal fields.
- **Domain/repository:** current-account update, idempotent repeat, lower-version
  no-op, unsupported future rejection, concurrency, cancellation, missing and
  inactive accounts.
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
- **Migration:** dry-run and apply counts around the UTC cutoff, rerun
  idempotency, missing-field behavior, pre-cutoff version 1, and post-cutoff
  version 0.
- **Consumer/build:** PublicAccount endpoint suite, focused Workshop backend
  tests, shared models/services builds, full Workshop Vitest suite, and
  TypeScript/Vite production build.
- **Visual/accessibility:** authenticated captures at 320, 375, 768, 1024, and
  1440 px plus a short-height viewport; keyboard traversal, reduced motion,
  zoom/reflow, contrast, scrolling, and dialog naming.

### Migration And Rollback

MongoDB compatibility is additive. Missing seen-version fields read as zero.
Before the new client is activated, run an idempotent backfill that sets version
1 for PublicAccounts created before the approved UTC activation cutoff. New
accounts at or after the cutoff remain zero until the Workshop presents version
1 and the endpoint records it.

Deployment order:

1. deploy model/domain/contract/API support while the old client ignores it;
2. audit and apply the cutoff-based backfill;
3. deploy shared frontend packages and Workshop client;
4. verify new registration, restored existing account, mutation, and replay;
5. monitor endpoint failures and unexpected version-zero counts.

Rollback order is client first, then API/contracts if necessary. The additive
field and version-1 values may remain safely in MongoDB. Do not roll values back
to zero because that would re-trigger automatic onboarding. If the cutoff was
wrong, stop client activation and correct the reviewed account set before
continuing; code rollback alone cannot infer the intended population.

No EF, cache, auth-cookie, service-worker, secret, or topology migration is
required.

### Open Questions

None identified. The user approved preserving existing skipped/completed users
and requested account-roaming first-use state; the versioned seen marker is the
recommended account-level implementation.
