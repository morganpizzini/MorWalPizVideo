# ADR-002: Explicit Host Authentication

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

A shared controller base currently applies `[Authorize]` to BackOffice, ServerAPI, and ShortLinks descendants. Administrator, API-key, public, development, cart, and future customer identities have different rights.

## Decision

Shared controller abstractions are host-neutral. Each host establishes explicit default and named policies:

- BackOffice management: JWT/secure cookie.
- Machine endpoints: API key.
- ServerAPI public endpoints: explicit anonymous access.
- Anonymous cart: opaque server-controlled cookie.
- Future customer endpoints: dedicated customer policy.
- ShortLinks redirect: anonymous.
- Internal cache operations: authenticated service identity.

Fake authentication requires Development plus `EnableDev`.

## Alternatives

- Keep inherited authorization and add exceptions: rejected because accidental exposure/failure remains likely.
- Use one JWT identity for every caller: rejected because privileges and lifecycle differ.

## Consequences

More explicit endpoint metadata and tests are required. Security intent becomes locally visible and independently evolvable.

## Migration And Rollback

Add policies and tests before removing inherited authorization. Compatibility can temporarily preserve current schemes per endpoint.

## Validation

Automated authorization matrix covers anonymous, admin, API key, fake, cart, and future customer principals.

## Migration Status

Implemented (2026-08-02):

- Shared `ApplicationControllerBase` (MvcHelpers) no longer applies `[Authorize]`; each host owns its default.
- BackOffice: `AddAuthorization` fallback policy requires an authenticated JWT/cookie principal by default; `AuthController` is explicitly `[AllowAnonymous]`.
- ServerAPI: public content controllers (`BioLinksController`, `CalendarEventsController`, `CompetitionsController`, `CompilationsController`, `ConfigurationController`, `CustomFormsController`, `MatchesController`, `PagesController`, `ProductsController`, `SponsorsController`) are explicitly `[AllowAnonymous]`.
- Internal cache operations: new `InternalService` shared-secret scheme (`MorWalPizVideo.MvcHelpers.Authentication`) protects `CacheController`; `CrossApiService` attaches the credential on outbound calls.
- ShortLinks: `FakeAuthenticationHandler` registration is now gated by `Development` + `EnableDev` (previously unconditional); redirect endpoint remains anonymous.
- Authorization regression tests added in `MorWalPizVideo.BackOffice.Tests/Features/AuthorizationPolicyTests.cs`.

Implemented (2026-08-06):

- BackOffice RBAC management API under `api/rbac` supports MongoDB `UserGroup` CRUD, multi-group user membership, direct user permissions, and normalized effective-permission resolution.
- `AllowUser` dynamic policy supports both group-style and permission-style declarations with OR semantics (`group OR permission`), including `[AllowUser("admin","contributor")]` and `[AllowUser("backoffice.access")]`.
- Legacy `CanAccessBackoffice` remains backward-compatible and is mapped to canonical permission key `backoffice.access`.
- BackOffice SPA includes an RBAC management section for users/groups/permissions/memberships.
- `POST /api/auth/validate` additively returns normalized effective permissions resolved from the cookie session. ADR-014 supersedes the original `/rbac` guard token: user reads require `users.view`, lifecycle mutations require their granular `users.*` operation, and groups, memberships, and direct-permission assignments require `users.permissions.manage`. `users.manage` and `backoffice.manageall` provide their documented overrides, while `backoffice.access` is limited to login and shell entry. LocalStorage is neither an authorization source nor required for authenticated shell controls.
- Focused BackOffice tests cover authorization matrix and RBAC CRUD assignment scenarios.

Deferred (not yet implemented):

- **Anonymous cart cookie**: the shop cart is still keyed by a route `customerId`, with no opaque server-controlled cookie. Requires new cookie-issuance/consumption design plus frontend changes; out of scope for the authorization-wiring migration to avoid breaking the deployed shop contract.
- **Future customer policy**: no dedicated customer authentication policy exists. `ShopAuthController`'s session token remains an unvalidated client-side stub.
- **Named `Machine` policy**: BackOffice API-key endpoints still use the existing `ApiKeyAuthAttribute` (`AuthenticationSchemes = "ApiKey"`) rather than a wrapping `AddAuthorization` named policy; behavior is already explicit and equivalent.
- The automated authorization matrix does not yet cover cart or customer principals (none exist yet), and ServerAPI/ShortLinks have no `WebApplicationFactory` HTTP-level harness for a full cross-host matrix; current coverage is attribute/reflection-based.
