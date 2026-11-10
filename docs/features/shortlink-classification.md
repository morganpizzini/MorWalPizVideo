# Short-link request classification

Consolidated 2026-11-09 from the classification plan.
**Delivery status unresolved:** its header says classifier/counters/events/
protected analytics implemented with UI/index/activation pending, while its
implementation notes say not implemented. No complete validation ledger resolves
the contradiction. Treat the following as acceptance/design, not verified delivery.
See [ADR-004](../architecture/adr/ADR-004-canonical-short-links.md).

## Intended behavior and invariants

- Deterministic versioned/rule-coded User-Agent estimate:
  SocialPreview, OtherAutomation, HumanCandidate, Unclassified.
  Automation rules precede browser matching; in-app brand/referrer alone is
  not bot evidence. Never call it unique users, impressions, or proven humans.
  No browser tracking or interstitial.
- Preserve raw ClicksCount/timestamps; atomically increment raw and exactly one
  category with the same UTC time. Historical residual
  unsegmented = raw - sum(categories), distinct from new Unclassified.
  No backfill/reclassification/clamp. Acceptance fixture: 100 historical,
  four new categories -> total 104, each category 1, residual 100.
- Separate minimal stable event ID/canonical link/time/category/rule/version/
  expiry. No IP, full UA/referrer, cookies, query, user ID.
  Proposed 90-day retention is **unapproved**. Nonexpiring totals are authority;
  events have bounded queries/coverage.
- Count first, event second. Event failure preserves redirect and signals
  incomplete coverage. Stable-ID retries never recount.
  No fire-and-forget/in-memory queue. Migration/merge/management writers preserve
  concurrent counters, not full-document replacement that erases increments.
- Existing `/clicks` raw array remains. Add protected analytics for 1h/24h/7d/all,
  distinguishing retention/incomplete coverage from zero.
  Dashboard seven-day comparison uses period events, not lifetime categories;
  no per-row analytics fanout.
- HEAD/OPTIONS/unresolved/unauthorized/last/clear do not count.
  Preserve target/device/status/query. Existing newsletter click remains raw,
  with no double count.

## Owners and proof needed

ShortLinks controller/redirect service/classifier and
[host tests](../../MorWalPizVideo.ShortLinks.Tests/); ShortLink model and Domain
repositories/feature/migration service; BackOffice ShortLinksController/Dashboard;
contracts/shared TypeScript/services/admin shortLinks/Home; Mongo operations.
The [video classification](content-import.md#reference-classification) record
includes classifier tests in a 16-test combined selection, but does not establish
this entire analytics/store/rollout scope.

No complete passing counts were recorded for this plan. Before calling it done:

1. Reconcile implemented versus proposed in source, including counter-preserving
   canonical migration/merge writers and frontend.
2. Approve retention and actual provider TTL/index behavior; audit/apply indexes.
3. Prove real-Mongo atomic races, stable event retry/no recount, event-failure
   coverage, historical residual, bounded query/permission behavior.
4. Verify actual branded host and `/sl/{code}` routing, UI ranges/coverage and
   dashboard period semantics. Record activation timestamp/version.

This is an Operate-mode dashboard refinement, not a redesign; use narrow
Impeccable clarify/harden and require responsive/keyboard/error-state evidence
when the UI is implemented.

## Rollout and rollback

Deploy compatible counter-preserving writers before activation. Flag-off traffic
becomes unsegmented. Disable classification while retaining fields/events on
rollback. Old full-document replacement binaries may erase counters and are
prohibited without specific verification. Never reconstruct historical categories
from guesses or delete events merely to make totals agree.
