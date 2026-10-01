# Windows Applications

## VideoImporter

### Responsibility

VideoImporter is a .NET 10 WPF application for tenant-aware local media preparation, scheduling, YouTube upload, and BackOffice integration.

### Current Architecture

- EF Core SQLite persists settings, tenants, schedules, and local state.
- `ITenantContext` and `ITenantService` coordinate tenant behavior.
- Configuration loads JSON, user secrets, environment variables, and optional Key Vault.
- YouTube upload is abstracted through `IYouTubeUploadService`.
- A Generic Host owns configuration and DI; `IHttpClientFactory` creates the named BackOffice client used by `IApiServiceFactory`.
- Startup resolves the constructor-injected `MainWindow` from the host after database, tenant, API settings, and upload-service initialization; `StartupUri` is no longer used.
- BackOffice calls preserve API-key authentication and existing DTOs.
- Static `App` properties remain compatibility facades for untouched child windows; main-window dependencies are injected. Tenant refreshes are serialized, and shutdown unsubscribes tenant events, cancels window work, awaits the active refresh, then stops/disposes the host.

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
- A Generic Host owns configuration and DI.
- `IBackOfficeInsightClient` is a typed factory-managed client and owns API-key submission behavior.
- Startup resolves/shows the constructor-injected `MainWindow` through the host, without `StartupUri`; `App` retains its existing static dependency API as a compatibility bridge.
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
- Both existing desktop xUnit/VSTest suites now execute on Windows in central CI with positive TRX executed-count checks. Local Release reruns pass: VideoImporter 9/9 and InsightScanner 4/4. See [delivery matrix](deployment.md#delivery-and-test-matrix).

## Testing Gaps

Neither WPF application has a complete automated suite. Prioritize tests for configuration binding, migrations, tenant isolation, contract serialization, retry/cancellation behavior, and fake external providers before UI automation.

## Desktop Slice 8 Verification (2026-10-01)

The bounded main-window constructor-DI source work is complete, and the full local desktop test gate now passes. Child-window static facades remain; this is not TD-018 closure or a full MVVM migration. No production visuals, SQLite schema/migrations, routes, DTOs, or child dependency APIs were changed by the fixture repair.

The Importer fixture runs on an STA dispatcher and constructs the real `App` and host-resolved base `MainWindow`. It captures/aborts the application's queued automatic startup callback before calling `InitializeComponent`, then explicitly starts the test host. This avoids real startup configuration/user-secret/Key Vault access. The generated resource URI is `/MorWalPiz.VideoImporter;component/app.xaml`, owned by the Importer assembly, not the test assembly. The fixture checks the loaded `BackgroundBrush` is the window's actual background; no replacement styles are supplied.

Only a unique temporary directory is used for SQLite, with an asserted connection path. The host test uses `EnsureCreated` and the existing initializer seam to preserve seeded local data, switch tenants, check tenant filters/channel selection, validate initialization once, cancel window work, unsubscribe tenant events, await an in-flight refresh before service disposal, and reopen the temporary database. It does NOT prove existing-database upgrades beyond the compatibility fixture; no production database was read.

Fresh commands, run on Windows with SDK `10.0.400-preview.0.26322.102` (VSTest/xUnit):

| Command | Result |
|---|---|
| `dotnet test MorWalPiz.VideoImporter.Tests/MorWalPiz.VideoImporter.Tests.csproj --filter FullyQualifiedName~Desktop_host --verbosity minimal` | After repair: 2 passed, 0 failed, 0 skipped; exit 0. Before repair: 1 passed, 1 failed, 0 skipped (test-subclass WPF resource mismatch). |
| `dotnet build MorWalPiz.VideoImporter/MorWalPiz.VideoImporter.csproj --verbosity minimal` | Passed; exit 0. |
| `dotnet build MorWalPiz.InsightScanner/MorWalPiz.InsightScanner.csproj --verbosity minimal` | Passed; exit 0. |
| `dotnet test MorWalPiz.VideoImporter.Tests/MorWalPiz.VideoImporter.Tests.csproj --configuration Release --no-restore --verbosity minimal` | 9 passed, 0 failed, 0 skipped. |
| `dotnet test MorWalPiz.InsightScanner.Tests/MorWalPiz.InsightScanner.Tests.csproj --configuration Release --no-restore --verbosity minimal` | 4 passed, 0 failed, 0 skipped. |

Residual gates: complete real interactive startup/child-window/long-running-operation validation and prove existing-database upgrades beyond the compatibility fixture. The fixture's manually controlled startup is not end-to-end WPF launch evidence. No assertions were deleted or waived; no commit, deployment, production access, or secret retrieval was performed.