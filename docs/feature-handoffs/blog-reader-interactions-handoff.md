# Blog Account Interactions Handoff

Status: implemented; deployment verification remains.
Initial evidence was collected on 2026-11-05. Implementation and validation details
are recorded below.

This revision supersedes the earlier channel-owned registration proposal. The user
now wants one shared public identity, compatible with FreezerAnalysis, a BackOffice
directory of all public accounts, and channel-specific portal restrictions.
There is no separate "reader account" type: reader describes someone viewing an
article, anonymously or using their existing public account.

## Outcome

Enable sharing, comments and like/dislike interactions on published articles.
Anonymous visitors can read approved comments; logged-in public accounts can
interact unless restricted for that portal's channel. BackOffice shows all public
accounts, including accounts with zero article activity, and their interaction
details filtered to the selected channel.

Confirmed decisions:

- Public identity/registration is shared, not channel-owned. The same account may
  use FreezerAnalysis and public portals. Staff BackOffice identity remains separate.
- Articles, comments, reactions and portal restrictions are channel-owned.
- Anonymous approved-comment viewing is allowed. Comment-blocked accounts can still
  read approved comments and react; blocking only disables comment submission.
- BackOffice provides an all-public-account directory and account detail, not a
  directory inferred from comments or a channel registration list.

Confirmed behavior:

- Every active public account can interact with a portal by default. No per-channel
  signup, membership record or onboarding step is required.
- Distinguish comment-only blocking from revocation of authenticated portal access.
  Comment blocking prevents comment submission but leaves approved comments visible
  and reactions available. Portal revocation prevents authenticated comments and
  reactions; public articles and approved comments remain readable.
- Neither action disables the shared account, logs it out of FreezerAnalysis, nor
  changes its global password/profile. Anonymous articles/comments remain public.
- Comments start pending; moderators approve, reject or hide them.

Completion boundary: existing account registration/login works in the public app;
an account can interact with an article; an authorized administrator finds that
account before or after any interaction, inspects selected-channel activity,
moderates comments, and blocks/revokes/restores that channel's access.

Non-goals: separate reader identities, channel account cloning or migration,
cross-host cookie SSO, global account suspension/password administration,
staff/public identity merging, social auto-publishing, shop work, replies,
notifications, and a general-purpose activity platform.

## Delivered Implementation

The public article page now has canonical social-share controls, authenticated
like/dislike reactions, and approved comments with a sign-in prompt for anonymous
visitors. Existing shared-account registration/login/profile routes are wired into
the public client. ServerAPI resolves the configured public channel; accounts remain
global and unchanged.

BackOffice has a permission-gated global public-account directory, interaction and
portal-access detail for the selected channel, article interaction summaries, and
comment moderation. Directory/detail DTOs omit email and credentials. Comment blocks
only prevent posting. Portal revocation prevents authenticated reactions and
comments. Both preserve public article and approved-comment reads.

Implementation owners include:
- Models, contracts, Domain repositories/services and authorization:
  [BlogInteractions.cs](../../MorWalPizVideo.Models/Models/BlogInteractions.cs),
  [BlogInteractionContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogInteractionContracts.cs),
  [BlogInteractionService.cs](../../MorWalPizVideo.Domain/BlogInteractions/BlogInteractionService.cs).
- Public API and BackOffice API:
  [ServerAPI BlogInteractionsController](../../MorWalPizVideo.ServerAPI/Controllers/BlogInteractionsController.cs),
  [BackOffice BlogInteractionsController](../../MorWalPizVideo.BackOffice/Controllers/BlogInteractionsController.cs),
  [PublicAccountsController](../../MorWalPizVideo.BackOffice/Controllers/PublicAccountsController.cs).
- Public client and shared contracts/services:
  [Interactions.tsx](../../frontend/morwalpizvideo.client/src/routes/blog/Interactions.tsx),
  [blogInteractionService.ts](../../frontend/fe-packages/services/src/blogInteractionService.ts),
  [blogInteractions.ts](../../frontend/fe-packages/models/src/blogInteractions.ts).
