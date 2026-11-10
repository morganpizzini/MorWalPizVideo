# QuickLinks Implementation Summary

## Overview
The public Linktree-style feature is now represented by the generic QuickLinks model:

- BackOffice manages each QuickLinks record independently. A channel may own multiple linktrees, while every normalized slug is unique across all channels.
- The public page is anonymous, standalone, and available at `/quick-links/:url`.
- Supported link kinds are External, Telegram, Instagram, Facebook, Video, and YouTube.
  Existing Video links continue to support YouTube and Vimeo; the explicit
  YouTube kind accepts YouTube hosts only.

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
- Shooting ITA accepts `/quick-links/:url` and preserves `/quick-link/:custom-linktree` as a compatibility alias. Both request `/api/shit/quicklinks/{slug}`. Its channel and content loaders request `/api/shit/channels` and `/api/shit/matches`.
- The HTML renderer is shared as `QuickLinksRenderer` from `@morwalpiz/layout`; MorWalPiz retains its existing styling and Shooting ITA supplies a scoped dark theme.
- The renderer provides kind-based default icons for social/video destinations.
  Configured images and custom icons take precedence over the default icon;
  no URL-based kind inference is performed.

### BackOffice public destinations

Configure the optional **Public website base URL** on channel creation/editing
(including self-service editing). `publicBaseUrl` is the public HTTP/HTTPS
origin, optionally with a port, not the short-link prefix or API host. The server
rejects credentials, non-root paths, query strings, and fragments and stores a
canonical origin without a trailing slash. Omitted/null update input preserves
the value; blank input clears it. Existing channels need no migration and
remain unconfigured until an authorized user sets an origin.

QuickLinks detail displays the absolute
`{publicBaseUrl}/quick-links/{encodedSlug}` as a safe new-tab anchor only when
the selected accessible channel owns that page. Missing/invalid configuration,
missing ownership, or a selection mismatch produces an explicit non-clickable
message, never an admin-relative fallback. Normal router revalidation refreshes
the channel context after a save. Deploy channel-writing APIs before the SPA
and configure only origins whose public deployment serves the route; this
change does not provision hosts or verify deployed reachability.

## Contracts and Types

The shared contract and model are defined in `MorWalPiz.Contracts/Contracts/QuickLinksContract.cs` and `frontend/fe-packages/models/src/quickLinks.ts`.
