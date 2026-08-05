# Build stage
FROM node:20-alpine AS build

WORKDIR /app

# Copy root workspace files and shared package manifests
COPY package.json yarn.lock ./
COPY fe-packages/models/package.json ./fe-packages/models/
COPY fe-packages/services/package.json ./fe-packages/services/
COPY fe-packages/layout/package.json ./fe-packages/layout/
COPY morwalpiz-shop.client/package.json ./morwalpiz-shop.client/

# Install dependencies
RUN yarn install --frozen-lockfile

# Build shared packages before the shop application
COPY fe-packages/models ./fe-packages/models
COPY fe-packages/services ./fe-packages/services
COPY fe-packages/layout ./fe-packages/layout

RUN yarn build:shared

# Copy shop application source
COPY morwalpiz-shop.client ./morwalpiz-shop.client

# Build the application
WORKDIR /app/morwalpiz-shop.client
RUN yarn build

# Production stage
FROM nginx:alpine

# Copy custom nginx config
COPY morwalpiz-shop.client/nginx.conf /etc/nginx/nginx.conf

# Copy built assets from build stage
COPY --from=build /app/morwalpiz-shop.client/dist /usr/share/nginx/html

# Copy docker entrypoint script
COPY morwalpiz-shop.client/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Expose port
EXPOSE 80

# Use custom entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]