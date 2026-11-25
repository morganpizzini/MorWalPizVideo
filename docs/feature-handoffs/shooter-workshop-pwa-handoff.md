# Shooter Workshop PWA and offline-work handoff

## Outcome

Keep the existing installable Workshop PWA and add the agreed level-3 offline
workflow for use at ranges with no or weak connectivity. After online sign-in and
explicit device preparation, users can view saved data, record shooting sessions
and maintenance events, create/archive catalogue definitions, create physical
components, and replace an installed component. Reconnect syncs queued work through
the existing authenticated API; server conflicts are reported as aborted operations.

**Boundaries:** catalogue editing/deletion and other out-of-scope mutations remain
online-only. The service worker caches only the static shell/assets, never API
responses. Do not add Push, Background Sync, a client credential, or an unrelated
redesign. Confirmed sign-out warns and clears all local workspaces and queued data.

## Implementation and validation

Implemented within the existing Workshop SPA and API boundaries:

- Preserved the existing manifest, icons, install prompt, account transport, and
  root-scoped worker registration.
- The worker precaches the SPA entry point and linked same-origin JS/CSS, caches
  same-origin static resources, serves the cached SPA shell for offline navigation,
  and bypasses `/api` requests.
- Added native IndexedDB workspaces partitioned by account, explicit snapshot
  preparation, optimistic local projections, a durable ordered command outbox,
  dependency ID mappings, and persistent-storage requests where supported.
- Added offline-aware data loaders and queued workflows for shooting, maintenance,
  catalogue create/archive, component creation, and remove-then-create component
  replacement. Existing network mutations still use the same service APIs.
- Offline authentication restores only the remembered account identity after a
  network failure. It never stores the HttpOnly cookie/token; reconnect checks the
  server session. Sign-out confirms before clearing every local workspace and
  records deferred server revocation when disconnected.
- Added idempotent catalogue-create/archive handling and shooting retry detection
  so lost responses do not duplicate or fail retries after stock has been consumed.
  Existing operation-ledger semantics remain authoritative.

Validation performed:

- `cd frontend && yarn workspace shooter-workshop.client build` — passed.
- `cd frontend && yarn workspace shooter-workshop.client test` — passed, 23 files
  and 67 tests.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — passed
  with existing package/compiler warnings.
- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore --no-build --filter
  "FullyQualifiedName~WorkshopServiceTests"` — passed, 19 tests.
- Storage-specific tests cover account isolation, queued projections, preparation
  state, and logout clearing. Backend tests cover catalogue idempotency, archive
  retry, and shooting retry after batch stock is consumed.

Not verified: real-browser or physical iOS/Android behavior, deployed-origin headers,
storage-persistence grants/eviction, offline install/launch, or a full prepare-edit-
reconnect scenario. No Chromium browser was available. These remain release checks;
unit tests and builds do not prove mobile-browser storage durability or production
hosting behavior.

## Current Evidence

- `frontend/shooter-workshop.client/index.html` links the same-origin manifest;
  `public/manifest.webmanifest` specifies root start/scope and standalone display.
  Before this implementation, it declared two SVG icons.
  `public/icon-192.svg` and `public/icon-512.svg` contain the existing Workshop mark.
- `src/main.tsx` imports `src/services/installPrompt.ts` before rendering and
  registers the root-scoped `public/workshop-sw.js`. The worker caches only shell
  and static resources and explicitly bypasses `/api`.
- `src/layouts/AppShell.tsx` places `src/components/InstallPrompt.tsx` and a reopen
  button inside the authenticated shell. The service captures `beforeinstallprompt`,
  tracks dismissal in local storage and handles `appinstalled` and standalone mode.
  `src/services/installPrompt.test.ts` tests core event and dismissal behavior.
- `frontend/back-office-spa/public/manifest.webmanifest` and its install-prompt
  component use the same broad pattern, including SVG icons and authenticated-shell
  prompting. BackOffice is the parity reference; `frontend/shooting-ita-frontend/src/sw.ts`
  is not a safe caching template for this private application.
- `src/services/offlineStore.ts` owns account-partitioned IndexedDB datasets, queued
  commands, id mappings, local projections, and the logout marker. It stores no
  authentication credential.
- `src/services/offlineData.ts` provides network-first reads with saved-data fallback;
  `src/services/offlineMutations.ts` queues only recognized network failures or
  known-offline actions and requires a prepared workspace.
- `src/routes/offlineAction.ts` prepares supported snapshots and serially syncs
  commands. Conflicts block dependent commands; replacement is deliberately
  reported as partial if removal succeeds but installation fails.
- `docs/architecture/frontend.md` and `docs/architecture/shooter-workshop.md`
  describe the shipped offline boundary. Real-device and deployed-origin checks
  remain release gates, not evidence supplied by source or unit tests.
- `frontend/shooter-workshop.client/nginx.conf.template` serves static content and
  falls back to the SPA entry point; its Dockerfile builds and copies the Vite output.
  `.github/workflows/deploy-shooter-workshop-client.yml` tests, builds and stages an
  image. Source alone does not establish what is currently deployed, or whether the
  actual origin serves the manifest and worker with correct headers.

## Implemented Design

1. Retain the single Workshop worker, early install-event capture, localized
   authenticated-shell invitation, and existing manifest/icon identity.
2. Cache only the SPA shell and static assets; use IndexedDB rather than Cache Storage
   for private offline snapshots and commands. Never intercept API responses.
3. Keep the existing HttpOnly-cookie account flow. Offline restoration is a local
   UX gate only; reconnection revalidates the server session before syncing.
4. Preserve server authority with ordered idempotent commands. Catalogue create and
   physical equipment creation can reference earlier local IDs. Replacement is a
   non-atomic remove-then-create sequence and reports its partial state.
5. Local builds and suites pass. Real-browser, physical-device, and deployed-origin
   verification remain open as noted above.

**UI direction:** refinement of the existing Workshop **Operate** surface, not a
redesign. Reuse the shell, responsive styles, Italian/English localization, and
accessible status/error patterns. Offline readiness, pending counts, aborted
operations, and storage-persistence warnings are explicit; no new visual identity
or design artifacts are required.

## Impacted Projects

- `frontend/shooter-workshop.client`: static worker, IndexedDB snapshot/outbox,
  authentication fallback, offline-aware routes, actions, status UI, translations,
  tests, and production bundle.
- `MorWalPizVideo.Models`, `MorWalPiz.Contracts`, `MorWalPizVideo.ServerAPI`, and
  `MorWalPizVideo.Domain`: additive idempotency metadata/contracts and server retry
  handling for the supported offline writes.
- Deployment: existing static image and API origin; no new service, secret, or
  authentication configuration.
- MongoDB schema: no migration; the client request hash is an optional additive field.

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/public/workshop-sw.js`: precache static shell
  assets, provide offline navigation fallback, and bypass API routes.
