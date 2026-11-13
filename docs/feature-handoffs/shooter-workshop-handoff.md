# Shooter Workshop Architecture Handoff

## Outcome

## Verification follow-up — 2026-11-13

The Workshop baseline received focused correctness fixes for replacement shooting
corrections, maintenance idempotency recovery, dashboard stock reporting, and
render-phase account redirects. Replacement corrections now validate replacement
quantities against the replay state after reversing the original operation.
Maintenance retries verify the idempotency payload and restore a missing maintenance
record when the ledger operation already exists. The SPA dashboard now consumes the
server-computed dashboard projection rather than reconstructing available stock from
batch totals.

Validation completed:

- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~WorkshopReplayTests"` — 3 passed.
- `cd frontend && yarn workspace shooter-workshop.client build` — passed.
- `cd frontend && yarn workspace @morwalpizvideo/services test --run src/workshopService.test.ts` — 5 passed.

The broader Workshop acceptance scope remains incomplete: firearm edit/archive and
allocation release/reallocation and shooting-correction user flows are not yet
exposed by the SPA.

Deliver a new private React application for personal firearm, physical-component,
maintenance, ammunition and shooting-history management. It is a standalone Yarn
workspace that reuses the structure, visual identity and genuinely shared components of
`frontend/morwalpizvideo.client`, while `MorWalPizVideo.ServerAPI` owns all Workshop API
behavior and MongoDB access.

The complete `docs/architecture/shooter-workshop.md` specification is the functional
baseline. All listed domains, workflows, acceptance scenarios and definition-of-done
items form one release scope. Work may be implemented in dependency order, but there is
no reduced MVP, deferred product domain or partially enabled first release.

## Implementation Status — delivery integration

The current working tree now contains the Workshop implementation baseline and its
delivery integration: ServerAPI controllers and public-account CORS, Domain replay and
reconciliation services, Models/Contracts, the standalone React workspace and account
transport, local Aspire orchestration, central CI matrix coverage, Workshop SPA image
staging, the dedicated `workshop.morwalpiz.com` workflow, and ADR-027.

The feature-owned Workshop index definitions are now included in the executable
BackOffice `MongoIndexOperationsService.Manifest` through the existing Domain-owned
manifest. The reviewed JSON operations snapshot records the same five definitions.
This is manual audit/apply governance; Workshop indexes are not added to the limited
startup auto-bootstrap path. BackOffice remains outside Workshop product behavior:
its only coupling is operational index audit/apply, with no Workshop UI, product
workflow, authorization boundary, or domain ownership.

Production credentialed CORS admits exactly `https://workshop.morwalpiz.com`. Local
AppHost exposes the client explicitly at `http://localhost:5179` and uses the existing
Development CORS behavior. The full product specification is not yet release-certified:
complete domain acceptance coverage, browser authentication proof, duplicate/index
audits and apply evidence, Azure custom-domain/TLS/runtime configuration, and release
activation remain open. No secrets or deployed values were added.

Delivery files changed in this stream:

- `MorWalPizVideo.AppHost/Program.cs`
- `.github/workflows/ci.yml`
- `.github/workflows/deploy-shooter-workshop-client.yml`
- `frontend/package.json`
- `frontend/shooter-workshop.client/package.json`
- `frontend/shooter-workshop.client/Dockerfile`
- `frontend/shooter-workshop.client/nginx.conf`
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`
- `MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs`
- `docs/architecture/operations/mongo-index-manifest.phase4.json`
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`
- `MorWalPizVideo.Domain/Workshop/{WorkshopRepository,WorkshopReplay,WorkshopService}.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `frontend/fe-packages/models/src/workshop.ts`
- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/shooter-workshop.client/src/...`
- `docs/architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md`
- `docs/architecture/{frontend,backend,api-design,domain-model,deployment,security,mongo-operations,architecture-decisions,shooter-workshop}.md`

Assumptions:

- "Subdomain `/workshop`" means the canonical origin
  `https://workshop.morwalpiz.com`, mounted at `/`.
- Existing public accounts are the Workshop identity; no separate Workshop user store
  and no BackOffice user/RBAC identity are introduced.
- Workshop data is private to the authenticated public account.
- BackOffice has no Workshop UI, API workflow or product-management responsibility.
  Its Mongo index manifest may still require an operational-only update.
- The new surface is an Impeccable **Operate** experience. This is a brand-preserving
  application refinement/extension, not a redesign of the MorWalPiz visual system.

## Current Evidence

### Frontend

- `frontend/package.json` defines a Yarn Classic workspace and ordered shared builds for
  `@morwalpizvideo/models`, `@morwalpizvideo/services` and `@morwalpiz/layout`. It has no
  Workshop workspace.
- `frontend/morwalpizvideo.client/package.json` verifies the incumbent React 19, React
  Router, Vite, TypeScript, React Bootstrap, shared-package and test stack.
- `frontend/morwalpizvideo.client/src/routes/config.tsx` owns route composition through
  a React Router route tree with route-local loaders and components.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx` composes the public shell,
  account provider, navigation providers, title, outlet and footer.
- `frontend/morwalpizvideo.client/DESIGN.md` is the current visual authority: Manrope,
  dark graphite surfaces, white text and selective `#FFFF36` signal-yellow actions and
  state.
- `frontend/fe-packages/layout/src/index.ts` exports reusable shell, header, footer,
  page-title, empty-state and presentation components. App-local routes and business
  workflows are not currently shared.
- `frontend/fe-packages/services/src/apiTransport.ts` already provides an isolated
  `account` transport mode that includes cookies and CSRF while removing bearer and
  channel context.
- `frontend/fe-packages/services/src/publicAccountService.ts` owns public-account
  registration, login, session restoration, profile and logout calls.
- `frontend/fe-packages/models/src/publicAccount.ts` owns the matching TypeScript account
  contracts.
- `.github/workflows/ci.yml` and the dedicated Workshop deployment workflow now
  enumerate `shooter-workshop.client` for tests, builds and image staging.

### Backend and data

- `MorWalPizVideo.ServerAPI/Program.cs` configures the public-account authentication
  scheme, credentialed CORS, MongoDB, mock/production repository branches and service
  registrations.
- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs` validates
  the HttpOnly public-account session cookie, emits the account ID as the name-identifier
  claim and defines the CSRF cookie/header contract.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` demonstrates
  versioned `api/v1` routes, explicit public-account authentication and CSRF-protected
  unsafe operations.
- `MorWalPizVideo.ServerAPI/Controllers/BlogInteractionsController.cs` demonstrates the
  existing account-owned request flow: claim-derived account ID, explicit account
  scheme, CSRF on mutation and downstream ownership filtering.
- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs` and the existing public
  account repository own session validation and account persistence.
- `MorWalPizVideo.Models/Constraints/DbCollections.cs` centralizes MongoDB collection
  names.
- `MorWalPizVideo.Domain/BlogInteractions/BlogInteractionMongoIndexStartupService.cs`
  demonstrates feature-owned stable index definitions and startup verification.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs` is the current
  central index audit/apply surface, including ServerAPI-owned account and interaction
  collections. This is operational coupling, not a BackOffice product dependency.
- `MorWalPizVideo.BackOffice.Tests/Infrastructure/ServerApiWebApplicationFactory.cs`
  starts ServerAPI with mock repositories; new controller dependencies must be
  registered in both production and mock branches.
- Workshop models, contracts, controller, Domain service/repository/replay,
  frontend workspace, account transport and deployment artifacts are present in the
  current working tree. Their full functional and production release gates remain
  incomplete.
- No MongoDB transaction convention was found in the current ServerAPI/Domain path.
  Cross-document atomicity must not be assumed.

### Current-state flow to reuse

The applicable identity flow is:

1. Public-account registration or login creates a persisted account session.
2. The browser receives the HttpOnly account cookie.
3. ServerAPI authenticates the cookie with the explicit public-account scheme.
4. The controller obtains `ClaimTypes.NameIdentifier`.
5. The service and repository receive that claim-derived account ID.
6. Every Workshop query and mutation filters by that account ID.

The request must never accept an owner account ID as authority from the client.

## Proposed Design

### Application and hosting boundary

Create `frontend/shooter-workshop.client` as a separate React 19/Vite workspace and
deployment unit. It calls the existing ServerAPI host through
`@morwalpizvideo/services` account transport. It is not a route inside
`morwalpizvideo.client`, does not share runtime state with that application and does not
reference its app-local source files.

Use a client-rendered SPA. The product is a private operational tool and has no SEO
requirement, so public-client SSR is not required merely for visual or structural reuse.
Retain the public client's route configuration, shell composition, error-boundary,
styles/tokens, responsive and accessibility conventions. Add PWA behavior only for
installability and static-shell updates; authenticated API responses and mutation
queues are not cached offline unless separately approved.

The Workshop route tree contains protected application routes for:

- dashboard;
- firearms and firearm details;
- component catalogue and physical-component inventory;
- maintenance;
- ammunition inventory, batches and allocations;
- recipes and immutable recipe history;
- production sessions;
- shooting sessions and corrections;
- chronological history;
- account login, registration and profile.

All domain routes require a restored public-account session. Login, registration and
legal routes are the only anonymous browser surfaces.

### Frontend reuse rule

"Reuse the same component and structure" means:

- consume `@morwalpiz/layout`, `@morwalpizvideo/models` and
  `@morwalpizvideo/services`;
- preserve the incumbent design tokens and shell hierarchy;
- follow route-local `Component`, loader/action and error-state organization;
- extract a public-client presentation component into `@morwalpiz/layout` only when
  both applications genuinely use the same semantics;
- keep Workshop forms, counters, reconciliation state and domain workflows local to
  the new application.

Do not import files directly from `frontend/morwalpizvideo.client/src`, and do not copy
shared account transport logic into a second implementation. The account route provider
and route guards remain app-owned because they compose application navigation; reusable
form and shell presentation can move into the layout package if both clients retain the
same behavior.

### Impeccable design direction

The target is **Operate** mode: frequent, accuracy-sensitive actions with dense but
scannable data. Use the incumbent `DESIGN.md` as visual authority. This is a refinement
and functional expansion, not a redesign.

The implementation workflow should apply:

- `shape` to the dashboard, shooting-session entry, component movement, production and
  reconciliation flows before UI implementation;
- `adapt` to prove desktop, tablet, 480px and 360px behavior;
- `harden` for empty, loading, stale, conflict, projection-pending, reconciliation,
  validation and network-failure states;
- `audit` for accessibility, responsive overflow and performance before release.

Minimum visual evidence is one desktop and one mobile capture for every primary
workflow, including empty, populated, validation-error and reconciliation-warning
states. Tables must become usable card/scroll patterns without page-level horizontal
overflow. All actions require keyboard access, visible focus, associated error text and
touch-safe targets.

### API boundary

Add versioned ServerAPI controllers under `/api/v1/workshop`. Use explicit Contracts
DTOs and the public-account authentication scheme on every protected controller. Unsafe
actions require the existing account CSRF contract.

Group endpoints by domain ownership rather than exposing generic CRUD:

- dashboard/read projections;
- firearms and archive;
- component definitions and physical instances;
- install, remove, retire and maintenance commands;
- recipes, immutable versions and archive;
- production and batch reads;
- allocations, releases and reallocations;
- shooting-session record, correction and read;
- activity history;
- reconciliation status and account-owned rebuild commands.

Controllers validate shape and authorization metadata. A focused Workshop application
service owns cross-document invariants, idempotency, historical effects, correction and
replay. Repositories own account-filtered MongoDB queries, atomic single-document
operations, bounded paging and stable ordering. Persistence entities never leave the
API.

### Aggregate and history model

The current-state aggregates are independent:

1. **Firearm aggregate**: identity, profile, archive state and projected total rounds.
2. **Physical component aggregate**: definition reference, lifecycle status,
   installation state, projected total rounds and service configuration.
3. **Ammunition batch aggregate**: production origin, immutable recipe snapshot,
   projected on-hand stock and allocation projection.

Supporting independently managed records include component definitions, installation
history, maintenance events, immutable recipe versions, production sessions, shooting
sessions and allocation state.

