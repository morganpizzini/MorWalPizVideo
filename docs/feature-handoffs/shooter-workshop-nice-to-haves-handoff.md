# Shooter Workshop Nice-to-Haves Handoff

**Status:** Deferred product candidates. None of these items are part of the current six-point delivery slice.

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

## Candidate 4 — Personal goals and reminders

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
- Reminders require an explicit notification and retention decision. Do not silently
  add email, push, or background jobs.
- A goal must distinguish “not recorded” from “zero activity.”

### Acceptance evidence

- A user can disable and delete a goal.
- Missing data is shown as unknown rather than interpreted as zero.
- Goal progress is derived from the same correction-aware analytics semantics.
- Notification behavior is opt-in and auditable if introduced.

### Dependencies

- Stable analytics summaries.
- A separately approved notification/privacy design if reminders are added.

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

## Candidate 7 — Opaque QR or short-code labels

### User value

Reduce selection effort when recording a component or batch in a workshop or at
the range.

### Proposed behavior

Scan a label containing only an opaque application identifier or short-lived lookup
token. Scanning selects a form value; it does not authorize or save a mutation.

### Constraints

- Never encode firearm descriptions, recipe details, account identity, or notes.
- The server must re-check account ownership and current entity state after scanning.
- Labels can be revoked or regenerated.
- Mobile browsers must provide a keyboard/manual fallback.
- Avoid short codes that are easily enumerable unless paired with ownership checks
  and rate limits.

### Acceptance evidence

- A label from another account cannot reveal a record.
- Scanning a retired or archived record gives a clear recoverable state.
- Manual entry and keyboard navigation remain fully supported.

### Dependencies

- Stable entity identifiers and mobile capture flow.
- Privacy review and rate-limit policy.

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
6. QR/short-code labels, after mobile capture behavior is proven.
7. Goals/reminders, only after observed retention need and notification approval.
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
