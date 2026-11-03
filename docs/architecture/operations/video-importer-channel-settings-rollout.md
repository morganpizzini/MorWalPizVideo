# VideoImporter Channel Settings Rollout

This runbook covers the additive BackOffice resource and the VideoImporter
channel-settings snapshot. It contains no credentials and does not authorize
production changes by itself.

## Preconditions

- Review [ADR-021](../adr/ADR-021-video-importer-channel-settings.md).
- Verify the target BackOffice build exposes `GET` and `PUT
  /api/v1/video-importer-settings`.
- Identify the channel-bound importer API key through the approved secret
  management procedure. Do not copy its value into tickets, logs, or this file.
- Grant only `videoimporter.settings.read` and
  `videoimporter.settings.write` to that key. The endpoint does not use the
  global configuration surface.
- Confirm backup/restore coverage for the BackOffice Mongo database.

## Mongo Index

The source-owned unique index key is
`videoimportersettings.channelid.unique`, defined in
`MongoIndexOperationsService` and recorded in
`mongo-index-manifest.phase4.json`. The executable service allowlist is the
runtime authority; the JSON file is a review snapshot and must be reconciled
before an apply operation.

1. Audit `videoImporterChannelSettings` for existing documents and duplicate
   `channelId` values.
2. Resolve duplicates through a reviewed recovery decision; do not delete or
   overwrite documents automatically.
3. Use the approved manual Mongo index audit/apply operation with the exact
   current allowlist key. Startup does not create this unique index.
4. Audit again and confirm `ux_videoimportersettings_channelid` is unique on
   `{ "channelId": 1 }`.
5. Record sanitized request/result evidence, environment, date, and operator
   approval in the appropriate environment evidence bundle. Source artifacts
   are not proof that a live index exists.

## Deployment Order

1. Deploy BackOffice Models, repository, service, and endpoint changes.
2. Complete duplicate audit and apply/verify the unique channel index.
3. Update the intended channel-bound importer key with the dedicated read/write
   scopes using protected administration.
4. Verify scoped GET: existing document is `200`; absent channel document is
   `204`; missing/mismatched channel and missing scope are not `204`.
5. Verify PUT replaces only the selected channel document and repeated PUT is
   idempotent.
6. Release VideoImporter. On explicit `204`, it validates and seeds the local
   snapshot. A route, authentication, authorization, network, malformed, or
   server failure must never trigger a seed.
7. Verify an edit from one workstation is restored by a clean second
   workstation, then restart the first workstation and verify remote values
   win. Verify offline operation retains its last local cache and reports
   degraded synchronization.

## Monitoring And Rollback

Monitor read outcome, seed/save failures, schema version, payload size,
duration, authentication/scope failures, duplicate-key errors, and local cache
projection failures. Payload content and credentials must never be logged.

If importer rollout is reversed, the previous client continues to use SQLite.
If the endpoint is rolled back, new clients must stay in visible degraded
local-cache mode; never return `204` as an endpoint compatibility fallback.
Keep the collection and index on backend rollback. Recover an incorrect
acknowledged snapshot from the approved Mongo backup/point-in-time recovery
process before restarting clients.

Live API, index, key-scope, and cross-workstation evidence is environment
specific and must be recorded separately from this runbook.
