# Public/Admin Transport Isolation

## Status And Scope

READY slice 7 is implemented and locally verified (2026-10-01). This closes the bounded transport-isolation authoring work, not all TD-019 or frontend/release gates. Cache reliability, shop feature/security refactoring, desktop DI and CI work remain separate. No deployment or production approval is implied.

Calendar contracts/revisions/services and Shooting Range security/session work already present in the worktree were preserved. No model, route, backend, visual, persisted-data or active Pages form changes were required. Consumer bundles were emitted under `$env:TEMP/morwalpiz-slice7`, not tracked `dist`/`dist-ssr`; consumer typechecks used `--noEmit --incremental false`.

## Current Contract

- `createApiClient({ mode, baseUrl? })` owns its credentials configuration, token provider, CSRF promise, selected channel and unauthorized callback. Explicit `baseUrl` belongs to that client; otherwise the existing runtime `window.ENV.VITE_API_BASE_URL`, runtime `API_BASE_URL`, build `VITE_API_BASE_URL`, relative-path priority is retained. Each call captures its base URL before asynchronous CSRF acquisition.
- `publicApiService` and public HTTP aliases always use `credentials: 'omit'`. They neither read bearer providers/local-storage tokens nor attach administrator channel/CSRF context. Caller-supplied Authorization, Cookie/Cookie2, X-Channel-Id and X-CSRF-TOKEN headers are stripped. Public 401 responses cannot invoke admin recovery.
- `adminApiService`, existing named HTTP methods and the default `apiService` HTTP methods use cookies only (`include`), regardless of legacy configuration setters. Bearer injection is ignored/stripped. Existing channel-scoped routes, collection exceptions and browser channel selection persistence remain. Unsafe requests acquire CSRF, except the existing login/CSRF exceptions. Session transitions retain `resetCsrfToken`; admin non-auth 401 recovery also handles non-JSON responses.
- Legacy configuration exports (`setAuthTokenProvider`, `setRequestCredentialsMode`, `setCookieOnlyMode`) now configure only `legacyApiService`. That instance preserves the former configurable credentials, bearer/local-storage fallback, CSRF and response behavior for frozen shop helpers. Its own channel/unauthorized/session methods remain available on the object. It cannot configure public or admin requests.
- Factory-created clients do not persist admin channel selections. The existing named admin channel exports own BackOffice browser persistence. Public clients never initialize a selected channel from storage. For SSR code requiring mutable channel/session configuration, create a client per incoming request and pass an explicit base URL; do not mutate the browser/admin singleton for server request state. The active public SSR entry points no longer mutate shared credentials at module initialization.
- A CSRF promise is shared only within its client and base URL. Reset, failed acquisition and URL changes allow reacquisition; an older failed promise cannot clear a newer session's promise.
- Existing export names, positional HTTP signatures, query/body/FormData handling, data-envelope extraction, full-response option, blobs, 204 handling, error-return objects and network rejection remain. No API endpoint/DTO changes or new application store were introduced.

## Changed Files

Shared ownership (all paths below are workspace-relative):

| File | Reason |
|---|---|
| `frontend/fe-packages/services/src/apiService.ts` | Reuse the existing transport/parser in per-client closures; expose public/admin/legacy clients; route public Forms/Survey/Ask/FAQ/Navigation/Newsletter helpers explicitly. |
| `frontend/fe-packages/services/src/index.ts` | Add factory/client/public aliases and options type; preserve existing exports, including prior Calendar/Blog work. |
| `frontend/fe-packages/services/src/blogService.ts` | Public Blog reads use public transport; admin revisions/uploads retain admin transport. |
| `frontend/fe-packages/services/src/channelNewsService.ts` | Public ChannelNews reads use public transport. |
| `frontend/fe-packages/services/src/videoChannelMap.ts` | Public channel-map reads use public transport. |
| `frontend/fe-packages/services/src/shopService.ts` | Binding-only change to the legacy instance; no shop endpoint/payload/identity changes. |
| `frontend/fe-packages/services/src/apiService.test.ts` | Characterize response/error contracts, all 20 active public helpers, legacy shop, interleaved clients, SSR, URL resolution, headers, CSRF and recovery. |
| `frontend/fe-packages/services/vitest.config.ts` | Explicit Node runner collecting the existing package test files. |
| `frontend/fe-packages/services/package.json` | Test script and existing-workspace Vitest 3.2.6 dependency. |
| `frontend/yarn.lock` | Reconciled by Yarn offline install; retain unrelated dependency changes already present. |

