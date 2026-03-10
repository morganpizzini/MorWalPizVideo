# Active Context - MorWalPizVideo

## Current Development Focus

As of the latest session (February 13, 2026), development has successfully implemented a comprehensive API Key authentication system to secure communication between the VideoImporter WinForms application and the BackOffice ChatController endpoints. This represents a significant security enhancement to the existing JWT-based authentication infrastructure.

## Recent Development Activity

### API Key Management Frontend Interface (Latest - February 13, 2026)
- **Complete Client Application UI**: Implemented comprehensive API key management interface in `morwalpizvideo.client`
  - **API Service Layer**: `services/apiKeys.js` with full CRUD operations and JWT authentication
  - **List View**: `routes/apiKeys.jsx` - Table display with status badges, quick actions, delete confirmation
  - **Create/Edit Form**: `routes/apiKeyForm.jsx` - Complete form with one-time key display modal after creation
  - **Detail View**: `routes/apiKeyDetail.jsx` - Full key details with regenerate functionality
  - **Router Integration**: Added 4 routes to `main.jsx` (/apikeys, /apikeys/create, /apikeys/:id, /apikeys/:id/edit)
  - **Security Features**: One-time key display, copy-to-clipboard, confirmation modals for destructive actions
  - **User Experience**: Bootstrap-styled responsive interface, loading states, success/error notifications, Italian formatting

- **Key Features**:
  - Full CRUD operations for API keys
  - Toggle active/inactive status
  - **Regenerate API keys** with one-time display modal
  - Rate limit configuration
  - IP whitelisting management
  - Expiration date support
  - Status badges (Active/Inactive/Expired)

- **Access**: Navigate to `/apikeys` to manage API keys (requires JWT authentication)

### API Key Authentication System (February 13, 2026)
- **Complete Authentication Infrastructure**: Implemented end-to-end API key authentication for VideoImporter-to-BackOffice communication
  - **Security Features**: SHA256 hashing, one-time key display, rate limiting, IP whitelisting, expiration dates
  - **Models & Configuration**: Created `ApiKey.cs` model with comprehensive security features
  - **Repository Pattern**: Implemented `IApiKeyRepository`, `ApiKeyRepository` (MongoDB), `ApiKeyMockRepository`
  - **Service Layer**: `ApiKeyService` for key management, `ApiKeyRateLimitingService` for request throttling
  - **Authentication Handler**: ASP.NET Core `ApiKeyAuthenticationHandler` with `[ApiKeyAuth]` attribute
  - **Management API**: Full CRUD operations via `ApiKeysController` (JWT-protected)
  - **Client Integration**: Updated `ApiService.cs` in VideoImporter to send API keys via X-API-Key header
  - **Configuration**: Updated `appsettings.json` and `app.config` with API key settings
  - **Documentation**: Comprehensive `API_KEY_AUTHENTICATION.md` with usage guide and best practices

- **Key Technical Decisions**:
  - API keys use SHA256 hashing for secure storage
  - Rate limiting: 60 requests/minute default (configurable per key)
  - Sliding window implementation using ConcurrentDictionary
  - Optional IP whitelisting with proxy header support (X-Forwarded-For, X-Real-IP)
  - Keys can be activated/deactivated without deletion
  - Separate authentication scheme from existing JWT (both can coexist)

- **Protected Endpoints**: `ChatController` now requires API key authentication via `[ApiKeyAuth]` attribute

- **Breaking Change**: Existing VideoImporter instances require API keys to continue accessing the API

### CompilationsController Refactoring (Latest - December 19, 2025)
- **Request Contract Pattern Applied**: Refactored `CompilationsController.cs` to use contract-based approach
  - Created `VideoRefContract` for simplified video reference input
  - Created `CreateCompilationRequest` for POST operations
  - Created `UpdateCompilationRequest` for PUT operations
  - Updated all actions to use `BaseRequestId` pattern
- **Contract to Domain Conversion**: Proper separation between API contracts and domain models
  - Convert `VideoRefContract[]` to `VideoRef[]` with proper initialization
  - Maintain existing validation logic for video existence
  - Use record `with` expressions for immutable updates
