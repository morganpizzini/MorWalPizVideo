# ServerAPI OpenAPI And Smoke Failure Handoff

## Outcome

Diagnose the ServerAPI `/openapi/v1.json` HTTP 500 and determine whether it explains the deployment smoke failure. The OpenAPI failure has a concrete source-level mechanism in Workshop request-contract inheritance. The deployment smoke failure is separate: GitHub Actions run `44151497999`, job `134281641366`, received HTTP 200 for attempts 1-11, but at least one unreported response predicate failed each time. Attempt 12 timed out and the script's per-attempt truncation erased the useful response evidence before final diagnostics.

Assumptions:

- The deployed revision contains the current Workshop contracts.
- Production enables `FeatureManagement:EnableSwagger` through runtime configuration, despite its checked-in default being false.
- No production log containing the underlying OpenAPI exception has yet been supplied.
- The recently added `OutputCache(NoStore = true)` attributes are part of the patch baseline and should be retained.

## Current Evidence

- `MorWalPizVideo.ServerAPI/Program.cs` conditionally registers `AddOpenApi()` and maps `/openapi/v1.json`; the Swagger UI uses that document.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` declares nullable `IdempotencyKey` on `CreateWorkshopComponentRequest`.
- In the same file, `CreateAndInstallWorkshopComponentRequest` and `UpdateWorkshopComponentRequest` inherit from that base and each redeclares a required `IdempotencyKey` with the same JSON name. Each resulting derived type therefore presents two JSON-visible properties named `idempotencyKey`.
- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs` exposes both derived request types as action bodies, making their schemas reachable while the OpenAPI document is generated.
- The expected runtime failure mechanism is a `System.InvalidOperationException` reporting that the JSON property name for one of the derived Workshop request types' `IdempotencyKey` properties collides with another property.
- `MorWalPizVideo.ServerAPI/Program.cs` converts non-development exceptions into generic HTTP 500 Problem Details, which explains why the Swagger client reports only an internal server error.
- `MorWalPizVideo.BackOffice.Tests/Infrastructure/ServerApiWebApplicationFactory.cs` explicitly disables Swagger in both process environment and test configuration. The ServerAPI CI test filter therefore cannot detect OpenAPI document-generation failures.
- `.github/workflows/deploy-serverapi.yml` does not smoke-test OpenAPI. Its only post-deployment smoke request is `GET /api/v1/account/csrf`, and success additionally requires a token body, CSRF cookie, exact allowed-origin header, and credentials header.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` implements the CSRF endpoint without referencing Workshop contracts.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` now applies `OutputCache(NoStore = true)` to CSRF and session reads, and `PublicProfileController.Get` has the same exclusion. This closes an output-cache risk but does not determine which deployed smoke predicate failed.
- `MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs` verifies those attributes through reflection and verifies the CSRF status, token, cookie reuse, and cache-control header. It still does not send the production origin or assert both CORS response headers required by deployment.
- Before this patch, non-Development startup forced `UseCors("MorWalPizPolicy")` while selected controllers declared `EnableCors("public-account")`, leaving the deployed CSRF policy selection untested. The patch now uses the default middleware path so endpoint metadata selects the named credentialed policy.
- The `FeatureManagement:EnableCors` values in the test factories are not consumed by ServerAPI startup and do not disable CORS in these tests.
- GitHub Actions run `44151497999`, job `134281641366`, reports HTTP 200 on smoke attempts 1-11. This proves the deployed CSRF endpoint was reachable and returned an HTTP-success response throughout most of the warm-up window.
- The smoke success condition contains five predicates: HTTP 200, token member present, CSRF `Set-Cookie` present, exact allowed-origin header present, and credentials header equal to `true`. The log reports only status, so it does not identify which of the latter four failed.
- Before every retry, `.github/workflows/deploy-serverapi.yml` truncates the shared header and body files. Attempt 12 then timed out with status `000`, leaving those files empty. The terminal `cat` commands therefore printed no evidence from the eleven successful HTTP responses.
- The timeout on attempt 12 is secondary evidence, not the primary failure: the smoke had already rejected eleven HTTP 200 responses.
- `PublicAccountSecurity.CsrfCookie` is `public_account_csrf`, so the workflow's case-insensitive `set-cookie:.*csrf` predicate should match the intended cookie name when the header is present.
- The OpenAPI and deployment smoke failures remain separate diagnoses. The smoke evidence now rules out a persistent host-startup failure and points to response-content or CORS/header mismatch.

