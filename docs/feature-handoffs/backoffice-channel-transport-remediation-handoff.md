# BackOffice partners, campaigns and Script Studio: channel transport

Status: implemented, 2026-11-09. Shared transport classification and focused
regressions are complete. No deployed request inspection was performed.

## Outcome

Fix missing `X-Channel-Id` on Partners, Campaigns and channel-owned Script Studio
requests at the existing shared transport boundary. Preserve bootstrap readiness,
channel authorization, public/account isolation, and global quota administration.

## Current Evidence

- [PartnersController.cs](../../MorWalPizVideo.BackOffice/Controllers/PartnersController.cs)
  and [CampaignsController.cs](../../MorWalPizVideo.BackOffice/Controllers/CampaignsController.cs)
  use `/api/[controller]`, `RequireChannelScope`, and channel-owned CRUD.
- [ScriptStudioController.cs](../../MorWalPizVideo.BackOffice/Controllers/ScriptStudioController.cs)
  puts `RequireChannelScope` on `/api/script-studio`, including generation and
  global-prompt actions. The separate `ScriptStudioQuotaController` at
  `/api/script-studio/quota/{userId}` has permission checks but no channel-scope
  attribute. Its administration must not accidentally acquire a channel prerequisite.
- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts),
  `scopedBackOfficePrefixes` and `isScopedBackOfficeRequest`, omit `/api/partners`,
  `/api/campaigns` and `/api/script-studio`. Paths are lowercased before matching.
  Merely changing endpoint capitalization cannot fix this.
- In the same file, bootstrap-aware admin requests wait for channel readiness and
  inject `X-Channel-Id` only when this classifier returns true. Public and account
  modes strip that header.
- [router.ts](../../frontend/back-office-spa/src/router.ts), `authLoader`, starts
  channel bootstrap before discovery and resolves it after accessible-channel
  selection. Route loaders can execute concurrently with the parent.
- [apiTransport.readiness.test.ts](../../frontend/fe-packages/services/src/apiTransport.readiness.test.ts)
  already proves the same fix for galleries and interactions and deliberately
  leaves the public account directory unscoped.
- [scriptStudioService.ts](../../frontend/fe-packages/services/src/scriptStudioService.ts)
  uses the shared transport for document, generation and global prompt operations.
  Blog Prettify also calls generation, so this bug is not limited to one screen.

Root cause is source-proven: these endpoints do not enter the channel-header and
bootstrap-readiness path. The reported deployed 400 responses are consistent with
that omission, but no deployed request was independently captured.

## Delivered Implementation

Extended the existing centralized classification for Partners, Campaigns and
Script Studio. The exact `/api/script-studio/quota` path and descendants are
explicitly unscoped before the general Script Studio prefix match; similarly
named paths such as `quota-other` remain scoped through segment-safe prefix
matching.

Preserve the current server contract: global-prompt requests remain channel-scoped
because their owning controller requires it, despite their global data meaning.
Quota administration remains unscoped. Do not change server scope attributes or
broaden browser credentials to compensate for the frontend omission.

Do not add headers in individual loaders, defer channel discovery until layout
mount, or duplicate the shared API client. All CRUD, nested timeline, generation
and prompt calls must use the same classification and readiness behavior.

## Impacted Projects

- `@morwalpizvideo/services`: classification and transport tests.
- `frontend/back-office-spa`: runtime consumer; route smoke verification.
- `MorWalPizVideo.BackOffice` and `MorWalPizVideo.BackOffice.Tests`: verify-only
  channel/auth boundary and endpoint coverage.
- Shared models/layout: no required source changes.
- Public client, Ask, Shooting ITA, FreezerAnalysis SPA: verify-only public/account
  isolation because they consume shared services.
- Shop: frozen, verify transport compatibility only; no feature work.
- ServerAPI, Windows tools, ShortLinks and AppHost: no required changes.

## Impacted Files

### Modify
- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts):
  classifier prefixes and quota exception.
- [apiTransport.readiness.test.ts](../../frontend/fe-packages/services/src/apiTransport.readiness.test.ts):
  table-driven readiness, headers and scope exceptions.

### Add
No production file needed. Add service-specific transport tests beside existing
service tests only if the classifier/readiness suite cannot exercise the real calls.

### Generated/Migration
None. Shared package output is built by existing tooling, not edited manually.

### Documentation
- This handoff after implementation.
- [frontend.md](../architecture/frontend.md): record the Script Studio quota
  exception if it is not already described by the owning transport documentation.

