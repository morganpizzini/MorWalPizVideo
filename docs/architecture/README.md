# MorWalPizVideo Architecture Guide

**Status:** Official architecture reference  
**Baseline:** Source reviewed through 2026-08-01  
**System center:** `MorWalPizVideo.BackOffice`

This directory is the long-term architectural reference for MorWalPizVideo. It records both the architecture that exists in source and the approved target direction. Future work should consult this guide before performing a repository-wide analysis.

Current source, project manifests, deployment workflows, and executable tests remain authoritative when they conflict with this guide. Update the relevant document and ADR whenever an implementation changes an architectural boundary.

## Scope

Included:

- ASP.NET services, shared .NET libraries, tests, and Aspire orchestration.
- BackOffice SPA, public application, shop client, Shooting ITA, and shared frontend packages.
- VideoImporter and InsightScanner Windows applications.
- MongoDB, SQLite, Blob Storage, caching, jobs, authentication, configuration, CI/CD, and external integrations.

Excluded:

- `frontend/TelePrompter`.
- `frontend/stage-designer`.
- `MorWalPizVideo.Operations`, which has no current source project.

## Reading Order

1. [Overview](overview.md)
2. [Solution Structure](solution-structure.md)
3. [Project Responsibilities](project-responsibilities.md)
4. [Backend Architecture](backend.md)
5. [Frontend Architecture](frontend.md)
6. [Windows Applications](windows-apps.md)
7. [Domain Model](domain-model.md)
8. [API Design](api-design.md)
9. [Infrastructure](infrastructure.md)
10. [Security](security.md)
11. [Deployment](deployment.md)
12. [Development](development.md)
13. [MongoDB Operations](mongo-operations.md)
14. [Architecture Decisions](architecture-decisions.md)
15. [Technical Debt](technical-debt.md)
16. [Future Improvements](future-improvements.md)
17. [Refactoring Roadmap](refactoring-roadmap.md)
18. [BackOffice Admin Dashboard](admin-dashboard.md)

## Architectural Baseline

- BackOffice is the management plane and owns administrative business operations and writes.
- ServerAPI is the public interface and owns public reads and explicitly approved public interactions. It must not expose administrative writes for core content entities.
- ShortLinks is a focused, anonymous branded redirect and visit-tracking service.
- Shared behavior belongs in existing Models, Domain, Contracts, MvcHelpers, ServiceDefaults, or frontend packages according to their established responsibilities. API projects must not reference each other.
- `morwalpiz.com` is the canonical public domain. The public frontend is hosted by Aruba and calls `https://morwalpiz-serverapi.azurewebsites.net` directly.
- `https://morwalpiz-admin-spa.azurewebsites.net` is the administrative SPA origin. `https://shorts.morwalpiz.com` is the branded redirect host.
- Digital artifacts are permanently free. Public previews may be anonymous; originals belong in private Blob Storage and are released through a server-controlled free-acquisition flow.
- Customer accounts and analytics are deferred, but identifiers and contracts must permit later attachment without redesigning products or acquisitions.
- JSON APIs will adopt URL-segment versioning beginning with `/api/v1`; branded redirect URLs remain unversioned.
- Development enables only `EnableDev` and `EnableSwagger`. Local CORS is permissive; deployed CORS is explicit and least-privilege.

## Document Status Labels

- **Current:** verified in source.
- **Target:** approved architecture not necessarily implemented.
- **Historical:** retained context that is not authoritative.
- **Unknown:** requires deployed-environment or operational verification.

## Superseded And Supporting Documents

The following remain useful history but are not the whole-system authority:

- `docs/morwalpiz-shop-client-implementation-plan.md`
- `docs/GITHUB_PRODUCTION_DEPLOYMENT.md`
- `docs/SHOOTING_ITA_PHASE4_ADVANCED_FEATURES.md`
- `implementation_plan.md`
- `LINKTREE_IMPLEMENTATION.md` (QuickLinks public route and API)
- `memory-bank/systemPatterns.md`
- `memory-bank/techContext.md`

Supporting security, health, feature, and setup documents should be read with this guide:

- `docs/AUTHENTICATION_SECURITY_IMPROVEMENTS.md`
- `MorWalPizVideo.BackOffice/HEALTH_CHECKS.md`
- `MorWalPizVideo.ServerAPI/KEYVAULT_SETUP.md`
- `specs/001-cache-invalidation-fixes/`
- `specs/002-pepperbox-clone/`

## Maintenance Rule

Every architectural change must update:

1. The affected architecture document.
2. The relevant ADR, or add a new ADR when the decision is significant.
3. The technical-debt and roadmap entries when risk or sequencing changes.
4. Diagrams and project inventories when runtime topology changes.

Do not place credentials, connection strings, API keys, or deployed secret values in this documentation.