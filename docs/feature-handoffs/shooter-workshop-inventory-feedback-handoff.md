# Shooter Workshop inventory feedback handoff

Status: implemented, including the follow-up Workshop UX updates. Focused
backend/frontend checks and the Workshop client production build passed. Visual
browser capture and live-server rollout checks were not run.

## Delivered implementation

- The API accepts optional package count and units-per-package snapshots for
  primer/bullet purchases. Stock lots store total purchased units, while purchase
  metadata retains the package details. Old unit-only requests and historical
  lots keep their existing meaning.
- Inventory product types are normalized at the shared Workshop boundary for all
  existing consumers, with explicit rejection of unknown wire values. The API
  advertises package and product-management capabilities; clients gate new
  actions for server-first rollout.
- Product name/brand edit, archive/restore and permanent deletion for products
  without detected references are implemented with account scoping,
  idempotency and expected revisions. Referenced products are rejected for
  deletion and can be archived instead. The reference scan and delete are not
  atomic with concurrent product-reference writes; this inherits the
  architecture's existing concurrency limitation. Archive is safer for any
  product with history.
- Component price entry/correction uses the existing dialog. Existing prices
  remain visible without an open form; corrections may omit the reason. New
  currency fields default to EUR, while existing currencies remain editable.
- Purchase-lot labels show brand, package count and individual units; available
  primer/bullet stock remains expressed as individual units. The dashboard
  uses the same normalized product types. No historical purchase was modified.
- The current enum and explicit request fields remain in place. A C# union would
  add serialization and compatibility work without solving the casing mismatch
  or per-purchase package snapshot requirement.
- Physical-component creation supports quantities from 1 to 100. Batch creation
  makes distinct component records with stable per-item idempotency keys; an
  optional entered price is treated as a per-component amount and recorded for
  each instance. Batch creation and price recording require connectivity;
  single unpriced component creation retains the existing offline queue behavior.
  Batch-created components remain uninstalled spares, since a batch cannot
  unambiguously replace or install multiple components on one firearm.
- Purchase-lot stock count and package-status forms now open in the existing
  accessible dialog. Supply-product row actions use a compact right-aligned
  group, and physical-component price actions align to the right of their rows.
- Activity titles now cover the inventory, component purchase and production
  correction operation types. New activity records resolve supply-product and
  component labels where those records exist; the UI no longer inserts the
  generic "Activity recorded" line. Historical activity documents were not
  rewritten.

Validation:

- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Packaged_primer_and_bullet_purchases_track_stock_and_cost_per_unit|FullyQualifiedName~Supply_products_can_be_edited_and_deleted_until_referenced|FullyQualifiedName~Referenced_supply_products_can_be_archived_but_not_deleted|FullyQualifiedName~Inventory_costs_use_consumption_facts_not_manual_count_adjustments_and_component_corrections_are_single_spend"`:
  passed (4 tests).
- `yarn workspace shooter-workshop.client test`: passed (97 tests).
- `yarn workspace @morwalpizvideo/services test`: passed (100 tests).
- `yarn build:shared`: passed for models, services and layout.
- `yarn workspace shooter-workshop.client build`: passed; Vite reported the
  existing >500 kB chunk advisory.
- The .NET test run emitted existing ImageSharp vulnerability advisories and
  package-pruning warnings; no dependency changes were made.
- Visual captures, live API contract tests and deployment/rollback exercises
  remain unverified.
- The Impeccable `harden` command is not available in the installed launcher;
  its available `detect` command scanned the component source but reported that
  page-level checks require a running server. No browser capture was available.
- Follow-up validation: `yarn --cwd frontend workspace shooter-workshop.client test src/routes/Components/action.test.ts src/routes/shared.test.tsx` passed (9 tests); `yarn --cwd frontend workspace shooter-workshop.client build` passed; the BackOffice.Tests project build passed and the focused activity/inventory filter passed (3 tests). The domain build also passed. .NET validation reported existing ImageSharp vulnerability advisories and package-pruning warnings.