## Proposed Design

Keep exactly one JSON-visible `IdempotencyKey` on each Workshop request type while preserving current wire semantics:

- Component creation may omit the key.
- Create-and-install and update require the key.

The contract design should avoid property hiding. Prefer separating reusable component fields from operation-specific request contracts through composition or a non-request base that does not own operation metadata. If the smallest compatible correction can preserve inheritance without duplicate JSON metadata, it must still prove required validation on the two derived operations and optional validation on basic creation.

Add a focused ServerAPI OpenAPI integration check with Swagger enabled. It should request the generated document and verify successful generation; it should not assert the full generated JSON or implementation constants.

Patch the CSRF/CORS flow independently:

1. Retain `OutputCache(NoStore = true)` on CSRF, session, and profile reads.
2. Register one environment-appropriate default CORS policy: the existing permissive policy in Development and the current exact-origin ordinary policy outside Development.
3. Invoke `UseCors()` without forcing a policy name. Ordinary endpoints then use the environment default, while endpoint metadata selects the named `public-account` policy for account, profile, Workshop, feedback, and blog-interaction controllers.
4. Do not add credentials to the ordinary public policy. Credentialed CORS remains limited to the exact named `public-account` origins.
5. Add an integration request matching deployment: `GET /api/v1/account/csrf` with origin `https://morwalpiz.com`. Verify 200, token presence, the `public_account_csrf` cookie name, exact allowed origin, credentials `true`, and no-store behavior. Add an unsupported-origin case proving that credentialed CORS headers are absent.

Change the deployment smoke diagnostics so every failed attempt logs a safe predicate summary:

- HTTP status and curl exit category.
- Whether the JSON token member is present, never the token value.
- Whether the expected CSRF cookie name is present, never the cookie value or complete `Set-Cookie` header.
- The normalized `Access-Control-Allow-Origin` value.
- The normalized `Access-Control-Allow-Credentials` value.
- Response content type and non-sensitive timing fields.

Preserve the latest HTTP response that reached the server in separate diagnostic files, or emit the safe summary immediately, so a later timeout cannot erase earlier evidence. Do not print the raw response body or complete headers because they contain the CSRF token and cookie value. Keep retries for transient transport and non-200 responses, but once an HTTP 200 response fails a deterministic contract predicate, the diagnostic summary is sufficient to identify the mismatch; repeated identical 200 responses should not be described as App Service warm-up.

Deliver the CSRF/CORS/smoke correction and the OpenAPI contract correction as independently revertible change groups. They share a deployment but not a failure mechanism. No frontend redesign is involved; existing TypeScript request shapes are compatibility evidence only.

## Impacted Projects

- `MorWalPiz.Contracts`: owns the conflicting Workshop request contracts.
- `MorWalPizVideo.ServerAPI`: exposes the affected request bodies and owns OpenAPI generation and the CSRF smoke endpoint.
- `MorWalPizVideo.BackOffice.Tests`: owns ServerAPI integration tests and the test application factory.
- `frontend/fe-packages/models`: verify-only consumer that establishes optional versus required `idempotencyKey` semantics.
- GitHub Actions ServerAPI deployment: modify the smoke diagnostics to identify failed response predicates safely and preserve useful evidence across retries.
- Other .NET projects, frontend applications, standalone tools, and deployment surfaces: no direct change identified.

## Impacted Files

### Modify

- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`: remove duplicate JSON-visible property ownership while preserving operation validation.
- `MorWalPizVideo.ServerAPI/Program.cs`: make the existing ordinary production policy the non-Development default and allow endpoint metadata to select the named credentialed policy.
- `MorWalPizVideo.BackOffice.Tests/Infrastructure/ServerApiWebApplicationFactory.cs`: permit a focused factory/configuration variant with OpenAPI enabled without weakening existing test isolation.
- `MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs`: add functional production-origin, rejected-origin, credentialed CORS, cookie-name, and no-store assertions.
- An existing ServerAPI endpoint test file under `MorWalPizVideo.BackOffice.Tests/Features/`, or a focused new file if no existing owner is coherent: cover successful OpenAPI generation and Workshop request validation.
- `.github/workflows/deploy-serverapi.yml`: emit per-predicate safe smoke diagnostics, distinguish transport failure from contract mismatch, and prevent a later timeout from erasing earlier successful-response evidence.

### Add

- None required if the OpenAPI test fits an existing ServerAPI endpoint test file; otherwise one focused ServerAPI OpenAPI integration test file.

### Generated/Migration

- None. The correction changes request metadata only and requires no MongoDB or SQLite migration.

### Documentation

- `docs/feature-handoffs/serverapi-openapi-smoke-failure-handoff.md`: update after implementation with actual files, validation, CI evidence, and residual risks.

### Verify Only

- `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`: confirm controller mapping and required-operation semantics remain unchanged.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs`: retain the new no-store attributes and current cookie behavior unless functional tests disprove it.
- `frontend/fe-packages/models/src/workshop.ts`: preserve current optional/required wire semantics.
- GitHub Actions run `44151497999`, job `134281641366`: compare the next run's predicate summary with this baseline.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | OpenAPI generation traverses invalid duplicate JSON metadata. CSRF behavior remains controller-owned while CORS policy selection is normalized in startup. |
| Frontend/UI | No UI change. Existing TypeScript optional/required key semantics must remain compatible. |
| Database/Data | Not applicable; no persisted schema or data transition. |
| Contracts/Models | Primary correction is in shared Workshop request contracts. |
| Authentication/Authorization | No auth change. Credentialed CORS remains limited to exact public-account origins; unsupported origins must not receive credential headers. |
| Validation | Preserve optional key for basic creation and required key for create-and-install/update. Add negative integration coverage. |
| Cache | Retain the new no-store boundary for CSRF, session, and profile reads. |
| Configuration/Secrets | Swagger remains feature-flagged. Do not expose runtime settings or secrets in diagnostics. |
| Logging/Telemetry | Production logs are required to confirm the exact thrown type and to classify the smoke failure. |
| Background Jobs | Not applicable. |
| Deployment/Operations | OpenAPI should be verified before deployment. The post-deployment smoke needs safe per-predicate diagnostics because eleven HTTP 200 responses were rejected without identifying the mismatch. |
| Backward Compatibility | JSON property name and operation requirements must remain unchanged. |

## Reuse Opportunities

- Reuse `ServerApiWebApplicationFactory` and existing ServerAPI test traits.
- Reuse the existing `/api/v1/account/csrf` integration-test setup for the exact production-origin request and rejected-origin case.
- Reuse current data-annotation validation rather than adding controller-local duplicate checks.
- Keep `Microsoft.AspNetCore.OpenApi` and the current feature flag; no new OpenAPI package or parallel Swagger generator is needed.

## Risks

- **High — OpenAPI remains broken:** another duplicate member survives the contract correction. Mitigation: request `/openapi/v1.json` through the test host with OpenAPI enabled. Signal: HTTP 200 and parseable OpenAPI JSON.
- **Medium — Wire validation regression:** restructuring makes the basic create key required or derived-operation keys optional. Mitigation: explicit positive and negative request tests for all three operations. Signal: expected model-validation status for omitted keys.
- **Medium — Smoke cause is guessed incorrectly:** changing OpenAPI does not repair the response predicate mismatch. Mitigation: add safe per-predicate diagnostics before changing endpoint or CORS behavior. Signal: the next run identifies the exact missing token/cookie/CORS signal.
- **High — Credentialed CORS broadening:** adding credentials to the ordinary public policy would allow cookies on more cross-origin endpoints than intended. Mitigation: make the ordinary policy the default but keep credentials only on named `public-account`. Signal: an allowed account origin receives credentials while unsupported origins do not.
- **Medium — Ordinary CORS regression:** changing middleware selection could remove CORS from ordinary public endpoints. Mitigation: preserve current exact ordinary origins as the non-Development default and add one representative ordinary-origin regression check.
- **Low — Frontend contract drift:** backend correction changes property naming or presence. Mitigation: preserve `idempotencyKey` and existing operation requirements. Signal: frontend type-check and affected Workshop component tests remain green.
- **Medium — Security exposure through diagnostics:** the raw body and complete `Set-Cookie` header contain CSRF values. Mitigation: log only booleans, normalized CORS values, content type, status, and timing; never print the raw body or cookie/token values. Signal: workflow logs contain no CSRF token or cookie value.
- **Low — Data integrity:** no persistence change is planned. Signal: existing Workshop behavior tests remain green.

