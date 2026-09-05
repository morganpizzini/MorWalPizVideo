# Shooting Range Booking

## Runtime

`MorWalPizVideo.ShootingRange` is a dedicated ASP.NET Core API. It uses the shared Models, Contracts, Domain and ServiceDefaults projects, and is registered with `MorWalPizVideo.AppHost` as `shooting-range`. `frontend/shooting-range.client` is a separate React/Vite application with the same SSR shape as `ask.client`, but no Ask-domain dependency.

## Defaults and time

The initial configuration is one global field named Pepperbox, timezone `Europe/Rome`, opening Saturday/Sunday, morning `09:00-13:00`, afternoon `14:00-18:00`, and one-hour hourly slots. `ReservedReleaseDaysBefore` defaults to `2`. Local field dates and period keys are authoritative; the API returns `startUtc` and `endUtc` as UTC instants. The browser formats those instants for display.

A reserved bay is restricted to its whitelist while the local field date is more than two calendar days away. At T-2 it is available to all users when no active booking exists. Administrative closures, bay exceptions and conflicts take precedence. This is calculated per request; no job is required for correctness.

## Data and concurrency

Collections are `shootingRangeConfigs`, `shootingRangeBays`, `shootingRangeExceptions`, `shootingRangeUsers`, `shootingRangeBookings` and `shootingRangeThreads`. Active booking uniqueness is enforced by a Mongo unique index on bay/date/period for Pending and Approved records, with duplicate keys mapped to HTTP 409. The mock repository uses a semaphore for the same critical section.

All account passwords are PBKDF2 hashes with per-account salts. Passwords are never logged or returned. New registrations are Pending, and only Approved users can authenticate. Admin reset marks `ForcePasswordChange`; the new password must be communicated out of band.

## Security policy

Authentication uses an HttpOnly, Secure, SameSite=None cookie. Unsafe authenticated requests require the `X-CSRF-TOKEN` token returned by `/api/shooting-range/csrf`. Admin routes use the `admin` role claim. Thread queries filter by the authenticated user unless the caller is an admin. Text input is length validated and trimmed; the API remains the authoritative validation boundary.

Cancellation/modification is intentionally not exposed in this first release. Admin rejection is the supported way to release a pending slot; this avoids ambiguous user-side policy while the operational cancellation policy is agreed.

## Deployment and rollback

Configure `MorWalPizDatabase:ConnectionString` and `MorWalPizDatabase:DatabaseName` for non-mock deployments. Development defaults to the mock repository when `FeatureManagement:EnableMock` is enabled. Deploy the API and SPA independently through their normal container/deployment path, then validate `/health` and a CSRF/login request. Rollback is application-version rollback; the additive collections and fields can remain in Mongo because they are not read by existing applications.
