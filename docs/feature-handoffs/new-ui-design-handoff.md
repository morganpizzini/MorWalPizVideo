# Public Homepage Editorial UI - Implementation Audit and Mobile Handoff

**Status:** Implemented; browser evidence remains a release-owner check  
**Audit date:** 2026-11-04  
**Owning application:** `frontend/morwalpizvideo.client`  
**Surface:** Public homepage and global public header/footer

### Outcome

The requested Digital Magazine / Creator Hub homepage is substantially present in current source. The implementation already includes the dark black/yellow editorial direction, mixed video/article/series content, featured content, filters, sponsors, gear, community, donation, and responsive layout rules.

This handoff replaces the former target-only specification with a source-verified completion plan. The remaining work is a refinement, not a redesign: preserve the current visual language and behavior while making the mobile experience release-ready and proving it in real browsers.

### Implementation delivered

- Phone content and article grids use one column below 480px; coarse-pointer controls use 44px minimum hit areas.
- Header navigation now focuses its first item on open, traps keyboard focus, closes on Escape and route activation, and returns focus to the toggle.
- Hover-only media and control styling is limited to fine pointers; sticky shell and footer include safe-area insets, and the viewport enables `viewport-fit=cover`.
- Static and generated SEO fallbacks use `morwalpiz.com`; the existing `VITE_CANONICAL_BASE_URL` override remains supported.
- `DESIGN.md` now documents the implemented black/yellow editorial system, responsive rules, interaction behavior, and canonical identity.
- Existing homepage, header, loader, and blog tests remain source-focused; canonical blog SEO coverage was updated for the approved domain.

Validation completed on 2026-11-04: focused tests (11 passed), focused TypeScript check, full client test suite (4 files, all passed), and `npm run build:all` (client and SSR passed). `npm run lint` remains red on pre-existing unrelated files/configuration (`src/routes/system/bluetooth.tsx`, `src/services/apiKeys.ts`, and generated-file rule definitions). Real-browser viewport, touch, zoom, and safe-area evidence was not run in this session and remains the release owner's check.

Assumptions:

- Scope is the public homepage plus the header and footer that frame it.
- Existing routes, public API contracts, content retrieval, sharing behavior, and URLs remain compatible.
- No backend, MongoDB, authentication, or cache changes are required unless the open Shorts sourcing decision explicitly expands scope.
- The implementation must not introduce Tailwind; the application currently uses Sass and Bootstrap.
- The installed Impeccable workflow applies in `Persuade` mode. The narrowest next command is `adapt` for mobile, followed by `harden` and `polish`. No `PRODUCT.md` or surface brief exists for this application; `DESIGN.md` is present but stale relative to current source.

### Current Evidence

#### Verified implementation

