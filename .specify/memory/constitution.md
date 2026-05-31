<!--
SYNC IMPACT REPORT
==================
Version change: (uninitialized template) → 1.0.0
Bump rationale: MAJOR — initial ratification of the project constitution for
an existing codebase that was developed prior to Spec Kit adoption. All
principles below are newly established rather than amended.

Modified principles: N/A (initial adoption)
Added sections:
  - Core Principles (7 principles)
  - Technology Stack & Constraints
  - Development Workflow & Quality Gates
  - Governance
Removed sections: N/A

Templates requiring updates:
  - ✅ .specify/templates/plan-template.md — Constitution Check gate now binds
       to the seven principles below; no structural edit required (gate text
       is intentionally generic and references this file).
  - ✅ .specify/templates/spec-template.md — Compatible as-is; no constitution-
       driven mandatory sections changed.
  - ✅ .specify/templates/tasks-template.md — Compatible as-is; task
       categorization already covers contracts, integration tests, polish.
  - ✅ .github/copilot-instructions.md — Existing Italian style guidance is
       preserved and treated as the canonical source for code-style rules
       referenced by Principle I.
  - ⚠ memory-bank/activeContext.md — Should be refreshed at the start of any
       new Spec Kit feature so the constitution gate has current context.

Follow-up TODOs:
  - None. Ratification date set to today (project pre-existed Spec Kit, but
     this is the first formal governance document).
-->

# MorWalPizVideo Constitution

## Core Principles

### I. Simplicity & Readability First
Code MUST be self-explanatory through descriptive naming of variables, methods,
and classes; comments are added ONLY when intent is non-obvious. Superfluous
logic, speculative abstractions, and premature generalization are prohibited
(YAGNI). Language- and framework-idiomatic style MUST be followed (see
`.github/copilot-instructions.md` for the canonical style rules).
**Rationale**: This is a multi-app solution maintained primarily by a single
developer with AI assistance; cognitive load is the dominant cost, so clarity
beats cleverness.

### II. Layered Architecture with DTO Boundaries (NON-NEGOTIABLE)
Domain entities MUST NOT cross HTTP, UI, or process boundaries directly.
- **.NET Web APIs**: Controllers MUST accept `BaseRequest<T>` / `BaseRequestId<T>`
  request contracts and return DTOs. Data access MUST go through the
  `IRepository<T>` / `DataService` layer; controllers MUST NOT call
  `MongoDB.Driver` directly. Controllers stay RESTful and single-responsibility.
- **WPF (`MorWalPiz.VideoImporter`)**: MVVM is mandatory. ViewModels stay slim,
  expose reactive properties via `INotifyPropertyChanged`, and contain no
  UI-framework calls. Complex bindings are avoided.
- **React clients**: Network and persistence concerns live in loaders, actions,
  hooks, or shared services — never inline in render.
**Rationale**: Preserves API contract stability, enables testing in isolation,
and keeps storage refactors local.

### III. Shared Contracts Are the Single Source of Truth
Cross-app types and API calls MUST live in the shared packages and be consumed
by reference, never duplicated:
- **.NET**: `MorWalPiz.Contracts` (DTOs) and `MorWalPizVideo.Models` (domain).
- **Frontend monorepo**: `@morwalpizvideo/models`, `@morwalpizvideo/services`,
  `@morwalpizvideo/layout` under `frontend/fe-packages/`.
New endpoints, request/response shapes, and shared UI primitives MUST be added
to the appropriate shared package before being consumed by any app. Drift
between an app-local type and a shared type is a defect.
**Rationale**: Multiple frontends (`back-office-spa`, `morwalpizvideo.client`,
`morwalpiz-shop.client`, `shooting-ita-frontend`) and multiple .NET hosts
(`BackOffice`, `ServerAPI`, `ShortLinks`, `Operations`, `VideoImporter`) make
duplication a recurring source of regressions.

### IV. Feature-First, Typed Frontend
React clients MUST use functional components with explicit TypeScript types
(interfaces) for every prop and state shape. React Router v7 routes follow the
established convention: each route directory contains `Component.tsx`,
`loader.ts`, `action.ts`, and `index.ts` as applicable. Server state is fetched
via loaders; local state is reserved for UI concerns. Files are organized
feature-first, not type-first.
**Rationale**: Matches the existing repository layout and keeps routes
self-contained for incremental work.

### V. Test-Backed Behavior Verification
Behavior changes MUST be covered by tests at the appropriate level:
- **.NET**: xUnit + SpecFlow under `MorWalPizVideo.BackOffice.Tests`; controller
  contracts and cross-service integration MUST have integration tests; pure
  logic in services MAY use unit tests.
- **Frontend**: Loaders, actions, and non-trivial hooks/helpers SHOULD have
  tests; pure presentational components MAY be skipped.
- A PR that changes observable behavior without a corresponding test is
  rejected unless explicitly justified in the PR description.
- Repository-wide coverage SHOULD trend toward the 85% target noted in
  `memory-bank/progress.md`; PRs MUST NOT regress overall coverage.
**Rationale**: Tests are the only durable specification for the pre-Spec-Kit
features of this codebase.

### VI. Secure by Default
- All user-facing APIs MUST authenticate via JWT; admin and external-integration
  endpoints additionally require API-key authentication with rate limiting
  (`ApiKeyRateLimitingService`).
