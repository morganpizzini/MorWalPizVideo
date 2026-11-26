# Shooter Workshop finance and supplies inventory handoff

Status: implementation complete; release and data contraction are not performed.
The inventory and finance feature, production editing, client-side Drawing cleanup
and a dry-run-first server migration are implemented. No deployment, index rollout,
or destructive Drawing contraction is authorized by this handoff.

## Revised consistency assumptions (2026-11-25)

- The user expects online operations by one person and does not require the
  previously proposed account-wide coordination protocol or stale-worker proof.
  Concurrent-write consistency is not guaranteed.
- The user accepts the risk that a server interruption during multiple writes can
  leave partially updated records. The interface should inform the user of a
  possible incomplete operation and offer a way to correct existing records. A user
  can edit an existing production record; editing it must update its recorded
  quantity, lot consumption and produced-batch counter, and preserve a correction
  history.
- The user expects a failed save not to count. A lost response or timeout alone does
  not establish that the server failed to save: an operation may already have been
  recorded or partially applied. Distinguish confirmed rejection from an unknown
  outcome; do not tell the user that an uncertain operation failed or safely retry
  it with a new identity. A key alone cannot guarantee atomic multi-document writes.
- Where the original proposal below requires a recoverable all-or-nothing operation,
  account-wide gating, stale-helper fencing, or standalone-Mongo fault-injection
  proof as a release prerequisite, those requirements are superseded by these
  assumptions. Do not interpret risk acceptance as a change to account isolation,
  normal insufficient-stock checks, expense semantics, or Drawing-removal scope.

## Delivered implementation and verification

- ServerAPI and Domain now provide account-scoped products, purchase lots,
  component purchase-price history, manual stock corrections/status, finance
  summaries, and production record reads, creation and editing. Production edits
  keep recipe identity and the historical recipe snapshot fixed, update lot and
  batch effects, enforce already-consumed quantity limits, and retain correction
  history.
- Frontend shared contracts and account transport call those endpoints. The
  Workshop SPA includes the online-only inventory screen and dashboard summary,
  recipe product selection, FIFO lot allocation, production create/edit forms,
  uncertain-save messaging, and independent inventory loading/error states.
  Recipe data stays viewable while inventory is slow, offline or unavailable.
- Inventory is not added to offline snapshots, queued commands, or cached API
  responses. Client recipe/snapshot types and UI no longer expose Drawing; legacy
  Drawing form input is not used as Bullet form. The server-side Drawing migration
  is separate, dry-run-first, and has not been run.
- The accepted partial-write limitation is documented in the owning architecture
  guide and [ADR-027](../architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md).
  There is no account-wide coordination gate or stale-worker requirement. A lost
  response is treated as an unknown outcome, not a confirmed failed save.

Verification performed:

- Workshop SPA full suite: `yarn workspace shooter-workshop.client test --passWithNoTests=false` — 29 files, 89 tests passed.
- Workshop SPA TypeScript: `yarn workspace shooter-workshop.client exec tsc --noEmit -p tsconfig.app.json` — passed.
- Workshop API service test: `yarn workspace @morwalpizvideo/services test src/workshopService.test.ts` — 13 tests passed.
- Shared package builds: `yarn workspace @morwalpizvideo/models build` and `yarn workspace @morwalpizvideo/services build` — passed.
- Backend build: `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — succeeded.
- Backend focused tests: `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~WorkshopServiceTests"` — 23 tests passed.
- The Vite production bundle completed successfully. No authenticated browser or visual-capture run was performed.

Remaining release/operations work:

- Review and audit/apply the feature-owned MongoDB indexes through the existing
  operations process, then deploy coordinated API and client versions.
- Verify the authenticated browser flow at the deployed Workshop origin and
  complete deployment/rollback evidence.
- The Drawing contraction requires an independently verified backup, a Workshop
  write freeze, and explicit operational approval. Do not execute it as part of
  normal application deployment.
- The known multi-document partial-write and overlapping-write risks remain
  accepted and are not proven away by mock-repository tests. Users must inspect
  records and stock after an uncertain result and correct existing production
  records when needed.

## Outcome

