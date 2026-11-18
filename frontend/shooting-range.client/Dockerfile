FROM node:22-alpine AS build

WORKDIR /app
COPY package.json yarn.lock ./
COPY shooting-range.client/package.json shooting-range.client/package.json
COPY fe-packages/models/package.json fe-packages/models/package.json
COPY fe-packages/layout/package.json fe-packages/layout/package.json
RUN yarn install --frozen-lockfile

COPY fe-packages/models fe-packages/models
COPY fe-packages/layout fe-packages/layout
COPY shooting-range.client shooting-range.client
RUN yarn workspace @morwalpizvideo/models build \
	&& yarn workspace @morwalpiz/layout build \
	&& yarn workspace shooting-range.client build:all

FROM node:22-alpine

WORKDIR /app

# Install only the runtime dependency required by the SSR server.
RUN printf '{"type":"module","dependencies":{"express":"^4.19.0"}}' > package.json \
	&& npm install --no-audit --no-fund

COPY --from=build /app/shooting-range.client/dist ./dist
COPY --from=build /app/shooting-range.client/dist-ssr ./dist-ssr
COPY shooting-range.client/server.mjs ./server.mjs

ENV NODE_ENV=production
EXPOSE 5177
ENV PORT=5177
CMD ["node", "server.mjs"]