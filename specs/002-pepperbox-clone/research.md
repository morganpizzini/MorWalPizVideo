# Phase 0 — Research: Pepperbox-Style Shooting ITA Portal

Source spec: [spec.md](spec.md). All Clarifications in the spec are resolved.

---

## R-001 — Reuse existing video API surface

**Decision**: The frontend consumes the existing endpoints already exposed by `MorWalPizVideo.ServerAPI` / `MorWalPizVideo.BackOffice`:
- `GET /api/matches` (list with skip/take) — primary source for cards/rails/category pages.
- `GET /api/matches/{url}` — used by the video detail route.
- `GET /api/channels` — NEW use: powers the card/hero badge (channel name + avatar) and the owner-resolution map.
- `GET /api/videos` (existing projection) — used by the Popular derivation to resolve view counts per `VideoRef.YoutubeId`.

**Rationale**: All four endpoints already exist and are cached. No new read endpoint is needed for the Pepperbox surfaces.

**Alternatives considered**:
- Add a dedicated `/api/videos/featured` / `/api/videos/exclusive` / `/api/videos/popular` — rejected; categories are derived client-side (Q2).
- Hit MongoDB directly from a new BFF — rejected, violates Principle II.

---

## R-002 — Client-side derivation of Discover categories

**Decision** (one rule, applied identically on the home rail and the Discover pages):

- **Latest Videos** = matches sorted by `max(VideoRefs[].publishedAt)` descending (the wrapper is not a video itself; sort always reads the leaf `VideoRef.publishedAt` — see Key Entities in spec).
- **Exclusives** = matches whose `Categories[]` contains a category id equal to the runtime env `VITE_EXCLUSIVE_CATEGORY_ID`. **If the env var is unset, every surface that derives Exclusives — including the home "Exclusive to Shooting ITA" rail and the Exclusives Discover page — MUST render the empty-state message (FR-010).** No silent fallback to "last N by date".
- **Popular Now** = matches sorted by `sum(Video.Views)` over each `VideoRef.YoutubeId` (joined via the existing `/api/videos` projection) descending. When view data is missing for a video, treat its contribution as zero.
- **Featured (hero)** = first 5 items from the Latest derivation.

**Rationale**: Keeps Phase 0 promise of "no new backend endpoints for Discover". Eliminates the inconsistency where the same env-unset condition behaved differently on home vs Discover (analysis finding C2). Pins the sort field on `VideoRef.publishedAt` (analysis finding C6) and resolves how Popular is computed (analysis finding C3).

**Alternatives considered**:
- Hard-code an Exclusives category slug — rejected, brittle across environments.
- Add a `/api/videos/popular` aggregation endpoint — rejected per Q2/Q5; deferred to a future feature.

---

## R-003 — Channel ownership via `Video.ChannelId` (NEW)

**Decision**: Add a single `ChannelId` string field to the existing `Video` document (`MorWalPizVideo.Models/Models/Video.cs`). Reuse the existing `YTChannel` collection (`ytChannels`) and the existing `/api/channels` endpoint as the source of the badge name and avatar. Do NOT introduce a separate `Shooter` entity, controller, contracts, or TS shapes — the previously-proposed Shooter abstraction is superseded by this domain shift.

**Indexes**:
- `Video.ChannelId` — ascending — supports the morwalpizvideo.client filter (`ChannelId == VITE_MORWALPIZ_CHANNEL_ID`) and the shooting-ita loader's owner-existence check.

**Backfill**: a one-time idempotent backfill (Hangfire job or one-shot script under `MorWalPizVideo.Operations/`) walks every `Video` whose `ChannelId` is empty, finds the `YTChannel` whose embedded `Videos[].VideoId` contains the `Video.YoutubeId`, and sets `Video.ChannelId` accordingly. Videos with no matching channel remain empty and are filtered out by FR-016.

**Importer obligation**: the WPF `MorWalPiz.VideoImporter` MUST populate `ChannelId` when it creates new `Video` records.

**Rationale**: Reuses the existing channel surface that `morwalpizvideo.client` already consumes. Avoids the maintenance cost of a parallel `shooters` collection + controller + DTOs + tests. The "shared-with" relationship from the earlier Shooter proposal is dropped in this iteration; if it becomes necessary it can be added later as a separate feature.

**Alternatives considered**:
- Denormalize `channelId` onto every `VideoRef` snapshot — rejected; requires rewriting every `YouTubeContent` document and a write-path change in every place that creates a match.
- Keep the `Shooter` entity from the previous plan — rejected by the user's explicit domain-shift clarification.

---

## R-004 — How the SPA gets the owning channel for the card badge

**Decision**: The home/category loaders issue two requests in parallel:
1. `GET /api/matches?skip=0&take=N` (existing, cached).
2. `GET /api/channels` (existing, cached).

The loader then builds, in memory, a `Map<youtubeId, { channelId, channelName, avatarUrl }>` from the union of:
- `Video.ChannelId` for any video included in the match projection (post-Phase-2; until the backfill completes, this may be empty).
- `YTChannel.Videos[].VideoId` for each channel (covers all rows including legacy).

