# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj", "MorWalPizVideo.ShootingRange/"]
COPY ["MorWalPizVideo.ServiceDefaults/MorWalPizVideo.ServiceDefaults.csproj", "MorWalPizVideo.ServiceDefaults/"]
RUN --mount=type=cache,id=shootingrange-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet restore "./MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj"
COPY . .
RUN --mount=type=cache,id=shootingrange-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet publish "./MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj" \
		-c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MorWalPizVideo.ShootingRange.dll"]
