# FreezerAnalysis Implementation Handoff

Status: implementation is complete for the approved local mock-authentication and match-analysis refinement boundary. Production deployment remains separately authorized.
Planning baseline: 2026-10-29. This handoff is maintained as the user-editable implementation record; the initial planning statement below has been superseded by the implementation evidence in this update.

## Implementation Record (2026-10-29)

### Delivered

- Mock mode now uses the existing cookie-backed `PublicAccountService`, `PublicAccountMockRepository` and host-lifetime `IMockScenario`; no cookie means an unauthenticated session. The seeded Development account is `development-account` / `freezerdemo`.
- The FreezerAnalysis SPA redirects protected online routes to login, preserves a sanitized return path, and uses the Vite same-origin `/api` and `/health` proxy in the AppHost topology. CSRF middleware remains enabled.
- The API owns the eight-value canonical division catalog and validates profile/draft writes against it. New drafts inherit profile identity, division and performance without overwriting existing drafts. The SPA uses division selects and explains performance as the expected share of stage maximum points, displayed as a percentage.
- Authenticated active-revision result summary/detail endpoints expose bounded athlete and Verify-stage read models. The SPA provides result filtering, lazy detail inspection and a pairwise stage-ordered HF comparison chart/table with explicit incomplete-result handling.

### Files Changed

The implementation touched `FreezerAnalysis.Api/Program.cs`, `FreezerAnalysis.Api/Controllers/FreezerAccountController.cs`, `FreezerAnalysis.Api/Controllers/FreezerAnalysisController.cs`, `FreezerAnalysis.Api/Services/FreezerAnalysisCoordinator.cs`, `MorWalPiz.Contracts/Contracts/FreezerAnalysis/FreezerAnalysisContracts.cs`, `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs`, `frontend/FreezerAnalysis.spa/src/main.tsx`, `frontend/FreezerAnalysis.spa/src/routes.tsx`, `frontend/FreezerAnalysis.spa/src/freezerApi.ts`, `frontend/FreezerAnalysis.spa/vite.config.ts`, `frontend/FreezerAnalysis.spa/package.json`, and `MorWalPizVideo.AppHost/Program.cs`.

### Validation

- `yarn --cwd frontend workspace freezer-analysis.spa build`: passed. Vite reported only the existing large-chunk and stale Browserslist warnings.
- `yarn --cwd frontend/FreezerAnalysis.spa test --run`: passed, 20 tests.
- `dotnet test FreezerAnalysis.Api.Tests/FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore -p:BuildInParallel=false -m:1 --verbosity:minimal`: passed, 77 tests total, 74 passed and 2 intentionally skipped.
- The focused cookie-login regression and profile/CSRF regression both pass.

### Residual Risks And Deferred Scope

- A real AppHost browser smoke test, two-account isolation test, pure comparison-helper test, explicit route-tab test and unknown-division import diagnostic test were not added in this delivery.
- The comparison UI is implemented within the existing route module rather than extracted into a dedicated component file.
- The two PDF extraction integration checks remain skipped unless `FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH` is configured.

## Route And Match-Day Refinement Proposal (2026-11-05)

The current route organization, HF selection, field-note usability, individual PNG downloads, and editable-plan reopening are covered by the user-editable [route and field refinement handoff](freezer-analysis-route-field-refinement-handoff.md). That document is a proposed plan, not an implementation record. It preserves frozen history and explicitly addresses historical queued notes when the working draft is reopened. The earlier completion record does not claim these new requests have been delivered.

## Technical Debt Reduction Plan (2026-10-30)

### Outcome

Reduce FreezerAnalysis to zero **feature-local release-blocking debt** before production activation. Zero does not mean deleting every compatibility path or implementing every deferred capability. It means that all High risks below are closed with executable evidence, Medium maintainability debt is either removed or bounded by an explicit invariant and owner, the artifact tested in CI is the artifact deployed, interrupted writes converge safely on retry, required Mongo indexes are source-owned and verified, and the complete authenticated workflow is proven with real Mongo and a real browser.

This analysis covers the current FreezerAnalysis API, Domain persistence/engine, contracts, SPA/PWA, tests, AppHost, workflows and owning architecture documentation. It does not reopen unrelated repository debt. Production deployment remains unauthorized. Original-application parity, PDF score publication, AI activation and public-result governance are external acceptance or product capabilities; they become technical debt only if the product is released while their stated gates are bypassed.

Assumptions: preserve PublicAccount identity, the separate application database, immutable published facts and frozen plans, account-private mappings/analysis, current `/api/v1` compatibility, and the deterministic engine. On 2026-10-31 the user confirmed that production uses separate SPA and API origins, following the existing pattern where a SPA on `morwalpiz.com` calls an API on an Azure Web App origin. FreezerAnalysis therefore retains independently deployed SPA and API hosts. The local same-origin Vite proxy remains a development convenience, not the production topology.

### Current Evidence

- `FreezerAnalysis.Api/Services/FreezerAnalysisCoordinator.cs` owns identity review, import publication, draft/estimate composition, freeze/replay, verification, parameter backtesting/adoption, field events and artifacts in one approximately 2,500-line class. `frontend/FreezerAnalysis.spa/src/routes.tsx` similarly owns nearly every route surface and interaction in one approximately 1,600-line module. These are maintainability and test-isolation debts, not by themselves correctness defects.
- `MorWalPizVideo.Domain/FreezerAnalysis/FreezerAnalysisMongoRepository.cs` defines the required unique and query indexes in `EnsureIndexesAsync`, but no composition or deployment path calls it. `FreezerAnalysisHostOptions.ApplyIndexesOnStartup` is configured and unused. Correctness currently depends on indexes that are neither applied nor audited by the feature.
- Import publication writes result payloads and a revision, moves the match active pointer, and only then inserts the published import manifest. An interruption after the pointer move can expose a complete shared revision while leaving the owner without the published availability manifest needed for estimation/verification. Retry then sees a revision conflict instead of completing the original operation.
- Freeze uploads a content-addressed input package, inserts a snapshot, and then conditionally binds the draft. If the process stops after snapshot insertion, the unique `(account, draft, draftRevision)` index can reject a retry while no draft points to that snapshot. The current duplicate-key path only recovers a snapshot already referenced by the draft.
- Field-event append increments the draft sequence before inserting the event. A crash or duplicate race can leave gaps and can apply rendering context without the corresponding event. The API currently promises idempotency by operation ID but does not define gap/recovery semantics.
- Estimate composition loads up to 1,000 earlier matches and performs revision, account-manifest, result and identity-resolution queries inside the loop. Result detail first loads every result in the active revision. The pure 500-competitor engine benchmark does not measure this persistence path, payload size or concurrent rendering.
- Result summaries are unpaged, repository result loads permit up to 20,000 documents, and publication has no explicit BSON preflight for revision/result-ID or backtest document size. The frozen input package alone has the planned 8 MiB guard.
- `FreezerAnalysis.Api/Dockerfile` builds the SPA and installs Poppler plus pinned Playwright Chromium. Central CI builds that container. In contrast, `.github/workflows/deploy-freezer-analysis-api.yml` deploys a plain `dotnet publish` ZIP, while `.github/workflows/deploy-freezer-analysis-spa.yml` deploys a separate nginx image. The tested native-runtime artifact is not the deployed artifact.
- Current tests provide strong deterministic-engine, parser, renderer, in-memory HTTP, CSRF and account-owned identity coverage. They do not use a disposable Mongo server, run the AppHost/browser flow, prove interruption recovery, exercise two complete accounts, execute the deployment image, or measure the full estimate/import/render path. The handoff records local passes, but GitHub execution and deployed smoke evidence remain absent.
- The SPA correctly precaches only static shell assets, keeps snapshots/events in account-partitioned IndexedDB, clears prior-account data and rechecks live snapshot ownership before sync. Remaining offline debt is browser/storage/restart evidence rather than a known ownership bypass.
- The API has ServiceDefaults telemetry but the feature code has no structured operational logs or feature metrics for imports, publication conflicts/recovery, estimate history size, renderer admission/failure, offline replay, index readiness or object integrity. Production diagnostics would depend mainly on generic request traces.
- No FreezerAnalysis ADR currently records the accepted account boundary, separate databases, publication visibility rule, freeze immutability/recovery protocol or deployment topology. Older same-origin production proposals in this handoff are superseded by the confirmed separate-origin decision and must be reconciled during implementation.
- Shared-result retention/correction/erasure ownership, lawful processing approval and approved LLM terms remain unresolved. PDF preview intentionally cannot publish because authoritative stage maxima and DQ mapping are unavailable. These block public/AI/PDF activation, but they should not trigger guessed implementation.

### Proposed Design

#### 1. Make multi-step writes retry-convergent

Keep the no-cross-database-transaction design, but turn import and freeze into explicit idempotent state machines.

- Import: create or upsert a deterministic account/source operation manifest in `Staged` state before writing result/revision records. Use deterministic or persisted operation-owned revision/result identities, verify content hashes on every retry, conditionally move the match pointer only after all referenced records exist, then transition that same manifest to `Published`. A retry must detect each completed phase and continue; it must never create a second conflict manifest for the same account/source hash. Readers continue to use only the match active pointer as shared-fact visibility, while account history requires the matching published manifest.
- Freeze: derive or persist one snapshot identity for `(accountId, draftId, expectedRevision)`, upsert/verify the immutable snapshot, then conditionally bind the draft. If the snapshot exists but is unbound, retry the bind; if another snapshot won, return that winner. A mismatched hash is corruption, not a normal conflict.
- Field events: make the client operation ID the durable reservation key and document that sequence numbers are monotonic, not necessarily contiguous. Persist the event and rendering-context transition through a recoverable protocol; a retry must either finish the same operation or return the prior event without allocating another effective transition.
- Add fault-injection tests after every persistence phase. Add an idempotent audit/repair operation for historical staged imports, unbound snapshots, missing private objects and orphan artifact manifests. It must default to report-only and must never infer content or ownership.

#### 2. Establish feature-owned Mongo readiness

Replace the unused startup option with one source-owned index manifest/service. Production startup should audit required index names, keys and uniqueness and mark readiness unhealthy when they differ. Index mutation should occur in an explicit deployment maintenance phase after duplicate/precondition audit, not silently during ordinary production startup. Development/Test may apply indexes to disposable databases.

The manifest must include every correctness index used by publication, freeze, events, identity mappings, verifications, adoptions and artifacts. Add query-plan evidence for match listing, prior-history selection, active-result lookup and account snapshot/event reads. Keep platform account/session indexes under their existing owner; FreezerAnalysis may audit but not redefine them.

#### 3. Deploy two qualified cross-origin artifacts

Keep the confirmed separate production hosts: the SPA image serves static assets and calls the explicit API origin through `VITE_FREEZER_API_BASE_URL`; the API image serves `/api`, health endpoints, Poppler extraction and Chromium rendering. Build each artifact once, identify each by immutable digest, qualify the pair together, and promote those same digests. Remove the SPA build/static copy from the API image once the separate topology is fully represented so the API artifact has one owner and purpose.

Replace the API ZIP deployment with the tested API container. The container must include pinned Poppler and Playwright Chromium exactly as exercised by CI. Keep the SPA nginx container and its independent deployment, but record the compatible SPA/API digest pair as one release. Do not keep the current mismatch where CI validates native dependencies in a container while production receives a plain .NET ZIP.

Configure the API with an exact environment-managed SPA origin, credentialed CORS, and no wildcard fallback. The host-only session and CSRF cookies must be `Secure` and `SameSite=None`; unsafe calls must include the CSRF header and pass exact origin validation. Qualify anonymous CSRF acquisition, registration, login, session restoration, mutation and logout in Chromium and WebKit against the real cross-origin shape. Because origins on different registrable domains can be treated as third-party contexts, browser cookie restrictions are a release risk; do not work around failure by weakening CSRF, broadening CORS or exposing browser bearer tokens. A custom same-site API domain or gateway would require a separate architecture decision if browser evidence proves the confirmed topology unreliable.

API readiness must cover both Mongo boundaries, index audit, private Blob access, Poppler executable availability and a lightweight Chromium launch. Renderer/extractor failure must not make already persisted snapshots or artifacts unreadable. SPA readiness must prove that the configured API base URL is HTTPS and belongs to the approved environment.

#### 4. Bound persistence cost and document growth

Add repository read models that project only the data required by each use case:

- paged active-revision result summaries and direct active-result detail lookup;
- one bulk historical-estimate input query or a small bounded set of aggregation/projection queries instead of per-match round trips;
- bounded account identity/resolution lookups keyed by the selected match set;
- explicit size/count validation before revision, result, snapshot, verification and backtest persistence.

Retain the 1,000-match compatibility ceiling only as a visible safety limit until measured query/storage evidence supports cursor-based expansion. Add cursor pagination without changing the existing list response in place: introduce the additive paged contract, migrate the SPA, retain the old endpoint for one compatibility window, then remove it only in a separately approved version change.

#### 5. Prove the production boundaries

Add a disposable real-Mongo suite for indexes, duplicate races, interruption/retry, publication visibility, freeze recovery, sequence idempotency, BSON guards and two-database isolation. Add Playwright browser coverage against the built runtime for registration/login, CSRF, protected redirects, two-account private-data isolation, import/profile/draft/freeze, result comparison, explicit offline download, restart/airplane mode, queued sync, artifact generation and logout/account switch.

Measure the complete 1,000-match estimate read plus engine, 500-competitor/34-stage result browsing, 20 MiB supported import, two concurrent artifact requests and private-object reads on the declared production tier. Preserve the current pure engine benchmark as a unit-level regression, not release-capacity evidence.

For the frontend, this is a refinement of the existing `Operate` surface, not a redesign. Apply Impeccable `harden` to offline/conflict/error states and `adapt` to the match result/comparison and field views. Minimum evidence is Chromium and WebKit captures at 320, 390, 768 and desktop widths, keyboard and screen-reader status checks, 200% zoom, 44-pixel touch targets, non-color status cues and no private API response in the service-worker cache.

#### 6. Decompose only after characterization gates

Preserve public contracts and behavior while splitting the coordinator by owned use case inside the existing API/Domain projects: imports/publication, identities, planning/freeze/replay, verification/backtesting, and cards/artifacts. Split the repository interface along the same cohesive read/write capabilities only where it reduces constructor and test scope; do not create a project per service.

Split persisted entities and contracts into feature files by aggregate/HTTP surface without changing BSON element names or JSON shapes. Split the SPA into route modules and local feature components/helpers; extract result filtering/stage alignment into a pure tested helper and keep route loaders as data owners. Split the large API and route test files by behavior. This work follows the crash-consistency and real-Mongo characterization tests so structural change cannot conceal a behavior regression.

#### 7. Add observability, governance and a closed debt ledger

Emit structured, non-identifying events and metrics for operation IDs, phase/status, counts, durations, bytes, conflict/recovery outcomes, history truncation, render queue wait, native dependency failure and integrity failure. Never log names, licenses, credentials, cookies, CSRF tokens, source content, private notes or object URLs.

Add a FreezerAnalysis ADR and reconcile backend, frontend, domain, deployment, development, testing and technical-debt docs with the selected topology and implemented recovery invariants. Record every remaining item as one of: closed with executable evidence; accepted bounded compatibility debt with owner/removal trigger; externally blocked product/governance decision; or out of scope. Do not report “zero debt” while any High item below is open.

### Impacted Projects

- `FreezerAnalysis.Api`: orchestration decomposition, recoverable operations, readiness, observability, bounded HTTP surfaces and runtime packaging.
- `MorWalPizVideo.Domain`: repository contracts, Mongo/in-memory behavior, index manifest/audit, projected reads and idempotent state transitions.
- `MorWalPizVideo.Models`: additive operation/recovery metadata only if required; aggregate file separation without BSON renames.
- `MorWalPiz.Contracts`: additive paged result/readiness or operation-status contracts while preserving current v1 responses during migration.
- `MorWalPizVideo.MvcHelpers`: verify-only reuse of PublicAccount cookie/CSRF behavior; change only if cross-origin browser evidence finds a missing options-level capability.
- `MorWalPizVideo.ServiceDefaults`: verify-only health/telemetry integration; no feature logic.
- `frontend/FreezerAnalysis.spa`: route/module extraction, paged result consumption, offline/error hardening and browser qualification.
- `frontend/fe-packages/services`: verify-only credential/CSRF transport unless browser evidence finds a shared defect.
- `FreezerAnalysis.Api.Tests`: real-Mongo, fault-injection, contract, security, performance and container-native coverage.
- `MorWalPizVideo.AppHost`: local topology/browser fixture alignment with the selected production model where practical.
- `.github/workflows`: one build/promote path for the qualified runtime, index maintenance/audit, nonempty browser/real-Mongo gates and post-deploy smoke evidence.
- Architecture/operations documentation: ADR, debt ledger, topology, index/recovery, retention and release evidence.
- Other APIs, SPAs, Windows tools, ShortLinks, Shooting Range, YouTube utilities and shop: not applicable beyond shared-library regression verification; no feature behavior change.

### Impacted Files

#### Modify

- `FreezerAnalysis.Api/Program.cs`: remove dead index configuration, register readiness/observability and align authentication/CORS/static hosting with the selected topology.
- `FreezerAnalysis.Api/Controllers/FreezerAnalysisController.cs`: retain current routes, add bounded/paged reads and consistent operation outcomes without embedding recovery logic.
- `FreezerAnalysis.Api/Services/FreezerAnalysisCoordinator.cs`: shrink to compatibility/facade responsibility while moving cohesive use cases to feature-owned services.
- `FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs`: register feature index audit/readiness and preserve separate database ownership.
- `FreezerAnalysis.Api/Services/FreezerDocumentRenderer.cs` and `PdfTextExtractor.cs`: expose bounded native-dependency readiness/metrics while preserving sandbox, timeout and concurrency behavior.
- `MorWalPizVideo.Domain/FreezerAnalysis/IFreezerAnalysisRepository.cs`, `FreezerAnalysisMongoRepository.cs` and `InMemoryFreezerAnalysisRepository.cs`: retry-convergent transitions, projections, pagination, size guards and audit/repair support.
- `MorWalPizVideo.Models/FreezerAnalysis/FreezerAnalysisEntities.cs`: additive state/recovery fields if the chosen protocol requires them; later split by aggregate with BSON compatibility preserved.
- `MorWalPiz.Contracts/Contracts/FreezerAnalysis/FreezerAnalysisContracts.cs`: additive paged and operation-status contracts; later split by surface without JSON changes.
- `frontend/FreezerAnalysis.spa/src/main.tsx`, `routes.tsx`, `freezerApi.ts`, `offlineStore.ts` and adjacent tests: route extraction, paged reads, hardened offline/conflict states and browser-test hooks without weakening account partitioning.
- `FreezerAnalysis.Api/Dockerfile`, `frontend/FreezerAnalysis.spa/Dockerfile`, `.github/workflows/ci.yml`, `deploy-freezer-analysis-api.yml` and `deploy-freezer-analysis-spa.yml`: build, test and promote the two compatible cross-origin artifacts; stop deploying the API as a ZIP.
- `MorWalPizVideo.AppHost/Program.cs`: preserve the current same-origin local proxy and expose the browser-test topology.

#### Add

- Cohesive FreezerAnalysis use-case services and matching focused tests under the existing API/Domain feature directories; exact file names follow the final extraction boundaries above.
- A feature-owned index manifest/audit/maintenance service and report-only recovery audit under `MorWalPizVideo.Domain/FreezerAnalysis` with API composition in `FreezerAnalysis.Api`.
- Disposable Mongo integration fixtures and interruption fault points under `FreezerAnalysis.Api.Tests`.
- Browser tests under a FreezerAnalysis-owned Playwright convention covering the built runtime, desktop/mobile/offline and two accounts.
- A FreezerAnalysis ADR under `docs/architecture/adr/` and an operations note for indexes, recovery, retention and rollback.
- Route-local result/comparison and field modules plus a pure stage-alignment helper under `frontend/FreezerAnalysis.spa/src/`.

#### Generated/Migration

- No destructive Mongo migration. Additive operation fields/indexes and reconciliation of existing staged/orphan records require a dry-run report, duplicate audit and idempotent apply record.
- Container digests, browser output, TRX/JUnit reports, screenshots and performance reports are generated CI/release evidence and are not source fixtures.
- Frontend lock metadata changes only if browser tooling or package extraction requires a dependency change.

#### Documentation

