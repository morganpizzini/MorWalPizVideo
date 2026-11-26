# ADR-027: Shooter Workshop immutable operation ledger and projection reconciliation

- **Status:** Accepted for the Workshop delivery baseline
- **Date:** 2026-11-13

## Finance and inventory addendum (2026-11-25)

The finance/inventory feature uses the existing public-account Workshop boundary
but does not introduce an account-wide coordination gate or require MongoDB
transactions. The user expects online operations by one person and accepts the
risk that an interruption between separate writes can leave a partial production,
lot, or batch update. This is a known limitation, not an atomicity guarantee.

Production records are editable. An edit must adjust the recorded primer/bullet
lot effects and produced-batch quantity and retain correction history. Users must
be able to inspect and correct records after an uncertain interruption. Explicit
server rejection is a failed operation; a lost response may have an unknown
outcome, so the client must preserve request identity for any retry it offers.

This addendum narrows only the new finance/inventory consistency scope. It does
not remove the existing immutable-ledger/replay rules for shooting and other
Workshop operations, nor does it relax account ownership, validation, stock
non-negativity checks, or auditability.

Inventory package size is an additive per-purchase snapshot for primers and
bullets. Lot quantities and production consumption remain individual-unit
quantities, preserving the existing stock and partial-cost denominator. Missing
package fields on old requests retain old unit semantics. Product type remains
the current enum and its existing wire representation; the frontend performs
explicit normalization rather than changing the serializer or introducing a
union contract. Product brand/name edits preserve identity. Products with
history are archived rather than deleted; deletion of an unreferenced product
uses an account-scoped reference check and optimistic revision, but that
multi-document check cannot exclude every concurrent reference write without a
new coordination mechanism. This is a known residual limitation, and archival
is preferred when concurrent writes are possible.

## Context

Shooter Workshop records one user operation that can affect several independently
projected values: firearm rounds, physical-component rounds, ammunition stock, and
ammunition allocation. MongoDB multi-document transactions are not an established
ServerAPI/Domain convention, so a sequence of document updates cannot be treated as
an atomic source of truth. Blind counter increments would make retries, corrections,
backdated sessions, component moves, and failures between writes capable of creating
plausible but divergent totals.

The Workshop is private to the existing public account boundary. It is not a
BackOffice product feature. The existing BackOffice central Mongo index manifest is
an unavoidable operational governance surface only: it may audit/apply the
feature-owned stable indexes, but it does not own Workshop commands, data access,
authorization, reconciliation, or UI behavior.

## Decision

The authoritative Workshop history is an immutable, account-owned operation ledger.
Each accepted command appends one complete versioned operation bundle containing:

- the operation ID, account ID, client idempotency key and payload digest;
- event type, schema version, effective/recorded timestamps and causal links;
- all typed firearm, physical-component, ammunition-stock and allocation deltas;
- the bounded shooting segments, installation/batch references and correction links
  required to explain those deltas.

The account ID is derived from the authenticated public-account claim; it is never
accepted as an authority-bearing request field. A unique account/idempotency-key
constraint makes identical retries return the original result and makes a different
payload a conflict.

Firearm totals, component totals, batch stock, allocated quantities and available
quantities are rebuildable projections. A command validates the complete effect set,
appends the ledger bundle once, and rebuilds every affected projection with
optimistic revision checks. The command returns success only when affected
projections are current. If a durable append is followed by a projection failure,
the operation remains explicitly pending/inconsistent and is recoverable; it is not
reported as ordinary success.

Reconciliation independently replays each aggregate stream from immutable ledger
facts and compares the rebuilt value and deterministic contribution digest with the
stored projection. Repair replaces projections only; it never edits, deletes, or
reinterprets ledger facts. Historical correction appends a complete compensating
bundle and, where needed, a complete replacement bundle linked to the original.