Histories must not be unbounded arrays on the aggregate documents. Recipe data used for
production remains embedded as an immutable snapshot on the production/batch fact.
Shooting-session consumption and usage segments may be embedded in the immutable
session fact because they belong to one bounded operation.

### Counter reconciliation: primary integrity design

Firearm rounds, physical-component rounds and ammunition stock are related but
independent projections. None is calculated from another projection, and a matching
number across two projections is not proof of consistency.

Introduce an immutable, account-owned Workshop operation ledger. One accepted command
creates one durable operation bundle with:

- operation/event ID and client idempotency key;
- account ID derived from authentication;
- event type and schema version;
- effective time and recorded time;
- source entity IDs and causal/correlation identity;
- original-operation and replacement-operation links for corrections;
- typed firearm round deltas;
- typed physical-component round deltas;
- typed ammunition stock deltas;
- typed ammunition allocation deltas;
- shooting-segment, installation and batch references needed to explain every delta.

A shooting-session bundle records:

- total firearm rounds;
- bounded usage segments whose sum equals the session total;
- the exact physical component IDs and installation IDs attributed in each segment;
- known batch-consumption quantities;
- an explicit untracked-ammunition quantity;
- allocation references where allocated stock is consumed.

The invariant is:

`known batch consumption + untracked ammunition = session total`.

Component attribution is captured in the accepted operation. It may be initially
derived from installation history, but replay never reinterprets an old session using a
new current installation. If equipment changes during a session, the user must provide
segments. An ambiguous or overlapping installation timeline is rejected rather than
guessed.

Stock is reconstructed as production plus explicit stock adjustments and correction
deltas minus consumption. Allocation is reconstructed separately from allocation,
release, reallocation and allocated-consumption deltas. Allocation does not change
on-hand stock. Consuming an allocation reduces both on-hand stock and that allocation;
direct unallocated consumption reduces on-hand and available stock without inventing an
allocation.

### Projection and command processing

The operation ledger is authoritative. Aggregate totals and dashboard values are
rebuildable projections.

For each mutation:

1. Authenticate and derive the account ID.
2. Validate ownership, aggregate versions, installation timeline, batch quantities and
   the complete cross-aggregate effect set.
3. Resolve the idempotency key. A repeated key with the same payload resumes or returns
   the original result; a repeated key with a different payload is a conflict.
4. Append the immutable operation bundle exactly once.
5. Recalculate every affected aggregate from its ledger facts and replace its projection
   using optimistic concurrency.
6. Record projection/reconciliation status and return success only when affected
   projections are current. If the event is durable but a projection fails, expose an
   explicit recoverable pending/inconsistent state; never return a success-shaped
   response.

Recalculation, rather than blind counter increments, makes retries idempotent without an
unbounded list of applied event IDs on each aggregate. Personal-account scale makes
affected-aggregate replay acceptable initially. Repository queries must still be
indexed and bounded by account plus aggregate identity.

A reconciliation service independently replays:

- each firearm round stream;
- each physical component usage stream;
- each batch stock stream;
- each batch allocation stream.

It compares rebuilt values and a deterministic contribution digest with current
projections. Repair replaces only projections; it never edits ledger facts. Reconciliation
results are account-owned and observable in the Workshop UI. A full account replay is a
support/recovery operation, while normal commands replay only affected aggregates.

### Corrections

Historical correction appends a compensating operation that reverses the complete
original effect bundle, followed by a replacement bundle when appropriate. The original
session, production, allocation or maintenance record remains visible and linked to its
correction.

A correction cannot modify only one of the three counter families when the original
operation affected more. The service derives the reversal from the stored original
bundle and then validates the replacement as a new complete command. Repeating the same
correction is idempotent.

Manual counter adjustments are explicit ledger events with reason, affected aggregate
and audit timestamps. They are never direct edits to projected numeric fields.

### Concurrency and indexes

Every mutable aggregate has a revision used for optimistic concurrency. Concurrent
component moves, batch allocations, consumption and corrections return conflict rather
than allowing last-write-wins data loss.

Required indexes include:

- unique account ID plus client idempotency key for operation bundles;
- account ID plus aggregate kind/ID plus effective/recorded ordering for replay;
- account ID plus active firearm/component/recipe lookup fields;
- account ID plus component installation interval queries;
- account ID plus batch and allocation lookup;
- account ID plus activity date/type pagination;
- uniqueness needed to prevent two active installations for one component, enforced
  with service validation plus the strongest compatible index.

Index definitions use stable names, are verified before writes are enabled and appear in
the repository's central operational index manifest. Duplicate audit precedes any unique
index application.

### Cache and observability

Do not output-cache authenticated Workshop responses. Derived counters live in MongoDB
projections, not in the existing output-cache abstraction. Dashboard reads may use
short-lived account-partitioned caching only after reconciliation behavior is proven;
cache is not part of the first implementation design.

Structured logs include operation ID, account-safe correlation identity, event type,
affected aggregate kinds, projection status, reconciliation mismatch kind and duration.
Do not log recipe values, personal notes, account email or firearm-identifying free
text. Metrics cover accepted commands, idempotent retries, optimistic conflicts,
projection failures, reconciliation mismatches, replay duration and repair outcome.

## Impacted Projects

