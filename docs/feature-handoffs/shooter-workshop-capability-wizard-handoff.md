# Shooter Workshop Capability Wizard Handoff

### Outcome

Replace the existing dashboard-only first-use setup guide with a wide,
multi-step Shooter Workshop capability wizard. It opens automatically after the
first successful authenticated Workshop shell load, remains optional, and can
always be reopened from a button in the top-right account controls.

The wizard explains what the Workshop can do, the relationship between its
main areas, and the safest useful next actions. It reuses the current
account-keyed browser presentation preferences and existing routes; it does not
create a second onboarding system, tutorial-only data, or a server-side
completion contract.

Assumptions:

- "First login" means the first authenticated Workshop shell load for an
  account in that browser profile. Cross-device completion is not required.
- Existing users who completed or skipped the current setup guide are not
  interrupted after this refinement; they can launch the new capability wizard
  from the header.
- New or still-active onboarding state opens the wizard regardless of whether
  the account already contains Workshop records. Completion is presentation
  state, not evidence that a business workflow was performed.

### Current Evidence

- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` is the persistent
  authenticated shell. It restores the PublicAccount session, redirects
  anonymous users, renders the top-right language/account/install/logout
  controls, and owns the primary Dashboard, Equipment, Ammunition, Recordings,
  and History navigation.
- The top-right controls are direct icon/link actions rather than a dropdown.
  The install prompt already establishes a replay button in this exact area.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx`
  implements a four-step setup panel with canonical links for firearms,
  components, recipes, shooting, and production.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx` shows
  that guide only when the workspace is empty and the account-keyed
  `onboardingState` is active. A skipped guide can be reopened only from the
  empty dashboard.
- `frontend/shooter-workshop.client/src/services/workshopPreferences.ts`
  persists `onboardingState` and `onboardingStep` under
  `workshop.presentation.v1:<accountId>`. Invalid values fall back to active
  step zero, storage failures are logged, and preferences are isolated by
  authenticated account ID.
- The current onboarding state already supports `active`, `skipped`, and
  `completed` with step values zero through four, matching a four-step wizard
  plus completion.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx`
  establishes the application-local dialog behavior: backdrop dismissal,
  dialog semantics, background inerting, initial focus, Escape, Tab
  containment, and focus restoration.
- `frontend/shooter-workshop.client/src/main.scss` owns the black/white/yellow
  Workshop shell, header icon controls, onboarding presentation, dialog
  backdrop, responsive breakpoints, and focus-visible treatment.
- `frontend/shooter-workshop.client/src/i18n.tsx` owns English source strings
  and Italian translations for the shell and current setup guide.
- The current guided onboarding is documented as implemented in
  `docs/feature-handoffs/shooter-workshop-nice-to-haves-handoff.md`; its
  historical validation reported four focused tests, 36 full-suite tests, and
  a successful application build, with browser visual review still open.

### Proposed Design

#### Existing onboarding transition

Evolve the existing onboarding state instead of adding another preference:

- retain `onboardingState: active | skipped | completed`;
- retain `onboardingStep: 0 | 1 | 2 | 3 | 4`;
- reinterpret the four presentation steps as the capability wizard;
- preserve existing `skipped` and `completed` states so deployed users are not
  forced through a replacement tour;
- treat existing `active` state as unfinished first-use onboarding and open the
  wizard at the saved step.

Remove the empty-workspace condition and the inline Dashboard panel/reopen
banner. `AppShell` becomes the single owner because first-login behavior and
top-right replay are shell responsibilities, not dashboard data behavior.

Automatic dismissal from an active first-use wizard records `skipped` and the
current step. Finishing records `completed` and step four. Manual replay starts
at the first step but closing it preserves the prior skipped/completed state;
replaying must not accidentally turn a completed user back into active
onboarding.

#### Shell ownership and launch

After `AppShell` has an authenticated session and `session.account.id`, it reads
the existing Workshop presentation preferences once for that account and opens
the wizard when `onboardingState` is active. The shell must reset its local
wizard state if the authenticated account changes without a full page reload.

Add a dedicated top-right icon button between the account link and install/sign
out controls. It remains visible to every authenticated account, uses the
existing `button button-quiet icon-button` treatment, and has translated
`aria-label` and `title` text such as **Open Workshop guide**. Extend the local
header SVG set with a neutral guide/help symbol; do not add an icon dependency.

The launcher opens the shell-owned modal from any authenticated route.
Navigating through a wizard action closes the modal and uses the existing
canonical route. No route is added for the wizard itself.

#### Four-step capability sequence

Keep the flow short enough for optional first-login use:

