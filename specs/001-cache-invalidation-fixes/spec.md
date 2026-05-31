# Feature Specification: Cache invalidation correctness and high-impact code-review fixes

**Feature Branch**: `001-cache-invalidation-fixes`

**Created**: 2026-05-31

**Status**: Draft

**Input**: User description: "Cache invalidation correctness and high-impact code-review fixes"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reliable admin mutations with cache invalidation (Priority: P1)

A back-office administrator edits, creates, or deletes a BioLink, ShortLink, or Video through the admin SPA. The change is persisted, all server caches are invalidated, and the SPA confirms success without surfacing a spurious error. Public visitors loading the corresponding page see the updated content on the next request, not stale cached output.

**Why this priority**: This is the regression that is actively misleading admins (false error toasts on successful writes) and serving stale content to end users. It is the core motivation for the feature and unblocks day-to-day editorial work.

**Independent Test**: From the admin SPA, mutate a BioLink. Verify the SPA receives a 2xx response with no error toast, then fetch the corresponding public endpoint and confirm the new value is returned immediately (not after TTL expiry).

**Acceptance Scenarios**:

1. **Given** an existing BioLink and a public endpoint whose response is currently cached, **When** the admin updates the BioLink via the SPA, **Then** the SPA shows a success state and the next public request returns the updated payload.
2. **Given** a ShortLink mutation succeeds on the server, **When** the in-memory cache reset and output cache eviction both complete, **Then** the SPA receives a successful response (no 5xx/4xx surfaced from cache plumbing).
3. **Given** a Video is deleted via the admin SPA, **When** the operation completes, **Then** the public listing no longer includes the deleted item on the next request.

---

### User Story 2 - Consistent output cache tag invalidation (Priority: P1)

Server-side response caching invalidates tagged entries reliably regardless of how the tag was written by attribute authors. An admin mutation that targets a tag evicts every cached response that declared that tag, even if casing differs across declarations.

**Why this priority**: Output cache tag mismatches silently leave stale content live until TTL. The fix is small but a precondition for User Story 1 to be observably correct on every endpoint.

**Independent Test**: Annotate two endpoints with the same tag in different casing (e.g., `biolinks` and `BioLinks`), trigger an eviction for that tag, and confirm both endpoints return fresh data on the next request.

**Acceptance Scenarios**:

1. **Given** two controllers tagged with the same logical tag but different letter casing, **When** the eviction routine runs for that tag, **Then** both cached responses are removed.
2. **Given** a developer adds a new `[OutputCache(Tags=[...])]` attribute, **When** they follow the documented convention, **Then** their tag is automatically compatible with all eviction call sites.

---

### User Story 3 - No duplicate BioLink ordering under concurrency (Priority: P2)

When multiple BioLink create or reorder operations execute concurrently, the resulting `Order` values remain unique and contiguous. No two BioLinks end up with the same `Order`.

**Why this priority**: Duplicate orders corrupt the displayed list, but the race window is narrow. Important to fix, lower urgency than the visible-on-every-mutation cache bug.

**Independent Test**: Issue 10 parallel BioLink create requests against an empty collection and assert each resulting record has a distinct `Order` value.

**Acceptance Scenarios**:

1. **Given** an empty BioLink collection, **When** 10 create requests are issued in parallel, **Then** all 10 records have distinct `Order` values.
2. **Given** an existing list of BioLinks, **When** two concurrent reorder operations run, **Then** the final state has no duplicate `Order` values.

---

### User Story 4 - Reliable HTTP client and resource lifecycle in back-office (Priority: P2)

Long-running back-office processes do not exhaust HTTP sockets or leak handlers. Sustained admin activity over hours or days does not degrade outbound HTTP performance or trigger socket exhaustion errors.

**Why this priority**: The defect is latent (manifests only under sustained load or long uptime) but trivial to introduce and hard to diagnose later. Fix while the surface area is small.

