# BackOffice blog: one-save revision and individual media removal

Status: implemented in the worktree on 2026-11-09. Focused validation is recorded
below; deployment and provider-retention verification remain outstanding.

## Implementation record

Implemented atomic save-and-publish routes and service transitions, preserving legacy
draft-save/publish routes. Existing articles advance one revision exactly once; new
published articles are inserted at revision 1. Added bounded `PendingImageDeletionIds`,
reference protection across draft/published nested blocks, channel-scoped authorized
image deletion, exact configured-container/key deletion, recoverable 503 responses,
optimistic finalization, and whole-post deletion protection.

The admin contract and shared client now carry pending IDs. The BackOffice editor uses
one publish request and presents separate per-image destructive confirmation plus
retryable pending state. The public client was not changed.

Modified source: `BlogPost.cs`, `BlogService.cs`, `BlogContracts.cs`,
`BlogPostsController.cs`, `BlogControllerTests.cs`, shared blog models/services,
and BackOffice blog form.
Modified documentation: this handoff, `structured-blog.md`, `backend.md`; added
`ADR-025-blog-media-deletion-and-atomic-publish.md`.

Validation performed: the full focused BackOffice suite passed (641 passed, 2 skipped),
the blog service regression filter passed (16 passed), and the targeted blog HTTP
regressions passed (3 tests) for one-revision publishing, published-reference
protection, and blob-delete failure/retry. Shared frontend packages built; the
targeted BackOffice SPA tests passed (34 tests) and its production build passed.
An additional mixed Vitest run including `asyncData.test.tsx` had 3 failures from
an undici/DOM `AbortSignal` mismatch in those generic route tests; rerunning the
changed form, interactions, and route-manifest tests alone passed. The UI detector
reported no findings. No provider storage inspection was possible here. Azure
soft-delete, versions, snapshots, retention, CDN and browser-cache behavior must
be verified before claiming irreversible erasure.

## Outcome

Make primary Save persist and publish an existing article with exactly one revision
increment, while preserving Save as draft. Add removal of one uploaded article
image, including its stored blob, without conflating it with Remove cover or
breaking published references. Metadata-only removal does not satisfy this request.

Assumptions for review:

- The previously approved primary Save = save-and-publish behavior remains wanted.
  The reported 12-to-14 jump is not a request to stop publishing.

Confirmed decision: Remove image permanently deletes that specific uploaded image
from storage as well as article metadata. It is not a request to delete every
article image, clear only the cover, or cancel an unuploaded crop. The operation
must clearly warn that application rollback cannot restore deleted bytes.

## Current Evidence

- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx), `action`,
  handles primary Save as `publish`: it calls `saveBlogPost`/`createBlogPost`,
  then `publishBlogPost` with the returned revision.
- [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs), `SaveAsync`,
  `PublishAsync` and `ReplaceAsync`, increment revision once per replacement.
  Therefore an existing post at 12 is saved at 13 and published at 14. This is
  two deliberate writes, not evidence of duplicate React events.
- [BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs)
  exposes separate create, save, publish, unpublish and image-upload operations.
  `MutateAsync` evicts public blog caches for publication operations.
- `BlogRepository.ReplaceAsync` atomically filters channel, post ID and expected
  revision. `MockBlogRepository` provides the equivalent locked replacement.
  No new repository interface or Mongo transaction is needed for one replacement.
- `BlogForm` Remove cover changes only local `coverImageId` and `coverAlt`.
  The media thumbnails only select a cover. There is no individual image-removal
  API, service function or thumbnail action.
- `BlogService.Validate` validates both Draft and Published against owned images.
  Covers and recursively nested image/gallery/carousel blocks can reference media.
- [BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs),
  `PublicBlogPost.From`, exposes only published referenced image records.
  Image IDs are opaque storage keys containing slashes, not safe path segments.
- [blog-authoring-controls-handoff.md](blog-authoring-controls-handoff.md)
  and [structured-blog.md](../architecture/structured-blog.md) describe the
  earlier two-request save/publish and partial-success behavior. This plan
  intentionally supersedes that behavior only for primary Save.