Consumer ownership:

| Exact Files | Reason |
|---|---|
| `frontend/back-office-spa/src/main.tsx` | Remove the global cookie-only toggle; named admin transport is intrinsically cookie-only. |
| `frontend/back-office-spa/src/services/apiService.csrf.test.ts` | Real CSRF stubs plus negative public browser-token/channel case. |
| `frontend/back-office-spa/src/routes/blog/transport.test.ts` | Preserve revision/body/multipart assertions while including the admin CSRF acquisition. |
| `frontend/ask.client/src/main.tsx`, `frontend/ask.client/src/entry-server.tsx` | Remove shared credential mutation; existing Ask helpers are intrinsically public. |
| `frontend/morwalpizvideo.client/src/main.tsx`, `frontend/morwalpizvideo.client/src/entry-server.tsx` | Remove shared credential mutation in browser/SSR startup. |
| `frontend/morwalpizvideo.client/src/services/apiKeys.ts`, `calendar.ts`, `compilations.ts`, `matches.ts`, `pages.ts`, `products.ts`, `quickLinks.ts`, `sponsors.ts`, `stream.ts` (same directory) | Import public HTTP aliases; preserve function bodies and routes, including existing legacy management-route ownership. |
| `frontend/shooting-ita-frontend/src/entry-server.tsx` | Remove shared credential mutation during SSR initialization. |
| `frontend/shooting-ita-frontend/src/routes/popular/loader.ts` | Explicit public HTTP alias. |
| `frontend/shooting-ita-frontend/src/services/quickLinks.ts`, `frontend/shooting-ita-frontend/src/services/shootingItaVideoService.ts` | Explicit public HTTP aliases; composition behavior unchanged. |
| `frontend/shooting-ita-frontend/src/__tests__/shootingItaVideoService.test.ts` | Mock the new public alias while retaining composition assertions. |

Documentation: this report, `docs/architecture/refactoring-roadmap.md`, `docs/architecture/technical-debt.md`, and `docs/architecture/security.md` distinguish implementation/source-test coverage from full consumer/release evidence.

## Executed Verification

Commands ran from the repository root unless specified. These are fresh local results, not inherited Expert evidence.

| Command | Result |
|---|---|
| `yarn --cwd frontend/fe-packages/services test` | 2 files, 18 tests passed (16 transport/response + 2 existing insights). Initial characterization before transport edits: 12 passed. |
| `yarn --cwd frontend/fe-packages/services tsc --noEmit --target ES2020 --module ESNext --lib 'ES2020,DOM' --strict --skipLibCheck --moduleResolution bundler src/apiService.test.ts src/vite-env.d.ts` | Passed; compiles test and source graph rather than only the library's test-excluding build. Initial unquoted PowerShell comma argument failed before compilation; quoted rerun passed. |
| `yarn --cwd frontend/back-office-spa test src/services/apiService.csrf.test.ts src/services/authService.test.ts src/contexts/ChannelContext.test.tsx src/routes/blog/transport.test.ts src/routes/calendarEvents/routes.test.ts src/routes/channels/__tests__/routing.test.ts` | 6 files, 31 passed; pretest rebuilt models/services. Initial 3 CSRF-stub failures were repaired locally and the same set rerun successfully. No full admin-suite claim. |
| `yarn --cwd frontend/shooting-ita-frontend test` | 12 files, 32 passed. |
| `yarn --cwd frontend/morwalpizvideo.client test` | 1 file, 4 passed. |
| `yarn --cwd frontend/ask.client test` | 1 file: 2 passed, 2 failed. Existing error-path SSR tests expect resolved HTML; the renderer/router propagates the Response/Error instead. Mocked transport and renderer logic are unchanged by this slice; no unrelated SSR behavior/test rewrite. |
| `yarn --cwd frontend build:shared` | Models, services, layout passed in dependency order. Initial local declaration error (`ResponseOptions` inside factory) was repaired and the ordered build rerun successfully. |
| `yarn --cwd frontend install --offline --ignore-scripts` | Passed; existing peer/mixed-lockfile warnings, no scripts/secrets/cloud access. |

