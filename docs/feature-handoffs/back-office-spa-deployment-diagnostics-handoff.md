# BackOffice SPA deployment diagnostics

## Outcome
Added Azure OIDC authentication before the BackOffice SPA container deployment. The user confirmed the failure message, "Deployment Failed, Error: No credentials found".

## Current Evidence
- [SPA workflow](../../.github/workflows/deploy-back-office-spa.yml): the active deployment uses `azure/webapps-deploy@v3` with an app name and commit-SHA container image.
- Before this change, its deployment job authenticated to the container registry through `docker/login-action@v3`, but contained neither `azure/login` nor a deployment publish-profile input. Registry authentication does not establish an Azure management session.
- [BackOffice API workflow](../../.github/workflows/deploy-backoffice-api.yml) provides existing prior art: a separate Azure login precedes package deployment.
- That workflow uses `azure/login@v2` with the `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` secrets, and grants `id-token: write` for OIDC.
- Node emits the supplied messages when deprecated APIs are used. The excerpts contain no deployment error, exit code, stack trace, resolved action revision, or completion status. They do not establish which dependency emitted the warnings or why the step failed.
- Node 22 setup occurs in the separate test job. Changing that setup does not select the JavaScript runtime used by the deployment action.
- [Deployment architecture](../architecture/deployment.md) describes image-only staging for other applications, but the inspected SPA workflow actively deploys. Source is authoritative for the current workflow; intended operational policy needs confirmation before changing delivery behavior.

## Implemented Design
Added job-scoped `contents: read`, `checks: write`, and `id-token: write` permissions to the image deployment job, preserving its previously inherited permissions while enabling OIDC. Added `azure/login@v2` immediately before Web App deployment with the same three identity secrets used by the BackOffice API workflow. Docker registry login, image build/push, tests, and deployment inputs remain unchanged. The Node deprecation warnings are separate and were not altered.

## Impacted Projects
Only the BackOffice SPA delivery workflow is a potential implementation surface. No .NET project, frontend package, standalone tool, or application behavior change is indicated.

## Impacted Files
- Modified: [SPA workflow](../../.github/workflows/deploy-back-office-spa.yml), for deployment-job OIDC permission and Azure login immediately before deployment.
- Add: None identified.
- Generated/Migration: Not applicable.
- Documentation: this diagnostic handoff; reconcile [deployment architecture](../architecture/deployment.md) if active deployment is confirmed as intended.
- Verify Only: [BackOffice API workflow](../../.github/workflows/deploy-backoffice-api.yml), for login prior art.

## Impact Matrix
| Area | Impact |
|---|---|
| Backend/API | No source change |
| Frontend/UI | No source or visual change; Impeccable not applicable |
| Database/Data | None |
| Contracts/Models | None |
| Authentication/Authorization | Azure deployment identity only; verify target-resource access |
| Validation | YAML syntax passed; actual Azure deployment not run |
| Cache | None |
| Configuration/Secrets | Verify production environment identity settings; never commit credential values |
| Logging/Telemetry | Preserve action errors; warnings must not be hidden as a substitute for diagnosis |
| Background Jobs | None |
| Deployment/Operations | Container deployment prerequisites and intended staging policy |
| Backward Compatibility | Preserve test gates, image tags, target app, and production environment |

## Reuse Opportunities
Reuse the existing Azure login pattern and current commit-SHA image publication. Do not add application-level workarounds for third-party action internals.

## Risks
- Medium: deployment still requires the production environment identity secrets, a federated credential matching the `production` GitHub environment, and target Web App access. These environment settings were not verifiable from repository source.
- Medium: deployment documentation describes image-only staging for other applications, while this SPA workflow actively deploys. The user confirmed the intended action for this failure; broader delivery-policy reconciliation remains out of scope.
- Low: Node deprecation warnings may remain because they are emitted independently of Azure authentication.

## Technical Debt
Deployment documentation and this SPA workflow disagree about staging policy. Resolve only the owning surface in a separate task; no unrelated workflow cleanup or action dependency modernization was included.

## Validation And Residual Risks
- Parsed the workflow successfully with Python PyYAML; `git diff --check` passed.
- A Ruby-based check was unavailable because Ruby is not installed; it was not needed after the YAML parser succeeded.
- No controlled Azure deployment was run. Verify environment secrets, federated credential configuration, and Web App permissions in GitHub/Azure, then confirm deployment uses the intended SHA image and the app is responsive.
- Frontend tests/build were not rerun because this change only modifies workflow authentication and YAML structure; their existing gate remains in the workflow.

## Testing Strategy
Workflow validation and a controlled deployment are the discriminating checks; application tests cannot prove Azure authentication. Preserve existing frontend test/build gates. Verify login success, image configuration, registry pull access, and application response separately. No new unit, component, contract, security-audit, migration, or database tests are indicated. An independent review is optional for a bounded login change, but warranted if permissions or deployment policy expand.

## Migration And Rollback
No schema, API, auth-cookie, cache, or application configuration migration is needed. If a rollout occurs, retain the previous known-good image reference for operational rollback. Rolling back workflow authentication does not roll back a deployed image. Deployment and registry-pull permissions are distinct prerequisites.

## Open Questions
- Are the production environment's three identity secrets, matching federated credential, and target Web App permissions configured? The repository cannot prove deployed identity configuration.
- The user authorized the active SPA deployment fix; the broader staging description remains documentation debt, not a blocker or permission to change other delivery surfaces.