- `frontend/shooter-workshop.client/src/services/offlineStore.ts`,
  `offlineData.ts`, `offlineMutations.ts`, and `src/routes/offlineAction.ts`: own
  IndexedDB storage, offline reads/writes, preparation, sync, and conflict states.
- Workshop route loaders/actions, `AuthProvider.tsx`, `AppShell.tsx`, equipment
  panels, styles, and `i18n.tsx`: connect supported workflows, session handling,
  user warnings, and localized status.
- `frontend/shooter-workshop.client/src/services/offlineStore.test.ts`: verify
  account partitioning, queued projections, and explicit logout clearing.
- `MorWalPizVideo.Models/Models/Workshop/WorkshopEntities.cs`,
  `MorWalPiz.Contracts/Contracts/Workshop/WorkshopContracts.cs`,
  `MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs`, and
  `MorWalPizVideo.Domain/Workshop/WorkshopService.cs`: preserve idempotent server
  behavior for create/archive and shooting retries.
- `MorWalPizVideo.BackOffice.Tests/Features/WorkshopServiceTests.cs`: cover the
  new backend retry behavior.

### Generated/Migration

No database migration. Vite build output and the deployment image are generated
normally and must not be checked in as source.

### Documentation

- `docs/architecture/frontend.md`, `docs/architecture/shooter-workshop.md`, and this
  handoff document the implemented offline boundary and remaining mobile/deployment
  verification.

### Verify Only

- `frontend/shooter-workshop.client/src/main.tsx`, `nginx.conf.template`,
  `Dockerfile`, `vitest.config.ts`, and
  `.github/workflows/deploy-shooter-workshop-client.yml`: preserve worker scope,
  static delivery, and deployment validation.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Adds idempotency metadata and deterministic retry handling; API authentication and authority remain server-owned. |
| Frontend/UI | Adds preparation, offline status, pending/conflict feedback, and logout deletion warning. |
| Database/Data | Account-partitioned IndexedDB snapshots/outbox; logout clears all local workspaces. Mongo has an optional additive operation field; no migration. |
| Contracts/Models | Optional client-request hash/key additions; existing route and response shapes remain compatible. |
| Authentication/Authorization | HttpOnly cookie/CSRF flow unchanged. Offline identity is not a credential; reconnect requires server session validation. |
| Validation | Frontend build/tests and focused backend build/tests pass; real browser/device checks remain. |
| Cache | Worker caches only static shell/assets. API and authenticated responses are never stored in Cache Storage. |
| Configuration/Secrets | None; no new secret, service, or configuration key. |
| Logging/Telemetry | Storage/session failures are surfaced or logged; no new telemetry service. |
| Background Jobs | Foreground reconnect sync only; no Push or Background Sync. |
| Deployment/Operations | Existing static image/API origin; verify HTTPS, worker scope, CSP, storage behavior, and mobile install on deployed host. |
| Backward Compatibility | Existing install identity, cookie auth, APIs, operation ledger, and legacy documents remain supported. |

