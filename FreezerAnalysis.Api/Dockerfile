FROM node:22-bookworm-slim AS frontend
WORKDIR /workspace
RUN npm install --global yarn@1.22.22
COPY frontend ./frontend
RUN yarn --cwd frontend install --frozen-lockfile \
    && yarn --cwd frontend build:shared \
    && yarn --cwd frontend workspace freezer-analysis.spa build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY . .
RUN dotnet restore FreezerAnalysis.Api/FreezerAnalysis.Api.csproj
RUN dotnet publish FreezerAnalysis.Api/FreezerAnalysis.Api.csproj \
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
