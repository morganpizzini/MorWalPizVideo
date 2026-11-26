# Recipe journal handoff

Status: implemented under the assumptions recorded below: append-only,
online-only, and associated with the recipe rather than a historical version.

## Outcome

Each recipe's details include a private, manual-entry journal table. Entries
record the date and optional time of day, weather, temperature and unit, power
factor, and notes. Saved entries are displayed for that recipe. There are no
calculations, assessments, recommendations or automatic weather lookups.

Entries are append-only in this slice. A date and at least one other field are
required; unknown fields may be left empty. The journal requires an online
connection. Product decisions about these assumptions are listed at the end.

## Current Evidence

- The recipes tab is inside the Ammunition route. The recipe detail dialog and
  its transient selection state are in
  [Recipes/component.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx);
  the route's actual loader and action are in
  [Ammunition/loader.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts)
  and [Ammunition/action.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/action.ts).
  [config.tsx](../../frontend/shooter-workshop.client/src/routes/config.tsx)
  redirects `/recipes` to that tab.
- [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs)
  uses public-account authentication, derives account identity on the server
  and checks CSRF on mutations. The recipe service and account-filtered Mongo
  and mock repositories are in
  [WorkshopService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopService.cs)
  and [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs).
- [WorkshopEntities.cs](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs)
  stores versioned recipes; production records retain snapshots.
  Recipe edits must not rewrite a journal's historical entries.
- [workshop.ts](../../frontend/fe-packages/models/src/workshop.ts) and
  [workshopService.ts](../../frontend/fe-packages/services/src/workshopService.ts)
  own the shared frontend contract and account-cookie/CSRF transport.
  [shooter-workshop.md](../architecture/shooter-workshop.md) describes the
  recordkeeping-only boundary and existing online/offline scope.
- The Workshop UI translates English and Italian through
  [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx).
  No Workshop-specific PRODUCT.md or DESIGN.md was found in the inspected tree;
  the existing application UI is the visual authority.

## Implemented Design

### Data and interaction

- Add a journal section inside the existing recipe details dialog, with a
  chronological table, an empty state and an add-entry form. Preserve current
  recipe actions and styling. Use existing dialog, form and feedback patterns.
- Store a calendar date, not a UTC instant; do not infer a date from a timezone.
  The date determines the time of year. Time of day is an optional explicit
  choice: morning, noon, afternoon or evening. Weather is optional free text.
  Temperature is an optional user-entered decimal string plus an explicit C
  or F unit, required together. Power factor is an optional user-entered
  decimal string; notes are optional text. Display values as supplied without
  conversion or calculation. Apply explicit reasonable length bounds, validate
  real dates and decimal syntax, allow negative temperatures and reject
  malformed entries; do not impose technical performance thresholds.
- Link each record to its owning account and recipe. Do not silently claim
  that a backdated entry represents the recipe's version at the event date.
  Journal entries remain associated with the recipe across edits and archive;
  creating a new entry for an archived recipe is disabled. Read access remains
  available for the owner's archived recipes.
- Keep the selected recipe in a URL search parameter on the existing
  Ammunition route, retaining the selected tab and other parameters. Its loader
  fetches entries only when a recipe is selected; an account-scoped,
  recipe-filtered server query returns newest dates first with a stable
  tie-breaker. Selection, closing and normal mutation revalidation must work
  without remounting an in-progress form or hiding a load error. Invalid or
  inaccessible selections must not expose another account's data.
- Submit a distinct journal intent through the Ammunition action and the shared
  account transport. Retain the same per-submission idempotency key across a
  retry of an uncertain submission; issue a new key only for a new entry.
  Server validation and explicit error responses are authoritative.

### Persistence and retry boundary

Use a separate account-owned journal collection and a single immutable document
per entry. These records have no inventory or counter effects, so they are not
replayable Workshop projection deltas and do not belong in the operation
ledger or activity feed. Document this narrow boundary in the Workshop
architecture. Reuse the service's stable account/key-derived identifier with
a journal-specific purpose, and store the normalized request identity in the
document. A single insert is the durability boundary: on duplicate identifier,
read the account-owned record and return it only for the same recipe and
payload; return a conflict for a different payload. Handle concurrent retries
and ambiguous network failures without duplicate records or a success-shaped
fallback. Implement the same semantics in the mock repository. Do not claim
that a multi-document operation and projection write is atomic.

Add a non-unique account/recipe/date index to the existing Workshop index
manifest. The document identifier already enforces single-document uniqueness;
the query index supports the expected read path, not authorization.

## Impacted Projects

- ServerAPI, Domain, Models and Contracts: authenticated API, isolated storage,
  contract and validation.
