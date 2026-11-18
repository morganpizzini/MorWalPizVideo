# Blog authoring

Consolidated 2026-11-09 from six handoffs; see the
[filename map](../feature-handoffs/README.md#former-handoff-map).
Implementation and validation below are **reported local evidence, not rerun
checks or deployment approval**. The latest delivery record wins over an older
plan. System contracts live in [Structured Blog](../architecture/structured-blog.md)
and [ADR-025](../architecture/adr/ADR-025-blog-media-deletion-and-atomic-publish.md).

## Editor and preview

Recorded delivered 2026-11-05:

- Stable Puck configuration/component identities, current images through
  editor-local context, inline restricted Tiptap rich text/headings, contextual
  settings, and an optional initially hidden sidebar.
- Immutable updates locate current Puck IDs, including column children.
  Canonical version-1 documents, adapters, and public renderer stay unchanged;
  no nested zones, second state store, or dependency upgrade.
- All seven incomplete block types remain selectable. Empty frames require
  at least 72 CSS pixels; initial canvas gutter is 48px top/16px inline.
  Actual toolbar bounds and clickability, not padding, determine acceptance.
  Heading limit is 2,000 characters; settings need keyboard/Escape/focus return.
- Preview reflects unsaved final-character edits without writes or editor
  chrome. It unmounts Puck: caret, selection, and undo continuity are not promised.

Owners: [Editor.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.tsx),
[blog route](../../frontend/back-office-spa/src/routes/blog/), and
[BlogRenderer.tsx](../../frontend/fe-packages/layout/src/components/BlogRenderer.tsx).

Final reported result: **7 files/28 tests**, TypeScript build-mode check and
targeted lint passed, superseding an earlier 27-test run. Pointer tests disable
CSS hit testing and cannot prove geometry. Chromium launch failed on missing
`libatk-1.0.so.0`; typing, drag/history, toolbar clipping/hit testing, narrow
screenshots, keyboard, and zoom remain unverified. No BackOffice Playwright
configuration was established.

## Save controls, tags, and article-local media

Recorded delivered 2026-11-05, with primary-save mechanics replaced below:

- Publishers' primary **Save publishes current edits**; **Save as draft**
  leaves Published unchanged. Nonpublishers receive explicitly labelled draft
  save. Ever-published slug locking remains permanent.
- Cover selection is article-local, not a global media library. Remove cover
  clears only its reference. Create article stays permission-gated in the
  header-right action slot.
- Upload needs update/manage permission; create alone is insufficient.
  A new/dirty article is explicitly saved as draft before an authorized upload.
  Use returned revisions, guard channel changes, report partial draft-save
  success, and retain the file on failure. Uploaded cover selection remains
  unsaved until a subsequent save.
- Tags reuse TagInput, but blog limits are **20 tags x 60 characters**,
  not video's 20 x 32. Preserve batch paste, comma/Enter/blur, removal/Backspace,
  case-insensitive deduplication, and pending-token capture on immediate save.
- Upload limits remain 10,000,000 input bytes, 25 megapixels, one decoded frame,
  alt text 1-300 characters, and 100 images per post.

Reported checked admin build and **64 blog/video-edit/TagInput tests** passed;
lint had three Fast Refresh warnings, formatting passed, detector was empty.
The existing >500 kB chunk warning remains. Browser dropdown clipping/focus,
touch, real upload, and public cover smoke were not performed.

## Cover crop and explicit upload

Crop delivered 2026-11-06; explicit-upload correction delivered 2026-11-07.
Neither record claims deployment.

- New covers use an oriented source-coordinate 16:9 crop, thirds overlay,
  bounded pan/zoom, pointer/touch and keyboard controls, reset/adjust/cancel.
  Export is exactly **1920 x 1080 JPEG**, browser quality 0.9, white transparency
  background. Warn on upscale; animation becomes a still image.
- Optional multipart `purpose=cover` opts into authoritative post-processing
  dimension validation before storage. Unknown purpose returns 400.
  Omitted purpose and existing four-argument service calls preserve general
  article-image behavior. Larger input may be downscaled by the existing
  processor, but stored decoded cover bytes must be exactly Full HD.
  No global cover invariant or existing-media conversion is introduced.
- Selection, crop confirmation, and alt changes perform **zero network
  mutations**. Only visible **Upload** authorizes save-before-upload.
  Confirmation prepares local JPEG/preview only; Adjust invalidates preparation,
  alt edits do not. Busy prevents duplicates; retry retains crop and alt.
- Default alt is the filename stem, not article title or a new metadata field:
  remove the final extension, replace underscore/hyphen runs with spaces, retain
  Unicode, trim to 300 characters, fallback to "Immagine". Each selection gets
  its own default; manual alt wins. Blank submit visibly resolves to the default.
- Original bytes are never a fallback upload. Ignore late decode/export and
  release object URLs on replacement/cancel/post/channel change/unmount.
  Preserve permissions, revisions, partial-save reporting, and blob compensation.

Owners include the [blog route](../../frontend/back-office-spa/src/routes/blog/),
[shared blog service](../../frontend/fe-packages/services/src/blogService.ts),
[BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs),
and [BlogControllerTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs).

Crop evidence: services build, **33 frontend tests across four files**, checked
admin build, backend Release build with zero errors, and **9 controller tests**
passed. Stored-byte decoding, rejection, and server downscale were covered;
canvas tests do not establish browser pixel equality. Two Fast Refresh warnings
and ImageSharp 3.1.12 NU1902/NU1903 advisories remained.
Explicit-upload initially passed 23 form tests before a final assertion;
jsdom blocked the rerun. The later integration record closes that narrow gap:
**25/25 form and cropper tests** passed after dependency repair without
manifest/lockfile changes. Browser raster/EXIF/touch/keyboard/network evidence
and staging remain outstanding.

Deploy compatible API before SPA. Older clients omit purpose. Rolling back the
API removes enforcement but does not invalidate saved JPEGs. Never overwrite or
delete existing media as crop rollback.

## Atomic save and recoverable image deletion

Recorded delivered 2026-11-09; replaces the older two-request primary Save,
not draft-save semantics or the upload partial-success workflow.

- `POST /api/blogposts/save-and-publish` and
  `PUT /api/blogposts/{id}/save-and-publish` require `pages.manage`.
  One request/one persistence revision: existing 12 becomes 13; new publication
  begins at 1. No fallback to two writes. Legacy actions retain semantics.
- Selected-image deletion is distinct from clearing cover selection.
  `DELETE /api/blogposts/{id}/images` accepts a query-encoded opaque `imageId`
  and expected revision; update/manage, channel, and CSRF checks apply.
  Only the server-resolved exact owned key in PageContainerName is deleted.
- Additive bounded `PendingImageDeletionIds` defaults empty for older documents
  and is admin-only. CAS reservation, exact blob deletion, then reload/CAS
  metadata finalization increment revisions in both phases.
  Draft **and Published** covers and nested image/gallery/carousel/column
  references block deletion. All writers preserve pending markers and reject
  new references to them; whole-post deletion is blocked while pending.
- Invalid input returns 400; absent/foreign post or image 404; stale revision,
  reference protection, or finalization conflict 409; unavailable storage gives
  recoverable 503 with confirmed revision/pending state.
  No success before storage and finalization complete. Retry is explicit,
  durable, and treats already-absent bytes as success; a later fully removed
  image returns 404.
- UI requires a separate destructive confirmation, no nested selection button,
  initiation disabled while dirty/busy, and pending items with Retry after reload.

Owners: [BlogPost.cs](../../MorWalPizVideo.Models/Models/BlogPost.cs),
[BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs),
[BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs),
[BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs),
shared models/services, and admin form/transport tests. Resolve owning paths
against the current tree before implementation; consolidation is not a source audit.

Reported focused backend run: **641 passed/2 skipped**; service filter **16
passed**; targeted HTTP regressions **3 passed**. Shared builds, **34 targeted
admin tests**, and checked admin production build passed. These selections
overlap and must not be added. A broader run retained **three generic asyncData
failures** from undici/DOM AbortSignal mismatch; changed tests passed separately.

**Release and rollback gates:** upgrade/drain every writer before enabling
deletion; deploy API before SPA. Never roll back to pending-marker-unaware writers
until all pending operations resolve. If storage is unavailable, retain the
compatible API and roll back SPA only. Deleted bytes cannot be restored by code
or Mongo rollback. Verify Azure soft-delete, versions, snapshots, retention,
per-object purge permissions, and CDN/browser limitations before claiming
permanent erasure.

## Editorial revision counter

The admin `Revision` display is an editorial-save count, separate from the
aggregate's optimistic-concurrency `revision` token:

- Draft creation and create-and-publish start at editorial revision **0**.
- Each later explicit Save, Save as draft, or save-and-publish increments the
  editorial revision exactly once.
- Publish/unpublish alone, image upload, image-deletion reservation/finalization,
  and other non-save writes do not increment the editorial revision.
- The aggregate revision remains the request token and advances for every
  aggregate mutation, preserving compare-and-swap protection.
- Existing articles retain their current displayed value as a baseline. If the
  new field is absent, the admin response uses the aggregate revision; the first
  later mutation persists that baseline atomically. No batch backfill is needed.
- If upload creates a new article before its first explicit Save, the article
  starts at editorial revision 0; that later Save advances it to revision 1.

The public blog contract is unchanged. The admin response additively includes
`editorialRevision`; clients continue submitting the existing `revision` token.
Deploy the compatible API before the admin SPA and drain old API writers before
new writes rely on the additive persisted field.

Recorded delivered 2026-11-18: the admin response now separates the displayed
editorial save count from the aggregate concurrency token. The focused
`BlogServiceTests`/`BlogControllerTests` selection passed **30 tests**.
The targeted BackOffice SPA form/index selection passed **32 tests**, and the
checked admin build passed TypeScript and Vite. Existing ImageSharp package
advisories and the admin large-chunk warning remain. No real-browser visual
capture was performed; drain old API writers before field writes begin.

## Editor links, breadcrumbs, and authenticated 404

Recorded delivered 2026-11-09:

- Interaction links target encoded `/blogposts/{id}/edit` only with
  `pages.update`/`pages.manage`; moderators otherwise see plain text.
  Protected `/blogposts/:id` redirects without another fetch.
- Typed route handles resolve `draft.title` from deferred data via Await/Suspense;
  slug/generic Article fallback, persisted-title revalidation, no breadcrumb
  fetch. Only the ID segment changes; Create remains Create and other flat
  breadcrumbIdentifier consumers retain behavior.
- Unknown routes are a wildcard child of the existing authenticated root,
  explicitly mapped to `backoffice.access`, with one shell/provider/auth loader.
  Anonymous unknown URLs go to login. Known-route 400/401/403/409/500 and entity
  404 are not rewritten.

Owners: [route manifest](../../frontend/back-office-spa/src/router/routes/lazy-index.ts),
[Breadcrumbs](../../frontend/back-office-spa/src/components/Breadcrumbs/),
and [blog interactions](../../frontend/back-office-spa/src/routes/blogInteractions/).
The shared 34-test/admin-build evidence above covers this delivery too.
Direct navigation, alias, host SPA fallback, long/stale titles, and responsive
accessibility remain unverified.

## Persistent article short links

Implemented in the current worktree:

- The saved-article header can create one durable, article-owned short link using
  the article's channel. It stays visible after reload and has an accessible copy
  control; creating it does not save or publish article content.
- The standalone short-link record stores the article ID and channel. The
  BackOffice action requires both blog update/manage and short-link create/manage
  permissions, with the existing administrator requirement for non-YouTube links.
- Draft links remain visible to editors but return 404 publicly until publication.
  Resolution looks up the current article slug by ID and channel, so pre-publication
  slug edits do not stale the link. Unpublished or deleted articles do not redirect
  or accrue clicks.
- Generic short-link APIs cannot create or mutate article-owned links. The admin
  response includes nullable article short-link metadata on article reads.

The existing normalized unique short-code index must be present before rollout,
and the ShortLinks host's configured channel must match the article channel.
Public website origin and short-link base configuration must be valid. Deploy
redirect support before enabling creation; older redirect instances cannot resolve
the new type. No blog-record migration or backfill is required.

Focused evidence: BackOffice/API and Domain build plus **33** targeted tests,
ShortLinks build plus **17** tests, and **34** BackOffice form tests passed.
Browser layout/copy validation and production index/channel configuration remain
deployment checks.

## Remaining work

Browser acceptance is a refinement, not a redesign: use the installed Impeccable
workflow narrowly for the Operate-mode editor, preserving the admin design.
Measure actual toolbar geometry and keyboard/touch behavior rather than treating
detector/jsdom output as visual proof. Independent review is warranted before
destructive media rollout. SPA rollback never automatically unpublishes, deletes
uploads, or reverses genuine author edits.
