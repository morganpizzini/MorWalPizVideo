# Public experience

Consolidated 2026-11-09 from homepage, site-wide layout, article/consent,
social-preview, gallery, and integrated remediation handoffs. Results below are
**historical local records**, not fresh verification or deployment approval.
See the [filename map](../feature-handoffs/README.md#former-handoff-map).

## Homepage and shared shell

The original homepage brief is historical; the recorded 2026-11-04 refinement
and later site-wide alignment supersede it.

- Retain black/yellow magazine identity, actual assets/content, mixed date-sorted
  video/article/series feed, filters, internal article/series links, outbound
  video/Shorts destinations, episode badges, sharing, sponsors/gear/community/
  donations. Use incumbent Sass/Bootstrap; no Tailwind. Shorts promotion is
  fixed content, not a new managed-content/API feature.
- Below 480px use one column; coarse-pointer targets are 44px. Restrict hover to
  fine pointers. Menu needs first focus/trap/Escape/route-close/focus return;
  shell/footer respect safe areas and viewport-fit=cover.
- No overflow from 320px, readable at 200% zoom, prioritized first image and
  stable lazy below-fold images. Preserve empty/retry/failure behavior and
  independent degradation of optional articles/sponsors.
- All registered pages, including gallery, quick-links, channel-news,
  compilation, custom forms, and survey, share Root header/footer/providers
  without URL/loader/action/error behavior changes. Remove compilation's
  duplicate header. Shell is not a main landmark: one page-level main and
  usable skip target; shared QuickLinksRenderer ownership stays unchanged.
- Initial shell work styled account pages without activating them; subsequent
  [account delivery](accounts-and-channels.md) supersedes that inactive status.
  Later overlay work aligned consent/push styling without changing behavior.

Owners: [public routes](../../frontend/morwalpizvideo.client/src/routes/),
[layouts](../../frontend/morwalpizvideo.client/src/layouts/),
[main.scss](../../frontend/morwalpizvideo.client/src/main.scss),
and [DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md).

Evidence: homepage **11 focused tests**, focused TypeScript, four-file full client
suite (total not recorded), and client/SSR build passed. Site-wide **8 files/23
tests** and client/SSR build passed; detector retained one intentional section
marker. Overlay build/detector passed. Full lint remained blocked by unrelated
Bluetooth/API-key/generated-output baseline; formatting baseline had 61 files.
No browser qualification was recorded. User accepted responsibility for device
testing; this is not passing evidence.

Visual acceptance matrix: 320x568, 375x812, 390x844, 667x375, 768x1024, 1024x768,
1440x900; touch, zoom, safe areas, route families, deep links, landmarks, and
functional checks. Use Impeccable adapt/audit as a refinement of the incumbent
Read/Persuade surfaces, not a redesign.

## Consent and article reading layout

Recorded delivered 2026-11-07:

- Columnar mobile banner clears horizontal flex-basis, has max 45dvh/45vh
  fallback, internal scrolling, safe-area spacing, and 44px actions.
  Suggested first-banner <=40% and preferences <=50% of 360x800/390x844 remain
  **unmeasured targets**, not approved outcomes. At landscape/200% zoom,
  reachable controls and partial page visibility take priority over ratios.
- Article header, cover, and body align to 75ch; preserve natural cover ratio,
  no recrop. Cover-width acceptance is within 2px at 1440px.
  Remove visible lead summary at every width, retaining summary in list,
  payload, SEO, and social metadata. Public comment label is "Commenti";
  approved filtering/count/moderation do not change.

Owners: [analytics](../../frontend/morwalpizvideo.client/src/analytics/) and
[blog route](../../frontend/morwalpizvideo.client/src/routes/blog/).
Five combined article/interaction tests and client/SSR build passed; no
bounding-box/screenshot evidence. Check 360/390/768/1440 widths, 667x375
landscape, 200% zoom, long content, safe areas, and updated PWA.
Visual rollback must not clear consent, increment policy versions, or force
new consent.

## Initial HTML and social previews

Recorded SSR delivery 2026-11-07, not deployed:

- Anonymous no-JavaScript HTML contains article body and unique head title,
  canonical, Open Graph, and Twitter metadata.
  Canonical/share is `https://morwalpiz.com/blog/:slug`; social values use
  editorial title/summary/cover, independently of ordinary SEO overrides.
  No cover uses the declared logo fallback, **not positive thumbnail acceptance**;
  empty summary has no invented description.
- Explicit canonical origin/SSR detection, no window shim or arbitrary forwarded
  host. Remove generic dynamic duplicates but preserve invariant metadata.
  React 19 hoisted Helmet tags belong in head; missing/unpublished returns 404,
  fetch/render failures 5xx, not a success shell.
  Preserve JSON-LD/escaping and concurrent-request isolation.

Owners: [seo.tsx](../../frontend/morwalpizvideo.client/src/utils/seo.tsx),
[entry-server.tsx](../../frontend/morwalpizvideo.client/src/entry-server.tsx),
and [server.mjs](../../frontend/morwalpizvideo.client/server.mjs).
Prepatch observation reported 200 Express SSR with duplicate generic metadata
and localhost canonical. It resolved the immediate static-versus-SSR coding
question, **not** Aruba/Azure operational ownership or postdeployment behavior.
No hosting migration is authorized.

Five focused tests passed; the integration record supplies the later passing
client/SSR build. Raw-HTTP mock-server checks, concurrent slugs, deployed
head/canonical/image, hydration, and operator-approved social rescan remain.
Roll back coupled client/SSR artifacts; social/CDN cache may outlive either.

## Galleries

### Build corrections

Recorded corrections preserve API/request/UI/persistence: export existing
getGallery, normalize optional raw/enveloped loader data, capture defined upload
ID, initialize form useRef, and narrow unknown errors.
Owners: [admin galleries](../../frontend/back-office-spa/src/routes/galleries/)
and [shared services](../../frontend/fe-packages/services/src/).
Direct local TypeScript compilation exited zero on 2026-10-21; an earlier npx
attempt was intercepted. New envelope-list Vitest regression stalled without
result; no completed runtime pass is claimed.

### Direct route, downloads, and lifecycle

- `/gallery/:slug` resolves the configured channel; missing
  VITE_MORWALPIZ_CHANNEL_ID is unavailable. The older outside-Root requirement
  is superseded by shared-shell delivery, not a URL change.
- Selectable grid/list, independent Select all/Deselect all, two-column
  filename/actions list, original filenames for single downloads, gallery ZIP
  name for bulk, per-image download, explicit modal close, accessible controls
  >=44px, and viewport/dynamic-height-constrained scrolling.
- Originals retain bytes/dimensions and are download-only.
  Optional oriented JPEG previews preserve aspect, no upscale, bounded
  1920x1080 landscape/1080x1920 portrait.
  Public/admin displays use previews; single/ZIP downloads use originals.
  Legacy absent preview falls back to original.
- Upload one file/request sequentially with processing/completed/failure/retry
  state, not invented percentages. Compensate original and preview on aggregate
  failure; delete both on individual, whole-gallery, and expiry cleanup.
- Creation uses editable GUID-prefilled normalized channel-unique slug and
  explicit conflict. Controller checks alone do not ensure concurrent uniqueness.
  Public 404/missing slug says the gallery expired or is unavailable; preserve
  other errors. Admin menu follows Images and reuses image permissions.

Owners: [public gallery route](../../frontend/morwalpizvideo.client/src/routes/gallery/),
both API controller families, shared gallery models/contracts/services, admin
galleries, media processor, and cleanup job.

Evidence, separate selections: repeated public route runs **1 passed** and
builds passed; ChannelNews/Gallery backend **13 passed**, both API Release builds
passed; admin menu completed without a reported count. Later scoped-header patch
had clean diagnostics but no executable test; final loader **2 passed** and public
build passed. Preview GalleryBehaviorTests passed with no count; shared build
reached models/services, touched TypeScript diagnostics clean, but combined app
build had no completion status.

Open: actual batch-failure cause; test three images around 20 MB total and a
single >20 MB where allowed. **No 20 MB cap was approved.** Establish bytes/pixel
limits, proxy limits, Blob display permissions, preview/ZIP memory budgets,
duplicate audit/approved unique index, and real browser downloads/lightbox.
Preview backfill requires explicit ownership, permissions, and a resumable plan.
Backend optional fields first, clients after; rollback retains originals, new
fields, and legacy fallback. No SQLite migration.

## Integrated November remediation evidence

The former public-client/admin umbrella record coordinated six bounded modules,
not a redesign or general refactor. Do not add these overlapping counts:

| Module | Strongest recorded local evidence | Still open |
| --- | --- | --- |
| Account session/navigation | Initial 3 service + 1 static UI test; later account record has stronger November 8 proof | Real-origin credentialed login/session |
| Consent/article and social metadata | 5 article/interaction tests and integrated client/SSR build | Geometry and deployed raw HTML |
| Channel socials/terminology | Release test build and 4/4 terminology/boundary tests | Real profiles/cache and corrupt BSON |
| Explicit cover upload | 25/25 form/cropper tests after jsdom repair, unchanged manifests/lockfile | Browser network/touch behavior |

See [account/channel details](accounts-and-channels.md) and
[authoring details](blog-authoring.md). Anonymous CSRF 200 supersedes an older
401 observation but proves no credentialed session. Public build closed a stale
shared-export failure, not every consumer gate. Shop stayed frozen; verify-only
consumers were not implicitly changed. No deployment or postdeployment check was
performed. Ordinary bundle rollback cannot undo accounts, publication, or uploads.
