# Channel editing, navigation, and response contracts

Status: Implemented in the worktree; not deployed.
This editable file records the implementation, validation, and remaining
release gates. No production database operation or deployment was performed.

## Outcome

Implemented the BackOffice channel create/edit experience and rationalized
`ChannelsController` responses without breaking channel tenancy, authorization,
publishing credentials, cache warnings, or existing data.

Requested outcomes:

1. Breadcrumbs display the channel's persisted `Id`, not `ChannelId`.
2. Remove the editable YouTube Studio URL; derive it from the YouTube channel ID.
3. Google Analytics survives create, save, reopen, and shell revalidation.
4. Authorized editors can change the YouTube channel ID.
5. Backend validation messages reach their corresponding inputs instead of being
   replaced by “Unable to update channel.”
6. Successful commands with no useful response data return 204. Manual input
   failures use the same Validation Problem Details shape as model validation.

Explicit assumptions and boundaries:

- “Id” means the channel document's existing `Id`, not its name. This is a
  **display-only** breadcrumb change; channel URLs continue to use `ChannelId`.
  A name-based breadcrumb is an alternative requiring user confirmation.
- Scope the response audit to `ChannelsController`, its direct consumers, and
  the narrow shared transport defect exposed by these consumers. Do not migrate
  every controller, introduce API versioning, or redesign generic forms.
- Preserve the current Analytics URL policy unless the user approves a separate
  change: absolute HTTPS, no credentials, query, or fragment.
- Changing the YouTube binding is not transferring a tenant, reassigning content,
  changing the uploader's OAuth account, or changing ownership.
- The identity split below was accepted for this implementation: the binding is
  editable while tenant identity remains stable.

The in-worktree implementation and focused automated checks are complete.
Production rollout remains gated on compatible deployed writers, external-client
inventory, and browser/Mongo verification. Wider cleanup needs separate approval.

## Current Evidence

### Entry points and data flow

The active SPA route tree in
[lazy-index.ts](../../frontend/back-office-spa/src/router/routes/lazy-index.ts)
routes global create/edit to the shared
[form](../../frontend/back-office-spa/src/routes/channels/form/) and self-service
edit through
[self/edit/action.ts](../../frontend/back-office-spa/src/routes/channels/self/edit/action.ts).
The latter resolves the selected channel and delegates to the same form action.
Older standalone create/edit modules exist, but are not the active global form
entries in this route tree.

The current write flow is:
shared form -> fetcher -> route action -> shared admin transport ->
ChannelsController -> DataService -> channel repository -> Mongo/mock.
The read flow returns either the shared channel conversion for lists or the
manual management projection for detail. Normal action revalidation refreshes
the root accessible-channel snapshot.

### Verified findings

| Report | Source evidence and conclusion |
|---|---|
| Breadcrumb shows ChannelId | [Breadcrumbs/index.tsx](../../frontend/back-office-spa/src/components/Breadcrumbs/index.tsx) falls back to the URL segment when `breadcrumbIdentifier` is unavailable. Channel loaders do not provide that identifier. Additionally, [guards.ts](../../frontend/back-office-spa/src/router/guards.ts) wraps feature loader results in deferred data, while the component only resolves deferred data specially for blog breadcrumbs. Adding a flat property alone is insufficient for the active deferred routes. |
| Studio URL is redundant | [YTChannel.cs](../../MorWalPizVideo.Models/Models/YTChannel.cs) currently persists `YouTubeStudioUrl`; [ContractUtils.Convert](../../MorWalPiz.Contracts/ContractUtils.cs) copies it. The form edits it and [AdminSidebar.tsx](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx) reads it from the selected accessible channel. |
| Analytics appears lost on edit | [ChannelsController.UpdateChannel](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs) writes `GoogleAnalyticsUrl`, but `ConvertManagement` omits both Analytics and Studio URLs when manually copying the shared contract. The inherited properties therefore remain empty on detail GET. Reopening hydrates the blank value; saving again can overwrite the stored Analytics URL with blank. This is a confirmed projection defect, not evidence that the first update failed to persist. |
| Analytics is absent on create | [form/action.ts](../../frontend/back-office-spa/src/routes/channels/form/action.ts) reads both link inputs, but its POST payload omits both. Its PUT payload includes them. This is a second, independent round-trip defect. |
| YouTube ID cannot be edited | [form/Component.tsx](../../frontend/back-office-spa/src/routes/channels/form/Component.tsx) shows/submits `yTChannelId` only in create mode and never hydrates it for edit. `UpdateChannelRequest` has no YouTube ID member. Both TypeScript update DTOs exclude it. |
| YouTube and tenant IDs are conflated | The persisted record only has `ChannelId`; [ContractUtils.Convert](../../MorWalPiz.Contracts/ContractUtils.cs) sets both response `ChannelId` and `YTChannelId` from it. [DataService](../../MorWalPizVideo.Domain/DataService.cs) gets and checks channel updates by this same value. The document `Id` is a third, separate identifier. |
| Manual validation is inconsistent | [ChannelsController](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs) returns strings for URL, social, publishing, terminology, and logo validation. Its inherited [ApplicationControllerBase](../../MorWalPizVideo.MvcHelpers/Controllers/ApplicationControllerBase.cs) has `[ApiController]`; annotation/model-binding validation is therefore a different response path. No custom model-state response factory was found in the inspected BackOffice/MVC source. |
| Field messages are discarded | [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts) preserves 400 `fieldErrors` when JSON parsing succeeds. [channels/response.ts](../../frontend/back-office-spa/src/routes/channels/response.ts) only recognizes an `errors` array and turns messages into `generics`, discarding `fieldErrors`. Its catch-path handling also ignores ordinary Error messages. |
| Legacy text errors can become generic failures | The admin 400 transport path unconditionally parses JSON. A plain-text `BadRequest(string)` response can reject during parsing, after which the channel action substitutes its fallback. Actual content negotiation/status headers for the reported live request were not captured here, so this is a source-supported mechanism, not a deployed reproduction. |
| Form feedback can remain behind a modal | The channel form's completion effect only handles a result without errors. A failed submission does not close its confirmation modal. The supplied [short-link form test](../../frontend/back-office-spa/src/routes/shortLinks/form/Component.test.tsx) already demonstrates the desired failure interaction in another feature. |
| Not every success body is redundant | Channel mutation callers inspect `cacheInvalidation` and display degraded-cache warnings. Logo upload also consumes `channel.channelLogoUrl`; terminology save replaces state with the normalized response. These payloads cannot all be discarded. |

