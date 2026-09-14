# Project Responsibilities

## BackOffice

### Owns

- Core content, channel, category, compilation, page, sponsor, product, digital-artifact, form, configuration, link, and schedule management.
- Video import, translation, transformation, publishing, and channel assignment.
- Blob uploads and administrative asset metadata.
- User, login-attempt, and API-key management.
- YouTube, Discord, Telegram, Facebook, Pinterest, AI, and insight administration.
- Hangfire jobs and cross-API cache coordination.

### Must Not Own

- Anonymous public catalog endpoints.
- Public cart or free-acquisition interactions.
- Public short-link resolution.

Shop-related ownership cleanup is frozen with the rest of the pre-production shop. Existing compatibility surfaces must not be expanded while the hold remains active.

## ServerAPI

### Owns

- Public DTO projections for published content and active catalog data.
- Anonymous form responses and sponsorship applications where explicitly approved.
- Public preview-image discovery.
- Public push-subscription behavior.

When the shop hold is explicitly lifted, ServerAPI is the intended owner of server-owned anonymous carts, permanent-free acquisitions, authorized original downloads, and future customer identity extension points. These are target responsibilities, not active implementation work.

### Must Not Own

- Administrative writes for videos, channels, categories, compilations, pages, products, or configurations.
- External publishing or administrative integration workflows.
- Raw storage keys in public responses.

## ShortLinks

### Owns

- Anonymous `/{code}` resolution on `shorts.morwalpiz.com`.
- Safe destination validation outcomes produced by management workflows.
- Atomic aggregate click count and optional visit events.
- Device-aware redirect behavior where still required.

### Must Not Own

- Link creation or management.
- Image or artifact delivery.
- General public API behavior.
- Blob credentials.

## Shared Libraries

### Models

Owns persistence-compatible records and centralized constraints. API request records currently found beside entities should move to Contracts over time.

### Domain

Owns repository ports, Mongo/mock adapters, focused application services, Blob abstractions, and external service ports. It must not depend on an API host.

### Contracts

Owns stable DTOs crossing process boundaries, including WPF-to-BackOffice and versioned API shapes where sharing is required.

### MvcHelpers

Owns host-neutral ASP.NET facilities. Authorization policy must not be embedded in a shared base class used by public and administrative hosts.

### ServiceDefaults

Owns consistent telemetry, service discovery, resilience, and health endpoint conventions. It does not own host-specific readiness checks or authorization.

## Frontend Applications

### BackOffice SPA

Owns authenticated management screens and uses loaders/actions/fetchers. API-key administration plus the RBAC and admin user-management UI belong here exclusively. The `/rbac` route tree consumes server-expanded effective permissions: user reads use `users.view`, lifecycle mutations use their granular `users.*` leaves, and groups, memberships, and direct grants use `users.permissions.manage`; `users.manage` and `backoffice.manageall` provide their documented overrides. LocalStorage is not an authority and the SPA does not own implication rules. The Domain security layer owns normalized directional expansion. The BackOffice API owns `UserGroup` persistence, memberships, direct grants, legacy `Role`/`CanAccessBackoffice` compatibility, granular user lifecycle policies, and `AllowUser` enforcement.

### Public Application

Owns public content discovery, presentation, SEO, PWA, and SSR behavior. It omits credentials and must not expose management routes.

### Shop Client

Pre-production and on hold. Its intended ownership of free-artifact discovery, cart UI, acquisition, and download remains documented for future reassessment, but no active roadmap work targets this client.

### Shooting ITA

Owns its focused content experience while reusing shared services and layout. Placeholder app-local API clients should be replaced with the shared package.

### Shooting Range POC

`MorWalPizVideo.ShootingRange` owns its account, booking, bay, configuration, exception, message, repository, and project-local contract behavior. `frontend/shooting-range.client` owns only the minimal authenticated POC experience.

The POC stays independent from BackOffice and the shared publishing libraries. Its domain endpoints are deny-by-default; only login, CSRF token acquisition, and health probes are anonymous. The first administrator is inserted manually into MongoDB. Ordinary-user onboarding remains unresolved, with admin-created users as the current working assumption.

Future messaging, schedule administration, cancellation, rescheduling, notifications, waitlists, custom sessions, and reporting remain deferred until observed use justifies them.

## Windows Applications

### VideoImporter

Owns local tenant-aware media preparation, scheduling, SQLite state, YouTube upload, and API-key BackOffice calls.

### InsightScanner

Owns local source scanning and API-key insight submissions. It does not own insight persistence or administrative review.

## Boundary Decision

Do not merge BackOffice and ServerAPI deployables. Reduce duplication by moving shared use-case logic into focused Domain services and contracts while retaining distinct authentication, exposure, scaling, and deployment boundaries.