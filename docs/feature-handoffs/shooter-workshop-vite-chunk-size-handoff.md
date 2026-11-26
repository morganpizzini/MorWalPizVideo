# Shooter Workshop Vite chunk-size handoff

Status: implemented and build-validated. Browser/offline update transitions and
responsive capture remain residual verification work.

## Implementation Result

The Workshop route manifest now resolves page components and route-owned
loaders/actions lazily. Account login/register/profile, dashboard, firearms,
firearm detail, ammunition, recipe production, shooting sessions, history,
inventory and reconciliation use route-level lazy boundaries. Redirect-only
routes and the authenticated shell remain eager. QR code and route modules are
no longer in the initial HTML static-import graph.

Vite emits a build-specific `asset-manifest.json` containing every emitted JS
and CSS asset. The service worker precaches that complete manifest, validates
HTML and asset content types, and publishes a new build cache only after every
asset is cached. Failed installation leaves the prior active cache usable.

### Comparable build measurements

Using the ordered shared-package build and the Workshop production build:

| Measure | Baseline | Final |
| --- | ---: | ---: |
| Largest emitted JS chunk | 501.68 kB (148.93 kB gzip) | 368.78 kB (116.61 kB gzip) |
| React chunk | 108.83 kB (36.51 kB gzip) | 108.83 kB (36.51 kB gzip) |
| QR chunk | 25.32 kB (9.92 kB gzip) | 25.32 kB (9.92 kB gzip), not eagerly requested |
| Initial HTML static JS graph | 610.51 kB | 477.61 kB |
| Initial HTML static JS reduction | - | 21.79% |
| Emitted chunks over 500 kB | 1 | 0 |

The baseline and final React chunks are apples-to-apples for the measured build
environment, while route and entry chunk hashes naturally vary. The 21.79%
figure is the historical baseline entry plus the React module-preload graph
compared with the final entry plus React graph. It excludes CSS. Gzip and browser
network measurements remain separate from precache/download totals.

## Outcome

Reduce the Shooter Workshop production JavaScript bundle warning and unnecessary
initial-route JavaScript without changing application behavior, authentication,
offline support, or the visual design. Scope is the Workshop client, not every
Vite application in the monorepo. The prior inventory implementation reported a
501.68 kB minified entry chunk (148.93 kB gzip); treat that as a historical
observation, not a current reproducible baseline.

**Acceptance:** using the same build environment and dependencies before and
after, no emitted JavaScript chunk exceeds Vite's existing 500 kB warning
threshold, and the total minified JavaScript in the initial document's
static-import graph decreases by at least 10%. Measure gzip and actual browser
network requests separately; do not claim first-visit transfer savings merely
because code was split. Keep the existing warning threshold; do not silence it
by raising the limit. If offline-safe splitting cannot meet these goals without
materially expanding scope, report the measured blocker rather than weakening
the offline contract.

**Non-goals:** a visual redesign, dependency upgrade, changes to API contracts,
offline data semantics, or other applications; a generic bundle-analysis
platform; and changes to CDN or server infrastructure unless implementation
evidence demonstrates they are necessary.

## Current Evidence

- The previous [inventory handoff](./shooter-workshop-inventory-feedback-handoff.md)
  records the Vite chunk advisory. The prior session's build output also
  reported separate React (108.83 kB) and QR-code (25.32 kB) chunks; sizes
  depend on the exact source and build and must be remeasured.
- [Vite configuration](../../frontend/shooter-workshop.client/vite.config.ts)
  already uses manual chunks for React/React Router and QR code. There is no
  custom size limit. [Recipe production](../../frontend/shooter-workshop.client/src/routes/RecipeProduction/component.tsx)
  imports QR code statically; the manual chunk alone does not establish that it
  is deferred from initial loading.
- [Route configuration](../../frontend/shooter-workshop.client/src/routes/config.tsx)
  statically imports page components, loaders, and actions. Its manifest also
  contains redirects, route-specific error handling, and revalidation rules.
  [Route tests](../../frontend/shooter-workshop.client/src/routes/config.test.tsx)
  explicitly inspect some of these entries. Deferred **data** in the
  [Workshop architecture](../architecture/shooter-workshop.md) is not evidence
  that route JavaScript is lazy loaded.
