# Calendar Compatibility And Revisions

## Scope

Accepted roadmap slices 5 and 6 cover Calendar only. Both APIs use `ICalendarService`; BackOffice accepts `SaveCalendarEventRequest` and returns the existing admin contract with an additive `revision`. ServerAPI returns `PublicCalendarEventResponse` with exactly the previous entity JSON fields, including `creationDateTime`, `oldEvent`, `channelId`, and enriched `matchUrl`. Public JSON does not expose revision. Routes, permissions, channel requirements, and existing cache behavior are unchanged.

The admin URL retains the title in the router's `:id` parameter. Loaders consume that already-decoded title and encode it once for `by-title`. Edits PUT to the persisted document ID, not the title. Route/body ID disagreement is rejected, server channel context overrides body channel, and updates preserve creation time. Categories retain embedded IDs/titles, including references absent from the current category list. Duplicate creation remains globally case-insensitive; duplicate-title updates retain their existing behavior.

## Concurrency And Compatibility

Only Calendar adds persisted BSON `revision`. Missing legacy revision means baseline `0`; new events begin at `1`. Mongo replacement/deletion atomically filter by document ID, channel, and expected revision. The baseline filter accepts a missing revision field. Mock mutations use the same condition under the shared scenario lock, and scenario cloning retains revision despite its public JSON exclusion.

Revision-aware updates increment revision; stale updates/deletes return HTTP 409. Invalid revisions return 400. The admin sends revision for edits and deletes, retains the unsaved draft on conflict, and permits an explicit persisted-ID reload that discards the draft and refreshes revision. Failed reload leaves the draft and conflict lock intact.

Supported legacy callers may omit expected revision. `DataService` Calendar methods and Calendar's generic repository interface mutations delegate to the feature service; successful updates advance the stored revision and generic creation starts at `1`. Such callers lack protection against edits made since they opened a document, though a competing write during their server-side read/write interval still fails CAS. Direct database writers and explicit casts to base repositories are outside this supported mutation path and must not be used during rollout.

No rewrite/backfill is required for existing documents. Coordinate compatible API-first/admin-second delivery. A rollback to an old writer that does not advance revision invalidates concurrency guarantees: pause Calendar mutations rather than mixing old writers with revision-aware clients.

## Focused Verification

Run from the repository root:

```powershell
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter "FullyQualifiedName~Calendar|FullyQualifiedName~FocusedServiceDependencyTests" --verbosity minimal
yarn --cwd frontend/back-office-spa test src/routes/calendarEvents/routes.test.ts src/routes/calendarEvents/Component.test.tsx
yarn --cwd frontend/back-office-spa test src/router/routes/index.test.ts
yarn --cwd frontend/back-office-spa test src/routes/successEffects.test.tsx -t calendarEvents
yarn --cwd frontend/back-office-spa tsc --project tsconfig.calendar.json --noEmit
yarn --cwd frontend/back-office-spa eslint src/routes/calendarEvents
yarn --cwd frontend/back-office-spa eslint src/router/routes/index.test.ts
yarn --cwd frontend workspace @morwalpiz/layout build
yarn --cwd frontend/morwalpizvideo.client tsc --project tsconfig.app.json --noEmit
yarn --cwd frontend/back-office-spa tsc --project tsconfig.app.json --noEmit
```

The existing admin `pretest` rebuilds models then services. Route fixtures mock built service modules instead of pulling legacy service sources into the admin compiler. The Calendar config inherits all app strict/no-unused checks and includes the existing DOM matcher setup; it does not replace the application-wide build gate.

Backend fixtures cover wire/BSON snapshots, legacy JSON inputs, identity/channel/permissions/validation, duplicate behavior, stale saves/deletes, generic legacy writers, compatibility delegates, and focused dependencies. Admin fixtures cover loaders/actions, persisted-ID writes, category/date mapping, server errors, pending forms, draft retention, reload failure/success, and conflict deletion recovery.

### Recovery Results (2026-10-01)