- BackOffice UI:
  [public accounts](../../frontend/back-office-spa/src/routes/publicAccounts/index.tsx),
  [account detail](../../frontend/back-office-spa/src/routes/publicAccounts/detail.tsx),
  [article interactions](../../frontend/back-office-spa/src/routes/blogInteractions/index.tsx).

Mongo collections are additive: `blogComments`, `blogReactions`, and
`blogPortalAccess`. ServerAPI and BackOffice create/verify their declared indexes
before accepting traffic in Mongo mode; mock mode skips the initializer. This
feature-scoped startup behavior is defined in
[ADR-022](../architecture/adr/ADR-022-startup-blog-index-bootstrap.md). Also verify
the deployed public account stores are shared as intended; identity compatibility
does not imply shared sessions.

Focused tests cover Domain behavior, API authorization/moderation/public reads,
frontend interaction behavior, and scoped versus unscoped transport. Final validation
for this delivery is listed in the completion record at the end of this document.
Browser visual capture and production account-store/index verification remain open.

## Initial Evidence (Before Implementation)

### Verified identity boundary

- [PublicAccount](../../MorWalPizVideo.Models/Models/PublicAccount.cs) has username,
  normalized username/email, display name, password hash, legal acknowledgements,
  activity and security version, but no channel ownership. Its sessions reference
  account ID with fixed expiry and revocation state.
- [PublicAccountService](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs)
  already owns registration, password verification, eight-hour sessions, account
  activity/security-version revalidation, logout and profile operations.
- [Repository interface](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs),
  [Mongo repository](../../MorWalPizVideo.Domain/Interfaces/Repository.cs) and
  [mock repository](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs) use
  global username/email/ID lookup. These match the revised shared identity model.
- [FreezerAnalysis Mongo registration](../../FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs)
  registers that same service/repository against its configured platform account
  database. Its application database is separate.
  [Freezer account controller](../../FreezerAnalysis.Api/Controllers/FreezerAccountController.cs)
  uses the shared service, but issues a distinct host-owned session cookie.
- [ServerAPI account controllers](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs)
  use the dedicated public account cookie/CSRF behavior in
  [PublicAccountAuthentication](../../MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs).
  Sharing identity does not currently share browser sessions across those hosts.
  Whether deployed hosts target the same account store requires operational proof.
- [Staff User](../../MorWalPizVideo.Models/Models/User.cs),
  [UserController](../../MorWalPizVideo.BackOffice/Controllers/UserController.cs)
  and [staff detail](../../frontend/back-office-spa/src/routes/rbac/UsersDetailPage.tsx)
  belong to staff RBAC. Their IDs/password controls are not public account management.

### Verified article and frontend flow

- Public flow is [blog route/loader](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx)
  -> [shared blog client](../../frontend/fe-packages/services/src/blogService.ts)
  -> [BlogController](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs)
  -> [BlogService/repositories](../../MorWalPizVideo.Domain/BlogService.cs)
  -> [BlogPost](../../MorWalPizVideo.Models/Models/BlogPost.cs).
  Public channel resolution is server-configured, not visitor-selected.
- Public blog reads expose only published content and use a 300-second output
  cache tagged `blog`. Drafts/publication revisions and media are managed by
  [BlogPostsController](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs)
  through channel scope and Pages permissions.
- [Blog contracts](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs) and
  [TS blog models](../../frontend/fe-packages/models/src/blog.ts) contain no comments,
  reactions or viewer-specific access. This gap was addressed by the implementation
  above using separate interaction contracts.
- [Account screens](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx)
  existed, but [active public routes](../../frontend/morwalpizvideo.client/src/routes/config.tsx)
  did not register login/register/profile URLs.
  [Root](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx) mounts the
  [account provider](../../frontend/morwalpizvideo.client/src/routes/layout/account.tsx),
  whose restoration catch treats every failure as anonymous.
- [Account service client](../../frontend/fe-packages/services/src/publicAccountService.ts)
  and [transport core](../../frontend/fe-packages/services/src/apiTransport.ts)
  already separate account credentials/CSRF from public credentialless calls and
  staff channel context.
