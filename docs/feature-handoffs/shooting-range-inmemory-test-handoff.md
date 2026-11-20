# Shooting Range in-memory API tests handoff

## Outcome

Run the ordinary Shooting Range API tests with the existing EF Core in-memory
provider, without Azure SQL configuration, while retaining the separate real
SQL Server integration gate and production fail-closed behavior. This is a
test-host and test-documentation change; no application behavior change is
authorized.

## Implemented Outcome

- The test factory explicitly enables `FeatureManagement:EnableInMemoryDb`
  using test-host settings and disables mock mode and Key Vault. API tests no
  longer depend on machine- or workflow-level storage configuration.
- Each test factory replaces the in-memory EF context options with a unique
  database name. Scopes within one host share data; independent hosts do not.
  Ordinary API tests do not seed an administrator. The dedicated provider
  test opts into a generated administrator password.
- Direct repository access in API tests uses disposed DI scopes. Production
  and Staging startup tests still reject mock and in-memory modes, and a host
  test confirms that SQL mode without its connection string fails closed.
- `MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs`
  uses a process-local gate rather than a SQL transaction for in-memory writes.
  `MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeSqlServerTests.cs`
  explicitly checks SQL uniqueness, locking, rollback and concurrency against
  a disposable server, and skips without its host setting.
- `.github/workflows/ci.yml` runs the full test project; the deployment workflow
  now accurately names the in-memory API gate and retains its separate
  mandatory real SQL Server gate.

## Implementation Details

The factory uses `UseSetting` for feature flags because the application selects
and validates its storage mode before the minimal-host app is built. It also
adds those settings to test configuration, disables Key Vault, and clears any
SQL connection-string value so host behavior is deterministic. For in-memory
hosts, `ConfigureTestServices` replaces the context options with a unique
database name. No application startup or repository code changed.

## Impacted Projects

- `MorWalPizVideo.ShootingRange.Tests`: test factory, test scopes and focused
  startup/isolation regression coverage.
- `MorWalPizVideo.ShootingRange`: verify-only; existing provider and guard
  behavior must remain unchanged.
- Shooting Range deployment workflow and architecture/testing documentation:
  make the focused suite description and configuration accurate. Central CI is
  verify-only.
- Frontends, other .NET projects, and runtime deployment configuration: no
  changes.

## Impacted Files

### Modify

- `MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeApiTests.cs`:
  factory configuration, isolated database, scoped access, seed and host
  validation tests.
- `.github/workflows/deploy-shooting-range-api.yml`: remove the ineffective
  `EnableMock` environment setting from the normal test step and name the step
  for its actual provider; leave the SQL gate intact.
- `docs/GITHUB_PRODUCTION_DEPLOYMENT.md`: accurately describe the API and SQL
  test gates.

### Add

None identified; put focused regression tests in the existing test class.

### Generated/Migration

None.

### Documentation

- `docs/architecture/testing.md`: describe the ordinary in-memory API suite
  and the distinct real-SQL gate; correct the outdated mock-mode wording.
- `docs/shooting-range-architecture.md`: correct the documented mock feature
  name to `UseMock` without changing production policy; describe test coverage
  if useful.
- This handoff: record implementation and validation results.

### Verify Only

- `MorWalPizVideo.ShootingRange/Program.cs` and
  `MorWalPizVideo.ShootingRange/Repositories/SqlServerShootingRangeRepositories.cs`:
  provider selection, startup validation and guarded writes.
- `MorWalPizVideo.ShootingRange.Tests/Services/ShootingRangeSqlServerTests.cs`:
  retain provider-specific integration tests and skip conditions.
- `MorWalPizVideo.ShootingRange.Tests/Features/ShootingRangeStartupConfigurationTests.cs`:
  preserve SQL and Key Vault configuration validation.
- `.github/workflows/ci.yml`: verify the full suite continues to run.
- Both Shooting Range project files: existing EF in-memory dependency is
  already available; no package change is expected.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No runtime code or endpoint changes. |
