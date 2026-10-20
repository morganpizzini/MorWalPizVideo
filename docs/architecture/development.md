# Development Architecture

## Goal

Developers should run core workflows without MongoDB, Key Vault, Blob Storage, YouTube, social networks, AI providers, email, or push infrastructure.

## Development Flags

Only these feature flags are enabled by default in development:

- `EnableDev`
- `EnableSwagger`

Development environment and `EnableDev` must both be true before fake authentication or permissive CORS is active. Production startup rejects development-only providers.

## Scenario-Based Mocks

Current source uses `IMockScenario`, `IMockScenarioLifecycle`, named scenarios, and `BaseMockRepository<T>` to provide cloned, lock-protected in-memory collections initialized directly in C#.

When mock mode is enabled, scenario precedence is fixture lifecycle `Select(...)` override, then startup `MockScenario` configuration (or `FeatureManagement:MockScenario`), then `Primary`. Each host owns an isolated singleton lifecycle. `Reset()` restores the selected scenario baseline; `Reinitialize()` recreates the selected scenario instance, allowing tests to reuse a host safely.

Target scenario characteristics:

- Stable IDs and deterministic timestamps.
- Coherent relationships across content, channels, categories, products, carts, and users.
- Fresh isolated scenarios for tests.
- Explicit named scenarios: `Primary`, `Empty`, `Authorization`, `ExternalFailure`, and `LegacyCompatibility`.
- No real credentials or production-derived personal data.

Repository interfaces remain unchanged between Mongo and mock modes.

## External Fakes

Provide deterministic implementations for:

- Blob upload/list/download and metadata (the current fake does not issue SAS URLs).
- YouTube metadata and uploads.
- Translator and AI completion.
- Discord, Telegram, Facebook, and Pinterest.
- reCAPTCHA and Web Push.
- Future transactional email.
- Clock/time behavior where expiry is tested.

Fakes support configurable latency, transient failure, permanent failure, malformed response, and cancellation. Do not scatter environment checks through controllers; select providers in composition roots.

## Local Application Matrix

| Application | Preferred local dependencies |
|---|---|
| BackOffice + SPA | In-memory scenario repositories, fake integrations, fake auth |
| ServerAPI + public/shop clients | Same scenario data, fake Blob, anonymous public behavior |
| ShortLinks | Canonical link scenario and in-memory visit tracking |
| Shooting ITA | Shared service mock or local ServerAPI |
| VideoImporter | Temporary SQLite, fake BackOffice, fake YouTube |
| InsightScanner | Fake scan sources and fake BackOffice client |

## Configuration Hygiene

- Use user secrets for developer credentials that are genuinely required.
- Never commit local API keys, service-account files, connection strings, or production snapshots.
- Use documented placeholder values that cannot authenticate.
- Keep scenario data small enough for review.

## Test Strategy

### Unit

Pure domain transitions, validators, URI safety, acquisition rules, cache tag normalization, and mapping.

### Integration

WebApplicationFactory with test authentication and isolated mock scenarios. Exercise HTTP contracts, authorization, validation, cache coordination, and compatibility.

### Frontend

Vitest/Testing Library with route-aware helpers and mocked shared services. Test pending, success, empty, validation, and error states.

### BackOffice SPA Entity Template

New administrative entities should follow the route and component structure already used by `categories` and `galleries`. Register the feature in the BackOffice route manifest with an index route and a nested `:id` route:

- `/<entities>`: index page for the list and collection-level actions.
- `/<entities>/create`: create form.
- `/<entities>/:id`: detail page.
- `/<entities>/:id/edit`: edit form.

The `:id` route should use an outlet with its index child as the detail page and its `edit` child as the edit form. Keep the route modules colocated by responsibility (`index`, `detail`, and `form`), and export the component, loader, and action from each module's `index.ts` as appropriate. Use the existing lazy route registration in `frontend/back-office-spa/src/router/routes/lazy-index.ts` as the integration pattern.

#### List page

- Read the collection through the route loader and `useResolvedLoaderData`.
- Render the collection with the shared `GenericTable`, stable entity IDs, explicit `ColumnDef` columns, a useful search placeholder, and an empty-state message.
- Provide row actions for detail, edit, and delete. Destructive actions should use the existing fetcher plus confirmation modal pattern and show success or validation errors through the shared toast and error components.
- The page header is required: use `PageHeader` with the entity collection title and `createLink="./create"`.

