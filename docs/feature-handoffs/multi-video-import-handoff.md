# Multi-Video Import Handoff

## Reference Classification Follow-Up

The [reference-based classification handoff](video-reference-classification-handoff.md)
supersedes stored type as the runtime video/collection decision: root import
starts with one reference; a second attached reference makes its effective type
a collection without rewriting the archival discriminator. Shared refresh now
retains all attachments and refreshes their actual IDs, preserving editorial
aggregate metadata for multi-reference content. Related mutation/compensation
cache invalidation includes enriched calendar destinations. The bulk contract,
feature flag, canonical-link outcomes, and historical plan below are unchanged.

## Outcome

Repair the feature-flagged BackOffice bulk import so an operator can import multiple root YouTube videos in one request, assign categories to every imported video, and attach zero or more newly imported reference videos to each root. The implementation must reuse the proven single-import and reference-append behavior instead of maintaining a third copy in `VideosController`.

Assumptions for this plan:

- A bulk request contains one or more root imports. Each root has required categories and zero or more reference imports; each reference also has its own required categories.
- A YouTube video ID may appear only once across the complete request and cannot be both a root and a reference.
- Reference choices come from the same discovered, not-yet-imported candidate set. Importing arbitrary IDs remains available through the existing single import and edit workflows.
- The batch is intentionally not one database transaction. A successful root remains valid if a later reference or another root fails, and the response reports every attempted or skipped item.
- The existing `EnableVideoBulkImport` flag remains the release and rollback control. It stays disabled in production until the new API, SPA, and smoke-test evidence are deployed together.

Completion means the grouped workflow works for at least two roots and multiple references, each persisted `VideoRef` keeps its selected categories and provider metadata, every persisted video receives a canonical short link, failures have explicit per-video outcomes, and the single-import and add-reference contracts remain behaviorally compatible.

Non-goals are importing into an arbitrary existing collection from this screen, concurrent/background import execution, YouTube upload, schema redesign, broad API versioning, cache infrastructure redesign, and unrelated cleanup in the large controller.

Independent post-validation review is warranted because this change coordinates persistence, compensation, canonical short links, authorization, cache invalidation, an external provider, and a feature-flagged UI. The user should select `MorWalPiz Code Reviewer` after implementation and focused validation.

## Current Evidence

