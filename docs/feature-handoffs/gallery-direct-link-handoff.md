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

## Scoped gallery transport regression

The shared frontend transport now treats `/api/galleries` as a channel-scoped BackOffice request, so admin gallery reads receive the selected `X-Channel-Id` header like the other scoped resources.

Modified files:

- `frontend/fe-packages/services/src/apiTransport.ts`: added `/api/galleries` to the scoped request prefixes.
- `frontend/fe-packages/services/src/apiTransport.readiness.test.ts`: added regression coverage for selected-channel injection on gallery requests.

Validation: editor diagnostics report no errors for either touched source file. The focused Vitest command could not run in the current shell because `yarn`, `npm`, `node`, and local Vitest executables were unavailable; no test result is claimed here.