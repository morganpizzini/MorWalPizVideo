# API Contract — Video ↔ Channel ownership

This feature adds a single `ChannelId` field to the existing `Video` document and reuses the existing `/api/channels` endpoint (`ChannelsController` in `MorWalPizVideo.BackOffice`) to resolve the owning channel for cards/hero badges. No new controller is introduced.

DTO additions live in `MorWalPiz.Contracts/Contracts/Videos/`.

---

## Field addition — `Video.ChannelId`

- Type: `string` (the YouTube channel id, matching `YTChannel.ChannelId`).
- Required: yes for any newly-imported video; empty allowed only for legacy rows pending the one-time backfill.
- Index: ascending on `channelId` (supports the morwalpizvideo.client filter and the shooting-ita-frontend join).
- Source-of-truth: `MorWalPizVideo.Models/Models/Video.cs` (`[BsonElement("channelId")]`).

Surfaced through:

- `VideoDto.ChannelId` (new field on the existing video DTO returned anywhere a `Video` projection is exposed — additive, backward-compatible).
- `VideoRefDto` / matches projection: enriched server-side at read time by looking up the owning `Video` (`channelId`) for each `VideoRef.YoutubeId`. This keeps `VideoRef` (embedded snapshot) untouched on disk while letting the SPA join client-side without an extra round trip per video.

---

## GET `/api/channels` (existing, reused)

- Returns the list of `YTChannel` documents (id, name, embedded `Videos`).
- The SPA builds, once per page load, a map `youtubeId → { channelId, channelName, avatarUrl }` from the union of (a) `YTChannel.Videos[].VideoId` and (b) `Video.ChannelId` (when included in match projections).
- No contract change to this endpoint. Cache tag remains `channels` (existing).

---

## POST `/api/videos/{youtubeId}/channel`  (new write endpoint)

Set or change the owning channel of an existing `Video`.

- **Controller**: extend the existing `VideosController` in `MorWalPizVideo.BackOffice` (or `MorWalPizVideo.ServerAPI` — whichever hosts the existing video write path; verify during T-impl).
- **Auth**: JWT + API-key middleware with rate limiting (existing admin policy used by `MatchesController` POST endpoints).
- **Request body** (`BaseRequest<VideoChannelAssignmentPayload>`):

  ```json
  { "data": { "channelId": "UC_xxxxxxxx" } }
  ```

- **200 Response**: the updated `VideoDto` including the new `channelId`.
- **400** if `channelId` is empty or not present in the `ytChannels` collection.
- **404** if the video does not exist.
- **Cache eviction** on success: `EvictByTagAsync(CacheKeys.Videos)` AND `EvictByTagAsync(CacheKeys.Matches)` (both lowercase per repo convention) — matches projections embed channel info and must be invalidated together.

---

## DTO source-of-truth

```
MorWalPiz.Contracts/Contracts/Videos/
└── VideoChannelAssignmentPayload.cs   // channelId
```

`VideoDto.ChannelId` is added to the existing DTO in place (additive). TypeScript shapes in `frontend/fe-packages/models/src/video/types.ts` gain an optional `channelId?: string` field (optional only until the backfill completes).

---

## Caching summary

| Tag (lowercase) | Set by | Evicted by |
|---|---|---|
| `channels` (existing) | `GET /api/channels` | the new `POST /api/videos/{id}/channel` AND existing channel-write endpoints |
| `videos` (existing) | video read endpoints | the new `POST /api/videos/{id}/channel` |
| `matches` (existing) | match read endpoints | the new `POST /api/videos/{id}/channel` (matches projections embed channel info) |

The repo-wide rule (`copilot-instructions.md`) that tags MUST be lowercase invariant is satisfied because every constant referenced here is already declared as a lowercase literal in `CacheKeys.cs`.

---

## Backfill (one-time)

Existing `Video` documents predate this field. The implementation includes a one-time idempotent backfill that, for each `Video` whose `ChannelId` is empty, looks up the `YTChannel` whose embedded `Videos[].VideoId` contains the `Video.YoutubeId` and sets `Video.ChannelId` accordingly. Videos with no matching channel are left empty and remain hidden from shooting-ita-frontend (per FR-016) and from morwalpizvideo.client (per FR-017).