## Outcome

Resolve the inventory feedback without redesigning the Workshop: allow optional
component price-correction reasons and show price forms only on request, default
new currency inputs to EUR, restore type-dependent controls and summaries, show
product brands, support product editing and deletion, and distinguish purchased
packages from actual available units. The user confirmed that package size belongs
to each purchase because the same product can be sold in different pack sizes.
The four-package purchase was only an example; the user does not need a historical
purchase-correction workflow. Never infer package size from the product name.

## Current Evidence

- [Inventory component](../../frontend/shooter-workshop.client/src/routes/Inventory/component.tsx)
  treats the product name as free text, records quantity without package size,
  omits the brand in lot labels, requires a reason only for price corrections,
  and leaves both new currency fields empty. Its count and status controls compare
  product types with PascalCase strings.
- [Inventory action](../../frontend/shooter-workshop.client/src/routes/Inventory/action.ts)
  and [component-correction contract](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs)
  reject an empty correction reason; the domain service also requires it.
  Initial price entry does not require a reason. Stock adjustment has a separate,
  still-required audit reason.
- [WorkshopController](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs)
  emits lowercase/camelCase product-type strings through `SupplyTypeName`, while
  [shared Workshop models](../../frontend/fe-packages/models/src/workshop.ts)
  declare PascalCase values and the
  [shared service](../../frontend/fe-packages/services/src/workshopService.ts)
  casts responses without normalizing them. Consequently valid products fail
  strict type comparisons in the dashboard, inventory, recipe and production
  screens. The screenshot's lowercase type is consistent with that mismatch.
- The [dashboard](../../frontend/shooter-workshop.client/src/routes/Dashboard/component.tsx)
  sums `availableQuantity` for matching primer/bullet products. Its displayed zero
  is explained by the type mismatch, subject to confirming the actual API response
  and lot values in the affected environment.
- [Inventory service](../../MorWalPizVideo.Domain/Workshop/WorkshopInventoryService.cs)
  stores a counted purchase's submitted quantity as the lot's purchased units;
  it does not parse the product name. A submission of `4` therefore records four
  units, not four packages of 1,500. Stock is projected from purchase and later
  ledger deltas. Production allocation and partial cost use lot units and the
  purchased-unit denominator. Powder and commercial items instead have one lot per
  purchased package; cases currently use the separate `itemCount` behavior.
- [Workshop entities](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs)
  retain product identity, purchase facts, lots and ledger facts separately.
  [Finance handoff](./shooter-workshop-finance-inventory-handoff.md) and
  [architecture guide](../architecture/shooter-workshop.md) describe the accepted
  online-only, account-owned, historical and partial-cost boundaries. Existing
  component correction history already permits a nullable reason.
- Supply products have an `Archived` flag and revision in the entity, but the
  inventory API currently exposes only creation and its product read contract
  does not expose a revision. Purchases, lots, recipes, production sessions and
  operations can retain product IDs. The existing component-definition pattern
  offers edit, archive and deletion guarded against references; it is not an
  existing supply-product implementation.
- The [shared Dialog](../../frontend/fe-packages/layout/src/components/Dialog.tsx)
  is already used by the Workshop recipe screen, and provides focus handling and
  keyboard dismissal.
- The relevant .NET projects target .NET 11. No repository-local language-version
  override or union-type usage was found. The user supplied a
  [Microsoft union-types reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union);
  its current contents and compiler availability were not independently verified
  during this read-only analysis.

## Approved implementation contract

1. Keep the existing wire type spellings for compatibility. At the shared
   Workshop service boundary, explicitly map the five supported wire values to
   the existing typed PascalCase client representation for inventory reads and
   product creation responses. Reject unknown values visibly rather than casting
   them into an invalid type. Cover every existing consumer, not just the two
   reported displays. Do not change global JSON enum serialization.
