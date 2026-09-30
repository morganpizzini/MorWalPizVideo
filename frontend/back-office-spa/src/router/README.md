# Router Structure

This directory contains the modularized router configuration for the Back Office SPA application.

## Directory Structure

```
router/
├── README.md           # This file
├── types.ts            # TypeScript type definitions for routes
├── utils.ts            # Utility functions for route creation
└── routes/             # Route configuration modules
    ├── index.ts        # Public protectedRoutes export
    ├── lazy-index.ts   # Lazy protected route registry
    └── auth.routes.ts  # Authentication routes (login)
```

## Files

### `types.ts`

Contains TypeScript type definitions used across the router configuration.

### `utils.ts`

Provides utility functions for creating routes:

- `createErrorElement()` - Creates a standard error boundary element
- `createRouteGroup()` - Creates a route group with common configuration

### `routes/auth.routes.ts`

Contains authentication-related routes (login page) that are accessible without authentication.

### `routes/lazy-index.ts`

Contains all protected routes organized by feature. Feature components and their loader/action modules are loaded through React Router's `lazy` route-module contract, so Vite emits static route assets instead of placing every feature in the initial router chunk.

Protected loaders are permission-wrapped before they run. A permitted loader returns a promise-valued field through `deferRouteData`; the route component is rendered immediately with `createAsyncRouteComponent`, and the component reads its resolved value with `useResolvedLoaderData`. This is the React Router `Await`/`Suspense` pattern used by this SPA. Rejected data is rendered by the shared async boundary, while authorization redirects remain router responses.

### `routes/index.ts`

Re-exports the protected route registry for the main router. Keep this public export stable when adding routes.

Protected routes include:

- Calendar Events
- Query Links
- Short Links
- Channels
- Categories
- Videos (with sub-routes: import, translate, detail/edit)
- Images
- MorWalPiz Configurations
- Product Categories
- Sponsors
- Products

## Main Router (`../router.ts`)

The main router file combines:

1. **Auth routes** - Public routes (login)
2. **Protected routes** - Wrapped in `PrimaryLayout` component

## Benefits of This Structure

1. **Modularity** - Routes are organized by feature/domain
2. **Maintainability** - Easy to find and update specific route configurations
3. **Scalability** - New route modules can be added without modifying existing files
4. **Type Safety** - TypeScript types ensure route configuration correctness
5. **Code Reuse** - Utility functions reduce duplication
6. **Clear Separation** - Auth routes vs protected routes are clearly distinguished

## Adding New Routes

To add new routes:

1. Add the route configuration to `routes/lazy-index.ts` in the `routeDefinitions` array
2. Use the `createRouteGroup()` utility for consistent structure
3. Use `feature()` or `indexFeature()` with a dynamic import for route modules
4. Export the route loader in the normal route module shape; `protectRoute` applies auth and permission checks before starting it
5. Keep loader results as the existing resolved data shape inside the promise; use `useResolvedLoaderData<T>()` in the component
6. Keep permission wrapping in `protectRoute`; do not attach unguarded loaders/actions directly to the main router

Example:

```typescript
createRouteGroup('newfeature', {
  action: NewFeature.action,
  children: [
    { index: true, path: '', loader: NewFeature.loader, Component: NewFeature.Component },
    { path: 'create', Component: NewFeatureCreate.Component, action: NewFeatureCreate.action },
    // ... more child routes
  ],
});
```

### Protected route checklist

- Validate session and permissions before starting any protected data request.
- Keep `authLoader` awaited for session validation, redirects, store hydration, selected channel, and feature state.
- Use `feature`, `indexFeature`, `group`, or `actionFeature` in `routes/lazy-index.ts`; do not edit the unused legacy registry.
- For a route with data, render through the lazy route-module boundary and consume data with `useResolvedLoaderData`.
- Preserve existing loader response shapes, actions, `Response` errors, and permission mappings.
- Verify pending, resolved, rejected, revalidated, anonymous, and forbidden behavior for new loaders.

`PrimaryLayout` owns the fixed, indefinite progress bar. It follows `useNavigation` for loading and submitting states and also observes deferred route data through `DeferredRouteProgressProvider`, so it remains active until an `Await` promise resolves, rejects, or is abandoned by an unmounted route.

## Loading ownership

`src/components/LoadingSkeleton` owns the shared visual loading states. Use `PageSkeleton` for route or entity content that is not available yet and `TableSkeleton` for an initial table-shaped fetch. Skeletons expose an accessible status and visually hidden loading label; they do not render generic visible `Loading...` copy.

The async route boundary owns protected route-data loading and uses `PageSkeleton`. Local components may use the same skeletons only for their initial data fetch. Keep meaningful operation feedback such as uploading, publishing, importing, analyzing, and dependent option loading visible because those states describe an action or a specific dependency rather than the route shell.

When adding a route, keep its loader data contract and error/null semantics unchanged. Use the async boundary for loader promises, use a skeleton for a local initial-data branch, and reserve text labels for actionable operations or specific recovery states. Do not add route-specific spinners when the shared boundary or skeleton already represents the state.

## Route Naming Conventions

- Use lowercase paths (e.g., `productcategories`, not `ProductCategories`)
- Use hyphens for multi-word paths in action routes (e.g., `upload-multiple`)
- Use camelCase for parameter names (e.g., `:productId`, `:categoryId`)
- Keep route paths consistent with the URL structure
