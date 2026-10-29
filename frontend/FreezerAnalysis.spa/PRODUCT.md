# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

Confirmed by the implementation handoff: .NET 10, React 19, TypeScript, MongoDB, and the existing Yarn workspace.

## Users

Primary user: an authenticated IPSC competitor preparing and shooting a match. The user needs the pre-match workflow on a phone, including when connectivity is unreliable.

## Product Purpose

FreezerAnalysis estimates stage hit factors from previously available match results, freezes a reproducible pre-start plan, and supports field updates and post-match verification. Success means reducing manual preparation while keeping the estimate, its evidence, and later corrections auditable.

## Positioning

The product combines an explicitly dated informant registry, deterministic stage estimates, a frozen input snapshot, and offline field use. Later results must not leak into an earlier estimate.

## Operating Context

The user imports match results and roster data, resolves uncertain shooter identities, reviews an estimate, accepts editable strategy/reconnaissance text, freezes and downloads a field card, records turn and timestamped updates, then imports final results for verification and optional parameter adoption.

## Capabilities and Constraints

- Multiple PublicAccount users may reuse published competition facts; profiles and analysis decisions are private.
- Frozen plans are immutable. Field changes append separate events.
- Calculations are C# and deterministic. AI is disabled until provider and data-processing approval; manual editing remains supported.
- Italian is the initial interface language; display dates and numbers use `Europe/Rome` and `it-IT`.
- Only documented CSV and explicitly recognized PDF report families are supported initially. Unknown PDF layouts must produce diagnostics rather than guessed data.
- The application database is separate from the platform account database.
- Assumptions about missing reference implementation details are recorded in the feature handoff. Original numerical and visual parity remains unconfirmed until user acceptance.

## Brand Commitments

- Product name: FreezerAnalysis.
- IPSC competition terminology and Italian-first copy are required.

## Evidence on Hand

- The user-authored implementation handoff and its cited original architecture brief.
- Two user-supplied result PDFs are retained as local parser acceptance inputs; their contents are personal data and must not be copied into logs, external provider payloads, or synthetic fixtures.
- The original Python implementation, historical archive, and rendered golden artifacts are unavailable in this worktree.

## Product Principles

- Preserve the exact data cutoff and inputs behind every estimate.
- Make uncertain, incomplete, and conflicting data visible before it can affect a published result.
- Keep field tasks brief, usable offline, and safe to reconcile later.
- Treat user approval as authoritative for identity merges, frozen plans, and parameter adoption.

## Accessibility & Inclusion

The responsive field surface must support keyboard and screen-reader use, 200% zoom, visible focus, non-color status cues, and primary touch targets of at least 44 CSS pixels.
