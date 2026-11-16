# Shooter Workshop Nice-to-Haves Handoff

**Status:** Guided onboarding, repeatable capture drafts, saved history views, presentation-only goals, connectivity retry hardening, bounded report export, and the direct recipe QR-to-production flow are implemented. Notifications and reminders are intentionally excluded from the current product direction. Backup/import and persisted analytics rollups remain gated product work.

**Owner:** Shooter Workshop product and architecture review.

**Relationship:** This handoff is intentionally separate from
[`shooter-workshop-future-improvements-handoff.md`](./shooter-workshop-future-improvements-handoff.md).
The main handoff defines the immediate capture and statistics work; this document
preserves the later candidates so they can be evaluated at the end of the current
workflow without silently expanding scope.

## Product boundary

Shooter Workshop is a private, account-owned recordkeeping application for
firearms, physical components, maintenance, recipes, ammunition production and
shooting activity. These candidates may make recording and review more convenient,
but they must not turn the product into:

- a public social or community platform;
- a legal firearm registry;
- a ballistic calculator or load recommendation engine;
- a manufacturer-certified maintenance adviser;
- a competitive training or performance-ranking service;
- an offline synchronization system without an explicit conflict and privacy design.

The following remain prohibited:

- serial numbers, matricola, licensing identifiers, or equivalent weapon
  registration data;
- community recipe sharing;
- powder-charge recommendations, load safety validation, or ballistic advice;
- inferred mechanical safety, unsafe-state, reliability, or replacement claims.

## Delivery update — guided first-use onboarding

The first sequenced candidate is now implemented as a presentation-only client
flow for empty Workshop accounts. The dashboard shows an optional four-step
guide for:

1. registering a firearm;
2. optionally adding a component;
3. optionally creating a recipe; and
4. recording a shooting session or ammunition production.

Each step links to the existing canonical route and command flow. The guide does
not introduce a wizard-specific write model, account-wide transaction, batch
selection, ammunition allocation, or safety/load recommendation. It explains
that tracked ammunition is resolved by the server and component intervals are
user-entered records rather than safety advice.

Skip, completion, and the current presentation step are stored in the existing
account-keyed browser presentation preferences. Invalid preference values fall
back to the active first step, and another account cannot read or change the
preference state. Skipped users can reopen the guide while the workspace remains
empty. The flow does not add API calls, persistent account data, service-worker
caching, or offline mutation behavior.

Implementation files:

- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.tsx`
- `frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx`
- `frontend/shooter-workshop.client/src/services/workshopPreferences.ts`
- `frontend/shooter-workshop.client/src/i18n.tsx`
- `frontend/shooter-workshop.client/src/main.scss`
- `frontend/shooter-workshop.client/src/routes/Dashboard/OnboardingGuide.test.tsx`
- `frontend/shooter-workshop.client/src/services/workshopPreferences.test.ts`

Validation completed:

- `yarn --cwd frontend workspace shooter-workshop.client test --passWithNoTests=false --run src/routes/Dashboard/OnboardingGuide.test.tsx src/services/workshopPreferences.test.ts` — 4 passed.
- `yarn --cwd frontend workspace shooter-workshop.client test --passWithNoTests=false` — 36 passed.
- `yarn --cwd frontend workspace shooter-workshop.client build` — passed.

Visual browser capture was not available in this environment; the implementation
uses the existing Workshop Operate visual system and responsive stylesheet
breakpoints. A browser-sized review remains a follow-up validation item.

## Delivery update — implementation slices across the backlog

The broader backlog was mapped in parallel by independent capture-productivity,
portability, and engagement/scaling workstreams. The following low-risk slices are
now implemented without changing the Workshop ownership or immutable-operation
boundaries:

- **Repeat session draft:** Shooting history offers “Repeat as draft”. It copies
  only the firearm, recorded recipe quantities, explicit untracked quantity, and
  notes into the canonical shooting form. It creates a new effective time and
  idempotency key and does not submit until the user confirms.
- **Neutral maintenance templates:** The global maintenance form offers factual
  Cleaning, Inspection, and General templates. They contain no certified interval,
  safety conclusion, or recommendation.
- **Connectivity-aware capture:** Global maintenance and firearm-detail inline
  maintenance now retain the submitted idempotency key through uncertain-outcome
  retries and rotate it only after a confirmed success. Validation failures remain
  visible form errors; no mutation queue or service-worker sync was added.
- **Saved history view Phase A:** History now uses the existing bounded
  `page`, `pageSize`, and `type` API query parameters, URL-backed pagination/type
  state, and validated account-keyed presentation preferences. It does not claim
  unresolved cross-screen or “since last maintenance” semantics.
- **Personal goals Phase A:** The dashboard supports one account-local target for
  shooting or produced rounds in the selected analytics period. It is explicitly
  presentation-only and sends no reminders. Notifications are outside the current
  Workshop product direction.
- **Bounded report export:** The authenticated Workshop API exposes a default
  30-day, correction-aware JSON/CSV report directly from account-scoped reads.
  Reports are bounded to one year, contain analytics and activity summaries, and
  do not create persisted files or act as a backup.

Implementation files additionally include:

- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx`
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx`
- `frontend/shooter-workshop.client/src/routes/Maintenance/action.ts`
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx`
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/action.ts`
- `frontend/shooter-workshop.client/src/routes/History/component.tsx`
- `frontend/shooter-workshop.client/src/routes/History/loader.ts`
- `frontend/shooter-workshop.client/src/routes/Maintenance/route.test.ts`
- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/fe-packages/services/src/endpoints-frontend.ts`
- `frontend/fe-packages/services/src/workshopService.test.ts`
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`

The following candidates remain intentionally gated rather than being represented
by unsafe client-only substitutes:

- **Spreadsheet import:** component/recipe idempotency and historical attribution
  contracts are not uniform enough for a safe commit path.
- **Portable backup:** requires a versioned complete package, integrity checks,
  duplicate handling, idempotent import, rollback, and recovery ownership.
- **Notifications and reminders:** intentionally excluded. Workshop is a utility
  that reacts to the signed-in user's actions; it does not schedule recurring
  engagement, maintenance or goal notifications.
- **Recipe QR links:** use a direct authenticated link to one active recipe's
  production page. No server-issued label token, revocation/regeneration workflow,
  short-code namespace, scan tracking or print-service scope is required.
- **Persisted analytics rollups:** requires representative performance evidence,
  an ADR, replay equivalence proof, lag visibility, rebuild/rollback ownership,
  and a recovery runbook.

## Focused product decision — no Workshop notifications

Workshop remains an on-demand utility. The user is not expected to create
reminders, recurring actions or engagement schedules, so the application does not
need notification consent, Web Push subscription handling, scheduled jobs,
notification retention or notification audit data.

The existing MorWalPizVideo and BackOffice Web Push implementation remains
unchanged and outside Workshop. `ApplicationKey` already identifies an existing
push consumer; no new Workshop `context` or application key is required.

Reconsider notifications only through a separate product decision that introduces
a concrete user-authored scheduled action. Maintenance thresholds, goals and
inactive usage are not permission to add notifications implicitly.

## Focused architecture decision — direct recipe QR links

### Direct answer

The QR is a normal authenticated link to one active recipe's production page. It
does not need a server-issued token, short code, revocation lifecycle, scan audit
or redirect service.

1. Open an active recipe in Workshop.
2. Display a QR for its canonical Workshop production-page URL.
3. Allow the user to download the QR as an image or capture it with the operating
   system's screenshot function.
4. The user prints that image using their own tools and preferred physical format.
5. Scanning opens the recipe-specific production page.
6. The page displays every current recipe value needed for reloading reference.
7. The page also provides the existing production inputs: quantity, production
   date and optional batch notes.
8. Submission creates a new ammunition production operation only after explicit
   confirmation.

The application does not own printer integration, label dimensions, print
templates, printer compatibility or physical scan-distance certification.

The recipe information displayed on the production page is:

- name;
- caliber;
- OAL;
- bullet weight;
- bullet form;
- drawing/trafilatura;
- primer;
- powder;
- powder weight;
- recipe notes;
- status; and
- current recipe version.

Those values are read-only on this page. Recipe edits remain in the recipe
management flow. The production form adds only the production-specific quantity,
date and notes. The server continues to snapshot the current recipe values and
version into the new production batch.

### Canonical navigation and unavailable recipes

Use a dedicated addressable route for production from one recipe, conceptually
owned below the existing Recipes area. The route identifier is the existing
application-generated recipe identifier and is a selector, not authorization.

The route loader must call an authenticated, account-scoped active-recipe read:

- an active recipe owned by the current account returns the complete current
  recipe;
- a missing recipe returns `404 Not Found`;
- an archived/inactive recipe returns the same `404 Not Found`;
- a recipe owned by another account returns the same `404 Not Found`; and
- malformed identifiers return the same non-revealing result.

There is no recovery picker on an invalid QR destination. Navigation ends on the
normal localized Workshop 404 surface, as requested.

The current API exposes a recipe collection but no account-scoped single-recipe
read. The implementation therefore needs an active-only recipe detail endpoint and
matching Domain/service method. It should reuse the existing account-filtered
repository lookup rather than filtering a full recipe collection in the browser.

The production command must repeat the active-recipe check at submission time. A
recipe can be archived after the page loads; that race must not create production
from an inactive recipe.

### QR generation and download boundary

Generate the QR in the Workshop client from the canonical recipe-production URL.
The QR contains no recipe fields, account identity, notes or authorization token.
It can be rendered to an image and offered through a normal download action.

No server-side QR persistence is required. No QR image is uploaded or retained.
The same recipe URL can generate the same functional QR again, and recipe edits do
not require regeneration because the destination always loads the current active
recipe version.

The QR presentation should include:

- the QR image;
- recipe name and current version adjacent to, but not encoded inside, the QR;
- the canonical Workshop origin so the user can recognize the destination;
- a download-image action; and
- accessible text identifying which recipe the QR opens.

There is no application print button requirement. Browser screenshots and the
downloaded image are sufficient; physical printing is user-owned.

### Why existing QR and ShortLinks surfaces are not reused

`MorWalPizVideo.BackOffice/Controllers/QRCodeController.cs` is an authenticated
administrative binary renderer. Calling it from Workshop would cross the
BackOffice boundary and add no ownership or navigation benefit. Its QRCoder
dependency is implementation prior art only, not a reusable Workshop endpoint.

`MorWalPizVideo.ShortLinks` provides globally addressable anonymous redirects,
public code lookup and click tracking. The Workshop QR already contains its final
authenticated destination, so an intermediate public redirect would add exposure,
failure modes and operational ownership without user value.

### Required backend behavior

The focused backend change is an active recipe read, not a label subsystem:

1. ServerAPI derives the public-account identifier from the authenticated claim.
2. Domain loads the recipe by account and recipe identifier.
3. Domain returns no result when the recipe is missing or not active.
4. ServerAPI returns the complete recipe contract or `404`.
5. Production submission reloads the same account-owned recipe and rejects it if
   it is no longer active.

No MongoDB collection, QR token, label entity, redirect record, scan record,
revocation field, expiry field, rate-limit policy or migration is required.

### Required frontend behavior

The dedicated recipe-production page must:

- be directly addressable and survive a hard reload;
- restore the exact local destination after login;
- display every recipe value listed above;
- make recipe data read-only;
- initialize a fresh production draft with date, quantity and notes;
- create a fresh idempotency key for the new production submission and retain it
  for uncertain-outcome retry according to existing capture rules;
- show pending, success, validation and uncertain-network states;
- return to the current page with the newly created production result visible or
  provide the established safe navigation to Ammunition history; and
- render the normal 404 route when the recipe cannot be resolved.

Scanning or loading the route never creates a batch. Only the explicit production
submission performs a mutation.

### Implementation impact

Expected existing files to modify:

- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — account-scoped active
  recipe read and active-state validation during production;
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` —
  authenticated single-recipe read returning `404` for missing, foreign or
  inactive recipes;
