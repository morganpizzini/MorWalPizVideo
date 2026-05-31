# Implementation Plan: Pepperbox-Style Shooting ITA Portal

**Branch**: `002-pepperbox-clone` | **Date**: 2026-05-31 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from [`/specs/002-pepperbox-clone/spec.md`](spec.md)

## Summary

Rework `frontend/shooting-ita-frontend` so its layout, presentation and content match Pepperbox TV: dark theme with a persistent left sidebar (brand, primary nav, Discover group, footer links), a hero carousel of the 5 most-recent videos, themed rails on the home page, and Discover category pages (Latest / Exclusives / Popular) with the banner + vertical-list layout. Content is sourced from the existing MorWalPiz video APIs already consumed by `morwalpizvideo.client`; Discover categories are derived client-side.

The only new backend surface is a single `ChannelId` field on the existing `Video` document, an admin-only `POST /api/videos/{youtubeId}/channel` write endpoint, and a one-time idempotent backfill that populates `ChannelId` on legacy rows from the existing `YTChannel.Videos[]` membership. The card/hero badge reuses the existing `/api/channels` endpoint and `YTChannel` collection — no separate "Shooter" entity is introduced.

`morwalpizvideo.client` is updated to filter videos to those whose `ChannelId` equals the configured MorWalPiz channel id (FR-017), preserving its current single-channel scope. `shooting-ita-frontend` shows videos from every channel. Every shown video must resolve to a known channel; unassigned videos are filtered out (FR-016). Log In / Sign Up are visual-only stubs in this iteration. The feature also produces a verification report (FR-013) mapping every layout element and acceptance scenario to Present / Partial / Missing in the shipped build.

## Technical Context

