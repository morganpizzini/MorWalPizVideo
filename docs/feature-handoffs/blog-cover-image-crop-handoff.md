# Blog cover image crop and Full HD export

Status: Implemented in the worktree on 2026-11-06; not deployed.
Focused frontend and BackOffice validation passed. Browser visual verification
remains outstanding; see Validation Performed.

## Outcome

For newly imported blog cover images, show an interactive preview with a fixed
16:9 selection and rule-of-thirds grid. Let the editor choose the retained
region, then export exactly 1920 x 1080 pixels before uploading. BackOffice
must validate the actual decoded output before storing it.

Assumptions for approval:

- "1080p" means exact landscape Full HD, not a longest-side maximum.
- Scope is the blog form's Cover image section, not Page, ChannelNews, logos,
  video thumbnails, or a repository-wide image-policy change.
- Small images may be upscaled to satisfy exact dimensions; warn when the
  selected source region is smaller than Full HD.
- Existing stored images and their selection behavior remain compatible.
  Retrofitting or re-cropping existing media is a separate feature.
- Cover output is a static JPEG, matching the existing server format. If a
  browser decodes an animated source, the preview/export is a still image;
  no animation preservation or frame-selection feature is included.

## Current Evidence

Paths below are relative to the repository root.

| Evidence | Verified behavior |
| --- | --- |
| [Blog form](../../frontend/back-office-spa/src/routes/blog/form.tsx), `BlogForm` | File selection opens the local crop workflow. Submission is gated on a confirmed prepared JPEG and retains the existing fetcher, revision, permission and save-before-upload flow. |
| Same file, `action` | Upload saves a new/dirty draft first, uses its returned revision, uploads, then selects the returned image as an unsaved cover. Partial success and channel changes are surfaced. |
| [Blog transport](../../frontend/fe-packages/services/src/blogService.ts), `uploadBlogImage` | Multipart file/revision/altText request to the existing post-owned images endpoint through shared authenticated transport. |
| [Blog controller](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs), `Upload` | Channel-scoped update/manage permission; 10,000,000-byte limit; revision/ownership checks; decoded 25-megapixel and single-frame checks before processing; blob cleanup on failed aggregate attachment. |
| [Media processor](../../MorWalPizVideo.BackOffice/Services/ChannelNewsMediaProcessor.cs), `PrepareImageAsync` | Auto-orients, preserves aspect ratio, and encodes JPEG at quality 90. Landscape and portrait have different bounds. `ResizeToBounds` exits when the long side is already within its maximum, so this is not an exact-size guarantee. |
| [Domain service](../../MorWalPizVideo.Domain/BlogService.cs), `AddImageAsync` / `ValidateSnapshot` | Revision-checked image append; covers must reference owned images with alt text. Image dimensions are not a cover constraint. |
| [Blog contracts](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs) and [frontend models](../../frontend/fe-packages/models/src/blog.ts) | Existing responses already carry image width and height. Public cover URL derives from the published snapshot's image reference. |
| [Form tests](../../frontend/back-office-spa/src/routes/blog/form.test.tsx), [transport tests](../../frontend/back-office-spa/src/routes/blog/transport.test.ts), [controller tests](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs) | Existing coverage for upload/save sequencing, scoped multipart transport, ownership, revision conflicts and image storage. |
| [Blob mock](../../MorWalPizVideo.Domain/BlobService.cs), `BlobServiceMock` | Stores bytes and supports download, allowing tests to decode stored output rather than checking metadata alone. |
| [SPA manifest](../../frontend/back-office-spa/package.json) | React 19, React Router 7, React Bootstrap, Lucide, Vitest/Testing Library and Playwright dependency; no cropper dependency declared. |
| [Workspace manifest](../../frontend/package.json) / [BackOffice project](../../MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj) | Yarn Classic workspace with ordered shared builds; BackOffice targets .NET 10 and already references ImageSharp. |

