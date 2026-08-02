# ADR-009: Authenticated Cache Invalidation

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

BackOffice invalidates ServerAPI caches through HTTP, but current protected endpoints and unauthenticated client calls conflict. Cache registration and middleware use separate flags.

## Decision

Use one coherent output-cache feature and centralized lowercase invariant tags. BackOffice invokes an authenticated internal invalidation command after successful mutations. Internal cache operations are not anonymous GET endpoints. Failures are logged, traced, and retry-aware.

## Alternatives

- Time expiry only: rejected because management changes should become visible promptly.
- Anonymous purge endpoint: rejected due to denial-of-service and exposure risk.
- Direct API project reference/shared memory: rejected because services deploy independently.

## Consequences

An internal authentication/configuration contract is required. Public cache behavior becomes deterministic and observable.

## Migration And Rollback

Add the authenticated command and dual invalidation telemetry before removing old endpoints. Time expiry remains a safety fallback.

## Validation

Integration tests mutate BackOffice data, verify authenticated eviction, reject anonymous purge, and observe refreshed ServerAPI reads.