# ADR-004: Canonical Short Links

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Short links are being moved from embedded match/channel fields to a standalone collection. New management writes use the standalone aggregate, but the current BackOffice management controller still reads legacy embedded links as a compatibility fallback and migrates them when updated or deleted. Public resolution is global and must not depend on an administrative channel selection.

## Decision

The target model is a canonical standalone aggregate with a normalized globally unique code, validated absolute HTTP/HTTPS destination, optional internal content/channel reference, structured query data, status, audit metadata, and atomic total count. The standalone record is authoritative for new writes and public global resolution; legacy embedded reads remain until migration/backfill is completed.

ShortLinks owns anonymous resolution/tracking only. BackOffice owns management. Detailed visits use a separate collection with an approved retention policy.

## Alternatives

- Keep embedded links: rejected for new writes, but retained temporarily as a compatibility read fallback because legacy documents still exist.
- Put management in ShortLinks: rejected because it expands a public edge service into an admin API.
- Redirect arbitrary strings: rejected because unsafe schemes/hosts would undermine branded trust.

## Consequences

New writes use the standalone collection and enforce safe absolute HTTP/HTTPS destinations for generic short links. Public resolution is a global indexed lookup for canonical records, with legacy fallback still required for embedded records. BackOffice listing and mutation are channel-scoped, including legacy embedded links; public resolution and redirect URLs are not.

## Migration And Rollback

Create the global unique normalized-code index after duplicate audit, use the standalone collection for new writes, and validate channel-scoped management authorization. Migration/backfill and removal of the legacy embedded fallback remain operational work; do not claim standalone-only resolution until that work is complete.

## Validation

The current behavior is validated for canonical writes, global public resolution, scoped management, and the legacy compatibility path. Remaining work is operational: duplicate audit, index application, migration/backfill, and evidence that the fallback can be removed.