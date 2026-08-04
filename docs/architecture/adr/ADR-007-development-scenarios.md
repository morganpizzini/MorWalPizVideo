# ADR-007: Deterministic Development Scenarios

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Local workflows depend on MongoDB, Blob, Key Vault, YouTube, social APIs, AI, and other providers. Mock repositories exist as code-initialized, in-memory entity collections (`IMockScenario`, `IMockScenarioLifecycle`, `BaseScenario`, and `BaseMockRepository<T>` in `MorWalPizVideo.Domain`) rather than JSON fixture files. Named scenarios are selected by fixture override, startup `MockScenario` configuration, or the `Primary` default.

## Decision

Use deterministic named scenarios behind existing repository interfaces and fake every external provider required for an offline workflow. Composition roots choose complete provider sets. Development enables only Dev and Swagger by default; scenarios select mocks without enabling unrelated production features. Supported names are `Primary`, `Empty`, `Authorization`, `ExternalFailure`, and `LegacyCompatibility`. `Reset()` restores the selected baseline and `Reinitialize()` replaces the selected scenario without rebuilding the host.

## Alternatives

- Require all cloud dependencies: rejected due to cost, speed, fragility, and offline needs.
- Mock controllers/UI only: rejected because behavior would bypass real application boundaries.
- Add a second application architecture for mocks: rejected because interfaces already permit adapter replacement.

## Consequences

Fakes require maintenance and contract tests against real adapters. Developers gain repeatable local, UI, and integration workflows.

## Migration And Rollback

Add scenarios and fakes one feature at a time while retaining real-provider configuration. Production rejects mock providers. VideoImporter, InsightScanner, frontend applications, browser runners, and frontend E2E are outside this backend-only scope.

## Validation

Run supported end-to-end workflows with no cloud credentials and verify scenario isolation between tests.