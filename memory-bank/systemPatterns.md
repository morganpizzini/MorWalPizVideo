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

### React Application Structure
```
BackOfficeSPA/
├── Components/ (Reusable UI components)
├── Routes/ (Page-level components with loaders/actions)
├── Services/ (API communication layer)
├── Models/ (TypeScript type definitions)
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
- **Input Validation**: Model binding and data annotation validation
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