- `frontend/morwalpizvideo.client/src/routes/home/Component.tsx` owns the homepage composition and implements a featured hero, collection navigation and episode badges, a mixed date-sorted feed, type/category/tag/text filtering, compact sharing, Shorts promotion, sponsors, gear, articles, community, donation, news, survey, live, empty, and retry states.
- `frontend/morwalpizvideo.client/src/routes/home/loader.ts` reuses existing service owners for matches, configuration, surveys, channel news, sponsors, and articles. Optional article and sponsor failures degrade independently; primary content failure exposes retry state.
- `frontend/morwalpizvideo.client/src/routes/home/style.scss` implements desktop, tablet, phone, and narrow-phone breakpoints at 1200px, 768px, 480px, and 360px boundaries.
- `frontend/morwalpizvideo.client/src/layouts/title-header.tsx` and `title-header.scss` implement a sticky public header, skip link, search, responsive hamburger drawer, active route state, social links, and account action.
- `frontend/morwalpizvideo.client/src/layouts/footer.tsx` and `footer.scss` preserve configured navigation, social links, sponsor, gear, article, donation, privacy, terms, and consent destinations.
- `frontend/morwalpizvideo.client/src/main.scss` owns the current black/yellow tokens, page containers, global focus indicator, and reduced-motion handling.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx` applies the dedicated homepage shell while preserving legacy light surfaces on other routes.
- `frontend/morwalpizvideo.client/src/routes/home/Component.test.tsx` verifies feed ordering, compound filtering, video/series/article destinations, series badges, SSR markup, alt text, and lazy image behavior.
- `frontend/morwalpizvideo.client/src/routes/home/loader.test.ts` verifies local filter URL changes, data composition, optional article failure, primary failure, and transport error envelopes.

#### Mobile implementation status

Mobile is structurally handled but not complete:

- The header switches to a hamburger drawer below 1200px and hides compact social actions below 576px.
- The hero becomes one column below 768px; its sidebar becomes one column below 480px.
- Content and article grids remain two columns from 360px through 767px, becoming one column only below 360px.
- Type, category, and tag filters use horizontal scrolling where space is constrained.
- Sponsors and gear stack below 1200px, and the third visible sponsor is hidden below 768px.
- Text wrapping, `minmax(0, ...)`, fixed media aspect ratios, and reduced page padding lower overflow risk.

The following mobile gaps are verified in source:

1. Several interactive targets are below the 44px mobile target: header icon buttons are 40px, compact social links 32px high, homepage share buttons 32px, sponsor controls 30px, filter chips 34px, text links 36px, and primary editorial buttons 40px.
2. The two-column content grid at 360-479px leaves very narrow cards, while card titles are 14px, collection CTA text is 10px, and metadata can be 10px. This is likely to reduce readability and tap accuracy.
3. Hover transforms run without a hover-capability query. Touch users do not lose content, but interaction styling is not input-aware.
4. The menu exposes `aria-expanded` and closes after link activation, but source has no Escape handling, focus return, focus containment, outside-click handling, or open-state scroll management.
5. The viewport metadata does not enable `viewport-fit=cover`; safe-area padding is applied to push opt-in only, not the sticky header, homepage shell, or footer.
6. There is no checked-in browser or screenshot evidence for 320px, common phone widths, tablet, landscape, zoom, text scaling, or touch behavior.
7. The global reduced-motion rule collapses all transitions to 0.01ms rather than preserving useful non-motion state feedback.
8. The donation panel stacks on narrow phones but remains part of the hero aside rather than becoming the dedicated mobile section described by the original brief.

#### Cross-document and release evidence gaps

- `frontend/morwalpizvideo.client/DESIGN.md` still describes the former photographic, white-surface, Bootstrap-blue design and says the home grid is single-column below 768px. It conflicts with current black/yellow source and must not guide further implementation until reconciled.
- `frontend/morwalpizvideo.client/index.html` and `src/utils/seo.tsx` still default canonical and social metadata to `morwalpiz.it`, while the architecture guide defines `morwalpiz.com` as canonical.
- The Shorts card uses a fixed YouTube destination and fixed blob image. This conflicts with the original goal to avoid hardcoded promotional content, although it does not currently require a backend change.
- Existing tests are source and SSR focused. They do not prove responsive layout, menu interaction, keyboard flow, browser overflow, or visual quality.

#### Source-level audit verdict

The implementation expresses a coherent, product-specific editorial system and is not a placeholder. It is not yet possible to state that everything is in place or that mobile is fully handled. The remaining issues are bounded and can be completed in the public frontend without a visual redesign.

### Proposed Design

Preserve the current homepage architecture and visual direction. Do not introduce a parallel content model or move app-specific homepage components into `@morwalpiz/layout`.

#### Mobile layout

- Use one content column below 480px. Keep two columns from 480px through 767px only where the card content remains readable at 200% text zoom.
- Keep the featured card full-width and place Follow and Donation beneath it. On narrow phones, treat Donation as a full-width section after Follow rather than a compressed peer.
- Keep horizontally scrollable filters, with visible edge affordance and keyboard scrolling. Do not allow the page itself to scroll horizontally.
- Preserve episode count, content type, title, destination, and share access at every width.
- Keep sponsor logos proportional and allow fewer visible logos instead of shrinking them below legibility.

#### Mobile interaction

- Raise all primary, filter, icon, share, carousel, social, search, account, and menu targets to at least 44 by 44 CSS pixels for coarse pointers.
- Restrict hover-only transforms to devices that support hover. Preserve clear active and focus-visible feedback for touch and keyboard users.
- Harden the mobile navigation as an accessible disclosure: Escape closes it, focus returns to the toggle, route changes close it, and open-state behavior does not strand focus or cause background interaction problems.
- Support safe-area insets in the sticky shell and footer after enabling full-viewport fitting.

#### Content and presentation

- Retain the current mixed feed and content-type-specific cards.
- Keep the first content image high priority and lazy-load below-fold media with stable aspect ratios.
- Preserve source-backed sponsors, articles, matches, channel news, surveys, and navigation.
- Decide whether Shorts is intentionally a fixed channel promotion or should be configured/data-driven. Do not add a shared contract or backend model without explicit approval.
- Keep the dark palette and yellow accent, but reconcile tokens and component rules in `DESIGN.md` so future work does not regress to the old visual system.

#### Completion criteria

The follow-up implementation is complete only when:

- no page-level horizontal overflow occurs at 320px or wider;
- core content remains readable and operable at 200% text zoom;
- all mobile/coarse-pointer interactive targets meet the 44px minimum;
- the header menu is keyboard and touch operable, with reliable close and focus behavior;
- video, series, article, sponsor, gear, donation, social, search, filter, and share destinations remain correct;
- collection badges remain visible without covering essential media or titles;
- empty, loading, optional-service failure, and primary failure states work at mobile and desktop widths;
- the canonical domain is `morwalpiz.com` in static and route-generated metadata;
- current design documentation matches source;
- focused tests, the application build, and the browser viewport matrix pass;
- desktop behavior outside this homepage refinement remains unchanged.

### Impacted Projects

- `frontend/morwalpizvideo.client`: Modify. Owns the public homepage, shell, styles, SEO, tests, and deployment artifact.
- `frontend/fe-packages/models`: Verify only. No new DTO is required for the recommended scope.
- `frontend/fe-packages/services`: Verify only. Existing service ownership should remain unchanged.
- `frontend/fe-packages/layout`: Verify only. The homepage is app-specific; no demonstrated second consumer justifies extraction.
- `MorWalPizVideo.ServerAPI`: Verify only. Existing public contracts remain sufficient unless Shorts sourcing is separately approved as data-driven.
- Aruba public frontend deployment: Verify. The static/SSR/PWA artifact must preserve routing, canonical metadata, responsive assets, and runtime API configuration.
- Other frontend applications: Not affected. No shared package behavior should change.

### Impacted Files

#### Modify

- `frontend/morwalpizvideo.client/src/routes/home/style.scss`: mobile grid, touch targets, pointer-aware interaction, donation placement, and overflow hardening.
- `frontend/morwalpizvideo.client/src/routes/home/Component.tsx`: only the minimal structural or accessibility changes required by mobile adaptation and the approved Shorts decision.
- `frontend/morwalpizvideo.client/src/layouts/title-header.tsx`: accessible menu close/focus behavior.
- `frontend/morwalpizvideo.client/src/layouts/title-header.scss`: coarse-pointer target sizes, safe areas, and mobile menu layout.
- `frontend/morwalpizvideo.client/src/layouts/footer.scss`: safe-area and narrow-layout adjustments if browser evidence shows a failure.
- `frontend/morwalpizvideo.client/src/main.scss`: responsive tokens, viewport behavior, and a reduced-motion treatment that preserves state feedback.
- `frontend/morwalpizvideo.client/index.html`: viewport fitting and canonical/social metadata domain.
- `frontend/morwalpizvideo.client/src/utils/seo.tsx`: canonical fallback domain.
- `frontend/morwalpizvideo.client/src/routes/home/Component.test.tsx`: content, accessible markup, and retained destination coverage.
- `frontend/morwalpizvideo.client/src/layouts/title-header.test.tsx`: menu semantics and interaction coverage using the established frontend test stack.
- `frontend/morwalpizvideo.client/DESIGN.md`: replace stale incumbent visual rules with the implemented black/yellow editorial system and mobile behavior.

#### Add

- No production file is required by the recommended design.
- Add a colocated interaction test only if the existing test files cannot cleanly cover menu state and focus behavior.

#### Generated/Migration

- None. Do not edit `dist`, generated service-worker output, archives, or lockfiles for this refinement.

#### Documentation

- `docs/feature-handoffs/new-ui-design-handoff.md`: keep implementation status, actual files changed, validation evidence, and residual risks current.

#### Verify Only

- `frontend/morwalpizvideo.client/src/routes/home/loader.ts`: retain current fallback and revalidation behavior.
- `frontend/morwalpizvideo.client/src/routes/home/loader.test.ts`: retain current data/failure coverage.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx`: confirm shell and main landmark behavior.
- `frontend/morwalpizvideo.client/src/components/SocialLinks.tsx`: confirm configured Discord and fixed channel destinations remain correct.
- `frontend/morwalpizvideo.client/src/layouts/footer.tsx`: confirm all required destinations remain available on mobile.
- `frontend/morwalpizvideo.client/package.json`: use the existing build, lint, formatting, and Vitest runners; do not add a UI framework.

