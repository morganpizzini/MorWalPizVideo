# MongoDB Operations

## Status

MongoDB is owned and operated by the project owner.

Source-owned index operations now exist:

- BackOffice API exposes authenticated index operations at `GET /api/mongoindexes/audit` and `POST /api/mongoindexes/apply`.
- Index definitions are maintained in source via `MongoIndexOperationsService` (runtime manifest) and the operational file `docs/architecture/operations/mongo-index-manifest.phase4.json`.

Operational evidence is now recorded for the Phase 4 verification baseline under `docs/architecture/operations/mongo-index-audits/`:

- `phase4-2026-08-03-sample-audit-output.json`
- `phase4-2026-08-03-sample-apply-output.json`
- `phase4-2026-08-03-explain-evidence.md`
- `phase4-2026-08-03-verification-bundle.md`

Per-environment production records should continue to be added for each rollout window.

## Safety Sequence

For every index or structural migration:

1. Take and verify a restorable backup/snapshot.
2. Inventory collection size and current indexes.
3. Run duplicate and malformed-data reports.
4. Resolve conflicts deterministically and record affected IDs.
5. Backfill normalized/additive fields in resumable batches.
6. Create indexes during an appropriate maintenance window.
7. Validate representative query plans and latency.
8. Monitor duplicate-key failures, lock/load impact, and storage growth.
9. Retain compatibility reads until deployed consumers are verified.

Never create a unique index before duplicate analysis.

## Recommended Index Inventory

| Collection | Definition | Purpose / prerequisite |
|---|---|---|
| `digitalProducts` | Unique normalized name | Prevent duplicate artifact names after case-insensitive duplicate cleanup |
| `digitalProducts` | `isActive`, creation timestamp descending | Public active catalog ordering |
| `digitalProducts` | Multikey `categoryIds` | Category filtering |
| `digitalProductCategories` | Unique normalized name | Administrative uniqueness after cleanup |
| `digitalProductCategories` | `displayOrder` | Server-side ordered category reads |
| `customers` | Unique normalized email | Future verified customer identity; defer creation until normalization exists |
| `carts` | Cart owner, completion/status, updated timestamp descending | Active cart lookup |
| `carts` | Partial unique cart owner where active | Enforce one active cart per owner after duplicate resolution |
| `freeAcquisitions` | Unique owner identity plus artifact ID | Idempotent permanent-free acquisition |
| `freeAcquisitions` | Artifact ID, acquired timestamp descending | Administrative/reporting lookup |
| `shortLinks` | Unique normalized lowercase code | Global short-code resolution after embedded-link migration |
| `quickLinks` | Unique normalized lowercase slug | Global QuickLinks page resolution across channel owners after duplicate audit |
| `shortLinkVisits` | Short-link ID, occurred timestamp descending | Recent visit analysis |
| `shortLinkVisits` | TTL on occurred timestamp, optional | Only after retention policy approval |
| `youtubeContent` | Partial unique non-empty URL | Detail lookup after duplicate audit |
| `youtubeContent` | Thumbnail video ID | Legacy/fallback lookup; uniqueness requires audit |
| `youtubeContent` | Multikey video reference YouTube ID | Video ownership and lookup |
| `youtubeContent` | Visibility/private state, creation timestamp descending | Public/private listing |
| `ytChannels` | Unique channel ID | Canonical channel identity |
| `ytChannels` | Channel name | Administrative lookup |
| `queryLinks` | Unique normalized title | Administrative uniqueness after cleanup |
| `apiKeys` | Secure key hash, unique | Authentication lookup without storing raw keys |
| `loginAttempts` | Identity/IP plus attempt timestamp | Throttling/audit queries; retention policy required |

Field names must be confirmed against deployed BSON before creation. Prefer explicit normalized fields over locale-dependent case-insensitive behavior when uniqueness matters.

## Canonical Short-Link Migration

