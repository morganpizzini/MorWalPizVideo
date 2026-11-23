# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute AS base
USER app
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj", "MorWalPizVideo.ShootingRange/"]
COPY ["MorWalPizVideo.ServiceDefaults/MorWalPizVideo.ServiceDefaults.csproj", "MorWalPizVideo.ServiceDefaults/"]
RUN dotnet restore "./MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj"
COPY . .
RUN dotnet publish "./MorWalPizVideo.ShootingRange/MorWalPizVideo.ShootingRange.csproj" \
		-c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MorWalPizVideo.ShootingRange.dll"]