- This handoff: update each debt item with actual files, validation and residual status after implementation.
- `docs/architecture/technical-debt.md`: add and track the FreezerAnalysis production-readiness debt until every High item closes.
- `docs/architecture/backend.md`, `frontend.md`, `domain-model.md`, `development.md`, `deployment.md`, `testing.md` and `architecture-decisions.md`: reconcile current behavior with the accepted topology/recovery contract.
- New FreezerAnalysis ADR and operations runbook: persistent ownership, publication/freeze visibility, index application, recovery, retention and release evidence.

#### Verify Only

- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs`: existing options already support Secure/HttpOnly and SameSite selection.
- `frontend/fe-packages/services/src/apiTransport.ts`: preserve account-mode credentials and CSRF isolation.
- Platform PublicAccount models/repositories/indexes: authenticate and validate only; do not migrate them into the feature database.
- Other application builds/tests that consume shared Contracts, Domain, MvcHelpers or ServiceDefaults.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Recoverable import/freeze/event transitions; paged/projected reads; cohesive use-case services; stable v1 compatibility window. |
| Frontend/UI | Existing Operate design retained; route extraction, cursor consumption and hardened browser/offline/conflict evidence. |
| Database/Data | Source-owned indexes, additive recovery metadata, orphan/staged audit, BSON size guards and real-Mongo qualification. No destructive rewrite. |
| Contracts/Models | Additive operation/paging contracts and serializer-tolerant metadata; persisted names and immutable historical values remain stable. |
| Authentication/Authorization | PublicAccount boundary retained. Separate origins require exact credentialed CORS, Secure `SameSite=None` cookies, CSRF/origin enforcement and Chromium/WebKit proof. Two-account isolation becomes a release gate. |
| Validation | Add document/count/operation-state validation and reject integrity mismatches. Existing numerical, division, upload and ownership validation remains. |
| Cache | No authenticated API output cache. Service worker remains shell-only; browser tests prove no private API caching. Content-addressed private objects remain immutable. |
| Configuration/Secrets | Remove dead index flag. Add no secrets. Keep exact environment-managed SPA and API origins; reject missing, non-HTTPS or wildcard production values. |
| Logging/Telemetry | Add privacy-safe operation metrics/traces, recovery outcomes, queue wait and dependency readiness; no personal/source data. |
| Background Jobs | Not required for correctness. Recovery/index work is explicit deployment/operator maintenance, not in-memory fire-and-forget work. |
| Deployment/Operations | Build once and promote the exact qualified image; audit indexes and dependencies before traffic; record smoke and rollback evidence. |
| Backward Compatibility | Existing snapshots/packages/engine versions remain replayable. Current result list remains during additive paging migration. Existing host-only API sessions remain compatible because the separate-origin production model is retained. |

### Reuse Opportunities

- Reuse content-addressed `IFreezerPrivateObjectStorage` writes and integrity checks for resumable freeze/artifact phases.
- Reuse current optimistic revision fields and unique operation indexes; extend their recovery semantics instead of introducing a queue or event-sourcing platform.
- Reuse the existing `FreezerAnalysisDatabase` boundary and repository pair for production/mock parity.
- Reuse ServiceDefaults health, OpenTelemetry and request tracing; add feature tags/metrics rather than another telemetry stack.
- Reuse the API container's pinned native dependencies, the SPA nginx image, AppHost local proxy, account-mode transport, PublicAccount security primitives and current renderer admission semaphore. Remove duplicated SPA content from the API image after separate deployment parity is proven.
- Reuse current engine/parser/renderer tests as characterization gates before decomposition.
- Reuse the SPA's account-partitioned IndexedDB and React Router loaders; no global store is needed.

### Risks

| Priority | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Missing/unverified Mongo indexes allow duplicates or slow unbounded reads. | Explicit manifest, preflight audit, deployment apply and readiness failure. | Disposable and target-like audits show exact indexes; duplicate races have one winner. |
| High | Import/freeze interruption leaves active facts without a usable manifest or an unbound immutable snapshot. | Retry-convergent deterministic operations plus fault injection and report-only repair. | Every injected interruption converges on retry with one visible revision/snapshot and matching private object. |
| High | CI validates a container that production does not deploy. | Promote the tested image digest and run native/browser smoke checks against it. | Deployed digest equals qualified digest; Poppler, Chromium, API, SPA and artifact smoke pass. |
| High | In-memory tests mask Mongo atomicity, index and serializer differences. | Disposable real-Mongo gate with concurrency and recovery scenarios. | Required suite passes against a replica-set-capable target and ordinary standalone compatibility where claimed. |
| High | Cross-origin cookies can be misconfigured or blocked by browser third-party-cookie policy. | Exact credentialed CORS, Secure `SameSite=None` cookies, strict CSRF/origin checks and real-domain browser qualification; escalate to a custom same-site API domain only through a separate decision. | Anonymous, login, session restoration, mutation, logout and disallowed-origin scenarios pass in Chromium/WebKit with third-party-cookie policy represented. |
| High | Personal sporting results lack approved retention/correction ownership. | Keep public/AI activation gated; approve policy and implement audited correction/erasure handling. | Named owner approves policy; retention/export/correction tests and runbook exist. |
| Medium | N+1 history/result reads and large unpaged payloads miss latency/document limits. | Projection/bulk queries, cursor paging, BSON guards and full-path budgets. | Explain plans use indexes; declared corpus/concurrency budgets pass without truncation hidden from users. |
| Medium | Large coordinator/routes/tests make changes couple unrelated workflows. | Characterize first, then extract cohesive use cases and route modules without contract changes. | Focused suites can construct/test each use case; shared regression remains green. |
| Medium | Sparse feature telemetry makes recovery and native dependency failures hard to diagnose. | Privacy-safe operation metrics, traces and readiness. | Dashboards/tests expose phase, duration, conflict/recovery and dependency state without personal data. |
| Medium | Offline behavior passes jsdom but fails on browser eviction/restart/account switching. | Real PWA/browser airplane-mode and storage-failure drills. | Cached field card survives declared restart case; queues reconcile once; switch/logout clears prior account data. |
| Low | Supported legacy engine/package paths become permanent accidental complexity. | Keep version registry and removal criteria; test only intentionally supported versions. | Unsupported versions fail explicitly; supported replay remains deterministic. |

### Technical Debt

| ID | Debt | Priority | Decision and zero-debt exit |
|---|---|---|---|
| FA-001 | Required Mongo indexes are defined but neither applied nor audited; `ApplyIndexesOnStartup` is dead configuration. | High | Address now. Exit when deployment applies/audits the source manifest and readiness proves exact target state. |
| FA-002 | Import and freeze have interruption windows that retry does not always repair. | High | Address now. Exit when phase fault tests converge and existing orphans/staged records have an audited repair path. |
| FA-003 | The intended separate-host topology deploys the API as a ZIP even though CI qualifies its Poppler/Chromium container. | High | Address now. Exit when immutable SPA and API images are qualified together and those exact digests are promoted. |
| FA-004 | No real-Mongo, complete two-account, real-browser or deployment-image gate. | High | Address now. Exit when those gates are required and have fresh passing evidence. |
| FA-005 | Estimate/result persistence access is N+1/unpaged and only the pure engine has scale evidence. | Medium | Address before production load. Exit when projected queries and full-path budgets pass on the declared corpus/tier. |
| FA-006 | Coordinator, entity/contract files, route module and broad tests concentrate unrelated behavior. | Medium | Address after FA-001/002 characterization. Exit when cohesive modules own each workflow and public shapes remain stable. |
| FA-007 | Feature-level operational telemetry and recovery diagnostics are sparse. | Medium | Address with readiness work. Exit when privacy-safe metrics/traces cover all durable/native phases. |
| FA-008 | Architecture/handoff contain stale auth and conflicting deployment statements; no ADR owns the boundary. | Medium | Address now in the same delivery. Exit when current source, ADR, deployment and runbook agree. |
| FA-009 | Real PDF publication semantics and original numerical/visual parity are unverified. | Externally blocked | Do not guess. Keep preview-only and parity pending until authoritative stage maxima/mappings and user acceptance exist. |
| FA-010 | Retention/correction/erasure and LLM processing approval have no named decision owner. | High for public/AI release | Keep those releases disabled. Exit only with approved policy, owner and executable handling evidence. |

Literal zero debt is neither credible nor desirable if it means deleting tested legacy replay or implementing unsupported PDF/AI behavior. The accepted target is zero High release debt, no stale/dead feature configuration, and only explicitly bounded Medium/Low debt with owner and removal trigger.

### Implementation Roadmap

1. **Freeze the baseline and debt ledger.** Add FA-001 through FA-010 to the architecture debt register, add characterization tests for current import/freeze/event behavior, inventory any staged imports/unbound snapshots, and record the production topology decision. Gate: no structural extraction begins until existing behavior and orphan counts are known.
2. **Own indexes and readiness.** Implement the feature index manifest/audit, duplicate preflight and explicit deployment apply phase; remove `ApplyIndexesOnStartup`. Gate: disposable Mongo starts empty, receives exact indexes, rejects duplicate invariants, and readiness fails on a missing or altered index.
3. **Repair import publication.** Introduce the staged-to-published retry-convergent protocol and report-only reconciliation. Gate: failure after each result, revision, pointer and manifest phase retries to one published operation; shared readers never observe incomplete referenced facts.
4. **Repair freeze and field-event idempotency.** Recover unbound deterministic snapshots, define monotonic sequence semantics and make operation retries converge without duplicate effective context changes. Gate: concurrent freeze/event requests and phase failures yield one immutable outcome and valid replay.
5. **Align the confirmed separate-origin runtime.** Deploy the API as its native-dependency container, retain the SPA nginx image, remove the API's duplicate SPA build after parity, and record/promote an immutable compatible digest pair. Gate: native dependency/readiness plus exact-origin cookie/CSRF/CORS browser tests pass against the release pair.
6. **Replace broad reads.** Add projected active-result paging/detail and bulk historical inputs; apply document/count guards and measured limits. Migrate the SPA to additive paging while retaining compatibility. Gate: query plans use expected indexes and complete-path performance budgets pass without hidden data loss.
7. **Add production-boundary qualification.** Require real-Mongo, two-account, Chromium/WebKit, PWA offline/restart, container and recovery tests. Gate: a full match lifecycle and second-account shared-fact/private-state scenario pass against the built image.
8. **Decompose behavior-preservingly.** Extract cohesive API/Domain use cases, aggregate/contract files, SPA route modules and pure comparison helpers. Run the focused gate after each extraction. Stop when ownership is coherent; do not redesign the interface or create new projects/stores.
9. **Close operations and documentation.** Add privacy-safe telemetry, ADR, index/recovery/retention runbook and actual release evidence. Execute Impeccable `harden` and `adapt` evidence for the existing Operate UI. Mark each FA item closed, bounded or externally blocked and stop at this boundary.

An independent post-validation review is warranted because the work changes durable publication/freeze recovery, Mongo index correctness, authentication topology and multi-account data exposure. The reviewer should focus on interruption convergence, owner scoping, cookie/CSRF behavior, deployed-artifact identity, query bounds and whether any High debt was relabelled rather than closed.

### Testing Strategy

- Unit: deterministic operation IDs/hashes, phase transitions, retry decisions, BSON/count guards, cursor validation, DTO mapping, stage comparison, legacy package support and privacy-safe telemetry tags.
- In-memory behavior: retain existing account, CSRF, engine, parser, renderer, replay, verification, backtest, offline-store and route tests as fast characterization.
- Real Mongo integration in `FreezerAnalysis.Api.Tests`: exact index manifest, duplicate races, staged publication, pointer/finalization interruption, unbound snapshot recovery, event idempotency, serializer compatibility, two-database isolation, query projections and document guards.
- API/security: unauthenticated/expired/revoked sessions, wrong origin/CSRF, two accounts attempting every private lookup/mutation/download, shared-result read policy, stale revisions and rate limits.
- Browser/PWA: registration/login/return path, profile, import, identity mapping, draft/freeze, result detail/comparison, artifact download, offline field use, browser restart, queued sync, logout/account switch and forbidden cross-account IDs in Chromium and WebKit.
- Runtime/container: Poppler extraction, Chromium render, no external renderer fetch, private Blob/Mongo readiness, non-root operation, health routes and exact release-image digest.
- Performance: complete 1,000-match estimate path, paged 500-competitor/34-stage browsing, supported maximum upload, renderer queue under two users, artifact/private-object integrity and BSON/index storage report.
- Recovery/operations: dry-run staged/orphan audit, idempotent repair, index drift, Blob unavailable/checksum mismatch, process termination at each durable phase, rollback image reading all prior records and backup/restore replay.
- Frontend visual/accessibility: existing `PRODUCT.md`/`DESIGN.md` direction, responsive captures, keyboard/focus, screen-reader status, 200% zoom, touch size, no color-only status and no API caching.

### Migration And Rollback

Mongo changes are additive and compatibility-first. Audit duplicates and staged/orphan state before creating or changing unique indexes. Existing published revisions, snapshots, packages, artifacts, verification reports and parameter adoptions are not rewritten. New operation metadata must default safely when absent; legacy records remain readable, and report-only reconciliation must identify rather than guess incomplete ownership/content.

Deploy in this order: compatible readers and audit tooling; dry-run data/index report; idempotent indexes; retry-convergent writers; additive paged endpoints; qualified API container; compatible SPA image; cross-origin browser/production-like smoke; traffic activation. Keep old result-list behavior for one compatibility window. The API hostname and host-only cookie boundary remain stable; do not widen cookie domains.

Rollback restores the previous compatible image while retaining additive fields/indexes and private objects. Do not roll back by deleting snapshots/revisions, disabling CSRF, restoring automatic authentication or changing active pointers manually. If a new writer has entered a phase an old image cannot complete, disable mutations and run the matching new-version reconciliation before image rollback. Content-addressed orphan objects may be retained during the compatibility window; deletion requires proof that no manifest/snapshot/artifact references them.

No SQLite, cache, EF or cross-service data migration applies. AI remains disabled and PDF score publication remains preview-only, so their rollback is not applicable. Recovery limits and any legally required deletion that breaks replay must be reported explicitly rather than replaced with fabricated data.

### Open Questions

None identified for topology. The user confirmed separate SPA and API production origins on 2026-10-31. Exact deployed FreezerAnalysis hostnames remain environment-managed and must be captured in release evidence without placing secrets in source.

## Mock Authentication And Match Analysis Refinement Plan (2026-10-29)

### Outcome

Make the local mock composition behave like the product boundary rather than an automatically authenticated bypass: the SPA must open on login/registration when no valid FreezerAnalysis session exists, online routes and mutations must remain unavailable until authentication succeeds, and CSRF-protected profile operations must work through the AppHost development topology. Replace free-text division fields with a forced selection, explain the numerical meaning of personal performance, initialize match planning from the saved profile identity/division, and add an authenticated match-results workspace for individual Verify inspection and pairwise athlete/Pareto comparison.

Assumptions: this is a refinement of the existing FreezerAnalysis `Operate` interface, not a redesign. Production PublicAccount behavior, account-scoped identity mappings, immutable published results, frozen-plan semantics, offline field access and existing verification/backtest behavior remain unchanged. The user confirmed the authoritative division catalog and defined the requested Pareto as a pairwise HF chart: the X axis retains `Stage 1`, `Stage 2`, through `Stage X` in match order, while the Y axis shows hit factor and each stage contains one adjacent bar per compared athlete. It has no impact sorting, gap transformation or cumulative-percentage line.

Implementation boundary: stop when cookie-backed mock registration/login, guarded routes, division selection, profile-seeded planning, result detail and the confirmed comparison/Pareto view pass focused API, component and browser checks. Do not add PDF publication, automatic identity resolution, new scoring semantics, cross-account aliases, arbitrary chart builders or changes to the deterministic forecast engine.

### Current Evidence

- `FreezerAnalysis.Api/Program.cs` composes `UnavailablePublicAccountService` and `FreezerDevelopmentAuthenticationHandler` when `EnableMock` is true. The handler authenticates every request as the configured development account; consequently `FreezerAccountController.Session` reports an authenticated development session before login, while registration and credential login cannot succeed because the account service is unavailable.
- `FreezerAnalysis.Api/Controllers/FreezerAccountController.cs` contains an explicit Development shortcut in `Session`. Normal production session behavior already uses the host-local `freezer_account_session` cookie and `IPublicAccountService.ValidateSessionAsync`.
- `MorWalPizVideo.Domain/Interfaces/MockRepository.cs` already provides `PublicAccountMockRepository`, including account lookup, session creation, active-session validation and revocation over `IMockScenario`. `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs` owns password hashing, registration validation and eight-hour session creation. These are the appropriate mock account boundaries to reuse.
- `FreezerAnalysis.Api/Program.cs` applies `FreezerCsrfMiddleware` to every unsafe `/api` request. `frontend/fe-packages/services/src/apiTransport.ts` in `account` mode includes credentials and acquires `/api/v1/account/csrf` for ordinary unsafe requests, so the SPA should not manually duplicate token handling.
- `MorWalPizVideo.AppHost/Program.cs` serves the Vite SPA over HTTP and injects the API HTTPS endpoint through `VITE_FREEZER_API_BASE_URL`. Development cookies are emitted non-secure with `SameSite=Lax` by `FreezerAccountController`/`PublicAccountSecurity.CreateCookieOptions`. The current cross-scheme browser topology is not represented by the API's HttpClient tests and can withhold the CSRF cookie, producing the reported 403.
- `frontend/FreezerAnalysis.spa/vite.config.ts` has no API proxy. `freezerApi.ts` already supports a relative API base URL when `VITE_FREEZER_API_BASE_URL` is absent, so a local proxy can make browser requests same-origin without changing production configuration.
- `frontend/FreezerAnalysis.spa/src/main.tsx` returns empty/null loader data when unauthenticated instead of redirecting protected online routes. `RootLayout` renders a prompt in place of the outlet, but a development auto-session bypasses it. Offline field access intentionally uses the remembered account ID and IndexedDB and must remain a separate exception.
- `frontend/FreezerAnalysis.spa/src/routes.tsx` renders profile division and match-plan division as text inputs. `FreezerProfileRequest` and `FreezerDraftRequest` accept any string up to 32 characters; the coordinator only trims and uppercases it. There is no authoritative division catalog or server validation today.
- `FreezerAnalysis.Api/Services/FreezerPdfPairParser.cs` recognizes `OPEN`, `OPTICS`, `PRODUCTION`, `PCC IRON`, `PCC OPTICS`, `PRODUCTION OPTICS`, `REVOLVER` and `STANDARD`. The user confirmed these eight values as the complete product catalog. The CSV parser currently accepts source division text, so publication must diagnose values outside this catalog rather than create unselectable profile data.
- `FreezerAnalysisEngine.CurrentParameters.Performance` defaults to `0.935`. `ErrorCostSeconds` uses performance as the expected fraction of stage maximum points when converting lost points into equivalent seconds. It is not an athlete percentile, confidence score or forecast probability.
- The match route loader currently fetches only draft and estimate. `MatchPage` renders `draft.identityId` as a text input and therefore cannot present or enforce the account-owned identity selected in the profile. `GetDraftAsync` initializes a new draft from adopted model parameters but does not apply the profile identity, division or performance.
- Published `FreezerCompetitorResult` documents already contain athlete metadata and per-stage Verify facts: status, HF, time, points, A/C/D, misses, no-shoots and procedurals. The repository can read the active revision results, but no authenticated response contract exposes them for normal match inspection.
- The existing verification endpoint is snapshot-centric and calculates a persisted report only for the frozen profile athlete. It must remain separate from a general match result browser; expanding it to all athletes would mix immutable plan verification with shared result exploration.
- `frontend/back-office-spa` already uses repository-pinned `recharts` for responsive bar/line charts. FreezerAnalysis currently has no chart dependency. Reusing the same package/version is preferable to introducing another chart stack, while the accessible table remains the authoritative representation.
- `frontend/FreezerAnalysis.spa/PRODUCT.md` and `DESIGN.md` define an Italian, mobile-first match-day ledger with explicit uncertainty, semantic lists/tables and 44-pixel controls. The requested work fits an Impeccable `shape` followed by `harden` workflow in `Operate` mode; it must preserve this visual direction.

### Proposed Design

#### 1. Cookie-backed mock accounts

Use the same `PublicAccountSecurity` authentication scheme in mock and production. In mock mode, register a host-lifetime `IMockScenario`, scoped `PublicAccountMockRepository`, and scoped `PublicAccountService`; remove `UnavailablePublicAccountService` and the automatic development authentication handler from the normal local composition. Remove the Development shortcut from the account session endpoint so no cookie means `Authenticated = false` in every environment.

Seed one documented fictional account into the account mock scenario for fast login and keep registration enabled so developers can exercise first-run behavior. Do not auto-issue a session cookie. Split Freezer mock data into shared match facts and account-private sample state: the seeded account receives the existing profile/identity/draft sample, while a newly registered account starts with shared matches but no private profile or mappings. This preserves the privacy boundary and makes profile onboarding testable.

Every online loader except login must redirect unauthenticated users to `/login` with a sanitized `returnTo` path. After login or registration, revalidate the root session and return to that path. Keep the existing offline exception only for a previously cached field snapshot partitioned by the remembered account ID; offline mode must not expose profile, imports, result browsing or plan mutations.

#### 2. Same-origin local CSRF path

Configure the FreezerAnalysis Vite server to proxy `/api`, `/health` and only other required host endpoints to an internal API target supplied by AppHost. In local AppHost composition, leave `VITE_FREEZER_API_BASE_URL` empty so the browser uses relative URLs; inject a server-only proxy target instead. Preserve the explicit production API base URL and secure `SameSite=None` cookie behavior for the confirmed separately hosted Azure topology.

Do not disable `FreezerCsrfMiddleware`, relax origin checks or make the CSRF cookie readable. Add a real browser check that obtains the CSRF cookie through the Vite origin and completes profile `PUT`, identity `POST` and logout. API HttpClient coverage remains necessary but is not sufficient for this topology defect.

#### 3. Server-owned reference data and profile defaults

Add a small authenticated reference-data response owned by FreezerAnalysis contracts that returns stable division codes and Italian labels. The API validates profile and draft division against the same catalog; the SPA renders one reusable required `DivisionSelect` for profile and plan. An empty placeholder is allowed only before profile completion and cannot be submitted. Existing stored values outside the catalog remain displayable as an explicit legacy/unsupported option until corrected, avoiding silent data loss.

The authoritative catalog is `OPEN`, `OPTICS`, `PRODUCTION`, `PCC IRON`, `PCC OPTICS`, `PRODUCTION OPTICS`, `REVOLVER` and `STANDARD`, as confirmed by the user on 2026-10-29. Keep parser aliases separate from these canonical division codes and reject or stage imports containing an unknown division with an explicit diagnostic; do not silently coerce it.

Profile identity, division and performance become required planning defaults. When a draft does not yet exist, `GetDraftAsync` initializes them from the account profile. Existing drafts remain stable and are not overwritten by later profile changes. The match loader fetches match summary, profile and account identities alongside draft/estimate; the plan shows the selected identity by human-readable alias in a select, defaults to the profile identity, and permits an explicit per-draft override only among account-owned identities. If profile identity or division is absent, the match page presents a blocking link to complete the profile rather than accepting raw IDs.

Explain `Performance personale` next to both profile and plan controls: it is the expected share of the stage's maximum points used to estimate the time-equivalent cost of lost points; `0.935` means `93.5%`. Display and edit it as a percentage while preserving the existing API decimal contract. Include the accepted range and state that it does not directly represent ranking or forecast confidence.

#### 4. Match result read model

Add a read-only, authenticated match-results use case over the active published revision, separate from snapshot verification. Use two bounded endpoints/contracts:

1. A result index for the active revision with stage metadata and athlete summaries needed for filtering/selection: result ID, local competitor ID, display name, division, class, power factor, squad, official total, DQ/zeroed state and stage-result count.
2. A detail endpoint for one result in that active revision with the Verify stage facts already persisted on `FreezerCompetitorResult`: stage number/name/minimum rounds, status, HF, time, net/classification points, A/C/D, misses, no-shoots and procedurals.

Do not expose normalized names, account identity IDs, resolution documents, federation licenses, source object keys or private availability/exclusion decisions. Return not found when the result is not part of the active revision. Results are immutable read models; no edit endpoint is in scope.

On `matches/:matchId`, use route-level tabs or an equivalent ledger switch for `Piano`, `Risultati` and `Confronto`. `Risultati` supports division/status filtering, a scan-friendly athlete table and lazy detail expansion. `Confronto` requires exactly two distinct athletes from the same active revision and displays a stage-aligned comparison table before any chart. Missing, DQ and zeroed stages remain explicit and do not become zero-HF bars.

#### 5. Pareto comparison

Reuse `recharts` at the repository-pinned version. Keep chart derivation in a pure feature helper with unit tests; the server returns facts, not presentation-specific chart points. The chart aligns the two selected athletes by canonical stage ID, orders categories by `StageNumber` ascending, labels the X axis `Stage 1`, `Stage 2`, through `Stage X`, and plots each athlete's finite scored hit factor as an adjacent bar on the Y axis. It does not sort by HF, calculate a gap, aggregate stages or add a cumulative series.

Provide two athlete selectors and no metric selector: HF is fixed by the accepted requirement. The legend and accessible labels use the athlete display names; the tooltip and adjacent table show stage, athlete and exact HF. When either athlete has a missing, DQ, zeroed or non-finite stage result, omit that athlete's bar for the stage and expose the status in the table and accessible description. Color is supplementary; bar patterns/legend markers and text preserve identity without color. On mobile, keep stage order and readable labels through bounded horizontal scrolling rather than sorting or shrinking text below the existing design floor.

Material alternative rejected: returning all stage details for every athlete in the initial match loader. A supplied sample has 142 athletes and 1,704 stage rows; summary-first and lazy details reduce initial payload and route latency while supporting the complete workflow.

### Impacted Projects

- `FreezerAnalysis.Api`: mock account composition, session behavior, reference-data and match-result endpoints, coordinator validation/defaulting and local cookie/CSRF integration behavior.
- `MorWalPizVideo.Domain`: reuse `PublicAccountService`, `PublicAccountMockRepository` and `IMockScenario`; extend only FreezerAnalysis orchestration/repository reads if a bounded active-revision result method is required.
- `MorWalPizVideo.Models`: no new persisted aggregate is expected; verify-only use of existing result/stage entities. Add a canonical division type here only if persistence-level validation is chosen, otherwise keep it in the feature contract/application boundary.
- `MorWalPiz.Contracts`: reference-data, result summary/detail and stage DTOs; division validation contract.
- `MorWalPizVideo.MvcHelpers`: verify-only reuse of cookie authentication and CSRF helpers; no weakened security behavior.
- `frontend/FreezerAnalysis.spa`: guarded loaders, login return flow, reusable division select, profile legend/defaults, match tabs, result explorer, comparison/Pareto UI, responsive/accessibility styling and tests.
- `frontend/fe-packages/services`: verify-only account transport; change only if browser evidence proves a shared transport defect after same-origin local routing.
- `frontend/back-office-spa`: verify-only source for the established Recharts version/pattern; no behavior change.
- `MorWalPizVideo.AppHost`: same-origin local proxy wiring for the FreezerAnalysis SPA.
- `FreezerAnalysis.Api.Tests`: mock account/session, authorization, CSRF, reference-data, draft default and result-read behavior.
- Deployment workflows: verify-only. Production base URL and secure cross-origin cookies remain unchanged for this scope.

### Impacted Files

#### Modify

- `FreezerAnalysis.Api/Program.cs`: compose mock account repository/service with standard cookie authentication; remove automatic mock identity from the normal branch.
- `FreezerAnalysis.Api/Controllers/FreezerAccountController.cs`: remove Development auto-session behavior while preserving registration/login/session/logout and cookie names.
- `FreezerAnalysis.Api/Controllers/FreezerAnalysisController.cs`: add reference-data and bounded result read actions.
- `FreezerAnalysis.Api/Services/FreezerAnalysisCoordinator.cs`: validate canonical divisions, initialize new drafts from profile, and project active-revision result summaries/details.
- `FreezerAnalysis.Api/Services/FreezerAnalysisMockData.cs`: separate shared facts from the seeded account's private data and align that data with the mock account fixture.
- `MorWalPiz.Contracts/Contracts/FreezerAnalysis/FreezerAnalysisContracts.cs`: add reference-data and result read DTOs; apply canonical division validation without changing stored score facts.
- `frontend/FreezerAnalysis.spa/src/main.tsx`: protected online route redirects and expanded match loader dependencies.
- `frontend/FreezerAnalysis.spa/src/freezerApi.ts`: typed reference-data and result methods.
- `frontend/FreezerAnalysis.spa/src/routes.tsx`: login return flow, profile legend/select, profile-driven plan controls, match result tabs and comparison/Pareto surface.
- `frontend/FreezerAnalysis.spa/src/styles.css`: responsive ledger/table/chart states, focus and non-color status treatment.
- `frontend/FreezerAnalysis.spa/package.json` and the authoritative Yarn lockfile: add the repository-pinned Recharts dependency.
- `frontend/FreezerAnalysis.spa/vite.config.ts`: development API proxy.
- `MorWalPizVideo.AppHost/Program.cs`: inject the Vite proxy target and stop exposing the API HTTPS origin directly to the local browser.
- `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs`: account gate, cookie/CSRF, profile-to-draft and result endpoint regressions.
- `frontend/FreezerAnalysis.spa/src/routes.test.tsx` and `freezerApi.test.ts`: guarded route, select, legend, profile default, result and comparison tests.
- `frontend/fe-packages/services/src/apiService.test.ts` or the owning transport test file: verify account-mode CSRF remains isolated and correct through relative/proxied URLs.

#### Add

- A small reusable division-select component and pure comparison/Pareto derivation helper beside the FreezerAnalysis route modules, following the app's existing local feature ownership.
- Focused component/helper tests for division options, result filtering, stage alignment, ascending stage order and paired HF chart points.
- A Playwright browser test under the existing FreezerAnalysis browser-test convention for first-load login, registration/login, profile mutation through the Vite origin and responsive result comparison.

#### Generated/Migration

- Yarn lock metadata changes for Recharts in the FreezerAnalysis workspace.
- No MongoDB migration or backfill is expected. No generated chart assets are persisted.

#### Documentation

- This handoff is the planning source of truth and must be updated after implementation with actual decisions, files, validation and residual risks.
- Update `docs/architecture/backend.md`, `frontend.md`, `development.md` and `deployment.md` if implementation changes the documented development identity, local proxy or result-read contract.
- Update `frontend/FreezerAnalysis.spa/PRODUCT.md` or `DESIGN.md` only if implementation changes durable product/interface direction; the current `Operate` ledger direction already supports this refinement.

#### Verify Only

- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs`, `MorWalPizVideo.Domain/Interfaces/MockRepository.cs` and `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs`: reuse existing behavior; avoid feature-specific copies.
- `MorWalPizVideo.Domain/FreezerAnalysis/IFreezerAnalysisRepository.cs` and both repository implementations: existing active revision/result reads may be sufficient; add no query unless bounded result lookup cannot be expressed safely.
- `.github/workflows/deploy-freezer-analysis-spa.yml`: production API URL remains explicit in this scope.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | New read-only reference/result endpoints; draft creation uses saved profile defaults. Existing mutation routes and snapshot verification stay intact. |
| Frontend/UI | Login becomes the first unauthenticated online surface. Profile/plan use constrained selects. Match detail gains result and comparison views with an accessible Pareto chart. |
| Database/Data | No schema change. Mock account sessions and private sample data become host-lifetime in-memory state. Existing Mongo values outside the division catalog require tolerant display/correction. |
| Contracts/Models | Add reference-data and result summary/detail DTOs. Persisted score entities remain unchanged. |
| Authentication/Authorization | Mock uses real cookie sessions. No online API or SPA workflow is available before registration/login. Offline cached field snapshots remain account-partitioned. |
| Validation | Division is required and catalog-backed for profile/draft/import writes. Result IDs must belong to the active revision. The comparison requires two distinct athletes and plots only finite scored HF values. |
| Cache | Not applicable; these authenticated account/result reads are not output-cached. |
| Configuration/Secrets | Add only a local Vite proxy target. No credentials or deployed secrets are committed. Seed credentials, if supplied, are fictional Development-only values. |
| Logging/Telemetry | Log auth/result endpoint failures structurally without passwords, cookies, CSRF values, athlete names, licenses or full result payloads. Existing OpenTelemetry remains. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Local topology changes to proxy/same-origin. Production separate-host topology and secure cookie policy remain; verify no environment accidentally uses Development mock auth. |
| Backward Compatibility | Existing sessions/contracts remain in production. Existing drafts are not overwritten. Unsupported stored divisions stay visible until corrected. New result contracts are additive. |