- `MorWalPizVideo.BackOffice/Controllers/VideosController.cs`, `Import`, implements root creation: effective-user resolution, duplicate concealment, channel-scoped category resolution, YouTube metadata retrieval, `YouTubeContent.CreateSingleVideo`, persistence/read-back, `IExternalDataService.RefreshMatch`, canonical short-link creation, compensation, and cache invalidation.
- The same controller's `AddVideoReference` implements reference attachment: manageable-target authorization, category and metadata validation, atomic `IContentService.AddVideoReferenceWithCacheAsync`, canonical short-link creation, append compensation, and cache-status reporting.
- Its current `BulkImport` does not reuse those paths. It duplicates category/metadata/root creation, appends references by replacing the complete aggregate through `UpdateMatchAsync`, does not create a canonical short link for appended references, returns `shortLinkStatus = notAttempted`, and resolves references only when their target appeared earlier in the request.
- `MorWalPizVideo.Domain/Interfaces/Repository.cs`, `YouTubeContentRepository.AddVideoReferenceAsync`, already provides the correct MongoDB primitive: duplicate-protected atomic append plus atomic recomputation of `latestPublishedAt`.
- `MorWalPizVideo.Domain/Interfaces/MockRepository.cs`, `MatchMockRepository.AddVideoReferenceAsync`, mirrors the duplicate and ordering behavior for deterministic tests.
- `MorWalPizVideo.Domain/FeatureServices.cs`, `ContentService.AddVideoReferenceWithCacheAsync`, wraps the atomic append and reports local indexed-cache status without converting a persisted mutation into a false failure.
- `MorWalPizVideo.Domain/FeatureServices.cs`, `LinksService.EnsureVideoShortLinkAsync`, owns idempotent canonical standalone YouTube links. `ShortLinkCleanupPendingException` means the canonical link was persisted and only legacy cleanup remains pending; this state must be a warning, not destructive compensation.
- `MorWalPizVideo.Models/Models/YouTubeContent.cs` models one content aggregate with root metadata and a `VideoRefs` array. `CreateSingleVideo` already creates the root reference, so additional selected videos must be appended rather than rebuilding the aggregate.
- `MorWalPizVideo.BackOffice/DTOs/VideoBulkImportRequests.cs` currently uses a flat item with one optional `Target`, documented as an existing content ID or an earlier item. This contract encodes ordering and does not express a root with multiple categorized references directly.
- `frontend/back-office-spa/src/routes/videos/import/Component.tsx` reproduces the ordering constraint by offering only existing targets and previously selected candidates. It holds bulk state locally, which is appropriate for an ephemeral form, but its current selection shape cannot represent the desired grouped workflow clearly.
- `frontend/back-office-spa/src/routes/videos/import/loader.ts` loads categories and legacy import targets. `frontend/back-office-spa/src/services/videoService.ts` owns app-local bulk request/result types and endpoint calls; no other repository consumer of the bulk contract was found.
- `frontend/back-office-spa/src/routes/videos/import/__tests__/Component.test.tsx` covers basic rendering, single import, mixed bulk toasts, and the disabled flag. It does not cover grouped payloads, multiple references, per-reference categories, uniqueness, or partial failure.
- `MorWalPizVideo.BackOffice.Tests/Features/AdminEditWorkflowTests.cs` verifies single import metadata/canonical links and reference append metadata, categories, canonical links, and cache operations. `ChannelTenancyAuthorizationTests.cs` and `VideoPermissionAuthorizationTests.cs` cover bulk tenancy and permissions, but there is no complete bulk workflow behavior suite.
- `docs/architecture/adr/ADR-004-canonical-short-links.md` requires standalone canonical links, mutation rights in the selected channel, duplicate-import safety, and cache eviction after video mutations.
- `MorWalPizVideo.BackOffice/appsettings.json` and `appsettings.Development.json` currently declare the flag enabled, while `docs/GITHUB_PRODUCTION_DEPLOYMENT.md` documents a production App Service override that disables it. Deployed flag state is not verifiable from source.
- No `PRODUCT.md` or `DESIGN.md` was found for the BackOffice SPA. The incumbent React Bootstrap admin workflow is therefore the visual authority; this is an Operate-mode refinement, not a redesign.

## Proposed Design

### Contract

Replace the order-dependent bulk request with an explicit grouped request at the existing feature-gated bulk endpoint:

- The request contains bounded root items.
- Each root item contains a trimmed root video ID, a non-empty category-ID collection, and a reference collection.
- Each reference contains its own trimmed video ID and non-empty category-ID collection.
- The response contains a result group for each requested root: one root outcome followed by outcomes for its references. Every outcome identifies the video, its root/reference role, status, short-link status, cache status when available, and a stable error code plus safe message when unsuccessful.
- Shape errors that invalidate the request as a whole return `400`: empty batch, limit exceeded, empty/duplicate IDs, self-reference, one video assigned more than once, or missing categories.
- Runtime outcomes remain per video in a successful HTTP response: already imported, provider metadata unavailable/missing, persistence failure, canonical-link warning/failure, cache degradation, root failed so reference was not attempted, or imported.

The implementation should define one reviewed maximum for the total distinct videos in a request, counting roots and references. Use a named validation constant rather than scattering a number across DTOs, controllers, and UI. The value should be chosen against acceptable synchronous YouTube and cache-call latency before implementation; 25 total videos is the recommended initial ceiling.

The old `Target` field must not drive the new workflow. Because the only source consumer is the SPA and production is documented as flag-disabled, coordinate the request change behind the flag instead of carrying two behavioral models indefinitely. Keep `GET import-targets` temporarily as a compatibility endpoint, mark it unused by the SPA, and remove it only in a later telemetry-backed cleanup.

### Reusable Workflow Boundary

Add a scoped BackOffice application service, `IVideoImportWorkflowService` with `VideoImportWorkflowService`, rather than invoking one controller action from another or moving BackOffice orchestration into Models/Contracts.

The service owns shared import mechanics:

1. Resolve and verify all requested category IDs in the selected channel.
2. Fetch and validate YouTube metadata and build the server-owned `VideoRef`.
3. Create a root through the existing `CreateSingleVideo` sequence, including metadata refresh, read-back, canonical short link, compensation, and cache reporting.
4. Attach a reference through `AddVideoReferenceWithCacheAsync`, then ensure its canonical short link and compensate only that append when link creation fails before a canonical record exists.
5. Map known provider, duplicate, persistence, short-link, cleanup-pending, and cache outcomes to typed service results. Unexpected exceptions remain logged and propagate to the host error boundary after any required local compensation.

The service receives explicit effective user ID and selected channel ID values from the controller. It does not read `HttpContext`, evaluate permissions, or return MVC results. `VideosController` remains responsible for authentication, channel context, `AllowUser`, concealment, target authorization, request-to-HTTP mapping, and response DTO projection.

Refactor all three entry points to use the service:

- `Import` calls the root operation and preserves the current single-import HTTP contract, including duplicate conflict behavior.
- `AddVideoReference` performs its existing manageable-target authorization and calls the reference operation, preserving the current response contract.
- `BulkImport` validates the complete graph before the first write, then processes each group root-first and references in submitted order. A failed root marks that group's references not attempted; other groups continue. A failed reference does not remove its valid root or previously attached references.

Do not optimize by deferring all cache invalidation to the end of the batch in this change. Per-mutation invalidation is more expensive but preserves correctness if the request is interrupted. A later measured optimization may add a durable/coalesced invalidation design.

### Short Links And Compensation

- Root persistence is compensated when metadata refresh or canonical-link creation fails before a usable canonical link exists.
- Reference append is compensated with the existing expected-reference removal when canonical-link creation fails before a usable canonical link exists.
- `ShortLinkCleanupPendingException` is a warning for both operations because its `Link` is already canonical and persisted. Keep the root/reference and return a pending warning; do not delete content and leave the canonical record orphaned.
- Cache failure after persistence is reported as degraded and logged, not treated as import failure.
- Batch rollback is explicitly unsupported because Mongo content and standalone short links span collections without a transaction boundary. The UI must explain outcomes through statuses, not promise atomicity.

### Frontend Flow

Refine the existing bulk tab in Operate mode:

- Keep date-range candidate discovery, channel requirement, single-import tab, PageHeader, toast, and error components.
- A selected candidate becomes a root import. Its category group is required.
- Each root exposes an Add reference control using remaining unassigned candidates. Each added reference renders its own required category group and a Lucide remove action with a tooltip/accessible label.
- Exclude the root itself and every candidate already assigned anywhere else in the batch. Removing a root or reference returns its candidates to the available set.
- Remove the legacy existing/previous-target selector and stop loading import targets from this route.
- Disable date, selection, category, and submit controls while the request is pending. Preserve form selections after partial/error outcomes so the operator can inspect or repair them; clear only groups whose complete result is successful.
- Render grouped results beneath each root, with text and Bootstrap semantic status treatment rather than color alone. Announce request-level validation and completion summaries accessibly.
- Keep request/result types app-local in `videoService.ts`; there is no second frontend consumer justifying expansion of `@morwalpizvideo/models` or `@morwalpizvideo/services`.

Use the installed Impeccable `shape` workflow during implementation planning for `frontend/back-office-spa/src/routes/videos/import/Component.tsx`. The target mode is Operate, incumbent code is the visual authority, and the work is a refinement. Minimum visual evidence is one desktop and one narrow/mobile capture showing multiple roots, nested references, long titles, validation, pending state, and mixed results, plus keyboard and label checks. No new `PRODUCT.md` or `DESIGN.md` is required for this bounded refinement.

## Impacted Projects

