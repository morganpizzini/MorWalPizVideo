# BackOffice Admin Dashboard

## Status

Current implementation for the BackOffice SPA. The dashboard uses the existing unversioned `/api/...` convention and does not add a MongoDB collection.

## Authorization

`backoffice.access` grants access to the administrative shell and dashboard. Module permissions are lowercase and use the resource-operation form: `<resource>.view`, `<resource>.create`, `<resource>.update`, `<resource>.delete`, and `<resource>.manage`.

The backend expands each declared `<resource>.manage` parent to its explicitly reviewed sibling capabilities. Specialized implications are `users.permissions.manage`, `videos.import`, `videos.translate`, `videos.publish`, `forms.responses.view`, and `insights.scan`. `images.manage` has no update leaf, and `diagnostics.view` remains standalone. Expansion is directional; a leaf does not imply its parent or siblings.

`backoffice.manageall` is the evaluator and frontend administrator override. Its effective-permission expansion adds only `backoffice.access`, not every catalog leaf. No legacy permission aliases are supported.

The mock `MorWalPiz` user belongs to the `admin` group. The group receives `backoffice.access` and `backoffice.manageall`.

The SPA filters navigation using server-expanded `effectivePermissions` returned by `/api/auth/validate` and contains no local hierarchy rules. This is a presentation concern only: every protected API operation must also enforce its permission server-side.

## Dashboard API

### `GET /api/dashboard/summary`

Returns the current administrative snapshot: total short links and cumulative clicks, global latest BackOffice login (`max(User.LastLogin)` among active BackOffice users), active users, videos published in the dashboard window, active forms and responses, pending insights, and the UTC generation timestamp.

### `GET /api/dashboard/video-publications?days=21`

Returns daily video publication points. `days` is bounded to 21. The source field is `VideoRef.PublishedAt`; dates are normalized to UTC and returned in chronological order. Each day contains its count and video IDs/titles so the SPA can navigate to `/videos/{id}`.

Videos without a valid `PublishedAt` are excluded. Historical click analytics and internal publication events are outside this snapshot contract and would require a separate event collection.

## Frontend structure

- `PrimaryLayout` composes header, responsive sidebar, breadcrumbs, content outlet, and footer.
- `adminMenu.ts` is the single navigation catalog and associates every module with a permission.
- `AdminSidebar` renders one permission-filtered navigation tree inside one Bootstrap `Offcanvas` with `responsive="lg"`.
- `Home` renders KPI panels, the 21-day Recharts publication chart, operational values, and clickable recent publications.
- Existing CRUD routes and `GenericTable` remain the module implementation surface.

### Responsive shell

Below Bootstrap's `lg` breakpoint (992px), the sidebar is an overlay opened by the header menu button. The Offcanvas close button and each navigation link close it, preserving keyboard focus and dismissal behavior supplied by React-Bootstrap.

At 992px and above, the same Offcanvas becomes a persistent 248px flex item. The main column uses its remaining width and `min-width: 0` to prevent the dashboard grid and responsive tables from forcing horizontal shell overflow. No second desktop navigation tree is rendered.

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

Real-browser viewport inspection was unavailable in the implementation environment because no Playwright package or supported local browser executable was installed. The remaining visual validation is one navigation landmark, no overlap or horizontal overflow, a non-zero chart area, and keyboard-accessible mobile dismissal immediately below and above 992px and at mobile, tablet, and wide-desktop widths.

## Test requirements

Backend tests must cover dashboard authorization, empty data, 21-day boundaries, chronological grouping, global last login, and `backoffice.manageall`. Frontend tests must cover permission-filtered navigation, KPI loading/error states, and chart-to-video navigation.