1. **Welcome to Workshop** — explain that it is private, account-owned
   recordkeeping for equipment, ammunition, and activity history; state that
   the guide takes about one minute and can be reopened from the header.
2. **Equipment and maintenance records** — describe firearms, physical
   components, installation/history, and factual maintenance entries. Link to
   `/firearms`; do not imply registry, inspection, safety, or replacement
   authority.
3. **Ammunition and recipes** — describe user-authored recipes, production
   records, batches, and available stock. Link to `/ammunition`; do not provide
   load recommendations, ballistic calculations, or safety validation.
4. **Record and review activity** — describe shooting-session capture,
   correction-aware history, dashboard summaries, and report/integrity access.
   Offer existing `/sessions/shooting` and `/history` paths, then allow Finish.

Each step has a concise title, one short explanation, relevant capability
bullets, and at most two existing-route actions. Back and Next remain the
primary progression. Skip is always visible. Route actions are optional
shortcuts, not required tasks, and never mutate Workshop data.

#### Modal and responsive behavior

This is an Impeccable **Operate**-mode `onboard` refinement. Preserve the
current Workshop visual world and its black/white/yellow hierarchy. Do not
introduce Bootstrap JavaScript, React-Bootstrap, an external tour library, or a
new shared-package component.

Reuse the existing feedback-dialog interaction contract in a Workshop-local
wizard component:

- one full-viewport backdrop above shell and feedback-launcher layers;
- wide desktop dialog, approximately the existing content width rather than a
  narrow form modal;
- bounded viewport height with an independently scrollable body;
- header with title, step count, and close action;
- one active step at a time;
- footer with Skip, Back, Next, Finish, and optional route actions;
- background inerting and `aria-hidden` while open;
- initial focus, Tab/Shift+Tab containment, Escape/backdrop dismissal, and
  focus restoration to the top-right launcher;
- reduced-motion-safe transitions with no autoplay or carousel behavior.

At narrow widths the dialog uses nearly the full viewport and stacks content
and actions without horizontal overflow. At short heights the header/footer
remain reachable while the step body scrolls. The launcher remains usable when
the account-name link is hidden by the current mobile breakpoint.

The feedback and wizard dialogs must not stack. Their existing z-index,
backdrop, inert, and focus behavior must be verified together; opening one must
make the other launcher unreachable until the active dialog closes.

### Impacted Projects

- `frontend/shooter-workshop.client`: existing onboarding ownership,
  authenticated shell, modal component, account-keyed preference use,
  localization, responsive styling, and tests.
- `docs`: update the Workshop architecture and nice-to-haves delivery record
  after implementation.
- Shared frontend packages, ServerAPI, Models, Domain, Contracts, MongoDB,
  AppHost, service worker, and deployment topology are not changed.

### Impacted Files

#### Modify

- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx`: own automatic and
  replay launch state, add the top-right guide button, handle account changes,
  and host the modal.
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx`: remove
  empty-workspace onboarding state, inline guide, and dashboard-only reopen
  banner while preserving dashboard analytics and goals.
- `frontend/shooter-workshop.client/src/services/workshopPreferences.ts`:
  preserve the existing onboarding fields and expose any narrowly needed
  helpers without changing the storage key or other preferences.
- `frontend/shooter-workshop.client/src/i18n.tsx`: replace obsolete setup-panel
  copy and add complete English/Italian wizard and launcher strings.
- `frontend/shooter-workshop.client/src/main.scss`: replace inline onboarding
  panel styles with wide responsive wizard styles and retain current focus and
  theme tokens.
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.test.tsx`:
  relocate/replace its assertions with modal behavior and shell integration.
- `frontend/shooter-workshop.client/src/services/workshopPreferences.test.ts`:
  preserve account isolation, invalid-state fallback, and old
  completed/skipped compatibility.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx`: preserve route
  and authenticated-shell behavior with the global wizard host.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.test.tsx`:
  add only the regression needed to prove the two dialog surfaces do not stack
  or break focus/inert cleanup.

The dashboard-owned
`frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx` is
retired after its content and state behavior move to the global component.

#### Add

- `frontend/shooter-workshop.client/src/components/CapabilityWizard.tsx`:
  controlled four-step dialog, local step navigation, existing-route actions,
  first-use completion handling, and accessible modal behavior.
- `frontend/shooter-workshop.client/src/components/CapabilityWizard.test.tsx`:
  focused first-use, replay, navigation, state, localization, keyboard, and
  dismissal coverage.

#### Generated/Migration

None. Existing browser preferences are read compatibly; no lockfile, API,
MongoDB, EF, or generated artifact changes are required.

#### Documentation

- `docs/architecture/shooter-workshop.md`: after implementation, record the
  shell-level first-use/replay behavior and browser-local compatibility.
- `docs/architecture/frontend.md`: update only if the documented Workshop
  onboarding ownership needs a durable shell-level note.
- `docs/feature-handoffs/shooter-workshop-nice-to-haves-handoff.md`: replace the
  inline empty-dashboard guide delivery description with the actual wizard
  behavior and validation.
- `docs/feature-handoffs/shooter-workshop-capability-wizard-handoff.md`: append
  actual files, validation, visual evidence, and residual risks after delivery,
  then consolidate and retire it.

#### Verify Only

- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx`: remain the
  source of authenticated `session.account.id`; no auth contract changes.