- [Service worker](../../frontend/shooter-workshop.client/public/workshop-sw.js)
  precaches the shell and JS/CSS links found in HTML; other same-origin assets
  are cached when fetched. It does not discover unvisited dynamic imports.
  API requests bypass it. Offline preparation in
  [offlineAction.ts](../../frontend/shooter-workshop.client/src/routes/offlineAction.ts)
  prepares account data independently, so splitting routes without ensuring
  their assets are cached can break a prepared device's first offline visit.
- [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx) registers
  the worker and owns the router bootstrap and unauthorized redirect.
  [Dockerfile](../../frontend/shooter-workshop.client/Dockerfile) builds the
  shared packages in order and publishes the Workshop build; the
  [nginx template](../../frontend/shooter-workshop.client/nginx.conf.template)
  falls back to the SPA entry for unknown paths.
- [Deployment workflow](../../.github/workflows/deploy-shooter-workshop-client.yml)
  runs shared builds, Workshop tests and the production build. The existing
  [browser test](../../frontend/shooter-workshop.client/e2e/authenticated-firearm.spec.ts)
  exercises login and firearm detail against a local mock API; it does not
  prove production-build chunk loading or prepared-device offline navigation.

## Proposed Design

1. Establish an apples-to-apples production baseline: record emitted minified
   and gzip sizes, the complete static-import graph of the HTML entry, and
   browser network requests for cold login, dashboard, and selected later routes.
   Include service-worker install/precache traffic separately and in total.
   Identify which existing imports actually dominate before choosing split points.
2. Prefer React Router route-level lazy loading for sufficiently large,
   infrequently visited route components and their route-owned modules. Keep
   paths and the protected shell stable, and preserve loaders, actions,
   `shouldRevalidate`, error boundaries, redirects, deferred data, and fetcher
   behavior. Keep only the route metadata and common shell required for matching
   and protection eager. Split only as many routes as the measurement requires.
   Use the existing loading and error presentation; no visual redesign.
3. Before shipping any dynamically loaded route, make offline assets complete
   for a prepared device, including all emitted JS and CSS in its dependency
   graph, even if the user never opened that route while online. Use a
   deterministic build-generated asset inventory consumed by the existing
   service worker, with immutable build-specific cache identity. Cache only
   same-origin public build assets, never API responses, account data, or
   arbitrary user-provided URLs. Include shared/static dependencies and CSS;
   verify actual deployed paths and content types instead of accepting an HTML
   fallback as a JavaScript asset.
4. Treat worker installation/update as an all-or-nothing asset preparation:
   surface failed or incomplete precaching rather than declaring a new offline
   version ready; keep the prior working cache until the new version is
   complete. Preserve in-flight old clients and test the version transition,
   including a new deployment with changed hashed filenames. If the existing
   worker lifecycle cannot guarantee this within scope, leave splitting
   unshipped and document the blocker.
5. Compare the static graph and real browser behavior after the change,
   including cold versus warm navigation, total precache bytes, and later-route
   latency. Offline precaching may intentionally fetch deferred files ahead of
   use; distinguish execution-graph improvement from total download and report
   any first-visit regression. Avoid a manual vendor split that only moves bytes
   into an equally eager dependency while claiming a performance improvement.

**Alternatives:** adjusting `chunkSizeWarningLimit` hides the symptom and is
rejected. Additional manual chunks can be appropriate for an evidence-backed
shared dependency, but do not by themselves prove lower startup work and may
introduce circular dependencies. A new service-worker/PWA framework or a
cross-application bundling overhaul is outside this handoff.

**UI direction:** this is an **Operate** implementation-preserving refinement,
not a redesign. Use the existing Workshop interface, loading states, and
accessibility behavior. Apply the installed Impeccable `harden` workflow to
loading/error and offline transition states; no new `PRODUCT.md` or
`DESIGN.md` is required. Capture desktop and narrow-screen evidence for a slow
route transition and an offline failure/recovery, and check keyboard focus and
announced errors.

## Impacted Projects

- **Shooter Workshop client:** route loading, production bundle, and potentially
  build-time asset enumeration and service-worker lifecycle.
- **Workshop tests:** existing Vitest route coverage and local Playwright
  production-build/offline coverage.