- `frontend/fe-packages/services/src/endpoints-frontend.ts` and
  `frontend/fe-packages/services/src/workshopService.ts` — typed recipe-detail
  transport through `accountApiService`;
- `frontend/shooter-workshop.client/src/routes/config.tsx` — dedicated
  recipe-production route and localized 404 behavior;
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — QR
  display/download entry point for an active recipe;
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/` — loader, component and
  action for current recipe information plus the production form;
- Workshop localization and stylesheet files — complete recipe labels, responsive
  read-only detail layout and QR download presentation;
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — active,
  archived, missing and cross-account recipe behavior; and
- focused Workshop route tests — deep-link load, complete value display, QR image
  download, explicit submission and 404 cases.

`MorWalPizVideo.Models/Models/ShortLink.cs`,
`MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs`,
`MorWalPizVideo.BackOffice/Controllers/QRCodeController.cs` and BackOffice
short-link routes are verify-only and remain unchanged.

### Acceptance gates

- The QR resolves directly to the canonical Workshop recipe-production page.
- Signed-out navigation returns to that exact page after successful login.
- The page displays every current recipe value, including version and notes.
- Recipe values cannot be edited from the production form.
- Missing, malformed, archived, deleted and foreign-account recipe identifiers all
  produce the localized 404 surface.
- A recipe archived between page load and submit cannot create a new batch.
- Scanning and page loading perform no mutation.
- Explicit submission uses the existing account, CSRF, idempotency and recipe
  snapshot rules.
- The downloaded QR image contains only the canonical Workshop URL.

### Delivery update — direct recipe QR-to-production

The direct QR flow is implemented. Active recipe cards now link to an
account-scoped `/recipes/{recipeId}/produce` page. The API returns only the
authenticated account's active recipe and returns `404` for missing, archived,
malformed or foreign-account identifiers. The page displays the complete
read-only recipe snapshot, generates the QR image in the browser, and provides
an image download. Its production form reuses the existing CSRF-protected,
idempotent production command; the service rechecks the recipe status before
creating the batch.

Implementation files:

- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `frontend/fe-packages/services/src/endpoints-frontend.ts`
- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/fe-packages/services/src/index.ts`
- `frontend/shooter-workshop.client/src/routes/RecipeProduction/`
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx`
- `frontend/shooter-workshop.client/src/routes/config.tsx`
- `frontend/shooter-workshop.client/src/i18n.tsx`
- `frontend/shooter-workshop.client/src/main.scss`
- `frontend/shooter-workshop.client/package.json`
- `frontend/yarn.lock`

QR images, scan events, label tokens and printer configuration are not persisted.
- No QR image, scan event, label token or printer configuration is persisted.
- Existing recipe editing and general Ammunition production remain available and
  unchanged.

### Impeccable planning lens

This is a refinement of the existing Workshop **Operate** interface, not a
redesign. Workshop has no app-local `DESIGN.md`; implementation should preserve the
functional dashboard treatment in `docs/architecture/shooter-workshop.md` and the
brand tokens documented by `frontend/morwalpizvideo.client/DESIGN.md`.

Before implementation, use the narrow `shape` workflow for the active-recipe
detail, QR download and production form hierarchy. After implementation, use
`harden` for expired sessions, hard reload, archived-between-load-and-submit,
missing/foreign recipe 404 behavior, long localized recipe values, download
failure and keyboard/screen-reader operation. Minimum visual evidence is desktop
and narrow-mobile capture of the recipe-production page and QR download state.

Validation for this broader slice:

- `cd frontend && yarn workspace shooter-workshop.client test` — 38 tests passed across 14 files.
- `cd frontend && yarn build:models && yarn build:services && yarn workspace shooter-workshop.client build` — passed.
- `dotnet build MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed with existing package vulnerability warnings.
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~WorkshopServiceTests"` — 12 passed.

