# ADR-018: Public Account Boundary

## Status

Implemented locally; production rollout evidence remains open.

## Decision

Public self-service accounts use separate `PublicAccount` and `PublicAccountSession` MongoDB collections and a dedicated `PublicAccount` cookie authentication scheme in ServerAPI. Domain owns password hashing, normalized identity lookup, fixed session expiry, revocation, and security-version revalidation. Contracts expose only safe account summaries.

The account transport is isolated from the anonymous public transport and BackOffice transport. Account requests use credentialed HttpOnly cookies and CSRF headers, while public content and anonymous Web Push continue using `credentials: omit`. Existing public routes remain anonymous and are not guarded until a future restricted feature requires it.

## Consequences

Registration, login, session restore, profile update, and logout are additive endpoints. Unique normalized username/email indexes and a session expiry TTL index are source-managed through the existing Mongo index operations. Push subscriptions remain browser-scoped and are not linked to accounts.

Production still requires exact-origin browser evidence, persistent Data Protection key storage, duplicate audits, approved index application, and deployment telemetry before this decision is considered operationally complete.