- `shooter-workshop.client`, shared models and services: route-bound journal UI
  and transport. Other shared-package consumers receive additive exports only.
- BackOffice.Tests: service, account endpoint and index-manifest coverage.
  BackOffice's central Mongo index operations are the only operational coupling;
  no BackOffice product feature is added.
- Deployment: deploy the compatible API before the client and review/apply
  the index through the existing operations process.

## Impacted Files

### Modify

- [WorkshopEntities.cs](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs)
  and [DbCollections.cs](../../MorWalPizVideo.Models/Constraints/DbCollections.cs):
  journal document and collection.
- [WorkshopContracts.cs](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs):
  request and response DTOs.
- [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs):
  account-and-recipe query, duplicate-insert handling, mock ownership and index.
- [WorkshopService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopService.cs):
  ownership checks, validation, stable identity and retry behavior.
- [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs):
  authenticated recipe-scoped list and create endpoints with CSRF on create.
- [workshop.ts](../../frontend/fe-packages/models/src/workshop.ts),
  [index.ts](../../frontend/fe-packages/models/src/index.ts),
  [workshopService.ts](../../frontend/fe-packages/services/src/workshopService.ts),
  [endpoints-frontend.ts](../../frontend/fe-packages/services/src/endpoints-frontend.ts)
  and [index.ts](../../frontend/fe-packages/services/src/index.ts): additive
  models, endpoint definitions and transport exports.
- [Ammunition/loader.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts),
  [Ammunition/action.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/action.ts),
  [Ammunition/component.tsx](../../frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx)
  and [Recipes/component.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx):
  selection, route-owned loading, submission and details UI.
- [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx): English and
  Italian labels and feedback; extend existing app styles only as needed.
- [WorkshopServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs),
  [PublicAccountEndpointTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs),
  [MongoIndexOperationsTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs)
  and [Ammunition/route.test.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts):
  focused behavior and authorization coverage.

### Add

- Focused recipe journal component tests beside the existing recipe route;
  extract an app-local journal component only if it keeps the dialog readable.

### Generated/Migration

- No generated migration or backfill. The collection is additive; existing
  recipes have no entries.

### Documentation

- Update [shooter-workshop.md](../architecture/shooter-workshop.md) with the
  journal's storage, manual-data, online-only and no-calculation boundaries.
  Update the backend architecture and record the direct-document exception
  to the Workshop ledger policy in an ADR.
- Update this handoff after implementation with actual decisions, changed
  files, validation results and residual risks.

### Verify Only

- [config.tsx](../../frontend/shooter-workshop.client/src/routes/config.tsx)
  for routing; [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts)
  for account transport; [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs)
  for manifest propagation; shared-package consumers for additive compatibility.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Add account-scoped list/create; no existing route changes. |
| Frontend/UI | Add a table and form to the current recipe detail; refinement, not redesign. |
| Database/Data | New Mongo collection and query index; immutable records and no backfill. |
| Contracts/Models | Additive DTOs and shared TypeScript types; no recipe contract changes. |
| Authentication/Authorization | Existing public-account cookie and CSRF; check recipe ownership on every read and write. |
| Validation | Validate date, optional text lengths, decimal syntax, unit pairing, time choice and at least one data field. No performance judgments. |
| Cache | No new cache or invalidation; route revalidation refreshes saved entries. |
| Configuration/Secrets | No new settings or secrets. |
| Logging/Telemetry | Existing host error logging; no journal values, identifiers or notes in logs. |
| Background Jobs | None. |
| Deployment/Operations | API first; BackOffice attempts source-defined indexes at startup without blocking application availability. |
| Backward Compatibility | Additive API and collection. Old clients and existing documents remain unchanged. |

## Reuse Opportunities

Reuse account transport, CSRF, existing Workshop account-filtered repository
patterns, `StableId`, shared Dialog, route loader/action/fetcher boundaries,
translations and test fixtures. Avoid another HTTP client, direct component
fetching, a second weather service, or a new shared UI abstraction.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
|---|---|---|
| High | Cross-account recipe or journal access leaks private records. | Filter by server-derived account and recipe on both endpoints; endpoint tests prove unauthenticated, missing CSRF and cross-account denial. |
| Medium | A network retry or concurrent request creates duplicates or loses a save. | One deterministic document per key and explicit conflict recovery; test duplicate, changed payload, concurrent and response-loss retry. |
| Medium | A backdated entry is incorrectly attributed to a recipe version. | Associate with the recipe only; never infer historical version. Test edits and old dates preserve entries unchanged. |
| Medium | Index or API rollout mismatch degrades the new UI. | Review index manifest, deploy API first, test real account in a deployed environment, and roll back client first. |
| Low | Offline use looks successful without saving. | Keep online-only, display failure and retain form contents; test unavailable network. |
| Low | Large journals make unbounded reads slow. | Query only the selected account and recipe using the index; monitor realistic volumes and introduce pagination separately if needed. |

