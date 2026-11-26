# Dev Container browser and authenticated Workshop handoff

## Outcome

Make a rebuilt VS Code Dev Container capable of launching the repository's
version-matched headless Chromium and capturing an authenticated local Workshop
page. Preserve normal authentication; do not make protected pages anonymous.

The selected target is Shooter Workshop. Other applications require their own
login and data setup.

Completion stops after the container prerequisites, Workshop local access,
focused browser proof, and directly related documentation are in place. A
repository-wide browser test framework, production authentication changes, and
other applications' fixture workflows require separate approval.

## Delivered

- `.devcontainer/devcontainer.json` now uses the published .NET 11 Dev
  Container image, retains Node 22, Yarn Classic, `vscode`, and existing port
  forwards, and adds Workshop ports 5179 and 5180.
- `.devcontainer/post-create.sh` runs the frozen workspace install, installs
  Linux dependencies for lockfile-resolved Playwright Chromium, then installs
  that browser for the non-root user and generates the local ASP.NET Core
  HTTPS development certificate. Provisioning stops visibly on errors.
- `frontend/shooter-workshop.client/viteServerConfig.ts` now selects the
  standalone port/API defaults or parses AppHost `PORT` and `ASPNETCORE_URLS`.
  Wildcard listeners are converted to loopback targets for the local Vite
  proxy. `vite.config.ts` binds for container port-forwarding and keeps `/api`
  same-origin, preserving cookie and CSRF behavior.
- `frontend/shooter-workshop.client` owns `@playwright/test` and provides
  `test:browser`, with a local-only, mock-confirmed test that checks anonymous
  API denial, registers a unique disposable user through the UI, signs in,
  creates a firearm, opens the authenticated detail page, verifies the
  HttpOnly cookie and same-origin API responses, and saves a screenshot under
  `/tmp` by default. It does not save browser authentication state.
- `docs/architecture/development.md` and `docs/architecture/testing.md` now
  describe the container setup, local mock-backed startup, and browser test.

## Evidence

- `MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj` and
  `MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj` target .NET 11.
- `frontend/yarn.lock` resolves Playwright to 1.65.1. The container setup uses
  that workspace binary for both operating-system dependencies and Chromium.
