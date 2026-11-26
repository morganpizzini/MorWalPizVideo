# ADR-008: Mongo Index And Migration Ownership

- **Status:** Superseded for shared-platform startup creation by [ADR-029](ADR-029-nonblocking-mongo-index-startup.md); retained for reviewed operator operations and data-migration safety
- **Date:** 2026-08-01

## Context

MongoDB is project-owned but no source-managed indexes or migrations exist. Queries often materialize full collections. Persisted documents require compatibility-safe evolution.

## Decision

Maintain reviewed index definitions and idempotent data migration/backfill operations in source or an explicit operational manifest. Audit and normalize data before unique indexes. Use additive fields, resumable batches, compatibility reads, query-plan validation, and backups. Destructive index removal requires a separate API operation backed by a source-controlled allowlist and a verified replacement definition.

For shared-platform startup creation, this policy is superseded by ADR-029.
Audits, explicit operator apply/remove operations, data cleanup, backfills, and
structural index migrations remain governed by this ADR and the current
operational runbook.

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

Mock validation verifies duplicate/malformed reports, deterministic migration overwrite,
resumability, and canonical cleanup ordering. Mongo production completion still requires
environment-specific duplicate reports, batch reconciliation, index audit/apply output,
query-plan and latency evidence, and duplicate-key monitoring. The short-link unique index
is an explicit operational action and is not created during host startup.