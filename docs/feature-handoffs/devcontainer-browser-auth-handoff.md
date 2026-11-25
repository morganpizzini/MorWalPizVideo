# Dev Container browser and authenticated Workshop handoff

## Outcome

Make a rebuilt VS Code Dev Container capable of launching the repository's
version-matched headless Chromium and capturing an authenticated local Workshop
page. Preserve normal authentication; do not make protected pages anonymous.

Assumption: the selected Shooter Workshop is the first target. The quoted
earlier screenshot failure does not identify its application or establish a
Playwright installation failure. Other applications require their own login
and data setup.

Completion stops after the container prerequisites, Workshop local access,
focused browser proof, and directly related documentation are in place. A
repository-wide browser test framework, production authentication changes, and
other applications' fixture workflows require separate approval.

## Current Evidence

- `.devcontainer/devcontainer.json` uses the .NET 10 Bookworm image, Node 22,
  Yarn Classic, the `vscode` user, and a frozen workspace install. It does not
  install browser dependencies or forward Workshop ports.
- `MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj` and
  `MorWalPizVideo.AppHost/MorWalPizVideo.AppHost.csproj` target .NET 11.
- `frontend/back-office-spa/package.json` already declares `@playwright/test`;
  `frontend/yarn.lock` resolves it to 1.65.1. The Workshop workspace does not
  declare Playwright, and no Playwright configuration was found in the
  frontend. The existing Yarn lockfile, not the duplicate npm lockfile, owns
  the container's current install.
- `frontend/shooter-workshop.client/vite.config.ts` defaults to port 5180 and
  proxies `/api` to HTTPS localhost port 7140. ServerAPI's
  `Properties/launchSettings.json` instead declares HTTPS port 7225. The
  AppHost declares Workshop port 5179 and supplies an API endpoint as an
  environment variable, but the current Workshop Vite configuration does not
  read that variable or the declared port. Verify actual effective listeners;
  neither wiring should be assumed to work without adjustment.
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
- `frontend/shooter-workshop.client/src/routes/FirearmDetail/loader.ts` loads
  detail by account-owned ID. A new account needs a corresponding firearm to
  reach a populated detail page.

## Proposed Design

1. Use a .NET 11-compatible Dev Container base verified to support the target
   host architecture, retaining Node 22, Yarn Classic, current development
   environment, forwarded ports, and the non-root `vscode` user. Do not assume
   an unverified image tag exists; select a published suitable image during
   implementation.
2. Install the Linux libraries required by the **lockfile-resolved Playwright
   version**, then install its Chromium/headless browser under a cache readable
   by `vscode`. Use the existing workspace's Playwright dependency, rather than
   an independently versioned global install. Ensure provisioning fails
   visibly if dependency or browser installation fails, and is repeatable on
   rebuild. Keep the existing frozen Yarn installation.
3. Forward the Workshop standalone and AppHost ports, and align its Vite
   proxy and port with the selected ServerAPI and AppHost endpoints. Preserve
   standalone defaults and honor the AppHost-provided endpoint and port; test
   both paths. Use the same-origin `/api` proxy so the browser receives its
   normal cookie and CSRF behavior. Avoid public-facing port bindings and
   broad CORS changes.
4. Run ServerAPI in Development with its existing mock flag, then use the
   normal Workshop registration and login UI with a disposable test identity.
   Create a firearm through the normal UI, open its detail, wait for the real
   data, and capture a screenshot. Do not commit credentials, session state,
   tokens, or real user data. Browser session state is a secret and should
   normally remain in memory; if a file is necessary, keep it outside the
   repository with restrictive permissions and delete it after use.
5. Treat browser tooling and Chromium as separate prerequisites. A successful
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

## Impacted Files

### Modify

- `.devcontainer/devcontainer.json`: supported SDK base, browser provisioning
  integration, and Workshop ports; retain existing settings.
- `frontend/shooter-workshop.client/vite.config.ts`: use supplied local
  endpoint and port while retaining safe standalone defaults.

### Add

- A `.devcontainer`-owned provisioning artifact only if needed to install
  system browser libraries and the matching browser reproducibly. Keep OS
  package installation separate from the `vscode` user's browser cache.