Add private, account-owned purchase and supplies records to Shooter Workshop. Users
can record the purchase price of a physical component; record purchases of primers,
bullets, powder containers, cases and commercial ammunition; choose a primer and powder in a recipe;
select bullets when recording production; view and correct recorded quantities; and
see an online inventory summary on the dashboard. Production reduces primer and
bullet stock, but never automatically reduces powder, case or commercial-ammunition
stock. Commercial ammunition is manually marked exhausted. Remove Drawing entirely,
including persisted values; Bullet form is the only field.

Confirmed: offline support is focused on range activities; the new supplies and
commercial-ammunition inventory is online-only. Users select the oldest available
purchases, and powder/case costs are recognized at purchase time as described below.
Product identity remains separate from individual purchases, and currency is recorded
per purchase. Produced ammunition continues to have its existing quantity counter
without an intrinsic price.
The user confirms no target MongoDB deployment supports transactions.
This is recordkeeping, not advice about ammunition, compatibility, loading
parameters, procedures, or safety.

### Observable acceptance

- A component can be created with a purchase price, and an existing or offline-created
  component can have missing purchase information entered later. Expense history
  does not disappear if the component is archived or deleted.
- A purchase records its item type, brand/name or number, purchase date, quantity
  and total price and currency. Multiple purchases of the same item remain
  distinguishable, including their different prices. Existing unrecorded finance
  starts at zero; a zero baseline is not a claim about historical spending.
- A recipe selects a primer product and a powder product. A production record
  explicitly selects a bullet product and oldest available primer and bullet
  purchase lots. Recording 100 units consumes 100 selected primer units and 100
  selected bullet units and creates the existing produced batch. A user can edit an
  existing production record's date, quantity, notes, bullet product and lot
  selections; its recipe identity stays fixed to preserve shooting-history links.
  The edit updates lot consumption and produced-batch counter and preserves
  correction history. It cannot reduce quantity below units already consumed. No
  automatic powder, case or commercial-ammunition deduction occurs. A recipe
  missing required references prompts its owner to update it before production.
- Stock cannot go negative; insufficient stock, stale versions, foreign-account IDs,
  duplicate retries and uncertain responses are handled explicitly. A user can
  record a corrected physical count, with an auditable reason, without changing
  purchase spending. Powder, cases and commercial-ammunition packages can be marked
  empty or reopened manually; produced-ammunition batches retain their own counter.
- Show expenses and a separate partial consumption-cost view, grouped by currency.
  Primer and bullet cost is attributed as recorded stock is consumed; powder and case
  purchase cost is recognized when the purchase is recorded, not estimated from
  production. Never add those figures twice or present them as a complete unit cost.
- The new inventory, its dashboard widget and stock mutations are unavailable
  offline; they are not saved to the offline snapshot, command queue or service
  worker cache. Existing offline shooting, components and existing ammunition
  behavior must continue to work.
- Drawing is removed from the UI, API, types and persisted recipe and snapshot data;
  no Drawing-to-Bullet-form migration or fallback is performed. Users are prompted
  to fill missing required fields rather than having their values guessed.

## Implementation References

- [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs)
  owns `/api/v1/workshop`, uses the public-account scheme, derives the account from
  the authenticated identity and checks CSRF on mutations. The feature is not a
  BackOffice product.
- [WorkshopEntities.cs](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs),
  [WorkshopContracts.cs](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs)
  and [WorkshopInventoryService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopInventoryService.cs)
  own the inventory records, API contracts and inventory/production behavior.
  [WorkshopService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopService.cs),
  [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs)
  and [WorkshopReplay.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs)
  integrate production, corrections, persistence and existing replay behavior.
- [WorkshopReplay.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs)
  rebuilds projections from operation deltas. The accepted
  [ADR-027](../architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md)
  records the accepted no-transaction/no-account-gate assumption and the known
  partial-write and overlapping-write limitations.
