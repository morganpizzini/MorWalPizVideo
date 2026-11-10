# Channel public base URL and QuickLinks destination

Status: Implemented and locally verified (2026-11-10). Production origin
configuration and deployed route reachability remain operational gates.
This is the editable implementation and delivery record.

## Delivered changes

- Added `PublicBaseUrl` to the persisted channel, shared .NET response/converter,
  manual management projection, and both TypeScript channel definitions.
  An explicit `BsonDefaultValue("")` preserves empty-string reads for old
  documents; the regression test showed that the C# initializer alone was
  insufficient for MongoDB record deserialization.
- Channel create/update validates an origin before persistence and canonicalizes
  it without a trailing slash. Omitted/null update input preserves existing
  configuration, while blank input clears it. Authorization, full-document
  repository persistence, and existing cache invalidation/warnings are unchanged.
- The active shared form hydrates/submits the field, exposes associated help
  and inline errors, and supports both global and self-service actions. Detail
  displays the saved value. The app-local `channels/publicBaseUrl.ts` helper is
  reused by action validation and defensive destination rendering.
- QuickLinks detail resolves the selected accessible channel by `channelId`,
  requires exact page ownership, and renders the complete encoded destination
  with `target="_blank"` and `rel="noopener noreferrer"`. Missing selection,
  ownership, configuration, and invalid stored configuration have distinct
  non-clickable messages. Long URLs wrap using the existing Bootstrap utility.
- Implemented the user's approval at the end of this handoff: Shooting ITA
  now serves `/quick-links/:url` through its existing component/loader and keeps
  `/quick-link/:custom-linktree` working. No API endpoint changes were needed.
- Real root-loader/action integration proves that normal router revalidation
  refreshes the store and destination; no context implementation change,
  additional cache, explicit revalidation workaround, or new fetch was needed.

### Actual file ownership

- Persistence/contracts: `MorWalPizVideo.Models/Models/YTChannel.cs`,
  `MorWalPiz.Contracts/Contracts/ChannelContract.cs`, and
  `MorWalPiz.Contracts/ContractUtils.cs`.
- API: `MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs`.
- Frontend contracts: `frontend/fe-packages/models/src/channel.ts` and
  `frontend/back-office-spa/src/models/channel.ts`.
- Channel UI/action: `frontend/back-office-spa/src/routes/channels/publicBaseUrl.ts`,
  `form/Component.tsx`, `form/action.ts`, and `detail/Component.tsx`.
- QuickLinks UI: `frontend/back-office-spa/src/routes/quickLinks/detail/Component.tsx`.
- Shooting ITA: `frontend/shooting-ita-frontend/src/routes/config.tsx` and
  `routes/quickLink/loader.ts`.
- Regression coverage: `ChannelTenancyAuthorizationTests.cs`, channel
  `__tests__/routing.test.ts`, `__tests__/detail.test.tsx`, new
  `channels/form/Component.test.tsx`, new QuickLinks detail
  `Component.test.tsx` and `refresh.test.tsx`, and Shooting ITA's new
  `routes/quickLink/loader.test.ts`. Existing `ChannelContext.test.tsx` was
  run unchanged; refresh coverage lives beside the destination instead.
- Documentation: this record, `LINKTREE_IMPLEMENTATION.md`,
  `docs/architecture/domain-model.md`, and `docs/architecture/frontend.md`.

### Executed validation

From the repository root unless a working directory is stated:

