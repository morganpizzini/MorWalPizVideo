# .NET 10 to .NET 11 migration handoff

Status: **Implemented in source; qualification incomplete.** Updated
2026-11-23. [Official release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json)
lists .NET 11 as active. No artifacts were published, deployed or used to
migrate data.
This record does not authorize release, database operations, or changes to
the shop hold.

## Outcome

All 21 C# project targets, required runtime-aligned package references,
CI SDK selectors, API container images and FreezerAnalysis Playwright test
paths now target .NET 11. No API, desktop UI, or persisted schema changes were
intended. The .NET 11 release is active; SDK 11.0.100 and runtime 11.0.0 are
installed locally. Azure, desktop distribution, mixed-version and production
runtime qualification remain release gates.

## Pre-Migration Baseline

- Before the change, all 21 C# project files targeted `net10.0` or
  `net10.0-windows`.
  [The solution](../../MorWalPizVideo.sln) includes the host, shared, test,
  desktop and API projects; project manifests are the inventory of record.
- [AppHost](../../MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj)
  references all five hosts and uses Aspire AppHost SDK 13.4.6 and JavaScript
  hosting 13.5.3. [AppHost composition](../../MorWalPizVideo.AppHost/Program.cs)
  includes public, admin, Ask, Workshop, Range, Freezer and on-hold shop local
  clients. Shared changes are built into each host, not independently deployed.
- [CI](../../.github/workflows/ci.yml) and five API deployment workflows
  selected SDK 10. CI builds the five hosts, AppHost, Windows clients and five API
  images; it runs seven C# test projects. The Freezer CI and deployment tests
  use a literal `net10.0` Playwright output path.
- The five API Dockerfiles used .NET 10 SDK and ASP.NET images. Freezer's
  [Dockerfile](../../FreezerAnalysis.Api/Dockerfile) also installs Chromium,
  uses a Noble runtime image, and switches back to an unprivileged user.
- [ServerAPI delivery](../../.github/workflows/deploy-serverapi.yml),
  [BackOffice delivery](../../.github/workflows/deploy-backoffice-api.yml)
  and [ShortLinks delivery](../../.github/workflows/deploy-shortlink.yml)
  gate both image staging and separate active code-package deployment. Image
  deployment is explicitly disabled. ServerAPI has a post-deploy account
  bootstrap smoke test. [Freezer delivery](../../.github/workflows/deploy-freezer-analysis-api.yml)
  builds an image but disables image deployment.
  **Unlike the older deployment guide**, current
  [Shooting Range delivery](../../.github/workflows/deploy-shooting-range-api.yml)
  contains an enabled image deployment step and a disposable SQL Server gate.
  Actual hosted execution, environment protections and deployed state are
  unverified. These workflows can run on pushes to main; a merge is not a
  harmless staging operation.
- [Shooting Range startup](../../MorWalPizVideo.ShootingRange/Program.cs)
  uses SQL Server in its non-mock path, verifies schema readiness, and runs
  EF migration only in its explicit migration mode. This supersedes older
  Mongo-related descriptions in deployment documentation.
  [VideoImporter startup](../../MorWalPiz.VideoImporter/Services/DatabaseService.cs)
  applies pending SQLite migrations, rejects unknown migration history, and
  verifies physical schema. BackOffice and ServerAPI use shared Mongo
  repositories; Freezer registers its own Mongo context.
- The baseline package families included ASP.NET and Extensions 10.0.11, EF Core
  10.0.11 for VideoImporter and 10.0.0 for Range, MongoDB driver 3.11.1,
  Hangfire 1.8.25, Playwright 1.58.0 and diverse test runner versions. Inspect
  project manifests, not a presumed central package catalog. No root
  `global.json` or central build/package props were found.

## Implemented Design

Retargeted every project together to `net11.0` or `net11.0-windows`. Changed
ASP.NET Core, Extensions and EF Core package references to stable 11.0
packages (11.0.1 for HTTP resilience and service discovery). Test SDK and
other unrelated third-party package versions were retained. No EF migration,
API contract, credential, or data transformation was added.