- `MorWalPizVideo.BackOffice`: owns the controller, new import workflow service, DI registration, feature gate, external integrations, canonical links, cache coordination, and BackOffice-local bulk DTOs.
- `MorWalPizVideo.Domain`: verify-only for `IContentService`, atomic repository append, metadata refresh dependency, and `ILinksService`; no ownership move or repository change is expected.
- `MorWalPizVideo.Models`: verify-only for `YouTubeContent.CreateSingleVideo`, `VideoRef`, category references, and `LatestPublishedAt`; no schema change is expected.
- `MorWalPiz.Contracts`: verify-only unless extraction exposes a missing shared outcome type. Existing single-import and add-reference request contracts should remain unchanged; the BackOffice-only bulk DTO should not be moved here.
- `frontend/back-office-spa`: owns the grouped import interaction, app-local API types, loader simplification, and component tests.
- `frontend/fe-packages/models`, `services`, and `layout`: verify-only consumers/build prerequisites; no shared-package change is recommended.
- `MorWalPizVideo.BackOffice.Tests`: owns HTTP behavior, authorization, provider-failure, compensation, cache, and compatibility tests.
- `MorWalPizVideo.ServerAPI` and `MorWalPizVideo.ShortLinks`: no source change; verify public cache refresh and canonical redirect behavior through existing cross-service contracts.
- `MorWalPizVideo.AppHost`: no topology change; verify-only for local composed smoke testing.
- Deployment surface: BackOffice API and BackOffice SPA deployment plus the production `EnableVideoBulkImport` setting.
- All other .NET projects, frontend applications, Windows tools, and standalone tools: not impacted.

## Impacted Files

### Modify

- `MorWalPizVideo.BackOffice/Controllers/VideosController.cs`: make single, reference, and bulk actions thin callers of the workflow service; retain auth/channel checks and HTTP mapping; remove duplicated import mechanics from the controller.
- `MorWalPizVideo.BackOffice/DTOs/VideoBulkImportRequests.cs`: replace the ordered target contract with grouped root/reference request DTOs, bounded validation, and explicit grouped outcome DTOs.
- `MorWalPizVideo.BackOffice/Program.cs`: register the scoped workflow service once for mock and production provider branches.
- `frontend/back-office-spa/src/routes/videos/import/Component.tsx`: implement grouped root/reference selection, per-video categories, validation, pending state, and grouped outcomes.
- `frontend/back-office-spa/src/routes/videos/import/loader.ts`: stop loading legacy targets and return only data needed by the refined route.
- `frontend/back-office-spa/src/services/videoService.ts`: replace flat target types with grouped request/result types while retaining shared transport and channel-header behavior.
- `frontend/back-office-spa/src/routes/videos/import/__tests__/Component.test.tsx`: replace target-order assertions and add grouped interaction, validation, payload, accessibility, pending, partial-result, and flag-disabled cases.
- `MorWalPizVideo.BackOffice/README.md`: describe the repaired grouped behavior and keep the flag's rollback role explicit.
- `docs/architecture/backend.md`: record the focused BackOffice video-import workflow boundary and its root/reference compensation semantics.
- `docs/architecture/api-design.md`: document the feature-gated grouped bulk contract, validation, per-video outcomes, and compatibility window.
- `docs/architecture/adr/ADR-004-canonical-short-links.md`: clarify that every bulk root/reference gets a canonical standalone link and that cleanup-pending is non-destructive.
- `docs/GITHUB_PRODUCTION_DEPLOYMENT.md`: replace disable-only wording with disabled-by-default staged activation, smoke checks, monitoring, and immediate flag rollback.

### Add

- `MorWalPizVideo.BackOffice/Services/Interfaces/IVideoImportWorkflowService.cs`: controller-independent root, reference, and grouped orchestration contract with typed outcomes.
- `MorWalPizVideo.BackOffice/Services/VideoImportWorkflowService.cs`: shared category, metadata, persistence, canonical-link, compensation, cache, and outcome orchestration.
- `MorWalPizVideo.BackOffice.Tests/Features/VideoBulkImportWorkflowTests.cs`: focused HTTP behavior coverage for grouped imports and failure boundaries.

### Generated/Migration

None. No MongoDB document shape, collection, or index changes are required. Do not add an EF or Mongo migration.

### Documentation

- `docs/feature-handoffs/multi-video-import-handoff.md`: this plan; update it after implementation with actual files, validation evidence, release state, and residual risks.

### Verify Only