All candidates must preserve the accepted Workshop rules:

- account identity is derived from the authenticated public-account session;
- immutable Workshop operations remain the source of truth;
- corrections append compensating facts;
- server-side FIFO batch resolution and component attribution remain authoritative;
- current ammunition behavior remains allocation-free;
- PWA behavior must not cache account/API data or introduce background sync without
  separate approval;
- Italian and English presentation remain culture-neutral at the API and persistence
  boundaries.

## Prioritization method

Evaluate each candidate against:

1. reduction in repeated data-entry work;
2. improvement in record completeness or traceability;
3. privacy and security cost;
4. correction/idempotency complexity;
5. mobile usability;
6. measurable demand from real Workshop usage;
7. compatibility with immutable replay and reconciliation.

Do not prioritize a candidate solely because it is visually attractive or easy to
add to the client. A candidate is ready for implementation only after its data
ownership, failure behavior, and rollback boundary are explicit.

## Candidate 1 — Guided first-use onboarding

### User value

Help a new user reach a useful first record without requiring them to understand
every domain before starting.

### Proposed flow

1. Create the first firearm.
2. Optionally create a component definition and physical instance.
3. Optionally create a recipe.
4. Record the first production or shooting session.
5. Explain tracked versus explicitly untracked ammunition.
6. Explain that component intervals are user-entered records, not safety advice.

