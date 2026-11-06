# FreezerAnalysis: route organization and match-day refinements

Status: implementation delivered; the SPA test suite now passes on Node 26.
The repository-wide API suite remains blocked by an unrelated BlogInteractions
compile error.
Evidence inspected: 2026-11-05. This document is user-editable.

## Implementation record (2026-11-05)

Delivered in this pass:

- Added the route-facing `src/routes/config.tsx` boundary and switched the
  bootstrap to import it without changing route URLs or loader behavior.
  Extracted the page implementations into typed feature-owned modules:
  `routes/layout`, `account`, `matches`, `profile`, `identity-review`,
  `match-detail`, `verification`, and `field`. `src/routes.tsx` is now a
  compatibility export facade only; it no longer contains page bodies.
- Fixed comparison selection to preserve empty slots, eagerly load both
  selected details, distinguish duplicate display names by result ID, and
  surface detail failures.
- Fixed the save/freeze submitter decision by reading the actual clicked
  submit button.
- Made turn-note text optional, derived a cyclic stage order when only the
  starting stage is supplied, and removed the empty-array numeric-zero render.
  Existing operation IDs and offline queue payloads remain unchanged.
- Added authenticated draft reopen with an expected-revision compare-and-swap
  transition. Reopen clears only the working pointer and increments revision;
  snapshots remain immutable. Historical queued events are accepted when their
  snapshot belongs to the same draft, while ownership and idempotency checks
  remain enforced.
- Snapshots now retain division/power-factor presentation facts and card
  rendering prefers the latest turn context rather than a later plain note.
- Added owner-authorized `phone-front`/`phone-back` PNG retrieval from the
  existing ZIP artifact, with PNG signature validation, `image/png`, fixed
  attachment names, and the ZIP endpoint retained. The field page now exposes
  separate front/back download actions alongside the ZIP action.
- Added keyboard-focusable, touch-compatible contextual `?` help for result
  comparison and parameter labels, with Italian explanations and tooltip
  semantics.
- Updated the Vitest jsdom setup for Node 26: expose jsdom's own storage
  objects despite Node's conflicting undefined storage globals, and align
  router abort signals with Node's `Request` implementation.
- Added responsive comparison controls (two columns above 760px and stacked
  controls with a 14px gap below it) while retaining the incumbent visual
  language and action-row wrapping.

Files changed for this refinement include the FreezerAnalysis SPA route/API
client/styles/bootstrap, `FreezerAnalysis.Api` controller/coordinator,
FreezerAnalysis models, and both freezer repository adapters. No new package,
authentication, CSRF, or API version was introduced.

Validation evidence:

- `yarn --cwd frontend workspace freezer-analysis.spa build` passed after the
  final frontend edit (TypeScript and Vite production build).
- `yarn --cwd frontend/FreezerAnalysis.spa test --run` passes on Node
  `v26.13.0`: 4 test files, 22 tests.
- `cd frontend/FreezerAnalysis.spa && ../../frontend/node_modules/.bin/tsc
  --noEmit --project tsconfig.json` passes, including the test setup.
- The initial Node 26 run exposed two jsdom/global Web API incompatibilities:
  storage was filtered from the jsdom global because Node exposes an
  unavailable `localStorage` property, and Node's `Request` rejected jsdom's
  `AbortSignal`. Both are corrected in the test-only setup and regression
  coverage verifies storage and request-signal compatibility.
- `yarn --cwd frontend workspace freezer-analysis.spa build` passed before the
  final test-environment-only change; production UI code was unchanged by that
  change.
- Serial API test build was attempted after restore, but compilation is blocked
  by the pre-existing unrelated `BlogInteractionService` reference to missing
  `IPublicAccountRepository`; the freezer changes reached compilation only
  after an initial repository-brace correction and require rerun once that
  baseline work is repaired.

Remaining scope/risk: native-renderer browser download verification and real
Mongo concurrency evidence remain deployment/test-environment gates.

## Outcome

Split the large FreezerAnalysis route module by feature, following the public client's route-local organization. Fix HF athlete selection and responsive controls, explain analysis parameters, provide individual PNG downloads, simplify turn notes, remove the unexplained red zero, separate field actions, and allow returning to an editable plan.

Assumptions:

- This is a refinement, not a redesign. Keep Italian copy, the existing CSS grid, account transport, URLs, and offline capabilities.
- "De-freeze" means reopen the working draft while retaining every historical frozen snapshot, input package, forecast, verification, artifact, and note. It never edits or deletes a frozen baseline.
- Individual images mean the existing phone front/back PNGs, downloaded through separate explicit actions. Preserve the full ZIP download for desktop use.
- For a turn note, structured start information is sufficient; descriptive text is optional.

Implementation boundary: only this organization and these interactions, including the persistence consequences of reopening. No engine changes, authentication migration, generic component library, dependency additions, parser changes, production activation, or broad coordinator rewrite.

## Current Evidence

Repository-relative links below identify current source, not planned behavior.

- [Public route configuration](../../frontend/morwalpizvideo.client/src/routes/config.tsx) imports feature-local `Component`, `loader`, and applicable `action` modules. Its [bootstrap](../../frontend/morwalpizvideo.client/src/main.tsx) constructs the router from that configuration.
- [Freezer bootstrap](../../frontend/FreezerAnalysis.spa/src/main.tsx) embeds the route tree and loaders. [Freezer routes](../../frontend/FreezerAnalysis.spa/src/routes.tsx) contains shell, login, imports/PDF preview, profile, identity review, results/HF comparison, preparation, verification/backtests, field view, note synchronization, and downloads. The inspected module ends at line 3257.
- `ResultWorkspace` reads `event.currentTarget.value` inside both functional state updaters. React clears `currentTarget` after dispatch; deferred evaluation explains the supplied null-value exception. Filtering the selection array also collapses the second slot into the first when a slot is cleared.
- The selectors never request result details. `loadDetail` is invoked only by "Apri Verify", so fixing the crash alone does not make comparison work. Detail rejection has no local notification. Chart series are keyed by display name, which cannot distinguish namesakes.
- In `FieldPage`, `item.displayOrder?.length` is used as a render operand. An empty array produces numeric zero, rendered in the list's accent color. The zero is not a score or sequence; the surrounding sequence already displays "in attesa" for queued events.
- Turn notes expose a manual stage-order field and require the descriptive textarea. Native required-field validation can prevent submission before the handler runs. The server additionally rejects a turn without a consistent squad size, and any nonempty order that does not contain every frozen stage exactly once. The exact failed submission reported by the user has not been reproduced.
- `MatchPage.saveAndFreeze` constructs form data without the submitting button, then checks an `intent` field present only on the buttons. Normal submission therefore omits the intent, allowing "Salva bozza" to take the freeze path. This directly affects the requested edit-and-save workflow.
- [Controller](../../FreezerAnalysis.Api/Controllers/FreezerAnalysisController.cs): authenticated `/api/v1` routes expose draft save, freeze, field events, ZIP creation and retrieval; there is no reopen or individual-image endpoint.
- [Coordinator](../../FreezerAnalysis.Api/Services/FreezerAnalysisCoordinator.cs): `SaveDraftAsync`, `FreezeAsync`, `AppendFieldEventAsync`, `GetCardPayloadAsync`, and `CreateOrGetArtifactAsync` own the relevant use cases.
- Both [Mongo](../../MorWalPizVideo.Domain/FreezerAnalysis/FreezerAnalysisMongoRepository.cs) and [in-memory](../../MorWalPizVideo.Domain/FreezerAnalysis/InMemoryFreezerAnalysisRepository.cs) repositories forbid draft writes when frozen. Field-event append currently requires the draft's active frozen pointer to equal the event snapshot.
- `GetCardPayloadAsync` reads division from the current draft/profile and rendering context from the last event regardless of kind. Reopening would therefore leak new draft choices into old cards; a later plain note can erase the turn context.
- [Renderer](../../FreezerAnalysis.Api/Services/FreezerDocumentRenderer.cs) already produces two PDFs and two 1290 x 2796 PNGs. The ZIP contains fixed `phone-front.png` and `phone-back.png` entries. Its ordering compares stage-number strings, while field events carry stage IDs; front/back splitting occurs before ordering.
- Frozen input schema 2 already contains draft division. `ReadFrozenReplayPackageAsync` and `FrozenReplayDraft` can read it. The [snapshot entity](../../MorWalPizVideo.Models/FreezerAnalysis/FreezerAnalysisEntities.cs) retains immutable forecasts and strategy separately.
- [Engine](../../MorWalPizVideo.Domain/FreezerAnalysis/FreezerAnalysisEngine.cs), `EstimateUserLevel` and `ForecastStage`, establish parameter semantics; explanatory text must follow these formulas, not generic assumptions.
- [Frontend manifest](../../frontend/FreezerAnalysis.spa/package.json) confirms React 19, React Router 7, Recharts, TypeScript build and Vitest. Bootstrap CSS is not a dependency.
- [API manifest](../../FreezerAnalysis.Api/FreezerAnalysis.Api.csproj) and [test manifest](../../FreezerAnalysis.Api.Tests/FreezerAnalysis.Api.Tests.csproj) target .NET 10. Feature tests use xUnit/WebApplicationFactory and frontend tests use memory routers, Testing Library, and fake IndexedDB.
- Source searches found feature contract consumers in this API and its tests; the repository abstraction is used by this host and its two adapters. Frontend feature DTO/transport consumers are confined to FreezerAnalysis. Shared account transport remains an unchanged dependency.
- [Owning product context](../../frontend/FreezerAnalysis.spa/PRODUCT.md) and [design direction](../../frontend/FreezerAnalysis.spa/DESIGN.md) specify immutable baselines, append-only notes, Italian-first copy and accessible field use. Actual CSS, not the design document's aspirational navigation descriptions, is the visual baseline.

