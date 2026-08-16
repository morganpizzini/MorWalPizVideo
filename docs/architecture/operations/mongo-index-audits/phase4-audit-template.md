# Phase 4 Mongo Audit Record Template

Date:
Environment:
Operator:

## Pre-checks

- Backup verified: yes/no
- Duplicate audit completed: yes/no
- Malformed data audit completed: yes/no

## Approved key set

- 

## Audit before apply

Attach output from:

- `GET /api/mongoindexes/audit`

## Apply output

Attach output from:

- `POST /api/mongoindexes/apply`

## Legacy removal output

Complete only after the replacement index has been verified and the post-apply audit retained.

- Approved removal keys:
- Attach output from `POST /api/mongoindexes/remove`:
- Removal result: `removed` / `skipped_absent`
- Post-removal audit attached: yes/no

## Query evidence

Attach representative explain-plan evidence for:

- `matches` paging/filter query
- `customForms` active/by-url query
- `customFormResponses` by form query
- `pages`, `compilations`, and `quickLinks` URL lookup queries

## Reconciliation evidence

- Backfill batches run:
- Final continuation token:
- Reconcile results sampled:

## Notes

- 
