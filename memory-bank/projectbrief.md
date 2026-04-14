# Project Brief - MorWalPizVideo

## Core Purpose
YouTube video content management platform with translation, categorization, and multi-platform distribution.

## Applications
- **MorWalPizVideo.BackOffice** - Admin API (.NET 8, MongoDB)
- **MorWalPizVideo.ServerAPI** - Public API (.NET 8)
- **back-office-spa** - Admin React SPA (React 19, TypeScript, Router v7)
- **morwalpizvideo.client** - Public React client
- **morwalpiz-shop.client** - Digital product shop React client
- **MorWalPiz.VideoImporter** - Desktop WPF bulk import tool

## Core Domains
- **Video Management** - YouTube import, metadata, collections (root + sub-videos)
- **Translation** - Azure Translator for titles/descriptions
- **Social Distribution** - Short links, Discord, Telegram integration
- **Digital Shop** - Product catalog, cart, customer auth
- **Insights** - Content planning, news scanning, topic management
- **Calendar Events** - Event management and scheduling

## Key Integrations
- YouTube Data API v3
- Azure Translator Service
- Discord/Telegram APIs
- MongoDB document storage

## Constraints
- YouTube API quota limits
- Azure Translator costs per character
- Desktop app Windows-only
- JWT auth across all apps