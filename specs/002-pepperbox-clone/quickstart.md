# Quickstart — Pepperbox-Style Shooting ITA Portal

This is the manual smoke test for the feature. It doubles as the input for the FR-013 verification report.

## Prerequisites

- .NET 8 SDK, Node 20+, npm.
- Local MongoDB reachable from `MorWalPizVideo.BackOffice` (connection string in `appsettings.Development.json`).
- A populated `matches` collection and `videos` collection (existing dataset).
- A populated `ytChannels` collection with at least the MorWalPiz channel and one additional channel; the additional channel MUST own at least one video that already exists in `videos` (otherwise the FR-016 filter hides everything in shooting-ita-frontend).
- The one-time backfill (Phase 2, T0xx) has been run at least once — or you accept that legacy videos without a populated `Video.ChannelId` are filtered out.

## Run the backend

```pwsh
cd MorWalPizVideo.BackOffice
dotnet run
```

API base: `https://localhost:5001` (default per `Properties/launchSettings.json`).

Smoke-check the new field + endpoint:

```pwsh
# read an existing video — channelId should be populated post-backfill
curl https://localhost:5001/api/videos/<some-youtubeId>

# assign a channel (admin auth required — replace token/api-key with your dev values)
curl -X POST https://localhost:5001/api/videos/<some-youtubeId>/channel `
  -H "Authorization: Bearer <jwt>" `
  -H "X-Api-Key: <api-key>" `
  -H "Content-Type: application/json" `
  -d '{ "data": { "channelId": "UC_xxxxxxxx" } }'
```

The first call MUST return JSON including a non-empty `channelId`. The second MUST return 200 + the updated DTO.

## Run the shooting-ita SPA

```pwsh
cd frontend/shooting-ita-frontend
npm install
# one-time: ensure shared packages are built
cd ../fe-packages/models   && npm run build
cd ../services             && npm run build
cd ../layout               && npm run build
cd ../../shooting-ita-frontend
# point to your API and (optional) exclusive category id
@"
VITE_API_BASE_URL=https://localhost:5001
VITE_EXCLUSIVE_CATEGORY_ID=<category-id>
"@ | Set-Content .env.local
npm run dev
```

Open `http://localhost:5173` (or the port Vite reports).

## Run morwalpizvideo.client (FR-017 verification)

```pwsh
cd frontend/morwalpizvideo.client
npm install
@"
VITE_API_BASE_URL=https://localhost:5001
VITE_MORWALPIZ_CHANNEL_ID=<MorWalPiz channel id from ytChannels>
"@ | Set-Content .env.local
npm run dev
```

Open at the port reported. Only videos whose `ChannelId` equals the configured MorWalPiz id should appear; videos from the additional channel(s) MUST be hidden here.

## Acceptance walkthrough

Run every step and tick the result. Each tick maps directly to a row in `verification-report.md`.

### User Story 1 — Pepperbox-style home (desktop, shooting-ita)

1. Load `/` at ≥ 1280 px width. Sidebar shows brand, primary nav (Home, Shows, Browse, Merch), Discover group (Latest Videos, Exclusives, Popular Now), Help, footer (FAQ, Privacy Policy, Terms of Service), copyright. ✅/❌
2. Hero shows one featured item with artwork, owning-channel badge, title overlay, Play button, pagination dots. ✅/❌
3. At least one rail titled e.g. "Exclusive to Shooting ITA" shows video cards with thumbnail + duration badge + title + channel name+avatar + relative publish time. ✅/❌
4. Clicking Play on the hero navigates to the video detail/playback route. ✅/❌
5. Clicking a card on the rail navigates to the same route for that video. ✅/❌
6. Resize the window to 360 px: sidebar collapses behind a hamburger; no horizontal page scroll. ✅/❌
7. Top-right shows Log In / Sign Up; clicking either shows a "coming soon" message (visual-only). ✅/❌

### User Story 2 — Discover category pages (shooting-ita)

1. Click "Exclusives" → wide themed banner + vertical list (large thumbnail left, title/channel/description/time right). ✅/❌
2. Repeat for "Latest Videos" and "Popular Now". ✅/❌
3. The active sidebar item is visually highlighted on each. ✅/❌
4. Temporarily unset `VITE_EXCLUSIVE_CATEGORY_ID` and reload `/`: the home "Exclusive to Shooting ITA" rail AND the Exclusives page BOTH show the empty-state message (consistent behavior). ✅/❌

### FR-017 — morwalpizvideo.client channel restriction

1. Browse the morwalpizvideo.client home. Every visible video belongs to the MorWalPiz channel; no video from any other channel appears. ✅/❌
2. Unset `VITE_MORWALPIZ_CHANNEL_ID` (dev) and confirm the SPA either shows the empty-state or refuses to render (per chosen behavior). ✅/❌

### Empty / degraded states

1. Temporarily clear `Video.ChannelId` on a subset of videos (or rename channel ids). Reload shooting-ita `/`: affected videos disappear; if all videos disappear, every rail/category renders the empty-state message. ✅/❌
2. Restore `ChannelId` values. Pick a channel whose record has no avatar — cards/hero MUST render an initials placeholder instead of a broken image. ✅/❌
3. With OS-level reduced motion enabled, the hero MUST NOT auto-advance; manual prev/next still works. ✅/❌

## Backend tests

```pwsh
cd MorWalPizVideo.BackOffice.Tests
dotnet test --filter FullyQualifiedName~VideoChannel
```

Required green: assign-channel, backfill-idempotence, missing-channel-rejected, cache eviction.

## Frontend tests

```pwsh
cd frontend/shooting-ita-frontend
npm test
```

Required green: `deriveCategories.test.ts`, `videoChannelMap.test.ts`, `HeroCarousel.test.tsx`, `VideoCard.test.tsx`, `EmptyState.test.tsx`, `PepperboxTopBar.test.tsx`, `CategoryBanner.test.tsx`, `CategoryVideoRow.test.tsx`.

## Producing the verification report

After the walkthrough, fill in `specs/002-pepperbox-clone/verification-report.md` (template is created during `/speckit.implement`) with the ticks captured above plus a Present / Partial / Missing row per layout element. This artifact satisfies FR-013.