- `MorWalPizVideo.Domain/FeatureServices.cs`: existing content append/cache and canonical-link behavior.
- `MorWalPizVideo.Domain/Interfaces/Repository.cs`: Mongo atomic append and latest-published calculation.
- `MorWalPizVideo.Domain/Interfaces/MockRepository.cs`: mock atomic append parity.
- `MorWalPizVideo.Models/Models/YouTubeContent.cs`: aggregate and root/reference invariants.
- `MorWalPizVideo.BackOffice.Tests/Features/AdminEditWorkflowTests.cs`: single import and reference-add regression evidence.
- `MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs`: cross-channel mutation denial.
- `MorWalPizVideo.BackOffice.Tests/Features/VideoPermissionAuthorizationTests.cs`: import permission and feature-state behavior.
- `frontend/fe-packages/services`: shared authenticated/channel-scoped transport.
- `MorWalPizVideo.AppHost`: composed BackOffice API/SPA startup and routing.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Replace duplicated controller bulk logic with a scoped application workflow and grouped request/results. Preserve single and add-reference endpoints. |
| Frontend/UI | Refine the bulk tab to model roots with multiple categorized references; remove ordering and target dropdown dependence. |
| Database/Data | No schema/index migration. Use existing root insert, atomic reference append, expected-reference compensation, and canonical short-link collection. Partial successful groups remain valid data. |
| Contracts/Models | BackOffice-local bulk DTO changes in coordination with its sole SPA consumer. Existing shared single/reference contracts and persisted models remain unchanged. |
| Authentication/Authorization | Keep `RequireChannelScope` and `videos.import`/`videos.manage` on bulk operations. Keep manageable-target checks on the standalone add-reference route. Never accept channel or creator identity from the payload. |
| Validation | Validate the full graph before writes: bounded total count, required/unique trimmed IDs, no self-reference, one assignment per ID, and categories for every root/reference. Server remains authoritative. |
| Cache | Reuse current matches/short-links reset, purge, reload, and per-match refresh behavior. Report degraded cache status without rolling back valid persistence. |
| Configuration/Secrets | Keep the existing feature flag; no new secret or setting. Production activation remains an external deployment setting after evidence. |
| Logging/Telemetry | Add structured group/root/reference outcome logs without titles, descriptions, tokens, or provider payloads. Record counts, channel ID, video ID, outcome, duration, and compensation/cache status. Existing OpenTelemetry captures request and HTTP-client spans. |
| Background Jobs | Not applicable. The bounded synchronous workflow remains operator initiated; Hangfire is not needed for the initial ceiling. |
| Deployment/Operations | Deploy API and SPA while the production flag is off, smoke test with controlled channel data, then enable deliberately. Monitor provider errors, compensation, latency, cache degradation, and 5xx rate. |
| Backward Compatibility | Single import, add-reference, persistence, and shared frontend packages remain compatible. The bulk request changes in lockstep with its only known SPA consumer while feature-disabled; keep the flag off across mixed-version deployment. |

## Reuse Opportunities

- Reuse `YouTubeContent.CreateSingleVideo` and `VideoRef`; do not construct a competing aggregate shape.
- Reuse `IContentService.SaveMatchAsync`, `FindMatchAsync`, `AddVideoReferenceWithCacheAsync`, `RemoveVideoReferenceAsync`, category lookup, and the indexed-cache notifications.
- Reuse `IYTService.FetchFromYoutube` and the existing mock provider; do not add another YouTube client.
- Reuse `IExternalDataService.RefreshMatch` for root URL/metadata enrichment so bulk roots match single import behavior.
- Reuse `ILinksService.EnsureVideoShortLinkAsync` and existing canonical-link compensation rules.
- Reuse `ICrossApiService` and centralized `CacheKeys`; do not introduce local cache strings.
- Reuse effective-user resolution, channel context, `IVideoAuthorizationService`, and existing `AllowUser` permissions at the controller boundary.
- Reuse `PageHeader`, `GenericErrorList`, toast context, React Bootstrap form controls, Lucide icons, and route-local state. Do not introduce a new store or shared layout component.
- Reuse `WebApplicationFactory`, deterministic scenarios, recording cross-API service, fake YouTube metadata, and current auth helpers in `MorWalPizVideo.BackOffice.Tests`.

