# Creator Partnership & Campaign Workspace

## Status
- Implemented; BackOffice SPA lists, forms, details, route wiring, permissions, navigation, and service endpoints added. Route smoke tests now provide the required router, toast, and resolved-loader-data dependencies. The interrupted frontend work is now compiling and its focused route smoke tests pass.

## Summary
Added a lightweight, channel-scoped BackOffice workspace for creator partners and their campaigns. Partners own contact details, campaigns reference one partner, and timeline entries are embedded under campaigns with a kind discriminator. The release deliberately excludes public API behavior, a dashboard, CRM automation, and structured link/content models.

## Impacted Areas
- Backend/API: Added `PartnersController` and `CampaignsController` under `/api/Partners` and `/api/Campaigns`.
- Database/Data: Added `partners` and `creatorCampaigns` repositories; timeline entries remain embedded in campaign documents.
- Contracts/Models: Added Partner, Campaign, TimelineEntry, lifecycle enums, and admin response contracts/converters.
- Auth/Security: Added channel-scoped `partners.*` and `campaigns.*` permissions with manage expansion.
- Tests/Validation: Added endpoint coverage and permission/route metadata coverage.

## Expected Change
CRUD supports partner records, campaign records, and campaign-owned timeline entries. Collection reads default to active records; `includeArchived=true` includes completed/archived campaigns and archived partners. Deleting a partner is rejected while a campaign still references it. Content references and timeline URLs are stored as strings only.

## User Review Notes
The source design document was not present in the repository workspace. Status names and the minimal timeline kind set were selected from the request scope and should be reconciled against the external design document if it is supplied later.

## Files and Ownership
| Area | Files | Why it matters |
| --- | --- | --- |
| Add | `MorWalPizVideo.Models/Models/CreatorPartnership.cs` | Persistence entities and lifecycle/timeline enums |
| Add | `MorWalPiz.Contracts/Contracts/CreatorPartnershipContracts.cs` | BackOffice response contracts |
| Add | `MorWalPizVideo.BackOffice/Controllers/PartnersController.cs` | Channel-scoped partner CRUD |
| Add | `MorWalPizVideo.BackOffice/Controllers/CampaignsController.cs` | Channel-scoped campaign and timeline CRUD |
| Modify | `MorWalPizVideo.Domain/DataService.cs`, repository interfaces/implementations, `MorWalPizVideo.BackOffice/Program.cs` | Shared data ownership and Mongo/mock registration |
| Modify | `MorWalPiz.Contracts/ContractUtils.cs`, authorization keys/expander | Conversion and permission contracts |
| Add | `MorWalPizVideo.BackOffice.Tests/Features/CreatorPartnershipEndpointTests.cs` | Scope, CRUD, timeline, and active-filter checks |
| Add | `frontend/back-office-spa/src/models/creatorPartnership.ts` | Contract-first response and request models for the BackOffice SPA |
| Add | `frontend/fe-packages/models/src/creatorPartnership.ts` | Shared contract types for service and SPA consumers |
| Modify | `frontend/fe-packages/models/src/index.ts` | Public shared-model exports |
| Modify | `frontend/fe-packages/services/src/endpoints.ts`, `apiService.ts`, `index.ts` | Typed partner, campaign, and timeline endpoints |
| Modify | `frontend/back-office-spa/src/models/creatorPartnership.ts` | Preserves the existing SPA model import surface over shared types |
| Modify | `frontend/back-office-spa/src/authorization/permissions.ts` | Adds `partners.*` and `campaigns.*` route permissions |
| Modify | `frontend/back-office-spa/src/layouts/adminMenu.ts` | Adds permission-filtered navigation items |
| Modify | `frontend/back-office-spa/src/router/routes/lazy-index.ts` | Registers CRUD route groups |
| Modify | `frontend/back-office-spa/vitest.config.ts` | Makes the SPA root and component aliases deterministic for direct and workspace Vitest runs |
| Add | `frontend/back-office-spa/src/routes/partners/**` | Partner list, form, detail, loaders, and delete action |
| Add | `frontend/back-office-spa/src/routes/campaigns/**` | Campaign list, form, partner loader, detail/timeline view, and delete action |
| Existing | `frontend/back-office-spa/src/routes/partners/__tests__/index.test.tsx`, `frontend/back-office-spa/src/routes/campaigns/__tests__/index.test.tsx` | Focused route smoke tests |