**Language/Version**: .NET 8 (C# 12) for backend; TypeScript 5.7 + React 19 for frontend.

**Primary Dependencies**:
- Backend: ASP.NET Core Web API, MongoDB.Driver, existing `IGenericDataService` + `IMorWalPizCache`, OutputCache, Hangfire (for the one-time backfill job).
- Frontend: Vite 6, React Router v7 (data-router with loaders), react-bootstrap 2.10, axios. Shared packages `@morwalpizvideo/models`, `@morwalpizvideo/services`, `@morwalpizvideo/layout` from `frontend/fe-packages/`.

**Storage**: MongoDB. No new collection. One additive field (`channelId`) on the existing `videos` collection with one new ascending index.

**Testing**:
- Backend: xUnit + SpecFlow under `MorWalPizVideo.BackOffice.Tests` — integration tests for the new `POST /api/videos/{id}/channel` endpoint, the backfill job, and the morwalpizvideo.client channel filter (controller-level, where applicable).
- Frontend: Vitest for the new layout pieces (sidebar highlight, hero rotation respecting reduced-motion, empty-state rendering, card metadata degradation, top-bar "coming soon" placeholder, channel-map join helper, Discover derivations).

**Target Platform**: Web (Azure App Service / Docker, nginx for the SPA, ASP.NET Core for the API).

**Project Type**: Web application (frontend SPA + backend API + shared .NET contracts + shared frontend packages).

**Performance Goals**: SC-005 — sidebar → Discover navigation < 1 s on broadband. Hero carousel keeps 60 fps on mid-range hardware. API: existing OutputCache pattern reused, no new endpoint heavy enough to need a separate p95 budget.

**Constraints**:
- Must render usably from 360 px to 1920 px (SC-004) without horizontal page scroll.
- Must respect `prefers-reduced-motion` for the hero auto-advance (FR-005).
- Discover categories MUST be derived client-side from the existing video API (no new endpoints for them).
- Every displayed video MUST have an owning channel; unassigned videos are filtered out (FR-016).
- `morwalpizvideo.client` MUST keep showing only MorWalPiz-channel videos (FR-017).
- Cache tags MUST be lowercase invariant per repo convention (`.github/copilot-instructions.md`).
- The `Video.ChannelId` field is additive and MUST NOT break existing readers; legacy documents without the field MUST continue to deserialize.

**Scale/Scope**:
- ~5 new sidebar/page surfaces (Home, Latest, Exclusives, Popular, plus placeholder Shows/Browse/Merch routes).
- Expected catalog size: low thousands of videos; low tens of channels.
- 1 new field on an existing collection, 1 new write endpoint, 1 one-time backfill, 0 new collections.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Simplicity & Readability First | PASS | Domain shift removed an entire entity, controller, DTO group, and service wrapper. The new surface is one field + one endpoint. |
| II. Layered Architecture with DTO Boundaries | PASS | New endpoint goes through the existing `IGenericDataService`; controller returns DTOs from `MorWalPiz.Contracts`. Loaders, not render code, fetch on the SPA. |
| III. Shared Contracts Are the Single Source of Truth | PASS | `VideoDto.ChannelId` added in `MorWalPiz.Contracts`; TS counterpart added to `@morwalpizvideo/models`; channel-map join helper added to `@morwalpizvideo/services`; layout primitives + the video player extracted to `@morwalpizvideo/layout`. `morwalpizvideo.client` and `shooting-ita-frontend` consume the same shared code. |
| IV. Feature-First, Typed Frontend | PASS | New routes under `frontend/shooting-ita-frontend/src/routes/` with `Component.tsx` + `loader.ts` per route. Explicit TS interfaces for every prop/state. |
| V. Test-Backed Behavior Verification | PASS | xUnit/SpecFlow integration tests for the new write endpoint and the backfill; Vitest tests for new shared layout components, the channel-map join helper, and the Discover derivations. The FR-013 verification report is an additional artifact, not a substitute for tests. |
| VI. Secure by Default | PASS | The new write endpoint requires the existing JWT + API-key + rate-limiting middleware (R-010 pins the exact attributes). Read endpoints reused as-is. SPA renders no user-supplied HTML; only API-provided text. |
| VII. Containerized, Cloud-Native Deployment | PASS | No new images; reuses the existing `shooting-ita-frontend` nginx image, the `morwalpizvideo.client` image, and the `MorWalPizVideo.BackOffice` / `ServerAPI` ASP.NET images. |

**Re-check after Phase 1**: still PASS — research, data-model, contracts, and quickstart did not introduce any new abstraction, secret, project, or cross-cutting concern beyond what the table above covers.

## Project Structure

### Documentation (this feature)

```text
specs/002-pepperbox-clone/
├── plan.md                      # This file
├── spec.md                      # Feature specification
├── research.md                  # Phase 0 output
├── data-model.md                # Phase 1 output
├── quickstart.md                # Phase 1 output
├── contracts/
│   └── videos-channel-api.md    # Phase 1 output (replaces the obsolete shooters contract)
├── checklists/
│   └── requirements.md          # Spec quality checklist
└── tasks.md                     # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
MorWalPiz.Contracts/
└── Contracts/
    └── Videos/
        └── VideoChannelAssignmentPayload.cs   # new: { channelId }
        # VideoDto in the existing Contracts/ gains: channelId

MorWalPizVideo.Models/
├── Models/
│   └── Video.cs                                # add ChannelId [BsonElement("channelId")]
└── Constraints/
    └── CacheKeys.cs                            # no change (videos/matches/channels already present)

MorWalPizVideo.Domain/  or  MorWalPizVideo.BackOffice/
└── (existing IRepository<T> / DataService reused)
└── Mongo bootstrap: add ascending index on Video.channelId (in the existing index-setup site)

MorWalPizVideo.BackOffice/   (host of the existing VideosController / ChannelsController)
└── Controllers/
    └── VideosController.cs                     # add POST /api/videos/{youtubeId}/channel

MorWalPizVideo.Operations/   (or a one-off Hangfire job in BackOffice/Jobs/)
└── BackfillVideoChannelIdJob.cs                # new: one-time idempotent backfill

MorWalPiz.VideoImporter/                        # WPF
└── Services/ImportVideoService.cs (or equivalent write site) # ensure ChannelId is set on new Video docs

MorWalPizVideo.BackOffice.Tests/
└── Features/
    └── VideoChannel/
        ├── AssignChannel.feature               # SpecFlow scenarios for the new endpoint
        ├── AssignChannelSteps.cs               # step definitions
        ├── BackfillChannel.feature             # backfill idempotence + missing-channel handling
        └── BackfillChannelSteps.cs

frontend/fe-packages/models/src/video/types.ts
└── add `channelId?: string` to the existing Video shape

frontend/fe-packages/services/src/
├── endpoints.ts                                # no change (channels endpoint already present)
└── videoChannelMap.ts                          # new: loadChannelMap(), buildOwnerMap(matches, channels)

frontend/fe-packages/layout/src/
├── components/
│   ├── PepperboxSidebar.tsx                    # new
│   ├── PepperboxTopBar.tsx                     # new
│   ├── HeroCarousel.tsx                        # new
│   ├── VideoCardRail.tsx                       # new
│   ├── VideoCard.tsx                           # new
│   ├── CategoryBanner.tsx                      # new
│   ├── CategoryVideoRow.tsx                    # new
│   ├── EmptyState.tsx                          # new
│   └── VideoPlayer.tsx                         # extracted from morwalpizvideo.client (R-009)
└── utils/
    ├── formatRelativeTime.ts                   # new
    └── prefersReducedMotion.ts                 # new

frontend/morwalpizvideo.client/
└── (consume the shared VideoPlayer; apply FR-017 filter using VITE_MORWALPIZ_CHANNEL_ID)

frontend/shooting-ita-frontend/src/
├── main.tsx                                    # switch to createBrowserRouter + RouterProvider
├── routes/
│   ├── root.tsx                                # shell using shared sidebar + topbar
│   ├── home/{Component.tsx, loader.ts}         # hero + exclusive-to-shooting-ita rail
│   ├── latest/{Component.tsx, loader.ts}       # category page
│   ├── exclusives/{Component.tsx, loader.ts}   # category page
│   ├── popular/{Component.tsx, loader.ts}      # category page
│   └── video/Component.tsx                     # detail/playback (shared VideoPlayer)
├── services/
│   └── shootingItaVideoService.ts              # composes matches + channels via videoChannelMap; applies FR-016 filter
├── utils/
│   └── deriveCategories.ts                     # pure derivation (latest / exclusives / popular / featured)
└── styles/
    └── theme.scss                              # dark theme + accent overrides

frontend/shooting-ita-frontend/src/__tests__/
├── deriveCategories.test.ts                    # vitest unit tests
├── HeroCarousel.test.tsx                       # reduced-motion + manual advance
├── VideoCard.test.tsx                          # graceful missing-metadata rendering
├── EmptyState.test.tsx                         # zero-results message
├── PepperboxTopBar.test.tsx                    # FR-012 "coming soon" placeholder behavior
├── CategoryBanner.test.tsx
├── CategoryVideoRow.test.tsx
└── videoChannelMap.test.ts                     # owner map building + FR-016 filter

specs/002-pepperbox-clone/
└── verification-report.md                      # FR-013 output, produced during /speckit.implement
```

**Structure Decision**:
- **Web application** layout: existing `MorWalPizVideo.BackOffice` (backend) + `frontend/shooting-ita-frontend` (frontend) + `frontend/morwalpizvideo.client` (sister SPA) + shared packages.
- All cross-cutting layout pieces and the video player live in `@morwalpizvideo/layout` so both SPAs reuse them (Principle III). App-local code in `shooting-ita-frontend` is limited to routing, loaders, the dark-theme override, and the FR-016 filter wrapper.
- The owning channel is stored directly on `Video.ChannelId` rather than on a separate Shooter document or denormalized onto `VideoRef`. This isolates the schema change to one collection, keeps `VideoRef` snapshots untouched on disk, and makes "videos by channel" a single ascending-index hit.
- `morwalpizvideo.client` gains the FR-017 filter as part of this feature, consuming the same shared channel-map helper that shooting-ita uses (with the filter applied).

## Complexity Tracking

> No Constitution Check violations. Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