- [PagesController.cs](../../MorWalPizVideo.BackOffice/Controllers/PagesController.cs)
  has image-index deletion, but copying its index-based URL is inappropriate for
  revisioned blog media and published snapshots.
- [BlobService.cs](../../MorWalPizVideo.Domain/BlobService.cs), the container-aware
  `DeleteImageAsync`, calls Azure `DeleteIfExistsAsync`. Its mock also supports
  idempotent deletion and configurable failures. The helper does not establish
  whether deployed soft deletion, versions, snapshots or retention preserve bytes.
- [BlogPost.cs](../../MorWalPizVideo.Models/Models/BlogPost.cs) currently has no
  deletion-in-progress state. Mongo and blob deletion cannot be treated as one
  transaction; losing the image's trusted storage identity before a failed delete
  would make reliable retry difficult.

## Proposed Design

### Atomic primary Save

Add dedicated authenticated/channel-scoped save-and-publish operations:

- Collection create-and-publish at `POST /api/blogposts/save-and-publish`.
- Existing article save-and-publish at
  `PUT /api/blogposts/{id}/save-and-publish`.

Reuse `SaveBlogPostRequest` and `BlogPostResponse`; both actions require
`pages.manage`, using existing permission expansion. No new permission family.
Keep existing create/save/publish/unpublish endpoints and their semantics for old
clients. New shared service functions target the new operations; do not overload
the meaning of legacy save silently.

In Domain, reuse/extract the current draft/slug preparation, validation and
publication transition. For an existing article, construct new Draft and Published
from the same validated snapshot, preserve owned images and first-publication/slug
rules, then perform exactly one expected-revision replacement. A post at 12 becomes
13 and publishes the edited tags/content in that same write. Never suppress the
publish revision on a separate write; that would undermine concurrency protection.

Create-and-publish constructs a valid published aggregate before one insert.
Its initial revision remains 1, just like current creation; subsequent primary
saves advance by one. Save as draft still replaces once without changing Published.
Standalone publish/unpublish and uploads retain their current revision rules.

Evict/reset the existing public blog caches after successful persistence using the
controller's existing bounded/logged warning behavior. Cache degradation does not
cause a second write or claim the article was not saved.

The frontend primary Save makes one request, adopts the returned post/revision and
shows a single success or error. It no longer offers "draft saved, publish failed"
for this atomic operation. Keep partial-success handling for save-before-upload,
which remains a different, intentionally sequential workflow.

### Individual uploaded-image removal

Add channel-scoped `DELETE /api/blogposts/{id}/images` accepting opaque `imageId`
and expected `revision` as query parameters. Return the updated `BlogPostResponse`
only after storage deletion and metadata finalization have both completed.
Use update/manage permission, consistent with media upload. Resolve the image
through the owned post metadata; never accept an arbitrary storage URL/key as
authority for a storage operation.

The Domain operation loads the owned post, checks expected revision and image
membership, and reserves exactly that image for deletion before accessing storage.
Reject removal with 409 and a clear instruction if either persisted Draft or
Published references it, including covers and blocks inside columns. Preserve
existing canonical validation as a second line of protection.

Absent post/foreign post/absent owned image returns 404; invalid ID/revision returns
400; stale revision returns 409. Validate revision before reference checks so
concurrency failures are not disguised as reference errors.

Use a bounded, additive `PendingImageDeletionIds` list on `BlogPost`, initially
empty for old documents, containing only IDs already present in its owned Images.
Expose the pending IDs additively in the admin response and TypeScript model, never
the public response. This is a per-image recovery marker, not a general outbox or
media-library subsystem.

The required sequence is:

1. Atomically mark the unreferenced owned image as pending using the existing
   expected-revision replacement. Retain its metadata and trusted storage key.
2. Reject any subsequent save/publish that would introduce a reference to a pending
   image. Remove pending images from selectable editor media. Existing unrelated
   edits remain valid and must preserve pending markers.
