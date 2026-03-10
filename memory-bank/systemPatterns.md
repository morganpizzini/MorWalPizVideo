# System Patterns - MorWalPizVideo

## Architecture Overview

MorWalPizVideo follows a **modular microservices architecture** with clear separation of concerns across multiple applications and services. The system is designed around **domain-driven design principles** with well-defined boundaries between video management, translation services, and social media integration.

## Core Architectural Patterns

### 1. Multi-Application Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   BackOffice    │    │  Video Importer │    │   Short Links   │
│   (Web API)     │    │   (WPF Desktop) │    │   (Web API)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
         ┌─────────────────────────────────────────────┐
         │            Shared Components                │
         │  • MorWalPizVideo.Domain                   │
         │  • MorWalPizVideo.Models                   │
         │  • MorWalPizVideo.ServiceDefaults          │
         └─────────────────────────────────────────────┘
```

### 2. Repository Pattern Implementation
- **Interface Segregation**: `IRepository<T>`, `IUserRepository`, `ILoginAttemptRepository`
- **Mock vs Production**: `MockRepository` for testing, production repositories for MongoDB
- **Generic Base**: Common CRUD operations abstracted in base repository

### 3. Service Layer Pattern
```csharp
// Service Dependencies Flow
DataService → MongoDB Operations
YTService → YouTube API Integration  
TranslatorService → Azure Translation
ExternalDataService → Metadata Fetching
CrossApiService → Cache Management
```

## Domain Models and Data Patterns

### Core Entity Relationships
```
YouTubeContent (Root Entity)
├── VideoRef[] (Collection of video references)
├── Category (string)
├── ThumbnailVideoId (string)
└── Metadata (title, description, url)

Video (Complete YouTube Video)
├── YoutubeId (Primary Identifier)
├── Metadata (title, description, views, likes, comments)
├── PublishedAt (DateOnly)
└── Category (string)
```

### Video Organization Hierarchy
1. **Single Videos**: Individual YouTube videos with metadata
2. **Root Collections**: Parent containers with multiple sub-videos
3. **Video References**: Lightweight pointers within collections
4. **Categories**: Organizational taxonomy for content classification

## Integration Patterns

### YouTube API Integration
- **Metadata Fetching**: Automatic retrieval of video details
- **Rate Limiting**: Respects YouTube API quotas
- **Batch Processing**: Efficient handling of multiple video requests
- **Error Handling**: Graceful degradation when API limits are reached

### Translation Service Pattern
```csharp
// Translation Workflow
VideoIds[] → TranslationRequest → Azure Translator → TranslatedContent → Database Update
```

### Social Media Integration
- **Configuration Services**: `IDiscordConfigurationService`, `ITelegramConfigurationService`
- **HTTP Client Factories**: Centralized client creation for external services
- **Settings Management**: Platform-specific configuration handling

## Data Access Patterns

### MongoDB Document Design
- **Document per Aggregate**: YouTubeContent as aggregate root
- **Embedded Objects**: VideoRef embedded within YouTubeContent
- **Collection Strategy**: Separate collections for different entity types
- **Indexing**: Strategic indexes on YoutubeId and Category fields

### Caching Strategy
```
Application Cache → Redis/Memory Cache → Database
├── Match Collections (Video Lists)
├── User Sessions (JWT tokens)
└── External API Responses (YouTube metadata)
```

## Frontend Architecture Patterns

### Monorepo Structure with Shared Models
```
MorWalPizVideo/
├── packages/
│   └── models/              # Shared TypeScript models library
│       ├── src/
│       │   ├── CalendarEvent.ts
│       │   ├── categories.ts
│       │   ├── channel.ts
│       │   ├── configuration.ts
│       │   ├── font.ts
│       │   ├── product.ts
│       │   ├── productCategory.ts
│       │   ├── queryLink.ts
│       │   ├── shortLink.ts
│       │   ├── sponsor.ts
│       │   ├── youTubeVideoLink.ts
│       │   ├── video/
│       │   │   ├── types.ts
│       │   │   └── service.ts
│       │   └── index.ts     # Barrel exports
│       ├── package.json
│       └── tsconfig.json
├── back-office-spa/         # Admin SPA application
│   └── imports from @morwalpizvideo/models
└── morwalpizvideo.client/   # Public client application
    └── imports from @morwalpizvideo/models
