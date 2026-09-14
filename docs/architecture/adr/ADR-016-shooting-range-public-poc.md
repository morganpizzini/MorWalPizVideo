# ADR-016: Authorization-First Public Posture For The Shooting Range POC

- **Status:** Accepted
- **Date:** 2026-09-11

## Context

`MorWalPizVideo.ShootingRange` and `frontend/shooting-range.client` form an independent booking proof of concept. They are not publicly exposed yet but are expected to be soon, and their deployment workflows remain bound to the GitHub `production` environment.

Current source allows anonymous registration and availability, accepts credentialed CORS from arbitrary origins, lacks a fallback authorization policy, and exposes some persistence entities. The POC must remain simple without making imminent public exposure unsafe or coupling it to the main publishing architecture.

## Decision

Apply deny-by-default authorization to every Shooting Range domain endpoint. The only anonymous technical exceptions are login, CSRF token acquisition, and liveness/readiness probes. Availability requires authentication, and administrator operations additionally require the `admin` role.

The first administrator is inserted manually into MongoDB. No bootstrap-admin endpoint is introduced, and credentials or reusable password hashes are never stored in source or documentation.

Public self-registration is not part of the approved release posture. Administrator-created ordinary users are the working assumption, but ordinary-user onboarding remains a product decision that may be revised without changing the service boundary.

Keep models, contracts, services, repositories, and frontend behavior locally owned by the Shooting Range projects. The initial UI remains limited to authentication and session restoration, forced password change, logout, availability and booking, the current user's bookings, and the minimum administrator actions needed for user creation and booking decisions if admin-created onboarding is confirmed.

Production exposure is gated on explicit credentialed CORS, safe response DTOs, account-state revalidation, login throttling, MongoDB uniqueness and readiness, authoritative booking validation, production rejection of mock mode, persistent Data Protection where required, and executable security/integrity tests.

## Alternatives

- Keep availability and registration anonymous: rejected because all domain functionality must be authorization-protected before public exposure.
- Add a public bootstrap-admin endpoint: rejected because the first administrator will be inserted manually.
- Complete every existing API workflow in the UI: rejected because the POC is intentionally minimal and observed use should drive expansion.
- Move Shooting Range into shared Domain, Models, Contracts, or frontend packages: rejected because no shared runtime consumer exists.

## Consequences

Login, CSRF token acquisition, and health probes remain anonymous by necessity; this is not an exception for domain data. Direct links and browser refreshes require session restoration and protected client routing.

Messaging, bay/configuration/closure management, cancellation, rescheduling, notifications, waitlists, custom-duration sessions, reporting, and invite-based onboarding remain deferred. Existing implementation does not make those features part of the committed UI.

Manual administrator insertion remains an operational responsibility. The procedure must use the current normalized username and password-hashing representation without recording secrets.

## Migration And Rollback

Audit normalized usernames before adding a unique index. Keep existing collections and documents readable through additive changes. Deploy API authorization and compatibility behavior before the client.

Application rollback must not restore unrestricted credentialed CORS, anonymous domain access, password-hash responses, or production mock repositories.

## Validation

- Anonymous domain requests return `401`; normal users receive `403` from administrator operations.
- Login, CSRF token acquisition, and health probes remain reachable anonymously.
- Missing or forged CSRF tokens fail for unsafe cookie-authenticated requests.
- Only the configured production client origin receives credentialed CORS responses.
- Disabled, demoted, and forced-password-change accounts are enforced from current server state.
- Responses never contain password hashes, whitelist identifiers, or persistence-only metadata.
- Duplicate normalized usernames and conflicting active bookings fail at the database boundary.
- Invalid dates, periods, closures, unavailable bays, and reservation restrictions fail during booking creation.
- A correctly inserted approved administrator can log in and perform the minimal administrator workflow.