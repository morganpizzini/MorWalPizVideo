# ADR-012: Desktop Composition Direction

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

VideoImporter and InsightScanner use static `App` services, direct HttpClient construction, and significant code-behind. They are supported applications with persisted/local workflows.

## Decision

Adopt .NET Generic Host, constructor injection, typed/factory-managed HTTP clients, and testable service boundaries incrementally. Move touched WPF behavior toward MVVM without a wholesale rewrite. Preserve SQLite compatibility and tenant filters.

## Alternatives

- Full rewrite: rejected due to risk and unrelated scope.
- Keep static service location permanently: rejected because tests and lifecycle management remain difficult.
- Move desktop workflows into web APIs: rejected because local upload/scanning responsibilities are intentional.

## Consequences

Old and new composition patterns coexist temporarily. New/touched services become independently testable and network clients follow repository policy.

## Migration And Rollback

Introduce host composition and adapt one workflow at a time. Existing static access may wrap DI temporarily; rollback retains the previous workflow without database changes.

## Validation

Windows CI builds, migration tests, tenant-isolation tests, fake-provider tests, cancellation behavior, and API contract tests gate each slice.