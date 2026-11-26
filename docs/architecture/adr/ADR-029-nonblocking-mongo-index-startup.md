# ADR-029: Non-Blocking Shared Mongo Index Startup

- **Status:** Accepted
- **Date:** 2026-11-26
- **Supersedes:** ADR-008 startup policy, ADR-022, and ADR-028 for shared-platform Mongo indexes

## Context

BackOffice owns the executable allowlist used to audit, apply, and remove shared
platform Mongo indexes. Index creation was split among BackOffice and ServerAPI
startup services, while BlogRepository also created two indexes on demand. This
left startup behavior incomplete and duplicated across hosts.

The application owner wants BackOffice to attempt all source-defined shared
platform indexes at startup, but explicitly accepts that applications can start
and serve requests while indexes are missing or creation fails. FreezerAnalysis's
separate application database is outside this decision.

## Decision

In Mongo mode, BackOffice starts one background initialization against every
entry in its canonical `MongoIndexOperationsService.Manifest`, including the
existing feature-owned manifests and the two BlogRepository indexes. Entries
are attempted independently and sequentially. After each apply attempt, the
initializer audits the actual definition. It logs each verified or failed
index and a final count of verified, failed, and unverified entries.

The background work must not block startup, readiness, or request traffic.
ServerAPI and ShortLinks do not verify or create indexes at startup. Mock mode
does not run Mongo index initialization. Existing Mongo connectivity health
checks remain unchanged; a database outage can still affect ordinary database
operations and the existing Mongo health status.

An index failure is not a successful index state. The user manages failures by
checking logs and the authenticated BackOffice audit endpoint, resolving data
or definition conflicts, then using the existing approved apply operation or
restarting BackOffice. There is one attempt per BackOffice process start; no
automatic retry loop is added.

Startup remains additive: it never removes or replaces an existing index,
cleans or backfills documents, or invokes the removal manifest. The existing
authenticated operator audit/apply/remove contracts remain available.

The policy covers indexes in the shared platform database only. It does not
change FreezerAnalysis's separate application database, host, or index lifecycle.

## Alternatives

- Keep startup blocked until every index exists: rejected by the owner, who
  accepts the availability and data-integrity consequences of fail-open startup.
- Keep independent index creation in ServerAPI and feature repositories:
  rejected because it duplicates ownership and does not cover the full manifest.
- Retry indefinitely or introduce a coordinator/lock: rejected; the operator
  owns remediation, and existing per-index create-conflict handling is retained.
- Automatically remove conflicting indexes or clean duplicate data: rejected
  because those changes require separate reviewed operations.

## Consequences

BackOffice requires privileges to list/create the manifest's collections and
indexes. Missing unique indexes can permit duplicate writes before they are
created; later creation can fail if duplicates accumulated. Missing query
indexes can degrade performance. A newly created TTL index can cause
database-managed expiry of already-expired documents. The owner accepts these
risks and will manage reported failures.

The initializer must distinguish verified, failed, and unverified indexes in
logs. Startup success, readiness, or an HTTP response must not be interpreted
as evidence that the full manifest was applied.

## Migration And Rollback

Before enabling the new BackOffice artifact in an environment, operators should
inventory current indexes and data, review duplicate risks for unique indexes,
review TTL retention effects, and verify a restorable backup. These are
operational safeguards, not startup or deployment gates.

Created indexes remain after code rollback. Rollback may restore the previous
ServerAPI/BackOffice startup creators and BlogRepository lazy creation; do not
drop indexes or restore expired data automatically. An interrupted or failed
attempt is reconciled by auditing actual database state before retrying.

## Validation

Focused tests verify the complete manifest is attempted, one failed index does
not prevent later attempts, actual definitions are audited, and host startup
returns while index work is pending. A disposable Mongo-compatible provider
test remains necessary to qualify provider-specific create, privilege, unique,
TTL, and conflict behavior before an environment rollout.