1. Inventory standalone links and legacy YouTube links embedded in `youtubeContent` and `ytChannels`.
2. Normalize codes and report cross-collection conflicts.
3. Choose conflict winners manually or generate replacement codes with redirects where needed.
4. Create canonical standalone records containing internal references and full validated destinations.
5. Seed aggregate click totals without double counting.
6. Deploy canonical-only resolution and management. Embedded YouTube links are not a runtime fallback.
7. Create the unique normalized-code index.
8. Switch all management writes to the canonical collection.
9. Observe failed legacy-code requests and reconcile any canonical records that were not created.
10. Remove or archive embedded YouTube fields only after backup, backfill verification, and rollback approval.

## Cross-Collection Write Constraint

Azure Cosmos DB for MongoDB RU does not support transactions spanning the `youtubeContent` and `ytChannels` collections. AssignChannel therefore performs additive, idempotent writes sequentially: validate the exact `VideoRef`, update the content aggregate, then add the video to the target channel if absent. A failure response identifies whether the content or channel write completed so operators can reconcile the second collection safely. Do not replace this flow with an assumed cross-collection transaction.

## QuickLinks Global Slug Rollout

The approved `quicklinks_url.unique` entry creates `ux_quicklinks_url_ci` on `quickLinks.url`. The constraint is global across channel owners; it is not a per-channel uniqueness rule. Runtime normalization is `QuickLinks.NormalizeUrl`: trim whitespace, trim surrounding slashes, then lowercase invariant.

Before applying the index:

1. Audit all existing `quickLinks.url` values using that exact normalization, including records owned by different channels.
2. Resolve every normalized duplicate deterministically and record the affected IDs; include empty or malformed values in the cleanup report.
3. Run the authenticated Mongo index audit and approve `quicklinks_url.unique` together with any other approved keys.
4. Apply during the maintenance window, then repeat the audit and capture URL lookup explain evidence.

The manifest and runtime service define the approved operation but do not prove production deployment. A production rollout is complete only when the duplicate audit, pre/post audit output, apply output, and explain evidence are recorded for that environment.

## Custom-Form Response Migration

Current source status:

- Separate `customFormResponses` collection exists (`DbCollections.CustomFormResponses`, `CustomFormResponseDocument`).
- Dual-write is implemented in `FormsService.AddResponseAsync` (upsert into response collection, then legacy embedded compatibility write).
- Dual-write is implemented in `FormsService.AddResponseAsync` (upsert into response collection, then legacy embedded compatibility write).
- Backfill and reconciliation operations are implemented in `FormsService.BackfillEmbeddedResponsesAsync` and `FormsService.ReconcileCountsAsync`, exposed in BackOffice at `POST /api/customforms/responses/backfill` and `GET /api/customforms/{id}/responses/reconcile`.
- BackOffice response reads and counts are now collection-authoritative through `GET /api/customforms/{id}/responses`; response IDs are de-duplicated by the repository key and ordered by submission time. Embedded responses remain only as migration input.

1. Add a `customFormResponses` collection keyed by form ID and submission ID.
2. Dual-write new responses while retaining existing reads.
3. Backfill embedded responses in resumable batches.
4. Verify counts and sampled payload equality.
5. Switch reporting/reads to the response collection.
5. Switch reporting/reads to the response collection. This is now complete for BackOffice and the SPA.
6. Remove embedded arrays only after production backfill/reconciliation evidence and rollback expiry.

Recommended indexes: form ID plus submitted timestamp descending; unique submission ID; optional retention only if product policy permits deletion.

## Query Requirements

Indexes provide no benefit when repositories materialize entire collections. Repository methods must:

- Apply filters in MongoDB.
- Project only required fields.
- Sort before materialization.
- Enforce bounded limits.
- Use atomic increment/update operators for counters and state transitions.
- Use optimistic concurrency or conditional updates where lost updates matter.

## Operational Monitoring

Track slow queries, collection/document growth, index size, replication/backup health, connection saturation, duplicate-key errors, and failed backfill batches. Do not log connection strings or sensitive document contents.