### Identity dependencies that rule out a simple rename

- [ChannelContext.cs](../../MorWalPizVideo.BackOffice/Services/ChannelContext.cs)
  resolves scope, API-key binding, and ownership with `ChannelId`.
- [SocialPublishingConfiguration.cs](../../MorWalPizVideo.BackOffice/Services/SocialPublishingConfiguration.cs)
  binds encryption/decryption associated data to `ChannelId` and provider.
  Renaming that key without re-encryption makes existing credentials unreadable.
- [InsightIngestionService.cs](../../MorWalPizVideo.BackOffice/Services/InsightIngestionService.cs)
  passes stored `ChannelId` to YouTube in two stored-channel comment scan paths.
  Those are external-binding consumers and must change if IDs are separated.
- [ShootingItaController.cs](../../MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs)
  uses stable channel IDs for public content joins and emits the shared channel
  conversion. ServerAPI public channel filters and the
  [ShortLinks controller](../../MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs)
  use the historically named `YouTubeChannelId` configuration as a tenant key.
  Do not reinterpret that configuration as the new editable external binding.
- [VideoImporter channel selection](../../MorWalPiz.VideoImporter/Views/ChannelSelectionDialog.cs)
  consumes `ChannelContract`. Preserve its `ChannelId` semantics and API-key
  scope; no desktop settings or OAuth migration is implied.
- [BaseRepository.cs](../../MorWalPizVideo.Domain/Interfaces/BaseRepository.cs)
  replaces documents on update. Older writers can erase newly added fields.

The relevant .NET projects target .NET 10. Current manifests confirm Domain also
depends on YouTubeUtilities; do not rely on an older Models-only dependency map.
The frontend is a Yarn Classic workspace with shared models/services/layout
build ordering. These findings do not establish deployed binary versions.

## Implemented Design

### 1. Separate the editable YouTube binding from stable identity

Keep all existing document `Id` and `ChannelId` values unchanged.
Add an optional persisted `YTChannelId` binding to the existing channel model,
using the established JSON name `yTChannelId` in requests/responses and an
explicit BSON field/default compatible with legacy records.

Use one shared, non-persisted effective-ID accessor at the Models boundary:
an absent legacy external binding resolves to the existing `ChannelId`.
Do not expose an extra public “effective ID” response property or create an
identity service/framework. Verify Mongo record deserialization of missing and
null fields, not just C# object initialization.

- New channels keep today's initial `ChannelId` assignment for compatibility
  and also persist their external YouTube binding.
- Update accepts optional `yTChannelId`: omission/null preserves the current
  binding; supplied blank is a field error; a supplied value is trimmed.
- Reject URL/path/control-character input rather than treating it as a Studio
  URL. Do not impose a new exact-length rule on every legacy stored key or
  require a live YouTube request to save unrelated channel fields.
- The edit UI hydrates and submits the actual YouTube ID on both global and
  self-service edit. Existing update permissions and ownership checks govern
  the binding change; no new permission or ownership mutation is introduced.
- Update the two stored-channel YouTube scan arguments to use the effective
  external binding. Persistence, topic ownership, scope, and scan attribution
  continue to use the stable `ChannelId`.
- Retain existing video histories/cursors and content references when the binding
  changes. New scans target the new binding; there is no implicit content move,
  history purge, or OAuth-account switch. Explain this in the confirmation UI.
- Retain creation's duplicate stable-key protection. New cross-tenant uniqueness
  constraints on the editable external binding are not part of this request.
  A YouTube binding is not a permission claim or proof of ownership.

**Rejected alternative:** directly replacing `ChannelId`, or replacing channel
routes with document `Id`. That would require a coordinated tenancy migration
across owners, API keys, content, settings, push audiences, configuration,
encrypted credentials, and desktop state. It is not a bounded editor fix.

### 2. Derive Studio; repair Analytics round trips

Derive `YouTubeStudioUrl` centrally in the existing shared channel conversion
from the effective YouTube binding and the fixed
`https://studio.youtube.com/channel/` prefix. Treat the binding as one encoded
path segment. Retain the response property so the existing sidebar and older
readers do not need a simultaneous contract removal.

Remove Studio state, input, and submitted value from the active form/action.
Retain the persisted legacy field and legacy request members for the
compatibility window, but stop treating them as authoritative: new writes
preserve the stored legacy value, and reads derive the response. Ignore obsolete
Studio input rather than validating a setting the user can no longer edit.
No destructive field cleanup or backfill is required.