### Impact Matrix

| Area | Impact | Decision |
|---|---|---|
| Backend/API | None expected | Preserve current ServerAPI routes and response shapes. |
| Frontend/UI | Direct | Refine homepage/header/footer responsive behavior without redesigning desktop. |
| Database/Data | Not applicable | No persisted model or migration change. |
| Contracts/Models | None expected | Reuse current match, blog, sponsor, survey, news, and navigation shapes. |
| Authentication/Authorization | Low | Public routes remain public; preserve isolated public account session behavior. |
| Validation | Direct UI validation | Verify URLs, empty data, failed optional sources, failed primary source, and accessible control state. |
| Cache | Not applicable | No public cache tag or mutation behavior changes. |
| Configuration/Secrets | Low | Canonical base may use existing public build/runtime configuration; no secret is introduced. |
| Logging/Telemetry | Low | Preserve page-view tracking; no new telemetry is required unless browser failures need temporary diagnostics. |
| Background Jobs | Not applicable | No background processing. |
| Deployment/Operations | Direct | Rebuild and deploy only the public client after browser proof; do not publish generated output manually. |
| Backward Compatibility | Required | Existing routes, deep links, search parameters, short links, and content destinations must continue to work. |

### Reuse Opportunities

- Reuse `createEditorialFeed`, `filterEditorialFeed`, `getMatchDestination`, and existing content-type rendering in the homepage component.
- Reuse the current loader and `@morwalpizvideo/services`; do not introduce direct fetch calls.
- Reuse the current `SocialLinks`, public navigation provider, account provider, SEO helper, and route shell.
- Reuse CSS custom properties in `main.scss`; extend the current token set instead of scattering additional colors.
- Reuse existing focus-visible, semantic landmarks, image aspect ratios, lazy loading, empty states, retry state, and share disclosure behavior.
- Reuse the current Vitest test files and established React testing dependencies.
- Keep homepage-specific components local until another application needs identical behavior.