FR-016 is enforced by dropping any match whose first `VideoRefs[0].YoutubeId` is not in the map. FR-017 is enforced (in `morwalpizvideo.client`, NOT here) by additionally filtering to matches whose owner equals `VITE_MORWALPIZ_CHANNEL_ID`.

**Rationale**: Both endpoints are `OutputCache`-tagged so repeat navigations are free. The same map powers shooting-ita-frontend (no filter) and morwalpizvideo.client (channel-id filter) with a single shared helper.

**Alternatives considered**:
- A per-video lookup endpoint — rejected, N+1.
- A new aggregated `/api/videos/with-channels` join — rejected, conflicts with Q2.

---

## R-005 — Reduced-motion handling in the hero carousel

**Decision**: A shared `prefersReducedMotion()` helper in `@morwalpizvideo/layout/utils` reads `window.matchMedia('(prefers-reduced-motion: reduce)').matches` on mount and subscribes to changes. `HeroCarousel` disables auto-advance when reduced motion is preferred; manual prev/next remains enabled.

**Rationale**: FR-005; matches WCAG 2.3.3.

---

## R-006 — Verification report (FR-013) format

**Decision**: A markdown file `specs/002-pepperbox-clone/verification-report.md` is produced during `/speckit.implement` with:
1. A table with one row per acceptance scenario from User Stories 1 and 2.
2. A table with one row per layout element listed in FR-001, FR-004, FR-006, FR-007.
3. A short "Gaps" section listing each Partial/Missing item as a concrete user-visible deficiency.

---

## R-007 — Shared layout package extraction

**Decision**: All Pepperbox layout primitives (sidebar, top bar, hero carousel, rails, cards, banner, empty state) are added to `@morwalpizvideo/layout`. `shooting-ita-frontend` consumes them and supplies the dark-theme overrides via its own SCSS. The Pepperbox-style channel-aware loader helper (`loadVideosWithChannels()`) is added under `@morwalpizvideo/services` so `morwalpizvideo.client` can adopt it later (with the channel-id filter applied) without duplicating logic.

**Rationale**: Principle III. Same path the previous plan took, generalized so both SPAs can use it.

---

## R-008 — Switch `shooting-ita-frontend` to React Router data-router

**Decision**: Migrate `main.tsx` from `<BrowserRouter><Routes>` to `createBrowserRouter([...])` + `<RouterProvider>`. Each new route directory exposes `Component.tsx` and `loader.ts`.

**Rationale**: Principle IV mandates the loader-per-route convention. Current `<Routes>` form forces fetching in render and violates Principle II.

---

## R-009 — Video detail player reuse (analysis finding C8)

**Decision**: The shooting-ita video detail route imports and reuses the existing single-video player component from `morwalpizvideo.client`. Because Principle III forbids cross-app source coupling, the player is first extracted to `@morwalpizvideo/layout/src/components/VideoPlayer.tsx` (with whatever minimal props it needs), and BOTH SPAs are updated to consume the shared component in the same PR.

**Rationale**: Resolves C8 explicitly. Avoids both source duplication and a permanent cross-app `import` dependency.

---

## R-010 — Admin authorization on write endpoints (analysis finding C9)

**Decision**: The new `POST /api/videos/{youtubeId}/channel` endpoint uses the EXACT same attribute set as the existing `MorWalPizVideo.BackOffice` admin write endpoints (verified during T-impl by reading one such endpoint and copying the attribute list verbatim — typically `[Authorize]` + the API-key attribute + a rate-limit policy). The attribute set is pinned by name in tasks.md before implementation begins.

---

## Open items resolved at planning time

| Item | Resolution |
|---|---|
| Where do "Exclusives" come from? | Configurable env var `VITE_EXCLUSIVE_CATEGORY_ID` on each SPA (R-002). |
| What if `VITE_EXCLUSIVE_CATEGORY_ID` is unset? | Empty-state on every Exclusives surface, including the home rail (R-002 / C2). |
| How is "Popular" computed without a new endpoint? | Sum `Video.Views` over `VideoRefs[].YoutubeId`; missing → zero (R-002 / C3). |
| What field drives Latest/Featured sort? | `max(VideoRefs[].PublishedAt)` always (R-002 / C6). |
| Do we keep the `Shooter` entity? | No — superseded by `Video.ChannelId` + `YTChannel` reuse (R-003 / C1). |
| How is morwalpizvideo.client restricted to MorWalPiz videos? | Client-side filter on `Video.ChannelId == VITE_MORWALPIZ_CHANNEL_ID` (FR-017). |
| How does the home page show the owning channel without N+1 calls? | Single `/api/channels` fetch + in-memory map (R-004). |
| How do we honor FR-013 without inventing a process? | Produce `verification-report.md` during `/speckit.implement` (R-006). |
| How do we avoid duplicating the video player across SPAs? | Extract to `@morwalpizvideo/layout`, both SPAs consume (R-009). |

No `NEEDS CLARIFICATION` markers remain.