- Workshop's `src/auth/AuthProvider.tsx`, `src/routes/AccountLogin/component.tsx`,
  and shared `frontend/fe-packages/services/src/publicAccountService.ts` use
  the real PublicAccount session flow and a CSRF token. Registration creates a
  session, then the registration component navigates to the login page.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` issues
  HttpOnly session cookies and validates CSRF on registration/login.
  `Controllers/Workshop/WorkshopController.cs` explicitly requires the
  PublicAccount scheme, including for the firearm detail read. The general
  development fake scheme in ServerAPI is not a substitute for this cookie.
- ServerAPI's `Program.cs` selects mock account and Workshop repositories
  when its mock flag is enabled and rejects that mode outside Development or
  Test. Mock state is process-local; a restart requires fresh data and login.
- `MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs` now initializes each
  account-owned Workshop collection for the existing mock repository.
- `MorWalPizVideo.ServerAPI/appsettings.Development.json` enables `EnableMock`;
  the browser test still requires an explicit `WORKSHOP_LOCAL_MOCK_API=1`
  acknowledgment and local base URL before it runs.
- The first browser run found that `PrimaryScenario` did not initialize any
  Workshop collections, so authenticated mock API reads failed. The scenario
  now initializes all Workshop collections as empty and
  `MockScenarioLifecycleTests` verifies each collection can be read.

## Implemented Design

1. The Dev Container uses the published `mcr.microsoft.com/devcontainers/dotnet:3-11.0`
   image. The lockfile-resolved Playwright CLI installs operating-system
   dependencies with elevated privileges and downloads Chromium into the
   `vscode` user's browser cache. `dotnet dev-certs https` creates the local
   HTTPS certificate needed by the existing ServerAPI launch profile.
2. The Workshop Vite server defaults to port 5180 and the ServerAPI HTTPS
   listener on port 7225. In AppHost it honors port 5179 and the API URL
   injected through `ASPNETCORE_URLS`. The same-origin `/api` proxy preserves
   the real cookie/CSRF flow.
3. Run ServerAPI in Development with its existing mock flag, then use the
   normal Workshop registration and login UI with a disposable test identity.
   The Playwright test creates a firearm in the UI, opens its detail, verifies
   the protected API response and captures a screenshot. It uses no committed
   credentials or persisted browser state.
4. Treat browser tooling and Chromium as separate prerequisites. A successful
   direct Playwright smoke check does not prove that the selected Copilot or
   Impeccable agent has a browser tool. Check its actual tool availability
   and execution location in the reopened container; configure an appropriate
   supported integration separately only if absent. Do not expose a browser
   debugging endpoint to the network.

Alternative: use an official Playwright image. It supplies browser libraries
but does not by itself satisfy this repository's .NET SDK, Yarn, VS Code user,
or agent-tool requirements. Prefer the existing container family with the
smallest necessary additions.

## Impacted Projects

- Dev Container: SDK, Linux browser dependencies, browser cache, port
  forwarding, and provisioning.
- `frontend/back-office-spa`: existing Playwright dependency used as the
  version source; no BackOffice product change.
- `frontend/shooter-workshop.client`: local Vite/AppHost port and proxy
  alignment and authenticated browser verification.
- `MorWalPizVideo.ServerAPI`: verify-only local mock-backed account and
  Workshop flows; no authentication bypass or API contract change.
- `MorWalPizVideo.AppHost`: verify integration; modify only if the existing
  supplied configuration cannot be honored by Workshop.
- Other frontends, backend projects, Windows-only applications, and deployed
  services: no planned behavioral changes.

## Changed Files

### Modified

- `.devcontainer/devcontainer.json`: .NET 11 base, post-create setup, and
  Workshop ports.
- `.devcontainer/post-create.sh`: frozen dependency install, Chromium system
  library setup, version-matched browser install, and HTTPS dev certificate.
- `frontend/shooter-workshop.client/vite.config.ts` and
  `viteServerConfig.ts`: standalone/AppHost port and API URL support.
- `MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs`: initialize empty
  Workshop mock collections so local account-scoped reads and writes work.
- `frontend/shooter-workshop.client/package.json`: workspace-owned Playwright
  dependency and browser-test command.
- `MorWalPizVideo.BackOffice.Tests/Features/MockScenarioLifecycleTests.cs`:
  regression coverage for all Workshop collections in the primary scenario.
- `docs/architecture/development.md` and `docs/architecture/testing.md`:
  local setup and test instructions.
- This handoff: delivered implementation and validation record.

### Added

- `frontend/shooter-workshop.client/viteServerConfig.ts` and
  `src/viteServerConfig.test.ts`: validated Vite settings and focused cases.
- `frontend/shooter-workshop.client/playwright.config.ts` and
  `e2e/authenticated-firearm.spec.ts`: local-only authenticated screenshot
  flow.

### Generated/Migration

None. No database or production migration.

### Documentation

- `docs/architecture/development.md` and `docs/architecture/testing.md`:
  document container prerequisites, local Workshop startup and mock login,
  browser/tool distinction, and verification.
- This handoff: record actual files, results, and remaining blockers after
  implementation.

### Existing Paths Reused

- `frontend/package.json`, `frontend/yarn.lock`, and
  `frontend/back-office-spa/package.json`: workspace and resolved browser
  version.
- `MorWalPizVideo.AppHost/Program.cs` and
  `MorWalPizVideo.ServerAPI/Properties/launchSettings.json`: endpoint wiring.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx`,
  `src/routes/AccountRegister/component.tsx`, `src/routes/AccountLogin/component.tsx`,
  and `src/routes/FirearmDetail/loader.ts`: unchanged user flow.
- `MorWalPizVideo.ServerAPI/Program.cs`, `Controllers/PublicAccountController.cs`,
  and `Controllers/Workshop/WorkshopController.cs`: mock selection and actual
  authorization boundary.
- `docs/architecture/shooter-workshop.md` and
  `docs/architecture/adr/ADR-018-public-account-boundary.md`: existing feature
  and account boundaries.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Existing local ServerAPI in mock mode; no new API. |
