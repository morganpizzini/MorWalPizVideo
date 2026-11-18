# ADR-028: Startup Bootstrap For Active Reader Indexes

- **Status:** Accepted
- **Date:** 2026-11-18

## Context

The public channel match reader and BackOffice feedback list rely on
provider-specific composite ordering indexes. Their repository queries must
filter, sort, skip and limit in MongoDB rather than materialize an entire
collection and sort in application memory. The existing reviewed index
operations manifest already describes the required non-unique reader indexes,
but registration of the manual operations service does not apply them.

## Decision

In Mongo mode, ServerAPI and BackOffice automatically verify or create the
public YouTube-content ordering index before accepting traffic. BackOffice also
automatically verifies or creates the feedback creation-order index. The
definitions are feature-owned in Domain and projected into the BackOffice
operator manifest so startup and manual audit use the same key definition.

The startup allowlist is explicit and currently contains only:

- `youtubecontent_isprivate_latestpublished_creation_desc`
- `feedback.createdat_desc`

Startup is additive and idempotent. It skips exact existing definitions,
fails on same-name or key-pattern conflicts, tolerates concurrent exact creates,
and never drops, renames, repairs data or creates unique/TTL indexes. All other
manifest operations remain reviewed manual operations.

The single-channel public match reader uses persisted `latestPublishedAt` and
`creationDateTime` ordering, followed by server-side skip/limit. Its indexed
content cache is not used for this list/count path because it requires
materializing and sorting the complete channel set. Other cache consumers remain
unchanged.

## Compatibility And Rollout

`latestPublishedAt` is maintained by repository write boundaries. Existing
documents must be audited and backfilled using the established resumable
procedure before enabling the database-sorted reader, because missing or stale
values cannot be reconstructed by the compound index. Provider-specific index
creation and readiness must be verified against the deployed Cosmos Mongo
provider; a create acknowledgment alone does not prove that the index is ready
to serve the query.

The feedback startup index is non-unique and does not authorize or apply the
unapproved feedback TTL index.

## Consequences

Public reads use bounded database work and can use the provider's composite
index, reducing memory and latency pressure for large collections. Startup
requires Mongo list/create-index privileges and can fail visibly on conflicts,
permissions or provider incompatibility. Existing index operations, API
contracts, authentication, cache invalidation and persisted document shape are
otherwise unchanged.

On rollback, retain any created index and backfilled compatibility values.
Restore the previous reader only after validating output-cache state; do not
drop indexes or reverse data changes automatically.
