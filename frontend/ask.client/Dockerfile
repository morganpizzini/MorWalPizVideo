FROM node:22-alpine AS build
WORKDIR /app
COPY package.json yarn.lock ./
COPY fe-packages/models/package.json fe-packages/models/package.json
COPY fe-packages/services/package.json fe-packages/services/package.json
COPY ask.client/package.json ask.client/package.json
RUN yarn install --frozen-lockfile
COPY fe-packages/models fe-packages/models
COPY fe-packages/services fe-packages/services
COPY ask.client ask.client
RUN yarn workspace @morwalpizvideo/models build
RUN yarn workspace @morwalpizvideo/services build
RUN yarn workspace ask.client build:all

FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/package.json /app/yarn.lock ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/ask.client ./ask.client
WORKDIR /app/ask.client
ENV NODE_ENV=production
EXPOSE 5180
CMD ["yarn", "serve"]