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

Three administrator-selected modes are supported: whole morning/afternoon periods (`Periods=0`), split 60-minute windows (`Hourly=1`), and continuous 60-minute opening (`HourlyContinuous=2`). Existing enum values, routes and legitimate response fields remain unchanged. Continuous opening uses explicit nullable `ContinuousStart`/`ContinuousEnd`, never inferred from split windows. Configuration writes require `HourlyMinutes=60`, valid minute-precision same-day windows, and a positive break for split modes. Only complete hourly slots are offered; ambiguous/invalid DST boundaries are excluded. Legacy non-60 configurations remain readable but must be corrected before new bookings are accepted.

A reserved bay is restricted to its whitelist while the local field date is more than two calendar days away. At T-2 it is available to all users when no active booking exists. Administrative closures, bay exceptions and conflicts take precedence. This is calculated per request; no job is required for correctness. Sessions come from `/api/shooting-range/sessions`; the client displays them in the configured field timezone and submits expected UTC boundaries to detect stale selections.

## Data and concurrency

Collections are `shootingRangeConfigs`, `shootingRangeBays`, `shootingRangeExceptions`, `shootingRangeUsers`, `shootingRangeBookings`, `shootingRangeThreads`, `shootingRangeLocalSessions` and `shootingRangeMutationGuard`. Every repository write participates in a transaction that increments the single field guard before reading admission state. Transaction retries have a 15-second deadline; unsupported or unavailable transactional storage fails closed, without an in-process Mongo fallback. Startup probes a cross-collection transaction and aborts it. This probe does not replace target-database verification.

Pending and Approved consume capacity through half-open persisted UTC intervals per bay, regardless of period key or current mode. Adjacent intervals and different bays are allowed. The existing partial unique bay/date/period index remains secondary protection, not an interval guarantee. Generic booking insert/replace paths also validate capacity; inactive-to-active transitions recheck admission, while Pending-to-Approved preserves the original reservation. Historical intervals and booking identity cannot be rewritten. Malformed active intervals block the affected bay until operational reconciliation. The mock repository serializes mutations and restores its snapshot on failure; mock tests do not prove Mongo atomicity.

All account passwords are PBKDF2 hashes with per-account salts. Passwords must never be logged or returned. Only Approved users can authenticate. Admin reset marks `ForcePasswordChange`; the new password must be communicated out of band.

The first administrator is inserted manually into `shootingRangeUsers`; there is no public or administrative bootstrap endpoint. The inserted document must use the normalized username and the password representation produced by the current password-hashing implementation. No credential or reusable hash belongs in source or documentation.

Ordinary-user onboarding is not yet a final product decision. Admin-created users are the working assumption because it keeps account creation behind authorization; public self-registration is not part of the approved public-release posture.

## Security policy

Authentication uses an HttpOnly, Secure, SameSite=None cookie with an eight-hour, non-sliding lifetime and a server-side revocable session. The target authorization policy is deny-by-default for every Shooting Range domain endpoint, including availability; that domain cutover is NOT READY until ordinary-user onboarding is approved. Current admin routes require the `admin` role from revalidated account state.

In the target policy, only these technical entry points remain anonymous:

- Login, because authentication cannot otherwise begin.
- CSRF token acquisition, because login and later unsafe cookie requests need a token.
- Liveness and readiness probes.

Login itself and unsafe cookie-authenticated requests require the `X-CSRF-TOKEN` token returned by `/api/shooting-range/csrf`. Acquire it before login and refresh it after successful login or password change. Login is limited to 10 attempts per five minutes per remote IP per process; scaled deployments require reviewed gateway/distributed throttling and trusted proxy configuration. Production credentialed CORS allows only the configured Shooting Range client origin.

Current source does not yet satisfy the full target: registration, configuration, sessions and availability are anonymous, and no fallback authorization policy exists. These remain public-release blockers outside the scoped schedule change. Credentialed CORS now permits exactly `https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net`; no suffix, trailing-slash or arbitrary-origin matching is enabled, including in Development.

Every cookie request revalidates the session's expiry/revocation/security version and current Approved account status and role, including mine/messages. Disabled accounts are rejected; demotion removes administrator access immediately. Logout revokes the current session. Password reset/change rotates `SecurityVersion`, invalidating older sessions; successful self-change revokes and renews the current session. Forced-change sessions permit only session inspection, password change, logout and technical CSRF acquisition. Old cookies without a session claim require reauthentication. The client restores sessions on refresh/direct navigation, handles expiry/retry, and supports logout without persisting credentials or redesigning the existing UI.

Booking admission also rechecks current approved account status and `ForcePasswordChange`; touched administrator operations recheck current administrator state. Admin-user responses explicitly preserve `Id`, `CreationDateTime`, username, names, status, administrator and forced-password flags, excluding `PasswordHash`. Safe projections for other persistence entities remain a separate cutover requirement. The API remains the authoritative admission boundary.

Cancellation/modification is intentionally not exposed in the POC. Admin rejection is the supported way to release a pending slot; this avoids ambiguous user-side policy while the operational cancellation policy is agreed.