### Risks

| Rank | Risk | Cause and affected surface | Mitigation | Verification signal |
|---|---|---|---|---|
| High | False responsive completion | CSS breakpoints exist, but no real-browser/mobile evidence exists. | Require the full viewport, zoom, orientation, and input matrix before acceptance. | Screenshots and interaction results show no overflow, overlap, clipping, or inaccessible control. |
| High | Incorrect canonical identity | Static and generated SEO defaults use `morwalpiz.it` while architecture defines `morwalpiz.com`. | Align both metadata owners and verify SSR output. | Canonical, Open Graph, and social URLs use `morwalpiz.com`. |
| Medium | Mobile tap errors | Multiple targets are 30-40px. | Enforce 44px targets for coarse pointers without bloating desktop density. | Browser inspection and touch testing confirm target dimensions and spacing. |
| Medium | Narrow-card readability | Two columns remain active down to 360px with 10-14px text. | Use one column below 480px and test text zoom. | 320px/375px captures and 200% zoom remain readable without clipping. |
| Medium | Menu accessibility regression | Current disclosure lacks explicit Escape/focus behavior. | Add focused interaction behavior and tests. | Keyboard opens, traverses, closes, and returns focus predictably. |
| Medium | External media/service degradation | YouTube, blob, sponsor, navigation, and article sources can fail independently. | Preserve fallbacks, stable media boxes, and independent degradation. | Failed optional source does not blank primary content or shift layout. |
| Medium | Shorts ownership expansion | Making the fixed promotion dynamic could trigger contract/backend work. | Keep it fixed unless the user approves a separate data-driven slice. | No unapproved API/model changes appear in the implementation. |
| Low | Design-system drift | `DESIGN.md` describes an obsolete visual system. | Reconcile the document after source refinements settle. | Documentation names the same tokens, typography, and breakpoints as source. |
| Low | Data integrity | No write path or persistence change is planned. | Keep scope frontend-only. | No schema, repository, or migration files change. |

### Technical Debt

- **Stale design authority:** `DESIGN.md` conflicts with current implementation. It does not block coding, but it blocks reliable future design decisions. Address in this refinement.
- **Hardcoded Shorts promotion:** Fixed destination and image are isolated debt. It does not block mobile completion. Keep isolated unless product ownership requires dynamic content.
- **Font Awesome dependency:** Current source uses Font Awesome rather than the original Lucide preference. This is not a functional blocker and should not be migrated in this scope.
- **Global reduced-motion kill rule:** It satisfies a basic preference check but removes nearly all transition feedback. Refine only enough to retain useful state communication; do not redesign motion broadly.
- **Legacy light routes:** The root intentionally preserves a separate light shell for non-home routes. Do not fold a site-wide redesign into this homepage task.

### Implementation Roadmap

1. **Freeze the refinement boundary.** Confirm the public homepage, header, footer, static metadata, generated SEO metadata, and design documentation as the only modification surfaces. Preserve all API contracts and desktop content behavior.
2. **Adapt the narrow layout.** Change the content and article grids to one column below 480px, make the donation panel a clear full-width narrow-phone section, and retain proportional media, episode badges, sponsor logos, and filter scrolling.
3. **Normalize touch interaction.** Bring all coarse-pointer controls to at least 44px, provide adequate separation, and constrain hover transforms to hover-capable pointers.
4. **Harden navigation and safe areas.** Add Escape, focus return, route-change closure, and appropriate background behavior for the mobile menu. Enable full-viewport fitting and apply safe-area spacing to the sticky shell and footer without double padding.
5. **Resolve content presentation details.** Preserve all source-backed content. Record and implement the approved fixed or data-driven Shorts decision without silently expanding backend scope.
6. **Align canonical metadata.** Replace legacy `morwalpiz.it` defaults with the architecture-defined `morwalpiz.com` in both static and generated metadata, preserving environment override behavior.
7. **Add focused automated coverage.** Extend homepage and header tests for retained destinations, accessible menu state, close/focus behavior, and relevant empty/failure rendering. Keep loader tests unchanged unless behavior changes.
8. **Run executable frontend gates.** Validate formatting, lint, focused tests, the complete public-client test suite, client build, and SSR build. Shared package builds are required only if a shared package unexpectedly changes.
9. **Run the Impeccable mobile evidence pass.** Use `adapt` for the responsive implementation, `harden` for interaction and edge states, and `polish` only after functional checks pass. Capture desktop and mobile together in one bounded visual pass, fix the observed issues in one batch, then perform at most one confirmation pass.
10. **Reconcile documentation.** Update `DESIGN.md` to current visual truth and append actual changed files, validation results, deployed evidence, and residual risks to this handoff.

