- The SPA stores display-only user information in local storage; the browser JWT remains in the HttpOnly cookie. The BackOffice client does not read `localStorage.authToken` or emit `Authorization: Bearer`; API-key and explicit non-browser bearer callers remain supported.
# Security Architecture

## Security Boundaries

| Boundary | Trust model |
|---|---|
| BackOffice SPA to BackOffice | Authenticated administrator |
| WPF tools to BackOffice | Authenticated API-key client |
| Public apps to ServerAPI | Explicit anonymous public routes; host-owned authenticated fallback for undecorated endpoints |
| Shooting Range client to Shooting Range API | Current-state revocable cookie sessions; anonymous registration/domain cutover still blocked |
| BackOffice to ServerAPI | Authenticated internal service call |
| Followers to ShortLinks | Anonymous untrusted input |
| APIs to Mongo/Blob/external providers | Managed service credentials |

## Immediate Secret Incident

Secret-bearing material exists in tracked BackOffice artifacts, VideoImporter migrations/settings, and development data. Assume exposed credentials are compromised until proven otherwise.

Required response:

1. Inventory every affected credential without copying values into tickets or docs.
2. Revoke and rotate service-account credentials, API keys, JWT secrets, storage credentials, and related tokens.
3. Remove secret-bearing files and seed values from current source.
4. Rewrite repository history where organizational policy permits.
5. Invalidate published build artifacts and caches containing old material.
6. Add automated secret scanning and protected configuration checks.
7. Record completion and owners outside this public architecture guide.

Rotation must occur before relying on source cleanup alone.

Both API projects now mark `credentials*.json` at any depth as `CopyToOutputDirectory=Never` and `CopyToPublishDirectory=Never`; Docker context also excludes those files. Offline source files are not deleted. Evaluated non-secret sentinel tests verify the two copy metadata values, not historical artifact removal. Use a clean publish destination: stale output and previously published artifacts still require operator review.

`YTService` retains its existing constructor callers and factory-managed HTTP client. `YouTube:CredentialsPath` (`YouTube__CredentialsPath` in environment configuration) selects an absolute, operator-provisioned credential file outside the application output directory. Provision it through a protected read-only mount with access limited to the service identity; do not upload it as application content or log its contents. Non-Development hosts require this external setting. Development/direct offline construction retains the legacy `credentials.json` path; no real credential file was read during verification. Historical credential rotation and artifact invalidation remain open.

## ServerAPI Host Containment

Fake authentication and the developer exception page require both actual `Development` and `EnableDev`. Staging, Production and Test ignore that flag for these controls and use JWT authentication with non-detailed exception responses. Configuration/database diagnostics have the same two-part gate and no longer return connection prefixes, database names or provider exception messages.

The ServerAPI-only fallback requires an authenticated principal. The inventory supporting this change preserves existing access metadata without changing routes or the host-neutral controller base:

- Explicit anonymous reads/interactions: Ask, Blog, CalendarEvents, Competitions, Compilations, Configuration, CustomForms/surveys, Matches, Navigation, Newsletter, newsletter webhooks (their existing verification remains), Pages, Products, QuickLinks, ShootingIta, Sponsors and V1Videos.
- FAQ reads and Push public-key are explicit anonymous exceptions; FAQ vote and Push subscription/unsubscription retain their authentication requirements.
- ShopAuth, ShopCart, ShopCatalog and ShopOrders retain their explicit anonymous compatibility metadata. This slice does not approve their security posture or reopen the shop hold.
- Cache operations retain their dedicated internal-service authentication scheme. Health endpoints retain explicit anonymous probe metadata. Feature-controlled OpenAPI/Swagger behavior is unchanged.
- ConfigTest is an explicit anonymous technical exception only to allow the environment/flag gate to return `404` outside Development; the gate remains authoritative.

`HostSecurityContainmentTests` inventories MVC endpoint metadata and exercises the actual host authentication/error matrix; public-page regression coverage remains. This is not a production readiness certification or a change to Range's pending anonymous cutover.

Current desktop seed source and migration artifacts use non-secret placeholders. This narrows current-source exposure but does not revoke historical credentials, remove them from repository history or old artifacts, or prove that old credentials fail. Those actions remain administrator-owned and require independently verifiable redacted evidence.

## Administrative Authentication

BackOffice uses JWT bearer and can read a secure cookie. Implemented browser posture:

