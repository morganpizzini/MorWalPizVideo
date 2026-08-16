# ADR-008: Mongo Index And Migration Ownership

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

MongoDB is project-owned but no source-managed indexes or migrations exist. Queries often materialize full collections. Persisted documents require compatibility-safe evolution.

## Decision

Maintain reviewed index definitions and idempotent data migration/backfill operations in source or an explicit operational manifest. Audit and normalize data before unique indexes. Use additive fields, resumable batches, compatibility reads, query-plan validation, and backups. Destructive index removal requires a separate API operation backed by a source-controlled allowlist and a verified replacement definition.

Repositories push filters, projections, ordering, limits, and atomic updates into MongoDB.

## Alternatives

- Manage indexes manually without source record: rejected because environments drift.
- Create indexes blindly at startup: rejected because unique conflicts and large builds can break startup.
- Replace MongoDB: rejected because current issues are operational/query design, not database suitability.

## Consequences

Database operations become reviewed release artifacts. Some migrations require maintenance windows and manual conflict resolution.

## Migration And Rollback

Follow `mongo-operations.md`; retain backups and old fields until compatibility expires. For index replacement, take a verified backup, complete duplicate checks, apply and verify the replacement, audit, remove only the allowlisted legacy index, and audit again. Indexes can be removed if plans regress, but data cleanup requires explicit restore/reconciliation. Repository artifacts never prove live deployment state.

## Validation

Verify duplicate reports, batch reconciliation, query plans, latency, index usage, and duplicate-key monitoring.