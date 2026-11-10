# Accounts and channels

Consolidated 2026-11-09. These are **reported delivery records**, not fresh source
or deployed verification. See the [handoff map](../feature-handoffs/README.md#former-handoff-map),
[ADR-018](../architecture/adr/ADR-018-public-account-boundary.md),
[ADR-017](../architecture/adr/ADR-017-anonymous-web-push.md), and
[transport isolation](../architecture/transport-isolation.md).

## Public identity and blog interactions

Recorded implemented; production governance/browser/Mongo proof remains open:

- One global PublicAccount compatible with FreezerAnalysis, separate from staff.
  No channel-owned signup/account cloning, membership requirement, or cross-host
  cookie SSO. Active accounts are allowed unless restricted.
- Comment block prevents posting only, not approved comment reads or reactions.
  Portal revoke prevents authenticated comments/reactions, not public reading,
  global login/profile, or Freezer sessions. Flags are independent; restoring
  portal access does not clear a comment block.
- Account routes, canonical sharing, reactions with clearing, anonymous approved
  comments, safe global directory including zero-activity accounts, and selected-
  channel detail/access/moderation/article summaries were recorded delivered.
  DTOs omit email/credentials; additive stores are blogComments, blogReactions,
  and blogPortalAccess.
- Feature-scoped create/verify at ServerAPI/BackOffice startup, mock skip and
  fail-closed behavior supersede this feature's original manual-only index plan;
  see [ADR-022](../architecture/adr/ADR-022-startup-blog-index-bootstrap.md).

Owners: Models/Domain blog-interaction boundaries, shared contracts/services,
both API interaction controllers, BackOffice PublicAccountsController, public
account/provider/Interactions, and admin
[publicAccounts](../../frontend/back-office-spa/src/routes/publicAccounts/) and
[blogInteractions](../../frontend/back-office-spa/src/routes/blogInteractions/).

Exploration values **not established as delivered guarantees**: comments 1-2000
characters; 5/account/minute and 20/IP/minute; cursor default 20/max 50, batch 50;
tombstone retention/pagination. Older text hiding approved comments for blocked
authors is superseded by the confirmed readable behavior above.
Author-withdrawal API-versus-completion wording remains unresolved; public UI
withdrawal is not claimed implemented.

Evidence: backend BlogInteraction **5 passed**, models/services builds,
shared services **50 passed**, public article/interactions **5 passed**, public
and admin builds passed. ImageSharp advisories remained.

Gates: real Mongo/concurrent/startup-provider compatibility, shared deployed
account store across three hosts, browser/crawler/cookies, directory permission
assignment, named moderation/retention/privacy owner. No activation flag was
added. Replies, notifications, recovery/verification/deletion/global admin and
share analytics remain deferred.
Rollback disables API capability and UI together, retains restrictions/data,
and must never restore endpoints ignoring retained restrictions.

## Registration, legal evidence, session, and return URLs

- Terms acceptance and Privacy acknowledgement cross form/contract/controller/
  Domain; versions and UTC times are server-owned. Account/session IDs are
  ObjectId-compatible. Preserve `/privacy`, `/terms`, `/cookie-policy` redirect,
  display-name fallback/truncation, and profile session refresh.
- Despite its old filename, the public-accounts/anonymous-push handoff supplied
  no unique push implementation details. Its build/backend test invocations had
  no output/counts and do not establish passing evidence.
- November 7 delivery with November 8 follow-up: successful registration replaces
  history to `/account/login` with one-shot Italian status. Retain the API-issued
  registration session; no artificial logout or password in URL/state.
  Distinguish account creation from failed session refresh; never auto-repeat
  registration.
- Validate anonymous/authenticated session envelopes; refresh returns explicit
  success/failure and clears failed session. Login navigates only after an
  authoritative authenticated session.
- Account-only transport preserves status/field/JSON/empty-body errors for
  401/403/409/429/5xx, without staff recovery or header leakage.
  Normalize slashless paths so exact login/register do not acquire a second
  cached CSRF token overwriting the fresh header. Logout stays CSRF-protected.
  CSRF acquisition and session restore are no-store, including client cache bypass.
- Return URLs are safe local paths, rejecting external/protocol-relative/auth
  loops. No wildcard credentialed CORS, JavaScript-readable session cookie,
  CSRF weakening, or bearer localStorage workaround.

Owners: [publicAccountService.ts](../../frontend/fe-packages/services/src/publicAccountService.ts),
[apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts),
public account routes/provider/header, ServerAPI PublicAccountController,
MVC PublicAccountAuthentication, and
[PublicAccountEndpointTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs).

Initial evidence: 3 service/1 static UI test, services build; client typecheck was
blocked by stale PublicChannelSocialLink export. Final follow-up: **6 service,
8 account/return/header, 4 endpoint tests**, services/client builds and Release
backend test build passed. Counts are separate selections, not full-suite proof.
No live credentials used. Anonymous CSRF 200 and Secure/SameSite=None observation
supersede an old 401, not credentialed login proof.

Still required: exact-origin synthetic browser preflight/cookie/GET-session,
legal-owner review, deployed artifacts, persistent Data Protection keys, approved
index/duplicate audits, telemetry. Bundle rollback cannot undo accounts;
session-less registration needs separate shared-protocol approval.

## Public channel social links

Recorded worktree implementation 2026-11-07:

- Channel.Socials is the sole authority, distinct from sharing/publishing secrets.
  Providers in deterministic order:
  telegram, discord, instagram, youtube, facebook, x, reddit, patreon.
  Do not infer profiles from menus, ChannelId, static fallbacks, or credentials.
- Normalize provider lowercase; duplicate last wins; blank removes row.
  Accept handles or approved provider-host HTTPS URLs; reject userinfo, control
  characters, lookalikes, and other schemes. Preserve valid paths/query/fragment,
  including Facebook profile.php. Omit an unsafe legacy entry individually with
  safe warning, not a fabricated profile or blanket empty success.
- Anonymous `GET /api/v1/channel/socials` resolves configured YouTubeChannelId,
  returns minimal provider/url DTO. Valid empty gives 200 empty; missing config/
  channel is explicit error. Cache is 300 seconds with channels invalidation.
  One credentialless public provider serves header/footer with distinct
  loading/error/retry/empty states.

Owners: Domain ChannelSocialLinks, shared public-social contract/types/endpoints,
ServerAPI ChannelSocialsController, BackOffice ChannelsController, public
social provider/SocialLinks/header/footer, and admin channel form.

Evidence: original terminology 2/2 compiled the affected .NET graph; full solution
interrupted by Windows/missing assets. Later SocialLinks **1/1**, header **3/3**,
Font Awesome 6.7.2 X-icon CDN presence and public build passed.
Integrated Release build/terminology+boundary **4/4** is stronger relevant proof.
Real profiles/cache round trip/deployment remain absent. Proposed admin legacy
warning/detail work was not evidenced in delivered file lists.
Deploy backend first; old editors cannot safely edit new providers. Retain socials
on rollback.

## Terminology null compatibility

Recorded backend/Models implementation 2026-11-07:

- Missing/null config/list is valid empty two-array response; null target may be
  compatible empty. Null mappings, missing/empty source, duplicate/corrupt
  structures are explicit errors, never filtered/defaulted away.
  Shared pure Models normalizer serves API plus new and legacy AI paths.
- GET corruption: **409 ProblemDetails**, code `channel_terminology_invalid`,
  no write. Authorized PUT/DELETE repairs; invalid request mappings/source gives
  400. Preserve 100 mappings, 80-character terms, case-insensitive duplicates.
  Empty AI config uses existing defaults; corruption fails before provider call.

Owners: [YTChannel.cs](../../MorWalPizVideo.Models/Models/YTChannel.cs),
Models normalizer, ChannelsController, BackOffice terminology and AI services.
Own 2/2 tests and later integrated **4/4** terminology/boundary + Release build
passed. The actual failed BSON/version remains unknown; malformed BSON may fail
before normalization. Proposed SPA reset/retry/mismatch/stale-response/Save gates
are **unconfirmed**, not delivered claims.
No backfill/index change; repair only through approved PUT/DELETE. Never collect
whole channel documents containing ciphertext; rollback can reopen null failures.

## Administrative channel transport

Recorded delivered 2026-11-09:

- Shared classifier scopes Partners, Campaigns, and Script Studio, including the
  global prompt because its controller requires a channel.
  Exact `/api/script-studio/quota` and descendants stay unscoped; segment-safe
  `quota-other` remains scoped.
- Wait for channel bootstrap before child loaders fetch; isolate channel
  generations. Missing/failed discovery is a local error with zero fetch.
  Preserve cookie/CSRF/server membership; public/account clients strip admin
  channel/bearer. Global directory/discovery stay unscoped; no per-loader hacks.

Owners: [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts),
readiness tests, admin authLoader and Script Studio callers/controllers.
**20 readiness tests** and shared build passed; no deployed requests inspected.
Manual prefix drift remains debt. Bundle rollback restores the original failure.

## Public account directory sort

Recorded delivered/focused-tested 2026-11-09:

- Mongo and mock use ascending username only, removing secondary ID sort.
  Preserve database-side escaped search/count/skip-limit/cancellation, global
  permission without channel, response shape, page 1-10000 and size 1-50.
- Equal usernames have unspecified order; mock case-insensitivity versus Mongo
  collation remains deferred. No in-memory sorting, backfill, retry fallback,
  composite index, or startup-index workaround.

Owners: Domain repository implementations and
PublicAccountRepositoryTests in the backend test project.
**2 passed/0 failed** with existing ImageSharp advisories.
Provider single-field index support, initial/search/second-page requests remain
unverified. Query rollback restores the original failing compound query; no
data/index deletion is needed.
