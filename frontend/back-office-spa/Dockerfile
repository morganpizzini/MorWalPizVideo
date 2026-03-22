# Stage 1: Build
FROM node:22-alpine AS builder

# Set working directory
WORKDIR /app

# Copy the local models package first
COPY fe-packages/models ./packages/models
COPY fe-packages/services ./packages/services
COPY package*.json ./

# Install dependencies and build models package
WORKDIR /app/packages/models
RUN npm ci && npm run build

WORKDIR /app/packages/services
RUN npm ci && npm run build

# Go back to app root and copy back-office-spa files
WORKDIR /app
COPY back-office-spa/package*.json ./back-office-spa/
COPY back-office-spa/tsconfig*.json ./back-office-spa/
COPY back-office-spa/vite.config.ts ./back-office-spa/

# Install dependencies for back-office-spa
WORKDIR /app/back-office-spa
RUN npm ci

# Copy source files
COPY back-office-spa/src ./src
COPY back-office-spa/index.html ./

# Build the application
RUN npm run build-uncheck

# Stage 2: Production
FROM nginx:alpine

# Copy custom nginx configuration
COPY back-office-spa/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built static files from builder stage
COPY --from=builder /app/back-office-spa/dist /usr/share/nginx/html

# Copy docker entrypoint script for runtime env injection
COPY back-office-spa/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Expose port 80
EXPOSE 80

# Use custom entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
