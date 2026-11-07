# BackOffice SPA deployment diagnostics

## Outcome
Repair missing Azure authentication before the BackOffice SPA container deployment. The user confirmed the failure message, "Deployment Failed, Error: No credentials found", and requested an Azure login action before deployment. Implementation is authorized by that request; this document is the handoff, not an implemented fix.

## Current Evidence
- [SPA workflow](../../.github/workflows/deploy-back-office-spa.yml): the active deployment uses `azure/webapps-deploy@v3` with an app name and commit-SHA container image.
- Its deployment job authenticates to the container registry through `docker/login-action@v3`, but contains neither `azure/login` nor a deployment publish-profile input. Registry authentication does not establish an Azure management session.
- [BackOffice API workflow](../../.github/workflows/deploy-backoffice-api.yml) provides existing prior art: a separate Azure login precedes package deployment.
- That workflow uses `azure/login@v2` with the `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` secrets, and grants `id-token: write` for OIDC. The SPA workflow currently lacks that permission.
- Node emits the supplied messages when deprecated APIs are used. The excerpts contain no deployment error, exit code, stack trace, resolved action revision, or completion status. They do not establish which dependency emitted the warnings or why the step failed.
- Node 22 setup occurs in the separate test job. Changing that setup does not select the JavaScript runtime used by the deployment action.
- [Deployment architecture](../architecture/deployment.md) describes image-only staging for other applications, but the inspected SPA workflow actively deploys. Source is authoritative for the current workflow; intended operational policy needs confirmation before changing delivery behavior.

## Proposed Design
Treat the warnings and confirmed missing deployment authentication as separate issues. Insert `azure/login@v2` immediately before the existing Web App deployment in the same job, using the same three identity secrets as the BackOffice API workflow. Grant `id-token: write` only to the deployment job and preserve its existing effective `contents: read` and `checks: write` permissions: a job-level permissions declaration overrides inherited permissions. Verify that the Azure identity has a federated credential matching this repository's `production` GitHub environment and appropriate target Web App access. Preserve Docker registry login and all image/test behavior. Do not suppress warnings, downgrade application Node, or change frontend dependencies to repair an action-owned warning.

## Impacted Projects
Only the BackOffice SPA delivery workflow is a potential implementation surface. No .NET project, frontend package, standalone tool, or application behavior change is indicated.

## Impacted Files
- Modify: [SPA workflow](../../.github/workflows/deploy-back-office-spa.yml), for deployment-job OIDC permission and Azure login immediately before deployment.
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
| Validation | Actual workflow error and deployment result required |
| Cache | None |
| Configuration/Secrets | Verify production environment identity settings; never commit credential values |
| Logging/Telemetry | Preserve action errors; warnings must not be hidden as a substitute for diagnosis |
| Background Jobs | None |
| Deployment/Operations | Container deployment prerequisites and intended staging policy |
| Backward Compatibility | Preserve test gates, image tags, target app, and production environment |

## Reuse Opportunities
Reuse the existing Azure login pattern and current commit-SHA image publication. Do not add application-level workarounds for third-party action internals.

## Risks
- High: granting an identity excessive access while repairing authentication. Mitigate with target-scoped deployment permissions; verify authorized deployment without broader grants.
- Medium: assuming warnings caused failure. Mitigate by inspecting the first actionable error and final step status.
- Medium: activating deployment contrary to staging policy. Resolve source/documentation mismatch before delivery-policy changes.
- Low: warnings remain after deployment authentication is repaired. Identify the emitting dependency from a trace or upstream action evidence before proposing an action update.

## Technical Debt
Deployment documentation and this SPA workflow disagree about staging policy. Resolve only the owning surface if implementation proceeds; do not expand into unrelated workflow cleanup. Action dependency modernization is separate from deployment authentication.

## Implementation Roadmap
1. Diagnosis complete: the user supplied the missing-credentials error and authorized adding Azure login before the active deployment.
2. Verify production environment identity secrets, the matching environment-scoped federated credential, and target Web App permissions. Missing environment configuration is an explicit operational blocker, not a reason to introduce committed credentials.
3. Add the deployment-job permissions and same-job login described above. Preserve existing validation, registry authentication, image publication, and deployment inputs.
4. Validate workflow syntax and a controlled deployment; confirm Azure login succeeds, the target app uses the intended SHA image, and the app is responsive. Update this document with actual evidence. Stop without unrelated runtime, frontend, or other workflow changes.

## Testing Strategy
Workflow validation and a controlled deployment are the discriminating checks; application tests cannot prove Azure authentication. Preserve existing frontend test/build gates. Verify login success, image configuration, registry pull access, and application response separately. No new unit, component, contract, security-audit, migration, or database tests are indicated. An independent review is optional for a bounded login change, but warranted if permissions or deployment policy expand.

## Migration And Rollback
No schema, API, auth-cookie, cache, or application configuration migration is needed. If a rollout occurs, retain the previous known-good image reference for operational rollback. Rolling back workflow authentication does not roll back a deployed image. Deployment and registry-pull permissions are distinct prerequisites.

## Open Questions
- Are the production environment's three identity secrets, matching federated credential, and target Web App permissions configured? The repository cannot prove deployed identity configuration.
- The user authorized the active SPA deployment fix; the broader staging description remains documentation debt, not a blocker or permission to change other delivery surfaces.