- HttpOnly, Secure, `SameSite=None` `auth_token` cookie for the separate HTTPS SPA/API origins.
- Explicit credentialed CORS for `https://morwalpiz-admin-spa.azurewebsites.net` only.
- `X-CSRF-TOKEN` protection for unsafe requests carrying the auth cookie, including logout.
- Bearer-only and API-key-only requests remain outside the browser-cookie CSRF flow.
- Short token lifetime, server-side revocation strategy where required, and audited login throttling.

The SPA stores display-only user information in local storage; the browser JWT remains in the HttpOnly cookie.

### Shared Transport Isolation

Slice 7 makes the shared named admin HTTP methods intrinsically cookie-only with client-owned CSRF/channel/401 recovery. Public video, Ask and Shooting ITA use explicit public helpers/aliases with `credentials: omit`, without reading token providers/local-storage tokens or carrying admin channel/CSRF context; even manually supplied Authorization/Cookie/context headers are stripped. The frozen shop retains its independent configurable legacy instance. Existing routes, API authentication policies and payload/error contracts are preserved; legacy transport setters cannot reconfigure the new admin/public instances.

SSR callers needing mutable request context must create a client per incoming request with an explicit base URL, never mutate the browser admin singleton. Active public SSR entry points no longer set global credentials. Interleaved-client, request-local SSR, browser-token negative, CSRF and unauthorized-callback tests establish local request construction, not browser/deployed CORS/cookie proof. See [transport isolation](transport-isolation.md) for the exact source/test coverage, failed full-consumer gates and outstanding browser release evidence.

## BackOffice RBAC

BackOffice authorization uses `AllowUserAttribute` with dynamic policy resolution and evaluates access using a normalized lowercase model:

- Real MongoDB `userGroups` documents (`UserGroup`) define reusable group codes and permission keys.
- Each user can belong to multiple groups via `User.GroupIds`.
- Users can have direct permission keys via `User.DirectPermissions`.
- Effective permissions are the normalized, transitive expansion of direct permissions union active-group permissions.
- Legacy `User.CanAccessBackoffice` is treated as canonical permission key `backoffice.access` for backward compatibility.
- `users.manage` directionally implies `users.view`, `users.create`, `users.update`, `users.delete`, and `users.permissions.manage`; the parent remains in the effective set. The reverse implication does not exist.
- Domain-owned permission expansion uses an explicit lowercase-invariant allowlist. Every declared `<resource>.manage` implies its reviewed CRUD siblings; `users.manage` also implies `users.permissions.manage`, `videos.manage` also implies import/translate/publish, `forms.manage` also implies `forms.responses.view`, and `insights.manage` also implies `insights.scan`. `images.manage` has no update implication and `diagnostics.view` is standalone.
- Implication is one-way: leaves do not grant a parent, siblings, or another resource. For example, `videos.create` does not grant import, `forms.responses.view` does not grant form management, `insights.scan` does not grant CRUD, and `users.permissions.manage` does not grant user lifecycle operations.
- `backoffice.manageall` remains the global authorization bypass and implies only `backoffice.access`; the expanded effective-permission set does not materialize every catalog leaf.

`[AllowUser(...)]` semantics are OR-based and support both syntaxes:

- `[AllowUser("admin", "contributor")]`: authorize when the principal has one of the required group codes OR one of the same permission keys.
- `[AllowUser("backoffice.access")]`: authorize when the principal has the required permission directly, inherited from a group, implied by another permission, or supplied as an equivalent claim.

Token normalization uses `ToLowerInvariant()` semantics to keep group and permission matching case-insensitive and stable across persisted data and claims. Existing direct and group parent grants gain their implications immediately without a migration.

The BackOffice SPA `/rbac` route tree uses the effective permissions returned by `/api/auth/validate`. User-list and detail reads require `users.view`; lifecycle mutations require `users.create`, `users.update`, or `users.delete`; group CRUD, memberships, and direct-permission assignments require `users.permissions.manage`. The frontend never expands implications. `users.manage` arrives server-expanded with the reviewed user-administration leaves, and `backoffice.manageall` is the global override. `backoffice.access` grants login and shell entry only and does not authorize RBAC or user administration. The API remains authoritative through `AllowUser`.

## BackOffice Impersonation

Impersonation is disabled by default through `FeatureManagement:EnableImpersonation`. When enabled, `POST /api/impersonation/grants` issues a single-use opaque grant to an operator with `backoffice.impersonate`; `backoffice.manageall` implies that permission. `POST /api/impersonation/sessions` redeems the grant for a separate HttpOnly, Secure, `SameSite=None` session cookie, and `DELETE /api/impersonation/sessions/current` ends it. Cookie-backed unsafe requests require `X-CSRF-TOKEN`, including grant issue, redemption, and session termination. Sessions and grants expire within 10 minutes, and grants are stored and redeemed atomically by hash.

