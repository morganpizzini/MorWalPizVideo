# ADR-019: Editorial Production Cards

- Status: Accepted for the first BackOffice release
- Date: 2026-11-01

## Context

Insight plans, Script Studio documents, Blog drafts, and imported videos are
owned by separate workflows. Operators need a private channel-scoped place to
track one production without duplicating those records or making an upload
mandatory.

## Decision

BackOffice owns an additive `EditorialProductionCard` document in the dedicated
Mongo `editorialProductionCards` collection. The Domain service is responsible
for channel scoping, revision checks, validation, and status transitions.
Contracts expose DTOs rather than Mongo entities. The SPA uses the existing
channel header and permission model. A card status change is never a publish or
upload operation. Optional external YouTube Studio and Google Analytics URLs
are stored on the channel only when they are valid HTTPS URLs and are opened
with safe external-link attributes.

The public ServerAPI and VideoImporter remain unchanged. Private provider
reports are not proxied or copied into BackOffice. References to insight,
content-plan, Blog, and video records remain references and require ownership
validation before automated association is enabled.

## Consequences

Cards can be rolled out API-first without a data backfill or SQLite migration.
The collection and any production indexes must be applied through the governed
Mongo operations process. Manual card editing is available before plan-to-card
and persistent Blog association are implemented. The public client analytics
adapter is a separate first-party, default-deny concern and does not make
BackOffice provider integrations implicit.

## Rollback

Hide the SPA route or remove its permission while leaving the additive
collection untouched. Existing insight, Blog, video, and importer workflows
continue to operate independently. Removing channel report URLs disables only
the external links and does not affect content.
