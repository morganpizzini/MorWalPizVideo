# Frontend Architecture

## Workspace Model

The frontend is a Yarn Classic workspace. Shared packages are built before applications:

1. `@morwalpizvideo/models`
2. `@morwalpizvideo/services`
3. `@morwalpiz/layout`
4. Application workspace

Shared packages are appropriate only when multiple applications genuinely consume the same contract, transport behavior, or presentation component.

## Shared Packages

### Models

Owns strict TypeScript API shapes. Models must represent public/admin DTOs rather than mirroring Mongo entities. Breaking changes require coordinated consumer review.

### Services

Owns:

- Endpoint constants and URL composition.
- Runtime `window.ENV` and build-time `VITE_*` API base resolution.
- Fetch-based HTTP behavior.
- Credential mode and token-provider injection.
- Shared domain API functions.

Applications must not introduce direct `fetch` or Axios clients when this package already owns the call. Public applications set credentials to `omit`; authenticated applications use the established token/cookie flow. Service-worker network interception and downloads from arbitrary URLs issued by the API remain direct-fetch exceptions because they do not call a repository-owned endpoint.

### Layout

Owns reusable visual components used by more than one application. App-specific business workflows and routes remain local.

## BackOffice SPA

- React 19 and React Router data routes.
- Protected root loader validates authentication.
- Feature routes use loaders, actions, and fetchers.
- React Bootstrap, Lucide icons, shared services, and local reusable management components.

Target authentication is secure HttpOnly cookie based. Local-storage JWT support is transitional and should be removed after CSRF and cross-origin cookie behavior is verified.

Administrative capabilities, including API-key and digital-artifact management, belong only here.

RBAC management is owned by this SPA and the BackOffice API. The `/rbac` route loader validates the HttpOnly-cookie session through `/api/auth/validate` and requires canonical `canaccessbackoffice` in the server-resolved effective permission union. Cached `localStorage.auth_user` is display-only; denied sessions redirect to `/` and invalid sessions to `/login`.

## Public Application

- React 19 with SSR, PWA behavior, SEO, analytics, and public routes.
- Hosted on Aruba at `https://morwalpiz.com`.
- Calls `https://morwalpiz-serverapi.azurewebsites.net` directly.
- Uses `credentials: omit` for public calls.

Remove API-key administration routes from this application. Replace legacy `morwalpiz.it` canonical metadata with the authoritative domain.

## Shop Client

The shop is a free digital-artifact application, not a payment application.

Target workflow:

1. Load active artifact DTOs from ServerAPI.
2. Render public preview images in ordinary `<img>` elements.
3. Add an artifact to a server-owned anonymous cart.
4. Persist a permanent-free acquisition linked to that cart.
5. Request a short-lived download URL for the private original.

The client never receives a storage key. Payment method, price-changing, and simulated paid-checkout contracts must be removed. A future customer account may claim anonymous acquisitions.

## Shooting ITA

Shooting ITA is in scope and reuses shared layout and service behavior. Current maintained routes include home and focused video/category views. Its unused hard-coded Axios placeholder and dependency have been removed; maintained API calls use `@morwalpizvideo/services`. Preserve its local category derivation where the behavior is application-specific.

## State Management

Use router loader/action/fetcher state, local component state, and existing contexts. Do not introduce a repository-wide state library without an approved architectural decision.

## Configuration

- Runtime Docker injection: `window.ENV.VITE_API_BASE_URL`.
- Vite build-time fallback: `VITE_API_BASE_URL`.
- Relative paths are development-only through Vite proxying.
- The Aruba deployment must publish the Azure ServerAPI base URL.

The shop deployment currently uses inconsistent variable names and must be aligned with the shared service package.

## Testing

BackOffice SPA and Shooting ITA use Vitest, Testing Library, and jsdom. Add focused route/action/service tests for changed behavior. BackOffice RBAC coverage includes route allow/deny cases, while the BackOffice test project covers the cookie validation contract. The public and shop applications need test coverage before their contracts become release gates.