3. Delete the exact server-resolved blob in configured `PageContainerName` through
   `IBlobService`. Never derive storage authority from a caller-supplied path.
   An already absent blob is an idempotent storage success.
4. Reload the latest post and finalize removal of both image metadata and marker
   with an expected-revision replacement, preserving unrelated edits. A concurrent
   update must not be overwritten. If finalization conflicts, report it explicitly
   and leave the durable marker for a retry with the current revision.

Both persisted phases advance the concurrency revision. This does not change the
separate requirement that one primary Save advances exactly once: do not hide
storage-state changes by reusing a revision or decrementing the displayed token.

If storage deletion fails, times out or the process stops, retain the pending
marker and metadata. Return an explicit non-success outcome with the confirmed
post/revision and deletion-pending indication when a response is possible; use 503
for storage unavailability and 409 for a finalization conflict. Log structured
post/channel/image identifiers and the exception, without credentials or signed
URLs. Reloading the editor exposes pending items with Retry deletion, not a false
success. A retry resolves the same owned metadata, skips an already completed
reservation, and safely repeats storage deletion/finalization. A fully removed
image returns 404 on a later repeat; refresh resolves a lost success response.

Whole-post deletion must reject while pending image deletions exist, so it cannot
discard the only recovery marker. No background job is required for this bounded
workflow: recovery is an explicit, persisted, user-visible retry. Do not restore
normal selectable media after an uncertain storage result.

Before release, verify target storage soft-delete/version/snapshot/retention
settings. Delete retained copies of this specific object where policy and provider
capabilities permit; the current helper may require a narrowly scoped extension.
Never disable account-wide retention or purge unrelated objects. If policy prevents
irreversible removal, report that limitation and block a permanent-erasure claim
until the owner resolves it. Source blob deletion cannot revoke bytes already
downloaded or held in browser/CDN caches; do not promise remote secure erasure.

Render a distinct Remove image action for each media item, beside rather than
nested inside its cover-selection button. Confirm permanent deletion of the
specific stored image with an irreversible-action warning; disable initiation
while busy or while unsaved draft changes exist, and explain why. Keep Remove
cover unchanged. The author clears references and saves first; an image still
referenced by the live snapshot remains protected until republish/unpublish.
Before-reservation failures preserve media, form state and revision. After a
reservation, adopt the confirmed revision and show pending/retry feedback without
claiming bytes or metadata are unchanged. Display pending recovery separately from
selectable media, including after reload.

### UI planning lens

Impeccable mode: Operate; refinement, not redesign. Implementation should use
`harden` narrowly for media confirmation, reference errors and pending states.
No BackOffice `PRODUCT.md` or `DESIGN.md` was found in the inspected frontend tree;
the existing React Bootstrap/Lucide editor is the incumbent visual authority.
Do not create a new design system. Capture desktop/mobile media controls and
keyboard/focus/error behavior after implementation.

## Impacted Projects

- `MorWalPizVideo.Domain` (.NET 10): atomic publication, deletion reservation,
  reference protection and retry/finalization.
- `MorWalPizVideo.BackOffice` (.NET 10): additive actions, validation and existing
  public-cache coordination.
- `MorWalPizVideo.BackOffice.Tests` (.NET 10): domain/concurrency and HTTP behavior.
- `@morwalpizvideo/services`: additive admin functions and transport regressions.
- `frontend/back-office-spa`: primary Save and media controls/tests.
- `MorWalPizVideo.Models`, `MorWalPiz.Contracts` and `@morwalpizvideo/models`:
  additive pending-deletion state and admin projection; public DTOs unchanged.
- ServerAPI, public client and shared layout: verify-only published projection,
  renderer and cache behavior. Other shared-service consumers retain existing APIs.
- Shop, Ask, Shooting ITA, FreezerAnalysis, Windows applications, ShortLinks,
  AppHost and deployment topology: no feature changes.

## Impacted Files

### Modify
- [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs): shared transition
  preparation, single-write create/save-and-publish, guarded deletion reservation,
  retry/finalization and protection against dropping pending state.
- [BlogPost.cs](../../MorWalPizVideo.Models/Models/BlogPost.cs): bounded pending
  image-ID list with an empty default.
