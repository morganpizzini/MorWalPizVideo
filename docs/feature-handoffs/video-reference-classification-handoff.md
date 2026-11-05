# Reference-Based Video Classification Handoff

Status: Implemented locally; deployment and browser/Windows evidence remain pending.
Evidence checked: 2026-11-04.

## Outcome

Remove the ineffective editable "Match Type" field. Determine whether content
is a single video or a collection from its attached `VideoRefs`, consistently
across administrative presentation, API responses, display conversion,
metadata refresh, calendar links, and short-link resolution.

Classification is not an operator setting:

| Attached references | Classification | Administrative acknowledgement |
|---|---|---|
| 0 | Collection, empty | Empty Collection (0 videos) |
| 1 | SingleVideo | Single Video (1 video) |
| 2 or more | Collection | Collection (N videos) |

Only aggregate `VideoRefs` count. Images, `YouTubeVideoLinks`, canonical short
links, and the thumbnail are not additional references. Missing/null legacy
arrays are empty for classification, not permission to invent a playable video.
Count stored attachments; do not silently deduplicate invalid legacy data.
Existing duplicate-protected append validation remains responsible for uniqueness.

Assumptions and boundary:

- This replaces the editable discriminator and its runtime authority, not every
  occurrence of the name `ContentType`. Image MIME types and the separate display
  enum remain untouched.
- Retain existing BSON `contentType` as non-authoritative compatibility metadata
  in this release. Do not synchronize it on every reference mutation.
- Keep current API `contentType`/`isLink` names and enum encodings, but derive
  their values from references. Physical field removal and breaking DTO removal
  require separate approval.
- Do not change aggregate identities, URLs/slugs, thumbnail choices, ownership,
  permissions, reference order, canonical link records, or bulk-import contracts.
- Application changes and local validation are recorded in Delivery Evidence
  below. No configuration, data migration, or deployment was performed.

Completion requires the matrix above even when stored type disagrees; successful
reference mutations and compensation change acknowledgement automatically;
metadata refresh preserves every attachment; and public links target actual
references rather than an inferred thumbnail or aggregate identifier.

An independent post-validation review is warranted: refresh can write aggregate
data, and three hosts expose compatibility-sensitive public behavior. The user
may select MorWalPiz Code Reviewer after implementation and focused verification.

## Current Evidence

Paths below are relative to the repository root. Source is authoritative over
historical plans and architecture descriptions.

- [Edit component](../../frontend/back-office-spa/src/routes/videos/edit/Component.tsx)
  renders a `matchType` select initialized from `match.contentType`.
  [Edit action](../../frontend/back-office-spa/src/routes/videos/edit/action.ts)
  forwards form entries, but
  [VideoUpdateRequest](../../MorWalPiz.Contracts/DTOs/VideoUpdateRequest.cs) has
  neither `MatchType` nor `ContentType`.
  [VideosController.Update](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs)
  preserves the stored type. The normal form update uses atomic mutable-field
  writes and omits `VideoRefs`; reference append is a separate mutation.
- [Video detail](../../frontend/back-office-spa/src/routes/videos/detail/Component.tsx)
  displays the persisted discriminator. Conversely,
  [VideoList.shouldShowMainUrl](../../frontend/back-office-spa/src/components/VideoList.tsx)
  already hides the aggregate URL exactly when reference count is one.
- [YouTubeContent](../../MorWalPizVideo.Models/Models/YouTubeContent.cs)
  persists `ContentType`; `IsLink` and `ToDisplayItem` currently read it.
  Append/remove methods change references without changing type.
  `CreateSingleVideo` creates one root reference, while `CreateCollection`
  supports empty content. `FromDisplayItem` uses the separate display enum.
- [ContractUtils](../../MorWalPiz.Contracts/ContractUtils.cs) copies the stored
  type into admin/public DTOs and maps `IsLink` into the public DTO.
  [V1VideosController.ToResponse](../../MorWalPizVideo.ServerAPI/Controllers/V1VideosController.cs)
  exposes its string name. [ShootingItaController.Matches](../../MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs)
  directly serializes content entities, rather than these DTOs. All three
  response paths must be covered; changing only ContractUtils is insufficient.
- [ExternalDataService](../../MorWalPizVideo.MvcHelpers/Services/ExternalDataService.cs)
  uses `IsLink` to choose fetch/refresh behavior. Its single branch fetches the
  thumbnail ID and replaces the complete reference array with one reference.
  Its collection branch refreshes all references and retains unavailable ones.
- [Public CalendarEventsController](../../MorWalPizVideo.ServerAPI/Controllers/CalendarEventsController.cs)
  chooses `ContentId` versus `Url` using the stored type. The
  [public calendar UI](../../frontend/morwalpizvideo.client/src/routes/calendar/Component.tsx)
  guesses a YouTube destination from `matchUrl === matchId`. The aggregate
  database ID, content ID, and sole YouTube reference need not coincide.
- [ShortLinkController](../../MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs)
  uses stored type for `/last` thumbnail-versus-reference selection and a
  canonical-link fallback. Canonical lookup already requires a matching target
  reference and configured-channel scope.
