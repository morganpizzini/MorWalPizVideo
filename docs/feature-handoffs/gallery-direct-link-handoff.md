# Gallery Direct-Link Handoff

## Requested behavior

Add the public gallery route for direct links at `/gallery/:slug`.

## Test-first checkpoint

- Added `frontend/morwalpizvideo.client/src/routes/gallery-route.test.tsx`.
- The test requires the route to resolve a slug, expose a loader and element, and remain outside the `Root` application shell.
- The route and public component are not implemented yet; the focused test is expected to fail until that work is complete.

## Implementation and validation

Implemented the direct public gallery route outside the application shell. The route loads the public gallery using the configured channel ID and renders a selectable list/grid gallery with modal navigation and downloads.

Modified files:

- `frontend/morwalpizvideo.client/src/routes/config.tsx`: registered `/gallery/:slug` as a top-level route.
- `frontend/morwalpizvideo.client/src/routes/gallery/loader.ts`: validates the slug and loads the gallery.
- `frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx`: renders the public gallery page.
- `frontend/morwalpizvideo.client/src/services/gallery.ts`: supplies the configured channel ID to the shared gallery API.
- `frontend/fe-packages/services/src/index.ts`: exports `getPublicGallery`.
- `frontend/fe-packages/services/src/apiService.ts`: exposes the public gallery blob download helper.
- `frontend/morwalpizvideo.client/src/utils/download-button.tsx`: exports the reusable blob download helper.
- `frontend/morwalpizvideo.client/vitest.config.ts`: mirrors the Vite path aliases for Vitest.

Focused validation passed with `npm test -- --run src/routes/gallery-route.test.tsx` from `frontend/morwalpizvideo.client`.
The public client build also passed with `npm run build` from `frontend/morwalpizvideo.client`.

The build reports the existing Vite warning for `/env-config.js` lacking a `type="module"` attribute and the existing stale Browserslist database notice; neither blocks the build.

During the gallery upload fix, the back-office upload path was changed to preserve the original image bytes and dimensions instead of running the shared news resize pipeline. The new `PrepareImageAsIsAsync` method keeps the source format and size, and `GalleriesController` now calls it when adding images to a gallery. This keeps the direct public gallery route and the upload pipeline consistent with the requirement that gallery images are stored as uploaded.

The BackOffice gallery compile blocker was fixed by importing `IGalleryRepository` from `MorWalPizVideo.Server.Services.Interfaces` in `MorWalPizVideo.BackOffice/Controllers/GalleriesController.cs` and `MorWalPizVideo.BackOffice/Jobs/GalleryCleanupJob.cs`. The existing `IBlobService` dependencies were preserved.

Focused backend validation passed with `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ChannelNewsBehaviorTests|FullyQualifiedName~GalleryBehaviorTests" --verbosity minimal` (13 passed, 0 failed).
Release builds passed for `MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj` and `MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj`. The BackOffice and ServerAPI builds still report existing `SixLabors.ImageSharp` vulnerability warnings, but the gallery upload fix itself is validated and green.

## Residual Risks

The gallery API requires `VITE_MORWALPIZ_CHANNEL_ID` at runtime; the loader returns a service-unavailable response when it is absent.

## Back-office gallery management access

Added the missing BackOffice navigation and route access for gallery management:

- `frontend/back-office-spa/src/layouts/adminMenu.ts`: added `Galleries` directly after `Images` in the Content menu, using the existing image view/manage permissions.
- `frontend/back-office-spa/src/router/routes/lazy-index.ts`: registered the existing `src/routes/galleries` module at `/galleries`.
- `frontend/back-office-spa/src/authorization/permissions.ts`: mapped `galleries` route authorization to `permissions.images` so gallery reads and actions follow the existing image permission contract.
- `frontend/back-office-spa/src/layouts/adminMenu.test.ts`: added focused coverage for menu ordering, route registration, and read/action permission mapping.

Focused validation command: `yarn --cwd frontend/back-office-spa vitest run src/layouts/adminMenu.test.ts --reporter=verbose --pool=threads --maxWorkers=1 --minWorkers=1` completed without reported failures. The initial pre-fix invocation stalled before producing assertions, so the test was retained as the regression proof but its pre-fix failure output could not be captured in this environment.

Residual risk: gallery management remains dependent on the existing gallery route module and backend authorization; this change only restores its BackOffice menu and route reachability.

## Gallery selection and presentation controls (2026-10-21)

Updated the public gallery component to support selecting or clearing every image,
with the existing download action continuing to use the selected indexes. The
list view now renders a responsive table with thumbnails and per-row selection,
while grid view retains the card presentation. Checkbox labels and controls have
44px touch targets, and the lightbox dialog/content are constrained to the
viewport with an independently scrollable body.

Modified files:

- `frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx`: added
  select-all/deselect-all behavior and the responsive list-table rendering.
- `frontend/morwalpizvideo.client/src/main.scss`: enlarged checkbox hit areas,
  styled the list table, and constrained modal dimensions to viewport height.

Validation performed:

- `npm test -- --run src/routes/gallery-route.test.tsx` from
  `frontend/morwalpizvideo.client` — passed (1 test).
- `npm run build` from `frontend/morwalpizvideo.client` — passed.