- [BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs): additive
  admin pending-ID projection; keep public media projection unchanged.
- [blog.ts](../../frontend/fe-packages/models/src/blog.ts): additive admin pending
  IDs and typed deletion-pending failure details.
- [BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs):
  additive operations, authorization, storage orchestration, explicit partial
  failure, latest-revision responses and publication cache coordination.
- [blogService.ts](../../frontend/fe-packages/services/src/blogService.ts):
  typed atomic-save and query-encoded image-removal functions.
- [index.ts](../../frontend/fe-packages/services/src/index.ts): export new functions.
- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx):
  one-request Save, irreversible per-image confirmation and persisted retry UI.
- [form.test.tsx](../../frontend/back-office-spa/src/routes/blog/form.test.tsx):
  updated Save semantics and media interaction regressions.
- [transport.test.ts](../../frontend/back-office-spa/src/routes/blog/transport.test.ts):
  real request paths and opaque image-ID encoding.
- [BlogServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs)
  and [BlogControllerTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs):
  revision, authorization, persistence and reference protections.

### Add
Shared-service tests beside `blogService.ts` if existing transport tests cannot
cover the new operations without duplication. No new service abstraction needed.

### Generated/Migration
No backfill, new collection, index or generated contract is required. The additive
pending-ID field defaults to empty; old documents remain readable.

### Documentation
- This handoff; [structured-blog.md](../architecture/structured-blog.md);
  [blog-authoring-controls-handoff.md](blog-authoring-controls-handoff.md).
  Record atomic primary Save, permanent media deletion, the two-phase recovery
  state and irreversible rollback limits.
- [backend.md](../architecture/backend.md): pending-media ownership and storage
  recovery boundary. Record the approved per-image deletion policy and state machine
  in an ADR under [adr/](../architecture/adr/) using the next available number;
  it changes persistence and destructive operational behavior.

### Verify Only
- [BlobService.cs](../../MorWalPizVideo.Domain/BlobService.cs): existing
  container-aware idempotent delete and failure-capable mock. Modify only if the
  verified target storage requires narrowly scoped version/snapshot handling.
- [BlogServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs):
  retain legacy publication, slug-lock and concurrency coverage.
- [public blog routes](../../frontend/morwalpizvideo.client/src/routes/blog/):
  published-only display and image projection.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Additive atomic-save and media-removal operations; legacy endpoints preserved. |
| Frontend/UI | Save becomes one request; distinct image selection/removal controls. |
| Database/Data | Additive bounded deletion markers; CAS reservation/finalization plus irreversible deletion of the specific blob. |
| Contracts/Models | Reuse Save shapes; add admin pending IDs and typed partial-failure details. Public contract unchanged. |
| Authentication/Authorization | Publish requires manage; media removal update/manage; preserve channel and CSRF checks. |
| Validation | Block deletion of referenced media, references to pending media and whole-post deletion that would lose pending recovery. |
| Cache | Atomic publication uses existing lowercase Blog tag/key purge/reset; removal changes no public references. |
| Configuration/Secrets | Reuse configured Page container; inspect retention settings without exposing secrets or changing global policy. |
| Logging/Telemetry | Explicit storage/finalization failures, confirmed revision and persisted retry state; no success before completion. |
| Background Jobs | None; explicit editor retry resumes durable pending state. |
| Deployment/Operations | Upgrade all BackOffice writers before enabling deletion; verify storage policy and actual blob absence. |
| Backward Compatibility | Old documents default pending state to empty; old full-document writers must not run while markers exist. |

## Reuse Opportunities

Reuse `BlogService.Validate`, slug normalization/ever-published lock,
`IBlogRepository.ReplaceAsync`/`CreateAsync`, mock locking, `MutateAsync`,
`EvictAsync`, existing Blog cache constants, shared successful-response handling,
fetcher state, channel-consistency guard, Bootstrap confirmation patterns, and the
existing draft/public projection tests. Reuse the container-aware idempotent blob
delete and failure-capable mock; never use its default upload-container overload
for blog media.

