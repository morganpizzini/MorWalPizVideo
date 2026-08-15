# Mongo Index Audit Artifacts (Phase 4)

This is the canonical operator guide for the source-owned Mongo index operation,
aligned with ADR-008. The runtime `MongoIndexOperationsService.Manifest` and the
operational manifest are the source of truth. This guide intentionally does not
create a separate index inventory.

## Required evidence per rollout

1. A verified backup or snapshot.
2. Duplicate and malformed-data audit reports before unique index creation.
3. Authenticated index pre-audit output.
4. The approved request body and apply response.
5. Post-apply audit output and representative explain-plan evidence.
6. A record of conflicts, affected IDs, backfills, and any failed or retried steps.

## Recommended operator sequence

1. Take and verify a restorable backup or snapshot.
2. Run duplicate and malformed-data audits, then resolve conflicts deterministically.
3. Resolve all unique-index conflicts before applying any unique index.
4. Run the authenticated `GET /api/mongoindexes/audit` endpoint and retain its output.
5. Apply the approved request below during the maintenance window.
6. Re-run the audit, compare actual index specifications, and capture explain evidence.
7. Explain the evidence in the environment-specific rollout record; source review and historical samples are not deployment proof.

## Apply contract

The endpoint is `POST /api/mongoindexes/apply`. `ApplicationControllerBase`
provides the `/api/[controller]` base route, so the exact path is
`/api/mongoindexes/apply`.

Authentication requires a valid configured API key in the `X-API-Key` header.
Bearer or cookie authentication alone is not sufficient. Replace the placeholder
below with the configured key; never document or commit a real key.

`approvalToken` must exactly equal `apply-approved-indexes`. It is a procedural
approval guard, not a secret. `approvedKeys` must be non-empty and may contain
only the current manifest keys shown in this complete request:

```bash
curl -X POST "https://<BACKOFFICE_HOST>/api/mongoindexes/apply" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: <YOUR_API_KEY>" \
  --data-raw '{
    "approvalToken": "apply-approved-indexes",
    "approvedKeys": [
      "shortlinks.code.unique",
      "customformresponses.formid_submittedat_desc",
      "customformresponses.formid_responseid.unique",
      "youtubecontent_isprivate_creation_desc",
      "youtubecontent_isprivate_latestpublished_creation_desc",
      "pages_url",
      "compilations_url.unique",
      "quicklinks_url.unique",
      "customforms_active_url",
      "calendarevents_creation_desc"
    ]
  }'
```

Do not add keys from older documents, guessed names, or a different environment.
Keep the runtime manifest and operational manifest aligned; this complete request
is the only index list duplicated here for copy-paste operation.

## Prerequisites and response handling

Unique indexes require conflict resolution first:

- `shortLinks.code` must have no duplicate or malformed normalized codes.
- `customFormResponses` must have no duplicate `(formId, responseId)` pairs.
- `compilations.url` must have no duplicate normalized public URLs.
- `quickLinks.url` must have no duplicate normalized URLs across channel owners; normalize by trimming whitespace, trimming surrounding slashes, then lowercasing invariant.

Before applying `youtubecontent_isprivate_latestpublished_creation_desc`, backfill
and verify that every existing `youtubeContent.latestPublishedAt` is populated
with the intended value. Record the backfill evidence and confirm the field is
ready for indexed ordering.

The operation processes approved keys sequentially. Each result reports either
`created` or `skipped_existing`. If a later key fails, earlier indexes may already
have been applied; re-audit the environment before retrying.

An existing index with the manifest name is reported as `skipped_existing` without
fully reconciling its key pattern or options. Compare the actual deployed index
specification separately; same-name existence is not proof that the specification
matches the manifest.

Indexes are never created at startup. Keep backups, rollback notes, audit/apply
responses, and explain evidence with each environment-specific rollout record.

Historical sample artifacts are not production evidence. Never record raw API keys,
connection strings, or other connection details in documentation or operational
artifacts.

## Committed Phase 4 baseline sample

- `phase4-2026-08-03-sample-audit-output.json`
- `phase4-2026-08-03-sample-apply-output.json`
- `phase4-2026-08-03-explain-evidence.md`
- `phase4-2026-08-03-verification-bundle.md`