## Risks

- **High - Partial multi-resource failure.** Cause: content, canonical links, external metadata, and cache calls do not share a transaction. Surface: a root can succeed while a reference fails. Mitigation: validate before writes, root-first ordering, per-operation compensation, explicit grouped outcomes, and no batch-atomic claim. Verification signal: failure tests show valid prior writes retained, failed append removed when required, and no orphan created by cleanup-pending handling.
- **High - Canonical short-link inconsistency.** Cause: current bulk append bypasses link creation, while current root handling may treat cleanup-pending as total failure. Surface: redirects and public content. Mitigation: one workflow service and explicit pending semantics for both root and reference operations. Verification signal: every imported ID resolves to exactly one standalone canonical link in success/pending tests.
- **High - Cross-channel or duplicate mutation.** Cause: grouped input can obscure ownership or reuse one candidate under multiple roots. Surface: tenant data integrity. Mitigation: derive channel/effective user server-side, reject duplicate graph nodes before writes, and retain authorization checks. Verification signal: cross-channel, collaborator, duplicate, self-reference, and repeated-assignment tests produce no mutation.
- **Medium - Mixed-version deployment.** Cause: the bulk DTO changes while API and SPA deploy independently. Surface: temporary request failures. Mitigation: keep production flag off until both artifacts are deployed and verified; do not enable during a mixed version. Verification signal: feature-state response remains false until post-deployment smoke evidence is complete.
- **Medium - Request latency and provider quota.** Cause: each selected video requires metadata, persistence, link, and cache work. Surface: operator timeout and YouTube quota. Mitigation: bounded total videos, sequential deterministic processing initially, request-duration telemetry, and no unbounded date selection submission. Verification signal: maximum-size mock performance test completes within the selected request timeout and production canary latency stays acceptable.
- **Medium - Cache amplification.** Cause: correctness-preserving per-item invalidation repeats cross-API calls. Surface: ServerAPI/cache availability. Mitigation: retain bounded batches and degraded status; defer coalescing until measured. Verification signal: recording fake proves required keys/tags and telemetry reports cache failures separately from import failures.
- **Medium - Retry usability after partial success.** Cause: imported candidates disappear from discovery after persistence. Surface: repairing failed references. Mitigation: preserve UI state and per-reference outcomes for immediate retry or direct the operator to the existing video edit/add-reference workflow. Verification signal: UI partial-failure test retains failed selections and identifies the created root.
- **Low - Visual density on narrow screens.** Cause: nested roots, references, and categories. Surface: mobile/admin usability. Mitigation: Operate-mode shape pass, stable nested layout, wrapping labels, and narrow-viewport evidence. Verification signal: no overlap or horizontal clipping in the required mobile capture.

## Technical Debt

- `VideosController` is large and currently owns orchestration. Extract only video import mechanics now; broader controller decomposition is non-blocking and deferred.
- Error responses mix strings, anonymous objects, and DTOs rather than consistent Problem Details. Use stable error codes in the new bulk DTO, preserve existing endpoint compatibility, and defer repository-wide error migration.
- The SPA has an unused route `action.ts` for import while the component calls `VideoService` directly. It is non-blocking; remove it only if route registration confirms it is dead during implementation, otherwise leave it isolated.
- `GET import-targets` becomes unused by the repaired SPA. Keep it temporarily for compatibility and defer removal until usage is verified.
- Per-item cache invalidation may be inefficient. It does not block correctness and should be optimized only with measured evidence and interruption-safe semantics.
- Checked-in flag defaults and deployment override documentation differ. This does not block implementation, but release evidence must record the actual deployed value before and after activation.

## Implementation Roadmap

