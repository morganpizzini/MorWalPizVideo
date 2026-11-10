# Delivery and tooling

Consolidated 2026-11-09 from deployment diagnostics, runner, container CI,
BuildKit, and BackOffice installability handoffs. This is **historical local
evidence**, not hosted execution or fresh validation.
Current delivery policy is in [Deployment](../architecture/deployment.md),
[ADR-023](../architecture/adr/ADR-023-container-image-staging.md), and
[ADR-024](../architecture/adr/ADR-024-dual-api-deployment-staging.md).
Those later decisions supersede older assumptions of active image deployment.

## Deployment authentication and current policy

The BackOffice SPA diagnostic recorded a user-confirmed "No credentials found":
registry login is not Azure management authentication. The workflow correction
added contents:read/checks:write/id-token:write and azure/login v2 using existing
protected identity references, preserving SHA image/test/build/push/target.
YAML parse/diff checks passed; no Azure deployment was executed.

Current ADR policy distinguishes image publication from runtime activation:
BackOffice API, ServerAPI, and ShortLinks retain independent validated ZIP
deployment and image-push jobs; image deployment stays disabled. Other applications
remain image-only staging. Either of the three API jobs may proceed after its
test gate even if the parallel image job fails; overall workflow still fails.
ServerAPI retains post-package anonymous account bootstrap smoke.

Open: exact production-environment federation/identity, Web App rights, registry
pull, SHA/runtime responsiveness. Node action-runtime deprecations are separate
from Node 22 application test setup. Reverting auth does not undo deployment;
recover the prior immutable image/package through the approved release path.

## Test runner timeout

**Proposal only**, no completion record.

- Recent >120s timeout emitted NETSDK1057 but no VSTest banner: not evidence of
  an application/testhost hang. Preview SDK VSTest loader failures, orphaned
  processes, and concurrent builds are candidate toolchain causes.
  Historical diagnosis eventually ran two tests in ~1s; CI reached tests in ~7s.
- Proposed root stable .NET 10/no-preview SDK policy and workflow parity must
  select an exact version from approved installed/green-CI evidence, not guess.
  Preserve Test SDK 18.9.0/xUnit runner 4.0.0 initially.
- Recover stale processes, then separate serial restore/build/no-build test
  phases. Do not change production startup without phase evidence.
  No overlapping .NET commands writing shared bin/obj.

Owners: proposed root SDK policy, [CI](../../.github/workflows/ci.yml) and API
deployment workflows, backend test project/factory; Mongo startup initializer is
verify-only. Gates: stable resolution without loader warnings, the two creator
endpoint tests, nonempty deployment/full/cross-project results, Windows and every
setup-dotnet site. Roll policy and workflows back together; no runtime/data
migration. See [creator incident](editorial-and-ai.md#creator-partnerships-and-campaigns).

## Clean container CI

Recorded implementation introduced a shared local
[build action](../../.github/actions/build-container-image/action.yml).
Its handoff snapshot covered three APIs (BackOffice/ServerAPI/Freezer) and five
SPAs (BackOffice/Ask/public/Range/Freezer), clean no-cache/no-push central builds,
five deployment workflows using the action, action-path triggers, preserved
contexts/tags/cache/SHA, and Freezer VITE_FREEZER_API_BASE_URL.
Shop was deliberately excluded/on hold.

The later deployment architecture records five backend Dockerfiles in central
CI. Treat the three-API list as a historical introduction, not the current
inventory; reconcile against workflows before future edits.
Only YAML/matrix/input review was recorded. No Docker daemon, actual image build,
or hosted run. Hosted clean builds, Azure pull/startup/health and qualified
rollback remain gates; build success alone would not prove them.

## Public-client BuildKit cache

Recorded Dockerfile/workflow implementation, not image/performance qualification:

- Node 22 Alpine two-stage SSR/runtime retained. Manifest-first frozen Yarn;
  models -> services -> layout copied/built separately; no forced reinstall;
  locked app-specific Yarn/npm mounts.
- ACR remains remote layer cache; no added gha/cache-mount export mechanism.
  Ephemeral mounts do not establish fresh-runner persistence.
  Action v6, tests, checked host prebuild, SHA/latest, port 80/runtime injection
  preserved. Services/layout COPY paths corrected to absolute after models
  WORKDIR nested them.
- Owner: [public Dockerfile](../../frontend/morwalpizvideo.client/Dockerfile)
  and owning deployment workflow; frontend effective ignore/shared manifests
  and runtime remain verify-only.

Evidence: **6 files/18 public tests**, shared build, checked client/SSR build,
path assertions/diff check passed. Docker/Buildx installed but daemon unavailable:
no clean/warm timings, size, runtime, filesystem/security inspection, or image.
Require authorized cache import/export/push, per-package invalidation, runtime/
hydration/browser smoke. Generated unpinned Express runtime dependency remains
deferred debt. Keep checked host build because Docker uses unchecked build.
Rollback previous immutable artifact plus Dockerfile/workflow; no data migration.

## ServerAPI BuildKit experiment

**ON HOLD. Do not continue or expand without new user approval.**

Local Dockerfile change only: seven-project manifest closure, matching locked
NuGet restore/publish mount, single no-restore publish; .NET 10/Release/nonroot/
ports/entrypoint preserved. No sibling/workflow/ignore/source scope.
Owner: [ServerAPI Dockerfile](../../MorWalPizVideo.ServerAPI/Dockerfile).

2026-11-04 structural checks and **62 ServerAPI/Shared/CrossApi tests**
passed with zero failures/skips. Earlier "Buildx missing" was superseded by
installed Docker 29.9.3/Buildx, but no daemon/image/cache timing/runtime/size/
security proof. Existing production ZIP path does not use this Dockerfile:
no deployed speed benefit is claimed.

Root noisy context remains. A future cache-enabled CI split/scheduled clean gate/
dedicated gha scope needs separate approval and cannot silently replace clean
build guarantees. Persistent builder and quantitative threshold remain undecided.
Rollback is the Dockerfile only; preserve normal code deployment and data.

## BackOffice installability

Recorded implementation, **automated/browser/container qualification incomplete**:

- Manifest scope/start `/`, standalone, same-origin 192/512 SVG any/maskable
  branding. Retain one existing `/push-sw.js` worker at `/` and subscriptions.
  No Workbox replacement, authenticated API/HTML precache, offline promise,
  or mandatory notification permission.
- Capture beforeinstallprompt before bootstrap; show CTA in authenticated
  PrimaryLayout only, native prompt on explicit click, versioned per-origin
  browser deferral and menu reopen, hide when installed/standalone.
  No ineffective CTA without event. iOS manual guidance needs verification.
- Docker copies public; nginx serves actual manifest/worker with correct MIME,
  missing-file 404, no-cache/revalidation, preserved SPA fallback. Registration
  failures are observable.

Owners: [BackOffice SPA](../../frontend/back-office-spa/), main/installPrompt
service, PrimaryLayout/Header, manifest/icons/nginx/Docker and existing push.
Editor diagnostics found no TypeScript errors; Vitest stalled before collection
and Vite build had no observable completion. No passing runner/build claim.

Gates: HTTPS static assets/MIME, Chromium desktop/Android and Safari/iOS
install/dismiss/relaunch/logout/deep links/push continuity, 992px boundary,
keyboard/screen reader. SVG may need equivalent PNG fallback.
Use narrow Impeccable Operate-mode audit/harden of the existing shell, not redesign.
Rollback prior SHA; registered workers can persist until valid update.
Never erase subscriptions or user data as cleanup.
