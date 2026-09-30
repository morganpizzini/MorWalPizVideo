# Phase 5 Activation And Recovery

This document remains an operations and recovery prompt. The checked-in BackOffice configuration now enables Hangfire, but activation is fail-fast until a durable SQL connection is supplied through secret configuration.

This runbook defines production operations that source changes and local tests cannot prove. Do not mark Phase 5 complete from configuration review alone.

**Authoring status (2026-10-01): COMPLETE for slice 10. Operational closure: BLOCKED. Production approval: NOT GRANTED.** This checklist does not execute operations or authorize activation. Existing feature configuration is retained; cache reliability is deferred, shop is frozen, and Range onboarding/domain cutover remains NOT READY.

## Hangfire Activation (Future Only)

The checked-in BackOffice default uses `FeatureManagement:EnableHangFire=true` with an empty `ConnectionStrings:HangfireConnection` placeholder. Keep the value secret and supply it only after an approved durable SQL store exists; leaving it empty fails startup before any server, scheduler, dashboard, storage, or Hangfire health probe is registered. The dashboard is restricted by the existing authenticated admin-group filter.

Before activation:

1. Provision and approve durable production storage.
2. Grant only the application identity the required database permissions.
3. Set the existing connection key in secret configuration; the feature flag is already enabled in the BackOffice application settings.
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
| `SocialAssetContainerName` | Private media assets; read access only through short-lived SAS |

Prefer managed identity and container-scoped least-privilege Blob roles. Grant BackOffice contributor only to required write containers and ServerAPI reader only to `ContainerName`; do not grant either runtime identity broad recovery access. Use a separate non-production account for recovery where practical. The existing connection-string option remains a compatibility fallback and must stay in secret configuration. Do not place either credential form in evidence.

Social asset SAS responses use User Delegation SAS when `BlobStorage:PreferManagedIdentity=true`. Configure the BackOffice managed identity, the Blob service `Endpoint`, `StorageAccountName`, `SocialAssetContainerName`, and the SAS TTL. Grant `Storage Blob Data Contributor` for upload access and `Storage Blob Delegator` at storage-account scope for `GetUserDelegationKeyAsync`. `StorageAccountKey` is not required in this path and must not be logged. If `PreferManagedIdentity=false`, both `StorageAccountName` and `StorageAccountKey` are required for the explicit Shared Key fallback; keep the key only in environment configuration or Key Vault. The service must never return a public Blob URL. The unique Mongo index `socialassets.channelid_idempotencykey.unique` must be present before production uploads are enabled.

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
| Artifact | Restricted, redacted evidence location and integrity hash; no private URL, cookie, password, token or connection string |
| Approval | Named independent reviewer, approval timestamp/change scope, or NOT GRANTED |

Copy these fields into the existing release/change record for each gate below; do not create empty proof files in the repository. Default record values are: owner role as listed below (named operator not yet assigned), environment NOT SUPPLIED, UTC timestamps NOT RECORDED, artifact NOT ATTACHED, observed result NOT RUN, recovery NOT VERIFIED, approval NOT GRANTED, status BLOCKED. Assignment, screenshots or a procedure alone cannot promote a record to PASS. A performed check with an unexpected result is FAIL, not BLOCKED or skipped. Record local/disposable/target evidence separately with commit SHA, runtime versions, topology and test counts; local results never substitute for target evidence.

## Release Evidence Checklist (2026-10-01)