1. **Lock acceptance and contract.** Confirm the grouped root/reference interpretation, the 25-video initial ceiling, candidate-only references, and partial-success semantics. Update this handoff if any decision changes. Gate: request and response properties, status/error vocabulary, and stop conditions are agreed before source edits.
2. **Add focused failing backend tests.** In `MorWalPizVideo.BackOffice.Tests`, specify two roots with multiple references and distinct categories, graph validation, permission/channel denial, provider failure, duplicate handling, link failure compensation, cleanup-pending warning, cache degradation, and preservation of existing single/reference contracts. Gate: tests fail specifically because current bulk ordering/duplication cannot satisfy them.
3. **Introduce the reusable workflow service.** Add the interface and scoped implementation in BackOffice, compose existing content, YouTube, external-data, links, cross-API, and logging dependencies, and register it in `Program.cs`. Keep HTTP/auth concerns out. Gate: direct service or HTTP tests prove root and reference operations preserve current successful and compensating behavior.
4. **Refactor existing entry points first.** Make `Import` and `AddVideoReference` call the service without changing their routes, permissions, status codes, or established response fields. Gate: existing AdminEditWorkflow and permission tests pass before bulk behavior changes.
5. **Implement grouped bulk orchestration.** Replace target-order processing with whole-request graph validation and root-first grouped execution. Continue independent groups after known failures and return explicit outcomes for unattempted references. Gate: new backend workflow tests pass and demonstrate atomic repository append rather than full aggregate replacement.
6. **Shape and implement the SPA refinement.** Apply the Impeccable `shape` lens, update app-local service types and loader, then implement root/reference assignment, categories, validation, pending state, and grouped outcomes in the route component. Gate: focused Vitest/Testing Library cases pass for multi-root payloads, multiple references, uniqueness, accessibility labels, partial failure, and flag-off behavior.
7. **Validate cross-boundary behavior.** Run the focused backend class, relevant existing import/reference/authorization classes, focused SPA test file, complete BackOffice test project, BackOffice SPA suite, and SPA production build. Perform a composed mock-mode smoke flow and verify canonical redirects/cache calls without real providers. Gate: no regression in single import or edit add-reference, and no empty selected test set.
8. **Update architecture and operational docs.** Record the final contract, service boundary, canonical-link semantics, flag activation, and actual validation in the listed documents and this handoff. Gate: docs distinguish implemented source behavior from pending production activation.
9. **Staged release and stop.** Deploy API and SPA with the flag off, verify feature state and selected-channel smoke behavior, enable for a controlled operator/channel, monitor, then broaden only after the observation window. Stop when the acceptance cases pass and the production flag decision is recorded; background import, existing-target import, cache coalescing, and broader controller cleanup require separate approval.

## Testing Strategy

- **Unit/service:** Test graph validation, category resolution, service result mapping, cleanup-pending behavior, root compensation, reference compensation, and root-failure/reference-not-attempted logic. Pure result mapping belongs in focused tests if extracted; avoid mocks that merely repeat implementation.
- **Backend integration/behavior:** In `MorWalPizVideo.BackOffice.Tests`, use `WebApplicationFactory`, test auth, selected channel, scenario repositories, fake YouTube service, and recording cross-API service. Cover two roots, multiple references, distinct categories, canonical links, latest-published ordering, duplicate/self/shared reference rejection, unknown/cross-channel categories, missing metadata, provider unavailable/timeout, persistence failure, short-link failure/pending, cache degradation, feature disabled, missing channel, import permission, manage permission, and collaborator denial.
- **Contract compatibility:** Keep existing single import conflict/error/success shapes and add-reference `VideoRefContract` behavior. Assert the new grouped bulk response has deterministic root/reference correlation and stable machine-readable statuses.
- **Frontend component:** In the existing import route suite, cover selecting multiple roots, adding/removing multiple references, per-video categories, global uniqueness, disabled/pending controls, exact payload, complete success clearing, partial failure preservation, mixed summary/result rendering, empty candidates, long titles, and flag-off single import.
- **Security:** Verify payload channel/creator values cannot be supplied, category IDs are scoped, unauthorized/inaccessible targets remain concealed, and logs/responses do not expose provider payloads or secrets.
- **Migration:** Not applicable to stored schema. Compatibility testing focuses on the feature-disabled API/SPA cutover.
- **End to end:** In mock mode, discover candidates, import two roots with references, retrieve each aggregate, verify canonical short-link records, exercise one redirect through the existing ShortLinks test boundary, and observe expected cache calls. Real YouTube and production cache behavior remain controlled canary evidence, not automated test dependencies.
- **Visual/accessibility:** Capture desktop and narrow layouts with nested references and mixed outcomes. Verify keyboard operation, focus visibility, labels/`aria-describedby`, status text independent of color, no overlap, and no horizontal clipping.
- **Performance:** Exercise the agreed maximum total-video request with deterministic fakes and record duration/call counts. Production canary monitoring, not a fake benchmark alone, determines whether the initial ceiling is acceptable.