Current flow: local file -> browser decode/limits -> 16:9 crop and grid ->
confirmed 1920 x 1080 JPEG -> optional draft save -> multipart request with
`purpose=cover` -> BackOffice preparation and exact-dimension gate -> blob
storage -> revision-checked image append -> unsaved cover selection ->
explicit draft save/publish.

No BackOffice `PRODUCT.md` or `DESIGN.md` was found in the frontend tree.
The current Bootstrap form is the incumbent visual authority. Historical
build results in the structured-blog document are not current validation.

## Implemented Design

### Local preparation, existing storage boundary

Keep crop state and processing local to the blog route. Use a small route-local
component and a pure geometry/export helper, not shared layout infrastructure,
a global store, or a new media endpoint.

1. Selecting JPEG, PNG or WebP opens an inline crop preview without saving or
   sending anything. Enforce the existing source byte limit and check decoded
   dimensions against the 25-megapixel limit. Invalid files show actionable
   feedback; the original file must never become an upload fallback.
2. Show a responsive fixed 16:9 frame with a rule-of-thirds overlay. Start
   with the largest centered crop that fits the browser-decoded image.
3. Allow drag/touch repositioning, zoom, and labeled native horizontal,
   vertical and zoom controls usable without dragging. Reset restores the
   initial framing. Bound movement/zoom so the crop never includes empty space.
   Keep geometry in oriented source coordinates, independent of CSS pixels
   and device pixel ratio.
4. "Use crop" produces a 1920 x 1080 canvas export as JPEG at quality 0.9.
   Composite transparent sources over white, consistent across preview and
   export. Display the prepared result and output dimensions. Keep the source
   locally available for "Adjust crop"; editing invalidates prior confirmation.
5. Upload is enabled only for a confirmed prepared file and valid alt text,
   with existing permission checks intact. It submits only the prepared JPEG,
   never the source. Preserve the original descriptive filename stem with a
   JPEG extension.
6. Preserve save-before-upload, returned revisions, partial success messaging,
   unsaved cover selection and subsequent explicit save/publish.

On a file replacement, cancel, successful upload, channel/post change or
unmount, invalidate outstanding decode/export work and release object URLs.
A late result must not replace a newer selection.
While exporting/uploading, prevent conflicting upload/crop actions and keep
errors explicit. Crop failures occur before the action can save a draft.
Failed network uploads retain the confirmed crop for retry.

### Narrow server-side enforcement

The upload action adds an optional multipart `purpose` field with only `cover`
supported when present. Absence preserves the existing general article-image
behavior. `uploadBlogImage` has an optional fifth, typed cover-purpose argument;
existing four-argument callers stay unchanged. The cover form always supplies
the purpose. Unknown/nonempty unsupported values return 400.

For cover requests, use the existing decoded format, frame, byte and pixel
checks and require actual prepared dimensions of exactly 1920 x 1080 before
`UploadImageAsync`. Reject a noncompliant upload explicitly instead of silently
cropping, stretching, padding or accepting it. Reuse `PrepareImageAsync` for
orientation and server JPEG encoding; verify dimensions after that processing
so EXIF cannot bypass the requirement.

The browser owns crop composition and exact-size export; the API reuses its
existing processor for orientation/re-encoding and possible downscaling, then
is the authoritative stored-output gate. The API does not auto-crop, stretch
or pad. No change to the shared processor's general behavior was necessary.

This is an additive upload discriminator, not a persisted media purpose or
global cover invariant. Do not impose dimension checks on `ValidateSnapshot`:
that would break existing cover selection and unrelated draft updates.

### Impeccable implementation lens

Mode: **Operate**. Direction: **refinement**, not redesign.
Target: the existing Cover image section in the blog form.
Use the installed Impeccable `shape` brief represented here, then `harden`
for loading/errors, input alternatives, responsive behavior and resource
lifecycle. The implementer should load target context once; do not create
product-wide design artifacts or restyle the article editor.