| Project/surface | Impact |
|---|---|
| `frontend/shooter-workshop.client` | New React SPA, routes, Operate shell, domain workflows and tests. |
| `frontend/fe-packages/models` | Shared Workshop request/response TypeScript DTOs. |
| `frontend/fe-packages/services` | Versioned Workshop endpoint functions using account transport. |
| `frontend/fe-packages/layout` | Reuse existing shell components; extract only presentation shared by public and Workshop clients. |
| `frontend/morwalpizvideo.client` | Possible narrow import updates if an incumbent presentation component is extracted; no Workshop routes or state. |
| `MorWalPizVideo.Models` | Persistence entities, embedded event effects, enums, collection names and index-compatible fields. |
| `MorWalPiz.Contracts` | Explicit API request/response, paging, conflict and reconciliation contracts. |
| `MorWalPizVideo.Domain` | Workshop application service, repositories, mock implementation, ledger replay, reconciliation and index definitions. |
| `MorWalPizVideo.ServerAPI` | Controllers, DI, public-account authorization/CSRF use and Workshop-origin CORS. |
| `MorWalPizVideo.BackOffice.Tests` | ServerAPI integration, security, service, replay, repository and operational-index tests. |
| `MorWalPizVideo.BackOffice` | No Workshop product behavior; operational index manifest only if current governance is retained. |
| `MorWalPizVideo.AppHost` | Local orchestration for the new frontend connected to existing ServerAPI. |
| GitHub Actions | CI matrix plus dedicated Workshop image/deployment workflow. |
| Azure/DNS | New frontend application/container, `workshop` DNS, TLS, runtime API URL and exact credentialed origin. |
| BackOffice SPA | Not impacted. |
| ShortLinks, Shooting Range, VideoImporter, InsightScanner, shop and other SPAs | Not impacted except shared-package consumer verification where applicable. |

## Impacted Files

### Modify

- `frontend/package.json` — add the Workshop workspace and development/build scripts.
- `frontend/yarn.lock` — capture the new workspace dependency graph.
- `frontend/fe-packages/models/src/index.ts` — export Workshop DTOs.
- `frontend/fe-packages/services/src/index.ts` and endpoint exports — expose Workshop
  service functions.
- `frontend/fe-packages/layout/src/index.ts` — only if a proven shared presentation
  component is extracted.
- `frontend/morwalpizvideo.client/src/...` — only the imports required by an approved
  extraction; no Workshop behavior.
- `MorWalPizVideo.Models/Constraints/DbCollections.cs` — centralized Workshop collection
  names.
- `MorWalPizVideo.ServerAPI/Program.cs` — services, repositories, index verification and
  exact Workshop CORS origin.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs` — operational
  Workshop index manifest, not product behavior.
- `MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs` — verify the
  central manifest includes feature-owned Workshop definitions.
- `MorWalPizVideo.AppHost/Program.cs` — orchestrate the new frontend against ServerAPI.
- `.github/workflows/ci.yml` — include Workshop tests, build and image validation.

### Add

- `frontend/shooter-workshop.client/` — app manifest, Vite config, Docker packaging,
  static assets, design context, route tree, shell, domain features and tests.
- `frontend/fe-packages/models/src/workshop.ts` — frontend API shapes.
- `frontend/fe-packages/services/src/workshopService.ts` — typed account-transport calls.
- `MorWalPizVideo.Models/Models/Workshop/` — persistence aggregates and immutable typed
  operation effects.
- `MorWalPiz.Contracts/Contracts/Workshop/` — versioned API DTOs.
- `MorWalPizVideo.Domain/Workshop/` — service, repository contracts, Mongo/mock
  implementations, replay/reconciliation and index definitions.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/` — versioned authenticated controllers.
- `MorWalPizVideo.BackOffice.Tests/Features/Workshop/` — focused behavior and
  integration tests.
- `.github/workflows/deploy-shooter-workshop-client.yml` — test, build, image and deploy
  the dedicated SPA.
- `docs/architecture/adr/` Workshop ledger/reconciliation ADR — preserve the primary
  integrity decision beyond implementation.

Exact feature filenames inside the new directories should follow the selected
feature-local convention; they are not invented here where no current Workshop source
exists.

### Generated/Migration

- Yarn lockfile changes are generated by the workspace dependency update.
- MongoDB has no EF-style generated migration. New collections are created through
  first writes, and reviewed index definitions are audited/applied explicitly.
- No existing Workshop data backfill exists for the initial release.

### Documentation

- `docs/architecture/shooter-workshop.md` — functional baseline and critical
  reconciliation direction.
- `docs/feature-handoffs/shooter-workshop-handoff.md` — this implementation source of
  truth.
- `docs/architecture/frontend.md` — new workspace, authentication mode and Operate
  ownership.
- `docs/architecture/backend.md` — ServerAPI Workshop boundary and replay service.
- `docs/architecture/domain-model.md` — aggregate, ledger and projection ownership.
- `docs/architecture/api-design.md` — `/api/v1/workshop` authentication, CSRF and error
  contract.
- `docs/architecture/deployment.md` — subdomain, image, runtime configuration and release
  gates.
- `docs/architecture/security.md` — private account ownership and CORS boundary.
- `docs/architecture/architecture-decisions.md` — link the new ADR.

### Verify Only

- `frontend/morwalpizvideo.client/DESIGN.md` — incumbent visual authority.
- `frontend/morwalpizvideo.client/src/routes/config.tsx` and
  `src/routes/layout/root.tsx` — structure to follow, not cross-import.
- `frontend/fe-packages/services/src/apiTransport.ts` and
  `publicAccountService.ts` — reuse account transport and session behavior.
- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs` — preserve
  cookie/CSRF semantics.
- `.github/workflows/deploy-serverapi.yml` — verify existing shared-project triggers and
  public-account CORS smoke checks cover the new origin as intended.
- `MorWalPizVideo.ShootingRange/` and `frontend/shooting-range.client/` — confirm they
  remain unrelated booking-POC applications.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | New versioned, public-account-protected Workshop controllers and focused domain service in ServerAPI. |
| Frontend/UI | New Operate-mode React workspace with complete dashboard, equipment, maintenance, ammunition, session and history surfaces. |
| Database/Data | New account-owned collections, immutable operation ledger, rebuildable projections, stable indexes and reconciliation status. |
| Contracts/Models | New persistence models plus separate explicit .NET and TypeScript API DTOs. |
| Authentication/Authorization | Existing public-account cookie scheme; exact Workshop origin in credentialed CORS; claim-derived ownership on every query. |
| Validation | Shape checks at DTO boundary; cross-aggregate invariants, optimistic revisions and idempotency in the Workshop service. |
| Cache | No authenticated output caching; current totals are persisted projections, not cache entries. |
| Configuration/Secrets | New public frontend origin and runtime API base URL only; no new credential type or committed secret. |
| Logging/Telemetry | Correlated operation/projection/reconciliation metrics without personal notes or recipe details. |
| Background Jobs | No Hangfire dependency. A ServerAPI-owned durable reconciliation/catch-up process may process ledger-backed pending projections; manual repair is not BackOffice-owned. |
| Deployment/Operations | New SPA image/app/subdomain/TLS; additive backend first, then frontend, CORS and one release activation gate. |
| Backward Compatibility | Existing public API and public client remain unchanged. New collections and routes are additive. |

## Reuse Opportunities

- Public-account authentication, CSRF, session restoration and claim identity.
- `accountApiService` and shared error parsing from `@morwalpizvideo/services`.
- Shared public-account TypeScript contracts and .NET account contracts.
- `@morwalpiz/layout` shell, header, footer, page title, empty state and visual utilities.
- Public-client design tokens, responsive shell and route/error organization.
- React Router loaders/actions/fetchers for route-owned server state.
- `BaseEntity`, centralized collection constants and feature-local Mongo repositories.
- Blog interaction controller/service/repository patterns for account ownership.
- Feature-owned index definition/startup verification and the central index audit.
- ServerAPI test factory, test authentication, mock repository registration and host
  security-containment tests.

Avoid:

- importing public-client app-local source into the new workspace;
- accepting account IDs from request bodies;
- generic CRUD over the counter-changing commands;
- direct numeric counter edits;
- using one aggregate projection as the source for another;
- introducing BackOffice RBAC, channel scope or API keys into the Workshop.

## Risks

### High — Cross-aggregate counter divergence

- **Cause:** one shooting/correction command can affect firearm rounds, multiple physical
  components, multiple ammunition batches and allocations without an existing
  cross-document transaction convention.
- **Surface:** all counters, maintenance warnings, stock, dashboard and history.
- **Mitigation:** immutable complete operation bundles, idempotency, effect replay,
  compensating corrections, projection status and explicit reconciliation.
- **Verification signal:** forced failure after ledger append and between projection
  writes is repaired by retry/replay; all three reconstructed aggregate families match
  their projections and contribution digests.

### High — Incorrect historical component attribution

- **Cause:** backdated sessions, overlapping installation records or component changes
  during one session.
- **Surface:** component lifecycle and maintenance thresholds.
- **Mitigation:** bounded shooting segments with exact component and installation
  snapshots; reject ambiguous timelines; never replay against current installation.
- **Verification signal:** moving a component and correcting a backdated split session
  reproduces each physical component's expected independent total.

### High — Account isolation or credentialed-origin failure

- **Cause:** default JWT authorization used accidentally, caller-supplied owner ID, or
  missing exact Workshop CORS origin.
- **Surface:** all private Workshop data and production login.
- **Mitigation:** explicit public-account scheme, claim-derived owner, account filters,
  CSRF on unsafe calls and production cross-origin browser smoke tests.
- **Verification signal:** cross-account IDs return concealed not-found/forbidden
  behavior; Workshop origin can restore session and mutate with CSRF; unapproved origins
  cannot.

### Medium — Ammunition oversubscription

- **Cause:** concurrent allocation, release, reallocation and consumption against the
  same batch.
- **Surface:** on-hand, allocated and available stock.
- **Mitigation:** aggregate revisions, atomic conditional replacement, separate stock
  and allocation deltas and conflict responses.
- **Verification signal:** concurrent tests never produce negative on-hand/available
  stock or allocation above on-hand stock.

### Medium — Idempotency collision or duplicate correction

- **Cause:** network retry after the authoritative operation was committed.
- **Surface:** every projection.
- **Mitigation:** unique account/idempotency-key index plus normalized payload digest.
- **Verification signal:** identical retry returns the prior outcome; different payload
  on the same key returns conflict; correction contribution is applied once.

### Medium — Public-account production readiness

- **Cause:** architecture documentation records a deployed CSRF endpoint discrepancy for
  the current public-account feature.
- **Surface:** Workshop authentication in production.
- **Mitigation:** treat real-browser account CSRF/session/CORS proof as a release gate.
- **Verification signal:** anonymous CSRF bootstrap, login, session restore, mutation and
  logout succeed from the Workshop origin with expected cookie headers.

### Medium — Central index operational coupling

- **Cause:** BackOffice currently audits/applies indexes for ServerAPI-owned collections.
- **Surface:** deployment sequencing, not Workshop user behavior.
- **Mitigation:** feature-owned stable index definitions incorporated by the central
  manifest and tested once.
- **Verification signal:** index audit reports the intended names/keys/options without
  duplicate conflicts before enabling writes.

### Low — Shared-component overreach

- **Cause:** extracting Workshop-specific workflows into the global layout package in
  the name of reuse.
- **Surface:** unrelated frontend consumers.
- **Mitigation:** extract presentation only after two consumers have identical behavior;
  keep domain flows local.
- **Verification signal:** shared package consumer builds pass and no unrelated route
  gains Workshop state or dependencies.

### Low — External-service risk

None identified. The specified product has no new external firearm, ammunition or
calculation provider. Existing authentication/database hosting remain operational
dependencies.

## Technical Debt

- **BackOffice index governance for ServerAPI data:** does not block the feature, but it
  creates operational coupling. Isolate Workshop product code from BackOffice and limit
  the change to index manifest/tests. A separate index-governance refactor is deferred.
- **Public-account deployed CSRF discrepancy:** blocks production activation if still
  present. Resolve or verify it as part of the release gate; do not create a second
  authentication system.
- **Public-client account UI is app-local:** does not justify cross-workspace imports.
  Extract only reusable presentation during implementation or keep thin app-owned
  wrappers over shared account services.
- **Shared services `get` API is not generic:** use explicit Workshop service return
  types and runtime response guards where established; a repository-wide transport
  redesign is deferred.
- **No established Mongo multi-document transaction path:** the ledger/replay design
  intentionally avoids depending on one. Adding generic transaction infrastructure is
  not required.
- **Existing public-client SSR server has broad fallback behavior:** not relevant to the
  private SPA and must not be copied as incidental structure.

None of this debt authorizes reducing the functional baseline.

## Implementation Roadmap

1. **Ratify the ledger and release contract**
   - Add the Workshop ADR covering authoritative operation bundles, independent
     projections, corrections, reconciliation and the no-partial-release gate.
   - Confirm the canonical host assumption and document exact credentialed origin.
   - Validation gate: the ADR can reconstruct firearm, component, stock and allocation
     examples without reading a current projected counter.

2. **Define complete contracts and persistence model**
   - Add persistence entities, typed operation effects, immutable recipe/session
     snapshots, revisions and collection constants.
   - Add explicit API and TypeScript contracts for every domain in the TPS.
   - Include idempotency, conflict, paging, reconciliation and correction shapes.
   - Validation gate: contract review maps every TPS field, invariant, scenario A-J and
     definition-of-done item to an owning request/response or stored fact.

3. **Implement repository, indexes and replay primitives**
   - Add account-filtered Mongo and mock repositories.
   - Add stable index definitions and central audit incorporation.
   - Implement per-aggregate replay and deterministic contribution digests.
   - Validation gate: repository tests prove ownership, ordering, concurrency,
     idempotency uniqueness and complete replay from ledger facts.

4. **Implement the Workshop application service**
   - Add commands for equipment, installation, maintenance, recipes, production,
     allocation, shooting and correction.
   - Append complete operations and rebuild affected projections.
   - Expose explicit pending/inconsistent status and account-owned reconciliation.
   - Validation gate: fault-injection and retry tests prove that a durable operation can
     never become an unrecoverable partial counter update.

5. **Expose the complete ServerAPI surface**
   - Add versioned controllers, DTO validation, account authentication, CSRF,
     Problem Details and bounded queries.
   - Register production/mock repositories and services.
   - Add the exact Workshop CORS origin without broad wildcard credentials.
   - Validation gate: host-security and integration tests cover every action, ownership,
     CSRF, conflicts and negative paths.

6. **Create the frontend workspace and shared API layer**
   - Add the new workspace, shared models/services exports, route shell, account
     restoration and protected routing.
   - Extract only proven shared presentation from the public client.
   - Validation gate: shared packages and every consumer compile; direct source imports
     from the public client do not exist.

7. **Build every functional domain**
   - Implement dashboard, firearms, components, maintenance, recipes, production,
     inventory, allocation, shooting, correction and history.
   - Preserve domain independence while linking contextual navigation.
   - Surface projection-pending and reconciliation mismatch states with recoverable
     actions; never mask them as ordinary success.
   - Validation gate: all specification workflows are reachable and scenarios A-J pass
     through the UI/API boundary.

8. **Harden the Operate experience**
   - Apply the Impeccable `shape`, `adapt`, `harden` and `audit` workflow described
     above.
   - Verify realistic minimum, typical and maximum lists; loading, empty, stale,
     validation, conflict, network and reconciliation states.
   - Validation gate: desktop/tablet/mobile evidence, keyboard traversal, focus,
     semantic labels, contrast and no page-level horizontal overflow.

9. **Wire local orchestration, CI and deployment**
   - Add AppHost frontend wiring, CI matrices, container validation and dedicated
     frontend deployment.
   - Provision the Workshop host, TLS and runtime ServerAPI URL.
   - Keep all Workshop routes disabled/unpublished until the integrated release gate is
     satisfied.
   - Validation gate: production-like browser authentication and API smoke tests succeed
     from the exact Workshop origin.

10. **Run integrated reconciliation and release gates**
    - Seed representative complete histories, force partial projection failures, replay
      all aggregates and compare deterministic results.
    - Execute full functional, security, responsive and recovery tests.
    - Update this handoff with actual files, decisions, validation and residual risks.
    - Stop condition: release only when the complete TPS is implemented; any omitted
      domain keeps the application unpublished.

An independent post-validation review is warranted because the feature introduces
private account data, cross-aggregate financial-like stock accounting, correction
semantics, new deployment/CORS behavior and a new frontend. The user should select
`MorWalPiz Code Reviewer` after implementation and validation.

## Testing Strategy

### Unit and domain tests

Owning project: `MorWalPizVideo.BackOffice.Tests` feature tests, with pure replay logic
tested without HTTP.

- Rebuild firearm totals from shooting and correction effects.
- Rebuild each physical component independently after moves between firearms.
- Rebuild batch on-hand, allocated and available quantities.
- Verify maintenance percentages from projected component totals.
- Verify recipe snapshots remain unchanged after recipe edits.
- Verify known consumption plus explicit untracked quantity equals session total.
- Reject negative quantities, ambiguous installations, overlapping active
  installations, over-allocation and over-consumption.
- Prove correction inversion and replacement are deterministic.
- Prove duplicate idempotency keys and payload-digest conflicts.

### Integration and behavior tests

Owning project: `MorWalPizVideo.BackOffice.Tests` with
`ServerApiWebApplicationFactory`.

- Every controller action has explicit public-account access metadata.
- Unsafe actions reject missing/invalid CSRF.
- Account A cannot read, mutate, reconcile or infer Account B records.
- Production plus allocation plus multi-batch consumption updates the correct
  projections.
- Untracked ammunition updates firearm/components but not stock.
- Component move retains prior usage and attributes later sessions to the new
  installation.
- Fault after ledger append, after one projection and before response is recoverable by
  the same request/reconciliation.
- Concurrent batch and component commands produce conflict, not lost updates.

## Backend delivery handoff (2026-11-13)

The bounded backend stream implemented the persistence and contract foundation plus a
ledger-backed core vertical slice. Added `WorkshopEntities.cs` and Workshop collection
constants in `MorWalPizVideo.Models`; added explicit Workshop request/operation/
reconciliation contracts in `MorWalPiz.Contracts`; added
`WorkshopRepository.cs`, `WorkshopReplay.cs` and `WorkshopService.cs` in
`MorWalPizVideo.Domain`; and added the authenticated
`ServerAPI/Controllers/Workshop/WorkshopController.cs` plus mock/production DI and
credentialed `https://workshop.morwalpiz.com` CORS registration in `Program.cs`.

