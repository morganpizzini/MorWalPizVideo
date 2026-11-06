# syntax=docker/dockerfile:1.7
#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj", "MorWalPizVideo.ServerAPI/"]
COPY ["MorWalPiz.Contracts/MorWalPiz.Contracts.csproj", "MorWalPiz.Contracts/"]
COPY ["MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj", "MorWalPizVideo.Domain/"]
COPY ["MorWalPizVideo.YouTubeUtilities/MorWalPizVideo.YouTubeUtilities.csproj", "MorWalPizVideo.YouTubeUtilities/"]
COPY ["MorWalPizVideo.Models/MorWalPizVideo.Models.csproj", "MorWalPizVideo.Models/"]
COPY ["MorWalPizVideo.MvcHelpers/MorWalPizVideo.MvcHelpers.csproj", "MorWalPizVideo.MvcHelpers/"]
COPY ["MorWalPizVideo.ServiceDefaults/MorWalPizVideo.ServiceDefaults.csproj", "MorWalPizVideo.ServiceDefaults/"]
RUN dotnet restore "./MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj"
COPY . .
WORKDIR "/src/MorWalPizVideo.ServerAPI"
RUN dotnet publish "./MorWalPizVideo.ServerAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MorWalPizVideo.ServerAPI.dll"]