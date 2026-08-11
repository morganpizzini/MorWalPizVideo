# ADR-004: Canonical Short Links

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Short links use two ownership paths during the current migration. YouTube video links remain embedded on the owning match so they can be resolved from the match's video references, while channel and generic links use the standalone collection. BackOffice management is channel-scoped. Public resolution is global for standalone links, while embedded YouTube video resolution is restricted to the configured public YouTube channel.

## Decision

The standalone record is canonical for channel and generic links, with a normalized globally unique code, validated absolute HTTP/HTTPS destination, optional channel reference, structured query data, status, audit metadata, and atomic total count. YouTube video links are canonical on the owning match for now; their target must match a referenced video. The standalone collection remains indexed and authoritative for its link types, while embedded video reads remain an intentional compatibility and ownership path.

ShortLinks owns anonymous resolution/tracking only. BackOffice owns management. Detailed visits use a separate collection with an approved retention policy.

## Alternatives

- Keep embedded links: rejected for new writes, but retained temporarily as a compatibility read fallback because legacy documents still exist.
- Put management in ShortLinks: rejected because it expands a public edge service into an admin API.
- Redirect arbitrary strings: rejected because unsafe schemes/hosts would undermine branded trust.

## Consequences

Channel and generic writes use the standalone collection and enforce safe absolute HTTP/HTTPS destinations for generic short links. Video writes update the owning match and invalidate match and shortlink caches. Public resolution uses an indexed lookup for standalone records; embedded video resolution scans only content associated with the configured public channel. BackOffice listing and mutation remain channel-scoped.

## Migration And Rollback

Create and maintain the global unique normalized-code index for standalone records, validate channel-scoped management authorization, and preserve the embedded video path until a reviewed migration moves video ownership without breaking public URLs or click counts. Do not claim standalone-only resolution until that migration and its backfill are complete.

## Validation

The current behavior is validated for standalone and embedded mock-scenario resolution, click counting, channel scoping, canonical writes, and scoped management. Remaining work is operational: duplicate audit, index application, video-link migration/backfill, and evidence that the embedded path can be removed.