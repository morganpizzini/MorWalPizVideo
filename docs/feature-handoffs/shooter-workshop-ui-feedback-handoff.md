# Shooter Workshop UI Feedback Handoff

> **Component decisions superseded:** The accepted user-check corrections in
> [Shooter Workshop component feedback handoff](./shooter-workshop-component-feedback-handoff.md)
> replace this document's definition-owned compatibility tags, required immediate
> firearm installation, and deferred component edit/archive assumptions. Unrelated
> sections of this handoff remain applicable.

## Outcome

Refine the existing private Shooter Workshop application so component, firearm,
maintenance, recipe, shooting, history, and account-session workflows match the
reported user expectations without weakening the accepted immutable-ledger model.
This is an Impeccable **Operate** refinement of the existing MorWalPiz visual system,
not a redesign.

Assumptions used by this handoff:

- The user's "weapon" is the existing `Firearm` aggregate. Technical contracts keep
  that established name.
- Compatibility tags such as `2011`, `CZ`, `Tanfoglio`, `CZ Shadow`, and `CZ TS`
  describe a reusable component definition, not one physical instance.
- A newly created physical component is immediately installed on the selected
  firearm, while still remaining visible in the owner's component inventory.
- "The session is not kept" means the authenticated public-account session is lost
  on browser reload, not that an unfinished shooting form must be restored.
- Component-inventory filtering is by compatibility tag. Catalogue and inventory
  remain on the Components route but become separate, independently scannable
  panels.
- OAL is an invariant decimal value with at most two fractional digits. Existing
  string-backed MongoDB values require tolerant reads during migration.

Implementation must stop at the feedback listed here. Recipe editing, component
definition editing/archiving, shooting-session draft persistence, and unrelated
Workshop baseline gaps remain separate work unless directly required by the changes
below.

## Current Evidence

- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` loads
  definitions and physical inventory independently. Successful definition and
  component creation close/reset forms but do not reload either resource.
- The same component route sends only `definitionId` and `identificationNote`.
  `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` performs an account-owned
  definition lookup and emits `Component definition was not found.` when that lookup
  fails. Static inspection does not prove why the reported request fails; the actual
  request payload, selected ID, account claim, and API response must be captured in a
  focused regression test before changing validation.
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` already models
  component-to-firearm ownership over time through `ComponentInstallation`.
  `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` exposes
  install/remove commands, but `frontend/fe-packages/services/src/workshopService.ts`
  and the Workshop SPA do not expose them.
- Firearms currently have one optional free-text `Category`. Component definitions
  and physical instances have no compatibility tags.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` shows
  firearm counters and management actions but does not query or render current
  installations.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` allows only
  component maintenance and asks the user for `roundCountAtEvent`. The backend
  accepts either a firearm or component and persists the supplied counter unchanged.
- Firearm creation has no initial-round-count or idempotency field. Current totals are
  rebuildable from `WorkshopOperation` deltas under ADR-027, so setting
  `Firearm.TotalRounds` directly would be erased by reconciliation.
