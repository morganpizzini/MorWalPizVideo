# Shooter Workshop Production Route Handoff

### Outcome

Refactor the Shooter Workshop Production route so recipe data is loaded by a React
Router loader and production forms are submitted through a fetcher-backed route
action. Fix the production failure in which ServerAPI returns "Recipe was not found."
for a recipe identifier returned by the recipes endpoint.

Assumptions:

- Scope is the existing `/production` route and the shared Workshop repository defect
  that prevents persisted ObjectId-backed entities from being found by string IDs.
- The current Production layout, fields, copy, localization behavior, endpoint shapes,
  authentication, CSRF handling, and idempotency semantics are preserved.
- This is an Operate-mode refinement, not a visual redesign.

### Current Evidence

- `frontend/shooter-workshop.client/src/routes/Production/component.tsx` calls
  `getWorkshopRecipes` through `useResource` during component lifecycle and calls
  `recordWorkshopProduction` directly from `WorkshopForm`'s submit callback.
- `frontend/shooter-workshop.client/src/routes/Production/index.ts` exports only the
  component, and `frontend/shooter-workshop.client/src/routes/config.tsx` registers
  `/production` without a loader or action.
- `frontend/shooter-workshop.client/src/routes/Components/loader.ts`,
  `frontend/shooter-workshop.client/src/routes/Components/action.ts`, and
  `frontend/shooter-workshop.client/src/routes/Components/component.tsx` establish the
  local precedent for route loaders, actions, `useLoaderData`, `useFetcher`, and
  `fetcher.Form`.
- `frontend/fe-packages/services/src/workshopService.ts` already owns the canonical
  recipes read and production mutation. The production request shape in
  `frontend/fe-packages/models/src/workshop.ts` matches
  `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` passes the
  authenticated account ID and request recipe ID to
  `WorkshopService.RecordProductionAsync`; the controller does not transform the ID.
