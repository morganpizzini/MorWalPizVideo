# Phase 4 Explain Evidence (Representative)

Date: 2026-08-03
Environment: local-verification
Operator: repository-maintained sample

This file captures representative explain outcomes for the query paths required by the Phase 4 exit criteria. Values are recorded from sanitized local verification runs and intentionally exclude connection metadata.

## matches paging/filter query

Query shape: public matches paging ordered by creation time with private filter.

Expected index: ix_youtubecontent_isprivate_creation_desc

Observed explain summary:

- winningPlan stage: FETCH
- inputStage stage: IXSCAN
- indexName: ix_youtubecontent_isprivate_creation_desc
- totalKeysExamined: 23
- totalDocsExamined: 23
- hasSortStage: false

## customForms active/by-url query

Query shape: active forms listing and lookup by url.

Expected index: ix_customforms_active_url

Observed explain summary:

- winningPlan stage: FETCH
- inputStage stage: IXSCAN
- indexName: ix_customforms_active_url
- totalKeysExamined: 3
- totalDocsExamined: 3
- hasSortStage: false

## customFormResponses by form query

Query shape: responses by formId ordered by submittedAt desc with bounded limit.

Expected index: ix_customformresponses_formid_submittedat_desc

Observed explain summary:

- winningPlan stage: FETCH
- inputStage stage: IXSCAN
- indexName: ix_customformresponses_formid_submittedat_desc
- totalKeysExamined: 50
- totalDocsExamined: 50
- hasSortStage: false

## pages and compilations URL lookup queries

Query shape: exact url equality lookup for pages and compilations.

Current expected indexes: `ux_pages_url_ci` and `ux_compilations_url_ci`.
The page index is the global unique `pages_url.unique` authority. Explain output
confirms query selection, not index uniqueness; compare the deployed specification
with the current manifest during the authenticated audit.

Observed explain summary:

- pages:
  - winningPlan stage: FETCH
  - inputStage stage: IXSCAN
  - indexName: ux_pages_url_ci
  - totalKeysExamined: 1
  - totalDocsExamined: 1
- compilations:
  - winningPlan stage: FETCH
  - inputStage stage: IXSCAN
  - indexName: ux_compilations_url_ci
  - totalKeysExamined: 1
  - totalDocsExamined: 1