- Recipes already persist primer. OAL is currently a string across
  `MorWalPiz.Contracts`, `RecipeSnapshot`, `AmmoRecipe`, and TypeScript models.
  `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` does not render
  primer or OAL inputs and recipe cards expose only caliber, version, powder, and
  status.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx`
  exposes raw `Consumption records JSON` and `Usage segments JSON` textareas.
  Positive sessions with the default empty usage array conflict with server
  validation.
- `WorkshopService.RecordShootingAsync` requires the sum of all usage segments to
  equal the firearm's session rounds. That invariant cannot represent several
  simultaneously installed components each receiving the full firearm round delta.
- ADR-027 requires accepted historical installation attribution to be persisted in
  ledger facts rather than inferred during replay. It does not require the browser to
  submit that attribution.
- `frontend/shooter-workshop.client/src/routes/shared.tsx` renders raw activity type
  and operation ID. Numeric enum serialization or absent user-facing metadata can
  therefore produce the reported numeric/undefined output.
- `AuthProvider` restores the account by calling `GET /api/v1/account/session` and
  `AppShell` waits for that call before redirecting. The session cookie has no
  explicit `MaxAge`/`Expires`, so it is a browser-session cookie. Production also
  builds a direct `VITE_API_BASE_URL`; if that host is outside the
  `morwalpiz.com` site, browser third-party-cookie policy can accept an in-memory
  login response while withholding the cookie on reload.
- Workshop product behavior belongs to ServerAPI, Domain, Models, Contracts, and
  `shooter-workshop.client`. `MorWalPizVideo.ShootingRange` is a separate range
  booking application and is not impacted.

## Proposed Design

### Component catalogue and inventory

Add normalized `CompatibleFirearmTags` to `ComponentDefinition`. Store display values
after trimming and whitespace normalization, compare case-insensitively, reject empty
or duplicate tags, and cap both tag count and individual length at contract/domain
boundaries. Existing definitions deserialize with an empty collection.

The definition form uses an accessible token input. Typing filters suggestions
aggregated from already loaded account-owned definition tags. Comma, Enter, or blur
commits a token; Backspace/removal controls delete tokens; committed values render as
badges. The UI must not require a new suggestion endpoint because all definitions are
already loaded for the page.

Split the route into a compact catalogue panel and a larger inventory panel. The
inventory panel shows definition name, compatibility badges, identification note,
installed firearm, status, and total rounds. A multi-select/tag filter applies
client-side to the loaded personal inventory and exposes a clear-filter action and an
empty-filter result. This change does not add server paging or Mongo indexes.

Make resource refresh explicit after successful mutation. Definition creation reloads
definitions before the selector is reused. Physical creation reloads inventory,
definitions, firearms, and installation views that are currently mounted. Mutation
errors remain visible through `WorkshopForm`; no success-shaped fallback is allowed.

### Create-and-install physical component

Extend the physical-component creation request with required `firearmId`,
`installedAt`, and `idempotencyKey`. The UI workflow is:

1. Select a non-archived component definition.
2. Review its compatibility badges.
3. Select a non-archived firearm.
4. Enter the optional identification note.
5. Save once.

The Domain service owns one idempotent create-and-install orchestration. It uses
deterministic identities derived from the account and idempotency key, validates the
definition and firearm under the same account, rejects archived firearms, persists
the component and installation, and recovers safely if a retry finds only one
projection record. Do not implement this as two independent browser mutations.

The persisted component continues to belong to inventory. Its `Installed` status and
active `ComponentInstallation` make it visible both in the inventory panel and the
firearm detail. Add account-filtered installation reads returning explicit Contracts
DTOs enriched with stable IDs and current display labels; do not expose persistence
entities.

The original definition-not-found response remains authoritative. Add tests that
prove a definition returned to the same account can immediately be used by
create-and-install, while a missing or cross-account definition produces the existing
validation failure. This discriminates stale UI state from a backend ownership bug.

### Maintenance

Replace the component-only selector with a target-kind choice (`Firearm` or
`Component`) and the corresponding account-owned selector. Exactly one target is sent.
Remove the round-count input from the browser and from the authoritative create
contract.

At command execution, Domain derives `RoundCountAtEvent` from immutable operation
deltas for the selected aggregate at or before the maintenance effective date. This
supports backdated maintenance and records the count after the previous registered
shooting operation, rather than trusting the current projection or browser input.
Persist the derived snapshot on `MaintenanceEvent` so future display does not need to
recalculate it.

Add an account-filtered maintenance-history read and show recent records for both
target kinds with target label, event type, effective date, derived rounds,
description, and notes. Firearm detail consumes the same read contract filtered to
that firearm.

### Initial firearm round count

Add non-negative `InitialRoundCount` and `IdempotencyKey` to firearm creation. A zero
value remains the default. A positive value is represented by a firearm round delta
in the immutable operation ledger with a specific initial-count reason/source; it is
never assigned only to the projection.

Make firearm creation idempotent with a deterministic firearm identity so recovery can
complete a missing projection after a durable ledger append and identical retries
return the same firearm. A reused key with a different payload is a conflict.
Reconciliation must rebuild the initial count together with later shooting deltas.

### Recipes

Change OAL to a nullable decimal domain value with at most two fractional digits.
Contracts accept legacy numeric strings and JSON numbers during a compatibility
window, normalize using invariant culture, and serialize the canonical response as a
number. MongoDB uses a tolerant serializer that reads existing string or numeric BSON
and writes the canonical decimal representation. Recipe snapshots and version
documents use the same representation.

The recipe form exposes primer as free text and OAL as a decimal input with a `0.01`
step, while backend validation remains authoritative. Reject negative values,
non-numeric values, and more than two decimal places rather than rounding silently.

Recipe cards add primer, OAL, powder with weight, bullet weight/form when present,
status, and version without becoming full detail views. Clicking a card opens an
accessible modal with all recipe fields and notes. The card remains keyboard
activatable, the modal traps/restores focus, Escape and a labelled close button close
it, and missing optional fields have intentional labels rather than `undefined`.

### Shooting sessions and automatic component usage

Remove both raw JSON textareas. Replace ammunition consumption JSON with optional
structured rows selecting an available batch/allocation and quantity. The UI computes
the remaining untracked quantity and prevents known plus untracked ammunition from
exceeding total rounds.

Stop accepting browser-authored component usage as authority. At recording time,
Domain queries every installation active for the selected firearm at the session
effective date. It creates one immutable usage fact and one component round delta for
each active installation, each with the full session round count. No active component
is valid and simply produces no component delta. Overlapping active installations for
the same physical component remain an explicit validation error.

Persist the derived `ComponentId`, `InstallationId`, and quantity in the shooting
session and operation bundle. Replay continues to use those accepted facts and never
consults current installation state. Remove the incorrect global
`usageSegments.Sum == totalRounds` invariant; validate each derived installation
segment against the session total instead.

Apply the same derivation to correction replacements after reversing the original
operation. The correction UI must select the original record from a readable
shooting-history list rather than require a raw operation ID; a manual ID is not part
of this feedback scope.

### History

Return a dedicated activity contract rather than serialized persistence entities.
Include string `operationType`, effective time, source IDs, and a non-localized
structured summary containing stable entity labels captured when the activity is
created. New activities store enough snapshot metadata to remain understandable after
an entity is archived or renamed.

For existing activity documents without summary metadata, the API emits a typed
generic fallback based on operation type. The frontend maps known operation types to
localized titles and readable descriptions. Operation IDs move to secondary,
copyable diagnostic metadata and are never the card title. Unknown future operation
types render a safe localized fallback, not `undefined` or a number.

### Account-session reload

First add browser-level evidence for login followed by hard reload and direct
navigation to a protected route. Verify whether the session cookie is absent,
rejected, expired, or withheld from `GET /api/v1/account/session`.

Production should expose ServerAPI to the Workshop browser through a same-origin
`/api` route on `https://workshop.morwalpiz.com`, using the existing client ingress or
nginx reverse proxy. The SPA's production API base becomes relative; local AppHost
may retain its explicit development API origin. The proxy forwards standard
forwarded headers and does not log cookies or authorization material.

