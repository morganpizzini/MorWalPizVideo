---

description: "Task list for feature 002-pepperbox-clone"
---

# Tasks: Pepperbox-Style Shooting ITA Portal

**Input**: Design documents from [`/specs/002-pepperbox-clone/`](./)

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/videos-channel-api.md](contracts/videos-channel-api.md), [quickstart.md](quickstart.md)

**Tests**: Included per Constitution Principle V (test-backed behavior verification is mandatory for the new write endpoint, the backfill job, and non-trivial frontend logic).

> **ADAPT note (2025-11)**: User selected the ADAPT path because the codebase has no top-level `videos` Mongo collection — `Video` is a transient C# record only. Source-of-truth for video↔channel ownership is `YTChannel.Videos[]` (embedded `YouTubeVideo` records). Consequences:
>
> - **T006 (Video.ChannelId)**: still applied to `Video.cs` so `VideoDto` projections / SPA TS shape stay aligned, but no persisted `videos` rows exist to backfill — the field is wire-only.
> - **T007 (`videos.channelId` index)**: skipped (no `videos` collection to index).
> - **T010 (`VideoDto.ChannelId`)**: skipped (no `VideoDto` exists in the codebase; the SPA reads `Video` via match projections, which expose `channelId` once enriched).
> - **T011 (backfill job)**: skipped (no `videos` collection to iterate; ownership already lives in `YTChannel.Videos[]`).
> - **T012 (importer)**: skipped (WPF importer uses EF/SQLite, never writes the Mongo `Video` record).
> - **T020 (backfill SpecFlow tests)**: skipped (no backfill).
> - **T026 (new endpoint)**: redirected — `POST /api/videos/{youtubeId}/channel` mutates `YTChannel.Videos[]`: removes the videoId from any other channel's `Videos[]` and ensures it is present on the target channel's `Videos[]`. Idempotent. Evicts `channels` and `matches` cache tags.
> - **T018/T019 (endpoint SpecFlow tests)**: still applied, but assertions adjusted to the YTChannel-based behavior above.
>
> All frontend tasks (Phase 1, Phase 2 non-backend, Phase 3, Phase 4, Phase 5, Phase 6) are unaffected by the ADAPT decision.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks).
- **[Story]**: User story label (US1, US2, US3) — required for user-story phase tasks.
- File paths are repository-relative.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repo-level wiring so backend, contracts, and frontend pieces compile together. No story-specific work.

