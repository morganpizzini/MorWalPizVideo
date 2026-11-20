# ADR-016: Authorization-First Public Posture For The Shooting Range POC

- **Status:** Accepted
- **Date:** 2026-09-11

## Context

`MorWalPizVideo.ShootingRange` and `frontend/shooting-range.client` form an independent booking proof of concept. They are not publicly exposed yet but are expected to be soon, and their deployment workflows remain bound to the GitHub `production` environment.

Current source still allows anonymous domain reads/registration, lacks a fallback authorization policy, and exposes some persistence entities. The schedule guard and session/security foundation restrict credentialed CORS to the exact deployed range client, exclude password hashes from admin-user responses, and revalidate revocable sessions against current account state. They do not complete the broader public-release posture. The POC must remain simple without making imminent public exposure unsafe or coupling it to the main publishing architecture.

## Decision

Apply deny-by-default authorization to every Shooting Range domain endpoint. The only anonymous technical exceptions are login, CSRF token acquisition, and liveness/readiness probes. Availability requires authentication, and administrator operations additionally require the `admin` role.

The first administrator is inserted manually into the Shooting Range Azure SQL database using the current password-hashing implementation. No bootstrap-admin endpoint is introduced, and credentials or reusable password hashes are never stored in source or documentation.

Public self-registration is not part of the approved release posture. Administrator-created ordinary users are the working assumption, but ordinary-user onboarding remains a product decision that may be revised without changing the service boundary.

Keep models, contracts, services, repositories, authentication, and API transport locally owned by the Shooting Range projects. The Range client may consume `@morwalpiz/layout` for shared presentation components and align its visual system with Shooter Workshop. This supersedes the earlier prohibition on shared frontend packages: that restriction reflected the former layout implementation, while the consolidated layout and theme now provide the intended common presentation layer. Do not import Workshop application source, account/authentication providers, API services, or domain models. The UI remains limited to authentication and session restoration, forced password change, logout, availability and booking, the current user's bookings, and the existing minimal administrator workflows.

Production exposure is gated on explicit credentialed CORS, safe response DTOs, account-state revalidation, login throttling, Azure SQL uniqueness and readiness, authoritative booking validation, production rejection of mock mode, persistent Data Protection where required, and executable security/integrity tests.

## Alternatives

- Keep availability and registration anonymous: rejected because all domain functionality must be authorization-protected before public exposure.
- Add a public bootstrap-admin endpoint: rejected because the first administrator will be inserted manually.
- Complete every existing API workflow in the UI: rejected because the POC is intentionally minimal and observed use should drive expansion.
- Move Shooting Range backend/domain behavior or contracts into shared projects: rejected because no shared runtime consumer exists.
- Reuse the consolidated shared presentation package: accepted for UI components and consistent styling; it does not change Range ownership of authentication, transport, or domain behavior.

## Consequences

The implemented foundation protects login with pre-login CSRF and per-IP throttling (10 attempts per five minutes per process), refreshes CSRF after identity changes, and uses revocable eight-hour non-sliding sessions. Logout revokes the current session; password reset/change invalidates older sessions, while successful self-change renews the current session after the database transaction commits. Disabled accounts and demoted roles are enforced on each cookie request. Forced-change sessions permit only session inspection, password change, logout and technical CSRF acquisition. Production/Staging reject mock storage; Azure SQL readiness requires an applied schema and guard row, while an explicit migration command applies schema changes.

The remaining anonymous-domain/onboarding cutover is NOT READY pending the ordinary-user onboarding decision. Registration, anonymous configuration/sessions/availability, and existing entity/whitelist projections remain unchanged for compatibility. Do not interpret the accepted target as implemented fallback authorization or approval for public exposure.

Login, CSRF token acquisition, and health probes remain anonymous by necessity; this is not an exception for domain data. Direct links and browser refreshes require session restoration and protected client routing.

Schedule, bay and closure administration and booking decisions are now accepted scope. Three modes preserve `Periods=0` and `Hourly=1`, adding `HourlyContinuous=2` with explicit continuous endpoints and fixed 60-minute slots. User cancellation is supported for a user's own future Pending or Approved booking, retaining its Cancelled record; administrators may restore future cancelled bookings only through guarded approval and normal capacity admission. Booking lists are filtered from yesterday onward in field-local dates. The dedicated admin booking page shows an accessible badge for future Pending bookings, refreshed while visible; this is not push. Bays also store nonnegative integer targets and optional paired positive height/width numbers, unit-free and precise to two decimal places. Messaging expansion, rescheduling, push notifications, waitlists, custom-duration sessions, reporting, and invite-based onboarding remain deferred.

### Booking workflow amendment — 2026-11-20

The client loads availability automatically after the date and offered session
are selected, and refreshes it when either changes. User cancellation follows
the future-only, owner-only guarded policy above. An additive SQL migration
adds target count and nullable dimensions; existing rows receive zero targets
and unspecified dimensions. The API requires new bay fields on full-replacement
requests so stale clients fail safely rather than erase new inventory data.

All local repository writes participate in a field-scoped SQL Server transaction guard using `UPDLOCK, ROWLOCK`; Pending/Approved capacity is checked using stored UTC overlap, not period-key uniqueness. All checks that authorize a mutation are performed inside the guarded transaction. The schema uses explicit EF Core migrations; startup never creates or upgrades tables. Disposable SQL Server independent-writer races, Azure SQL backup/restore, existing Range data migration and browser CORS/CSRF checks remain release gates; local mock tests cannot satisfy them.

Manual administrator insertion remains an operational responsibility. The procedure must use the current normalized username and password-hashing representation without recording secrets.

### Frontend presentation amendment — 2026-11-18

The original frontend isolation constraint is superseded. The Range client now uses the consolidated `@morwalpiz/layout` presentation layer where its components fit, with Range-local styling and workflows. This amendment does not make Workshop authentication, services, models, or application code shared, and it does not complete or alter the separate public-release authorization/onboarding decision.

## Migration And Rollback

Old cookies without the new session claim require reauthentication. Sessions are persisted in the Azure SQL `shooting_range_local_sessions` table; account `security_version` and `normalized_username` columns are required, and the unique normalized-username index uses binary collation. Deploy the compatible security API before the client; full domain authorization remains a separate cutover.

Pause writes for guarded API cutover, ensure all instances use the guard, deploy the client next and enable new modes last. Application rollback must use a compatible guarded version; otherwise suspend mutations. Never restore unrestricted credentialed CORS, password-hash responses, or production mock repositories. See the Shooting Range architecture procedure for data preflight and unresolved public-release controls.

## Validation

These are full release acceptance criteria, not a claim that every item passes in the current foundation. Anonymous-domain rejection and broad safe DTO projections remain blocked by the cutover decision; target Azure SQL, backup/restore, browser and Data Protection evidence remains required separately from local executable tests.

- Anonymous domain requests return `401`; normal users receive `403` from administrator operations.
- Login, CSRF token acquisition, and health probes remain reachable anonymously.
- Missing or forged CSRF tokens fail for unsafe cookie-authenticated requests.
- Only the configured production client origin receives credentialed CORS responses.
- Disabled, demoted, and forced-password-change accounts are enforced from current server state.
- Responses never contain password hashes, whitelist identifiers, or persistence-only metadata.
- Duplicate normalized usernames and conflicting active bookings fail at the database boundary.
- Invalid dates, periods, closures, unavailable bays, and reservation restrictions fail during booking creation.
- A correctly inserted approved administrator can log in and perform the minimal administrator workflow.