```

### Shared Models Package Pattern
- **Single Source of Truth**: All TypeScript models defined once in shared package
- **Version Control**: Centralized model versioning across applications
- **Type Safety**: Consistent types between backend contracts and frontend usage
- **Build Output**: Compiled to `dist/` with ES module exports
- **Import Pattern**: `import { Model } from '@morwalpizvideo/models'`

### React Application Structure
```
BackOfficeSPA/
├── Components/ (Reusable UI components)
├── Routes/ (Page-level components with loaders/actions)
├── Services/ (API communication layer)
└── Layouts/ (Page layout components)
```

### React Router v7 Pattern
- **File-based Routing**: Route definitions in individual files
- **Loaders**: Data fetching before component rendering
- **Actions**: Form submission and mutations
- **Error Boundaries**: Graceful error handling per route

### State Management Pattern
- **Server State**: React Router loaders for data fetching
- **Client State**: Local component state for UI interactions
- **Form State**: React Router actions for mutations
- **No Global State**: Avoiding complexity with simple local state

## API Controller Patterns

### Request Contract Pattern
All BackOffice API controllers follow a contract-based approach that separates API contracts from domain models:

```csharp
// 1. Define Request Contracts (in controller file)
public class CreateEntityRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public NestedContract[] Items { get; set; } = [];
}

public class UpdateEntityRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public NestedContract[] Items { get; set; } = [];
}

// 2. Controller Actions Using Contracts
[HttpPost]
public async Task<ActionResult> Create(BaseRequest<CreateEntityRequest> request)
{
    // Convert contract to domain model
    var entity = new DomainEntity(
        request.Body.Title,
        request.Body.Description,
        request.Body.Url,
        ConvertItems(request.Items)
    );
    
    await _dataService.SaveEntity(entity);
    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
}

[HttpPut("{id}")]
public async Task<ActionResult> Update(BaseRequestId<UpdateEntityRequest> request)
{
    var existing = await _dataService.GetEntityById(request.Id);
    if (existing == null)
        return NotFound();
    
    var updated = existing with
    {
        Title = request.Body.Title,
        Description = request.Body.Description,
        Url = request.Body.Url,
        Items = ConvertItems(request.Body.Items)
    };
    
    await _dataService.UpdateEntity(updated);
    return NoContent();
}

[HttpDelete("{id}")]
public async Task<ActionResult> Delete(BaseRequestId request)
{
    var entity = await _dataService.GetEntityById(request.Id);
    if (entity == null)
        return NotFound();
    
    await _dataService.DeleteEntity(request.Id);
    return NoContent();
}
```

### BaseRequest Helper Classes
Standard request wrappers for consistent parameter binding:

```csharp
// From MorWalPizVideo.MvcHelpers.Utils
public class BaseRequest
{
    // Can include common headers like Environment
}

public class BaseRequest<T> : BaseRequest where T : class, new()
{
    [FromBody]
    public T Body { get; set; }
}

public class BaseRequestId : BaseRequest
{
    [FromRoute]
    [Required]
    public string Id { get; set; } = string.Empty;
}

