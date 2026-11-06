# ADR-022: Startup Bootstrap For Blog Interaction Indexes

- **Status:** Accepted
- **Date:** 2026-11-06
- **Supersedes:** ADR-008 only for the narrowly scoped blog-interaction indexes below

## Context

Blog comments, reactions and portal access use new Mongo collections with required
unique and query indexes. Requiring a separate manual index operation before feature
use adds rollout steps and risks serving writes before idempotency and uniqueness
constraints exist. The collections may not yet exist when the APIs start.

ADR-008 correctly rejects blanket startup index creation: legacy unique indexes can
fail on existing duplicate data, and large index builds can disrupt production.

## Decision

ServerAPI and BackOffice run one shared Domain-owned initializer in Mongo mode before
the host accepts traffic. It verifies and creates only the blog-interaction indexes
owned by `BlogInteractionMongoIndexes`. Mock mode does not register or run it.

The initializer:

- handles an absent collection as an empty index set, creates the empty collection,
  then creates its indexes;
- skips an exact existing index and never drops, replaces or renames an index;
- fails host startup on a same-name definition mismatch, a conflicting key pattern,
  duplicate data, or any database error that prevents index creation;
- tolerates concurrent API instances creating the same collection or exact index;
- uses the same source definitions as the BackOffice audit/apply manifest.

All other manifest indexes retain ADR-008's reviewed manual-apply process. This
exception is not a general startup migration facility.

## Alternatives

- Manually apply these new-feature indexes before each deployment: rejected for this
  feature because both API hosts can safely ensure the small, additive set before
  accepting interaction traffic.
- Automatically apply every manifest index: rejected because legacy unique indexes
  need data audits and some index builds may be operationally disruptive.
- Create indexes only in BackOffice: rejected because ServerAPI owns public writes
  and could otherwise accept interaction requests before BackOffice starts.
- Silently ignore index errors or continue without indexes: rejected because the
  uniqueness/idempotency guarantees would not hold.

## Consequences

Both real API hosts need Mongo permissions to list collections/indexes and create
collections/indexes for the interaction collections. A failed initialization makes
that host fail startup and requires operator inspection; it does not silently proceed.
Deployments using a Mongo-compatible provider must validate its collection/index
creation semantics and required privileges.

## Migration And Rollback

Deployment is additive. On first startup, APIs create only the three feature-owned
collections and their declared indexes. Existing documents are preserved. If an
unexpected conflicting index or duplicate data prevents creation, startup fails;
resolve it through the reviewed manual audit/apply process rather than deleting or
rewriting data automatically.

Rollback removes the initializer from the API deployment or reverts the application
version. Retain the additive collections and indexes; do not drop them as part of
application rollback.

## Validation

Focused tests verify startup-manifest ownership, missing-namespace classification,
index definition comparison including partial filters, and both API builds. A
disposable or deployed Mongo integration test remains necessary to verify the exact
behavior and permissions of the selected Mongo-compatible provider.
