# Domain Model

## Model Style

The model is persistence-oriented and uses immutable C# records with MongoDB attributes. Several entities include behavior through copy-based methods. API DTOs and UI projection helpers are still mixed into Models in places and should move to their owning boundaries.

## Candidate Aggregate Roots

### YouTubeContent

Owns content identity, title/description, URL, thumbnail, categories, free-form tags, video references, linked YouTube videos, and privacy/visibility. It supports single-video and collection forms. YouTube short links are standalone records that reference this aggregate for target validation.

`EffectiveContentType` is derived from attached `VideoRefs`: exactly one is a
single video; zero or multiple references are a collection. `IsLink` is the
compatibility boolean for this rule. The stored BSON `contentType` remains
archival compatibility metadata, not a runtime decision or editable setting.
Admin/public DTOs, v1 string types, and Shooting ITA responses project the
effective type without rewriting storage. Images, links, and the thumbnail
do not count as attachments. The computed property is excluded from BSON/JSON.
See the [classification record](../features/content-import.md#reference-classification).

Tags are aggregate-level free-form strings persisted in the `tags` Mongo element. Legacy documents without the element deserialize to an empty array (`[BsonIgnoreExtraElements]` plus a default value), and both the admin and public contracts always serialize `tags` as `[]` when absent. Normalization is centralized in `MorWalPizVideo.Models.Constraints.ContentTagRules`: values are trimmed, empty/whitespace values are rejected, and duplicates collapse case-insensitively while keeping the first display casing. Bounds are 20 tags per aggregate and 32 characters per tag.

Target boundary:

- Retain video references and content metadata as one aggregate where consistency requires it.
- Replace `IsPrivate` with additive visibility semantics: Public, RegisteredCustomer, EntitlementRequired.
- Keep short links in their own standalone aggregate; content only supplies the referenced video identity used for validation.
- Move UI display conversion out of the persistence entity.

### YTChannel

Owns channel identity and legacy embedded video/idea information. Short links are not owned or read from this aggregate. The relationship between content video references and channel ownership must have one canonical representation.

### CustomForm

Currently owns questions and all responses. Unbounded embedded responses risk MongoDB document growth and write contention. Keep form definition/questions together; migrate responses into a separate collection keyed by form ID.

### Creator Partnership Workspace

BackOffice owns the channel-scoped `Partner` and `Campaign` aggregates. Partner stores contact details and lifecycle status; Campaign references one partner and embeds administrative `TimelineEntry` records with a kind discriminator. Content references and timeline URLs remain strings rather than duplicated content or link aggregates. A dashboard and public API are deferred.

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

### Sponsor

A sponsor belongs to one `YTChannel` through the nullable `channelId` field. New
BackOffice sponsor writes preserve that ownership and ensure one owned
standalone `ShortLink` with `LinkType.CustomUrl`, `SponsorId`, and matching
management channel metadata. `ShortLinkId` is nullable for legacy documents;
public projections use its current target when present and fall back to the
sponsor's persisted `url` when it is missing or unavailable.

Sponsor and owned short-link writes are sequential because MongoDB does not
provide a cross-collection transaction here. Create, update repair, and delete
are idempotent at the application boundary; a later update repairs a missing
owned link. Existing sponsor documents are manually assigned a channel and do
not require an automatic backfill.

### FreezerAnalysis

The FreezerAnalysis database owns published match revisions, immutable competitor score facts, private account profiles and drafts, and frozen analysis artifacts. Shooter identity records and match-result resolutions are currently account-owned; resolution documents reference the match and immutable result ID and use optimistic revisions. A resolution never rewrites an official score fact. Candidate matching uses normalized names and explicitly supplied aliases, but only a user-confirmed mapping affects an account's analysis. Estimate composition uses only strictly earlier published matches with the requesting account's published availability manifest and explicit result mappings. Frozen input packages retain the bounded score projections, source/revision provenance, identity mapping, availability/exclusion sets and target stage definition used by that estimate; the private object reference includes its compressed byte length and SHA-256. Legacy references without a stored length remain readable only after SHA-256 verification. Verification reports are immutable and idempotently keyed by account, frozen snapshot and final revision hash; they retain per-stage actual/forecast errors, interval coverage and missing/DQ/zeroed diagnostics. Cross-account alias sharing, federation-license handling, independent replay and related retention/notice policy remain unimplemented.

## Other Independent Roots

Categories, compilations, pages, sponsors, sponsor applications, configurations, publish schedules, query links, users, API keys, competitions, user requests, and insight records are primarily independent Mongo roots.

`VideoImporterChannelSettings` is a dedicated channel-owned root in the
`videoImporterChannelSettings` collection. It stores a schema version and
opaque JSON payload separately from `YTChannel`; the scoped endpoint derives
`channelId` from the request context. Schema version 1 is owned by VideoImporter
and includes only application preferences, languages/disclaimers, and
value-based publish schedules. The local SQLite rows are a recoverable cache,
not the durable authority. See [ADR-021](adr/ADR-021-video-importer-channel-settings.md).

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
- Sponsor reads and BackOffice mutations are channel-scoped.
- Sponsor-owned short links cannot be deleted through generic short-link CRUD.
- Customer email is unique after normalized comparison when customer identity is introduced.
- Only one active cart exists per anonymous cart identity.
- Free acquisition is unique by owner identity and artifact ID.
- Public DTOs never expose private storage keys.
- Private or entitlement content applies the same policy to metadata, images, and downloads.

## Compatibility

Mongo changes are additive first. Readers tolerate missing legacy fields; backfills are idempotent; old fields remain during a verified compatibility window. Index creation follows duplicate auditing.