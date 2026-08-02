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

## Administrative Authentication

BackOffice uses JWT bearer and can read a secure cookie. Target browser posture:

- HttpOnly, Secure cookie.
- Appropriate SameSite mode for the admin/API origin relationship.
- Explicit credentialed CORS for the admin SPA only.
- CSRF protection for state-changing cookie-authenticated requests.
- Short token lifetime, server-side revocation strategy where required, and audited login throttling.

Remove browser JWTs from local storage after the cookie flow is complete.

## API-Key Authentication

API keys support VideoImporter, InsightScanner, and selected machine workflows. Store only secure hashes, show raw keys once, enforce expiry and revocation, rate-limit using shared state when scaled horizontally, and trust forwarded client IP headers only behind configured proxies.

## Public And Cart Security

Public endpoints are explicitly anonymous. An anonymous-cart cookie is opaque, HttpOnly, Secure, narrowly scoped, integrity protected, and rotated when ownership changes. API routes derive cart identity from the server-controlled cookie, never a route/query customer ID.

Cart possession authorizes only the corresponding permanent-free acquisitions. Original Blob keys are never returned. Short-lived SAS URLs use minimum read permission and expiry.

## Private Content

Target visibility policies are Public, RegisteredCustomer, and EntitlementRequired. Authorization applies consistently to content metadata, image lists, original files, and associated endpoints. Generic `IsAuthenticated` checks are insufficient because administrator, API-key, fake, and customer identities have different rights.

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

Required tests include authorization matrices, CSRF, CORS, cookie tampering, cross-cart denial, expired/revoked credentials, unsafe redirects, hidden storage keys, private Blob access, SAS expiry, rate limits, and secret-scanner CI gates.