- [ProductionForm.tsx](../../frontend/shooter-workshop.client/src/routes/ProductionForm.tsx),
  [Recipes/component.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx),
  [RecipeProduction/component.tsx](../../frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx)
  and [AmmunitionPanel.tsx](../../frontend/shooter-workshop.client/src/routes/Ammunition/AmmunitionPanel.tsx)
  implement recipe product references, FIFO lot allocation and shared production
  create/edit behavior.
- The [dashboard loader](../../frontend/shooter-workshop.client/src/routes/Dashboard/loader.ts)
  and [dashboard component](../../frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx)
  include a separately loaded online inventory summary.
- [offlineStore.ts](../../frontend/shooter-workshop.client/src/services/offlineStore.ts)
  stores recipes, produced batches, component and other datasets, and queues shooting,
  component and maintenance operations. Inventory status and prices are not added
  to offline datasets or the command queue; the existing service worker bypasses API
  requests.
- [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs)
  filters reads by account and owns feature index definitions for the existing central
  manifest. Index audit/apply remains an operational release step.
- [shooter-workshop.md](../architecture/shooter-workshop.md) is the owning feature
  document. Current code, not its historical target sections, determines present
  behavior. There is no Workshop-local `PRODUCT.md` or `DESIGN.md` in the tree.

## Accepted Design and Scope

### Boundaries and identity

Keep all behavior under the existing public-account Workshop boundary. Add an
account-owned **supply product** (type, brand and user-defined product/name/number)
and separate **purchase lots**. A product can have multiple lots at different
prices. Do not equate products by free text or brand alone: references use stable
account-owned IDs, and similar names need not mean the same product. Keep physical
components separate from supplies; component purchase facts refer to the component
instance, not its reusable definition.

Track primer and bullet quantities as nonnegative whole numbers. Represent powder,
cases and commercial ammunition as individually identifiable purchased packages
with manually controlled available/empty status; cases may also have an explicitly
corrected item count. Empty status does not infer a rate of consumption. Keep
zero-count/empty lots and their purchase facts for history. Archive products rather
than deleting referenced purchase or production evidence.

Use an additive purchase record with purchase date, original quantity, total
purchase amount and ISO currency. Require price for new online purchases; explicit
zero means free. For existing data and offline-created components without a price,
financial totals start at zero and the UI identifies the missing price and prompts
for an update. Do not invent historical purchases or imply zero is known historical
spending. Store exact decimal or integer minor units, with server validation and
deterministic rounding; never use floating-point as the accounting authority. Group
totals by currency, with no exchange conversion. Report purchases as spending, not
as present inventory value or accounting profit. Component-price corrections must
update/compensate the same purchase record rather than creating a duplicate
purchase; preserve the correction history.

### Recipe, production and cost

Recipes reference a primer product and a powder product, not a particular
purchase lot. Snapshot the display identity and referenced IDs with the recipe
version and production record. Empty powder may still be a historical recipe
reference; make availability clear without inventing a consumption rule.

At production, the user selects one bullet product and explicitly selects the
oldest available purchase lot for the recipe's primer product and for the selected
bullet product. Show lots oldest purchase date first, with stable lot ID as a
tie-breaker; ignore empty lots. If one lot does not cover the quantity, the user
also selects the next oldest available lot or lots. The server verifies account
ownership, product, expected recipe version, selected lot revisions, oldest-first
order and sufficient current stock, then allocates quantities in that order. Reject
skipped older lots or changed stock with a clear reload-and-reselect message. The
ledger records the actual primer and bullet lot quantities and the
purchase-price/currency evidence in the same production operation as the new
produced batch. A changed recipe, stale stock or invalid item rejects the whole
command. Do not infer identity or compatibility from text or caliber.

Provide two distinct per-currency views: purchase spending records the full price of
each purchase once; partial consumption cost records primer/bullet costs when their
lots are consumed, and the full powder/case purchase costs at purchase time. This
defines the requested cost-at-purchase rule without claiming those items were
physically depleted. A commercial-ammunition purchase appears in spending once; its
price is not a produced-batch price or automatically attributed to a shooting
session. Do not add spending and consumption-cost totals together. Apportion primer
and bullet purchase prices by purchased quantity using exact arithmetic and
deterministic rounding; reversals and price corrections must remain auditable.
Manual count adjustments never manufacture consumption cost. Label consumption
cost partial and show the zero historical baseline and any missing component prices.