- **Consistency Achievement**: All BackOffice controllers now follow the same pattern
  - ProductsController, CompilationsController use request contracts
  - Automatic validation via data annotations
  - Type-safe route parameter binding
- **Documentation**: Documented the pattern in `systemPatterns.md` with complete examples

### Docker Containerization Setup (December 19, 2025)
- **Multi-Stage Dockerfile**: Created production-ready containerization for back-office-spa
  - Stage 1: Node.js 20 Alpine for building with `@morwalpizvideo/models` dependency
  - Stage 2: Nginx Alpine for serving (~50-80MB final image)
- **Nginx Configuration**: Production web server setup
  - SPA routing with fallback to index.html
  - Static asset caching (1 year for immutable files)
  - Security headers (X-Frame-Options, X-Content-Type-Options, X-XSS-Protection)
  - Gzip compression enabled
  - Health check endpoint at `/health`
- **Runtime Environment Injection**: Dynamic API URL configuration
  - `docker-entrypoint.sh` creates `env-config.js` at startup
  - Supports VITE_API_BASE_URL, API_BASE_URL environment variables
  - No rebuild required to change API endpoints
- **Build Context Optimization**: `.dockerignore` for efficient builds
- **Azure Deployment Documentation**: Complete guide for ACR, App Service, and ACI
- **CI/CD Integration**: GitHub Actions example for automated builds and deployments
- **Comprehensive Documentation**: `DOCKER.md` with build/run/deploy instructions

### Shared Models Library Architecture (December 2025)
- **Workspace Configuration**: Implemented npm workspaces for monorepo structure
- **Shared Models Package**: Created `@morwalpizvideo/models` as centralized TypeScript models library
- **Package Structure**: 
  - `packages/models/` - Shared models with 14 model files
  - TypeScript compilation to `dist/` directory
  - Proper exports configuration for ES modules
- **Model Migration**: Moved all models from `back-office-spa/src/models` to shared package
- **Cross-Application Usage**: Both `back-office-spa` and `morwalpizvideo.client` now import from shared library
- **Import Path Updates**: Changed from `@models` to `@morwalpizvideo/models` across both applications
- **Export Configuration**: Fixed enum exports (ContentType, LinkType) to work as values, not just types
- **Build System**: Configured TypeScript compilation for shared library with proper module resolution

### Products and Sponsors Management System
- **Products API**: Complete CRUD operations in `ProductsController.cs`
- **Product Categories API**: Full management in `ProductCategoriesController.cs`
- **Sponsors API**: Complete endpoints in `SponsorsController.cs`
- **DataService Extensions**: Added CRUD methods for Product, ProductCategory, and Sponsor entities
- **Repository Pattern**: Implemented `IProductCategoryRepository` interface
- **Category Validation**: Products validate category references before creation/update

### Video Translation System
- **Video Translation Controller**: Enhanced `VideosController.cs` with translation endpoints
- **Translation DTOs**: `VideoTranslationRequest.cs` and `VideoTranslationResponse.cs` for API contracts
- **Frontend Integration**: React components for video translation workflow
- **Azure Translator Integration**: Service integration for automated content translation

### Router Architecture Refactoring (Latest)
- **Modular Router Structure**: Split monolithic `router.ts` (400+ lines) into organized modules
- **Router Utilities**: Created reusable functions for route configuration (`createErrorElement`, `createRouteGroup`)
- **Type Safety**: Centralized TypeScript types for route configurations
- **Documentation**: Comprehensive README explaining router structure and conventions
- **Bug Fix**: Removed duplicate `querylinks` route definition
- **File Organization**: 
  - `src/router/types.ts` - Type definitions
  - `src/router/utils.ts` - Utility functions
  - `src/router/routes/auth.routes.ts` - Authentication routes
  - `src/router/routes/index.ts` - Protected routes (all features)
  - `src/router/README.md` - Complete documentation