## Migration And Rollback

- **MongoDB:** No document, index, or collection migration. Existing `YouTubeContent.VideoRefs`, `latestPublishedAt`, and standalone short links remain authoritative. Partial successful imports are ordinary valid records and must not be deleted automatically during rollback.
- **API contract:** The grouped bulk request is a coordinated breaking change for a feature documented as production-disabled and with one known source consumer. Keep the flag off while API and SPA versions differ. The single and add-reference contracts remain compatible.
- **Authentication:** No migration. Existing cookie/JWT host behavior, `X-Channel-Id`, effective impersonated user, and permissions remain authoritative.
- **Cache:** No key/tag migration. Rollback does not require cache schema work; after disabling the feature, use existing operational cache refresh only if monitoring shows stale content.
- **Configuration:** No new setting or secret. Preserve the production flag override as false through deployment. Enable only after API/SPA and smoke gates; record actual environment state because source cannot prove it.
- **Deployment order:** First deploy BackOffice API with the new service/contract while the flag is off, then deploy the SPA, verify feature state and smoke tests, then enable for a controlled channel/operator. Do not enable between artifact deployments.
- **Monitoring:** Track grouped request count, total/root/reference item counts, duration, provider failures/timeouts, duplicate/validation outcomes, compensation attempts/failures, short-link pending/failures, cache degradation, and 5xx responses. Avoid titles/descriptions and credentials in telemetry.

## Implementation Update

The feature is implemented in the existing feature-gated BackOffice bulk endpoint and SPA route. The request now uses grouped `roots` with categorized `references`, validates the complete graph and 25-video limit before writes, processes roots independently, appends references through the atomic content-service operation, creates canonical links for appended references, and returns grouped per-item outcomes. Failed roots produce `notAttempted` reference outcomes; cleanup-pending short links are retained as warnings.

Changed files: `MorWalPizVideo.BackOffice/DTOs/VideoBulkImportRequests.cs`, `MorWalPizVideo.BackOffice/Controllers/VideosController.cs`, `frontend/back-office-spa/src/services/videoService.ts`, `frontend/back-office-spa/src/routes/videos/import/loader.ts`, and `frontend/back-office-spa/src/routes/videos/import/Component.tsx`.

Validation: `dotnet restore` and the BackOffice build pass with no errors; the build reports 68 existing warnings. The focused `Component.test.tsx` import-route suite passes under Node 26 using the pinned Yarn Classic command. The planned `IVideoImportWorkflowService` extraction remains deferred; the current behavior is still controller-owned and should be extracted before broader reuse. Production activation remains feature-flag controlled and requires API/SPA deployment together with the staged smoke checks described above.
- **Rollback:** Immediately disable `EnableVideoBulkImport`. Existing single import and edit add-reference remain available. If necessary, roll back SPA and API only after the flag is confirmed off. Do not mass-delete successful records; reconcile any reported failed/pending item through repository and canonical-link evidence.
- **Rollback limit:** Disabling or reverting code cannot undo valid content and links created before rollback. Data correction must be evidence-based and item-specific because batches permit partial success.

## Open Questions

1. Confirm that “one or multiple reference videos” means each new root video owns zero or more newly imported candidate references, rather than choosing one or more existing content aggregates as targets. This plan adopts the former and removes the target selector from the SPA.
2. Confirm the initial synchronous request ceiling. This plan recommends 25 distinct videos across all roots and references; a materially larger ceiling may require background execution, durable progress, and a different timeout/retry design.
3. Confirm partial-success policy. This plan retains a successful root and earlier references when a later reference fails, then exposes repair through preserved UI state or the existing edit/add-reference workflow. All-or-nothing group semantics would require broader cross-collection compensation and link cleanup design.