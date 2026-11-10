# BackOffice Admin Dashboard

## Status

Current implementation for the BackOffice SPA. The dashboard uses the existing unversioned `/api/...` convention and does not add a MongoDB collection.

### Insight-to-article workflow

Users with both insight viewing and Pages create/manage permissions can select **Create
article** from an insight content plan. The modal supports a direct editor prefill, an AI
first draft, expanded context, or additional editor information. The authenticated
`POST /api/insights/article-drafts` endpoint validates the channel-scoped topic and plan,
uses the existing insight agent for AI modes, and returns a typed unsaved draft. The SPA
passes that result into the existing Blog editor; it never creates or publishes a Blog
post until the user explicitly saves it.

### Editorial production cards

The BackOffice SPA exposes the protected `/editorialproductioncards` route
for channel-scoped production tracking. Cards are persisted by the BackOffice
API with explicit revision checks and server-owned status transitions; saving
a card never uploads or publishes a video. Optional YouTube Studio and Google
Analytics URLs belong to the selected channel and are rendered as safe
external links in the sidebar when configured. The public client continues to
own consent-aware analytics, while the BackOffice does not proxy or store
private provider reports.

### Blog article prettify

The Blog create/edit editor exposes an authenticated **Prettify article** action. It
uses the existing channel-scoped Script Studio generation endpoint with the dedicated
`prettify` operation, so the existing permission, quota, audit, provider, and
cancellation behavior remains shared. The operation validates non-empty, bounded
content and asks the provider to preserve Markdown, HTML, placeholders, URLs,
code-like tokens, and meaning. The response is applied only to the current unsaved
editor draft after a stale-source check; it never saves or publishes automatically.

## Authorization

`backoffice.access` grants access to the administrative shell and dashboard. Module permissions are lowercase and use the resource-operation form: `<resource>.view`, `<resource>.create`, `<resource>.update`, `<resource>.delete`, and `<resource>.manage`.

The backend expands each declared `<resource>.manage` parent to its explicitly reviewed sibling capabilities. Specialized implications are `users.permissions.manage`, `videos.import`, `videos.translate`, `videos.publish`, `forms.responses.view`, and `insights.scan`. `images.manage` has no update leaf, and `diagnostics.view` remains standalone. Expansion is directional; a leaf does not imply its parent or siblings.

`backoffice.manageall` is the evaluator and frontend administrator override. Its effective-permission expansion adds only `backoffice.access`, not every catalog leaf. No legacy permission aliases are supported.

The mock `MorWalPiz` user belongs to the `admin` group. The group receives `backoffice.access` and `backoffice.manageall`.

The SPA filters navigation using server-expanded `effectivePermissions` returned by `/api/auth/validate` and contains no local hierarchy rules. This is a presentation concern only: every protected API operation must also enforce its permission server-side.

## Dashboard API

### `GET /api/dashboard/summary`

Returns the current administrative snapshot: total short links and short-link clicks recorded in the rolling seven-day UTC window, global latest BackOffice login (`max(User.LastLogin)` among active BackOffice users), active users, videos published in the dashboard window, active forms and responses, pending insights, and the UTC generation timestamp.

### `GET /api/dashboard/video-publications?days=21`

Returns daily video publication points. `days` is bounded to 21. The source field is `VideoRef.PublishedAt`; dates are normalized to UTC and returned in chronological order. Each day contains its count and video IDs/titles so the SPA can navigate to `/videos/{id}`.

Videos without a valid `PublishedAt` are excluded. Historical click analytics and internal publication events are outside this snapshot contract and would require a separate event collection.

## Frontend structure

- `PrimaryLayout` composes header, responsive sidebar, breadcrumbs, content outlet, and footer.
- `adminMenu.ts` is the single navigation catalog and associates every module with a permission.
- The Blog menu item targets `/blogposts`; the protected route catalog owns `/blogposts`,
  `/blogposts/create`, and `/blogposts/:id/edit`. These routes and their existing
  draft/save/publish workflow remain the Blog reachability contract.