2. Keep product type, brand and product name as identity, with `1500` remaining
   literal user-entered name data. Show type, brand and name together in lot and
   product selectors; omit missing brand cleanly. Do not silently rename existing
   products or interpret their names as quantities.
3. For new primer/bullet purchases, collect **package count** and **units per
   package** separately. Snapshot both with the purchase/lot; validate positive
   whole values and checked multiplication on the server, with resulting quantities
   representable safely by the TypeScript client. Keep the existing 1–10,000
   purchase-quantity boundary. The purchase price remains the total for the whole
   purchase. The lot's purchased and available quantities remain individual units,
   so existing consumption, adjustment, FIFO and prorated-cost logic uses one
   consistent unit. Show both purchased packages and purchased/remaining units
   with explicit labels; a partly used package is not represented as a whole
   number of available packages. Dashboard and lot availability must agree.
4. Preserve current powder, case and commercial-item behavior; do not infer
   consumption or introduce unit-size semantics for those types. Their controls
   become visible after type normalization. Keep all existing stock-adjustment
   reasons and revision/ownership checks.
5. Make the component **price correction** reason optional end to end; trim and
   preserve a supplied reason, store missing/blank as absent, and continue
   recording correction history, revision and idempotency. Existing nonempty
   correction payloads must retain compatible retry hashes.
6. Default EUR only for new purchase and initial component-price forms. Keep
   corrections prefilled with the recorded currency, allow other valid currency
   codes, and preserve per-currency totals without conversions.
7. For each physical component show the recorded price and date without an
   always-open form. If no price exists, show its missing-price state and an
   **Add price** button. If a price exists, show a **Correct price** button.
   Open the corresponding form in the existing shared `Dialog`; keep errors and
   entered values visible on rejection, close on confirmed success, and preserve
   uncertain-save safeguards when the dialog is dismissed or reopened. A recorded
   price is not a request for further input; no external expected price is assumed.
8. Add product edit, delete and archive controls. Edit brand and name without changing the
   stable product ID or rewriting historical records. Make type immutable after
   creation: changing it could reinterpret existing stock and references; create a
   different product instead. Offer confirmed permanent deletion only when the
   product has no purchase, lot, recipe, production or other historical reference
   beyond its own creation event. For referenced products offer a clearly labelled
   **Archive** action instead; preserve history and exclude archived products from
   new purchases and selections. Explain the distinction in the UI rather than
   calling archival permanent deletion. Enforce the rules on the server for the
   owning account and expected product revision, not just by hiding buttons.
   Product create/edit/delete/archive must be idempotent and protect against
   concurrent reference creation; if the existing multi-document model cannot
   enforce that invariant, block deletion rather than risk orphaned data.

### Union-type evaluation

The earlier response was too categorical about union types: distinguish the C#
language version from the .NET target and defer to the current Microsoft
reference for the feature's actual version, support and limitations. A union can
represent alternative request shapes, but this feature already has a stable
type-discriminated supply record and an existing flat purchase contract. Union
syntax alone would not normalize the current JSON type strings, record historical
package sizes, enforce category-specific validation, or preserve existing JSON
and MongoDB shapes. It could also require a wire-format/serializer change. Prefer
the current enum plus explicit validated type-specific rules for this bounded
change. Before adopting unions in any later change, verify the linked reference
against the repository's actual SDK/compiler, preview requirements, JSON
serialization and old-client compatibility; do not make a compiler or dependency
upgrade as part of this handoff.

### Compatibility and historical data

- An omitted package-size field means the old API's existing unit-count behavior;
  do not retroactively multiply old lots or derive a size from free text. Missing
  package metadata means historical package details are unknown, not that a
  confirmed physical package size was one.
- An additive server capability in the inventory response gates the new
  package-aware form. A new client must not submit package metadata to an older
  server that would ignore unknown JSON fields: show a clear unavailable/update
  message instead. An old client continues to submit unit quantities with the old
  meaning. Deploy the server first, then the client.
