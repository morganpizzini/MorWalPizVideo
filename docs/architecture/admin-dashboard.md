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
- `AdminSidebar` hides unauthorized items and uses Bootstrap responsive offcanvas behavior.
- `Home` renders KPI panels, the 21-day Recharts publication chart, operational values, and clickable recent publications.
- Existing CRUD routes and `GenericTable` remain the module implementation surface.

## Test requirements

Backend tests must cover dashboard authorization, empty data, 21-day boundaries, chronological grouping, global last login, and `backoffice.manageall`. Frontend tests must cover permission-filtered navigation, KPI loading/error states, and chart-to-video navigation.