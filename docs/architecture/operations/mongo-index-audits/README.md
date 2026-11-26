# Mongo Index Audit Artifacts (Phase 4)

This is the canonical operator guide for the source-owned Mongo index operation,
aligned with ADR-008's retained data-safety rules and the non-blocking startup
policy in ADR-029. The runtime
`MongoIndexOperationsService.Manifest` and the operational manifest are the source
of truth. This guide intentionally does not create a separate index inventory.

BackOffice attempts every executable manifest index at startup in Mongo mode,
sequentially and independently. It audits after each attempt and logs verified,
failed, and unverified outcomes without blocking application startup, readiness,
or traffic. ServerAPI and ShortLinks do not create or verify indexes at startup.
Startup never removes/replaces an index or cleans/backfills documents. A failed
or successful startup is not proof of actual index state; use the audit endpoint.
FreezerAnalysis's separate application database is outside this policy.

## Required evidence per rollout

1. A verified backup or snapshot.
2. Duplicate and malformed-data audit reports before unique index creation.
3. Authenticated index pre-audit output.
4. The approved request body and apply response.
5. Post-apply audit output and representative explain-plan evidence.
6. If removing a legacy index, the approved removal request and removal response.
7. A record of conflicts, affected IDs, backfills, and any failed or retried steps.

## Recommended operator sequence

1. Take and verify a restorable backup or snapshot.
2. Run duplicate and malformed-data audits, then resolve conflicts deterministically.
3. Resolve all unique-index conflicts before applying any unique index.
4. Run the authenticated `GET /api/mongoindexes/audit` endpoint and retain its output.
5. Apply the approved request below during the maintenance window.
6. Verify the replacement index specification, re-run the audit, and capture explain evidence.
7. Submit the separate removal request only after the replacement verification succeeds.
8. Re-run the audit after removal and retain both audit responses.
9. Explain the evidence in the environment-specific rollout record; source review and historical samples are not deployment proof.

## Apply contract

The endpoint is `POST /api/mongoindexes/apply`. `ApplicationControllerBase`
provides the `/api/[controller]` base route, so the exact path is
`/api/mongoindexes/apply`.

Authentication requires a valid configured API key in the `X-API-Key` header.
Bearer or cookie authentication alone is not sufficient. Replace the placeholder
below with the configured key; never document or commit a real key.

`approvalToken` must exactly equal `apply-approved-indexes`. It is a procedural
approval guard, not a secret. `approvedKeys` must be non-empty and may contain
only current manifest keys. The following example illustrates an explicit
operator-selected subset; it is not a complete manifest or startup result:

```bash
curl -X POST "https://<BACKOFFICE_HOST>/api/mongoindexes/apply" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: <YOUR_API_KEY>" \
  --data-raw '{
    "approvalToken": "apply-approved-indexes",
    "approvedKeys": [
      "shortlinks.code.unique",
      "shortlinkrequestevents.shortlinkid_timestamp_desc",
      "shortlinkrequestevents.expiresat.ttl",
      "customformresponses.formid_submittedat_desc",
      "customformresponses.formid_responseid.unique",
      "youtubecontent_isprivate_creation_desc",
      "youtubecontent_isprivate_latestpublished_creation_desc",
      "pages_url.unique",
      "navigation_channel.unique",
      "compilations_url.unique",
      "quicklinks_url.unique",
      "customforms_active_url",
      "calendarevents_creation_desc",
      "socialassets.channelid_idempotencykey.unique",
      "publicaccounts.normalizedusername.unique",
      "publicaccounts.normalizedemail.unique",
      "publicaccountsessions.expiresat.ttl"
    ]
  }'
```

Do not add keys from older documents, guessed names, or a different environment.
Use the authenticated audit response and executable manifest for current keys.

## Prerequisites and response handling

Unique indexes require conflict resolution first:

- `shortLinks.code` must have no duplicate or malformed normalized codes.
- `shortLinkRequestEvents` must contain only the approved event shape; verify
  `shortLinkId`, `timestamp`, and `expiresAt` are present before applying the
  query and TTL indexes.
- `customFormResponses` must have no duplicate `(formId, responseId)` pairs.
- `pages.url` must have no duplicate normalized URLs across channel owners; the
  current authority is the global unique `pages_url.unique` index named
  `ux_pages_url_ci`.
- `compilations.url` must have no duplicate normalized public URLs.
- `quickLinks.url` must have no duplicate normalized URLs across channel owners; normalize by trimming whitespace, trimming surrounding slashes, then lowercasing invariant.

Before applying `youtubecontent_isprivate_latestpublished_creation_desc`, backfill
and verify that every existing `youtubeContent.latestPublishedAt` is populated
with the intended value. Record the backfill evidence and confirm the field is
ready for indexed ordering.

The operator operation processes approved keys sequentially. Each result reports
either `created` or `skipped_existing`. If a later key fails, earlier indexes may
already have been applied; re-audit the environment before retrying. Startup
processes manifest entries separately and logs failures; index failures do not
block service startup.

An index is `skipped_existing` only when its key pattern and declared uniqueness,
partial filter, collation and expiration options match the manifest. A same-name
index with a different definition is not treated as a match and must be reconciled
through an explicit reviewed operation.

## Legacy removal contract

The current desired page URL state is `pages_url.unique` on collection `pages`,
with the unique `{ "url": 1 }` index `ux_pages_url_ci`. The short-link state is
the collation-free unique `{ "code": 1 }` index `ux_shortlinks_code`; the key
`shortlinks_code_collation` removes the old `ux_shortlinks_code_ci` index only
after the replacement has been applied and verified. Legacy removal keys are
not valid for the apply request.

Removal uses `POST /api/mongoindexes/remove` with the same configured API key and
the same procedural approval token as apply. `approvedRemovalKeys` must be
non-empty and may contain only source-controlled removal keys:

```bash
curl -X POST "https://<BACKOFFICE_HOST>/api/mongoindexes/remove" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: <YOUR_API_KEY>" \
  --data-raw '{
    "approvalToken": "apply-approved-indexes",
    "approvedRemovalKeys": ["shortlinks_code_collation"]
  }'
```

Before dropping a legacy index, the service verifies that its replacement exists
and is unique with the expected key pattern. If the replacement is absent
or incorrect, removal is rejected and the legacy index is retained. A successful
result reports `removed`; a missing legacy index reports `skipped_absent` and is
idempotent. Unknown removal keys fail with HTTP 400. Mongo index conflicts and
invalid operation errors are returned as clear bad-request responses.

BackOffice attempts all shared-platform manifest indexes at startup. Use the
reviewed audit/apply operations here to diagnose and remediate failures. Keep
backups, rollback notes, audit/apply responses, and explain evidence with each
environment-specific rollout record. A missing unique index can permit
duplicates while the service continues; a newly created TTL index can expire
existing records according to its declared retention.

Historical sample artifacts are not production evidence. Never record raw API keys,
connection strings, or other connection details in documentation or operational
artifacts.

## Committed Phase 4 baseline sample

The current page URL authority is global and unique: `pages_url.unique` on
`pages.url`, named `ux_pages_url_ci`. The older `pages_url` / `ix_pages_url`
pair is represented only by the source-controlled legacy removal entry and must
not be used in an apply request or treated as production deployment evidence.

- `phase4-2026-08-03-sample-audit-output.json`
- `phase4-2026-08-03-sample-apply-output.json`
- `phase4-2026-08-03-explain-evidence.md`
- `phase4-2026-08-03-verification-bundle.md`
- `phase4-2026-08-16-sample-remove-output.json`