Copy both derived Studio and saved Analytics values into the management
projection. Forward Analytics on POST as well as PUT.
Define Analytics update semantics explicitly: omitted/null preserves existing
configuration; explicit empty/whitespace clears it; valid nonempty input is
normalized and persisted; invalid input does not mutate anything.
Do not let missing form data silently become an explicit clear.

After save, use normal root loader revalidation to refresh the selected channel,
Studio destination, and Analytics link. Do not add a second store, manual cache,
logout requirement, or imperative reload workaround.

### 3. Display the correct breadcrumb identifier

Add document `id` to both TypeScript channel read shapes as optional during
compatibility, since the current API already emits it. Do not accidentally add
a required document ID to create DTOs. Make update DTOs allow the external
YouTube binding while continuing to exclude immutable identity from edits.

Provide channel breadcrumb metadata through the route-handle/deferred-data
pattern already used for blog, with a narrow typed channel resolver.
The dynamic channel segment displays the resolved document `id`; its link
continues to navigate with the original stable channel route parameter.
Use a neutral “Channel” label while loading/on unavailable legacy ID, not a
misleading external ID. Preserve blog and existing flat-loader breadcrumb
behavior. Self-service routes have no dynamic ID segment and should not acquire
an invented channel URL.

### 4. Unify field validation and preserve it end to end

For manual **input validation** within ChannelsController, add errors to
ModelState under canonical request-property keys and return Validation Problem
Details through the existing MVC mechanism. Keep automatic `[ApiController]`
validation; do not replace the global response factory.

Map failures to the owning input:

- Channel name -> `ChannelName`; YouTube binding -> `yTChannelId`.
- Public origin -> `PublicBaseUrl`; short-link base -> `ShortLinkUrl`.
- Analytics -> `GoogleAnalyticsUrl`, preserving the existing specific message:
  “Analytics links must be absolute HTTPS URLs without credentials, query strings,
  or fragments.”
- Social rows -> the affected provider/handler path when available, otherwise
  the existing Socials section.
- Publishing replacement/clear conflict -> the affected provider credential
  path, with a section-level fallback.
- Terminology -> `ItalianToEnglish` or `InvariantEnglish`, including a row path
  when the existing validator identifies one.
- Logo input/format -> `logo`.

Do not convert missing/inaccessible resources into field errors: preserve 404
concealment, 401/403 authorization semantics, and existing terminology corruption
409 Problem Details. Unexpected infrastructure failures remain server failures,
not invalid-form errors.

At the shared transport boundary, narrowly harden the admin 400 response reader:
read the body once; handle Validation Problem Details, JSON strings, plain text,
and empty bodies; retain actual HTTP status, field map, and channel-context flags.
Use the existing response/error abstractions rather than adding a second client.
Do not change account/public transport isolation or normalize every status as
part of this feature.

In the channel response adapter, reuse `ApiResponseError`,
`requireSuccessfulResponse`, and `getApiFieldErrors` where applicable. These
already exist in the services package. Follow the
[calendar action](../../frontend/back-office-spa/src/routes/calendarEvents/form.ts)
precedent for translating request-property keys to UI fields.
Preserve the channel adapter's existing context-error flag.

Normalize casing and known field aliases explicitly, including `YTChannelId`
versus `yTChannelId`; map indexed/nested fields to their actual form controls or
owning section. Keep all messages: the existing string-valued FieldError can
receive joined messages without a repository-wide component redesign.
Unknown field keys and genuine non-field errors go into the generic summary;
never silently discard them or show the generic validation title instead of
available field messages.

Add invalid-state styling and associated descriptions/errors to affected
controls. On a completed failed submission, close the confirmation modal,
retain entered data, focus the first invalid input or summary, and show no
success toast/navigation. Use explicit `success` discrimination in route action
results rather than checking `.length` on an errors object.

### 5. Audit each success response, not every 200 indiscriminately

The implemented wire contract is endpoint-specific. Router action results may continue
to include their own local success flag; that does not require the API to emit
one.

| Endpoint | Implemented successful API response | Reason |
|---|---|---|
| Channel list, accessible list, detail GET | 200 with existing DTO(s) | Data is required. |
| Terminology GET | 200 with terminology | Data is required. |
| Terminology PUT | 200 with normalized terminology | `saveChannelTerminology` feeds the result back into the editor. |
| Terminology DELETE | 204, unchanged | Already bodyless. |
| Channel POST, PUT, DELETE | 204 when cache invalidation completes | Callers need only success on the normal path. |
| Channel POST, PUT, DELETE with failed cache invalidation after commit | 200 with the existing meaningful cache-warning information, without a redundant success flag | The mutation succeeded, but the UI must warn. Do not return an error that encourages replaying an already committed operation. |
| Logo POST | 200 with channel/logo DTO and any cache warning, without a redundant success flag | Upload consumes the resulting logo URL. |
| Logo DELETE | 204 normally; 200 with warning information when invalidation fails | Logo state is cleared locally; only a degraded-cache warning requires a body. |

Update `hasCacheInvalidationWarning` and direct logo consumers so warning
detection does not depend on the API's removed success flag. The form/index
actions explicitly construct local success after a successful transport result.
The shared transport already handles 204 without trying to parse JSON.

