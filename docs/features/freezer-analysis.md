# FreezerAnalysis

Consolidated 2026-11-09 from the foundation and route/field-refinement handoffs.
**Mixed status:** local feature work is recorded delivered; larger M0-M8
acceptance remains incomplete and production activation is not authorized.
The following is historical evidence, not a fresh source/test/deployment audit.
See [TD-033](../architecture/technical-debt.md) and the
[former filename map](../feature-handoffs/README.md#former-handoff-map).

## Ownership and security

- Independent API/SPA, shared platform PublicAccount identity but separate
  feature-application Mongo database and host sessions. No cross-host cookie SSO.
  User confirmed separate production SPA/API origins on 2026-10-31, superseding
  combined/same-origin production proposals. Exact credentialed CORS,
  Secure/SameSite=None and CSRF are required; no bearer/browser workaround.
- Latest local composition uses cookie-backed PublicAccount mock, replacing
  earlier automatic development authentication/unavailable-account-service
  claims. No cookie means unauthenticated. Protect online routes/sanitize return;
  retain Development FeatureManagement flags, scoped adapters over host-lifetime
  state, AppHost Vite same-origin proxy and CSRF.
- Published facts/revisions are immutable/shared. Identity mappings, availability,
  exclusions, plans, notes, reports, and parameter adoption are account-owned.
  Freeze exact compressed/hashed input package, versions, cutoffs, mappings,
  history; later facts/parameters cannot alter replay.
- Field events append idempotently; adoption is explicit and future-draft-only.
  PWA caches shell only. IndexedDB is account-partitioned; live ownership/auth
  precedes sync. Clear previous-account data on logout/switch; offline does not
  establish a live session.

Owners: [API](../../FreezerAnalysis.Api/),
[API tests](../../FreezerAnalysis.Api.Tests/),
[SPA](../../frontend/FreezerAnalysis.spa/), Domain/Models/Contracts FreezerAnalysis
boundaries, AppHost and owning workflows. Consult SPA PRODUCT/DESIGN before UI
work. Do not copy seeded credentials or athlete personal data into documentation.

## Local analysis and numerical acceptance

Recorded refinement:

- Divisions: OPEN, OPTICS, PRODUCTION, PCC IRON, PCC OPTICS,
  PRODUCTION OPTICS, REVOLVER, STANDARD. Required selects; new drafts inherit
  profile identity/division/performance, existing drafts unchanged.
- Performance 0.935 means 93.5% expected stage maximum points, not rank/confidence.
  Active summary/lazy Verify detail; exactly two distinct stage-ordered adjacent
  HF series/table. Omit missing/DQ/zeroed/nonfinite, never render as zero bars.

Acceptance constraints retained from the larger plan, **not all proven passed**:

| Area | Constraint |
| --- | --- |
| History/model | Strictly earlier dates; no arbitrary same-day order; 200 alternating fit iterations; sample-SD/group convention short 1-12, medium 13-24, long 25+ |
| Pool | +/-8.3%; dispersion 15%; overlap max(5, valid stages - 3) |
| Defaults | beta 1.2, drift 4%, variability 0.105, performance 0.935; distance 0.25 unconfirmed; afternoon 0.94 and one-hand 0.89 opt-in |
| Invalid data | Undefined/nonfinite/no-positive-data gives diagnostic; no epsilon or fabricated estimate |
| Independent examples | HF 7.5/performance 0.935 error cost 2.22s at 32 rounds, 2.73s at 12; Charlie HF 6.84/32 gives 4-5, half-up; inclusive rhythm thresholds 4/6/7.5/8.5/9.5 |
| Deferred original parity | Sicily beta 1.25/drift 5%; stage 4: 9.27/10.55/11.82; tolerance 0.01 HF, registry 0.05 percentage points; verification -5.4% mean, 8.0% SD, 10/12 coverage, +0.4% normal, beta 1.18 |

Original Python/golden/archive/parity inputs remain absent. Synthetic technical
readiness is allowed; do not claim historical parity.

## Import, PDFs, and artifacts

- Supplied samples: 9 classification pages, 48 verification pages, 142/142
  entries, 1,704 rows, 11 advisory mismatches, seven DQ appendix entries with
  zero unique mappings. Stage maxima/minimum rounds lack authority:
  **CanPublish=false**. Never guess DQ/non-DQ or maxima.
- Candidate inferred-maxima fit 142/142 within 0.02, maximum difference 0.000499,
  is **not accepted reconciliation**; raw sums matched 1/142.
- Preserve [sample fixtures](../feature-handoffs/freezer-analysis-samples/) at
  their current path. FreezerPdfPairSampleTests requires classifica.pdf and
  verify.pdf there for repository discovery. CSV and PNG in that directory also
  remain untouched; no personal sample contents are reproduced here.
- Local Poppler/pinned Chromium, A5/A4 and two 1290x2796 PNGs. Proposed phone
  surface 430x932 at 3x, text area 278-816 logical/834-2448 pixels, minimum 12pt.
  Overflow fails, never silently shrinks/truncates. Use local assets, no external
  renderer fetch; artifacts are private/owner-authorized.
- AI proposals remain unimplemented/disabled pending processing approval and
  never become authoritative scoring, identity, or adoption.

## Route and field refinements

Recorded delivered 2026-11-05:

- Typed route/layout/account/matches/profile/identity-review/match-detail/
  verification/field modules; routes.tsx remains a compatibility facade despite
  proposed retirement. Selection captures primitives, shows two empty slots,
  uses stable IDs for duplicate names; clicked submitter distinguishes save/freeze.
- Optional turn text and cyclic stage-ID order from starting stage; no rendered
  numeric zero. Preserve operation IDs/queues; latest turn, not arbitrary last
  note, owns context. Keyboard/touch/tooltip help. HF comparison uses two columns
  above 760px, stacked below/equal with 14px gap; action rows wrap with 10px gap.
- Reopen requires auth/CSRF/owner, expected revision+snapshot CAS, not-started
  state; clear working pointer/cache and advance revision.
  **Never reset event counter or delete frozen history.**
  Historical queued notes for the same owned draft/snapshot remain accepted
  after reopen/refreeze. Snapshots retain division/power factor; legacy fallback
  must not masquerade as frozen evidence.
- Owner-authorized fixed phone-front/back extraction from existing private ZIP,
  PNG signature/image/png/fixed attachment; ZIP retained.
  Proposed preparation descriptors/hash/ETag, full-stage ordering before split,
  renderer-version advance are not individually established by delivered bullets.

Rollback hides UI first but retains backend historical-note acceptance after any
reopen. Old pointer-bound append strands queues. No forced rebind, counter
rollback, or queue purge. Use narrow Impeccable Operate-mode adapt/harden checks
against the existing PRODUCT/DESIGN, not a redesign.

## Validation ledger

Separate historical runs; **do not sum**:

| Scope | Recorded result and limit |
| --- | --- |
| Composition | 73 passed, 0 failed, 2 skipped |
| Later local refinement | SPA build/20 frontend tests passed; API says 77 total, 74 passed, 2 skipped: **one result unaccounted for** |
| Earlier API iterations | 74, 64, and 68 passed with zero skips in separate runs |
| Native-focused | Synthetic extraction 1, renderer 2, supplied sample 1 passed |
| Other local checks | Repeated SPA 20; shared services 44; AppHost build/YAML parse passed |
| Engine benchmark | 20 warm synthetic samples, 500 competitors/34 stages: p95 119.47ms, min 102.03, max 135.26; not end-to-end capacity |
| Route/field final | Node 26.13.0, four files/22 tests and SPA TypeScript/build passed; test-only jsdom storage/Request-AbortSignal compatibility fixed |
| Route/field API | Serial build after restore blocked by unrelated BlogInteractionService missing IPublicAccountRepository after a brace correction; **no backend pass** |

Docker daemon unavailable; hosted workflow execution and browser captures absent.
Two PDF checks skip without configured extractor. Rerun API/renderer tests and
verify native PNG downloads, history/ownership/races and browser geometry.

## Production gates

The FA-001-010 debt-reduction work is **proposed**, not delivered closure:

1. Establish governed manual index audit/apply and readiness ownership; reconcile
   unapplied indexes/dead option. Enforce two database/account boundaries with
   real Mongo/concurrency tests.
2. Make import pointer/manifest, freeze retry identity, and field sequence/events
   retry-convergent; report-only repair before any reviewed mutation.
3. Qualify an exact compatible SPA/API digest pair with native dependency and
   health readiness. Existing API ZIP differs from the native-dependency image;
   no image/runtime activation is implied by workflow authoring.
4. Prove real-Mongo two-account isolation, Chromium/WebKit cross-origin cookie/
   CSRF, offline/switch/logout/reopen queue behavior, and container artifacts.
5. Address measured N+1/unpaged reads: 1,000-match history cap and 20,000-result
   broad loads are known constraints. BSON must remain below 16 MiB;
   proposed feature guard is 8 MiB.
6. Measure on approved production tier/two-user workload: estimate p95 <=1s,
   freeze <=2s, artifact set <=10s, 20 MiB import <=30s.
   Phone turn 60s/3 actions, midday 120s/5, dictation 180s remain unmeasured.
7. Resolve coordinator/route size, feature telemetry, stale ADR/docs, authoritative
   PDF mapping/scoring, original parity and lawful optional ESS/WinMSS samples.
8. Assign governance/retention/correction/erasure/LLM terms and ownership.
   Agree tier/concurrency/volume, complete manual full lifecycle and independent
   review before production approval.

No destructive reset/backfill is authorized. Retain additive records/objects/
queues; new writer phases need matching reconciliation before old images return.
Do not regress CSRF/cookie auth. Legal deletion may make replay explicitly
unavailable; never fabricate replacement evidence.