## Technical Debt

- Swagger is disabled in the ServerAPI test factory, leaving document generation outside the current CI gate. This directly enabled the regression and should be addressed now with a focused enabled variant.
- Deployment smoke assertions exceed current integration coverage, particularly exact-origin credentialed CORS.
- The smoke script conflates deterministic HTTP 200 contract mismatches with warm-up and stores diagnostics in files that are truncated before each retry. Address this observability defect now; defer endpoint/CORS changes until the improved log identifies the failing predicate.
- `FeatureManagement:EnableCors` is set by test factories but is not consumed by ServerAPI startup. It does not block this patch and must not be treated as proof that CORS is disabled in tests.
- Property hiding in externally serialized DTO inheritance is the immediate design debt. Remove it within the affected contracts; do not broaden into an unrelated contract hierarchy refactor.

## Implementation Roadmap

1. Add the exact production-origin CSRF request to `PublicAccountEndpointTests` and assert all deployment predicates, no-store behavior, and an unsupported-origin negative case. This is the first gate and should fail before startup correction if policy selection is the defect.
2. Normalize ServerAPI CORS registration in `Program.cs`: configure the existing ordinary policy as the non-Development default, retain the named credentialed `public-account` policy, and invoke `UseCors()` without forcing the ordinary policy. Do not broaden origins or credentials.
3. Retain the recently added `OutputCache(NoStore = true)` attributes and functional cache-control assertions. Do not add more CSRF controller behavior unless the exact integration test still fails.
4. Update the deployment smoke to emit a safe named result for every predicate, preserve the latest server-response summary across later transport failures, and distinguish contract mismatch from warm-up or transport failure.
5. Build `MorWalPizVideo.BackOffice.Tests` once and run focused public-account tests without rebuilding, then run the complete ServerAPI-related CI filter.
6. Deploy and confirm the smoke passes. If it fails, use the new predicate summary and do not make another speculative CSRF change.
7. Separately confirm the deployed OpenAPI exception. If it matches the duplicate Workshop property mechanism, correct request-contract ownership so each concrete type has one JSON-visible `idempotencyKey`, preserving optional basic-create and required create-and-install/update semantics.
8. Add focused Workshop validation cases and an OpenAPI-enabled ServerAPI integration test that retrieves and parses `/openapi/v1.json`.
9. Run the focused OpenAPI/Workshop tests and complete ServerAPI-related CI filter, then verify deployed Swagger independently from the CSRF smoke.
10. Update this handoff with actual files changed, validation, smoke predicate evidence, OpenAPI exception, and residual risk.

An independent post-validation review is warranted because a shared external contract and a production deployment gate are involved. The reviewer should focus on wire compatibility, model validation, OpenAPI coverage, and whether the smoke diagnosis is evidence-based.

## Testing Strategy