Keep existing lowercase centralized cache keys/tags and the entire current
reset/purge sequence. Do not drop a cache warning merely to make every response
bodyless. Prefer the existing warning body over introducing CORS-exposed warning
headers or a durable notification subsystem.

Audit logo error classification while touching its returns: the current broad
catch labels storage/update/cache exceptions as invalid PNG and echoes exception
messages. Limit validation responses to expected media-input failures. Let
unexpected failures use the host's server-error path with structured logging and
no exception text in the user-facing response. Preserve the existing upload
response shape needed for the preview; broader blob compensation is deferred.

### UI implementation lens

This is an **Operate-mode refinement**, not a redesign. Preserve React
Bootstrap, existing page layout, labels outside scope, confirmation workflow,
and channel selector. No BackOffice PRODUCT.md or DESIGN.md was found; do not
borrow another application's design artifacts or make their creation a gate.

During implementation, apply the installed Impeccable `harden` workflow narrowly
to channel create/edit error states and breadcrumb loading. No design execution
was performed by this planning handoff. Minimum visual evidence: desktop and
mobile captures of edit hydration, changed YouTube ID confirmation, and an
Analytics field error after the modal closes. Verify keyboard focus, programmatic
label/error association, loading state, long-ID wrapping, no horizontal overflow,
and readable errors without relying on color.

## Impacted Projects

| Project/surface | Impact |
|---|---|
| MorWalPizVideo.Models | Add compatible external binding and shared effective-ID behavior. |
| MorWalPiz.Contracts | Correct channel conversion; retain existing response property names. |
| MorWalPizVideo.BackOffice | Request/projection/validation/success contracts; stored-channel YouTube consumers. |
| MorWalPizVideo.Domain | Verify channel persistence and legacy compatibility; tenant-key operations remain unchanged. No general DataService extraction is required. |
| MorWalPizVideo.BackOffice.Tests | Backend contract, authorization, serialization, credential, cache, and scan regression proof. |
| frontend/fe-packages/models | Channel ID typing and editable update DTO; preserve compatibility for consumers. |
| frontend/fe-packages/services | Bounded admin 400 parsing fix and transport regressions. |
| frontend/back-office-spa | Form/action, response adapter, breadcrumbs, logo consumer, and sidebar-refresh proof. |
| MorWalPizVideo.ServerAPI | Rebuild with the changed shared conversion/model; verify Shooting ITA channel response and stable-key filtering. No endpoint redesign. |
| MorWalPizVideo.ShortLinks | Verify existing configured tenant-key semantics remain unchanged; no source change expected. |
| MorWalPiz.VideoImporter | Shared ChannelContract consumer; verify stable selection/header behavior on Windows. No SQLite or OAuth change. |
| Public video and Shooting ITA clients | Verify existing shared channel-reader compatibility and stable owner-map joins. No UI change expected. |
| Other shared-package consumers | Check workspace type compatibility where affected. Shop remains frozen/on hold; no shop feature or migration work. |
| MVC helpers, ServiceDefaults, AppHost, YouTubeUtilities, InsightScanner, Range, FreezerAnalysis, Ask, Shoot Recorder, standalone tools | No application behavior change identified; do not expand work into these surfaces. Existing shared transport isolation tests cover unaffected client modes. |
| Deployment | Updated BackOffice and ServerAPI shared-model binaries plus rebuilt admin SPA. No new host, secret, CORS rule, scheduled job, or topology change. |

## Impacted Files

Paths below are concrete owners, not permission to perform unrelated cleanup.

### Modify

- [YTChannel.cs](../../MorWalPizVideo.Models/Models/YTChannel.cs): optional external
  binding, BSON compatibility, and non-persisted effective-ID accessor.
- [ContractUtils.cs](../../MorWalPiz.Contracts/ContractUtils.cs): effective YouTube
  ID and derived Studio conversion.
- [ChannelsController.cs](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs):
  update request, complete management projection, Analytics omission semantics,
  manual validation, success-response audit, and logo error classification.
- [InsightIngestionService.cs](../../MorWalPizVideo.BackOffice/Services/InsightIngestionService.cs):
  the two stored-channel external lookup arguments only.
- [shared channel.ts](../../frontend/fe-packages/models/src/channel.ts) and
  [admin channel.ts](../../frontend/back-office-spa/src/models/channel.ts):
  optional document ID, identifier semantics, and update DTO.
- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts):
  bounded admin 400 parsing; preserve existing field/status/context envelope.
- [channel form component](../../frontend/back-office-spa/src/routes/channels/form/Component.tsx):
  editable binding, remove Studio input, field errors and submission lifecycle.
- [channel form action](../../frontend/back-office-spa/src/routes/channels/form/action.ts):
  create/update payload parity, explicit omitted/clear semantics, local success.
- [channel response adapter](../../frontend/back-office-spa/src/routes/channels/response.ts):
  preserve/map field errors and useful error messages; warning discrimination.
- [Breadcrumbs](../../frontend/back-office-spa/src/components/Breadcrumbs/index.tsx)
  and [lazy route definitions](../../frontend/back-office-spa/src/router/routes/lazy-index.ts):
  typed deferred channel breadcrumb metadata/resolution without changing routes.
- [channel detail](../../frontend/back-office-spa/src/routes/channels/detail/Component.tsx):
  compatible logo success/error consumption and useful validation feedback.