### Verify Only
- [router.ts](../../frontend/back-office-spa/src/router.ts).
- [apiService.test.ts](../../frontend/fe-packages/services/src/apiService.test.ts).
- [scriptStudioService.ts](../../frontend/fe-packages/services/src/scriptStudioService.ts).
- [CreatorPartnershipEndpointTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/CreatorPartnershipEndpointTests.cs).
- [ChannelTenancyAuthorizationTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs).
- [creator-partnership-campaign-workspace-handoff.md](creator-partnership-campaign-workspace-handoff.md).

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Preserve existing required channel context and status codes. |
| Frontend/UI | Existing pages and Blog Prettify obtain correct channel context; no visual changes. |
| Database/Data | No schema, query or write semantics change. |
| Contracts/Models | Unchanged. |
| Authentication/Authorization | Preserve cookies/CSRF, resource permissions, quota exception and server membership checks. |
| Validation | Missing channel fails locally through existing readiness errors; no request is sent. |
| Cache | Preserve bootstrap request coalescing and generation isolation. |
| Configuration/Secrets | No new settings or credentials. |
| Logging/Telemetry | Preserve explicit context errors and visible route errors. |
| Background Jobs | No impact on Script Studio retention jobs. |
| Deployment/Operations | Rebuild shared services and deploy consuming admin bundle. |
| Backward Compatibility | Existing scoped and deliberately unscoped routes retain behavior. |

## Reuse Opportunities

Reuse `isScopedBackOfficeRequest`, channel-bootstrap generation handles, existing
selected-channel storage/providers, `ChannelContextError`, and the gallery readiness
regressions. Keep authorization server-owned; the header is context, not authority.

## Risks

| Rank | Cause/surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Overbroad scoping can leak admin context to another transport. | Test public/account modes still omit admin headers and preserve their credentials rules. |
| Medium | Script Studio quota shares a URL prefix but is global. | Explicit segment-safe exception; quota works without channel discovery while channel operations wait. |
| Medium | Parent/child loader race sends a request before discovery. | Hold bootstrap unresolved in tests; fetch remains uncalled until resolution. |
| Medium | Channel switch uses stale readiness generation. | Retain generation-isolation tests and verify next request uses the new selected channel. |
| Low | Shared-bundle distribution serves old classification. | Confirm deployed admin artifact and successful request headers, not only local unit tests. |

## Technical Debt

The manual prefix list can drift from server attributes. Isolate that debt with
focused contract tests here; generating a global endpoint registry or auditing
every API family is outside scope. Script Studio global-prompt channel scoping is
an existing contract, not something to redesign in this fix.

## Implementation And Validation

1. Updated `frontend/fe-packages/services/src/apiTransport.ts` only at the
   shared classifier boundary.
2. Added table-driven readiness/header coverage for Partners, Campaigns and
   channel-owned Script Studio, quota descendants and boundaries, plus public
   and account transport isolation.
3. Preserved the existing bootstrap wait, missing-channel failure, credentials,
   CSRF, and selected-channel injection behavior.
4. Parent validation passed the focused `apiTransport.readiness.test.ts` run
   (20 tests) and the shared frontend package build. No deployed request or
   browser inspection was performed.

## Final Scope

Work stops at Partners, Campaigns, channel-owned Script Studio, and the exact
quota exclusion. Blog implementation, account sorting, routing files, backend
scope attributes, and other handoffs were not changed.

## Testing Strategy

- Classifier: collection/detail/nested timeline routes; Script Studio generation
  and global prompt; case-insensitive matching and query strings.
- Negative matching: similarly prefixed names must not match; quota path and quota
  descendants stay unscoped.
- Readiness: both generic/admin service entry points; no fetch before discovery;
  null/failed discovery yields an explicit error and zero fetches.
- Header checks: selected channel on GET and unsafe methods; CSRF/cookie behavior
  unchanged; channel changes use current selection.
- Isolation: public/account clients never emit channel or BackOffice bearer
  context; public-account directory and channel discovery remain unscoped.
- API/security: existing missing-channel, foreign-channel, permission and CSRF
  outcomes remain unchanged; adding a browser header does not bypass membership.
- End-to-end: actual affected pages load with the selected channel and show explicit
  failure if no channel is available. No new UI or migration tests are needed.

## Migration And Rollback

No database, DTO, auth, cache or configuration migration. Backend remains compatible
with old/new clients; the old client remains broken for these scoped calls.
Deploy rebuilt shared services inside the admin SPA. Rollback restores the old
bundle and the original failures, without data restoration. No new feature flag
or automatic index/data operation is justified.

## Open Questions

None blocking. Whether global-prompt operations should eventually become
channel-independent is separate architecture work and not authorized by this bug.
