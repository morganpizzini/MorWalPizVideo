# Shooter Workshop Future Improvements Handoff

**Status:** Six priority improvements implemented and validated; deferred nice-to-haves remain approval-ready in the dedicated handoff.

**Scope:** Shooter Workshop only. `MorWalPizVideo.ShootingRange` is a separate bay-booking proof of concept and is not part of this analysis.

**Evidence baseline:** 2026-11-15 repository source, architecture documentation, and all Shooter Workshop feature handoffs.

## Outcome

Shooter Workshop is a private, account-owned, installable web application for practical and sport shooters. It is an operational recordkeeping system, not a public community product, legal firearm registry, ballistic calculator, or ammunition-safety adviser. Its core job is to let a user quickly record equipment, component installation and usage, maintenance, ammunition production and stock, and shooting activity while preserving an auditable history.

The strongest future direction is:

1. make the existing recording workflows fast, contextual, consistent, and retry-safe;
2. complete approved history and component-management gaps before adding broad analytics;
3. derive statistics from immutable Workshop operations and historical facts, not mutable counters;
4. present actionable personal trends without implying mechanical safety, load safety, or certified maintenance advice;
5. preserve the existing privacy boundary: no serial numbers, registration identifiers, public equipment profiles, or social sharing.

Assumptions:

- The primary user is an individual shooter recording their own data, often on a phone at the range or in a workshop, and reviewing deeper history on a larger screen.
- Existing personal-account scale remains valid for the next delivery increment, but analytical reads must not deepen the current full-collection materialization pattern.
- Italian remains the default culture and English remains the secondary culture.
- Allocation-free ammunition tracking is the accepted target. Historical allocation-bearing records remain compatibility data only.
- Recommendations below require user approval before they become delivery scope.

## Current Evidence

### Application type, audience, and purpose

- The functional baseline defines a personal equipment and ammunition management application deployed independently on a dedicated Workshop origin: `docs/architecture/shooter-workshop.md:1-14`.
- The application replaces spreadsheets, handwritten notes, and disconnected trackers across equipment, maintenance, ammunition, and shooting history: `docs/architecture/shooter-workshop.md:144-171`.
- It deliberately excludes serial numbers, legal documentation, public profiles, social sharing, ballistic calculations, load recommendations, safety validation, and manufacturer-certified maintenance recommendations: `docs/architecture/shooter-workshop.md:1206-1224`.
- The source implementation is a React 19, React Router 7, Vite, TypeScript, Bootstrap/SCSS SPA in `frontend/shooter-workshop.client`, using authenticated `/api/v1/workshop` routes in ServerAPI.
- It is an installable PWA, but the service worker is lifecycle-only. It does not cache API/account data and does not provide background synchronization: `docs/architecture/shooter-workshop.md:26-36`.
- The authenticated application is an **Operate** surface: users repeatedly enter and inspect factual records. Its established visual authority is the current graphite/black, white, and yellow `#FFFF36` implementation and the UI section in `docs/architecture/shooter-workshop.md:1040-1094`.
- No Workshop-specific `PRODUCT.md` or `DESIGN.md` exists. Current source, the functional baseline, and accepted handoffs therefore remain the design authority.

### Current workflow and information architecture

The current application includes:

- dashboard;
- firearms and firearm detail;
- component catalogue and physical inventory;
- maintenance;
- combined ammunition production and batch inventory;
- recipes;
- shooting sessions with inline correction;
- chronological history;
- integrity/reconciliation;
- account profile.

Implemented behavior includes:

- firearm registration, editing, archiving, and initial-round recording;
- component definitions, physical component instances, and installations;
- maintenance events associated with firearms or components;
- recipe creation and optimistic versioned editing;
- ammunition production with immediate batch review;
- recipe-quantity shooting capture with explicit untracked ammunition;
- server-owned FIFO batch resolution;
- immutable correction and delete compensation;
- firearm cumulative round history and component interval status;
- account projection reconciliation.

Key source boundaries:

- API ownership: `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- domain orchestration: `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- repository access and indexes: `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs`
- replay and integrity: `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs`
- persisted facts: `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`
- shared API contracts: `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- frontend route manifest: `frontend/shooter-workshop.client/src/routes/config.tsx`
- shared frontend transport: `frontend/fe-packages/services`
- shared TypeScript contracts: `frontend/fe-packages/models`

### Current statistical capability

The dashboard now exposes operational summaries:

- active firearm count;
- total firearm rounds;
- physical component count;
- components at or above their configured interval;
- total rounds produced;
- current ammunition on hand;
- recent activity;
- reconciliation status.

The current application also renders:

- a cumulative firearm round graph;
- component interval meters;
- recent chronological activity;
- reconciliation mismatch identifiers.

The new period analytics endpoint accepts bounded 30-, 90-, or 365-day reads from the authenticated account and aggregates shooting, tracked/untracked ammunition, production, current available stock, maintenance, malfunctions, corrections, and daily buckets from account-scoped facts. Correction operations are considered across the account so a later correction still supersedes an older session in historical periods. The read remains projection-aware and does not expose operational payloads.

The dashboard, capture/history, firearm, and reconciliation routes now use React
Router loaders for server-owned reads. Route-owned mutations use colocated actions
and fetchers, including shooting corrections and maintenance recording, so pending,
error, and revalidation state stays with the router.

No current implementation was found for:

- weekly or monthly shooting trends;
- ammunition production-versus-consumption reporting;
- tracked-versus-untracked ammunition rates;
- component usage-rate reporting;
- maintenance-frequency reporting;
- malfunction rates with a round denominator;
- stock-age reporting;
- data-quality reporting;
- export or backup reports;
- cross-firearm analytical comparison.

### Accepted target decisions that future work must preserve

The handoff set reviewed for this analysis is:

1. `docs/feature-handoffs/shooter-workshop-handoff.md`
2. `docs/feature-handoffs/shooter-workshop-ui-feedback-handoff.md`
3. `docs/feature-handoffs/shooter-workshop-component-feedback-handoff.md`
4. `docs/feature-handoffs/shooter-workshop-shooting-session-fifo-inline-correction-handoff.md`
5. `docs/feature-handoffs/shooter-workshop-allocation-removal-handoff.md`
6. `docs/feature-handoffs/shooter-workshop-firearm-detail-refinement-handoff.md`
7. `docs/feature-handoffs/shooter-workshop-production-route-handoff.md`
8. `docs/feature-handoffs/shooter-workshop-ammunition-production-merge-handoff.md`
9. `docs/feature-handoffs/shooter-workshop-recipes-route-edit-handoff.md`
10. `docs/feature-handoffs/shooter-workshop-localization-handoff.md`
11. `docs/feature-handoffs/shooter-workshop-pwa-handoff.md`
12. `docs/feature-handoffs/shooter-workshop-container-test-dependency-handoff.md`

The following accepted targets constrain future improvements:

- `WorkshopOperation` is the authoritative historical source. Firearm totals, component totals, and stock are independently rebuildable projections.
- Corrections append compensating facts and never rewrite the original operation.
- Shooting attribution belongs to the server. The browser supplies recipe quantities and explicit untracked rounds; it does not select batches or component installations.
- Current ammunition behavior is allocation-free. New functionality must not revive firearm allocation.
- Physical component facts belong to component instances; definitions are reusable categories.
- Firearm detail is the primary contextual status surface and should eventually support safe swap, replacement creation, and detach workflows.
- Route loaders, actions, fetchers, and normal revalidation are the desired client ownership model.
- Localized presentation must not change culture-neutral routes, stored domain values, or API payload semantics.
- PWA installation must not silently introduce account-data caching, background synchronization, or push behavior.

### Material gaps before broad feature expansion

1. Recipe search, filter, duplicate, archive, production history, and version-history presentation remain follow-up work.
2. Custom-period analytics, drill-down links, richer comparison views, and persisted rollups remain follow-up work.
3. Production duplicate/index audit, deployed exact-origin browser authentication, custom-domain/TLS, rollback, and reconciliation evidence remain open release concerns.
4. Maintenance-event semantics for a future “rounds since last maintenance” metric remain intentionally undefined; phase-one analytics report event counts only and do not imply safety or service recommendations.

## Delivery update — six priority improvements

Implemented in this slice:

1. Firearm-detail shooting now enters the canonical recipe-aware flow with firearm context preserved.
2. Shooting and maintenance capture expose editable effective date/time values; history can distinguish effective time from recorded time where available.
3. Dashboard and firearm detail expose contextual maintenance/shooting quick actions.
4. Dashboard, shooting, maintenance, history, firearms, and reconciliation reads use route loaders; their saves and rebuilds use route actions/fetchers and normal route revalidation.
5. Authenticated analytics provide bounded 30-, 90-, and 365-day summaries for rounds, tracked/untracked ammunition, production, stock, maintenance, malfunctions, corrections, and daily buckets. Corrections recorded after a selected period still supersede sessions inside it.
6. Firearm creation and shooting capture preserve idempotency across retry attempts. Firearm detail provides independent pagination metadata and controls, labels superseded sessions, and links into a contextual component install/swap/detach flow. Presentation-only browser preferences are limited to dashboard period and last recipe selection; the PWA remains free of account-data caching and background mutation queues.

Primary implementation surfaces:

- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`
- `frontend/fe-packages/models/src/workshop.ts`
- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/shooter-workshop.client/src/routes/`

Validation completed:

- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
- focused `WorkshopServiceTests`: 12 passed
- `cd frontend && yarn workspace shooter-workshop.client build`
- `cd frontend && yarn workspace shooter-workshop.client test`: 32 passed

Residual implementation risks are correction semantics for future non-shooting aggregates,
query-index evidence against long histories, and the intentionally deferred maintenance
interval semantics described above. The dedicated [nice-to-haves handoff](./shooter-workshop-nice-to-haves-handoff.md)
remains deferred.

## Proposed Design

### Product direction

Treat future work as a refinement of the existing operational application, not a redesign. The application should become a dependable personal logbook with two complementary modes:

- **Capture mode:** minimal decisions, context already selected, factual confirmation before saving, immediate review of the resulting record.
- **Review mode:** time-bounded trends, maintenance and inventory context, data-quality signals, and drill-down to the immutable facts behind every number.

The UI should remain familiar and restrained. Yellow identifies primary action, current selection, and important values; semantic warning/error colors communicate factual threshold or integrity states. Statistics should reduce uncertainty, not decorate the dashboard.

### Target user jobs

1. “I just completed a session; record it accurately before I leave.”
2. “I serviced or moved a component; record what changed at the correct effective time.”
3. “I produced ammunition; record the batch once and immediately verify inventory.”
4. “Show what needs my attention based on the thresholds I configured.”
5. “Show how much I shoot, produce, consume, maintain, and correct over time.”
6. “Let me trace any total back to the sessions, batches, components, and corrections that produced it.”

### Improvement group A — efficient recording

#### A1. One canonical contextual shooting flow

- Replace the firearm-detail total-only quick form with an entry point into the canonical shooting workflow.
- Preselect the firearm from context.
- Preserve recipe quantities, explicit untracked rounds, server FIFO resolution, idempotency, validation, and correction behavior.
- Show the calculated total as confirmation; do not make the browser total authoritative.
- After success, return to the originating context and show the new session and updated totals through route revalidation.

This is the highest-value capture improvement because it removes a current path that unintentionally reduces data quality.

#### A2. Explicit effective date and time

- Let the user review and change the effective date/time for shooting, maintenance, component installation/removal, and production where the domain supports it.
- Default to “now,” but never hide the value on operations whose historical attribution depends on it.
- Detect obviously unusual future dates and require correction through normal validation.
- Present the effective time separately from the recorded time in history and corrections.

#### A3. Contextual quick actions

- Firearm detail: record shooting, maintenance/malfunction, component swap/detach, and archive.
- Component detail: record maintenance, install, remove, replace, or inspect history.
- Ammunition: record production above the inventory result, following the established “record then review” pattern.
- Recipe detail: produce from this recipe, duplicate, archive, inspect production history, and inspect version history.
- Dashboard attention cards should deep-link to a prefiltered, pretargeted flow rather than a generic form.

#### A4. Safe remembered preferences

Remember only presentation-level choices such as:

- last selected recipe for a given firearm;
- last history date-range filter;
- preferred dashboard period;
- expanded/collapsed sections.

These preferences must never become authoritative command data, and browser storage must not contain session tokens, account records, operation payloads, or sensitive inventory details.

#### A5. Repeat and template actions

- “Repeat previous session” may copy firearm, recipe quantities, and presentation fields into a new unsaved form.
- The user must review effective time, quantities, and notes before submission.
- A repeat action creates a new idempotent command; it must never replay the prior operation identity or correction links.
- Maintenance templates may prefill event type and note structure, but must not state certified service advice.

#### A6. Draft and interruption handling

- Preserve unsaved form state in memory during route-local validation and network errors.
- Warn before abandoning a materially changed form.
- Do not add persistent offline account-data drafts or background synchronization without a separate privacy, conflict, and threat-model decision.
- Continue to show explicit offline/network state and retry the same idempotent command rather than creating a second operation.

#### A7. Guided onboarding

Add a short first-run sequence that helps the user:

1. create the first firearm;
2. optionally create a component definition and physical instance;
3. optionally create a recipe;
4. record the first production or shooting session;
5. understand tracked versus untracked ammunition;
6. understand that intervals and forecasts are user-defined records, not safety recommendations.

The sequence must remain skippable because equipment-only and ammunition-only usage are both supported product modes.

#### A8. Optional import after core convergence

A spreadsheet import wizard is a later nice-to-have for users leaving existing trackers.

- Import only supported factual entities and initial balances.
- Validate every row before committing.
- Present a dry-run summary and rejected-row reasons.
- Convert accepted initial counters and stock to explicit ledger-backed facts.
- Never import serial numbers or other excluded identifying fields.
- Make the import idempotent and resumable.

### Improvement group B — trustworthy statistics

#### B1. Activity trends

Provide selectable 30-day, 90-day, 12-month, and custom periods:

- rounds per week/month across all firearms;
- rounds per firearm;
- session count;
- average rounds per session;
- active versus archived firearm contribution.

Use effective operation time. Corrected or deleted sessions must contribute their net ledger effect, not remain double-counted.

#### B2. Ammunition flow

Show:

- produced, consumed, and on-hand rounds by period;
- grouped by caliber and recipe;
- tracked versus explicitly untracked consumption;
- remaining batch age using production date;
- consumption rate by recent period;
- batches with no recent consumption.

This is inventory reporting only. It must not evaluate recipe quality, calculate loads, or recommend quantities.

#### B3. Component usage and maintenance

Show:

- total rounds per physical component;
- rounds during the current installation;
- rounds since the most recent relevant maintenance event;
- configured interval consumption and remaining rounds;
- average recent usage rate;
- an optional estimated threshold date.

Any estimate must be labelled as a projection based on recent recorded usage and the user’s own threshold. It must never be described as a safety, replacement, or manufacturer recommendation.

#### B4. Maintenance and malfunction context

Show:

- maintenance events over time;
- last maintenance by firearm and component;
- malfunction count by firearm;
- malfunctions per 1,000 recorded rounds with the exact event count, round denominator, and date range;
- drill-down to the underlying events and sessions.

Do not infer cause, resolution, reliability, “safe/unsafe,” or component failure because the current event model does not support those claims.

#### B5. Data-quality and integrity indicators

Show:

- percentage of shooting rounds recorded as untracked ammunition;
- correction rate and recent correction count;
- sessions missing canonical operation links where legacy data allows that state;
- pending, inconsistent, or failed projection count;
- mismatch types from reconciliation;
- last successful reconciliation.

These indicators help the user improve record completeness and confidence. They should remain separate from shooting-performance statistics.

#### B6. Traceable analytical drill-down

Every aggregate card or chart must support a route to its contributing factual records:

- trend point to sessions;
- stock value to batches and consumption;
- component interval to installations, round contributions, and maintenance events;
- correction-adjusted totals to original and compensating operations.

The UI must clearly distinguish:

- “effective on” from “recorded on”;
- current projection from historical operation;
- tracked from untracked ammunition;
- factual current totals from forecasts.

### Improvement group C — useful nice-to-haves

#### C1. Saved views and filters

- Save named date-range/entity/type filter combinations as presentation preferences.
- Provide “this year,” “last 90 days,” and “since last maintenance” shortcuts.
- Keep the underlying API account-scoped and bounded even when a saved filter is restored.

#### C2. Personal goals

- Allow optional user-defined activity or recording-completeness goals.
- Goals may track recorded sessions, rounds, maintenance logging, or reduction in untracked entries.
- Do not create competitive leaderboards or public sharing.
- Do not present goals as training, medical, legal, or safety advice.

#### C3. Export and personal backup

- Provide scoped CSV/JSON exports for the account owner and a printable summary for selected periods.
- Require recent authentication or explicit confirmation for a full account export.
- Exports must exclude server-only identifiers unless needed for round-trip backup.
- Document that exported files can reveal sensitive ownership and usage patterns.
- A round-trip restore requires a separate compatibility and idempotency design; do not imply that a report export is a backup format.

#### C4. Labels and scanning

QR or short-code labels for user-created component instances and ammunition batches could reduce selection effort.

- Encode only an opaque application identifier or short-lived lookup token.
- Never encode firearm descriptions, recipe details, account identity, or other inventory facts directly.
- Scanning may select an item; the server must still verify account ownership and current state.
- Treat this as a later feature requiring mobile browser and privacy validation.

#### C5. Connectivity-aware capture

The application may detect loss of connectivity and keep the current unsaved form visible, but persistent offline mutation queues are not recommended in the current phase. Offline queues would introduce conflict ordering, stale installation attribution, recipe-version drift, stock races, and account-data-at-rest concerns.

### Analytical architecture

#### Source of truth

- Use `WorkshopOperation` for round, stock, correction, idempotency, and causal history.
- Use installation facts for interval attribution.
- Use maintenance events for user-recorded maintenance and malfunction context.
- Use production sessions, recipe snapshots, and batches for production and stock reporting.
- Treat current entity counters as projections suitable for current-state cards, not independent historical evidence.

#### Read-model boundary

Extend the existing authenticated Workshop read boundary with additive, account-scoped analytics contracts. Prefer a small number of responsibility-focused reads:

- activity summary for a required date range;
- ammunition flow summary for a required date range and optional grouping;
- component/maintenance summary;
- data-quality and reconciliation summary.

Each request must:

- derive account identity from authentication;
- require bounded date or entity criteria;
- return safe DTOs rather than persistence entities;
- include the applied date range and data freshness/reconciliation state;
- use culture-neutral values;
- avoid returning raw internal mismatch data when a typed user-facing category is sufficient.

#### Query and projection strategy

Phase 1 should use bounded indexed queries and deterministic aggregation over the personal account’s facts. Do not add a second analytics datastore or background processing merely to draw the first reports.

Before shipping historical dashboards:

- add repository methods that query by account, effective time, aggregate type, and entity;
- add or extend central Workshop index definitions for those access paths;
- avoid full account collection materialization in each request;
- define deterministic inclusion of reversals and replacements;
- expose reconciliation freshness with the result.

Only consider persisted analytical rollups after measured query evidence shows they are necessary. If introduced later, rollups must be disposable and rebuildable from immutable facts, versioned, correction-aware, and never become a new source of truth.

### UX direction and Impeccable workflow

- **Target surfaces:** Dashboard, Firearm Detail, Shooting Sessions, Maintenance, History, and a possible Insights route.
- **Mode:** Operate.
- **Incumbent context:** no `PRODUCT.md` or `DESIGN.md`; use the current Workshop SCSS/components, the black/graphite/white/yellow specification, localization behavior, and accepted route conventions as authority.
- **Change type:** refinement and functional expansion, not visual redesign.
- **Narrowest planning workflow:** use Impeccable `shape` to confirm information hierarchy, contextual capture transitions, realistic empty/loading/error/conflict/reconciliation states, and mobile flow before UI implementation.
- **Implementation quality workflow:** use `adapt` for range-side phone layouts and `harden` for error, conflict, localization, overflow, stale-data, and accessibility states. Use `audit` after implementation for technical accessibility and responsive checks.
- **Minimum visual evidence:** authenticated desktop and mobile captures for empty, representative, long-content, validation-error, network-error, pending, successful-save, correction, stale/conflict, and reconciliation-warning states.
- **Accessibility checks:** keyboard completion, visible focus, programmatic labels, chart alternatives, semantic status text not dependent on color, dialog focus return, reduced motion, and touch targets suitable for mobile use.

### Recommended delivery boundary

The first approved improvement release should stop after:

1. canonical contextual shooting entry;
2. explicit effective time;
3. route ownership convergence for touched capture surfaces;
4. complete maintenance and firearm-detail history states;
5. bounded activity, ammunition, component, and data-quality summaries;
6. traceable dashboard drill-down;
7. responsive, localized, accessible states and focused tests.

Onboarding, spreadsheet import, goals, export/restore, scanning, offline queues, and persisted analytics rollups should remain separately approved follow-up slices.

## Impacted Projects

### Required for the recommended first improvement release

- **MorWalPizVideo.Models** — verify operation and event facts are sufficient; modify only if a typed event fact needed by an approved statistic is genuinely absent.
- **MorWalPizVideo.Domain** — bounded repository queries, deterministic analytics aggregation, reconciliation-aware results, and contextual command convergence.
- **MorWalPiz.Contracts** — additive request/response DTOs for analytics, pagination, typed data-quality states, and any contextual command changes.
- **MorWalPizVideo.ServerAPI** — authenticated `/api/v1/workshop` read endpoints and consistent command entry points.
- **frontend/fe-packages/models** — TypeScript representation of additive Workshop contracts.
- **frontend/fe-packages/services** — centralized Workshop analytics and contextual command transport.
- **frontend/shooter-workshop.client** — capture-flow convergence, dashboard and drill-down refinement, maintenance/history improvements, localization, accessibility, and responsive presentation.
- **MorWalPizVideo.BackOffice.Tests** — Workshop service, replay, API behavior, account isolation, correction, and analytical aggregation coverage.
- **Workshop frontend Vitest suite** — loader/action, form, chart/table alternative, route state, localization, and responsive behavior tests.
- **docs/architecture** — approved feature direction, data/query decisions, operations, deployment evidence, and technical-debt updates.

### Verify only unless implementation evidence requires a change

- **MorWalPizVideo.AppHost** — verify client/API topology and same-origin proxy assumptions.
- **MorWalPizVideo.BackOffice** — central index operational governance only; it must not gain Workshop product workflows.
- **MorWalPizVideo.ServiceDefaults** — verify existing telemetry and health behavior remains sufficient.

### Not impacted

- **MorWalPizVideo.ShootingRange** and `frontend/shooting-range.client` — separate booking application.
- **MorWalPiz.VideoImporter** — no Workshop ownership.
- **Shop, public magazine, Shooting ITA, ShortLinks, FreezerAnalysis, and InsightScanner** — no direct behavior or contract change.

## Impacted Files

The exact implementation file set depends on which recommendations the user approves.

### Modify

- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — additive analytics, drill-down, pagination, and contextual command DTOs.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — deterministic bounded summaries and canonical contextual flow.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — account/time/entity-bounded reads and feature-owned index definitions.
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs` — verify analytics inclusion rules match replay and correction semantics; modify only if a reusable deterministic net-effect helper belongs here.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — authenticated versioned read endpoints and contextual command entry points.
- `frontend/fe-packages/models/src` Workshop model module — additive TypeScript contracts.
- `frontend/fe-packages/services/src` Workshop service module — centralized endpoints and typed transport.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — register route data functions and optional Insights route.
- `frontend/shooter-workshop.client/src/routes/Dashboard` — time-range summary, attention items, and traceable drill-down.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail` — canonical pretargeted shooting and maintenance entry plus complete history state.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions` — explicit effective time, reusable canonical form, repeat-as-draft, correction state, and route ownership.
- `frontend/shooter-workshop.client/src/routes/Maintenance` — effective time, pretargeted entry, typed history, and route ownership.
- `frontend/shooter-workshop.client/src/routes/History` — bounded filters, effective/recorded time, and correction links.
- `frontend/shooter-workshop.client/src/routes/Reconciliation` — typed user-facing data-quality and freshness state.
- `frontend/shooter-workshop.client/src/main.scss` and route-local styles — restrained responsive hierarchy, chart/table alternatives, and state presentation.
- `frontend/shooter-workshop.client/src/i18n` resources — Italian and English copy for every new field, statistic, state, disclaimer, and accessible label.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — bounded summary, account isolation, backdating, and correction cases.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` — statistical net effects remain replay-equivalent.
- existing Workshop frontend test files and route-local test files — capture and summary behavior.
- `docs/architecture/shooter-workshop.md` — only after scope approval and implementation, reconcile accepted target with delivered behavior.
- `docs/architecture/future-improvements.md` — mark approved recommendations and later delivered status.
- `docs/architecture/technical-debt.md` — update query, route ownership, deployment, and reconciliation debt.

### Add

- `frontend/shooter-workshop.client/src/routes/Insights/` using the established route-local `component.tsx`, `loader.ts`, and test naming pattern, only if analytics cannot remain coherent on Dashboard and existing detail routes.
- Route-local reusable reporting components under the Workshop client’s established component organization, only when at least two Workshop surfaces need identical behavior.
- Focused backend test files under `MorWalPizVideo.BackOffice.Tests/Features/` if analytics scenarios would make existing Workshop test files unreadable.
- A new ADR under `docs/architecture/adr/` only if implementation introduces persisted analytical rollups, offline synchronization, restore semantics, or another architectural boundary not covered by ADR-027.

### Generated/Migration

- No MongoDB document rewrite is required for the recommended first release if all statistics are derived from existing immutable facts.
- Central Workshop Mongo index manifest entries may be added for bounded analytical access paths.
- No EF Core migration applies.
- No generated API client is used by the current architecture.

### Documentation

- `docs/feature-handoffs/shooter-workshop-future-improvements-handoff.md` — this user-editable analysis and future implementation boundary.
- Existing architecture and ADR documents listed above, only when the user approves and implementation changes accepted architecture.

### Verify Only

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — verify facts and schema versions cover each statistic before proposing persistence changes.
- `MorWalPizVideo.Models/Constraints/DbCollections.cs` — no new collection expected for phase 1.
- `MorWalPizVideo.ServerAPI/Program.cs` — verify existing DI, auth, CORS, feature flag, and repository registration.
- `frontend/shooter-workshop.client/src/main.tsx` and PWA service worker files — preserve no-API-cache/no-background-sync behavior.
- `frontend/shooter-workshop.client/nginx.conf.template` — preserve same-origin `/api/` behavior.
- `.github/workflows/deploy-shooter-workshop-client.yml` and ServerAPI delivery workflows — verify test and staged deployment evidence.
- `MorWalPizVideo.AppHost/AppHost.cs` — verify local orchestration only.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Additive account-scoped, time-bounded analytical reads; canonical contextual command flow; no persistence entities returned. |
| Frontend/UI | Operate-mode refinement across capture, dashboard, history, maintenance, and optional Insights route; no redesign. |
| Database/Data | Existing immutable facts remain authoritative. Add query indexes if required. No initial document rewrite or analytics database. |
| Contracts/Models | Additive DTOs and TypeScript contracts. Persisted entities change only if a required historical fact is proven absent. |
| Authentication/Authorization | Reuse public-account cookie, CSRF, and server-derived account identity. Every new route remains authenticated and owner-scoped. |
| Validation | Validate date ranges, effective times, quantities, grouping values, pagination, and entity ownership. Server remains authoritative. |
| Cache | Authenticated personal analytics should not use shared output caching. Short-lived client route state may revalidate normally; no service-worker API caching. |
| Configuration/Secrets | No new secret is expected. Optional export storage or telemetry must not introduce committed credentials. |
| Logging/Telemetry | Structured logs for analytical query duration, bounded result counts, reconciliation freshness, validation failures, and command retries; no sensitive payload logging. |
| Background Jobs | Not applicable for phase 1. Add no rollup jobs until measured evidence justifies persisted analytics. |
| Deployment/Operations | Apply reviewed indexes before activating dependent reads; preserve same-origin authentication; collect query and integrity evidence during staged release. |
| Backward Compatibility | Additive APIs and contracts; old clients continue to work. Historical corrections and legacy allocation-bearing operations remain readable. |

## Reuse Opportunities

- `WorkshopOperation` and ADR-027 correction/replay semantics for all round and stock trends.
- `WorkshopReplay` and reconciliation state for data-confidence indicators.
- `WorkshopDashboardContract` and `WorkshopFirearmDetailContract` as existing operational read-model boundaries.
- Firearm Detail as the contextual entry and drill-down surface.
- Components, Ammunition, Recipes, and Firearm Detail loader/action/fetcher patterns for remaining routes.
- Ammunition “record then review” behavior as the interaction precedent for shooting and maintenance.
- Shared public-account transport, unauthorized handling, CSRF, and same-origin proxy behavior.
- Shared frontend model and service packages for contracts and endpoint composition.
- Existing localization provider and culture-neutral API conventions.
- Existing component interval meter and cumulative round visualization as presentation foundations.
- Existing Workshop service and replay test fixtures for corrections, FIFO consumption, attribution, and deterministic rebuild.
- Central MongoDB operational index manifest and BackOffice governance path.

Avoid:

- a second Workshop API client;
- direct `fetch` from route components;
- client-selected batch or installation IDs;
- analytics computed from only current component installation state;
- duplicated chart calculations in multiple components;
- reviving ammunition allocations;
- shared/global state management without demonstrated cross-route pressure;
- background jobs or a separate analytics store without measured need.

## Risks

### High — misleading statistics after corrections

- **Cause:** Aggregating shooting sessions or mutable totals without applying reversal/replacement semantics.
- **Surface:** Every trend, rate, stock number, and component-usage report.
- **Mitigation:** Derive net effects from immutable operations using shared replay semantics; include correction and delete cases in every aggregate test set.
- **Verification signal:** Analytical totals equal rebuilt projection totals for the same account and cutoff across original, corrected, deleted, and replayed histories.

### High — incorrect historical component attribution

- **Cause:** Inferring usage from the currently installed components instead of the installation facts accepted at the session’s effective time.
- **Surface:** Component rounds, interval status, and maintenance forecasts.
- **Mitigation:** Use persisted operation component effects and installation IDs; never re-infer old sessions from current state.
- **Verification signal:** Backdated sessions, moves, removals, and split installations produce deterministic component totals.

### High — privacy leakage through analytics or exports

- **Cause:** Personal inventory, usage cadence, or raw internal records exposed across accounts, logs, caches, URLs, or export files.
- **Surface:** API, telemetry, browser history, saved files, scanning.
- **Mitigation:** Owner identity from auth only, safe DTOs, no shared caching, structured logs without payloads, explicit export warning, opaque scan identifiers.
- **Verification signal:** Cross-account requests fail, logs contain no inventory payloads, and export/scanning threat-model tests pass.

### High — duplicate effects during faster capture

- **Cause:** Quick actions, repeat actions, mobile retries, or network recovery create a new command identity for the same intended save.
- **Surface:** Firearm/component counters and ammunition stock.
- **Mitigation:** Preserve one idempotency key for retries, disable duplicate submissions while pending, and complete firearm-create convergence before reusing it in onboarding.
- **Verification signal:** Repeated identical submissions and timeout retries create one durable effect.

### Medium — analytics query degradation

- **Cause:** Reusing full-account list methods for charts and drill-down.
- **Surface:** Dashboard/API latency and MongoDB load as personal history grows.
- **Mitigation:** Bounded queries, stable indexes, pagination, measured query plans, and result-size limits.
- **Verification signal:** Representative long-history fixtures meet an approved latency and result-size budget without full collection scans.

### Medium — false safety interpretation

- **Cause:** Threshold colors, forecasts, and malfunction rates appear to certify mechanical condition or ammunition safety.
- **Surface:** Maintenance, component, recipe, and dashboard wording.
- **Mitigation:** Factual labels, user-defined threshold attribution, sample sizes, date ranges, and explicit non-advisory copy; no causal or safety labels.
- **Verification signal:** UX/content review finds no “safe,” “unsafe,” “recommended load,” “reliable,” or certified replacement claims.

### Medium — stale or inconsistent analytical results

- **Cause:** Projection reconciliation is pending while a summary is displayed as final.
- **Surface:** Current totals and data-quality cards.
- **Mitigation:** Return reconciliation/freshness state with summaries; show a non-blocking factual warning and direct access to integrity details.
- **Verification signal:** Injected projection failures produce transparent stale/pending UI, and rebuild converges the result.

### Medium — offline queue conflicts

- **Cause:** Deferred mutations replay after component installation, recipe version, or stock changes.
- **Surface:** Mobile PWA capture.
- **Mitigation:** Keep offline changes out of the first release; preserve an unsaved in-memory form and explicit retry only.
- **Verification signal:** Going offline never reports a save or silently queues an account mutation.

### Low — dashboard overload

- **Cause:** Every possible statistic is promoted to the landing page.
- **Surface:** Daily capture speed and mobile usability.
- **Mitigation:** Dashboard shows attention and a small period summary; deeper comparison belongs in Insights/detail routes.
- **Verification signal:** Primary quick actions and attention items remain visible and keyboard/touch reachable at target phone and desktop sizes.

## Technical Debt

### Address now when the related surface is touched

- Canonicalize firearm-detail shooting entry.
- Converge touched routes on loader/action/fetcher ownership.
- Make effective time explicit.
- Complete correction/supersession and pagination presentation.
- Add bounded analytical repository methods rather than extending full-collection materialization.
- Add focused Shooting Sessions and Firearm Detail frontend coverage.

### Complete before relying on the affected feature

- Component swap convergence before presenting swap as a quick action.
- Maintenance history projection before maintenance-frequency drill-down.
- Production index audit/apply evidence before historical inventory analytics release.
- Exact-origin browser authentication and deployment recovery proof before public activation.

### Defer and isolate

- Physical removal of allocation compatibility fields and operations.
- Persisted analytics rollups.
- Offline mutation queues.
- Import/restore.
- QR labels.
- Personal goals.
- Full design-system documentation.

None of these deferred items should be silently included in the first analytics/capture release.

## Implementation Roadmap

1. **Confirm the first-release product brief**
   - Approve the target users, capture scenarios, initial statistics, date ranges, and explicit non-safety language.
   - Use Impeccable `shape` on Dashboard, Firearm Detail, Shooting Sessions, Maintenance, and History.
   - Gate: confirmed mobile and desktop flow, state inventory, and stop boundary.

2. **Lock analytical semantics**
   - Define inclusion rules for effective time, original operations, reversals, replacements, delete corrections, initial balances, archived entities, untracked ammunition, and reconciliation state.
   - Map every displayed value to its immutable source facts.
   - Gate: examples reconcile exactly with ADR-027 replay outcomes.

3. **Complete capture prerequisites**
   - Make firearm creation retry-convergent if onboarding or quick creation uses it.
   - Complete required component swap and maintenance-history behavior.
   - Finish firearm-detail pagination and correction state needed by drill-down.
   - Gate: duplicate retry, backdating, swap, and correction behavior tests pass.

4. **Define additive contracts**
   - Add date-bounded summary, drill-down, pagination, and typed data-quality DTOs in `MorWalPiz.Contracts`.
   - Mirror them in shared TypeScript models.
   - Preserve culture-neutral values and safe public fields.
   - Gate: contract review confirms additive compatibility and account ownership remains server-derived.

5. **Implement bounded repository queries**
   - Query by account, effective time, operation type, aggregate/entity ID, recipe, and batch as required.
   - Add central index definitions for proven query shapes.
   - Gate: representative long-history query plans are bounded and deterministic.

6. **Implement domain summaries**
   - Aggregate activity, ammunition flow, component/maintenance, and data-quality results using shared net-effect semantics.
   - Include applied range and reconciliation freshness.
   - Gate: summaries equal replay-derived expected totals across corrections and backdated events.

7. **Expose authenticated ServerAPI reads**
   - Add responsibility-focused `/api/v1/workshop` reads.
   - Validate ranges, pagination, grouping, and entity ownership.
   - Add structured duration/result-size logging without payload data.
   - Gate: API behavior proves cross-account isolation, safe DTOs, and explicit invalid-input failures.

8. **Converge the canonical shooting flow**
   - Reuse one recipe-aware form from the Sessions route and Firearm Detail.
   - Preselect context, expose effective time, preserve idempotency across retries, and return through safe route paths.
   - Gate: all shooting entry points create equivalent server commands and show the saved result after normal revalidation.

9. **Refine maintenance and history capture**
   - Add pretargeted maintenance entry, effective/recorded time, typed event history, correction links, and clear empty/error/conflict states.
   - Gate: a user can record then immediately verify the resulting factual event on phone and desktop.

10. **Deliver dashboard and analytical drill-down**
    - Keep the Dashboard concise: attention, selected-period activity, ammunition state, and data confidence.
    - Add an Insights route only if the approved information hierarchy cannot remain coherent in existing detail routes.
    - Provide non-visual equivalents for every chart.
    - Gate: every aggregate drills into the matching records and correction-adjusted totals.

11. **Localize, adapt, and harden**
    - Add Italian and English copy, accessible names, keyboard behavior, mobile layout, loading skeletons, validation, stale/conflict, network, and reconciliation states.
    - Preserve no-API-cache/no-background-sync PWA behavior.
    - Gate: Impeccable `adapt`, `harden`, and `audit` evidence plus focused automated checks.

12. **Stage and observe**
    - Review/apply indexes, deploy additively, monitor query latency, result sizes, validation failures, reconciliation warnings, and retry conflicts.
    - Preserve rollback to the existing UI and endpoints; additive reads can remain unused.
    - Gate: deployment, browser-auth, index, and recovery evidence is recorded before broader activation.

13. **Evaluate separately approved follow-ups**
    - Use observed recording friction and report usage to prioritize onboarding, repeat templates, saved views, export, import, labels, goals, or persisted rollups.
    - Gate: each follow-up has its own privacy, compatibility, and stop boundary.

An independent post-validation review is warranted for the first analytics release because correction semantics, cross-account privacy, and historical attribution can produce plausible but wrong results. The user may select `MorWalPiz Code Reviewer` after implementation and validation.

## Testing Strategy

### Domain and unit behavior

Owning project: `MorWalPizVideo.BackOffice.Tests`.

Cover:

- activity totals by effective period;
- original plus positive/negative correction;
- zero/delete correction;
- initial round facts;
- tracked plus untracked ammunition;
- FIFO consumption across recipes and batches;
- production and consumption around range boundaries;
- component move, removal, reinstall, and backdated shooting;
- maintenance before/after the selected range;
- malfunction rate with zero rounds and small samples;
- archived entities;
- pending and failed projection state;
- deterministic ordering at equal timestamps;
- replay equality before and after reconciliation.

### API and contract behavior

Cover:

- authentication required;
- account identity cannot be supplied or overridden by the caller;
- cross-account entity filters return no data or the repository-standard not-found response;
- invalid date order, excessive ranges, invalid grouping, invalid pagination, and unknown entity IDs fail explicitly;
- additive JSON compatibility;
- culture-neutral numeric/date payloads;
- safe DTOs contain no persistence-only or account-internal fields;
- no shared cache behavior on personal results.

### Frontend route and component behavior

Owning workspace: `frontend/shooter-workshop.client`.

Cover:

- Firearm Detail preselects the firearm in the canonical shooting flow;
- recipe quantities and explicit untracked rounds calculate a confirmation total;
- effective time is visible and editable;
- pending submission cannot be duplicated;
- timeout retry reuses the command identity;
- success revalidates and shows the new record;
- validation preserves entered values;
- correction and supersession states are understandable;
- period filters update loader requests;
- chart points and cards link to matching detail filters;
- chart data has a table/text alternative;
- reconciliation warning and last-freshness state;
- Italian and English copy, number/date formatting, and long text;
- mobile cards/tables, keyboard operation, focus return, and semantic status text.

### Integration and behavior tests

Use existing Workshop mock repositories for focused service/API scenarios and the repository’s executable MongoDB test pattern where index/query behavior or independent-writer behavior is material.

Important integrated scenarios:

1. record production, consume by recipe, correct the session, and verify stock/activity summaries;
2. install a component, record shooting, move it, backdate a session, and verify attribution;
3. inject a projection failure, show stale analytical state, reconcile, and verify convergence;
4. submit the same quick-capture command twice and verify one effect;
5. request another account’s entity in an analytics filter and verify isolation.

### Security and privacy checks

- CSRF remains enforced on mutations.
- Exact Workshop origin and cookie behavior are proven in a deployed-like browser environment.
- URLs, client logs, server logs, and telemetry contain no recipe payloads, inventory details, or personal notes.
- Export and scan features, if separately approved, receive dedicated authorization and data-minimization tests.

### Migration and performance checks

- Verify every new query uses the reviewed account/time/entity index shape.
- Use representative high-history personal fixtures rather than empty/demo data.
- Compare analytical totals with replay output.
- Verify old clients ignore additive response fields.
- Verify no existing operation or projection document requires rewriting for phase 1.

### End-to-end acceptance

At minimum, capture visual and interaction evidence for:

- empty first-use dashboard;
- typical account;
- long-history account;
- phone shooting capture from Firearm Detail;
- network failure and retry;
- validation failure;
- corrected/deleted session;
- stale reconciliation state;
- maintenance attention and drill-down;
- Italian and English;
- keyboard-only completion.

## Migration And Rollback

### MongoDB

- Phase 1 is additive and should not rewrite historical Workshop documents.
- Apply reviewed analytical indexes through the central manifest before enabling dependent queries.
- Existing immutable operations, legacy allocation-bearing operations, and projections remain readable.
- If a missing historical fact is discovered, choose an additive tolerant-read and explicit idempotent backfill plan; do not fabricate historical values.
- Rollback removes UI use of the additive endpoints and may remove unused indexes after evidence confirms no consumer depends on them. Existing facts remain unchanged.

### API contracts

- Add new endpoints and fields; do not repurpose existing meanings.
- Keep old capture routes compatible until every Workshop entry point uses the canonical flow.
- The frontend should deploy only after the required API and indexes are available.
- API rollback is safe while new endpoints remain additive; client rollback returns to the existing dashboard and entry flows.

### Authentication and privacy

- No auth migration is required.
- Preserve the account cookie, CSRF behavior, server-derived owner identity, exact-origin CORS, and same-origin proxy.
- Do not activate export, scanning, or persistent browser drafts without separate approval.

### Cache and PWA

- No cache migration is required.
- Keep personal analytical responses out of shared output cache.
- Keep the service worker lifecycle-only.
- Rollback requires no cache purge beyond normal client asset deployment behavior.

### Configuration

- No new secret is expected.
- Optional rollout flags may gate the Insights navigation and canonical Firearm Detail entry if repository-standard feature management is already available to this client/API boundary.
- Do not use an insecure default when a required rollout setting is absent.

### Deployment order

1. approve semantics and UI brief;
2. add/review indexes;
3. deploy additive contracts, queries, summaries, and endpoints;
4. validate deployed-like auth, ownership, and reconciliation behavior;
5. deploy frontend capture convergence and analytics;
6. observe latency, validation, correction, and integrity signals;
7. broaden activation only after recovery evidence is recorded.

### Rollback limits

- UI and additive read endpoints are straightforward to roll back.
- New immutable operations created by users must never be deleted during rollback; old clients and replay must continue to read them.
- A new persisted event schema, rollup, import, restore, or offline queue would need its own forward/rollback compatibility plan and is outside the first-release boundary.

## Open Questions

1. Which first-release statistics matter most to the actual user: shooting volume, ammunition flow, component intervals, maintenance, or data quality?
2. What is the largest realistic per-account history: operations, sessions, batches, components, and years retained?
3. Is recording primarily phone-first at the range, desktop-first in the workshop, or evenly split?
4. Should effective time capture minute precision, or is date-only sufficient for most operations while preserving deterministic ordering internally?
5. Which maintenance event types count as “since last maintenance” for each component, and should that mapping remain user-selected?
6. Should malfunction rate be included in the first release, given the current model has no cause or resolution state?
7. Should analytics live as a dedicated Insights route, or should the first release limit itself to Dashboard plus existing detail routes?
8. Is full account export wanted as a report, a portable backup, or both? These require different compatibility and security designs.
9. Is spreadsheet import important enough to justify ledger-backed initial-balance and idempotent resume behavior?
10. What latency and data-volume budgets should become release gates after representative production-scale data is measured?
