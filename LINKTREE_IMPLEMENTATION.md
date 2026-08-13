# QuickLinks Implementation Summary

## Overview
The public Linktree-style feature is now represented by the generic QuickLinks model:

- BackOffice manages one stable QuickLinks ID while administrators edit the slug, display metadata, and ordered links.
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

## Contracts and Types

The shared contract and model are defined in `MorWalPiz.Contracts/Contracts/QuickLinksContract.cs` and `frontend/fe-packages/models/src/quickLinks.ts`.