An absolute supply count adjustment translates to a ledger delta against a supplied
revision, records a reason, permits increases and decreases, and does not create a
purchase or rewrite previous consumption. When several lots exist, adjust a selected
lot, not an ambiguous product total. Keep the existing produced-ammunition counter
and its shooting behavior independent from supplies and commercial packages; do
not add a price or a new produced-batch adjustment feature in this scope.

Commercial ammunition is a purchased package in the online inventory, distinct from
produced batches and recipe-derived ammunition. Its status changes only when the user
marks it empty or reopens it. Recording a shooting session does not update that
status or infer commercial-ammunition quantities; users may continue to record
untracked ammunition using the existing shooting flow.

### Non-transactional consistency and accepted operational risk

The target MongoDB deployment does not support transactions. The user expects
online, single-user inventory operations and accepts that an interruption between
separate document writes can leave a partially updated production record, lot count
or batch projection. Do not introduce the formerly proposed account-wide gate,
stale-worker fencing or a broad rewrite of existing Workshop writers for this
feature.

Continue using account-scoped validation, existing idempotency and optimistic
revisions where already supported. Persist the production record's selected lots
and edit history so a user can inspect and correct an existing record after an
interruption. Show explicit server validation failures as failures; when the client
cannot know whether a write completed, explain that the result is uncertain and
provide an edit/reconciliation path rather than claiming no save occurred.
Repeated or overlapping requests are not guaranteed to serialize across multiple
tabs or server instances. Record this limitation in the Workshop architecture and
release documentation. Focused tests should prove ordinary success, validation,
account ownership, idempotent retries and production edit inversion; a real-Mongo
concurrency/crash gate is not a prerequisite under the accepted risk.

### Existing records and Drawing removal

Do not match free text to inventory or fabricate legacy spending. Existing recipes
that lack required product references remain viewable, but production is blocked
with a specific prompt to update the recipe and inventory. No untracked-production
bypass is offered. Existing produced-ammunition counters and shooting histories are
not reset: the requested zero baseline applies to finance, not existing operational
quantities.

Remove `Drawing` from UI, translation, contracts, domain and persisted recipe
documents and recipe snapshots, including snapshots inside versions, production
sessions and batches. Do not copy Drawing to Bullet form or retain a fallback.
This intentionally destroys historical Drawing values; other snapshot fields and
ledger facts/digests must remain unchanged. Perform a scoped, resumable, idempotent
data contraction with writes stopped and verify every affected collection before
new readers are enabled. Handle old browser snapshots explicitly, without deleting
unrelated queued offline commands. Backups and rollback limitations are described
below; do not promise recovery of deleted values without an independently retained
backup.

### Online-only boundary and UI

Expose supplies/finance through dedicated account-scoped online endpoints and
separate frontend models, not new stock/cost fields on the offline-cached recipe,
component or batch responses. Recipe reference IDs may be present in recipe data,
but supply counts, status and prices must not be copied into IndexedDB or offline
commands. The dashboard inventory region uses an independent live request and
independent loading/error/offline state, so failure does not hide unrelated dashboard
content. No fallback to stale stock or queued inventory mutations. Existing offline
shooting and component creation continue; a component created offline has unknown
price and contributes zero to finance until explicitly completed online. Existing offline produced-batch snapshots
remain for the supported shooting workflow; the online-only restriction refers to
the new supplies inventory, not removal of existing offline capability.

Use an inventory section under the existing Workshop navigation, with compact
product/lot management, purchases, manual adjustments and an expenses summary.
Show the new dashboard widget with links into that section, unavailable/offline and
inconsistent states, and no fabricated low-stock thresholds. Keep both production
entry points consistent, accessible and localized. Range use is the purpose of
offline support; no new offline inventory or finance operations are required.

## Impacted Projects

- **Models, Domain, Contracts and ServerAPI:** account-owned product/lot/purchase
  facts, production lot allocations and editable corrections, DTOs and authenticated
  endpoints. Mongo multi-document partial-write risk is accepted and documented.