Implementation must stop after these completion criteria are met. Site-wide route restyling, shared design-system extraction, dynamic Shorts API work, Font Awesome replacement, and backend model changes require separate approval.

An independent post-validation review is warranted because the change affects the public first viewport, navigation, SEO identity, accessibility, and multiple responsive states. The reviewer should inspect only the accepted boundary and validation evidence.

### Testing Strategy

#### Unit and component

- Keep feed ordering, filtering, type classification, destinations, and collection episode count coverage in the homepage test.
- Add menu open/closed state, accessible name, Escape close, route close, and focus-return scenarios in the header test using the established Vitest/React stack.
- Verify share disclosure remains keyboard operable and closes without losing the trigger.
- Verify optional article/sponsor failure and primary failure remain distinct.

#### Integration and contract

- Keep loader tests for matches, configuration, surveys, news, sponsors, and blog composition.
- Verify no service endpoint, DTO, credentials mode, or public account transport behavior changes.
- Verify search parameters continue to filter locally without unnecessary loader revalidation.

#### Accessibility

- Keyboard-test skip link, menu, search, filters, cards, share menu, sponsor controls, donation, footer, and consent preferences.
- Check semantic landmarks, heading order, `aria-expanded`, `aria-pressed`, status/alert announcements, alt text, focus visibility, and focus order.
- Check WCAG AA contrast and distinguish content types by text/icon as well as color.
- Verify 200% text zoom and reduced-motion behavior.

#### Responsive and browser

- Minimum viewport evidence: 320x568, 375x812, 390x844, 667x375 landscape, 768x1024, 1024x768, and 1440x900.
- Check Chromium and WebKit; include Firefox for layout/keyboard confirmation. Use at least one real iOS Safari and one real Android Chrome device before release when available.
- At each viewport, check page overflow, sticky header, open menu, search popover, hero crop/readability, collection badge, filters, share popover, sponsor controls, footer wrapping, empty state, and failure state.
- Confirm touch targets, safe areas, no hover dependency, and no content overlap at browser text scaling.

#### Performance and SSR

- Verify the first featured image remains prioritized and below-fold images remain lazy with stable aspect ratios.
- Verify no avoidable layout shift is introduced by target-size or safe-area changes.
- Verify client and SSR builds emit matching canonical metadata and the homepage hydrates without warnings.

#### End to end

- From the homepage, exercise video, collection, article, filter, search, sponsor, gear, donation, social, privacy, terms, and consent paths.
- Confirm external destinations use safe new-tab behavior and internal destinations remain client-routed.

### Migration And Rollback

- **MongoDB/SQLite:** No migration is needed because no persisted data changes.
- **API contracts:** No compatibility window is needed for the recommended scope. If dynamic Shorts is approved later, it requires a separate contract and rollout decision.
- **Authentication:** No migration. Public account cookie/CSRF behavior must remain untouched.
- **Cache:** No cache migration or tag eviction is required.
- **Configuration:** Canonical-domain fallback changes are public metadata changes, not secrets. Preserve environment override support and verify production injection.
- **Deployment order:** Build and verify the public client, validate the static/SSR artifact, then deploy the public frontend. No API deployment should be coupled to this refinement.
- **Monitoring:** Watch client errors, hydration warnings, navigation failures, page-view continuity, external image failures, and search/filter behavior after release.
- **Rollback:** Restore the previous public-client artifact if navigation, hydration, canonical output, or mobile layout regresses. Because there is no data migration, rollback is immediate and does not require data recovery.

### Open Questions

1. **Shorts ownership:** Is the Shorts card intentionally a permanent channel promotion, or must its image/title/destination be managed as content? The recommended scope keeps it fixed; a data-driven answer requires separate contract ownership approval. Response: keep it fixed
2. **Production device availability:** Which real iOS and Android devices are available for the final mobile gate? Emulation can provide preliminary evidence but is not sufficient for release acceptance. response: don't bother, i'll care about testing it myself

No other blocking architecture question is identified.

---

## Historical Design Brief (Superseded As Implementation Authority)

The following original brief is retained for visual intent and acceptance-history context. Where it conflicts with the audited sections above or current source, the audited sections above control further implementation.

## 1. Project Objective

Redesign the existing MorWalPiz website into a modern Digital Magazine / Creator Hub.

The website is a content platform where visitors can discover:
- YouTube videos.
- YouTube Shorts.
- Video collections / mini-series.
- Editorial blog articles.
- Recommended accessories and referral products.
- Sponsors and partners.
- Community and donation opportunities.

The homepage must combine videos and articles in a unified editorial feed instead of presenting a traditional chronological list of identical cards.

### Main design goals