The flow must be skippable. A user may intentionally use only equipment,
maintenance, ammunition, or shooting history.

### Constraints

- Each step must call the existing canonical command and idempotency path.
- Do not create a wizard-specific write model.
- Do not persist a half-complete account-wide onboarding transaction.
- A retry must converge on the same command effect.
- Resume state may be a non-sensitive presentation preference, not an account
  record or token.

### Acceptance evidence

- A new account can skip any optional step.
- Refreshing or retrying does not duplicate firearms, recipes, batches, or sessions.
- The first-use explanation contains no safety or load recommendation.
- The flow works in Italian and English and at mobile widths.

### Dependencies

- Retry-convergent firearm creation.
- Canonical shooting and production routes.
- Stable empty states and account session restoration.

## Candidate 2 — Repeat session and maintenance templates

### User value

Reduce repeated typing for common range sessions and recurring maintenance notes.

### Proposed behavior

- “Repeat previous session” creates a new unsaved draft containing firearm,
  recipe quantities, explicit untracked quantity, and presentation fields.
- The user must review effective time, quantities, and notes before saving.
- Maintenance templates prefill event type and optional note structure.

### Constraints

- A repeat is a new command, never a reuse of the prior operation ID, idempotency
  key, correction link, or recorded timestamp.
- Templates must not encode manufacturer-certified intervals or safety conclusions.
- Drafts remain local to the current session unless persistent draft storage is
  separately approved.
- The server revalidates firearm, recipe, component, stock, and installation state.

### Acceptance evidence

- The copied form is visibly unsaved and can be changed before submission.
- A repeated session receives a fresh idempotency key.
- Network retry produces one new operation.
- The original session remains unchanged.

### Dependencies

- Canonical shooting form shared by Sessions and Firearm Detail.
- Explicit effective time.
- Focused retry and correction tests.

## Candidate 3 — Saved views and personal filters

### User value

Let users return quickly to views such as “last 90 days,” “since last
maintenance,” or one firearm’s shooting history.

### Proposed behavior

Support presentation-only saved views for:

- date range;
- firearm, component, recipe, or event type;
- dashboard period;
- expanded/collapsed sections.

Provide built-in shortcuts such as “this year,” “last 90 days,” and “since last
maintenance.”

### Constraints

- Saved views must not contain account data, operation payloads, notes, or tokens.
- If stored in the browser, treat them as preferences and validate their shape on
  read.
- If later stored server-side for cross-device access, add an account-owned DTO and
  retention/delete policy first.
- The server must still apply account ownership, date bounds, pagination, and query
  limits.

### Acceptance evidence

- Invalid or stale preference values fall back safely.
- Restoring a saved view never bypasses route authorization or server filtering.
- Long labels and empty result sets remain understandable.

### Dependencies

- Bounded analytics/history endpoints.
- Stable filter semantics and localized labels.

## Candidate 4 — Personal goals without reminders

### User value

Give users optional motivation to record consistently or organize maintenance.

### Possible goals

- recorded sessions in a period;
- recorded rounds in a period;
- percentage of rounds with known ammunition;
- maintenance events recorded;
- reduction in untracked entries.

