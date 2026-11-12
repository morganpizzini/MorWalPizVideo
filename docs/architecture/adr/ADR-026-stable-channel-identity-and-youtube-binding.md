# ADR-026: Stable channel identity and editable YouTube binding

## Status

Accepted and implemented in the worktree. Production rollout remains gated on
compatible channel writers and deployed-client verification.

## Context

`YTChannel.ChannelId` is used as the tenant key across ownership, authorization,
content, API-key binding, settings, and route identity. It cannot safely double
as an editable external YouTube identifier. Replacing it would require a
cross-system tenant migration. The Mongo document `Id` is a distinct persistence
identifier and does not replace the stable tenant key in routes.

## Decision

- Preserve document `Id` and tenant `ChannelId`; use document `Id` only for
  display/navigation metadata where requested.
- Store the external YouTube binding as optional `YTChannelId`. For legacy
  documents without a binding, derive the effective binding from `ChannelId`.
- Permit authorized editors to update the external binding without changing
  ownership, API-key scope, content references, scan history, or OAuth identity.
- Use the effective binding for YouTube scans and derive Studio URLs in shared
  channel projections. Keep scan attribution and tenant ownership on
  `ChannelId`.
- Keep old persisted Studio URLs non-authoritative. Preserve Analytics when
  update input is omitted/null and let blank input explicitly clear it.
- Return field-keyed Validation Problem Details for invalid channel inputs.
  Return 204 for normal bodyless channel mutations; retain 200 warning payloads
  when a committed mutation's cache invalidation fails and retain upload data
  needed by the editor.

## Consequences

The persistence transition is additive and tolerant; it requires no backfill or
index. Every channel writer must be compatible before binding edits are exposed,
because an older replacement writer could erase the optional field. Older
clients may assume 200 or a `success` property, so deployment order and external
client inventory must be verified before production rollout.

## Alternatives rejected

- Replacing `ChannelId` would require coordinated migration of every dependent
  tenant reference and is outside this editor change.
- Treating Mongo `Id` as the new route or authorization key would break current
  ownership and API-key contracts.
- Adding a new identity service, uniqueness constraint, or forced YouTube
  ownership verification is not required for this bounded change.