## Minimal UI scope

The initial public-facing POC is limited to:

- Login, session restoration, forced password change, and logout.
- Authenticated availability search and booking.
- The current user's bookings.
- Minimal administrator user creation and booking approval or rejection, if admin-created ordinary users are confirmed.

Schedule, bay and closure management and booking decisions are now accepted administrator scope. The client preserves `/`, `/messages` and `/admin`; this refinement does not authorize unrelated messaging expansion. Cancellation, rescheduling, notifications, waitlists, custom-duration sessions, and reporting remain deferred.

## Future extension candidates

Reassess these only after real POC use establishes operational value:

1. Cancellation and rescheduling with an explicit policy.
2. Further administration workflows beyond the accepted schedule, bay and closure controls.
3. Message threads and replies.
4. Notifications and waitlists.
5. Custom-duration sessions.
6. Attendance and utilization reporting.
7. Invite-based onboarding if administrator-created accounts become burdensome.

## Deployment and rollback

Development defaults to the mock repository when `FeatureManagement:EnableMock` is enabled; production must reject mock mode. For production Mongo configuration, operations manages `KeyVaultUrl` and `FeatureManagement:EnableKeyVault` as external application settings. When Key Vault is enabled, grant the App Service managed identity read access and store the required values as `MorWalPizDatabase--ConnectionString` and `MorWalPizDatabase--DatabaseName`; Azure Key Vault maps the double dashes to the configuration section `MorWalPizDatabase`. The API fails startup when the vault is unavailable or either required value is missing, and does not fall back to direct Mongo settings. Keep these settings outside the deployment workflow.

The API and client continue to deploy independently through workflows bound to the GitHub `production` environment. Before public exposure, validate the authorization matrix, configured CORS origin, CSRF/login flow, persistent Data Protection keys, MongoDB readiness, unique normalized usernames, booking concurrency, and successful login by the manually inserted administrator.

Mock repositories are allowed only in Development or Test; enabling `FeatureManagement:EnableMock` in Production or Staging fails startup before storage resolution. Mongo readiness checks writable replica-set/mongos topology with logical sessions; it does not replace the startup cross-collection transaction/rollback probe or target-environment evidence. Startup performs a read-only username preflight before creating the unique `normalized_username_unique` index and `local_session_expiry` TTL index. Missing/mismatched normalized fields or duplicate normalized usernames fail startup without rewriting accounts. Authorization checks session expiry independently of asynchronous TTL cleanup.

For legacy accounts, pause all account writes and take a verified backup. Audit usernames using the current `Trim().ToLowerInvariant()` normalization and record duplicate IDs in a restricted operational artifact. An operator must explicitly reconcile duplicates without automatic account merge/deletion, approve an additive `normalizedUsername` backfill, and rerun the preflight before enabling the unique index. Record index name/options, duplicate-free results, backup/restore evidence, operator, environment, timestamp and redacted artifact location. Manual first-admin insertion must include the matching normalized field. Missing legacy `securityVersion` remains readable; subsequent password changes rotate it. Existing cookies require reauthentication after the session cutover.

Before rollout, pause booking/admin writes, take a verified backup, inventory BSON/configuration/indexes, and audit all active intervals per bay for overlaps or malformed timestamps. Preserve records and quarantine affected capacity until an operator records reconciliation; do not auto-reject existing customers. Prove transaction capability and rollback on the target Mongo topology, then run independent-writer create/reactivation/decision races. Ensure every API instance uses the guard before writes resume. Deploy API first, client second, and enable new modes last.

Real-Mongo tests require `SHOOTING_RANGE_TEST_MONGO_URI` pointing to an explicitly disposable transaction-capable test database server; absent configuration skips those tests locally and blocks release evidence. The API deployment workflow requires the same secret in the GitHub `production` environment and runs the Mongo fixture before publishing; missing configuration fails the job. Client delivery no longer accepts a zero-test run. Configure only a disposable test server, never the live customer database. These delivery checks do not replace target-topology/data verification or production-environment approval.

Browser verification must cover the exact allowed origin, denied lookalikes, cookie/CSRF flow, desktop/mobile controls, SSR and direct navigation. Unavailable Mongo or browser tooling blocks release, not authoring; no deployment is implied by local checks.

Use R1-R8 and R14 in the existing [release evidence checklist](architecture/operations/phase5-activation-and-recovery.md#release-evidence-checklist-2026-10-01) for named owners, exact environment/UTC timestamps, redacted artifacts, independent approval and compatible recovery actions. All absent target proofs are BLOCKED. Slice 10 runbook authoring is complete; operational release closure is not. Slice 4 remains NOT READY until ordinary-user onboarding is explicitly confirmed; current anonymous registration is not silently removed.

Rollback requires a tested compatible guarded and session-security-capable API/client; the original unguarded or claim-only-cookie writer is not a safe rollback after the cutover. If no compatible version exists, suspend mutations and preserve history. Never restore permissive CORS or password-hash responses or production mock mode. Additive collections and fields remain in Mongo.
