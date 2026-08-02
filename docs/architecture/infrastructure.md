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

Mocks and external fakes should be selected through an explicit local scenario profile rather than enabling every runtime feature flag. Cache, output cache, Key Vault, Hangfire, and production integrations remain disabled locally unless a focused test requires them.

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

- Public preview images: anonymous read or explicitly public delivery.
- Private digital originals: no anonymous access.
- Administrative uploads: least-privilege write access.

ServerAPI issues short-lived, read-only SAS URLs after acquisition verification. Public DTOs never expose storage keys.

Operational controls:

- Prefer managed identity and least-privilege Blob roles over connection strings.
- Enable blob/container soft delete.
- Enable versioning where overwrite recovery is required.
- Configure lifecycle cleanup for obsolete versions and temporary uploads.
- Set content type, content disposition, checksum, size, ETag, and cache metadata.
- Monitor capacity, latency, egress, authorization failures, and unusual downloads.
- Document restore and key/identity rotation procedures.

## MongoDB

MongoDB is externally managed by the project owner. Source must own index definitions and idempotent initialization or an explicit operational manifest. See [MongoDB Operations](mongo-operations.md).

## Caching

Output cache accelerates public projections. Memory cache is process-local and must not be treated as distributed coherence. Cache registration, middleware, tags, and eviction contracts are deployed as one feature.

## Jobs

Hangfire belongs to BackOffice. Production storage must be durable. Dashboard access is authenticated and restricted. Jobs are idempotent, retry-aware, observable, and safe under repeated execution.

## Resilience And Telemetry

ServiceDefaults configures standard HTTP resilience and OpenTelemetry. Add host-specific readiness checks for MongoDB, Blob Storage, job storage, and critical dependencies. Liveness must not depend on optional external providers.

## Email Extension Point

No email sender currently exists. When needed, define a provider-neutral transactional sender with a verified `@morwalpiz.com` identity, typed options, named `HttpClient`, mock sender, delivery webhook validation, and health/telemetry. Configure SPF, DKIM, DMARC, bounces, complaints, and suppression handling operationally.