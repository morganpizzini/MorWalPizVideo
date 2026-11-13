FROM node:22-alpine AS builder

WORKDIR /app
COPY package.json yarn.lock ./
COPY fe-packages/models/package.json ./fe-packages/models/
COPY fe-packages/services/package.json ./fe-packages/services/
COPY fe-packages/layout/package.json ./fe-packages/layout/
COPY shooter-workshop.client/package.json ./shooter-workshop.client/

RUN yarn install --frozen-lockfile

COPY fe-packages/models ./fe-packages/models
COPY fe-packages/services ./fe-packages/services
COPY fe-packages/layout ./fe-packages/layout
COPY shooter-workshop.client ./shooter-workshop.client

WORKDIR /app/fe-packages/models
RUN yarn build

WORKDIR /app/fe-packages/services
RUN yarn build

WORKDIR /app/fe-packages/layout
RUN yarn build

WORKDIR /app/shooter-workshop.client
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN yarn build

FROM nginx:alpine

COPY shooter-workshop.client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/shooter-workshop.client/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