- Existing backend tests:
  [AdminEditWorkflowTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/AdminEditWorkflowTests.cs),
  [ChannelTenancyAuthorizationTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs),
  [ChannelTerminologyTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTerminologyTests.cs),
  [ShortContentInsightTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/ShortContentInsightTests.cs),
  and [SocialPublishingServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Services/SocialPublishingServiceTests.cs),
  limited to changed behavior and affected expectations.
- Existing frontend tests:
  [apiService.test.ts](../../frontend/fe-packages/services/src/apiService.test.ts),
  [channel routing tests](../../frontend/back-office-spa/src/routes/channels/__tests__/routing.test.ts),
  [form component tests](../../frontend/back-office-spa/src/routes/channels/form/Component.test.tsx),
  [detail tests](../../frontend/back-office-spa/src/routes/channels/__tests__/detail.test.tsx),
  and [AdminSidebar.test.tsx](../../frontend/back-office-spa/src/layouts/AdminSidebar.test.tsx).

### Add

- A focused breadcrumb test beside the existing
  [Breadcrumbs component](../../frontend/back-office-spa/src/components/Breadcrumbs/),
  including the real deferred wrapper. No new production module is required.
- Only if existing fixtures cannot exercise the external-binding scan:
  a focused test in the existing backend Features test directory using fake
  YouTube and isolated repositories. Do not add a new test project.

### Generated/Migration

None required. No EF migration, Mongo backfill, index, generated API client, or
hand-edited generated Reqnroll output is planned.

### Documentation

- This handoff: update with accepted decisions, real files changed, checks and
  results, rollout evidence, and residual risks.
- [domain-model.md](../architecture/domain-model.md): document the three IDs and
  tolerant legacy binding reads.
- [api-design.md](../architecture/api-design.md): channel field errors and
  endpoint-specific 204/data/warning responses.
- [backend.md](../architecture/backend.md): replace the current self-service
  “cannot change identity” wording with precise immutable-tenant versus editable
  external-binding rules.
- [frontend.md](../architecture/frontend.md) and
  [accounts-and-channels.md](../features/accounts-and-channels.md): editor,
  derived Studio, validation, and refresh delivery facts after implementation.
- Add an identity-separation ADR under the existing
  [ADR directory](../architecture/adr/) once this proposal is accepted; allocate
  the next available number at implementation time.

### Verify Only

- [ChannelContract.cs](../../MorWalPiz.Contracts/Contracts/ChannelContract.cs) and
  [ChannelManagementContract.cs](../../MorWalPizVideo.BackOffice/DTOs/ChannelManagementContract.cs):
  existing read properties already cover the planned wire shape.
- [DataService.cs](../../MorWalPizVideo.Domain/DataService.cs),
  [BaseRepository.cs](../../MorWalPizVideo.Domain/Interfaces/BaseRepository.cs),
  [ChannelContext.cs](../../MorWalPizVideo.BackOffice/Services/ChannelContext.cs),
  and [SocialPublishingConfiguration.cs](../../MorWalPizVideo.BackOffice/Services/SocialPublishingConfiguration.cs):
  stable lookup, replacement, authorization, and credential-binding invariants.
- [form loader](../../frontend/back-office-spa/src/routes/channels/form/loader.ts),
  [detail loader](../../frontend/back-office-spa/src/routes/channels/detail/loader.ts),
  [self loader](../../frontend/back-office-spa/src/routes/channels/self/loader.ts),
  and [self edit action](../../frontend/back-office-spa/src/routes/channels/self/edit/action.ts):
  reuse the existing hydrated entity and stable selected channel.
- [index action](../../frontend/back-office-spa/src/routes/channels/index/action.ts):
  already creates local success; verify 204 and warning-body compatibility.
- [AdminSidebar.tsx](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx),
  [ChannelContext.tsx](../../frontend/back-office-spa/src/contexts/ChannelContext.tsx),
  and [QuickLinks refresh test](../../frontend/back-office-spa/src/routes/quickLinks/detail/refresh.test.tsx):
  preserve root revalidation and selected-channel identity. No sidebar production
  change is expected when the API retains its derived Studio property.
- [apiTransport readiness tests](../../frontend/fe-packages/services/src/apiTransport.readiness.test.ts),
  [public account service tests](../../frontend/fe-packages/services/src/publicAccountService.test.ts),
  and [router async-data tests](../../frontend/back-office-spa/src/router/asyncData.test.tsx):
  preserve unaffected transport/deferred behavior.
- [ShootingItaController.cs](../../MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs),
  [ChannelSocialsController.cs](../../MorWalPizVideo.ServerAPI/Controllers/ChannelSocialsController.cs),
  [videoChannelMap.ts](../../frontend/fe-packages/services/src/videoChannelMap.ts),
  [VideoImporter ApiService](../../MorWalPiz.VideoImporter/Services/ApiService.cs),
  and [ChannelSelectionDialog.cs](../../MorWalPiz.VideoImporter/Views/ChannelSelectionDialog.cs):
  stable IDs and compatible shared read contracts.
- [frontend manifest](../../frontend/package.json),
  [admin manifest](../../frontend/back-office-spa/package.json), and relevant
  project manifests: current build/test selection; no dependency edits expected.

## Impact Matrix