### Reuse Opportunities

- Reuse `PublicAccountService`, `PublicAccountMockRepository`, `IMockScenario`, `PublicAccountSecurity` and the account transport instead of creating Freezer-specific credential/session logic.
- Reuse the current `FreezerAnalysisState` host-lifetime pattern for feature sample continuity, while keeping account and feature state ownership explicit.
- Reuse active revision/result repository methods and existing `FreezerCompetitorResult` stage facts; do not parse PDFs again for the match result UI.
- Reuse one `DivisionSelect` across profile and plan and one match-result DTO set across individual/comparison views.
- Reuse Recharts and responsive chart conventions already present in BackOffice, without moving feature UI into the shared layout package because there is only one consumer.
- Reuse React Router loaders and local component state; no new global store is warranted.
- Impeccable implementation workflow: run `shape` against `matches/:matchId`, then `harden` after behavior is complete. Minimum evidence is desktop and mobile captures of login, profile, individual results and two-athlete comparison, plus keyboard, 200% zoom, touch target and non-color-state checks.

### Risks

| Priority | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Mock mode can remain implicitly authenticated or expose seeded private data to newly registered accounts. | Use cookie auth in every mode; separate shared seed facts from account-private fixtures; test two accounts. | No-cookie API requests return 401/unauthenticated session; account B cannot read account A profile/draft/mappings. |
| High | CSRF continues failing in the real AppHost browser topology despite passing HttpClient tests. | Use relative browser URLs through Vite proxy and retain double-submit/origin checks. | Browser profile PUT, identity POST and logout succeed through `http://localhost:5178`; missing/mismatched CSRF still returns 403. |
| High | Result DTOs leak private identity mappings or personal identifiers not needed for analysis. | Explicit projection allow-list; omit normalized names, identity IDs, licenses and provenance keys. | Contract serialization tests and payload review show only approved fields. |
| Medium | Parser aliases or legacy free-text values drift from the confirmed eight-value division catalog. | Server-owned canonical catalog, explicit alias mapping, tolerant legacy display and diagnostic rejection of unknown new values. | Every accepted source division maps to one approved option; unknown stored/source values produce a diagnostic, not silent coercion. |
| Medium | Profile changes unexpectedly mutate an existing draft or frozen plan. | Apply profile defaults only at draft creation; preserve optimistic revisions and immutable snapshots. | Existing draft/snapshot remains byte-for-byte semantically unchanged after profile update. |
| Medium | Stage alignment or sorting compares the wrong HF values or changes the match sequence. | Join by canonical stage ID, sort only by stage number ascending, label both athletes and keep the table authoritative. | Fixtures with out-of-order inputs, missing/DQ/zeroed stages and unequal arrays produce the expected paired `Stage 1..X` sequence. |
| Medium | Loading all Verify rows harms mobile latency for large matches. | Summary-first endpoint, lazy details and exactly two selected athletes in comparison. | Initial match result payload contains no stage arrays; sample 142-athlete route remains responsive. |
| Low | Recharts produces inaccessible or unreadable output at narrow widths. | Pair chart with ordered table/text summary and test keyboard/zoom/mobile layouts. | Information remains complete with chart hidden and at 200% zoom. |

### Technical Debt

- Automatic Development authentication conflicts with the product session boundary and blocks meaningful first-run testing. It blocks this change and should be removed from the normal FreezerAnalysis mock path now.
- The current handoff and architecture text describe a Development identity. Update them after implementation; do not preserve stale behavior for documentation compatibility.
- Division values are currently parser-normalized strings without a domain catalog. This blocks reliable select validation and should be centralized now, while broader federation/ruleset versioning remains deferred.
- `routes.tsx` contains multiple large surfaces. Extract only the new result/comparison components and reusable select/helper needed for testability; a full route-file refactor is unrelated and deferred.
- The PDF preview already renders temporary Verify details, but those types are parser-specific and include page/pairing diagnostics. Reusing them for published match results would couple canonical reads to import preview debt; keep the new contracts separate.

### Implementation Roadmap

1. Establish mock account composition. Register host-lifetime account scenario state, `PublicAccountMockRepository`, `PublicAccountService` and standard cookie authentication in the mock branch; seed a fictional account without creating a session; remove automatic Development session behavior. Gate: session is false without cookie, registration/login issues a Freezer cookie, logout invalidates it, and protected API requests return 401 before login.
2. Correct local browser topology. Add the Vite proxy and AppHost proxy target, keep browser API URLs relative locally, and preserve deployed API base URL behavior. Gate: real browser login/profile/logout succeeds through the SPA origin while invalid CSRF and disallowed origins still fail.
3. Guard the SPA online route tree. Redirect unauthenticated loaders to login with return path, revalidate after auth, and preserve only the account-partitioned offline field exception. Gate: direct profile/match/result URLs cannot render or issue API calls before auth; successful login returns to the requested route.
4. Introduce canonical reference data. Add the confirmed eight-value division catalog to the server-owned response and validation, then create the reusable required select. Gate: profile/draft/import writes reject or stage unknown new values with diagnostics, all eight accepted options round-trip, and legacy unknown values are visible for correction.
5. Apply profile defaults to planning. Load profile/identities in the match route, initialize only new drafts from profile identity/division/performance, and render readable selects plus the performance legend. Gate: a completed profile produces a new draft with the same values; changing profile later does not alter existing drafts or snapshots.
6. Add bounded result read contracts/use cases. Project active-revision summaries and individual details without private fields and enforce result membership. Gate: individual mock athletes expose all five seeded Verify stages; unknown/stale result IDs return 404; a second account sees shared facts but not private mappings.
7. Build the match result workspace. Add `Piano`, `Risultati` and `Confronto` views, filters, lazy athlete detail, exactly two distinct athlete selectors and explicit incomplete-result states. Gate: desktop/mobile component tests cover individual, DQ, zeroed, missing and pairwise comparison states.
8. Implement the confirmed Pareto as a pure stage-alignment derivation and Recharts grouped-bar view paired with an ordered table. Gate: out-of-order source fixtures render `Stage 1..X` in ascending stage order; each stage shows up to two correctly labelled HF bars; missing/DQ/zeroed/non-finite values are omitted and explained; the chart remains understandable without color.
9. Run focused backend/frontend/shared transport suites, production builds and AppHost browser smoke checks. Execute the Impeccable `harden` pass and capture desktop/mobile evidence. Update architecture documents and this handoff with actual implementation and residual risks, then stop at the stated boundary.

An independent post-validation review is warranted because this work changes authentication behavior, CSRF development topology and the exposure of shared athlete result data in one delivery. The reviewer should focus on pre-auth leakage, cross-account isolation, contract minimization, Pareto correctness and mobile accessibility.

### Testing Strategy

- Backend unit/configuration: mock account repository/service registration, canonical division normalization/validation, profile-to-new-draft defaulting and pure result projection.
- API integration in `FreezerAnalysis.Api.Tests`: unauthenticated session and 401s; register/login/logout cookie lifecycle; missing/mismatched CSRF 403; two-account profile/draft isolation; division acceptance/rejection; active-revision summary/detail; stale/foreign result 404; no private fields in serialized payloads.
- Shared transport: account mode with relative base URL fetches CSRF once per base/session, includes credentials/header for unsafe calls and never adds bearer/admin headers.
- SPA component/router: first load and direct protected URL redirect; login return path; required division selects; performance percentage conversion/copy; profile identity default in plan; existing draft preservation; result filters/details; exactly two distinct comparison selections; loading/empty/error/offline states.
- Pure comparison tests: canonical stage alignment, ascending stage-number ordering, two HF series, equal HF values, missing/DQ/zeroed/non-finite values and mismatched athlete stage arrays.
- Browser/end-to-end: AppHost SPA-origin registration/login, profile save, plan load with profile defaults, individual Verify view, comparison/Pareto, logout and blocked revisit. Repeat representative captures at mobile and desktop widths, keyboard-only, 200% zoom and disconnected cached-field mode.
- Regression: existing import preview, identity review, estimate, freeze, offline queue, replay, verification, backtest, artifact rendering and production cookie tests remain green.

### Migration And Rollback

No MongoDB or SQLite migration is required. Existing production accounts/sessions use the same service, repository and cookie contract. Existing profile/draft division strings remain readable; only new writes become catalog-constrained, with an explicit legacy correction path.