- `frontend/shooter-workshop.client/src/routes/config.tsx`: keep the wizard
  inside the authenticated `AppShell`, not login/register/legal routes.
- `frontend/shooter-workshop.client/src/components/feedback/FeedbackProvider.tsx`:
  retain current feedback behavior unless the smallest dialog-coordination fix
  is required.
- `frontend/shooter-workshop.client/src/components/InstallPrompt.tsx` and
  `src/services/installPrompt.ts`: preserve install invitation and replay.
- `frontend/shooter-workshop.client/package.json`: reuse current React, router,
  Sass, Vitest, jsdom, and Testing Library dependencies.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Not applicable; no endpoint or transport change. |
| Frontend/UI | Existing dashboard guide becomes a global wide shell modal with a top-right replay button. |
| Database/Data | Existing account-keyed browser presentation state only. |
| Contracts/Models | No shared contract; local component props and step metadata only. |
| Authentication/Authorization | Uses authenticated PublicAccount session ID; never authorizes or changes route protection. |
| Validation | Preserve old state compatibility and handle missing/invalid/unavailable storage without blocking the shell. |
| Cache | Not applicable; no service-worker or response caching. |
| Configuration/Secrets | No configuration or secret changes. |
| Logging/Telemetry | Existing storage warnings only; no account ID, completion analytics, or telemetry added. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Shooter Workshop client artifact only; existing deployment path. |
| Backward Compatibility | Existing skipped/completed users are not interrupted; active/new users receive the modal. |

### Reuse Opportunities

- Reuse `WorkshopPresentationPreferences`, its account-keyed storage, and
  current active/skipped/completed semantics.
- Reuse `AppShell` session, navigation paths, header icon-button pattern,
  install replay placement, and account-switch lifecycle.
- Reuse the current onboarding's factual product boundaries and canonical route
  actions while replacing task-checklist presentation with capability
  explanation.
- Reuse feedback modal backdrop, inert, focus trap, Escape, and focus-return
  behavior as the accessibility reference without coupling feedback data to the
  wizard.
- Reuse current Workshop theme variables, buttons, responsive breakpoints,
  translation function, and English/Italian catalog.

### Risks

- **Medium — duplicate onboarding:** leaving the dashboard panel would create
  two competing guides. Mitigation: move ownership to `AppShell` and retire the
  dashboard guide/reopen banner. Signal: only one onboarding launcher/modal
  exists in rendered authenticated routes.
- **Medium — existing-user interruption:** re-defaulting completed/skipped
  preferences would reopen onboarding after deployment. Mitigation: retain the
  current fields and semantics. Signal: compatibility tests keep both states
  closed automatically while replay still works.
- **Medium — wrong first-login scope:** browser-local state does not roam and
  can be cleared. Mitigation: document this as presentation state and avoid
  implying server-account completion. Signal: a second browser intentionally
  opens independently.
- **Medium — firearm/ammunition claims:** capability copy could sound like
  registry, safety, or ballistic advice. Mitigation: preserve the established
  product boundaries and use factual recordkeeping language. Signal: copy
  review contains no prohibited identifiers, recommendations, or conclusions.
- **Medium — competing modal focus:** feedback and wizard overlays could stack
  or leave the shell inert. Mitigation: verify z-index, launch reachability,
  focus cleanup, and account/route transitions. Signal: focused DOM tests and
  keyboard browser review.
- **Low — header crowding:** another icon may overflow narrow devices.
  Mitigation: retain icon-only mobile action with accessible label and review
  320/375 px widths. Signal: no horizontal document overflow or clipped
  controls.
- **Low — preference regression:** broad changes to the shared presentation
  object could damage goals/history settings. Mitigation: change only
  onboarding consumers and preserve read/write shape. Signal: the full
  preference test suite remains passing.

### Technical Debt

