# Phase 1 — Data Model: Pepperbox-Style Shooting ITA Portal

This feature does NOT introduce a new entity. It adds one field to the existing `Video` document and reuses the existing `YTChannel` collection for the badge data.

---

## Field addition: `Video.ChannelId` (NEW)

`MorWalPizVideo.Models/Models/Video.cs` (collection `videos`).

| Field | Type | Required | Description |
|---|---|---|---|
| `ChannelId` | `string` | yes for newly-imported videos; may be empty on legacy rows pending the one-time backfill | YouTube channel id (matches `YTChannel.ChannelId`). Stored as `[BsonElement("channelId")]`. Indexed ascending. |

### Invariants

1. **Single channel per video**: `Video.ChannelId` is a single string; a video cannot be owned by more than one channel.
2. **Referential integrity (eventually-consistent)**: every non-empty `ChannelId` SHOULD correspond to a `YTChannel.ChannelId` document. The shooting-ita-frontend loader treats unresolvable ids as "unowned" and filters the video out (FR-016).
3. **Importer obligation**: the WPF `MorWalPiz.VideoImporter` and any other write path that creates `Video` records MUST populate `ChannelId`.
4. **Backfill**: one-time idempotent pass that, for every `Video` with empty `ChannelId`, looks up the `YTChannel` whose embedded `Videos[].VideoId` contains the `Video.YoutubeId` and sets `ChannelId` to that channel's id. Videos with no matching channel are left empty.

### API operation (new)

- `setChannel(youtubeId, channelId)` — `POST /api/videos/{youtubeId}/channel`, admin-only. Idempotent. Validates that `channelId` exists in `ytChannels`. Evicts cache tags `videos` and `matches`.

---

## Reused entity: YTChannel (READ-ONLY in this feature)

`MorWalPizVideo.Models/Models/YTChannel.cs` (collection `ytChannels`). Unchanged. Used by both backend and frontend:

- Backend uses it to validate `ChannelId` on assign.
- shooting-ita-frontend fetches `/api/channels` once per page load and builds an in-memory `Map<channelId, { name, avatarUrl }>`.

Fields used by the SPA:

- `YTChannel.ChannelId`
- `YTChannel.ChannelName` → card/hero badge label.
- `YTChannel.Videos[].VideoId` → used by the backfill and as a fallback owner index if `Video.ChannelId` is missing.
- Channel avatar — if the existing record does not carry an avatar URL, the SPA renders an initials placeholder (FR-011).

---

## Reused entity: Video / VideoRef / YouTubeContent (READ-ONLY otherwise)

`Video` gains `ChannelId` (above); no other schema change.

`YouTubeContent` (the **wrapper** / "match") is **not** a video itself: it groups one or more `VideoRef`s sharing the same context (e.g. a race recap + behind-the-scenes). Sorting and view counts MUST be derived from its embedded `VideoRef`s:

- `match.publishedAt` for sort = `max(match.VideoRefs[].PublishedAt)`.
- `match.views` for "Popular Now" = `sum(Video.Views)` over each `VideoRef.YoutubeId` resolved against the `videos` collection.

`VideoRef` itself is unchanged on disk (it is an embedded snapshot). The SPA joins to `Video.ChannelId` and `Video.Views` via the channel map (built from `YTChannel.Videos`) and via the existing `/api/videos` projection.

FR-016 is enforced in the SPA loader by dropping any match whose `VideoRefs[0].YoutubeId` does not resolve to a channel in the map. FR-017 is enforced in `morwalpizvideo.client` by also filtering to matches whose owning channel equals the configured MorWalPiz channel id.

---

## Reused entity: Category / CategoryRef (READ-ONLY)

The Exclusives derivation looks for a category whose id equals the runtime env `VITE_EXCLUSIVE_CATEGORY_ID`. No new fields.

---

## TypeScript shapes

Additive change to `frontend/fe-packages/models/src/video/types.ts`:

```ts
// existing Video shape gains:
export interface Video {
  // ...existing fields...
  channelId?: string; // optional until backfill completes; required-by-contract for new rows
}
```

Channel shape is already exported by `@morwalpizvideo/models` (used by `morwalpizvideo.client`); reused as-is. No new Shooter shapes are introduced.