The operation ledger is account-owned and idempotent, stores immutable complete typed
deltas and a deterministic contribution digest, and deliberately does not store
projection status. Projection/reconciliation state is stored separately and replay
rebuilds firearm, component, batch and allocation values independently. Shooting
requests require explicit untracked ammunition and exact component/installation
segments; known consumption plus untracked ammunition must equal the session total.
DTOs remain in Contracts and are mapped by ServerAPI; Domain references no API DTOs.

Validation performed: `dotnet build
MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj --no-restore` succeeded.
Focused Workshop tests were not added or run in this bounded stream. Remaining gaps
include complete install/remove, maintenance, recipe-edit/archive, allocation-release/
reallocation, correction endpoints, startup index application/central BackOffice
manifest integration, projection fault-injection tests, and the frontend/deployment
surfaces explicitly excluded by the request.
- Reconciliation detects deliberate projection corruption and repairs only the
  projection.
- Activity paging is stable and account-filtered.
- Mock and production DI graphs resolve the complete controller surface.

### Frontend component and route tests

Owning workspace: `frontend/shooter-workshop.client`.

- Protected route restoration, login redirect and safe return URL.
- Loader/action success, Problem Details validation, conflict and session expiry.
- Forms for all domains, including split session segments and mixed tracked/untracked
  consumption.