1. `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
   passed, compiling Models, Contracts, Domain, MvcHelpers, BackOffice, and
   ServerAPI. Existing ImageSharp vulnerability warnings remain; no dependency
   changes were made.
2. `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter 'FullyQualifiedName~ChannelTenancyAuthorizationTests|FullyQualifiedName~AdminEditWorkflowTests' --logger 'console;verbosity=minimal'`
   passed: **57 tests**. Covers create/update/all response mappings,
   normalization/ports, rejection without mutation, omitted/null preservation,
   explicit clearing, legacy BSON, existing ownership checks, and cache warnings.
3. `yarn --cwd frontend build:shared` passed in models/services/layout order.
4. In `frontend/back-office-spa`, `../node_modules/.bin/vitest run src/routes/channels/__tests__/routing.test.ts src/routes/channels/__tests__/detail.test.tsx src/routes/channels/form/Component.test.tsx src/routes/quickLinks/detail/Component.test.tsx src/contexts/ChannelContext.test.tsx`
   passed: **39 tests**.
5. In that same directory,
   `../node_modules/.bin/vitest run src/routes/quickLinks/detail/refresh.test.tsx`
   passed: **1 integration test** using the real root auth loader, channel
   provider/store, shared form action, and router fetcher revalidation.
6. In `frontend/shooting-ita-frontend`,
   `./node_modules/.bin/vitest run src/routes/quickLink/loader.test.ts`
   passed: **2 route compatibility tests**.
7. `../node_modules/.bin/tsc -b --pretty false` passed from both affected
   application directories.
8. The Impeccable detector reported no findings for the three modified UI
   components. Headless Chromium exercised the running Vite app at
   **1440x1000** and **390x844**, using intercepted API fixtures: edit hydration,
   focus, save, client-side navigation with refreshed context, exact safe href,
   long URL wrapping/no horizontal overflow, and non-clickable missing state
   all passed. Six screenshots were captured and visually inspected in the
   session artifact folder. The temporary server/browser were stopped.

Initial failed checks were resolved: a record/list reference-equality assertion
was replaced by BSON document equivalence, the missing BSON default was added,
test commands were run from their actual app directories, and isolated tests
reuse the repository's Node/jsdom AbortController workaround and mock unrelated
reCAPTCHA/browser-storage dependencies. No production workaround was introduced.

### Remaining release gates

- Browser API responses above are fixtures, not a deployed end-to-end claim.
  Configure verified real public origins and check route reachability after
  API-first rollout, including deployment of the Shooting ITA alias.
- Full workspace/solution builds, broad lint/format/hygiene checks and full test
  suites are deferred to the downstream automatic final procedure. Windows
  application execution was not attempted on Linux.
- Keep the rollback/export precautions below: older channel-writing binaries
  may discard the added BSON field during whole-document replacement.
- Unrelated pre-existing worktree edits were preserved.

## Outcome

Let channel administrators and authorized channel owners configure a public-site
base domain during channel creation/editing. QuickLinks detail must generate an
absolute destination consisting of that channel's public origin followed by
`/quick-links/` and the existing normalized page slug.

Assumptions and completion criteria:

- Name the setting `PublicBaseUrl` in .NET and `publicBaseUrl` in JSON,
  TypeScript, and MongoDB. It describes the public website, not the redirect
  service or API host.
- It is optional for compatibility. Missing configuration produces an explicit
  non-clickable configuration message, never a relative BackOffice link.
- A configured example origin `https://example.com` and slug `community`
  produce exactly `https://example.com/quick-links/community`.
- Both create and update persist the setting; detail, global-list, and
  accessible-channel responses return it; reopening edit restores it.
- The displayed destination updates from refreshed channel context, without
  requiring logout or a hard reload after saving the channel.
- Preserve existing public routes and add the approved Shooting ITA
  `/quick-links/:url` alias. No short-link behavior changes, host provisioning,
  slug migrations, authorization redesign, or general channel-form cleanup.

## Investigation Evidence (pre-implementation)

Paths are relative to the repository root; links resolve from this document.

- [YTChannel](../../MorWalPizVideo.Models/Models/YTChannel.cs) is a MongoDB
  record with explicit BSON fields and defaults. Its existing `ShortLinkUrl`
  is separate from the requested public origin.
- [ChannelsController](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs)
  owns create/update request types, normalization, persistence assignments,
  and a manual `ConvertManagement` response projection. Global and accessible
  lists use `ContractUtils.Convert`; channel detail uses `ConvertManagement`.
  Adding a property to only the shared projection would lose it in detail.
