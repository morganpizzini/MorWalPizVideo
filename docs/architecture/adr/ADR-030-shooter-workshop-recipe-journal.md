# ADR-030: Shooter Workshop recipe journal records

- **Status:** Accepted
- **Date:** 2026-11-26

## Context

The Shooter Workshop recipe detail needs a private manual journal for
date-stamped user observations. These observations do not change inventory,
ammunition stock, firearm/component counters, or other rebuildable projections.
Treating them as operation-ledger events would add unrelated replay semantics;
embedding an unbounded history into a versioned recipe would couple journal
retention to recipe updates.

The journal is descriptive only. A record may include calendar date, optional
time of day, weather, temperature and unit, power factor, and notes. It must not
calculate, assess, recommend, or fetch weather. A backdated entry does not prove
which recipe version was used.

## Decision

- Persist each entry as one immutable document in a separate account-owned
  Workshop collection. Store the date as `yyyy-MM-dd`, not a UTC instant.
- Associate each record with its account and recipe. Preserve entries when the
  recipe changes or is archived. Permit owner reads after archive and reject
  new entries for archived recipes.
- Derive account identity from the authenticated public-account claim. Reads
  and writes must verify recipe ownership; entry creation uses the existing
  public-account cookie and CSRF boundary.
- Normalize and validate date, time-of-day choice, optional text bounds,
  decimal syntax, temperature/unit pairing, and the requirement for at least
  one detail in addition to the date. Preserve accepted decimal text and do
  not apply technical thresholds or calculations.
- Derive the entry document ID from the account, idempotency key, and the
  journal-specific purpose. Persist the normalized request fields and a
  payload hash. A duplicate document ID returns the existing record only for
  the same recipe and payload; a different payload returns a conflict. The
  single insert is the durability boundary; no multi-document atomicity is
  claimed.
- Query entries by account and recipe, ordered by calendar date descending and
  ID descending. Add a non-unique
  `{ accountId: 1, recipeId: 1, entryDate: -1, _id: -1 }` query index to the
  Workshop index manifest. The index is not an authorization control.
- Keep the feature online-only and route reads/writes through the existing
  shared account transport and React Router loader/action boundaries.

## Alternatives

- **Append journal facts to the Workshop operation ledger:** rejected because
  journal entries have no projection effects and do not need replay or
  correction semantics.
- **Embed entries in the recipe document:** rejected because an append-only
  history would grow the mutable recipe document and make history retention
  depend on recipe updates.
- **Use a multi-document transaction:** rejected because a single journal
  document is sufficient and Workshop does not claim general MongoDB
  transaction guarantees.
- **Store a UTC event timestamp for the journal date:** rejected because it
  can shift a user-entered calendar date across time zones.

## Consequences

The API and collection are additive; existing recipe documents and clients
remain unchanged. The idempotency key remains stable across retry of a single
entry and must not be reused for a different payload. Archived recipes remain
readable but have no journal-create affordance. A missing query index affects
read performance only, not ownership or correctness.

The existing BackOffice Mongo index operations surface remains operational
governance only. It applies the feature-owned index definition without taking
ownership of Workshop behavior.

## Migration and rollback

No backfill or schema migration is required. The API is deployed before the
client; the central nonblocking index bootstrap attempts the additive query
index and operators can verify it through the existing audit surface. Rollback
removes the UI first and may restore the API, but leaves journal documents
intact. User-entered history must not be deleted as a rollback shortcut.

## Validation

Focused service tests cover normalized values, date/decimal/unit validation,
same-payload retry, changed-payload conflict, account isolation, and archived
read/write behavior. ServerAPI integration tests cover authentication, CSRF,
round-trip, retry, and cross-account concealment. Frontend tests cover
route-owned loading, URL selection, input preservation, retry identity, and
archived read-only behavior. Actual Mongo index application and deployed
same-origin browser behavior remain operational verification gates.
