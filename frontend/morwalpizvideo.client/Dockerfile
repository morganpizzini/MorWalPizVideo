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

# Build the application (client bundle)
RUN yarn build-uncheck

# Build the SSR bundle (bundles all deps; no node_modules needed at runtime)
RUN yarn build:ssr

# Stage 2: Production — Node.js SSR server
FROM node:22-alpine

WORKDIR /app

# Create a minimal package.json with only the production HTTP server dep
RUN printf '{"type":"module","dependencies":{"express":"^4.19.0"}}' > package.json \
    && npm install --no-audit --no-fund

# Copy client static files and the self-contained SSR bundle
COPY --from=builder /app/morwalpizvideo.client/dist      ./dist
COPY --from=builder /app/morwalpizvideo.client/dist-ssr  ./dist-ssr

# Copy the SSR server entry point
COPY morwalpizvideo.client/server.mjs ./

# Expose the Node.js SSR server port
EXPOSE 3000

CMD ["node", "server.mjs"]