- [ChannelContract](../../MorWalPiz.Contracts/Contracts/ChannelContract.cs)
  and [ContractUtils.Convert](../../MorWalPiz.Contracts/ContractUtils.cs) own
  shared channel responses.
  [ChannelManagementContract](../../MorWalPizVideo.BackOffice/DTOs/ChannelManagementContract.cs)
  inherits the shared contract and adds sanitized publishing state.
- [DataService](../../MorWalPizVideo.Domain/DataService.cs) reads channels by
  `ChannelId`; `SaveChannel` and `UpdateChannel` use the existing
  `IYTChannelRepository`. The Mongo and mock implementations in
  [Repository](../../MorWalPizVideo.Domain/Interfaces/Repository.cs) and
  [MockRepository](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs)
  already persist the complete channel model.
- The active [route manifest](../../frontend/back-office-spa/src/router/routes/lazy-index.ts)
  routes channel creation/editing to the shared
  [channel form](../../frontend/back-office-spa/src/routes/channels/form/Component.tsx)
  and [form action](../../frontend/back-office-spa/src/routes/channels/form/action.ts).
  The action constructs create/update payloads separately. Both must carry the
  new field. Separate legacy create/edit modules are not the active routes.
- [Self-service edit](../../frontend/back-office-spa/src/routes/channels/self/edit/action.ts)
  delegates to that same form action using the selected channel ID.
- [Shared Channel](../../frontend/fe-packages/models/src/channel.ts) and the
  [app-local Channel](../../frontend/back-office-spa/src/models/channel.ts)
  are currently different definitions. The form uses the shared type; context
  and root bootstrap use the local type. Both require the additive setting.
- [ChannelContext](../../frontend/back-office-spa/src/contexts/ChannelContext.tsx)
  exposes accessible channels and the selected `channelId`, backed by the app
  store. [authLoader](../../frontend/back-office-spa/src/router.ts) reloads
  accessible channels and hydrates that store.
- [QuickLinks detail](../../frontend/back-office-spa/src/routes/quickLinks/detail/Component.tsx)
  currently uses a relative React Router link. Its
  [loader](../../frontend/back-office-spa/src/routes/quickLinks/detail/loader.ts)
  loads only the QuickLinks record.
- [QuickLinksController](../../MorWalPizVideo.BackOffice/Controllers/QuickLinksController.cs)
  requires channel scope for writes but its detail/list reads are global.
  [QuickLinks](../../frontend/fe-packages/models/src/quickLinks.ts) includes
  an optional owner `channelId`; detail is therefore not guaranteed to match
  the current channel selection.
- [Public routing](../../frontend/morwalpizvideo.client/src/routes/config.tsx)
  supports `/quick-links/:url`.
  [Shooting ITA routing](../../frontend/shooting-ita-frontend/src/routes/config.tsx)
  instead supports `/quick-link/:custom-linktree`. The requested destination
  shape is not currently universal across public hosts.
- [ShootingItaController](../../MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs)
  also projects `ChannelContract` and output-caches channel lists.
  Channel mutations already call the existing public-cache invalidation helper.
- Existing coverage includes
  [channel tenancy tests](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs),
  [channel route tests](../../frontend/back-office-spa/src/routes/channels/__tests__/routing.test.ts),
  [channel context tests](../../frontend/back-office-spa/src/contexts/ChannelContext.test.tsx),
  and [QuickLinks route tests](../../frontend/back-office-spa/src/routes/quickLinks/__tests__/routing.test.ts).
- Owning backend manifests target .NET 10; the
  [frontend manifest](../../frontend/package.json) defines Yarn workspaces and
  ordered shared-package builds. The
  [BackOffice manifest](../../frontend/back-office-spa/package.json) supplies
  Vitest, TypeScript/Vite, ESLint, and Testing Library.

