# Phase 5 Activation And Recovery (Deferred)

This document is a future operations prompt only. Hangfire activation and all related recovery automation are deferred; nothing in the current video-platform implementation activates or changes Hangfire.

This runbook defines production operations that source changes and local tests cannot prove. Do not mark Phase 5 complete from configuration review alone.

## Hangfire Activation (Future Only)

Hangfire remains disabled by default through `FeatureManagement:EnableHangFire=false`. Keep `ConnectionStrings:HangfireConnection` as a secret placeholder until an approved durable SQL store exists. Enabling the flag without that connection fails startup before any server, scheduler, dashboard, storage, or Hangfire health probe is registered.

Before activation:

1. Provision and approve durable production storage.
2. Grant only the application identity the required database permissions.
3. Set the existing connection key, then enable the existing feature flag.
4. Confirm `/hangfire` rejects anonymous and non-admin users and accepts only an authenticated `admin`.
5. Confirm recurring IDs remain `news-job` and `youtube-sync-job`; retain `YouTubeSyncCron` for the latter.
6. Restart the application and prove recurring-job continuity from the durable store.

Structured started, completed, and failed events are emitted with stable event IDs and `JobId`, `JobStatus`, `TimestampUtc`, and `DurationMilliseconds`. Production telemetry backend selection, thresholds, and alert proof remain deferred.

## Blob Controls

Container exposure must be verified in Azure:

| Configuration key | Required exposure |
|---|---|
| `ContainerName` | Public preview read remains public |
| `UploadContainerName` | Private originals/admin uploads |
| `SponsorContainerName` | Public sponsor previews remain public; BackOffice write is authorized |
| `PageContainerName` | Public page previews remain public; BackOffice write is authorized |
| `RecoveryContainerName` | Private recovery, restricted operator access |

Prefer managed identity and container-scoped least-privilege Blob roles. Grant BackOffice contributor only to required write containers and ServerAPI reader only to `ContainerName`; do not grant either runtime identity broad recovery access. Use a separate non-production account for recovery where practical. The existing connection-string option remains a compatibility fallback and must stay in secret configuration. Do not place either credential form in evidence.

Configure and verify 30-day blob soft delete, container soft delete, and version retention. Configure approved lifecycle rules that delete temporary and recovery artifacts after 7 days. Public preview behavior must remain unchanged.

## Recovery Drill

1. Select a non-sensitive private original and record its source ETag, size, metadata, and SHA-256.
2. Copy it into the private recovery container without making either object public.
3. Restore to a private temporary path.
4. Download through an authorized operator path and calculate SHA-256.
5. Require an exact checksum match before declaring success.
6. Remove the temporary restore according to the 7-day cleanup policy.

Record commands or portal queries and redacted output. A local checksum unit test is not recovery evidence.

## Credential Administration

Credential revocation and rotation are administrator-owned. The administrator must revoke or rotate affected API keys, connection strings, and integration credentials, verify old credentials no longer authenticate, and attach independently verifiable redacted evidence. Source placeholders and secret scanning do not prove revocation.

## Evidence Template

Create one record per gate with:

| Field | Required value |
|---|---|
| Gate | Hangfire restart, dashboard authorization, Blob lifecycle/RBAC, recovery drill, telemetry alert, or credential revocation |
| Environment | Exact Azure subscription/resource group/app/storage identifiers, redacted where necessary |
| Timestamp | UTC start and completion |
| Operator | Named responsible administrator |
| Change reference | Deployment, ticket, or pull request identifier |
| Procedure | Exact commands or portal queries used |
| Expected result | Objective pass condition |
| Redacted result | Output sufficient for independent verification |
| Rollback | Tested or documented rollback action |
| Status | Pass, fail, or blocked |

Never record secrets, tokens, complete connection strings, or private Blob URLs. Do not claim restart continuity, telemetry alerts, lifecycle enforcement, recovery success, or credential revocation until its evidence record passes review.

## Evidence Assessment (2026-08-04)

| Criterion | Objective evidence | Status |
|---|---|---|
| Blob client selection, options, metadata, typed failures, and readiness | `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter FullyQualifiedName~BlobStorageConfigurationTests --no-restore`: 15 passed | Pass (local) |
| BackOffice and ServerAPI compile with Blob controls | Built as dependencies of the focused test project | Pass (local) |
| Desktop credential seed cleanup | `CredentialSourceAuditTests`: five identified source/migration artifacts contain placeholders only; importer build succeeded | Pass (current source only) |
| Blob lifecycle and RBAC | No Azure commands, portal output, or reviewed assignment evidence attached | Pending administrator evidence |
| Private checksum-verified restore | No Azure recovery drill record attached | Pending administrator evidence |
| Credential revocation/rotation | No proof that historical credentials no longer authenticate | Pending administrator evidence |
| Hangfire durable restart and retry/idempotency activation review | No approved store or restart drill attached; feature remains disabled | Pending future activation |
| Exported Hangfire/Blob telemetry and alerts | No production backend, threshold, or alert-firing evidence attached | Pending operations evidence |

The accepted Phase 5 source implementation slices are complete. Phase 5 itself is **not complete** because the operational gates above remain open.