Minimum visual proof: desktop and narrow-mobile captures of portrait and
landscape input framing and the confirmed result; keyboard-only controls;
visible focus; screen-reader labels/status/errors; grid contrast over light
and dark images; no overflow at 320 CSS pixels or 200% zoom. Grid is decorative
and must not enter the accessibility tree.

## Impacted Projects

- **frontend/back-office-spa:** crop UI, browser export, upload state and tests.
- **@morwalpizvideo/services:** backward-compatible optional upload discriminator.
- **MorWalPizVideo.BackOffice:** enforce cover output in the existing upload action.
- **MorWalPizVideo.BackOffice.Tests:** decoded-output and compatibility regressions.
- **Documentation:** this handoff and the structured-blog feature reference.
- **MorWalPizVideo.Domain / Models / MorWalPiz.Contracts:** verify-only; existing
  image storage, dimensions, ownership and DTO responses suffice.
- **@morwalpizvideo/models / @morwalpiz/layout:** verify-only; no schema or
  shared UI changes.
- **ServerAPI / public client:** verify-only published-cover rendering and
  cache behavior; no public API or rendering change required.
- **Other workspaces, standalone tools, Windows apps, ShortLinks, AppHost and
  ServiceDefaults:** no change; no new consumer, runtime dependency or service.
- **Deployment:** existing BackOffice API and SPA pipelines; deploy API first.

## Impacted Files

### Modify

- [frontend/back-office-spa/src/routes/blog/form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx):
  integrate confirmation/export lifecycle and cover-purpose submission.
- [frontend/back-office-spa/src/routes/blog/form.test.tsx](../../frontend/back-office-spa/src/routes/blog/form.test.tsx):
  confirmed-file upload gating and existing revision workflow regressions.
- [frontend/back-office-spa/src/routes/blog/BlogCoverCropper.tsx](../../frontend/back-office-spa/src/routes/blog/BlogCoverCropper.tsx),
  [frontend/back-office-spa/src/routes/blog/BlogCoverCropper.css](../../frontend/back-office-spa/src/routes/blog/BlogCoverCropper.css)
  and
  [frontend/back-office-spa/src/routes/blog/BlogCoverCropper.test.tsx](../../frontend/back-office-spa/src/routes/blog/BlogCoverCropper.test.tsx):
  local crop preview, accessible position/zoom controls, confirmation and cancellation.
- [frontend/back-office-spa/src/routes/blog/coverImageCrop.ts](../../frontend/back-office-spa/src/routes/blog/coverImageCrop.ts)
  and [frontend/back-office-spa/src/routes/blog/coverImageCrop.test.ts](../../frontend/back-office-spa/src/routes/blog/coverImageCrop.test.ts):
  crop geometry and exact Full HD JPEG export.
- [frontend/back-office-spa/src/routes/blog/transport.test.ts](../../frontend/back-office-spa/src/routes/blog/transport.test.ts):
  verify additive multipart purpose and unchanged credentials/CSRF/channel headers.
- [frontend/fe-packages/services/src/blogService.ts](../../frontend/fe-packages/services/src/blogService.ts):
  optional typed argument; omit purpose for existing callers.
- [MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs):
  extend the existing multipart binding and validate prepared cover dimensions.
- [MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs):
  exact stored-byte dimensions, rejected requests and legacy uploads.

### Add

Under `frontend/back-office-spa/src/routes/blog/`, add one local crop component,
one geometry/export helper and their adjacent tests, following the route's
existing component/helper naming. Use local styles only if Bootstrap utilities
cannot express the frame/grid. No cropper dependency is needed for the fixed
frame plus native position/zoom controls.

### Generated/Migration

None. No schema, migration, generated asset or dependency-lock change proposed.

### Documentation

- This handoff: implementation, validation and residual visual-verification gap.
- [docs/architecture/structured-blog.md](../architecture/structured-blog.md):
  document the new cover workflow, exact output, optional purpose and legacy compatibility.
  This is the owning feature document; a separate ADR is unnecessary for this
  local additive policy unless implementation expands the boundary.