## Implemented Design

### Persisted setting and API

Add a string property with an empty default to `YTChannel` and `ChannelContract`.
Extend the shared converter and the explicit management projection.

Use one controller-owned normalization rule for both mutation requests:

- Null/omitted on create and blank input mean unconfigured.
- On update, omitted/null preserves the existing setting; an explicitly blank
  string clears it. This prevents old clients from unintentionally erasing it.
- Trim whitespace; accept only absolute HTTP/HTTPS origins with a nonempty
  host and optional port. Reject credentials, non-root paths, query strings,
  fragments, relative URLs, and other schemes.
- Store a canonical origin without a trailing slash. HTTPS is recommended in
  help text; HTTP remains accepted for local development.
- Reject invalid nonblank input before persistence using the existing
  controller validation-response style. Do not fetch or probe the destination.

Do not repurpose `ShortLinkUrl`: it can contain a path such as `/sl`.
Its current normalizer is not suitable unchanged because it allows paths and
does not explicitly reject credentials. Avoid a new shared URL framework for
this single field.

### Channel UI

Extend the active shared form's state, edit initialization, submission, and
both action payload branches. Add a labeled optional URL input near the
short-link base setting, with help distinguishing the public origin from the
short-link prefix. Provide field-level feedback through `FieldError`; retain
the existing generic API-error presentation and save/cache-warning behavior.
Expose the saved setting in channel detail using the existing detail layout.

Keep both current channel type definitions compatible; do not combine this
change with their broader consolidation.

### QuickLinks detail

Resolve the selected channel from the existing context by `channelId`, not by
Mongo document ID. A clickable public destination requires:

1. A selected accessible channel.
2. The QuickLinks owner to match that selected channel.
3. A valid configured public origin.

If selection and owner differ, show an explicit message asking the user to
select the owning channel; do not silently generate a link on another host.
For legacy records without an owner, show an ownership-unavailable message.
Do not expand read authorization or infer ownership from a slug.

Compose the fixed route with the existing normalized slug as a path segment.
Use a normal external anchor, display the complete absolute URL, and retain
new-tab behavior with `noopener noreferrer`. Never use the admin origin,
short-link URL, API base URL, a hardcoded production domain, or a previous
channel as a fallback. Defensively reject invalid legacy configuration with
visible feedback rather than a render exception or unsafe scheme.

Rely on normal router action revalidation to refresh the root accessible-channel
data after saves; prove that the existing store receives the new setting.
If the focused test identifies a skipped root refresh, explicitly revalidate
through the existing router seam, not a second channel cache or direct fetch.

### UI workflow for implementation

This is an **Operate**-mode refinement, not a redesign. Use the installed
Impeccable `harden` workflow during implementation for URL validation, missing
configuration, ownership mismatch, and accessible feedback. Do not run that
workflow as part of this architecture handoff.

No BackOffice `PRODUCT.md` or `DESIGN.md` was found in the frontend tree.
Preserve its React Bootstrap form groups, `PageHeader`, `DetailPanel`, and
existing error/toast components; another application's design file is not
BackOffice's design authority. Minimum visual evidence: desktop and narrow
mobile captures of the new field and populated/missing QuickLinks destination;
check keyboard focus, label/error association, and long-domain wrapping.

## Impacted Projects

- **Modify:** `MorWalPizVideo.Models`, `MorWalPiz.Contracts`,
  `MorWalPizVideo.BackOffice`, `MorWalPizVideo.BackOffice.Tests`,
  `frontend/fe-packages/models`, and `frontend/back-office-spa`.
- **Verify only:** Domain Mongo/mock persistence; ServerAPI's shared channel
  projection and existing channel-cache invalidation.
- **Shared frontend consumers:** services and layout packages, public client,
  Shooting ITA, and frozen shop declare the models dependency. Keep the field
  optional and preserve their behavior; no feature work in frozen shop.
