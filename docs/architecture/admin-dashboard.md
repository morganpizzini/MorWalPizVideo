# BackOffice Admin Dashboard

## Status

Current implementation for the BackOffice SPA. The dashboard uses the existing unversioned `/api/...` convention and does not add a MongoDB collection.

## Authorization

`backoffice.access` grants access to the administrative shell and dashboard. Module permissions are lowercase and use the resource-operation form: `<resource>.view`, `<resource>.create`, `<resource>.update`, `<resource>.delete`, and `<resource>.manage`.

Special operations use explicit permissions, for example `videos.import`, `videos.translate`, and `videos.publish`. `backoffice.manageall` is the administrator override and grants every module and operation. No legacy permission aliases are supported.

The mock `MorWalPiz` user belongs to the `admin` group. The group receives `backoffice.access` and `backoffice.manageall`.

The SPA filters navigation using `effectivePermissions` returned by `/api/auth/validate`. This is a presentation concern only: every protected API operation must also enforce its permission server-side.

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

- Focused Vitest coverage passes for `AdminSidebar`, `Header`, and `Home`, including one navigation tree, permission filtering, active state, and close behavior.
- The complete BackOffice SPA suite passes: 18 files and 85 tests.
- The checked TypeScript/Vite production build passes. Its generated HTML references an existing combined CSS asset in which application shell rules follow Bootstrap.
- Static checks of representative login and video routes found no required changes from reconnecting the existing global stylesheet.
- Repository-wide lint still reports pre-existing findings outside this repair.

Real-browser viewport inspection was unavailable in the implementation environment because no Playwright package or supported local browser executable was installed. The remaining visual validation is one navigation landmark, no overlap or horizontal overflow, a non-zero chart area, and keyboard-accessible mobile dismissal immediately below and above 992px and at mobile, tablet, and wide-desktop widths.

## Test requirements

Backend tests must cover dashboard authorization, empty data, 21-day boundaries, chronological grouping, global last login, and `backoffice.manageall`. Frontend tests must cover permission-filtered navigation, KPI loading/error states, and chart-to-video navigation.
