# MorWalPiz Shop Client

E-commerce frontend application for selling digital products (PDFs, documents).

## Overview

This application is built with React 19, TypeScript, and Vite, using a shared monorepo architecture with reusable packages.

## Features

- Email-only authentication (no passwords)
- Digital product catalog
- Shopping cart functionality
- Secure checkout with email verification
- Download management for purchased content
- SEO-optimized with react-helmet-async
- ReCaptcha v3 integration for security
- Responsive design with Bootstrap 5.3
- Parametric layout components from `@morwalpiz/layout`

## Tech Stack

- **React 19** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **React Router v7** - Routing
- **Bootstrap 5.3** - UI framework
- **SCSS** - Styling
- **Yarn Workspaces** - Monorepo management

## Shared Packages

- `@morwalpizvideo/models` - Shared TypeScript models
- `@morwalpizvideo/services` - API service layer
- `@morwalpiz/layout` - Reusable layout components (header, footer, app shell)

## Development

```bash
# Install dependencies (from frontend directory)
yarn install

# Start development server
yarn dev

# Build for production
yarn build

# Preview production build
yarn preview

# Lint code
yarn lint

# Format code
yarn format
```

## Environment Variables

Create a `.env` file in the project root:

```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_RECAPTCHA_SITE_KEY=your_recaptcha_site_key_here
```

## Project Structure

```
src/
├── main.tsx              # Application entry point
├── main.scss             # Global styles
├── routes/               # Route components
│   ├── root.tsx          # Root layout with AppShell
│   ├── index.tsx         # Home page
│   └── error-page.tsx    # Error boundary
├── utils/
│   └── layout-config.ts  # Layout configuration
└── types/
    └── env.d.ts          # Environment type definitions
```

## Known Issues

### Build Issue with @morwalpiz/layout Package

The production build currently fails with a package resolution error for `@morwalpiz/layout`. This is a known Vite/Rollup issue with workspace packages in monorepos.

**Workaround**: The application works correctly in development mode (`yarn dev`). For production builds, consider:
1. Publishing the `@morwalpiz/layout` package to npm
2. Using Vite's resolve.alias configuration
3. Building with the `--no-bundle` flag

## Deployment

The application is designed to be deployed as a static site. Build the application and deploy the `dist` folder to your hosting provider.

### Docker Support

A Dockerfile will be added following the same pattern as the `morwalpizvideo.client` application.

## Integration with Backend

The application connects to the MorWalPiz Video BackOffice API for:
- Product catalog management
- Order processing
- User authentication
- Digital content delivery via Azure Blob Storage

API base URL is configured via `VITE_API_BASE_URL` environment variable.

## Next Steps (Implementation Phases)

- Phase 3: Authentication implementation
- Phase 4: Product catalog pages  
- Phase 5: Shopping cart and checkout
- Phase 6: Legal pages (terms, privacy)
- Phase 7: Backoffice UI for product management
- Phase 8: Testing and hardening
- Phase 9: Production deployment

## License

Proprietary - MorWalPiz