- [Homepage ShareMenu](../../frontend/morwalpizvideo.client/src/routes/home/Component.tsx)
  uses existing `react-share` Facebook/WhatsApp controls.
  [SEO](../../frontend/morwalpizvideo.client/src/utils/seo.tsx) emits canonical/Open
  Graph metadata; article detail lacks share controls.
- BackOffice currently registers no public-account repository in its
  [composition root](../../MorWalPizVideo.BackOffice/Program.cs).
  [Admin blog index](../../frontend/back-office-spa/src/routes/blog/index.tsx)
  has editorial actions but no interaction summary or moderation.
- [Mongo index operations](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs)
  declare global account username/email uniqueness and session TTL.
  Keep these definitions: no per-channel identity index replacement is needed.
- Relevant .NET manifests target .NET 10. [Frontend manifest](../../frontend/package.json)
  defines Yarn Classic workspaces and ordered shared models/services/layout builds.
  No new project reference, package dependency or deployable is needed.

### Evidence limits

Account UI documentation conflicts with current router wiring; source wins.
[Frontend architecture](../architecture/frontend.md) records a deployed CSRF blocker;
the [ServerAPI deployment workflow](../../.github/workflows/deploy-serverapi.yml)
now has a bootstrap smoke gate, but no current deployed result was established.
The guide identifies Aruba hosting while the
[public deployment workflow](../../.github/workflows/deploy-morwalpizvideo-client.yml)
builds SSR and deploys an Azure container. Confirm the artifact serving the canonical
domain before claiming cookie or social-crawler readiness. No checks were run here.

## Design Notes And Deferred Recommendations

The following analysis preserves the original design exploration; it is not a
statement that every suggested refinement was implemented. Prefer the delivered
behavior above and the completion record below when describing current capability.
Author comment withdrawal exists in the public API, but the public UI does not expose
it. Reaction clearing is implemented. Specific limits, cursor strategies and retention
details in this section are recommendations, not validated production guarantees.

### Shared accounts, channel-owned capabilities

Keep `PublicAccount` and `PublicAccountSession` unchanged. Registration, passwords,
profile and authentication remain Domain-owned shared identity behavior. Do not add
ChannelId to accounts/sessions, clone identities, partition email uniqueness, or
create a Freezer-versus-portal legacy namespace.

Add a separate account/portal access aggregate keyed uniquely by channel/account.
It holds comment-blocked and portal-access-revoked states, revision, and latest
actor/time/internal reason. Keep the two states independently so restoring portal
access does not unintentionally clear a previous comment block.

No access record means allowed for an active authenticated account. A failed
repository query is an error, not "no record" or a permissive fallback.
BackOffice determines the channel through its authorization pipeline; ServerAPI
resolves the configured public channel. Requests cannot forge either ownership or
the account author ID.

Validate the shared account session first, then check portal access on every
authenticated portal capability. Keep this policy in focused Domain services,
not shared identity authentication: a channel ban must not invalidate Freezer sessions.
Global login/profile/logout stay available even if a portal is revoked.
Do not grant channel administrators global suspension, password change or identity
editing through the portal-access action.

Portal revocation here means authenticated capability denial, not hiding already
public articles. Denying a person anonymous access to public content is not achievable
with an account restriction; making content private would be a different feature.

### Registration and public UI

Wire the existing account screens under the public root, with discoverable
sign-in/register navigation and a validated relative return path to the article.
Reject external/protocol-relative return destinations. An existing Freezer account
should sign in with its existing credentials rather than register again, provided
both deployments use the same configured account store.

Distinguish restoration loading, actual anonymous state and restoration failure.
Do not silently expose the signed-in comments section as anonymous on a network
failure. Anonymous state shows approved comments and a posting sign-in invitation.
Allowed signed-in state adds form, own pending submissions and reactions.
Comment-blocked state hides the entire comments section; revoked state shows a
portal-unavailable notice and no authenticated interaction controls.

Refresh access on article entry, account changes, window focus and relevant errors.
Clear stale comments/controls after a forbidden response. Existing open tabs update
on next revalidation; do not promise immediate remote erasure or add WebSockets.

### Comments, reactions and persistence

Add focused Models aggregates and Domain Mongo/mock repositories:

