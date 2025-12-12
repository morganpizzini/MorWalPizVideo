# Project Brief - MorWalPizVideo

## Project Overview

MorWalPizVideo is a comprehensive video management and content platform built around YouTube video aggregation, translation, and social media integration. The system serves as a content management hub for organizing, translating, and distributing video content across multiple platforms.

## Core Business Purpose

**Primary Goal**: Manage and organize YouTube video collections with advanced features for content creators and managers who need to:
- Import and categorize YouTube videos
- Translate video content for international audiences  
- Create video collections and manage hierarchical video structures
- Distribute content through short links and social media integration
- Provide back-office administration tools

## Key Stakeholders

- **Content Creators**: Users who manage video collections and need translation services
- **Content Managers**: Administrators who organize and categorize video content
- **Social Media Managers**: Users who distribute content through various channels
- **International Audiences**: End users consuming translated content

## Technical Scope

### Core Components
1. **BackOffice Management System** - Web-based admin interface for content management
2. **Video Import & Translation Services** - YouTube integration and content translation
3. **Short Links Management** - URL shortening and tracking system
4. **Social Media Integration** - Discord, Telegram, and other platform connections
5. **Desktop Video Importer** - Windows WPF application for bulk video operations

### Technology Stack
- **Backend**: .NET 8+ with ASP.NET Core Web APIs
- **Frontend**: React 19+ with TypeScript, Bootstrap, React Router v7
- **Desktop App**: WPF (Windows Presentation Foundation)
- **Database**: MongoDB for document storage
- **External APIs**: YouTube Data API, Azure Translator Service
- **Social Platforms**: Discord, Telegram integration

## Success Criteria

1. **Video Management**: Efficiently import and organize YouTube videos into categorized collections
2. **Translation Workflow**: Seamless translation of video metadata and content
3. **Content Distribution**: Easy sharing through short links and social media platforms
4. **User Experience**: Intuitive admin interface for content management operations
5. **Performance**: Fast video metadata retrieval and responsive user interfaces

## Key Features

### Video Operations
- Import individual YouTube videos by ID
- Create video collections (root videos with sub-videos)
- Convert single videos into collection roots
- Swap thumbnail videos for collections
- Automatic metadata fetching from YouTube API

### Translation System
- Translate video titles and descriptions
- Support for multiple target languages
- Integration with Azure Translator service
- Batch translation capabilities

### Content Management
- Categorization and tagging system
- Image upload and management
- Short link creation and management
- User authentication and authorization

### Social Integration
- Discord bot connectivity
- Telegram channel management
- Bio links and profile management
- Query links for content discovery

## Project Constraints

- **Platform Dependency**: Heavy reliance on YouTube API quotas and rate limits
- **Translation Costs**: Azure Translator service usage costs
- **Desktop Compatibility**: Video Importer limited to Windows environments
- **Database**: MongoDB document structure requires careful schema management
- **Authentication**: JWT-based security across multiple applications

## Future Considerations

- Multi-platform desktop applications (beyond Windows)
- Additional social media platform integrations
- Enhanced analytics and reporting features
- Content recommendation algorithms
- API rate limiting and caching improvements