Factor shared transition logic only where necessary to prevent divergent
validation between legacy and atomic operations. No generic transaction or media
library abstraction is needed.

## Risks

| Rank | Cause/surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Saving and publishing through two writes retains the bug or creates partial publication. | Atomic replacement; assert one write and exact 12-to-13 transition with edited Published content. |
| High | Removing a live/nested image breaks public content. | Inspect both snapshots recursively; conflict with no mutation; public projection remains valid. |
| High | New endpoint weakens publish permission or cross-channel ownership. | Manage-only publish plus existing channel pipeline; test denied/foreign/CSRF requests. |
| Medium | Concurrent save/removal overwrites edits. | Existing CAS; competing requests yield one winner and one 409, no lost content. |
| Medium | API/SPA rollout mismatch leaves Save unavailable. | Deploy API first; explicit request failure, never fallback to two writes. |
| High | Blob delete succeeds before reference protection, breaking a concurrently saved article. | Persist reservation first; all save/publish paths reject pending references; race tests show no live dangling image. |
| High | Storage or finalization failure loses recovery state or reports false completion. | Persist owned metadata/marker until confirmed delete and finalization; restart/retry fault tests and visible pending response. |
| High | Older full-document writer drops the new marker during mixed-version deployment. | Upgrade/drain all BackOffice writers before deletion activation; prohibit rollback with unresolved markers. |
| Medium | Retention/snapshots/versioning preserve stored copies despite live blob deletion. | Inspect target policies and verify permitted object-specific purge; do not claim irreversible erasure from DeleteIfExists alone. |
| Medium | Old revision tests or cache handling encode two-call assumptions. | Update only primary-Save expectations; retain legacy endpoint/upload tests and post-persistence cache warnings. |

## Technical Debt

Existing blog media storage has no general lifecycle/retention workflow. Add only
the pending per-image recovery state needed for this confirmed destructive action;
defer bulk cleanup, automatic sweeps and general media management. Legacy clients may still produce
two revisions for their two requests; preserve compatibility rather than rewriting
their concurrency semantics. Existing save-before-upload partial success is
intentional and not part of this revision fix.

## Implementation Roadmap

1. Add focused failing domain tests for atomic existing Save at revision 12,
   create-and-publish revision 1, legacy draft behavior and reference-safe permanent
   deletion, including failed storage and process-interruption recovery.
2. Add the compatible pending-state model/admin projection and implement Domain
   transitions using existing CAS operations. Preserve pending state across every
   mutation, block pending references and guard whole-post deletion. Verify primary
   Save increments exactly once; deletion reservation/finalization each advance
   the concurrency token.
3. Add manage-only atomic API operations and update/manage media removal.
   Orchestrate reservation, exact-container blob deletion and finalization; surface
   pending failures and latest revision. Reuse cache invalidation for publication.
   Gate: unauthorized, stale, invalid and foreign requests perform no writes.
4. Add shared-service functions and exports. Verify URL/query encoding, credentials
   and channel context; propagate typed pending failure state instead of hiding it
   behind a generic error. Build models before services.
5. Replace the frontend primary-Save sequence with one call; preserve draft,
   standalone publication, crop/upload and channel-switch protections.
   Add permanent-removal confirmation, referenced/dirty-state guidance, pending
   state after reload and explicit Retry deletion.
6. Run targeted backend tests with serial build/no-rebuild execution and affected
   service/editor tests. Build the admin after shared packages in dependency order.
7. Verify object-specific storage retention behavior, upgrade/drain all API writers,
   then deploy the SPA that exposes deletion. Capture real storage absence, failure
   recovery, desktop/mobile and public-article proof; update owning docs, ADR and
   this handoff with actual files/results/risks.

Stop at atomic Save and safe permanent deletion of one selected owned image,
including persisted retry state. Do not build a global media library, bulk purge
pipeline, new revision display counter or unrelated data migration.
Independent post-validation review is recommended because publication, optimistic
concurrency, media references and public visibility intersect. The user may select
MorWalPiz Code Reviewer; this handoff does not execute that review.

