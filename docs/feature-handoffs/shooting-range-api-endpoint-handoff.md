# Shooting Range API endpoint handoff

**Status:** Analysis only; implementation, deployment and live verification not performed.

## Outcome

Make the Shooting Range browser call its own backend rather than the client host. Preserve Range's existing SSR, independent API and credentialed authentication. Assume the intended deployment uses separate HTTPS client and API hosts; their actual settings and health have not been verified.

## Current Evidence

- [Range server](../../frontend/shooting-range.client/server.mjs) generates `env-config.js` at startup from runtime `API_BASE_URL`, substituting an empty string when absent. [index.html](../../frontend/shooting-range.client/index.html) loads it before the client.
- [Range API client](../../frontend/shooting-range.client/src/api.ts) selects that runtime value ahead of `VITE_API_BASE_URL`, even when empty, and otherwise requests relative `/api/shooting-range/...` URLs. Requests include credentials and unsafe requests obtain CSRF tokens.
- [Range Dockerfile](../../frontend/shooting-range.client/Dockerfile) runs an Express SSR server and does not bake in an API URL. Its [Vite configuration](../../frontend/shooting-range.client/vite.config.ts) proxies `/api` only in development; the production Express server does not proxy it.
- The [Range workflow](../../.github/workflows/deploy-shooting-range-client.yml) builds and pushes an image, but its deployment step is disabled. Neither it nor the Dockerfile sets a runtime App Service value. This does not prove what is set in the live host.
- [Workshop's workflow](../../.github/workflows/deploy-shooter-workshop-client.yml) passes a build argument, but its [Dockerfile](../../frontend/shooter-workshop.client/Dockerfile) deliberately builds a relative-path SPA; its [Nginx template](../../frontend/shooter-workshop.client/nginx.conf.template) proxies `/api/` to a runtime upstream. That is a different transport, not simply a missing Vite variable.
- The [Range API](../../MorWalPizVideo.ShootingRange/Program.cs) permits credentialed CORS from one exact client origin and issues Secure, SameSite=None cookies. [Deployment architecture](../architecture/deployment.md) already specifies runtime `API_BASE_URL` for Range.

**Likely mechanism:** an absent or empty runtime setting yields relative requests, which go to the client host. Confirm by inspecting the deployed `env-config.js` and browser network requests; deployed configuration and any external proxy are unknown.

## Proposed Design

Immediate operational recovery: set the client App Service's `API_BASE_URL` application setting to the actual **Shooting Range API HTTPS origin**, not the Workshop/ServerAPI or client origin; apply/restart according to the host's normal process. Verify the setting takes effect on every instance by fetching `env-config.js`, then inspect browser request destinations and test login and CSRF. Do not put credentials in the URL. This existing setting needs no new image when the deployed image contains the described server.

For durable prevention, preserve runtime configuration rather than copying Workshop's Nginx implementation or baking environment-specific URLs into a reusable image. Make the production Range server reject missing, empty, malformed, credential-bearing or non-HTTPS API origins at startup, with a clear error; keep local Vite development and its relative proxy intact. Safely serialize the public origin into the JavaScript configuration rather than interpolating raw text. Do not silently fall back to a relative production endpoint. If same-origin production proxying is actually intended, stop and agree on a different deployment design rather than weakening the guard.

The workflow remains image-only; do not enable deployment or introduce a GitHub build variable as a substitute for the runtime host setting. Document the required App Service setting and its validation. Production SSR remains neutral and must not fetch private data during rendering.

## Impacted Projects

- `frontend/shooting-range.client`: startup configuration, browser transport and focused tests.
- Range client delivery workflow: verify-only; no deployment activation.
- Range API and its tests: verify-only for existing CORS, cookies and CSRF; no API change assumed.
- Workshop, shared frontend packages, databases and AppHost: verify-only or unaffected; no shared behavior changes.

## Impacted Files

### Modify

- [server.mjs](../../frontend/shooting-range.client/server.mjs): validate and safely publish runtime configuration.
- [api.test.ts](../../frontend/shooting-range.client/src/api.test.ts): exercise configured origin and existing relative development behavior.

### Add

- Focused server startup/configuration tests in the Range client's existing test area, if needed to exercise actual startup and generated configuration.

### Generated/Migration

None.

### Documentation

- [Shooting Range architecture](../shooting-range-architecture.md) and [deployment architecture](../architecture/deployment.md): record the production requirement, failure behavior and operational verification.
- Update this handoff with actual changes, checks and residual risks.

### Verify Only

- [Range API client](../../frontend/shooting-range.client/src/api.ts), [HTML entry](../../frontend/shooting-range.client/index.html), [Dockerfile](../../frontend/shooting-range.client/Dockerfile), [workflow](../../.github/workflows/deploy-shooting-range-client.yml), [Vite configuration](../../frontend/shooting-range.client/vite.config.ts), [Range API startup](../../MorWalPizVideo.ShootingRange/Program.cs), and [API workflow](../../.github/workflows/deploy-shooting-range-api.yml).

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No endpoint changes; verify backend health and actual target. |
| Frontend/UI | No redesign; runtime URL directs existing browser requests. SSR and local Vite proxy remain intact. |
| Database/Data | None. |
| Contracts/Models | None. |
| Authentication/Authorization | Preserve Range cookies, CSRF and exact-origin CORS. Separate-site browser cookie policies can still block login. |
| Validation | Require an absolute HTTPS origin without credentials or URL path, query or fragment in production; fail visibly. |
| Cache | No API cache change; verify generated configuration is refreshed on all instances and not stale in browser/intermediary caches. |
| Configuration/Secrets | `API_BASE_URL` is a public runtime setting, not a secret; do not commit deployed values or credentials. |
| Logging/Telemetry | Report invalid or missing configuration without logging sensitive URL contents. |
| Background Jobs | None. |
| Deployment/Operations | Host setting and instance restart/reconfiguration required; existing image workflows do not deploy. |
| Backward Compatibility | Existing valid runtime configuration remains supported; production startup without it intentionally fails. |

## Reuse Opportunities

Reuse the existing runtime `API_BASE_URL`, Range API client, Vite development proxy, and existing API and authentication tests. Do not share Workshop auth, copy its proxy blindly, or add a new configuration mechanism.

## Risks

- **High — unverified production setting or host:** source explains the symptom but not the actual deployed state. Inspect `env-config.js`, request target and host settings before claiming recovery.
- **High — cross-site cookies/CORS:** a correct target alone does not ensure login; verify exact origin, credentialed CORS, cookies, CSRF and browser third-party-cookie behavior. Do not broaden CORS as a shortcut.
- **Medium — fail-fast rollout:** an existing production instance without the setting will fail on replacement. Set and verify configuration before rollout; retain the prior compatible image for rollback.
- **Medium — stale configuration or multiple instances:** verify each instance and browser sees the current setting; do not cache the generated config indefinitely.
- **Low — external dependency availability:** confirm Range API readiness and TLS; an unreachable backend is a separate failure from wrong URL selection.

## Technical Debt

The current raw JavaScript interpolation and empty-string fallback are directly related and should be addressed. Workshop/Range transport convergence, broader Range authorization release blockers and unrelated deployment activation remain separate work.

## Implementation Roadmap

1. **Operations:** confirm the deployed client image and actual backend host, set runtime `API_BASE_URL`, and verify generated configuration and browser network destinations. Stop if backend identity or allowed origin is uncertain.
2. **Range client:** implement production startup validation and safe configuration serialization; preserve local development and existing API/auth behavior.
3. **Validation:** run focused Range tests and client/SSR build; test a production image with valid and missing/invalid settings and verify direct SSR navigation and API origin selection.
4. **Documentation and rollout:** update operational documentation, set the host configuration before replacing instances, and perform browser auth/CSRF checks. Do not enable the disabled deployment steps.

Completion boundary: correct Range API destination, explicit configuration failure, preserved session flow, and focused validation. A proxy redesign, CORS expansion, deployment activation or public-release approval requires separate authorization. An independent review is optional for this bounded change.

## Testing Strategy

- Unit/component: configured browser URL, encoded query parameters, credentials and CSRF behavior; retain relative local development behavior.
- Startup/container: valid HTTPS origin produces valid configuration; missing, blank, malformed, credential-bearing, non-HTTPS and non-origin URLs fail visibly; special input cannot inject script; preserve SSR and direct navigation.
- Integration/browser: exact deployed origin, preflight where applicable, CSRF acquisition, login, session restore and logout; check network destinations and avoid logging credentials. Verify missing/backend failures remain visible.
- Contract, database and migration tests: not applicable unless implementation changes their existing boundaries. No evidence of live validation yet.

## Migration And Rollback

No schema, contract, cache or auth migration. Configure the runtime setting first, then deploy the compatible image when separately approved. Monitor startup errors and browser authentication. Restore the prior compatible image and its previous valid runtime setting if needed; do not restore permissive CORS or expose secrets. Existing public-release gates remain in force.

## Open Questions

- What is the actual deployed Shooting Range API HTTPS origin, and does the client host already have `API_BASE_URL` configured?
- Is the browser using the exact client origin allowed by Range CORS, and do its cookie policies permit this cross-site session?
- Is there an external same-origin proxy not represented in this repository? If so, reconsider the direct-origin production guard before implementation.
