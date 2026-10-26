# BackOffice Test Runner Timeout Handoff

### Outcome
Make local BackOffice tests start and complete reliably by correcting the .NET test-toolchain mismatch and stale-process behavior observed on Windows. Assume CI remains the behavioral reference because the supplied GitHub run reached xUnit and completed the failing endpoint tests in about seven seconds. This is a test infrastructure correction, not an API or endpoint redesign.

### Current Evidence
- The timed-out focused invocation emitted only `NETSDK1057` and never emitted the VSTest `Test run for ...` banner. The process therefore stalled before test discovery/execution and before `BackOfficeWebApplicationFactory` could start the API host.
- `docs/architecture/deployment.md` records the local SDK as `10.0.400-preview.0.26322.102`; all active GitHub workflows select stable `10.0.x` through `actions/setup-dotnet@v4`.
- The repository has no root `global.json`, so SDK selection depends on each machine's installed SDKs and can select a preview independently of CI.
- `MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj` targets `net10.0` and references `Microsoft.NET.Test.Sdk` 18.9.0 plus `xunit.runner.visualstudio` 4.0.0.
- `public-match-test.diag.log` identifies the local VSTest runner as `18.7.0-release-26322-102` from the preview SDK. It repeatedly fails to load `Microsoft.Bcl.AsyncInterfaces, Version=9.0.0.8` and also reports a missing `Microsoft.VisualStudio.TestPlatform.Fakes` assembly before continuing that historical run. This is direct evidence that the preview SDK's test platform is not cleanly aligned with the current test packages.
- The same historical diagnostic eventually starts VSTest and completes two tests in about one second, showing that these loader failures are intermittent/toolchain-sensitive rather than an endpoint-level deterministic wait.
- The recent timed-out process was left active after the 120-second caller timeout, and later validation started another `dotnet` operation. The existing creator-partnership handoff also records `MSB4242` SDK resolver/worker-node failures. Concurrent or orphaned build nodes can therefore compound the SDK problem.
- `MorWalPizVideo.BackOffice.Tests/Features/CreatorPartnershipEndpointTests.cs` contains no sleeps, manual waits, or timeout primitives.
- The only BackOffice `IHostedService` found is `MongoIndexStartupInitializer`; it is not registered in the mock branch used by `BackOfficeWebApplicationFactory`. Current evidence does not implicate application startup.
- `.github/workflows/deploy-backoffice-api.yml` restores first and then executes tests with `--no-restore`; the supplied CI run reached xUnit, confirming that the project can execute under the workflow toolchain.

### Proposed Design
Use one supported stable .NET 10 SDK/test-platform baseline for local development and CI.

1. Recover the current machine by ending the still-running timed-out `dotnet`/testhost/MSBuild work and shutting down reusable build-server nodes before another attempt. Do not start overlapping validation attempts.
2. Install/select a stable .NET 10 SDK whose bundled VSTest is compatible with the repository's `Microsoft.NET.Test.Sdk` 18.9.0. Confirm the exact stable SDK from a green CI job or installed stable SDK inventory; do not pin the recorded preview build.
3. Add a root `global.json` that rejects preview SDK selection and uses a deliberate stable roll-forward policy within the approved .NET 10 servicing boundary.
4. Make GitHub setup use the same root SDK policy rather than maintaining an independent floating selection. Prefer the setup-dotnet mechanism that reads the checked-in global policy; if unavailable in the installed action version, pin the same stable SDK explicitly in all .NET workflows.
5. Keep package versions unchanged initially. CI already reaches VSTest with the current `Microsoft.NET.Test.Sdk` and xUnit packages, so downgrading test packages would mask environment drift and increase scope.
6. Validate in distinct phases: restore, compile, then test without rebuilding. This makes any recurrence attributable to restore/build, VSTest discovery, testhost startup, or application behavior. Capture VSTest diagnostics and hang dumps only if execution reaches VSTest and then stalls.

Material alternative: change `Microsoft.NET.Test.Sdk` to match the preview SDK's bundled VSTest. Reject this because the preview SDK is the divergent environment, all server projects target stable .NET 10, CI selects stable .NET 10, and other test projects already standardize on Test SDK 18.9.0.

### Impacted Projects
- Entire .NET solution: root SDK selection affects every .NET project and test project.
- `MorWalPizVideo.BackOffice.Tests`: primary validation owner; package references remain unchanged unless stable-SDK validation disproves compatibility.
- `MorWalPizVideo.BackOffice`: verify-only through WebApplicationFactory startup after VSTest begins.
- Other .NET test projects: regression validation because they share the root SDK and most use the same Test SDK/runner versions.
- GitHub workflow surfaces that install .NET: align with the checked-in SDK policy.
- Frontend workspaces and standalone browser tools: not affected.

