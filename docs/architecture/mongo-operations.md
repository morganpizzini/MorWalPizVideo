# MongoDB Operations

## Status

MongoDB is owned and operated by the project owner. No Mongo index definitions or migration runner are currently found in source. The operations below are required design work, not evidence that indexes already exist.

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

1. Inventory standalone links and links embedded in `youtubeContent` and `ytChannels`.
2. Normalize codes and report cross-collection conflicts.
3. Choose conflict winners manually or generate replacement codes with redirects where needed.
4. Create canonical standalone records containing internal references and full validated destinations.
5. Seed aggregate click totals without double counting.
6. Deploy dual-read resolution with canonical-first behavior.
7. Create the unique normalized-code index.
8. Switch all management writes to the canonical collection.
9. Observe legacy-read telemetry.
10. Remove embedded links only after backup and compatibility-window completion.

## Custom-Form Response Migration

1. Add a `customFormResponses` collection keyed by form ID and submission ID.
2. Dual-write new responses while retaining existing reads.
3. Backfill embedded responses in resumable batches.
4. Verify counts and sampled payload equality.
5. Switch reporting/reads to the response collection.
6. Remove embedded arrays only after rollback expiry.

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