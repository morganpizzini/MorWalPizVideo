# Docker Image CI Gate Handoff

## Outcome

Build every active SPA Docker image in central CI without pushing it, alongside the existing API Dockerfile builds. Use the same Buildx implementation in central CI and SPA deployment workflows so a Dockerfile/build-step failure is caught before merge and image-build mechanics do not drift.

The CI gate is not intended to guarantee Azure image pulls, container startup, or platform availability.

## Current Evidence

- Central CI previously built the three API Dockerfiles but not the SPA images that deployment workflows build and push.
- Active SPA image deployments use repository-root frontend build contexts and Dockerfile-specific arguments/cache settings. The FreezerAnalysis SPA requires `VITE_FREEZER_API_BASE_URL`.
- Shop remains on hold and is not included in the active image matrix.
- The local Docker CLI is installed, but this environment has no reachable Docker daemon.

## Implementation

- Added `.github/actions/build-container-image/action.yml` as the shared Buildx setup/build action.
- Expanded `.github/workflows/ci.yml` to build the BackOffice, ServerAPI, and FreezerAnalysis API Dockerfiles plus BackOffice, Ask, public video, Shooting Range, and FreezerAnalysis SPA images. Builds disable cache and pushes.
- Updated the five active SPA deployment workflows to use the same action while preserving their build contexts, FreezerAnalysis build argument, tags, registry cache settings, and deployment-by-SHA behavior. Each deployment workflow also triggers when the shared action changes.
- Updated `docs/architecture/deployment.md` with the current coverage and local validation limitation.

## Validation

- Workflow/action YAML and matrix references reviewed for intended contexts, Dockerfiles, arguments, push behavior, tags, and cache configuration.
- Local Docker image builds could not be run because Docker reports no connection to `/var/run/docker.sock`.
- GitHub Actions execution is still required to prove the clean builds on the hosted runner.

## Residual Risks

- A successful image build does not prove Azure can pull the image or that the container starts and becomes healthy.
- Deployment-time build arguments and registry/cache access remain dependent on the configured GitHub environment values and credentials.
- Shop image coverage remains intentionally excluded while the shop is on hold.