- No historical package correction, purchase editing, automatic conversion or
  bulk migration is in scope. Existing lots retain their recorded unit semantics.
  The ordinary stock adjustment changes availability but not purchased quantity
  or the purchase-cost denominator; it is not a full financial correction. Adding
  a replacement purchase also records additional spending. Do not present either
  action as a lossless historical repair or imply the upgrade provides one.

### UX direction

This is an **Operate** refinement, not a redesign. Preserve the existing Workshop
visual and interaction conventions, mutation blocking, localization and online-only
behavior. Use the existing shared dialog for price and product editing, with focus
return, labelled controls and accessible close/error states. Do not discard an
uncertain save or its idempotency key on dialog close. No Workshop-specific
PRODUCT.md or DESIGN.md was found. The implementer
should use the installed Impeccable `harden` workflow for the inventory and
dashboard surfaces, without creating an unrelated design system. Capture desktop
and narrow-screen evidence for full, partially consumed, empty and unavailable
states; verify keyboard use, accessible labels/errors, and English/Italian copy.

## Impacted Projects

- **Models, Domain, Contracts, ServerAPI:** additive purchase metadata and
  capability, product lifecycle, validation,
  inventory accounting and optional correction reason.
- **BackOffice.Tests:** Workshop service and actual serialized ServerAPI contract
  behavior; BackOffice itself does not own the feature.
- **Shared frontend models and services:** package metadata and verified
  type-normalization boundary. Check all consumers before changing shared types.
- **Shooter Workshop client:** purchase, lots, dashboard and existing
  recipe/production consumers; localized labels and focused tests.
- **Deployment:** independently delivered ServerAPI and Workshop client require
  capability-gated server-first rollout. AppHost is verify-only. Other SPAs,
  Windows apps, offline storage, and other services have no intended changes.

## Impacted Files

### Modify

- [WorkshopEntities.cs](../../MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs):
  additive purchase/lot package snapshots and product operations.
- [WorkshopInventoryService.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopInventoryService.cs):
  count conversion, product lifecycle, validation,
  purchase facts, and nullable correction reason.
- [WorkshopContracts.cs](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs)
  and [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs):
  additive request/read contracts, capability and correction validation.
- [workshop.ts](../../frontend/fe-packages/models/src/workshop.ts) and
  [models barrel](../../frontend/fe-packages/models/src/index.ts), plus
  [workshopService.ts](../../frontend/fe-packages/services/src/workshopService.ts):
  typed metadata, product lifecycle and correction requests, and explicit runtime
  normalization.
- [services barrel](../../frontend/fe-packages/services/src/index.ts):
  export the new product lifecycle operations to app consumers.
- [endpoints-frontend.ts](../../frontend/fe-packages/services/src/endpoints-frontend.ts):
  Workshop endpoint definitions for added product actions.
- [Inventory component](../../frontend/shooter-workshop.client/src/routes/Inventory/component.tsx),
  [Inventory action](../../frontend/shooter-workshop.client/src/routes/Inventory/action.ts),
  and [i18n.tsx](../../frontend/shooter-workshop.client/src/i18n.tsx):
  forms, labels, display, defaults and translations. The dashboard component
  consumes the shared normalized inventory response without a local change.
- [WorkshopServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs),
  [workshopService.test.ts](../../frontend/fe-packages/services/src/workshopService.test.ts),
  [Inventory action tests](../../frontend/shooter-workshop.client/src/routes/Inventory/action.test.ts),
  and affected recipe/production tests: focused regression coverage.

### Test coverage

The implementation extends existing domain, shared-service and inventory-action
tests. No dedicated ServerAPI HTTP contract or Inventory component test existed
in the owning test setup, so those checks were not added; the UI behavior has
build and action-level coverage but no rendered-browser verification.

### Generated/Migration

No automatic migration or generated artifact. Existing lots retain their recorded
meaning; historical purchase correction is outside scope.

### Documentation