1. Create a strong visual identity consistent with the existing black-and-white MorWalPiz logo.
2. Make content discovery engaging and visually interesting.
3. Clearly distinguish videos, collections, articles and promotional content.
4. Give collections a more prominent visual treatment.
5. Maintain easy access to social channels, sponsors, accessories and donations.
6. Create a responsive experience for desktop, tablet and mobile.

The redesign should improve the existing UI without unnecessarily changing backend logic, content models or navigation functionality.

---

## 2. Visual Identity and Design System

### Color palette

The design is based on a predominantly black interface with a distinctive yellow accent.

| Token | Color | Usage |
|---|---|---|
| Primary background | #101010 | Main page background |
| Secondary background | #181818 | Cards and sections |
| Elevated background | #222222 | Hover states and elevated elements |
| Primary accent | #FFFF36 | CTA, highlights, active states |
| Primary text | #FFFFFF | Titles and important text |
| Secondary text | #B8B8B8 | Descriptions and metadata |
| Muted text | #777777 | Secondary metadata |
| Border | #383838 | Card borders and separators |
| Success | #42C77A | Success feedback |
| Error | #F04444 | Error feedback |

Yellow must be used selectively. It is the primary visual identifier of the brand, not a background color for every component.

### Typography

Use a modern sans-serif typeface.

Recommended:
- Inter
- Manrope
- Space Grotesk

Typography hierarchy:

| Element | Desktop size | Weight |
|---|---:|---|
| Hero title | 36–44px | 700–800 |
| Section heading | 24–28px | 700 |
| Card title | 17–20px | 600–700 |
| Body text | 14–16px | 400 |
| Metadata | 12–13px | 400–500 |
| Navigation | 14–15px | 600 |

Use compact typography and generous spacing to create an editorial feeling.

### UI principles

- Dark surfaces with subtle tonal differences.
- Rounded corners, generally 10–14px.
- Thin borders rather than excessive shadows.
- Yellow active states and primary actions.
- Consistent iconography, preferably Lucide.
- Smooth hover transitions (150–250ms).
- Avoid excessive gradients, glassmorphism and unnecessary decorative effects.

---

## 3. Global Layout

### Desktop

Reference viewport: 1440–1536px wide.

- Main content max-width: 1480px.
- Horizontal page padding: 30px.
- Main section spacing: 32–44px.
- Grid gap: 16–20px.

The homepage should use a structured editorial layout.

### Header / Navigation

A horizontal navigation bar at the top of the page.

Elements:
- MorWalPiz logo, aligned left.
- Main navigation centered:
  - Home
  - Videos
  - Series
  - Articles
  - About
- Search action.
- Social shortcuts:
  - Telegram
  - Discord
  - Instagram
  - YouTube

Specifications:
- Height: approximately 76px.
- Background: #101010.
- Bottom border: #383838.
- Logo should use the actual existing SVG/image asset.
- Active navigation item uses #FFFF36.
- Social icons use yellow or neutral monochrome styling.
- Sticky navigation is recommended.

On mobile, replace the desktop navigation with a compact header and hamburger menu.

---

## 4. Homepage Structure

The homepage follows an editorial feed approach.

### Section 1 — Featured Content / Hero

A prominent featured content area immediately below the header.

Layout:
- Two-column structure on desktop.
- Main featured content: approximately 76% width.
- Community sidebar: approximately 24% width.

Main featured card:
- Large photographic thumbnail.
- Aspect ratio approximately 16:9.
- Background image covers the entire card.
- Dark gradient overlay on the left/bottom to improve text readability.
- Yellow content-type badge.
- Large title.
- Short description.
- Primary CTA.
- Optional secondary CTA.

For a collection:
- Show collection episode count in a badge positioned at the top-right.
- Example: "8 episodes".
- CTA: "Explore the series".

For a standard video:
- CTA: "Watch on YouTube".

Sidebar:
1. Follow the journey:
   - Telegram
   - Discord
   - Instagram
   - YouTube
2. Support the channel:
   - Short description.
   - Donation CTA.
   - Yellow outline or yellow-accented background.

The hero should support different content types: video, collection or article.

### Section 2 — Latest Content

This is the most important section of the homepage.

It contains a mixed editorial feed of videos, articles, collections and Shorts.

Section heading:
"Latest Content"

At the top of the section:
- Content type filters: All, Videos, Articles, Series.
- Optional category filters.
- Optional tag filters.
- View all action.

The content feed should not be a simple vertical list.

Use a responsive CSS Grid with visually distinct card types.

Suggested desktop layout:
- 4 or 5 columns depending on viewport width.
- Standard cards occupy one grid column.
- Collections can occupy one or two columns depending on editorial importance.
- Articles can use the same card structure as videos but with an article badge.
- Shorts use a narrower, vertical-oriented presentation.

The grid should support mixed content and different card sizes.

Filters should work without requiring a full page reload when the existing architecture supports client-side filtering.

### Section 3 — Sponsors and Gear