| Aggregate | Responsibility |
| --- | --- |
| Blog comment | ID, channel/post/account IDs, plain-text body, state, creation/update UTC, revision, submission request ID and latest moderation metadata |
| Blog reaction | Channel/post/account IDs, one like/dislike value, update UTC and revision |
| Account portal access | Channel/account IDs, comment block, portal revocation, revision and latest administrative metadata |

Use ObjectId-compatible entity IDs and stable post ID after slug resolution.
Do not embed unbounded interactions in articles/accounts or increment an editorial
revision on every vote.

Comments contain trimmed plain text of 1-2000 characters; render escaped text and
display names. No HTML, uploads, link previews or arbitrary embeds. Pending is the
default. Public reads expose approved comments only; own-pending reads are private.
Moderators approve/reject pending comments, hide approved comments, and explicitly
reapprove hidden/rejected comments with revision checks. They do not rewrite text.
Author withdrawal is terminal, removes ordinary body exposure and preserves only
necessary tombstone/idempotency metadata under the approved retention policy.

Submissions carry an account/channel-unique request ID. An identical replay returns
the existing submission; a different post/body under that ID conflicts. Withdrawal
must not let a retry recreate the comment. Proposed validated rate limits: 5
submissions/account/minute, 20/IP/minute, no queue; return `429` with retry guidance.

Reactions require an active shared account with portal access. A comment-only block
does not stop voting. Use set/switch/clear semantics, unique channel/post/account
persistence and revision checks; resolve conflicting duplicate upserts explicitly.
Return authoritative counts and caller state without exposing a voter directory.
Retain a revision/tombstone for cleared reactions so stale requests cannot overwrite
a newer choice; count only active values.

Queries must push predicates, projection, ordering and limits into MongoDB.
Comments use stable creation-time/ID cursors, default 20 and maximum 50 per page.
Account lists use bounded global search and registration-time/ID pagination.
Activity details expose totals/continuation rather than pretending a first page is
all history. Query reaction/comment aggregates for counts; batch article summaries
for up to 50 IDs instead of per-row calls. No separate cached-counter system initially.

### BackOffice directory, detail and moderation

Add Community/Public Accounts separate from staff RBAC. Query all `PublicAccount`
records directly, including Freezer-created and zero-activity accounts. Creation
origin is not inferred or required. Selecting a channel does not filter identity
out of the global directory; it changes portal access controls and interaction tabs.

Detail shows safe account identity/registration date, selected-channel portal status,
comment restriction, paginated comments and current reactions with article links.
No activity is a valid empty state. A channel administrator can preemptively restrict
an existing account before its first interaction. Article commenters link back to
the same public account detail.

The global directory is an explicit platform visibility permission, not something
automatically granted to every staff user with a channel assignment. Proposed keys:
`publicaccounts.view`, `blog.interactions.view`, `blog.comments.moderate`,
`portalaccess.manage`, `blog.interactions.manage`.
The interaction umbrella implies its viewing/moderation leaves; portal-access-manage
implies interaction-view. Directory/detail identity reads require publicaccounts-view;
selected-channel activity requires interaction-view and channel authorization.
Minimal commenter display identity can be returned to moderators without granting
the directory. Keep `backoffice.manageall` and existing staff permission assignment.
Do not infer these permissions from `users.manage` or `pages.update`.

Global directory projection initially contains account ID, username, display name,
registration date and active state, not email, password hashes, session records,
legal evidence or staff permissions. Contact-data access/global identity mutation
would require a separate explicit platform permission and approved scope.

Portal block/revoke/restore actions are channel-authorized, revision-protected,
audited and require an internal reason. Persist latest actor/time/reason in the
changed record so attribution survives a best-effort audit sink failure.
Never mutate global account IsActive/security version to implement these actions.
Historical comments/reactions remain; moderators handle comment visibility separately.
Add per-article moderation and a channel-wide pending queue with filters/counts.

### API, cache and publication

