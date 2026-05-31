# Feature Specification: Pepperbox-Style Shooting ITA Portal

**Feature Branch**: `002-pepperbox-clone`

**Created**: 2026-05-31

**Status**: Draft

## Clarifications

### Session 2026-05-31

- Q: Should Log In / Sign Up be functional in this feature, or visual-only placeholders matching Pepperbox? → A: Visual-only placeholders (buttons render; clicking shows a "coming soon" message).
- Q: Where do the videos for hero, rails and Discover categories come from? → A: Reuse existing MorWalPiz video/channel APIs (same ones `morwalpizvideo.client` consumes); derive Latest/Exclusives/Popular client-side over that dataset. No new backend endpoints required in this feature.
- Q: What defines a "Featured" hero item? → A: Top 5 most recent videos across the catalog (no new data model or admin curation in Fthis feature).
- Q: What is a video's "Channel / Author" in this product? → A: Introduce a new "Shooter" entity that owns or shares a video; a video can be owned by exactly one shooter and additionally shared with zero or more other shooters. The card/hero badge shows the owning shooter; videos shared with a shooter also appear on that shooter's surfaces.
- Q: Resolve the conflict between Q2 (no new backend endpoints) and Q4 (new Shooter entity)? → A: Keep Shooter; this feature's scope explicitly INCLUDES the backend data model + endpoints needed to persist Shooters and the owns / shared-with relationships, and to query "videos by shooter". Q2 is superseded for the Shooter surface; the Discover categories (Latest / Exclusives / Popular) still derive client-side over existing video data with no new endpoints.
- Q: (supersedes Q4/Q5) Domain shift — ownership of a video is now expressed as the YouTube channel that produced it, reusing the existing `YTChannel` collection. → A: Drop the new `Shooter` entity entirely. Add a `ChannelId` field to the existing `Video` document and reuse the existing `/api/channels` endpoint to resolve the badge name/avatar. The card/hero "shooter" badge shows the owning channel's name; videos can no longer be "shared with" other shooters in this iteration. `morwalpizvideo.client` MUST continue to show only videos whose `ChannelId` equals the configured MorWalPiz channel id; `shooting-ita-frontend` shows videos from every channel.

**Input**: User description: "frontend/shooting-ita-frontend is an application that wants to show a conglomerate of videos and information about shooters in Italy, like Pepperbox TV (https://www.pepperbox.tv/). I want it to simulate Pepperbox's layout, presentation and content. Something is already in place and I want to verify that everything works fine."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover shooting videos from a Pepperbox-style home (Priority: P1)

A visitor lands on the Shooting ITA home page and is immediately presented with a Pepperbox-style experience: a persistent left sidebar with primary navigation (Home, Shows, Browse, Merch) and a Discover section (Latest Videos, Exclusives, Popular Now), a featured hero/carousel area at the top with a large title, channel/brand badge, and a Play call-to-action, and below it horizontally-scrollable / grid rails of video cards grouped by theme (e.g. "Exclusive to Shooting ITA", "Latest", "Popular Now"). The visitor can scan the home page, identify featured content, and click Play to watch a featured video without needing to sign in.

**Why this priority**: This is the primary landing experience and the core of the "looks like Pepperbox TV" requirement. Without it the product does not meet the user's stated goal.

**Independent Test**: Load the application root in a browser at desktop resolution and verify the sidebar, hero carousel and at least one rail of video cards render with real data from the existing backend and that clicking Play on the hero or a card navigates to the corresponding video detail/playback view.

**Acceptance Scenarios**:

1. **Given** the user opens the site root on a desktop viewport, **When** the page finishes loading, **Then** a fixed left sidebar is visible containing the brand logo at the top, the primary nav items (Home, Shows, Browse, Merch), a "Discover" group (Latest Videos, Exclusives, Popular Now), a Help entry and footer links (FAQ, Privacy Policy, Terms of Service, copyright).
2. **Given** the home page is loaded, **When** the user looks at the main area, **Then** a hero block displays a featured item with a background image/artwork, a channel/brand badge, a title overlay and a visible Play button, with pagination dots indicating multiple featured items.
3. **Given** the home page is loaded, **When** the user scrolls below the hero, **Then** at least one section titled like "Exclusive to Shooting ITA" shows a row of video cards, each with thumbnail, duration badge, title, channel/author label and relative publish time (e.g. "2 hours ago").
4. **Given** the user clicks Play on the hero or clicks a video card, **When** the navigation completes, **Then** the user reaches a video playback or detail view for that item.
5. **Given** the viewport is narrowed below the desktop breakpoint, **When** the layout reflows, **Then** the sidebar collapses behind a hamburger toggle and the hero/rails remain usable without horizontal page scroll.

---

### User Story 2 - Browse curated category pages (Exclusives / Latest / Popular) (Priority: P2)

A visitor uses the sidebar Discover entries to open dedicated category pages. Each category page shows a themed banner header (e.g. "EXCLUSIVES" artwork) and a vertical list of videos with larger thumbnail on the left and full title, channel, description and publish time on the right, matching Pepperbox's category list presentation.

**Why this priority**: Category pages are the second-most-visible surface and required for the experience to feel like a real "channel" portal rather than a single landing page.

**Independent Test**: Click "Exclusives" (and separately "Latest Videos", "Popular Now") in the sidebar and verify a list view of the appropriate videos renders with the themed banner and the row layout described above, sourced from the existing backend.

**Acceptance Scenarios**:

1. **Given** the user clicks "Exclusives" in the sidebar, **When** the category page loads, **Then** a wide banner with the "Exclusives" artwork/title is shown at the top of the main area.
2. **Given** the user is on a category page, **When** the list renders, **Then** each row shows a thumbnail with duration badge on the left and title, channel name, short description and publish time on the right.
3. **Given** the user is on any category page, **When** the user clicks the currently-active sidebar item, **Then** that item is visually highlighted as selected.

---

### User Story 3 - Verify the existing implementation against the target experience (Priority: P1)

The project owner needs to verify that the current `frontend/shooting-ita-frontend` codebase actually delivers Stories 1 and 2 end-to-end, identify any gaps against the Pepperbox reference, and produce a checklist of what is missing or broken so the work can be planned.

**Why this priority**: The user explicitly asked to "verify that all works fine" against an existing partial implementation; without this verification step the feature cannot be considered done.

**Independent Test**: Run the existing frontend locally against the existing backend, walk through the acceptance scenarios of Stories 1 and 2, and produce a written verification report listing per-scenario pass/fail and per-element gap (e.g. "sidebar missing Discover section", "hero carousel has only static placeholder data").

**Acceptance Scenarios**:

1. **Given** the verifier runs the app locally, **When** they execute every acceptance scenario from Stories 1 and 2, **Then** each scenario is marked Pass or Fail with a short note.
2. **Given** the verification is complete, **When** the report is produced, **Then** it lists every visible Pepperbox layout element (sidebar groups, hero, rails, category banners, card metadata) and marks it Present / Partial / Missing in the current build.
3. **Given** gaps are identified, **When** they are recorded, **Then** each gap is phrased as a concrete user-visible deficiency (not an implementation task).

---

### Edge Cases