Set an explicit session-cookie lifetime aligned with the persisted public-account
session lifetime so a normal browser restart/reload preserves authentication until
server-side expiry or logout. Keep `HttpOnly`, `Secure`, least-privilege path,
credentialed requests, CSRF validation, and server-side revocation. Do not move the
session token to `localStorage`.

`AuthProvider` continues to block protected rendering while restoration is pending.
The login page honors `returnUrl` after successful login, restricted to local
application paths to prevent open redirects.

### Impeccable workflow

This is an established **Operate** surface. Use the incumbent
`frontend/morwalpizvideo.client/DESIGN.md` as visual authority until the finished
Workshop surface is documented separately. Run `shape` during implementation only to
confirm the component create/install sequence, inventory filter hierarchy,
maintenance target switch, recipe modal, and structured shooting form. Then use
`harden`, `adapt`, and `audit`; do not initiate a new visual world.

Minimum visual evidence:

- Components at desktop, tablet, 480 px, and 360 px with empty, populated,
  many-item, filtered-empty, validation-error, and save-success states.
- Maintenance with firearm and component targets, plus readable derived counts.
- Recipe grid and modal with complete and sparse records.
- Shooting form with no installed components, several installed components, tracked
  ammunition, untracked ammunition, and validation errors.
- History with every current operation type and one unknown fallback.
- Login restoration during loading, successful hard reload, expired session, and
  unavailable API states.

All token inputs, badges, cards, modal controls, filters, and selectors require
keyboard operation, visible focus, associated validation text, touch-safe targets,
and no page-level horizontal overflow.

## Impacted Projects

- `MorWalPizVideo.Models` — additive compatibility tags and activity metadata,
  decimal OAL persistence compatibility, and ledger operation representation.
- `MorWalPizVideo.Domain` — create/install orchestration, installation and
  maintenance reads, as-of counter derivation, initial-count ledger contribution,
  shooting attribution, history projection, repository queries, and Mongo
  serialization/index review.
- `MorWalPiz.Contracts` — explicit request/response DTO updates and decimal OAL
  compatibility.
- `MorWalPizVideo.ServerAPI` — Workshop endpoints and public-account cookie lifetime.
- `MorWalPizVideo.MvcHelpers` — only if shared cookie option construction is extended
  to carry an explicit lifetime without affecting other public-account consumers.
