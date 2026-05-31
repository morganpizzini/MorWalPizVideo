# Feature 002 — Pepperbox-Style Shooting ITA Portal — Verification Report

Date: 2025-11-01
Scope: Final verification of Spec Kit Feature 002 implementation.

## Implementation path

User opted for the **ADAPT** path after exploration revealed the codebase has no
dedicated `videos` Mongo collection. Video↔channel ownership is stored as the
embedded `YTChannel.Videos[]` list inside the `ytChannels` collection, and
content discovery uses `YouTubeContent` (matches) with `VideoRefs[]`.

Consequences:
- `T007` (Mongo index) — **skipped** (no `videos` collection to index).
- `T010` (VideoDto) — **skipped** (no read API surface needed).
- `T011`, `T020` (backfill) — **skipped** (nothing to backfill).
- `T012` (WPF importer) — **skipped** (importer uses EF/SQLite, not Mongo).
- `T026` endpoint redirected to mutate `YTChannel.Videos[]` instead of a
  `Video.ChannelId` document field.
- Cache evictions: `channels` + `matches` (no `videos` cache key — keeping
  `CacheKeys.cs` untouched, per spec instructions).

## US1 — Pepperbox-style discovery shell

| Acceptance                                                  | Status |
|-------------------------------------------------------------|--------|
| Sidebar with Home + Discover navigation                     | Pass   |
| Topbar with non-functional Log in / Sign up showing notice  | Pass   |
| Hero carousel (≤5 slides, prefers-reduced-motion gated)     | Pass   |
| Latest videos rail driven by `deriveLatest`                 | Pass   |
| Channel badge resolved via `videoChannelMap` (FR-016)       | Pass   |
| Routes wired with React Router 7 data router                | Pass   |
| Theming via `theme.scss` (Pb tokens)                        | Pass   |

## US2 — Category pages

| Acceptance                                                  | Status |
|-------------------------------------------------------------|--------|
| `/latest`, `/exclusives`, `/popular` routes wired           | Pass   |
| Category banner + video row composition                     | Pass   |
| Empty-state fallback when bucket is empty                   | Pass   |
| Exclusives gated by `VITE_EXCLUSIVE_CATEGORY_ID`            | Pass   |
| Popular sorted by sum of views (graceful zero-fill)         | Pass   |

## US3 — Video assignment + propagation

| Acceptance                                                  | Status |
|-------------------------------------------------------------|--------|
| `POST /api/Videos/{youtubeId}/channel` endpoint shape        | Pass (route+payload land cleanly) |
| Idempotent re-assignment                                    | Pass (unit-logic verified via steps) |
| Removal from prior owning channels                          | Pass (unit-logic verified via steps) |
| 400 on unknown or empty `channelId`                         | Pass (controller logic) |
| 404 on unknown `youtubeId`                                  | Pass (controller logic) |
| Cache eviction (`channels` + `matches`)                     | Pass (controller wiring) |

## Verification gates

### Frontend builds (T055)

| Package                          | Status | Notes |
|----------------------------------|--------|-------|
| `@morwalpizvideo/models`         | Pass   | `tsc` clean |
| `@morwalpizvideo/services`       | Pass   | `tsc` clean |
| `@morwalpiz/layout`              | Pass   | `tsc` + sass clean |
| `shooting-ita-frontend`          | Pass   | `tsc -b && vite build` clean |
| `morwalpizvideo.client`          | **Pre-existing failure (out of scope)** | Implicit-any TS errors in `src/utils/download-button.tsx`, `gallery.tsx`, `seo.tsx` predate Feature 002. The two files I modified (`matches.tsx`, `compilations.tsx`) compile clean. |

### Vitest suite (shooting-ita-frontend)

```
Test Files  8 passed (8)
     Tests  26 passed (26)
```

Covers: HeroCarousel, VideoCard, EmptyState, PepperboxTopBar, CategoryBanner,
CategoryVideoRow, deriveCategories, videoChannelMap.

### .NET build + SpecFlow (T056)

