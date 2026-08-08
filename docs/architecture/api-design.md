# API Design

## API Surfaces

### BackOffice API

Authenticated administrative commands and queries. Default authorization is appropriate at the host boundary, with explicit anonymous or API-key exceptions.

### ServerAPI

Anonymous public projections and narrowly approved public interactions. Public access is explicit rather than inherited accidentally from development authentication.

### ShortLinks

Unversioned `GET /{code}` redirect surface. It is not a general JSON API.

## Versioning

Target JSON routes use URL-segment versioning:

`/api/v1/{resource}`

Use maintained ASP.NET API versioning and API Explorer support. OpenAPI document labels alone do not constitute versioning. Existing unversioned routes remain compatibility aliases until consumers migrate and telemetry shows zero use.

Version only externally observable contracts. Internal implementation and persistence changes do not require a new API version when compatibility is preserved.

## Resource Conventions

- Plural resource nouns.
- `GET` reads without mutation.
- `POST` creates resources or invokes non-idempotent domain commands.
- `PUT` replaces a complete client-owned representation.
- `PATCH` performs partial updates when implemented consistently.
- `DELETE` removes or deactivates according to documented semantics.
- Maintenance and cache operations are internal commands, not public GET requests.

## DTO Policy

- Requests and responses use explicit DTOs.
- Mongo entities are never returned directly from new endpoints.
- Admin and public DTOs differ where sensitive fields exist.
- IDs, timestamps, enum representation, nullability, and pagination are explicit.
- Storage keys, credential hashes, internal configuration, and private integration metadata are excluded from public responses.

## Validation

- Data annotations enforce request shape.
- Focused application services enforce domain and cross-record invariants.
- Server validation remains authoritative.
- Validation failures return Problem Details with field errors and stable codes.

## Error Contract

Use RFC Problem Details consistently:

- 400 malformed or invalid request.
- 401 missing/invalid authentication.
- 403 authenticated but unauthorized.
- 404 resource not found or intentionally concealed.
- 409 uniqueness/state conflict.
- 422 valid shape but rejected domain operation where appropriate.
- 429 rate limited.
- 500 unexpected failure without sensitive detail.

### Channel scope contract

BackOffice `GET /api/channels` establishes the accessible-channel list. Requests to scoped resources carry the selected channel in `X-Channel-Id`; the header is required even when the caller is an administrator. Missing header returns `400` with `channel_context_required`. Unknown, inaccessible, and API-key binding mismatches return `404` with `channel_context_unavailable`.

The effective impersonated target controls channel and content authorization. API keys carry a persisted channel binding and cannot impersonate. Administrative compilation and short-link management is scoped, while public compilation URL resolution is anonymous and global; public cache keys therefore use the URL, not the administrative channel.

## Authentication Matrix

| Surface | Scheme |
|---|---|
| BackOffice SPA management | JWT bearer or secure cookie |
| VideoImporter/InsightScanner | API key |
| Public content/catalog | Anonymous |
| Anonymous cart/acquisition | Opaque HttpOnly cart cookie |
| Future customer endpoints | Dedicated customer policy |
| Internal cache invalidation | Authenticated service identity |
| Short-link redirect | Anonymous |

API-key management is authenticated with the administrator's BackOffice principal. Creation binds the key to the selected channel; only an administrator may reassign it to another existing channel.

## Shop Contract

- Catalog returns active free-artifact DTOs.
- Preview image is public.
- Add-to-cart derives cart identity from a server-issued cookie.
- Acquisition is idempotent and durable.
- Download validates acquisition and returns a short-lived URL or streamed response.
- No payment intent, payment method, mutable price, or caller-supplied customer/cart owner is accepted.

## Pagination And Querying

Use bounded page sizes and stable ordering. Push filters and sorting to MongoDB. Cursor pagination is preferred for high-volume append-oriented data; offset pagination may remain for bounded administration lists.

## OpenAPI

Publish version-aware OpenAPI for BackOffice and ServerAPI when enabled. Describe JWT, API-key, anonymous-cart cookie, validation, and Problem Details contracts accurately. Swagger remains disabled outside explicitly approved environments.