- **Other shared .NET consumers:** MvcHelpers, ShortLinks, FreezerAnalysis,
  and Windows VideoImporter/InsightScanner dependency paths need compatible
  shared-library compilation, not a new feature or local database schema.
- **Deployment:** existing BackOffice API and admin SPA releases; ServerAPI
  recompilation carries the additive response field when released. AppHost,
  authentication, DNS, infrastructure, and standalone tools have no changes.

## Impacted Files

Paths below are relative to the repository root.

### Modify

- `MorWalPizVideo.Models/Models/YTChannel.cs`: persisted optional setting.
- `MorWalPiz.Contracts/Contracts/ChannelContract.cs`: additive response field.
- `MorWalPiz.Contracts/ContractUtils.cs`: shared channel projection.
- `MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs`: request fields,
  shared validation, create/update assignments, management projection.
- `frontend/fe-packages/models/src/channel.ts`: shared channel and derived DTOs.
- `frontend/back-office-spa/src/models/channel.ts`: context/bootstrap type.
- `frontend/back-office-spa/src/routes/channels/form/Component.tsx`: input,
  hydration, feedback, and submission.
- `frontend/back-office-spa/src/routes/channels/form/action.ts`: parse/validate
  and forward the setting through create and update.
- `frontend/back-office-spa/src/routes/channels/detail/Component.tsx`: saved
  setting in the existing detail presentation.
- `frontend/back-office-spa/src/routes/quickLinks/detail/Component.tsx`:
  context-derived external destination and explicit unavailable states.
- `MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs`:
  persistence, response, validation, compatibility, and ownership regressions.
- `frontend/back-office-spa/src/routes/channels/__tests__/routing.test.ts`:
  create/update payload and validation regressions.
- `frontend/back-office-spa/src/routes/channels/__tests__/detail.test.tsx`:
  saved setting display.
- `frontend/back-office-spa/src/contexts/ChannelContext.test.tsx`: refreshed
  accessible-channel values and selection changes.

### Add

- Component tests beside the channel form and QuickLinks detail, following
  existing `Component.test.tsx` naming: hydration/submission, generated URL,
  selection/owner states, missing/invalid configuration, and safe anchor.
- No new application service, endpoint, package, infrastructure, or general
  URL utility is required.

### Generated/Migration

None. No EF migration, Mongo index, generated API client, or committed build
output is needed.

### Documentation

- This handoff: update with actual files, validation results, and residual risks.
- `LINKTREE_IMPLEMENTATION.md`: document channel-based BackOffice destinations
  and configuration requirements.
- `docs/architecture/domain-model.md`: record the new channel-owned setting
  after implementation, without presenting the proposed field as current.
- No new ADR is necessary: ownership, topology, and authorization remain intact.

### Verify Only

- `MorWalPizVideo.BackOffice/DTOs/ChannelManagementContract.cs`: inherits field.
- `MorWalPizVideo.Domain/DataService.cs` and Domain repository implementations:
  existing whole-channel persistence.
- `frontend/back-office-spa/src/router.ts`,
  `src/contexts/ChannelContext.tsx`, and `src/routes/channels/self/edit/action.ts`:
  bootstrap/revalidation and self-service reuse.
- `MorWalPizVideo.ServerAPI/Controllers/ShootingItaController.cs`: additive
  contract output and established channel cache.
- Public-client and Shooting ITA route configuration: verify destination host
  supports the exact requested route. The public client is unchanged; Shooting
  ITA receives the explicitly approved compatible alias described above.
- Existing project/package manifests and workflows: no dependency or pipeline
  changes; validate through their existing tooling.

## Impact Matrix

