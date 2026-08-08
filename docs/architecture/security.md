- The SPA stores display-only user information in local storage; the browser JWT remains in the HttpOnly cookie. The BackOffice client does not read `localStorage.authToken` or emit `Authorization: Bearer`; API-key and explicit non-browser bearer callers remain supported.
# Security Architecture

## Security Boundaries

| Boundary | Trust model |
|---|---|
| BackOffice SPA to BackOffice | Authenticated administrator |
| WPF tools to BackOffice | Authenticated API-key client |
| Public apps to ServerAPI | Anonymous by default; explicit cart/customer policies |
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

Current desktop seed source and migration artifacts use non-secret placeholders. This narrows current-source exposure but does not revoke historical credentials, remove them from repository history or old artifacts, or prove that old credentials fail. Those actions remain administrator-owned and require independently verifiable redacted evidence.

## Administrative Authentication

BackOffice uses JWT bearer and can read a secure cookie. Implemented browser posture:

- HttpOnly, Secure, `SameSite=None` `auth_token` cookie for the separate HTTPS SPA/API origins.
- Explicit credentialed CORS for `https://morwalpiz-admin-spa.azurewebsites.net` only.
- `X-CSRF-TOKEN` protection for unsafe requests carrying the auth cookie, including logout.
- Bearer-only and API-key-only requests remain outside the browser-cookie CSRF flow.
- Short token lifetime, server-side revocation strategy where required, and audited login throttling.

The SPA stores display-only user information in local storage; the browser JWT remains in the HttpOnly cookie.

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

### First-admin bootstrap

`POST /api/user/bootstrap-admin/{username}` is an operational bootstrap endpoint. It is anonymous only because no authenticated administrator exists yet; it still requires `X-Bootstrap-Secret` matching protected `BootstrapSettings:Secret` configuration. Empty configuration disables the endpoint. The endpoint accepts only an existing active username, creates or repairs the `admin` group with `backoffice.access`, and assigns that group membership. It refuses to run after any active user already has BackOffice access, including legacy `CanAccessBackoffice`, direct permission, or active-group permission. It never creates a user or predictable password.

Operators must remove or rotate the bootstrap secret after first use. The supported bootstrap path is `/api/user/bootstrap-admin/{username}` with the deployment secret header; no legacy `init` route should be documented or used for new administrator provisioning.

## API-Key Authentication

API keys support VideoImporter, InsightScanner, and selected machine workflows. Store only secure hashes, show raw keys once, enforce expiry and revocation, rate-limit using shared state when scaled horizontally, and trust forwarded client IP headers only behind configured proxies.

## Public And Cart Security

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

## CORS And Host Security

Development allow-all CORS is gated by Development plus `EnableDev`. Production policies are explicit:

- ServerAPI: `https://morwalpiz.com`, no credentials for public requests; cookie endpoints require a reviewed credential policy.
- BackOffice: `https://morwalpiz-admin-spa.azurewebsites.net`, credentials enabled.
- ShortLinks: no CORS required for navigation redirects.

Reject lookalike suffixes. Configure AllowedHosts and forwarded-header trusted networks/proxies independently.

## Data Protection And Privacy

- Minimize stored IP, user-agent, and email data.
- Define retention before enabling detailed short-link or customer analytics.
- Protect MongoDB and Blob backups.
- Avoid logging request bodies or sensitive query strings on authentication and integration routes.
- Use structured audit events for administrative mutations, key lifecycle, acquisitions, and protected downloads.

## Security Verification

Required tests include authorization matrices, CSRF, CORS, cookie tampering, cookie-backed validation effective-permission responses, SPA RBAC route allow/deny cases, cross-cart denial, expired/revoked credentials, unsafe redirects, hidden storage keys, private Blob access, SAS expiry, rate limits, and secret-scanner CI gates.