# ADR-016: Authorization-First Public Posture For The Shooting Range POC

- **Status:** Accepted
- **Date:** 2026-09-11

## Context

`MorWalPizVideo.ShootingRange` and `frontend/shooting-range.client` form an independent booking proof of concept. They are not publicly exposed yet but are expected to be soon, and their deployment workflows remain bound to the GitHub `production` environment.

Current source still allows anonymous domain reads/registration, lacks a fallback authorization policy, and exposes some persistence entities. The schedule guard and session/security foundation restrict credentialed CORS to the exact deployed range client, exclude password hashes from admin-user responses, and revalidate revocable sessions against current account state. They do not complete the broader public-release posture. The POC must remain simple without making imminent public exposure unsafe or coupling it to the main publishing architecture.

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

The implemented foundation protects login with pre-login CSRF and per-IP throttling (10 attempts per five minutes per process), refreshes CSRF after identity changes, and uses revocable eight-hour non-sliding sessions. Logout revokes the current session; password reset/change invalidates older sessions, while successful self-change renews the current session. Disabled accounts and demoted roles are enforced on each cookie request. Forced-change sessions permit only session inspection, password change, logout and technical CSRF acquisition. Production/Staging reject mock storage; Mongo readiness supplements the startup transaction probe.

The remaining anonymous-domain/onboarding cutover is NOT READY pending the ordinary-user onboarding decision. Registration, anonymous configuration/sessions/availability, and existing entity/whitelist projections remain unchanged for compatibility. Do not interpret the accepted target as implemented fallback authorization or approval for public exposure.

Login, CSRF token acquisition, and health probes remain anonymous by necessity; this is not an exception for domain data. Direct links and browser refreshes require session restoration and protected client routing.

Schedule, bay and closure administration and booking decisions are now accepted scope. Three modes preserve `Periods=0` and `Hourly=1`, adding `HourlyContinuous=2` with explicit continuous endpoints and fixed 60-minute slots. Messaging expansion, cancellation, rescheduling, notifications, waitlists, custom-duration sessions, reporting, and invite-based onboarding remain deferred.

All local repository writes participate in a field-scoped transaction guard; Pending/Approved capacity is checked using stored UTC overlap, not period-key uniqueness. Unsupported transactional storage fails closed. Real target-Mongo capability, independent-writer races, historical data/index audit and browser CORS/CSRF checks remain mandatory release gates; local mock tests cannot satisfy them.

Manual administrator insertion remains an operational responsibility. The procedure must use the current normalized username and password-hashing representation without recording secrets.

## Migration And Rollback

Old cookies without the new session claim require reauthentication. Sessions use the additive `shootingRangeLocalSessions` collection; account `securityVersion` and `normalizedUsername` fields are additive. A read-only startup preflight rejects normalized duplicates and missing/mismatched normalized fields before creating `normalized_username_unique`. With writes paused, operators must review duplicates, approve an additive backfill using the current trim/lowercase-invariant normalization, and verify the index; never automatically merge or delete accounts. Session TTL cleanup is not an authorization boundary: expired sessions are rejected independently. Keep existing collections and documents readable. Deploy the compatible security API before the client; full domain authorization remains a separate cutover.

Pause writes for guarded API cutover, ensure all instances use the guard, deploy the client next and enable new modes last. Application rollback must use a compatible guarded version; otherwise suspend mutations. Never restore unrestricted credentialed CORS, password-hash responses, or production mock repositories. See the Shooting Range architecture procedure for data preflight and unresolved public-release controls.

## Validation

These are full release acceptance criteria, not a claim that every item passes in the current foundation. Anonymous-domain rejection and broad safe DTO projections remain blocked by the cutover decision; live Mongo/browser/Data Protection evidence remains required separately from local executable tests.

- Anonymous domain requests return `401`; normal users receive `403` from administrator operations.
- Login, CSRF token acquisition, and health probes remain reachable anonymously.
- Missing or forged CSRF tokens fail for unsafe cookie-authenticated requests.
- Only the configured production client origin receives credentialed CORS responses.
- Disabled, demoted, and forced-password-change accounts are enforced from current server state.
- Responses never contain password hashes, whitelist identifiers, or persistence-only metadata.
- Duplicate normalized usernames and conflicting active bookings fail at the database boundary.
- Invalid dates, periods, closures, unavailable bays, and reservation restrictions fail during booking creation.
- A correctly inserted approved administrator can log in and perform the minimal administrator workflow.