### Impacted Files
**Modify**
- `.github/workflows/ci.yml`: consume the repository SDK policy for every .NET job.
- `.github/workflows/deploy-backoffice-api.yml`: align the test and deployment jobs with the same SDK.
- `.github/workflows/deploy-serverapi.yml`: align shared test/deployment SDK selection.
- `.github/workflows/deploy-shortlink.yml`: align ShortLinks test/deployment SDK selection.
- `.github/workflows/deploy-shooting-range-api.yml`: align Shooting Range SDK selection.
- `docs/architecture/development.md`: document the stable local SDK prerequisite and no-preview policy.
- `docs/architecture/testing.md`: distinguish pre-VSTest driver stalls from testhost/application hangs and require sequential restore/build/test diagnosis.
- `docs/architecture/deployment.md`: replace the preview-SDK local baseline after stable validation.

**Add**
- `global.json`: root stable .NET 10 SDK selection and preview rejection policy. The implementer must select the exact version from a current green CI environment or approved stable installed SDK rather than inventing it.

**Generated/Migration**
- None. Do not commit diagnostic logs, dumps, TestResults, `bin`, or `obj` outputs.

**Documentation**
- `docs/feature-handoffs/backoffice-test-runner-timeout-handoff.md`: implementation decisions, actual selected SDK, validation, and residual risks.

**Verify Only**
- `MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj`: current Test SDK and xUnit packages.
- `MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs`: only investigate after VSTest starts.
- `MorWalPizVideo.BackOffice/Program.cs` and `MorWalPizVideo.BackOffice/Services/MongoIndexStartupInitializer.cs`: only investigate if diagnostics prove a host-start phase stall.
- Every other `*Tests.csproj`: shared SDK compatibility regression surface.

### Impact Matrix
| Surface | Impact |
| --- | --- |
| Backend/API | No route, controller, service, or runtime behavior change. |
| Frontend/UI | Not applicable; no Impeccable workflow or visual evidence required. |
| Database/Data | No MongoDB or SQLite access, schema, or data change. |
| Contracts/Models | No contract or model change. |
| Authentication/Authorization | No change; test authentication remains intact. |
| Validation | Test infrastructure only; endpoint validation behavior remains separately covered. |
| Cache | Not applicable. |
| Configuration/Secrets | Add non-secret SDK policy only; no environment credentials. |
| Logging/Telemetry | Temporary local VSTest diagnostics may be generated but must remain untracked. |
| Background Jobs | Not applicable; mock tests keep production jobs disabled. |
| Deployment/Operations | Workflow SDK setup becomes deterministic and shared with local policy. |
| Backward Compatibility | Runtime binaries remain .NET 10; developer/CI machines must have or acquire the approved stable SDK. |

### Reuse Opportunities
Reuse the existing .NET 10 target framework, `actions/setup-dotnet@v4`, current test packages, workflow restore-before-test pattern, `docs/architecture/testing.md` runbook, and current focused test filters. Do not add a wrapper script, custom test runner, alternate xUnit adapter, or repository-local SDK binary.

### Risks
- **High - SDK version chosen incorrectly:** a non-installed or unavailable exact SDK can break all .NET commands. Mitigation: derive the version from a current green CI runner or approved stable installation and validate every test project; signal: SDK resolution is stable locally and in every workflow.
- **High - orphaned process mistaken for code hang:** another timeout can leave build/test nodes active. Mitigation: terminate the timed-out operation and reusable build servers before retry; signal: a clean sequential run reaches VSTest promptly.
- **Medium - workflow drift:** some .NET workflow jobs may continue to float independently. Mitigation: inventory every setup-dotnet occurrence and route all through the same policy; signal: workflow logs report the same stable SDK family.
- **Medium - roll-forward policy too broad:** future feature bands could change VSTest unexpectedly. Mitigation: use the narrowest stable servicing roll-forward that still works on developer and hosted runners; signal: SDK identity remains predictable across patch updates.
- **Medium - package churn hides the cause:** downgrading Test SDK/xUnit may create a second compatibility matrix. Mitigation: keep packages fixed for the first stable-SDK validation; signal: current packages run under stable SDK without loader failures.
- **Low - application issue remains after toolchain repair:** if VSTest starts but WebApplicationFactory then stalls, a second diagnosis is needed. Mitigation: preserve phase-specific diagnostics; signal: logs clearly cross from VSTest discovery into testhost/host startup.
- Security, data integrity, cache coherence, and external-service risks: none; no production behavior or secrets change.