- **BackOffice:** verify central Mongo index manifest only; no Workshop product
  behavior or permissions.
- **BackOffice.Tests:** focused Workshop service, replay, account endpoint and index
  coverage. Real-Mongo concurrency/recovery is not a release prerequisite.
- **Frontend shared models and services:** additive Workshop contracts and
  account-client calls; verify all consumers and compatibility.
- **shooter-workshop.client:** inventory and finance screens, component prices,
  recipe and both production entry points, dashboard, translations and offline
  boundaries.
- **Deployment:** verified backup and stopped writes before the separately authorized
  Drawing contraction; audit/apply indexes and coordinate API/client release. No new host, external service, background
  job, or frontend shared-layout package is required.
- **Other applications and standalone tools:** no intended behavior change; verify
  shared package consumers rather than changing them.

## Implementation File Map

### Core implementation

- [WorkshopEntities.cs](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs):
  supply, purchase and operation facts, recipe references and removal of Drawing.
- [DbCollections.cs](../../MorWalPizVideo.Models/Constraints/DbCollections.cs):
  feature-owned collection names.
- [WorkshopContracts.cs](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs):
  additive online supply/finance contracts and production/version fields.
- [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs):
  account-filtered product/lot persistence, mock behavior and stable indexes.
- [WorkshopService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopService.cs):
  component purchasing, recipe linking, production, production editing and count
  adjustment. Extract a narrowly scoped supplies service only if needed
  for ownership clarity, not a general inventory framework.
- [WorkshopReplay.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs):
  version-aware lot replay and reconciliation.
- [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs):
  authenticated, CSRF-protected online actions and reads.
- [workshop.ts](../../frontend/fe-packages/models/src/workshop.ts),
  [workshopService.ts](../../frontend/fe-packages/services/src/workshopService.ts),
  their package exports and existing endpoint definitions: additive models and
  isolated account-transport calls.
- [config.tsx](../../frontend/shooter-workshop.client/src/routes/config.tsx) and
  [AppShell.tsx](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx):
  new inventory route and navigation.
- [Recipes/component.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx),
  [Recipes/action.ts](../../frontend/shooter-workshop.client/src/routes/Recipes/action.ts):
  product selection, missing-field notification and Drawing removal.
- [AmmunitionPanel.tsx](../../frontend/shooter-workshop.client/src/routes/Ammunition/AmmunitionPanel.tsx),
  [Ammunition/action.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/action.ts),
  [Ammunition/loader.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts),
  [RecipeProduction/component.tsx](../../frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx),
  [RecipeProduction/action.ts](../../frontend/shooter-workshop.client/src/routes/RecipeProduction/action.ts)
  and [RecipeProduction/loader.ts](../../frontend/shooter-workshop.client/src/routes/RecipeProduction/loader.ts):
  consistent live selections, retries and feedback.
- [EquipmentPanels.tsx](../../frontend/shooter-workshop.client/src/routes/Firearms/EquipmentPanels.tsx)
  and [Components/action.ts](../../frontend/shooter-workshop.client/src/routes/Components/action.ts):
  online component purchase information and missing-price prompt.
- [Dashboard/component.tsx](../../frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx),
  [Dashboard/loader.ts](../../frontend/shooter-workshop.client/src/routes/Dashboard/loader.ts),
  and [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx): independent
  online summary, English/Italian copy and errors.
- [offlineStore.ts](../../frontend/shooter-workshop.client/src/services/offlineStore.ts):
  remove Drawing from existing cached recipe and batch snapshots without dropping
  pending commands, and prevent it and new supply status from being cached again.
- Existing focused tests:
  [WorkshopServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs),
  [WorkshopReplayTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs),
  [PublicAccountEndpointTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs),
  [MongoIndexOperationsTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs),
  [Recipes/route.test.ts](../../frontend/shooter-workshop.client/src/routes/Recipes/route.test.ts),
  [Ammunition/route.test.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/route.test.ts),
  [RecipeProduction/route.test.ts](../../frontend/shooter-workshop.client/src/routes/RecipeProduction/route.test.ts),
  [Dashboard/component.test.tsx](../../frontend/shooter-workshop.client/src/routes/Dashboard/component.test.tsx),
  [offlineStore.test.ts](../../frontend/shooter-workshop.client/src/services/offlineStore.test.ts)
  and [workshopService.test.ts](../../frontend/fe-packages/services/src/workshopService.test.ts).

