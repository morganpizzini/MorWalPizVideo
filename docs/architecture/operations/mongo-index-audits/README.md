# Mongo Index Audit Artifacts (Phase 4)

These artifacts are source-owned operational records aligned with ADR-008.

## Required artifacts per rollout

1. Duplicate and malformed-data audit report before unique index creation.
2. Index pre-check (`GET /api/mongoindexes/audit`) output.
3. Approved index key list used for apply.
4. Apply output (`POST /api/mongoindexes/apply`) result.
5. Representative explain-plan evidence for touched query paths.

## Operator flow

1. Run data audits and resolve conflicts.
2. Call `GET /api/mongoindexes/audit` (optional `keys` filter).
3. Prepare approved keys list from `mongo-index-manifest.phase4.json`.
4. Call `POST /api/mongoindexes/apply` with approval token and approved keys.
5. Re-run audit and capture final evidence.

## Apply contract

Endpoint: `POST /api/mongoindexes/apply`

Body:

```json
{
  "approvalToken": "apply-approved-indexes",
  "approvedKeys": [
    "shortlinks.code.unique",
    "customformresponses.formid_responseid.unique",
    "quicklinks_url.unique"
  ]
}
```

Notes:

- Apply is idempotent (`skipped_existing` for already present indexes).
- Indexes are never created at startup.
- Keep backups and rollback notes with each apply record.
- `compilations_url.unique` is named `ux_compilations_url_ci` in the authoritative phase 4 manifest and enforces global uniqueness for normalized public compilation URLs.
- `quicklinks_url.unique` is named `ux_quicklinks_url_ci` in the authoritative phase 4 manifest and enforces global uniqueness for normalized QuickLinks slugs across channel owners. Audit duplicates after trim, surrounding-slash removal, and lowercase normalization before applying it.
- `shortlinks.code.unique` remains a global uniqueness requirement for public short-link resolution. Manifest inclusion is not deployment evidence; production status for any unique index requires a duplicate audit, audit/apply results, and explain evidence from that environment.

The committed 2026-08-03 sample outputs are historical local verification artifacts and do not establish that `quicklinks_url.unique` has been audited or deployed. Capture a new environment-specific audit and apply record when rolling out the QuickLinks index.

## Committed Phase 4 baseline sample

- `phase4-2026-08-03-sample-audit-output.json`
- `phase4-2026-08-03-sample-apply-output.json`
- `phase4-2026-08-03-explain-evidence.md`
- `phase4-2026-08-03-verification-bundle.md`
