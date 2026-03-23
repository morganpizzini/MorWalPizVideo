# Back Office SPA Docker Build Guide

This document explains how to build and run the Back Office SPA using Docker with Yarn workspaces.

## Prerequisites

- Docker installed on your system
- Access to the repository root directory

## Build Context

The Docker build must be executed from the `frontend/` directory, as it needs access to:
- `package.json` and `yarn.lock` - Monorepo root workspace configuration
- `fe-packages/models` - Shared TypeScript models package
- `fe-packages/services` - Shared services/API layer package  
- `back-office-spa/` - The main application

## Package Manager

The monorepo uses **Yarn 1.x (Classic)** with workspaces for dependency management. The `yarn.lock` file at the frontend root ensures consistent installs across all environments.

## Building the Docker Image

### From the frontend directory:

```bash
cd frontend
docker build -f back-office-spa/Dockerfile -t back-office-spa:latest .
```

**Important**: The build context (`.`) is the `frontend/` directory, not the repository root or the `back-office-spa/` subdirectory.

## Running the Container

```bash
docker run -p 8080:80 \
  -e VITE_API_BASE_URL=https://your-api-url.com \
  -e VITE_OTHER_CONFIG=value \
  back-office-spa:latest
```

The application will be available at `http://localhost:8080`.

## Environment Variables

The following environment variables can be configured at runtime:

- `VITE_API_BASE_URL` - Base URL for the backend API
- Other `VITE_*` variables as needed by the application

These are injected at container startup via the `docker-entrypoint.sh` script, which generates `/usr/share/nginx/html/env-config.js`.

## Multi-Stage Build Details

The Dockerfile uses a two-stage build:

1. **Builder Stage**: 
   - Copies and builds `fe-packages/models` and `fe-packages/services`
   - Copies and builds the back-office-spa application
   - Uses Node.js 22 Alpine

2. **Production Stage**:
   - Uses nginx Alpine for serving static files
   - Copies built assets from builder stage
   - Includes runtime environment variable injection

## Troubleshooting

### Build fails with "cannot resolve @morwalpizvideo/models"

This indicates the Docker build context is incorrect. Ensure you're running the build command from the `frontend/` directory with the correct `-f` flag pointing to the Dockerfile.

### Build fails with "cannot find fe-packages"

The build context must be `frontend/`, not the repository root or `back-office-spa/` directory.

## Workspace Resolution

The monorepo uses Yarn workspaces with the following structure:
- `@morwalpizvideo/models` → `fe-packages/models`
- `@morwalpizvideo/services` → `fe-packages/services`
- `back-office-spa` → main application
- `morwalpizvideo.client` → public-facing client

The Dockerfile:
1. Copies root `package.json` and `yarn.lock` first for layer caching
2. Copies all workspace `package.json` files
3. Runs `yarn install --frozen-lockfile` once at the monorepo root
4. Builds shared packages in order
5. Builds the back-office-spa application

This approach respects Yarn's workspace hoisting and ensures consistent dependency resolution.