### Constraints

- Goals are private and user-defined.
- No public ranking, social comparison, or competitive leaderboard.
- Goals must not be presented as training prescription, health guidance, or safety
  advice.
- Goals remain visible only while the user is actively using Workshop. Do not add
  email, push, scheduled jobs, recurring actions or inactivity engagement.
- A goal must distinguish “not recorded” from “zero activity.”

### Acceptance evidence

- A user can disable and delete a goal.
- Missing data is shown as unknown rather than interpreted as zero.
- Goal progress is derived from the same correction-aware analytics semantics.
- No notification subscription or scheduled delivery is created.

### Dependencies

- Stable analytics summaries.
- No notification dependency.

## Candidate 5 — Account export and personal backup

### User value

Allow users to inspect, print, or retain their own Workshop history.

### Separate product decisions

An export report and a portable backup are different products:

- **Report:** human-readable CSV/JSON/PDF-like summary for a selected period.
- **Backup:** complete, versioned data package that can be validated and restored.

Do not label a report as a backup.

### Constraints

- Export is account-owner-only and should require recent authentication or explicit
  confirmation for a full account export.
- Export scope, date range, and included domains must be visible before creation.
- Sensitive personal notes and inventory data require a clear warning.
- Server-only identifiers should be omitted unless required for a documented
  round-trip backup format.
- A backup format requires schema versioning, integrity checks, idempotent import,
  duplicate handling, and a tested rollback/recovery path.
- Do not place exported data in a shared cache or service-worker cache.

### Acceptance evidence

- Another account cannot retrieve an export by changing an identifier or URL.
- Export content matches correction-aware analytical totals.
- Large exports are bounded, observable, and do not block normal account requests.
- A documented retention and deletion policy exists for generated files.

### Dependencies

- Stable analytics and immutable operation semantics.
- Explicit privacy, retention, storage, and download design.

## Candidate 6 — Spreadsheet import

### User value

Help users migrate from spreadsheets and handwritten trackers without retyping
their entire history.

### Proposed flow

1. Upload a supported CSV template.
2. Map or validate columns.
3. Run a dry-run preview.
4. Show accepted rows, rejected rows, and reasons.
5. Require explicit confirmation.
6. Commit idempotently and provide an import report.

### Supported first scope

- firearms without excluded real-world identifiers;
- component definitions and physical instances;
- recipes;
- production batches;
- initial counters or balances only when represented by explicit ledger-backed
  facts.

Historical shooting import requires a separate design because installation
attribution, effective time, recipe snapshots, and correction history may be absent.
The importer must not fabricate those facts.

### Constraints

- Validate every row before mutation.
- Use an import identity per file/row to support retry and resume.
- Never partially claim success without an itemized result.
- Reject serial numbers, licensing fields, unsafe load claims, and unsupported
  allocations.
- Keep uploaded files private, time-limited, and removable.
- Do not introduce a second importer-specific domain model.

### Acceptance evidence

- A dry run performs no domain mutation.
- Retrying the same import does not duplicate accepted records.
- Invalid rows remain inspectable without exposing other accounts.
- An interrupted import can resume or be safely abandoned.

### Dependencies

- Retry-convergent create commands.
- Versioned backup/import contract.
- Privacy and file-retention decision.

## Candidate 7 — Direct recipe QR links

### User value

Place a printed QR near the user's reloading equipment so scanning opens the exact
active recipe and a fresh ammunition-production form without searching.

### Proposed behavior

The active recipe UI displays a QR containing the canonical authenticated Workshop
URL for that recipe's dedicated production page. The user may download the QR as
an image or capture it with a screenshot and then print it independently.

Scanning displays all current recipe values and a production form for quantity,
date and optional notes. It never saves automatically.

### Constraints

- Encode only the canonical Workshop origin, route and application-generated
  recipe identifier.
- The recipe identifier selects a record but never authorizes access.
- The server re-checks account ownership and active recipe status on page load and
  production submission.
- Missing, malformed, archived, deleted and foreign-account recipes return the same
  localized `404 Not Found` result.