Deploy backend additive result/reference contracts before the SPA that consumes them. Production authentication behavior should be unchanged because the composition correction is limited to `EnableMock`; verify this with a production-like host test. Deploy the SPA after API health and contract smoke checks. The local AppHost proxy change can roll back independently to the prior explicit base URL, but doing so reintroduces the known browser-CSRF risk and is not an acceptable steady state.

Rollback the match-result UI by removing its navigation while retaining additive read endpoints if needed. Existing published results and frozen snapshots are unaffected. Do not roll back by restoring automatic mock authentication or disabling CSRF. If canonical division validation rejects a legitimate source, temporarily retain tolerant writes behind an explicit Development-only compatibility decision while extending the catalog; never coerce the value silently.

Monitor 401/403 rates, CSRF acquisition failures, result endpoint latency/payload size and client chart errors after rollout. No backfill, dual write or destructive recovery step is needed.

### Open Questions

None identified. The user confirmed the eight-value division catalog and the pairwise stage/HF chart definition on 2026-10-29.

## Composition Alignment Fix Plan (2026-10-29)

### Outcome

Align `FreezerAnalysis.Api` with the repository's BackOffice/ServerAPI composition conventions: use Microsoft Feature Management instead of feature-specific booleans, select a complete mock/development composition locally without reading MongoDB configuration, and register request-facing services and repositories as scoped. Preserve production PublicAccount authentication, the separate platform/application MongoDB boundary, and singleton lifetimes only for thread-safe infrastructure handles such as `IMongoClient`, database accessors and Blob clients.

Assumptions: `FeatureManagement:EnableMock` selects the complete in-memory persistence branch and `FeatureManagement:EnableDev` selects development authentication only when the host environment is Development. Development defaults both flags to `true`; production defaults both to `false`. This follows current ServerAPI behavior and avoids the invalid mixed states allowed by `FreezerAnalysis:UseMockRepository` plus `FreezerAnalysis:EnableDevAuthentication`.

### Current Evidence

- `FreezerAnalysis.Api/Program.cs` currently reads `FreezerAnalysis:UseMockRepository` and `FreezerAnalysis:EnableDevAuthentication` directly. Mongo registration is controlled by the authentication flag, while the in-memory repository is controlled by a different flag. A mismatched or missing value can therefore invoke `FreezerAnalysisMongoRegistration.AddRepositories` during local startup.
- `FreezerAnalysis.Api/appsettings.Development.json` currently enables both custom booleans. These settings differ from the shared `FeatureManagement` shape used by BackOffice, ServerAPI and ShortLinks.
- `MorWalPizVideo.BackOffice/Program.cs` and `MorWalPizVideo.ServerAPI/Program.cs` register Feature Management, read `MyFeatureFlags.EnableMock`/`EnableDev`, reject mock mode outside Development/Test, and choose one mock or production repository branch.
- `docs/architecture/development.md` explicitly assigns FreezerAnalysis local development to a development identity and in-memory feature repository, with no MongoDB dependency.
- `FreezerAnalysis.Api/Program.cs` registers `InMemoryFreezerAnalysisRepository`, `UnavailablePublicAccountService`, CSV/PDF parser services and the document renderer as singletons. The coordinator is scoped.
- `FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs` correctly scopes `IFreezerAnalysisRepository` and `IPublicAccountService`, but registers `IPublicAccountRepository` as singleton. It also validates the two database names before constructing production Mongo access, which must remain production-only.
- `FreezerAnalysis.Api/Services/PrivateObjectStorage.cs` registers local file or Azure Blob storage and Blob clients as singletons. These are infrastructure adapters without request-owned state and are not part of the scoped-lifetime correction unless implementation finds mutable request state.
- `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs` proves Development-host workflows, but it relies on the custom development settings and resolves repositories from the root provider in some tests. It does not assert Feature Management selection or DI lifetimes.

### Proposed Design

Register Microsoft Feature Management in the FreezerAnalysis composition root and use the existing shared `MyFeatureFlags.EnableMock` and `MyFeatureFlags.EnableDev` names. Remove `UseMockRepository` and `EnableDevAuthentication` from the FreezerAnalysis options/configuration contract rather than supporting two parallel switches.

Use one repository branch:

1. When `EnableMock` is true, require Development or Test, register development/in-memory dependencies only, and do not call the Mongo registration helper or read Mongo database settings.
2. When `EnableMock` is false, register both platform-account and FreezerAnalysis Mongo repositories and retain the distinct-database validation.
3. When `EnableDev` is true, allow development authentication only in Development (and an explicit test authentication path in Test). Otherwise use PublicAccount cookie authentication. `EnableMock` and `EnableDev` remain conceptually separate, but Development configuration enables both so the normal local path is fully offline.

Register `IFreezerAnalysisRepository`, `IPublicAccountRepository`, `IPublicAccountService`, `UnavailablePublicAccountService`, `FreezerAnalysisCoordinator`, `FreezerCsvParser`, `PdfTextExtractor`, `FreezerPdfReportDetector`, `FreezerPdfPairParser`, and `FreezerDocumentRenderer` as scoped request services. Keep `IMongoClient`, `FreezerAnalysisDatabase`, `BlobContainerClient`, immutable options infrastructure, and private object storage adapters singleton where they are thread-safe and hold no request/account state. Do not make the in-memory data disappear between HTTP requests: if its current repository instance owns the data store, separate that store into a singleton feature-state holder and keep the repository adapter scoped, matching the repository convention without losing local workflow continuity.

Material alternative rejected: mapping the old custom booleans into Feature Management for backward compatibility. The application is not production-released, and retaining both configuration surfaces would preserve contradictory states and make startup behavior harder to reason about.

### Impacted Projects

- `FreezerAnalysis.Api`: composition root, package dependency, configuration schema, mock/production branch and service lifetimes.
- `FreezerAnalysis.Api.Tests`: feature-selection, startup, scope/lifetime and no-Mongo local regressions.
- `MorWalPizVideo.Domain`: only if the in-memory repository needs a separate singleton state holder so a scoped adapter preserves data across requests.
- `MorWalPizVideo.MvcHelpers`: verify-only reuse of `MyFeatureFlags` and any existing disabled-feature handler; no new FreezerAnalysis-specific flag constants.
- `MorWalPizVideo.BackOffice` and `MorWalPizVideo.ServerAPI`: verify-only reference implementations; no behavior changes.
- `frontend/FreezerAnalysis.spa`, contracts, models, Mongo schema, AppHost and deployment topology: no contract or product behavior change; verify local startup integration only.

### Impacted Files

#### Modify

- `FreezerAnalysis.Api/Program.cs`: add Feature Management, derive `EnableMock`/`EnableDev`, select one coherent composition branch, and change request-facing registrations to scoped.
- `FreezerAnalysis.Api/FreezerAnalysis.Api.csproj`: add a direct `Microsoft.FeatureManagement.AspNetCore` dependency at the repository-standard version instead of relying on a transitive reference.
- `FreezerAnalysis.Api/appsettings.json`: add production-safe `FeatureManagement` defaults and remove the two custom mode booleans from `FreezerAnalysis`.
- `FreezerAnalysis.Api/appsettings.Development.json`: enable `EnableMock` and `EnableDev` under `FeatureManagement` so ordinary local startup requires no database connection.
- `FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs`: scope `IPublicAccountRepository`; retain singleton Mongo client/database infrastructure and distinct-name validation.
- `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs`: assert branch selection and service lifetimes, resolve scoped dependencies through scopes, and prove Development startup/workflows do not require Mongo configuration.
- `docs/architecture/backend.md` and `docs/architecture/development.md`: replace the current description of explicit custom FreezerAnalysis branches with the implemented shared feature-flag convention.

#### Add

- A focused test fixture or test-only factory under `FreezerAnalysis.Api.Tests` only if needed to construct Development, Test and Production-like configurations without repeating configuration setup.
- A feature-owned in-memory state holder under `MorWalPizVideo.Domain/FreezerAnalysis` only if repository inspection confirms that scoping the current repository would otherwise reset data on every request.

#### Generated/Migration

None. No MongoDB, API-contract or data migration is required. Restore metadata may change because of the direct package reference; generated build output remains untracked.

#### Documentation

- This handoff records the accepted correction and implementation evidence.
- The backend/development guides should state that FreezerAnalysis uses shared Feature Management and that Development defaults to the offline mock composition.

#### Verify Only

- `MorWalPizVideo.MvcHelpers/Utils/MyFeatureFlags.cs`: reuse `EnableMock` and `EnableDev` unchanged.
- `MorWalPizVideo.BackOffice/Program.cs`, `MorWalPizVideo.ServerAPI/Program.cs`, and their settings: verify the intended composition pattern and package version.
- `FreezerAnalysis.Api/Services/PrivateObjectStorage.cs`: confirm singleton adapters are stateless/thread-safe; do not change lifetime merely for uniformity.
- AppHost local launch: confirm it supplies Development environment and does not override the new flags with stale custom keys.

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Startup composition only; endpoint routes and responses remain unchanged. |
| Frontend/UI | No UI change. Local SPA/API startup becomes database-independent. |
| Database/Data | Production two-database validation remains. Development must not parse or connect to Mongo settings. In-memory data must persist across request scopes for the host lifetime. |
| Contracts/Models | No HTTP or persisted-model changes. |
| Authentication/Authorization | Development identity is gated by Development plus `EnableDev`; production PublicAccount cookies remain unchanged. Mock mode remains forbidden outside Development/Test. |
| Validation | Add startup validation for unsafe flag/environment combinations and tests for coherent branch selection. |
| Cache | Not applicable; no cache registration changes. |
| Configuration/Secrets | Replace custom mode keys with shared Feature Management keys. No credentials or fallback connection strings are introduced. |
| Logging/Telemetry | Preserve ServiceDefaults. Log the selected provider mode without database names, connection strings or secrets if startup diagnostics already support this convention. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Production defaults stay real and fail closed when required Mongo configuration is absent. Development defaults become offline. Environment overrides using old keys must be removed before deployment. |
| Backward Compatibility | Endpoint/data compatibility is unchanged. Configuration-key compatibility is intentionally broken before release to eliminate contradictory switches. |

### Reuse Opportunities

- Reuse `MyFeatureFlags.EnableMock` and `EnableDev`, `AddFeatureManagement`, and the environment guards already established in BackOffice/ServerAPI.
- Reuse WebApplicationFactory and existing authenticated FreezerAnalysis HTTP workflows to prove the local branch end to end.
- Preserve `FreezerAnalysisMongoRegistration` as the single production database boundary instead of moving Mongo construction back into `Program.cs`.
- Reuse the existing in-memory repository behavior; extract only its durable host-level state if scoped registration requires it.

### Risks

| Priority | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | A scoped in-memory repository may reset all local data if the repository instance currently owns its collections. | Separate singleton state from scoped repository behavior before changing the lifetime. | Data created in one HTTP request is available in a later request, while repository instances differ across scopes. |
| High | Incorrect flag combinations could re-enable development auth or mocks in production. | Require environment checks independently for `EnableMock` and `EnableDev`; production defaults false. | Production-like host rejects unsafe flags and never resolves development auth/mock repositories. |
| Medium | The local host may still touch Mongo through account services, health checks or eager construction. | Keep every Mongo/account registration and Mongo health check inside the non-mock branch. | Development host starts with Mongo keys removed and completes profile/import/freeze smoke workflows. |
| Medium | Scoped services may be resolved from singleton consumers or the root provider. | Enable scope validation in tests and inspect constructor chains. | Provider builds with scope validation and each HTTP integration test resolves successfully. |
| Low | Old environment overrides continue setting removed custom keys and silently select production mode. | Search deployment/AppHost settings, remove stale keys, and document the new names. | Repository/configuration search finds no active `UseMockRepository` or `EnableDevAuthentication` references. |

### Technical Debt

- The current custom booleans duplicate shared feature flags and permit incoherent combinations. This blocks predictable local startup and should be removed now.
- The in-memory repository may combine adapter and host-lifetime state ownership. Address only the minimum state extraction needed for scoped DI; do not introduce the full cross-application `IMockScenarioLifecycle` unless multiple scenarios are required by accepted FreezerAnalysis scope.
- Private object storage remains environment-selected rather than feature-selected. This does not cause the Mongo startup defect and does not block this correction; defer unless offline storage behavior fails a focused test.

### Implementation Roadmap

1. Add the direct Feature Management dependency and configure `AddFeatureManagement` in the API host. Reuse shared flag constants; do not add feature-specific aliases. Gate: configuration can resolve `EnableMock` and `EnableDev` from the standard section.
2. Replace the two custom mode booleans with one mock branch and one authentication branch. Put Mongo repositories, platform account services and Mongo health checks exclusively in the non-mock branch. Gate: a Development host with all Mongo keys absent starts and serves an authenticated API request.
3. Convert request-facing repositories and feature services to scoped. If the in-memory implementation owns collections, extract only those collections/locks into singleton host state and inject that state into the scoped repository. Gate: separate scopes produce separate service/repository adapters while data survives sequential requests.
4. Update base and Development settings. Production-safe defaults are false; Development enables both flags. Remove all active references to the old keys, including launch/AppHost/environment mappings if present. Gate: repository search and configuration tests show one source of truth.
5. Add focused tests for service descriptors, environment guards, Development no-Mongo startup, production-like missing-configuration failure, Mongo branch lifetime, and an HTTP create-then-read workflow across requests. Gate: narrow API tests pass with scope validation.
6. Run the full FreezerAnalysis API suite and local API/SPA smoke path, then update backend/development documentation and this handoff with actual files/results. Stop after composition parity is proven; do not expand into mock scenarios, feature-gated endpoints, storage redesign or deployment.

Independent post-validation review is not required for this bounded composition correction if the focused lifetime and startup tests pass. It becomes warranted if implementation must extract shared mutable mock state or changes production authentication/database construction beyond the registration branch described here.

### Testing Strategy

- Unit/configuration: standard feature keys resolve expected booleans; obsolete keys have no effect; unsafe mock/dev-auth flags are rejected outside permitted environments.
- DI: inspect service descriptors for scoped request-facing services/repositories and singleton infrastructure; build with scope validation; resolve two scopes and compare adapter identities.
- Integration: Development WebApplicationFactory with Mongo settings removed starts successfully, exposes the development identity, and completes create/read operations across separate requests.
- Production-like integration: with flags false, missing platform/application Mongo configuration fails with the existing actionable validation; equal database names remain rejected case-insensitively.
- Regression: existing import, profile, estimate, freeze, replay, verification, renderer and PDF tests remain green. No frontend contract test changes are expected.
- Security: Development auth requires Development plus `EnableDev`; mock mode cannot activate in Production; production cookie authentication remains selected when `EnableDev` is false.

### Migration And Rollback

No data migration is needed. Apply the configuration rename atomically with the host change. Deployment settings must define the standard flags or rely on production defaults; remove old custom keys to avoid operator confusion.

Rollback is the previous application image plus its matching configuration keys. Do not partially roll back only the settings because the old and new binaries read different keys. Mongo documents, account sessions and frozen plans are unaffected. The scoped lifetime change has no persisted-data rollback requirement; if local in-memory continuity fails, restore the prior lifetime only in Development/Test while correcting state ownership, never as the production design.

### Open Questions

None identified. The requested alignment, standard flag names, environment policy, lifetime boundary and no-database local behavior are all established by current repository conventions.

### Implementation Evidence

- `FreezerAnalysis.Api/Program.cs` now uses `FeatureManagement:EnableMock` and `FeatureManagement:EnableDev` through the shared `MyFeatureFlags` constants. Mock mode is a single coherent branch that registers development authentication, the scoped in-memory repository and unavailable platform-account service without invoking Mongo registration. Production mode retains the existing PublicAccount/Mongo branch and separate database validation.
- `FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs` now registers `IPublicAccountRepository` as scoped. `IMongoClient`, `FreezerAnalysisDatabase` and private object-storage infrastructure remain singleton infrastructure handles.
- `MorWalPizVideo.Domain/FreezerAnalysis/InMemoryFreezerAnalysisRepository.cs` now uses a singleton `InMemoryFreezerAnalysisState` with scoped repository adapters, preserving local data across HTTP requests while matching request lifetime conventions.
- `FreezerAnalysis.Api/appsettings.Development.json` enables `EnableDev` and `EnableMock` under `FeatureManagement`; the old custom mode keys were removed from both base and Development settings. The API project now declares the repository-standard Feature Management package directly.
- `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs` covers Mongo-free Development composition, scoped adapter identity, shared mock state, and explicit scopes for direct repository test setup.
- Validation: `dotnet test FreezerAnalysis.Api.Tests\\FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore --logger "console;verbosity=detailed"` passed with 73 tests passed, 0 failed and 2 skipped. Restore was required after adding the direct package reference. The VS Code test controller did not discover this xUnit file, so the project test command is the authoritative check.
- No frontend, HTTP contract, persisted schema or deployment changes were required. The AppHost terminal had a pre-existing failed `dotnet run` attempt; this correction was validated through the focused API project suite rather than claiming an AppHost smoke run.

## Implementation Status

The current branch contains an end-to-end application foundation, but it does not yet satisfy the M0-M8 completion boundary. Do not treat the presence of a route, DTO, pure calculation helper or repository method as proof that the corresponding workflow is complete.