## Proposed Design

### Feature ownership and extraction

Move the route tree into `src/routes/config.tsx`. Keep bootstrap focused on styles, service-worker registration and router mounting.

Use route-local folders:

| Folder | Responsibility |
|---|---|
| `routes/layout` | Root shell, root loader/data type, route error and login prompt |
| `routes/account` | Login/registration component and safe return handling |
| `routes/matches` | Match list, import forms, PDF preview subcomponent and list loader |
| `routes/profile` | Profile component and loader |
| `routes/identity-review` | Identity reconciliation component and loader |
| `routes/match-detail` | Preparation component/loader, result workspace, HF comparison and comparison derivation |
| `routes/verification` | Verification component/loader and separate parameter-backtest panel |
| `routes/field` | Field component/loader, notes form/list, replay report, document actions and field synchronization hook |

Use `Component.tsx` and `loader.ts` like the public client. Keep current component-owned mutation handlers initially; do not introduce actions merely to populate a naming convention. Extract only cohesive subcomponents, not one file per small JSX fragment.

Use app-local shared components for error feedback, division select and field help. Place reused session/root-data access and formatting helpers in clearly owned utility/hook modules. A hook must be named as a hook. Preserve the current API facade and IndexedDB public interfaces.

Separate behavior-preserving moves from fixes: update imports and test ownership during extraction, then apply the scoped changes below. Remove the old implementation module after consumers migrate; no permanent compatibility barrel unless an actual external consumer is found.

### HF comparison and responsive behavior

- Store two independent nullable selection slots. Capture the primitive selected ID synchronously, before updating state; never retain a DOM event in an updater.
- Selecting either athlete loads missing details automatically through the existing typed API. Preserve "Apri Verify". Notify failures locally, permit retry and prevent stale responses from replacing current selections or a different match's cache.
- Require two distinct athletes. Allow selecting slot two first, independent clearing and rapid reselection. Explain incomplete and duplicate selections.
- Use stable series identifiers independent of display names. Keep stage-number order, finite scored HF only, explicit missing/DQ/zeroed status and the adjacent accessible table.
- Heading and introductory text occupy a full-width heading area; the two labelled selectors share one row above the mobile breakpoint. Below the existing 760px mobile boundary each selector occupies the entire row, equivalent to width 12/12, with a 14px vertical gap. Use scoped grid rules, not Bootstrap classes or a global form-column change.
- Keep chart overflow bounded to its container; no whole-page horizontal overflow.

### Help and action spacing

Add an app-local question-mark-in-circle help control beside domain labels. It must work on hover, keyboard focus and touch activation, dismiss with Escape, expose an accessible name and connect the control to its explanation. Do not nest an interactive button inside the input's clickable label; retain correct label association.

Cover preparation parameters, corresponding backtest fields, estimated center/interval, HF/result status, and turn context. Keep wording consistent:

- Beta adjusts sensitivity to estimated stage difficulty and also its uncertainty contribution; increasing it does not universally increase HF.
- Drift contributes uncertainty to informant weighting; it is not an automatic percentage added to the forecast.
- Own variability widens forecast uncertainty; it does not change its central value by itself.
- Distance penalty reduces the influence of informants with larger log-distance, rather than directly subtracting HF.
- Performance is the expected share of maximum points used for equivalent error-cost seconds; it is neither confidence nor ranking.
- Afternoon and one-hand options apply the configured factors to the forecast; explain the actual coordinator-wide application rather than implying automatic stage detection.
- Exclusions and strategy text have different effects: calculations versus documentation.
- Turn, squad size and cyclic stage order affect field presentation, never the immutable forecast.

Keep existing fractional input units and ranges in this change; show explicit examples such as 0.04 = 4% where useful. Do not silently convert inputs to a different scale.

Group verification, replay, synchronization and document actions in the existing wrapping `action-row`, with 10px spacing on both axes. Include reopen in a distinct navigation/edit area rather than implying it is another verification.

### Turn notes and automatic order

- Remove manual order entry. Sort actual frozen stages by stage number and rotate from the selected start stage; for 10 stages starting at 5 the result is 5,6,7,8,9,10,1,2,3,4.
- Require a valid start stage for a new turn note; descriptive text is optional. Start turn is optional, but when provided requires squad size and must be within it. A supplied squad size must be within existing server bounds. Other note kinds continue to require meaningful text.
- Show an immediate read-only order preview, clear field-level validation, a pending save state and explicit saved/queued/error feedback.
- Send the derived stage-ID order with the queued payload so offline and online writes share one representation. Server derives the canonical rotation for new turn events with a start stage, validating against the frozen revision. Continue accepting previously queued full legacy orders; do not remove the existing DTO property.
- Resolve rendering context from the last applicable turn event for that snapshot, not the last arbitrary note. Preserve the current treatment of corrections unless explicitly changing it.
- Convert stage IDs into ordered card stages at the coordinator/renderer boundary. Order the complete stage list before splitting front/back; apply the same sequence to field view, PNGs and PDFs.
- Render optional metadata only through boolean conditions. Localize kind labels; distinguish server sequence from "In attesa di sincronizzazione". Never render an empty array length as content.
- Insert locally queued notes into the visible list immediately, using the same operation ID, and replace them with the synchronized record rather than duplicating them.

### Individual image download

Retain the existing full package action. Add "Scarica immagini" preparation followed by independent "Scarica fronte PNG" and "Scarica retro PNG" actions. Two explicit downloads avoid browser restrictions on automatic multiple downloads and let a competitor fetch only the needed image.

Add an authenticated image-preparation endpoint under the snapshot artifact surface, returning an artifact ID and typed front/back descriptors. Reuse `CreateOrGetArtifactAsync` and its existing content-addressed private archive. Add authenticated retrieval under that artifact for a closed front/back side value; validate ownership, verify stored integrity, read only the fixed PNG entry, and return `image/png`, a descriptive attachment filename and an image-specific hash/ETag.

The ZIP remains a server implementation detail for reuse; the browser receives only each selected PNG, never a ZIP or client-side ZIP parsing dependency. This optimizes field interaction, not a promised image-only rendering latency: first preparation still uses the existing renderer. Missing/corrupt entries must produce explicit failure, not regenerated success-shaped output.

Reuse account transport for metadata and binary retrieval, including credentials and CSRF for preparation. Use separate loading/error states and dispose object URLs after the download has been initiated safely. Do not claim offline image download availability; queued notes must be synchronized before preparing images, or preparation must explicitly warn that local notes are not included.

### Reopen without rewriting history

Add an authenticated, CSRF-protected reopen mutation for the match draft. Request identifies expected draft revision and the expected currently frozen snapshot; response is the existing draft response.

The coordinator checks ownership and match association. A repository operation atomically matches account, draft, revision, active frozen snapshot and a null start marker; it clears only the active frozen pointer, advances revision and updates the timestamp. Keep draft ID, settings and monotonic event counter. Clear the mutable draft rendering-context cache, never snapshot or event data. Return an explicit conflict on stale state; duplicate requests reload the draft rather than resetting it twice.