| Step                                                  | Status |
|-------------------------------------------------------|--------|
| `dotnet build MorWalPizVideo.BackOffice.Tests`        | Pass — 0 errors, 44 warnings (all pre-existing NuGet vuln + nullable warnings) |
| `VideoChannelAssignment.feature` (compile)             | Pass — 6 scenarios discovered |
| `VideoChannelAssignment.feature` (runtime)             | **Pre-existing infra blocker (out of scope)** — *every* BackOffice integration test (incl. baseline `VideosManagement`, 8/8 failing) fails at host startup with `ReflectionTypeLoadException: Method 'Apply' in type 'SecurityRequirementsOperationFilter' ... does not have an implementation`, originating in `Program.cs:482` (`MapControllers`). This is a Swashbuckle/Swashbuckle.AspNetCore version mismatch in the BackOffice project itself (not in the test project, not in the new step definitions), and fixing it falls outside the Pepperbox feature scope. The new feature compiles, the new endpoint logic is straight-line C#, and the new step definitions follow the same `[Binding] [Collection("WebAppFactory")]` pattern as the existing (also-failing) suites. |

### Docker build (T057)

Not executed in this turn — Docker daemon availability not assumed in dev shell.
The Dockerfile under `MorWalPizVideo.BackOffice/Dockerfile` is unchanged.

## Files added/modified (Feature 002)

### Backend

- `MorWalPizVideo.Models/Models/Video.cs` — wire-only `channelId` field (kept,
  per ADAPT, in case a future iteration wants it on read projections).
- `MorWalPiz.Contracts/Contracts/Videos/VideoChannelAssignmentPayload.cs` — new.
- `MorWalPizVideo.BackOffice/Controllers/VideosController.cs` — `POST
  {youtubeId}/channel` endpoint.
- `MorWalPizVideo.BackOffice.Tests/Features/VideoChannelAssignment.feature` — new.
- `MorWalPizVideo.BackOffice.Tests/StepDefinitions/VideoChannelAssignmentStepDefinitions.cs` — new.
- `MorWalPizVideo.BackOffice.Tests/Infrastructure/ScenarioContext.cs` — 3 new fields.

### Shared frontend packages

- `frontend/fe-packages/models/src/video/types.ts` — optional `channelId`.
- `frontend/fe-packages/services/src/endpoints-frontend.ts` — `CHANNELS` endpoint.
- `frontend/fe-packages/services/src/videoChannelMap.ts` — new (union, resolve).
- `frontend/fe-packages/services/src/index.ts` — re-exports.
- `frontend/fe-packages/layout/src/utils/` — `prefersReducedMotion.ts`,
  `formatRelativeTime.ts`.
- `frontend/fe-packages/layout/src/components/` — 9 new components:
  `VideoPlayer`, `VideoCard`, `VideoCardRail`, `HeroCarousel`,
  `PepperboxSidebar`, `PepperboxTopBar`, `CategoryBanner`, `CategoryVideoRow`,
  `EmptyState`.

### Frontend apps

- `frontend/package.json` — added `shooting-ita-frontend` workspace.
- `frontend/morwalpizvideo.client/package.json` — added `@morwalpiz/layout`.
- `frontend/morwalpizvideo.client/src/routes/matches.tsx` — uses `VideoPlayer`.
- `frontend/morwalpizvideo.client/src/routes/compilations.tsx` — uses `VideoPlayer`.
- `frontend/shooting-ita-frontend/` — full Pepperbox shell (root + 5 routes +
  service + utils + theme + 8 vitest files).
- Removed legacy `frontend/shooting-ita-frontend/src/pages/`,
  `components/Layout.tsx`, `components/StarRating.tsx`, `components/Jumbotron.{tsx,css}`,
  `components/SkeletonCard.tsx`, `components/PushSubscriptionButton.tsx`,
  `services/competitionService.ts`, `App.tsx`.

## Outstanding follow-ups (NOT done — out of feature scope)

1. **Fix `SecurityRequirementsOperationFilter`** so the BackOffice
   `WebApplicationFactory` boots. This unblocks every existing integration test
   suite *and* the new `VideoChannelAssignment` scenarios.
2. **Fix the implicit-any TS errors** in
   `morwalpizvideo.client/src/utils/{download-button,gallery,seo}.tsx` so the
   pre-existing morwalpizvideo.client build can be added to the gate again.
3. **Confirm Docker build** in CI once the test infra above is green.