### Verify Only

- [MorWalPizVideo.BackOffice/Services/ChannelNewsMediaProcessor.cs](../../MorWalPizVideo.BackOffice/Services/ChannelNewsMediaProcessor.cs):
  unchanged general-image processing.
- [MorWalPizVideo.Domain/BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs),
  [MorWalPizVideo.Domain/BlobService.cs](../../MorWalPizVideo.Domain/BlobService.cs),
  [MorWalPiz.Contracts/Contracts/BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs),
  [MorWalPizVideo.Models/Models/BlogPost.cs](../../MorWalPizVideo.Models/Models/BlogPost.cs)
  and [frontend/fe-packages/models/src/blog.ts](../../frontend/fe-packages/models/src/blog.ts):
  unchanged persistence/response contracts.
- [frontend/back-office-spa/src/routes/blog/Editor.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.tsx)
  and [frontend/fe-packages/layout/src/components/BlogRenderer.tsx](../../frontend/fe-packages/layout/src/components/BlogRenderer.tsx):
  unchanged article image choices and rendering.
- [deployment API workflow](../../.github/workflows/deploy-backoffice-api.yml)
  and [deployment SPA workflow](../../.github/workflows/deploy-back-office-spa.yml):
  current independent triggers and validation gates already cover the touched surfaces.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | Additive multipart purpose; decoded exact-size gate before storage. |
| Frontend/UI | Local crop/confirmation/export states; preserve the rest of the form. |
| Database/Data | No schema changes, backfill, overwrite or deletion of existing media. |
| Contracts/Models | Optional transport input only; responses and stored models unchanged. |
| Authentication/Authorization | Preserve update/manage, create-before-upload and channel ownership; shared cookie/CSRF transport remains authoritative. |
| Validation | Local source bounds, crop bounds and export errors; server checks decoded prepared output, never filename/MIME alone. |
| Cache | No eviction on crop or upload; unchanged publish/unpublish/delete coordination and lowercase centralized blog tags. |
| Configuration/Secrets | No settings or secrets added. |
| Logging/Telemetry | Preserve controller failure/cleanup handling; visible local errors and existing upload error messages. No image contents in logs. |
| Background Jobs | Not applicable; local preparation and current synchronous upload suffice. |
| Deployment/Operations | API first, then shared-service/SPA artifact; no workflow redesign. |
| Backward Compatibility | Missing purpose and all existing stored covers retain current behavior. New cover uploads get the exact-size guarantee. |

## Reuse Opportunities

Reuse `uploadBlogImage`, the route fetcher/action, server-returned revisions,
permission guards, channel-keyed form remount, existing alt-text fields,
ImageSharp preparation and blob compensation. Reuse the existing form and
transport tests and `PageControllerWebApplicationFactory` already used by
`BlogControllerTests`. Download mock blob bytes for dimension verification.
Reuse Bootstrap controls and Lucide affordances; do not duplicate the shared
HTTP client or move a one-consumer workflow into layout.

## Risks