- [YouTubeContentRepository](../../MorWalPizVideo.Domain/Interfaces/Repository.cs)
  atomically appends/removes references and recomputes `latestPublishedAt`.
  Mutable metadata edits do not replace references. The indexed cache is
  refreshed through [ContentService](../../MorWalPizVideo.Domain/FeatureServices.cs).
  Preserve these primitives; no persisted derived-type field is needed.
- [VideosController.InvalidateVideoCachesAsync](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs)
  records cache degradation and refreshes matches/short links, but not calendar
  enrichment. Calendar has both memory and output-cache entries tagged with
  [CacheKeys.CalendarEvents](../../MorWalPizVideo.Models/Constraints/CacheKeys.cs).
- [PrimaryScenario](../../MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs)
  intentionally supplies a collection discriminator with one reference and a
  non-video content ID. It is useful disagreement/identity evidence, not a
  fixture to rewrite just to make old expectations pass.
- [Workspace manifest](../../frontend/package.json) and owning project manifests
  confirm Yarn Classic shared-package order, .NET 10 hosts, and separate
  BackOffice/ServerAPI and ShortLinks test projects.

Owning documentation:
[domain model](../architecture/domain-model.md),
[frontend](../architecture/frontend.md),
[backend](../architecture/backend.md),
[calendar compatibility](../architecture/calendar-compatibility.md),
[canonical links ADR](../architecture/adr/ADR-004-canonical-short-links.md),
and [multi-video import](multi-video-import-handoff.md).

## Delivered Design

### One authoritative backend classification

Add one computed, non-persisted, non-serialized `EffectiveContentType` on
`YouTubeContent`, using the reference-count matrix. Existing `IsLink` becomes
the compatibility boolean for this computed classification, not the stored
enum. Keep the legacy persisted `ContentType` member and factory/constructor
signatures to avoid a BSON or source-contract migration.

No repository mutation writes or reconciles a discriminator. A reloaded
aggregate immediately derives its current classification, including after
append, removal, replacement, or failed-append compensation.

Wire projections use the effective value:

- Admin/public ContractUtils conversions preserve numeric enum values:
  SingleVideo is 0; Collection is 1. Public `isLink` means exactly one reference.
- v1 preserves existing string values `SingleVideo` and `Collection`.
- Shooting ITA preserves its existing response envelope and entity-shaped field
  set, using a response-only copy with effective `ContentType`. Never persist
  that copy or introduce a broad DTO/privacy redesign in this task.
- The computed helper is excluded from BSON and JSON; it is not a new API field.
- `ToDisplayItem` classifies from references and uses the sole reference's
  YouTube ID for single-video identity, while preserving the thumbnail choice.
  Empty content stays an empty playlist display item.
- `FromDisplayItem` may retain the explicit display discriminator as input
  because display single-video items have empty `VideoIds` and a `PrimaryVideoId`.
  Once converted, aggregate classification comes exclusively from `VideoRefs`.

### Administrative acknowledgement

Remove the select completely, without replacing it with a hidden type input.
Show a read-only "Content" acknowledgement with classification and count on
detail and beside the edit reference manager. In edit, derive it from the
existing local `videoRefs` state, which is updated after successful append and
loader revalidation, not from a submitted draft ID.

Reuse/refine the existing count rule alongside `shouldShowMainUrl` in
VideoList for these app-local consumers. Do not add a shared layout component
or new state store. The list URL rule remains unchanged; add its missing
zero-reference regression case. No separate type column is required.

Failed/pending attachment does not change the count or acknowledgement.
Successful attachment from 0 to 1 changes Empty Collection to Single Video;
from 1 to 2 changes Single Video to Collection.

Impeccable implementation lens: Operate-mode refinement, not redesign.
The narrow applicable workflow is `clarify`, preserving React Bootstrap
spacing, existing badge colors, reference table, notifications, and navigation.
No app-local PRODUCT.md or DESIGN.md was found; incumbent source is the visual
authority. Do not require new design-system artifacts for this bounded change.
Required visual evidence: batched desktop/mobile captures of edit and detail
for 0/1/2 references, keyboard navigation, readable text independent of color,
and no empty layout row left by the removed field. Implementation used the
incumbent-source clarification lens and mechanical detector; browser captures
remain deferred as recorded below.

### Metadata refresh without attachment or editorial data loss

Refresh IDs come from attached references for every classification, never from
the thumbnail. Preserve array length, order, categories, channel IDs, and
creation timestamps; missing provider results retain their original reference.
Use the same reference refresh path for fetch-time enrichment and forced refresh.
Zero references require no provider request and no synthetic root reference.

Presentation classification must not confer provider ownership of editorial
metadata. Preserve the existing root-import aggregate refresh behavior only
when the aggregate has exactly one reference and its `ContentId` equals that
reference's YouTube ID, the identity established by `CreateSingleVideo`.
Refresh title/description and generate a slug only under that condition.
Otherwise preserve aggregate title, description, URL, and thumbnail while
refreshing reference metadata. Preserve existing collection date aggregation
and root publication-date behavior; do not change ordering policy.

This distinction is identity-based metadata ownership, not another stored
video/collection discriminator. It prevents a legacy one-reference editorial
collection from losing its custom text merely because it is now presented
as a single video. Do not expand this task into a metadata-provenance schema.

### Calendar and short-link identity

Calendar enrichment uses the computed classification. For one reference,
`matchUrl` contains the sole reference's YouTube ID; otherwise use the existing
content URL. Do not overwrite persisted calendar fields.

