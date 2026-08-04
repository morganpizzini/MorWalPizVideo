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
- Public catalog to anonymous cart to private download.
- Short-link management to redirect and count.
- Desktop API-key submission to BackOffice (deferred; VideoImporter and InsightScanner are excluded from this backend-only iteration).

Browser runners and frontend E2E are also deferred. Backend coverage and HTTP contract tests are the primary validation target.

## Common Commands

Use project scripts and solution commands as defined by current manifests. Shared frontend packages build in models, services, layout order. Run the narrowest affected test/build first, then broaden to consumers.

## Documentation Discipline

New features update this guide and an ADR when they alter ownership, contracts, persistence, authentication, deployment, or operational policy.