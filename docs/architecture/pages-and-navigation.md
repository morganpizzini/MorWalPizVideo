# Pages and Navigation

This document is the current feature reference for the Pages and Navigation slices.
Runtime controllers, contracts, and the current Mongo index manifest are authoritative
when this document and an older sample disagree.

## Behavior and Ownership

- The public browser route is `/pages/{url}`. It does not carry public channel identity.
  ServerAPI resolves the URL through anonymous `GET /api/pages/{url}` and returns only
  `PagePublicContract`, which omits `ChannelId` and `Status`.
- Page URLs are normalized by trimming whitespace and surrounding slashes, then applying
  invariant lowercase. Valid URLs are ASCII slugs from 1 to 120 characters. They are
  globally unique across channel owners through the `pages_url.unique` manifest entry,
  collection `pages`, and unique index `ux_pages_url_ci`.
- BackOffice page administration is channel-scoped. The selected channel is sent as
  `X-Channel-Id`; page reads, writes, image operations, and navigation page references
  are restricted to that channel. A duplicate URL in another channel is a conflict even
  though administrative ownership remains channel-specific.
- `Draft` pages are available to authorized BackOffice users only. Only `Published`
  pages can be resolved publicly or projected into public navigation.

## Navigation Rules

- `ChannelNavigation` is owned by a channel, with one configuration per channel through
  `navigation_channel.unique`. The single-site public application has one active public
  navigation: `GET /api/navigation` returns a conflict if more than one active channel
  navigation exists.
- Header and footer items preserve `displayOrder`. Footer items also have a zero-based
  `column`; the configured footer column count is between 1 and 8.
- Item types are `Page`, `Internal`, and `External`. Page items reference a page in the
  selected channel and become `/pages/{url}` links publicly. Internal targets must start
  with one slash and contain no backslash. External targets must be absolute `http` or
  `https` URLs without credentials and open in a new tab.
- Public projection filters page items to published pages. A missing, draft, or otherwise
  unavailable page reference is omitted; internal and external items remain in their
  configured order and footer column.

## Page Content and Media

- Page metadata is stored in MongoDB. The page record includes title, normalized URL,
  status, HTML content, optional thumbnail URL and video ID, timestamps, and inline image
  metadata. Uploaded image bytes are stored in Azure Blob Storage under the configured
  `BlobStorage:PageContainerName`; Mongo stores the storage key, public URL, content type,
  dimensions, and alt text.
- Page image uploads use `POST /api/pages/{id}/images` and deletions use
  `DELETE /api/pages/{id}/images/{imageIndex}`. Images are auto-oriented and encoded as
  JPEG. The processor resizes proportionally to a maximum 1920-pixel long side and does
  not upscale smaller images.
- Deleting a page removes the Mongo page, removes page-reference items from every
  navigation configuration, and deletes all of the page's inline image blobs. Deleting an
  individual image removes its blob after updating the Mongo metadata.

## HTML Sanitization

Page HTML is sanitized before persistence. The tag allowlist is:

`p`, `br`, `strong`, `em`, `u`, `s`, `ol`, `ul`, `li`, `a`, `h2`, `h3`, `h4`,
`blockquote`, `div`, `figure`, `figcaption`, and `img`.

Allowed attributes are deliberately narrow: safe `href` values on links, `_blank` or
`_self` link targets, the exact `noopener noreferrer` relation, `alt` on images, and
`class` values `page-columns` or `page-column` on `div` elements. Image `src` values
must be absolute HTTP(S) URLs without credentials and must match an image public URL
already registered in the page's inline image metadata. Scripts, styles, embedded content,
event handlers, unsafe links, and unapproved image URLs are removed.

## API and Contracts

### BackOffice

The protected, channel-scoped controller routes are:

| Method | Route | Contract or purpose |
| --- | --- | --- |
| GET | `/api/pages` | List `PageContract` values for the selected channel. |
| GET | `/api/pages/{id}` | Read one channel-owned `PageContract`. |
| POST | `/api/pages` | Create from `PageRequest`; returns `PageContract`. |
| PUT | `/api/pages/{id}` | Update from `PageRequest`; returns `PageContract`. |
| DELETE | `/api/pages/{id}` | Delete the page and its dependent references/media. |
| POST | `/api/pages/{id}/images` | Multipart image upload; returns `PageImageContract` values. |
| DELETE | `/api/pages/{id}/images/{imageIndex}` | Delete one image; returns remaining `PageImageContract` values. |
| GET | `/api/navigation` | Read the selected channel's `ChannelNavigationContract`. |
| PUT | `/api/navigation` | Save the selected channel's `NavigationRequest`; returns `ChannelNavigationContract`. |

### Public ServerAPI

| Method | Route | Contract or purpose |
| --- | --- | --- |
| GET | `/api/pages/{url}` | Anonymous published page lookup; returns `PagePublicContract` or 404. |
| GET | `/api/navigation` | Anonymous single-site public navigation; returns `PublicNavigationContract`, null, or 409 when misconfigured. |

`PageContract` and `PagePublicContract` intentionally differ: the public contract does
not expose channel ownership, status, or storage keys. Navigation responses expose page,
internal, and external target URLs; public external items are marked `OpenInNewTab`.

## Frontend Surfaces

- The BackOffice SPA exposes `/pages`, `/pages/create`, `/pages/:id`, and
  `/pages/:id/edit`, plus `/navigation`. The page editor handles title, public slug,
  Draft/Published status, thumbnail/video fields, sanitized rich HTML, image upload and
  insertion, image previews, and image deletion. The navigation editor manages active
  state, header/footer order, footer columns, and all three item types.
- The public frontend loads `/pages/:url` through the page API loader. The root layout
  fetches public navigation once through its navigation provider. Header and footer render
  their configured items, and the home view also renders the configured header items.
  Page links use client routing; external links use a new tab with `noopener noreferrer`.

## Cache and Index Operations

- Public page reads use the `Pages` output-cache tag and vary by the `url` route value.
  Public navigation uses the `Navigation` output-cache tag.
- Page mutations reset and purge both page and navigation cache tags because page status,
  URL, deletion, and navigation references can affect both public surfaces. Navigation
  mutations reset and purge the navigation tags.
- Indexes are not created at startup. Run the authenticated Mongo index audit/apply
  operation during a maintenance window after resolving duplicate normalized URLs. The
  current page authority is `pages_url.unique` / `ux_pages_url_ci`. The legacy
  `pages_url` / `ix_pages_url` pair is a removal key only and is not a current
  apply key.