Consumer source gates used the exact command `yarn --cwd frontend/<consumer> tsc -p tsconfig.app.json --noEmit --incremental false`:

- `ask.client`, `morwalpizvideo.client`, `shooting-ita-frontend`, `morwalpiz-shop.client`: passed.
- `back-office-spa`: failed with the same 8 pre-existing errors in 4 files: ToastNotification test callback return (1), product fixture missing creationDateTime (1), duplicate sponsor edit Component exports (2), sponsor action fixtures/mock typing (4). None are transport errors; not repaired.

Consumer browser bundles used `yarn --cwd frontend/<consumer> vite build --outDir "$env:TEMP/morwalpiz-slice7/<output>"`:

- `back-office-spa` -> `back-office`, `ask.client` -> `ask`, `morwalpizvideo.client` -> `public-video`, `shooting-ita-frontend` -> `shooting-ita`: passed. Vite-only admin success does not override the failed full typecheck/build gate.
- `morwalpiz-shop.client` -> `shop`: failed in existing `src/main.scss` Sass processing (`[sass] undefined`), not TypeScript/import resolution. Shop source compatibility passed, transport legacy tests passed, but full shop bundle compatibility is not certified. No shop stylesheet/config edits.

SSR bundles used `yarn --cwd frontend/<consumer> vite build --mode ssr --ssr src/entry-server.tsx --outDir "$env:TEMP/morwalpiz-slice7/<output>"`: `ask.client` -> `ask-ssr`, `morwalpizvideo.client` -> `public-video-ssr`, `shooting-ita-frontend` -> `shooting-ita-ssr` all passed.

Focused lint:

- `yarn --cwd frontend/back-office-spa eslint src/main.tsx src/services/apiService.csrf.test.ts src/routes/blog/transport.test.ts`: passed.
- `yarn --cwd frontend/shooting-ita-frontend eslint src/entry-server.tsx src/routes/popular/loader.ts src/services/quickLinks.ts src/services/shootingItaVideoService.ts src/__tests__/shootingItaVideoService.test.ts`: passed.
- `yarn --cwd frontend/morwalpizvideo.client eslint src/main.tsx src/entry-server.tsx src/services/apiKeys.ts src/services/calendar.ts src/services/compilations.ts src/services/matches.ts src/services/pages.ts src/services/products.ts src/services/quickLinks.ts src/services/sponsors.ts src/services/stream.ts`: failed only on 7 pre-existing `no-explicit-any` annotations in `apiKeys.ts`; that file's sole slice change is its transport import.
- VS Code test discovery found no tests for the requested admin paths; validation used the owning CLI above. Editor diagnostics for touched transport/test files were clean.

## Residual Gates

Aggregate executed consumer/package tests: 87 passed, 2 failed, across 22 files (21 passed, 1 failed). This is not an all-frontend/full-admin green gate. Preserve and resolve the documented full admin typecheck, Ask SSR error-path, public API-key lint and frozen shop Sass gates under their respective owners/scopes.

Browser cookie/CSRF/CORS and deployed-origin verification were not performed. Node tests prove request construction and independent SSR client state, not real cross-origin cookie delivery. UI was untouched, so no Impeccable redesign/visual workflow was invoked. Target browser evidence remains a release requirement. No Mongo check is needed for this transport-only slice; existing Range/Calendar Mongo release evidence remains unmet and unchanged. No commit, deployment, credential read or production access occurred.