| Area | Decision |
|---|---|
| Backend/API | Add optional channel setting; validate before mutation; preserve response/status conventions. |
| Frontend/UI | Channel field and saved display; absolute QuickLinks anchor from current context. |
| Database/Data | Additive Mongo channel field; legacy documents remain readable. |
| Contracts/Models | Shared .NET response and both SPA channel types updated; optional TS field. |
| Authentication/Authorization | Existing create-admin and route-channel owner/update checks remain unchanged. No token, cookie, or CORS changes. |
| Validation | Origin-only HTTP/HTTPS rule on server; client field feedback; unsafe legacy values are non-clickable. |
| Cache | Preserve existing channel-mutation invalidation and warning behavior; no new cache or QuickLinks purge just for UI URL composition. |
| Configuration/Secrets | Channel data, not environment configuration; no secrets or outbound destination probes. |
| Logging/Telemetry | Existing mutation/cache logging and UI errors; no new telemetry service or credential-bearing URL logging. |
| Background Jobs | Not applicable; synchronous configuration and presentation only. |
| Deployment/Operations | API-first rollout, then SPA; operators configure verified public origins. No automatic DNS/hosting work. |
| Backward Compatibility | Missing field tolerated; old update clients preserve it; blank input explicitly clears. |

## Reuse Opportunities

Reuse `YTChannel`, `ChannelContract`, `ContractUtils`, management-contract
inheritance, `DataService`, and existing Mongo/mock repositories. Reuse the
shared form action for global and self-service editing, the existing service
transport/endpoints, `ChannelContext`, router revalidation, `FieldError`,
`GenericErrorList`, `DetailPanel`, and existing test factory/router mocks.
There is no need to modify the public QuickLinks contract or store a redundant
computed public URL per QuickLinks record.

## Risks

| Level | Cause and affected surface | Mitigation and verification signal |
|---|---|---|
| Medium | Global QuickLinks detail may belong to another channel. | Require owner/selection agreement; two-channel test proves no wrong-host link. |
| Medium | Manual management projection can drop the field. | Test create, detail, edit reopen, accessible list, and global list independently. |
| Medium | Unsafe URL input or legacy values become clickable. | Strict origin validation and defensive UI check; reject credentials, schemes, paths, query, and fragment. |
| Medium | Older API binaries replace channel documents without retaining a field they do not model. | Upgrade all channel-writing hosts before population; back up setting values before backend rollback. |
| Medium | Root/store data remains stale after editing. | Integration-style context refresh test and real-browser save-to-detail check. |
| Medium | A configured deployment does not serve `/quick-links/:url`, including Shooting ITA before alias rollout. | Deploy the compatible alias and verify route reachability; do not infer alternate routes from channel flags. |
| Low | Cache reset fails after channel change. | Preserve explicit degraded-cache warnings; test the existing invalidation path. |
| Low | Long URL disrupts narrow layouts. | Mobile capture and wrapping/focus checks. |

## Technical Debt

- Shared and local channel types duplicate ownership. Add the field to both
  now; defer general consolidation.
- Inactive channel create/edit modules coexist with the active shared form.
  Do not extend or remove unused paths solely for this change.
- Existing analytics-field creation/projection omissions were encountered in
  adjacent code. They are unrelated; do not bundle fixes.
- Global QuickLinks reads versus scoped writes cause selection mismatch.
  Isolate through explicit presentation feedback, not an authorization rewrite.
- Shooting ITA's historical route remains compatible alongside the approved
  fixed-path alias. A configurable path/template system remains out of scope.

## Implementation Roadmap

1. **Contracts and persistence:** extend Models, shared .NET contract/converter,
   controller request/projection paths, and both TypeScript channel types.
   Gate: legacy document/response tolerance and round-trip response shape.
2. **API invariants:** add one normalization rule, persist on create/update,
   preserve omitted update values, support explicit clearing, and retain
   authorization/cache behavior. Depends on step 1. Gate: focused backend
   behavior tests including invalid input with no mutation.
3. **Channel editing:** wire the active form and both action payload branches;
   show the saved setting in detail. Depends on steps 1-2. Gate: component and
   action tests for create, edit, self-service reuse, clear, and failure.