## Validation Plan
- `dotnet build MorWalPiz.Contracts/MorWalPiz.Contracts.csproj --no-restore`: passed.
- `dotnet build MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj --no-restore`: passed.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --no-restore`: passed before the final assertion-only test edit; subsequent retries hit `MSB4242` SDK resolver/worker-node failures.
- Targeted `dotnet test` was attempted but exceeded the 120-second execution window before emitting the VSTest test-run banner; no test-host or test result was captured. See `backoffice-test-runner-timeout-handoff.md`.
- `frontend/back-office-spa/node_modules/.bin/tsc.cmd -p frontend/back-office-spa/tsconfig.app.json --noEmit --pretty false`: passed after removing unused React imports and initializing form result refs.
- `yarn --cwd frontend build:models`: passed.
- `yarn --cwd frontend build:services`: passed.
- `frontend/back-office-spa/node_modules/.bin/tsc.cmd -p frontend/back-office-spa/tsconfig.app.json --noEmit --pretty false`: passed.
- `yarn --cwd frontend workspace back-office-spa build`: passed before the smoke-test harness adjustment.
- `frontend/back-office-spa/node_modules/.bin/tsc.cmd -p tsconfig.app.json --noEmit --pretty false` from `frontend/back-office-spa`: passed after the smoke-test harness adjustment.
- Focused Vitest execution from `frontend/back-office-spa` passed: 2 files and 2 tests passed for the partner and campaign index routes.

## Real Implementation Notes
The implementation uses the established `DataService`, `IRepository<T>`, mock/Mongo registration, `RequireChannelScope`, and `AllowUser` patterns. Partner contact details are not duplicated on campaigns. Timeline update ordering uses due date, then occurrence date, and timeline entry IDs are generated for new entries. Campaign collection filtering excludes `Completed` and `Archived` by default.

## Residual Risks
- Runtime endpoint tests need to be rerun after the local stable-SDK/test-runner issue in `backoffice-test-runner-timeout-handoff.md` is resolved.
- The external design document should confirm the selected lifecycle names and timeline kinds before production data is created.
- Vitest startup remains slow in this workspace, but the focused route tests now collect and pass when run from the SPA project directory.

## Final Documentation Notes
No public ServerAPI surface, content duplication, structured link model, or CRM automation was introduced. Partner deletion remains conservative and fails when campaigns reference the partner.

## CI Failure Follow-Up: 2026-10-26

### Outcome
Diagnose the two reported `CreatorPartnershipEndpointTests` failures and prepare a bounded developer handoff. This follow-up supersedes any implication above that backend endpoint execution has been verified. Analysis only: no application/test changes or execution were performed.

The immediate failure is proven: both CI tests read a top-level `id` from a JSON object that does not contain it. The upstream HTTP failure is not proven because the supplied log contains neither response status nor body. Do not label a casing, authorization, or validation change as a confirmed root-cause fix without that evidence.

### Current Evidence
- Supplied CI run 40135014352 reports `JsonElement.GetProperty` failures at the partner-ID lookup in both tests. Neither test reaches its campaign assertions in that run. The two Calendar Mongo skips are separate from these exceptions; their skip reasons were not supplied.
- `MorWalPizVideo.BackOffice.Tests/Features/CreatorPartnershipEndpointTests.cs`, both test methods: current source already checks partner response success and includes the response body on failure. Current ID lookups are at lines 24 and 49 rather than CI lines 23 and 47. The supplied trace therefore does not match the current file exactly. The checks do not yet cover campaign creation before parsing or every intermediate response.
- `MorWalPizVideo.BackOffice/Controllers/PartnersController.cs`, `PartnerRequest` and `Create`: current optional email/website properties are nullable; conversion normalizes null to empty persisted strings. A successful create returns `PartnerContract` with status Created, not an envelope. Conflict returns a string body.
- `MorWalPiz.Contracts/Contracts/CreatorPartnershipContracts.cs` and `MorWalPiz.Contracts/ContractUtils.cs`: partner/campaign contracts contain `Id`, and conversion copies the entity ID. `MorWalPizVideo.BackOffice/Program.cs` registers controller JSON converters but no property-casing override. No evidence supports changing the API JSON casing to fix this trace.
- `MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs`: clients select `PrimaryScenario.ChannelId`, authenticate as `test-user-id`, and pass requested permission claims. `MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs` initializes both partnership collections and active ownership for that user/channel. Missing default ownership or collections is not supported by current source.
- `MorWalPizVideo.BackOffice/Services/ChannelContext.cs`: scope rejection returns an object containing `code` and `message`, not `id`. `MorWalPizVideo.BackOffice/Authentication/CookieAntiforgeryMiddleware.cs` requires an auth cookie before enforcing CSRF; these newly created test clients do not explicitly supply one.
- `MorWalPizVideo.Domain/DataService.cs`, `SavePartnerAsync`: trims the nonempty name, checks channel-local uniqueness, and persists through the partner repository. Both tests generate unique names.
- `MorWalPizVideo.BackOffice/Controllers/CampaignsController.cs` and `DataService.GetCampaignsAsync`: collection reads exclude Completed and Archived by default; timeline writes return the campaign contract with embedded entries.
- Both tests share an `IClassFixture<BackOfficeWebApplicationFactory>` and mutable scenario storage. The first creates an Active campaign. The second's global empty-collection assertion can fail when that campaign remains in the shared factory. This is a separate latent failure, not the reported partner-ID exception.
- `MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj` and `MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj` target net10.0. `.github/workflows/deploy-backoffice-api.yml` selects BackOffice, Shared, and CrossApi test categories and writes `backoffice-related.trx`.

### Proposed Design
Keep the endpoint contracts, casing, authorization, persistence, and serializer settings unchanged.

Ranked diagnosis:
1. **Confirmed:** unchecked response-shape assumptions hide the original failure in the CI version. Verify the expected status before deserializing every response used by a subsequent step; include status/body in failed assertions.
2. **Leading upstream hypothesis, not confirmed:** request validation rejected omitted optional contact fields in the CI revision. Both tests omit website; one also omits email. Current source already represents those fields as nullable and preserves empty-string persistence through conversion. Confirm the failing revision and response validation errors before attributing the incident to that difference. The exact CI request model has not been inspected.
3. **Lower-ranked alternative:** an early scope/other JSON error was parsed as a partner. The standard current fixture has the required ownership and permissions, so investigate this only if captured status/body supports it.

Complete the local diagnostic assertions; use the shared response contracts for behavioral assertions if consistent with nearby tests, with explicit nonempty ID checks. Typed deserialization must not substitute for checking HTTP success or silently accept a default empty ID.

Make the collection test independent of other tests' records: retain the created Completed campaign's ID, verify that exact ID is absent from the default collection, and verify that exact ID is present with Completed status when archived records are included. Also verify the default results contain neither Completed nor Archived statuses. Do not clear a shared scenario or grant administrative bypass permissions to make the tests pass.

### Impacted Projects
- `MorWalPizVideo.BackOffice.Tests`: response diagnostics, focused optional-field coverage, and collection-test isolation.
- `MorWalPizVideo.BackOffice`: verify the existing nullable request handling; modify only if captured errors demonstrate a remaining request-validation defect.
- `MorWalPiz.Contracts`, `MorWalPizVideo.Domain`, and `MorWalPizVideo.Models`: verify-only; no contract, repository, or data-model change is currently justified.
- Frontend workspaces, standalone tools, other APIs, AppHost, and deployment definitions: no changes. This is not a UI refinement or redesign, so no Impeccable workflow is required.

### Impacted Files
**Modify**
- `MorWalPizVideo.BackOffice.Tests/Features/CreatorPartnershipEndpointTests.cs`: check each dependent HTTP response first, validate IDs, add narrow omitted/valid/invalid optional-contact cases, and remove the global-empty assumption. Reuse this test file rather than introducing another harness.

**Add**
- None required.

**Generated/Migration**
- None required.

**Documentation**
- `docs/feature-handoffs/creator-partnership-campaign-workspace-handoff.md`: preserve this diagnosis and later record actual edits, response evidence, and validation results.

**Verify Only**
- `MorWalPizVideo.BackOffice/Controllers/PartnersController.cs`: preserve nullable optional fields, format validation for supplied values, and normalization at the request-to-entity boundary. A code change here is conditional on observed validation failures, not assumed.
- `MorWalPizVideo.BackOffice/Controllers/CampaignsController.cs`, `MorWalPizVideo.BackOffice/Program.cs`: response shape and existing serialization/registration.
- `MorWalPiz.Contracts/Contracts/CreatorPartnershipContracts.cs`, `MorWalPiz.Contracts/ContractUtils.cs`: existing response types and ID conversion.
- `MorWalPizVideo.Domain/DataService.cs`, `MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs`, `MorWalPizVideo.Domain/Scenarios/BaseScenario.cs`: channel-local behavior and shared mutable fixture semantics.
- `MorWalPizVideo.BackOffice/Services/ChannelContext.cs`, `MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs`: keep scope enforcement and fixture identity intact.
- `.github/workflows/deploy-backoffice-api.yml`: existing deployment validation gate; no workflow change planned.

### Impact Matrix
| Surface | Decision |
| --- | --- |
| Backend/API | Verify optional request handling; do not alter successful response shape. |
| Frontend/UI | No UI changes or visual checks required. |
| Database/Data | No schema/backfill; preserve existing null-to-empty request mapping. |
| Contracts/Models | Reuse existing contracts; no shared-contract changes. |
| Authentication/Authorization | Preserve resource permissions, channel selection, and ownership. |
| Validation | Accept omitted optional email/website; reject nonempty malformed supplied values; assert errors before parsing success bodies. |
| Cache | No cache behavior or tag changes. |
| Configuration/Secrets | No changes; diagnostic output must not include authentication headers or secret values. |
| Logging/Telemetry | Improve test failure diagnostics only; no production logging additions. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Existing filtered BackOffice test gate remains authoritative. |
| Backward Compatibility | Preserve routes, statuses, JSON property names, numeric enums, and stored string fields. |

### Reuse Opportunities
Reuse `BackOfficeWebApplicationFactory.CreateClientWithPermissions`, primary scenario ownership, existing Partner/Campaign/Timeline contracts, and the current endpoint test file. Do not introduce a custom JSON converter, generic response wrapper, fixture reset mechanism, or new application service.

### Risks
- **High, diagnosis:** the original response is absent and current source differs from CI. Mitigation: capture exact revision plus first partner status/body; signal: a specific original HTTP rejection is identified rather than another missing-key exception.
- **Medium, test isolation:** shared Active campaigns invalidate a global empty assertion. Mitigation: assert on the created campaign ID and lifecycle rules; signal: both methods pass together and independently without order assumptions.
- **Medium, validation/compatibility:** suppressing validation globally could admit malformed contact data. Mitigation: preserve format attributes and add omitted/valid/invalid cases; signal: omitted fields succeed and malformed nonempty values return BadRequest.
- **Medium, security:** administrative bypass or scope removal would conceal fixture problems. Mitigation: retain resource-specific permissions and ownership; signal: existing cross-channel request still returns NotFound.
- **Low, rollout:** a test-only change requires no API deployment. Any proven controller repair must preserve existing responses; signal: focused and deployment-filter gates both pass.
- Data-integrity, cache-coherence, and external-service changes: none proposed; existing behaviors are left intact.

### Technical Debt
- Parsing success bodies before checking status obscures the first HTTP failure. Address locally now, including campaign setup and timeline requests.
- Shared mutable fixture state makes collection-wide emptiness assertions order-dependent. Address locally through record-specific assertions; defer harness redesign.
- The existing scenario test name mentions CRUD but does not exercise all update/delete paths. Do not expand into full CRUD coverage for this incident.
- Previous build-only evidence is not runtime proof. Replace the pending endpoint validation note only after a completed test run.

### Implementation Roadmap
1. **Establish original evidence.** Owner: failing CI revision and existing test file. Capture partner response status/body before any property lookup, using current early assertions if applicable. Gate: identify whether validation, scope, or another response caused the missing `id`; if the current tests pass, report the original cause as unconfirmed rather than inventing a repair.
2. **Complete response diagnostics.** Owner: `CreatorPartnershipEndpointTests.cs`. Check exact expected status before parsing partner/campaign/timeline bodies and before consuming collection results; assert nonempty IDs. Gate: a bad setup request reports the originating HTTP status/body, not a downstream JSON lookup.
3. **Verify optional-contact behavior.** Owner: same tests and verify-only `PartnersController.PartnerRequest`. Cover omitted email/website, supplied valid values, and malformed nonempty email/website. Retain the current nullable handling if tests pass. Gate: successful creates return Created with an ID, while malformed supplied values return BadRequest. If a different original cause appears, stop and revise this handoff before changing unrelated production behavior.
4. **Remove order dependency.** Owner: collection test. Track its created Completed campaign, assert its exclusion/inclusion by ID and status, and validate lifecycle exclusions in default results. Gate: both original methods pass independently and together, including when an Active campaign already exists.
5. **Validate and close.** Owner: BackOffice test project and this handoff. Run the focused class first, then the existing deployment-equivalent BackOffice/Shared/CrossApi selection. Record actual results, revision, and residual risks. Stop without unrelated API, frontend, Mongo, serializer, or workflow changes. A separate architecture review is not required for this bounded fix; independent code review is proportionate if production validation changes prove necessary.

### Testing Strategy
- Endpoint/contract: partner and campaign creation return Created before body access; IDs are nonempty; timeline mutation returns OK with a nonempty embedded entry ID.
- Validation: omitted email/website and valid supplied values succeed; malformed nonempty email and website fail with BadRequest and field-specific validation evidence. Verify response shapes rather than relying solely on case-insensitive typed deserialization.
- Collection behavior: the exact created Completed record is absent by default and present with `includeArchived=true`; default records exclude Completed and Archived even when unrelated Active records exist.
- Security: retain the missing-channel NotFound assertion and existing deployment-filter tenancy/permission coverage; do not replace an owned contributor with a manage-all identity.
- Execution: first the existing `CreatorPartnershipEndpointTests` class, then the BackOffice deployment filter in `.github/workflows/deploy-backoffice-api.yml`, following `docs/architecture/testing.md`. No commands or tests were executed during this architecture analysis.
- Unit/component/browser/migration tests: not required for this bounded endpoint-test/validation slice. Calendar Mongo integration skips are a separate evidence gap, not a cause of these failures.

### Migration And Rollback
No MongoDB, SQLite, contract, authentication, cache, or configuration migration is proposed. No backfill, feature flag, or deployment sequencing change is needed. Test-only repairs have no runtime rollout. If a controller validation repair is proven necessary, deploy through the existing BackOffice gate and retain the previous application artifact for rollback; persisted fields and response contracts remain unchanged. Do not revert the current nullable handling merely to match historical CI source.

### Open Questions
- What HTTP status and response body did the first partner create return in failing run 40135014352, and which source revision ran? The supplied stack trace cannot establish the upstream root cause, and the current workspace is not identical to that trace.
- Do the current nullable optional-field handling and early status assertions already eliminate the partner-creation failure? This requires runtime validation by an implementation-capable session.

## Implementation Update: 2026-10-26

### Actual Changes
- Modified `MorWalPizVideo.BackOffice.Tests/Features/CreatorPartnershipEndpointTests.cs`.
- Added immediate status assertions for campaign creation and timeline mutation before reading JSON properties.
- Changed the active-campaign collection test to track and assert its own Completed campaign ID, so it remains valid when the shared fixture contains unrelated Active campaigns.
- Kept endpoint routes, contracts, serializers, authorization, channel scope, persistence, and production code unchanged.

### Validation
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --verbosity minimal`: passed.
- Focused `CreatorPartnershipEndpointTests` execution was attempted with `dotnet test ... --filter "FullyQualifiedName~CreatorPartnershipEndpointTests" --verbosity minimal`, but the command exceeded 120 seconds before VSTest/testhost startup and emitted no test summary. Runtime pass/fail remains unverified; the runner diagnosis is owned by `backoffice-test-runner-timeout-handoff.md`.
- Editor diagnostics for this handoff: no errors.

### Residual Risk
The original CI upstream response remains unidentified because the supplied trace omitted HTTP status/body. A subsequent runtime test run is still required to establish whether the current production request handling needs a controller fix. The Calendar Mongo skips remain unrelated and unchanged.