# Technical Context - MorWalPizVideo

## Stack
- **Backend**: .NET 8, ASP.NET Core Web API, MongoDB
- **Frontend**: React 19, TypeScript, Vite, React Router v7, Bootstrap
- **Desktop**: WPF (.NET 8)
- **External**: YouTube Data API v3, Azure Translator, Discord/Telegram APIs

## Development Setup
```bash
# Backend
dotnet restore
dotnet run --project MorWalPizVideo.BackOffice

# Frontend monorepo
cd frontend
npm install              # Installs all workspaces
npm run build -w models  # Build shared models package
cd back-office-spa && npm run dev

# Desktop
dotnet run --project MorWalPiz.VideoImporter
```

## Workspace Structure
```
frontend/
├── package.json         # Root workspace config
├── fe-packages/
│   ├── models/         # @morwalpizvideo/models
│   ├── services/       # @morwalpizvideo/services
│   └── layout/         # @morwalpizvideo/layout
├── back-office-spa/
├── morwalpizvideo.client/
└── morwalpiz-shop.client/
```

## Configuration
### Backend (appsettings.json)
```json
{
  "ConnectionStrings": { "MongoDB": "..." },
  "YouTubeApi": { "ApiKey": "...", "QuotaLimit": 10000 },
  "AzureTranslator": { "SubscriptionKey": "...", "Endpoint": "..." }
}
```

### Frontend Environment
- `VITE_API_BASE_URL` - API endpoint
- Runtime injection via Docker `env-config.js`

## Deployment
### Docker Build
```dockerfile
FROM node:20-alpine AS builder
# Build shared packages + React app
FROM nginx:alpine
# Serve static files with runtime env injection
```

### Azure Resources
- **ACR** - Container image registry
- **App Service** - Managed container hosting  
- **Key Vault** - Production secrets
- **GitHub Actions** - CI/CD pipelines

## Database
- **MongoDB** with document collections
- Indexes: `youtubeId` (unique), `category`
- BSON serialization via MongoDB.Driver

## Key Commands
- `dotnet build` - Build .NET solution
- `npm run dev` - Start Vite dev server
- `npm run build` - Production build
- `docker build -t app .` - Container build