- Secrets (MongoDB connection strings, YouTube API keys, Azure Translator keys,
  VAPID keys, Discord/Telegram tokens) MUST come from `appsettings.*.json`
  outside source control, environment variables, or Azure Key Vault. Secrets
  MUST NOT be committed.
- OWASP Top 10 risks (injection, broken auth, SSRF, insecure deserialization,
  XSS in SPA-rendered content) MUST be considered for every endpoint and new
  React route.
- CORS, security headers, and SPA fallback rules defined in the nginx
  configuration MUST be preserved across deployment changes.
**Rationale**: The system handles paid digital products, private competition
data (Shooting ITA), and third-party API quotas — each a real abuse surface.

### VII. Containerized, Cloud-Native Deployment
Every deployable app MUST build to a Docker image via the established
multi-stage pattern (Node builder → nginx for SPAs; .NET SDK → ASP.NET runtime
for APIs). Runtime configuration for SPAs MUST be injected via the
`env-config.js` mechanism, never baked into the bundle. Images are published to
Azure Container Registry and hosted on Azure App Service via the GitHub Actions
CI/CD pipeline. A change is not "done" until its container builds green in CI.
**Rationale**: Local-only changes have repeatedly caused production drift;
container parity removes that class of bug.

## Technology Stack & Constraints

The following stack is fixed and MUST be honored by all new work unless an
amendment to this constitution is ratified:

- **Backend**: .NET 8, ASP.NET Core Web API, Hangfire (recurring jobs),
  MongoDB.Driver.
- **Storage**: MongoDB (document-per-aggregate; indexes on `youtubeId`,
  `category`). Schema changes for existing collections MUST consider live
  documents and provide a migration or backward-compatible read path.
- **Frontend**: React 19, TypeScript, Vite, React Router v7, Bootstrap.
  npm workspaces under `frontend/`.
- **Desktop**: WPF on .NET 8 (Windows-only by design).
- **External services**: YouTube Data API v3 (quota-bounded — calls MUST be
  batched and cached where possible), Azure Translator (cost per character —
  translations MUST be deduplicated and persisted), Discord API, Telegram API,
  Pinterest API, Web Push (VAPID).
- **Cloud**: Azure Container Registry, Azure App Service, Azure Key Vault,
  GitHub Actions.
- **Auth**: JWT for users; API key + rate limiting for service-to-service and
  scraper endpoints.

When designing new persistence, **Azure Cosmos DB best practices**
(see `vscode-userdata:.../azurecosmosdb.instructions.md`) apply only if Cosmos
DB is explicitly adopted; the current default remains MongoDB.

## Development Workflow & Quality Gates

1. **Spec Kit lifecycle**: All new features authored after this constitution's
   ratification MUST flow through `/speckit.specify` → optional
   `/speckit.clarify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`.
   Pre-existing features are back-filled into `specs/` lazily, only when they
   are next materially modified.
2. **Constitution Check**: `plan-template.md` MUST verify each of the seven
   principles before Phase 0 and again after Phase 1. Any violation requires
   an entry in the plan's Complexity Tracking table with explicit justification
   and a rejected simpler alternative.
3. **Branching**: Feature work happens on branches created by the
   `speckit.git.feature` hook; direct commits to `main` are reserved for
   constitution, documentation, and CI fixes.
4. **PR gates** (CI must be green before merge):
   - `dotnet build` succeeds for the full solution.
   - `dotnet test` passes (BackOffice.Tests).
   - `npm run build` succeeds for every affected workspace under `frontend/`.
   - Docker image build succeeds for any app whose `Dockerfile` is in scope.
5. **Memory bank hygiene**: `memory-bank/activeContext.md` and
   `memory-bank/progress.md` MUST be updated as part of any PR that ships a
   user-visible change, so future Spec Kit runs start from accurate context.
6. **Documentation language**: Spec, plan, tasks, and constitution artifacts
   are authored in English. Inline code comments MAY remain in Italian to match
   the existing style guidance in `.github/copilot-instructions.md`.

## Governance

This constitution supersedes ad-hoc conventions and prior informal practices.
Conflicts between this document and any other in-repo guidance (READMEs,
`memory-bank/`, `docs/`, `.github/copilot-instructions.md`) are resolved in
favor of this constitution unless the conflicting document is itself updated
as part of an amendment.

**Amendments** require:
1. A pull request that updates `.specify/memory/constitution.md`.
2. An updated Sync Impact Report at the top of this file describing the
   version bump, affected principles, and templates touched.
3. A version bump using semantic versioning:
   - **MAJOR**: A principle is removed, made non-binding, or replaced with an
     incompatible rule.
   - **MINOR**: A new principle or governance section is added, or an existing
     principle is materially expanded.
   - **PATCH**: Wording clarifications, typo fixes, or non-semantic edits.
4. Propagation: the templates listed in the Sync Impact Report MUST be
   reviewed and, if affected, updated in the same PR.

**Compliance reviews**: Every `/speckit.plan` run executes the Constitution
Check gate. Any violation that ships MUST be recorded in the feature's
`plan.md` Complexity Tracking section; recurring violations of the same
principle are a signal to amend rather than to keep granting exceptions.

**Runtime guidance**: Day-to-day implementation specifics (build commands,
project layout details, integration notes) live in `memory-bank/` and the
per-project READMEs. This constitution governs *what must be true*; those
documents describe *how things currently are*.

**Version**: 1.0.0 | **Ratified**: 2026-05-31 | **Last Amended**: 2026-05-31