| Area | Decision |
|---|---|
| Backend/API | Bounded ChannelsController contract correction plus two external-lookup call sites. No controller-wide service extraction. |
| Frontend/UI | Operate-mode refinement: correct identifiers, fewer inputs, actionable errors, preserved confirmation/navigation. |
| Database/Data | One optional external-binding field. Existing document/tenant IDs, references, histories, ciphertext, and legacy Studio field remain. |
| Contracts/Models | Additive persistence/read semantics; existing `yTChannelId` becomes independently editable. Studio response remains compatible and derived. |
| Authentication/Authorization | Existing route permission and ownership checks unchanged. No caller-supplied tenant reassignment or X-Channel-Id reinterpretation. |
| Validation | Model and manual input errors share Validation Problem Details; canonical field keys survive transport and UI mapping. |
| Cache | Retain current channels/matches/quicklinks/channelnews reset and purge. Preserve post-commit warning visibility. |
| Configuration/Secrets | No new values or secret handling. Historically named YouTubeChannelId configuration remains the stable tenant selector. |
| Logging/Telemetry | Existing cache warning logs remain. Unexpected logo failures are logged/classified as server failures, without raw exception text. Never log publishing credentials or request bodies. |
| Background Jobs | No new job or retry policy. Stored-channel scan paths resolve the editable external binding; do not move existing work/history to another tenant. |
| Deployment/Operations | Compatible read phase first, refreshed SPA, then command-status change. All channel writers must understand the new field before external edits. |
| Backward Compatibility | Old documents fall back to stable ID; legacy Studio field retained. Older writers and clients expecting fixed 200/success flags require rollout controls. |

## Reuse Opportunities

- Keep DataService and existing Mongo/mock channel repository ownership.
- Extend the existing shared channel converter rather than constructing Studio
  links independently in the form, sidebar, and public clients.
- Reuse ASP.NET ModelState/ValidationProblem and the existing `[ApiController]`
  model-validation pipeline.
- Reuse shared `ApiResponseError`, `requireSuccessfulResponse`,
  `getApiFieldErrors`, and the calendar form's field-key translation precedent.
- Keep FieldError and GenericErrorList local usage patterns; join multiple field
  messages rather than redesigning all form contracts.
- Reuse the blog route-handle + Suspense/Await breadcrumb technique, with explicit
  channel typing and no unrelated route migration.
- Reuse normal action/root-loader revalidation and the existing Zustand snapshot.
- Reuse WebApplicationFactory/mock repositories, fake CrossApiService and YouTube,
  and existing channel test fixtures. Follow the short-link test's failed-modal
  interaction and QuickLinks' real router refresh coverage.

## Risks

| Rank | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Treating editable YouTube ID as a tenant rename breaks ownership, scope, API keys, content, and credential associated data. | Keep Id/ChannelId immutable; use a separate external binding. | Same route, selected channel, owner/key access, content references, and credential decryption after the edit. |
| High | Old binaries replace whole channel documents and discard the new external binding. | Update every channel-writing binary; prohibit old writers during/after edits; export affected metadata before rollback. | A scan/settings/channel save preserves the new field, including cross-host writer compatibility. |
| Medium | Returning 204 everywhere removes logo state or hides cache degradation; old SPA clients may require `success`. | Endpoint-specific matrix, dual-shape client compatibility, client-first status rollout. | Normal 204 succeeds; failed invalidation still warns once; upload preview and normalized terminology remain correct. |
| Medium | A missing management projection field or omitted form value clears Analytics accidentally. | Correct all read projections and make omission distinct from clear. | Create/update/detail/list/accessible/reopen agree; omitted updates preserve; blank explicitly clears. |
| Medium | Real Google Analytics links may contain fragments currently rejected. | Preserve policy in this feature and surface the exact message; seek explicit approval before widening accepted URLs. | Invalid fragment/query input produces an inline explanation, never a generic fallback. |
| Medium | Switching a stored channel scan to a different YouTube binding changes its external source. | Explain binding semantics; keep histories and attribution stable; use fake provider tests, then authorized operational check. | New provider calls use the edited binding while news ownership and historical content remain unchanged. |
| Medium | Error parsing/mapping regressions hide backend messages or alter other applications. | Narrow admin 400 handling; retain envelope/status and existing isolated modes. | JSON/text/empty and field-error fixtures pass; account/public credential-isolation tests stay unchanged. |
| Low | Deferred breadcrumb changes leak labels across routes or show an invalid link. | Typed handle, neutral loading label, unchanged route target. | Deferred success/failure plus blog/flat-loader regression checks. |
| Medium | Broad logo catch exposes internals and classifies provider failures as bad input. | Expected input errors only become 400; server failures use safe host error handling. | Invalid PNG has field error; blob failure is not a 400 and reveals no exception detail. |

## Technical Debt

- **Required now:** conflated tenant/external IDs blocks safe YouTube editing.
  Separate them without rewriting existing identity.
- **Required now:** manual management projection drifts from shared conversion;
  fix the affected properties and prove parity. General projection generation
  or contract-mapper frameworks are deferred.
- **Required now:** channel action field-error loss and failed-modal lifecycle.
  Fix locally; do not normalize every form in the repository.
- **Required now:** the logo response audit must not preserve exception disclosure
  or classify infrastructure failures as user input errors.
- **Deferred:** duplicate app-local/shared channel DTO definitions. Keep both
  directly affected definitions consistent without a type-ownership migration.
- **Deferred:** standalone legacy channel create/edit modules and old route tree.
  Do not delete them without a separate consumer audit.
- **Deferred:** DataService's silent duplicate/no-match save behavior and concurrent
  deletion race. Do not introduce new silent returns; existing behavior does not
  justify tenant renaming. Atomic mutation outcomes/index enforcement are a
  separate persistence task unless a focused regression demonstrates a blocker.