- `MorWalPizVideo.BackOffice.Tests` — Domain/replay/controller behavior and account
  isolation coverage.
- `frontend/fe-packages/models` — matching Workshop contracts.
- `frontend/fe-packages/services` — installation, maintenance-history, guided
  shooting, activity, and revised create endpoints.
- `frontend/shooter-workshop.client` — all requested UI workflows and focused
  component/auth tests.
- `MorWalPizVideo.AppHost` — verify-only for the direct local development API path.
- Workshop deployment workflow/container — same-origin production API routing and
  browser-authentication proof.
- `MorWalPizVideo.BackOffice` — verify-only unless a new stable Mongo index is
  approved for the central operational manifest.
- `MorWalPizVideo.ShootingRange` and its tests/client — not applicable; it does not
  own Workshop behavior.

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs` — tags, decimal OAL,
  activity summaries, and any new operation type/source metadata.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` — revised commands
  and dedicated installation, maintenance, history, and shooting response DTOs.
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs` — all domain orchestration and
  validation described above.
- `MorWalPizVideo.Domain/Workshop/WorkshopReplay.cs` — initial-count replay coverage;
  preserve old schema-version interpretation.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — bounded account-owned
  installation, maintenance, shooting, and activity queries plus any reviewed index.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` — endpoint
  mappings and explicit DTO projections.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` — persistent
  cookie lifetime aligned with the server session.
- `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs` —
  cookie-option lifetime parameter only if needed by the controller.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs` — command,
  validation, retry, account-isolation, and derivation scenarios.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopReplayTests.cs` — initial counts,
  several installed components, correction reversal, and old-event replay.
- `frontend/fe-packages/models/src/workshop.ts` — revised browser contracts.
- `frontend/fe-packages/services/src/workshopService.ts` — new reads and command
  shapes.
- `frontend/fe-packages/services/src/endpoints-frontend.ts` — endpoint constants.
- `frontend/fe-packages/services/src/workshopService.test.ts` — transport shape,
  credentials, CSRF, and compatibility cases.
- `frontend/shooter-workshop.client/src/routes/Components/component.tsx` — refreshed
  resources, tokenized tags, create/install flow, separate inventory panel, filters.
- `frontend/shooter-workshop.client/src/routes/Firearms/component.tsx` — initial
  round count and idempotent create.
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/component.tsx` —
  installed components and maintenance history.
- `frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx` — target
  switch, no counter input, readable history.
- `frontend/shooter-workshop.client/src/routes/Recipes/component.tsx` — complete
  inputs, richer cards, and modal.
- `frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx` —
  structured ammunition input and server-derived component usage.
- `frontend/shooter-workshop.client/src/routes/History/component.tsx` and
  `frontend/shooter-workshop.client/src/routes/shared.tsx` — readable activity cards.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx`,
  `frontend/shooter-workshop.client/src/layouts/AppShell.tsx`, and
  `frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx` — reload,
  unavailable-session, and safe return-path behavior.
- `frontend/shooter-workshop.client/src/i18n.tsx` — all user-facing labels,
  operation titles, validation, empty states, and fallbacks.
- `frontend/shooter-workshop.client/src/styles.scss` — separate panel layout,
  tag/token controls, filters, cards, modal, and responsive states.
- `frontend/shooter-workshop.client/nginx.conf`,
  `frontend/shooter-workshop.client/Dockerfile`, and
  `.github/workflows/deploy-shooter-workshop-client.yml` — same-origin production
  API route and environment-owned upstream.
- `docs/architecture/shooter-workshop.md` — accepted behavior for compatibility tags,
  create/install, derived maintenance counts, initial counts, and shooting
  attribution.
- `docs/architecture/adr/ADR-027-shooter-workshop-immutable-ledger-reconciliation.md`
  — clarify initial-count and server-derived installation attribution decisions.
- `docs/architecture/security.md` and `docs/architecture/deployment.md` —
  same-origin Workshop API routing and cookie lifetime.

### Add

- Focused route/component tests beside Components, Maintenance, Recipes,
  ShootingSessions, History, and authentication using the existing Vitest/Testing
  Library convention.
- A dedicated MongoDB OAL compatibility serializer in the existing Models
  serializer/converter location if no suitable repository serializer exists.
- A browser authentication test in the existing ServerAPI/Workshop integration test
  area proving login, reload, protected request, expiry, and logout.

### Generated/Migration

- No EF migration.
- MongoDB schema changes are additive except OAL representation. Use tolerant
  dual-read/canonical-write first; an idempotent operational backfill may later
  convert legacy string OAL values after audit. Do not require that backfill for the
  compatible reader to deploy.
- If query evidence requires an installation or maintenance index, add its stable
  definition to `WorkshopMongoIndexes`, the central operational manifest, its
  reviewed snapshot, and manifest tests. Do not add speculative indexes.

### Documentation

- This handoff is the implementation source of truth for the feedback batch.
- Update the existing Workshop handoff after implementation with actual files,
  validation evidence, rollout state, and residual risks.

### Verify Only

- `MorWalPizVideo.ServerAPI/Program.cs` — account CORS and Workshop DI remain valid.
- `MorWalPizVideo.AppHost/Program.cs` — local Workshop client/API routing remains
  explicit and functional.
- `frontend/package.json` — shared package build order remains models, services,
  layout, client.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs` — no change
  unless a reviewed stable index is added.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Revised component/firearm/maintenance/shooting commands; new account-owned installation and maintenance reads; dedicated history DTOs. |