- `MorWalPizVideo.Models/Models/BaseEntity.cs` maps string `Id` values to MongoDB
  ObjectId `_id` values with `BsonRepresentation(BsonType.ObjectId)`.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` uses raw string field
  definitions for `_id` in `GetAsync`, `ReplaceAsync`, and `DeleteAsync`. This bypasses
  the member serializer used for `BaseEntity.Id`, so a valid 24-character string can
  be compared as a BSON string against an ObjectId and fail to match.
- `WorkshopMockRepository.GetAsync` compares deserialized string IDs directly, which
  explains why mock-backed service tests do not reproduce the persisted MongoDB
  failure.
- `MorWalPizVideo.Domain/Interfaces/BaseRepository.cs` and existing repository code
  demonstrate explicit ObjectId-aware filtering as prior art.

### Proposed Design

1. Add a Production loader that returns the recipe collection from
   `getWorkshopRecipes`.
2. Add a Production action that reads the route form data, validates and converts the
   submitted values into `CreateWorkshopProductionRequest`, creates one idempotency
   key for that submission, calls `recordWorkshopProduction`, and returns structured
   success or validation data suitable for fetcher rendering.
3. Register the loader and action on the `/production` route and export them from the
   Production route barrel.
4. Make the component consume loader data and render a named, fetcher-backed form.
   Recipe ID, production date, quantity, and notes travel as form fields. Fetcher
   state owns pending UI; fetcher data owns accessible errors and success handling.
5. Correct Workshop Mongo repository identity filters to use the typed `Id` member
   serializer, or an equivalent explicit ObjectId conversion that supports the
   repository's valid ID contract. Apply the correction consistently to get, replace,
   and delete so the immediate production bug is not reintroduced through recipe
   edit/archive flows.
6. Preserve account scoping in every repository identity filter. Invalid IDs must
   remain non-matching or produce the repository-standard validation response; they
   must not broaden a query or drop the account predicate.

Material alternative rejected: changing only `RecordProductionAsync` to parse the
recipe ID would fix one call site while leaving recipe update/archive and every other
Workshop repository identity lookup inconsistent. The repository is the narrowest
responsible shared boundary.

### Impacted Projects

- `frontend/shooter-workshop.client`: Production route data flow, route registration,
  form state, route tests, and user feedback.
- `frontend/fe-packages/services`: verify-only; existing service functions and account
  transport remain authoritative.
- `frontend/fe-packages/models`: verify-only; existing request and recipe models remain
  unchanged.
- `MorWalPizVideo.Domain`: serializer-aware Workshop repository identity filtering.
- `MorWalPizVideo.Models`: verify-only; existing BaseEntity ObjectId representation
  defines the persistence constraint.
- `MorWalPizVideo.ServerAPI`: verify-only; the existing endpoint and controller mapping
  remain unchanged.
- `MorWalPiz.Contracts`: verify-only; no request-contract change is required.
- `MorWalPizVideo.BackOffice.Tests`: repository/service regression ownership for shared
  ServerAPI and Domain behavior.
- Deployment surfaces: no topology or configuration change.

### Impacted Files

#### Modify

- `frontend/shooter-workshop.client/src/routes/Production/component.tsx` — consume
  loader data and submit with a fetcher-backed form.
- `frontend/shooter-workshop.client/src/routes/Production/index.ts` — export the route
  component, loader, and action.
- `frontend/shooter-workshop.client/src/routes/config.tsx` — register the Production
  loader and action.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — assert that the
  Production route exposes both data functions.
- `MorWalPizVideo.Domain/Workshop/WorkshopRepository.cs` — make account-scoped ID
  filters honor BaseEntity's ObjectId serializer for get, replace, and delete.

#### Add

- `frontend/shooter-workshop.client/src/routes/Production/loader.ts` — load recipes.
- `frontend/shooter-workshop.client/src/routes/Production/action.ts` — parse and submit
  production form data.
- A focused Production route test beside the route, following current Workshop Vitest
  conventions, covering loader, action, pending state, payload, success, and errors.
- A focused Workshop repository filter regression test in
  `MorWalPizVideo.BackOffice.Tests/Features/`, using the Mongo serializer or an
  explicitly configured Mongo integration fixture to prove that a string model ID is
  rendered/queried as an ObjectId while account scoping is retained.

#### Generated/Migration

None. Persisted documents already use ObjectId IDs; the defect is query serialization,
not stored data.

#### Documentation

- `docs/architecture/frontend.md` — authoritative Workshop route data/mutation rule.
- `docs/architecture/shooter-workshop.md` — feature-specific route ownership and
  Production flow.
- `docs/feature-handoffs/shooter-workshop-production-route-handoff.md` — implementation
  source of truth and later completion record.

#### Verify Only

- `frontend/fe-packages/services/src/workshopService.ts`
- `frontend/fe-packages/services/src/workshopService.test.ts`
- `frontend/fe-packages/models/src/workshop.ts`
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`
- `MorWalPizVideo.Models/Models/BaseEntity.cs`
- `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`
- `frontend/shooter-workshop.client/src/components/WorkshopForm.tsx`

### Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | No endpoint change. Production continues at the existing authenticated Workshop route. |
| Frontend/UI | Same screen and fields; router owns loading, pending, validation, success, and revalidation. |
| Database/Data | No data rewrite. Mongo ID filters must match existing ObjectId storage. |
| Contracts/Models | No wire-shape change. Existing shared TypeScript and C# contracts are reused. |
| Authentication/Authorization | Unchanged account cookie, CSRF, and authenticated account scope. Repository filters retain `accountId`. |
| Validation | HTML constraints remain helpful; action conversion and ServerAPI validation remain authoritative. Invalid quantity/date/recipe values produce accessible form feedback. |
| Cache | Not applicable; Workshop reads are not using output-cache tags in this flow. Router revalidation refreshes route data only. |
| Configuration/Secrets | None. |
| Logging/Telemetry | No new event required. Existing HTTP telemetry captures failures; do not log form notes or identifiers unnecessarily. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Frontend and ServerAPI/Domain artifacts deploy together or backend first. No infrastructure change. |
| Backward Compatibility | Existing API clients and stored recipes remain compatible. Repository correction broadens successful matching only to the intended serialized ID representation. |

### Reuse Opportunities

- Reuse `getWorkshopRecipes` and `recordWorkshopProduction`; do not add direct fetch.
- Reuse the Components route's loader/action/fetcher organization and route
  registration pattern.
- Reuse `getApiFieldErrors` for structured API validation feedback where its current
  response parsing fits.
