# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj", "MorWalPizVideo.BackOffice/"]
COPY ["MorWalPiz.Contracts/MorWalPiz.Contracts.csproj", "MorWalPiz.Contracts/"]
COPY ["MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj", "MorWalPizVideo.Domain/"]
COPY ["MorWalPizVideo.YouTubeUtilities/MorWalPizVideo.YouTubeUtilities.csproj", "MorWalPizVideo.YouTubeUtilities/"]
COPY ["MorWalPizVideo.Models/MorWalPizVideo.Models.csproj", "MorWalPizVideo.Models/"]
COPY ["MorWalPizVideo.MvcHelpers/MorWalPizVideo.MvcHelpers.csproj", "MorWalPizVideo.MvcHelpers/"]
COPY ["MorWalPizVideo.ServiceDefaults/MorWalPizVideo.ServiceDefaults.csproj", "MorWalPizVideo.ServiceDefaults/"]
RUN --mount=type=cache,id=backoffice-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet restore "./MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj"
COPY . .
RUN --mount=type=cache,id=backoffice-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet publish "./MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj" \
		-c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MorWalPizVideo.BackOffice.dll"]