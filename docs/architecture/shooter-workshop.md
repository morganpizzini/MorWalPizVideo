# MorWalPiz Range Manager
## Technical Product Specification (TPS)

**Document purpose:** Product and technical specification for the Architect agent.

**Product type:** Personal equipment and ammunition management application.

**Deployment:** Dedicated subdomain.

**Database:** MongoDB (document-oriented database).

**Visual identity:** MorWalPiz — Black / White / Yellow (#FFFF36).

**Status:** Target specification and complete functional baseline.

**Implementation analysis:** [Shooter Workshop architecture handoff](../feature-handoffs/shooter-workshop-handoff.md).

---

## Implementation status

The current working tree contains the initial Workshop implementation baseline across
ServerAPI, Domain, Models, Contracts, the standalone frontend workspace, account
transport, AppHost, CI, and image-staging deployment metadata. The feature-owned
Mongo index definitions are included in the executable central operations manifest for
reviewed audit/apply. They are intentionally not part of the limited startup
blog-index bootstrap.

This status is implementation evidence, not release approval. Full domain acceptance
coverage, exact-origin browser authentication proof, duplicate/index audit and apply
evidence, Azure custom-domain/TLS/runtime configuration, and complete operational
rollback evidence remain open. BackOffice remains operational index governance only;
it has no Workshop product behavior or ownership.

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
- View ammunition allocations.
- View shooting history.
- Associate shooting sessions with a firearm.

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
| Manufacturer | String | No | Manufacturer |
| Model | String | No | Component model |
| Description | String | No | Description |
| DefaultServiceInterval | Number | No | Default user-defined interval |
| TrackingMode | Enum | Yes | Counter, Interval or Manual |
| Notes | String | No | Additional information |

The catalogue must be customizable by each user.

Users can create their own definitions without depending on a predefined global catalogue.

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
| Manufacturer | String | No | Optional override |
| Model | String | No | Optional override |
| TotalRounds | Number | Yes | Accumulated usage |
| ServiceInterval | Number | No | Instance-specific interval |
| TrackingMode | Enum | Yes | Counter, Interval or Manual |
| Status | Enum | Yes | Inventory, Installed, Maintenance, Retired |
| Notes | String | No | Personal notes |
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

Each component instance can have its own service interval.

The component definition can provide a default interval, but the physical instance can override it.

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

## Main navigation

| Navigation item | Main screen |
|---|---|
| Dashboard | Application overview |
| My Firearms | Firearm list and details |
| Components | Component catalogue and inventory |
| Maintenance | Component thresholds and maintenance history |
| Ammunition | Ammunition inventory and batches |
| Recipes | Recipe list and details |
| Sessions | Production and shooting history |

## 12.1 Dashboard

Displays the overview widgets and quick actions.

## 12.2 Firearms

List of registered firearms.

Each firearm detail page must include:

- General information.
- Total rounds.
- Installed components.
- Installation history.
- Maintenance history.
- Ammunition allocations.
- Shooting sessions.

## 12.3 Components

The component area must provide two distinct views:

**Catalogue**
- Component definitions.
- Default tracking configuration.
- Default service interval.

**My Inventory**
- Physical component instances.
- Identification notes.
- Accumulated rounds.
- Current status.
- Installation information.

The user must be able to navigate from a component definition to all its physical instances.

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

Ammunition inventory overview.

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

| Token | Value | Usage |
|---|---|---|
| Primary background | #101010 | Main background |
| Secondary background | #181818 | Cards and panels |
| Elevated background | #222222 | Elevated surfaces |
| Primary accent | #FFFF36 | Primary actions and highlights |
| Primary text | #FFFFFF | Titles and important text |
| Secondary text | #B8B8B8 | Descriptions |
| Muted text | #777777 | Metadata |
| Border | #383838 | Separators |
| Success | #42C77A | Informational positive status |
| Warning | #E9B949 | Approaching thresholds |
| Error | #EF4444 | Threshold reached or validation error |

Yellow must remain the recognizable brand accent.

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