- Reuse React Router automatic action revalidation instead of calling a component
  reload function.
- Reuse BaseEntity's configured ID serializer rather than duplicating ObjectId parsing
  in WorkshopService.
- Keep `WorkshopForm` unchanged unless a fetcher-compatible API can be introduced
  without altering its many existing callback-based consumers. A Production-local
  form is preferable to broad unrelated migration.

### Risks

- **High — persistence mismatch remains partially fixed:** correcting only production
  leaves edit/archive/delete broken. Mitigation: update all three generic Workshop
  repository identity operations. Signal: rendered filter and persisted lookup tests
  pass for get, replace, and delete.
- **High — tenant scope regression:** simplifying the filter could omit `accountId`.
  Mitigation: compose typed ID and account predicates together. Signal: a same-ID,
  different-account negative test cannot read or mutate the record.
- **Medium — action errors escape to the route error boundary:** service exceptions
  must become structured expected validation data while unexpected failures remain
  explicit. Signal: API validation is displayed in the form and unexpected errors are
  not reported as success.
- **Medium — duplicate submissions:** regenerating idempotency keys on retries or
  allowing concurrent submits can create duplicate operations. Mitigation: create one
  key per accepted fetcher submission and disable controls while non-idle. Signal:
  repeated browser submission while pending creates one request; server idempotency
  tests remain green.
- **Low — visual regression:** replacing `WorkshopForm` locally can alter spacing or
  feedback. Mitigation: preserve existing classes, labels, field order, and accessible
  alert semantics. Signal: desktop/mobile visual check shows no unintended redesign.
- **Low — external service risk:** none; this flow uses only ServerAPI and MongoDB.

### Technical Debt

- Multiple Workshop routes still use `useResource` and callback-based
  `WorkshopForm`. This is real architectural drift but does not block this fix. Defer
  route-by-route migration to separately approved work; do not expand this change.
- Components action error rendering currently assumes structured fetcher data but its
  action does not normalize service errors. Do not include that unrelated correction
  here.
- Direct entity responses from the Workshop controller are a legacy contract choice.
  No DTO redesign is required for this bounded fix.

### Implementation Roadmap

1. Add a serializer-sensitive repository regression that fails with the current raw
   `_id` string filter and proves account isolation.
2. Correct Workshop repository get, replace, and delete filters at the shared Domain
   persistence boundary. Validate existing mock behavior remains unchanged.
3. Add the Production loader and action using existing shared services and request
   models. Define explicit form conversion, expected validation output, and
   per-submission idempotency behavior.
4. Register and export the data functions from the Production route and route manifest.
5. Refactor only the Production component to use loader data and a fetcher-backed form.
   Preserve layout, copy, localization, input constraints, and accessible errors.
6. Add focused route tests and extend the route-manifest test.
7. Validate the shared service transport test, Workshop frontend tests/build, focused
   backend regression, and the owning backend test project.
8. Update this handoff after implementation with actual files, validation evidence,
   rollout status, and residual risks. Stop without migrating other Workshop routes.

Independent post-validation review is warranted because the repository filter is
shared by multiple Workshop aggregates and is account-scoped. The user may select
MorWalPiz Code Reviewer after implementation.

### Testing Strategy

- **Unit/route loader:** loader returns the recipe collection and propagates an
  explicit service failure.
- **Unit/route action:** valid form data produces the exact shared request shape,
  numeric quantity, unchanged recipe ID/date/notes, and a non-empty idempotency key.
  Missing/invalid fields return expected validation feedback without invoking a
  success-shaped result.
- **Component:** loader recipes populate the select; fetcher submission includes named
  fields; pending state disables submission; API validation appears with `role=alert`;
  success clears or preserves fields according to the accepted current UX.
- **Route contract:** `/production` has both loader and action in `config.test.tsx`.
- **Shared service:** retain the existing test proving the canonical endpoint, server
  DTO names, account credentials, and CSRF header.
- **Persistence:** prove a string `BaseEntity.Id` is encoded as BSON ObjectId in
  account-scoped get/replace/delete filters. Include wrong-account and unknown-ID
  negatives.
- **Backend behavior:** record production from a persisted recipe, then verify the
  operation, production session, and ammunition batch are created with the recipe
  snapshot. This case must fail against the current repository implementation when
  run against Mongo semantics.
