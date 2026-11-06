# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MorWalPizVideo.ShortLinks/MorWalPizVideo.ShortLinks.csproj", "MorWalPizVideo.ShortLinks/"]
COPY ["MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj", "MorWalPizVideo.Domain/"]
COPY ["MorWalPizVideo.Models/MorWalPizVideo.Models.csproj", "MorWalPizVideo.Models/"]
COPY ["MorWalPizVideo.MvcHelpers/MorWalPizVideo.MvcHelpers.csproj", "MorWalPizVideo.MvcHelpers/"]
COPY ["MorWalPizVideo.ServiceDefaults/MorWalPizVideo.ServiceDefaults.csproj", "MorWalPizVideo.ServiceDefaults/"]
COPY ["MorWalPizVideo.YouTubeUtilities/MorWalPizVideo.YouTubeUtilities.csproj", "MorWalPizVideo.YouTubeUtilities/"]
RUN --mount=type=cache,id=shortlinks-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet restore "./MorWalPizVideo.ShortLinks/MorWalPizVideo.ShortLinks.csproj"
COPY . .
RUN --mount=type=cache,id=shortlinks-nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet publish "./MorWalPizVideo.ShortLinks/MorWalPizVideo.ShortLinks.csproj" \
		-c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MorWalPizVideo.ShortLinks.dll"]