| Frontend/UI | Not applicable. |
| Database/Data | Test-only ephemeral EF store; production SQL unchanged. |
| Contracts/Models | No changes. |
| Authentication/Authorization | Existing session and CSRF behavior remains under API tests. |
| Validation | Test host selects storage explicitly; deployed non-SQL modes remain rejected. |
| Cache | Not applicable. |
| Configuration/Secrets | Test-only flags; no committed credentials or process-global mutations. |
| Logging/Telemetry | Existing startup warnings remain unchanged. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Correct test-step configuration and wording; preserve mandatory SQL gate. |
| Backward Compatibility | No application flags, SQL configuration, contracts or persistence formats changed. |

## Reuse Opportunities

Reuse the existing in-memory feature, EF provider, scoped repository
implementation, factory, startup checks, seed test, and SQL integration
fixtures. No new provider, library, schema or production abstraction is needed.

## Risks

| Rank | Risk and mitigation | Verification signal |
| --- | --- | --- |
| High | In-memory does not enforce SQL transactions, indexes, or cross-process locking. Keep the independent SQL gate mandatory. | SQL-specific tests execute against a disposable server in deployment CI. |
| High | Test configuration could accidentally bypass production restrictions. Keep production code unchanged and exercise both forbidden modes in Production and Staging. | Host startup rejects each mode and missing SQL configuration. |
| Medium | Shared database names, root-scoped contexts, or global environment settings may leak state or cause parallel races. Isolate each factory and use disposed scopes. | Independent factories cannot see each other's data; scopes within one factory can. |
| Medium | Optional admin seed changes user-count assumptions. Disable it for ordinary tests and explicitly opt in only for seed coverage. | Existing admin/session assertions and dedicated seed test pass. |
| Low | Workflow or documentation still describes an ineffective mock flag. Correct the test step and docs using source's actual flag. | CI configuration and docs match test-host behavior. |

## Technical Debt

The deployment workflow had set `EnableMock` although application startup reads
`UseMock`; the API workflow no longer sets this ineffective variable because
its regular API gate now uses the in-memory provider. Production feature names
and runtime compatibility are unchanged. SQL provider differences are inherent
and cannot be eliminated by in-memory tests.

## Implementation Roadmap

1. Configure the API test factory to use isolated in-memory databases, scoped
   repository resolution and optional test-only administrator seeding. **Done.**
2. Verify production/staging restrictions and the fail-closed SQL startup
   requirement through host tests. **Done.**
3. Update the deployment test step and directly related documentation while
   retaining the real-SQL gate. **Done.**
4. Build and run the reported test, the API class, and the complete project;
   record SQL-test availability. **Done.**

## Testing Strategy

- Build: `dotnet build MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` passed. It reported existing NU1902/NU1903 warnings for `System.Security.Cryptography.Pkcs` 9.0.4.
- Reported regression: `dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~Password_change_invalidates_other_sessions_and_renews_current_session" --verbosity minimal` passed.
- API host tests: the `FullyQualifiedName~ShootingRangeApiTests` filter passed 25 tests, including scoped data sharing, separate-factory isolation, mock/in-memory deployment rejection and missing SQL configuration.
- Full suite: `dotnet test MorWalPizVideo.ShootingRange.Tests/MorWalPizVideo.ShootingRange.Tests.csproj --configuration Release --no-restore --no-build --verbosity minimal` passed 66 tests; six SQL Server integration tests skipped because `SHOOTING_RANGE_TEST_SQL_HOST` was not configured.
- The full suite includes the existing service and startup-configuration tests. No frontend or contract tests changed.
- SQL integration: provider-specific tests remain separate and unchanged, but no SQL proof is claimed because no disposable SQL Server was available for this run.
- End-to-end and migrations: not required for this test-only change; release
  checks remain unchanged.

## Migration And Rollback

No schema, data, API, auth or configuration migration is needed. Production
continues to require Azure SQL and rejects local storage. Roll back the test
factory/workflow/documentation change together if it destabilizes CI; do not
weaken the SQL release gate or change deployed flags as a workaround.

## Open Questions

None identified.
