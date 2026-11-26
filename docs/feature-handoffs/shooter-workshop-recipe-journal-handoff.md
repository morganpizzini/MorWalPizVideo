# Recipe journal handoff

Status: proposed, awaiting user review. This is a plan, not an implementation.

## Outcome

Add a private, manual-entry journal table to each recipe's existing details.
An entry records the date and any user-supplied time of day, weather,
temperature, power factor and notes. Display the saved entries for that recipe.
There are no calculations, assessments, recommendations or automatic weather
lookups.

Assumptions for review: entries are append-only in this first slice; a date
and at least one other field are required; users may leave unknown fields
empty. The journal requires an online connection. These assumptions can be
changed before implementation.

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

## Proposed Design

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

## Implementation Roadmap

1. Agree on the assumptions and fields. Confirm that the journal is descriptive
   only and that append-only/online-only behavior is acceptable.
2. Add the separate document and DTOs, scoped query and index manifest. Verify
   old recipe documents and old clients remain untouched.
3. Implement validation, ownership, single-insert idempotency and authenticated
   endpoints. Gate on focused service, retry, isolation and CSRF tests.
4. Add shared client contracts and services, then route-owned loading/actions
   and the localized table/form. Gate on route and component tests and shared
   package builds in dependency order.
5. Update the Workshop architecture and this handoff. Verify desktop and mobile
   layouts, keyboard operation, labels, pending/error/empty states, both
   languages and dark/light themes.
6. Request an independent post-validation review: this change crosses
   authentication, persistence and retry boundaries. Stop after the agreed
   journal scope; additional features need separate approval.

Impeccable implementation lens: **Operate**, existing-surface refinement;
use the narrow `shape` workflow for the entry form/table, preserving incumbent
visual conventions. Capture one desktop and one narrow-screen rendered view,
including empty, populated and error states, and check keyboard/accessibility.
Do not redesign the application or introduce design artifacts solely for this
feature.

## Testing Strategy

- Service tests: round-trip all supplied fields, missing optional fields,
  negative temperature, invalid date/decimal/unit, empty payload, archive
  policy, recipe changes, cross-account access and idempotent retry/conflict.
  Include real-provider concurrent insert/index proof where available; mocks
  alone cannot prove Mongo uniqueness behavior.
- API integration: authenticated read/create, unauthorized access, missing CSRF,
  another account's recipe, malformed request and conflict response.
- Frontend route/component: URL selection and revalidation, table order and
  empty state, all form fields, validation, failure preserving draft, retry key,
  successful refresh, keyboard interaction and Italian/English labels.
- Contract: preserve supplied decimal text, temperature unit and calendar date
  across serialization; check additive compatibility with old clients.
- End-to-end: signed-in user adds and reloads an entry; a second account cannot
  see it; verify network loss is visible. No data migration test is required,
  but exercise an existing recipe with no journal documents.

## Migration And Rollback

No Mongo, SQLite, authentication or existing-contract migration is required.
Apply the optional query index through the established reviewed operations
surface; its absence must not change authorization or data correctness. Deploy
API before UI. On rollback, remove the UI first; leave the new collection
intact and restore the API to recover access. Do not delete user-entered data
as a rollback shortcut. No offline outbox, feature flag or configuration
migration is introduced.

## Open Questions

1. Is append-only acceptable, or must users be able to edit/delete mistaken
   entries in the first release?
2. Is an online-only journal acceptable, and should temperature support both
   Celsius and Fahrenheit?
3. Should an entry identify a specific historical recipe version, rather than
   only the recipe? A backdated date alone cannot establish that version.
