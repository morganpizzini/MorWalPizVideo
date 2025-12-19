# Technical Context - MorWalPizVideo

## Technology Stack Overview

MorWalPizVideo is built using modern Microsoft .NET technologies with React for the frontend, designed for scalability, maintainability, and developer productivity.

## Backend Technologies

### .NET Ecosystem
- **.NET 8+**: Latest LTS version with improved performance and features
- **ASP.NET Core Web API**: RESTful API development framework
- **Entity Framework Core**: ORM for database operations (MongoDB provider)
- **Dependency Injection**: Built-in IoC container for service management

### Database & Storage
- **MongoDB**: Document database for flexible schema and scalability
- **MongoDB.Driver**: Official .NET driver for MongoDB operations
- **BSON Serialization**: Efficient document serialization/deserialization
- **Azure Blob Storage**: External file and image storage

### External Services Integration
- **YouTube Data API v3**: Video metadata retrieval and management
- **Azure Translator Service**: AI-powered text translation
- **Discord API**: Bot integration for social media management
- **Telegram Bot API**: Messaging and channel management

## Frontend Technologies

### React Ecosystem
- **React 19**: Latest version with improved concurrent features
- **TypeScript**: Type-safe JavaScript development
- **React Router v7**: File-based routing with loaders and actions
- **React Bootstrap**: UI component library for consistent design

### Development Tools
- **Vite**: Fast build tool and development server
- **ESLint**: Code linting and quality enforcement
- **Prettier**: Code formatting and style consistency
- **Husky**: Git hooks for quality gates

### Testing & Quality
- **TypeScript Compiler**: Type checking and compilation
- **Jest/Vitest**: Unit testing framework
- **React Testing Library**: Component testing utilities
- **Lint-staged**: Pre-commit code quality checks

## Desktop Application

### WPF (Windows Presentation Foundation)
- **.NET 8 WPF**: Modern desktop application framework
- **XAML**: Declarative UI markup
- **MVVM Pattern**: Model-View-ViewModel architecture
- **Data Binding**: Two-way data binding for reactive UIs

## Development Environment

### IDE & Editors
- **Visual Studio 2022**: Primary IDE for .NET development
- **Visual Studio Code**: Preferred editor for frontend development
- **IntelliSense**: Advanced code completion and navigation

### Package Management
- **NuGet**: .NET package management
- **npm**: Node.js package management with workspaces support
- **npm Workspaces**: Monorepo package management for shared libraries
- **MSBuild**: .NET build system
- **dotnet CLI**: Command-line interface for .NET operations

### Monorepo Structure
- **Shared Models Package**: `@morwalpizvideo/models` - Centralized TypeScript models
- **Workspace Configuration**: Root `package.json` manages multiple packages
- **Package Organization**:
  - `packages/models/` - Shared TypeScript type definitions
  - `back-office-spa/` - Admin SPA application
  - `morwalpizvideo.client/` - Public client application
- **Build Pipeline**: TypeScript compilation to distributable ES modules
- **Version Management**: Single source of truth for model versions

### Version Control
- **Git**: Distributed version control
- **GitHub**: Repository hosting and collaboration
- **GitHub Actions**: CI/CD pipeline automation

## Configuration & Deployment

### Configuration Management
```json
{
  "ConnectionStrings": {
    "MongoDB": "connection-string"
  },
  "YouTubeApi": {
    "ApiKey": "api-key",
    "QuotaLimit": 10000
  },
  "AzureTranslator": {
    "SubscriptionKey": "key",
    "Endpoint": "endpoint-url"
  }
}
```

### Environment Setup
- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development overrides
- **appsettings.Production.json**: Production settings
- **User Secrets**: Local development secrets
- **Azure Key Vault**: Production secret management

### Deployment Technologies
- **Docker**: Containerization for consistent deployments
- **Azure App Service**: Cloud hosting platform
- **Azure Container Registry**: Docker image storage
- **GitHub Actions**: Automated CI/CD pipelines