The actor remains the authenticated primary identity for CSRF and audit purposes while authorization and content ownership resolve the explicit target identity. Targets must be active BackOffice users and cannot be security accounts, administrators, or users with `backoffice.manageall`. User/password, API-key, RBAC, and configuration operations are hard-blocked during impersonation; ordinary content operations use the target's effective permissions. API-key principals cannot issue, redeem, or inherit browser impersonation sessions. Audit events keep actor, target, and session identifiers separate and never store raw grants or session tokens.

### First-admin bootstrap

`POST /api/user/bootstrap-admin/{username}` is an operational bootstrap endpoint. It is anonymous only because no authenticated administrator exists yet; it still requires `X-Bootstrap-Secret` matching protected `BootstrapSettings:Secret` configuration. Empty configuration disables the endpoint. The endpoint accepts only an existing active username, creates or repairs the `admin` group with `backoffice.access`, and assigns that group membership. It refuses to run after any active user already has BackOffice access, including legacy `CanAccessBackoffice`, direct permission, or active-group permission. It never creates a user or predictable password.

Operators must remove or rotate the bootstrap secret after first use. The supported bootstrap path is `/api/user/bootstrap-admin/{username}` with the deployment secret header; no legacy `init` route should be documented or used for new administrator provisioning.

## API-Key Authentication

API keys support VideoImporter, InsightScanner, and selected machine workflows. Store only secure hashes, show raw keys once, enforce expiry and revocation, rate-limit using shared state when scaled horizontally, and trust forwarded client IP headers only behind configured proxies.

Managed keys have a persisted `channelId`. Scoped BackOffice resources require `X-Channel-Id`; missing context is `400 channel_context_required`, while unknown, inaccessible, or key/channel mismatches are `404 channel_context_unavailable`. Creation binds a key to the selected channel, and only an administrator can reassign it to another existing channel. API-key principals are excluded from browser impersonation.

The effective impersonated target identity is used for ordinary content/channel authorization, while the primary actor remains the audit and CSRF identity. Administrators can select any channel; normal users can select only owned channels. Video collaborators can read but not mutate shared videos. Compilation management and other BackOffice resources remain scoped, but public compilation URLs and short-link redirects are anonymous global lookups. Scoped responses and caches must include channel and effective-identity authorization inputs; public URL caches must remain global.

## Public And Cart Security (Target, On Hold)

The shop is pre-production and all shop implementation is on hold. The following rules remain the accepted target if the portfolio hold is lifted; they do not authorize active shop work.

Public endpoints are explicitly anonymous. An anonymous-cart cookie is opaque, HttpOnly, Secure, narrowly scoped, integrity protected, and rotated when ownership changes. API routes derive cart identity from the server-controlled cookie, never a route/query customer ID.

Cart possession authorizes only the corresponding permanent-free acquisitions. Original Blob keys are never returned. Short-lived SAS URLs use minimum read permission and expiry.

## Private Content

Target visibility policies are Public, RegisteredCustomer, and EntitlementRequired. Authorization applies consistently to content metadata, image lists, original files, and associated endpoints. Generic `IsAuthenticated` checks are insufficient because administrator, API-key, fake, and customer identities have different rights.

Blob authorization follows container purpose. Match, sponsor, and page previews retain anonymous read for current public URLs. Originals/uploads and recovery content remain private. BackOffice receives contributor rights only on required write containers; ServerAPI receives reader rights only on the preview container it lists; the recovery container is operator restricted and isolated in a non-production account where practical. Managed identity is preferred, and connection strings remain secret-configured fallback material.

## Short-Link Safety

- Normalize codes with lowercase invariant rules.
- Allow only absolute HTTP/HTTPS destinations.
- Reject user-info credentials, unsafe schemes, malformed hosts, and prohibited internal/private network destinations.
- Preserve query parameters through structured URI APIs.
- Return safe not-found behavior without revealing management metadata.
- Rate-limit abuse and monitor redirect anomalies.
- Keep management in BackOffice and resolution in ShortLinks.
- Sponsor public projections use only the configured `YouTubeChannelId`. They
	expose the resolved destination URL for navigation, while analytics remain
	anonymous aggregate-only and carry no sponsor-specific identifiers.

## CORS And Host Security

Development allow-all CORS is environment-gated; development flags cannot enable it in Production. Production policies are explicit:

- ServerAPI: `https://morwalpiz.com`, no credentials for public requests; cookie endpoints require a reviewed credential policy.
- BackOffice: `https://morwalpiz-admin-spa.azurewebsites.net`, credentials enabled.
- Shooting Range: exactly `https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net`, credentials enabled; no arbitrary origins or Development exceptions currently enabled.
- ShortLinks: no CORS required for navigation redirects.

Reject lookalike suffixes. Configure AllowedHosts and forwarded-header trusted networks/proxies independently.

## Shooting Range POC Security

Shooting Range is expected to become publicly reachable while remaining a minimal POC. The accepted release target is deny-by-default for all domain endpoints, including availability, with anonymous technical exceptions for login, CSRF and health. This target is not yet the current anonymous-domain posture.

The first administrator is inserted manually into MongoDB. There is no bootstrap-admin endpoint and no secret or reusable password hash in source or documentation. Public registration is not part of the approved release posture; administrator-created ordinary users are the working assumption pending final confirmation.

Required release controls are:

- Explicit credentialed CORS for the deployed client origin.
- Secure HttpOnly authentication cookie and CSRF validation for unsafe requests.
- Session revalidation against current account status, administrator role, and forced-password state.
- Explicit response DTOs that exclude password hashes, bay whitelist identifiers, and persistence-only metadata.
- Rate limiting for anonymous login.
- MongoDB uniqueness for normalized usernames and active booking conflicts.
- Authoritative booking validation for dates, opening days, closures, bay status, periods, reservations, and overlap.
- Production rejection of mock repositories and readiness coverage for MongoDB.
- Persistent Data Protection keys when more than one instance or application restart must preserve sessions.

The scoped schedule implementation now restricts CORS to the exact deployed client origin, projects admin users without password hashes while preserving legitimate fields, revalidates touched admin/admission state, and guards all repository writes with transactional per-bay interval validation. Startup fails closed when the transaction probe fails. Pending/Approved consume capacity; historical UTC intervals survive schedule changes. The period-key unique index is secondary protection only.

The session/security foundation now adds pre-login CSRF, post-login/password-change CSRF refresh, per-IP login throttling (10 attempts per five minutes per process), server-side revocable eight-hour non-sliding sessions, current approved-state/role validation on every cookie request including mine/messages, and logout revocation. Password reset/change rotates an additive security version; self-change also revokes and renews the current session. Old cookies without a session claim require reauthentication. Forced-change sessions allow only session inspection, password change, logout and the technical CSRF endpoint. The client restores sessions, gates direct routes, handles expiry/retry and provides logout without persisting credentials.

Mock mode is rejected outside Development/Test. Mongo readiness checks writable replica-set/mongos topology and logical sessions; the existing startup transaction rollback probe remains. A read-only username preflight rejects normalized duplicates or missing/mismatched `normalizedUsername` fields before creating the unique index; legacy data needs an operator-approved additive backfill, never an automatic merge/delete. Session expiry has a TTL index, but authorization checks expiry independently. See the Range architecture document for rollout requirements.

Release remains blocked: anonymous registration/config/sessions/availability and entity/whitelist projections are deliberately preserved until slice 4's product decision (administrator-created users versus another protected registration workflow). Range fallback authorization and removal of `AllowAnonymous` are not implemented in this slice. Persistent Data Protection, distributed/gateway throttling when scaled, target-Mongo transactions/rollback/races/data/index/backfill evidence and real-browser allowed/denied-origin cookie/CSRF verification remain mandatory operational gates. Local tests, topology classification and evaluated credential metadata do not satisfy those gates. No deployment is authorized.

## Data Protection And Privacy

- Minimize stored IP, user-agent, and email data.
- Define retention before enabling detailed short-link or customer analytics.
- Protect MongoDB and Blob backups.
- Avoid logging request bodies or sensitive query strings on authentication and integration routes.
- Use structured audit events for administrative mutations, key lifecycle, acquisitions, and protected downloads.

## Security Verification

Required tests include authorization matrices, CSRF, CORS, cookie tampering, cookie-backed validation effective-permission responses, SPA RBAC route allow/deny cases, expired/revoked credentials, unsafe redirects, rate limits, and secret-scanner CI gates. Shop-specific cross-cart, hidden-storage-key, private-Blob, and SAS-expiry scenarios remain required only when the shop hold is lifted.

Local checks are not release approval. The [release evidence checklist](operations/phase5-activation-and-recovery.md#release-evidence-checklist-2026-10-01) owns mandatory redacted records for browser/SSR, current-state sessions, Data Protection rotation, Mongo audit/recovery/races, secrets/artifact invalidation and credentials. Every unavailable proof is BLOCKED with approval NOT GRANTED; no rotation or Azure access is authorized by these docs.