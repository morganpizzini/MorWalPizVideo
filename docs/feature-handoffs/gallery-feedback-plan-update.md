Gallery image performance and UX — implementation plan
Outcome
Preserve every uploaded original unchanged for downloads. Generate one smaller display image per original, bounded to 1920 × 1080 for landscape or 1080 × 1920 for portrait, preserving aspect ratio and never enlarging smaller images. Improve uploads, image removal, slugs, and public-gallery usability without redesigning the gallery.

Current Evidence
BackOffice sends selected images together; the API handles the batch in one request and copies originals into memory during preparation: /home/runner/work/MorWalPizVideo/MorWalPizVideo/frontend/back-office-spa/src/routes/galleries/detail/Component.tsx and /home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.BackOffice/Controllers/GalleriesController.cs.
The API has an image-delete endpoint, but the BackOffice detail page has no remove control.
Public and admin displays use original URLs. Public single-image and ZIP downloads read original storage keys: /home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.ServerAPI/Controllers/GalleriesController.cs.
Slugs are entered manually; lookup is channel-scoped, but creation does not currently enforce uniqueness.
Three images totaling roughly 20 MB can fail together; some individual images are 20 MB or slightly larger. The cause has not been established.
Proposed Design
Generate one preview at upload time, not through a resize middleware. Use it in BackOffice and the public grid, list, and modal. Keep the original key as the exclusive source for single-image and ZIP downloads.

Upload selected files sequentially, one request per image. Show which image is being processed, completed and failed items, and a retry action for failed items. Since server processing cannot currently report percentage completion, show an indeterminate status rather than an invented percentage. Ensure a failure cannot publish an incomplete image reference or leave unmanaged blobs.

Prefill new galleries with a GUID-based slug. Keep it editable on creation and update; normalize and enforce uniqueness per channel on the server. Reject duplicates clearly without silently changing existing public links.

Add confirmed image removal in BackOffice. Removal, gallery deletion, and expiration cleanup must delete the original and its preview. Retain a display fallback for older gallery records that have no preview.

On the public page, left-align the list’s first column; give mobile selection and download controls a deliberate layout; replace modal footer navigation with accessible overlay carets and position dots; and provide prominent, accessible feedback during long downloads and on failure.

Impacted Projects
Backend: MorWalPizVideo.BackOffice, MorWalPizVideo.ServerAPI, MorWalPizVideo.Domain, MorWalPizVideo.Models, and MorWalPiz.Contracts.
Frontend: frontend/back-office-spa, frontend/morwalpizvideo.client, and the shared frontend models and services packages.
Tests: MorWalPizVideo.BackOffice.Tests and affected frontend workspace tests.
Operations: Blob access and cleanup, MongoDB uniqueness, and deployed request-size limits. No shop or Windows application change.
Impacted Files
Modify

/home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.BackOffice/Controllers/GalleriesController.cs — upload validation, slug conflicts, and complete image deletion.
/home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.BackOffice/Services/ChannelNewsMediaProcessor.cs — gallery preview preparation without changing other image flows.
/home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.BackOffice/Jobs/GalleryCleanupJob.cs — preview cleanup.
/home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.Models/Models/Gallery.cs, /home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPiz.Contracts/Contracts/GalleryContract.cs, and /home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPiz.Contracts/ContractUtils.cs — optional preview references and compatible projections.
Gallery repository interface, Mongo implementation, and mock implementation under /home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.Domain/Interfaces/ — aligned per-channel slug behavior.
/home/runner/work/MorWalPizVideo/MorWalPizVideo/MorWalPizVideo.ServerAPI/Controllers/GalleriesController.cs — preserve original-only downloads and verify public projection behavior.
/home/runner/work/MorWalPizVideo/MorWalPizVideo/frontend/fe-packages/models/src/gallery.ts and gallery helpers under /home/runner/work/MorWalPizVideo/MorWalPizVideo/frontend/fe-packages/services/src/ — compatible preview types and deletion requests.
Gallery form and detail routes under /home/runner/work/MorWalPizVideo/MorWalPizVideo/frontend/back-office-spa/src/routes/galleries/ — slug, upload, preview, and removal interactions.
/home/runner/work/MorWalPizVideo/MorWalPizVideo/frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx and /home/runner/work/MorWalPizVideo/MorWalPizVideo/frontend/morwalpizvideo.client/src/main.scss — display and UX refinements.
Add: focused backend and frontend tests. Add a resumable preview backfill only after confirming its operational owner and Blob permissions.

Generated/Migration: an approved MongoDB unique index after auditing existing duplicates. No SQLite migration.

Documentation: record the final storage, limits, rollout, and validation decisions in the gallery handoff and affected architecture documents.

Verify only: gallery channel-header handling, other shared-contract consumers, Blob configuration, existing tests, and deployed request/proxy limits.

Impact Matrix
Area | Required outcome
|---|---|
Backend/API	| Store original plus one preview; download originals exclusively.
Frontend/UI	| Sequential upload feedback and retry, removal, previews, responsive controls, modal navigation, and download status.
Database/Data | Optional preview reference and normalized per-channel slug uniqueness.
Contracts/Models | Add preview information compatibly; retain existing original references.
Authentication/Authorization | Preserve BackOffice channel scope and image permissions; public gallery reads remain anonymous.
Validation | Validate supported image format and safe processing limits server-side; reject duplicate slugs.
Cache | Verify Blob/CDN behavior rather than assuming a gallery output-cache tag exists.
Configuration/Secrets | Check deployed upload thresholds and storage access; commit no secrets.
Logging/Telemetry | Observe upload failures, processing memory and duration, download latency, and cleanup failures.
Background Jobs | Extend existing expiration cleanup; keep any backfill controlled and resumable.
Deployment/Operations | Deploy compatible backend and storage access before clients switch to previews.
Backward Compatibility | Keep existing links, originals, downloads, and a display fallback for older records.

