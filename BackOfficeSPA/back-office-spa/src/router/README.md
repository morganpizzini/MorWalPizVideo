# Router Structure

This directory contains the modularized router configuration for the Back Office SPA application.

## Directory Structure

```
router/
├── README.md           # This file
├── types.ts            # TypeScript type definitions for routes
├── utils.ts            # Utility functions for route creation
└── routes/             # Route configuration modules
    ├── index.ts        # Protected routes (main application routes)
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

### `routes/index.ts`
Contains all protected routes organized by feature:
- Calendar Events
- Query Links
- Short Links
- Channels
- Categories
- Videos (with sub-routes: import, translate, create-root, etc.)
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

1. Add the route configuration to `routes/index.ts` in the `protectedRoutes` array
2. Use the `createRouteGroup()` utility for consistent structure
3. Follow the existing pattern for detail/edit/create sub-routes

Example:
```typescript
createRouteGroup('newfeature', {
  action: NewFeature.action,
  children: [
    { index: true, path: '', loader: NewFeature.loader, Component: NewFeature.Component },
    { path: 'create', Component: NewFeatureCreate.Component, action: NewFeatureCreate.action },
    // ... more child routes
  ],
})
```

## Route Naming Conventions

- Use lowercase paths (e.g., `productcategories`, not `ProductCategories`)
- Use hyphens for multi-word paths in action routes (e.g., `create-root`)
- Use camelCase for parameter names (e.g., `:productId`, `:categoryId`)
- Keep route paths consistent with the URL structure