- Accessible pending, error, empty, correction and reconciliation states.
- Responsive navigation, dense lists, mobile cards and keyboard operation.
- Dashboard values come from API projections and visibly disclose inconsistent/pending
  state rather than synthesizing success locally.

Shared services tests verify that Workshop calls:

- use account mode with credentials and CSRF;
- never send bearer or channel headers;
- preserve typed Problem Details and field errors;
- target only versioned Workshop endpoints.

### Contract and schema tests

- .NET and TypeScript DTO fields, enum values, nullability and timestamps remain
  aligned.
- Event schema versions are additive and unknown versions fail explicitly during replay.
- Legacy/missing optional fields have deliberate defaults; no silent zero is accepted
  where it could change a reconstructed total.

### Security tests

- Exact-origin CORS with credentials.
- CSRF bootstrap, mutation and logout from the Workshop origin.
- No serial number or registration identifier fields in persistence/API/UI.
- Notes and recipe values excluded from logs and telemetry.
- Rate/size bounds on history, operation and reconciliation endpoints.
- Ownership checks use claim-derived account ID for every identifier path.

### Migration and recovery tests

- Empty-database index creation and duplicate-audit behavior.
- Projection rebuild after deletion or deliberate corruption of current totals.
- Retry after process termination between event append and projection completion.
- Rollback frontend while preserving ledger and API compatibility.
- Event replay across every supported schema version.

### End-to-end acceptance

Automate the specification's scenarios A-J and add one integrated lifecycle:

1. create two firearms and three identical physical components;
2. install/move components and record maintenance;
3. version a recipe and produce multiple batches;
4. allocate stock across firearms;
5. record tracked, mixed and untracked sessions;
6. correct a prior split session;
7. corrupt each projection family in test setup;
8. reconcile and prove all firearm, component, stock and allocation values are restored;
9. verify the original history and correction chain remain visible.

## Migration And Rollback

### MongoDB

The initial release is additive: new collections and indexes, with no existing Workshop
documents to backfill. Persist event schema versions from the first write. Readers must
tolerate only explicitly optional additive fields; replay must reject unsupported event
versions rather than guessing.

Audit and apply indexes before enabling writes. If a later schema requires backfill, use
an idempotent account/aggregate replay from the ledger rather than deriving history from
projected totals.

### API contracts

Deploy additive ServerAPI routes before the SPA. Keep them unpublished/feature-disabled
until all domains are present. DTO additions are optional-first; breaking changes require
a new API version. The complete application is activated once, not domain by domain.

### Authentication and CORS

No cookie format or identity migration is required. Add only the exact Workshop origin
to the existing credentialed account CORS policy. Production activation requires
real-browser CSRF/session proof.

### Cache

No cache migration is required because authenticated Workshop responses are not output
cached. Projection documents are durable state and remain reconstructable.

### Deployment order

1. Deploy additive models, repositories, indexes, replay service and disabled API.
2. Verify index audit, account isolation and complete backend tests.
3. Deploy the complete SPA and runtime API configuration without public activation.
4. Verify exact-origin authentication, all functional scenarios and full reconciliation.
5. Publish DNS/route access and enable the complete feature.

### Rollback

- Disable Workshop entry and roll back the SPA independently.
- Disable new command acceptance while retaining read/reconciliation access for already
  accepted operations.
- Roll back ServerAPI only to a version that can still deserialize every persisted event
  schema version.
- Never delete ledger events or compensate by editing current counters.
- If projections are suspect, rebuild them from the ledger after restoring a compatible
  service version.
- Index rollback is limited to indexes proven unnecessary and safe to remove; durable
  event data remains.

## Open Questions

1. **Canonical URL wording:** this plan assumes `https://workshop.morwalpiz.com/`.
   If `/workshop` means a path under `morwalpiz.com`, base paths, PWA scope, deployment,
   same-origin behavior and CORS must be revised before provisioning.
2. **Reconciliation exposure:** the plan includes account-owned status and repair in the
   Workshop UI. Decide whether full-account repair is self-service or support-gated;
   this changes authorization and rate limits but not the ledger design.
3. **Data export/retention:** the functional baseline requires durable history but does
   not define account export, deletion or retention policy. A production privacy review
   must resolve these before general availability; it must not weaken replay integrity
   for active accounts.

## Backend continuation delivery note

The bounded backend continuation is complete for the current edit. The implementation
now includes the following account-owned `api/v1/workshop` command and projection
surface:

- firearm, component-definition and physical-component reads/creation;
- component installation and removal, maintenance recording;
- recipe creation, version mutation and archive;
- production and batch reads;
- allocation, release and reallocation commands;
- shooting-session recording with explicit untracked ammunition and exact
  component/installation usage segments;
- immutable shooting correction through compensating ledger operations;
- dashboard, paged/type-filtered history, reconciliation status and projection rebuild.

The ledger/replay safeguards were retained and tightened during this continuation:

- `WorkshopOperation` remains immutable and contains no projection status;
- contribution digests are canonical and deterministic (the delta digest no longer
  recursively calls itself);