New JSON routes use `/api/v1`. Separate anonymous approved-comment/count controllers,
PublicAccount-scheme capabilities and staff directory/moderation/access controllers.
Do not add unsafe actions to the class-level anonymous BlogController.
Global directory reads are intentionally unscoped; selected-channel activity/access
routes require channel context. Update shared transport prefix classification so
global reads do not wait for or leak channel context, but scoped mutations include it.

Public reads omit credentials; account operations use dedicated cookies/CORS and
CSRF on every unsafe request. Staff calls use existing staff auth/CSRF. Admin bearer,
API-key or fake-development identity cannot substitute for a public account on
production interaction writes. Shared identity cookies are not cross-host SSO.

Keep the existing 300-second article cache and publication eviction unchanged.
Interaction/access/private account/admin responses initially have no output caching.
Use private/no-store for personalized responses and exclude them from service-worker
caching. No caller-specific data enters cached public article/comment DTOs.
A reaction does not purge editorial content.

Every public interaction checks current publication/channel, even when cached HTML
is still visible. Unpublication retains interactions; republishing restores them.
Hard deletion leaves moderated history with an unavailable-article marker, never
reattached to a new post reusing the slug. Admin history queries may inspect these
records; permanent purge needs approved retention. Already-authorized in-flight
requests may finish; requests after restriction/unpublication acknowledgment must
honor the new state. Anonymous approved reads remain available after an account ban.

### Sharing, UX and additional article functionality

Extract the homepage share control app-locally; reuse existing Facebook/WhatsApp,
add copy-link and progressive native Web Share. Use canonical article URLs without
query/tracking fragments; fix touched canonical/share derivation for alternate hosts.
Cancellation is not failure; real clipboard/share errors need accessible fallback.
Do not report share clicks as confirmed social publications or promise a complete
account sharing history. Analytics remains subject to existing consent.

Impeccable implementation workflow: `shape` then `harden`, as refinement, not redesign.
Public article mode is Read; preserve [public DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md),
body, SEO and renderer. BackOffice mode is Operate; preserve React Bootstrap/Lucide,
page headers, data routes, permission-aware actions and channel selection. No public
PRODUCT.md or BackOffice PRODUCT.md/DESIGN.md was found; source remains incumbent.
Require mobile/desktop captures for public article, global account directory,
selected-channel detail and queue; test empty/populated/long-content states, keyboard,
focus, contrast, pressed reaction states, announcements and overflow.

Deferred recommendations: copy-link, email verification/recovery/deletion, report-comment queue,
per-article close-comments, reapproved editing, replies/notifications, related articles,
tag filtering, reading time/table of contents and bookmarks. Do not implement these
without approval.

## Impacted Projects

| Project/surface | Impact |
| --- | --- |
| Models | Three new aggregates, collections and permission constants; existing accounts/sessions unchanged |
| Domain | Portal access/interaction services, repositories/mocks, global paginated account queries, permission expansion |
| MorWalPiz.Contracts | Safe public/admin DTOs; current registration/session/article bodies remain compatible |
| ServerAPI | Public/account interaction controllers, access checks, composition and limits |
| BackOffice | Global account directory, scoped activity/moderation/access APIs, audit/index registrations |
| BackOffice.Tests | Shared/API/auth/channel/mock/concurrency behavior tests |
| Shared frontend models/services | Additive DTOs, typed functions and correct scoped/unscoped transport routing |
| Public client | Account-route repair, article interactions, sharing and state/error behavior |
| Admin SPA | Global directory/detail, selected-channel controls, moderation queue and permissions |
| FreezerAnalysis.Api and tests | Verify only: shared identity/store/session behavior remains unaffected |
| FreezerAnalysis.spa, ask.client, Shooting ITA | Verify only: shared service/model compatibility; no feature changes |
| Shop client | Frozen compatibility consumer; no implementation or activation |
| Layout, MvcHelpers, ServiceDefaults, AppHost | Verify existing rendering/auth/telemetry/topology reuse; no behavioral changes required |
| Other services, WPF apps and standalone tools | Not applicable: no blog interaction ownership or necessary reference change |
| API/admin/public deployment and Mongo operations | Additive index/backend-first rollout, account-store and browser/crawler proof |

## Impacted Files

Repository-relative paths; new names below are recommendations following current ownership.