- [X] T001 [P] Install Vitest + React Testing Library devDeps in [frontend/shooting-ita-frontend/package.json](../../frontend/shooting-ita-frontend/package.json) (`vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `jsdom`) and add `"test": "vitest run"` to scripts.
- [X] T002 [P] Add the Vitest config + `setupTests.ts` (jsdom env, jest-dom matchers) at [frontend/shooting-ita-frontend/vite.config.ts](../../frontend/shooting-ita-frontend/vite.config.ts) and [frontend/shooting-ita-frontend/src/setupTests.ts](../../frontend/shooting-ita-frontend/src/setupTests.ts).
- [X] T003 [P] Add `VITE_API_BASE_URL` and `VITE_EXCLUSIVE_CATEGORY_ID` placeholders to [frontend/shooting-ita-frontend/.env](../../frontend/shooting-ita-frontend/.env) and document them in [frontend/shooting-ita-frontend/README.md](../../frontend/shooting-ita-frontend/README.md).
- [X] T004 [P] Add `VITE_MORWALPIZ_CHANNEL_ID` placeholder to [frontend/morwalpizvideo.client/.env](../../frontend/morwalpizvideo.client/.env) and document it in [frontend/morwalpizvideo.client/README.md](../../frontend/morwalpizvideo.client/README.md) (FR-017).
- [X] T005 Create [MorWalPiz.Contracts/Contracts/Videos/](../../MorWalPiz.Contracts/Contracts/Videos/) (folder placeholder for T009).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema field, backfill, shared TS shapes, channel-map helper, dark theme, router migration — every user story depends on these.

**CRITICAL**: No user-story work can start until Phase 2 is complete.

- [~] T006 Add the `ChannelId` field to the existing record at [MorWalPizVideo.Models/Models/Video.cs](../../MorWalPizVideo.Models/Models/Video.cs): `[BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;`. Update the existing constructor + `[JsonConstructor]` to accept an optional `channelId` parameter (defaulting to `""`) so existing call sites continue to compile and legacy documents continue to deserialize.
- [-] T007 Add an ascending index on `videos.channelId` in the existing MongoDB index-setup site under [MorWalPizVideo.BackOffice/](../../MorWalPizVideo.BackOffice/) (locate by searching for existing `CreateIndex` calls on the `videos` collection; add the new one next to them — concrete file path is pinned during T-impl by inspecting the existing pattern). _SKIPPED per ADAPT note: no `videos` collection exists._
- [X] T008 [P] Add `channelId?: string` to the existing `Video` TS interface at [frontend/fe-packages/models/src/video/types.ts](../../frontend/fe-packages/models/src/video/types.ts). No new files.
- [X] T009 [P] Add `VideoChannelAssignmentPayload.cs` (`ChannelId` string) to [MorWalPiz.Contracts/Contracts/Videos/VideoChannelAssignmentPayload.cs](../../MorWalPiz.Contracts/Contracts/Videos/VideoChannelAssignmentPayload.cs).
- [-] T010 [P] Add `ChannelId` to the existing `VideoDto` (or equivalent) in [MorWalPiz.Contracts/Contracts/](../../MorWalPiz.Contracts/Contracts/) — additive, optional in any constructor. _SKIPPED per ADAPT note: no `VideoDto` exists._
- [-] T011 Implement the one-time idempotent backfill job at [MorWalPizVideo.BackOffice/Jobs/BackfillVideoChannelIdJob.cs](../../MorWalPizVideo.BackOffice/Jobs/BackfillVideoChannelIdJob.cs): for every `Video` whose `ChannelId` is empty, look up the `YTChannel` whose embedded `Videos[].VideoId` contains the `Video.YoutubeId` and set `ChannelId` accordingly; leave empty when no match. Register as a Hangfire job runnable on demand from the admin UI / CLI. Goes through `IGenericDataService`, no direct `MongoDB.Driver` calls (Principle II). _SKIPPED per ADAPT note: no `videos` collection to backfill._
- [-] T012 Update the WPF importer at [MorWalPiz.VideoImporter/Services/](../../MorWalPiz.VideoImporter/Services/) (locate the `Video` write site by searching for `new Video(` constructors) so every newly-imported `Video` is created with a non-empty `ChannelId` sourced from the import context. _SKIPPED per ADAPT note: WPF importer uses EF/SQLite and never writes Mongo `Video` records._
- [X] T013 [P] Add the shared dark-theme stylesheet at [frontend/shooting-ita-frontend/src/styles/theme.scss](../../frontend/shooting-ita-frontend/src/styles/theme.scss) (dark background, white/orange accent on active sidebar item, large bold hero typography, rounded card thumbnails — FR-014).
- [X] T014 [P] Add the reduced-motion helper at [frontend/fe-packages/layout/src/utils/prefersReducedMotion.ts](../../frontend/fe-packages/layout/src/utils/prefersReducedMotion.ts) (reads `matchMedia('(prefers-reduced-motion: reduce)')`, subscribes to change).
- [X] T015 [P] Add the relative-time helper at [frontend/fe-packages/layout/src/utils/formatRelativeTime.ts](../../frontend/fe-packages/layout/src/utils/formatRelativeTime.ts) (`"2 hours ago"` style) and export both helpers from [frontend/fe-packages/layout/src/index.ts](../../frontend/fe-packages/layout/src/index.ts).
- [ ] T016 [P] Add the channel-map helper at [frontend/fe-packages/services/src/videoChannelMap.ts](../../frontend/fe-packages/services/src/videoChannelMap.ts) exporting:
  - `loadChannelMap(): Promise<Map<channelId, ChannelBadge>>` — wraps the existing channels endpoint.
  - `buildOwnerMap(matches, channels): Map<youtubeId, ChannelBadge>` — union of `Video.ChannelId` (when projected on the match's VideoRefs) and `YTChannel.Videos[].VideoId` (legacy fallback).
  - `resolveOwner(match, ownerMap): ChannelBadge | undefined` — picks the badge for the match's first `VideoRefs[0].YoutubeId`.

  Re-export from [frontend/fe-packages/services/src/index.ts](../../frontend/fe-packages/services/src/index.ts).
- [ ] T017 Migrate [frontend/shooting-ita-frontend/src/main.tsx](../../frontend/shooting-ita-frontend/src/main.tsx) from `<BrowserRouter><Routes>` to `createBrowserRouter` + `<RouterProvider>` with an empty placeholder router; route entries are added per story (R-008). Keep the existing `GoogleReCaptchaProvider` wrapper and Bootstrap import.

**Checkpoint**: Foundation ready — Stories US1, US2, US3 can now proceed in parallel.

---

## Phase 3: User Story 1 — Pepperbox-style home (Priority: P1) MVP

**Goal**: Visitor lands on `/`, sees the persistent dark Pepperbox sidebar + hero carousel of the 5 most-recent videos + at least one rail of cards, with every video resolving to an owning channel, and can click Play / a card to navigate to the detail route. Below the breakpoint the sidebar collapses behind a hamburger.

**Independent Test**: Quickstart "User Story 1" walkthrough (Acceptance Scenarios 1–7) — all must pass.

### Tests for User Story 1

- [ ] T018 [P] [US1] Create the SpecFlow feature [MorWalPizVideo.BackOffice.Tests/Features/VideoChannel/AssignChannel.feature](../../MorWalPizVideo.BackOffice.Tests/Features/VideoChannel/AssignChannel.feature) covering: `POST /api/videos/{id}/channel` returns 200 + updated DTO with new `channelId`; 400 when `channelId` is empty; 400 when `channelId` does not exist in `ytChannels`; 404 when the video does not exist; cache tags `videos` and `matches` are both evicted on success; the call is idempotent (assigning the same id twice is a no-op-by-effect). Require admin auth: missing JWT → 401; missing API key → 403.
- [ ] T019 [US1] Generate step definitions at [MorWalPizVideo.BackOffice.Tests/Features/VideoChannel/AssignChannelSteps.cs](../../MorWalPizVideo.BackOffice.Tests/Features/VideoChannel/AssignChannelSteps.cs) using the existing in-memory test harness used by `Videos.feature`.
- [ ] T020 [P] [US1] Create the SpecFlow feature [MorWalPizVideo.BackOffice.Tests/Features/VideoChannel/BackfillChannel.feature](../../MorWalPizVideo.BackOffice.Tests/Features/VideoChannel/BackfillChannel.feature) + steps file covering: backfill sets `Video.ChannelId` when an owning `YTChannel.Videos[]` membership exists; leaves `ChannelId` empty when none matches; second run is a no-op (idempotence); does not overwrite a non-empty `ChannelId`.
- [ ] T021 [P] [US1] Add the Vitest unit suite for the hero behavior at [frontend/shooting-ita-frontend/src/__tests__/HeroCarousel.test.tsx](../../frontend/shooting-ita-frontend/src/__tests__/HeroCarousel.test.tsx): auto-advance happens when `prefers-reduced-motion` is `no-preference`, is suppressed when `reduce`; manual prev/next always works (FR-005).
- [ ] T022 [P] [US1] Add the Vitest unit suite for card degradation at [frontend/shooting-ita-frontend/src/__tests__/VideoCard.test.tsx](../../frontend/shooting-ita-frontend/src/__tests__/VideoCard.test.tsx): renders with full metadata, with missing thumbnail (placeholder), with missing duration/publishedAt (hidden), with missing channel avatar (initials placeholder) — FR-011, FR-016.
- [ ] T023 [P] [US1] Add the Vitest unit suite for empty state at [frontend/shooting-ita-frontend/src/__tests__/EmptyState.test.tsx](../../frontend/shooting-ita-frontend/src/__tests__/EmptyState.test.tsx) (FR-010).
- [ ] T024 [P] [US1] Add the Vitest unit suite for the top-bar placeholder at [frontend/shooting-ita-frontend/src/__tests__/PepperboxTopBar.test.tsx](../../frontend/shooting-ita-frontend/src/__tests__/PepperboxTopBar.test.tsx): clicking Log In and Sign Up each shows a visible "coming soon" message and does NOT navigate (FR-012). Resolves analysis finding C7.
- [ ] T025 [P] [US1] Add the Vitest unit suite for the channel-map helper at [frontend/shooting-ita-frontend/src/__tests__/videoChannelMap.test.ts](../../frontend/shooting-ita-frontend/src/__tests__/videoChannelMap.test.ts): `buildOwnerMap` merges `Video.ChannelId` and `YTChannel.Videos[]`; `resolveOwner` returns undefined for unowned videos; the FR-016 filter drops unowned entries.

### Implementation for User Story 1

- [ ] T026 [US1] Add the new endpoint `POST /api/videos/{youtubeId}/channel` to the existing controller at [MorWalPizVideo.BackOffice/Controllers/VideosController.cs](../../MorWalPizVideo.BackOffice/Controllers/VideosController.cs) per [contracts/videos-channel-api.md](contracts/videos-channel-api.md). Use `IGenericDataService` for persistence (no `MongoDB.Driver` in the controller, Principle II). Apply the EXACT same authorization attribute set used by the existing admin write endpoint on the same controller — open `MatchesController` or `ProductsController` and copy the attribute list verbatim into this endpoint (typically `[Authorize]` + the API-key attribute + a rate-limit attribute; pin the exact names while editing). On success, call `EvictByTagAsync(CacheKeys.Videos)` AND `EvictByTagAsync(CacheKeys.Matches)`. Return 400 if the supplied `channelId` is not present in `ytChannels`. Resolves analysis finding C9.
- [ ] T027 [P] [US1] Create the empty-state component at [frontend/fe-packages/layout/src/components/EmptyState.tsx](../../frontend/fe-packages/layout/src/components/EmptyState.tsx) (props: `title`, `message`).
- [ ] T028 [P] [US1] Create the video card component at [frontend/fe-packages/layout/src/components/VideoCard.tsx](../../frontend/fe-packages/layout/src/components/VideoCard.tsx) (props: `youtubeId`, `title`, `thumbnailUrl?`, `duration?`, `channel: { channelName: string; avatarUrl?: string }`, `publishedAt?: string`, `onClick`). Renders placeholder thumbnail / initials avatar / hides missing metadata per FR-011.
- [ ] T029 [P] [US1] Create the horizontal rail at [frontend/fe-packages/layout/src/components/VideoCardRail.tsx](../../frontend/fe-packages/layout/src/components/VideoCardRail.tsx) (props: `title`, `items: VideoCardProps[]`). Renders `EmptyState` when items is empty.
- [ ] T030 [P] [US1] Create the hero carousel at [frontend/fe-packages/layout/src/components/HeroCarousel.tsx](../../frontend/fe-packages/layout/src/components/HeroCarousel.tsx) (props: `slides` with artwork/title/channel/playTarget, max 5). Auto-advance every N seconds gated by `prefersReducedMotion()` (T014). Manual prev/next + pagination dots.
- [ ] T031 [P] [US1] Create the Pepperbox sidebar at [frontend/fe-packages/layout/src/components/PepperboxSidebar.tsx](../../frontend/fe-packages/layout/src/components/PepperboxSidebar.tsx) (props: `brand`, `primaryNav`, `discoverNav`, `helpHref`, `footerLinks`, `activePath`). Implements FR-001/FR-002 (highlight active) and FR-003 (hamburger drawer below the breakpoint via `react-bootstrap` Offcanvas).
- [ ] T032 [P] [US1] Create the top bar at [frontend/fe-packages/layout/src/components/PepperboxTopBar.tsx](../../frontend/fe-packages/layout/src/components/PepperboxTopBar.tsx) with the visual-only "Log In" / "Sign Up" placeholders showing a brief "coming soon" message on click (FR-012).
- [ ] T033 [US1] Extract the existing single-video player from [frontend/morwalpizvideo.client/](../../frontend/morwalpizvideo.client/) into [frontend/fe-packages/layout/src/components/VideoPlayer.tsx](../../frontend/fe-packages/layout/src/components/VideoPlayer.tsx) and update `morwalpizvideo.client` to consume it (no behavior change there). Resolves analysis finding C8. Depends on identifying the current player file (search `morwalpizvideo.client/src` for `youtube`/`iframe`/`react-player`).
- [ ] T034 [US1] Export all new layout components and `VideoPlayer` from [frontend/fe-packages/layout/src/index.ts](../../frontend/fe-packages/layout/src/index.ts). Depends on T027–T033.
- [ ] T035 [US1] Create the root shell route at [frontend/shooting-ita-frontend/src/routes/root.tsx](../../frontend/shooting-ita-frontend/src/routes/root.tsx) composing `PepperboxSidebar` + `PepperboxTopBar` + `<Outlet />`, importing `theme.scss` (T013). Highlight uses `useLocation()` for `activePath`. Depends on T031, T032, T034, T017.
- [ ] T036 [US1] Add the FR-016 composition wrapper at [frontend/shooting-ita-frontend/src/services/shootingItaVideoService.ts](../../frontend/shooting-ita-frontend/src/services/shootingItaVideoService.ts) exposing `loadMatchesWithChannels()` that fetches matches + channels in parallel via the shared `videoChannelMap` helper (T016), applies the FR-016 filter (drop matches whose first `VideoRefs[0].YoutubeId` has no owning channel), and returns `{ matches: MatchWithChannel[], ownerMap }`. No filter for MorWalPiz channel here — that filter lives in morwalpizvideo.client per FR-017.
- [ ] T037 [US1] Add the pure-derivation helper at [frontend/shooting-ita-frontend/src/utils/deriveCategories.ts](../../frontend/shooting-ita-frontend/src/utils/deriveCategories.ts) exporting `deriveLatest(matches)`, `deriveExclusives(matches, exclusiveCategoryId)`, `derivePopular(matches, videoViewsById)`, `deriveFeatured(matches, count = 5)`. Sort key is ALWAYS `max(VideoRefs[].publishedAt)` (resolves analysis finding C6). `deriveExclusives` MUST return `[]` when `exclusiveCategoryId` is empty so every consumer renders the empty state consistently (resolves analysis finding C2). `derivePopular` MUST sort by `sum(videoViewsById[VideoRefs[].youtubeId])` (resolves analysis finding C3). Add [frontend/shooting-ita-frontend/src/__tests__/deriveCategories.test.ts](../../frontend/shooting-ita-frontend/src/__tests__/deriveCategories.test.ts) covering each derivation, the FR-016 filter, the empty-exclusiveCategoryId rule, and the views-missing-as-zero fallback.
- [ ] T038 [US1] Create the home loader at [frontend/shooting-ita-frontend/src/routes/home/loader.ts](../../frontend/shooting-ita-frontend/src/routes/home/loader.ts) that calls `loadMatchesWithChannels()` (T036), resolves view counts via `/api/videos` (existing endpoint) into a `Map<youtubeId, views>`, and derives: `featured = deriveFeatured(matches)`; `exclusiveRail = deriveExclusives(matches, import.meta.env.VITE_EXCLUSIVE_CATEGORY_ID)`. Returns `{ featured, exclusiveRail, ownerMap }`.
- [ ] T039 [US1] Create the home component at [frontend/shooting-ita-frontend/src/routes/home/Component.tsx](../../frontend/shooting-ita-frontend/src/routes/home/Component.tsx) using `useLoaderData` + `HeroCarousel` + `VideoCardRail`. Clicking Play / a card navigates via `useNavigate` to `/video/{youtubeId}` (FR-008). Empty `exclusiveRail` MUST render `EmptyState` (FR-010 + the C2 consistency rule). Depends on T030, T029, T038.
- [ ] T040 [US1] Create the video detail route at [frontend/shooting-ita-frontend/src/routes/video/Component.tsx](../../frontend/shooting-ita-frontend/src/routes/video/Component.tsx) using the shared `VideoPlayer` from `@morwalpizvideo/layout` (T033). Resolves the videoId from the URL param.
- [ ] T041 [US1] Wire the `home` and `video` routes into the data-router config in [frontend/shooting-ita-frontend/src/main.tsx](../../frontend/shooting-ita-frontend/src/main.tsx) (root → home as index, plus `/video/:youtubeId`). Depends on T035, T039, T040.

**Checkpoint**: US1 is fully functional. Home + video routes are live; sidebar collapses on mobile; hero respects reduced motion; cards degrade; empty state shows consistently when shooters/channels are unassigned or the exclusive env var is unset.

---

## Phase 4: User Story 2 — Discover category pages (Priority: P2)

**Goal**: Each Discover entry (Latest Videos / Exclusives / Popular Now) opens a page with a themed banner and the vertical "large thumbnail + title/channel/description/time" row layout. Active sidebar item is highlighted.

**Independent Test**: Quickstart "User Story 2" walkthrough — three category pages render correctly with derived content and active highlighting; the env-unset Exclusives case shows the empty state on both home rail and Exclusives page.

### Tests for User Story 2

- [ ] T042 [P] [US2] Add Vitest test [frontend/shooting-ita-frontend/src/__tests__/CategoryBanner.test.tsx](../../frontend/shooting-ita-frontend/src/__tests__/CategoryBanner.test.tsx) verifying the banner renders with the supplied title + artwork URL and applies the themed class.
- [ ] T043 [P] [US2] Add Vitest test [frontend/shooting-ita-frontend/src/__tests__/CategoryVideoRow.test.tsx](../../frontend/shooting-ita-frontend/src/__tests__/CategoryVideoRow.test.tsx) verifying the large-thumb-left layout renders title, channel, description, and publish time, and falls back gracefully on missing description (FR-007, FR-011).

### Implementation for User Story 2

- [ ] T044 [P] [US2] Create the banner component at [frontend/fe-packages/layout/src/components/CategoryBanner.tsx](../../frontend/fe-packages/layout/src/components/CategoryBanner.tsx) (props: `title`, `artworkUrl`).
- [ ] T045 [P] [US2] Create the row component at [frontend/fe-packages/layout/src/components/CategoryVideoRow.tsx](../../frontend/fe-packages/layout/src/components/CategoryVideoRow.tsx) (props: `youtubeId`, `title`, `thumbnailUrl?`, `duration?`, `channel`, `description?`, `publishedAt?`, `onClick`).
- [ ] T046 [US2] Export `CategoryBanner` and `CategoryVideoRow` from [frontend/fe-packages/layout/src/index.ts](../../frontend/fe-packages/layout/src/index.ts). Depends on T044, T045.
- [ ] T047 [P] [US2] Create the Latest route: [frontend/shooting-ita-frontend/src/routes/latest/loader.ts](../../frontend/shooting-ita-frontend/src/routes/latest/loader.ts) calls `loadMatchesWithChannels()` + `deriveLatest`; [frontend/shooting-ita-frontend/src/routes/latest/Component.tsx](../../frontend/shooting-ita-frontend/src/routes/latest/Component.tsx) renders `CategoryBanner` ("Latest Videos") + a vertical list of `CategoryVideoRow`. Empty list → `EmptyState`. Depends on T036, T046.
- [ ] T048 [P] [US2] Create the Exclusives route at [frontend/shooting-ita-frontend/src/routes/exclusives/](../../frontend/shooting-ita-frontend/src/routes/exclusives/) using `deriveExclusives(matches, import.meta.env.VITE_EXCLUSIVE_CATEGORY_ID)`. The env-unset case is handled by `deriveExclusives` returning `[]` (T037) so the page just renders `EmptyState` — same behavior as the home rail (C2 consistency). Depends on T036, T046.
- [ ] T049 [P] [US2] Create the Popular route at [frontend/shooting-ita-frontend/src/routes/popular/](../../frontend/shooting-ita-frontend/src/routes/popular/) using `derivePopular(matches, viewsMap)`. Depends on T036, T046.
- [ ] T050 [US2] Register `/latest`, `/exclusives`, `/popular` in the data-router config in [frontend/shooting-ita-frontend/src/main.tsx](../../frontend/shooting-ita-frontend/src/main.tsx) under the root shell. Wire the sidebar Discover entries (props supplied by T035) to those paths. Depends on T047, T048, T049, T035.

**Checkpoint**: US2 is fully functional. Three Discover pages render banner + rows with derived content; active sidebar item is highlighted on each; env-unset Exclusives is consistent with the home rail.

---

## Phase 5: User Story 3 — Verification report (Priority: P1)

**Goal**: Produce the written verification report mapping every layout element listed in FR-001/FR-004/FR-006/FR-007 and every acceptance scenario from US1 + US2 (including the FR-017 morwalpizvideo.client filter check) to Present / Partial / Missing in the shipped build (FR-013, SC-002).

**Independent Test**: The report exists, every row from the Quickstart walkthrough is filled in (Pass/Fail + note), and every layout element appears in the element table.

### Implementation for User Story 3

- [ ] T051 [US3] Run the Quickstart walkthrough in [quickstart.md](quickstart.md) end-to-end against the local build produced by US1 + US2 (covers shooting-ita AND morwalpizvideo.client for FR-017).
- [ ] T052 [US3] Author [specs/002-pepperbox-clone/verification-report.md](verification-report.md) with: (1) per-scenario Pass/Fail table for every Acceptance Scenario in US1 + US2 + the FR-017 check; (2) Present / Partial / Missing table covering every element enumerated in FR-001, FR-004, FR-006, FR-007; (3) "Gaps" section phrasing every Partial/Missing entry as a concrete user-visible deficiency (not an implementation task) per US3 Acceptance Scenario 3.
- [ ] T053 [US3] For every Partial / Missing row in the report, open a follow-up entry in [memory-bank/activeContext.md](../../memory-bank/activeContext.md) so the next Spec Kit run starts from accurate context (Constitution §5). Do NOT modify code in this task — fixes belong to a follow-up feature.

**Checkpoint**: US3 is fully functional. The verification report is the artifact that closes the feature.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T054 Delete the legacy placeholder route(s) that the data-router migration replaces under [frontend/shooting-ita-frontend/src/pages/](../../frontend/shooting-ita-frontend/src/pages/) (and any other dead `<Routes>`-era files) after T041 / T050 ship. Resolves analysis finding C5 (SC-003 "zero pages still rely on hard-coded placeholder items").
- [ ] T055 [P] Run `npm run build` in [frontend/fe-packages/models](../../frontend/fe-packages/models), [frontend/fe-packages/services](../../frontend/fe-packages/services), [frontend/fe-packages/layout](../../frontend/fe-packages/layout), [frontend/shooting-ita-frontend](../../frontend/shooting-ita-frontend), and [frontend/morwalpizvideo.client](../../frontend/morwalpizvideo.client) — all five must succeed (PR gate, Constitution §4).
- [ ] T056 [P] Run `dotnet build` at the repo root and `dotnet test --filter FullyQualifiedName~VideoChannel` in [MorWalPizVideo.BackOffice.Tests](../../MorWalPizVideo.BackOffice.Tests/) — both must succeed (PR gate).
- [ ] T057 [P] Verify the existing `shooting-ita-frontend` and `morwalpizvideo.client` Docker image builds still succeed against the new sources (Constitution VII).
- [ ] T058 Append a short "Pepperbox-style portal shipped" entry to [memory-bank/progress.md](../../memory-bank/progress.md) and refresh [memory-bank/activeContext.md](../../memory-bank/activeContext.md) (Constitution §5).
- [ ] T059 Re-run [quickstart.md](quickstart.md) one final time on the freshly built containers to confirm SC-001..SC-006 hold AND the morwalpizvideo.client FR-017 filter still excludes non-MorWalPiz videos.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: starts immediately.
- **Foundational (Phase 2)**: depends on Phase 1 — BLOCKS US1/US2/US3.
- **User Story 1 (Phase 3)**: depends on Phase 2. MVP.
- **User Story 2 (Phase 4)**: depends on Phase 2 (reuses T036/T037 from US1 — sequence US1 → US2 if single developer).
- **User Story 3 (Phase 5)**: depends on US1 + US2 being deployable.
- **Polish (Phase 6)**: depends on US1, US2, US3 complete.

### Cross-story shared work

- T036 (`shootingItaVideoService`) and T037 (`deriveCategories`) are consumed by both US1 and US2. Build them once in US1.
- T034 + T046 are barrel-file exports from `@morwalpizvideo/layout`; touching the same file → must be sequenced (T046 runs after T034).
- T017 (router migration) and T035 (root shell) are prerequisites for T041 (US1) and T050 (US2).
- T006 (Video.ChannelId) and T011 (backfill) MUST land before T036 because the channel-map join relies on the field — but the helper also has a fallback to `YTChannel.Videos[]` so the order is enforced by tests, not by a runtime crash.

### Within each user story

- Models / DTOs / TS shapes → services → components → routes → router wiring.
- Tests are written alongside implementation (not strict TDD), but the SpecFlow features T018+T020 MUST exist before T026 / T011 are merged.

### Parallel opportunities

- **Phase 1**: T001, T002, T003, T004 — different files, all `[P]`.
- **Phase 2**: T008–T010, T013–T016 — different files, all `[P]`. T006 → T007 → T011 are sequential (same MongoDB layer and depend on the field existing).
- **US1 tests**: T021–T025 — five different test files → parallel. T018 + T020 (backend, different feature files) → also parallel.
- **US1 implementation**: T027, T028, T029, T030, T031, T032 — independent files → parallel. T033 (player extraction) is independent. T034 sequences after them.
- **US2 implementation**: T044, T045, T047, T048, T049 — different files → parallel after T046.
- **Polish**: T055, T056, T057 — different runtimes → parallel.

### Parallel example — US1 component fan-out

After Phase 2 completes:

```
T027 EmptyState.tsx
T028 VideoCard.tsx
T029 VideoCardRail.tsx
T030 HeroCarousel.tsx
T031 PepperboxSidebar.tsx
T032 PepperboxTopBar.tsx
T033 VideoPlayer.tsx (extraction)
```

All seven touch independent files. Merge T034 once they land, then T035 → T036 → T037 → T038 → T039 → T040 → T041.

---

## Implementation Strategy

1. **MVP scope = Phase 1 + Phase 2 + Phase 3 (US1)**. Ships the Pepperbox home page end-to-end with `Video.ChannelId`, the backfill, the hero, rail, sidebar, top-bar, dark theme, FR-016 filter, reduced-motion behavior, and the shared `VideoPlayer`. Independently demoable. Satisfies SC-001, SC-003 (home rail), SC-004 (home), SC-005 (home navigation), SC-006 (empty state). FR-017 is also satisfied (morwalpizvideo.client filter is part of Phase 2/Phase 3 since the shared helper is consumed by both SPAs).
2. **Increment 2 = Phase 4 (US2)**. Adds the three Discover category pages reusing Phase 3 components and `shootingItaVideoService`. Satisfies SC-003 / SC-004 / SC-005 / SC-006 for the category surfaces.
3. **Increment 3 = Phase 5 (US3)**. Produces the verification report; closes FR-013 and SC-002.
4. **Hardening = Phase 6**. Legacy placeholder cleanup (C5), CI gates green, memory-bank refreshed, final Quickstart pass on containers.