| Gate / responsible owner | Procedure and objective expected result | Recovery / approval boundary | Current status |
|---|---|---|---|
| R1 Range Mongo capability / database operator + Range API owner | On an explicitly disposable transaction-capable replica set/mongos first, prove writable/logical-session readiness, the actual cross-collection transaction and explicit abort with zero persisted partial writes. Then independently record target topology/capability without using live customer data for destructive races. Unsupported storage must fail closed. | Stop writes on capability loss; restore only a guarded/session-capable SHA. Database operator approves target probe scope. | BLOCKED: no reviewed live/disposable artifact attached |
| R2 Range independent writers, abort/retry / API + database owner | Two independent service/process writers race same-bay overlapping creates, inactive-to-active updates and approval decisions; at most one conflicting active interval survives. Adjacent intervals/different bays remain valid. Inject failure after guard/write and before commit: no partial state remains; transient retry succeeds once or reaches the existing 15-second deadline without unsafe fallback. Record before/after IDs/statuses/UTC intervals and retry counts, not credentials. | Pause all writers, retain failed-case data for audit, reconcile explicitly; never roll back to unguarded writers. Requires reviewed disposable fixture evidence and target capability proof. | BLOCKED: mock/skip results cannot pass this gate |
| R3 Active interval/configuration audit / Range operations owner | While writes are paused, inventory Pending/Approved UTC half-open intervals per bay, malformed/missing timestamps, overlap, historical modes/DST and configuration. Preserve historical intervals; only approved reconciliations may release affected capacity. Expected: no unexplained active overlap or malformed interval; all instances use the guard before resuming. | Quarantine affected capacity, preserve customer history; no automatic rejection/deletion. Operator signs reconciliation and resume decision. | BLOCKED: target interval audit absent |
| R4 Username/index preflight / database + account owner | Audit `Trim().ToLowerInvariant()` usernames, missing/mismatched normalized fields and duplicate groups before startup/index creation. After verified backup and approved additive backfill/reconciliation, rerun read-only preflight; record actual collection/index names/options, including `normalized_username_unique`, session expiry TTL and secondary booking index. Expected: duplicate-free accounts, matching normalization and intended indexes; TTL is not the authorization expiry authority. | Keep writes paused if audit fails; no automatic account merge/deletion. Preserve IDs/hashes in restricted storage only; independently approve backfill/index work. | BLOCKED: no target audit/backfill/index artifact |
| R5 Mongo backup/restore / database recovery owner | Before cutover, export approved backup plus BSON/index/configuration manifest and restore into an isolated protected environment. Verify record counts, identity/interval/session/revision compatibility and approved checksums. Verify application reads/guarded writes against the restored copy. | Retain original and restore evidence; additive fields/collections remain. Resume only after restore owner signs off; no destructive live restore is authorized here. | BLOCKED: recovery drill absent |
| R6 Browser cookie/CSRF/CORS / frontend + security owner | Capture desktop/mobile and direct-navigation/SSR flows from exact approved origins. Verify Secure/HttpOnly/SameSite cookie attributes, pre-login CSRF, post-login/password-change refresh, logout, restored/expired/revoked sessions, disabled/demoted/forced-change accounts and allowed unsafe requests. Missing/invalid CSRF fails; denied/lookalike origins receive no usable credentialed response. Public transport emits no admin cookies/CSRF/channel headers. Record redacted network/status evidence, keyboard/focus/error states and viewport/browser versions. | Stop exposure on auth/CSRF failure; use compatible secure API/client rollback. Full anonymous/user/admin matrix depends on slice 4 approval/implementation, not current anonymous registration. | BLOCKED: no real-browser evidence; slice 4 NOT READY |
| R7 Data Protection continuity/rotation / platform + security owner | Document existing host key storage/encryption, identity permissions, stable application discriminator, multi-instance access and retention. Restart/scale: valid sessions remain readable and revoked/expired sessions remain denied. Rotate keys in approved non-production rehearsal: old/new cookies decrypt during the approved retention window; no raw keys enter evidence. | Retain protected prior keys/discriminator for compatible rollback; revoke sessions deliberately during compromise. Security owner approves rotation and recovery window before action. | BLOCKED: persisted key-ring/rotation proof absent |
| R8 Secret/artifact audit and credential rotation / security + release owner | Scan current source and a clean publish/container artifact with approved non-secret sentinels; credential JSON must not ship. Inventory historical artifacts/history/caches separately. Record protected external YouTube credential provisioning, then independently prove old credentials fail and replacements work after operator-approved rotation; never copy secret values. | Revoke exposed credentials first; isolate old artifacts. Do not restore compromised credentials on rollback. Rotation is a separate approved change, not part of this authoring task. | BLOCKED: metadata exclusions are local evidence only; revocation/artifact invalidation absent |
| R9 Hangfire durable SQL / job + database owner | Approve least-privilege SQL store and existing secret connection key; preserve `news-job`, `youtube-sync-job` and `YouTubeSyncCron`. Prove admin-only dashboard, persisted recurring state after restart, injected failure/retry and idempotent side effects with job IDs/attempts/output counts. Each recurring job needs its own proof; logging success alone is insufficient. | If store/retry safety fails, stop scheduling through existing feature control, preserve durable job state and review before resuming. No provisioning/migration/activation approval is implied. | BLOCKED: durable restart/retry/idempotency artifacts absent |
| R10 Blob exposure/RBAC/lifecycle / storage + security owner | Inventory actual container exposure and runtime/operator identities against existing purposes below. Validate least-privilege public previews/private originals/recovery/social assets, authorized short-lived SAS, denied cross-role access, 30-day retention and approved 7-day temporary cleanup. Record redacted policy/role evidence, not SAS URLs/keys. | Suspend unsafe upload/download paths; preserve originals and approved previous policy. Lifecycle deletion/role changes need separate approval and recovery evidence. | BLOCKED: target role/policy evidence absent |
| R11 Blob recovery / storage recovery owner | Run the private recovery drill below with source/restored ETag, size, metadata and SHA-256; exact checksum match and authorized private access are required. Verify cleanup only after recovery approval. | Retain original/recovery objects until sign-off; no public restore or destructive cleanup. | BLOCKED: no checksum-verified provider restore |
| R12 Readiness/telemetry / platform + owning API/job owner | Record dependency-loss readiness failure separately from process liveness. Export structured started/completed/failed job signals and rejection/retry signals with stable correlation; demonstrate reviewed alerts/thresholds using safe injected faults. No secret/request-body leakage. | Halt unsafe mutations/jobs, restore approved dependencies/configuration and verify recovery signals before resume. | BLOCKED: target telemetry/alert proof absent |
| R13 Calendar CAS/compatibility / Domain + API owner | Run independent Mongo writers with expected revisions; stale update/delete conflicts, channel isolation, missing legacy revision baseline and legacy writes advancing revision must hold. Verify public JSON/MatchUrl compatibility and admin conflict recovery against coordinated clients. | Retain additive revision fields/compatibility delegates; use compatible API-first rollback and preserve conflicting edits. | BLOCKED: target/independent Mongo and browser artifacts absent |
| R14 Delivery and release authorization / CI + release owner | Attach immutable SHA, exact workflow jobs/collected counts, clean containers, builds/typechecks, applicable lint, and reviewed R1-R13 records. All required checks must pass with no empty suites, ignored errors or unchecked replacement builds. Approve account onboarding explicitly before implementing slice 4 and its matrix. | Failed checks stop publish/release; no production action from this checklist. Release owner and security/database owners must explicitly sign approval. | BLOCKED: Ask/desktop failures, admin typecheck/public lint, container/browser/target evidence and onboarding remain open |