| Frontend/UI | Major refinement of seven Workshop workflows; no new application or global state library. |
| Database/Data | Additive tags/activity metadata; ledger-backed initial count; decimal OAL compatibility; no destructive migration. |
| Contracts/Models | Coordinated .NET and TypeScript changes; persistence entities remain internal. |
| Authentication/Authorization | Same public-account claim ownership, CSRF, and account filtering; same-origin production routing and persistent cookie lifetime. |
| Validation | Normalized tags, exactly one maintenance target, non-negative initial count, decimal scale <= 2, account-owned selections, archived-firearm rejection, unambiguous installation timelines. |
| Cache | Not applicable; Workshop reads currently have no output-cache contract. Client resources reload after mutation. |
| Configuration/Secrets | Production API upstream remains environment-owned and non-secret; no credentials enter source or workflow text. |
| Logging/Telemetry | Add operation-safe event IDs and failure stage only; exclude cookie values, account email, notes, recipe values, and firearm free text. |
| Background Jobs | Not applicable; commands and personal-account replay stay synchronous. |
| Deployment/Operations | Models/API deploy before the coordinated client; same-origin proxy and cookie behavior require browser proof. |
| Backward Compatibility | Existing tagless/activity documents remain readable; old operation schema replays unchanged; OAL accepts legacy strings during transition. |

## Reuse Opportunities

- Reuse `useResource.reload` and make its completion/error semantics explicit rather
  than adding a second client cache.
- Reuse `WorkshopForm` for pending and field/general error presentation.
- Reuse existing account transport, CSRF acquisition, public-account authentication,
  and claim-derived ownership.
- Reuse `ComponentInstallation` as the only physical-component/firearm relationship.
- Reuse immutable `WorkshopOperation` deltas, `WorkshopReplay`, deterministic stable
  IDs, and existing idempotency conflict rules.
- Reuse recipe snapshots so historical production never changes when a recipe is
  edited or migrated.
- Reuse existing MorWalPiz tokens, cards, badges, modal conventions, and responsive
  breakpoints; keep Workshop-specific workflows app-local.
- Reuse current mock repository and ServerAPI test factory; extend them for new
  records rather than introducing a second persistence abstraction.

## Risks

| Rank | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Initial count written only to `Firearm.TotalRounds` would be erased by replay. | Persist a ledger delta and make creation idempotent/recoverable. | Full reconciliation preserves initial plus session rounds. |
| High | Browser-derived usage can omit components or forge installation IDs. | Derive all active installations server-side at effective time and persist accepted facts. | One session increments firearm and every active component exactly once; replay matches. |
| High | Cross-site cookie blocking can keep login only in React memory. | Same-origin production `/api`, explicit lifetime, hard-reload browser test. | Cookie is sent after hard reload and protected route stays authenticated. |
| High | Changing OAL from BSON string to decimal can break deserialization or old deployments. | Dual-read/canonical-write serializer and coordinated deployment; retain rollback-compatible API reader. | Mixed legacy/canonical documents load, round-trip, and snapshot correctly. |
| Medium | Create-and-install can partially persist without Mongo transactions. | Deterministic IDs, one idempotent command, explicit recovery for each failure stage. | Fault-injection retries converge to one component and one active installation. |
| Medium | Backdated sessions/maintenance can select the wrong installation/count. | Query and replay by effective timestamp; reject overlap/ambiguity. | Tests cover before install, active interval, removal boundary, and later reinstall. |
| Medium | Case/spacing variants fragment compatibility filters. | Domain normalization and case-insensitive uniqueness. | `CZ`, ` cz `, and duplicate entry resolve predictably. |
| Medium | Activity enrichment from current records loses labels after rename/archive. | Persist structured label snapshots for new events; typed fallback for old events. | Archived/renamed entity history remains readable and never renders undefined. |
| Low | Client-side filtering becomes slow for unusually large personal inventories. | Memoized filtering and bounded rendering observation; defer server paging until measured. | Representative maximum list remains responsive at target devices. |
| Low | Same-origin proxy misconfiguration hides API failures behind SPA fallback. | Explicit `/api/` location, upstream health proof, status/header preservation. | API 401/403/5xx remain API responses and never return `index.html`. |