Keep the existing started-draft restriction. Normal current freeze does not set the start marker; do not invent a new start-time lifecycle or silently erase a legacy marker.

Before enabling this transition:

- Decouple field-event append from the active snapshot pointer. After coordinator validation of the owned snapshot/draft relationship, allocate the monotonic counter on that owned draft even for historical snapshots. Store context in the event; stop writing snapshot-specific context into the shared working draft. Existing operation-ID deduplication stays intact. Never reset the counter on reopen.
- Remove live draft division from historical card composition: use frozen package division. For new snapshots capture otherwise-live card metadata, including power factor, in the input package with a tolerant additive reader. For old packages missing that metadata, retain an explicitly documented legacy profile fallback; do not fabricate historical values or rewrite stored packages. Old division must still come from the existing frozen package.
- Use only snapshot-local events for new card context. A legacy draft-context fallback is permissible only while that exact snapshot is still the active pointer; never apply another snapshot's context.
- Guard freeze against stale expected revision before materializing inputs, and keep the repository compare-and-swap gate. Reopen increments the revision, preventing reuse of the old unique snapshot key.

The UI offers "Torna alla preparazione" without mutation and a clear "Riapri piano per modificarlo" action with confirmation that the previous snapshot remains available. Fetch current draft state before reopening; navigate to the explicit match URL after confirmed success, not browser-history back. If already editable, navigate without another mutation. If a newer snapshot is active, show conflict and never silently reopen it.

Reopen is online-only. Existing cached snapshots and queued notes remain retained and synchronizable. Refreezing creates a new snapshot; old replay and verification continue to use their original evidence. Fix submitter-intent handling so saving the reopened draft cannot accidentally freeze it.

## Impacted Projects

- `frontend/FreezerAnalysis.spa`: feature modules, loaders, comparison, help, field actions/notes and focused tests.
- `FreezerAnalysis.Api`: reopen and image endpoints, canonical turn presentation, historical card composition.
- `MorWalPizVideo.Domain`: repository reopen capability and historical-snapshot event allocation in both adapters.
- `MorWalPiz.Contracts`: additive reopen request and image descriptor responses.
- `FreezerAnalysis.Api.Tests`: API, ownership, concurrency, card-order and compatibility coverage.
- `MorWalPizVideo.Models`: verify-only; existing document fields support the transition. Frozen input JSON gains tolerant optional metadata, not a Mongo entity schema.
- Shared frontend services, MVC helpers and ServiceDefaults: verify-only reuse of transport/auth/infrastructure.
- Public client: verify-only organization reference; no modifications.
- AppHost, Freezer deployment workflows and containers: verify-only. Deploy API before SPA; no topology/configuration changes.
- All other APIs, SPAs and standalone/Windows tools: not affected; no shared behavior change.

## Impacted Files

### Modify

- `frontend/FreezerAnalysis.spa/src/main.tsx`: import route configuration.
- `frontend/FreezerAnalysis.spa/src/routes.tsx`: extract and retire the monolith.
- `frontend/FreezerAnalysis.spa/src/routes.test.tsx`: migrate feature tests without losing existing coverage.
- `frontend/FreezerAnalysis.spa/src/freezerApi.ts` and `freezerApi.test.ts`: typed reopen and image operations.
- `frontend/FreezerAnalysis.spa/src/styles.css`: scoped comparison layout, help affordance and field action spacing.
- `FreezerAnalysis.Api/Controllers/FreezerAnalysisController.cs`: authenticated additive endpoints and precise errors.
- `FreezerAnalysis.Api/Services/FreezerAnalysisCoordinator.cs`: reopen, canonical order/context, frozen card metadata and image extraction reuse.
- `FreezerAnalysis.Api/Services/FreezerDocumentRenderer.cs`: apply canonical full-list order before splitting; advance renderer version when output changes.
- `MorWalPizVideo.Domain/FreezerAnalysis/IFreezerAnalysisRepository.cs`: atomic reopen contract.
- `MorWalPizVideo.Domain/FreezerAnalysis/FreezerAnalysisMongoRepository.cs` and `InMemoryFreezerAnalysisRepository.cs`: equivalent reopen and event allocation semantics.
- `MorWalPiz.Contracts/Contracts/FreezerAnalysis/FreezerAnalysisContracts.cs`: additive requests/descriptors and intentional note validation.
- `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs` and `FreezerDocumentRendererTests.cs`: transition, image bytes, ordering and negative cases.

