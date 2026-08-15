# ADR-004: Canonical Short Links

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Short links need one globally addressable owner for reliable lookup, uniqueness, and click counting. BackOffice management is channel-scoped, while public resolution is anonymous and global. A YouTube video target still needs to reference a video in its owning content aggregate, but that relationship is validation data, not a legacy short-link storage or resolution path.

## Decision

The standalone record is canonical for every short-link type, including YouTube videos, with a normalized globally unique code, validated destination/reference, optional content or channel reference, structured query data, status, audit metadata, and an atomic total count. A YouTube video link stores the owning content ID and target video ID in the standalone record; creation and management validate that the target is an existing video reference. Embedded YouTube short links are not read, redirected, listed, mutated, or counted.

ShortLinks owns anonymous resolution/tracking only. BackOffice owns management. Detailed visits use a separate collection with an approved retention policy.

## Alternatives

- Keep embedded YouTube links: rejected because split ownership prevents one authoritative lookup and leaves click counts and uniqueness ambiguous. Legacy records require an explicit inventory/backfill decision; they are not a runtime compatibility path.
- Put management in ShortLinks: rejected because it expands a public edge service into an admin API.
- Redirect arbitrary strings: rejected because unsafe schemes/hosts would undermine branded trust.

## Consequences

All short-link writes use the standalone collection and enforce safe absolute HTTP/HTTPS destinations for generic links. YouTube writes validate the owning content and video reference, then persist the canonical record. Public resolution uses the indexed standalone lookup and validates a canonical YouTube link against the configured public channel before redirecting. BackOffice listing, publishing, and mutation use standalone records only and remain channel-scoped.

BackOffice video import and short-link creation are mutation operations: a caller must be able to mutate the owning content in the selected channel. Read-only collaborators may inspect a match but cannot use bulk import or YouTube short-link operations to change it. Duplicate video imports return a conflict and do not refresh content or create a short link. Canonical match output-cache entries are tagged and evicted with the same lowercase `CacheKeys.Matches` value after content mutations.

## Migration And Rollback

Create and maintain the global unique normalized-code index for standalone records. Before rollout, inventory legacy embedded YouTube links, reconcile duplicate codes and click totals, and backfill canonical records idempotently. Because Azure Cosmos DB for MongoDB RU does not support cross-collection transactions, any workflow that updates content and channel collections uses explicit sequential writes, reports which step failed, and supports reconciliation. After canonical records are verified, legacy embedded records remain archival data only and are excluded from runtime reads.

## Validation

The current behavior is validated for standalone resolution and atomic click counting, embedded YouTube non-resolution, channel scoping, canonical writes, public DTO projection, scoped management, duplicate-import conflict handling, collaborator mutation denial, and cache-tag alignment. Remaining work is operational: duplicate audit, index application, legacy-link backfill/reconciliation, and production evidence for removing or retaining archival embedded fields.