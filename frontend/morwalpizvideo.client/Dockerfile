# Stage 1: Build
FROM node:22-alpine AS builder

# Set working directory
WORKDIR /app

# Copy the local models package first
COPY fe-packages/models ./packages/models
COPY package*.json ./

# Install dependencies and build models package
WORKDIR /app/packages/models
RUN npm ci && npm run build

# Go back to app root and copy morwalpizvideo.client files
WORKDIR /app
COPY morwalpizvideo.client/package*.json ./morwalpizvideo.client/

# Install dependencies for morwalpizvideo.client
WORKDIR /app/morwalpizvideo.client
RUN npm ci

# Copy source files
COPY morwalpizvideo.client/src ./src
COPY morwalpizvideo.client/index.html ./
COPY morwalpizvideo.client/vite.config.ts ./
COPY morwalpizvideo.client/tsconfig*.json ./
COPY morwalpizvideo.client/public ./public

# Build the application
RUN npm run build

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