### Add

- Feature-local `Component.tsx`, `loader.ts`, cohesive subcomponents, types and colocated tests under the route folders listed above.
- `frontend/FreezerAnalysis.spa/src/routes/config.tsx`.
- App-local error/division/help components and genuinely reused utility/hook modules. Keep names ownership-based.
- Focused pure order/comparison tests and route-configuration tests. A field synchronization hook gets its own tests through the field feature, not a new state-management framework.

### Generated/Migration

No generated source, dependencies, lockfile changes, Mongo backfill or SQLite migration. New optional frozen input metadata is written only for future snapshots.

### Documentation

- This task-local handoff and the parent `docs/feature-handoffs/freezer-analysis-handoff.md`: link scope and record actual implementation/validation afterward.
- `docs/architecture/frontend.md`: actual route ownership after extraction.
- Owning FreezerAnalysis lifecycle documentation: explain reopening, historical note acceptance, image retrieval and legacy metadata fallback. Record a focused ADR for the mutable-draft/immutable-history transition when approved; do not label this proposal as implemented.
- Product/design documents: update only if shipped behavior requires a durable correction; preserve the incumbent direction.

### Verify Only

- `frontend/FreezerAnalysis.spa/src/offlineStore.ts` and `offlineStore.test.ts`: unchanged database partition/schema and queued payload compatibility.
- `frontend/FreezerAnalysis.spa/package.json`, `FreezerAnalysis.Api/FreezerAnalysis.Api.csproj`, and `FreezerAnalysis.Api.Tests/FreezerAnalysis.Api.Tests.csproj`: use existing tooling.
- `MorWalPizVideo.Models/FreezerAnalysis/FreezerAnalysisEntities.cs`: preserve existing aggregate/index fields.
- `frontend/morwalpizvideo.client/src/routes/config.tsx` and `main.tsx`: organization reference.
- `MorWalPizVideo.AppHost/Program.cs`, `.github/workflows/ci.yml`, `.github/workflows/deploy-freezer-analysis-api.yml`, `.github/workflows/deploy-freezer-analysis-spa.yml`, and API/SPA Dockerfiles: existing deployment/Chromium gates.

## Impact Matrix

| Area | Decision |
|---|---|
| Backend/API | Add reopen and image preparation/retrieval; preserve existing URLs and ZIP endpoints. |
| Frontend/UI | Feature ownership, responsive comparison, accessible help, shorter field workflow. |
| Database/Data | Atomic revision/pointer transition; historical snapshots untouched; counter never reset. |
| Contracts/Models | Additive request/response DTOs and optional input metadata; retain legacy order fields. |
| Authentication/Authorization | Existing host cookie and CSRF; ownership checked for draft, snapshot and artifact. |
| Validation | Fixed selection slots, distinct athletes, meaningful note data, valid frozen stage rotation and CAS state. |
| Cache | No public output-cache tags affected. Existing artifact content key and renderer version distinguish changed output; PNG ETag hashes PNG bytes. |
| Configuration/Secrets | No new settings or secrets; existing separate-origin transport remains authoritative. |
| Logging/Telemetry | Use existing structured failure reporting; record operation outcome/correlation, never note text or athlete personal data. UI errors remain visible. |
| Background Jobs | Not applicable; rendering and queue reconciliation stay in existing request/client flows. |
| Deployment/Operations | API-first rollout, existing native renderer environment and separate SPA deployment. |
| Backward Compatibility | Preserve snapshots, old routes, ZIPs, event IDs and queues; new endpoints tolerate old metadata. |

## Reuse Opportunities

Reuse the public client's feature module pattern, existing route loaders, `freezerApi` account client, binary `accountApi.call`, offline store/operation IDs, `action-row`, native controls, Recharts/table pairing, renderer/private artifact store and frozen input reader. Reuse memory-router and fake IndexedDB fixtures and API test factory. No additional application consumes this UI, so do not move field components or DTOs into shared frontend packages.

## Risks