## Testing Strategy

- Domain/unit: exact 12-to-13 revision and one replacement; Draft and Published
  contain edited tags; first-publication preserved; slug remains locked after ever
  publishing; create-and-publish starts at 1.
- Legacy contract: draft-only Save leaves live content unchanged; standalone
  publish/unpublish/upload still increment as before.
- Concurrency: atomic Save racing draft Save or image removal; one CAS succeeds,
  stale request gets 409 without overwrite. Finalization racing an unrelated edit
  preserves that edit and leaves a retriable marker on conflict.
- Media: remove one unreferenced image while retaining others/order; protect Draft
  and Published covers, image/gallery/carousel blocks and nested columns;
  no metadata/blob mutation on reference/stale/foreign initiation failure.
  Successful removal deletes the exact owned key in PageContainerName, leaves
  other images intact and removes both metadata and recovery marker.
- Recovery: blob already absent; storage exception/timeout; interruption after
  reservation or after blob deletion; metadata-finalization failure; reload/retry;
  missing/stale retry; whole-post deletion during pending work. No success-shaped
  response while deletion is pending, no arbitrary key deletion and no loss of
  persisted recovery information.
- HTTP/security: manage versus update-only versus create-only; missing/foreign
  channel; missing image; invalid/overflow revision; CSRF; expected status/DTO shape.
- Cache: successful atomic publish invokes existing purge/reset; a cache failure
  retains the committed revision and emits existing warning/logging.
- Services/components: one primary-Save network request; no follow-up publish;
  returned revision adopted; visible conflict; draft and upload partial-success
  paths unchanged; opaque IDs with slashes encoded as query values.
- UX/end-to-end: thumbnail action is independently keyboard reachable, no nested
  buttons; irreversible confirmation/focus return; pending/dirty/reference errors
  and durable retry after reload;
  desktop/mobile wrapping; selected cover and live article remain intact.
- Public consumer: changed tags/cover/body appear from Published only; pending
  state never appears in public DTOs and unreferenced deletion cannot break live
  article media.
- Compatibility/storage: old documents load with empty markers; all writers
  preserve new state. With a disposable image on target storage, verify source
  absence and permitted version/snapshot deletion. Do not use valuable existing
  media for destructive validation.

## Migration And Rollback

Additive Mongo state, no backfill: absent pending IDs mean empty. Deploy model,
projection and compatible mutation paths to every BackOffice writer before the
SPA exposes permanent deletion. ServerAPI public projection remains unchanged.
Do not allow old full-document writers to coexist with active deletion markers:
ignoring unknown fields while reading does not preserve them on replacement.
Keep legacy endpoints throughout the compatibility window, with the new pending
reference guards. Monitor 409s, deletion-pending failures, Save errors and cache
warnings.

Do not renumber historical revisions or normalize 12-to-14 history. Revisions are
concurrency tokens, not a retroactive count of user gestures.

Rolling back the SPA restores the old two-request behavior but remains compatible.
Do not remove new API endpoints while a new SPA bundle may still be cached.
Newly published documents need no conversion on rollback. Before rolling the API
back to a version without pending-state support, stop new deletion requests and
resolve every pending deletion; do not silently drop markers or mark uncertain
blob outcomes as restored. If storage is unavailable, keep the compatible API
deployed and roll back only the SPA.

Permanent deletion cannot be undone by restoring Mongo metadata or deploying old
code. Recovery would require a separately available, policy-permitted backup of
the bytes; no such backup is promised. Do not restore metadata pointing at a
deleted object. No auth/cache migration, global retention change or new feature
flag is required; activation is controlled by deployment order.

## Open Questions

No outstanding product question about deletion scope: the user confirmed permanent
storage deletion on 2026-11-09. The one-save/one-revision design remains unchanged.

Operational release evidence remains outstanding: target storage retention,
soft-delete, snapshots and version policies must be inspected. If they prevent
permanent object-specific deletion, return the precise policy limitation to the
owner rather than implementing a metadata-only substitute or bypassing retention.
