# Stage 1: Build
FROM node:22-alpine AS builder

# Set working directory
WORKDIR /app

# Copy root package files and workspace manifests
COPY package.json yarn.lock ./
COPY fe-packages/models/package.json ./fe-packages/models/
COPY fe-packages/services/package.json ./fe-packages/services/
COPY morwalpizvideo.client/package.json ./morwalpizvideo.client/

# Install all dependencies (workspaces)
RUN yarn install --frozen-lockfile

# Copy shared package sources and build them
COPY fe-packages/models ./fe-packages/models
COPY fe-packages/services ./fe-packages/services

WORKDIR /app/fe-packages/models
RUN yarn build

WORKDIR /app/fe-packages/services
RUN yarn build

# Refresh workspace links after building packages
WORKDIR /app
RUN yarn install --force --frozen-lockfile

# Copy morwalpizvideo.client source files
WORKDIR /app/morwalpizvideo.client
COPY morwalpizvideo.client/tsconfig*.json ./
COPY morwalpizvideo.client/vite.config.ts ./
COPY morwalpizvideo.client/src ./src
COPY morwalpizvideo.client/index.html ./
COPY morwalpizvideo.client/public ./public

# Build the application
RUN yarn build-uncheck

# Stage 2: Production
FROM nginx:alpine

# Copy custom nginx configuration
COPY morwalpizvideo.client/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built static files from builder stage
COPY --from=builder /app/morwalpizvideo.client/dist /usr/share/nginx/html

# Copy docker entrypoint script for runtime env injection
COPY morwalpizvideo.client/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Expose port 80
EXPOSE 80

# Use custom entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]