Add nullable `matchVideoId` to the public Calendar response only: the sole
reference ID for a visible single-video aggregate, otherwise null. It is a
response projection, not a Mongo field or writable calendar DTO member.
The public calendar UI uses this explicit ID for its existing YouTube action
and uses `matchUrl` for internal content navigation otherwise. Distinguish an
absent field from explicit null during rolling deployment; absent-field
responses temporarily retain the current legacy rendering rule. With the
field present, never infer link kind from equality of unrelated IDs.
New server responses must include the field even when null; verify this through
the host serializer, not just a DTO unit test. Current ServerAPI controller JSON
registration does not configure null omission.

This small additive field is required because changing server classification
alone would route a one-reference collection's YouTube ID to an internal
`/matches` URL. Keep existing copy/icons and do not add new calendar workflows.

For `/last`, choose the reference with greatest `CreationDateTime`, retaining
current tie ordering and aggregate selection/channel scope. For a single
reference this is naturally the sole attached video. Preserve query strings,
mobile schemes, analytics, and existing no-video failure responses.
Canonical YouTube links must resolve their stored target in `VideoRefs`;
remove the thumbnail/type fallback rather than inventing an unattached target.

### Cache coherence

Extend the existing video cache invalidation boundary to reset and purge
`CacheKeys.CalendarEvents` as well as matches after persisted video changes
that can affect classification or calendar URL. Include append, compensation,
legacy reference replacement, URL edit, refresh, and delete. Reuse the existing
cache operation reporting and lowercase constants; no new cache infrastructure,
client, authentication, retries, or success-shaped fallback.

At activation, invalidate relevant existing memory/indexed/output-cache state
so old payloads do not outlive the new projections.

## Impacted Projects