public class BaseRequestId<T> : BaseRequest<T> where T : class, new()
{
    [FromRoute]
    [Required]
    public string Id { get; set; } = string.Empty;
}
```

### Benefits of Contract Pattern
1. **Separation of Concerns**: API contracts separate from domain models
2. **Automatic Validation**: Data annotations provide built-in validation
3. **Type Safety**: Route parameters properly bound via BaseRequestId
4. **Consistency**: All controllers follow the same pattern
5. **Maintainability**: Easier to evolve API contracts independently of domain
6. **No Breaking Changes**: Domain model changes don't automatically affect API

### Examples
- **ProductsController**: Uses CreateProductRequest, UpdateProductRequest, BaseRequestId
- **CompilationsController**: Uses CreateCompilationRequest, UpdateCompilationRequest, VideoRefContract
- **CalendarEventsController**: Similar pattern with event-specific contracts

## Security Patterns

### Authentication & Authorization
```
JWT Token → Middleware Validation → Role-based Access → Controller Actions
```

- **JWT Service**: Centralized token generation and validation
- **Rate Limiting**: Protection against brute force attacks
- **Login Attempts**: Tracking and monitoring authentication attempts

### API Security
- **CORS Configuration**: Proper cross-origin request handling
- **Input Validation**: Model binding and data annotation validation via request contracts
- **Error Handling**: Consistent error responses without information leakage

## Service Communication Patterns

### Inter-Service Communication
- **HTTP APIs**: RESTful communication between services
- **Shared Libraries**: Common models and utilities
- **Event-Driven**: Cache invalidation and update notifications

### External Service Integration
- **Factory Pattern**: HTTP client factories for external services
- **Circuit Breaker**: Resilience against external service failures
- **Retry Logic**: Automatic retry for transient failures

## Development Patterns

### Dependency Injection
```csharp
// Service Registration Pattern
services.AddScoped<DataService>();
services.AddScoped<IYTService, YTService>();
services.AddScoped<IExternalDataService, ExternalDataService>();
```

### Configuration Management
- **Options Pattern**: Strongly-typed configuration objects
- **Environment-specific**: Development vs Production settings
- **Secret Management**: External configuration for sensitive data

### Testing Patterns
- **Repository Mocking**: MockRepository for unit tests
- **Integration Tests**: Full API testing with test databases
- **Component Testing**: React Testing Library for frontend components

## Deployment & Containerization Patterns

### Docker Multi-Stage Build Pattern
```dockerfile
# Stage 1: Build Stage
FROM node:20-alpine AS builder
- Install dependencies
- Build shared packages (@morwalpizvideo/models)
- Build React SPA application
- Output static files

# Stage 2: Production Stage  
FROM nginx:alpine
- Copy built static files
- Copy nginx configuration
- Runtime environment injection
- Minimal production footprint (~50-80MB)
```

### Containerization Strategy
- **Multi-Stage Builds**: Separate build and runtime environments
- **Alpine Linux**: Minimal base images for security and size
- **Layer Caching**: Optimized Dockerfile for efficient rebuilds
- **Build Context**: Monorepo-aware builds from repository root

### Runtime Configuration Pattern
```bash
# Environment injection at container startup
ENTRYPOINT creates env-config.js with:
- VITE_API_BASE_URL
- API_BASE_URL
- Runtime-configurable API endpoints
```

### Nginx SPA Serving Pattern
```nginx
# SPA Routing: All routes fallback to index.html
location / {
    try_files $uri $uri/ /index.html;
}

# Static Asset Caching: 1 year for immutable files
location ~* \.(js|css|png|jpg|svg|woff|woff2)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}

# Security Headers: XSS protection, content type sniffing
add_header X-Frame-Options "SAMEORIGIN";
add_header X-Content-Type-Options "nosniff";
```

### Azure Deployment Pattern
- **Container Registry (ACR)**: Centralized image storage
- **App Service**: Managed container hosting with automatic scaling
- **Container Instances (ACI)**: Lightweight container deployment
- **Environment Variables**: Platform-level configuration injection

### Health Check Pattern
```nginx
# Health endpoint for monitoring
location /health {
    return 200 "healthy\n";
    access_log off;
}
```

### CI/CD Integration Pattern
```yaml
# GitHub Actions workflow
Build → Test → Push to ACR → Deploy to Azure
- Automated builds on commits
- Tagged versions for releases
- Environment-specific deployments
```

## Performance Patterns

### Caching Strategy
- **Application-level**: In-memory caching for frequently accessed data
- **API-level**: Response caching for expensive operations
- **Database-level**: Query optimization and proper indexing

### Lazy Loading
- **Video Metadata**: Fetch on-demand when needed
- **Image Loading**: Progressive loading for thumbnails
- **Route Splitting**: Code splitting for frontend bundles

## Error Handling Patterns

### Exception Management
```csharp
try 
{
    // Operation
} 
catch (SpecificException ex) 
{
    // Specific handling
} 
catch (Exception ex) 
{
    // General logging and user-friendly response
}
```

### Frontend Error Boundaries
- **Route-level**: Error boundaries for each major route
- **Component-level**: Graceful fallbacks for component failures
- **API Error**: Consistent error messaging and user feedback

These patterns ensure the system remains maintainable, scalable, and follows established software engineering principles while meeting the specific needs of video content management.