### Operator Record Example (Not Evidence)

| Field | Unfilled record |
|---|---|
| Gate / owning source | R2 / Range Mongo repository guarded mutation |
| Owner | Range API + database operator; named assignee NOT ASSIGNED |
| Environment | Disposable URI supplied privately; topology/version NOT RECORDED; never a production customer race database |
| UTC start / finish | NOT RECORDED |
| Change / commit | NOT RECORDED |
| Procedure / expected | Independent-writer overlap and injected pre-commit abort; one active reservation maximum, no partial writes |
| Observed / artifact | NOT RUN / NOT ATTACHED; restricted redacted artifact and integrity hash required |
| Recovery | Pause writers; compatible guarded/session-capable rollback; NOT VERIFIED |
| Approval | NOT GRANTED; named independent reviewer and UTC decision required |
| Status | BLOCKED |

### Approval And Recovery Sequence

1. Assign named owners and safe test environments; obtain procedure approval before accessing infrastructure. Confirm ordinary-user onboarding as administrator-created versus another protected registration flow. Do not implement slice 4 while unresolved.
2. Capture local/disposable proofs and resolve failed software gates without suppressions. Obtain independently reviewed target audits, backups/restores, key-ring and browser evidence.
3. Approve each sensitive action separately: backfill/index, credentials/key rotation, SQL activation and Blob retention/RBAC changes. A PASS for one gate does not approve another.
4. At a later approved deployment, pause Range writes, retain verified backups and compatible artifacts, ensure all instances are guarded/security-capable, deploy compatible API first and client second, then verify before resuming. No deployment is performed by authoring this runbook.
5. On a failed gate, stop exposure/mutations/scheduling as appropriate and preserve evidence. Restore only compatible guarded/security-capable code/configuration; never restore hash projections, permissive CORS, mock production storage or compromised credentials. Release remains BLOCKED until independent approval is recorded.

Never record secrets, tokens, complete connection strings, or private Blob URLs. Do not claim restart continuity, telemetry alerts, lifecycle enforcement, recovery success, or credential revocation until its evidence record passes review.

## Historical Local Evidence Assessment (2026-08-04)

| Criterion | Objective evidence | Status |
|---|---|---|
| Blob client selection, options, metadata, typed failures, and readiness | `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter FullyQualifiedName~BlobStorageConfigurationTests --no-restore`: 15 passed | Pass (local) |
| BackOffice and ServerAPI compile with Blob controls | Built as dependencies of the focused test project | Pass (local) |
| Desktop credential seed cleanup | `CredentialSourceAuditTests`: five identified source/migration artifacts contain placeholders only; importer build succeeded | Pass (current source only) |
| Blob lifecycle and RBAC | No Azure commands, portal output, or reviewed assignment evidence attached | BLOCKED |
| Private checksum-verified restore | No Azure recovery drill record attached | BLOCKED |
| Credential revocation/rotation | No proof that historical credentials no longer authenticate | BLOCKED |
| Hangfire durable restart and retry/idempotency activation review | No approved store or restart drill attached; connection remains an empty checked-in placeholder | BLOCKED |
| Exported Hangfire/Blob telemetry and alerts | No production backend, threshold, or alert-firing evidence attached | BLOCKED |

The accepted Phase 5 source implementation slices are complete. Phase 5 itself is **not complete** because the operational gates above remain open.