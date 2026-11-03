# ADR-021: BackOffice-Authoritative VideoImporter Channel Settings

- **Status:** Accepted
- **Date:** 2026-11-02

## Context

VideoImporter stores configuration in tenant-filtered SQLite while its API key
and BackOffice workflows already operate on a selected channel. Settings edited
on one workstation therefore do not follow the channel to another workstation,
and several WPF screens commit configuration directly to SQLite. The same
SQLite `Settings` row also contains local bootstrap values and secrets that
must not be copied into shared storage.

## Decision

BackOffice MongoDB owns a dedicated `videoImporterChannelSettings` document per
channel. The `GET`/`PUT /api/v1/video-importer-settings` endpoints use the
existing API-key authentication and channel-scope middleware, plus dedicated
`videoimporter.settings.read` and `videoimporter.settings.write` scopes. The
channel ID comes only from resolved request context. `GET` returns `200` for an
existing snapshot and `204` only when the scoped channel has no document.
`PUT` replaces the complete client-owned snapshot and is idempotently upserted
by channel.

The importer owns schema version 1 and serializes only an allowlisted snapshot:
application name, default hashtags, languages and optional disclaimers keyed
by normalized language code, and value-based publish schedules. The snapshot
excludes all SQLite IDs, tenant/channel selection, endpoint/key, YouTube and
social credentials, Key Vault references, machine paths, history, and
arbitrary files.

Remote settings are authoritative after first use. The importer may seed from
SQLite only after a successful scoped `204`, and commits UI mutations locally
only after remote `PUT` acknowledgement. Applying an acknowledged snapshot
updates only the selected tenant's in-scope rows in one SQLite transaction and
preserves local bootstrap values and excluded tables. If the remote write
succeeds but local projection fails, the importer reports the degraded cache
and attempts to restore from BackOffice.

The unique channel index is source-managed and manually applied under
[ADR-008](ADR-008-mongo-index-migrations.md); application startup does not
create it. Deployment is backend/index/API-key scopes first, importer last.

## Alternatives

- Reuse global `MorWalPizConfiguration`: rejected because it is not channel
  scoped and does not provide the required least-privilege machine boundary.
- Add importer fields to `YTChannel`: rejected because channel administration
  and importer-owned schema would become coupled.
- Synchronize all SQLite state: rejected because secrets, machine state, and
  operational history do not belong in channel settings.
- Use SQLite as the authority and push asynchronously: rejected because a
  successful save could be lost across machines or during an outage.

## Consequences

- BackOffice owns the durable value; desktop local values are a recoverable
  projection/cache.
- An older BackOffice without the endpoint is a failure, never a first-use
  signal. New importers continue to display local cached data and show a
  degraded status.
- The complete-snapshot replacement relies on the approved single-writer
  assumption; concurrency tokens and merge UI are intentionally deferred.
- Existing local endpoint/key fields remain local for compatibility and are
  not part of schema version 1.
- Social access-token storage, image-generation settings, prompt templates,
  and other local state remain outside this decision.

## Migration And Rollback

Deploy the additive BackOffice endpoint and aggregate, audit and manually
apply the unique channel index, grant only the read/write scopes to the
channel-bound importer key, verify the endpoint, then distribute the importer.
The first successful scoped `204` lazily seeds a channel from existing local
values. Old importers remain local-only.

Rolling back the importer leaves SQLite intact and ignores the additive
collection. Rolling back the route leaves new clients operating from their
local cache in visible degraded mode; do not make a missing endpoint return
`204`. Retain the Mongo collection and index on backend rollback. An incorrect
acknowledged replacement is recovered through the normal Mongo backup/
point-in-time recovery process.

## Validation

Focused BackOffice endpoint tests cover scoped access, channel isolation,
read/write scopes, explicit absence, idempotent replacement, and invalid
payloads. Windows importer tests cover first-use, remote precedence, bootstrap
preservation, cache projection, failure/no-seed behavior, remote-first saves,
and secret/ID exclusion. Full desktop startup and interactive UI/accessibility
smoke checks remain additional release evidence; source tests do not prove
deployed endpoint, Mongo index, API-key scope, or visual runtime state.