## Technical Debt

- Firearm update currently requires an idempotency key in .NET but the TypeScript
  update contract and edit form omit it. This is directly coupled to the touched
  firearm workflow and must be corrected now.
- Workshop create-definition, create-component, create-recipe, and create-firearm
  operations are not uniformly ledger-backed/idempotent. Only firearm creation must
  change now because initial counters require ledger authority; broader normalization
  is deferred.
- History pagination currently loads all account activity before paging. Keep this
  feedback implementation bounded, but use repository-side paging if the new history
  contract work already requires changing the query abstraction.
- Component definitions have an archive field but no edit/archive workflow. Tags are
  entered on create in this scope; editing existing definitions is deferred.
- Shooting correction currently requires a raw operation ID. Replace it with selection
  from readable shooting history only if needed to remove the raw-ID dependency from
  the corrected structured form; broader correction UX is deferred.
- Workshop lacks its own `PRODUCT.md` and `DESIGN.md`. This does not block the
  refinement because the public-client design authority and Workshop handoffs exist.
  Use Impeccable `document` after the finished UI is accepted, not during this plan.

## Implementation Roadmap

1. **Lock regression evidence.** Add failing service/controller/client tests for
   immediate definition use, create-and-install retry stages, hard-reload auth,
   numeric/undefined history, OAL scale, maintenance derivation, and several installed
   components. Capture the exact reported definition-not-found request before changing
   validation.
2. **Evolve persistence compatibility.** Add definition tags, activity summary
   metadata, decimal OAL tolerant reads, and any new versioned operation metadata.
   Prove old documents and schema-version-1 operations remain readable/replayable.
3. **Evolve .NET contracts.** Introduce idempotent firearm and component commands,
   dedicated installation/maintenance/history DTOs, decimal OAL validation, and
   browser-free shooting attribution. Preserve account IDs as claim-derived only.
4. **Implement Domain invariants.** Add normalized tags, deterministic create/install,
   as-of maintenance counts, ledger-backed initial counts, server-derived shooting
   usage, correction parity, and structured activity summaries. Gate on focused
   Domain/replay tests.
5. **Add bounded repository reads.** Query current/as-of installations, maintenance,
   shooting records, and activity pages under account ownership. Add indexes only
   after query-plan evidence and central-manifest review.
6. **Expose ServerAPI behavior.** Map DTOs explicitly, retain CSRF on mutations, return
   validation/conflict responses consistently, and add controller tests for
   cross-account IDs and malformed inputs.
7. **Update shared frontend packages.** Change TypeScript contracts first, then
   services/endpoints and transport tests. Keep the ordered models -> services build
   dependency.
8. **Fix component workflows.** Refresh definitions/inventory after save, implement
   accessible compatibility tokens, add firearm selection, call the single
   create-and-install command, split panels, filter by tags, and show current firearm
   on each inventory row.
9. **Fix firearm and maintenance workflows.** Add initial rounds and idempotency,
   render installed components on detail, support firearm/component maintenance,
   remove manual rounds, and show derived history.
10. **Fix recipe and shooting workflows.** Add primer/OAL, richer cards/modal,
    structured ammunition rows, automatic installed-component attribution, and
    correction parity.
11. **Fix history semantics.** Localize known operation titles and structured
    descriptions; retain operation ID only as secondary diagnostics; prove unknown and
    legacy events have readable fallbacks.
12. **Harden session restoration.** Deploy same-origin API routing, align cookie and
    persisted session lifetimes, preserve loading/error distinctions, and honor safe
    return paths. Gate on production-like browser reload tests.
13. **Run Impeccable refinement gates.** Apply `shape` assumptions, then `harden`,
    `adapt`, and `audit` to the named screens and evidence sizes. This is refinement,
    not redesign.
