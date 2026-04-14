# System Patterns - MorWalPizVideo

## Architecture
Multi-app .NET/React system with shared libraries and MongoDB storage.

## Backend Structure
### .NET Projects
- **MorWalPizVideo.Domain** - Repository pattern, DataService, MongoDB access
- **MorWalPizVideo.Models** - Domain models, converters, constraints
- **MorWalPizVideo.MvcHelpers** - Shared utilities, BaseRequest wrappers
- **MorWalPizVideo.BackOffice** - Admin API controllers
- **MorWalPizVideo.ServerAPI** - Public API controllers

### Repository Pattern
- `IRepository<T>` interface for generic CRUD
- `MockRepository` for testing, production repositories for MongoDB
- Service layer: `DataService`, `YTService`, `TranslatorService`, `ExternalDataService`

### API Controller Pattern
Controllers use request contracts separate from domain models:
```csharp
public class CreateEntityRequest { /* validation attributes */ }
public class BaseRequest<T> { [FromBody] public T Body { get; set; } }
public class BaseRequestId<T> : BaseRequest<T> { [FromRoute] public string Id { get; set; } }

[HttpPost]
public async Task<ActionResult> Create(BaseRequest<CreateEntityRequest> request)
{
    var entity = MapToDomain(request.Body);
    await _dataService.SaveEntity(entity);
    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
}
```
Benefits: API contract separation, automatic validation, consistent patterns

## Frontend Structure
### Monorepo with Shared Packages
```
frontend/
├── fe-packages/
│   ├── models/     # @morwalpizvideo/models - shared TypeScript types
│   ├── services/   # @morwalpizvideo/services - shared API calls  
│   └── layout/     # @morwalpizvideo/layout - shared components
├── back-office-spa/       # Admin SPA
├── morwalpizvideo.client/ # Public client
└── morwalpiz-shop.client/ # Shop client
```

### Shared Models Package
- Single source of truth for TypeScript types
- Import: `import { Video, Product } from '@morwalpizvideo/models'`
- Compiled to ES modules in `dist/`

### React Router v7 Pattern
- File-based routing with loaders (data fetch) and actions (mutations)
- Each route: `Component.tsx`, `loader.ts`, `action.ts`, `index.ts`
- Server state via loaders, local state for UI

## Data Models
### Core Entities
- **YouTubeContent** - Root video collection with VideoRef[] array, thumbnailVideoId
- **Video** - Complete video with metadata (youtubeId, title, description, category)
- **VideoRef** - Lightweight reference within collections
- **DigitalProduct** - Shop products with category, price, stock
- **Customer** - Shop user with cart
- **CustomForm** - Dynamic forms with typed questions (text, select, checkbox, etc.)
- **InsightTopic/NewsItem/ContentPlan** - Content planning domain

### MongoDB Collections
- Document per aggregate root
- Embedded objects (VideoRef in YouTubeContent)
- Indexes on youtubeId, category fields

## Authentication & Security
- JWT tokens with role-based access
- API key system for external integrations (ScraperController)
- Rate limiting via `ApiKeyRateLimitingService`
- CORS configuration for cross-origin requests

## Deployment
### Docker Pattern
Multi-stage builds: Node builder → nginx alpine runtime
- Stage 1: Build shared packages + React app
- Stage 2: nginx serves static files, runtime env injection via env-config.js
- Image size: ~50-80MB

### Nginx SPA Config
- All routes fallback to `index.html` for client-side routing
- Static assets cached 1 year with immutable flag
- Security headers (X-Frame-Options, X-Content-Type-Options)
- Health endpoint at `/health`

### Azure Deployment
- Container Registry (ACR) for image storage
- App Service for managed hosting
- Environment variables injected at runtime
- GitHub Actions CI/CD pipeline