Reuse Opportunities
Reuse the existing Blob service, image-delete route, image-processing library, shared gallery contracts and transport, and route-local React state. Do not introduce a general-purpose image-resizing middleware.

Risks
High — upload failure or memory pressure: aggregate request size is plausible but unconfirmed. Check deployed request limits and measure memory with three images totaling about 20 MB and a single image over 20 MB.
High — partial writes: failure between original upload, preview upload, and gallery update may orphan blobs. Verify cleanup and retry.
High — Blob access mismatch: confirm preview URLs can be displayed under deployed permissions without exposing private storage keys.
Medium — slug duplicates: audit existing values before adding a unique index; verify concurrent writes.
Medium — compatibility: old galleries and mixed deployments need preview fallback.
Medium — bulk-download memory: the current ZIP is assembled in memory; measure representative selections and address it if necessary.
Technical Debt
The memory-backed ZIP and original-only display contract are relevant. Improve ZIP delivery if measurements justify it; leave unrelated image-upload flows untouched.

Implementation Roadmap
Inspect deployed multipart/proxy limits and Blob permissions. Reproduce or characterize the failing batch, including peak memory during preview generation.
Audit per-channel slugs; add optional preview data and compatible contracts, then enforce normalized uniqueness with aligned Mongo and mock behavior.
Generate the orientation-aware preview without modifying the original. Cover partial-failure cleanup and every image-deletion path.
Add the generated editable slug, sequential uploads, per-image status/retry, preview display, and confirmed removal to BackOffice.
Backfill older previews in bounded, resumable batches if approved. Switch public display to previews with fallback while keeping downloads on originals.
Refine public list alignment, mobile toolbar, modal navigation, and accessible download feedback. Validate and deploy backend/storage changes before client changes.
The UI work is a refinement, not a redesign. Use Impeccable adapt for responsive behavior and harden for loading, error, and accessibility states. Preserve each application’s current visual conventions.

Testing Strategy
Backend: unchanged original bytes; landscape and portrait bounds; no upscaling; original-only single and ZIP downloads; invalid image handling; partial-failure cleanup; deletion and expiration; legacy records; permissions; duplicate slugs within one channel and reuse across channels.
Frontend: sequential requests for three selected images, per-image failure and retry, removal confirmation, preview fallback, download feedback, mobile toolbar, and keyboard/dot modal navigation.
Operational and visual: test a three-image selection totaling roughly 20 MB and individual images above 20 MB where deployment permits. Measure peak memory during preview generation and ZIP creation. Check desktop/mobile layouts, long filenames, touch targets, focus, and accessible status announcements.
Migration And Rollback
Add optional preview fields before switching clients. Audit duplicate slugs before applying the MongoDB index. Keep originals and their references throughout a resumable preview backfill. A client rollback can display original URLs; backend rollback must tolerate additive records. Monitor preview failures and orphaned blobs. No SQLite migration is needed.

Open Questions
The safe maximum original byte size and decoded pixel count, and the actual cause of the batch failure, require deployed configuration and measurement. Do not impose a 20 MB per-image cap by assumption. Preview resolution, editable per-channel slugs, and original-only downloads are settled.

An independent post-validation review is warranted for Blob lifecycle, public-contract compatibility, and data integrity.

## Implementation Update (2026-10-22)

Implemented the additive gallery storage and UX changes in the current source tree:

- `GalleryImage` and the shared contracts now support optional preview storage keys and public URLs. Public conversion uses the preview when present and preserves the original URL as the legacy fallback.
- BackOffice uploads preserve the original bytes, generate an orientation-aware bounded JPEG preview, clean up all uploaded keys on partial failure, and delete both original and preview blobs for image deletion, gallery deletion, and expiration cleanup.
- Gallery create/update checks normalized slugs within the selected channel and returns a conflict instead of silently changing an existing public link.
- The BackOffice SPA uploads selected files sequentially, reports per-file processing state, uses generated editable slugs for new galleries, displays previews with legacy fallback, and supports confirmed image removal.
- The public gallery now reports download failures accessibly, keeps the list’s first column left-aligned, stacks mobile download controls, and uses overlay modal navigation with keyboard-compatible dots and carets.

Validation completed:

- `dotnet test MorWalPizVideo.BackOffice.Tests --filter FullyQualifiedName~GalleryBehaviorTests` passed.
- Shared frontend package build completed far enough to compile the models and services packages; editor diagnostics report no errors in the touched TypeScript files. The combined frontend build runner did not return a completion status after starting, so the full application build remains to be rerun in CI or a dedicated terminal.

Residual operational work:

- Production Blob permissions, multipart/proxy limits, peak memory, duplicate-slug audit, and the approved Mongo unique index still require deployment-owner verification before rollout. The current controller check prevents ordinary duplicate writes but does not replace a production unique index under concurrent requests.