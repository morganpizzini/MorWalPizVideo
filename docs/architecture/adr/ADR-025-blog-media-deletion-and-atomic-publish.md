# ADR-025: Atomic blog publish and recoverable media deletion

## Status

Accepted for the BackOffice blog implementation.

## Decision

Primary Save uses a dedicated save-and-publish endpoint and one optimistic-concurrency
replacement (or one insert for a new article). Draft Save keeps its existing behavior.
Individual image removal persists a pending image ID, deletes the server-resolved exact key
from `PageContainerName`, and finalizes with another expected-revision replacement.
Pending state survives unrelated writers, blocks references and whole-post deletion, and is
retried explicitly by the editor.

## Consequences

Pending IDs are administrative-only. Provider soft-delete/version/snapshot/retention
configuration must be verified before describing the operation as irreversible erasure;
cached or downloaded bytes cannot be revoked by this application.