**Independent Test**: Run a sustained loop of admin mutations (e.g., 1000 sequential operations) and confirm the process does not accumulate `TIME_WAIT` sockets beyond the factory's expected handler lifetime, and that no `HttpClient` is constructed outside the factory.

**Acceptance Scenarios**:

1. **Given** the back-office service is running, **When** any outbound HTTP call is made, **Then** the call uses a client obtained from `IHttpClientFactory` and is not wrapped in `using`.
2. **Given** a service that depends on `IHttpClientFactory`, **When** the service is registered in DI, **Then** it does not implement `IDisposable` for the purpose of disposing factory-managed clients.
3. **Given** the Pinterest integration code, **When** it issues an outbound request, **Then** no `new HttpClient()` allocation occurs.

---

### User Story 5 - Reliable async data access and observable API-key telemetry (Priority: P3)

Admin API actions do not block thread-pool threads on synchronous Mongo calls, and admin API-key usage telemetry is updated reliably with failures logged rather than silently swallowed.

**Why this priority**: Correctness and observability improvements with low immediate user impact, but cheap to bundle with the other fixes in this feature.

**Independent Test**: Static review (or analyzer) confirms no synchronous Mongo calls in `async Task<IActionResult>` methods. Force a failure in the API-key "last used" update path and confirm an error log entry is produced.

**Acceptance Scenarios**:

1. **Given** any admin controller action declared `async`, **When** it accesses Mongo, **Then** it uses an `*Async` API.
2. **Given** an authenticated admin request, **When** the API-key "last used" update fails, **Then** an error is logged with sufficient context to diagnose the failure.
3. **Given** an authenticated admin request, **When** the API-key "last used" update succeeds, **Then** the timestamp reflects the request time.

---

### Edge Cases

- Admin mutation succeeds on the data store but the cache invalidation endpoint is temporarily unreachable: the admin response must still convey the write outcome accurately (data persisted; cache may be stale until next TTL). Failures in cache invalidation must be logged.
- Output cache tag is declared with unicode or accented characters: the casing-normalization rule must use an invariant, culture-insensitive comparison.
- Concurrent BioLink creates that race with a reorder: the atomic ordering scheme must remain correct under the combined workload.
- API-key "last used" update fires after the response has been written: failures must not affect the response status code returned to the client.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Admin mutation endpoints for BioLinks, ShortLinks, and Videos MUST return a success response to the SPA whenever the underlying data write succeeds.
- **FR-002**: Admin mutation endpoints MUST successfully invalidate the in-memory cache for affected keys after a successful write.
- **FR-003**: Admin mutation endpoints MUST successfully invalidate the output cache for affected tags after a successful write.
- **FR-004**: The back-office service's cache invalidation HTTP calls MUST agree with the public API's cache invalidation endpoint contract (path, verb, and parameter shape).
- **FR-005**: Output cache tag invalidation MUST be case-insensitive: a tag declared on a cached response in any letter casing MUST be evicted when an invalidation request is issued for the same tag in any other casing.
- **FR-006**: Output cache tag declarations and eviction call sites in the codebase MUST follow a single documented casing convention (lowercase) so future additions remain consistent without depending on the case-insensitive safety net.
- **FR-007**: Concurrent BioLink create operations MUST NOT produce two records with the same `Order` value.
- **FR-008**: Concurrent BioLink reorder operations MUST NOT leave the collection with duplicate `Order` values.
- **FR-009**: The back-office service MUST obtain all outbound `HttpClient` instances from `IHttpClientFactory`; no code path may instantiate `HttpClient` directly.
- **FR-010**: Code that obtains an `HttpClient` from `IHttpClientFactory` MUST NOT wrap the client in a `using` block or otherwise dispose it.
- **FR-011**: Services that depend on `IHttpClientFactory`-managed clients MUST NOT implement `IDisposable` for the purpose of disposing those clients.
- **FR-012**: Admin controller actions declared `async` MUST use asynchronous Mongo APIs (no `Find().ToList()`, no `DeleteOne()`, etc.) when accessing Mongo.
- **FR-013**: Updates to the admin API-key "last used" timestamp MUST persist successfully on the happy path and MUST emit a log entry at error severity when they fail.
- **FR-014**: Failure of the API-key "last used" update MUST NOT change the HTTP status code returned to the caller for the originating request.
- **FR-015**: Existing public site and admin SPA contracts (request/response shapes, status codes, authentication semantics) MUST remain backwards-compatible.
- **FR-016**: The change MUST NOT introduce new external runtime dependencies.
- **FR-017**: Automated tests MUST cover the cache-purge URL contract between back-office and public API.
- **FR-018**: Automated tests MUST cover the atomic BioLink ordering guarantee under concurrent creation.