## Gallery selection and list-table refinement (2026-10-21)

Refined the public gallery controls without changing loader, download, or
selection-state behavior. Select all and Deselect all are now independent
toolbar actions with appropriate disabled states. List mode is a two-column
table labeled Filename and Download; row selection remains available beside
each filename while the second column contains only the per-image download
button. Mobile checkbox controls use larger touch targets, and the modal
dialog/content can use the full viewport height while retaining its
scrollable body.

Modified files:

- `frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx`
- `frontend/morwalpizvideo.client/src/main.scss`

Validation performed:

- `npm test -- --run src/routes/gallery-route.test.tsx` from
  `frontend/morwalpizvideo.client` — passed (1 test).
- `npm run build` from `frontend/morwalpizvideo.client` — passed.

Residual risk: browser visual capture was not run; the existing
`/env-config.js` module warning and stale Browserslist database notice are
unchanged.
- `git diff --check` — passed.

The build retains the existing `/env-config.js` module warning and stale
Browserslist database notice. Browser capture was not run.

## Gallery controls and viewport behavior (2026-10-21)

Completed the public gallery interaction patch in the existing route and shared
stylesheet. The gallery now provides touch-sized selection controls,
select-all/deselect-all toolbar actions, a responsive two-column table in List
mode, per-image download actions, and a lightbox constrained to the viewport
with a scrollable body. The table wrapper retains horizontal scrolling on
narrow viewports instead of clipping its two columns.

Modified files:

- `frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx`: owns the
  selection toolbar, list/table rows, per-row downloads, and lightbox markup.
- `frontend/morwalpizvideo.client/src/main.scss`: owns gallery touch targets,
  table responsiveness, and viewport-constrained modal sizing.

Validation performed:

- `npm test -- --run src/routes/gallery-route.test.tsx` from
  `frontend/morwalpizvideo.client` — passed.
- `npm run build` from `frontend/morwalpizvideo.client` — passed.

Browser visual capture was not run. The build retains the existing
`/env-config.js` module warning and stale Browserslist database notice.

## Gallery presentation refinement (2026-10-21)

Updated the public gallery route presentation without changing its loader,
download behavior, or route contract. `Component.tsx` now uses gallery-specific
structure and state labels for the header, toolbar, image grid/list cards, and
lightbox. `main.scss` owns the shared responsive gallery styling, including
card treatment, image cropping, list sizing, modal backdrop, and mobile
adjustments.

Validation performed:

- `frontend/morwalpizvideo.client/node_modules/.bin/vitest.cmd run src/routes/gallery-route.test.tsx` — passed (1 test).
- `npm run build` from `frontend/morwalpizvideo.client` — passed.
- `git diff --check` — passed.

The build retains the existing `/env-config.js` module warning and stale
Browserslist database notice. No gallery-specific residual risks were
identified. The stylesheet's existing formatting was preserved rather than
running a whole-file formatter; visual browser capture was not run.

## Gallery list actions and modal constraints (2026-10-21)

Completed the public gallery presentation controls requested for the gallery
route. The list/table view now uses two columns: the uploaded filename and
row actions. Each row keeps a touch-sized checkbox and provides an individual
download action, while the toolbar retains select-all/deselect-all and bulk
download behavior. Downloaded single images use their source filename and
bulk downloads retain the gallery ZIP filename. The lightbox now includes an
explicit close control and is constrained to the viewport with a scrollable
body.

Modified files:

- `frontend/morwalpizvideo.client/src/routes/gallery/Component.tsx`: added
  filename extraction, per-image downloads, accessible action labels, and a
  close button for the modal.
- `frontend/morwalpizvideo.client/src/main.scss`: constrained modal width and
  height and sized the action column for the two-column table.

Validation performed:

- `npm run build` from `frontend/morwalpizvideo.client` — passed.
- `npm test -- --run src/routes/gallery-route.test.tsx` from
  `frontend/morwalpizvideo.client` — passed.

## Scoped gallery transport regression

The shared frontend transport now treats `/api/galleries` as a channel-scoped BackOffice request, so admin gallery reads receive the selected `X-Channel-Id` header like the other scoped resources.

Modified files:

- `frontend/fe-packages/services/src/apiTransport.ts`: added `/api/galleries` to the scoped request prefixes.
- `frontend/fe-packages/services/src/apiTransport.readiness.test.ts`: added regression coverage for selected-channel injection on gallery requests.

Validation: editor diagnostics report no errors for either touched source file. The focused Vitest command could not run in the current shell because `yarn`, `npm`, `node`, and local Vitest executables were unavailable; no test result is claimed here.

## Final gallery UX validation (2026-10-21)

Revalidated the public gallery route and its gallery-specific stylesheet after
the presentation and list-action updates. The lightbox uses dynamic viewport
height where supported, with a `100vh` fallback, to keep its dialog and image
within the visible mobile viewport. The focused route test passed, and the
production client build passed. The build continues to report the pre-existing
`/env-config.js` module warning and stale Browserslist database notice; neither
blocks the gallery bundle.

Validation performed:

- `npm --prefix frontend\morwalpizvideo.client test -- --run src/routes/gallery-route.test.tsx` — passed (1 test).
- `npm --prefix frontend\morwalpizvideo.client run build` — passed.