| Module | Current evidence | Status and remaining acceptance blockers |
|---|---|---|
| M0 - Scope and numerical specification | Requirements and conservative insufficient-data behavior are captured here; independent tests now cover exact error costs, Charlie rounding, turn wrapping, inclusive rhythm boundaries, two-way model recovery, inverse-variance informant combination, optional adjustment factors, division change, verification error/interval/beta calculations, finite-input handling, insufficient history, and short/medium/long stage-group boundaries. Verification uses sample standard deviation; stage groups are explicitly keyed by minimum rounds: short 1–12, medium 13–24, long 25+. | Partial. Full written-specification boundary coverage is not complete, including renderer/layout rules and import reconciliation where source semantics are not yet established. |
| M1 - Host, session boundary and contracts | Separate API and SPA projects, account-mode shared transport, host/AppHost/solution wiring and host tests exist. Production Mongo registrations are centralized in the API composition helper; no-network tests confirm the FreezerAnalysis database name is distinct, both Mongo repositories resolve, and equal database names are rejected case-insensitively. | Partial. Live two-database isolation, account/session/CSRF/origin regressions across both hosts, deployment-origin behavior and complete contract conformance are not proven end to end. |
| M2 - Facts, import and history | Versioned CSV import/publication, roster parsing, source storage and Mongo/in-memory repository paths exist. PDF extraction preserves page breaks; the authenticated SPA offers paired score previews with classification and stage fields, source pages, pair status and coverage diagnostics without publishing. The PDF preview also exposes parsed DQ appendix rows, page numbers, mapping status and mapped/unverified counts; classification and Verify rows show a DQ status only when the appendix entry uniquely joins both reports. Account-owned shooter identities, alias candidates and explicit revision-checked result mappings are available in the API and SPA. | Incomplete. The opt-in local sample test parses all 142 classification rows and 142 verification sections, including all 1,704 stage rows, across the 9-page and 48-page reports. Division/name pairs were unique across this sample pair. All seven DQ appendix rows parse, but none uniquely maps to a classification and Verify entry; they remain visible and unverified rather than being guessed. The preview's advisory four-decimal hit-factor/time calculation flags 11 stage rows; its scoring semantics are not approved. PDF scores are not mapped into canonical results, no total reconciliation is performed, and publication remains disabled. Shared cross-account identity/alias governance and legal data-processing requirements also remain. |
| M3 - Numerical engine | A deterministic log-space two-way fit and registry/forecast/verification helpers exist in Domain. The estimate use case composes strictly earlier published matches, account-owned availability/exclusion snapshots, and explicitly mapped identities; independent tests cover known model recovery, inverse-variance informants, adjustment factors, strict date cutoff, division change, verification error/interval/beta calculations, deterministic order and unavailable inputs. Frozen inputs now support deterministic forecast replay using the frozen parameter and adjustment values. The engine identity records sample dispersion and the stage-group boundary convention; the prior engine identifier remains explicitly supported for numerically unchanged frozen replay/backtests. | Incomplete. Full M3 specification coverage and independent synthetic verification results remain outstanding. Only the most recent 1,000 prior matches are considered; prior shared matches without this account's published availability manifest and unresolved identities are explicitly excluded. Miss/Charlie costs remain null pending verified semantics. |
| M4 - Planning, freeze and verification | Profile/draft/freeze, immutable snapshot/package, field-event and artifact workflows are present; the SPA includes private identity review, profile binding, replay and final-verification views. The verification route uses the match's current final revision only when the account owns its published import. Stage forecasts are available when required inputs are sufficient. Frozen package schema v2 contains historical scoring projections, applied parameters, source/revision provenance, explicit identity mappings, availability/exclusion sets, target stage definitions and the estimate; its private object reference records compressed length and hash. Authenticated replay recalculates from those frozen inputs, reports unsupported legacy packages explicitly, compares against both the package estimate and persisted snapshot, and does not consult later match revisions. The verification API persists idempotent per-stage, aggregate and short/medium/long group error/interval metrics with explicit denominators, plus observed beta. Informant drift is computed as `(final informant HF / exp(frozen informant log-level + frozen beta × frozen stage log-difficulty)) - 1` for account-mapped, same-division candidates selected by the frozen registry. The report records scored comparison count, sample SD and missing/DQ/zeroed/unverifiable exclusions without exposing identity details. This is a documented provisional calculation assumption pending original-reference parity. Parameter backtests recalculate selected account-owned schema-v2 snapshots using each snapshot's own frozen history and an explicitly selected distinct final revision; reports are persisted account-scoped with per-stage and aggregate error/coverage measures. Revision-checked account adoption accepts only supported backtests with verifiable results and only after explicit confirmation; adoption is applied to newly created drafts and never changes frozen snapshots or existing drafts. | Partial. Replay, package provenance, final-revision verification, idempotent report retrieval, group and informant-drift metrics, backtest stability after later publication, account adoption, expected-revision conflict, and adopted-parameter carry-forward are covered by API/SPA tests. Real-Mongo concurrency qualification remains open. Backtests require supported frozen-package schema and numerically compatible engine versions; unsupported races are marked unavailable and prevent adoption. |
| M5 - Documents and AI proposals | Server renderer and authorized artifact path exist. Focused Playwright tests now render A5/A4 PDFs and exact 1290x2796 phone PNGs locally, and verify that over-capacity content fails instead of being silently shrunk or truncated. | Partial. Clean pinned production Chromium/Poppler packaging, PDF page-size/content/overflow evidence and container renderer failure/idempotency qualification remain. AI proposals/provider boundary and explicit acceptance are not implemented. |
| M6 - Mobile/offline | SPA account, CSV import, paired PDF preview, profile, identity review, draft/freeze, field, frozen-input replay, final-verification, multi-race parameter-backtest and explicit account-adoption workflows, offline snapshot and event queue foundations exist. PDF preview displays its no-publication boundary, keeps extracted rows expandable for review, and presents DQ appendix rows with explicit matched/ambiguous/unmatched status and page provenance; unresolved DQ does not become a non-DQ status in competitor tables. Verification now displays group counts/coverage denominators, the minimum-round grouping convention, and informant-drift comparison/exclusion counts separately from the frozen plan. Replay, verification and candidate reports remain separate from frozen plans. Backtest controls are disabled offline; changing a race selection or parameter invalidates the previous report and approval, and adoption requires a separate confirmation. IndexedDB operations now resolve writes only after transaction commit; account-indexed snapshots and queued events are deleted on logout or successful account switch, and a shared-device warning explains that local data is not encrypted. Reconnect synchronization requires a live session for the queued account, verifies snapshot ownership on the server, and relies on server idempotency before removing the queued operation. | Partial. Focused tests cover account-scoped deletion, logout, login account-switch cleanup, reauthentication/ownership checks and queued-event replay; real airplane-mode browser evidence, accessibility and artifact visual evidence remain. |
| M7 - Historical transition and operations | Solution/AppHost/workspace integration and architecture documentation are present. A multi-stage FreezerAnalysis container currently builds the SPA, publishes the API, includes Poppler and the Playwright-pinned Chromium runtime, and copies the SPA into the API image. CI includes the feature SPA tests/build, API renderer browser setup/tests, and container build. Dockerfile-specific context exclusions prevent the supplied sample PDFs and unrelated workspace data from entering the image build context. The user has since confirmed separate production SPA/API origins, so the duplicate SPA layer in the API image is transitional rather than the target deployment shape. | Incomplete. Local Docker image construction could not run because the Docker daemon is unavailable; GitHub Actions has not run. The API deployment still publishes a ZIP rather than the native-dependency image. Cross-origin browser qualification, retention/erasure and recovery procedures, historical migration/import proof, indexed 1,000-match report, least-privilege checks and recovery drill remain. No deployment has been performed. |
| M8 - Qualification | Focused component/host tests and a synthetic 500-competitor/34-stage fit benchmark have run locally. Twenty isolated warm engine-fit samples measured p95 119.47 ms (min 102.03 ms, max 135.26 ms) on this workstation. The benchmark runs in a non-parallel xUnit collection to avoid other suite work contaminating the measurement. | Not started as a release gate. The measurement covers engine fitting only, not the complete estimate/persistence/artifact path or the declared deployment resource tier. Performance budgets, two-concurrent-user lifecycle, complete two-account isolation, real Mongo proof, full container/browser checks, execution of the manual acceptance checklist below, and independent review remain. No deployment has been performed. |

### Validation recorded

- `FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH=<local Poppler pdftotext.exe> dotnet test FreezerAnalysis.Api.Tests\FreezerAnalysis.Api.Tests.csproj --no-restore` passed after the PDF DQ appendix and Mongo-registration boundary changes: 74 passed, 0 skipped. This includes the supplied-PDF sample, missing-counterpart report case, distinct database registration and shared-name rejection. The Mongo tests do not contact a live server. The documented Development API and Vite commands also started locally; `/health` and the SPA root returned HTTP 200, and `/api/v1/account/session` reported `authenticated: true` for the Development identity. Both temporary servers were stopped after the check.
- `FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH=<local Poppler pdftotext.exe> dotnet test FreezerAnalysis.Api.Tests\FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore`: passed, 64 tests, 0 skipped after the verification-group and informant-drift changes. Coverage includes account-scoped identity review, estimate composition from a synthetic prior match, strict same-day exclusion, missing account availability diagnostics, insufficient-history behavior, deterministic forecast ordering, independent model recovery, inverse-variance informants, optional adjustments, division-change selection, minimum-overlap boundaries, exact error costs, Charlie half-up rounding, rhythm thresholds, independent verification metrics, missing-informant denominator diagnostics, frozen-registry informant drift, per-stage/group error and interval metrics, frozen-package provenance/name minimization, replay matching the frozen estimate and remaining unchanged after later publication and account parameter adoption, account-scoped snapshot/replay, final-revision verification, idempotent report retrieval, multi-race candidate backtest persistence and recalculation unchanged after later publication, explicit adoption confirmation, stale adoption conflict, adopted-parameter initialization/preservation in new drafts, idempotent field-event append/retry and unknown-snapshot conflict, legacy package length compatibility with SHA-256 integrity, synthetic PDF pair parsing/extraction, and real local Chromium rendering/overflow checks.
- `dotnet test FreezerAnalysis.Api.Tests\FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore --logger 'console;verbosity=detailed'`: passed, 68 tests, 0 skipped. The isolated synthetic 500-competitor/34-stage fit's 20 warm samples measured p95 119.47 ms (min 102.03 ms, max 135.26 ms). This is engine-fit-only workstation evidence, not the complete estimate/resource-tier/concurrency budget.
- `dotnet test FreezerAnalysis.Api.Tests\FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~FreezerPdfExtractionIntegrationTests`: passed, 1 synthetic PDF extraction-to-parser integration test with Poppler.
- `dotnet test FreezerAnalysis.Api.Tests\FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~FreezerDocumentRendererTests`: passed, 2 tests after installing the pinned Playwright Chromium revision in the local test environment. Confirmed A5/A4 PDF signatures, exact 1290x2796 phone PNG dimensions, and explicit failure on over-capacity content. This is local renderer evidence, not clean-container qualification.
- `docker build --no-cache --file FreezerAnalysis.Api\Dockerfile --tag freezer-analysis-validation .`: not run successfully; Docker CLI is present but its daemon pipe is unavailable in this environment. The CI workflow YAML parses successfully; its container job remains unexecuted locally.
- `FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH=<local Poppler pdftotext.exe> dotnet test FreezerAnalysis.Api.Tests\FreezerAnalysis.Api.Tests.csproj --no-build --filter FullyQualifiedName~FreezerPdfPairSampleTests`: passed, 1 supplied-PDF sample test. It asserts 9/48 pages, 142/142 parsed entries, 1,704 stage rows, 11 advisory hit-factor/time mismatches, seven parsed DQ appendix rows with zero unique mappings and seven unresolved entries, and `CanPublish == false`. It emits no competitor names or license values.
- A local aggregate-only investigation compared all 142 uniquely paired classification entries with stage-normalized totals. A candidate using the maximum observed hit count per stage and precise `reported points / time` hit factors came within 0.02 points for 142/142 entries (maximum difference 0.000499). This is **not accepted reconciliation**: neither PDF provides an authoritative course/stage maximum, and deriving it from observed hits is explicitly disallowed. Raw stage-point sums alone matched only 1/142. The preview therefore continues to report missing stage-maximum evidence and `CanPublish == false`; no score was imported or published.
- `yarn --cwd frontend workspace freezer-analysis.spa build`: passed after the DQ preview update; TypeScript build and Vite/PWA production output completed. Browserslist data is outdated; no dependency update was made.
- `yarn test --run` from `frontend\FreezerAnalysis.spa`: passed, 20 tests across the API client, offline store and routes, including DQ appendix counts and unresolved-row display without implying a non-DQ status. The React Router harness still emits its existing missing `HydrateFallback` warning.
- `yarn --cwd frontend workspace freezer-analysis.spa test --run`: passed, 20 tests, including account-partitioned offline snapshot/event storage, logout and account-switch cleanup, reconnect authentication and ownership verification before queued-event append, replay endpoint wiring, deterministic replay result rendering, paired PDF request wiring, preview-only behavior, file validation, final-verification access gating/report rendering including group coverage denominators and informant-drift-unavailable state, multi-race parameter selection/backtest, explicit adoption confirmation, identity assignment and stale-revision error states. React Router still reports the expected missing `HydrateFallback` warning in the test harness.
- Impeccable mechanical detector on the changed SPA route and stylesheet: no findings. A live browser capture was not available, and the detector reported that no approved design-comp/build-phase state exists; visual screenshot qualification remains open under M8.
- `yarn workspace @morwalpizvideo/services test --run` from `frontend`: passed, 44 tests.
- `dotnet build MorWalPizVideo.AppHost\MorWalPizVideo.AppHost.csproj --no-restore`: passed. Restore/build output included existing vulnerability warnings for unrelated dependencies.
- `python -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml', encoding='utf-8'))"`: passed after adding FreezerAnalysis API build/test jobs. GitHub Actions itself has not run this workflow change.

The FreezerAnalysis API build and test project are now included in `.github/workflows/ci.yml`; this workflow change has not yet been executed by GitHub Actions. These are focused checks only. They do not prove the unimplemented module gates above, production packaging, real-Mongo behavior, mobile-browser behavior, deployment readiness or original-application parity.

Identity resolution is deliberately account-scoped in this delivery: names and user-entered aliases can offer exact candidates only within the current account, and the user explicitly confirms each match. Published score facts remain immutable. Estimate composition uses only prior published history for which the current account owns a published import manifest and explicit result mappings. Freeze stores the exact reduced numerical history, identity-resolution map, availability/exclusion snapshot, source revisions and target stage definitions used for that forecast. This does not yet satisfy the planned shared-alias model, federation/license resolution, legal notice/retention approval, frozen identity-revision governance, or independent deterministic replay requirements.

The estimate integration test exposed that full division names accepted by imports (for example `PRODUCTION`) exceeded the existing eight-character profile/draft contract and SPA input limit. Both existing division DTO fields and their inputs now allow up to 32 characters so accepted imported divisions can be selected without lossy abbreviations.

The supplied PDFs were extracted and parsed locally with a user-scoped Poppler installation. The opt-in test asserts aggregate coverage only; no athlete names or license values are written to this handoff or emitted by the test. This is parser-preview evidence, not validated scoring semantics, canonical-result import, total reconciliation, publication, or production-runtime proof.

## Outcome

Replace the conversation-and-files workflow described in "Studio Pre-gara IPSC - Architettura esistente e punti cardine" with `FreezerAnalysis.Api` and `frontend/FreezerAnalysis.spa`. Implement the documented numerical behavior, automate acquisition and preparation where evidence permits, support multiple platform accounts, generate documents on the server, and make the three field workflows practical on a phone. Exact equivalence to the original application is a later user-acceptance check, not a prerequisite for implementation.

The supplied brief was read in full from the main checkout, outside this worktree: [original brief](<../../../../MorWalPizVideo/docs/Studio Pre-gara IPSC — Architettura esistente e punti cardine.md>). That link is checkout-layout-dependent. The Python project, rule documents, PDFs, CSV archive, frozen JSON, and expected rendered artifacts referenced by it are **not present in this worktree**. Its claims about Python behavior are requirements, not independently verified implementation facts.

Update 2026-10-29: the user supplied two actual PDF examples and explicitly requested copying them into documentation: [classifica.pdf](freezer-analysis-samples/classifica.pdf) and [verify.pdf](freezer-analysis-samples/verify.pdf). Both now reside in this worktree as candidate parser acceptance inputs; this does not supply the Python project, archive or original golden outputs. Copies were verified against the supplied originals using SHA256. No Git commit was created.

User-approved validation change: do not wait for those materials. Deliver the entire working workflow, including real persistence, import, rendering, configured integrations and automated checks, using synthetic data and independently calculated specification examples. The user will then check real results manually and report corrections. Distinguish "technically ready for user acceptance" from "original-application parity confirmed"; never mark unavailable historical comparisons as passed. This changes acceptance sequencing, not authentication, data-integrity, numerical-finiteness or deployment-safety requirements.

Accepted requirements from the brief:

- C# calculation engine; no production Python service.
- PDF and structured-data ingestion; format adapters rather than one parser tied to report filenames.
- Existing platform identity and Mongo server, but a **different application database** from ServerAPI's database.
- Shared competition facts reusable across accounts; private account-specific analysis and decisions.
- Existing LLM integration conventions; user approval remains authoritative.
- Server-generated A5, foldable A4, and two iPhone PNGs.
- Historical storage designed for substantially more than a few dozen competitions annually.

Planning assumptions:

1. "Same users as ServerAPI" means its current `PublicAccount` boundary, not BackOffice `User`/RBAC and not Shooting Range or shop identities. Source proves these are distinct. Implement against `PublicAccount` unless this assumption is explicitly changed before module M1.
2. Sharing means authenticated FreezerAnalysis users can reuse published competition facts; it does not mean anonymous bulk publication, shared private notes, or permission to overwrite another user's imports.
3. Initial mobile dictation uses the phone keyboard's dictation into a text field. An in-app audio-recording/transcription pipeline is outside the initial release.
4. Support the documented archive CSV schema as the first structured format, verified with synthetic files. ESS, FITDS and WinMSS adapters require lawful sample exports and validation; no undocumented live API is assumed.
5. Proceed with documented, versioned assumptions where the brief leaves details unspecified. Do not claim those assumptions reproduce the missing Python implementation. User feedback after complete delivery may refine them.

Initial completion boundary: M0-M8 below, including import/reconciliation, identity resolution, deterministic estimates, freeze, printable artifacts, field updates/offline reading, final verification, candidate backtesting, and user-approved parameter adoption. Deliver all of these before requesting the user's result validation; missing legacy files are not a reason to stop at a prototype or partial workflow. Rival probability simulations, arbitrary ad-hoc AI analysis, automatic parameter adoption, live result scraping, and a new identity platform are not part of this release.

## Current Evidence

| Evidence | Verified current behavior and architectural consequence |
|---|---|
| [Architecture guide](../architecture/README.md), [backend guide](../architecture/backend.md), [frontend guide](../architecture/frontend.md) | Independent hosts, established shared libraries and frontend packages. Current source overrides stale summaries. FreezerAnalysis is not an existing host/workspace. |
| [ServerAPI project](../../MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj), [Domain project](../../MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj), [Models project](../../MorWalPizVideo.Models/MorWalPizVideo.Models.csproj), [Contracts project](../../MorWalPiz.Contracts/MorWalPiz.Contracts.csproj) | Inspected projects target .NET 10. Domain references Models and YouTubeUtilities; Contracts references Models. Use the existing `MorWalPiz.Contracts`, not the similarly named legacy folder. |
| [PublicAccount model](../../MorWalPizVideo.Models/Models/PublicAccount.cs), [PublicAccountService](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs) | Public accounts and hashed sessions already exist. Login, eight-hour sessions, account activity/security-version validation, and session revocation belong to Domain. This is not BackOffice RBAC. |
| [PublicAccountController](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs), [PublicAccountSecurity](../../MorWalPizVideo.ServerAPI/Security/PublicAccountSecurity.cs) | Versioned account/session/profile endpoints; host-local cookie scheme and CSRF helper. Cookies are host-only because no domain is set. A cookie issued by ServerAPI is not automatically sent to a different API host. |
| [ServerAPI composition root](../../MorWalPizVideo.ServerAPI/Program.cs), [MongoDbService](../../MorWalPizVideo.MvcHelpers/Services/MongoDbService.cs), [database settings](../../MorWalPizVideo.Domain/MorWalPizDatabaseSettings.cs) | The existing settings/service resolve one database; `IMongoDatabase` is registered as the platform database. Registering a second unqualified database would create a wiring hazard. |
| [Repository implementations](../../MorWalPizVideo.Domain/Interfaces/Repository.cs), [repository contracts](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs), [mock repositories](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs) | Public-account repository operates over the injected platform database. Mongo/mock pairs exist. Specialized methods already use targeted queries/updates. |
| [BaseRepository](../../MorWalPizVideo.Domain/Interfaces/BaseRepository.cs), [BaseEntity](../../MorWalPizVideo.Models/Models/BaseEntity.cs) | Generic CRUD includes whole-document replacement and unbounded collection reads. IDs follow BSON ObjectId representation. Neither generic replace nor a generic update controller is suitable for frozen plans. |
| [Existing Competition model](../../MorWalPizVideo.Models/Models/Competition.cs) | Existing competitions support listings, embedded stages and community evaluations, not scoring revisions, source provenance or immutable analysis. Do not silently repurpose this collection. |
| [BackOffice composition root](../../MorWalPizVideo.BackOffice/Program.cs), [FAQ AI provider](../../MorWalPizVideo.BackOffice/Services/SemanticKernelFaqCandidateAiProvider.cs), [FAQ provider interface](../../MorWalPizVideo.BackOffice/Services/Interfaces/IFaqCandidateGenerationService.cs) | Azure OpenAI/Semantic Kernel registration and a typed-response provider pattern exist, but the inspected provider/interface are BackOffice-owned. There is no verified host-neutral IPSC AI service to inject unchanged. |
| [BlobService](../../MorWalPizVideo.Domain/BlobService.cs), [SocialAssetBlobService](../../MorWalPizVideo.Domain/SocialAssetBlobService.cs) | Shared Blob clients, checksums, content types and fakes are reusable infrastructure. Existing abstractions are image/social-specific; do not send private PDFs through public image URL paths. |
| [ServiceDefaults](../../MorWalPizVideo.ServiceDefaults/Extensions.cs) | Existing telemetry, health endpoints, discovery and resilient factory-managed HTTP defaults can be reused by the new host. |
| [Frontend workspace manifest](../../frontend/package.json), [transport implementation](../../frontend/fe-packages/services/src/apiTransport.ts), [services barrel](../../frontend/fe-packages/services/src/index.ts), [public-account helpers](../../frontend/fe-packages/services/src/publicAccountService.ts) | Yarn Classic monorepo; account transport includes credentials/CSRF without admin context. `createApiClient` has an explicit base URL but is not exported by the inspected package barrel. Add a public export before app consumption; do not deep-import package source. |
| [Shooting ITA service worker](../../frontend/shooting-ita-frontend/src/sw.ts), [Shoot Recorder PWA setup](../../frontend/shoot-recorder/vite.config.ts), [Shoot Recorder manifest](../../frontend/shoot-recorder/package.json) | PWA precedent exists. Shooting ITA's broad cache-first interception is not an account-aware offline strategy and must not be copied for private analysis. |
| [AppHost](../../MorWalPizVideo.AppHost/Program.cs), [AppHost project](../../MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj), [CI](../../.github/workflows/ci.yml) | Host/workspace registration, explicit test/build matrices and ordered shared builds are existing integration points. FreezerAnalysis is currently absent. |
| [Public-account tests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs), [Range test project](../../MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj), [test runbook](../architecture/testing.md) | Account HTTP tests use the mixed test project and `WebApplicationFactory`; a separate host-owned xUnit project also has repository precedent. Neither is evidence of FreezerAnalysis tests passing. |
| [Security guide](../architecture/security.md), [deployment guide](../architecture/deployment.md) | Documentation records a deployed public-account bootstrap blocker and historical credential-rotation obligations. Deployment state was not checked in this task. Do not weaken authorization to work around an unverified environment issue. |
| [ADR-018](../architecture/adr/ADR-018-public-account-boundary.md) | Public accounts are an explicitly separate boundary; normalized-identity uniqueness and session TTL indexes are source-managed. Its production rollout evidence remains open. FreezerAnalysis must not provision or rename those platform indexes independently. |