| Severity | Cause / surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| High | Crop coordinates diverge from preview due to EXIF, zoom or responsive scaling. | Use one oriented source coordinate model for preview/export; never derive export from a screenshot. | Color-region fixtures prove the retained area, including rotated portraits and edge crops. |
| High | Browser decode/orientation and responsive crop rendering may differ across engines. | Canvas export uses the same oriented `HTMLImageElement` and source-coordinate crop as the preview. | Geometry/export unit tests pass; real-browser EXIF and visual crop verification is still outstanding. |
| High | Client-only checks could allow malformed or wrong-size bytes into storage. | Validate decoded prepared dimensions and existing limits before blob write. | Controller tests reject wrong-size/unknown-purpose requests before storage and decode the stored JPEG dimensions. |
| Medium | Late decode/export overwrites another file/channel, or leaks memory. | Invalidate work on replacement/remount/unmount; release URLs/bitmaps. | Deferred-result tests cannot enable/upload stale files; repeated selections clean up resources. |
| Medium | Existing covers or article images are accidentally forced to 16:9. | Purpose applies only to new cover uploads; do not modify domain snapshot validation or shared processor policy. | Legacy square upload, old cover save and image-block tests remain valid. |
| Medium | Upscaling or JPEG transparency changes quality. | Visible low-resolution warning; white compositing preview; disclose static JPEG output. | Small/transparent fixtures match confirmed preview and exact output dimensions. |
| Medium | External blob failure or revision conflict causes a partially saved draft or orphaned upload. | Preserve existing partial-success handling and compensation; retain crop for retry. | Conflict/failure tests keep returned revision and verify cleanup. |
| Low | API/SPA rollout order is independent. | Verify API gate in staging before enabling the new SPA release. | Direct wrong-size cover request is rejected before the SPA deployment. |
| Low | Public view appears unchanged due to publication/cache state. | Keep explicit save/publish semantics and existing cache eviction. | Draft crop leaves live cover unchanged; publish updates it through the current path. |

## Technical Debt

- The processor's general resize shortcut is not a true two-dimensional bound.
  Cover uploads are dimension-gated after processing, but the general-purpose
  upload path intentionally retains the existing arbitrary-dimension behavior.
  Defer changing that shared behavior because it affects other callers.
- Controller-local multipart binding already exists. Extend it narrowly rather
  than relocating all upload contracts for this feature.
- Media uploads and draft saves are intentionally separate revisions, not an
  atomic transaction. Preserve and test this behavior; do not redesign it.
- Historical structured-blog build/audit findings require fresh implementation
  validation; do not claim they still exist or repair unrelated problems here.

## Implementation And Validation Results

1. **Backend and transport:** implemented optional typed `purpose=cover`;
   rejects unsupported purposes and rejects post-processing dimensions other
   than 1920 × 1080 before blob write. A >Full-HD 16:9 fixture is downscaled by
   the existing processor and the stored JPEG decodes to the exact dimensions.
   Requests without purpose keep legacy behavior.
2. **Crop geometry/export:** implemented centered 16:9 source-coordinate crop,
   bounded pan/zoom and exact 1920 × 1080 JPEG export at quality 0.9, with a
   white canvas background. Geometry and canvas-draw/export contract tests pass.
3. **Local UI:** implemented crop grid, pointer/touch drag, native position and
   zoom sliders, reset, upscaling warning, confirm/adjust/cancel, prepared
   preview and upload gating. Parent-form coverage proves only the confirmed
   JPEG is submitted; existing save-before-upload and revision flow is retained.
4. **Focused validation:** frontend tests/build, backend focused tests,
   formatting, lint and the Impeccable detector results are recorded below.
   Browser captures, real-browser EXIF fixtures and touch/keyboard walkthroughs
   were not performed in this task.
5. **Documentation:** this handoff and the owning structured-blog document are
   updated. Staging and deployment have not been performed.

Completion boundary: new cover imports have a user-controlled 16:9 preview,
confirmed exact Full HD bytes before upload, and an authoritative pre-storage
gate; existing media and publication semantics remain unchanged. Stop there.
No asset backfill, generic upload refactor, public redesign or unrelated cleanup.

An independent post-validation Code Reviewer pass is warranted for crop
coordinate correctness and asynchronous state/compatibility edges; the user
decides whether to request it.

## Testing Strategy

- **Unit / helper:** centered crop, all edges, extreme aspect ratios, bounded
  zoom/pan, fractional coordinates, viewport resizing, zero/invalid dimensions,
  low-resolution warning, selected-region preservation and export failure.
- **Component / BackOffice Vitest:** preview after selection, rule-of-thirds
  overlay, keyboard controls, reset/adjust/cancel, confirmation gate, replacement
  races, busy states, error announcement, cleanup and unchanged permission UI.
- **Contract / existing transport tests:** purpose present only for cover call;
  four-argument call remains compatible; same endpoint, revision, alt text,
  cookie credentials, channel header and CSRF; browser sets multipart boundary.