- Backend filter: 20 discovered, 18 passed, 0 failed, 2 Mongo skips. Both owning APIs and dependencies compiled through the test project. The isolated serialization check passed all 5 tests; compilation surfaced an unrelated existing CS8602 warning in `PageNavigationControllerTests.cs:79`.
- Calendar admin route/form suites: 19 passed, 0 failed, 0 skipped. Existing router suite: 5 passed, including actual Calendar lazy registration and special-title matching. Existing success fixture filtered to Calendar: 2 passed, 0 failed, 26 non-Calendar tests intentionally filtered out.
- Shared models/services rebuilt successfully by `pretest`; layout build passed. Strict Calendar and public-client checks passed. Calendar and router-fixture lint passed with 0 errors/warnings. Editor diagnostics and scoped `git diff --check` passed. The Impeccable detector reported no findings on the four Calendar views; it is not browser evidence.
- Full admin strict check remains failed: 8 errors outside Calendar across `components/ToastNotification/index.test.tsx` (callback return type), `routes/products/index/Component.behavior.test.tsx` (missing required product creation date), `routes/sponsors/edit/index.ts` (duplicate Component exports), and `routes/sponsors/form/action.test.ts` (incomplete router arguments and mock typing). No unrelated files were repaired or compiler rules relaxed. This application-wide gate still needs separate resolution before release.

### Source Ownership

- Contracts: `MorWalPiz.Contracts/Contracts/CalendarRequests.cs`, `Contracts/CalendarEventContract.cs`, and `ContractUtils.cs` own explicit inputs, admin revision, and public conversion.
- Domain/model: `MorWalPizVideo.Domain/CalendarService.cs`, `DataService.cs`, `Interfaces/IRepository.cs`, `Interfaces/Repository.cs`, `Interfaces/MockRepository.cs`, `Scenarios/BaseScenario.cs`, and `MorWalPizVideo.Models/Models/CalendarEvent.cs` own focused orchestration, compatibility dispatch, Calendar-only CAS, clone behavior, and BSON baseline.
- Hosts: `MorWalPizVideo.BackOffice/Controllers/CalendarEventsController.cs`, `MorWalPizVideo.ServerAPI/Controllers/CalendarEventsController.cs`, and each host's `Program.cs` own DTO/channel boundaries and focused-service registration. Preserve unrelated host/security changes already in the worktree.
- Shared frontend: `frontend/fe-packages/models/src/CalendarEvent.ts` and `src/index.ts`; `frontend/fe-packages/services/src/calendarService.ts`, `src/endpoints.ts`, and `src/index.ts` own additive types/exports and compatible API paths.
- Admin Calendar: `frontend/back-office-spa/src/routes/calendarEvents/form.ts`; `create/Component.tsx` and `create/action.ts`; `edit/Component.tsx`, `edit/loader.ts`, and `edit/action.ts`; `detail/Component.tsx`, `detail/loader.ts`, and `detail/action.ts`; `index/Component.tsx` and `index/action.ts` own payloads, identity/category mapping, revision submission, and conflict recovery.
- Verification: `MorWalPizVideo.BackOffice.Tests/Features/CalendarControllerTests.cs`, `CalendarSerializationTests.cs`, `CalendarRevisionTests.cs`, `CalendarMongoTests.cs`, `PublicCalendarControllerTests.cs`, and `FocusedServiceDependencyTests.cs`; `frontend/back-office-spa/src/routes/calendarEvents/routes.test.ts`, `Component.test.tsx`, `src/router/routes/index.test.ts`, and `tsconfig.calendar.json` own focused gates. The existing `src/routes/successEffects.test.tsx` was run with a Calendar filter, not edited during recovery.

## Release Evidence Still Required

- Run both `CalendarMongoTests` against an explicitly disposable server using `CALENDAR_TEST_MONGO_URI`. They create unique databases and drop them afterward; never point this setting at production. Independent-client races and missing-revision BSON need actual Mongo evidence, not mock inference. An absent setting produces two explicit skips, not passes.
- Verify create/edit/detail/delete and stale conflict recovery in a real browser at desktop/mobile sizes, including keyboard/focus and direct navigation. jsdom tests and strict TypeScript do not certify browser layout or cookie/CSRF behavior.
- Retain normal coordinated-release build/security gates. Missing Mongo/browser evidence blocks release certification, not scoped Calendar authoring. No production deployment is approved here.

## Remaining Decomposition

BackOffice still has direct `DataService` consumers, including Categories, Channels, Configuration, ProductCategories, Products, PublishSchedule, QueryLinks, SponsorApplies, Sponsors, User, and YouTubeVideoLinks controllers. These are separate feature-sized migrations, not part of Calendar completion. Preserve their contracts and current behavior; shop remains frozen, cache reliability deferred, and ShootingRange security work independent. The existing catalog recent-Calendar read remains compatible for other consumers; it was not removed as part of this extraction.