- Update [Workshop architecture](../architecture/shooter-workshop.md) and the
  relevant architectural decision record for package snapshots,
  product lifecycle and compatibility.
- After implementation, update this handoff with actual files, validation results,
  rollout evidence and historical-data limitations.

### Verify Only

- [productionAllocations.ts](../../frontend/shooter-workshop.client/src/routes/productionAllocations.ts),
  [submitProduction.ts](../../frontend/shooter-workshop.client/src/routes/submitProduction.ts),
  [Recipes component](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx),
  [WorkshopReplay.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs),
  [WorkshopRepository.cs](../../MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs),
  and [Workshop deployment workflow](../../.github/workflows/deploy-shooter-workshop-client.yml).
  Also verify the [shared Dialog](../../frontend/fe-packages/layout/src/components/Dialog.tsx).
  Modify only if the focused change requires it.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Add compatible package information and product lifecycle; keep wire types stable. |
| Frontend/UI | Normalize types, clarify quantities and defaults; use requested price dialogs and product controls. |
| Database/Data | Add optional snapshots; preserve old purchase evidence and prohibit orphaning references. |
| Contracts/Models | Add optional fields, capability, revision and nullable correction reason; preserve older requests. |
| Authentication/Authorization | No change; retain account ownership and CSRF enforcement. |
| Validation | Positive integral quantities, safe product and supported type; verify product references and revision; price reason optional only for component prices. |
| Cache | No new caching; inventory remains online-only. |
| Configuration/Secrets | No configuration or secret changes. |
| Logging/Telemetry | Preserve existing error visibility; do not log personal inventory or fabricate success. |
| Background Jobs | None. |
| Deployment/Operations | Server-first with capability check; client and server are independently deployed. |
| Backward Compatibility | Old requests keep unit semantics; old lots and existing consumption history remain unchanged. |

## Reuse Opportunities

Reuse `WorkshopSupplyType`, existing inventory service, purchase/lot and operation
ledger, quantity allocation, revision checks, partial-cost computation, shared
Workshop transport and existing mutation forms. Reuse component history's nullable
reason and the shared Dialog; do not introduce a polymorphic hierarchy or duplicate
quantity state in the frontend.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Old purchases may be reinterpreted incorrectly. | Never infer or convert historical package sizes; verify old quantity and cost evidence remains unchanged. |
| High | Older API silently ignores package metadata. | Gate the new input and submit path on a server capability; test old-server/new-client and server-first rollout. |
| High | Overflow, retries, concurrent mutations or partial writes corrupt quantity/price evidence. | Checked math, stable idempotency and explicit existing recovery behavior; test replay and relevant interruption paths. |
| High | Product deletion races with a new reference and orphans history. | The API checks account-owned references and revision before deleting; this is not atomic with concurrent writes. Prefer archive where concurrency is possible. A stronger invariant needs coordination beyond the current architecture. |
| Medium | Casing mismatch also breaks recipes and production choices. | Normalize once and test all five real wire values through the shared service and affected screens. |
| Medium | A relaxed reason weakens price audit or leaks into stock correction. | Keep revision history and optional supplied reason; retain required stock reason; test both. |
| Medium | Wrong cost denominator or double counting across lots. | Keep purchased quantity in units and total purchase price once; verify partial consumption and correction calculations. |
| Low | Independent deployment, localized or narrow-screen UI regresses. | Capability gating, focused translated/a11y/browser checks, monitored rollout and rollback. |

## Technical Debt

- Existing multi-document Workshop writes have documented partial-write and
  multi-tab limitations; this work must not claim transactional guarantees or
  silently expand into a transaction redesign.
- The transport's unchecked generic cast allowed the type mismatch to escape
  tests. Fix the Workshop inventory boundary only; broader transport validation
  is deferred.
- Existing historical package details remain unknown and are not corrected by
  this plan. A stock adjustment does not fix a historical purchase denominator;
  document that limitation rather than presenting it as a complete repair.