### Generated/Migration

None. No database or production migration.

### Documentation

- `docs/architecture/development.md` and `docs/architecture/testing.md`:
  document container prerequisites, local Workshop startup and mock login,
  browser/tool distinction, and verification.
- This handoff: record actual files, results, and remaining blockers after
  implementation.

### Verify Only

- `frontend/package.json`, `frontend/yarn.lock`, and
  `frontend/back-office-spa/package.json`: workspace and browser version.
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
  PublicAccount endpoints. Verify unauthenticated access is denied and the
  real login cookie grants only the test account's data.
- **Medium — endpoint mismatch:** AppHost, Vite, and ServerAPI currently
  declare inconsistent ports. Verify effective listeners, same-origin
  requests, and both standalone and AppHost modes.
- **Medium — browser mismatch:** package, browser cache, Linux libraries,
  architecture, and user permissions can disagree. Rebuild and launch the
  exact installed version as `vscode`.
- **Medium — unavailable agent integration:** installing Chromium does not
  guarantee a browser tool exists in a particular chat agent. Verify the
  actual tool in the remote container and report a separate blocker.
- **Low — mock lifecycle:** restarting ServerAPI invalidates in-memory data
  and sessions. Create fresh fixtures and verify a populated detail response.

## Technical Debt

- The .NET 10 container image lags the projects' .NET 11 targets: address now
  because local ServerAPI execution is required.
- Workshop's AppHost and Vite endpoint settings diverge: address only the
  local wiring needed for this outcome.
- Historical duplicate lockfiles and broader browser test coverage are
  unrelated; defer.

## Implementation Roadmap

1. Verify a published .NET 11-compatible image and target architecture; update
   container configuration without discarding existing development features.
2. After frozen workspace installation, provision version-matched Playwright
   Chromium and its Linux dependencies; ensure the non-root user can launch it.
   Rebuild the container and verify SDK, Node, Yarn, and browser launch.
3. Align Workshop Vite's port and proxy with AppHost configuration and preserve
   standalone behavior. Forward the two Workshop ports. Verify the browser
   reaches the intended local ServerAPI in both modes.
4. Start isolated mock-backed ServerAPI and Workshop; register a disposable
   account, log in, create account-owned detail data, and capture the requested
   page. Verify anonymous denial and authenticated data isolation.
5. Verify the selected chat agent can access a browser in the container; if it
   cannot, report the missing integration independently of the direct
   Playwright result.
6. Update development and testing documentation and this handoff with actual
   outcomes. An independent review is appropriate if container provisioning or
   authorization behavior changes beyond local wiring; the user decides.

## Testing Strategy

- Container: clean rebuild, frozen dependency installation, correct .NET SDK,
  browser dependency resolution, non-root headless launch and screenshot.
- Workshop: standalone and AppHost listener/proxy checks, real registration,
  login, session restore, populated detail and logout.
- Security: anonymous detail denied, invalid login rejected, account A cannot
  read account B's record, CSRF remains required, no secrets in artifacts.
- Negative cases: unavailable API, missing browser executable or library,
  expired session, mock server restart, absent record, and absent agent browser
  tool must produce visible failure rather than a success-shaped screenshot.
- Responsive/accessibility: capture desktop and narrow viewport evidence for
  the same populated page and check basic keyboard navigation. This verifies
  access, not an interface redesign or full accessibility audit.
- Existing focused Workshop tests and build: run where the Vite configuration
  changes; no full-suite expansion unless focused results require it.

## Migration And Rollback

No persistent data, contract, cache, or authentication migration. Local mock
state is disposable and recreated after restart. Roll back the container and
development-server wiring together if startup regresses; retain existing
production configuration and cookies. Chromium downloads may require network
access on initial provisioning. Do not replace browser sandboxing with
privileged container execution as a workaround.

## Open Questions

- Is Shooter Workshop the intended screenshot target, or was the quoted detail
  page in another application? That changes the authentication and fixture
  path.
- Which browser integration is enabled for the user's Copilot/Impeccable
  session after reopening the container? It must be verified, not inferred from
  the installed browser.