- The existing feedback modal contains application-local focus-management logic
  rather than a reusable dialog primitive. Do not turn this wizard into a broad
  modal refactor; duplicate only the minimum behavior needed, or extract a
  shared helper solely if focused tests prove both consumers unchanged.
- Workshop's fixed navigation and onboarding routes are separately declared.
  This small wizard does not justify redesigning route metadata; keep paths
  explicit and cover them with route tests.
- Browser visual evidence for the current inline guide was never captured. The
  new modal must close that validation gap rather than inherit the unverified
  claim.

### Implementation Roadmap

1. **Lock compatibility behavior.** Extend preference tests first for active,
   skipped, completed, invalid, unavailable-storage, and two-account cases.
   Gate: current non-onboarding preferences remain byte-for-byte unaffected by
   onboarding transitions.
2. **Build the Workshop capability modal.** Move/refine the four-step content
   into a controlled global component with local replay semantics, route
   actions, accessible dialog behavior, and full localization. Apply the
   Impeccable `onboard` workflow in Operate mode. Gate: focused component tests
   cover steps, back/next, skip/close/Escape/backdrop, finish, focus, and copy
   boundaries.
3. **Move ownership to the authenticated shell.** Add automatic launch and the
   top-right replay button in `AppShell`, reset safely on account change, and
   keep anonymous routes untouched. Gate: active opens once; skipped/completed
   remain closed; manual replay works from every protected route.
4. **Retire dashboard-only onboarding.** Remove the empty-workspace gate, inline
   panel, and reopen banner without changing dashboard loaders, analytics,
   goals, or empty data. Gate: dashboard route tests and existing behavior
   remain passing.
5. **Harden coexistence and responsive behavior.** Verify install, feedback,
   language, profile, sign-out, navigation, and modal focus at narrow/short
   viewports. Gate: browser evidence at 320, 375, 768, 1024, and 1440 px with no
   overflow, clipped action, stacked modal, console error, or focus leak.
6. **Validate and document.** Run focused onboarding/preferences/shell/dialog
   tests, the full Workshop suite, and production build; update the durable
   Workshop and nice-to-haves records. Stop without server persistence,
   analytics, new dependencies, route redesign, or feature-specific tours.

An independent post-validation review is not mandatory for this client-only
refinement. It becomes warranted if implementation changes authentication,
adds server persistence, introduces a general modal framework, or alters
Workshop domain workflows.

### Testing Strategy

- **Preferences:** defaults, active resume, skipped/completed compatibility,
  invalid values, storage read/write failure, account isolation, and no changes
  to dashboard/history/goal preferences.
- **Wizard component:** four translated steps, progress, back/next boundaries,
  skip, close, Escape, backdrop, finish, manual replay, route actions, focus
  containment/return, and prohibited-claim copy review.
- **Shell integration:** automatic opening only for active authenticated
  accounts, no anonymous/login rendering, account switch, top-right launcher,
  narrow header, install coexistence, and navigation from non-dashboard routes.
- **Dashboard regression:** no inline guide or reopen banner; analytics, goals,
  empty state, and loader data remain unchanged.
- **Dialog regression:** feedback opens and closes normally before and after
  wizard use; no stacked overlays or residual inert/aria-hidden state.
- **Localization:** English keys and complete Italian values; no raw key text in
  either culture.
- **Build:** full Shooter Workshop Vitest suite followed by its TypeScript/Vite
  production build.
- **Visual/accessibility:** authenticated captures at 320, 375, 768, 1024, and
  1440 px plus a short-height viewport; keyboard-only traversal, reduced motion,
  zoom/reflow, contrast, scrolling, and screen-reader dialog naming.
- **Backend, API contract, database, security endpoint, and migration tests:**
  not applicable because the feature uses the existing authenticated session
  and browser presentation preference.

### Migration And Rollback

No MongoDB, API, auth, cache, service-worker, or configuration migration is
required. The existing preference key and onboarding fields form the
compatibility window:

- `active` opens the new wizard at the stored step;
- `skipped` and `completed` suppress automatic opening;
- missing/invalid state behaves as active for that browser/account;
- manual replay is available for every authenticated account.

Deploy as one Shooter Workshop client artifact. Rollback restores the inline
dashboard guide and removes the header launcher/modal host while leaving the
same preference values readable. No persistent business data or account session
requires recovery.

### Open Questions

1. Confirm that existing skipped/completed users should not see the replacement
   wizard automatically. The plan preserves their choice and gives them the
   top-right replay button.
2. Confirm that browser-local first-login semantics are sufficient. A
   cross-device or auditable completion requirement would require a separate
   server-backed preference design and is intentionally outside this plan.