- **Unit:** not necessary unless contract validation is extracted into pure logic.
- **Integration/behavior:** in `MorWalPizVideo.BackOffice.Tests`, send the exact production-origin CSRF request and assert token, expected cookie name, exact origin, credentials, and no-store; assert an unsupported origin receives no credentialed CORS headers. Separately request `/openapi/v1.json` with Swagger enabled and verify basic create accepts an omitted key while create-and-install/update reject it.
- **Component:** not applicable unless the frontend model changes, which is not recommended.
- **Contract:** verify a single camel-case `idempotencyKey` appears for each affected OpenAPI request schema and current JSON payloads still bind.
- **Security:** confirm Workshop auth/CSRF requirements are unchanged, ordinary public CORS does not gain credentials, unsupported origins receive no credentialed CORS headers, and OpenAPI exposure remains governed by the existing feature flag.
- **Migration:** not applicable.
- **End-to-end:** after deployment, verify Swagger document retrieval separately from the anonymous account CSRF smoke. For the CSRF smoke, verify status, token, expected cookie name, exact origin, and credential header. Prove that a failed predicate is named without exposing its value and that a later timeout does not erase prior diagnostics.

## Migration And Rollback

- **MongoDB/SQLite:** no migration or backfill.
- **API contracts:** maintain the existing JSON name and required/optional behavior, so no compatibility window or dual-read/write is needed.
- **Authentication:** no intended change.
- **Cache:** retain explicit no-store behavior for account reads.
- **Configuration:** normalize CORS policy selection without changing the approved origin lists.
- **Deployment order:** deploy shared contract and ServerAPI together through the existing ServerAPI package. Frontend redeployment is unnecessary if its models remain unchanged.
- **Monitoring:** observe OpenAPI request status and the post-deployment CSRF smoke independently. The smoke log must distinguish transport failure, HTTP failure, and response-contract mismatch.
- **Rollback:** revert the contract correction and OpenAPI test together only if the correction breaks Workshop binding; this would restore the known OpenAPI defect, so a forward correction is preferred. Smoke-specific changes require their own rollback based on the proven cause.

## Implementation Result

Implemented the plan without requiring additional deployment answers:

- `MorWalPizVideo.ServerAPI/Program.cs` now registers the exact ordinary production origins as the default policy outside Development and always calls `UseCors()` without forcing that policy. Endpoint metadata can therefore select the credentialed `public-account` policy for account and Workshop routes, while ordinary public routes remain non-credentialed.
- The recent `OutputCache(NoStore = true)` attributes on CSRF, session, and profile reads were retained.
- `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs` now keeps shared component fields in `WorkshopComponentRequestBase`. Basic creation, create-and-install, and update each own one `IdempotencyKey` property, removing the OpenAPI JSON-property collision while preserving optional versus required semantics.
- `ServerApiWebApplicationFactory` supports an isolated Swagger-enabled instance for the OpenAPI regression test without changing the existing default test isolation.
- `PublicAccountEndpointTests` now covers the exact production origin, credentialed CORS headers, CSRF cookie name, no-store behavior, and rejection of credentialed CORS for an unsupported origin.
- `ServerApiOpenApiTests` retrieves and parses `/openapi/v1.json` with Swagger enabled.
- `.github/workflows/deploy-serverapi.yml` now reports safe per-attempt values for HTTP status, curl exit code, token presence, CSRF cookie presence, allowed-origin matching, and credential matching. It no longer prints raw response bodies or headers containing CSRF values.

## Validation Result

- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  - Passed. Existing package vulnerability and nullable-analysis warnings remain.
- Focused public-account tests:
  - `dotnet test ... --no-build --filter "FullyQualifiedName~PublicAccountEndpointTests"`
  - Passed: 9/9.
- Focused OpenAPI test:
  - `dotnet test ... --no-build --filter "FullyQualifiedName~ServerApiOpenApiTests"`
  - Passed: 1/1.
- CI-equivalent ServerAPI gate:
  - `dotnet test ... --no-build --filter "Category=TestGroup:ServerAPI|Category=TestGroup:Shared|Category=TestGroup:CrossApi"`
  - Passed: 112/112.

The GitHub Actions smoke script was not executed locally. Its next deployment run is the required proof that the production response satisfies all named predicates; the new log format will identify any remaining mismatch without exposing CSRF values.

## Residual Risks

- The deployment environment still needs one successful run after this change.
- Existing `FeatureManagement:EnableCors` test settings remain unused by ServerAPI startup; no behavior depends on them, so they were left untouched.
- Existing package vulnerability and nullable-analysis warnings were not part of this patch.