#### Detail page

- Load one entity by route `id`; return a loading skeleton when the loader data is not available and render the entity details in `DetailPanel` or the nearest existing shared detail component.
- The page header is required: use `PageHeader` with the entity's display name, a link back to the collection, and an edit link. Include the existing delete callback/modal flow when the entity supports deletion.
- Keep detail-only operations, such as image management, on this page when they require an existing entity ID.

#### Create and edit form

- Reuse one form component for both modes where the fields and validation are shared. Detect the mode from the route parameter and populate edit values from the loader result.
- Submit through the established React Router `useFetcher`/`fetcher.Form` or `fetcher.submit` pattern and keep server validation authoritative. Render collection-level errors with `GenericErrorList` and field errors with `FieldError`.
- Disable controls while the mutation is pending, show a toast on success, and navigate to the detail page after create/update or back to the collection when that is the established entity flow. Preserve the existing confirmation-modal pattern for mutations that use it.
- The form page header is required: use `PageHeader` with a create title or an edit title that identifies the entity, plus the established back/cancel navigation.

#### Loader and data contract

- A collection loader returns the service/API collection result consumed by the list. An entity loader uses the route `id` and returns the service/API entity result, or `null` for create mode.
- Match the response shape already used by the owning service. Where an endpoint can return either a value or a `{ data: value }` envelope, normalize that shape at the route boundary before rendering rather than spreading response-shape checks through the component.
- The form loader must provide every reference collection needed to render controls, not just the entity (for example, the product form loads both the product and its categories).
- Keep mutation actions in the route module that owns the form or list operation. Use the shared service methods and preserve their request, response, validation, authentication, and cache behavior; do not introduce an ad hoc client in the component.

#### Images and file inputs

When an entity includes images, use a real file input with `accept="image/*"`; add `multiple` only when the entity supports multiple images. Keep selected files and upload state separate from the entity form state, disable the input and buttons while uploading, display server errors accessibly, and clear the input after a successful upload.

For create flows that accept images, include the files in the create form's `FormData` under the service's established field name. For edit/detail flows, prefer the existing entity-specific upload service after an ID exists, then refresh or update the loader-backed entity. Render previews from the returned public image data, enforce the server's image-count/validation limits in the UI, and provide the existing delete-image operation where supported. The `galleries/detail` and `channelNews/form` implementations are the repository examples for these two upload shapes.

### End To End

- BackOffice mutation to public cache refresh.
- Public catalog to anonymous cart to private download remains on hold with shop; no active test expansion.
- Short-link management to redirect and count.
- Desktop API-key submission to BackOffice remains an interactive/provider verification gate; existing Windows test suites now execute in CI.

Existing frontend Vitest and Windows VSTest runners execute in CI alongside backend HTTP tests. Real-browser desktop/mobile/SSR cookie/CSRF/CORS proof is still BLOCKED, not satisfied by Node or HTTP tests; see the [operational checklist](operations/phase5-activation-and-recovery.md).

`MorWalPizVideo.BackOffice.Tests` remains the owner of BackOffice, ServerAPI, shared, cross-API, and ShortLinks management/Reqnroll coverage. Host-owned redirect/resolution/click coverage lives in `MorWalPizVideo.ShortLinks.Tests`, while all five ShootingRange test files and their local fixtures/support live in `MorWalPizVideo.ShootingRange.Tests`. Reqnroll metadata remains declared in `.feature` sources and generated `.feature.cs` files must not be edited. Central CI runs all three backend test projects; deployment workflows use the dedicated host projects for ShortLinks and ShootingRange, while BackOffice and ServerAPI retain their existing mixed-project owner filters.

## Common Commands

Use project scripts and solution commands as defined by current manifests. Shared frontend packages build in models, services, layout order. Run the narrowest affected test/build first, then broaden to consumers. See the [test execution runbook](testing.md) for selected-test paths, exact commands, and CI-equivalent filters.

The [delivery/test matrix](deployment.md#delivery-and-test-matrix) lists actual runners and fresh pass/fail/not-run results. Empty active frontend suites fail (`--passWithNoTests=false`); filtered backend deployment suites emit TRX results and fail when the selected count is zero; desktop tests require Windows. `build-uncheck` is not a substitute for a failed TypeScript gate. CI changes do not authorize production operations.

## Documentation Discipline

New features update this guide and an ADR when they alter ownership, contracts, persistence, authentication, deployment, or operational policy.