14. **Update architecture and delivery evidence.** Record actual files, schema
    compatibility, browser proof, validation results, and residual risks in the
    Workshop architecture, ADR, security/deployment docs, and handoff.

An independent post-validation review is warranted because this batch changes
authentication delivery, immutable counter attribution, and MongoDB representation.
The user may select `MorWalPiz Code Reviewer` after implementation.

## Testing Strategy

- **Domain/unit — `MorWalPizVideo.BackOffice.Tests`:**
  tag normalization; decimal parsing/scale; as-of round replay; deterministic IDs;
  idempotent payload conflicts; several installed components; no installed
  components; remove/reinstall boundaries; correction reversal/replacement; initial
  count plus sessions; legacy operation replay.
- **Repository/integration — `MorWalPizVideo.BackOffice.Tests`:**
  account-scoped definitions, installations, maintenance, shooting, and history;
  mixed legacy/canonical OAL documents; stable ordering and paging; cross-account IDs
  rejected.
- **Controller/security — ServerAPI test factory:**
  unauthenticated 401; missing/invalid CSRF 403; claim-derived account ownership;
  archived firearm rejection; invalid tag/OAL/input shapes; no owner field accepted.
- **Shared service contract — `@morwalpizvideo/services`:**
  exact endpoint/body/response shapes, credentials, CSRF, numeric OAL, installation
  reads, and absence of browser-authored usage segments.
- **Component — `shooter-workshop.client`:**
  post-save refresh; tag keyboard behavior/suggestions/filtering; create/install
  selection and errors; initial rounds; target-specific maintenance; modal focus and
  sparse fields; structured consumption; readable history fallbacks; auth loading,
  success, expiry, network failure, and safe return URL.
- **Migration compatibility:**
  load string, Decimal128, missing, and invalid OAL documents; canonical writes have
  <= 2 decimals; old tagless/activity documents render safely; rollback reader can
  consume all values written during the compatibility window.
- **End to end:**
  login -> hard reload -> create tagged definition -> create/install component ->
  verify inventory and firearm detail -> record shooting -> verify firearm/component
  counters -> record firearm maintenance -> verify derived count/history -> create
  recipe with primer/OAL -> open modal -> inspect readable chronological history.
- **Accessibility/responsive:**
  keyboard-only token input and modal, focus restoration, labels/errors, status
  announcements, contrast, 200% zoom, 360/480/tablet/desktop layouts, long tags,
  long notes, and large inventory.

## Migration And Rollback

- **MongoDB:** Deploy tolerant readers before canonical decimal writes. Tags and
  activity summaries are optional additive fields. Audit legacy OAL values; quarantine
  invalid values rather than coercing them. Any backfill is account-bounded,
  idempotent, observable, and reversible from a pre-change export.
- **Ledger:** Introduce a documented operation schema/version only if new event
  interpretation requires it. Old events remain immutable. Initial counts and derived
  component attribution are new facts; rollback ServerAPI only to a version that can
  deserialize them. Projection recovery always replays facts and never edits them.
- **API contracts:** Deploy compatible Models/Domain/ServerAPI before the client.
  During the window, accept legacy OAL strings and omitted new optional read fields.
  Do not allow an old client to send authoritative usage segments once server
  derivation is active.
- **Authentication:** Enable and verify the same-origin `/api` route before switching
  the production client to a relative API base. Retain the prior direct API origin for
  rapid routing rollback while keeping server sessions valid. Never roll back by
  exposing tokens to JavaScript storage.
- **Cache:** No server cache migration. A client rollback may show older layouts but
  must remain contract-compatible during the deployment window.
- **Configuration:** API upstream and session lifetime are environment-owned,
  non-secret settings. Validate them in staging before production. Missing settings
  must fail deployment/health checks rather than silently routing to the SPA.
- **Deployment order:** compatible persistence readers -> Domain/API -> same-origin
  route -> shared frontend packages/client -> optional backfill -> remove legacy write
  support only in a later approved release.

## Delivered implementation — 2026-11-14

The coordinated implementation batch is integrated across the existing Workshop
contracts, immutable ledger, ServerAPI, shared frontend packages, and SPA. It stayed
within this handoff's scope and did not introduce recipe editing, component-definition
editing/archiving, draft persistence, or unrelated baseline features.

Implemented:

- [`frontend/shooter-workshop.client/src/routes/Components/component.tsx`](../../frontend/shooter-workshop.client/src/routes/Components/component.tsx)
  now persists normalized compatibility tags, filters inventory by tag, refreshes both
  lists after saves, and creates a physical component with an immediately selected
  firearm through the idempotent create-and-install command.
