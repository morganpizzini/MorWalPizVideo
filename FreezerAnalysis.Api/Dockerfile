# syntax=docker/dockerfile:1.7

FROM node:22-bookworm-slim AS frontend
WORKDIR /workspace
COPY frontend/package.json frontend/yarn.lock ./frontend/
COPY frontend/fe-packages/models/package.json ./frontend/fe-packages/models/
COPY frontend/fe-packages/services/package.json ./frontend/fe-packages/services/
COPY frontend/fe-packages/layout/package.json ./frontend/fe-packages/layout/
COPY frontend/back-office-spa/package.json ./frontend/back-office-spa/
COPY frontend/ask.client/package.json ./frontend/ask.client/
COPY frontend/morwalpizvideo.client/package.json ./frontend/morwalpizvideo.client/
COPY frontend/morwalpiz-shop.client/package.json ./frontend/morwalpiz-shop.client/
COPY frontend/shooting-ita-frontend/package.json ./frontend/shooting-ita-frontend/
COPY frontend/shooting-range.client/package.json ./frontend/shooting-range.client/
COPY frontend/shoot-recorder/package.json ./frontend/shoot-recorder/
COPY frontend/FreezerAnalysis.spa/package.json ./frontend/FreezerAnalysis.spa/
RUN --mount=type=cache,id=freezer-analysis-yarn,target=/usr/local/share/.cache/yarn,sharing=locked \
	yarn --cwd frontend install --frozen-lockfile
COPY frontend/fe-packages ./frontend/fe-packages
COPY frontend/FreezerAnalysis.spa ./frontend/FreezerAnalysis.spa
RUN yarn --cwd frontend build:shared \
	&& yarn --cwd frontend install --force --frozen-lockfile \
	&& yarn --cwd frontend workspace freezer-analysis.spa build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY ["FreezerAnalysis.Api/FreezerAnalysis.Api.csproj", "FreezerAnalysis.Api/"]
COPY ["MorWalPiz.Contracts/MorWalPiz.Contracts.csproj", "MorWalPiz.Contracts/"]
COPY ["MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj", "MorWalPizVideo.Domain/"]
COPY ["MorWalPizVideo.Models/MorWalPizVideo.Models.csproj", "MorWalPizVideo.Models/"]
COPY ["MorWalPizVideo.MvcHelpers/MorWalPizVideo.MvcHelpers.csproj", "MorWalPizVideo.MvcHelpers/"]
COPY ["MorWalPizVideo.ServiceDefaults/MorWalPizVideo.ServiceDefaults.csproj", "MorWalPizVideo.ServiceDefaults/"]
COPY ["MorWalPizVideo.YouTubeUtilities/MorWalPizVideo.YouTubeUtilities.csproj", "MorWalPizVideo.YouTubeUtilities/"]
RUN --mount=type=cache,id=freezer-analysis-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet restore FreezerAnalysis.Api/FreezerAnalysis.Api.csproj
COPY . .
RUN --mount=type=cache,id=freezer-analysis-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet publish FreezerAnalysis.Api/FreezerAnalysis.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends wget ca-certificates \
    && wget --quiet https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
    && dpkg --install packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install --yes --no-install-recommends powershell poppler-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=frontend /workspace/frontend/FreezerAnalysis.spa/dist ./wwwroot
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
EXPOSE 8080
RUN pwsh /app/playwright.ps1 install --with-deps chromium \
    && rm -rf /var/lib/apt/lists/* \
    && chown -R app:app /app /ms-playwright
USER app
ENTRYPOINT ["dotnet", "FreezerAnalysis.Api.dll"]