| Rank | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Clearing the frozen pointer blocks queued historical notes | Decouple allocation from active pointer; preserve owner/draft binding and operation IDs | Old queued note synchronizes before and after refreeze without duplication |
| High | Live draft/profile/context leaks into historical cards | Frozen division, additive metadata, snapshot-local turn context and bounded legacy fallback | Editing new draft changes neither old forecast nor frozen division/context |
| High | Concurrent reopen/save/freeze races or index collision | Atomic expected revision and snapshot match; increment revision; never reset counter | One reopen wins; stale requests conflict; second freeze has a new ID/key |
| Medium | Missing required text or inconsistent turn prevents note submission | Conditional requirements and inline server-matched validation | Structured-only turn saves; invalid turn is explained without losing values |
| Medium | Comparison stale data, namesakes or unavailable detail | Fixed slots, stable keys, scoped cache and visible retry | Rapid selections and duplicate names produce correct independent series |
| Medium | Stage-ID/number mismatch and premature split break cyclic output | Canonical ordered full stage list | Starting at 5 yields exact 10-stage sequence across field, PNG and PDF |
| Medium | Multiple downloads blocked or render/native dependencies unavailable | Separate explicit PNG actions; existing renderer gates; visible errors | Real mobile download yields two valid independent files, never ZIP |
| Medium | New feature/API deployed out of order or cross-origin downloads fail | API first, cookie/CSRF transport, old endpoints retained | Deployed browser smoke proves authenticated PNG and reopen flows |
| Low | Feature moves alter auth/offline/navigation behavior | Extraction-only gate before functional changes | Existing lifecycle and route tests pass unchanged in meaning |

## Technical Debt

- Monolithic route ownership and untested comparison: address in scope.
- Submitter intent, image ordering and last-event context: address because they directly break requested workflows.
- Existing field-event counter allocation can leave gaps after an interrupted insert; reopening must not worsen it. Defer broader interruption recovery from the parent debt plan; do not promise contiguous numbering.
- Legacy input packages lack some historically accurate presentation metadata. Isolate and document the fallback, never claim it is frozen evidence.
- The broader coordinator monolith, index activation, renderer deployment parity and production acceptance remain parent-handoff debt. No global rewrite or deployment repair in this task; native-runtime proof remains necessary for downloads.

## Implementation Roadmap

1. **Establish the preservation baseline.** Own SPA route/API tests and source inventory. Run current focused tests and TypeScript build; capture current match/field desktop and phone views. Record pre-existing failures separately.
2. **Extract route ownership only.** Move configuration/loaders and cohesive feature components; preserve URLs, mutation locations, account/offline interfaces, chart behavior and error surfaces. Gate: existing SPA tests and build pass with migrated imports.
3. **Fix comparison and draft submission.** Own match-detail component/helper/tests. Capture select values, retain two slots, fetch details, surface errors, use stable series keys, and honor the clicked submitter. Gate: selections work without opening Verify first; save does not freeze.
4. **Implement turn/order presentation.** Own field modules, coordinator/renderer and note tests. Conditional validation, derived preview/order, immediate queued notes, boolean metadata rendering and last-turn context. Gate: exact rotation, no red zero, structured-only save, consistent PNG/PDF ordering.
5. **Add transition contracts and repository behavior.** Own Contracts, Domain adapters and API coordinator/controller. Add atomic reopen and historical event support; establish frozen card metadata before exposing reopen. Gate: conflicts/ownership, old queued notes, new snapshot identity and unchanged historical evidence in both repository modes.
6. **Wire reopen UI.** Own field/match loaders and components. Explicit match navigation, confirmation, online guard and stale-snapshot conflict. Gate: reopen, edit, save, refreeze; old snapshot remains accessible/replayable.
7. **Add individual PNG API and UI.** Own artifact controller/coordinator, typed client and document actions. Reuse existing archive server-side; expose descriptors and per-side PNG retrieval. Gate: MIME/signature/dimensions/filename/hash and owner isolation; no ZIP delivered for PNG actions.
8. **Apply the narrow interface refinement.** Own CSS/help components and feature labels. Use Impeccable `shape` for this established `Operate` surface, then `adapt`, `clarify` and `harden` as bounded implementation checks. This architect handoff does not execute live design tooling. Preserve PRODUCT/DESIGN context; no replacement visual world.
9. **Complete proof and documentation.** Run targeted regression selectors first, then the owning SPA suite/build and relevant API/renderer tests with one serial .NET build and no-build subsequent tests. Capture one batched desktop/mobile/accessibility pass, apply material fixes together and confirm once. Update this handoff with actual files, evidence and residual risks.