No PDF extraction or server PDF/PNG renderer was found in the inspected Domain, BackOffice and ServerAPI source/manifests. Poppler and Chromium are proposed new runtime dependencies, not existing repository capabilities.

### Supplied PDF examples: evidence and importer consequences

The supplied attachments are available in [freezer-analysis-samples](freezer-analysis-samples) at the user's explicit request. Use these documentation copies for local importer checks or accept equivalent files through the import UI. They contain real sporting results; keep their contents out of logs and external provider payloads.

| Sample | User-described role | Directly verified PDF metadata |
|---|---|---|
| `classifica.pdf` | Final match classification, separated by weapon division. | PDF 1.4, title `Overall`, 9 pages, first-page media box 595 x 842 points, producer PDFsharp 1.50.5147-gdi, creation date field 2026-10-25 16:09:48 +01:00. |
| `verify.pdf` | One table per shooter containing per-stage points, HF and time. | PDF 1.4, title `Verify Tiratori`, 48 pages, first-page media box 595 x 842 points, same producer, creation date field 2026-10-25 16:10:07 +01:00. |

Evidence limit: the available file reader exposed PDF objects and compressed streams, not decoded table text or rendered pages. Actual headers, column positions, competitor IDs/licenses, divisions, hit-zone counts, penalties, row counts, match identity and numeric reconciliation have **not** been verified. A PDF creation timestamp is not an authoritative match date. Matching producers/timestamps alone do not prove that the two reports belong to the same event.

Required implementation behavior for this pair:

1. Accept both files in one import session; detect report roles from decoded content, using title metadata only as a hint. Extract all 9/48 pages, preserve page provenance, handle repeated headers and tables continuing across pages, and never assume one page equals one shooter.
2. Use verification tables as the per-shooter/per-stage fact source and classification as the independent official totals/division reconciliation source. Distinguish raw stage score from normalized classification points. Confirm each actual column's meaning before mapping it.
3. Join by a report-local competitor identifier where both reports provide it. Otherwise offer normalized-name/division candidates with explicit resolution for collisions. Do not assume a license or stable competitor ID is present.
4. Preview match identity, participant/stage counts, matched/unmatched shooters, divisions and validation diagnostics before publication. Keep the two sources' roles/provenance in one match revision rather than create a separate match per PDF.
5. Where stage maximum points, division stage maxima and eligible participant results are available, recompute division-normalized stage points and total them; compare against classification within 0.02 points. An overall total is not the sum of raw stage points or HFs. If these prerequisites are absent, report precisely which reconciliation checks are unavailable rather than infer stage maxima from observed hits or claim totals passed.
6. Preserve missing/DQ/zeroed/unmatched cases explicitly. Never infer all penalties or hit-zone counts from points/HF/time alone; the user has not stated that these PDFs contain those extra fields.
7. M2 must exercise the real extraction/parser pipeline on these supplied samples in addition to synthetic fixtures, reporting actual coverage and discrepancies. Unknown original layouts are a parser issue to expose and refine, not a reason to require the missing Python application.
8. The documentation copies are user-authorized real samples, not sanitized fixtures. Preserve the Downloads originals unchanged. Use fictional or appropriately sanitized data for additional regression fixtures; do not include athlete names/licenses in this plan or logs. Review data-handling requirements before any public distribution.

Current account flow: public browser -> ServerAPI account controller -> Domain account service -> account/session repository -> platform Mongo database. Current AI flow: BackOffice use case -> BackOffice typed provider -> Semantic Kernel/Azure OpenAI. There is currently no source-verified FreezerAnalysis flow.

## Proposed Design

### 1. Ownership and dependency direction

Add one .NET 10 host, `FreezerAnalysis.Api`, and one client-only React 19/TypeScript workspace, `frontend/FreezerAnalysis.spa`. No SEO/SSR server is needed for an authenticated analysis tool. Keep feature entities in Models, focused application/persistence services under Domain's new FreezerAnalysis area, transport DTOs in `MorWalPiz.Contracts`, and host-specific controllers/integration adapters in the new API.

Reuse shared libraries without adding an API-to-API project reference. Do not put analysis in ServerAPI controllers, BackOffice `DataService`, Shooting Range, VideoImporter, or the existing community competition aggregate. Feature-owned repositories expose only allowed transitions, not generic frozen-document replacement.

Proposed flow:

1. Authenticated upload/file selection -> format detection -> extraction -> canonical import staging.
2. Validation and identity suggestions -> user resolves only ambiguous cases -> explicit publication of a shared competition revision.
3. Account profile + shared results + private exclusions/availability + parameter/rule versions -> deterministic estimate draft.
4. User review -> atomic freeze referencing an immutable input package -> server render and explicit offline download.
5. Turn/midday/reconnaissance additions -> separate timestamped events and derived render editions, never snapshot mutation.
6. Final official revision -> verification against the frozen baseline -> candidate backtests -> explicit adoption for future estimates.

### 2. Identity, sessions and two databases

Use the existing platform `PublicAccount` ID as the only account key. Add a FreezerAnalysis profile in the application database, linked to the account and to the chosen stable shooter identity. Division, power factor and model preferences are profile data, not additions to the shared account document. Never copy passwords or sessions into the application database.

For the initial release, reuse credentials/accounts, **not implicit cross-host SSO**. FreezerAnalysis exposes local CSRF, login, session-restoration and logout endpoints using the existing Domain account service/repository. Registration/profile administration remain owned by ServerAPI; offer a link rather than cloning those workflows.

Apply host-owned per-IP login throttling, bounded credential input and generic authentication failures, following ServerAPI's account endpoint pattern. Keep existing platform uniqueness/expiry indexes under their current operational owner; confirm they are applied before pilot activation.

Extract the existing host-local public-account authentication handler and CSRF/cookie logic into an options-driven shared MVC authentication component. Preserve ServerAPI's current route, scheme, cookie names and behavior. Configure FreezerAnalysis with distinct host-only session/CSRF cookie names and the same account endpoint paths expected by account-mode transport. No cookie-domain widening, browser bearer token, BackOffice permission grant, or shared localStorage session.

Serve the FreezerAnalysis SPA and API from separate production HTTPS origins, matching the confirmed repository deployment pattern. The SPA receives the explicit API origin through `VITE_FREEZER_API_BASE_URL`; the API allows only the exact configured SPA origin with credentials. Use host-only Secure/HttpOnly `SameSite=None` session and CSRF cookies plus origin/CSRF checks for unsafe requests. Keep the local Vite proxy as a development convenience. Qualify the actual cross-origin flow in Chromium and WebKit because browsers may restrict cookies between different registrable domains; do not fall back to broad CORS or browser bearer storage.

Keep the existing platform database resolution for `IPublicAccountRepository` only in the new host. Introduce a feature-specific application database accessor/settings type and inject it explicitly into every FreezerAnalysis repository. Validate at startup that platform and application database names differ and resolve to the intended same Mongo deployment. Use least-privilege database roles: platform account/session access only as required by login and validation; application collections in the separate database. A single server does not imply shared collection ownership. No cross-database transactions or joins are required.

### 3. Shared facts, private decisions and stable shooter identity

- Shared: competition/stage metadata, participant entries, official scoring/DQ/zeroed facts, source provenance, and confirmed shooter aliases.
- Private by default: profile-to-shooter selection, rivals, exclusions, reconnaissance text, strategies, frozen plans, verification reports, parameter proposals and field updates.
- `azzerato` is an official result fact. `escluso` belongs to the account's historical-stage selection. `prima` belongs to a particular estimate's availability snapshot/cutoff, not to the shared score.
- Never infer real availability solely from a competitor's squad. Retain the exact source revision and score set available when estimating.
- Resolve shooter identity by issuing federation plus license when available, then explicit aliases; a normalized name proposes a match but never silently merges homonyms. Keep the match-local competitor ID as a distinct identifier.
- Ambiguous entries can be imported with unresolved identity but are excluded from cross-match linkage/informants until resolved. Never substitute zero performance for unresolved or absent results.
- Scope division and power factor to match entries. Reproduce the reference rule that a division change uses only the most recent qualifying competition; do not overwrite historical entry data.
- Initial importer owns publication/replacement of its match. Other accounts may reuse published facts or submit a correction proposal, not replace them. Cross-uploader duplicate matches require a confirmed link/owner resolution; uncertain match fingerprints remain staged.
- Alias corrections and accepted shared-result corrections create new revisions with provenance. Existing frozen inputs retain the original mapping/revision.

Public sporting results remain personal data. Shared reuse requires an approved notice, lawful basis, retention and correction/deletion process; public availability alone does not establish unrestricted processing rights. Keep federation licenses out of ordinary responses, exports and LLM payloads.

### 4. Storage layout and history economy

Use readable typed BSON documents; avoid opaque packed arrays, premature time-series collections, or one giant all-history aggregate. Do not repeat names, match dates, divisions or stage metadata on every stage row.

| Proposed application collection/group | Granularity and lifetime |
|---|---|
| Matches and immutable revision manifests | One match header with active revision pointer; each revision embeds bounded stage/squad metadata and references its participant result documents. |
| Shooter identities and identity revisions | Stable ID, federation/license when available, aliases and correction provenance; historical match entries preserve their own division/factor and identity mapping version. |
| Participant result documents | One immutable competitor/match payload with an array of stage facts. Reuse an unchanged payload across match revisions using a canonical content hash; changed payloads get new IDs. |
| Imports/source manifests | Hash, format/parser version, completeness, validation outcome, source Blob references, uploader, and commit status. Raw source bytes exist once in private content-addressed Blob storage. |
| Account profiles and analysis selections | Account-scoped preferences, chosen shooter, historical exclusions and planning selections. Availability selection is captured with each estimate. |
| Parameter and rule sets | Immutable versions plus small adoption records. Separate adopted, candidate and unconfirmed values; adoption is account-specific. |
| Plan drafts and frozen snapshots | A replaceable draft revision; an insert-only frozen snapshot containing estimate outputs, accepted content, engine/rule/parameter versions, and immutable input-package reference/hash. |
| Field events, render manifests and verifications | Separate append-only event/report documents. Artifacts reference snapshot + event sequence + rendering context + renderer/template version. |

The frozen input package contains only the exact scoring projections, identity mapping, historical selections, registry inputs and provenance needed to reproduce that calculation. Store it compressed in private Blob storage and deduplicate by hash; do not copy all available historical PDFs/results into every plan. It supplements shared revision references so later correction or authorized retention changes do not silently make a plan unreproducible. Access and lawful retention still apply to snapshots.

Computed registry, stage profiles and user-level estimates remain transient except where needed as frozen outputs or reproducibility evidence. Do not persist a global "current registry" and apply it to old races. Do not save an artifact on every keystroke or an LLM conversation transcript by default.

Required indexes are feature-owned and deployed idempotently: match/source identity, unique import/content hashes, match/result content identity, participant/shooter history, match dates/status, owner/match/draft lookup, snapshot identity, owner/event sequence, parameter/rule version, and render input key. Hashes are separate indexed fields, not BSON ObjectId values. Exact index definitions and query explain evidence belong to M2, not an automatic production startup index migration.

Publish imports without a replica-set transaction requirement: stage immutable result documents and a complete revision manifest, validate them, then conditionally switch the match's active revision pointer. Readers see only complete published manifests. A crash before publication leaves invisible staging data; recovery can retry using the same idempotency key. A stale base revision returns a conflict. An unreferenced-staging cleanup is an explicit operator task until durable scheduling is approved.

Storage acceptance is measurable rather than an invented compression percentage:

- Reimporting identical data adds no result payloads or source bytes; it may add bounded audit metadata.
- A new account's analysis adds no duplicate shared result payloads.
- Changing one competitor's scores adds that payload and a new manifest, not all competitors again.
- A frozen snapshot contains no embedded PDF/PNG bytes or entire unrelated archive.
- On synthetic small/large races and a synthetic 1,000-match corpus, record BSON, indexes, compressed source/input packages, artifacts, and retained revisions separately. Verify match-scoped requests are indexed and do not materialize all matches. Repeat on the two reference races only if they later become available.
- No BSON document may reach 16 MiB. Add an 8 MiB feature validation guard and exercise the expected maximum participant/stage counts before publication.

Do not delete referenced revisions, mappings, rules or input packages for space. Raw-source retention, final artifacts and personal-data erasure require a retention matrix approved before production. Regenerable unreferenced preview artifacts may expire; essential frozen evidence may not silently expire.

### 5. Import and numerical engine

Implement adapters with a common canonical result and diagnostic shape. Distinguish extraction from parsing, and parsing from publication.

- PDF baseline: C# parsers over a pinned local Poppler layout-text extractor. This follows the extractor/layout mode described in the brief, without claiming verified parity. Run the extractor in the API container with generated temporary paths, no shell interpolation, no network, resource/time limits, cancellation and cleanup. Test real extraction using synthetic PDF report fixtures, not just mocked extracted strings.
- Recognize report family from content, not filename. Support partial results, gross/net export variants, decimal comma, optional OT, Overall/Stage Results/Score Verification, and explicit unknown/unsupported-format errors.
- Structured baseline: validated import of the documented archive CSV columns, flag semantics and match metadata, tested on synthetic fixtures. Define the same canonical import contract for subsequent ESS/WinMSS adapters.
- Competitor Listing is a separate new parser: preview squad/day/division/power factor/class/license and require correction of uncertain mappings. Implement against a documented supported layout with synthetic PDF fixtures; provide structured roster entry/correction when an actual PDF differs.
- Never assume a live FITDS/ESS/PractiScore API or claim a CAB format has been implemented. Add an actual provider adapter only after representative exports, access permission and field mapping are available; these optional connectors do not block the rest of the application. Do not substitute LLM extraction for official score validation.
- Publish a supported-format description and a complete CSV import route for both scores and rosters. Real PDF layout compatibility remains unverified until actual extraction/parser checks pass; the supplied Overall/Verify pair is now available for that work. Unknown layouts must return explicit diagnostics and a usable structured-import option, not guessed scores.
- Partial imports retain an explicit completeness level. Full official totals are required before final publication/verification; provisional data can support estimates only with visible warnings. Missing scores, DQ and zeroed classification are distinct states.

The engine is a pure C# service with deterministic ordering, pinned engine version and no database/network/LLM calls. Use floating point for the reference's log/exp/sinh/regression path and explicit decimal half-up behavior for required display decisions. Do not round intermediate model values to match the UI.

Preserve all reference formulas: net-score recovery, 200 alternating-mean iterations with centered stage effects, same-division weighted informants, uncertainty interval, exact penalty-equivalence costs, opponent actual/predicted values, overlap thresholds and inverse-variance combination, last-division rule, retrospective verification and beta regression.

Keep the documented candidate-pool boundaries explicit: scarto within +/-8.3% and stage dispersion within 15%; rival dispersion above 15% is visibly uncertain. Registry pairing requires at least the larger of five stages and the user's valid-stage count minus three. For current defaults preserve beta 1.2, drift 4%, own variability 0.105 and performance 0.935; keep the distance penalty 0.25 visibly unconfirmed, afternoon 0.94 and one-hand 0.89 as opt-in candidates. M0 must state units and percent/log-space conversions explicitly rather than infer undocumented Python behavior.

Insufficient data is a typed diagnostic: no positive HF, zero time, invalid counts, no eligible informants, insufficient overlap, undefined regression or zero-variance weights must not produce NaN, infinity, a fabricated interval, or an optimistic default. If the error-cost denominator is non-positive, report that the finite equivalence is undefined. Use conservative, explicit behavior for unspecified edge cases: exclude non-positive HF from logarithmic fits with a reason; report an unavailable estimate when the fit or eligible-informant set is empty; reject non-finite weights rather than introduce an arbitrary epsilon. Record the grouping and sample/population dispersion convention in the engine version and diagnostic report. These choices may be refined after user acceptance without rewriting frozen results.

Compute the registry using strictly earlier competition dates. For replay, use the frozen historical revisions and the exact pre-start availability set, not the latest completed results. Store UTC instants and explicit local match dates/time zone; display Italian dates/numbers in `Europe/Rome`/`it-IT`. Never convert historical same-day matches into an arbitrary causal order.

Separate historical parameter versions from today's defaults. Sicily's golden replay uses beta 1.25 and drift 5%; today's beta 1.2/drift 4% must not overwrite those inputs. Afternoon and one-hand candidates are off unless explicitly enabled for a draft. Freeze every applied value, including any unconfirmed distance penalty.

### 6. Freeze, field events and verification

Use a small mutable planning header for draft revision, owner, start state and frozen snapshot reference. Freeze atomically checks the expected draft revision and that the user has not started, then binds one snapshot ID. Snapshot insertion and input-package upload precede this pointer update; only a bound snapshot is visible as frozen. Retried requests return the same result.

The frozen collection has insert/read methods only. Where supported, grant no update privilege to that collection for normal runtime use. Concurrent edits/freezes/start operations must produce one winning transition and a conflict for stale requests. An explicit legacy-import pathway may bind a historical frozen plan but cannot create a new supposedly pre-start prediction after results are known.

Field updates have owner, client operation ID, server receipt time, user-observed local time/offset, snapshot ID, sequence and content. They append; they never replace frozen estimates, strategies or the baseline input set. Corrections append a superseding event. Starting turn, squad-size correction and display order are rendering context revisions, not edits to the immutable baseline. This resolves the apparent conflict between "freeze" and regenerating the card at roll call.

Verification pins one final official result revision and one frozen snapshot. Publish the stage/group errors, interval coverage, observed beta and informant drift with explicit denominators and missing/DQ diagnostics. Parameter candidates are immutable proposals with deterministic backtest results per historical race; adoption creates a new account adoption record and affects only future drafts. Backtests rebuild each race from its own prior-history and availability set. If an archival race lacks that evidence, mark the backtest unsupported rather than using final results as pre-match inputs.

### 7. AI boundary and document generation

Reuse the existing typed-provider/Semantic Kernel approach, not the BackOffice API or its FAQ-specific interface. Add an app-specific provider behind a FreezerAnalysis interface and deterministic fake. Initially register its host-specific adapter in FreezerAnalysis; do not move unrelated BackOffice prompts or add Semantic Kernel to all Domain consumers.

AI may propose short text, normalize dictated reconnaissance into a typed draft, or explain a deterministic backtest. It may not calculate authoritative scores, merge identities, exclude stages, alter a snapshot or adopt parameters. Validate schema, stage IDs, permitted symbols, length and unsupported claims. Prompt content is untrusted; no tools, arbitrary URLs or filesystem access. Show proposals separately and require explicit acceptance.

Only minimum de-identified numeric/stage context may leave the service, and only through an organization-approved provider deployment with a verified processing/retention agreement. No names, licenses, raw PDFs, account data or private notes are sent by default. LLM rollout remains disabled until that approval; manual editing is an explicit supported mode, not a silent fallback after provider failure. Provider failures return actionable diagnostics and preserve edits.

Use a server HTML/CSS template rendered by a pinned Playwright .NET/Chromium runtime. This is a new dependency justified by the reference renderer, not a repository capability claim. Pin fonts/template version, embed assets locally, escape all text, disable external fetches and browser navigation, and bound browser concurrency.

Produce A5 front/back, A4 folding layout, and two 1290 x 2796 PNGs from the same semantic card payload. Implement documented stage/turn ordering, inclusive rhythm boundaries, half-up charlie ranges, symbol/group styling, maximum two strategy sentences and lunch separator. Overflow detection is mandatory; do not silently shrink beyond the declared readable minimum or truncate content. Without reference images, adopt an explicit provisional logical viewport of 430 x 932 points at scale 3, making the documented 278-816 point text area 834-2448 pixels vertically. Test that mapping and use a declared minimum 12-point logical text size; have the user confirm readability/layout after complete delivery, without claiming the original renderer used that convention.

Generated artifact identity includes snapshot ID, accepted event sequence, rendering context, template/fonts/renderer version and payload hash. Store checksums and private Blob keys, not public image URLs. Download through authorized endpoints. A failed render does not unfreeze a plan or invalidate an already generated artifact; it records failure and permits an idempotent retry.