- **Deferred:** broad cache reliability, blob-orphan compensation, full channel
  optimistic concurrency, external-binding uniqueness policy, and historical
  `YouTubeChannelId` configuration naming.

## Implementation Roadmap

1. **Lock contract decisions and regression expectations.**
   Owners: this handoff, channel backend/frontend tests.
   Accept document-ID breadcrumb meaning, stable-identity split, and warning
   exception to 204. Capture current failing round trips, field feedback, and
   deferred breadcrumb behavior. Gate: tests fail for the intended defects, not
   fixture/bootstrap failures.
2. **Add compatible binding and shared conversion.**
   Owners: Models, Contracts, both channel TypeScript definitions.
   Implement legacy fallback, derived Studio, and complete management projection.
   Keep document/tenant identity untouched. Gate: legacy BSON, projection parity,
   optional read-ID typing, and old document round trips.
3. **Implement request/persistence and external-call semantics.**
   Owners: ChannelsController and InsightIngestionService.
   Add optional update binding, Analytics preserve/clear behavior, field-keyed
   validation, and switch only external stored-channel arguments. Gate:
   authorization, references, existing credential decryption, provider arguments,
   and invalid-input no-mutation tests. No data backfill.
4. **Preserve error information in transport and channel adapters.**
   Owners: services apiTransport and channel response helper/action.
   Handle admin 400 JSON/text/empty safely, retain field map/status, translate
   keys, and preserve useful generic errors. Gate: field arrays, nested/unknown
   keys, plain text, malformed/empty body, context 400/404, and unaffected modes.
5. **Complete the editor and navigation refinement.**
   Owners: shared form/action, route metadata, Breadcrumbs, existing sidebar
   test coverage.
   Expose YouTube binding in edit/confirmation, remove Studio input, send
   Analytics on create, associate errors, close failed confirmation, and display
   document ID from deferred data. Gate: create/global/self edit, no error-path
   navigation/toast, ID display with unchanged href, and real root revalidation.
6. **Apply the endpoint success matrix and logo error classification.**
   Owners: ChannelsController, response warning helper, logo detail consumer,
   backend/SPA tests.
   Support current/new response shapes in clients before changing server
   statuses; retain required values and warning-only 200s. Gate: every endpoint
   row, zero-body assertions for 204, degraded-cache warning after persistence,
   and safe infrastructure errors.
7. **Validate integration, rollout, and documentation.**
   Owners: affected project checks, browser evidence, architecture/feature docs.
   Build shared frontend packages in order; run focused backend tests after one
   serial build, with no implicit test rebuild; run relevant transport/router/UI
   tests and affected type checks. Capture desktop/mobile evidence, verify
   ServerAPI channel projection and Windows selection compatibility where
   available, then update this file with exact actual results and limitations.
   Gate: all requested outcomes and invariant tests pass; no deployed claim
   without deployed evidence.

An independent post-validation review is warranted because the change touches
tenancy-adjacent identity, shared transport, and post-commit response semantics.
The user may select MorWalPiz Code Reviewer after implementation; review execution
is not part of this architect's work.

## Testing Strategy

### Backend unit and contract checks

Owner: MorWalPizVideo.BackOffice.Tests.

- Legacy BSON with missing/null external binding yields the old effective ID;
  new fields survive serialization/replacement. A normal unrelated write keeps
  the edited binding.
- Shared conversion and management projection return the same document ID,
  stable channel ID, external ID, derived Studio URL, and Analytics.
- Studio derives from the edited external ID even when a legacy stored/custom
  Studio value exists; there is no trailing path or arbitrary-host injection.
- Required/model-binding failures and manual URL errors share a field-errors
  dictionary and HTTP 400; check messages and field keys, not just the status.

### Integration/behavior and authorization

- Create Analytics; verify repository plus detail/list/accessible responses.
  Update, reopen, change an unrelated field, and verify Analytics remains.
  Omitted/null preserves; explicit blank clears; invalid input leaves data and
  cache calls unchanged.
- Edit binding through authorized global and self-service paths. Verify
  document ID, ChannelId, ownership, keys, content, selected scope, and encrypted
  credentials remain valid.
- Non-owner/impersonated unauthorized edits remain concealed as 404; permission
  denial behavior stays unchanged; selected header does not redirect route-based
  channel mutation to another channel.
- Fake YouTube captures the new binding in both stored-channel scan paths;
  attribution remains the original stable tenant key.
- Verify the entire success matrix, including empty 204 bodies, normalized
  terminology, logo preview data, and each relevant post-commit cache-warning
  path. A warning must not masquerade as an uncommitted operation.
- PNG input failures return safe field errors; simulated blob/update provider
  failures remain server errors with no exception disclosure.

### Transport, route, and component checks

Owners: shared services Vitest tests and BackOffice SPA Vitest/Testing Library.

- Admin 400 fixtures: Validation Problem Details, JSON string, plain text,
  empty/non-JSON body; retain 400 and useful messages/fields.
- Field mapping: PascalCase/camelCase, exact YouTube alias, multiple messages,
  publishing/social nesting, and unmapped keys visible in the generic summary.
- Existing context-error 400/404 handling and account/public isolation remain.
  Verify successful 204 avoids JSON parsing and produces local action success.
