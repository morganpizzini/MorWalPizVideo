# QuickLinks Implementation Summary

## Overview
The public Linktree-style feature is now represented by the generic QuickLinks model:

- BackOffice manages each QuickLinks record independently. A channel may own multiple linktrees, while every normalized slug is unique across all channels.
- The public page is anonymous, standalone, and available at `/quick-links/:url`.
- Supported link kinds are External, Telegram, Instagram, Facebook, and Video.

## Current Backend Surface

BackOffice controller: `MorWalPizVideo.BackOffice/Controllers/QuickLinksController.cs`

- `GET /api/QuickLinks`
- `GET /api/QuickLinks/{id}`
- `POST /api/QuickLinks`
- `PUT /api/QuickLinks/{id}`
- `DELETE /api/QuickLinks/{id}`

Public controller: `MorWalPizVideo.ServerAPI/Controllers/QuickLinksController.cs`

- `GET /api/QuickLinks/{url}`
  - Anonymous read of a normalized shortlink slug.
  - Output-cache tags use the centralized lowercase QuickLinks cache keys.
- QuickLinks records carry a `ChannelId` owner. BackOffice create/update uses the required `X-Channel-Id` scope; the slug remains globally unique across all channel owners.
- The `quicklinks_url.unique` MongoDB index in the approved index manifest provides the database-level uniqueness guard after existing duplicate data has been resolved.
- Shooting ITA reads use the additive `api/shit` controller:
  - `GET /api/shit/channels` returns every channel whose wire property `isSHIT` is true.
  - `GET /api/shit/matches` returns public content whose `VideoRef.ChannelIds` includes one of those channels.
  - `GET /api/shit/quicklinks/{url}` only returns a linktree owned by a channel whose `isSHIT` value is true.
  - All three endpoints derive visibility from persisted channel records. Unflagging a channel revokes its `/api/shit` access, including linktree lookup.

## Frontend Integration

Service and loader:

- `frontend/morwalpizvideo.client/src/services/quickLinks.ts`
- `frontend/morwalpizvideo.client/src/routes/quickLinks/loader.ts`

Route:

- `/quick-links/:url`

Behavior:

- The page renders title, optional subtitle, ordered links, and optional title/subtitle/label/provider/icon/image metadata.
- Links are normal anchors with safe `_blank` and `noopener noreferrer` handling.
- The route is outside the Root shell, so it has no public menu, header, footer, or match-specific legacy behavior.
- Shooting ITA uses `/quick-link/:custom-linktree` and requests `/api/shit/quicklinks/{customLinktree}`. Its channel and content loaders request `/api/shit/channels` and `/api/shit/matches`.
- The HTML renderer is shared as `QuickLinksRenderer` from `@morwalpiz/layout`; MorWalPiz retains its existing styling and Shooting ITA supplies a scoped dark theme.

## Contracts and Types

The shared contract and model are defined in `MorWalPiz.Contracts/Contracts/QuickLinksContract.cs` and `frontend/fe-packages/models/src/quickLinks.ts`.