Initial slow operations are bounded request/response work, with persisted operation IDs/results for retry and bounded resource admission; do not return a background-job success response. No in-memory fire-and-forget work. If the approved PDF corpus cannot complete imports/renders within the M8 budget, stop release and revise the durable-job boundary; do not quietly bolt on a custom Mongo queue. Existing BackOffice Hangfire uses SQL Server, so reusing its pattern would require an explicit additional operational dependency, not just another Mongo collection.

### 8. Mobile UI and offline contract

Routes cover account session, profile/shooter binding, match selection/import preview, identities requiring review, draft analysis, frozen field card, updates, artifacts and verification/backtests. Use React Router loaders/actions/fetchers, local edit state, server-authoritative validation, visible pending/conflict states and Italian copy. Do not introduce a global store without demonstrated need.

Expose the existing transport factory and options from the shared services package. Instantiate an account-mode client for this application's base URL; keep feature endpoint functions/TypeScript DTOs app-local because there is one consumer. Do not add FreezerAnalysis scoring types to every SPA's shared model package. No direct component fetch clients, bearer storage, admin channel header or mutable singleton reconfiguration.

Offline mode is deliberately bounded:

- Explicit "download for the field" saves the shell, selected frozen payload and already generated artifacts, keyed by account/snapshot/hash, in an app-owned cache/IndexedDB.
- Display the frozen version, local download time, connectivity, and pending additions prominently.
- Allow local turn overlay and queued text/reconnaissance additions with client operation IDs. These remain unaccepted local work until server reconciliation; PDF/PNG regeneration and LLM use require connectivity.
- On reconnect, reauthenticate, verify snapshot/owner, then reconcile each operation idempotently. Rebased or conflicting edits require review; never overwrite a frozen baseline.
- Freeze, shared import publication and parameter adoption require online confirmation. A downloaded result is not proof of an active session.
- Clear private caches/queues on logout and before another account uses the app; do not cache auth endpoints or all API GETs. Warn that shared-device storage is not encrypted protection and remote logout cannot instantly erase a disconnected device.

Proposed field acceptance budgets: turn change within 60 seconds and at most three primary actions; midday text update within 120 seconds and five primary actions; dictated reconnaissance capture/review within 180 seconds. Measure with representative users on a phone; these are targets, not existing performance measurements.

Impeccable implementation lens: a **new Operate surface**, not a redesign of existing SPAs. No incumbent `PRODUCT.md` or `DESIGN.md` exists for FreezerAnalysis; the only located [DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md) belongs to the public content site and is not an application-wide theme mandate. During M6 use `shape` to settle the narrow mobile workflow; use `harden` for offline/conflict/error states and `adapt` for responsive behavior. This planning task does not execute those workflows. Reuse technical/layout primitives only when the behavior genuinely matches. Minimum evidence: phone screenshots of draft, frozen, turn, queued/offline and error states; tablet/desktop field view; A5/A4 and both full-resolution PNGs; keyboard/focus, screen-reader status announcements, contrast and 200% zoom checks. Use 44 CSS-pixel minimum primary touch controls and no color-only stage or status cues.

## Impacted Projects

| Project/surface | Planned impact |
|---|---|
| `FreezerAnalysis.Api` | Add .NET 10 host, versioned controllers, authentication wiring, PDF extraction/rendering and app-specific AI adapters; expose an exact credentialed CORS boundary for the separately deployed SPA. |
| `frontend/FreezerAnalysis.spa` | Add React/TypeScript PWA workspace, feature-local DTOs/services/routes, tests and account-aware offline storage. |
| `FreezerAnalysis.Api.Tests` | Add host-owned xUnit/WebApplicationFactory, pure-engine, synthetic/specification-fixture and disposable real-Mongo tests; historical golden comparisons are optional later additions. |
| [MorWalPizVideo.Models](../../MorWalPizVideo.Models) | Add feature entities, enums/options and collection names without changing existing community competition or account schemas. |
| [MorWalPizVideo.Domain](../../MorWalPizVideo.Domain) | Add engine, use cases, database-specific repositories, interfaces and mock/fake providers. |
| [MorWalPiz.Contracts](../../MorWalPiz.Contracts) | Add explicit analysis HTTP DTOs; retain existing account contracts. |
| [MorWalPizVideo.MvcHelpers](../../MorWalPizVideo.MvcHelpers) | Extract/configure public-account security primitives for host reuse while preserving ServerAPI behavior. |
| [MorWalPizVideo.ServerAPI](../../MorWalPizVideo.ServerAPI) | Narrow composition/controller imports change for shared security extraction only; no scoring routes or FreezerAnalysis data in its database. |
| [MorWalPizVideo.BackOffice.Tests](../../MorWalPizVideo.BackOffice.Tests) | Add/retain account extraction regressions in existing ServerAPI/Shared groups. |
| [Shared frontend services](../../frontend/fe-packages/services) | Add package-barrel exports for the existing factory/options and transport-isolation tests; no new global mode. |
| [AppHost](../../MorWalPizVideo.AppHost), [frontend workspace root](../../frontend), [solution](../../MorWalPizVideo.sln) | Register host, workspace/dev entry and test project; keep shared-build dependency order. |
| [ServiceDefaults](../../MorWalPizVideo.ServiceDefaults), [shared layout](../../frontend/fe-packages/layout), [shared TS models](../../frontend/fe-packages/models) | Reuse/verify only initially. No shared scoring UI or models without a second genuine consumer. |
| [BackOffice](../../MorWalPizVideo.BackOffice) | Verify-only AI/provider and auth regressions; no required production changes. Do not add a remote orchestration dependency. |
| Existing public/admin/Ask/ITA/Range/Recorder clients | Verify shared transport/build compatibility; no product-feature changes. Preserve frozen shop behavior without expanding its scope. |
| Mongo, Blob, container/CI/deployment | New app database, least-privilege access, private artifact/source container, Poppler/Chromium runtime, conditional deployment and readiness gates. |
| ShortLinks, Windows tools, YouTubeUtilities, standalone TelePrompter/stage-designer | Not applicable: no feature ownership or direct contract change. |

## Impacted Files

New paths below are proposed ownership locations, not claims that those files already exist. Within each new feature directory, select names matching the repository's existing class/route conventions; do not create an extra project for each use case.

### Modify

- [MorWalPizVideo.sln](../../MorWalPizVideo.sln): register API and its tests.
- [ServerAPI Program.cs](../../MorWalPizVideo.ServerAPI/Program.cs), [PublicAccountController.cs](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs), [PublicAccountSecurity.cs](../../MorWalPizVideo.ServerAPI/Security/PublicAccountSecurity.cs): replace host-local security implementation with shared registration/imports; preserve endpoint and cookie compatibility.
- [DbCollections.cs](../../MorWalPizVideo.Models/Constraints/DbCollections.cs): feature-prefixed application collection constants; no collection rename.
- [Models project](../../MorWalPizVideo.Models/MorWalPizVideo.Models.csproj), [Domain project](../../MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj), [Contracts project](../../MorWalPiz.Contracts/MorWalPiz.Contracts.csproj), [MvcHelpers project](../../MorWalPizVideo.MvcHelpers/MorWalPizVideo.MvcHelpers.csproj): verify dependency direction; change only when a required reference is demonstrated.
- [services index.ts](../../frontend/fe-packages/services/src/index.ts): export existing `createApiClient`/options; preserve all current exports.
- [apiService.test.ts](../../frontend/fe-packages/services/src/apiService.test.ts): interleaved FreezerAnalysis account/admin/public client isolation and existing error semantics.
- [frontend package.json](../../frontend/package.json): workspace and local developer entry.
- [AppHost Program.cs](../../MorWalPizVideo.AppHost/Program.cs), [AppHost project](../../MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj): reference new host and frontend dev resource.
- [CI workflow](../../.github/workflows/ci.yml): add API/test/workspace/container gates and required artifact handoff, without enabling the on-hold shop.
- [PublicAccountEndpointTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs): extraction and old-cookie/route behavior regressions; add adjacent focused security tests if existing organization warrants them.

### Add

- `FreezerAnalysis.Api/`: project/composition root, versioned feature controllers and local session controller, importer/PDF extractor, AI adapter, document renderer/templates/fonts, native-dependency Dockerfile and non-secret settings schema.
- `FreezerAnalysis.Api.Tests/`: project, WebApplicationFactory/test auth, engine/golden/parser/HTTP/security/render tests, and synthetic fixture builders.
- `MorWalPizVideo.Models/FreezerAnalysis/`: typed feature records, version/state enums and options. Preserve BSON ObjectId conventions.
- `MorWalPizVideo.Domain/FreezerAnalysis/`: calculation engine, application services, feature-specific database accessor, targeted Mongo/mock repositories, source/artifact storage boundary, and AI/clock/render abstractions for tests.
- `MorWalPiz.Contracts/Contracts/FreezerAnalysis/`: request/response contracts and explicit mapping ownership.
- `MorWalPizVideo.MvcHelpers/Authentication/`: shared options-driven public-account cookie/handler/CSRF components extracted from the existing ServerAPI file.
- `frontend/FreezerAnalysis.spa/`: app manifest/config, entry/router, feature routes/components, local DTO/service modules, PWA registration and account-partitioned offline store/tests.
- `.github/workflows/deploy-freezer-analysis-api.yml` and `.github/workflows/deploy-freezer-analysis-spa.yml`: independently build and deploy immutable compatible API/SPA images, with environment-managed exact origins and nonempty native/browser gates.
- Fixture directory under `FreezerAnalysis.Api.Tests/`: synthetic CSV/PDF/roster inputs, hand-worked specification examples and generated-layout baselines clearly identified as new-application fixtures. Add sanitized historical data only if it later becomes available with permission; never label generated fixtures as original golden artifacts.

### Generated/Migration

- [frontend yarn.lock](../../frontend/yarn.lock): package-manager generated changes for the new workspace/dependencies, not hand edits.
- Mongo application index manifest/backfill evidence in the owning feature's operational documentation. No EF/SQLite migration.
- Container-installed browser/Poppler and build-produced SPA artifacts are generated; do not check installed browsers, binaries, `dist`, generated tests or personal-data archives into source.
- Initial CSV/PDF migration produces versioned application records through the same validated import path. No migration of platform credentials/accounts.

### Documentation

- This handoff remains the user-editable source of truth; update with actual files, gates, results and residual risks after implementation.
- [Architecture index](../architecture/README.md), [solution structure](../architecture/solution-structure.md), [project responsibilities](../architecture/project-responsibilities.md), [backend](../architecture/backend.md), [frontend](../architecture/frontend.md), [domain model](../architecture/domain-model.md), [security](../architecture/security.md), [infrastructure](../architecture/infrastructure.md), [deployment](../architecture/deployment.md), [development](../architecture/development.md), [testing](../architecture/testing.md): update only affected ownership, two-database wiring, auth, offline and release sections when implemented.
- Add an ADR under [architecture ADRs](../architecture/adr) for FreezerAnalysis ownership, shared account identity/separate app sessions, two-database isolation and immutable revision/freeze policy. Record approved target versus implemented state explicitly.
- Add feature-level retention/recovery, fixture provenance and manual-acceptance notes. Record assumptions, known unverified report layouts and the user's eventual result confirmation. Do not expose production database names, credentials or fixture personal data.

### Verify Only

- [PublicAccountService.cs](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs), [PublicAccount.cs](../../MorWalPizVideo.Models/Models/PublicAccount.cs), [Repository.cs](../../MorWalPizVideo.Domain/Interfaces/Repository.cs), [MockRepository.cs](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs), [MongoDbService.cs](../../MorWalPizVideo.MvcHelpers/Services/MongoDbService.cs): preserve existing platform persistence; use explicit construction/registration rather than modifying global database resolution.
- [BackOffice Program.cs](../../MorWalPizVideo.BackOffice/Program.cs), [FAQ AI provider](../../MorWalPizVideo.BackOffice/Services/SemanticKernelFaqCandidateAiProvider.cs), [BlobService.cs](../../MorWalPizVideo.Domain/BlobService.cs), [SocialAssetBlobService.cs](../../MorWalPizVideo.Domain/SocialAssetBlobService.cs): infrastructure prior art, not copied business behavior.
- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts), [publicAccountService.ts](../../frontend/fe-packages/services/src/publicAccountService.ts), [account contracts](../../MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs): reuse unchanged unless focused tests identify a requirement not met by the current factory/DTOs.
- Existing active frontend manifests/builds and [deployment workflows](../../.github/workflows): verify shared-package and path-filter compatibility; do not modify unrelated release pipelines.

## Impact Matrix

| Area | Decision and verification |
|---|---|
| Backend/API | Dedicated .NET 10 host; `/api/v1` DTO endpoints, bounded requests, pagination, cancellation and structured Problem Details. No API-to-API project references. |
| Frontend/UI | New Operate-mode SPA/PWA; route-owned data, explicit field workflows, Italian formatting, account-aware offline support and visible conflicts. |
| Database/Data | Shared platform account/session database only for auth; separate application database on the same server. Immutable score revisions, atomic publication pointers, private user selections. |
| Contracts/Models | Models/Domain/Contracts layering; feature DTOs, not Mongo documents, cross HTTP. App-local TS DTOs with contract tests; shared transport factory only. |
| Authentication/Authorization | Reuse PublicAccount service with app-local host-only cookies; default authenticated policy, CSRF, origin checking, account ownership on every private lookup and download. No BackOffice RBAC reuse. |
| Validation | Upload limits, format detection, scoring/totals reconciliation, identity ambiguity, finite numerical results, draft-version checks and overflow errors. No silent fallbacks. |
| Cache | No server output caching for personal API responses. Immutable artifacts addressed by input hash; browser HTTP responses private/no-store, explicit offline download separate. No cross-API cache purge required. If shared read caching is later introduced, use centralized lowercase tags keyed by published revision. |
| Configuration/Secrets | Typed two-database settings, private Blob settings, approved provider configuration and exact origins. Secrets via existing environment/Key Vault practices; fail startup on unsafe or ambiguous database selection. |
| Logging/Telemetry | ServiceDefaults plus import/engine/freeze/render identifiers, duration/size counters, validation counts and explicit failure codes. No names, licenses, passwords, tokens, raw prompts or signed URLs in logs. |
| Background Jobs | Not needed for the bounded initial path; no fake durable queue. Time-budget failure blocks release and requires a separately approved Hangfire/storage design. |
| Deployment/Operations | Separate SPA/API images, exact credentialed origins, pinned API extraction/render dependencies, resource limits, two-database and private-storage readiness. CI must qualify the compatible image pair. |
| Backward Compatibility | Preserve platform account IDs, old ServerAPI cookie/routes, existing transport exports and all other apps. Additive feature collections; frozen inputs never resolve through mutable latest pointers. |

## Reuse Opportunities

- Existing `PublicAccount`, session repository/service and account contracts: reuse identity and password/session lifecycle without duplicating accounts.
- Existing host-specific account tests: bracket the shared MVC security extraction with regression proof.
- Domain repository interfaces/Mongo/mock patterns and deterministic scenarios: use feature-scoped seeds, no production-derived personal data.
- Models/Contracts DTO boundaries and BSON ObjectId conventions: keep Mongo shapes distinct from the HTTP surface.
- Shared `createApiClient` account mode and explicit base URL: export existing capability; do not add a second HTTP stack.
- Existing route loader/action/fetcher pattern, Vitest/Testing Library, PWA dependency and selected layout primitives: reuse mechanics, not public-site page composition.
- Blob client factory/checksum/content-type practices: add an application-private wrapper rather than repurposing public image paths or the social container.
- Semantic Kernel typed provider and fake integration pattern: reuse the integration method with a new feature contract; keep numerical results deterministic.
- ServiceDefaults/AppHost/CI: integrate into established lifecycle, health and ordered-build gates.

## Risks

| Priority | Risk/cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Missing Python/rules/golden corpus leaves original numerical and visual parity unverified. | User accepts implementation from the brief; use independent synthetic/specification tests and versioned assumptions, then manual acceptance after full delivery. | Technical readiness evidence plus a separate pending/confirmed user-parity record; never count missing comparisons as passed. |
| High | Wrong platform identity assumption or accidental application writes into the platform DB. | Adopt PublicAccount explicitly; feature-specific database accessor; differing-name validation and least-privilege roles. | Two distinct disposable databases; feature writes absent from platform DB and vice versa. |
| High | Future-data leakage or mutable references changes historical estimates. | Date-filtered history, immutable revisions/input packages, frozen identity/rule versions and exact availability sets. | Replay unchanged after new results, alias corrections and parameter adoption. |
| High | Concurrent publication/freeze overwrites accepted facts or baseline. | Conditional pointer transitions, immutable payloads, unique idempotency keys and no snapshot update API. | Competing clients yield one winner; retries cannot duplicate or modify frozen records. |
| High | Public-result reuse and AI processing exceed lawful/organizational permissions. | Approved retention/notice/correction process; private defaults; de-identified approved-provider payloads only. | Data-owner/provider review; export/retention tests; no identifying data in provider spy logs. |
| High | Malicious PDFs/archives/HTML or provider-provided URLs affect the server. | Bounded local extractor/browser sandbox, safe temp paths, escaped templates, no remote fetch, no arbitrary URL ingestion. | Corrupt/oversized/path-traversal and external-resource tests fail safely with no publication. |
| Medium | Name collisions, missing licenses or division changes corrupt the registry. | Scoped IDs, explicit alias resolution, revisioned mappings and per-match division history. | Homonym/division-change fixtures match reference or require review. |
| Medium | Duplicate sources/revisions/plans grow historical storage unexpectedly. | Content-addressed source/result/input packages, no duplicated stage metadata, measured retention. | Idempotent storage counters and 1,000-match corpus report. |
| Medium | Browser/extractor/native dependencies exceed hosting resources or are absent. | Pinned runtime image, non-root operation, concurrency/timeout limits, startup checks. | Clean Linux container import/render gate at budget and expected dimensions. |
| Medium | Field connectivity or iOS storage eviction loses unsubmitted work. | Explicit download status, local operation queue, idempotent sync and visible pending items. | Airplane-mode/restart/reconnect drill; storage failure is visible. |
| Medium | App-local sessions misunderstood as seamless SSO; revoked account remains visible offline. | State session contract clearly; live auth before sync; shared-device cache warning and purge. | Real-browser login/logout/account-switch tests and offline warning. |
| Medium | AI drafts are malformed, overlong, misleading or unavailable. | Schema/length validation, manual mode, explicit acceptance and cost/rate limits. | Fake failure/prompt-injection tests; no unauthorized state transition. |
| Medium | Existing deployed account-bootstrap issue or environment policy blocks rollout. | Verify deployed revisions/host auth; do not weaken the authenticated fallback. | Environment-specific anonymous CSRF acquisition and authenticated session smoke proof. |
| Low | Italian decimal/time or font differences change displayed meaning/layout. | Culture-separated calculations/formatting, pinned fonts, Rome DST tests. | Numeric/glyph/boundary tests and approved rendered comparisons. |

## Technical Debt

- Generic repositories allow unbounded reads and whole-document replacement. **Isolate now** behind feature-specific repositories; do not refactor all legacy repositories.
- Account security is ServerAPI-local while Domain accounts are reusable. **Address narrowly in M1**, with old-host tests; no broader auth redesign or SSO platform.
- The LLM integration is not a shared host-neutral business service. **Isolate** a new feature adapter; defer global AI-provider consolidation until there are genuinely shared operations.
- Shared transport has permissive/legacy response typing and error-return shapes. **Reuse with typed feature wrappers and guards**; do not perform a monorepo transport rewrite.
- PublicAccount activation/credential-rotation risks are documented, not verified here. **Release gate where applicable**, not permission to repair unrelated environments or expose secrets.
- Existing competition/community-stage model does not own analysis. **Keep separate**, not a migration or schema expansion of the public feature.
- Current Python parameter defaults and prose are duplicated according to the brief. **Resolve in migration** into versioned records, preserving historical values and provenance.
- Advanced rival simulations and ad-hoc analysis lack a retained reference implementation. **Defer explicitly**; the initial release includes deterministic per-stage rival actual/expected values only.

## Implementation Roadmap

Each module must update this handoff with actual changes/evidence. Dependencies are explicit; no mandatory post-approval decomposition phase is needed.