A horizontal section with two distinct areas.

Left side — Sponsors:
- Section title: "Our Sponsors".
- Horizontal carousel.
- Sponsor logos.
- Previous/next controls.
- Logos should preserve their original proportions.
- Hover can reveal the sponsor name or link.

Right side — Gear & Accessories:
- Yellow icon.
- Title: "Gear & Accessories".
- Short descriptive text.
- CTA: "Visit the shop".

Keep this section visually separated from the content grid using a subtle border and dark elevated background.

### Section 4 — Latest Articles

Dedicated editorial section highlighting recent blog articles.

Layout:
- Section title: "Latest Articles".
- View all action.
- Three cards on desktop.
- Image, category, title and short excerpt.
- Publication date.
- CTA to read the article.

Articles can also appear in Latest Content. This section provides additional editorial visibility.

### Section 5 — Community

A visually distinctive community banner.

Content:
- "Join the community".
- Short description.
- Social channel shortcuts.
- Optional decorative diagonal pattern using dark tones and yellow.

### Section 6 — Footer

Include:
- Logo.
- Main navigation.
- Social links.
- Sponsors.
- Referral/accessory links.
- Privacy policy.
- Terms and legal information.
- Copyright information.

---

## 5. Content Card Components

All cards should share a common design language but have different visual identities.

### 5.1 Standard Video Card

Structure:
1. Thumbnail.
2. Video duration overlay in top-right.
3. Video type badge.
4. Title.
5. Short description.
6. Publication date.
7. Share action.

Thumbnail:
- Aspect ratio: 16:9.
- Border radius: 10–12px.
- Object-fit: cover.

Video badge:
- Yellow background.
- Black text.
- Small play icon.
- Label: VIDEO.

The card should be clickable, opening the associated YouTube video.

### 5.2 Collection / Series Card

This is the most important card type and should receive particular attention.

It should have the same overall structure as a standard video card, but with a dedicated collection identity.

Structure:
1. Video thumbnail.
2. Episode count overlay in the top-right.
3. SERIES badge in the top-left.
4. Collection title.
5. Short description.
6. Primary CTA: "Explore the series".

Episode count:
- Dark semi-transparent background.
- White text.
- Small collection/layers icon.
- Positioned inside the thumbnail at top-right.
- Example: "8 episodes".

Series badge:
- Yellow background.
- Black text.
- Label: SERIES.