- [`MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`](../../MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs),
  [`MorWalPizVideo.Domain/Workshop/WorkshopService.cs`](../../MorWalPizVideo/MorWalPizVideo.Domain/Workshop/WorkshopService.cs),
  [`MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`](../../MorWalPizVideo/MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs),
  and [`MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`](../../MorWalPizVideo/MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs)
  add additive compatibility tags, deterministic create/install retries, derived
  maintenance counts, server-derived installation attribution, activity summaries,
  tolerant OAL persistence, and ledger-backed initial firearm rounds.
- [`frontend/fe-packages/models/src/workshop.ts`](../../frontend/fe-packages/models/src/workshop.ts)
  and [`frontend/fe-packages/services/src/workshopService.ts`](../../frontend/fe-packages/services/src/workshopService.ts)
  expose the compatible request/response shapes and account/CSRF-preserving command
  transport.
- [`frontend/shooter-workshop.client/src/routes/Recipes/component.tsx`](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx)
  exposes the existing primer and OAL request fields, displays optional recipe data
  without `undefined`, and provides keyboard-accessible recipe details with Escape,
  close-button, focus-trap, and focus-restoration behavior.
- [`frontend/shooter-workshop.client/src/routes/Firearms/component.tsx`](../../frontend/shooter-workshop.client/src/routes/Firearms/component.tsx)
  accepts optional initial rounds, which are recorded through the backend ledger.
- [`frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx`](../../frontend/shooter-workshop.client/src/routes/Maintenance/component.tsx)
  supports firearm and component targets without asking the browser to author the
  derived round count.
- [`frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx`](../../frontend/shooter-workshop.client/src/routes/ShootingSessions/component.tsx)
  uses structured ammunition records and no longer asks the browser to author
  component installation attribution.
- [`frontend/shooter-workshop.client/src/routes/shared.tsx`](../../frontend/shooter-workshop.client/src/routes/shared.tsx)
  maps known string and numeric operation values to readable activity titles, keeps
  operation IDs as secondary diagnostics, and provides safe fallbacks for unknown or
  missing activity data.
- [`frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx`](../../frontend/shooter-workshop.client/src/routes/AccountLogin/component.tsx)
  and [`frontend/shooter-workshop.client/src/layouts/AppShell.tsx`](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx)
  preserve safe local return paths, query strings, and hashes through login and
  protected-route redirects without persisting tokens in browser storage.
- [`frontend/shooter-workshop.client/src/routes/shared.test.tsx`](../../frontend/shooter-workshop.client/src/routes/shared.test.tsx)
  covers the history title and fallback behavior.

Validation completed:

```text
dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj \
  --configuration Release --no-restore -m:1 -p:BuildInParallel=false
  Passed

dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj \
  --configuration Release --no-restore --no-build \
  --filter "FullyQualifiedName~WorkshopServiceTests|FullyQualifiedName~WorkshopReplayTests"
  Passed: 10 tests

cd frontend && yarn build:shared
  Passed

cd frontend && yarn workspace @morwalpizvideo/services test --run src/workshopService.test.ts
  Passed: 8 tests

cd frontend && yarn workspace shooter-workshop.client test --run
  Passed: 6 test files, 13 tests

cd frontend && yarn workspace shooter-workshop.client build
  Passed: TypeScript and Vite production build
```

Residual release work is operational rather than an untracked source omission:
installation-specific read projections are not yet exposed, so firearm detail cannot
show its current installations; definition tag editing remains outside this handoff;
recipe editing remains outside this handoff; and deployed same-origin proxy, cookie,
CSRF, hard-reload, custom-domain/TLS, index audit/apply, and rollback evidence still
require staging/production verification. Generated build output was not retained as a
source change.
- **Rollback limit:** After new ledger facts or canonical decimal documents are
  written, rollback is limited to builds that understand those representations.
  Frontend-only rollback remains safe while compatibility contracts are retained.

## Open Questions

- Confirm whether compatibility tags should be editable on existing definitions in
  this feedback batch. The default plan permits tags on new definitions and safely
  displays/filter existing tagless definitions; edit/archive remains deferred.
- Confirm the desired public-account persistence duration. The implementation should
  align the cookie with the existing server session lifetime, but product policy may
  choose a shorter duration.
- Confirm whether recipe OAL is measured in millimetres, inches, or user-selected
  units. The data model can store a decimal without a unit, but the UI must not imply
  an unverified unit.
