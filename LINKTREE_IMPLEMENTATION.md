# Linktree Implementation Summary

## Overview
The Linktree public feature remains active and has been aligned with the current architecture:

- BackOffice no longer exposes YouTube-link management mutations.
- Public Linktree now resolves link targets with URL-first priority.
- Legacy `youTubeVideoId` is kept only as fallback compatibility data.

## Current Backend Surface

Controller: `MorWalPizVideo.BackOffice/Controllers/YouTubeVideoLinksController.cs`

- `GET /api/YouTubeVideoLinks/{matchId}/links`
  - Returns creator link cards for the requested match.
  - Response shape includes: `shortLinkUrl`, `shortLinkCode`, `shortLinkTarget`, `directVideoUrl`, and legacy `youTubeVideoId`.
- `GET /api/YouTubeVideoLinks/image/{imageName}`
  - Returns creator image bytes (`image/png`) when available.

Removed from active API scope:

- `POST /api/YouTubeVideoLinks/create`
- `DELETE /api/YouTubeVideoLinks/{matchId}/links/{videoId}`

## Link Target Resolution Order

Public Linktree client now resolves target URLs in this order:

1. `shortLinkUrl`
2. `/sl/{shortLinkCode}`
3. `directVideoUrl`
4. `https://www.youtube.com/watch?v={youTubeVideoId}` (legacy fallback)

This is implemented in:

- `frontend/morwalpizvideo.client/src/routes/linktree.tsx`

## Frontend Integration

Service and loader:

- `frontend/morwalpizvideo.client/src/services/linktree.ts`
- `frontend/morwalpizvideo.client/src/routes/linktree.loader.ts`

Route:

- `/linktree/:matchId`

Behavior:

- Match metadata and link cards are loaded in parallel.
- Cards are keyboard-accessible.
- Creator image fallback to initials remains in place.

## Contracts and Types

Updated response contract/type now treats `youTubeVideoId` as optional fallback:

- `MorWalPiz.Contracts/DTOs/YouTubeVideoLinkResponse.cs`
- `frontend/fe-packages/models/src/youTubeVideoLink.ts`

## Notes

- Existing persisted records with only `youTubeVideoId` continue to work.
- New UX and routing assume shortlink/direct-url first, without breaking older data.