The browser expresses known ammunition consumption as recipe ID and quantity. The
Domain resolves that intent against account-owned ammunition batches in ascending
production-date order, using batch ID as a stable tie-breaker, and persists the
resolved batch facts in the operation and shooting-session read document. Batch and
allocation IDs are not user-entered authoritative inputs. The Workshop UI exposes
correction inline: a positive save appends reversal plus replacement facts, while
delete or a confirmed zero quantity appends only the reversal. Original ledger facts
and audit history remain immutable.

Allocation/reservation is retired from the accepted current product model. Recipe and
batch stock are not firearm-specific, and current availability is total batch on-hand
quantity. New operations do not emit allocation aggregates or allocation IDs. Legacy
allocation operation types, delta fields, digests, and correction reversal remain
read-compatible until deployed retention evidence permits a separate physical
contraction. Inline positive correction supplies explicit per-recipe quantities,
preserves original untracked ammunition unchanged, and derives total rounds rather
than redistributing recipes proportionally.

Recipe snapshots and accepted session segment attribution are persisted with their
operation facts. Replay never derives historical component attribution from the
current installation. For a shooting session:

`known batch consumption + explicit untracked ammunition = session total`.

The Workshop service rejects ambiguous installation timelines, overlapping active
installations, over-allocation, over-consumption, revision conflicts, and incomplete
cross-aggregate effects rather than guessing or accepting a partial update.

Feature-owned index definitions use stable names, are duplicate-audited before any
unique index is applied, and are included in the existing central operational
manifest. This operational handoff does not add a BackOffice dependency to product
behavior.

## Alternatives

- **Blind updates to current counters:** rejected because partial writes and retries
  can permanently diverge related totals.
- **Use one aggregate projection as the source for the others:** rejected because
  firearm use, component attribution, stock, and allocation have different meanings
  and lifecycles.
- **Require a MongoDB multi-document transaction:** rejected because the current
  ServerAPI/Domain path has no established transaction convention and deployment
  topology cannot be assumed to provide one.
- **Store unbounded applied-event IDs on every aggregate:** rejected because it
  duplicates the authoritative history and grows aggregate documents without
  improving replayability.
- **Make BackOffice the Workshop service or reconciliation owner:** rejected because
  Workshop product behavior belongs to ServerAPI/Domain and the public account
  boundary; BackOffice remains only an operational index-manifest participant.

## Consequences

Workshop writes require idempotency keys, complete effect validation, optimistic
revisions, stable account/aggregate indexes, and explicit projection status. Reads
must expose pending or inconsistent state rather than synthesizing a successful
dashboard from stale counters. Personal-account scale permits replaying affected
streams synchronously for normal commands; full-account repair remains a bounded
support/recovery operation.

Ledger facts and correction chains increase durable storage and require schema-version
compatibility discipline. Logs and metrics may include operation-safe correlation
metadata, but must exclude account email, notes, recipe values, and firearm
identifying free text.

## Migration and rollback

The initial release is additive: new collections and indexes are introduced without
backfilling existing Workshop data. Persist an event schema version from the first
write. Unknown versions fail explicitly during replay. Readers may tolerate only
documented optional additive fields.

Deploy compatible API/model/repository behavior and verify indexes before enabling
Workshop writes. A frontend can be rolled back independently. ServerAPI may only be
rolled back to a version that can deserialize all persisted ledger schema versions.
If a projection is suspect, disable new command acceptance as needed and rebuild it
from the ledger. Never delete ledger facts or repair by editing projected counters.

## Validation

Release evidence must cover duplicate-idempotency and payload-conflict behavior,
faults after ledger append and between projection writes, correction inversion,
backdated component attribution, concurrent allocation/consumption conflicts,
account isolation, deterministic full replay, deliberate projection corruption and
projection-only repair. The exact-origin browser authentication gate is
`https://workshop.morwalpiz.com`; local AppHost development uses the explicit
Workshop endpoint on `http://localhost:5179` under the existing Development CORS
behavior.