### Modify

- `MorWalPizVideo.Models/Constraints/DbCollections.cs` and `AuthorizationKeys.cs`:
  new collections and directory/moderation/access permission constants.
- `MorWalPizVideo.Domain/Security/AuthorizationPermissionExpander.cs`: explicit implications.
- `MorWalPizVideo.Domain/Interfaces/IRepository.cs`, `Repository.cs`, `MockRepository.cs`:
  add bounded global account directory queries without changing identity lookup semantics.
- `MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs`: synthetic global accounts,
  zero-activity examples, two-channel interactions and restriction states.
- `MorWalPizVideo.ServerAPI/Program.cs` and `MorWalPizVideo.BackOffice/Program.cs`:
  focused services/Mongo/mock composition; BackOffice public-account repository.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`: additive
  interaction/access/directory indexes, preserving global account indexes.
- `frontend/fe-packages/models/src/index.ts` and services `src/index.ts`,
  `endpoints.ts`, `endpoints-frontend.ts`, `apiTransport.ts`: exports/endpoints and
  accurate global versus channel-scoped routing.
- Public `src/routes/config.tsx`, `routes/account/Components.tsx`,
  `routes/layout/account.tsx`, `layouts/title-header.tsx`: account routing/navigation,
  safe return destinations and explicit restoration states.
- Public `src/routes/blog/index.tsx`, `style.scss`, `routes/home/Component.tsx`,
  `utils/seo.tsx`: interaction composition, share reuse and canonical consistency.
- Admin `src/routes/blog/index.tsx`, `router/routes/lazy-index.ts`,
  `authorization/permissions.ts`, `layouts/adminMenu.ts`: summaries, new routes,
  global-directory visibility and channel-sensitive actions.
- Adjacent existing blog/account/permission/menu/transport tests: direct regression coverage.

### Add

- Comment, reaction and account-portal-access models under Models/Models.
- Focused Domain services and repository interfaces/Mongo/mock implementations.
- `MorWalPiz.Contracts/Contracts/BlogInteractionContracts.cs` plus public-account
  directory/access DTOs under Contracts; no credential/entity serialization.
- ServerAPI interaction controllers and BackOffice global directory/scoped activity/
  portal-access/moderation controllers under their existing controller directories.
- Shared frontend interaction/account-directory model/service modules and tests.
- Public app-local share component and blog interaction components/hooks.
- Admin public account list/detail, article interaction and moderation queue routes.
- Focused tests under `MorWalPizVideo.BackOffice.Tests/Features/` and beside new
  frontend owners; approved disposable-Mongo validation when required.

### Generated/Migration

None for existing identities/articles: no account cloning, ownership backfill, EF
migration, credential copy, cookie scope change, lockfile or global-index replacement.
New collections/indexes require approved Mongo operations, not automatic migration.

### Documentation

- This handoff remains the editable source of truth; record implementation/results later.
- Update relevant structured-blog, backend, frontend, security, domain-model, API and
  Mongo operations sections; distinguish shared identity from portal authorization.
- Link the interaction boundary from ADR-018 without changing shared registration.
  Add a focused ADR for global-directory authorization, channel restrictions and
  anonymous comment visibility; update the architecture index.

### Verify Only

- Models `PublicAccount.cs` and `BlogPost.cs`, Domain `Accounts/PublicAccountService.cs`,
  `BlogService.cs`, shared public account/blog contracts: preserve identity and content.
- MvcHelpers public authentication, ServerAPI account controllers, Freezer account
  controller/Mongo registration/Program and Freezer tests: no portal policy in auth.
- BackOffice staff UserController/RBAC pages, BlogPostsController, channel/authorization
  pipeline and audit helpers: preserve staff/editorial behavior.
- Public Vite/SSR/server/container files, all consuming manifests and AppHost:
  no private response caching, new dependency or topology changes.
- CI and API/admin/public/Freezer deployment workflows: coverage, trigger paths,
  compatible shared sources and actual release evidence.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | Shared identity; public interactions; staff global directory with scoped access/moderation |
| Frontend/UI | Existing account-route repair and incumbent article/admin refinement |
| Database/Data | Three additive collections, bounded queries/indexes; current accounts unchanged |
| Contracts/Models | Additive safe DTOs, no personalized article/account shape break |
| Authentication/Authorization | Existing sessions; portal policy after authentication; global visibility separated from channel powers |
| Validation | Body/cursor bounds, ownership/publication, revisions/idempotency and restriction reasons |
| Cache | Existing article tags/eviction preserved; interaction/private state uncached |
| Configuration/Secrets | Validated limits/activation; verify shared account store, no new secrets |
| Logging/Telemetry | Structured outcomes/audit; no comment bodies, email, passwords or session tokens |
| Background Jobs | Not needed; notifications/retention jobs require separate approval |
| Deployment/Operations | Additive backend/index-first rollout with real-browser/crawler and moderation readiness |
| Backward Compatibility | Freezer identity/sessions, staff RBAC, anonymous articles and push remain intact |

## Reuse Opportunities

Reuse PublicAccountService/password/session behavior; do not reinvent identity.
Reuse BlogService/public-channel resolution, public/account/admin transports, URL/error
helpers, channel bootstrap, permission expansion, AllowUser/RequireChannelScope,
page headers/Bootstrap/Lucide, audit recording/display, react-share, SEO, BlogRenderer,
existing Mongo index operations and scenario/test fixtures.
Do not use generic audit's first 50 rows as "all article activity"; query paginated
interaction records directly.

## Risks

| Rank | Cause / surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| High | Portal restriction accidentally suspends shared identity or Freezer access | Separate channel/account record; no IsActive/security-version mutation | Revoked portal interaction fails while Freezer login/session/profile remains usable |
| High | Global directory exposes identities to all channel staff | Explicit platform visibility permission and minimal projection | Plain staff/channel assignment cannot enumerate accounts; authorized admin sees zero-activity accounts |
| High | Foreign-channel restriction/activity or forged author IDs | Trusted channel/account context and server checks | Guessed IDs/body/header overrides cannot mutate/read another channel |
| High | Mixed public/private comments expose pending content | Approved-only public projection and separate private endpoints | Anonymous/cross-account requests never receive pending/rejected/hidden text |
| High | Duplicate/concurrent submission/reaction/moderation corrupts state | Unique indexes, request IDs and atomic revisions/tombstones | Exact persisted cardinality and stale-request rejection |
| High | Unwired account UI or cross-site cookies blocks feature | Repair routes and prove canonical-origin browser behavior | Existing account login/registration survives reload in supported browsers |
| Medium | Deployments use different account stores despite same service | Operator verification without revealing config values | One synthetic identity can sign in independently at both hosts |
| Medium | Account restoration failure becomes anonymous bypass-shaped UI | Explicit failed state and retry, no silent fallback | Failed restore does not mount signed-in comments/form |
| Medium | Abuse/retention/unsafe text | Premoderation, limits, withdrawal and approved policy; escaped text | Hostile text inert, queue staffed, content absent from logs |
| Medium | Cached deleted article or reused slug misroutes interactions | Live publication and stable post ID | Unpublished routes unavailable; new post inherits no old activity |
| Medium | "Portal revocation" misunderstood as anonymous content denial | Explicit authenticated-capability semantics | Public articles/comments still work after logout |
| Medium | Expensive directory/aggregation or missing share preview | Bounded indexed/batched reads and actual crawler HTML | No full scans/per-row query fanout; accessible article OG metadata |
| Low | Audit sink fails after access/moderation update | Latest actor/time/reason in persisted aggregate; logged sink failure | Committed change remains attributable |

## Technical Debt

Unwired account routes and silent restoration failures block this feature; repair now.
Documentation/deployed-host conflicts require evidence, not assumed closure.
Current account profile replacement is unchanged because admin identity editing is
not required; keep restrictions outside that document to avoid lost updates.
BlogMockRepository has independent state: preserve blog fixtures and explicitly share
synthetic account/article/access state for cross-host tests.
Global identity administration, general cache reliability and shop lifecycle remain
separate backlog. The prior channel-account migration/index-contraction proposal
is withdrawn, not a prerequisite or future default.

## Completion And Validation

Implemented end to end across Models, Domain, Contracts, ServerAPI, BackOffice,
shared frontend packages, the public client and BackOffice SPA. No feature activation
flag was added. BackOffice account list/detail responses omit email and credentials;
the focused API test asserts that projection.

Validation completed after the final behavior changes:

- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BlogInteraction"` — passed, 5 tests.
- `yarn workspace @morwalpizvideo/models build` and `yarn workspace @morwalpizvideo/services build` — passed.
- `yarn workspace @morwalpizvideo/services test` — passed, 50 tests.
- `yarn workspace morwalpizvideo.client test src/routes/blog/Interactions.test.tsx src/routes/blog/index.test.tsx` — passed, 5 tests.
- `yarn workspace morwalpizvideo.client build` — passed.
- `yarn workspace back-office-spa build` — passed.
- Prettier was applied to the changed files that failed the focused formatting check.