- production, allocation and release retries use deterministic resource IDs and compare
  the semantic payload hash before returning an idempotent result;
- idempotent retries reapply affected projections and surface optimistic-concurrency
  conflicts instead of returning success after a lost projection update;
- allocation release enforces the request's expected allocation revision;
- grouped batch and allocation consumption is validated before ledger append;
- replay reconstructs firearm rounds, component rounds, batch stock and allocation
  original/active/consumed/released values independently;
- reconciliation repairs projections and performs a fresh verification pass;
- correction appends reversal effects rather than rewriting the original operation.

Files owning the backend slice are:

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`
- `MorWalPizVideo.Models/Constraints/DbCollections.cs`
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `MorWalPizVideo.ServerAPI/Program.cs`
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs`

Validation completed after the final backend edit:

```text
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --no-restore --filter FullyQualifiedName~WorkshopReplayTests
Passed: 3, Failed: 0, Skipped: 0
```

That focused test also rebuilt the dependent Domain, MvcHelpers, ServiceDefaults,
ServerAPI and test projects successfully. Existing unrelated compiler warnings and
the repository's pre-existing `SixLabors.ImageSharp` vulnerability warnings remain.

Additional integrated validation:

```text
dotnet build MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj
  --configuration Release --no-restore -m:1 -p:BuildInParallel=false
Succeeded: 0 warnings, 0 errors

dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore --no-build
  --filter "FullyQualifiedName~WorkshopReplayTests|FullyQualifiedName~MongoIndexOperationsTests"
Passed: 23, Failed: 0, Skipped: 0

cd frontend && yarn build:shared
  && yarn workspace shooter-workshop.client build
  && yarn workspace shooter-workshop.client test --passWithNoTests=false
Shared packages and Workshop build succeeded; Workshop route test passed.

cd frontend/fe-packages/services && npm test
Passed: 82 tests across 9 files.

dotnet build MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj
  --configuration Release --no-restore -m:1 -p:BuildInParallel=false
Succeeded: 0 errors; existing package vulnerability warnings were reported.
```

Remaining release gaps are intentionally outside this bounded continuation: complete
HTTP/account-isolation/CSRF integration coverage, fault-injection tests for ledger versus
projection failure, Mongo concurrency testing, and broader acceptance coverage. The
correction replacement path also needs a dedicated integration test for final stock and
allocation non-negativity. No frontend, AppHost, CI, deployment, or architecture source
was changed for this continuation.

## Frontend integration continuation

The Workshop frontend service layer was subsequently canonicalized against the
implemented `WorkshopController` rather than the earlier projected API proposal.
The shared TypeScript contracts now match the controller's raw entity responses and
request field names. In particular:

- production uses `POST /api/v1/workshop/ammunition/production` with
  `productionDate`, `quantityProduced`, and `idempotencyKey`;
- allocation uses `POST /api/v1/workshop/ammunition/allocations`;
- shooting uses `POST /api/v1/workshop/sessions/shooting` with explicit
  `untrackedAmmunition`, `consumptionRecords`, and `usageSegments`;
- maintenance recording uses `POST /api/v1/workshop/maintenance` and is exposed in
  the maintenance workflow with an idempotency key;
- reads use the controller's raw firearm, component, recipe, batch, activity, and
  reconciliation arrays/contracts rather than synthetic dashboard/page envelopes;
- all Workshop calls remain on `accountApiService`, preserving cookie credentials,
  CSRF acquisition, and the absence of bearer/channel headers.

Frontend files changed for this continuation:

- `frontend/fe-packages/models/src/workshop.ts`
- `frontend/fe-packages/models/src/index.ts`
- `frontend/fe-packages/services/src/endpoints-frontend.ts`
- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/fe-packages/services/src/workshopService.test.ts`
- `frontend/fe-packages/services/src/index.ts`
- `frontend/shooter-workshop.client/src/routes/pages.tsx`
- `frontend/shooter-workshop.client/src/routes/config.tsx`
- `frontend/shooter-workshop.client/src/routes/config.test.tsx`
- `frontend/shooter-workshop.client/src/vite-env.d.ts`
- `frontend/shooter-workshop.client/vitest.config.ts`

Validation:

```text
cd frontend/fe-packages/models && npm run build
  Passed

cd frontend/fe-packages/services && npm run build && npm test
  Passed: 9 test files, 81 tests

cd frontend/shooter-workshop.client && npm run build
  Passed: TypeScript and Vite production build
```

The Workshop app remains a standalone client for the `workshop.morwalpiz.com`
deployment assumption. Dashboard and detailed projection reads remain intentionally
composed from the controller's account-filtered raw entity reads; command routes for
maintenance, installation, release and correction are owned by the backend and can be
extended with richer read projections without changing the ledger contract.

## Backend completion — firearm lifecycle and allocation/correction retries (2026-11-13)

Implemented the remaining backend command surface without changing existing public
account authentication, repository boundaries, or persisted entity shapes:

- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` now defines
  idempotent, revision-aware firearm update and archive requests.
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` adds the
  `FirearmUpdated` ledger operation type.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` implements optimistic,
  idempotent firearm edit/archive operations and preserves historical firearm data.
  Reallocation now validates archived destinations and retries an already committed
  operation before active-quantity checks. Shooting correction retries validate the
  complete payload digest after reversal/replacement construction, so a retry remains
  idempotent while a changed payload conflicts.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` exposes
  `PUT /api/v1/workshop/firearms/{firearmId}` and
  `POST /api/v1/workshop/firearms/{firearmId}/archive`, using the existing account,
  CSRF, validation, and conflict conventions.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` covers firearm
  edit/archive revision and retry behavior, allocation release/reallocation retries,
  and shooting correction retry behavior. Existing replay tests remain in scope.

Validation:

```text
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore
  --filter "FullyQualifiedName~WorkshopServiceTests|FullyQualifiedName~WorkshopReplayTests"
  Passed: 6 tests
```

The command emitted existing package vulnerability and nullable-analysis warnings; no
test or compile failures remain in the focused scope. HTTP-level account/CSRF
integration, Mongo concurrency/fault-injection, and frontend exposure of these newly
available routes remain downstream release work and were not changed here.