- Active create and edit send Analytics and the YouTube binding; missing fields
  do not become clears; Studio is absent from editable controls/payloads.
- Edit hydrates the saved values. Failed confirmation closes; inputs survive;
  focus reaches the invalid control/summary; no success toast or navigation.
- Real router deferred data renders document ID in the breadcrumb and keeps
  the stable route href. Include loading/rejection and unchanged blog/flat
  breadcrumb paths.
- Root-loader revalidation updates derived Studio and Analytics in the sidebar
  without reloading or changing selected tenant.

### Migration and browser/end-to-end checks

- Use existing isolated fixtures, not production Mongo documents or credentials.
  Add a real Mongo compatibility check only in an authorized test environment;
  mock success alone is not serializer/provider proof.
- Browser check at desktop and mobile widths: create -> save -> detail -> edit,
  change binding -> save -> correct Studio destination, Analytics survives,
  invalid URL -> actionable inline message. Repeat relevant flow on my-channel.
- Check keyboard-only confirmation/error recovery, field descriptions,
  breadcrumb current-page semantics, long-ID wrapping, and no overflow.
- Windows desktop validation is a separate platform gate if unavailable on the
  implementation host. No upload to YouTube or desktop OAuth change is required.
- Run the smallest relevant selections first. Report actual results and any
  unavailable gate; do not substitute historical handoff counts for new proof.

## Migration And Rollback

### Persistence and configuration

This is an additive Mongo transition with tolerant reads. No tenant rename,
content backfill, index, or SQLite migration is required. Missing legacy external
binding resolves to the existing stable key; explicit new bindings are preserved.
The legacy Studio field remains stored but is no longer authoritative.
No configuration values, public tenant selectors, API-key bindings, ownership
records, or credential associated data change.

Because replacement writes can erase unknown fields, all channel writers must
be on compatible binaries before enabling external-binding edits. This includes
scan paths that update channel histories, not just the administration endpoint.
No new feature flag is required for steady state; use controlled staged release
or temporarily withhold the new editor until this compatibility gate passes.

### Deployment order

The worktree backend already returns 204 on normal bodyless mutations and the
SPA already supports 204 plus warning-only 200 responses. Do not deploy only one
side while old browser sessions or external clients still require a body or
`success` flag. Use a coordinated maintenance release, or first add an explicitly
approved compatibility bridge that can stage client support before exposing
binding edits and changing server response statuses.

Before production exposure, verify every channel writer is compatible with the
additive field, rebuild/deploy both APIs that use the shared conversion, refresh
browser assets, inventory external consumers, and observe error statuses,
cache-warning logs, Analytics round trips, and YouTube scan targets. Never log
full channel requests or secrets.

### Rollback limits and recovery

- A client-only rollback cannot safely restore the old response assumptions
  against a new server. Restore compatible success envelopes first or deploy
  client/server rollback together.
- Keep compatible server model readers/writers after a UI rollback. Old binaries
  may erase the binding on any channel replacement.
- Before rolling back external-ID support, export the minimal affected identity
  metadata under normal operational controls, stop incompatible writers, and
  either keep tolerant binaries or deliberately restore external bindings to
  their prior values. Do not reassign the tenant or purge new content/history.
- Retain Analytics and legacy Studio values. Do not perform destructive cleanup
  as part of rollback. Already cleared values need authorized backup recovery;
  this handoff does not invent a lost historical Analytics value.
- Preserve cache invalidation/warnings. Deployment rollback cannot undo a
  committed channel mutation or external calls that already completed.
- Authentication, cache-tag naming, configuration, and SQLite have no migration
  to reverse.

## Implementation And Validation

Implemented the stable/external identity split, derived Studio URL, Analytics
round-trip, field validation and error mapping, endpoint-specific mutation
responses, logo warning handling, and document-ID breadcrumb. Added
[ADR-026](../architecture/adr/ADR-026-stable-channel-identity-and-youtube-binding.md)
and updated the owning architecture and feature records.

Validation performed:

- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed; existing ImageSharp advisories and nullable warnings remain.
- Focused backend channel/tenancy tests — 37 passed.
- Broader `AdminEditWorkflowTests` and `ChannelTenancyAuthorizationTests` selection — 58 passed, 1 unrelated failure: `QuickLinks_create_persists_and_returns_the_additive_youtube_kind` expects 200 but receives 204. QuickLinks is outside this change and was not modified.
- `yarn --cwd frontend/fe-packages/services test src/apiService.test.ts` — 24 passed.
- Focused SPA breadcrumb/routing/form tests — 28 passed.
- `yarn --cwd frontend build:shared` — passed.
- `yarn --cwd frontend/back-office-spa build` — passed; existing large-chunk warning remains.

React Bootstrap `controlId` and Node `TimeoutNaNWarning` warnings appeared in the
SPA tests. No browser captures, deployed-client inventory, production Mongo
compatibility test, or deployment was performed. These remain release gates.

## Remaining Release Gates

1. Verify deployed channel writers are compatible with the additive optional
   binding; replacement writes from older binaries can erase it.
2. Inventory external and stale-browser clients before the coordinated rollout
   because normal channel mutation responses now return 204.
3. Perform real-browser desktop/mobile checks and persistent-Mongo legacy
   missing/null field compatibility verification.
4. Preserve the existing Analytics policy: absolute HTTPS without credentials,
   query, or fragment. Widening it requires a separate decision.
5. A true tenant rename/content transfer remains out of scope and requires a
   separate migration handoff.
