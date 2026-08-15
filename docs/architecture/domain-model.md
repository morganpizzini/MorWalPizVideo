# Domain Model

## Model Style

The model is persistence-oriented and uses immutable C# records with MongoDB attributes. Several entities include behavior through copy-based methods. API DTOs and UI projection helpers are still mixed into Models in places and should move to their owning boundaries.

## Candidate Aggregate Roots

### YouTubeContent

Owns content identity, title/description, URL, thumbnail, categories, video references, linked YouTube videos, and privacy/visibility. It supports single-video and collection forms. YouTube short links are standalone records that reference this aggregate for target validation.

Target boundary:

- Retain video references and content metadata as one aggregate where consistency requires it.
- Replace `IsPrivate` with additive visibility semantics: Public, RegisteredCustomer, EntitlementRequired.
- Keep short links in their own standalone aggregate; content only supplies the referenced video identity used for validation.
- Move UI display conversion out of the persistence entity.

### YTChannel

Owns channel identity and legacy embedded video/idea information. Short links are not owned or read from this aggregate. The relationship between content video references and channel ownership must have one canonical representation.

### CustomForm

Currently owns questions and all responses. Unbounded embedded responses risk MongoDB document growth and write contention. Keep form definition/questions together; migrate responses into a separate collection keyed by form ID.

### DigitalProduct

Represents a permanently free digital artifact. Target semantics:

- Stable product/artifact ID.
- Public metadata and preview.
- Private original storage key retained only in persistence/admin contracts.
- `IsActive` controls catalog visibility.
- Price/payment fields are removed after compatibility migration.
- Existing free artifacts never become paid editions.

### Cart And FreeAcquisition

Cart owns a transient set of requested artifacts. `FreeAcquisition` is a separate durable record proving that an anonymous cart or future customer obtained the artifact while free. Adding the same artifact is idempotent.

### ShortLink

Target aggregate fields:

- Normalized globally unique code.
- Destination kind.
- Validated absolute HTTP/HTTPS URI.
- Optional internal content/channel reference.
- Preserved query parameters.
- Enabled status and audit timestamps.
- Atomic aggregate click count.

Detailed visit events belong in `shortLinkVisits`, not the aggregate document.

## Other Independent Roots

Categories, compilations, pages, sponsors, sponsor applications, configurations, publish schedules, query links, users, API keys, competitions, user requests, and insight records are primarily independent Mongo roots.

## Value Object Opportunities

- Normalized email address.
- Short-link code.
- Validated destination URI.
- Content visibility.
- Blob artifact locator.
- Category and video references.

Introduce value objects only when they centralize an enforced invariant and remain serializer-compatible.

## Invariants

- Short-link code is globally unique after lowercase invariant normalization.
- Customer email is unique after normalized comparison when customer identity is introduced.
- Only one active cart exists per anonymous cart identity.
- Free acquisition is unique by owner identity and artifact ID.
- Public DTOs never expose private storage keys.
- Private or entitlement content applies the same policy to metadata, images, and downloads.

## Compatibility

Mongo changes are additive first. Readers tolerate missing legacy fields; backfills are idempotent; old fields remain during a verified compatibility window. Index creation follows duplicate auditing.