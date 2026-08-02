# ADR-007: Deterministic Development Scenarios

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Local workflows depend on MongoDB, Blob, Key Vault, YouTube, social APIs, AI, and other providers. Mock repositories exist as code-initialized, in-memory entity collections (`IMockScenario`, `BaseScenario`, `PrimaryScenario`, `BaseMockRepository<T>` in `MorWalPizVideo.Domain`) rather than JSON fixture files. Only one scenario (`PrimaryScenario`) exists today and several external providers (Translator/AI completion, Pinterest, reCAPTCHA, Web Push) still lack fakes, so named scenarios and full external-fake coverage remain incomplete.

## Decision

Use deterministic named scenarios behind existing repository interfaces and fake every external provider required for an offline workflow. Composition roots choose complete provider sets. Development enables only Dev and Swagger by default; scenarios select mocks without enabling unrelated production features.

## Alternatives

- Require all cloud dependencies: rejected due to cost, speed, fragility, and offline needs.
- Mock controllers/UI only: rejected because behavior would bypass real application boundaries.
- Add a second application architecture for mocks: rejected because interfaces already permit adapter replacement.

## Consequences

Fakes require maintenance and contract tests against real adapters. Developers gain repeatable local, UI, and integration workflows.

## Migration And Rollback

Add scenarios and fakes one feature at a time while retaining real-provider configuration. Production rejects mock providers.

## Validation

Run supported end-to-end workflows with no cloud credentials and verify scenario isolation between tests.