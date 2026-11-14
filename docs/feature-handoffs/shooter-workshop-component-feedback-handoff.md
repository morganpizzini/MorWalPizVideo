# Shooter Workshop Component Feedback Handoff

## Outcome

Align the Shooter Workshop Components route with the accepted user-check feedback:
load initial route data through React Router `loader.ts`, submit all mutations through
fetchers backed by `action.ts`, correct the definition-versus-physical-instance data
ownership, support optional firearm installation, and add edit/archive/delete
management for both catalogue definitions and physical inventory.

This is an Impeccable **Operate** refinement of the existing interface, not a visual
redesign. The implementation boundary is the Components route and its directly
required Workshop contracts, API behavior, persistence compatibility, tests, and
documentation. Unrelated Workshop routes and general component-system refactoring are
out of scope.

Assumptions resolved from the feedback:

- A component definition is a user-owned category/template. Its editable business
  fields are name, description, and notes.
- Manufacturer, model, compatibility tags, tracking mode, and service threshold
  ("max rounds" in the feedback) belong to each physical component.
- A physical component may remain an uninstalled spare. Selecting a firearm during
  creation is optional and creates an installation only when supplied.
- Archive is the normal reversible lifecycle action. Hard delete is offered only for
  pristine records with no historical references.
- Existing `ServiceInterval` remains the technical contract name for the user-facing
  maximum-round/service-threshold value.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` calls
  `useResource` three times during render for definitions, inventory, and firearms.
  The route has no `loader.ts` or `action.ts`, and
  `frontend/shooter-workshop.client/src/routes/config.tsx` registers only an element.
- Both current forms call shared service mutations directly. No Workshop route uses
  React Router `useLoaderData` or `useFetcher`, and
  `frontend/shooter-workshop.client/src/components/WorkshopForm.tsx` owns its own
  asynchronous submission state.
- The physical-component form always calls
  `createAndInstallWorkshopComponent`, marks firearm selection as required, and posts
  `definitionId`, `firearmId`, installation time, identification note, and an
  idempotency key. The reported `Firearm was not found.` response originates from the
  same-account lookup in
  `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`.
- Static inspection does not prove why a visibly selected firearm is rejected.
  `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` maps
  `request.FirearmId` directly into the command, and the frontend sends the selected
  state value. A request/response regression test must establish whether the failure
  is an empty/stale client value, account mismatch, route-action serialization issue,
  or server ownership defect before the error is considered fixed.
- `ComponentDefinition` currently persists manufacturer, model,
  `DefaultServiceInterval`, `TrackingMode`, notes, compatibility tags, and archived
  state. It has no revision. `ComponentInstance` already owns manufacturer, model,
  `ServiceInterval`, `TrackingMode`, status, notes, and revision, but has no
  compatibility tags or independent archived flag.
- `WorkshopService.CreateComponentAsync` currently inherits omitted physical values
  from the definition. That behavior conflicts with the corrected category-like
  definition model.
- The API exposes create/list only for definitions and physical components.
  Update/archive/delete endpoints and service methods do not exist. The Workshop
  repository supports account-scoped list/get/add/replace but has no account-scoped
  delete primitive.
- The immutable ledger and installation history require retired or historically used
  physical components to remain resolvable. The functional baseline explicitly says
  retired components remain in historical records.
- The current compatibility filter reads tags from each item's definition and is
  rendered before the physical-component form. The accepted feedback moves tag
  ownership to physical components and places the filter immediately above the
  inventory list, after the collapsible add form.
- `WorkshopComponentDefinition`, create requests, service functions, endpoint
  constants, and account-transport tests live in the shared frontend models/services
  packages, so API changes must remain synchronized across those packages and the
  standalone Workshop application.

## Proposed Design

### Route data and mutations

Add route-local `loader.ts` and `action.ts` beside the Components route. The loader
fetches definitions, physical components, and firearms concurrently and returns one
typed route-data object. `component.tsx` consumes it through `useLoaderData`; it must
not initiate initial network reads through effects or `useResource`.

The action accepts a required, allow-listed `intent` and dispatches definition or
physical-component create, update, archive, or delete operations. Each form uses its
own `useFetcher` instance so pending/error state is local and catalogue actions do not
block inventory actions. Return typed validation/problem details for expected API
errors and let unexpected failures reach the route error boundary; do not convert
failures into successful empty results.

Use fetcher forms or `fetcher.submit`, not direct service calls from the component.
On a successful action, rely on React Router revalidation to rerun the loader and
refresh all three datasets. Preserve form values and keep the form open on failure;
reset and close only after that fetcher reports successful completion.

### Corrected data ownership

Reduce `ComponentDefinition` to category data: name, description, notes, archive
state, revision, account ownership, identity, and timestamps. New definition writes
must not accept manufacturer, model, tracking mode, service interval, or compatibility
tags.

Make `ComponentInstance` the owner of manufacturer, model, compatibility tags,
tracking mode, service interval, lifecycle status, notes, revision, and a distinct
`Archived` flag. Keep `Status` for inventory/installation/maintenance/retirement
semantics; archive must not be represented by `Retired`, because hiding a record and
retiring a physical item are different user decisions.

Physical-component create and edit validation is:

- definition: required, active, same account;
- identification note, manufacturer, model, notes: optional and length bounded;
- compatibility tags: optional, trimmed, normalized consistently, de-duplicated
  case-insensitively, and count/length bounded;
- tracking mode: required;
- service interval: required and positive only for `Interval`; null for `Counter` and
  `Manual`;
- firearm: optional, but when supplied must be active and owned by the same account.

Do not inherit manufacturer, model, tracking mode, interval, or compatibility tags
from the definition. The physical form is authoritative for those values.

### Create spare or create-and-install

Use one canonical idempotent physical-component create command with optional
`firearmId`, `installedAt`, and installation notes. Without a firearm, it creates an
`Inventory` component only. With a firearm, it atomically creates both the component
and installation and returns the component plus nullable installation result.

Keep the existing `/components/install` endpoint during a compatibility window for
already deployed clients, but move this route to the canonical create endpoint.
Both paths must share the same Domain orchestration rather than duplicate validation.
The create command must reject an archived definition or firearm and must recover
idempotently from partial projection persistence.

The regression for the reported firearm failure must submit the same firearm ID
returned by the route loader under the same authenticated account and assert
successful creation/installation. Add negative cases for blank, missing,
cross-account, and archived firearm IDs. The user-visible error must identify the
invalid selected firearm without exposing account existence.

### Edit, archive, and delete lifecycle

Add revision-checked, idempotent update and archive commands for definitions and
physical components. Archive keeps records readable for history, excludes them from
new component/installation selectors, and can be shown when the user chooses to view
archived records. Archive must not silently remove an active installation; the API
rejects archiving an installed component until it is removed.

Hard delete is deliberately constrained:

- a definition can be deleted only when no physical component references it;
- a physical component can be deleted only when it has never been installed, has no
  maintenance event, no ledger delta/operation reference, and no other historical
  reference;
- delete is account-scoped and revision-checked;
- a conflict returns a clear instruction to archive instead;
- deletion must never cascade into immutable operations, installations, maintenance,
  or activity history.

Extend `IWorkshopRepository` with the smallest account-scoped, revision-aware delete
primitive needed by these rules, implemented consistently for MongoDB and the mock
scenario. Domain code performs reference checks before repository deletion. Do not
expose a generic unguarded delete service.

### Components route UX

Keep the existing two-panel catalogue/inventory layout and incumbent visual system.
Both "New component definition" and "Add physical component" use explicit show/hide
buttons with accurate expanded state, `aria-expanded`, and stable control
relationships.

Definition cards/rows show category information and active/archived state only. Each
row exposes edit, archive, and delete actions; destructive actions require a
confirmation that explains when archive is safer. Physical inventory rows expose the
same management actions and show definition name, identification, manufacturer/model,
compatibility tags, tracking mode/threshold, accumulated rounds, installation status,
and archive state.

Place the compatibility-tag filter directly above the inventory list and below the
collapsible add form. Filter against each physical component's tags, case
insensitively. Keep filter state while fetcher mutations revalidate data and provide a
clear action plus an explicit no-match state.

The physical form labels the firearm as optional and explains that leaving it blank
adds a spare part to inventory. Hide installation-time fields when no firearm is
selected. Tracking mode controls whether the service-threshold field is shown and
required. All controls, actions, errors, confirmation copy, and status labels require
Italian translations in the existing i18n dictionary.

### Impeccable workflow

This is an established **Operate** surface and a refinement, not a redesign. Preserve
the owning application's black/white/yellow visual language and existing panel,
button, form, badge, and list conventions. During implementation, use Impeccable
`shape` only to confirm the final management-action hierarchy, then `harden` for
error/destructive/empty states, `adapt` for the two-panel layout on narrow screens,
and `audit` for accessible disclosure controls, labels, focus, keyboard operation,
and contrast.

Minimum visual evidence is the Components route at desktop and narrow mobile widths
with both forms collapsed and expanded, edit mode, archived rows, a filtered
no-result state, a server validation error, and destructive confirmation. Do not
create a replacement visual world.

## Impacted Projects

- **MorWalPizVideo.Models** — correct persisted ownership, revisions, archive state,
  operation types, and tolerant legacy fields.
- **MorWalPizVideo.Domain** — validation, idempotent commands, guarded deletion,
  persistence orchestration, and mock/Mongo repository behavior.
- **MorWalPiz.Contracts** — create/update/archive/delete request and response
  contracts.
- **MorWalPizVideo.ServerAPI** — authenticated, CSRF-protected account-scoped
  component endpoints.
- **MorWalPizVideo.BackOffice.Tests** — focused Workshop Domain/API behavior tests;
  this project currently owns Workshop backend tests despite its name.
- **frontend/fe-packages/models** — synchronized TypeScript contracts.
- **frontend/fe-packages/services** — endpoint constants and typed account transport
  functions.
- **frontend/shooter-workshop.client** — route loader/action, fetcher-based UI,
  translations, component-route tests, responsive/accessibility refinement.
- **MorWalPizVideo.AppHost / deployment surfaces** — verify only; no topology,
  configuration, or secret change is expected.
- **MorWalPizVideo.BackOffice, ShootingRange, VideoImporter, other SPAs** — not
  impacted; they do not consume Workshop component contracts.

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — move template fields
  out of new definition behavior, add physical compatibility/archive data, definition
  revision, and lifecycle operation types.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — align create and
  management request DTOs with corrected ownership and optional installation.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — canonical create,
  update/archive/delete commands, validation, idempotency, and reference guards.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — account/revision-scoped
  guarded delete support in Mongo and mock implementations.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — expose the
  management routes and keep CSRF/auth/problem response behavior consistent.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — Domain
  lifecycle, data-ownership, idempotency, and guarded-delete coverage.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` — verify archived
  or legacy component changes do not break replay/history when applicable.
