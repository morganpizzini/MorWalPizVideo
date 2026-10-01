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

### End To End

- BackOffice mutation to public cache refresh.
- Public catalog to anonymous cart to private download remains on hold with shop; no active test expansion.
- Short-link management to redirect and count.
- Desktop API-key submission to BackOffice remains an interactive/provider verification gate; existing Windows test suites now execute in CI.

Existing frontend Vitest and Windows VSTest runners execute in CI alongside backend HTTP tests. Real-browser desktop/mobile/SSR cookie/CSRF/CORS proof is still BLOCKED, not satisfied by Node or HTTP tests; see the [operational checklist](operations/phase5-activation-and-recovery.md).

`MorWalPizVideo.BackOffice.Tests` remains the owner of BackOffice, ServerAPI, shared, cross-API, and ShortLinks management/Reqnroll coverage. Host-owned redirect/resolution/click coverage lives in `MorWalPizVideo.ShortLinks.Tests`, while all five ShootingRange test files and their local fixtures/support live in `MorWalPizVideo.ShootingRange.Tests`. Reqnroll metadata remains declared in `.feature` sources and generated `.feature.cs` files must not be edited. Central CI runs all three backend test projects; deployment workflows use the dedicated host projects for ShortLinks and ShootingRange, while BackOffice and ServerAPI retain their existing mixed-project owner filters.

## Common Commands

Use project scripts and solution commands as defined by current manifests. Shared frontend packages build in models, services, layout order. Run the narrowest affected test/build first, then broaden to consumers.

The [delivery/test matrix](deployment.md#delivery-and-test-matrix) lists actual runners and fresh pass/fail/not-run results. Empty active frontend suites fail (`--passWithNoTests=false`); filtered backend deployment suites emit TRX results and fail when the selected count is zero; desktop tests require Windows. `build-uncheck` is not a substitute for a failed TypeScript gate. CI changes do not authorize production operations.

## Documentation Discipline

New features update this guide and an ADR when they alter ownership, contracts, persistence, authentication, deployment, or operational policy.