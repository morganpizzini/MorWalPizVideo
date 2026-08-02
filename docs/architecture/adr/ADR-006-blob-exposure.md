# ADR-006: Public Previews And Private Originals

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Images render through `<img>` tags, while original artifacts should download after acquisition. Current services return direct Blob URLs and public entities expose storage keys. ShortLinks was considered as a media endpoint.

## Decision

Keep previews in a public-read container for ordinary image display. Keep original artifacts in a private container. ServerAPI validates acquisition and returns a short-lived read-only SAS URL or streams content when stronger per-request control is required. Public DTOs never expose storage keys.

ShortLinks does not deliver media. No additional media subdomain is required.

## Alternatives

- Serve media from ShortLinks: rejected because it couples redirects to Blob credentials and media concerns.
- Make originals permanently public: rejected because acquisition cannot govern access.
- Stream every download through ServerAPI: retained as an option but not default due to App Service bandwidth.

## Consequences

Blob containers, metadata, managed identity/credentials, lifecycle, and health checks become explicit infrastructure. SAS URLs remain shareable until short expiry.

## Migration And Rollback

Copy and checksum originals into a private container, switch references, and retain old locations read-only during verification.

## Validation

Test private direct access denial, hidden keys, acquisition checks, content metadata, SAS scope/expiry, and missing Blob behavior.