# Shooting Range Booking

## Status and scope

Shooting Range is a pre-release proof of concept that is expected to become publicly reachable. It remains independently deployable through the GitHub `production` environment, but public exposure does not make it a feature-complete product.

The approved direction is an authorization-first, minimal user experience. Preserve the current project-local contracts, service, and repository boundaries so later features can be added without coupling the POC to BackOffice, Domain, Models, or the shared frontend packages. Do not implement speculative workflows before observed use establishes a requirement.

The shop is unrelated to this runtime and remains pre-production and on hold.

## Runtime

`MorWalPizVideo.ShootingRange` is a dedicated ASP.NET Core API. Its models, request/response contracts, application service, repository interfaces, Mongo and mock repositories, and password hashing implementation are owned by the project itself. It references only ServiceDefaults among the solution's application projects and is registered with `MorWalPizVideo.AppHost` as `shooting-range`. `frontend/shooting-range.client` is a separate React/Vite application with the same SSR shape as `ask.client`, but no Ask-domain dependency.

The API is intentionally detached from `MorWalPizVideo.BackOffice`, `MorWalPizVideo.Domain`, `MorWalPizVideo.Models`, and `MorWalPiz.Contracts`. BackOffice is a pattern reference only; no BackOffice entities or shared ShootingRange types are used at runtime.

## Defaults and time

The initial configuration is one global field named Pepperbox, timezone `Europe/Rome`, opening Saturday/Sunday, morning `09:00-13:00`, afternoon `14:00-18:00`, and one-hour hourly slots. `ReservedReleaseDaysBefore` defaults to `2`. Local field dates and period keys are authoritative; the API returns `startUtc` and `endUtc` as UTC instants. The browser formats those instants for display.

A reserved bay is restricted to its whitelist while the local field date is more than two calendar days away. At T-2 it is available to all users when no active booking exists. Administrative closures, bay exceptions and conflicts take precedence. This is calculated per request; no job is required for correctness.

## Data and concurrency

Collections are `shootingRangeConfigs`, `shootingRangeBays`, `shootingRangeExceptions`, `shootingRangeUsers`, `shootingRangeBookings` and `shootingRangeThreads`. Active booking uniqueness is enforced by a Mongo unique index on bay/date/period for Pending and Approved records, with duplicate keys mapped to HTTP 409. The mock repository uses a semaphore for the same critical section.

All account passwords are PBKDF2 hashes with per-account salts. Passwords must never be logged or returned. Only Approved users can authenticate. Admin reset marks `ForcePasswordChange`; the new password must be communicated out of band.

The first administrator is inserted manually into `shootingRangeUsers`; there is no public or administrative bootstrap endpoint. The inserted document must use the normalized username and the password representation produced by the current password-hashing implementation. No credential or reusable hash belongs in source or documentation.

Ordinary-user onboarding is not yet a final product decision. Admin-created users are the working assumption because it keeps account creation behind authorization; public self-registration is not part of the approved public-release posture.

## Security policy

Authentication uses an HttpOnly, Secure, SameSite=None cookie. Authorization is deny-by-default for every Shooting Range domain endpoint, including availability. Admin routes additionally require the `admin` role claim.

Only these technical entry points remain anonymous:

- Login, because authentication cannot otherwise begin.
- CSRF token acquisition, because login and later unsafe cookie requests need a token.
- Liveness and readiness probes.

Unsafe cookie-authenticated requests require the `X-CSRF-TOKEN` token returned by `/api/shooting-range/csrf`. Refresh the token after successful login. Production credentialed CORS allows only the configured Shooting Range client origin.

Current source does not yet satisfy this target: registration and availability are anonymous, no fallback authorization policy exists, and credentialed CORS accepts arbitrary origins. These are public-release blockers, not accepted POC shortcuts.

Account status, current administrator role, and `ForcePasswordChange` must be enforced server-side rather than trusted from browser state. API responses use explicit DTOs and never expose password hashes, bay whitelist identifiers, or persistence-only metadata. Text input is length validated and trimmed; the API remains the authoritative validation boundary.

Cancellation/modification is intentionally not exposed in the POC. Admin rejection is the supported way to release a pending slot; this avoids ambiguous user-side policy while the operational cancellation policy is agreed.

## Minimal UI scope

The initial public-facing POC is limited to:

- Login, session restoration, forced password change, and logout.
- Authenticated availability search and booking.
- The current user's bookings.
- Minimal administrator user creation and booking approval or rejection, if admin-created ordinary users are confirmed.

Messaging, bay/configuration/closure management, cancellation, rescheduling, notifications, waitlists, custom-duration sessions, and reporting are deferred. Existing API capability does not require an equivalent UI until the workflow is approved.

## Future extension candidates

Reassess these only after real POC use establishes operational value:

1. Cancellation and rescheduling with an explicit policy.
2. Bay, schedule, and closure administration.
3. Message threads and replies.
4. Notifications and waitlists.
5. Custom-duration sessions.
6. Attendance and utilization reporting.
7. Invite-based onboarding if administrator-created accounts become burdensome.

## Deployment and rollback

Configure `MorWalPizDatabase:ConnectionString` and `MorWalPizDatabase:DatabaseName` for non-mock deployments. Development defaults to the mock repository when `FeatureManagement:EnableMock` is enabled; production must reject mock mode.

The API and client continue to deploy independently through workflows bound to the GitHub `production` environment. Before public exposure, validate the authorization matrix, configured CORS origin, CSRF/login flow, persistent Data Protection keys, MongoDB readiness, unique normalized usernames, booking concurrency, and successful login by the manually inserted administrator.

Rollback is application-version rollback; additive collections and fields remain in Mongo because existing applications do not read them. Do not roll back by restoring permissive CORS, anonymous domain access, or persistence-entity responses.
