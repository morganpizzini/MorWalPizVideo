# Content import and video classification

Consolidated 2026-11-09 from grouped import and classification handoffs.
All implementation/checks are **reported local evidence**, not rerun or
deployed verification. See the [handoff map](../feature-handoffs/README.md#former-handoff-map).

## Grouped multi-video import

Recorded implemented; activation remains held behind **EnableVideoBulkImport**.
Checked-in defaults enabled do not prove deployed flags; the delivery record
requires the production override off until qualification.

- Groups have a root and zero or more categorized new-candidate references.
  Trimmed video IDs must be globally unique, with no root/reference reuse or
  self-reference. Categories are per video. Enforce total **25 videos** before
  writes.
- Root-first sequential processing; independent groups continue; failed root
  means its references are notAttempted. Existing-target screen was removed,
  while the import-targets compatibility endpoint remains.
- Report persistence/shortlink/cache/error per video. Every successful root/ref
  has a canonical standalone link. Cleanup-pending means canonical exists:
  warning, not destructive compensation.
  Actual pre-canonical failure compensates only the affected root/append.
  Cache degradation never undoes persisted content. No batch transaction,
  whole-batch rollback, or end-only invalidation.
- Proposed IVideoImportWorkflowService extraction/refactoring across single,
  add-reference, and bulk was **not delivered**: orchestration remains in the
  controller. Extract before broader reuse, not as implied completed work.

Owners: VideoBulkImportRequests, BackOffice VideosController,
[videoService.ts](../../frontend/back-office-spa/src/services/videoService.ts),
and [import route](../../frontend/back-office-spa/src/routes/videos/import/).
Reuse Domain atomic append/LinksService/ExternalDataService rather than new
parallel import semantics.

Evidence: restore/BackOffice build, zero errors/68 existing warnings; focused
import component suite passed on Node 26/Yarn Classic with no count recorded.
Comprehensive HTTP/compensation/maximum-size/E2E acceptance was not established.
Grouped interpretation, 25 cap, and partial success were implemented choices;
separate final product confirmation was not recorded.

Deploy API/SPA with flag off, then controlled canonical-link/cache/provider
smoke before enable; browser and independent review remain pending.
Rollback flag first, then artifacts; preserve successful partial records/links
and reconcile only affected items. No data migration.

## Reference classification

Recorded locally implemented 2026-11-04, not deployed.
See [Domain Model](../architecture/domain-model.md),
[Calendar compatibility](../architecture/calendar-compatibility.md), and
[canonical short-link ADR](../architecture/adr/ADR-004-canonical-short-links.md).

- Count actual stored attached references, without deduplication/synthesis:
  zero = empty Collection; one = SingleVideo; two or more = Collection.
  Null references mean empty. Thumbnail, links, or images are not attachments.
- Keep archival BSON contentType/factories unchanged; EffectiveContentType is
  derived and excluded from BSON/JSON. Preserve existing numeric 0/1,
  v1 strings, and isLink aliases. Converters and response-only Shooting ITA
  copies project effective type. Admin count is read-only, replacing ineffective
  editable type.
- Refresh actual attached IDs, preserving order/categories/channel/timestamps
  and unavailable references. Empty references make no provider call.
  Only when the sole reference ID equals factory-owned ContentId may provider
  metadata replace aggregate title/description/slug; otherwise preserve editorial
  metadata, URL, and thumbnail.
- Calendar nullable response-only matchVideoId: absent permits legacy client
  compatibility; explicit null forbids ID-equality guessing. Ship compatible
  public client first. No Calendar BSON/revision rewrite.
- `/last` uses greatest reference creation time. Canonical shortlinks must match
  attached targets; never use thumbnail fallback. Preserve device/query/
  accounting/channel/privacy semantics.
- Calendar memory reset/output purge joins matches/links for edits, refs, refresh,
  deletion, assignments and compensation, including failed compensation leaving
  a mutation. Purge old payloads on activation and rollback.

Owners: YouTubeContent, ContractUtils, CalendarRequests, ExternalDataService;
ServerAPI Calendar/V1Videos/ShootingIta, BackOffice VideosController, ShortLinks
redirect controller; admin video list/edit/detail, shared calendar/video types,
public calendar, Shooting ITA consumers.

Evidence (overlapping selections, do not add): backend 53 and 61 with six overlap
= **108 distinct**; ShortLinks **16**; admin initial 27/final edit 11 =
**28 distinct**; public calendar/home/loader **11**; Shooting ITA **7**.
Final selected tests had no skips. Shared builds and three strict app TypeScript
checks passed; detector empty. Node 26 experimental storage was disabled for tests
only, not production. Existing ImageSharp/compiler warnings remained.

Open: full solution/workspace/suites/lint, Windows consumers on a suitable host,
browser, deployed Mongo/cache/redirect destinations, external consumers, backups.
Coordinate three hosts/two SPAs on rollout. No backfill.
Rollback must pause unsafe old refresh and purge caches: old stored-SingleVideo
multi-reference refresh can truncate again. Already-mutated metadata requires
operator backup, not merely code reversal.
