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