Independent post-validation review is warranted for the reopen/event lifecycle, account isolation and old-snapshot metadata handling. The user may select MorWalPiz Code Reviewer; architecture planning does not execute that review.

## Testing Strategy

- **Pure logic:** ascending stage alignment, exact rotation for 10/start 5, first/last start, one stage, non-contiguous stage numbers, invalid start, missing results and duplicate athlete names.
- **Component/route:** select second first; clear either slot; two distinct selections; automatically fetch details; retry failures; rapid selection/match change; save versus freeze intent; all routes/deep links and root auth/offline behavior after extraction.
- **Note forms:** valid structured-only turn; whitespace-only descriptive note rejection; start turn without squad; turn above squad size; malformed/out-of-range values; busy submit protection; success/queued/error feedback with form contents preserved on failure.
- **Offline:** immediate queued-note display, no numeric-zero node for empty order, operation-ID replacement, reconnect without duplicate note, account partition isolation, old snapshot queue after reopen/refreeze, no reopen offline.
- **API/security:** unauthenticated and missing/invalid CSRF mutations denied; another account cannot reopen/read images/append; missing snapshot/artifact, invalid side, revision/snapshot conflict, repeated request and started marker restriction.
- **Persistence:** Mongo and in-memory equivalence, competing reopen/freeze/save, continued monotonic event allocation and existing unique keys. In-memory HTTP tests alone are insufficient proof of atomic Mongo filters; use an isolated authorized Mongo fixture where available.
- **Artifacts/contract:** individual bytes are PNG, `image/png`, attachment filename, own hash and 1290 x 2796; compare each with the fixed entry from the same artifact; preserve ZIP contents. Test corruption/missing entry, changed turn followed by plain note, ordering before split, old schema metadata and new additive metadata.
- **End-to-end:** authenticated prepare -> save -> freeze -> structured turn -> individual image -> reopen -> modify -> save -> refreeze -> inspect original snapshot/replay/verification. Exercise a prior offline queue during this sequence.
- **Visual/accessibility:** desktop 1280px, mobile 390px and 320px, boundary 760/761px, 200% zoom. Two selectors in one row only above mobile; full-width stacked selectors with 14px gap below. Actions have 10px gaps and wrap without collision. Help works with mouse, keyboard and touch, visible focus/Escape and screen-reader association; controls meet 44px targets. Verify real mobile PNG download behavior, not only mocked anchor clicks.

Planning validation performed: source/manifests/tests/workflows inspected and requirements mapped. No builds, tests, browsers or production mutations were executed by the architect. Runtime gates above remain implementation work.

## Migration And Rollback

- **Mongo:** no collection/schema backfill. Reopen uses existing fields and indexes. Preserve monotonically increasing revisions/counters; never roll them backward. Deployment must retain acceptance of historical notes after reopening.
- **Frozen inputs:** optional new metadata is additive and applies only to future snapshots. Tolerant readers accept existing packages; old packages and hashes are never rewritten. Existing division is recoverable from schema 2; unknown/unavailable package data produces an explicit diagnostic.
- **API:** deploy backward-compatible API before SPA. Existing consumers keep draft/freeze/ZIP routes; legacy order payloads remain accepted. No API version or authentication change.
- **Cache/artifacts:** advance renderer version for ordering changes so new output has a distinct content key. Existing downloadable ZIPs remain stored and accessible. Image extraction does not introduce a public cache.
- **Offline:** no IndexedDB migration. Retain queued operation IDs and old snapshot IDs; never purge a queue to unblock reopen.
- **Operations:** production rollout remains separately authorized. Confirm existing Chromium/native-runtime availability and cookie/CSRF downloads in the deployed topology.
- **Rollback:** hide/revert new SPA affordances first. Retain the backend historical-note compatibility after any reopening has occurred; restoring the old pointer-bound append implementation would strand queues. Do not automatically rebind drafts, delete new snapshots, reset revisions or reinterpret a changed draft as its original frozen evidence. Recovery must preserve both history and editable state.

## Open Questions

None blocking the proposed architecture under the stated assumptions. The user's failed turn submission remains a reported symptom rather than a reproduced request; implementation must reproduce it and distinguish native validation from server rejection before claiming the fix.