Dockerfiles use the official `11.0-resolute` SDK/runtime image variants
(Ubuntu 26.04); FreezerAnalysis uses the matching Microsoft package feed.
Playwright's current Linux support list includes Ubuntu 26.04. CI
and deployment workflows select SDK `11.0.x`. The AppHost SDK and Aspire
JavaScript package were both updated to 13.6.5, the latest 13.x patch verified
to fix the [critical dashboard advisory](https://github.com/advisories/GHSA-4g86-54fm-h9h8)
and [high Aspire.Hosting advisory](https://github.com/advisories/GHSA-f7vh-h5g3-7jrh)
found during restore. The .NET 11 AppHost build succeeds with that compatible
package set.

No temporary dual-targeting or root `global.json` was added. Existing .NET 10
artifacts and runtime configuration were not changed or removed. Mixed-version
runtime and data compatibility still require qualification before any rollout.

Existing workflow triggers and deployment paths were preserved. Review the
trigger and protected-environment behavior before merge; no workflow run or
release was initiated here.

## Impacted Projects

- Shared: `MorWalPizVideo.Models`, `MorWalPiz.Contracts`,
  `MorWalPizVideo.YouTubeUtilities`, `MorWalPizVideo.Domain`,
  `MorWalPizVideo.MvcHelpers`, `MorWalPizVideo.ServiceDefaults` and
  `MorWalPizVideo.AppHost`: project graph, package/telemetry and orchestration.
- Hosts: `MorWalPizVideo.BackOffice`, `MorWalPizVideo.ServerAPI`,
  `MorWalPizVideo.ShortLinks`, `MorWalPizVideo.ShootingRange`,
  `FreezerAnalysis.Api`: target, runtime image and compatibility.
- Desktop: `MorWalPiz.VideoImporter`, `MorWalPiz.InsightScanner`: Windows
  runtime/install prerequisites, WPF, integration and local SQLite.
- Tests: `MorWalPizVideo.BackOffice.Tests`, `MorWalPizVideo.ShortLinks.Tests`,
  `MorWalPizVideo.ShootingRange.Tests`, `FreezerAnalysis.Api.Tests`,
  `MorWalPizVideo.YouTubeUtilities.Tests`, `MorWalPiz.VideoImporter.Tests`,
  `MorWalPiz.InsightScanner.Tests`: target and actual nonempty execution.
- Frontend workspaces, including the tagged Workshop dashboard: verify-only
  consumers; no UI redesign or frontend package changes are planned. The
  on-hold shop remains on hold. Standalone frontend tools: not affected.
- Deployment: central CI, five API image pipelines, three ZIP releases,
  Range's enabled image deployment, Azure runtime configuration, local
  Aspire and desktop distribution are affected or require verification.

## Changed Files

### Project targets, packages and tooling

- All 21 C# project manifests: target frameworks and required Microsoft
  runtime-aligned package updates. Project references and desktop Windows
  targeting remain intact.
- [CI](../../.github/workflows/ci.yml) and
  [BackOffice](../../.github/workflows/deploy-backoffice-api.yml),
  [ServerAPI](../../.github/workflows/deploy-serverapi.yml),
  [ShortLinks](../../.github/workflows/deploy-shortlink.yml),
  [Shooting Range](../../.github/workflows/deploy-shooting-range-api.yml),
  [Freezer](../../.github/workflows/deploy-freezer-analysis-api.yml)
  workflows: toolchain selection and Freezer test artifact paths; existing
  gates and triggers remain unchanged.
- [BackOffice Dockerfile](../../MorWalPizVideo.BackOffice/Dockerfile),
  [ServerAPI Dockerfile](../../MorWalPizVideo.ServerAPI/Dockerfile),
  [ShortLinks Dockerfile](../../MorWalPizVideo.ShortLinks/Dockerfile),
  [Range Dockerfile](../../MorWalPizVideo.ShootingRange/Dockerfile),
  [Freezer Dockerfile](../../FreezerAnalysis.Api/Dockerfile): .NET 11
  `resolute` SDK/runtime images and Ubuntu 26.04 Microsoft package feed;
  existing users, ports, publishing and browser setup are retained.

### Documentation

- Updated [testing](../architecture/testing.md),
  [deployment](../architecture/deployment.md), and
  [Windows applications](../architecture/windows-apps.md); this handoff
  records implementation and validation. Deployment documentation now
  reflects Range's SQL Server readiness and active image-deploy workflow.
  No ADR was needed for a framework version update.

### Verify Only / Not Changed

- [AppHost composition](../../MorWalPizVideo.AppHost/Program.cs),
  [Range startup](../../MorWalPizVideo.ShootingRange/Program.cs),
  [Importer database service](../../MorWalPiz.VideoImporter/Services/DatabaseService.cs),
  existing EF migrations, Mongo serializers/index initialization,
  auth/cookie/CSRF middleware, cross-API cache services and
  [shared container action](../../.github/actions/build-container-image/action.yml).
- Existing frontend clients, protected environment settings, Azure runtime
  versions/OS/architecture, desktop distribution method, backups and recovery
  access: verify without recording credentials or assuming deployed state.

No source application behavior, API contract, generated code, EF migration,
database, frontend source, dependency lockfile, or deployment secret changed.

## Implementation And Validation

- Official release metadata lists .NET 11 as active. Installed SDK/runtime:
  11.0.100 / 11.0.0.
- Official .NET 11 Docker manifests, the Ubuntu 26.04 Microsoft repository
  package URL, and the [Playwright Linux support list](https://playwright.dev/dotnet/docs/intro#system-requirements)
  were checked. No image was pulled/built because local disk capacity was low.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  passed. The full no-build suite passed: 771 passed, 2 skipped, 0 failed.
- `dotnet build MorWalPizVideo.ShortLinks.Tests/MorWalPizVideo.ShortLinks.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  passed; its no-build suite passed: 17 passed.
- `dotnet build MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  passed; its no-build suite passed: 100 passed, 9 skipped. Skips are the
  disposable SQL Server integration cases, not a SQL compatibility result.
  A later CI SQL Server run exposed that the duplicate-username test's direct
  `DbContext` insert omitted the required tenant ID and failed on the tenant
  foreign key before reaching the unique index. The test row now uses the
  fixture's current tenant. The affected test project builds successfully;
  the focused local test is skipped because no disposable SQL Server is
  configured. A disposable SQL Server run is still needed to confirm the
  expected unique-index error.
  A later CI SQL Server run also exposed that the concurrent-friend test passed
  the recipient's ID where `RequestFriendAsync` requires a username. Its helper
  now supplies the seeded recipient username separately while retaining the
  ID for request lookup and acceptance. The disposable SQL Server workflow
  should rerun both corrected cases. After this correction the focused test
  project build passed, all 54 `ShootingRangeServiceTests` passed, and the
  concurrent SQL test was discovered but skipped locally because no
  disposable SQL Server is configured.
- `dotnet build MorWalPizVideo.FreezerAnalysis.Api.Tests/FreezerAnalysis.Api.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  passed. The full suite had 73 passed, 2 skipped and 2 failures because
  Playwright Chromium was not installed. Re-running with
  `--filter 'FullyQualifiedName!~FreezerDocumentRendererTests'` passed:
  73 passed, 2 skipped. The browser/rendering behavior remains unverified.
- `dotnet build MorWalPizVideo.YouTubeUtilities.Tests/MorWalPizVideo.YouTubeUtilities.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  passed; its no-build suite passed: 6 passed.
- `dotnet build MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  passed and compiled all five API hosts on .NET 11. An earlier restore attempt
  failed with `No space left on device`; build succeeded with restored assets.
- Windows test projects were not executed in this Linux environment.
  Clean container builds were not completed: the shared filesystem had under
  0.5 GB free and both NuGet restore and Docker image extraction hit the space
  limit. The official image tag family was checked, but no image was built.
- Restore/build emitted existing package audit warnings, including critical
  Aspire Dashboard SDK and high Aspire.Hosting advisories before the AppHost
  package update. Their patched versions were verified as 13.6.5 and applied.
  The remaining high/moderate ImageSharp advisories and NU1510 package-pruning
  warnings are pre-existing, were not suppressed, and were not changed here.
  Aspire 13.6.5 also reports ASPIRE010 because the existing AppHost disables
  the CLI bundle; no CLI-bundle setting was changed.
- No GitHub workflow, Azure deployment, or production runtime was exercised.

## Impact Matrix

| Area | Decision / check |
| --- | --- |
| Backend/API | No intentional route or behavior changes; test all five hosts and mixed-version interactions. |
| Frontend/UI | No frontend changes; smoke current consumers, especially authenticated flows and Workshop proxy. |
| Database/Data | Preserve Mongo documents/indexes, Range SQL schema and Importer SQLite; no automatic new migrations. |
| Contracts/Models | Preserve JSON, dates, enums, null behavior, error shapes and embedded Mongo representations across versions. |
| Authentication/Authorization | Verify JWT, API keys, cookie protection/key persistence, CSRF, CORS and authorization failures, including rolling instances. |
| Validation | Compare request validation and status/error shapes, including invalid and anonymous requests. |
| Cache | Verify output-cache tags, no-store account responses and cross-API invalidation across version overlap; no redesign. |
| Configuration/Secrets | Preserve protected providers and flags. Independently confirm Azure framework and Windows runtimes; never add credentials. |
| Logging/Telemetry | Preserve structured logs, health/readiness, trace export, and record versions/errors without secrets. |
| Background Jobs | If enabled in the target environment, verify Hangfire storage, recurring IDs, retry/idempotency and single execution during overlap. Do not activate it. |
| Deployment/Operations | Keep active ZIP and Range image mechanisms; verify staged images separately and require actual platform support and approved rollout. |
| Backward Compatibility | Both host versions must share data and interoperate during rollout; desktop older/newer versions must remain compatible with BackOffice. |

## Reuse Opportunities

Use the existing host-specific and mixed API test fixtures, mock scenarios,
Windows fake services, SQL disposable integration gate, CI container matrix,
ServerAPI smoke, health endpoints, shared cache invalidation and existing
deployment approvals. Do not introduce new infrastructure, HTTP clients,
serializers, schema mechanisms or feature flags merely for the upgrade.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | .NET 11/Azure/Windows or dependency not supported; production cannot start. | Verify official release/support matrix, architecture and platform runtime availability; prove published artifact startup in a production-like environment before release. |
| High | Main-branch workflow automatically publishes or deploys an unqualified artifact. | Isolate qualification, inspect triggers/protected environments and require explicit release approval; record staging and production artifact identity. |
| High | SQLite/SQL provider upgrade or startup migrates data incompatibly; rollback loses access or writes. | No new migrations; test old/new reads and writes on disposable production-shaped copies and validate recovery; stop if changes are necessary. Importer rejects unknown migration history. |
| High | Auth, serialization, cache or shared-data drift breaks mixed-version API requests. | Cross-version tests in both directions, including cookie/CSRF and cache invalidation; retain prior artifacts and observe errors. |
| Medium | Aspire, Mongo, Hangfire, Playwright, test adapters or third-party packages have unsupported behavior. | Review vendor support and security advisories; targeted integration tests, Chromium rendering, job continuity and nonempty test results. |
| Medium | Container distro, native libraries or permissions differ; Freezer PDF generation fails. | Clean build and run each image; test non-root startup, readiness and actual PDF/render path on supported base. |
| Medium | A desktop requires a runtime or installation flow absent on user machines. | Inventory distribution/framework-dependent versus self-contained, qualify Windows versions and upgrade/rollback on a test workstation. |
| Medium | Cross-version artifacts or database restore overwrite live writes. | Roll back app/runtime first; restore data only with explicit recovery approval and a documented write-loss/reconciliation decision. |
| Low | Stale documentation obscures actual Range SQL and deployment behavior. | Correct evidence after implementation and check workflow/runtime separately. |

## Technical Debt

- Deployment documentation now reflects Range SQL readiness and its enabled
  image deployment. Confirm that deployment is intentional before release.
- Aspire AppHost SDK and JavaScript hosting packages are now aligned at 13.6.5.
  The build reports ASPIRE010 because CLI bundle use remains disabled; verify
  whether existing local workflows need any bundled-only Aspire feature.
- Importer and other test projects use different runner versions. Keep them
  unless compatibility requires change; do not use the migration as a
  blanket dependency refresh.
- Existing browser, hosted runtime and interactive desktop evidence is
  incomplete. Record it as a release gate, not a passing result.

## Implementation Roadmap And Status

1. **Inventory and qualify — source checked.** .NET 11 release metadata and
   the local SDK/runtime were verified; the official .NET 11 `resolute` image
   tags were checked. Azure runtime availability, production settings and
   Windows desktop distribution remain unverified.
2. **Protect release and data — not executed.** No release, deployment,
   backup/restore exercise or production data access was performed. Existing
   triggers remain; inspect approvals and artifact retention before merge or
   release.
3. **Retarget shared graph and consumers — implemented.** All 21 manifests,
   runtime-aligned packages and Aspire 13.6.5 applied. The AppHost build
   compiles all five hosts. All seven test project target frameworks were
   updated; five suites were built and four test suites plus the
   non-Chromium Freezer subset executed. Windows and SQL Server-only tests
   remain unrun/skipped.
4. **Update delivery and test paths — implemented, partly validated.** CI and
   API workflow selectors, Freezer Playwright output paths and five image
   Dockerfiles were updated. Hosted CI and clean container builds remain
   unverified because local image extraction failed under low disk capacity.
5. **Compatibility and operational qualification — incomplete.** Freezer
   Chromium tests, Range disposable SQL tests, Windows tests, mixed-version
   data/auth/cookie checks and live host readiness are still release gates.
   The full Freezer test command failed in renderer tests because the required
   local Chromium executable was absent; the remaining subset passed.
6. **Controlled rollout — not authorized or run.** Azure runtime, protected
   environment approvals and host configuration must be independently
   verified. No artifact was published and no host deployed.
7. **Close after observation — pending.** Keep prior artifacts and runtimes.
   Record any later hosted/production evidence and obtain explicit owner
   approval before removing .NET 10 artifacts, data compatibility or backups.

## Testing Strategy And Results

- **Unit:** existing YouTube, model and domain tests; add focused cases only
  for demonstrated runtime differences.
- **Integration/behavior:** all seven C# suites with positive discovery and
  executed counts; existing BackOffice/ServerAPI/ShortLinks filtered release
  tests, shared/cross-API, Freezer and real disposable Range SQL transaction
  and concurrency gates. Do not overlap builds writing the same outputs.
- **Contracts/security:** old and new HTTP producers/consumers, JSON/null/date
  shapes, validation errors, unauthorized/forbidden responses, CSRF, CORS,
  cookie decrypt across instances, API-key client behavior, no-store and
  cache eviction. Confirm stable data-protection key configuration, not
  just same-process cookies.
- **Data/migration:** disposable representative Mongo, SQL and SQLite
  snapshots: old reads/new writes and new reads/old writes, schema and
  migration history unchanged, tenant isolation, restart and failed startup.
  Importer startup must not introduce a migration unknown to the old binary.
- **Desktop:** both Windows test suites and actual WPF start, configuration,
  WebView2, fake BackOffice calls and Importer local database on supported
  Windows, including old-version reopen after new-version use.
- **End-to-end/operations:** clean builds and startup of all five images;
  ZIP publish under the actual Azure-compatible runtime; non-root ports and
  health/readiness; Freezer PDF plus Chromium; Aspire app startup; existing
  browser flows, redirects, BackOffice-to-public cache coordination and
  desktop API integration. Collect artifact identities and results; mock
  runs alone do not prove external services or real browser behavior.

Completed local results are listed under
**Implementation And Validation**. No schema migration was generated; no
production integration or mixed-version release test was performed.

## Migration And Rollback

No Mongo, SQL, SQLite, API, auth or cache data migration is planned. The
compatibility window begins before the first deployment and ends only after
owner-approved observation and explicit retirement approval. Verify both old
and new readers/writers on representative isolated copies; no backfill or
destructive contraction. Keep compatible data protection keys and cache
contracts during rolling deployment. If a provider/runtime unexpectedly
requires a schema or representation change, stop and produce a separately
approved expand/migrate/verify/contract plan, including idempotency and
partial-failure monitoring.

Rollback each host using its previous exact ZIP or immutable image and the
corresponding supported runtime/configuration; a Git revert alone does not
roll back a deployed artifact. Retain the old desktop installer/runtime and
verify that old Importer starts against data last written by the new build.
Never downgrade a database or restore over live writes as an automatic
rollback: assess missing writes and recovery explicitly with the data owner.
Monitor readiness, authentication failures, error rates, latency, jobs,
serialization and data integrity throughout; thresholds, observation period,
and release owners require approval before execution.

## Open Questions

1. Confirm all five hosts and both Windows applications are in release scope,
   and whether desktop and Range/Freezer rollout should remain independent.
2. Which Azure App Service runtime settings and Windows desktop distribution
   targets must be qualified, and what observation window will the owner
   approve before rollout?
3. What are the actual production deployment states, protected-environment
   approvals, artifact retention, desktop distribution method and recovery
   owners? In particular, is Range's enabled image deployment intentional?
4. Which production-like integration environments, data-safe fixtures and
   explicit monitoring thresholds are available for the compatibility gates?
