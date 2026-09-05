FROM node:22-alpine AS build

WORKDIR /app
COPY package.json yarn.lock ./
COPY shooting-range.client/package.json shooting-range.client/package.json
RUN yarn install --frozen-lockfile

COPY shooting-range.client shooting-range.client
RUN yarn workspace shooting-range.client build:all

FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/package.json /app/yarn.lock ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/shooting-range.client ./shooting-range.client
WORKDIR /app/shooting-range.client
ENV NODE_ENV=production
EXPOSE 5177
CMD ["yarn", "serve"]