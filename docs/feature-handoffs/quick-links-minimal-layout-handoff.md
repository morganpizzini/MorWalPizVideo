# Quick Links minimal layout handoff

Status: Implemented; focused tests and builds passed. Browser captures were not
run in this turn.

Scope updated: automatic kind-specific icons and an explicit YouTube kind
are included alongside the standalone layout.

## Outcome

`/quick-links/:url` now renders as a standalone Linktree-style page without
site navigation, full site header, footer, or push opt-in. It shows the
configured page title, optional subtitle, and ordered links in a narrow,
responsive column. The current dark/yellow identity and configured link content
are preserved. This is a focused refinement, not a redesign.

Known platform kinds should supply recognizable default icons without manual
icon entry. Add a distinct YouTube choice while preserving generic Video
and all existing kind values. Explicit images/custom icons remain overrides.

Assumption: the requested removal concerns the surrounding site shell, not
the content header containing the configured page title.

## Current Evidence

Paths below are relative to the repository root.

- `frontend/morwalpizvideo.client/src/routes/quickLinks/Component.tsx` only
  supplies metadata and delegates content to the shared `QuickLinksRenderer`.
- `frontend/morwalpizvideo.client/src/routes/config.tsx` now defines
  `quick-links/:url` as a top-level sibling of `Root`.
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx` renders
  `TitleComponent`, `.site-content`, `Footer`, and `PushOptIn`, together with
  navigation, account, and social providers.
- `frontend/morwalpizvideo.client/src/routes/gallery-route.test.tsx` now
  asserts Quick Links has no `Root` ancestor and the other utility routes
  retain their shell.
- `frontend/fe-packages/layout/src/components/QuickLinksRenderer.tsx`
  already renders one main landmark, title/subtitle, and ordered anchors.
  Anchors retain `_blank` and `noopener noreferrer`.
- `frontend/morwalpizvideo.client/src/routes/quickLinks/style.scss` supplies
  viewport-height coverage, independent responsive gutters, and the same
  centered 40rem content width.
- `frontend/morwalpizvideo.client/src/main.scss` owns globally available
  theme tokens, focus styling, reduced-motion behavior, and the flex root.
- The loader and `src/services/quickLinks.ts` already fetch the anonymous
  public page through the established shared transport.
- `src/main.tsx` and `src/entry-server.tsx` consume the same route
  configuration. Both render `BuyMeWidget` outside the router; the client
  also renders `ConsentBanner` globally.
- `src/utils/buy-me-widget.tsx` injects a script without lifecycle cleanup.
  `public/js/buy-me-coffee-widget.js` appends controls directly to the body
  and registers a global event listener. Moving only the React component
  does not prove that the floating controls disappear on navigation.
- `LINKTREE_IMPLEMENTATION.md` described a standalone route and has been
  reconciled with the implemented route and supported kinds.
- `frontend/morwalpizvideo.client/vitest.config.ts` uses a Node environment.
  Existing route and shell tests use route matching and server-rendered markup.
- `frontend/fe-packages/models/src/quickLinks.ts` and
  `MorWalPizVideo.Models/Models/QuickLinks.cs` now define YouTube=5 while
  preserving External=0, Telegram=1, Instagram=2, Facebook=3, and Video=4.
- `QuickLinksRenderer` now renders kind-based brand icons for Telegram,
  Instagram, Facebook, and YouTube, with Lucide icons for External and Video.
  Configured images/icons override the default; configured image and icon
  continue to render together.
- `frontend/back-office-spa/src/routes/quickLinks/form/Component.tsx` derives
  its kind options from the shared TypeScript object, provides an accessible
  kind label/description and previews the selected default icon. It continues
  to submit complete link objects through the hidden JSON field without
  exposing a new custom-icon field.
- `MorWalPizVideo.Domain/QuickLinksService.cs` rejects undefined kinds and
  applies platform host validation. Video currently accepts youtube.com,
  youtu.be, and vimeo.com, including their subdomains.
- `MorWalPiz.Contracts/Contracts/QuickLinksContract.cs` reuses the Models
  enum; `MorWalPiz.Contracts/ContractUtils.cs` owns public/admin projections.
  Both API project manifests reference this contracts project, not the
  similarly named `MorWalPizVideo.Contracts` directory.
- `MorWalPizVideo.BackOffice/Controllers/QuickLinksController.cs` delegates
  create/update validation to that service, requires channel scope, and
  invalidates the existing QuickLinks cache after mutations.
- `MorWalPizVideo.ServerAPI/Controllers/QuickLinksController.cs` returns
  anonymous public projections with the existing QuickLinks cache tag.
- Both the public client and Shooting ITA consume `QuickLinksRenderer`.
  Shooting ITA has an existing renderer test at
  `frontend/shooting-ita-frontend/src/__tests__/QuickLinksRenderer.test.tsx`.
- The shared layout uses Lucide plus inline Font Awesome SVG brand definitions;
  neither public consumer relies on an app-specific Font Awesome stylesheet.

## Implemented Design

1. Moved Quick Links to a top-level sibling of the `/` shell route. The URL,
   loader, component, and metadata are unchanged; `Root` has no visibility flags.
2. Give the standalone route the existing `ErrorPage` as its error boundary.
   Loader failures must remain explicit and must not restore the site shell.
3. Retained the existing `QuickLinksRenderer` ownership, page title, subtitle,
   content ordering, optional images/icons, and safe anchor behavior.
4. Adjusted only app-local Quick Links styles to supply viewport-height
   coverage and responsive horizontal gutters independently of
   `.site-content`. Retain the current centered maximum width, typography,
   theme tokens, focus indication, and normal vertical scrolling.
5. Preserved global consent and support-widget behavior. The user requested
   removal of the site shell; these controls are mounted outside `Root`.

### Kind and icon behavior

- Append `YouTube=5` to the C# enum and TypeScript kind object. Do not
   rename, renumber, remove, or repurpose Video=4.
- Extend the existing service host switch: YouTube accepts youtube.com
   and youtu.be, including subdomains under the existing host-boundary rule.
   Vimeo remains allowed for Video but not YouTube. Keep the existing
   HTTP/HTTPS and no-user-info rules; do not broaden them.
- Do not infer kinds from URLs, rewrite existing records, embed players,
   or add provider lookups. A saved Video link remains Video even if its
   URL happens to be YouTube.
- Generate default Telegram, Instagram, Facebook, and YouTube brand icons.
   Use the existing Lucide dependency for generic External/link and
   Video/play icons. Add `@fortawesome/free-brands-svg-icons` to the layout
   package for the four brand definitions and render them as inline SVG.
   This small dependency is intentional: it supplies maintained brand
   shapes without a new CDN/font requirement or hand-drawn approximations.
   Preserve its license notices and import only the needed definitions.
- Preserve existing image/custom-icon behavior: if either is nonblank,
   render the existing explicit presentation without adding an automatic
   icon. If both are present, preserve both. Otherwise render exactly one
   kind-derived icon. Whitespace-only custom values do not suppress defaults.
- Keep icons decorative and hidden from assistive technology; the existing
   text remains the accessible destination name. Never interpret stored icon
   text as HTML or arbitrary SVG.
- Extend fallback text to YouTube. For an unknown numeric kind from a newer
   server, retain the renderer's existing external-link text fallback and
   use the generic link icon. Server writes still reject undefined kinds.
- The BackOffice selector will receive YouTube from the shared kind object.
   Add concise help explaining automatic icons and a small decorative preview
   of the selected kind using the same shared icon resolver. Export that
   focused presentation helper from the layout package; do not duplicate
   the mapping in the form or embed a whole public-page preview.

Impeccable implementation lens: target the public Quick Links surface in
**Operate** mode: the visitor chooses a provided destination. Use the
installed `distill` refinement workflow, preserving the incumbent
`frontend/morwalpizvideo.client/DESIGN.md`. No app-local `PRODUCT.md` was
found. Minimum visual evidence is one desktop and one narrow-mobile capture
of the same configured page, plus keyboard focus and overflow checks.
Do not create a replacement design system or change factual content.

The BackOffice form retains its Bootstrap conventions and now previews the
selected kind with the shared icon resolver; it does not duplicate icon
mapping or render a separate public-page preview.

## Impacted Projects

- `frontend/morwalpizvideo.client`: routing, local spacing, and regression tests.
- `frontend/fe-packages/layout`: shared icon rendering and reusable icon helper.
- `frontend/fe-packages/models`: additive YouTube kind.
- `frontend/back-office-spa`: selector/help/icon preview and form regression tests.
- `frontend/shooting-ita-frontend`: renderer tests and theme verification;
  receives automatic icons through the shared package.
- `MorWalPizVideo.Models`: additive enum member.
- `MorWalPizVideo.Domain`: YouTube-specific host validation.
- `MorWalPiz.Contracts`: verify enum projections; no new contract fields.
- `MorWalPizVideo.BackOffice`, `MorWalPizVideo.ServerAPI`: rebuild/release and
  API round-trip verification, preserving their existing controllers.
- `MorWalPizVideo.BackOffice.Tests`: validation and API regression coverage.
- Shared services and other frontend model/layout consumers: compatibility
  verification only; no transport or unrelated UI changes.
- Standalone tools and deployment infrastructure: no changes required.

## Impacted Files

### Modify

- `frontend/morwalpizvideo.client/src/routes/config.tsx`: detach only Quick Links
  and attach its standalone error boundary.
- `frontend/morwalpizvideo.client/src/routes/quickLinks/style.scss`: independent
  gutters and viewport-height presentation.
- `frontend/morwalpizvideo.client/src/routes/gallery-route.test.tsx`: preserve
  shell assertions for all other listed routes; remove Quick Links from that
  shared-shell expectation.
- `frontend/back-office-spa/src/index.css`: style the compact selected-kind
  icon preview.
- `MorWalPizVideo.Models/Models/QuickLinks.cs`: append the enum member.
- `MorWalPizVideo.Domain/QuickLinksService.cs`: YouTube host validation.
- `frontend/fe-packages/models/src/quickLinks.ts`: matching kind value.
- `frontend/fe-packages/layout/src/components/QuickLinksRenderer.tsx`:
  use the shared default-icon helper and add YouTube fallback text.
- `frontend/fe-packages/layout/src/index.ts`: export the focused icon helper.
- `frontend/fe-packages/layout/package.json`: declare the brand-icon dependency.
- `frontend/yarn.lock`: resolve the shared brand-icon dependency.
- `frontend/back-office-spa/src/routes/quickLinks/form/Component.tsx`:
  kind help and selected-kind preview, with associated field labels.
- `frontend/back-office-spa/src/routes/quickLinks/form/Component.test.tsx`:
  YouTube selection, preview, serialized kind, and preserved URL focus.
- `frontend/shooting-ita-frontend/src/__tests__/QuickLinksRenderer.test.tsx`:
  shared icon matrix and override behavior.
- `MorWalPizVideo.BackOffice.Tests/Features/AdminEditWorkflowTests.cs`:
  verify the BackOffice create/read contract persists the new kind.
- `MorWalPizVideo.BackOffice.Tests/Services/QuickLinksServiceTests.cs`:
  verify host validation, stable numeric assignments, and BSON round-trip.

### Add

- A route regression test beside
  `frontend/morwalpizvideo.client/src/routes/gallery-route.test.tsx` for
  standalone Quick Links matching.
- A focused icon helper under `frontend/fe-packages/layout/src/components/`,
  following existing component naming; owns the only kind-to-icon mapping.
- Focused service validation tests under
  `MorWalPizVideo.BackOffice.Tests/`, following its existing unit-test naming.

### Generated/Migration

- `frontend/yarn.lock` was updated through Yarn for the brand-icon dependency.
- No database migration or generated source changes.

### Documentation

- This handoff: update with actual files, validation results, and remaining risks.
- `LINKTREE_IMPLEMENTATION.md`: reconcile standalone behavior with delivered code.
  Also record supported kinds, icon precedence, and additive compatibility.
- `docs/architecture/frontend.md`: records Quick Links as an exception to
  the public content shell.
- `docs/architecture/backend.md`: records numeric QuickLinks kind compatibility.

### Verify Only

- `frontend/morwalpizvideo.client/src/routes/quickLinks/Component.tsx`
- `frontend/morwalpizvideo.client/src/routes/quickLinks/loader.ts`
- `frontend/morwalpizvideo.client/src/services/quickLinks.ts`
- `frontend/morwalpizvideo.client/src/routes/layout/root.tsx`
- `frontend/morwalpizvideo.client/src/routes/layout/root.test.tsx`
- `frontend/morwalpizvideo.client/src/error-page.tsx`
- `frontend/morwalpizvideo.client/src/main.scss`
- `frontend/morwalpizvideo.client/src/main.tsx`
- `frontend/morwalpizvideo.client/src/entry-server.tsx`
- `MorWalPiz.Contracts/Contracts/QuickLinksContract.cs`
- `MorWalPiz.Contracts/ContractUtils.cs`
- `MorWalPizVideo.BackOffice/Controllers/QuickLinksController.cs`
- `MorWalPizVideo.ServerAPI/Controllers/QuickLinksController.cs`
- `frontend/back-office-spa/src/routes/quickLinks/form/action.ts`
- `frontend/shooting-ita-frontend/src/routes/quickLink/Component.tsx`
- `frontend/shooting-ita-frontend/src/styles/quick-links.scss`
- `frontend/package.json`

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | Existing endpoints accept/persist the additive kind; host validation extended. |
| Frontend/UI | Standalone page, automatic icons in both public consumers, editor preview/help. |
| Database/Data | Additive enum value only; no rewrite or backfill. BSON round-trip test passes. |
| Contracts/Models | YouTube=5 appended; existing numeric values/DTO fields unchanged. |
| Authentication/Authorization | Remains anonymous; no new guard or account dependency. |
| Validation | Existing loader failures preserved; YouTube host allowlist and negative cases added. |
| Cache | No API tag, eviction, or cache-policy changes. |
| Configuration/Secrets | Layout dependency/lockfile only; no runtime configuration or secrets. |
| Logging/Telemetry | Preserve existing error and consent behavior; no new telemetry. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Deploy updated readers/backends before the editor exposes new-kind writes. |
| Backward Compatibility | Preserve 0-4 and Video semantics; old readers may use generic fallback for 5. |

## Reuse Opportunities

Reuse `QuickLinksRenderer`, the existing loader/service, `ErrorPage`, shared
route configuration, global theme/focus tokens, and existing Vitest
route-matching/server-rendering patterns. Reuse existing service URL validation,
the shared enum contract, BackOffice form fixtures, and Shooting ITA renderer
tests. Keep one shared kind/icon resolver; do not copy it between applications.
Do not introduce another transport, layout package, or shell abstraction.

## Risks

- **Low — global controls remain:** support widget and consent banner are
  outside `Root` and remain visible. Confirm separately if those must also
  be hidden on the Quick Links page.
- **Medium — older backend compatibility:** deployments that predate kind 5
  reject YouTube writes. Deploy updated backends before enabling it in BackOffice.
- **Low — viewport verification:** builds and route tests pass, but desktop
  and narrow-mobile browser captures were not available in this run.
- Cache tags, external network behavior, secrets, and auth are unchanged.

## Technical Debt

- The global support widget has external DOM/listener ownership without React
  cleanup. Defer unless its removal is approved; do not assume relocating its
  component solves client-side navigation or delayed script initialization.
- Other public utility pages deliberately retain the shell in current tests.
  Do not fold a wider utility-route redesign into this change.
- Older backend deployments do not accept kind 5. Update backend readers and
  validators before enabling YouTube links in the admin editor.

## Implementation Record

1. Moved Quick Links outside `Root`, added standalone spacing, and changed
   route expectations while preserving other utility routes.
2. Added `YouTube=5`, host validation, shared kind icon rendering, and the
   admin preview/help; left Video=4 and stored records unchanged.
3. Added form/render/API/BSON regressions. Focused tests and builds below pass.
4. Updated Quick Links feature documentation and the frontend/backend
   architecture guides.
5. Deploy backend validation and public/shared renderer changes before the
   BackOffice editor exposes kind 5. No backfill, new DTO field, provider call,
   or new telemetry is needed.

## Validation Performed

- `yarn --cwd frontend build:shared` — passed; models, services, and layout
  built in dependency order.
- `yarn --cwd frontend/back-office-spa vitest run src/routes/quickLinks/form/Component.test.tsx`
  — passed (2 tests), including YouTube icon preview/payload and URL focus.
- `yarn --cwd frontend/shooting-ita-frontend vitest run src/__tests__/QuickLinksRenderer.test.tsx`
  — passed (2 tests), covering all kinds and custom media precedence.
- `yarn --cwd frontend/morwalpizvideo.client vitest run src/routes/gallery-route.test.tsx src/routes/layout/root.test.tsx`
  — passed (3 tests); standalone route and existing shell assertions.
- `yarn --cwd frontend/back-office-spa build` — passed.
- `yarn --cwd frontend/morwalpizvideo.client build` and `build:ssr` — passed.
- `yarn --cwd frontend/shooting-ita-frontend build` — passed.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
  — passed. Existing ImageSharp vulnerability advisories appeared as build warnings.
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~QuickLinksServiceTests|FullyQualifiedName~QuickLinks_create_persists_and_returns_the_additive_youtube_kind"`
  — passed (15 tests), including enum assignments, BSON round-trip, host validation,
  and API create/read.