- What happens when the backend returns zero videos for a rail or category? The section should render an empty-state message instead of disappearing or showing broken thumbnails.
- What happens when a video has no thumbnail, no duration, or no channel/author info? The card must degrade gracefully (placeholder image, hide missing metadata) without breaking the row layout.
- What happens on the smallest mobile viewport? The hero must remain readable, the sidebar must be reachable via a toggle, and rails/lists must not require horizontal page scroll.
- What happens if the user navigates directly to a category URL (deep link) before the sidebar JS has hydrated? The correct sidebar item must still be highlighted once hydration completes.
- What happens when the user has reduced-motion settings enabled? Hero carousel auto-advance must respect that preference.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST render a persistent left sidebar on desktop viewports containing, in order: brand logo, primary nav (Home, Shows, Browse, Merch), a "Discover" group (Latest Videos, Exclusives, Popular Now), a Help entry, and a footer block with FAQ, Privacy Policy, Terms of Service links and a copyright line.
- **FR-002**: The sidebar MUST visually highlight the entry corresponding to the current route.
- **FR-003**: Below a defined breakpoint the sidebar MUST collapse into a hamburger-triggered overlay/drawer accessible from a top bar.
- **FR-004**: The home page MUST display a hero/featured area showing one item at a time with background artwork, a channel/brand badge, a title overlay, a Play call-to-action, and pagination indicators for multiple featured items. The featured set MUST be the 5 most recent videos available from the existing video API, ordered newest first; no curation flag or admin selection is introduced by this feature.
- **FR-005**: The hero MUST allow the user to advance between featured items (manual control) and SHOULD auto-advance unless the user has reduced-motion enabled.
- **FR-006**: The home page MUST display at least one horizontally laid-out rail of video cards under a section title (e.g. "Exclusive to Shooting ITA"), with each card showing thumbnail, duration badge, title, owning shooter name + avatar, and relative publish time.
- **FR-007**: Each sidebar Discover entry (Latest Videos, Exclusives, Popular Now) MUST open a dedicated category page with a themed banner header and a vertical list of videos using the larger thumbnail + title/channel/description/publish-time row layout.
- **FR-008**: Clicking the Play button on the hero or on any video card MUST navigate the user to the playback or detail view of that video.
- **FR-009**: Video, channel and category content MUST be sourced from the existing MorWalPiz video APIs already consumed by `frontend/morwalpizvideo.client`. The Discover categories (Latest Videos, Exclusives, Popular Now) MUST be derived client-side from that dataset (e.g. Latest = sorted by `VideoRef.publishedAt` desc, Exclusives = filtered by the existing exclusive category, Popular Now = sorted by the sum of `VideoRef[].views` for each match — see Key Entities note on the wrapper vs leaf distinction). Placeholder/static demo data MUST NOT remain in shipped pages.
- **FR-015**: The `Video` document MUST carry a `ChannelId` field identifying the YouTube channel that produced it (matching `YTChannel.ChannelId`). A write endpoint MUST exist at API level so an admin can assign or change a video's owning channel; a dedicated admin UI is out of scope for this feature. The existing `/api/channels` endpoint and `YTChannel` collection are reused as the source of badge name + avatar — no new "Shooter" entity is introduced.
- **FR-016**: Every Video shown in `shooting-ita-frontend` MUST resolve to a `ChannelId` that exists in the `ytChannels` collection. Videos with an empty or unresolved `ChannelId` MUST NOT appear in the hero, rails, or Discover category pages.
- **FR-017**: `morwalpizvideo.client` MUST continue to show ONLY videos whose `ChannelId` equals the configured MorWalPiz channel id (sourced from a runtime config value, e.g. `VITE_MORWALPIZ_CHANNEL_ID` for the SPA or an equivalent backend filter). `shooting-ita-frontend` MUST NOT apply this filter — it shows videos from every channel present in `ytChannels`.
- **FR-010**: Sections, rails and category pages MUST render an explicit empty-state message when the backend returns no items.
- **FR-011**: Video cards MUST degrade gracefully when optional metadata (thumbnail, duration, channel, publish date) is missing, without breaking the surrounding layout.
- **FR-012**: The top-right area of the layout MUST expose "Log In" and "Sign Up" buttons styled to match the reference; in this iteration they are visual-only placeholders that, when clicked, display a brief "coming soon" message and do not navigate to any auth flow.
- **FR-013**: The verification activity MUST produce a written report mapping every layout element listed in FR-001, FR-004, FR-006, FR-007 and the acceptance scenarios of Stories 1–2 to a Present / Partial / Missing status against the current `frontend/shooting-ita-frontend` build.
- **FR-014**: The product MUST adopt the visual tone of the reference (dark theme, white/orange accent on the active sidebar item, large bold typography for hero titles, rounded video card thumbnails); exact brand colors and typography MAY differ but the dark theme and accent treatment are required.

