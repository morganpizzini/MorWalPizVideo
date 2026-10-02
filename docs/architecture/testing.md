# Test Execution Runbook

This is the source-aligned runbook for selecting and reporting tests in
MorWalPizVideo. Project files, package manifests, and workflow files are
authoritative if this document becomes stale.

## Start Narrow

1. Identify the owning project or frontend workspace.
2. List or run the smallest relevant class, test name, feature, or category.
3. Broaden to the owning project, then to the CI-equivalent filter.
4. Report the exact command, working directory, selected path/filter, and
   result when asking for help.

Do not use a historical `TestResults` artifact as current pass/fail evidence.

## Prerequisites and Working Directories

- Backend tests require the .NET 10 SDK used by CI. Run the commands below
  from the repository root.
- `MorWalPiz.InsightScanner.Tests` and `MorWalPiz.VideoImporter.Tests` target
  `net10.0-windows` and enable WPF. Run them on Windows with the required
  Windows desktop runtime.
- Frontend tests require Node.js `>=18.0.0` and Yarn Classic `1.22.22`.
  Central CI uses Node 22; the BackOffice SPA deployment workflow uses Node
  20. Run frontend commands from the repository root; `--cwd frontend`
  selects the Yarn workspace root.
- Install frontend dependencies with the committed lockfile before testing:

  ```powershell
  yarn --cwd frontend install --frozen-lockfile
  ```

- Shared frontend packages are built in dependency order. The Admin SPA
  `pretest` builds models and services, while CI explicitly builds all shared
  packages:

  ```powershell
  yarn --cwd frontend build:shared
  ```

- Most backend tests use fake services, test authentication, and isolated
  mock scenarios. The Shooting Range real-Mongo deployment gate additionally
  requires the environment-managed `SHOOTING_RANGE_TEST_MONGO_URI`; do not
  place its value in documentation or commands.

## .NET Test Projects

All current .NET test projects are listed below. Use the project path as the
first narrowing boundary.

| Project | Repository-relative path | Platform and coverage |
|---|---|---|
| BackOffice and mixed API coverage | `MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj` | `net10.0`; BackOffice, ServerAPI, shared, cross-API, and BackOffice management/Reqnroll tests |
| ShortLinks host coverage | `MorWalPizVideo.ShortLinks.Tests/MorWalPizVideo.ShortLinks.Tests.csproj` | `net10.0`; redirect, resolution, and click behavior |
| Shooting Range API coverage | `MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj` | `net10.0`; API and startup configuration tests |
| YouTube utilities | `MorWalPizVideo.YouTubeUtilities.Tests/MorWalPizVideo.YouTubeUtilities.Tests.csproj` | `net10.0`; parsing and transformation reliability |
| InsightScanner Windows tests | `MorWalPiz.InsightScanner.Tests/MorWalPiz.InsightScanner.Tests.csproj` | `net10.0-windows`, WPF; fake scan-service coverage |
| VideoImporter Windows tests | `MorWalPiz.VideoImporter.Tests/MorWalPiz.VideoImporter.Tests.csproj` | `net10.0-windows`, WPF; image and database compatibility coverage |

Restore a selected project, then run it:

```powershell
dotnet restore MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --verbosity normal
```

Replace the project path in both commands for another project. The Windows
projects use the CI-compatible form below:

```powershell
dotnet restore MorWalPiz.InsightScanner.Tests/MorWalPiz.InsightScanner.Tests.csproj -p:Platform=AnyCPU
dotnet test MorWalPiz.InsightScanner.Tests/MorWalPiz.InsightScanner.Tests.csproj --configuration Release --no-restore -p:Platform=AnyCPU --logger trx --results-directory TestResults
```

List discovered tests before selecting one:

```powershell
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --list-tests
```

Select a class, method, or unique name with the standard VSTest filter:

```powershell
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~CrossApiServiceContractTests"
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HttpClientLifetimeAuditTests"
```

For Reqnroll-generated xUnit tests, select the category emitted by the
generated source or narrow by the feature/test name:

```powershell
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:BackOffice"
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Compilations"
```

The `.feature` files under
`MorWalPizVideo.BackOffice.Tests/Features/` are the scenario sources.
Generated `.feature.cs` files are checked-in generated output and must not be
edited; regenerate them only through the project's established Reqnroll
tooling.

## BackOffice Admin SPA Tests

“BackOffice Admin SPA tests” means the `back-office-spa` Yarn workspace, not
the .NET `MorWalPizVideo.BackOffice.Tests` project. The workspace is at
`frontend/back-office-spa`; its test script is `vitest run`.

Run the complete Admin SPA suite, equivalent to the focused CI test command:

```powershell
yarn --cwd frontend workspace back-office-spa test --passWithNoTests=false
```

Run a single Admin SPA test file or test name:

```powershell
yarn --cwd frontend workspace back-office-spa test --passWithNoTests=false --run src/path/to/example.test.ts
yarn --cwd frontend workspace back-office-spa test --passWithNoTests=false --run -t "renders the expected state"
```

Useful local variants defined by the workspace manifest are:

```powershell
yarn --cwd frontend workspace back-office-spa test:watch
yarn --cwd frontend workspace back-office-spa test:coverage
```

The Admin SPA `pretest` builds the shared models and services packages. If
shared package output or dependencies are stale, run
`yarn --cwd frontend build:shared` first. CI also requires the checked
`back-office-spa` build after its test gate:

```powershell
yarn --cwd frontend workspace back-office-spa build
```

If a report says “ffice-spaq” or “admin spa,” first confirm whether it means
the `frontend/back-office-spa` workspace and include that path and command in
the report.

## Other Frontend Suites

The central CI frontend matrix runs these workspaces with
`--passWithNoTests=false`:

| Suite | Repository-relative workspace |
|---|---|
| Admin SPA | `frontend/back-office-spa` |
| Ask | `frontend/ask.client` |
| Public video | `frontend/morwalpizvideo.client` |
| Shooting ITA | `frontend/shooting-ita-frontend` |
| Shooting Range | `frontend/shooting-range.client` |
| Shoot Recorder | `frontend/shoot-recorder` |
| Shared services | `frontend/fe-packages/services` |

Run any matrix workspace using its package name:

```powershell
yarn --cwd frontend workspace ask.client test --passWithNoTests=false
yarn --cwd frontend workspace morwalpizvideo.client test --passWithNoTests=false
yarn --cwd frontend workspace shooting-ita-frontend test --passWithNoTests=false
yarn --cwd frontend workspace shooting-range.client test --passWithNoTests=false
yarn --cwd frontend workspace shoot-recorder test --passWithNoTests=false
yarn --cwd frontend workspace @morwalpizvideo/services test --passWithNoTests=false
```

The shop client and shared `models` and `layout` packages currently do not
define a test script and are not active entries in the central test matrix.
`frontend/TelePrompter` and `frontend/stage-designer` are outside the
architecture guide's active test scope.

## CI-Equivalent Focused Runs

These commands are copied from the current workflows. They include TRX
logging and results directories where the workflow consumes test results.
Deployment jobs reject an empty selected or executed test set.

### BackOffice and ServerAPI deployment gates

```powershell
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:BackOffice|Category=TestGroup:Shared|Category=TestGroup:CrossApi" --logger "trx;LogFileName=backoffice-related.trx" --results-directory ./TestResults/backoffice --verbosity normal
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:ServerAPI|Category=TestGroup:Shared|Category=TestGroup:CrossApi" --logger "trx;LogFileName=serverapi-related.trx" --results-directory ./TestResults/serverapi --verbosity normal
```

### ShortLinks deployment gate

```powershell
dotnet test MorWalPizVideo.ShortLinks.Tests/MorWalPizVideo.ShortLinks.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:ShortLinks" --logger "trx;LogFileName=shortlinks-related.trx" --results-directory ./TestResults/shortlinks --verbosity normal
```

### Shooting Range deployment gates

```powershell
dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~ShootingRange --logger "trx;LogFileName=shooting-range-related.trx" --results-directory ./TestResults/shooting-range --verbosity normal
dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-build --filter FullyQualifiedName~ShootingRangeMongoTests --logger "trx;LogFileName=shooting-range-mongo.trx" --results-directory ./TestResults/shooting-range --verbosity normal
```

The second command is the environment-dependent real-Mongo gate and should
not be substituted for the normal local mock-mode run.

### Central frontend CI shape

The central workflow installs dependencies, builds shared packages, and then
runs each matrix workspace:

```powershell
yarn --cwd frontend install --frozen-lockfile
yarn --cwd frontend build:shared
yarn --cwd frontend workspace back-office-spa test --passWithNoTests=false
```

Replace `back-office-spa` with another workspace from the frontend table.

## Reporting a Failure

Include:

- the exact repository-relative project or workspace path;
- the exact command, including filter, test name, configuration, and working
  directory;
- whether dependencies were restored or installed;
- the platform and SDK/Node/Yarn versions;
- the first failing test name and whether the result came from local output or
  a TRX file.

This makes a request such as “BackOffice Admin SPA tests are failing”
actionable without guessing whether the failing surface is the frontend
workspace, the mixed .NET BackOffice project, or a deployment-specific filter.