### Frontend Architecture Improvements
- **React Router v7**: Migration to latest version with file-based routing
- **Component Organization**: Structured component hierarchy with proper separation
- **TypeScript Integration**: Full type safety across the frontend application
- **Bootstrap Integration**: Consistent UI framework implementation

### Desktop Application Development
- **WPF Video Importer**: Active development of Windows desktop application
- **Video Translation Dialog**: UI for bulk translation operations
- **API Service Integration**: Connection between desktop app and web API

## Deployment & Infrastructure

### Docker Containerization
**Current State**: Production-ready Docker setup for back-office-spa
- Multi-stage Dockerfile with optimized layers
- Nginx-based serving with SPA routing support
- Runtime environment variable injection
- Comprehensive deployment documentation

**Key Files**:
- `back-office-spa/Dockerfile` - Multi-stage build configuration
- `back-office-spa/nginx.conf` - Production web server setup
- `back-office-spa/.dockerignore` - Build context optimization
- `back-office-spa/docker-entrypoint.sh` - Runtime configuration script
- `back-office-spa/DOCKER.md` - Complete deployment guide

**Azure Deployment Ready**:
- Azure Container Registry (ACR) integration
- Azure App Service deployment patterns
- Azure Container Instances (ACI) support
- Environment variable configuration at platform level

**Build Command**:
```bash
# From monorepo root
docker build -f back-office-spa/Dockerfile -t back-office-spa:latest .
```

**Run Command**:
```bash
docker run -d -p 8080:80 \
  -e VITE_API_BASE_URL=https://api.example.com \
  back-office-spa:latest
```

## Key Working Areas

### 1. Video Management Workflow
**Current State**: Core video operations are functional but being refined
- Video import from YouTube IDs
- Collection creation and management
- Thumbnail swapping functionality
- Category-based organization

**Active Improvements**:
- Enhanced metadata fetching from YouTube API
- Better error handling for API rate limits
- Improved batch processing capabilities

### 2. Translation System Enhancement
**Current Implementation**: Basic translation workflow exists
- Integration with Azure Translator service
- Batch translation capabilities
- Translation request/response handling

**Active Development**:
- Frontend UI for translation management
- Desktop application translation features
- Translation status tracking and progress indicators

### 3. Social Media Integration
**Current Features**: Basic Discord and Telegram integration
- Configuration services for social platforms
- HTTP client factories for external API calls
- Settings management for platform credentials

**Ongoing Work**:
- Enhanced error handling for social media APIs
- Improved configuration management
- Better integration workflows

## Router Architecture Pattern

### Modular Route Structure
```
router/
├── types.ts           # Route configuration types
├── utils.ts           # Helper functions (createErrorElement, createRouteGroup)
└── routes/
    ├── index.ts       # All protected routes (organized by feature)
    └── auth.routes.ts # Public authentication routes
```

### Route Organization Benefits
1. **Maintainability**: Routes grouped logically by feature (Videos, Products, Categories, etc.)
2. **Scalability**: Easy to add new routes without modifying existing code
3. **Consistency**: Utility functions ensure uniform route structure
4. **Discoverability**: Clear separation between public and protected routes
5. **Documentation**: Inline comments and comprehensive README

### Adding New Routes
```typescript
// In src/router/routes/index.ts
createRouteGroup('newfeature', {
  action: NewFeature.action,
  children: [
    { index: true, path: '', loader: NewFeature.loader, Component: NewFeature.Component },
    { path: 'create', Component: NewFeatureCreate.Component, action: NewFeatureCreate.action },
    // ... more child routes
  ],
})
```

## Important Current Patterns

### Data Flow Architecture
```
Frontend (React) → BackOffice API → Domain Services → MongoDB
                                 ↓
                    External APIs (YouTube, Azure Translator)
```

### Products Management Pattern
```
Product Entity
├── Title (string)
├── Description (string)
├── Url (string)
└── Categories (CategoryRef[])
    └── Validated against ProductCategory collection
```

### Video Entity Management
- **YouTubeContent**: Primary aggregate for video collections
- **VideoRef**: Lightweight references within collections
- **Video**: Complete metadata entities
- **Category-based Organization**: Hierarchical content structure