- **Shared frontend packages:** verify their existing build outputs and
  consumers; do not change them unless measurement demonstrates a necessary
  shared defect.
- **Deployment:** verify the existing client image, nginx static delivery,
  deployment workflow, and update/rollback behavior. No intended ServerAPI,
  .NET, database, AppHost, other SPA, or Windows application change.

## Impacted Files

### Modify

- [routes/config.tsx](../../frontend/shooter-workshop.client/src/routes/config.tsx):
  introduce measured route split boundaries while preserving route semantics.
- [routes/config.test.tsx](../../frontend/shooter-workshop.client/src/routes/config.test.tsx):
  assert observable route matching, data/mutation behavior, and error handling
  after lazy resolution rather than requiring eager functions.
- [vite.config.ts](../../frontend/shooter-workshop.client/vite.config.ts):
  only if needed for build asset inventory or a measured chunk boundary.
- [workshop-sw.js](../../frontend/shooter-workshop.client/public/workshop-sw.js):
  if routes are split, precache the complete versioned build asset set and
  implement safe update/failure handling.
- [offlineAction.ts](../../frontend/shooter-workshop.client/src/routes/offlineAction.ts)
  and existing offline UI only if needed to ensure preparation cannot report
  success before required assets are ready; preserve existing data preparation.
- [authenticated-firearm.spec.ts](../../frontend/shooter-workshop.client/e2e/authenticated-firearm.spec.ts):
  extend or complement the existing browser flow with focused production-build
  route and offline checks.

### Add

- A build-generated static asset inventory, if required, at a Workshop-owned
  build-output location; do not commit generated output. Add a focused existing-
  framework test in the owning test directory for any new worker/build logic.

### Generated/Migration

Only generated frontend build assets and cache versions; no database or
persisted account-data migration. Do not treat generated assets as source.

### Documentation

- Update this handoff with actual changes, comparable baseline/final
  measurements, tests, and residual risks.
- Update [Workshop architecture](../architecture/shooter-workshop.md) if the
  worker's precache/update contract changes.

### Verify Only

- [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx),
  [index.html](../../frontend/shooter-workshop.client/index.html),
  [Dockerfile](../../frontend/shooter-workshop.client/Dockerfile),
  [nginx template](../../frontend/shooter-workshop.client/nginx.conf.template),
  [Playwright config](../../frontend/shooter-workshop.client/playwright.config.ts),
  and [deployment workflow](../../.github/workflows/deploy-shooter-workshop-client.yml).
  Modify only when a demonstrated delivery defect makes it necessary.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No intended changes; existing local mock API supports browser checks. |
| Frontend/UI | Route-code loading and public static-asset handling; preserve existing UI and behavior. |
| Database/Data | No schema or offline account-data changes; do not cache authenticated responses. |
| Contracts/Models | None. |
| Authentication/Authorization | Preserve shell protection, login return paths, cookie/CSRF behavior, and API bypass. |
| Validation | Measure the actual emitted static graph, chunks, gzip sizes and offline readiness, not warning text alone. |
| Cache | Versioned public asset caching and worker update/rollback are material; account data stays separate. |
| Configuration/Secrets | No new secrets or runtime configuration intended. |
| Logging/Telemetry | Surface asset/precache failures; do not log personal data. No new telemetry service. |
| Background Jobs | No server jobs; the existing browser worker is the asset lifecycle owner. |
| Deployment/Operations | Verify Docker output, nginx delivery, cache updates, and old-client behavior. |
| Backward Compatibility | Preserve existing routes, prepared offline workflows, and safely handle old/new asset versions. |

## Reuse Opportunities

Reuse React Router's existing route manifest, loader/action boundaries and
error handling, the current worker and Cache Storage, the existing Vitest and
Playwright suites, and shared-package build ordering. Reuse existing Workshop
loading states and translation patterns rather than adding a new UI system.

## Risks

