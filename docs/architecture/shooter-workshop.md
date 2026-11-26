# MorWalPiz Range Manager
## Technical Product Specification (TPS)

**Document purpose:** Product and technical specification for the Architect agent.

**Product type:** Personal equipment and ammunition management application.

**Deployment:** Dedicated subdomain.

**Database:** MongoDB (document-oriented database).

**Visual identity:** MorWalPiz — Black / White / Yellow (#FFFF36).

**Status:** Target specification and complete functional baseline.

**Open delivery and release gates:** [Shooter Workshop architecture handoff](../feature-handoffs/shooter-workshop-handoff.md).

---

## Implementation status

The current working tree contains the initial Workshop implementation baseline across
ServerAPI, Domain, Models, Contracts, the standalone frontend workspace, account
transport, AppHost, CI, and image-staging deployment metadata. The feature-owned Mongo index definitions are included in the executable central
operations manifest, which BackOffice attempts through its non-blocking startup
initializer. Index failures are logged but do not gate Workshop traffic; operators
can inspect and remediate actual state through the existing audit/apply surface.

The 2026-11-14 refinement batch is now represented in source: component-definition
compatibility tags are normalized and persisted; component creation can atomically
converge with installation through an idempotent command; maintenance counters are
derived from the ledger; shooting attribution is derived for every installation active
at the session date; initial firearm rounds are ledger-backed; legacy and canonical OAL
values are read compatibly with two-decimal validation for new recipes; and activity
records carry additive summary metadata. The Workshop SPA exposes the compatible
component, recipe, maintenance, shooting, history, initial-round, and safe-return-path
flows without introducing browser-authored installation attribution.

The Workshop SPA is installable and supports bounded offline work. Its root-scoped
service worker precaches the application shell and static assets, serves the SPA
entry point for offline navigation, and bypasses API requests. It never caches
authenticated API responses. Native IndexedDB stores explicitly prepared,
account-partitioned snapshots and a local command outbox. The supported offline
operations are shooting sessions, maintenance events, catalogue create/archive,
physical component creation, and component replacement; catalogue update/delete
and unrelated routes remain online-only.

Route-level lazy loading is backed by a build-generated same-origin JS/CSS asset
manifest. The service worker stages a build-specific cache and activates it only
after the shell and every manifest asset pass status and content-type validation.
Failed installs keep the previously active cache, and activation removes only
older Workshop shell caches. No API response or arbitrary same-origin URL is added
to the build cache by fetch handling.

Offline access requires a previously successful online sign-in and local device
preparation. The browser stores only the remembered account identity, not the
HttpOnly session cookie or a token. On reconnection, the server session is checked
again and pending commands synchronize in order through existing authenticated
APIs. Commands carry idempotency keys; server conflicts are reported and block
dependent operations. Component replacement is a two-command remove-then-create
sequence, so a failed second phase is explicitly surfaced as partial completion.
The foreground shell syncs when connectivity returns; Background Sync and Push
are not used. Confirmed sign-out warns and clears all local workspaces and queued
commands. Preparation requests persistent browser storage where supported and
warns if it is not granted; browser/device verification remains a release gate.
The authenticated shell owns offline-state tracking and foreground reconnect
sync on every protected route. The offline readiness status and explicit
prepare/manual-sync controls render only in the dashboard hero; preparation
success, persistence warnings, and failures use dismissible shared-layout toast
notifications. Blocked commands and sync-session failures remain visible in
shell-level alerts on every protected route.

Workshop account login keeps the existing HttpOnly cookie transport. An unchecked
login creates an eight-hour server session and a browser session cookie; selecting
“Remember me” creates a thirty-day server session and a persistent cookie. The
session token is never exposed to frontend JavaScript or stored in browser storage.

The authenticated shell includes a four-step capability wizard. New account
registrations initialize `WorkshopCapabilityGuideSeenVersion` to `0`; the wizard
opens when that account first reaches Workshop and can be reopened from the
top-right Guide control on any protected route. Legacy account documents missing
the field are interpreted as version `1` so existing users are not interrupted.
Skip and completion advance the marker through the CSRF-protected
`POST /api/v1/workshop/capability-guide/seen` endpoint. The update is monotonic
and atomic, and profile updates use targeted fields so concurrent profile changes
cannot erase the marker. The marker is presentation state only and is not an
authorization or domain-setup signal.

The shooting-session workflow now accepts recipe quantities rather than browser-authored
batch or allocation IDs. ServerAPI resolves each recipe quantity into deterministic
oldest-production-date-first batch facts before appending the immutable operation. New
shooting-session documents retain the resolved recipe and batch evidence plus their
ledger operation ID. The authenticated shooting route lists those sessions and uses
inline positive corrections or confirmed zero/delete corrections; correction operations
remain compensating ledger facts and are never physically deleted.

Workshop route data uses React Router's deferred-data convention. Protected page
loaders start their existing service requests and return raw promises without awaiting
page data. Route components render their static chrome immediately and resolve each
data-dependent region with React `Suspense` and React Router `Await`, using local
table-row, card, activity, metric, panel, or detail skeletons rather than a generic
whole-page loading message. React Router 7's removed `defer()` helper is not
recreated; raw promises are the supported interface. Route actions, fetchers, and
automatic loader revalidation remain the freshness owner after successful mutations.

## Finance and supplies inventory

The finance and supplies inventory is an online-only, account-owned Workshop
feature. It records individual product purchases and lots for primers, bullets,
powder, cases and commercial ammunition, plus purchase-price information for
physical components. Existing records and offline-created components without
purchase information have an unknown historical price and contribute nothing to
recorded spending until entered; this zero baseline is not a claim about historical
spend.

Recipes reference primer and powder products. Production selects a bullet product
and oldest available primer/bullet purchase lots; the production record retains the
selected lot and purchase-price evidence. Editing an existing production record updates its date, quantity, notes, bullet
product and lot effects and retains an auditable correction history. Recipe identity
stays fixed so shooting-history links are not silently rewritten; the production's
recipe snapshot also remains unchanged when the current recipe is later revised.
Corrected quantity cannot be less than the amount already consumed.
Primer and bullet purchase cost is attributed as recorded stock is consumed.
Powder and case cost is recognized at purchase time without inferring their
consumption. Purchase spending and partial consumption cost are separate,
per-currency views and are not combined into a complete unit-cost claim.
Commercial ammunition is a manually managed purchased package, distinct from
produced batches and shooting-session consumption. Inventory data is not included
in offline snapshots, commands or service-worker caches.

Primer and bullet purchases may include package count and units per package.
Those values are snapshotted on the purchase and its stock lot; purchased,
available, consumed, and cost-attribution quantities remain individual units.
Older requests without package fields retain their existing unit-count meaning,
and older lots are not converted or inferred from product names. Product brand
and name may be edited without changing the product ID. Referenced products
should be archived; permanent deletion is limited to products with no recorded
references, but its multi-document reference scan is not atomic with concurrent
Workshop writes. Archive is the safe choice when a product has history or a
concurrent write is possible.

The inventory API keeps its existing lower-case/camel-case product type strings;
the shared Workshop service validates and normalizes these to the client's
existing typed product enum. Package-aware purchase inputs and product
management are gated by additive inventory capabilities for independently
deployed clients and servers. New currency inputs default to EUR without
restricting other valid currencies. Physical-component price correction reasons
are optional; stock-adjustment reasons remain required. Product kind remains the
existing enum and request fields remain explicit; introducing a C# union type
would require new serialization/contract behavior without solving a current
domain requirement.

### Recipe journal

The recipe journal is an online-only, append-only record of user-entered
calendar date, optional time of day, weather, temperature and unit, power factor,
and notes. It performs no calculations, assessments, recommendations, or
automatic weather lookup. Entries belong to the owning account and recipe, not
to a recipe version; recipe edits and archive do not rewrite or remove history.
Owners may read entries for archived recipes, but cannot add new entries to
them.

Journal entries are stored in a separate account-owned collection. They do not
change stock, counters, projections, or the immutable operation ledger. The
server derives account identity from the authenticated public-account session,
checks recipe ownership for every read and write, and requires CSRF for entry
creation. A stable account/key-derived document ID makes retries idempotent:
the same normalized request returns the saved entry, while reuse for another
payload returns a conflict. The account/recipe/date query index supports newest
date first reads with a stable ID tie-breaker.

The user expects online operations by one person and accepts the risk that
interruption between separate MongoDB writes can leave a partial production,
inventory or batch update. This feature does not add a MongoDB transaction or an
account-wide coordination gate. Concurrent-write serialization across tabs or
server instances is not guaranteed. The UI must distinguish explicit rejection
from an unknown result after a lost response and offer a way to inspect or edit an
existing production record. This is a documented product limitation, not a
correctness guarantee.

Drawing is removed from current recipe/snapshot contracts and UI, with no
Drawing-to-Bullet-form fallback. Existing persisted Drawing data is contracted only
through the separately reviewed, restartable migration at
`scripts/migrations/remove-workshop-drawing.mongosh.js`; it requires a verified
backup and stopped Workshop writes and is not run by application startup or feature
deployment.

**Accepted target — allocation removal:** Ammunition allocations/reservations are not
part of the current Workshop product model. Recipes describe ammunition and batches
carry on-hand stock; neither is associated with a firearm before consumption.
Availability is total batch on-hand stock. New commands and projections must not create
or depend on allocation records. Historical allocation-bearing operations remain
readable and replayable during the compatibility migration and are never rewritten.
Inline correction edits explicit recipe quantities, derives total rounds on the
server, and preserves the original untracked-ammunition quantity unchanged.

Subsequent user checks corrected the component ownership target: definitions are
category-like records, while manufacturer, model, compatibility tags, tracking mode,
and service threshold belong to physical component instances. Firearm installation is
optional when a physical component is created. The current implementation described
above implements that corrected Components route ownership.

This status is implementation evidence, not release approval. Full domain acceptance
coverage, exact-origin browser authentication proof, duplicate/TTL review and
actual-index audit evidence, Azure custom-domain/TLS/runtime configuration, and
complete operational rollback evidence remain open. BackOffice remains
operational index governance only;
it has no Workshop product behavior or ownership. Installation-specific read
projections and deployed same-origin browser proof remain open. Recipe editing is
implemented through the account-owned, versioned Workshop PUT route, and the
manual recipe journal is an additive online-only flow. Recipe search, filters,
archive, duplicate, production history, and version-history UI remain follow-up
work explicitly tracked in the delivery handoff.

# Architectural Direction and Delivery Baseline

This specification is the functional baseline for one integrated implementation. Every
domain, workflow, acceptance scenario and definition-of-done item in this document is
required before the Workshop application is considered releasable. The implementation
may be sequenced internally, but it must not redefine this specification as an MVP,
defer a listed domain, or publish a reduced first release.

The target application is a new React workspace that reuses the established structure,
visual system and genuinely shared components of `frontend/morwalpizvideo.client`
without coupling the two applications through app-local source imports. It is deployed
independently at the Workshop subdomain and uses `MorWalPizVideo.ServerAPI` for its
authenticated API. The architecture handoff assumes the canonical browser origin is
`https://workshop.morwalpiz.com` and the application is mounted at `/`; if `/workshop`
was intended as a path on another host, hosting, routing, cookie and CORS decisions must
be revised before implementation.

BackOffice has no Workshop user interface, management workflow or business operation
in the current scope. Its only possible impact is repository-wide operational
infrastructure, particularly the existing central MongoDB index manifest; that does not
make BackOffice part of the Workshop product flow.

## Firearm detail status surface

The authenticated Workshop route `/firearms/:id` owns a typed
`GET /api/v1/workshop/firearms/{id}/detail` read. The account-scoped projection
includes the firearm record, currently installed components with definitions and
installation metadata (including each installation date in the installed-component
list), paged installation history, maintenance events correlated to component
installation intervals, shooting sessions, immutable firearm round-history points,
replacement inventory candidates, malfunction count, and reconciliation state.
Round history offers chart and ledger-table views of the same operations; shooting
session notes remain a distinct record list. The route renders factual history only; it
does not infer mechanical safety or resolution state.

Ammunition allocations are not part of the current product model. Batch stock is
reviewed in Ammunition, and firearm consumption remains attributable through shooting
sessions; the detail route must not recreate allocation records.

Maintenance and malfunction recording use the existing maintenance-event model;
`Malfunction` is an additive event value. The detail route also provides
pre-targeted shooting, maintenance/malfunction, and archive actions while retaining
the existing account transport, CSRF, idempotency, and server validation rules.
Component swapping remains a separate follow-up until the durable multi-document
convergence command is delivered.

## Critical architectural risk: counter reconciliation

**Firearm rounds, physical-component rounds and ammunition stock are three related but
independent aggregates. No one aggregate counter is the source of truth for either of
the other two.**

A shooting operation may affect all three, only firearm and component usage, or a
subset of ammunition batches. Allocation affects ammunition availability but must not
affect firearm or component usage. Component installation determines attribution but
must not transfer or reset component usage. These differences make a partially applied
operation, duplicate retry, correction, backdated session or component move capable of
silently producing three plausible but inconsistent totals.

The implementation must therefore preserve immutable, account-owned historical
operation facts containing the complete typed effects needed to replay each aggregate
independently:

- firearm round deltas by firearm and shooting segment;
- component round deltas by physical component, firearm, installation and segment;
- ammunition stock and allocation deltas by batch;
- an idempotency identity and causal link for the originating command;
- explicit reversal and replacement links for corrections;
- event schema version, effective time and recorded time.

Current totals are rebuildable projections, not independent facts. A reconciliation
operation must be able to discard and reconstruct firearm totals, every physical
component total, batch on-hand stock, allocated quantities and available quantities
from the historical operation ledger. Corrections append compensating facts; they do
not rewrite or delete the original contribution. A mutation is not complete merely
because one visible counter changed: it succeeds only when the authoritative event is
durable and all affected projections have either been updated or are explicitly marked
for recoverable reconciliation.

Physical-component creation accepts an optional non-negative initial round count. A
nonzero value is recorded as a component aggregate delta in the immutable operation
ledger; `ComponentInstance.TotalRounds` is only the replayed projection. Updating a
component may append a signed correction delta when the requested current total differs
from the projection, subject to the expected revision and idempotency key. Omitting the
value or submitting the existing total leaves historical usage unchanged. Existing
documents remain compatible because no new persisted field is required.

The complete replay invariant and fault-injection tests defined in the architecture
handoff are release gates. This is the highest-risk architectural concern in the
application and takes precedence over dashboard and presentation work.

---

# 1. Product Overview

## 1.1 Vision

MorWalPiz Range Manager is a personal management application designed for practical shooting enthusiasts and sport shooters.

Its purpose is to provide a centralized place where users can manage their shooting equipment, track component lifecycle, organize ammunition inventory and maintain a historical record of their shooting activities.

The application should replace spreadsheets, handwritten notes and disconnected tracking systems.

The product is composed of three interconnected domains:

1. **Equipment Management:** Firearms, component inventory and installation history.
2. **Maintenance Management:** Component usage counters, service intervals and maintenance records.
3. **Ammunition Management:** Personal recipe records, production sessions, ammunition batches, allocation and consumption.

These domains should be independently usable but integrated where their data naturally overlaps.

For example, recording a shooting session should update:
- Firearm accumulated rounds.
- Installed component accumulated rounds.
- Ammunition inventory, when tracked ammunition batches are consumed.

## 1.2 Product principles

The following principles guide the entire implementation:

- A physical component belongs to the user's inventory, not permanently to a firearm.
- Component usage counters follow the physical component when moved between firearms.
- Every component instance must be individually identifiable, even when multiple identical components exist.
- Ammunition production, allocation and consumption are separate operations.
- Historical information must remain consistent when recipes, components or configurations are changed.
- Firearm registration must not collect serial numbers or other sensitive identifying weapon information.
- All quantities and counters must be traceable to their originating records.
- Users should be able to use equipment management without using ammunition management, and vice versa.

## 1.3 Deployment and application boundaries

The application will run on a dedicated subdomain of the MorWalPiz domain.

It is a separate application from the Digital Magazine, although it shares the same brand identity.

The Architect should inspect the existing repository and infrastructure before deciding how to integrate the new application.

Existing infrastructure and common application capabilities should be reused where appropriate.

---

# 2. Scope and Functional Domains

The application must provide the following functional areas:

| Domain | Main functionality |
|---|---|
| Dashboard | Overview of equipment, maintenance and ammunition |
| Firearms | Register and manage firearms |
| Component Catalogue | Define reusable component types |
| Component Inventory | Manage physical component instances |
| Maintenance | Track usage, service intervals and maintenance history |
| Ammunition Recipes | Maintain personal recipe records |
| Production Sessions | Record ammunition production |
| Ammunition Inventory | Track produced ammunition batches |
| Ammunition Allocation | Assign ammunition to firearms |
| Shooting Sessions | Record shooting activity and ammunition consumption |
| History | Review historical equipment and ammunition activity |

All domains listed above are included in the implementation scope.

---

# 3. Equipment Management

## 3.1 Firearm Registry

Users must be able to register multiple firearms.

A firearm represents a physical piece of equipment owned by the user.

### Firearm information

| Field | Type | Required | Description |
|---|---|---|---|
| Id | ObjectId | Yes | Internal identifier |
| Manufacturer | String | Yes | Manufacturer name |
| Model | String | Yes | Model name |
| Caliber | String | Yes | Firearm caliber |
| Category | String | No | Shooting discipline/category |
| Nickname | String | No | User-defined identifier |
| PurchaseDate | Date | No | Purchase date |
| Notes | String | No | Personal notes |
| TotalRounds | Number | Yes | Accumulated rounds |

### Functional requirements

- Register multiple firearms.
- Edit firearm information.
- Archive or remove a firearm.
- View accumulated rounds.
- View currently installed components.
- View component installation history.
- View maintenance history.
- View shooting history.
- Associate shooting sessions with a firearm.

Ammunition allocations in earlier functional requirements are superseded by the
accepted allocation-removal target above. Batch availability belongs to Ammunition;
shooting-session records retain firearm consumption details.

### Privacy requirements

The application must not request or store:
- Serial numbers.
- Matricola.
- Registration identifiers.
- Other real-world firearm identification numbers.

The application-generated identifier is sufficient.

Firearm photographs must not be mandatory.

---

# 4. Component Management

This is one of the central domains of the application.

The system must distinguish between a component definition and a physical component instance.

## 4.1 Component Catalogue

A component definition represents a reusable component type.

Examples:
- Extractor.
- Main spring.
- Hold-open.
- Recoil spring.
- Magazine spring.

A definition is a template used to create physical component instances.

### Component definition

| Field | Type | Required | Description |
|---|---|---|---|
| Id | ObjectId | Yes | Internal identifier |
| Name | String | Yes | Component name |
| Description | String | No | Description |
| Notes | String | No | Additional information |
| Archived | Boolean | Yes | Hidden from new physical-component selection while retained for existing references |
| Revision | Number | Yes | Optimistic-concurrency revision |

The catalogue must be customizable by each user.

Users can create their own definitions without depending on a predefined global catalogue.

Manufacturer, model, compatibility, tracking mode and service threshold are facts
about a physical item and must not be inherited from the definition.

## 4.2 Physical Component Inventory

A component instance represents a specific physical item.

Example:

A user has three extractors of the same model:

- Extractor A.
- Extractor B.
- Extractor C.

All three reference the same component definition but maintain independent usage histories.

### Component instance

| Field | Type | Required | Description |
|---|---|---|---|
| Id | ObjectId | Yes | Internal identifier |
| DefinitionId | ObjectId | Yes | Component definition reference |
| IdentificationNote | String | No | User-defined identification |
| Manufacturer | String | No | Physical component manufacturer |
| Model | String | No | Physical component model |
| CompatibilityTags | String[] | No | User-defined firearm/platform compatibility labels |
| TotalRounds | Number | Yes | Accumulated usage |
| ServiceInterval | Number | No | Instance-specific interval |
| TrackingMode | Enum | Yes | Counter, Interval or Manual |
| Status | Enum | Yes | Inventory, Installed, Maintenance, Retired |
| Archived | Boolean | Yes | Inventory-list archive state, separate from retirement |
| Notes | String | No | Personal notes |
| Revision | Number | Yes | Optimistic-concurrency revision |
| CreatedAt | Date | Yes | Creation date |

The component instance must be independent of any firearm.

Its accumulated counter must never be reset simply because it is removed or installed elsewhere.

## 4.3 Component Tracking Modes

Three tracking modes must be supported.

### Counter

The component has an accumulated usage counter without a service threshold.

Example:

Extractor: 9,000 rounds.

### Interval

The component has an accumulated counter and a user-defined service threshold.

Example:

Main spring: 2,850 / 3,000 rounds.

The application should calculate the percentage of the configured interval and display the remaining quantity.

### Manual

The component is tracked through manually recorded maintenance and inspection events without an automatic round-based threshold.

## 4.4 Component Installation History

Components can be installed and removed from firearms.

Each installation must be recorded independently.

### Component installation

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Installation identifier |
| ComponentId | ObjectId | Physical component |
| FirearmId | ObjectId | Associated firearm |
| InstalledAt | Date | Installation date |
| RemovedAt | Date? | Removal date |
| InstallationNotes | String | Optional notes |
| RemovalNotes | String | Optional notes |

### Business rules

1. A physical component can only be installed in one firearm at a time.
2. Removing a component must not delete its usage history.
3. Installing a component in another firearm must preserve its accumulated counter.
4. Previous installations must remain accessible.
5. A component can exist in inventory without being installed.
6. A component with existing accumulated usage can be installed in another firearm.
7. A retired component must remain available in historical records.

Example:

Extractor A:
- STI Executive 2003: 6,000 rounds.
- Tanfoglio Stock II: 3,000 rounds.
- Accumulated usage: 9,000 rounds.

The component retains the accumulated total independently of the firearms.

---

# 5. Maintenance Management

Maintenance management provides an overview of component condition and historical maintenance activity.

## 5.1 Maintenance Intervals

Each component instance can have its own service interval. Component definitions do
not provide tracking or interval defaults.

Illustrative user-defined examples:

| Component | Tracking mode | Interval |
|---|---|---:|
| Main spring | Interval | 3,000 |
| Hold-open | Interval | 13,000 |
| Extractor | Interval | 10,000 |
| Recoil spring | Counter | None |

These values are entered and maintained by the user. They are not manufacturer-certified replacement recommendations.

## 5.2 Maintenance Status

For interval-tracked components, calculate:

- Accumulated rounds.
- Configured interval.
- Percentage of interval consumed.
- Remaining rounds.

Suggested visual statuses:

| Percentage | Status |
|---|---|
| Below 70% | Normal |
| 70–89% | Approaching threshold |
| 90–99% | Attention |
| 100% or above | Threshold reached |

These are configurable application display rules, not technical assessments of component safety.

## 5.3 Maintenance Events

Users must be able to record maintenance events independently of shooting sessions.

Examples:
- Inspection.
- Cleaning record.
- Component replacement.
- Component maintenance.
- Manual counter adjustment.
- General maintenance notes.

### Maintenance event

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Event identifier |
| FirearmId | ObjectId? | Associated firearm |
| ComponentId | ObjectId? | Associated component |
| EventType | Enum | Maintenance event type |
| Date | Date | Event date |
| RoundCountAtEvent | Number? | Counter at event |
| Description | String | Event description |
| Notes | String | Additional notes |

Maintenance records must be retained as historical events.

---

# 6. Ammunition Management

Ammunition management must support personal recipe records, production sessions, inventory batches, allocation and consumption.

The application is a recordkeeping tool.

It must not calculate, recommend, validate or optimize ammunition loads, powder quantities or loading procedures. User-entered recipe information is stored as supplied and must not be represented as technically verified.

## 6.1 Recipe Management

Users must be able to create multiple personal recipes for the same caliber.

A recipe is a reusable record of a user-defined ammunition configuration.

### Recipe fields

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Recipe identifier |
| Name | String | User-defined recipe name |
| Caliber | String | Ammunition caliber |
| OAL | Number/String | User-entered overall length |
| BulletWeight | Number | User-entered bullet weight |
| BulletForm | String | Bullet form description |
| Drawing | String | Bullet drawing description (Italian: trafilatura) |
| Primer | String | Primer description |
| Powder | String | Powder description |
| PowderWeight | Number | User-entered powder weight |
| Notes | String | Personal notes |
| Status | Enum | Active or Archived |
| CreatedAt | Date | Creation date |
| UpdatedAt | Date | Last modification |

Measurement fields must preserve their units and original user-entered values.

The application must not derive or calculate loading parameters.

### Recipe functionality

- Create recipes.
- Edit recipes.
- Duplicate recipes.
- Archive recipes.
- Browse recipes by caliber.
- Search recipes by name.
- View production sessions associated with a recipe.
- View historical recipe versions.

### Recipe versioning

Recipe information must be preserved historically.

When a recipe is modified, previous production sessions must retain the exact recipe data used when that production occurred.

The application must not overwrite historical recipe information.

Each production batch must contain a recipe snapshot or an immutable reference to an immutable recipe version.

---

# 7. Production Sessions

A production session represents an event in which the user records a quantity of produced ammunition.

## 7.1 Production Session Fields

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Session identifier |
| RecipeId | ObjectId | Selected recipe |
| RecipeSnapshot | Object | Recipe data at production time |
| ProductionDate | Date | Production date |
| QuantityProduced | Number | Number of cartridges recorded |
| Notes | String | Personal session notes |
| BatchId | ObjectId | Generated ammunition batch |

### Functional requirements

The user must be able to:

1. Select an existing recipe.
2. Record the production date.
3. Enter the produced quantity.
4. Add personal notes.
5. Optionally allocate the produced ammunition to one or more firearms.

Firearm assignment must not be mandatory during production.

A production session creates a distinct ammunition batch.

The batch must remain independently identifiable even when multiple production sessions use the same recipe.

---

# 8. Ammunition Inventory and Batches

Ammunition must be tracked through individual production batches.

The application must not maintain only a single global quantity per caliber.

Each batch represents a traceable quantity of ammunition produced during one session.

## 8.1 Ammunition Batch

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Batch identifier |
| RecipeId | ObjectId | Original recipe reference |
| RecipeSnapshot | Object | Historical recipe data |
| ProductionSessionId | ObjectId | Originating production session |
| ProductionDate | Date | Production date |
| ProducedQuantity | Number | Original quantity |
| Notes | String | Batch notes |

Remaining stock should be derived from production, allocations and consumption records.

## 8.2 Inventory Views

The user must be able to browse:

- Total ammunition produced.
- Available ammunition.
- Allocated ammunition.
- Consumed ammunition.
- Ammunition grouped by caliber.
- Ammunition grouped by recipe.
- Ammunition grouped by batch.

The application must clearly distinguish production from consumption.

Allocation does not mean consumption.

## 8.3 Ammunition Allocation

A batch can be assigned to one or more firearms.

Example:

Batch #001:
- Produced: 500 rounds.
- STI Executive 2003: 200 rounds allocated.
- Tanfoglio Stock II: 150 rounds allocated.
- Unassigned: 150 rounds.

### Allocation entity

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Allocation identifier |
| BatchId | ObjectId | Ammunition batch |
| FirearmId | ObjectId | Destination firearm |
| Quantity | Number | Allocated quantity |
| AllocatedAt | Date | Allocation date |
| Notes | String | Optional notes |

### Allocation rules

- A batch can have multiple allocations.
- A firearm can receive ammunition from multiple batches.
- Allocation quantities must not exceed the available quantity.
- Allocation can be modified or released.
- Releasing an allocation returns the quantity to available stock.
- Allocation must not increment firearm round counters.
- Allocation must not increment component usage counters.

Unassigned ammunition must remain available for future allocation.

---

# 9. Shooting Sessions and Ammunition Consumption

This domain connects equipment and ammunition management.

A shooting session represents actual ammunition consumption associated with a firearm.

## 9.1 Shooting Session

### Fields

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Session identifier |
| FirearmId | ObjectId | Firearm used |
| Date | Date | Shooting date |
| TotalRounds | Number | Total rounds fired |
| Notes | String | Session notes |
| ConsumptionRecords | Array | Ammunition batch consumption |
| CreatedAt | Date | Record creation |

A shooting session must support multiple ammunition batches.

Example:

- Firearm: STI Executive 2003.
- Batch #001: 100 rounds.
- Batch #003: 50 rounds.
- Total rounds: 150.

The sum of recorded batch consumption must equal the session total when all ammunition is tracked.

The application should also support shooting sessions where ammunition origin is unknown or not tracked, so the user can still maintain accurate firearm and component counters.

## 9.2 Consumption Records

| Field | Type | Description |
|---|---|---|
| Id | ObjectId | Consumption identifier |
| BatchId | ObjectId? | Consumed batch, if known |
| Quantity | Number | Quantity consumed |
| AllocationId | ObjectId? | Related allocation, if applicable |

### Business rules

- Consumption reduces ammunition stock.
- Consumption updates the firearm total round counter.
- Consumption updates the usage counters of installed components.
- Consumption must be attributed to the correct physical component instances.
- Consumption must never update components that were not installed at the relevant time.
- The same shooting session must not increment counters more than once.
- Historical corrections must preserve the original record and correction information.

## 9.3 Component Usage Attribution

The application must maintain component installation history to determine which components were installed when a shooting session occurred.

For a normal shooting session, the recorded round count is applied to each tracked component installed in the firearm at that time.

If installation changes occur during a session, the user must be able to split the session into appropriate usage segments.

This prevents incorrect historical attribution.

---

# 10. Event and History Management

The application must preserve historical records for important operations.

The following operations must produce traceable events:

- Firearm created or archived.
- Component created.
- Component installed.
- Component removed.
- Component maintenance recorded.
- Component counter adjusted.
- Ammunition produced.
- Ammunition allocated.
- Ammunition allocation changed or released.
- Shooting session recorded.
- Shooting session corrected.
- Ammunition consumed.

### Historical consistency

Historical records must not be silently rewritten when the current state changes.

Examples:

- Removing a component does not delete its previous installations.
- Modifying a recipe does not change previous production batches.
- Replacing a component does not reset or delete the previous component counter.
- Correcting a shooting session must account for the previous counter contributions.
- Archiving a firearm does not delete historical shooting sessions.

The application should support a chronological activity view with filters for:
- Firearm.
- Component.
- Ammunition batch.
- Activity type.
- Date range.

---

# 11. Dashboard

The dashboard is the main landing page after entering the application.

It should provide a concise overview of the user's equipment and ammunition situation.

## 11.1 Dashboard Widgets

### Equipment overview
- Registered firearms.
- Tracked physical components.
- Total accumulated rounds across firearms.

### Maintenance overview
- Components approaching their configured thresholds.
- Components that have reached their configured thresholds.
- Recent maintenance events.

### Ammunition overview
- Total available ammunition.
- Allocated ammunition.
- Total produced ammunition.
- Ammunition grouped by caliber.

### Recent activity
- Recent shooting sessions.
- Recent production sessions.
- Recent component installations.
- Recent maintenance events.
- Daily activity combines production and shooting totals in one rounds column, distinguishes production from use, shows a session count beside the date when multiple sessions occurred, and displays recorded notes or `-`.

### Quick actions
- Add firearm.
- Add component.
- Record production.
- Record shooting session.
- Allocate ammunition.
- Record maintenance.

The dashboard should prioritize actionable information over decorative statistics.

---

# 12. Application Navigation and Screens

The application should use a dashboard-oriented layout with persistent navigation.

### Culture and localization

The client supports Italian (`it-IT`) as the fallback/default culture and English
(`en-GB`) as the secondary culture. On first load, the browser resolves the culture
from its language preferences by primary language subtag; unsupported or unavailable
preferences fall back to Italian. An explicit user selection is persisted only in
browser-local Workshop storage and takes effect immediately on both login and
authenticated surfaces. Routes, API payloads, account contracts, and stored domain
values remain culture-neutral. Dates and numbers are formatted at the client
presentation boundary using the selected culture.

### Route data and mutation ownership

The Workshop client uses React Router data routes as the boundary for server state.
Each route-owned read belongs in a colocated `loader.ts`; each route-owned form
mutation belongs in a colocated `action.ts`. The route manifest registers those
functions, components consume loader data through React Router, and forms submit
through fetchers so pending, validation, success, and revalidation state remain owned
by the router. Route components must not replace this flow with effect-driven API
loading or direct service calls from submit callbacks. Component state remains
appropriate for transient disclosure, filtering, and other presentation-only state.

The Ammunition route loads the current account's recipes and produced batches through
its loader and presents Ammunition and Recipes as URL-selected tabs. Its production
form sends the selected recipe identifier, production date, quantity, notes, and a
per-submission idempotency key through the route action. The action also dispatches
recipe create/update intents to the recipe action, preserving one route-owned
mutation boundary. The component does not create a second API client or mutation
path. The production form is full-width on desktop with recipe, quantity, and date
in the first column and notes in the second; successful fetcher submissions use
normal route revalidation. `/recipes` redirects to the Recipes tab and the former
`/production` URL redirects to `/ammunition` for compatibility.

Recipe create/update intents remain implemented by `Recipes/action.ts`. New Recipe
and recipe detail/edit forms use the shared layout Dialog, with successful creates
closing the modal and failed submissions retaining the draft and error. The shared
`TabbedWorkspace` and `Dialog` primitives are domain-neutral exports from
`@morwalpiz/layout`; feature-specific routing, copy, and styling remain in this
client.

The Shooting Sessions route loads firearms, recipes, sessions, component definitions,
and physical inventory once, then presents Shooting Sessions and Maintenance as
URL-selected tabs. Shooting and maintenance creation forms use modal dialogs;
Repeat as draft selects the shooting tab, generates a fresh idempotency key, and
opens the populated shooting dialog. `/maintenance` redirects to the Maintenance
tab. The Dashboard, History, Equipment, and Reconciliation
routes load their account-scoped read models through colocated loaders. Equipment
is the canonical `/firearms` workspace and loads firearms, physical component
inventory, component definitions, and installations once, presenting Firearms,
Inventory, and Catalogue as URL-selected tabs. The legacy `/components` URL
redirects to the Inventory tab without issuing a read. Its mutations are owned by
the Equipment action boundary, which delegates component intents to the focused
component action. Their mutations are submitted through fetchers, including
shooting corrections, maintenance events, firearm creation, and projection rebuilds.
Successful submissions use normal route revalidation. The Dashboard loader requests
bounded analytics periods from the authenticated Workshop API; analytics are derived
from immutable operations and historical facts, include reconciliation state, and are
not cached by the PWA or shared output caching. Firearm detail pagination preserves
independent maintenance and shooting cursors, and corrected shooting sessions remain
visible with a superseded label.

## Main navigation

| Navigation item | Main screen |
|---|---|
| Dashboard | Application overview |
| Equipment | Firearm list, component inventory and catalogue |
| Maintenance | Component thresholds and maintenance history |
| Ammunition | Ammunition inventory and batches |
| Recipes | Recipe list and details |
| Sessions | Production and shooting history |

## 12.1 Dashboard

Displays the overview widgets and quick actions.

## 12.2 Equipment

The canonical Equipment workspace is `/firearms`. Firearms, Inventory, and
Catalogue are URL-selected tabs backed by one combined route loader and action.
The former `/components` route remains a compatibility redirect to the Inventory
tab and preserves its query parameters.

The Firearms tab lists registered firearms and provides the registration form.
The Inventory tab manages physical component instances and their installations.
The Catalogue tab manages reusable component definitions.

Each firearm detail page must include:

- General information.
- Total rounds.
- Installed components.
- Installation history.
- Maintenance history.
- Shooting sessions.

Ammunition allocations are not displayed because they are not part of the current
Workshop model; review batch stock in Ammunition and consumption in shooting sessions.

## 12.3 Component domain

The component area must provide two distinct views:

**Catalogue**
- Component definitions.
- Category description and notes.
- Edit, archive and guarded delete actions.

**My Inventory**
- Physical component instances.
- Identification notes.
- Manufacturer, model and compatibility tags.
- Tracking mode and instance-specific service interval.
- Accumulated rounds.
- Current status.
- Installation information.
- Edit, archive and guarded delete actions.

The user must be able to navigate from a component definition to all its physical instances.

A physical component can be created without a firearm and remain as a spare in
inventory. Hard deletion is allowed only for pristine records with no historical
references; otherwise the user must archive the record.

## 12.4 Maintenance

A consolidated view of all tracked components.

Include:
- Component name.
- Installed firearm.
- Accumulated rounds.
- Configured threshold.
- Remaining rounds.
- Current status.

Users must be able to open the component detail and access its complete maintenance history.

## 12.5 Ammunition

Ammunition inventory and production overview. The production form appears above the
produced-batches list so recording stock and reviewing its result are one workflow.

Include:
- Available stock.
- Allocated stock.
- Batch list.
- Caliber filters.
- Recipe filters.
- Allocation actions.

## 12.6 Recipes

Recipe management.

Include:
- Recipe list.
- Caliber filters.
- Search.
- Recipe details.
- Historical production batches.
- Recipe version history.

## 12.7 Sessions

A unified activity history with separate filters for:

- Shooting sessions.
- Production sessions.
- Maintenance events.
- Component installation events.

---

# 13. User Interface and Visual Design

The application must reuse the visual identity established for the MorWalPiz Digital Magazine.

However, this application has a functional dashboard purpose, so its layout should be optimized for frequent operations and information density.

## 13.1 Color palette

| Token | Dark | Light | Usage |
|---|---|---|---|
| Page background | #101010 | #F2F4F6 | Main background |
| Surface | #181818 | #FFFFFF | Cards and panels |
| Subtle surface | #222222 | #E8EDF1 | Elevated surfaces |
| Primary text | #FFFFFF | #1C2B36 | Titles and important text |
| Secondary text | #B8B8B8 | #536573 | Descriptions and metadata |
| Border | #383838 | #C9D3DA | Separators |
| Primary action | #FFFF36 | #38566D | Actions and selected states |
| Action text | #101010 | #FFFFFF | Text on primary actions |
| Focus | #FFFF36 | #205B82 | Visible keyboard focus |

The shared `@morwalpiz/layout` theme owns these semantic roles. System is the
first-visit default and follows the device; explicit Light or Dark choices are
stored in origin-local browser storage. Yellow remains the recognizable dark
brand accent and is not reused as light-theme body text. Status colors retain
their shared success, warning, error, and information roles.

Use it selectively for primary actions, active navigation, important values and selected states.

## 13.2 Component styling

- Dark graphite surfaces.
- Rounded cards, generally 10–14px.
- Subtle borders.
- Clear typography hierarchy.
- Compact metadata.
- Consistent Lucide iconography.
- Clear visual indicators for maintenance status.
- Consistent spacing and alignment.

## 13.3 Responsive behavior

The application must support desktop, tablet and mobile layouts.

Dashboard information should adapt to available screen space.

Tables should become mobile-friendly cards or horizontally scrollable views where appropriate.

Forms should remain usable on mobile without excessive scrolling or unnecessarily complex layouts.

## 13.4 Appearance preference

The shared selector is present in the authenticated shell and on login and
registration. Each Workshop origin stores its own versioned preference; it
does not synchronize with the public site, Shooting Range, Workshop account, or
the existing culture preference. The bootstrap sets the document's resolved
theme before client startup. The installation manifest and service-worker
lifecycle colors remain static and unchanged.

Workshop unit suites and the checked production build have passed during their
respective implementation gates. Screenshot-based responsive review, deployed CSP,
and real-device offline preparation, reconnect, eviction, and sign-out verification
remain release gates; the local environment had no Chromium-based browser available.

---

# 14. MongoDB Data Architecture

MongoDB is the application's document database.

The Architect must design the persistence model around document-oriented access patterns rather than simply translating relational tables into collections.

## 14.1 Proposed collections

| Collection | Responsibility |
|---|---|
| firearms | User firearm registry |
| componentDefinitions | Reusable component catalogue |
| componentInstances | Physical component inventory |
| componentInstallations | Installation and removal history |
| maintenanceEvents | Maintenance records |
| componentUsageEvents | Component usage and counter history |
| ammoRecipes | Recipe records and current version |
| ammoBatches | Production batches and recipe snapshots |
| ammoAllocations | Ammunition allocation records |
| shootingSessions | Shooting session records |
| activityEvents | Consolidated historical activity |

The Architect may adjust this structure according to MongoDB best practices and the existing repository conventions.

## 14.2 Embedding versus references

Use references for independently managed entities:

- Firearms.
- Component definitions.
- Physical component instances.
- Ammunition batches.
- Recipes.

Use embedded documents for data that belongs to a parent record and must remain historically consistent.

Examples:

**Recipe snapshot in an ammunition batch**

The production batch should contain an immutable snapshot of the recipe used at production time.

**Consumption records in a shooting session**

A shooting session can contain an array of batch consumption entries because these entries belong to the same shooting event.

**Installation metadata**

Installation and removal information should be preserved as historical records rather than overwritten on the component instance.

Avoid unnecessarily embedding unbounded arrays of historical events into firearm or component documents.

## 14.3 Document ownership

All domain documents must be associated with their owning user according to the existing application conventions.

User-specific data must remain logically isolated.

## 14.4 Counters and derived values

Counters such as:
- Firearm total rounds.
- Component accumulated rounds.
- Ammunition remaining stock.
- Allocated ammunition.
- Remaining maintenance interval.

Must have a clearly defined source of truth.

The preferred approach is to maintain historical events as the authoritative record and maintain aggregate values as derived or cached values where useful.

The Architect must define consistency rules between historical records and aggregate values.

---

# 15. Core Domain Invariants

The following rules are mandatory and must be preserved throughout the application.

## Equipment

1. A component belongs to a user's inventory.
2. A component is independent of a firearm.
3. A component has its own accumulated usage.
4. A component can only be installed in one firearm at a time.
5. Component movement never resets its accumulated counter.
6. Installation history is preserved.
7. Multiple identical components must remain individually identifiable.

## Maintenance

8. Tracking mode is configurable per component instance.
9. Service intervals are user-defined.
10. A component without a threshold still accumulates usage when applicable.
11. Maintenance history must remain available.

## Ammunition

12. Every production session creates a distinct batch.
13. Every batch retains the recipe snapshot used at production.
14. A batch can be allocated to multiple firearms.
15. Allocation is not consumption.
16. Unassigned ammunition remains available.
17. Consumption cannot exceed the available batch quantity.
18. Recipe modifications must not affect historical batches.

## Shooting sessions

19. A shooting session increments the firearm counter.
20. A shooting session increments the counters of the relevant installed components.
21. Ammunition consumption reduces the corresponding batch stock.
22. Untracked ammunition can still be used to record firearm usage.
23. Historical corrections must account for previous counter contributions.
24. Duplicate session processing must not increment counters twice.

## Privacy

25. No serial number or matricola is collected.
26. Internal identifiers must not expose real-world firearm identifiers.

---

# 16. Functional Acceptance Criteria

The implementation must be validated against the following scenarios.

### Scenario A — Register multiple firearms

Given a user with an existing account,

When they register multiple firearms,

Then each firearm must have its own record, independent counter and component inventory relationship.

### Scenario B — Multiple identical components

Given a component definition named Extractor,

When the user creates three physical extractors,

Then each extractor must have an independent counter, status and identification note.

### Scenario C — Move a component between firearms

Given an extractor installed in Firearm A with 6,000 accumulated rounds,

When it is removed and installed in Firearm B,

Then its counter must remain 6,000 and the previous installation must remain accessible.

### Scenario D — Track a component interval

Given a component with a configured interval of 10,000 rounds,

When its accumulated usage reaches 9,000 rounds,

Then the application must display 90% interval consumption and 1,000 remaining rounds.

### Scenario E — Produce ammunition

Given an existing recipe,

When the user records a production session of 250 rounds,

Then a new batch must be created with 250 produced rounds and the recipe snapshot.

### Scenario F — Allocate ammunition later

Given a batch containing 500 available rounds,

When the user allocates 200 to Firearm A and 150 to Firearm B,

Then 150 rounds must remain unassigned.

### Scenario G — Record a shooting session

Given a firearm with installed components and tracked ammunition,

When the user records a shooting session consuming 150 rounds,

Then:
- The firearm counter increases by 150.
- Each relevant installed component counter increases by 150.
- The consumed ammunition batch quantities decrease by the corresponding amounts.
- The session appears in historical activity.

### Scenario H — Modify a recipe

Given a recipe used to produce an existing batch,

When the user modifies the recipe,

Then the existing batch must retain the original recipe snapshot.

### Scenario I — Replace a component

Given an installed component,

When the user removes it and installs a different physical component,

Then the previous component must retain its counter and installation history, while the new component maintains its own independent counter.

### Scenario J — Record untracked ammunition consumption

Given a firearm and its installed components,

When the user records a shooting session without identifying the ammunition batches,

Then firearm and component counters must still be updated without changing ammunition inventory.

---

# 17. Out of Scope

The following functionality is explicitly excluded from this product specification:

- Firearm serial number or registration management.
- Firearm licensing or legal documentation.
- Public firearm profiles.
- Social sharing of personal equipment inventories.
- Community recipe sharing.
- Ammunition load calculations.
- Powder charge recommendations.
- Load safety validation.
- Ballistic calculations.
- Automated ammunition recipe optimization.
- Manufacturer-certified maintenance recommendations.
- Marketplace or commercial ammunition sales.

The application is a private management and recordkeeping tool.

---

# 18. Architect Deliverables

The Architect agent must inspect the existing repository and produce an implementation plan covering the complete specification.

The expected architectural output should include:

1. **Repository assessment**
   - Existing frontend architecture.
   - Existing backend architecture.
   - Existing MongoDB conventions.
   - Existing domain and persistence patterns.
   - Existing application integration points.

2. **Domain design**
   - Complete domain entities.
   - Relationships and references.
   - MongoDB document structures.
   - Embedded versus referenced data decisions.
   - Domain invariants.

3. **API design**
   - Endpoint definitions.
   - Request and response contracts.
   - Domain operation boundaries.
   - Validation requirements.
   - Historical correction operations.

4. **Frontend architecture**
   - Application navigation.
   - Page hierarchy.
   - Reusable components.
   - Dashboard widgets.
   - Forms and data-entry workflows.
   - Responsive layouts.

5. **Data consistency**
   - Counter aggregation rules.
   - Ammunition stock calculations.
   - Component installation attribution.
   - Historical snapshots.
   - Correction and reversal behavior.

6. **Implementation breakdown**
   - Backend tasks.
   - Frontend tasks.
   - Database tasks.
   - Integration tasks.
   - Functional test scenarios.

7. **Risk assessment**
   - Potential data consistency issues.
   - Historical data integrity.
   - Component movement edge cases.
   - Ammunition stock reconciliation.
   - MongoDB document growth considerations.

The implementation plan must cover the entire product scope. It must not reduce the requirements to an MVP or postpone any of the specified functional domains.

The Architect should make necessary technical decisions based on the existing codebase, documenting deviations from this specification and their rationale.

---

# 19. Definition of Done

The application is considered complete when:

- All specified functional domains are implemented.
- Firearms can be registered and maintained.
- Physical components can be tracked independently of firearms.
- Component installation and removal history is preserved.
- Component counters update correctly.
- Configurable maintenance intervals work.
- Recipes can be created and maintained.
- Historical recipe snapshots are preserved.
- Production sessions create ammunition batches.
- Ammunition batches can be allocated to multiple firearms.
- Shooting sessions update firearm and component counters.
- Ammunition consumption updates inventory correctly.
- Historical records remain consistent after corrections.
- Dashboard statistics reflect the underlying records.
- All major workflows are usable on desktop and mobile.
- The application follows the MorWalPiz black / white / #FFFF36 visual identity.
- Existing application infrastructure and conventions are respected.

**Final product objective:**

A single application that allows a shooter to understand what equipment they own, how much each physical component has been used, what maintenance has been recorded, how much ammunition they have produced, where that ammunition has been allocated and how much they have actually fired.

The application should provide a reliable historical record of the user's shooting equipment and activity without collecting unnecessary sensitive firearm identification information.