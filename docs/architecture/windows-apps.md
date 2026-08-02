# Windows Applications

## VideoImporter

### Responsibility

VideoImporter is a .NET 10 WPF application for tenant-aware local media preparation, scheduling, YouTube upload, and BackOffice integration.

### Current Architecture

- EF Core SQLite persists settings, tenants, schedules, and local state.
- `ITenantContext` and `ITenantService` coordinate tenant behavior.
- Configuration loads JSON, user secrets, environment variables, and optional Key Vault.
- YouTube upload is abstracted through `IYouTubeUploadService`.
- BackOffice calls use API-key authentication.
- Startup and much UI behavior rely on static `App` properties and code-behind.

### Target Direction

- Adopt Generic Host composition and constructor injection incrementally.
- Keep persisted SQLite schemas backward compatible through migrations.
- Move network and long-running work off the UI thread.
- Replace directly constructed `HttpClient` instances with managed factory/typed clients.
- Move touched UI behavior toward MVVM without rewriting unrelated legacy screens.
- Store API keys in user secrets, environment configuration, Key Vault, or OS-protected storage, never SQLite seed data.

## InsightScanner

### Responsibility

InsightScanner is a .NET 10 WPF application that scans external sources and submits normalized insight data to BackOffice using API-key authentication.

### Current Architecture

- Configuration loads JSON, user secrets, and environment variables.
- `HybridInsightScanner` composes source strategies.
- `IBackOfficeInsightClient` owns submission behavior.
- Startup exposes static services through `App`.
- WebView2 and code-behind coordinate parts of the workflow.

### Target Direction

- Use Generic Host and DI.
- Use a typed/factory-managed BackOffice client.
- Preserve source-strategy extensibility.
- Add deterministic fake source strategies and a fake BackOffice client for offline development.
- Keep API-key material out of source and logs.

## Shared Desktop Rules

- Both applications consume .NET Contracts, not API persistence entities.
- Service-to-service endpoints use the BackOffice API-key scheme and explicit scopes/permissions when introduced.
- Configuration precedence is documented and secrets remain external.
- UI errors are user-safe while structured diagnostic details go to logs.
- Cancellation and progress reporting are required for long-running work.
- Desktop builds are included in CI on a Windows runner.

## Testing Gaps

Neither WPF application has a complete automated suite. Prioritize tests for configuration binding, migrations, tenant isolation, contract serialization, retry/cancellation behavior, and fake external providers before UI automation.