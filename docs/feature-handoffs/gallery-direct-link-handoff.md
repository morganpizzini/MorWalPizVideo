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

The BackOffice gallery compile blocker was fixed by importing `IGalleryRepository` from `MorWalPizVideo.Server.Services.Interfaces` in `MorWalPizVideo.BackOffice/Controllers/GalleriesController.cs` and `MorWalPizVideo.BackOffice/Jobs/GalleryCleanupJob.cs`. The existing `IBlobService` dependencies were preserved.

Focused backend validation passed with `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~GalleryBehaviorTests" --verbosity minimal` (7 passed).
Release builds passed for `MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj` and `MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj`. The BackOffice build continues to report the existing SixLabors.ImageSharp vulnerability warnings.

## Residual Risks

The gallery API requires `VITE_MORWALPIZ_CHANNEL_ID` at runtime; the loader returns a service-unavailable response when it is absent.