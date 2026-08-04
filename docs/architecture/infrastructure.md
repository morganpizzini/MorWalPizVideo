# Infrastructure

## Dependency Injection

Each host registers its own composition. Shared libraries expose interfaces and registration helpers only where a stable repeated pattern exists. Constructor injection is the default; static service location is transitional desktop debt.

Repository registration must keep Mongo and mock implementations aligned. Mock mode is permitted only in Development or Test.

## Configuration

Configuration sources include JSON, environment variables, user secrets, optional Key Vault, and runtime frontend environment injection.

Rules:

- Required production secrets fail startup when unavailable.
- Do not log connection strings, tokens, secret prefixes, or credential payloads.
- Key Vault failure must not silently downgrade a production host into insecure configuration.
- Environment-specific values remain outside source.

## Feature Flags

Development enables only:

- `EnableDev`
- `EnableSwagger`

Mocks and external fakes are selected through the explicit `MockScenario` profile rather than enabling every runtime feature flag. Fixture overrides use `IMockScenarioLifecycle.Select`; `Reset` restores the selected baseline and `Reinitialize` replaces the selected scenario without rebuilding a test host. Cache, output cache, Key Vault, Hangfire, and production integrations remain disabled locally unless a focused test requires them.

Feature flags currently include cache, output cache, mocks, Key Vault, Hangfire, CORS, development, and Swagger. Consolidate flags where registration and middleware must move together.

## CORS And Hosts

### Local

Allow all development origins. Never carry this policy outside Development.

### Deployed

- ServerAPI allows `https://morwalpiz.com` without credentials for the current public frontend.
- BackOffice allows `https://morwalpiz-admin-spa.azurewebsites.net` with credentials.
- ShortLinks needs no browser CORS for redirects.
- `https://shorts.morwalpiz.com` is added to ServerAPI only if scripts hosted there call ServerAPI.

CORS does not replace host filtering. Configure actual Azure/custom hosts and trusted forwarded headers independently.

## Blob Storage

Separate containers by exposure:

- Public preview images (`ContainerName`): anonymous read remains enabled.
- Public sponsor and page previews (`SponsorContainerName` and `PageContainerName`): anonymous read remains enabled because current public DTOs compose direct preview URLs.
- Private digital originals and administrative uploads (`UploadContainerName`): no anonymous access.
- Private recovery copies (`RecoveryContainerName`): no anonymous access, restricted operator access, and a separate non-production account where practical.

ServerAPI issues short-lived, read-only SAS URLs after acquisition verification. Public DTOs never expose storage keys.

Operational controls:

- Prefer managed identity and least-privilege Blob roles over connection strings.
- Scope BackOffice Blob Data Contributor to only the preview/upload containers it writes. Do not grant its runtime identity recovery-container access.
- Scope ServerAPI Blob Data Reader to `ContainerName`, the only container it lists. Public sponsor/page previews remain anonymous compatibility reads and require no ServerAPI data role.
- Retain blob and container soft-deleted data and versions for 30 days.
- Delete temporary and recovery artifacts after 7 days through an approved lifecycle rule.
- Persist explicit content type plus SHA-256, size, and upload-time metadata; use the service ETag for recovery evidence and configure preview cache policy at the container/CDN boundary.
- Monitor capacity, latency, egress, authorization failures, and unusual downloads.
- Execute checksum-verified restores only through the private recovery container.
- Prefer managed identity with least-privilege container-scoped Blob roles; retain the existing connection-string keys only as a compatibility fallback.
- Follow `operations/phase5-activation-and-recovery.md` for restore, lifecycle, RBAC, and credential evidence.

## MongoDB

MongoDB is externally managed by the project owner. Source must own index definitions and idempotent initialization or an explicit operational manifest. See [MongoDB Operations](mongo-operations.md).

## Caching

Output cache accelerates public projections. Memory cache is process-local and must not be treated as distributed coherence. Cache registration, middleware, tags, and eviction contracts are deployed as one feature.

## Jobs

Hangfire belongs to BackOffice and remains disabled by default. In disabled mode no server, recurring registration, dashboard, storage health probe, or runtime store dependency is activated. Enabling it requires the existing durable `ConnectionStrings:HangfireConnection`; startup fails fast when that key is empty. Source does not provision SQL or migrations. The existing `/hangfire` dashboard is available only while enabled and requires an authenticated `admin` role. Existing recurring IDs and `YouTubeSyncCron` remain unchanged. Restart durability, safe retries/idempotency, and exported telemetry with approved thresholds are mandatory activation gates; see `operations/phase5-activation-and-recovery.md`.

## Resilience And Telemetry

ServiceDefaults configures standard HTTP resilience and OpenTelemetry. Add host-specific readiness checks for MongoDB, Blob Storage, job storage, and critical dependencies. Liveness must not depend on optional external providers.

## Email Extension Point

No email sender currently exists. When needed, define a provider-neutral transactional sender with a verified `@morwalpiz.com` identity, typed options, named `HttpClient`, mock sender, delivery webhook validation, and health/telemetry. Configure SPF, DKIM, DMARC, bounces, complaints, and suppression handling operationally.