## Implementation Roadmap

1. **Contracts and data:** add optional purchase snapshots and capability; define
   omitted-size legacy behavior and preserve existing payload-hash replay for old
   requests. Define product revisions. Verify
   old documents and request/response compatibility first.
2. **Domain and API:** implement checked per-purchase conversion for counted
   types and guarded product lifecycle; preserve purchase price and unit-based
   lot accounting, and make only component-price correction reasons optional.
   The deletion reference scan is best-effort under the accepted no-transaction
   model; archive products when concurrent reference creation is possible.
3. **Shared frontend:** normalize existing wire type strings at the service
   boundary and expose typed package metadata/capability. Verify real serialized
   responses, unknown types and existing consumers.
4. **Workshop client:** add capability-gated package inputs,
   product edit/delete/archive controls, clear package/unit labels, brand, optional
   correction reason, price dialog and EUR defaults. Preserve recorded currencies,
   online-only operation and unrelated type behavior. Verify dashboard/lot
   consistency and recipe/production flows.
5. **Documentation and gates:** update the architecture record and this handoff;
   run focused backend and frontend checks, then the applicable builds and visual
   checks. Stop: do not add historical purchase correction, bulk-migrate data, change
   product types or redesign unrelated inventory without separate approval.

An independent post-validation review is warranted because this changes financial
denominators, persisted quantities and cross-version behavior; the user may select
the MorWalPiz Code Reviewer after implementation.

## Testing Strategy

- **Backend unit/behavior:** in BackOffice.Tests verify a new four-package
  purchase with 1,500 units each yields 6,000 units; subsequent consumption and
  corrections use the remaining units and cost denominator; old purchases remain
  units. Cover zero, fractions, multiplication overflow, limits, negative stock,
  multiple lots, stale revisions, ownership, idempotent retry and partial-write
  recovery. Confirm unknown historical package size is not fabricated and old
  purchase quantities and cost denominators remain unchanged.
- **Product lifecycle:** verify identity-preserving name/brand edit, immutable type,
  deletion of an unreferenced product after confirmation, rejection of referenced
  deletion, archived-product selection rules, replay and reference-creation races.
- **HTTP/contract/security:** verify actual lowercase wire types and capability,
  old requests, unknown input, optional and supplied reasons, unchanged required
  stock reason, authenticated account isolation and CSRF rejection.
- **Frontend:** service tests for all five real wire values and unknown values;
  component/action tests for controls, brand, EUR, preserved existing currencies,
  optional reason and validated package inputs. Verify price forms remain hidden
  until opened, success closes the dialog, focus returns, rejection preserves input
  and uncertain saves cannot be accidentally resubmitted. Test product management,
  dashboard/lot agreement after partial consumption, and
  recipe/production selectors with actual server-shaped payloads.
- **Migration/rollout:** read old Mongo-shaped documents without modifying them;
  test new client against absent capability and old client against new server.
  Verify no historical purchase is converted. No backfill test is needed.
- **End-to-end/visual:** as a gated implementation check, exercise a new
  purchase through stock change in a disposable account, and inspect desktop and
  narrow layouts, focus, errors, translations and offline/unavailable states.

## Migration And Rollback

There is no automatic MongoDB, SQLite or auth migration. Additive fields allow
older documents and clients; retain old unit semantics throughout the compatibility
window. Deploy the capable server before the gated client. Monitor validation,
unknown save outcomes and inventory discrepancies without exposing user data.
Client rollback leaves server-created unit quantities and snapshots intact.
Before any package-aware write, verify actual BSON compatibility with the rollback
version and that the older server preserves the meaning of newly recorded units.
If rollback cannot read or safely handle the new documents, pause writes and deploy
a compatible server instead. No bulk backfill is authorized.

## Open Questions

None identified. The user confirmed that archiving should be available and that
the four-package example does not need a historical correction workflow. The plan
preserves referenced records by offering archive instead of permanent deletion.