## Technical Debt

- The current generic repository list method filters only by account; add the
  narrow recipe query rather than fetching all users' journal entries or
  generalizing the entire repository. Existing unrelated query performance is
  deferred.
- Existing recipe version history is not exposed here. Historical version
  selection, editing/deleting entries, exports, analytics and offline sync are
  outside this slice.

## Implementation Record

### Actual files changed

- Persistence, service and API: [DbCollections.cs](../../MorWalPizVideo.Models/Constraints/DbCollections.cs),
  [WorkshopEntities.cs](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs),
  [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs),
  [WorkshopService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopService.cs),
  [PrimaryScenario.cs](../../MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs),
  [WorkshopContracts.cs](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs),
  and [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs).
- Shared frontend contract and transport: [workshop.ts](../../frontend/fe-packages/models/src/workshop.ts),
  [models index.ts](../../frontend/fe-packages/models/src/index.ts),
  [endpoints-frontend.ts](../../frontend/fe-packages/services/src/endpoints-frontend.ts),
  [workshopService.ts](../../frontend/fe-packages/services/src/workshopService.ts),
  and [services index.ts](../../frontend/fe-packages/services/src/index.ts).
- Route and UI: [Ammunition loader](../../frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts),
  [Ammunition action](../../frontend/shooter-workshop.client/src/routes/Ammunition/action.ts),
  [Ammunition component](../../frontend/shooter-workshop.client/src/routes/Ammunition/component.tsx),
  [Recipes action](../../frontend/shooter-workshop.client/src/routes/Recipes/action.ts),
  [Recipes component](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx),
  [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx),
  and [main.scss](../../frontend/shooter-workshop.client/src/main.scss).
- Focused tests: [WorkshopServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs),
  [PublicAccountEndpointTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs),
  [MongoIndexOperationsTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs),
  [Ammunition route tests](../../frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts),
  [Recipes route tests](../../frontend/shooter-workshop.client/src/routes/Recipes/route.test.ts),
  and [Recipes component tests](../../frontend/shooter-workshop.client/src/routes/Recipes/component.test.tsx).
- Architecture: [shooter-workshop.md](../architecture/shooter-workshop.md),
  [architecture-decisions.md](../architecture/architecture-decisions.md),
  [README.md](../architecture/README.md),
  and [ADR-030](../architecture/adr/ADR-030-shooter-workshop-recipe-journal.md).

### Validation performed

- BackOffice.Tests Release build passed with zero errors; four focused
  `Recipe_journal` tests passed.
- `yarn workspace @morwalpizvideo/models build` passed.
- `yarn workspace @morwalpizvideo/services build` passed.
- Focused Shooter Workshop tests passed: 3 files, 19 tests, covering route
  loading/submission, URL selection, journal display, archived recipes, and
  retry-key/draft retention.
- `yarn workspace shooter-workshop.client build` passed, including TypeScript
  compilation and the Vite production build.

### Not verified / residual risks

- No live MongoDB provider test or deployed index audit/application was
  performed. The source index is non-unique; deterministic document IDs
  implement idempotency.
- No authenticated browser capture or live same-origin API session was run.
  The existing Playwright gate requires a local ServerAPI started with
  `EnableMock=true`.
- The backend build emitted existing package, nullable and analyzer warnings,
  including advisories for the existing `SixLabors.ImageSharp` version; these
  were not changed as part of this feature.

Impeccable implementation lens: **Operate**, existing-surface refinement.
The form/table follows the existing application visual conventions. Automated
component tests and the production build passed; desktop/mobile rendered
captures and visual theme checks remain outstanding.

## Migration And Rollback

No Mongo, SQLite, authentication or existing-contract migration is required.
Apply the optional query index through the established reviewed operations
surface; its absence must not change authorization or data correctness. Deploy
API before UI. On rollback, remove the UI first; leave the new collection
intact and restore the API to recover access. Do not delete user-entered data
as a rollback shortcut. No offline outbox, feature flag or configuration
migration is introduced.

## Product decisions retained as assumptions

1. The first release is append-only. Editing or deleting entries would be a
   separate behavior change.
2. The journal is online-only and supports Celsius or Fahrenheit, without
   conversion.
3. Entries identify the recipe, not a historical recipe version. A backdated
   date alone cannot establish that version.
