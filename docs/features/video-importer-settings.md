# VideoImporter channel settings

Consolidated 2026-11-09. The former handoff was proposal-only, but the
later [ADR-021](../architecture/adr/ADR-021-video-importer-channel-settings.md),
[TD-035](../architecture/technical-debt.md), and
[rollout runbook](../architecture/operations/video-importer-channel-settings-rollout.md)
record source delivery. **This consolidation did not rerun or source-verify
that implementation.** Do not carry the old "not implemented" label forward as
an operational conclusion.

## Accepted boundary

- BackOffice owns one additive Mongo videoImporterChannelSettings document per
  selected channel. Versioned envelope, typed importer-owned schema 1; complete
  atomic idempotent replacement, approved single active writer. No ETag/merge UI.
- API-key-only `GET/PUT /api/v1/video-importer-settings`, separate
  videoimporter.settings.read/write scopes, server-resolved channel.
  GET 200 = existing, 204 = genuinely absent only. Bounded JSON object/version/
  size validation; no global configuration or admin SPA surface.
- Include ApplicationName/DefaultHashtags, languages/default-selected flags/
  disclaimers keyed by normalized language code, and value-based schedules.
  Exactly one default language; unique schedule names/value fields.
- Exclude endpoint/API key/channel/tenant/local IDs, OAuth/provider/social
  tokens, secret references, machine paths/fake flags, arbitrary files/prompts,
  dialog/hashtag history and image-generation config. Preserve local bootstrap
  values and all other tenants/tables.
- Resolve tenant/access first and serialize pull/switch. Remote 200 wins.
  Seed validated existing SQLite only on successful scoped 204, **never**
  404/auth/429/5xx/network/cancel/malformed. UI candidate -> acknowledged remote
  PUT -> one transactional SQLite projection. Remote failure leaves cache
  unchanged; remote success/local failure is visible degraded cache with
  pull recovery.
- All affected settings/language/default/disclaimer/schedule writers must use
  the sync boundary, not independent database/tenant construction.

Owners: [VideoImporter](../../MorWalPiz.VideoImporter/), App/MainWindow,
snapshot/sync/API/fake services and settings/language/disclaimer/schedule screens;
Models/Domain/contracts/settings endpoint, backend tests, and Windows importer
tests. The ADR/runbook are the durable decision and operational procedure;
do not duplicate their index apply steps.

## Evidence and remaining gates

The old proposal ran no validation; conflicting earlier 9/9/migration notes were
not proof. Later architecture records **19/19 Windows importer tests** and
**17/17 focused BackOffice endpoint/index tests**. These were not rerun here.

Still required: live duplicate audit/manual unique channel index verification,
least-privilege key scopes, existing/missing/error endpoint smoke, clean second-
workstation restore, restart precedence, offline visible degradation, full
desktop startup and interactive UI/accessibility.
Fake/temp SQLite tests must not contact BackOffice/Key Vault/Mongo.
Retain secret-field exclusion and first-use/cancel/transaction/failure tests.

Scope exclusions remain binding. Social-token migration is separate; do not
silently rename the importer user-secret identifier because it resembles
InsightScanner. Local endpoint/key UI remains local compatibility state.

## Migration and rollback

Backend -> duplicate audit/manual governed unique index -> channel-key scopes
-> endpoint verification -> importer. No SQLite migration/backfill is required;
first successful scoped 204 seeds lazily. Startup does not create this index.
The executable Mongo operation allowlist takes precedence over its JSON snapshot.

Old importer remains local-only. Backend rollback leaves new clients visibly
degraded using cache, never a fake 204 compatibility shim. Retain collection and
index. Incorrect acknowledged full replacement requires approved backup/
point-in-time recovery, not an automatic overwrite from stale SQLite.
No credentials or payload content in logs. Single-writer assumption remains a
limit; concurrency/merge is separate work.