| Rank | Risk and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | An unvisited lazy route fails on a prepared offline device. | Precache the complete build asset graph; test first offline visits and actual offline mutations after preparation. |
| High | A failed update or changed asset hash breaks existing open clients or rollback. | Version caches, stage complete assets before activation, retain the previous working version as needed, and exercise deployment/update/rollback transitions. |
| Medium | Nginx serves HTML for a missing asset, which appears cacheable but cannot execute. | Validate asset status/content type and test a missing chunk; never record a false successful precache. |
| Medium | Splitting changes action, error-boundary, authentication or revalidation semantics. | Preserve manifest responsibilities; test direct loads, navigation, mutation and error recovery on protected routes. |
| Medium | Smaller chunks increase total precache traffic or route latency. | Compare total cold/warm network bytes, initial static graph and later-route timings; disclose the tradeoff. |
| Low | Circular manual chunks or duplicate dependencies erase benefits. | Inspect the real dependency graph and build warnings; prefer minimal route boundaries. |

## Technical Debt

- The current worker's HTML-link-only precache and non-fatal asset-fetch
  failures are directly relevant to route splitting; address the offline
  invariant in this task if splitting is implemented.
- Existing chunk size is a warning, not proof of a user-perceived performance
  defect. Broader performance budgets, service-worker rewrites, and unrelated
  application chunk warnings are deferred.

## Implementation Roadmap

1. **Measure:** reproduce the prior production build in the existing ordered
   workspace flow and capture initial static graph, minified/gzip chunk sizes,
   cold/warm navigation, and worker precache traffic. Verify the 500 kB warning
   still exists and identify the modules responsible.
2. **Offline contract:** implement the smallest reliable, build-versioned asset
   inventory and worker readiness/update behavior needed for dynamic routes.
   Verify complete asset delivery, API exclusion, failed install and cache
   preservation before relying on lazy chunks.
3. **Split:** apply the minimum evidence-backed route lazy boundaries needed to
   reach both acceptance measurements. Preserve protected routes, loader/action
   timing, deferred data, revalidation, redirects and existing errors.
4. **Validate and document:** run focused tests and builds, exercise a real
   production build in the browser online/offline and across update/rollback,
   record comparable measurements in this handoff, and update Workshop
   architecture if needed. Stop at this boundary; unrelated optimizations need
   separate approval.

An independent post-validation review is recommended because asset-version
transitions can strand installed offline clients; the user decides whether to
request it.

## Testing Strategy

- **Unit/component:** route matching, lazy resolution, loader/action execution,
  `shouldRevalidate`, error boundaries, redirects, and unchanged fetcher
  feedback; worker asset-list validation and failed precache where testable.
- **Integration/contract/security:** anonymous versus authenticated direct
  navigation, login return path, protected loader/action and CSRF behavior;
  assert API traffic is never stored in the shell cache.
- **Production browser:** with the existing local mock API safety checks, serve
  the built assets through production-equivalent static fallback; verify direct
  navigation and reload for login, dashboard, firearm, inventory and another
  split route. Prepare an account online, then visit previously unvisited
  supported offline routes offline and perform a supported queued operation.
  Verify expected online-only messaging, reconnect and sync.
- **Update/recovery:** install version A, prepare offline, introduce version B
  with changed hashes, interrupt precaching, and verify version A remains
  usable; complete B and verify navigation, old open tab, offline reload and
  rollback. Confirm missing chunks or HTML fallback cannot masquerade as
  successful cached JS.
- **Measurements:** compare all emitted JS chunks with the unchanged 500 kB
  threshold and initial static-import graph with the 10% reduction target.
  Record gzip, total worker precache and browser transfer bytes as well as
  later-route load behavior; specify build and browser conditions.
- **Migration:** no database or contract migration tests. Test both a fresh
  installation and an existing prepared device upgrading from the current
  worker. Run existing Workshop tests and production build and capture the
  requested responsive/keyboard evidence.

## Migration And Rollback

Deploy one self-consistent frontend image containing its shell, worker and
build-specific assets. Stage and verify the new cache before activation;
retain a known-working previous cache for existing clients until it is safe to
retire. Exercise old-client/new-deployment transitions and rollback using
versioned assets; if the host cannot serve required old hashed assets and
cache retention does not cover the scenario, do not claim safe rollout.
Offline account snapshots and queued commands must survive frontend upgrades
and rollback. No API, authentication, MongoDB, SQLite or configuration
migration is expected. Monitor worker installation and asset failures without
collecting private account data.

## Open Questions

None blocking. The 500 kB warning and 10% initial static-graph reduction are
proposed acceptance targets for this handoff; actual gains and precache tradeoffs
must be established by the baseline measurement before implementation.
