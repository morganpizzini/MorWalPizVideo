# Architecture Decisions

Architecture Decision Records live in `docs/architecture/adr`. Accepted ADRs govern future implementation until explicitly superseded.

| ADR | Decision | Status |
|---|---|---|
| [ADR-001](adr/ADR-001-management-public-boundary.md) | Separate management and public API boundaries | Accepted |
| [ADR-002](adr/ADR-002-explicit-authentication.md) | Explicit host-specific authentication and authorization | Accepted |
| [ADR-003](adr/ADR-003-versioned-dto-apis.md) | Versioned DTO-based JSON APIs | Accepted |
| [ADR-004](adr/ADR-004-canonical-short-links.md) | Canonical short-link aggregate and focused redirect service | Accepted |
| [ADR-005](adr/ADR-005-permanent-free-artifacts.md) | Permanent-free artifacts and anonymous acquisitions | Accepted |
| [ADR-006](adr/ADR-006-blob-exposure.md) | Public previews and private originals | Accepted |
| [ADR-007](adr/ADR-007-development-scenarios.md) | Deterministic scenario-based local development | Accepted |
| [ADR-008](adr/ADR-008-mongo-index-migrations.md) | Source-owned Mongo index and migration operations | Accepted |
| [ADR-009](adr/ADR-009-cache-invalidation.md) | Authenticated tag-based cache invalidation | Accepted |
| [ADR-010](adr/ADR-010-domain-and-cors.md) | Canonical domains and least-privilege CORS | Accepted |
| [ADR-011](adr/ADR-011-email-boundary.md) | Provider-neutral future transactional email boundary | Accepted |
| [ADR-012](adr/ADR-012-desktop-composition.md) | Incremental Generic Host and MVVM direction for WPF | Accepted |

## ADR Process

Create an ADR when a change affects service ownership, dependency direction, data compatibility, authentication, API contracts, deployment topology, infrastructure policy, or a cross-application convention.

An ADR contains context, decision, alternatives, consequences, migration, rollback, and validation. Do not edit the substance of an accepted ADR after implementation diverges; add a superseding ADR and update this index.