- Impeccable detector on changed UI targets — passed with no findings.
- Desktop/mobile screenshot, browser navigation, and visual overflow checks
  were not run; browser capture tooling was unavailable. These remain unverified.

## Migration And Rollback

No document rewrite, SQLite migration, new contract fields, auth/config
transition, or cache-tag change is required. The MongoDB change is an additive
enum value with explicit existing-value preservation and serialization proof.
No existing Video records are automatically converted to YouTube.

Release updated public readers/shared renderer and enum-capable APIs before
the admin editor offers new-kind writes. These must be independently deployable;
publish the editor last, after both APIs accept and return kind 5.
Use existing mutation cache invalidation; no mass cache purge is needed for
old records, whose generated icons are frontend presentation.

UI rollback may restore the old route/styles while retaining enum support.
Once kind 5 has been persisted, do not roll the backend validator back to an
unsupported version: older backends would reject edits containing that kind.
If a complete rollback is unavoidable, first stop new YouTube writes and obtain
approval for a controlled conversion to compatible Video=4 records. Preserve
record identity/order/URLs/metadata and record the affected IDs for recovery;
conversion is not part of this implementation. Monitor save errors, public
loader failures, and icon rendering after release.

## Open Questions

- Should the floating support widget also be absent? Recommended for a
  strictly links-only experience, but it requires a separately bounded,
  navigation-safe lifecycle change because it lives outside the site shell.
  Consent controls should remain available either way.
