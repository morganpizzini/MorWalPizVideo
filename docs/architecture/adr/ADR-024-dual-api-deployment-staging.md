# ADR-024: Dual API Deployment During Container Image Staging

- **Status:** Accepted
- **Date:** 2026-11-06
- **Supersedes:** ADR-023 for BackOffice API, ServerAPI, and ShortLinks only

## Context

Container image publication is being introduced incrementally, but the migration
will take time. BackOffice API, ServerAPI, and ShortLinks must continue their
existing Azure Web App code-package deployments while their Docker images are also
built and pushed. Other application workflows remain image-only staging as defined
by ADR-023.

## Decision

The BackOffice API, ServerAPI, and ShortLinks workflows retain separate code
package deployment jobs in addition to their image-push jobs. Both jobs depend on
the owning workflow's existing validation job. The image deployment action remains
disabled; pushing an image does not change the running App Service.

ServerAPI continues its anonymous account bootstrap smoke test after its code
package deployment. The three workflows retain Azure OIDC permission for the
package deployment. Other workflows retain image-only behavior and do not receive
Azure OIDC permission for an inactive App Service deployment step.

## Alternatives

- Replace the code deployment immediately with container deployment: rejected
  because the runtime transition is not ready.
- Let Docker build/push success gate the existing code deployment: rejected
  because image publication must not interrupt the established code-release path
  during the migration.
- Remove the image-push job for these APIs: rejected because the registry build
  and cache need to be exercised in parallel with the existing delivery path.

## Consequences

Each API workflow can deploy the validated code package even if the parallel image
publication fails. The overall workflow still reports the image failure. Azure
deployment and image publication each use the protected `production` environment.
No application is switched to consume the image by this decision.

## Migration And Rollback

Keep both paths until each API's container runtime configuration, health/readiness
checks, and rollback have been verified. A later decision can disable package
deployment and enable image deployment for an individual API. Reverting this
decision restores ADR-023 image-only staging; it does not delete pushed images or
change the currently deployed application.

## Validation

Workflow YAML parsing and structural checks verify both deployment jobs depend on
the existing test gate, the image deployment action remains skipped, and the code
deployment remains active. Cloud execution remains subject to GitHub Actions and
the protected environment.