### Added files and tests

- Feature-owned inventory route and focused tests under
  [routes/](../../frontend/shooter-workshop.client/src/routes/), following the
  existing component/loader/action pattern.
- [ProductionForm.tsx](../../frontend/shooter-workshop.client/src/routes/ProductionForm.tsx),
  [onlineInventory.ts](../../frontend/shooter-workshop.client/src/routes/onlineInventory.ts),
  and focused tests for production allocation/submission and inventory actions.
- Backend regression coverage in
  [WorkshopServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs).
  These mock-repository tests do not establish crash recovery or cross-instance
  serialization, which remain accepted risks.

### Migration and indexes

- The dry-run-first
  [Drawing contraction script](../../scripts/migrations/remove-workshop-drawing.mongosh.js)
  is implemented but not run. It is intentionally destructive; do not execute it
  without a verified backup, a Workshop write freeze and explicit operational
  approval. Feature-owned indexes are in the existing central manifest; their
  audit/apply remains a deployment operation. No account coordination-gate
  collection is part of this design.

### Documentation

- [shooter-workshop.md](../architecture/shooter-workshop.md) and
  [ADR-027](../architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md)
  record the implemented ownership, finance semantics, online boundary, production
  editing and accepted non-transactional risk. This handoff records delivery and
  remaining release gates.

### Verify Only

- [workshop-sw.js](../../frontend/shooter-workshop.client/public/workshop-sw.js),
  [offlineData.ts](../../frontend/shooter-workshop.client/src/services/offlineData.ts):
  verify no new inventory snapshots, responses or outbox payloads.
- [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs)
  and [ci.yml](../../.github/workflows/ci.yml): existing manifest and test/build
  surfaces; no assumed deployment or topology changes.
- [AppHost/Program.cs](../../MorWalPizVideo.AppHost/Program.cs) and the existing
  [Workshop deployment workflow](../../.github/workflows/deploy-shooter-workshop-client.yml):
  verify build ordering and coordinated API/client release sequence.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Add account-scoped supply, commercial purchase, adjustment and summary operations; require missing recipe references before production. |
| Frontend/UI | Existing Operate-mode visual identity, route loaders/actions, online states, two production forms, dashboard and localization. |
| Database/Data | Add lots, purchases and indexes; remove Drawing from recipes and snapshots; retain ledger facts and existing produced counters. |
| Contracts/Models | Add supply IDs and DTOs; remove Drawing entirely; outdated client requests must fail explicitly. |
| Authentication/Authorization | Existing public-account cookie, CSRF and account ownership on every ID; no BackOffice RBAC. |
| Validation | Nonnegative counts and prices, date/currency bounds, allowed types, expected revisions, recipe version, insufficient stock and prompts for missing information. |
| Cache | No new server output cache or offline API cache; no supplies state in IndexedDB or the outbox. Existing shooting snapshots remain. |
| Configuration/Secrets | No new secret; review indexes and the documented non-transactional limitation. |
| Logging/Telemetry | Safe correlation, command type and outcome only; exclude personal identifiers, notes, sensitive descriptions and prices. Surface conflicts and uncertain commit outcomes. |
| Background Jobs | None; reconciliation remains explicit and bounded. |
| Deployment/Operations | Duplicate/index audit; verified backup and stopped writes before the separately authorized Drawing contraction; deploy coordinated clients/API. |
| Backward Compatibility | No legacy finance backfill or untracked production bypass; missing data prompts an update; incompatible old clients/servers cannot write. |

## Reuse Opportunities

Reuse Workshop account transport and CSRF, account-filtered repository patterns,
idempotency keys, stable IDs, immutable operations, reconciliation, optimistic
revisions, existing recipe version snapshots, dashboard deferred regions, shared
dialogs and Workshop localized form feedback. Reuse central index governance rather
than adding a separate index subsystem. Do not reuse the offline queue for inventory
or introduce another frontend state store.