### Key Entities *(include if feature involves data)*

- **Video** (leaf): A piece of shooting-related video content. Key attributes (user-visible): title, thumbnail/artwork, duration, view count, publish date, and the new `ChannelId` identifying the owning channel. Source-of-truth for sorting and view counts.
- **YouTubeContent / Match** (wrapper): A grouping object that links one or more `VideoRef`s sharing the same context (e.g. a race recap and its behind-the-scenes), and stores the common metadata (title, description, categories) shown above the rail/card. It is NOT a video itself; sorting and view counts MUST be derived from its embedded `VideoRef`s. A match's "published date" used for sort is the maximum `VideoRef.publishedAt` over the match; a match's view count is the sum of `VideoRef[].views`.
- **Channel (YTChannel — reused)**: The existing YouTube channel record (`MorWalPizVideo.Models/Models/YTChannel.cs`, collection `ytChannels`) that owns one or more videos. The card/hero badge shows this channel's `ChannelName` and (if available) avatar. No new entity is introduced.
- **Category (Discover entry)**: A curated grouping surfaced as a sidebar entry and as a dedicated page. Key attributes: name (Latest Videos / Exclusives / Popular Now), banner artwork, ordered list of matches.
- **Featured Item (Hero slide)**: A match promoted in the home hero carousel; in this iteration the set is the 5 most recent matches ordered by max `VideoRef.publishedAt` desc. Key attributes: artwork, overlaid title, owning-channel badge, Play target, position in the carousel.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time visitor landing on the home page can identify a featured video and start playback in under 10 seconds without scrolling beyond the hero.
- **SC-002**: 100% of the visible layout elements enumerated in FR-001, FR-004, FR-006 and FR-007 are present on the corresponding pages of the shipped build (verified by the report in FR-013).
- **SC-003**: All home page rails and all Discover category pages display real backend-sourced content in production builds; zero pages still rely on hard-coded placeholder items.
- **SC-004**: The home page and every Discover category page render usably (no horizontal page scroll, sidebar reachable, hero readable) at viewport widths from 360 px to 1920 px.
- **SC-005**: Navigating from the sidebar to any Discover category and back to Home completes in under 1 second on a typical broadband connection.
- **SC-006**: When the backend returns zero items for a rail or category, the user sees a clear empty-state message in 100% of cases (no blank space, no broken card skeletons).

## Assumptions

- The reference experience to mirror is the public Pepperbox TV home and category pages as captured in the attached screenshots (sidebar + hero + rails on home; banner + vertical list on category pages).
- The existing `frontend/shooting-ita-frontend` React + Vite + react-router app is the basis to extend. Videos come from the existing MorWalPiz video APIs that `frontend/morwalpizvideo.client` already consumes; the Discover categories are derived client-side from that dataset. The only new backend surface introduced by this feature is a `ChannelId` field on the existing `Video` document and one admin endpoint to assign it (`POST /api/videos/{youtubeId}/channel`); the existing `YTChannel` collection and `/api/channels` endpoint are reused for badge data.
- The product targets the Italian shooting community and content is in Italian or English as already produced by the existing channels; localization beyond the current state is out of scope.
- Authentication (Log In / Sign Up) is presentation-only for this feature unless clarified otherwise; the existing reCAPTCHA / request-video / request-ad flows are not being removed but are not part of the Pepperbox layout work.
- Video playback uses whatever player the existing detail pages already provide; building a new player is out of scope.
- Merch and Shows sidebar entries may link to placeholder pages in this iteration; their full content is out of scope.
- "Verify that all works fine" means producing the written verification report in FR-013 and fixing any gaps surfaced against the acceptance scenarios; it does not mandate automated end-to-end test coverage in this feature.
