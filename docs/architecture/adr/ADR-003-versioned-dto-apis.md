# ADR-003: Versioned DTO-Based JSON APIs

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Current APIs use unversioned routes, direct Mongo entities, anonymous response shapes, strings, and inconsistent errors. Coordinated deployment reduces but does not eliminate contract risk across web, desktop, and cached clients.

## Decision

New JSON APIs use URL-segment `/api/v1` routes, explicit request/response DTOs, version-aware OpenAPI, authoritative server validation, and RFC Problem Details. Redirect routes remain unversioned.

Persistence-only fields never appear in public DTOs. A version changes only for incompatible externally observable contracts.

## Alternatives

- No versioning: rejected because multiple independently deployed clients exist.
- Header-only versioning: rejected because URL segments are easier to inspect, route, document, and cache.
- Version persistence models: rejected because storage evolution is internal.

## Consequences

Compatibility aliases temporarily increase route/test volume. Contracts become safer to evolve and document.

## Migration And Rollback

Run unversioned and v1 routes in parallel. Migrate shared services and clients, observe usage, then remove aliases after a defined window.

## Validation

Contract serialization tests, OpenAPI checks, and old/new route equivalence tests run during migration.