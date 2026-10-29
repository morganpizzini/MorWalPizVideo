FROM node:22-alpine AS builder

WORKDIR /app
COPY package.json yarn.lock ./
COPY fe-packages/models/package.json ./fe-packages/models/
COPY fe-packages/services/package.json ./fe-packages/services/
COPY fe-packages/layout/package.json ./fe-packages/layout/
COPY FreezerAnalysis.spa/package.json ./FreezerAnalysis.spa/

RUN yarn install --frozen-lockfile

COPY fe-packages/models ./fe-packages/models
COPY fe-packages/services ./fe-packages/services
COPY fe-packages/layout ./fe-packages/layout
COPY FreezerAnalysis.spa ./FreezerAnalysis.spa

WORKDIR /app/fe-packages/models
RUN yarn build

WORKDIR /app/fe-packages/services
RUN yarn build

WORKDIR /app/fe-packages/layout
RUN yarn build

WORKDIR /app
RUN yarn install --force --frozen-lockfile

ARG VITE_FREEZER_API_BASE_URL
ENV VITE_FREEZER_API_BASE_URL=$VITE_FREEZER_API_BASE_URL

WORKDIR /app/FreezerAnalysis.spa
RUN yarn build

FROM nginx:alpine

COPY FreezerAnalysis.spa/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/FreezerAnalysis.spa/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]