| Frontend/UI | Development-server wiring only; no visual redesign. |
| Database/Data | Isolated in-memory account and Workshop data; discarded on restart. |
| Contracts/Models | None. |
| Authentication/Authorization | Real PublicAccount cookie and CSRF; no bypass. |
| Validation | Existing registration and firearm validation remain authoritative. |
| Cache | No production cache changes; browser and transient app state stay local. |
| Configuration/Secrets | Local ports, SDK and browser dependencies; no committed credentials. |
| Logging/Telemetry | Surface installation, startup, login, proxy and browser failures. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Development container only; existing production deployment unchanged. |
| Backward Compatibility | Preserve existing development defaults, forwarded ports, workspace and other applications. |

## Reuse Opportunities

Reuse the committed Playwright dependency and Yarn lock, the existing
Development-only mock repositories, PublicAccount registration and login,
Workshop firearm creation, the Vite reverse proxy, and existing AppHost
endpoint injection. Do not duplicate authentication or create a synthetic
cookie.

## Risks

- **High — authentication exposure:** persisted browser state can impersonate
  a user. Use disposable mock identities and in-memory state; do not share
  secrets or expose browser control endpoints. Verify no credential-bearing
  artifacts enter the repository.
- **High — false security proof:** fake default auth does not authenticate
  PublicAccount endpoints. The browser test verifies anonymous denial and
  access to the signed-in test account's detail, not every cross-account
  authorization case.
- **Medium — AppHost mode:** standalone API/UI browser flow passed and the
  AppHost Vite settings are unit-tested; AppHost itself was not run end to end.
- **Medium — unavailable agent integration:** installing Chromium does not
  guarantee a browser tool exists in a particular chat agent. Verify the
  actual tool in the remote container and report a separate blocker.
- **Low — mock lifecycle:** restarting ServerAPI invalidates in-memory data
  and sessions. Create fresh fixtures and verify a populated detail response.

## Technical Debt

- Historical duplicate lockfiles and broader browser test coverage are
  unrelated; defer.

## Implementation and Validation

- Published image tag and amd64/arm64 manifest verified; `.devcontainer`
  JSON and post-create shell syntax passed.
- Frozen Yarn workspace contains Playwright 1.65.1; direct CLI reports
  version 1.65.1.
- Focused Vite settings tests: 4 passed.
- Shooter Workshop unit tests: 76 passed.
- Shooter Workshop production build: passed.
- Playwright test discovery: 1 test found.
- Focused TypeScript check for Vite and Playwright files: passed.
- ServerAPI/Domain test build passed; focused `MockScenarioLifecycleTests`:
  5 passed.
- Scoped `git diff --check`: passed.
- The first clean container provisioning attempt exposed that `sudo` filters
  Node from `PATH`; preserving `PATH` for the privileged dependency installer
  fixed it. Clean container provisioning then passed and installed Playwright
  1.65.1 Chromium plus its Linux dependencies.
- API startup initially exposed a missing ASP.NET HTTPS development
  certificate. `dotnet dev-certs https` generated one successfully, and the
  API then started with its existing HTTPS launch profile.
- Authenticated Playwright flow passed end to end in the container (1/1):
  anonymous API request returned 401; disposable registration, UI login,
  HttpOnly session cookie, firearm creation and same-origin detail read
  succeeded. Screenshot `/tmp/workshop-authenticated-firearm.png` was
  generated (134,485 bytes).
- The selected Copilot/Impeccable browser-tool availability remains
  unverified; Playwright success alone does not provide that tool.

## Migration And Rollback

No persistent data, contract, cache, or authentication migration. Local mock
state is disposable and recreated after restart. Roll back the container and
development-server wiring together if startup regresses; retain existing
production configuration and cookies. Chromium downloads may require network
access on initial provisioning. Do not replace browser sandboxing with
privileged container execution as a workaround.

## Open Questions

- Which browser integration is enabled for the user's Copilot/Impeccable
  session after reopening the container? It must be verified, not inferred from
  the installed browser.