## API Design Patterns

### RESTful API Structure
```
GET    /api/videos              # List all videos
POST   /api/videos/import       # Import new video
PUT    /api/videos/{id}         # Update video
DELETE /api/videos/{id}         # Delete video
POST   /api/videos/translate    # Translate videos
```

### Request/Response Patterns
- **DTOs**: Data Transfer Objects for API contracts
- **Model Binding**: Automatic request deserialization
- **Action Results**: Standardized response types
- **Error Handling**: Consistent error response format

## Database Schema Design

### MongoDB Collections
```javascript
// YouTubeContent Collection
{
  "_id": ObjectId,
  "youtubeId": "string",
  "title": "string", 
  "description": "string",
  "url": "string",
  "category": "string",
  "thumbnailVideoId": "string",
  "videoRefs": [
    {
      "youtubeId": "string",
      "category": "string",
      "title": "string",
      "description": "string",
      "publishedAt": "date"
    }
  ]
}

// Users Collection
{
  "_id": ObjectId,
  "username": "string",
  "email": "string", 
  "passwordHash": "string",
  "role": "string",
  "lastLogin": "datetime"
}
```

### Indexing Strategy
- **Primary Keys**: `_id` fields (automatic)
- **Unique Indexes**: `youtubeId` for video identification
- **Compound Indexes**: `category + youtubeId` for filtering
- **Text Indexes**: Full-text search on titles and descriptions

## Security Implementation

### Authentication & Authorization
- **JWT Tokens**: Stateless authentication
- **Role-based Access**: Admin, Manager, User roles
- **Password Hashing**: BCrypt for secure password storage
- **Rate Limiting**: API throttling and abuse prevention

### Data Protection
- **HTTPS**: TLS encryption for all communications
- **CORS**: Cross-Origin Resource Sharing configuration
- **Input Validation**: Data annotation and model validation
- **SQL Injection Prevention**: Parameterized queries (MongoDB safe)

## Performance Optimization

### Caching Strategies
- **Memory Cache**: In-process caching for frequently accessed data
- **Response Caching**: HTTP response caching middleware
- **Cache Invalidation**: Smart cache clearing on data updates

### Database Optimization
- **Connection Pooling**: Efficient database connection management
- **Query Optimization**: Efficient MongoDB query patterns
- **Batch Operations**: Bulk inserts and updates where possible

### Frontend Optimization
- **Code Splitting**: Lazy loading of route components
- **Tree Shaking**: Elimination of unused code
- **Bundle Optimization**: Minimized JavaScript bundles
- **Asset Optimization**: Compressed images and static assets

## Development Workflow

### Local Development Setup
```bash
# Backend setup
dotnet restore
dotnet build
dotnet run --project MorWalPizVideo.BackOffice

# Frontend setup
cd BackOfficeSPA/back-office-spa
npm install
npm run dev

# Desktop app
dotnet run --project MorWalPiz.VideoImporter
```

### Testing Strategy
- **Unit Tests**: Individual component testing
- **Integration Tests**: API endpoint testing
- **End-to-End Tests**: Full workflow testing
- **Performance Tests**: Load and stress testing

### Code Quality Tools
- **SonarQube**: Code quality analysis
- **Code Coverage**: Test coverage metrics
- **Static Analysis**: Security vulnerability scanning
- **Dependency Scanning**: Package vulnerability checks

## Monitoring & Logging

### Application Monitoring
- **Application Insights**: Performance and error monitoring
- **Health Checks**: Endpoint health monitoring
- **Custom Metrics**: Business-specific metric tracking

### Logging Framework
- **Serilog**: Structured logging framework
- **Log Levels**: Debug, Info, Warning, Error, Critical
- **Log Sinks**: Console, File, Azure Application Insights
- **Request Logging**: HTTP request/response logging

This technical foundation provides a robust, scalable platform that can handle the complex requirements of video content management while maintaining high performance and developer productivity.