CTA:
- Yellow background (#FFFF36).
- Black text.
- Full card width where appropriate.
- Rounded corners.
- Arrow icon.

Clicking the card or CTA navigates to the internal collection page, not directly to YouTube.

The card should visually communicate that the user is opening a collection of videos rather than an individual video.

### 5.3 Article Card

Structure:
1. Editorial image.
2. ARTICLE badge.
3. Article title.
4. Short excerpt.
5. Publication date.
6. Optional reading time.

Use the same dark card surface as video cards.

Articles should feel editorial, not like YouTube thumbnails.

Clicking navigates to the internal article page.

### 5.4 YouTube Shorts Card

Shorts are promotional content cards.

Structure:
1. Vertical thumbnail.
2. Shorts badge.
3. Play icon overlay.
4. Short title.

Use a vertical thumbnail ratio (9:16) within a narrower card.

CTA: "Watch on YouTube".

Clicking opens the associated YouTube Shorts URL.

### 5.5 Donation Card

The donation card must not look like regular content.

Use a dedicated highlighted component.

Structure:
- Support icon.
- Title: "Support the channel".
- Short personal message.
- Donation CTA.

Use yellow accents, a subtle border and a darker background.

It can appear as a fixed promotional item in the content feed, but should remain visually distinct from editorial content.

---

## 6. Card Interaction and Hover States

Desktop:
- Slight thumbnail zoom on hover (scale 1.02–1.04).
- Border becomes slightly brighter.
- CTA changes brightness.
- Transition duration: 200ms.
- No excessive movement.

Mobile:
- No hover-dependent information.
- All important content must remain visible.
- Entire card should have a sufficiently large clickable area.

Share functionality:
- Use a compact share icon rather than displaying Facebook and WhatsApp buttons permanently.
- On activation, expose available sharing options.
- Preserve existing sharing functionality.

---

## 7. Responsive Design

### Desktop: 1200px and above
- Full navigation.
- Two-column hero.
- Multi-column content grid.
- Sponsors and gear side by side.
- Three-column article section.

### Tablet: 768–1199px
- Compact navigation.
- Hero sidebar can move below the hero.
- Two or three content columns.
- Sponsors and gear can stack or become horizontally scrollable.
- Reduced horizontal spacing.

### Mobile: below 768px
- Compact header.
- Single-column hero.
- Social links in a compact row.
- Single-column or two-column content grid depending on available width.
- Collection cards retain their episode badge.
- Article cards remain readable.
- Horizontal sponsor carousel.
- No horizontal page overflow.
- Filters should be horizontally scrollable.
- Donation card should be a dedicated section rather than competing with the feed.

Recommended breakpoints:
- Mobile: 0–767px.
- Tablet: 768–1199px.
- Desktop: 1200px and above.

---

## 8. Technical Implementation Guidelines

Assume the existing frontend architecture and preserve its current routing, API contracts and content retrieval mechanisms.

Recommended implementation approach:
- Reuse existing React and TypeScript components where possible.
- Use Tailwind CSS for styling if already configured.
- Create reusable components for cards and editorial sections.
- Avoid duplicating markup for different content types.
- Use data-driven rendering.
- Preserve existing API integrations and URL structures.
- Do not hardcode content or sponsor information in the UI.
- Do not replace real content with mock data in production.

Suggested component hierarchy:

```text
HomePage
├── SiteHeader
├── FeaturedContent
│   ├── FeaturedContentCard
│   └── CommunitySidebar
│       ├── SocialLinks
│       └── DonationCard
├── LatestContentSection
│   ├── ContentFilters
│   └── ContentGrid
│       ├── VideoCard
│       ├── CollectionCard
│       ├── ArticleCard
│       ├── ShortsCard
│       └── DonationCard
├── SponsorsAndGearSection
│   ├── SponsorsCarousel
│   └── GearReferralBanner
├── LatestArticlesSection
├── CommunityBanner
└── SiteFooter
```

### Content model

The UI should be driven by a common content representation.

Conceptual TypeScript model:

```typescript
type ContentType =
  | "video"
  | "collection"
  | "article"
  | "shorts"
  | "donation";

interface ContentItem {
  id: string;
  type: ContentType;
  title: string;
  description: string;
  thumbnailUrl?: string;
  publishedAt?: string;
  categories?: string[];
  tags?: string[];
  externalUrl?: string;
  internalUrl?: string;
  duration?: string;
  episodeCount?: number;
  featured?: boolean;
}
```

This is a conceptual UI model, not a requirement to change the existing backend data model.

Use a component renderer based on content type:

```tsx
function ContentCard({ item }: { item: ContentItem }) {
  switch (item.type) {
    case "video":
      return <VideoCard item={item} />;

    case "collection":
      return <CollectionCard item={item} />;

    case "article":
      return <ArticleCard item={item} />;

    case "shorts":
      return <ShortsCard item={item} />;

    case "donation":
      return <DonationCard item={item} />;

    default:
      return null;
  }
}
```

Adapt the model and renderer to the existing implementation rather than introducing a parallel content system.

---

## 9. Accessibility and Performance

Accessibility:
- Maintain WCAG AA contrast wherever possible.
- Yellow (#FFFF36) on black is the primary accent combination.
- Use semantic HTML.
- Provide meaningful image alt text.
- Ensure keyboard navigation.
- Provide visible focus indicators.
- Buttons must have accessible labels.
- Do not rely exclusively on color to communicate content type.

Performance:
- Lazy-load content thumbnails below the fold.
- Use appropriately sized responsive images.
- Avoid loading all carousel content immediately.
- Prevent layout shifts by defining image aspect ratios.
- Optimize image formats (WebP/AVIF where supported).
- Respect reduced-motion preferences.
- Keep animations lightweight.

---

## 10. Implementation Priorities

Implement in the following order:

**Phase 1 — Design foundation**
- Color tokens.
- Typography.
- Spacing system.
- Buttons and badges.
- Global header.

**Phase 2 — Content components**
- Standard video card.
- Collection card.
- Article card.
- Shorts card.
- Donation card.

**Phase 3 — Homepage**
- Featured hero.
- Latest Content mixed grid.
- Filters.
- Sponsors and Gear.
- Latest Articles.
- Community banner.
- Footer.

**Phase 4 — Responsive optimization**
- Tablet layout.
- Mobile layout.
- Touch interactions.
- Navigation menu.

**Phase 5 — Final polish**
- Hover states.
- Transitions.
- Loading states.
- Empty states.
- Accessibility.
- Performance.

---

## 11. Acceptance Criteria

The redesign is considered complete when:

- The homepage has a clear editorial hierarchy.
- Videos and articles coexist in the same content feed.
- Collections are immediately distinguishable from standard videos.
- Collection episode counts appear in the top-right of thumbnails.
- Clicking a collection opens its internal collection page.
- Clicking a video or Short opens the appropriate YouTube URL.
- Clicking an article opens its internal article page.
- Existing sharing functionality is preserved.
- Category and tag filters continue to work.
- Sponsors and referral links remain accessible.
- Donation functionality is preserved.
- The website is fully responsive.
- Existing backend functionality and content management are not broken.
- The final UI follows the black / #FFFF36 design system.

**Important:** Treat the supplied visual reference as a design direction, not a pixel-perfect specification. Preserve the actual MorWalPiz logo, real sponsor assets, real content thumbnails and existing application functionality. Do not reproduce fictional sponsor logos or generated imagery from the mockup.