- **Security:** verify one account cannot produce from another account's recipe.
- **End to end/manual:** authenticated user opens Production, sees recipes, submits a
  valid recipe, receives success, and sees the resulting batch/history after normal
  navigation or revalidation.
- **Impeccable:** apply a narrow `harden` review to the existing Operate-mode surface,
  not a redesign. Minimum evidence is desktop and narrow-mobile rendering plus keyboard
  submission, visible focus, pending control state, and screen-reader-readable errors.
  No `PRODUCT.md` or `DESIGN.md` exists in this workspace; current implementation and
  feature architecture remain the incumbent visual truth.

### Migration And Rollback

- **MongoDB:** no migration or backfill. Existing recipe IDs remain ObjectIds. Deploy
  the backend correction before or with the frontend refactor.
- **API contracts:** no compatibility window is needed because routes and payloads do
  not change.
- **Authentication/cache/configuration:** no migration.
- **Rollout monitoring:** watch production endpoint validation failures, especially
  "Recipe was not found.", and successful production operation counts after release.
- **Rollback:** the frontend can roll back independently to callback submission, but
  retaining the backend identity-filter correction is preferred. If the repository
  correction must roll back, production and other persisted-ID operations will regress;
  no data recovery is required because the change does not rewrite documents.

### Open Questions

- Should a successful Production submission reset quantity/notes and retain the
  selected recipe/date, or reset the full form? Current code has no explicit success
  behavior. This affects UX and component assertions but not architecture.
- Should invalid non-ObjectId Workshop IDs be treated as a normal not-found result or
  as validation failure? The implementation must follow the repository's accepted ID
  contract consistently without weakening account isolation.

### Implementation Completed

Implemented on 2026-11-15:

- Added `frontend/shooter-workshop.client/src/routes/Production/loader.ts` to load
  recipes through `getWorkshopRecipes`.
- Added `frontend/shooter-workshop.client/src/routes/Production/action.ts` to parse
  the form, validate the recipe/date/quantity fields, create one idempotency key per
  submission, call `recordWorkshopProduction`, and return structured success/error
  feedback.
- Updated the Production route barrel and route manifest to register/export the
  loader and action.
- Reworked `Production/component.tsx` to consume `useLoaderData` and submit through
  `useFetcher`/`fetcher.Form`. Existing field order, labels, classes, constraints,
  localization calls, and page structure remain intact. Pending controls are
  disabled and action feedback is rendered with `role="alert"`.
- Updated the Workshop Mongo repository's `GetAsync`, `ReplaceAsync`, and
  `DeleteAsync` filters to use the typed `BaseEntity.Id` expression. MongoDB now
  applies the configured `BsonRepresentation(ObjectId)` serializer instead of
  comparing `_id` as a BSON string; account predicates remain present.
- Added route data-function tests for recipe loading, valid submission payload and
  idempotency, invalid input rejection, service failure feedback, and route
  registration.
- Added the small `form-success` style required for the new successful action
  feedback state.

### Validation Completed

- `yarn --cwd frontend/shooter-workshop.client test src/routes/Production/route.test.ts src/routes/config.test.tsx`
  — passed, 5 tests.
- `yarn --cwd frontend/shooter-workshop.client build` — passed TypeScript and Vite
  production build.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed.
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore --no-build
  --filter "FullyQualifiedName~WorkshopServiceTests"` — passed, 7 tests.

The backend build reported existing NuGet audit warnings for
`SixLabors.ImageSharp` 3.1.12, including high-severity advisories. No dependency
changes were made because they are outside this handoff's scope.

Deferred to downstream automatic validation: full frontend test suite, broad
workspace lint/format checks, full backend test suite, and real-browser authenticated
desktop/mobile verification. No Mongo integration test was run because the required
environment-managed Mongo test URI was not available in this validation context.

### Residual Risks

- The Mongo repository correction is covered by compilation and mock-backed Workshop
  service tests, but a live Mongo filter integration test remains deferred until the
  repository's managed Mongo test environment is available.
- Other Workshop routes still use the legacy `useResource`/callback pattern. They were
  intentionally not migrated as part of this focused Production fix.
- The frontend worktree contained unrelated pre-existing changes; those files were
  preserved.
