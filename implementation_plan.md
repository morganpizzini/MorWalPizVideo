# ChannelNews Implementation Plan

## Canonical behavior

`ChannelNews` is a channel-scoped editorial aggregate with title, subtitle, sanitized HTML body, slug, ordered images (maximum 10), status (`Draft`, `Scheduled`, `Published`, `Archived`), UTC publication time, display order, and audit timestamps. Scheduled visibility is evaluated at read time; no background job is required.

`MorWalPizVideo.Models` owns the entity, status, image metadata, collection name, `CacheKeys.ChannelNews` reset identity, and lowercase `ApiTagCacheKeys.ChannelNews` output tag. `MorWalPizVideo.Domain` owns repositories, normalization, and HTML allowlist sanitization. `MorWalPiz.Contracts` owns admin/public contracts. BackOffice owns authenticated channel-scoped mutations and media lifecycle; ServerAPI owns the anonymous public feed.

## Routes and contracts

BackOffice routes are `/api/ChannelNews`, `/api/ChannelNews/{id}`, `/api/ChannelNews/{id}/status`, `/api/ChannelNews/{id}/images`, and `/api/ChannelNews/{id}/images/{imageIndex}`. CRUD and status requests use `ChannelNewsRequest` and `ChannelNewsStatusRequest`; responses use `ChannelNewsContract`, whose image metadata includes public URL, content type, dimensions, alt text, and display order. The SPA uses the selected `X-Channel-Id`; server authorization and tenancy remain authoritative.

The public routes are `/api/shit/channelnews` and `/api/shit/channelnews/{id-or-slug}`. Only `IsSHIT` channels and `Published` or due `Scheduled` items are returned. `ChannelNewsPublicContract` includes channel identity/logo, sanitized HTML, ordered public image metadata, and publication data, but never storage keys or admin timestamps. Missing channel logos use `/images/logo-150.png`.

## WYSIWYG and media rules

The BackOffice form uses a native `contentEditable` WYSIWYG surface with a small formatting toolbar and submits its HTML through the existing form action. The server allowlist sanitizer is always applied before persistence and public output; client editing is not a security boundary.

Images are selected in the SPA, previewed in server order with metadata, uploaded in batches, and deleted by index. The server decodes and resizes without cropping, distortion, or upscaling: the long side is at most 1920px (landscape bound 1920x1080, portrait bound 1080x1920). There is no application byte-size limit; transport/blob limits remain operational concerns. Channel logos are independent PNG uploads, proportionally resized to a maximum 500px width without upscaling. A logo failure does not roll back channel `IsSHIT` or other channel presentation changes.

## Cache and validation

ChannelNews mutations reset the internal `CacheKeys.ChannelNews` entry and purge the public `ApiTagCacheKeys.ChannelNews` tag. Channel create/update/delete, `IsSHIT`, logo, and presentation mutations reset their normal channel/match/quick-link keys plus the ChannelNews reset key and purge the corresponding public tag, including `ApiTagCacheKeys.ChannelNews`.

Focused coverage covers sanitization, dimensions/count, logo validation, public filtering/fallback, BackOffice authorization/tenancy/status/media, cache identities, contracts, and SPA form/editor/image rendering. Affected builds and focused tests are the completion checks; unrelated baseline warnings/failures are reported rather than changed.