4. **QuickLinks destination:** consume context, check selected owner and origin,
   and replace the relative link with the safe absolute anchor or visible
   unavailable message. Depends on steps 1-3. Gate: exact href, cross-channel,
   missing configuration, invalid legacy value, and refresh tests.
5. **Compatibility and delivery:** validate existing builds in shared-package
   order, preserve public contracts/cache behavior, perform desktop/mobile and
   real-browser save-to-open checks, then update the linked documentation.
   Depends on steps 2-4. Gate: recorded results and verified configured host.

Stop when the property round-trips and the correct external destination is
proved, including explicit unavailable states. No feature flag, migration job,
new framework, public route redesign, or unrelated form repair belongs here.
An independent post-validation review is warranted for the manual mappings,
owner/selection guard, and old-client update semantics; the user may select
MorWalPiz Code Reviewer after implementation.

## Testing Strategy

- **Backend integration/behavior:** use the existing BackOffice test factory,
  test identity/owner setup, mock channel repository, and channel tenancy suite.
  Assert stored values and all three response shapes. Exercise create/update,
  whitespace/trailing slash normalization, HTTP/HTTPS with ports, blank clear,
  omitted/null preservation, and legacy absent BSON field.
- **Validation/security regressions:** relative/protocol-relative URL, unsafe
  scheme, credentials, query, fragment, and non-root path return validation
  failure with no persistence. Existing unauthorized/non-owner behavior and
  cookie/CSRF boundaries must remain unchanged.
- **Component/action:** prove field initialization and resubmission; assert
  exact create and update payloads, inline errors, and API error presentation.
  No success toast/navigation on failed saves.
- **QuickLinks:** assert the complete href and text, new-tab rel attributes,
  no admin-relative fallback, no duplicate slash, current-channel changes,
  owner mismatch, missing owner/selection, missing origin, invalid stored origin,
  and long URL presentation.
- **Context/contract:** save changed origin, revalidate accessible channels,
  and prove subsequent QuickLinks detail uses the refreshed value. Old API
  responses without the field show configuration feedback.
- **Cache:** existing channel reset/purge behavior and failed-invalidation
  warning remain intact; no new cache tags.
- **Builds:** use existing targeted backend checks, serial build followed by
  no-rebuild test execution; never overlap outputs. Build frontend shared
  models, services, and layout before the admin type-check/build. Start with
  touched tests; broaden only for shared-contract failures. Windows consumers
  require their supported Windows validation environment if compilation is
  implicated; do not attempt WPF execution on Linux.
- **End-to-end/operations:** create or edit an authorized channel, save its
  verified public origin, select it, open an owned QuickLinks record, and open
  the resulting exact URL on desktop/mobile. Confirm public route reachability
  separately from DOM correctness.

Executed implementation checks are recorded in the delivery section above.

## Migration And Rollback

Use additive tolerant reads, not a backfill. Existing Mongo documents remain
unconfigured until an authorized user enters a real origin. Do not infer values
from `ShortLinkUrl`, channel name, or a production-domain default. No index,
SQLite, auth, cache schema, API version, or environment migration is needed.

Release the new channel-writing backend before the SPA and before populating
values. Old SPA clients remain compatible; omitted update fields preserve the
new setting. The new SPA against an old API must display missing configuration
rather than a relative destination.

After rollout, verify configured origins and exact route support, successful
response mappings, fresh context after save, and existing cache warnings.
Configuration can be corrected or explicitly cleared per channel.

SPA rollback is straightforward but reintroduces the old relative-link defect.
Backend rollback is not data-loss-free: older models may discard the added BSON
element when replacing a channel. Export only the channel ID/setting pairs under
normal operational access controls before rollback, avoid channel writes on the
old binary where feasible, and restore verified values after redeployment.
No automated recovery job is part of this slice.

## Resolved Scope Approval

- The user approved aligning Shooting ITA with `/quick-links/:url` ("yes align
  the endpoints"). Implemented as a compatible public route alias, retaining
  its existing filtered API and legacy route; no configurable path system.