### Authentication Flow
```
User Login → JWT Generation → Token Storage → API Authorization → Protected Resources
```

## Active Technical Decisions

### Frontend State Management
- **Decision**: Using React Router loaders instead of global state management
- **Rationale**: Simplifies data flow and reduces complexity
- **Implementation**: Loaders for data fetching, actions for mutations

### Database Strategy
- **Decision**: MongoDB document-based storage with embedded references
- **Rationale**: Flexible schema for video metadata and relationships
- **Implementation**: YouTubeContent as aggregate root with VideoRef arrays

### API Design Pattern
- **Decision**: RESTful APIs with clear resource-based endpoints
- **Rationale**: Standard, predictable API structure
- **Implementation**: Separate controllers for different domain areas

## Current Challenges & Solutions

### YouTube API Rate Limiting
- **Challenge**: Managing YouTube API quota consumption
- **Current Approach**: Efficient batching and caching strategies
- **Next Steps**: Implementation of smart retry logic and quota monitoring

### Translation Cost Management
- **Challenge**: Controlling Azure Translator service costs
- **Current Approach**: Batch processing and selective translation
- **Next Steps**: Translation caching and cost tracking features

### Desktop-Web Integration
- **Challenge**: Synchronizing data between desktop app and web interface
- **Current Approach**: Shared API layer with consistent data models
- **Next Steps**: Real-time sync capabilities and conflict resolution

## Immediate Next Steps

### High Priority Items
1. **Products Frontend Implementation**: Create TypeScript models, API services, and React routes for Products/Sponsors
2. **Translation UI Enhancement**: Improve user experience for translation workflows
3. **Error Handling Improvement**: Better error messages and recovery mechanisms
4. **Desktop App Refinement**: Polish WPF application user interface

### Medium Priority Items
1. **Performance Optimization**: Database query optimization and caching improvements
2. **Security Enhancement**: Additional authentication and authorization features
3. **Monitoring Implementation**: Application health checks and logging improvements
4. **Documentation Updates**: API documentation and user guides

## Development Environment Notes

### Active Configuration
- **Backend**: Running on `MorWalPizVideo.BackOffice` project
- **Frontend**: React development server with Vite
- **Database**: MongoDB with local development instance
- **External APIs**: YouTube Data API v3, Azure Translator

### Current Dependencies
- **Major Updates**: React 19, .NET 8, React Router v7
- **Package Management**: npm for frontend, NuGet for backend
- **Development Tools**: Visual Studio Code, Visual Studio 2022

### Recent File Changes
- **API Key Management Frontend** (February 13, 2026):
  - Created `services/apiKeys.js` - API service layer
  - Created `routes/apiKeys.jsx` and `apiKeys.loader.js` - List view
  - Created `routes/apiKeyForm.jsx` and `apiKeyForm.loader.js` - Create/Edit form
  - Created `routes/apiKeyDetail.jsx` and `apiKeyDetail.loader.js` - Detail view
  - Updated `main.jsx` - Added 4 API key routes
- **Router Refactoring**: 
  - Created modular router structure with 6 new files
  - Reduced main `router.ts` from 400+ to 20 lines
  - Fixed duplicate querylinks route bug
  - Added comprehensive router documentation
- **New Controllers**: `ProductsController.cs`, `ProductCategoriesController.cs`, `SponsorsController.cs`
- **DataService Updates**: Extended with Product, ProductCategory, and Sponsor CRUD methods
- **Repository Interfaces**: Added `IProductCategoryRepository` to `IRepository.cs`
- Video translation components in React frontend
- Controller enhancements for video operations
- Desktop application dialog improvements

## Project Health Status

- **Backend APIs**: Stable and functional
- **Frontend Application**: Active development, mostly stable
- **Desktop Application**: In development, core features working
- **External Integrations**: Functional with room for improvement
- **Database Schema**: Stable, minor enhancements ongoing

The project is in a healthy development state with clear direction and active progress across all major components. Focus remains on enhancing user experience and system reliability.