- `frontend/fe-packages/models/src/workshop.ts` — corrected entities and request
  shapes.
- `frontend/fe-packages/services/src/endpoints-frontend.ts` — detail/archive endpoint
  constants.
- `frontend/fe-packages/services/src/workshopService.ts` — typed CRUD and canonical
  create transport functions.
- `frontend/fe-packages/services/src/workshopService.test.ts` — URL, payload, CSRF,
  optional-firearm, and identifier-encoding tests.
- `frontend/shooter-workshop.client/src/routes/Components/index.ts` — export route
  component, loader, and action.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` — consume
  loader data, use fetchers, correct forms/list/actions/filter placement.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — attach loader and action
  to the Components route.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — assert the route
  owns its loader/action as well as its path.
- `frontend/shooter-workshop.client/src/i18n.tsx` — all new/changed UI strings.
- `docs/architecture/shooter-workshop.md` — corrected target component ownership and
  lifecycle behavior.
- `docs/feature-handoffs/shooter-workshop-ui-feedback-handoff.md` — mark the earlier
  definition-tag/create-and-install assumptions as superseded by this accepted
  feedback.

### Add

- `frontend/shooter-workshop.client/src/routes/Components/loader.ts` — typed concurrent
  initial data read.
- `frontend/shooter-workshop.client/src/routes/Components/action.ts` — allow-listed
  mutation dispatcher and typed action results.
- Component-route test files following the application's existing Vitest/Testing
  Library naming convention — loader/action and user-interaction coverage.

### Generated/Migration

- No EF migration or generated client is involved.
- Add an idempotent MongoDB backfill owned by the repository's existing operations
  process only if deployed data contains definition-level compatibility tags. It
  copies normalized tags to existing physical instances that do not yet have their
  own tags and initializes missing revision/archive fields. It must not overwrite
  already populated physical tags.

### Documentation

- This handoff is the implementation source of truth for the accepted component
  feedback.
- The functional baseline and earlier UI-feedback handoff require the targeted
  corrections listed above.

### Verify Only

- `frontend/shooter-workshop.client/package.json` — React Router 7 already supports
  route loaders/actions/fetchers; no dependency change is expected.
- `frontend/shooter-workshop.client/src/components/WorkshopForm.tsx` — reuse only if
  it can become a presentational/fetcher-aware form without breaking other routes;
  otherwise keep route-local forms and do not broaden this task.
- `MorWalPizVideo.AppHost` and deployment workflows — confirm no runtime wiring
  change.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Add canonical optional-install create and revision/idempotency-aware definition/component management endpoints. Preserve the legacy create-and-install route temporarily. |
| Frontend/UI | Route loader/action architecture, independent fetchers, corrected forms, row actions, disclosure controls, and relocated filter. |
| Database/Data | Additive physical tags/archive and definition revision; tolerant reads plus optional idempotent backfill. Guarded hard delete only for unreferenced records. |
| Contracts/Models | Breaking semantic correction: definition fields shrink; physical component owns tracking/manufacturer/tags. Coordinate backend and shared TypeScript packages in one release. |
| Authentication/Authorization | Existing public-account cookie scheme, account scoping, and CSRF remain mandatory for every mutation. Cross-account records remain indistinguishable from missing records. |
| Validation | Domain is authoritative for ownership, active state, tracking/interval consistency, tags, revisions, archive, and delete eligibility. Client mirrors only for immediate guidance. |
| Cache | Not applicable. Workshop account reads are not output-cached and no shared cache tags are used. React Router revalidation refreshes browser route data. |
| Configuration/Secrets | Not applicable. No new setting, credential, connection string, or feature flag is required. |
| Logging/Telemetry | Preserve structured request telemetry. Add structured conflict/validation context without logging component notes, cookies, or account secrets. |
| Background Jobs | Not required. Any one-time Mongo backfill uses the established operational migration process, not Hangfire. |
| Deployment/Operations | Deploy backend compatibility first, then shared frontend packages and Workshop SPA. Run/audit backfill before removing legacy definition-tag reads. |
| Backward Compatibility | Existing documents must deserialize safely. Legacy `/components/install` remains available during the frontend rollout. Old definitions may retain ignored legacy fields until cleanup is separately approved. |

## Reuse Opportunities

- Reuse the Firearm update/archive pattern in `WorkshopService` for revision checks,
  idempotency hashing, conflicts, and account ownership.
- Reuse `NormalizeTags` for physical compatibility tags rather than introducing a
  second normalization rule.
- Reuse `accountApiService` and `requireSuccessfulResponse`; all component mutations
  must retain cookie credentials and CSRF acquisition.
- Reuse the current route's catalogue/inventory composition, layout classes,
  `PageTitle`, `EmptyState`, badges, and i18n utilities.
- Reuse React Router's automatic loader revalidation after fetcher actions instead of
  retaining manual resource reload callbacks.
- Reuse existing Workshop mock scenarios and WebApplicationFactory authentication/
  CSRF patterns for API behavior tests.

## Risks

- **High — historical data loss:** unrestricted delete could orphan installation,
  maintenance, ledger, or activity history. Mitigate with Domain reference checks,
  account/revision constraints, no cascades, and tests proving referenced records
  return conflict.
- **High — semantic migration mismatch:** backend and frontend may disagree while
  fields move from definitions to physical instances. Mitigate with additive backend
  deployment, tolerant reads, backfill, compatibility endpoint window, and contract
  tests.
- **Medium — reported firearm failure persists:** changing form architecture without
  reproducing the actual same-account request can mask rather than fix the defect.
  Mitigate with an authenticated API regression using a loader-returned firearm ID
  and browser/component test asserting the submitted form data.
- **Medium — lost edits during revalidation:** multiple fetchers can reset local state
  if success/error transitions are not scoped. Mitigate with per-row/per-form
  fetchers and success-specific reset tests.
- **Medium — archive/status ambiguity:** treating archive as `Retired` would corrupt
  physical lifecycle meaning. Mitigate with an explicit archived field and separate
  validation/display.
- **Medium — stale legacy tags:** copying definition tags can create misleading
  physical compatibility. Mitigate with non-overwriting backfill, user-editable
  physical tags, audit counts, and a rollback snapshot.
- **Low — responsive action clutter:** three actions per row can overwhelm narrow
  layouts. Mitigate with an accessible action group/menu that preserves keyboard
  visibility and destructive distinction.
- **Low — external services:** none are involved; component workflows use only the
  account API and MongoDB.

## Technical Debt

- The Components route currently follows the same `useResource`/direct-mutation
  pattern as other Workshop routes. Convert only this route now; a broad route-data
  migration is non-blocking and deferred.
- `WorkshopForm` combines presentation, request execution, and error mapping. Do not
  redesign every form. Adapt it narrowly only if compatibility is preserved;
  otherwise use route-local fetcher forms.
- Persistence entities are returned directly by several Workshop endpoints. Dedicated
  response DTOs are desirable, but this task should add only the contracts needed to
  prevent definition/instance ambiguity.
- The repository lacks a delete primitive. Add the constrained capability required
  here, not a broad generic CRUD abstraction exposed above Domain policy.
- The earlier UI-feedback handoff encoded definition-owned compatibility and required
  immediate installation. This feedback supersedes only those component decisions;
  its unrelated accepted work remains intact.

## Implementation Roadmap

1. **Lock regression behavior and compatibility rules.** Add backend tests that
   reproduce create-with-loader-firearm, create-spare, cross-account/missing/archived
   firearm rejection, and current legacy document reads. Gate: tests fail for the
   intended missing behavior and distinguish the reported error's source.
2. **Correct persistence and contracts additively.** Add physical tags/archive,
   definition revision, corrected request DTOs, canonical optional-install response,
   and new operation types. Retain tolerant legacy fields for reads. Gate: old
   documents deserialize and new contract validation rejects invalid tracking/interval
   combinations.
3. **Implement Domain lifecycle policy.** Add canonical idempotent create, update,
   archive, restore if product-confirmed, and guarded delete methods; remove definition
   inheritance for new physical writes. Extend repository deletion narrowly. Gate:
   service tests prove revision conflicts, idempotent retries, no partial duplicate
   installation, and no deletion of referenced history.
4. **Expose compatible account API endpoints.** Add CSRF-protected update/archive/
   delete routes and move canonical creation to optional installation while retaining
   legacy `/install`. Gate: authenticated integration tests cover status/problem
   shapes, account isolation, CSRF, and identifier routing.
5. **Synchronize shared frontend packages.** Update TypeScript models, endpoint
   constants, service methods, and transport tests. Gate: shared package type-checks
   and service tests prove exact payloads and optional firearm omission.
6. **Add Components loader and action.** Fetch the three datasets concurrently,
   implement allow-listed intents, map expected API errors, and attach both to route
   configuration. Gate: loader/action unit tests and route-configuration test pass.
7. **Refine the Components UI.** Replace `useResource` reads/direct mutation calls
   with `useLoaderData` and independent fetchers; implement corrected fields,
   optional firearm, both show/hide controls, edit/archive/delete actions, and filter
   placement. Gate: component tests cover spare creation, installed creation,
   successful revalidation, retained values/errors on failure, and destructive
   confirmations.
8. **Complete localization and visual hardening.** Add Italian copy and run the
   Impeccable Operate refinement checks described above. Gate: keyboard-only
   disclosure/actions, labelled errors, focus behavior, mobile layout, archived
   visibility, and filter empty state have captured evidence.
9. **Migrate and deploy safely.** Deploy backend compatibility, audit/backfill legacy
   tags and defaults, then deploy shared packages and SPA. Gate: production-like
   smoke tests create one spare and one installed component, edit/archive/delete only
   eligible records, and confirm loader revalidation.
10. **Update durable documentation.** Reconcile this handoff and the functional
    baseline with actual files, validation, migration evidence, and residual risks.
    Stop after this component scope; do not migrate unrelated Workshop routes.

An independent post-validation review is warranted because this changes persistence
semantics, destructive operations, account isolation, and a multi-package API
contract. The user may select `MorWalPiz Code Reviewer` after implementation.

## Testing Strategy

- **Domain/unit — `MorWalPizVideo.BackOffice.Tests`:** corrected ownership, tag
  normalization, tracking/interval validation, spare versus installed creation,
  idempotent retries, revisions, archive restrictions, and every guarded-delete
  reference type.
- **API/integration — `MorWalPizVideo.BackOffice.Tests`:** loader-equivalent reads
  followed by same-account mutations, missing/cross-account resources, archived
  selectors, CSRF rejection, malformed intents/DTOs, concurrency conflict, and legacy
  endpoint compatibility.
- **Repository/Mongo behavior:** account-scoped revision delete, race between reference
  check and delete, mock parity, legacy BSON fields, and idempotent backfill.
- **Shared service contract — `frontend/fe-packages/services`:** exact routes,
  encoded IDs, CSRF headers, create payload with and without firearm, update/archive/
  delete payloads, and ProblemDetails propagation.
- **Route loader/action — Workshop SPA:** concurrent successful load, one rejected
  dependency, allow-listed intents, malformed form data, typed error response, and
  successful revalidation.
- **Component — Workshop SPA:** show/hide buttons, required definition, optional
  firearm explanation, conditional threshold, edit prefill, independent pending
  states, archive/delete confirmation, conflict guidance, filter location/behavior,
  archived display, and translated accessible names.
- **Security:** no cross-account probing, CSRF on every mutation including delete,
  no user-entered notes/tags in logs, and no ability to delete referenced history.
- **Migration:** dry-run counts, copy-only-empty physical tags, rerun idempotency,
  mixed old/new document reads, rollback snapshot, and no mutation of ledger facts.
- **End-to-end:** authenticate, navigate directly to `/components`, observe loader
  data, create a spare, create an installed component from a loader-returned firearm,
  edit both record types, archive eligible records, reject ineligible deletion, delete
  pristine records, filter inventory, and hard reload.

## Migration And Rollback

Use an additive MongoDB compatibility window. New fields default safely when absent:
definition revision `0`, physical compatibility tags empty, and physical archived
`false`. Continue reading legacy definition fields while the backfill and frontend
rollout complete, but stop writing them from new definition commands.

If deployed definitions contain compatibility tags, snapshot/audit affected accounts
and run an idempotent backfill that copies normalized tags only to physical components
whose own tag list is absent or empty. Manufacturer/tracking/interval legacy defaults
must not be bulk-copied without separate user approval because the feedback establishes
instance-specific ownership and an automatic copy could assert inaccurate physical
facts.

Deployment order is Models/Domain/Contracts/ServerAPI first, operational backfill
second, shared frontend packages third, Workshop SPA last. Monitor validation/conflict
rates for component create/update and specifically the firearm-not-found response.
Keep `/components/install` until telemetry confirms no old client dependency.

Rollback the SPA first to the prior client while the additive backend remains
compatible. Roll back backend code only while retaining additive BSON fields; MongoDB
does not require removing unknown fields. Restore data from the migration snapshot if
the tag backfill is wrong. Hard deletes cannot be undone without backup, which is why
the first release must enforce pristine-only deletion and operational backups. No
SQLite, cache, auth, secret, or deployment-topology migration is required.

## Open Questions

None blocking. This handoff resolves the remaining delivery choices as follows:
archive is one-way in this scope (matching existing firearm behavior), definition
description and notes remain separate for compatibility, and the UI label is
"Maximum rounds" with supporting text explaining that it is the service threshold.
A restore workflow, field consolidation, or terminology change requires separate user
approval.

## Implementation Status

Implemented in the current worktree:

- Added Components route `loader.ts` and `action.ts`; initial definitions, inventory,
  and firearms load concurrently, while mutations are dispatched through React Router
  fetchers and trigger normal route revalidation.
- Reworked the Components UI to make both forms explicit show/hide disclosures,
  move compatibility filtering above the inventory list, make firearm selection
  optional for spare parts, and expose edit/archive/delete controls.
- Added physical compatibility tags and archive state, definition/component revision
  support, guarded account-scoped deletion, and optional-firearm create-and-install
  handling in Models, Contracts, Domain, repository, and ServerAPI.
- Preserved legacy definition fields and the existing `/components/install` endpoint
  for rollout compatibility; new UI writes physical tracking/manufacturer/model/tags
  and does not send definition-level configuration.
- Added the required Italian UI strings for the new component controls and states.

Validation completed:

- `dotnet build MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~WorkshopServiceTests"` — 7 passed
- `cd frontend && yarn workspace shooter-workshop.client build`
- `cd frontend && yarn workspace shooter-workshop.client test --run src/routes/config.test.tsx`

Residual rollout risk: the existing legacy definition documents may still contain
manufacturer, tracking, interval, and compatibility fields. They remain tolerated
but are no longer used by new physical-component writes. A production data audit and
non-destructive backfill for physical tags remain operational follow-up work.