The .NET build reports existing ImageSharp 3.1.12 NU1902/NU1903 vulnerability
warnings. No browser visual capture, disposable-Mongo concurrency test, production
deployment test or cross-host account-store verification was performed.

## Rollout And Remaining Work

Before production use, verify the Mongo-compatible provider supports the startup
collection/index operations and grants the required permissions. The startup path
fails closed on conflicts; it never drops indexes or resolves duplicate data.
Verify that ServerAPI, BackOffice and FreezerAnalysis use the intended shared
public-account store. Their account identity is shared conceptually, but host-owned
session cookies remain separate.

Assign the global `publicaccounts.view` permission only to approved staff; ordinary
channel assignment does not imply access to the global account directory. Confirm a
moderation owner and retention/privacy policy for pending, rejected and hidden comment
bodies before collecting production data. Author withdrawal, reply threads,
notifications, share analytics, email recovery/verification and global account
administration are not implemented.

Canonical-host cookie behavior and social crawler metadata also need deployment
verification. Visual browser validation was not available for this delivery.

No identity migration, account cloning, EF migration, or destructive rollback is
required. Existing account IDs, credentials, sessions and Freezer references remain
unchanged. Rollback must remove/disable both API interaction capabilities and their
UI controls while retaining records and restrictions; do not bring back an endpoint
that ignores those restrictions. Public articles/comments remain public.