1. **M0 - Specification baseline and synthetic fixtures.** Depends on none. Owner: feature documentation and test fixtures.
   - Do not wait for the missing scripts, rules, historical archive or golden artifacts. Translate the written formulas/rules into a versioned acceptance checklist and create fictional score, roster and PDF fixtures.
   - Record units, dispersion/grouping conventions, conservative undefined-case handling, same-day history and provisional layout choices. Calculate small expected examples independently, before accepting engine-generated baselines; do not resolve ordinary ambiguity by blocking implementation.
   - Adopt PublicAccount and documented CSV support as planned. Implement explicit unknown-PDF diagnostics and structured correction paths. Optional ESS/WinMSS adapters remain sample-dependent; governance remains a production activation requirement.
   - Gate: written-spec tests and synthetic fixtures cover the planned workflow; assumptions and deferred historical comparisons are explicit. Original Python/data/artifacts are not required to pass this gate.

2. **M1 - Host, session boundary and contracts.** Depends on M0 identity/scope decisions, not on completion of the numerical port. Owners: new API/test project, Models/Contracts, MvcHelpers, ServerAPI, AppHost and shared frontend services.
   - Add .NET 10 host/tests and feature DTO/model contracts; default authenticated authorization and Problem Details.
   - Extract/configure account security; reuse account service with distinct FreezerAnalysis cookie names. Preserve old host behavior.
   - Wire platform DB explicitly for accounts and a separate application accessor; fail invalid configurations. Export existing frontend client factory/options without altering modes.
   - Gate: local fake mode works without external providers; session/CSRF/owner tests pass; ServerAPI account regressions pass; two-database isolation is proven.

3. **M2 - Canonical facts, import and historical storage.** Depends on M0 synthetic format fixtures and M1 boundaries. Owners: Models/Domain repositories, API importer/storage adapters and tests.
   - Implement revision manifests, immutable per-competitor results, source hashing, idempotent staging/publication and query indexes.
   - Implement CSV/PDF adapters and roster preview; identity suggestions and explicit ambiguity/correction flow. Separate private exclusions/availability from shared flags.
   - Use the supplied `classifica.pdf`/`verify.pdf` pair as real-layout inputs: extract all pages, document actual field mappings, import them together and report the reconciliation coverage described under Current Evidence. Keep report titles/producers as hints, not sole format discriminators.
   - Implement dataset import using validated APIs, not a one-off database script. Retain source/revision provenance.
   - Gate: synthetic full-race totals reconcile within 0.02 points, partial imports are explicitly classified, real synthetic PDFs traverse extraction/parsing, and duplicate/collision, restart-safe publication and storage tests pass on real Mongo. Original-race comparisons await manual acceptance.

4. **M3 - Deterministic C# numerical engine.** Depends on M0 written-specification tests and M2 canonical score shape; can proceed independently of UI/rendering after these are stable. Owners: Domain engine and new test project.
   - Implement registry, two-way model, weighted level/uncertainty, opponents, costs, card-derived numerical rules and verification calculations from the brief, not unavailable Python source.
   - Implement historical-date/availability replay, exact parameter version application and explicit insufficient-data diagnostics.
   - Gate: every applicable specification-example, synthetic-fit, invariant and boundary test below passes against independent expected values; repeat runs and data ordering are deterministic. Do not gate on unavailable Sicily/National datasets.

5. **M4 - Planning, freeze and verification use cases.** Depends on M1-M3. Owners: Domain/API/Contracts and HTTP/concurrency tests.
   - Implement user profile, draft revisions, snapshot/input-package creation, freeze/start transitions, append-only updates and authorized lookup.
   - Implement final-revision verification, candidate backtests and account-specific adoption with explicit approval.
   - Gate: new results, corrections, account selections and parameter adoption cannot alter a frozen replay; cross-user and stale-transition tests pass.

6. **M5 - Server documents and AI proposals.** Rendering depends on M0 layout specification and M4 snapshot contract; AI depends on M1/M4 and approved data-processing policy. Owners: API adapters/templates, private Blob boundary and integration tests.
   - Add pinned Poppler/Chromium runtime packaging, semantic card templates, overflow diagnostics, authorized artifact download and idempotent render keys.
   - Add typed AI adapter/fake, de-identified payload validator, proposal acceptance and explicit manual mode. Keep AI disabled until provider approval.
   - Gate: A5/A4/PNG dimensions, content, documented layout and corruption/timeout/overflow tests pass; no AI failure or proposal can mutate authoritative data. Template escape/network isolation tests pass. Original visual parity awaits the user's later comparison.

7. **M6 - Mobile workflow and offline delivery.** Depends on M1 transport/session contracts and M4-M5 behavior; shell/prototype may start earlier against typed fixtures. Owner: `FreezerAnalysis.spa`.
   - Apply the specified Impeccable planning lens during implementation; build only scoped workflows.
   - Add loader/action/service routes, import/identity review, draft/freeze, field card, turn/updates/reconnaissance, verification/backtests and downloads.
   - Implement explicit offline download, owner-partitioned storage and queued operation reconciliation. No offline server-render success claim.
   - Gate: component/error/contract tests, phone task budgets, account switching, airplane-mode/reconnect, accessibility and artifact visual evidence pass.

8. **M7 - Historical transition and operational readiness.** Depends on M2-M6. Owners: feature runbook, deployment/CI, database and data owners.
   - Exercise historical migration and a legacy frozen-plan import using synthetic races with explicit parameter/rule/cutoff versions. Keep the real-data import path available for user acceptance; do not require the two original races before delivery.
   - Establish retention/erasure matrix, index application/recovery procedure, private storage permissions and backups for both required databases plus source/input/artifact Blob data.
   - Wire workspace/solution/AppHost/CI and combined-container deployment, explicit flags and readiness checks. Keep deployment authorization distinct from source completion.
   - Gate: recovery drill, synthetic 1,000-match storage/query report, least-privilege check and clean container proof pass; architecture/ADR/runbook reflect actual ownership.

9. **M8 - Qualification, pilot and stop.** Depends on M0-M7. Owners: implementer and release/data owners.
   - Run narrow suites first, then affected consumer builds/tests because shared MVC security and frontend exports changed; retain existing mixed API and transport regressions.
   - Proposed baseline: a 500-competitor/34-stage match; estimate computation p95 <= 1 second excluding network, freeze persistence p95 <= 2 seconds, a full artifact set p95 <= 10 seconds, and a supported 20 MiB PDF import <= 30 seconds. Measure on the declared deployment resource tier with two concurrent users and warm runtime; record cold-start results separately. These are proposed release budgets, not observed facts.
   - If corpus limits or budgets fail, revise the plan and scope before release; do not claim success by silently dropping reports/stages or adding an unapproved background system.
   - Pilot an entire match lifecycle with one account, then reuse the same published facts from a second account; verify result isolation and no duplicated shared data.
   - Deliver a complete runnable application, supported-format examples, setup/run documentation and a concise manual-acceptance checklist. Include owner-authorized diagnostic exports of selected inputs, applied parameters, exclusions, intermediate numerical results and final outputs so the user can explain mismatches without exposing other accounts.
   - Mark the handoff "technically ready for user acceptance; original parity pending" when automated/end-to-end gates pass. The user then checks real imports, expected values and printable artifacts; record confirmation or corrections separately. Do not interrupt implementation for staged result approvals or treat a demo with mocked production paths as complete.
   - Independent post-validation review is warranted for multi-user authorization, historical leakage, freeze concurrency, two-database wiring and untrusted rendering/import. Recommend the user select MorWalPiz Code Reviewer; this plan does not assign or execute that review.
   - Stop at the initial completion boundary. Additional connectors, simulations, audio services or wider analytics require separate approval.

## Testing Strategy

### Numerical and import acceptance

Owning suite: new `FreezerAnalysis.Api.Tests`; technical readiness uses independently hand-worked specification examples, synthetic fixtures and mathematical invariants. Values produced by the engine under test are not independent expected results. The rows explicitly requiring the original races are deferred manual/parity checks, not failing or falsely passing technical-readiness tests.

| Required proof | Acceptance |
|---|---|
| Full-race import | Before delivery: synthetic official totals for every classified shooter reconcile within 0.02 points, including DQ/zeroed cases. After delivery: apply the same check to user-supplied actual reports, including the original two races if available. |
| Historical registry | Before delivery: hand-worked ratios/weights, homonym/division-change and prior-date tests. After delivery: reference comparison within 0.05 percentage points if inputs are supplied. |
| Sicily replay - deferred comparison | Requires the actual 42 earlier competitors and beta 1.25/drift 5%; target within 0.01 HF per stage; stage 4 lower/center/upper 9.27 / 10.55 / 11.82. Do not synthesize data merely to force those outputs. |
| Sicily verification - deferred comparison | Targets: displayed mean error -5.4%, SD 8.0%, coverage 10/12, normal-stage mean +0.4%, observed beta 1.18. Synthetic tests independently cover these calculations; actual historical outputs require the user's data. |
| Synthetic model recovery | Construct a complete positive-HF matrix from known centered stage effects and shooter levels; recover the known fit with an explicit floating-point tolerance. Independently test a hand-worked noisy/missing-score matrix, interval components, rival projections and weighted multi-race registry. |
| Exact error cost | At HF 7.5 and performance 0.935: 2.22 seconds for 32 rounds and 2.73 seconds for 12 rounds, displayed to two decimals. Also test non-positive denominators. |
| Charlie rounding | HF 6.84 and 32 rounds produces the displayed range "4-5" using an en dash in the UI; test all half-up tenth boundaries and 0%-50% clamps. |
| Turn arithmetic | Fourth of eight on stage 7 gives stages 7-12: 4,3,2,1,8,7 and stages 1-6: 2,1,8,7,6,5. No naive continuation across the last-to-first stage wrap. |
| Other boundaries | Inclusive rhythm thresholds 4.0/6.0/7.5/8.5/9.5; short/medium/long thresholds; Major/Minor; lunch separator; exact target-rule thresholds; empty/partial data; all excluded; single stage; regression degeneracy. |

Additional import tests: both decimal formats; gross/net reports; OT; report headers renamed files; duplicate/corrupt/encrypted/unsupported PDF; missing roster license; stage mismatches; partial/current/final transitions; conflicting same-match revisions; no publication on validation failure. Zero/HF/time/count checks must be consistent across CSV and PDF adapters.

### API, contracts, security and data integrity

- New host WebApplicationFactory tests: successful and negative import/profile/estimate/freeze/events/verification/adoption/artifact paths; typed error codes and cancellation.
- Real Mongo in disposable databases: unique-index races, conditional freeze/publication, interrupted staging, cursor pagination/query plans, cross-database isolation and no snapshot replacement. In-memory tests cannot prove these guarantees.
- Owner tests on every plan, event, input package, verification, parameter adoption, import mutation and download. Guessing another account's ID returns the established unavailable response; list endpoints never leak records.
- Session tests: missing/expired/revoked session, inactive/security-version change, stale CSRF, anonymous bootstrap only, origin restrictions, logout and no BackOffice cookie/bearer acceptance.
- Existing mixed-project `PublicAccountEndpointTests` plus focused ServerAPI host-containment/account metadata tests must remain green after shared-security extraction.
- Contract tests compare serialized C# DTO payloads with the app-local TypeScript consumers, including enums, nullability, provisional/unresolved states, UTC timestamps and diagnostics. Do not persist `it-IT` formatted strings as numeric DTO fields.
- Shared service tests: interleave public/admin/account clients and base URLs; prove no bearer/channel leakage, CSRF isolation and non-success responses are not treated as data.

### Rendering, UI and end-to-end

- Real-container renderer tests: page sizes/counts, fold orientation, two exact PNG dimensions, text area, required symbols, stage/turn order, no overflow and local-only resources.
- Before delivery, assert page dimensions, semantic content, safe text bounds and readability with pinned fonts/browser, then retain new-application raster baselines for regressions. These baselines do not prove original visual parity. After delivery, the user checks real cards/backgrounds; compare original images only if provided. Do not use PDF binary equality because metadata may differ.
- Vitest/Testing Library beside feature routes: loading/empty/error/provisional/ambiguous states, field-level errors, duplicate submission, stale freeze, manual/AI proposal acceptance, offline and reconnect outcomes.
- Real mobile browser: login, source upload, approve a draft, freeze/download, airplane-mode reading and queued updates, reconnect, download regenerated documents, final import/verification and second-account reuse.
- Verify 200% zoom, keyboard/focus order, screen-reader announcements, touch targets, text contrast and no horizontal scrolling at 320/390/768 CSS pixels; larger screens must preserve the field-first hierarchy.
- External failures: provider timeout/malformed schema, Blob unavailable/checksum mismatch, full browser storage, denied download permission and renderer exhaustion. Preserve prior accepted artifacts/work and surface failure.

### Migration and performance proof

- Dry-run historical import with synthetic records, second identical import, legacy snapshot binding and post-migration replay. No later result can contaminate the original cutoff. Real historical migration is available when the user supplies data, not a prerequisite to technical readiness.
- Backup/restore both application Mongo and referenced Blob packages into isolated storage, then reproduce a frozen estimate/artifact with pinned versions.
- Record full storage categories and targeted query performance on synthetic small/large races and 1,000 synthetic matches; no original or personal-data corpus is needed for scale tests.
- Apply M8 latency/resource gates on the real deployment image and declared resource tier. Publish actual samples/counts, not anecdotal "milliseconds."
- These operational qualification checks remain future delivery gates; focused API, SPA, renderer and synthetic numerical checks recorded above do not establish Mongo/Blob recovery, 1,000-match performance, or real-deployment resource budgets.

### Local development and supported example

The local API configuration is deliberately mock-only. It enables development authentication and the in-memory analysis repository only when `ASPNETCORE_ENVIRONMENT` is `Development` or `Test`; local frozen packages are written under the current user's LocalApplicationData. This path is for synthetic development data, not a production or multi-user deployment.

1. In one PowerShell window, start the API with the Development settings:

   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = 'Development'
   $env:ASPNETCORE_URLS = 'http://localhost:5072'
   dotnet run --project FreezerAnalysis.Api
   ```

2. In a second PowerShell window, point the SPA to that API and start Vite:

   ```powershell
   $env:VITE_FREEZER_API_BASE_URL = 'http://localhost:5072'
   yarn --cwd frontend workspace freezer-analysis.spa dev --host localhost
   ```

   Open the Vite URL printed in the second window. The Development API accepts loopback SPA origins and uses the configured development authentication identity; its in-memory match data starts empty on each API restart.

3. Poppler is only needed for PDF extraction. Install it locally and either add its `pdftotext` executable to `PATH` or set `FreezerAnalysis__PdfTextExtractorPath` to the executable path before starting the API. Do not add Poppler binaries or user PDFs to source control.

4. The synthetic CSV format example is [supported-score-csv-example.csv](./freezer-analysis-samples/supported-score-csv-example.csv). The supplied Overall/Verify PDFs are local parser-acceptance inputs, not distributable fixtures. PDF score previews remain unpublished until official scoring and total reconciliation are verified.

Production requires platform-account Mongo configuration, a distinct FreezerAnalysis application database, HTTPS private Blob storage with managed identity, production origins and host/session configuration. Do not enable the Development mock repository or development authentication outside Development/Test; no production credential values belong in this document.

### Manual acceptance checklist (after technical gates)

- [ ] Sign in with two PublicAccount identities; verify each user's profile, identity mappings, drafts, snapshots, field events, verification reports and adoptions remain private while both can reuse the same published match facts.
- [ ] Import a supported CSV and roster; confirm completeness, stage/competitor counts, identity uncertainty and official totals before publishing. Exercise missing, DQ and zeroed cases.
- [ ] Review the pre-match estimate and its included/excluded history, parameter version and diagnostics; freeze it, then confirm later publications and parameter adoption do not alter its replay.
- [ ] Download the A5/A4 and phone field card; check stage order, starting turn, symbols, text bounds and legibility. During an airplane-mode trial, read the frozen card, append field events and confirm reconnect reconciliation is idempotent and visibly complete.
- [ ] Import a final revision owned by the account; verify per-stage errors, group denominators/coverage, informant-drift comparison/exclusion counts, missing/DQ/zeroed diagnostics and that the frozen plan is unchanged.
- [ ] Backtest multiple eligible frozen races; verify selection/parameter changes clear the prior result and approval, adoption requires explicit confirmation, and stale adoption conflicts without changing existing drafts.
- [ ] Preview the supplied PDF report pair and confirm unknown/unreconciled score semantics remain explicitly unpublished; no PDF result may enter official verification until total reconciliation is independently accepted.
- [ ] Repeat key import, freeze, verification and download tasks at 320/390/768 CSS pixels and 200% zoom; check keyboard/focus order, screen-reader status announcements, touch targets, contrast and absence of unintended horizontal page scrolling.

## Migration And Rollback

1. **Compatibility window:** all changes are additive except the carefully bracketed shared-security extraction. Deploy the behavior-preserving security component with ServerAPI regression proof before enabling FreezerAnalysis. Old cookies/routes and account records remain valid.
2. **Mongo transition:** provision the separate application database and approved indexes; keep platform account/session collections in place. Import through versioned staging/publication, keyed by source/match identity. No EF/SQLite migration or global registry backfill is required.
3. **Historical data:** initial installation may start empty, with optional fictional demo data limited to Development/Test. Original historical migration is not required for readiness. When data is provided, map names/licenses with unresolved cases visible; migrate `escluso` and `prima` into the original account's private selections/snapshot availability. Preserve historical parameter/rule versions and frozen artifacts; never recompute them using current defaults. Label legacy imports explicitly.
4. **Deployment order:** private storage/database permissions and approved non-secret configuration -> feature indexes -> API/container and SPA artifact together -> session/readiness smoke checks -> restricted import/analysis pilot -> field pilot -> broader enablement.
5. **Flags:** introduce app availability plus AI-provider enablement independently. Disabled production features return explicit unavailable responses; mocks/fake auth remain Development/Test only. No silent fallback to the platform database.
6. **Cache:** existing APIs need no cache eviction. Version/hash-addressed artifacts remain immutable; client releases use versioned shell caches without discarding unsynced operations. A template revision produces a new artifact, not overwritten old bytes.
7. **Backups/monitoring:** retain current/previous app images, pinned engine/templates/fonts and encrypted backups of referenced Mongo/Blob data. Watch import rejection, unresolved identities, replay drift, freeze conflicts, render failures, latency, storage and sync backlog. Readiness must cover both Mongo boundaries, required private storage and required extraction/render dependencies without exposing connection details.
8. **Rollback:** disable new mutations/AI, retain private read/download access where safe, and restore the previous compatible image. Do not delete the app database, roll back platform accounts, replace shared results with older source files, or discard queued phone changes.
9. **Recovery limits:** code rollback cannot remove already disclosed data or reverse accepted shared corrections/user decisions. Recover those by a new audited revision or incident process. If snapshot/input-package deletion is legally required, mark associated replay unavailable and report the loss of reproducibility rather than fabricate an equivalent baseline.
10. **Durable jobs:** no new SQL/Hangfire migration is included initially. Any later asynchronous design requires its own approved storage, retry, idempotency, telemetry and rollback plan.

## Open Questions

Only consequential unresolved evidence/approval remains:

1. The supplied Overall/Verify pair exposes 142 classification entries and 1,704 per-stage rows, with 142 unique normalized-name/division pairs. The verification rows contain hit factor, points, A/C/D, deductions, misses, no-shoots, procedurals, overtime and time; the Overall report supplies rank, percent and classification points plus a disqualification appendix. All seven DQ appendix entries parse, but none maps uniquely to both participant lists; the preview preserves those names/page references as unresolved and does not infer non-DQ for any participant while mapping is incomplete. The PDFs do not state authoritative stage minimum rounds or per-division stage maxima. An aggregate-only candidate using maxima inferred from observed hits fits the 142 classification totals within 0.02 points, but the handoff explicitly prohibits treating observed hits as authoritative stage maxima; that candidate is not accepted reconciliation and publication stays disabled. Obtain the official course/stage maxima or owner-validated mapping before enabling score publication. Roster layouts and original mathematical/visual parity remain unverified until separately exercised.
2. Is an optional ESS/WinMSS/live connector required after the initial documented CSV/PDF import surface? Representative exports/access permission would determine that separate adapter scope.
3. Who approves shared-result retention, correction/deletion handling and the approved de-identified LLM deployment/processing terms? These govern AI activation/public release, not completion of the application in an isolated test environment.
4. What production resource tier, host binding, expected concurrency and yearly-retained match volume should qualify the M8 budgets? Use the stated baseline for implementation/testing and document the measured environment; do not claim production capacity without it.

Resolved for implementation: proceed without Python/datasets/golden artifacts; assume PublicAccount identity; use the documented CSV baseline and explicit provisional layout; deliver all application workflows before requesting the user's numerical/visual confirmation. Manual acceptance does not waive automated correctness, security, persistence or end-to-end testing.
