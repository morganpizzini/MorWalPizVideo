---
name: handoff-cleanup
description: Consolidate completed feature handoffs into durable repository documentation and retire only safely completed planning files. Use for handoff cleanup, completed-plan consolidation, or retiring feature handoffs.
---

# Handoff cleanup

Clean handoffs by proving that their durable information has an owning
continuing record. Do not treat a handoff filename or an old "implemented"
claim as a cleanup criterion.

## 1. Classify

Inspect only `docs/feature-handoffs/` Markdown plans. Exclude `README.md`,
`feature-handoff-template.md`, preserved fixtures, and the former-handoff map.

Classify each candidate before editing:

- **Remove**: implementation is complete and the handoff's decisions, actual
  validation, compatibility constraints, and residual risks are consolidated.
- **Keep**: draft, ready for review, proposal-only, incomplete implementation,
  or a handoff whose pending gate or unresolved work is still useful as intake.
- **Investigate**: status is ambiguous or implementation evidence conflicts
  with the handoff.

An open deployment or browser verification gate does not make implementation
incomplete. If it remains operationally important, consolidate it into the
continuing record and state that it is a pending gate; do not erase or close it.

## 2. Find owners

Start with [the documentation index](../../../docs/README.md),
[the architecture guide](../../../docs/architecture/README.md), and
`docs/feature-handoffs/README.md`.

For each remove candidate, identify the single narrowest owning continuation:

- feature behavior or delivery status in `docs/features/`
- architectural boundary or accepted decision in `docs/architecture/` or an ADR
- environment-specific procedure in `docs/architecture/operations/`
- standing implementation rules in an existing root-level documentation record
- source and executable tests when documentation intentionally does not own a
  detail

Never create a second feature library or duplicate the same decision across
unrelated records.

## 3. Verify before consolidation

Compare the handoff's claims with current source, manifests, workflows, and
focused tests. Source and executable evidence take precedence over historical
documentation.

For every handoff, verify at minimum:

- requested outcome and implemented behavior
- final files and ownership
- decisions and compatibility constraints
- actual validation results and the repository state that produced them
- pending rollout, migration, browser, monitoring, or recovery gates
- security, authorization, cache, configuration, secret, and data-retention
  consequences

Do not copy proposed behavior into a delivered section. Mark proposals as
proposed and preserve unresolved work as unresolved.

## 4. Consolidate

Update the continuing record rather than replacing it with a full handoff:

- add or correct durable decisions and present behavior
- record delivered evidence and its date where useful
- keep pending gates explicit and non-promissory
- update links that point at the retired handoff
- remove the former filename from inbound references
- add a former-filename entry to the handoff migration map only when the entry
  helps discovery

For root-level working documents, preserve their existing purpose and only move
information that is durable. Do not delete source examples or test fixtures.

## 5. Retire and prove

Retire a handoff only after its owner and migration references are complete.
Use the repository diff to verify that the removal is paired with the intended
documentation updates and no source, tests, fixtures, or pending plans were
deleted accidentally.

Report a compact inventory:

- removed handoffs and their continuing records
- kept handoffs and the reason
- ambiguous handoffs and missing evidence
- documentation and links changed
- validation run or explicitly not rerun

Cleanup is documentation-only unless the user separately authorizes source
changes. Historical test results are not fresh verification.
