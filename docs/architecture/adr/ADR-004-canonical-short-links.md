# ADR-004: Canonical Short Links

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Short links are stored standalone and embedded in content and channels. Resolution scans collections and click updates replace full documents. Global code uniqueness and safe generic URL handling are impossible in this form.

## Decision

Every short link becomes a canonical standalone aggregate with a normalized globally unique code, validated absolute HTTP/HTTPS destination, optional internal content/channel reference, structured query data, status, audit metadata, and atomic total count.

ShortLinks owns anonymous resolution/tracking only. BackOffice owns management. Detailed visits use a separate collection with an approved retention policy.

## Alternatives

- Keep embedded links: rejected because uniqueness and indexed resolution span aggregates.
- Put management in ShortLinks: rejected because it expands a public edge service into an admin API.
- Redirect arbitrary strings: rejected because unsafe schemes/hosts would undermine branded trust.

## Consequences

A data backfill and conflict resolution are required. Resolution becomes one indexed lookup and counts become concurrency-safe. Canonical short-link codes are normalized and compared case-insensitively for compatibility, while new writes enforce safe absolute HTTP/HTTPS destinations for generic short links.

## Migration And Rollback

Backfill, dual-read canonical-first, create the unique index, switch writes, observe legacy use, then remove embedded data. Retain backups and compatibility reads for rollback.

## Validation

The canonical short-link implementation is now in place and validated for the current behavior slice. The core behavior is implemented and covered by the current tests; remaining follow-up is operational (for example monitoring rollout, retention, and any later cleanup of legacy compatibility reads) rather than a missing core feature.