- `AdminSidebar` renders one permission-filtered navigation tree inside one Bootstrap `Offcanvas` with `responsive="lg"`. Dashboard remains a direct link; Publishing, Media library, Website, Audience, Growth & insights, Catalog, and Administration are one-level, single-open sections. Empty permission-filtered sections are omitted, and the section for a deep-linked module opens automatically. Channel selection and configured external reports remain part of the same scrollable sidebar.
- `Home` places permission-filtered shortcuts for video management and article writing/listing above the existing KPI panels, 21-day Recharts publication chart, operational values, and recent publications. Shortcuts navigate to existing routes and never perform a write. Video links and chart drill-through are available only to users with video-list access; other dashboard users can still read analytics and publication titles.
- Dashboard summary and publication reads refresh when the selected channel changes. Previous-channel figures are hidden while refreshed data loads; stale responses are ignored. Analytics loading, error/retry, and no-selected-channel states do not hide the page heading or available shortcuts. The server response remains authoritative for metric scope; global account/form values are not described as channel-local.
- Existing CRUD routes and `GenericTable` remain the module implementation surface.

### Responsive shell

Below Bootstrap's `lg` breakpoint (992px), the sidebar is an overlay opened by the header menu button. The Offcanvas close button and each navigation link close it, preserving keyboard focus and dismissal behavior supplied by React-Bootstrap.

At 992px and above, the same Offcanvas becomes a persistent 248px flex item. The main column uses its remaining width and `min-width: 0` to prevent the dashboard grid and responsive tables from forcing horizontal shell overflow. No second desktop navigation tree is rendered.

The sidebar body remains vertically scrollable at short viewport heights. Section toggles are native buttons with `aria-expanded` and `aria-controls`; collapsing a section hides its links from the accessibility tree without removing the current-route indication from the section header.

Application styles are imported immediately after Bootstrap in `main.tsx`, so the shell, sidebar, dashboard panels, and responsive sizing rules participate in the production cascade.

## Frontend validation

Validated on 2026-08-08:

- Focused Vitest coverage passes for `Header`, `AdminSidebar`, route guards, and the video index: 4 files and 16 tests. This includes a valid cookie-authenticated shell with no localStorage display identity.
- The complete BackOffice SPA suite passes: 20 files and 98 tests.
- The TypeScript/Vite production build passes. Vite reports the existing warning for a minified router chunk larger than 500 kB.
- Focused BackOffice authorization coverage passes: 67 tests. Representative Products and ProductCategories read/create HTTP checks verify exact leaves, resource `manage`, global `manageall`, and mismatched-leaf denial; route metadata inventory covers the broader controller surface.
- The complete `MorWalPizVideo.BackOffice.Tests` run reports 199 passed, 28 failed, and 2 skipped. Twenty-seven existing product, compilation, and query-link scenarios receive `403 Forbidden`; `AdminEditWorkflowTests.Video_update_persists_video_references_submitted_by_edit_form` expects `204 NoContent` and receives `405 MethodNotAllowed`. They are outside this narrow authorization validation update.
- The BackOffice project build passes.

HTTP authorization tests intentionally remain representative rather than exhaustive for every controller-operation pair. The explicit attribute inventory plus focused RBAC, video, insights, forms, and catalog HTTP tests is the current regression signal; newly added sensitive operations still require a focused HTTP case.

The 2026-08-08 validation did not have a supported browser available; the later refinement validation below adds a bounded mocked-API viewport review.

### Navigation and home refinement validation

The latest BackOffice navigation/home refinement was verified with:

- 54 passing Vitest tests across seven files covering the navigation catalog, sidebar disclosure, dashboard actions/channel refresh, protected routes, shell, article form, and video index.
- A passing BackOffice SPA TypeScript/Vite production build. Vite emitted its existing warning for minified JavaScript chunks larger than 500 kB.
- A headless Playwright review using mocked API responses at widths 375, 768, 991, 992, and 1440px, plus a 375x520 short-height viewport. Each viewport retained a 1:1 document/viewport width; the chart retained a 320px render area and no browser page errors occurred. Desktop and mobile home/sidebar screenshots were captured as local session artifacts, not committed.
- API requests were intercepted locally; this visual review did not validate a deployed backend or real user session.

## Test requirements

Backend tests must cover dashboard authorization, empty data, 21-day boundaries, chronological grouping, global last login, and `backoffice.manageall`. Frontend tests must cover permission-filtered navigation, KPI loading/error states, and chart-to-video navigation.
