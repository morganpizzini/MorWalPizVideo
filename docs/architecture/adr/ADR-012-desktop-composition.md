# ADR-012: Desktop Composition Direction

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

At acceptance, VideoImporter and InsightScanner relied on static `App` services and significant code-behind, with HTTP-client composition requiring incremental improvement. They are supported applications with persisted/local workflows. Current bounded implementation status is recorded below rather than treating that historical context as the current main-window composition.

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

## Slice 8 Implementation Status (2026-10-01)

Both existing Generic Hosts register and resolve constructor-injected main windows; startup no longer uses `StartupUri`. Factory-managed HTTP composition and child-window static dependency facades are preserved. Importer initializes database/tenant/upload dependencies before window use and stops tenant subscriptions/window work before awaiting refresh completion and disposing the host. No SQLite schema or production visual changes are part of this slice.

Source-complete is not gate-complete: Windows builds pass and the repaired STA real-App resource fixture verifies host/tenant/cancellation/shutdown behavior on temporary `EnsureCreated` SQLite. Importer's separate migration test still fails on pending model changes; existing-database upgrades are unproven. InsightScanner's unrelated fake DTO equality assertion remains failing. TD-018 is partially addressed, not closed; [Windows applications](../windows-apps.md) records exact fresh results and remaining validation.