## Risks

1. **Accepted — partial production after interruption or overlapping writes:** separate
   MongoDB writes may leave a lot, production session or batch projection only
   partially updated. The single-user online assumption does not eliminate multiple
   tabs, retries or server interruption. Mitigation: preserve source records and
   edit history, surface unknown outcomes, allow users to inspect/edit production
   and adjust physical counts, and document the limitation. This is not a guarantee
   of automatic recovery or atomicity.
2. **High — intentional Drawing data loss and interrupted access:** contraction
   destroys stored Drawing values; incomplete recipes cannot produce. Mitigation:
   scoped preflight, backup and confirmation, stopped writes, explicit user prompts
   and no guessing. Signal: all targeted documents lack Drawing and users can update
   missing fields.
3. **High — rollback or reconciliation corrupts new facts:** old readers may not
   understand new operation schemas. Mitigation: versioned replay, tested repair,
   feature gate and compatible rollback floor. Signal: replay old and new fixtures
   and recover projections after induced failure.
4. **Medium — misleading expense/cost totals:** zero historical baseline, multiple
   currencies, rounding and purchase-time powder/case recognition can look complete
   or be double-counted. Mitigation: separate per-currency views, exact arithmetic
   and explicit partial/baseline labels.
   Signal: mixed-currency, missing-price and rounding tests.
5. **Medium — offline privacy/freshness:** extending existing cached responses can
   accidentally store or show stale supplies. Mitigation: separate online DTOs and
   region; inspect IndexedDB/outbox during offline/browser tests. Signal: no new
   status, quantities or prices persisted locally.
6. **Medium — account isolation or CSRF:** supply IDs and purchase records are
   private financial data. Mitigation: claim-derived account, scoped lookup and
   existing CSRF validation. Signal: foreign-account and missing-CSRF endpoint tests.
7. **Low — external-service failure:** no new external integration. Existing account
   and Mongo outages must be surfaced, not shown as empty inventory.

## Technical Debt

- MongoDB has no multi-document transaction in the target deployment. Partial
  production, purchase or lot updates after process/network interruption and
  overlapping writes remain possible under the accepted risk. Users need an
  inspection/edit path; this feature does not promise automatic repair.
- Existing offline snapshots include produced ammunition, recipes and components.
  Keep that established functionality; separating the new online data is required,
  not a wholesale offline redesign.
- Recipe search, archive UI, reporting expansion and broad finance/accounting
  features are unrelated backlog and must not enter this delivery.

## Historical Implementation Roadmap (Superseded)

The sequence below records the original implementation plan; it is not a list of
pending code work. The current delivery status and remaining operational gates are
above.

1. Add account-owned product/lot/purchase persistence, exact-money contracts,
   account-scoped indexes, recipe references and a separate online inventory
   summary. Do not backfill historical purchases.
2. Implement purchases, component purchase-price entry, physical count corrections,
   manual package status and finance summary. Validate account ownership, allowed
   product types, currency, dates, stock bounds and idempotent retries.
3. Implement production lot selection/consumption and editable production records.
   Persist selected lots and correction evidence; update the existing batch quantity
   and affected lots. Validate oldest-first allocation, stock availability,
   revisions, product references and correction inversion.
4. Add the inventory route, online dashboard region, component-price flow, recipe
   product selection, both production edit/recording entry points and translations.
   Keep stock/cost out of IndexedDB, offline payloads and service-worker caches.
5. Add and test the scoped, resumable Drawing contraction. Keep it dry-run by
   default; do not execute without verified backup, a write freeze and explicit
   operational approval. Update architecture/ADR and verify focused tests, routes,
   indexes, browser behavior and responsive/accessibility evidence.

Completion boundary: the acceptance criteria above, not advanced accounting,
external imports, inventory forecasting, powder/case consumption inference, or
broader Workshop cleanup. An independent post-validation review is warranted
because the change crosses financial data, concurrency, immutable history and
offline/authentication boundaries; the user decides whether to request it.

### Original visual-planning note (not verification evidence)