## Reuse Opportunities

Reuse Workshop's existing manifest, worker registration, account transport, route
actions/loaders, translations, and operation ledger. Use native IndexedDB rather
than introducing a persistence framework; keep private API data out of Cache Storage.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Sensitive cached datasets or pending commands could cross account boundaries. | Partition all records by account; clear every workspace on confirmed logout; verify account switching and browser storage on real devices. |
| High | Browser eviction or storage corruption may remove a long-lived offline queue. | Request persistent storage during preparation, warn when denied, and advise reconnect/sync before sign-out; browser eviction behavior remains a device release check. |
| High | A replacement is not atomic; the old installation can be removed before the new item is installed. | Persist a sync phase and report partial completion explicitly; verify recoverable state and user messaging in real-device runs. |
| Medium | An expired server session may delay sync until the user signs in again. | Revalidate on reconnect; preserve the queue on 401 and route through normal login before retry. |
| Medium | Cached data may be stale or a domain conflict may abort an operation. | Server remains authoritative; block the failed command and dependent work with an explicit reason. |
| Medium | Release host, worker update, or storage policy differs from local source. | Verify deployed HTTPS origin, worker scope, CSP, mobile storage, install, offline navigation and reconnect. |
| Low | Offline views are limited to datasets explicitly prepared by the supported workflow. | Keep online-only routes/operations explicit; expand only with a reviewed data contract and tests. |

## Remaining Technical Debt

- Real-browser and iOS/Android release evidence is outstanding.
- IndexedDB is not encrypted by the app; the agreed physical-device lock is the
  local protection boundary. Browser storage can still be evicted or cleared.
- Component replacement is a sequence rather than a server-atomic operation;
  partial completion is surfaced but still needs device-level verification.
- Offline mode does not cover every Workshop page or operation. Only prepared
  datasets and listed writes are available.

## Implementation Roadmap And Status

1. **Installability baseline — retained:** Existing manifest/icons/install prompt
   remain; verify fresh-profile Chromium and Safari behavior before release.
2. **Offline shell and storage — implemented:** Static worker cache, account-scoped
   IndexedDB snapshots/outbox, preparation, persistence request, and logout clear.
3. **Domain operations — implemented:** Supported catalogue, component, shooting,
   maintenance, and replacement commands with ordered sync and idempotency.
4. **Validation — local complete:** Workshop build, complete frontend suite, focused
   backend build, and Workshop service tests passed. Mobile/browser/deployed gates
   remain open.
5. **Documentation — updated:** Architecture guide and this handoff reflect the
   implemented behavior and remaining risks.

## Testing Strategy

- **Unit/component:** account-partitioned store, queue projection, logout clearing,
  route mutation behavior, operation idempotency, conflicts, and server retry cases.
- **Build/static:** Workshop workspace build and full Vitest suite; backend test
  project build and focused Workshop service tests. Inspect deployed manifest/worker
  content types and ensure Cache Storage excludes APIs.
- **Browser/end-to-end:** on Android and iOS, prepare while online, confirm saved
  pages and supported writes in airplane mode, reconnect with a valid/expired
  session, test conflict reporting, storage persistence, app restart, sign-out
  warning, and complete local deletion. Verify install and deep links as well.
- **Security/offline:** verify another account cannot read a different partition;
  no API response/token is cached; deferred logout revokes the cookie on reconnect;
  same-account session validation gates sync.
- **Contracts/migration:** no Mongo migration; optional operation field remains
  compatible with legacy documents. API request idempotency contract must be
  deployed with the backend before enabling the offline client in production.

## Migration And Rollback

No database migration or new configuration is needed. Deploy the backend request
contract and idempotency behavior before the client that queues those operations.
Keep app identity, root scope and worker URL stable. Deploy through the existing
pipeline and verify the released revision on HTTPS. If rollback is needed, preserve
the API compatibility field and do not assume clients have discarded their
IndexedDB outboxes; keep server retry behavior backward-compatible while old clients
may still be syncing.

## Remaining release decisions

- Verify storage grants, browser eviction, app restart, and offline behavior on the
  actual supported iPhone and Android devices.
- Confirm production API and static image deployment ordering, CSP, worker scope,
  and real conflict responses before enabling the offline client for users.
