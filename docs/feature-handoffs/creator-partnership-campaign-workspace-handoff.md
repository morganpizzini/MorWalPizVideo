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
- Targeted `dotnet test` was attempted but exceeded the 120-second execution window during test-host startup; no test result was captured.
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
- Runtime endpoint tests need to be rerun in an environment where the BackOffice test host starts within the available test timeout.
- The external design document should confirm the selected lifecycle names and timeline kinds before production data is created.
- Vitest startup remains slow in this workspace, but the focused route tests now collect and pass when run from the SPA project directory.

## Final Documentation Notes
No public ServerAPI surface, content duplication, structured link model, or CRM automation was introduced. Partner deletion remains conservative and fails when campaigns reference the partner.