Target: Workshop inventory, component purchase field, production forms and dashboard
widget. Mode: **Operate**. This is a refinement and extension of the existing
Workshop identity, not a redesign. No Workshop-specific `PRODUCT.md` or `DESIGN.md`
was found; use the current app and its feature document as visual authority. At
implementation, use the narrow `shape` workflow for interaction decisions and
`harden` for offline/error/accessibility states, without inventing a new design
system. Capture representative desktop and narrow mobile renders of empty,
populated, error and offline states; verify keyboard operation, labels, focus,
contrast and English/Italian text. This plan does not execute the visual workflow.

## Additional Verification Boundaries

The following describes the broader evidence this feature touches; it is not a
claim that every item was exercised by the focused validation listed above.

- **Domain/unit:** product identity, nonnegative adjustments, explicit oldest-lot
  selection, skipped/empty lots, tied dates, lot splitting and stale selections,
  multiple currencies and rounding, zero historical finance,
  powder/case cost recognition at purchase, manual commercial-ammunition exhaustion,
  missing-recipe prompts and no Drawing fallback.
- **Integration/behavior:** account-scoped API and CSRF, duplicate key same/different
  payload, stale revisions and recipe versions, insufficient stock, production in
  both entry points, component create/install/offline-later-price, historical
  snapshot contraction and retained purchase history.
- **Accepted limitation:** a real-Mongo crash/concurrency proof is not a release
  prerequisite. Tests should still verify server-side nonnegative stock validation,
  account ownership, duplicate retries and production correction inversion under the
  existing test infrastructure. Document that these tests do not establish atomicity
  across multiple writes.
- **Replay/migration:** old operation schemas, new supply deltas, projection
  corruption, repair and repeated recovery. Exercise Drawing-only, Bullet-form-only
  and conflicting documents across all snapshot collections; verify Drawing is
  absent and other fields and ledger digests remain unchanged. No historical finance
  is fabricated.
- **Frontend/component/contracts:** both production forms, accessible inventory
  selection, counts and errors; dashboard's independent live state; missing-price
  labels, no stock fallback, localization and shared service shapes.
- **Browser/end-to-end:** prepared offline workspace, connectivity loss and reconnect,
  unchanged offline shooting/component queues, no new inventory data in IndexedDB,
  no queued inventory requests or cached API responses; removal of cached Drawing
  without loss of offline commands; account switch and sign-out.
  Verify desktop and mobile layouts and keyboard navigation. Existing test projects
  and browser infrastructure own these checks.

## Migration And Rollback

No historical finance backfill: baseline is zero and missing component prices are
flagged. Existing recipes must be updated before new production if required product
references are absent. Existing produced-ammunition counters, non-Drawing snapshot
fields and ledger facts remain intact.

Drawing removal is intentionally destructive. The migration
[`remove-workshop-drawing.mongosh.js`](../../scripts/migrations/remove-workshop-drawing.mongosh.js)
is dry-run by default and requires explicit apply and confirmation flags. Before
running it, take and verify an operational backup under existing retention/security
policy and stop Workshop writes. Audit all recipe, recipe-version,
production-session and batch documents, including embedded snapshots. Remove only
Drawing and verify no targeted field remains; remove it from client IndexedDB
snapshots on upgrade while preserving pending offline commands. Devices that never
reconnect cannot be remotely cleaned; retained backups may also contain Drawing
until their normal expiration. These copies cannot be claimed erased by the server
migration.
Deploy compatible API and client code; reject obsolete production payloads rather
than silently dropping required references. Audit/apply indexes before enabling
writes. Monitor validation/conflict responses without logging sensitive details.
An interrupted operation may need user inspection and correction; do not imply
automatic recovery. A frontend rollback must not allow unsupported writes. Restoring
deleted Drawing values requires an appropriate pre-contraction backup and a
coordinated restore; restoring an entire database would also roll back subsequent
user changes and requires an explicit recovery decision. Otherwise Drawing deletion
is irreversible. There is no auth or configuration-secret migration.

## Open Questions

None identified. The user confirmed the offline boundary, oldest-purchase selection
and purchase-time powder/case cost interpretation.