### Key Entities

- **BioLink**: An admin-managed link displayed on the public landing page. Has a unique identifier and an `Order` value that determines display position; uniqueness of `Order` must hold across concurrent mutations.
- **ShortLink**: An admin-managed short URL → target URL mapping. Mutations must invalidate the corresponding public caches.
- **Video**: An admin-managed video resource. Mutations must invalidate the corresponding public caches.
- **Output Cache Tag**: A string label associated with a cached response, used to invalidate groups of cached responses together. Comparison for invalidation is case-insensitive; declarations should use a single documented casing.
- **In-Memory Cache Key**: A key identifying a cached value in the back-office in-memory cache, reset as part of admin mutations.
- **Admin API Key**: A credential used by the admin SPA to authenticate to the back-office. Carries a "last used" timestamp updated on every authenticated request.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Mutating a BioLink, ShortLink, or Video from the admin SPA returns a 2xx response and surfaces no error indication in the SPA in 100% of cases where the underlying data write succeeds.
- **SC-002**: A public endpoint whose response is currently served from the output cache returns the updated payload on the very next request after a corresponding admin mutation, with no wait for cache expiry.
- **SC-003**: 10 BioLink create requests issued in parallel against an empty collection produce 10 records, all with distinct `Order` values, in 100% of trial runs.
- **SC-004**: A code audit of the back-office finds zero occurrences of `new HttpClient(` and zero `using` statements wrapping a client returned by `IHttpClientFactory.CreateClient(...)`.
- **SC-005**: A code audit of `async Task<IActionResult>` methods in the back-office finds zero synchronous Mongo calls.
- **SC-006**: When the API-key "last used" update path is forced to fail in a test, exactly one error-level log entry is produced and the originating request's status code is unchanged.
- **SC-007**: All pre-existing automated tests continue to pass after the change.

## Assumptions

- The public-site cache invalidation endpoint will adopt the back-office's existing call shape (query parameter) rather than the inverse, so the SPA's existing behaviour and the existing call sites in `CrossApiService` change minimally. Final direction (query vs. path) is an implementation decision in `/speckit.plan`; either is acceptable as long as both sides agree.
- "Lowercase" is the chosen casing convention for output cache tags; case-insensitive comparison is the runtime safety net but new code is expected to follow the convention.
- BioLink ordering is implemented in MongoDB; the atomic ordering guarantee can be achieved with an atomic update operator (e.g., `$inc`) without introducing a new dependency.
- The back-office already registers `IHttpClientFactory`; the fix is to consume it correctly rather than to introduce it.
- Logging infrastructure (`ILogger<T>`) is already available in `ApiKeyAuthenticationHandler` / `ApiKeyService`; no new logging dependency is required.
- The following items are explicitly out of scope and deferred to follow-up specs: ShopCart optimistic concurrency, cache-stampede protection in `IMorWalPizCache.GetOrCreate`, WPF `VideoImporter` async-void sweep, MongoDB index review, Polly resilience policies, frontend double-submit / `AbortController` cleanup, and batching the 2–3 sequential cache calls into a single endpoint.
