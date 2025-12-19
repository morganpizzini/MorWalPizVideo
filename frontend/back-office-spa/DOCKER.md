# Docker Setup for Back Office SPA

This document provides instructions for building and running the back-office-spa application using Docker.

## Prerequisites

- Docker installed on your system
- Access to the monorepo root directory

## Project Structure

The Dockerfile is configured to:
1. Build the `@morwalpizvideo/models` package from `packages/models`
2. Build the React SPA application
3. Serve the static files using nginx
4. Support runtime environment variable injection

## Building the Docker Image

### From Monorepo Root

The Dockerfile must be built from the **monorepo root** directory to access both `back-office-spa` and `packages` directories:

```bash
# Navigate to monorepo root
cd /path/to/MorWalPizVideo

# Build the Docker image
docker build -f back-office-spa/Dockerfile -t back-office-spa:latest .
```

### Build Arguments (Optional)

You can pass build arguments if needed:

```bash
docker build -f back-office-spa/Dockerfile \
  --build-arg NODE_VERSION=20 \
  -t back-office-spa:latest .
```

## Running the Container

### Basic Run

```bash
docker run -d \
  --name back-office-spa \
  -p 8080:80 \
  back-office-spa:latest
```

Access the application at: http://localhost:8080

### With Environment Variables

To inject API URLs at runtime:

```bash
docker run -d \
  --name back-office-spa \
  -p 8080:80 \
  -e VITE_API_BASE_URL=https://api.example.com \
  -e API_BASE_URL=https://api.example.com \
  back-office-spa:latest
```

### Using Docker Compose

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  back-office-spa:
    build:
      context: .
      dockerfile: back-office-spa/Dockerfile
    ports:
      - "8080:80"
    environment:
      - VITE_API_BASE_URL=https://api.example.com
      - API_BASE_URL=https://api.example.com
    restart: unless-stopped
```

Run with:

```bash
docker-compose up -d
```

## Azure Deployment

### Azure Container Registry (ACR)

1. **Login to ACR:**
```bash
az acr login --name <your-acr-name>
```

2. **Tag the image:**
```bash
docker tag back-office-spa:latest <your-acr-name>.azurecr.io/back-office-spa:latest
```

3. **Push to ACR:**
```bash
docker push <your-acr-name>.azurecr.io/back-office-spa:latest
```

### Azure App Service (Container)

Deploy using Azure CLI:

```bash
az webapp create \
  --resource-group <resource-group> \
  --plan <app-service-plan> \
  --name <app-name> \
  --deployment-container-image-name <your-acr-name>.azurecr.io/back-office-spa:latest
```

Set environment variables:

```bash
az webapp config appsettings set \
  --resource-group <resource-group> \
  --name <app-name> \
  --settings \
    VITE_API_BASE_URL=https://your-api.azurewebsites.net \
    API_BASE_URL=https://your-api.azurewebsites.net
```

### Azure Container Instances (ACI)

Deploy using Azure CLI:

```bash
az container create \
  --resource-group <resource-group> \
  --name back-office-spa \
  --image <your-acr-name>.azurecr.io/back-office-spa:latest \
  --dns-name-label back-office-spa \
  --ports 80 \
  --environment-variables \
    VITE_API_BASE_URL=https://your-api.azurewebsites.net \
    API_BASE_URL=https://your-api.azurewebsites.net
```

## Environment Variables

The application supports the following environment variables at runtime:

| Variable | Description | Example |
|----------|-------------|---------|
| `VITE_API_BASE_URL` | Primary API base URL | `https://api.example.com` |
| `API_BASE_URL` | Alternative API base URL | `https://api.example.com` |
| `REACT_APP_API_URL` | Legacy API URL variable | `https://api.example.com` |

These variables are injected into `window.ENV` object at container startup.

### Using Environment Variables in Code

To use the injected environment variables in your React application, add this script tag to your `index.html`:

```html
<script src="/env-config.js"></script>
```

Then access them in your code:

```typescript
const apiBaseUrl = window.ENV?.VITE_API_BASE_URL || 'http://localhost:5000';
```

## Health Check

The nginx configuration includes a health check endpoint:

```bash
curl http://localhost:8080/health
```

Expected response: `healthy`

## Troubleshooting

### Container won't start

Check logs:
```bash
docker logs back-office-spa
```

### Build fails

1. Ensure you're building from the monorepo root
2. Check that `packages/models` exists and has a valid `package.json`
3. Verify all required files are not excluded in `.dockerignore`

### Environment variables not working

1. Verify the entrypoint script is executable
2. Check that `/env-config.js` is created in the container:
```bash
docker exec back-office-spa cat /usr/share/nginx/html/env-config.js
```

### Port already in use

Change the host port:
```bash
docker run -d --name back-office-spa -p 9090:80 back-office-spa:latest
```

## Image Optimization

The final image size is optimized using:
- Multi-stage build (separates build and runtime)
- Alpine Linux base images
- Minimal nginx configuration
- Efficient layer caching

Typical image size: ~50-80MB

## Security Considerations

- The image runs nginx as a non-root user (default Alpine nginx)
- Security headers are configured in `nginx.conf`
- Hidden files are blocked
- Only necessary files are copied from build stage

## Development vs Production

This Dockerfile is designed for **production use**. For development:

```bash
# Use the standard npm dev server
npm run dev
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Push Docker Image

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Login to ACR
        uses: docker/login-action@v2
        with:
          registry: ${{ secrets.ACR_NAME }}.azurecr.io
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}
      
      - name: Build and push
        uses: docker/build-push-action@v4
        with:
          context: .
          file: back-office-spa/Dockerfile
          push: true
          tags: ${{ secrets.ACR_NAME }}.azurecr.io/back-office-spa:latest
```

## Support

For issues or questions, please refer to the main project documentation or contact the development team.