## Migration And Rollback

No identity migration is needed. Preserve existing account IDs, credentials, username/
email uniqueness, sessions and Freezer references. Existing active accounts receive
portal capability by default unless an access record says otherwise. Do not create
membership records for every account or infer who was a "reader" previously.

Approve/apply additive indexes: reaction channel/post/account unique; comment
channel/account/request-ID unique; access channel/account unique; comment post/state
and account history cursor indexes; moderation queue and reaction history indexes;
global account registration/search indexes matching real queries.
Verify BSON field names and current account uniqueness/session TTL deployment.
No request-time index creation, global-index removal, EF migration or backfill.

Deploy additive API support and indexes, then shared packages/admin/public clients;
activate interactions only after identity-store/browser/moderation gates pass.
Monitor auth/CSRF, forbidden capability use, conflicts, 429, queue backlog, query
latency and audit failures without sensitive content.

Rollback disables interactions on both hosts and removes controls while retaining
comments, reactions and access restrictions. Existing identity/login/Freezer behavior
stays usable. Never enable an older interaction endpoint ignoring retained restrictions.
No destructive collection deletion or identity reversal is required.
Public content remains public; restrictions cannot erase previously delivered HTML.

Retention, rejected/hidden bodies, tombstone/idempotency window, privacy requests and
permanent purge need owner approval before collecting live data. No content TTL,
new cookie scheme, social credential deployment or background job is proposed.

## Deferred Product Decisions

1. Assign platform-wide `publicaccounts.view` and interaction/moderation permissions
   to the intended BackOffice roles.
2. Set moderation, retention, privacy-request and abuse-escalation ownership.
3. Decide whether email verification/recovery/deletion should be a separate feature.
4. A full reaction-change timeline, share-intent analytics, replies and notifications
   are additional persistence/privacy scope and are not implied by this implementation.