- **Integration / BackOffice.Tests:** decode downloaded JPEG and assert
  1920 x 1080, not just DTO metadata. Reject wrong dimensions, oriented wrong
  dimensions, unknown purpose, corrupt/oversized/multi-frame upload, missing
  alt text, foreign post and stale revision; no blob/revision mutation on
  pre-storage rejection. Verify compensation after an attachment conflict.
- **Preservation:** omitted-purpose 16 x 16 upload still works; existing
  arbitrary-ratio cover saves/publishes; image blocks and public owned-image
  projection stay unchanged; draft upload does not modify live snapshot.
- **Real-browser end-to-end:** use colored-region landscape/portrait fixtures
  and rotated JPEGs, choose a visibly off-center crop, inspect the outgoing
  JPEG dimensions/content, upload/save/publish and inspect stored/public bytes.
  Include small and transparent images, touch/keyboard, desktop and mobile.
  Use available browser tooling; a Playwright dependency alone is not evidence
  of an established E2E runner.
- **Migration/security:** no migration test needed because schemas do not
  change. Use direct invalid multipart requests for server validation and
  existing test auth/channel fixtures; no third-party image service involved.

## Validation Performed

| Command | Result |
| --- | --- |
| `yarn --cwd frontend build:services` | Passed; shared services package TypeScript build. |
| `yarn --cwd frontend/back-office-spa vitest run src/routes/blog/coverImageCrop.test.ts src/routes/blog/BlogCoverCropper.test.tsx src/routes/blog/form.test.tsx src/routes/blog/transport.test.ts` | Passed; 4 files, 33 tests. |
| `yarn --cwd frontend/back-office-spa build` | Passed; TypeScript project build and Vite production bundle. Vite reports its existing large-chunk advisory. |
| `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false` | Passed; 0 errors. Emits existing NU1902/NU1903 advisories for ImageSharp 3.1.12. |
| `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~BlogControllerTests"` | Passed; 9 tests, including wrong-size/unknown-purpose rejection, server downscale, and decoding stored output to 1920 × 1080. |
| `yarn --cwd frontend prettier --check` with all changed frontend source/tests | Passed. |
| `yarn --cwd frontend/back-office-spa eslint` on changed BackOffice blog source/tests | Passed with 0 errors and 2 Fast Refresh warnings in the existing form module. |
| `node .github/skills/impeccable/scripts/detect.mjs --json frontend/back-office-spa/src/routes/blog/form.tsx frontend/back-office-spa/src/routes/blog/BlogCoverCropper.tsx frontend/back-office-spa/src/routes/blog/BlogCoverCropper.css` | Passed; empty findings. Not browser visual evidence. |

No browser screenshots or real-browser EXIF/touch verification were performed.
The canvas unit test verifies dimensions, JPEG parameters and source crop
arguments, but does not raster-compare the browser-produced image.

## Migration And Rollback

No MongoDB or SQLite migration, backfill, cache-key migration or auth/config
transition is needed. Crop coordinates/originals are transient local state.
Only a new owned JPEG and existing image metadata are persisted.

Deploy the compatible API first. Old SPA uploads omit purpose and continue
working. After staging proves the new gate, deploy the updated shared-service
and SPA build. No new feature flag is required for this bounded additive flow.
Monitor upload errors/conflicts and verify published cover changes through
existing cache coordination.

Rollback the SPA first; older clients work against the additive API. Rollback
of the API does not make uploaded Full HD images unreadable, but removes the
server gate, so do not claim enforcement while it is rolled back. Never
overwrite/delete old image blobs or references during rollout/recovery.
New cropped JPEGs remain valid ordinary images under old releases.

## Open Questions

No implementation decisions are waiting on approval. The ImageSharp dependency
advisories and real-browser visual/orientation verification remain outstanding.
Re-cropping existing media, rejecting animated source imports before browser
rasterization, or applying the policy to other cover surfaces would be a
separate scope change.
