# Progress - MorWalPizVideo

## Shipped ✅
- Core video management (import, collections, root/sub-videos)
- Azure translation integration
- Social distribution (short links, Discord, Telegram)
- Digital product shop (catalog, cart, checkout)
- Insights system (topics, news scanning, content planning)
- Custom forms with dynamic question types
- Desktop WPF bulk importer
- JWT authentication across all apps
- Shared frontend packages (@morwalpizvideo/models, services, layout)
- Docker containerization with nginx
- Azure deployment pipeline
- API key authentication system
- **Shooting ITA — private video gatekeeping** (`IsPrivate` on `YouTubeContent`, 403 gate in controller)
- **Shooting ITA — Stage evaluation API** (`POST /api/competitions/{id}/stages/{n}/evaluations`)
- **Shooting ITA — UserRequests controller** (BackOffice admin endpoint for user request management)
- **Shooting ITA — Web Push backend** (VAPID keys, `WebPushService`, subscribe/unsubscribe endpoints, `PushSubscriptionInfo` model)
- **Shooting ITA — YouTube metadata sync job** (`YouTubeSyncJob` Hangfire recurring job, daily at 03:00 UTC, cron override via `YouTubeSyncCron` config key)
- **Shooting ITA frontend — Skeleton screens** (`SkeletonCard`, `CompetitionsSkeletonGrid` components replacing spinner)
- **Shooting ITA frontend — Status filters** (ButtonGroup filter by `CompetitionStatus` on CompetitionsPage)
- **Shooting ITA frontend — Star rating + evaluation** (`StarRating` component, inline evaluation form per stage in CompetitionDetailPage)
- **Shooting ITA frontend — Push subscription UI** (`PushSubscriptionButton` component with VAPID subscribe/unsubscribe)

## In Progress 🔄
- Products/Sponsors frontend UI completion
- Translation UI improvements
- Performance optimization

## Next Up 📋
- Complete products/sponsors admin routes
- Add to main navigation
- API documentation
- Bulk operations UI enhancements
- Wire `VITE_VAPID_PUBLIC_KEY` env var in `.env` files for push notifications

## Technical Debt ⚠️
- Increase test coverage (current ~70%, target 85%)
- Database query optimization
- Frontend bundle size reduction
- Comprehensive API documentation