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

The current anonymous BackOffice `api/shop/*` controllers duplicate ServerAPI and are scheduled for removal after consumer verification.

## ServerAPI

### Owns

- Public DTO projections for published content and active catalog data.
- Anonymous form responses and sponsorship applications where explicitly approved.
- Public preview-image discovery.
- Server-owned anonymous cart, permanent-free acquisition, and authorized original download.
- Future customer identity and analytics extension points.
- Public push-subscription behavior.

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

Owns authenticated management screens and uses loaders/actions/fetchers. API-key administration plus the RBAC and admin user-management UI belong here exclusively. The `/rbac` route guard consumes effective permissions from the BackOffice cookie-validation flow; localStorage is not an authority. The BackOffice API owns `UserGroup` persistence, user group memberships, direct permissions, effective-permission union, legacy `Role`/`CanAccessBackoffice` compatibility, admin user lifecycle endpoints, and `AllowUser` enforcement.

### Public Application

Owns public content discovery, presentation, SEO, PWA, and SSR behavior. It omits credentials and must not expose management routes.

### Shop Client

Owns free-artifact discovery, anonymous cart UI, acquisition, and download. It does not own authorization decisions or storage URLs.

### Shooting ITA

Owns its focused content experience while reusing shared services and layout. Placeholder app-local API clients should be replaced with the shared package.

## Windows Applications

### VideoImporter

Owns local tenant-aware media preparation, scheduling, SQLite state, YouTube upload, and API-key BackOffice calls.

### InsightScanner

Owns local source scanning and API-key insight submissions. It does not own insight persistence or administrative review.

## Boundary Decision

Do not merge BackOffice and ServerAPI deployables. Reduce duplication by moving shared use-case logic into focused Domain services and contracts while retaining distinct authentication, exposure, scaling, and deployment boundaries.