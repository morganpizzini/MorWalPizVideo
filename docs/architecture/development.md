# Development Architecture

## Goal

Developers should run core workflows without MongoDB, Key Vault, Blob Storage, YouTube, social networks, AI providers, email, or push infrastructure.

## Development Flags

Only these feature flags are enabled by default in development:

- `EnableDev`
- `EnableSwagger`

Development environment and `EnableDev` must both be true before fake authentication or permissive CORS is active. Production startup rejects development-only providers.

## Scenario-Based Mocks

Current source uses `IMockScenario`, `PrimaryScenario`, and `BaseMockRepository<T>` to provide cloned, lock-protected in-memory collections initialized directly in C#.

Target scenario characteristics:

- Stable IDs and deterministic timestamps.
- Coherent relationships across content, channels, categories, products, carts, and users.
- Fresh isolated scenarios for tests.
- Explicit named scenarios such as empty, standard, authorization failure, external failure, and legacy compatibility.
- No real credentials or production-derived personal data.

Repository interfaces remain unchanged between Mongo and mock modes.

## External Fakes

Provide deterministic implementations for:

- Blob upload/list/download and SAS issuance.
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
| BackOffice + SPA | JSON repositories, fake integrations, fake auth |
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
- Desktop API-key submission to BackOffice.

## Common Commands

Use project scripts and solution commands as defined by current manifests. Shared frontend packages build in models, services, layout order. Run the narrowest affected test/build first, then broaden to consumers.

## Documentation Discipline

New features update this guide and an ADR when they alter ownership, contracts, persistence, authentication, deployment, or operational policy.