| Project or surface | Impact |
|---|---|
| MorWalPizVideo.Models | Computed classification, compatibility boolean, display conversion; legacy BSON retained. |
| MorWalPiz.Contracts | Effective admin/public projections; additive public calendar video ID. |
| MorWalPizVideo.MvcHelpers | Safe reference-driven fetch/refresh; preserve editorial metadata. |
| MorWalPizVideo.BackOffice | Cache fan-out and mutation verification; request DTO/permissions stay unchanged. |
| MorWalPizVideo.ServerAPI | Calendar, v1, and Shooting ITA projections; existing match DTO endpoints verified. |
| MorWalPizVideo.ShortLinks | Reference-based `/last` and canonical target resolution. |
| MorWalPizVideo.Domain | Verify-only repository/mock/indexed-cache ownership; no new storage or DI contract. |
| MorWalPizVideo.BackOffice.Tests | Model, serializer, refresh, mixed API, mutation, tenancy, and cache regressions. |
| MorWalPizVideo.ShortLinks.Tests | Redirect identity/cardinality regressions. |
| frontend/back-office-spa | Remove editable type; read-only count acknowledgement and tests. |
| @morwalpizvideo/models | Document computed wire semantics; additive optional calendar response field. |
| frontend/morwalpizvideo.client | Explicit calendar link kind and component tests; reference rendering unchanged. |
| frontend/shooting-ita-frontend | Verify changed classification responses; no UI rewrite. |
| @morwalpizvideo/services, @morwalpiz/layout | Verify/rebuild shared-model consumers; transport and layout unchanged. |
| frontend/morwalpiz-shop.client | Frozen verify-only compatibility; no shop feature work. |
| frontend/ask.client, frontend/FreezerAnalysis.spa | Services consumers only; no feature changes. |
| MorWalPiz.VideoImporter, MorWalPiz.InsightScanner | Contracts dependents, verify-only compilation/response compatibility on Windows. No SQLite/UI change. |
| MorWalPizVideo.AppHost, ServiceDefaults, FreezerAnalysis.Api | Shared-library dependent compilation only; no topology/config changes. |
| Shooting Range, shoot-recorder, TelePrompter, stage-designer, YouTubeUtilities | No classification behavior identified; no source changes. |
| BackOffice, ServerAPI, ShortLinks and both affected SPA deployment workflows | Coordinate compatible artifacts and cache activation; no workflow edits expected. |

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/YouTubeContent.cs`: computed rule, IsLink,
  ToDisplayItem, and documentation of legacy storage.
- `MorWalPiz.Contracts/ContractUtils.cs`: effective type projection.
- `MorWalPiz.Contracts/Contracts/CalendarRequests.cs`: additive public response ID.
- `MorWalPizVideo.MvcHelpers/Services/ExternalDataService.cs`: reference refresh
  and aggregate metadata ownership boundaries.
- `MorWalPizVideo.ServerAPI/Controllers/CalendarEventsController.cs`: enrichment.
- `MorWalPizVideo.ServerAPI/Controllers/V1VideosController.cs`: string projection.
- `MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs`: response-only
  effective type while preserving its current shape.
- `MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs`: reference selection.
- `MorWalPizVideo.BackOffice/Controllers/VideosController.cs`: related calendar
  cache invalidation across existing mutation paths.
- `frontend/back-office-spa/src/routes/videos/edit/Component.tsx`: remove select;
  acknowledgement derived from existing state.
- `frontend/back-office-spa/src/routes/videos/detail/Component.tsx`: computed
  acknowledgement, not raw type badge.
- `frontend/back-office-spa/src/components/VideoList.tsx`: owning app-local count
  helper reuse; preserve current URL and publishing rules.
- `frontend/fe-packages/models/src/video/types.ts`: computed compatibility semantics.
- `frontend/fe-packages/models/src/CalendarEvent.ts`: optional response field.
- `frontend/morwalpizvideo.client/src/routes/calendar/Component.tsx`: explicit
  YouTube destination and absent-field compatibility.
- `MorWalPizVideo.BackOffice.Tests/Features/AdminEditWorkflowTests.cs`:
  disagreement/cardinality, mutation, and cache evidence.
- `MorWalPizVideo.BackOffice.Tests/Features/PublicCalendarControllerTests.cs`:
  explicit identity and additive projection.
- `MorWalPizVideo.BackOffice.Tests/Features/CalendarSerializationTests.cs`:
  public response shape, preserving persisted/admin boundaries.
- `MorWalPizVideo.ShortLinks.Tests/Features/ShortLinksMockScenarioTests.cs`:
  `/last`, reference target, and thumbnail mismatch.
- `frontend/back-office-spa/src/routes/videos/edit/__tests__/Component.test.tsx`,
  `edit/__tests__/action.test.ts`, `detail/Component.test.tsx`, and
  `src/components/VideoList.test.tsx`: absent selector/payload, count, transitions,
  failures, and unchanged URL behavior.

### Add

- Focused model/BSON/projection tests under
  `MorWalPizVideo.BackOffice.Tests/Features/`, following existing naming patterns.
- Focused ExternalDataService tests under the owning backend test project's
  established Features or Services directory, with a recording YouTube fake.
  No existing FetchMatches/RefreshMatch test coverage was found by symbol search.
- A public calendar component test beside its owning route.
- No production helper file, service, repository, configuration, or package needed.

### Generated/Migration

None required. Standard build output is generated by existing tools, not edited.
No Mongo rewrite, EF migration, index, lockfile, or configuration migration.

### Documentation

- This handoff: update with actual decisions, changed files, validation, residual
  risks, and approval after implementation.
- `docs/architecture/domain-model.md`: reference cardinality versus legacy type.
- `docs/architecture/calendar-compatibility.md`: additive response identity and
  coordinated rollout; revise the current exact-field-set statement.
- `docs/architecture/frontend.md`: acknowledgement and calendar behavior.
- `docs/architecture/adr/ADR-004-canonical-short-links.md`: clarify attached-reference
  selection without changing canonical ownership.
- `docs/feature-handoffs/multi-video-import-handoff.md`: keep implemented reference
  attachment/classification behavior aligned without rewriting historical evidence.

### Verify Only

- Existing admin/public YouTubeContent contracts and
  `MorWalPiz.Contracts/DTOs/VideoUpdateRequest.cs`: preserve compatibility.
- `MorWalPizVideo.Models/Models/VideoDisplayItem.cs`: separate display enum/factories.
- `MorWalPizVideo.Domain/Interfaces/Repository.cs`, `Interfaces/MockRepository.cs`,
  `FeatureServices.cs`, `Scenarios/PrimaryScenario.cs`: atomic writes/cache/fakes.
- `MorWalPizVideo.BackOffice/Services/CrossApiService.cs`,
  `MorWalPizVideo.ServerAPI/Controllers/CacheController.cs`, and
  `MorWalPizVideo.Models/Constraints/CacheKeys.cs`: reuse existing authenticated
  reset/purge and tags.
- `MorWalPizVideo.ServerAPI/Controllers/MatchesController.cs`,
  `MorWalPiz.Contracts/DTOs/V1VideoShortLinkDtos.cs`: preserve routes/envelopes.
- Backend `VideoPermissionAuthorizationTests.cs`,
  `ChannelTenancyAuthorizationTests.cs`, `V1ShortLinkAndMigrationTests.cs`,
  `PublicMatchOrderingTests.cs`, `Services/ContentServiceTests.cs`,
  `Services/YouTubeContentIndexedCacheTests.cs`, and
  `Infrastructure/CrossApiServiceContractTests.cs`: regression gates.
- Both affected route loaders and the edit action: unchanged fetch/payload ownership.
- Public matches route and Shooting ITA video service/category tests: preserve
  reference rendering/order and category derivation.
- Owning project/package manifests, shared model barrel, and existing
  `deploy-backoffice-api.yml`, `deploy-serverapi.yml`, `deploy-shortlink.yml`,
  `deploy-back-office-spa.yml`, `deploy-morwalpizvideo-client.yml`, and `ci.yml`
  under `.github/workflows/`: verify packaging and validation selection only.

## Impact Matrix

| Area | Decision |
|---|---|
| Backend/API | All maintained type projections and behavior use reference cardinality. |
| Frontend/UI | Read-only acknowledgement; explicit calendar video identity; preserve incumbent styling. |
| Database/Data | Legacy BSON type retained, non-authoritative; no automatic cleanup or backfill. |
| Contracts/Models | Existing numeric/string encodings retained; public calendar adds nullable response-only ID. |
| Authentication/Authorization | Unchanged cookie/CSRF/RBAC/channel/private-content boundaries; count never grants access. |
| Validation | Existing append/category/channel checks retained; do not synthesize references from thumbnail. |
| Cache | Add calendar reset/purge at relevant video mutation boundary; preserve degradation reporting. |
| Configuration/Secrets | Not applicable; no new values, clients, keys, or flags. |
| Logging/Telemetry | Reuse existing cache/redirect logs; add no identity-rich telemetry or logging framework. |
| Background Jobs | Not applicable; no new scheduling. Existing refresh callers inherit corrected semantics. |
| Deployment/Operations | Coordinate three hosts, two SPA artifacts, and cache reset; no topology changes. |
| Backward Compatibility | Type field shapes remain; values intentionally change for disagreement documents. Additive calendar field has an absent-field window. |

## Reuse Opportunities

- Existing YouTubeContent/IsLink boundary and enum values; one computed rule.
- Existing app-local `shouldShowMainUrl` count semantics and loader/fetcher state.
- ContractUtils plus existing v1 and Shooting ITA response ownership.
- Atomic repository append/remove and mutable-field update; mock scenario parity.
- Existing reference metadata retention behavior in collection refresh.
- Canonical target lookup, configured-channel checks, and redirect schemes.
- ICrossApiService, cache tags, indexed-cache drain, and cache degradation reporting.
- Existing WebApplicationFactory, test authentication, isolated scenarios,
  recording cache service, Vitest/Testing Library, and route-aware render helpers.

## Risks

| Rank | Cause / affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Refresh currently branches on a stale discriminator and can truncate attached refs. | Fetch/map all attached IDs; preserve missing provider results and reference order. | Multi-reference stored-SingleVideo refresh retains every ID/category/channel/timestamp. |
| High | One-reference editorial content becomes single and might have text replaced. | Separate reference-derived presentation from identity-based root metadata ownership. | Non-video ContentId with one ref retains title/description/URL/thumbnail after enrichment. |
| High | Calendar guesses video identity from unrelated IDs. | Sole-reference ID plus additive explicit projection and UI support. | Distinct database/content/thumbnail/reference IDs produce the correct YouTube href. |
| Medium | Only converters change, leaving v1 or raw Shooting ITA responses stale. | Cover every projection and exclude the new computed helper from serialization. | All response variants agree while exact legacy field shapes remain otherwise unchanged. |
| Medium | Cached calendar destinations outlive reference changes or rollback. | Reset memory and purge calendar output tag on relevant video mutations and activation. | 1-to-2 and compensation transitions change calendar links without waiting for expiry. |
| Medium | Old host replicas and external clients assume stored type semantics. | Coordinated artifacts, short refresh pause, compatibility aliases, and rollback limits. | Artifact versions and post-deployment 0/1/2 smoke cases recorded. |
| Medium | Refactor loosens canonical target or private/channel validation. | Keep original lookup authorization; no thumbnail fallback. | Foreign channel, removed target, and private calendar content remain non-disclosing. |
| Low | Incomplete provider results or empty content create fabricated playable IDs. | Retain original refs; zero means empty; preserve explicit failure responses. | No provider calls for zero refs; unattached targets never redirect. |

## Technical Debt

- Stored aggregate type becomes explicitly archival and can disagree with
  effective wire values. Isolate now; physical BSON/DTO removal is deferred.
- `IsLink` is a legacy name for single-reference classification. Preserve it
  for compatibility; renaming/removing it is not required.
- Shooting ITA returns entity-shaped data. Contain this change in its response
  projection; broad contract/privacy convergence is separate work.
- Calendar's equality-based URL guessing blocks safe classification migration
  and must be corrected here, not deferred.
- Metadata provenance is implicit in aggregate identity. Use the factory-owned
  identity rule here; broader provenance/edit protection is separate scope.
- Existing full-replacement refresh concurrency and inconsistent cache call
  paths are debt. Do not weaken atomic edit/append paths or redesign concurrency.
  Narrowly ensure every affected mutation evicts calendar enrichment.

## Implementation Roadmap

1. **Establish discriminating regression cases.** Backend tests and the two SPA
   route suites cover 0/1/2 refs, stored disagreement, distinct identities,
   refresh preservation, and absent edit tracking. Record existing behavior
   separately from deliberate new semantics. Gate: failures discriminate the
   requested change rather than merely testing the legacy enum.
2. **Introduce the model rule and compatible projections.** Models then
   Contracts; update IsLink/display conversion, converters, v1, and raw Shooting
   ITA response handling. Keep stored type untouched. Gate: BSON round-trip
   compatibility, exact enum encodings, no extra helper field in JSON, and
   correct values on all response surfaces.
3. **Adapt metadata refresh and redirects.** MvcHelpers and ShortLinks consume
   actual references, preserve unavailable metadata/editorial content, and
   eliminate unattached thumbnail targets. Gate: refresh never changes reference
   count/order, and canonical and `/last` destinations match actual references.
4. **Complete calendar identity and cache coherence.** Contracts response,
   ServerAPI enrichment, shared calendar model, and video cache fan-out.
   Gate: distinct IDs, private/missing content, mutation/compensation transitions,
   and explicit logged cache degradation work without persisted calendar changes.
5. **Remove editable type and show derived acknowledgement.** BackOffice edit,
   detail, existing helper/test owners; public calendar uses the additive ID.
   Gate: no `matchType` input/payload, no count change before append success,
   correct 0/1/2 acknowledgement, and correct public calendar destinations.
6. **Verify shared consumers and delivery.** Build shared frontend packages in
   models/services/layout order before affected apps; run focused backend and
   frontend gates, then owning type/build checks and deployment-related filters.
   Verify .NET shared dependents and Windows Contracts consumers with existing
   platform tooling. Gate: no unrelated cleanup or weakened validation flags.
7. **Record proof and activate compatible artifacts.** Update listed owning docs
   and this handoff; capture browser and smoke evidence, artifact versions, cache
   invalidation, and rollback limits. Stop at the stated boundary. Physical type
   deletion, new concurrency mechanisms, provenance schema, and redesign need
   separate user approval.

Steps 2 through 5 depend on the baseline cases. Metadata refresh and redirects
depend on the model rule; calendar UI depends on the additive response contract.
Final validation/activation depends on all implementation modules.

## Testing Strategy

- **Unit/model:** exact 0/1/2/many matrix; null legacy array; stored type
  disagreement in both directions; copy-based add/remove transitions;
  IsLink consistency; display conversion and display-to-aggregate round-trip.
- **Serialization/contract:** legacy BSON missing/present enum round-trip;
  archival value preserved after unrelated persistence; effective helper absent
  from BSON/JSON; numeric admin/public and string v1 values; raw Shooting ITA
  envelope and fields; public calendar additive ID, no revision leak, unchanged
  admin/write DTO and calendar BSON.
- **Integration/behavior:** BackOffice.Tests existing fixtures cover standard
  single import, append, full reference replacement, delete, failed append
  compensation, privacy, channel scope, permissions, indexed-cache updates,
  and calendar reset/purge. Expected classification uses reference count, not
  the same legacy property under test.
- **Provider/refresh:** recording fake verifies requested IDs, provider omission,
  root/thumbnail mismatch, metadata-empty bulk fetch, preserved editorial text,
  retained categories/channel ownership/timestamps, unchanged order, and
  zero-reference no-call behavior. Do not use the production YouTube service.
- **Redirect/security regression:** ShortLinks.Tests covers canonical sole and
  multi-reference targets, `/last`, differing thumbnail ID, empty refs, removed
  target, wrong channel, desktop/mobile URLs, queries, and unchanged click/event
  behavior. This is scoped regression testing, not a new security audit.
- **Component/action:** edit has no type control or type payload; 0-to-1 and
  1-to-2 acknowledgements update on success/revalidation only; errors retain the
  prior count; detail ignores stale wire type; list URL behavior includes zero.
  Public calendar tests distinguish absent field, explicit null, and sole ID,
  preserving internal links and existing presentation.
- **Migration compatibility:** new clients accept old payloads; old field names/
  encodings remain readable; no silent reference repair; preserved archival
  discriminator does not influence new behavior.
- **End-to-end/visual:** verify one imported root plus added reference, then
  refresh and calendar/short-link navigation; smoke a one-reference editorial
  collection and empty content. Capture desktop/mobile edit/detail and relevant
  calendar actions; check keyboard focus and text alternatives to badge color.
- **Execution reporting:** implementation follows the existing
  [test runbook](../architecture/testing.md), records selected suites/results and
  skips, and runs no overlapping .NET builds in the same checkout. Local results
  and remaining platform/visual checks are recorded below.

## Migration And Rollback

No data migration is required: references already exist and become authoritative
at read time. Keep BSON discriminator and factory signatures, and do not
backfill type, references, identities, URLs, or indexes. Missing reference data
is not repaired from thumbnails. No SQLite, auth, secrets, or DI migration.

Retain all existing video DTO aliases for this release. The only new response
member is nullable public `matchVideoId`; introduce compatible public-client
support first, then ServerAPI. A missing field allows old-response behavior
during the window, while explicit null means no single-video YouTube action.
BackOffice UI removal can ship before backend classification because it removes
an ineffective control, but do not declare completion until all hosts agree.

Coordinate BackOffice, ServerAPI, and ShortLinks shared-library versions; pause
refresh briefly during activation rather than running old and new refresh
implementations together. Invalidate matches, calendar enrichment, and relevant
indexed/output state after the new artifacts are active. Use existing delivery
workflows and monitored errors/cache status, not a new feature flag.

Rollback can restore old artifacts because archival BSON type remains. It
restores old semantics, not necessarily reference-safe behavior: a stored
SingleVideo aggregate with several refs can again be truncated by old refresh.
Pause metadata refresh if rolling back into that condition. New frontend can
remain on old API through its absent-field window; purge incompatible cached
calendar responses when rolling back ServerAPI.

Before activation record artifact versions and take the normal operator-owned
backup/recovery evidence for affected content. The planned implementation does
not automatically rewrite editorial text, but rollback cannot undo provider
metadata already refreshed; recover any unexpected data loss from that backup.
Physical type cleanup and a release dropping aliases would narrow rollback and
are explicitly outside this handoff.

## Open Questions

None blocking the delivered design. The explicit decisions remain:
zero refs remain an empty collection; stored/wire aliases are retained for
compatibility; and presentation cardinality does not transfer ownership of
editorial metadata to YouTube.

Deployed artifact versions, external consumer inventory, and backup/activation
evidence remain operationally unverified. They are release gates, not grounds
to invent a breaking contract or automatic data migration.

## Delivery Evidence

### Actual Implementation

The approved reference-count matrix is implemented. Persisted `contentType`
and existing factory/constructor contracts remain unchanged; effective values
are computed for runtime decisions and API responses. No dependency, lockfile,
configuration, database, authentication, or deployment migration was added.

Production changes and their owning boundaries:

- [YouTubeContent.cs](../../MorWalPizVideo.Models/Models/YouTubeContent.cs):
  effective classification, compatibility IsLink, and reference-based display
  identity. The derived property is excluded from BSON/JSON.
- [ContractUtils.cs](../../MorWalPiz.Contracts/ContractUtils.cs),
  [V1VideosController.cs](../../MorWalPizVideo.ServerAPI/Controllers/V1VideosController.cs),
  and [ShootingItaController.cs](../../MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs):
  numeric, string, and raw entity-shaped response projections agree without
  persisting the response-only Shooting ITA copy.
- [CalendarRequests.cs](../../MorWalPiz.Contracts/Contracts/CalendarRequests.cs)
  and [CalendarEventsController.cs](../../MorWalPizVideo.ServerAPI/Controllers/CalendarEventsController.cs):
  additive response-only `matchVideoId`, sole-reference URL enrichment, and
  unchanged calendar persistence/revision/private-content boundaries.
- [ExternalDataService.cs](../../MorWalPizVideo.MvcHelpers/Services/ExternalDataService.cs):
  one attachment-mapping path for fetch/refresh; unavailable references,
  categories, channel IDs, order, and creation timestamps retained. Single
  import identity alone permits provider-owned aggregate metadata refresh.
- [ShortLinkController.cs](../../MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs):
  latest attached-reference `/last` selection and no unattached thumbnail
  fallback for canonical links. Existing device/query/accounting behavior remains.
- [VideosController.cs](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs):
  calendar reset/purge on reference/URL edits, refresh, delete, assignment,
  and append/compensation paths. Cache degradation remains explicit.
  Failed compensation also invalidates because the appended reference remains
  persisted; the endpoint still returns its existing error.
- [VideoList.tsx](../../frontend/back-office-spa/src/components/VideoList.tsx),
  [edit Component.tsx](../../frontend/back-office-spa/src/routes/videos/edit/Component.tsx),
  and [detail Component.tsx](../../frontend/back-office-spa/src/routes/videos/detail/Component.tsx):
  existing app-local count rule reused; editable selector removed; read-only
  acknowledgement with count and an edit live region. No new store/component.
- [video/types.ts](../../frontend/fe-packages/models/src/video/types.ts) and
  [CalendarEvent.ts](../../frontend/fe-packages/models/src/CalendarEvent.ts):
  compatibility semantics documented and optional public calendar video ID.
- [public calendar Component.tsx](../../frontend/morwalpizvideo.client/src/routes/calendar/Component.tsx):
  explicit video identity with absent-field-only legacy fallback.

New focused tests:
[VideoReferenceClassificationTests](../../MorWalPizVideo.BackOffice.Tests/Features/VideoReferenceClassificationTests.cs),
[ExternalDataServiceTests](../../MorWalPizVideo.BackOffice.Tests/Services/ExternalDataServiceTests.cs),
[PublicVideoReferenceClassificationTests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicVideoReferenceClassificationTests.cs),
and [public calendar component tests](../../frontend/morwalpizvideo.client/src/routes/calendar/Component.test.tsx).
Updated owners are AdminEditWorkflowTests, CalendarSerializationTests,
PublicCalendarControllerTests, ShortLinksMockScenarioTests, VideoList tests,
edit component/action tests, and detail component tests listed above.
[RecordingCrossApiService](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/RecordingCrossApiService.cs)
adds a test-only per-key failure control to prove calendar degradation.
The action fixture uses URLSearchParams to avoid a Node/jsdom FormData mismatch;
production action/request behavior did not change.

Tests prove 0/1/2/many cardinalities and stale discriminator disagreement;
archival BSON preservation; exact response encodings; no extra helper field;
actual video versus thumbnail/content/database identity; successful and failed
attachment acknowledgements; editorial metadata preservation; provider failure
propagation; compensation success/failure; private/foreign/removed-target
boundaries; calendar cache fan-out; and unchanged reference ordering/accounting.

Architecture/domain/frontend/calendar documentation, ADR-004, and the
multi-video import handoff were updated to the implemented behavior. No
independent reviewer or sub-agent was invoked.

### Local Validation

Commands ran from the repository root unless `--cwd` specifies an application.
All final relevant checks passed, with no tests skipped:

- BackOffice/mixed API test project: 53 tests passed for
  `VideoReferenceClassificationTests`, `ExternalDataServiceTests`,
  `PublicVideoReferenceClassificationTests`, `PublicCalendarControllerTests`,
  `CalendarSerializationTests`, and `AdminEditWorkflowTests`.
- Same project compatibility selection: 61 tests passed for
  `PublicVideoReferenceClassificationTests`, `VideoPermissionAuthorizationTests`,
  `ChannelTenancyAuthorizationTests`, `PublicMatchOrderingTests`,
  `ContentServiceTests`, `YouTubeContentIndexedCacheTests`,
  `V1ShortLinkAndMigrationTests`, and `CrossApiServiceContractTests`.
  Six API cases overlap the first selection: 108 distinct tests in this project.
- ShortLinks test project: 16 tests passed for ShortLinksMockScenarioTests and
  ShortLinkRequestClassifierTests. All modified .NET production projects were
  compiled through these test projects.
- Administrative four-suite selection passed 27 tests, then the final edit
  suite passed all 11 including the added second-attachment case: 28 distinct
  admin tests covered.
- Public calendar/home/match-loader selection: 11 tests passed.
- Shooting ITA video-service/category selection: 7 tests passed.
- Models and services TypeScript builds passed through the admin pretest;
  shared layout TypeScript/styles build passed afterward in dependency order.
- Strict `tsconfig.app.json` checks passed for BackOffice SPA, public client,
  and Shooting ITA. No compiler rules or validation flags were weakened.
- One Impeccable detector pass over edit, detail, and public calendar returned
  `[]`. This is mechanical evidence, not a browser screenshot/a11y audit.
- The new public calendar test was formatted once with the existing formatter.

Reproduction commands for the focused proof:

```bash
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter 'FullyQualifiedName~VideoReferenceClassificationTests|FullyQualifiedName~ExternalDataServiceTests|FullyQualifiedName~PublicVideoReferenceClassificationTests|FullyQualifiedName~PublicCalendarControllerTests|FullyQualifiedName~CalendarSerializationTests|FullyQualifiedName~AdminEditWorkflowTests' --verbosity minimal -m:1 -p:BuildInParallel=false --no-restore
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter 'FullyQualifiedName~PublicVideoReferenceClassificationTests|FullyQualifiedName~VideoPermissionAuthorizationTests|FullyQualifiedName~ChannelTenancyAuthorizationTests|FullyQualifiedName~PublicMatchOrderingTests|FullyQualifiedName~ContentServiceTests|FullyQualifiedName~YouTubeContentIndexedCacheTests|FullyQualifiedName~V1ShortLinkAndMigrationTests|FullyQualifiedName~CrossApiServiceContractTests' --verbosity minimal -m:1 -p:BuildInParallel=false --no-restore
dotnet test MorWalPizVideo.ShortLinks.Tests/MorWalPizVideo.ShortLinks.Tests.csproj --filter 'FullyQualifiedName~ShortLinksMockScenarioTests|FullyQualifiedName~ShortLinkRequestClassifierTests' --verbosity minimal -m:1 -p:BuildInParallel=false
NODE_OPTIONS=--no-experimental-webstorage yarn --cwd frontend/back-office-spa test src/components/VideoList.test.tsx src/routes/videos/edit/__tests__/Component.test.tsx src/routes/videos/edit/__tests__/action.test.ts src/routes/videos/detail/Component.test.tsx --reporter=default
NODE_OPTIONS=--no-experimental-webstorage yarn --cwd frontend/back-office-spa vitest run src/routes/videos/edit/__tests__/Component.test.tsx --reporter=default
yarn --cwd frontend/morwalpizvideo.client test src/routes/calendar/Component.test.tsx src/routes/home/Component.test.tsx src/routes/matches/loader.test.ts
NODE_OPTIONS=--no-experimental-webstorage yarn --cwd frontend/shooting-ita-frontend test src/__tests__/shootingItaVideoService.test.ts src/__tests__/deriveCategories.test.ts
yarn --cwd frontend workspace @morwalpiz/layout build
yarn --cwd frontend/back-office-spa tsc --project tsconfig.app.json --noEmit
yarn --cwd frontend/morwalpizvideo.client tsc --project tsconfig.app.json --noEmit
yarn --cwd frontend/shooting-ita-frontend tsc --project tsconfig.app.json --noEmit
```

The installed Node 26 runtime exposes an unusable localStorage without a file,
unlike the repository's documented Node 20/22 CI runtimes. Admin and Shooting
ITA tests initially failed during transport import; disabling Node's experimental
web storage for those commands restores jsdom storage and the suites pass.
No transport/configuration workaround was added to production.

The .NET runs retain existing ImageSharp advisory warnings and unrelated
compiler warnings. No warning in the final backend run points to the changed
classification/refresh/controller/tests. Dependencies were not upgraded in
this feature.

Test builds regenerated six checked-in Reqnroll files with newline-only churn.
Those initially clean generated files were returned to their prior content;
no feature source/generated scenario changes belong to this implementation.
Unrelated concurrent StringUtils and public-layout/design work was preserved,
including the non-conflicting public calendar wrapper.

### Remaining Evidence And Release Gates

- Chromium/Chrome and Playwright/Puppeteer are unavailable here. Desktop/mobile
  screenshots, visual keyboard/focus checks, and real-browser navigation remain
  unverified. Component and SSR markup tests do not replace that evidence.
- No Mongo migration or index change is needed. Real-database mutation/cache
  smoke evidence and deployed public destinations remain operator release gates.
- Windows Contracts consumers were not built/run on this Linux environment.
  Their retained enums, constructors, and existing field shapes are unchanged;
  complete the existing Windows pipeline before release.
- Full solution/workspace builds, broad lint/format checks, and full test suites
  are deferred to the downstream automatic final procedure under the validation
  budget. They are not represented as already passed.
- Public-client-first additive response support, coordinated host artifacts,
  cache activation/rollback, refresh pause during rollback to unsafe old writers,
  backup evidence, and external consumer inventory remain as specified above.
  No deployment was performed and archival fields/aliases were not removed.