- The page always displays the current recipe version; there is no immutable
  historical-version production flow.
- Recipe values are read-only on the production page.
- Submission continues through the existing CSRF and idempotent production command.
- Do not add server-issued tokens, short codes, redirect records, scan analytics,
  revocation/regeneration, printer integration or application-owned print layouts.

### Acceptance evidence

- A direct scan survives hard reload and returns to the same destination after login.
- The production page shows every current recipe value and the current version.
- A QR for another account, archived recipe or missing recipe resolves to 404
  without revealing why.
- Scanning performs no mutation; explicit submit creates one production operation.
- The downloaded image contains only the canonical authenticated URL.

### Dependencies

- Account-scoped active recipe detail read.
- Recipe-specific production route and login return-path preservation.
- Client-side QR image rendering/download.

## Candidate 8 — Connectivity-aware capture without offline mutation queues

### User value

Prevent users from losing a partially completed form when connectivity fails.

### Approved initial direction

- Detect offline/network failure.
- Keep the unsaved form values visible in memory.
- Explain whether the command may have been accepted.
- Retry the same idempotent command only after reconciling the uncertain outcome.

### Explicitly deferred

Do not implement a persistent offline mutation queue yet. It would need decisions
for:

- stale component installation attribution;
- recipe-version drift;
- ammunition stock races;
- effective-time ordering;
- correction semantics;
- account data at rest;
- multi-tab and multi-device conflicts;
- queue deletion and logout behavior.

### Acceptance evidence

- An offline form never displays a false success.
- A timeout does not blindly create a second command.
- Logout removes or safely isolates any in-memory draft.
- The PWA service worker continues not to cache account/API data.

### Dependencies

- Complete idempotency and uncertain-outcome handling.
- A separate ADR and threat model before any persistent queue.

## Candidate 9 — Persisted analytics rollups

### User value

Improve response time for very large personal histories or repeated reports.

### Approval condition

Only consider this after measuring bounded query performance on representative
long-history accounts. Current personal-account scale does not justify a second
analytics source by default.

### Constraints

- Rollups are disposable projections, never the source of truth.
- They must be rebuildable from immutable Workshop operations.
- Corrections, backdated events, schema versions, and reconciliation state must be
  handled deterministically.
- Rollup lag must be visible to the user.
- A rebuild and rollback procedure must exist before activation.

### Acceptance evidence

- Rollup output equals direct replay output for the same cutoff.
- A failed rollup does not corrupt current ledger or projections.
- Disabling rollups returns to a correct bounded/read-replay path.

### Dependencies

- Measured query evidence.
- New ADR, operational ownership, monitoring, and recovery runbook.

## Recommended sequencing after the current workflow

1. Guided onboarding, after canonical capture and retry-safe firearm creation.
2. Repeat session and maintenance templates, after explicit effective time.
3. Saved views, after analytics filters and bounded reads stabilize.
4. Export report, after correction-aware analytics and privacy review.
5. Spreadsheet import and portable backup, as separate compatibility projects.
6. Direct recipe QR links, after active-recipe detail and login return-path behavior
   are proven.
7. Presentation-only goals, with no notification or recurring-action expansion.
8. Offline mutation queues and persisted analytics rollups last, each with a separate
   architectural decision and recovery proof.

## Required follow-up artifacts

Before implementation of any candidate, create or update:

- a scoped feature brief with user outcome and stop condition;
- an acceptance matrix covering success, retry, stale/conflict, empty, and
  account-isolation states;
- a privacy/data-retention decision;
- a migration and rollback note if persistence or import is involved;
- focused backend/frontend tests;
- updated architecture and technical-debt documentation when an architectural
  boundary changes.

## Stop conditions

Stop and request product clarification if a proposed nice-to-have:

- requires serial numbers or public profiles;
- infers safety, reliability, or certified maintenance status;
- changes the source-of-truth role of immutable operations;
- reintroduces firearm-specific ammunition allocation;
- needs persistent offline writes without a conflict model;
- exposes account data to shared caches, URLs, logs, or third-party services;
- expands into Shooting Range booking/reporting behavior.