### Technical Debt
- No root SDK policy allows local/CI toolchain divergence. This blocks reliable validation and should be addressed now.
- Diagnostic logs are present at repository root but are not authoritative test artifacts. Do not expand this task into artifact cleanup unless they are tracked; keep new diagnostics outside source control.
- Test package versions differ in `MorWalPiz.VideoImporter.Tests`; this does not cause the BackOffice pre-VSTest stall and should remain deferred unless the root SDK change exposes a reproducible compatibility failure there.
- The existing creator-partnership handoff labels the timeout as test-host startup. Correct that record: available evidence places the recent stall before VSTest/testhost startup.

### Implementation Roadmap
1. **Recover the local runner.** Stop the outstanding timed-out test and all stale reusable .NET build/test nodes. Verify no previous validation remains active before proceeding. Gate: a fresh SDK identity check returns promptly and identifies stable versus preview selection.
2. **Select the supported baseline.** Identify a stable .NET 10 SDK available to developers and GitHub runners, and verify its bundled VSTest aligns with Test SDK 18.9.0. Gate: no preview identifier and no missing `Microsoft.Bcl.AsyncInterfaces` loader warning in a diagnostic smoke run.
3. **Check in SDK policy.** Add root `global.json` with preview disabled and a bounded stable roll-forward policy. Gate: repository-root SDK selection resolves to the approved stable build on a clean shell.
4. **Align workflows.** Update every .NET setup-dotnet use to consume the same policy or exact approved version. Gate: static workflow review finds no independent `10.0.x` selector where deterministic parity is required.
5. **Validate by phase.** Restore once, compile the BackOffice test project, then execute the focused creator-partnership class without restore or rebuild. Gate: VSTest banner appears promptly and the class produces a pass/fail summary rather than a caller timeout.
6. **Broaden regression proof.** Run the BackOffice deployment filter, full BackOffice test project, then the other .NET test projects appropriate to their platform. Gate: nonempty test counts and no SDK/test-platform loader errors.
7. **Escalate only with phase evidence.** If a stable no-build run reaches VSTest but then hangs, capture VSTest diagnostics and blame-hang output and trace WebApplicationFactory startup. Do not modify application startup before this gate is crossed.
8. **Update documentation.** Record the exact stable SDK, workflow changes, focused/full results, and any remaining platform-specific gaps in this handoff and architecture runbooks. Stop at SDK/test reliability; endpoint behavior fixes remain separate.

An independent post-validation review is warranted because root SDK/workflow policy affects every .NET project and release gate. The user may select `MorWalPiz Code Reviewer` after implementation.

### Testing Strategy
- SDK selection: verify repository-root resolution uses stable .NET 10 and rejects the recorded preview SDK.
- Test-platform smoke: execute one small test assembly and confirm diagnostics contain no missing test-platform dependency warnings.
- Focused integration: execute `CreatorPartnershipEndpointTests` without build/restore and require a concrete two-test result.
- BackOffice regression: execute the deployment-equivalent BackOffice/Shared/CrossApi filter with a nonempty TRX count, then the full BackOffice project.
- Cross-project regression: run ShortLinks, Shooting Range, YouTubeUtilities, InsightScanner, and VideoImporter tests on their established platforms.
- CI: verify each changed workflow reports the approved SDK and publishes nonempty test results.
- Negative case: on a machine with only the preview SDK, repository SDK resolution must fail clearly rather than silently selecting preview.
- No frontend, browser, database migration, contract, authorization, cache, or end-to-end product tests are required for the SDK-policy change.

### Migration And Rollback
No database, API, auth, cache, or application configuration migration is required. Roll out the root SDK policy and workflow alignment together so local and CI selection do not diverge. Developers lacking the stable SDK must install it before .NET work. Rollback removes the root pin and restores workflow selectors, but that reintroduces the diagnosed drift and should be used only if the selected stable SDK is unavailable. Preserve current Test SDK/xUnit package versions during rollback analysis so only one variable changes at a time.

### Open Questions
- Which exact stable .NET 10 SDK version is installed by the latest green CI job? This determines the checked-in baseline and must be captured during implementation.
- Is the previously timed-out `dotnet test` process still active in one of the open PowerShell terminals? It must be stopped before any discriminating rerun.
