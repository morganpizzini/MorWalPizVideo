# ADR-015: BackOffice SPA App-Store Ownership

- **Status:** Accepted
- **Date:** 2026-08-16

## Context

The BackOffice SPA needs logged-user identity, effective permissions, session state, feature flags, accessible channels, and selected-channel state across route navigation. Keeping these values in auth services, route loaders, and a channel context separately caused stale renders and repeated feature requests. Authentication and channel-header behavior also have compatibility constraints.

## Decision

Use a local Zustand app store under `frontend/back-office-spa/src/state` for cross-screen shell state. The protected root loader hydrates it once from session validation, the unscoped `/api/features` endpoint, accessible channels, and the persisted selected channel. The store is not an authentication authority and never imports React Router.

`authService` remains responsible for HttpOnly-cookie validation, validation deduplication, CSRF behavior, and 401 handling. `@morwalpizvideo/services` remains responsible for selected-channel persistence and `X-Channel-Id`. `ChannelContext` remains a compatibility facade that validates selections, delegates persistence, and requests route revalidation. Logout and unauthorized responses reset the store.

## Alternatives

- Keep state in `authService` and `ChannelContext`: rejected because non-reactive consumers and duplicate ownership produce stale shell UI.
- Fetch feature state in the import route: rejected because navigation would repeat bootstrap work and would gate single import incorrectly.
- Add a repository-wide store: rejected because the need is local to the BackOffice SPA.

## Consequences

Shell consumers use selectors for reactive user, permissions, feature, and channel state. Route loaders own route-specific data; components own ephemeral form state. Persisted channel selection remains in the shared service, while the store holds the current validated snapshot. The server remains authoritative for feature gates, permissions, authentication, and channel access.

## Migration And Rollback

Existing public APIs and service persistence remain unchanged. The legacy channel-scoped video import-status endpoint remains available for compatibility but is not used for SPA bootstrap. Rollback can remove the SPA store consumers and dependency while retaining the backend `/api/features` endpoint.

## Validation

Focused SPA tests cover hydration/reset, channel selection and revalidation, root-loader hydration, and disabled bulk-import rendering without candidate/target requests. SPA build and BackOffice build remain required checks.
