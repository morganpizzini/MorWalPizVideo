# Active Context - MorWalPizVideo

## Current Focus
Feature 002 — Pepperbox-Style Shooting ITA Portal (specs/002-pepperbox-clone)
shipped via ADAPT path on 2025-11-01. See
`specs/002-pepperbox-clone/verification-report.md` for the full report.

Highlights:
- New shared layout components live in `@morwalpiz/layout` (HeroCarousel,
  VideoCard*, PepperboxSidebar/TopBar, CategoryBanner/VideoRow, EmptyState,
  VideoPlayer).
- Video↔channel ownership is resolved client-side via
  `@morwalpizvideo/services` → `videoChannelMap` (union of `YTChannel.Videos[]`
  + optional `Video.channelId` on match projections).
- New backend endpoint: `POST /api/Videos/{youtubeId}/channel` mutates
  `YTChannel.Videos[]` (idempotent, removes from prior owners, evicts
  `channels` + `matches` caches).
- Two pre-existing blockers documented (NOT fixed this iteration):
  `SecurityRequirementsOperationFilter.Apply` missing impl (breaks the
  BackOffice WebApplicationFactory and therefore every integration test);
  implicit-any TS errors in `morwalpizvideo.client/src/utils/*` that predate
  Feature 002.


## Recent Changes
- **March 2026**: Service layer consolidation - Migrated morwalpizvideo.client services to shared @morwalpizvideo/services package
- Eliminated ~60 lines of duplicated code
- Centralized endpoint management in shared package

## Next Steps
Update this file when starting new work to track:
- What you're building
- Current decisions and approach
- Blockers or considerations
- Related files being modified

## Key Decisions
Track important technical decisions and their rationale here as they happen.