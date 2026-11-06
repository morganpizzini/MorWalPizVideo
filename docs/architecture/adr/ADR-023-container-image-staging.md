# ADR-023: Container Image Staging Before App Service Deployment

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

The application portfolio is moving toward Docker-based delivery, but changing
running Azure Web Apps to consume new images must happen incrementally. Existing
deployment workflows already build some frontend images; backend workflows still
publish ZIP packages. The current Azure Web Apps must remain untouched while the
container images and build pipeline are prepared.

## Decision

Every existing application deployment workflow validates its source, then builds
and pushes a container image to the configured Azure Container Registry. Images
are tagged with the commit SHA and `latest`. Buildx imports and exports a
per-application registry cache using a `buildcache` tag.

Azure Web App deployment steps remain in the workflows but are explicitly skipped.
The ServerAPI post-deployment smoke test is skipped as well. Publishing an image
does not authorize changing an App Service, runtime configuration, or the shop's
pre-production hold. Registry credentials continue to come from the protected
GitHub `production` environment; these image-only jobs do not need Azure OIDC
permissions.

## Alternatives

- Continue ZIP-based deployment while preparing Dockerfiles separately: rejected
  because it delays exercising image builds and registry credentials.
- Automatically deploy pushed images to Azure Web Apps: rejected because runtime
  configuration, health verification, and rollback need a separate rollout.
- Remove the Azure deployment steps: rejected for now because retaining a clearly
  disabled step makes the temporary transition and reactivation point explicit.

## Consequences

The production App Services continue running their currently deployed versions.
Each successful workflow publishes a SHA-addressed image and updates `latest` and
the registry cache tag. Registry storage grows until an operator establishes a
retention policy. The shop image is staged only; its runtime/product hold remains.

## Migration And Rollback

This is the additive preparation stage; no deployed app or image is removed.
When an app is ready for container deployment, re-enable its Azure Web App image
deployment, confirm required runtime configuration and health checks, and retain a
known-good image SHA for rollback.

To roll back the preparation stage, revert the workflow changes. Existing deployed
apps remain unchanged, and already-pushed image tags are left for registry
retention management rather than being deleted automatically.

## Validation

Central CI performs clean, non-pushing Buildx builds for all five API Dockerfiles
and five active SPA images. Deployment jobs reuse the shared Buildx composite action
with registry cache import/export. Cloud workflow execution and registry access
remain to be verified by GitHub Actions.
