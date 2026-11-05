# Blog Reader Interactions Handoff

Status: proposed architecture for user review; not approved or implemented.
User decisions incorporated: anonymous comment viewing, channel-owned registration
and administration, and a directory of every portal registrant (2026-11-05).
Evidence inspected: 2026-11-05. Source inspection only; no applications, tests,
builds, deployments, or database operations were run.

## Outcome

Enable article sharing, registered-reader comments, and like/dislike reactions in
the public MorWalPiz application. Give BackOffice an article interaction view,
comment moderation, and reader details containing their comments and reactions.
A signed-in comment-blocked reader must not see the comments section, and ServerAPI
must reject that account's authenticated comment operations. Approved comments are
public; accessing them after logout is explicitly acceptable to the user.

Confirmed requirements:

- Anonymous visitors can read approved comments. Posting and reacting require an
  active account registered to the current portal's channel.
- Registration, account management, articles, interactions and restrictions are
  channel-owned. Public clients cannot select a foreign channel in a request.
- A comment block applies across articles in the selected channel, not across
  unrelated applications. It does not disable login, sharing, or reactions.
- BackOffice lists every account registered to the selected channel's portal,
  including readers with no comments or reactions, and manages them from detail.

Proposed defaults retained for review:

- Sharing needs no account. Comments start pending; moderators approve, reject or hide.
- Reader details show all paginated comments and current reactions in that channel,
  not staff RBAC history or an invented record of completed social shares.
- Reader administration includes profile editing and account deactivation/reactivation,
  separately from comment-only blocking. Password recovery and permanent deletion
  remain separately scoped lifecycle work.

Completion boundary: one coherent path from working registration through an
article interaction to BackOffice inspection, reader management, moderation,
blocking and unblocking. Registration must immediately populate the channel directory.
Do not redesign the blog editor, merge identity stores, reopen the shop, implement
social auto-publishing, or add a general-purpose activity/event platform.

## Current Evidence

### Capability assessment

| Capability | Verified current source | Gap |
| --- | --- | --- |
| Structured articles | Published-only public list/detail, separate draft snapshot, channel ownership, revision protection | No interaction model, endpoints, or article interaction UI |
| Reader registration | Public account contracts, Domain service, Mongo/mock repository, registration/login/session/profile/logout endpoints | Screens are not connected to the active router; accounts/lookups are not channel-scoped |
| Public account navigation | Root mounts the account provider; authenticated header links to profile | The profile URL is not registered; anonymous discovery must be included in the repair |
| Social sharing | Homepage uses `react-share` for Facebook/WhatsApp; blog article has Open Graph and Article metadata | Article detail has no share controls; deployed crawler behavior is unverified |
| Comments / like / dislike | No such fields or workflow in the inspected blog model, contracts, service or routes | New focused persistence, contracts, APIs and UI required |
| BackOffice user details | Staff profile, RBAC, password controls and administrative audit logs | Reads staff `User`, not `PublicAccount`; cannot show registered readers by reusing the staff ID |
| BackOffice article management | Create/save/publish/unpublish/delete/images under channel scope and Pages permissions | No reaction counts, comment queue or reader restriction controls |
| Comment restriction | Public accounts have activity/security-version state, not a blog comment restriction | Needs a separately owned, server-enforced moderation rule |

### Source ownership and flow

- [Public route configuration](../../frontend/morwalpizvideo.client/src/routes/config.tsx)
  defines `/blog` and `/blog/:slug`, but imports none of the account screens and
  defines no `/account/login`, `/account/register`, or `/account/profile`.
  [Browser bootstrap](../../frontend/morwalpizvideo.client/src/main.tsx) uses this
  route configuration directly.
- [Account screens](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx)
  already invoke registration/login/profile services and navigate to those URLs.
  [Public root](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx)
  mounts the [account provider](../../frontend/morwalpizvideo.client/src/routes/layout/account.tsx).
  Its restoration catch currently converts every failure to an anonymous session,
  making an outage indistinguishable from being signed out.
- [PublicAccountController and PublicProfileController](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs)
  expose versioned endpoints and use account CSRF validation.
  [PublicAccountService](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs)
  normalizes identities, records legal acknowledgements, hashes passwords,
  creates eight-hour sessions, and validates account activity/security version.
  [PublicAccount](../../MorWalPizVideo.Models/Models/PublicAccount.cs) and its session
  are separate from [staff User](../../MorWalPizVideo.Models/Models/User.cs).
- [Public account authentication](../../MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs)
  reads the dedicated HttpOnly cookie and revalidates the persisted session.
  [Public account transport](../../frontend/fe-packages/services/src/publicAccountService.ts)
  uses the isolated account client; [transport core](../../frontend/fe-packages/services/src/apiTransport.ts)
  separately owns public, account and administrative request contexts.
- Public blog flow is
  [article route/loader](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx)
  → [blog service client](../../frontend/fe-packages/services/src/blogService.ts)
  → [BlogController](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs)
  → [BlogService and repositories](../../MorWalPizVideo.Domain/BlogService.cs)
  → [BlogPost](../../MorWalPizVideo.Models/Models/BlogPost.cs).
  ServerAPI resolves the configured public channel, never a visitor-supplied one.
  Both public content endpoints have 300-second output caching tagged `blog`.
- [BlogPostsController](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs)
  receives channel ownership through the established pipeline. Publication and
  deletion coordinate content cache eviction through `ICrossApiService`.
  Blog writes replace the aggregate with an expected revision.
- [Blog contracts](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs) and
  [TypeScript blog models](../../frontend/fe-packages/models/src/blog.ts) expose
  published content without public account identity or interaction state.
- [Staff detail page](../../frontend/back-office-spa/src/routes/rbac/UsersDetailPage.tsx)
  reads RBAC and staff audit endpoints. [UserController](../../MorWalPizVideo.BackOffice/Controllers/UserController.cs)
  reads staff through `DataService`; it is not a public reader-management API.
  BackOffice currently registers no public-account repository in its
  [composition root](../../MorWalPizVideo.BackOffice/Program.cs).
- [Homepage ShareMenu](../../frontend/morwalpizvideo.client/src/routes/home/Component.tsx)
  provides prior art. [Public package manifest](../../frontend/morwalpizvideo.client/package.json)
  already declares `react-share`; no sharing SDK dependency is necessary.
  [SEO helper](../../frontend/morwalpizvideo.client/src/utils/seo.tsx) emits canonical
  and Open Graph metadata, but browser canonical origin follows the current host.
- [Mongo index operations](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs)
  declare public-account username/email uniqueness and session TTL indexes.
  Source declarations are not evidence that indexes have been applied.
- Channel ownership is absent from `PublicAccount` and its session. Current
  [repository interface](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs),
  [Mongo repository](../../MorWalPizVideo.Domain/Interfaces/Repository.cs) and
  [mock repository](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs) find
  identities by username/email/ID without channel filtering. Current indexes make
  username/email unique across the whole account collection, not per portal.
- [Freezer account controller](../../FreezerAnalysis.Api/Controllers/FreezerAccountController.cs)
  also uses `IPublicAccountService`. Its
  [Mongo registration](../../FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs)
  registers the same account repository against the platform database, while its
  feature data uses a distinct database. The public cookie handler also invokes the
  shared service without an expected identity scope. Every one of these consumers
  must be updated before scoped identities are introduced.
- [Endpoint tests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs)
  exercise registration, CSRF, restoration, profile/logout and legal acceptance.
  [Blog controller tests](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs)
  and [service tests](../../MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs)
  cover publication, ownership, revisions, validation and caches.
  [Public route tests](../../frontend/morwalpizvideo.client/src/routes/blog/index.test.tsx)
  cover published rendering, SEO and loader errors. These were inspected, not run.

### Evidence conflicts and release uncertainty

- Architecture documents describe additive account UI, but the active router does
  not connect those screens. Source wins; registration is not end-to-end complete.
- [Frontend architecture](../architecture/frontend.md) records a deployed account
  CSRF `401` blocker. The current [ServerAPI deployment workflow](../../.github/workflows/deploy-serverapi.yml)
  now includes an anonymous CSRF/CORS/cookie smoke gate. Neither source nor that
  workflow proves the current deployed gate passed; obtain release evidence.
- The architecture guide identifies Aruba hosting, while the current
  [public client workflow](../../.github/workflows/deploy-morwalpizvideo-client.yml)
  builds client plus SSR and deploys an Azure container. Confirm which artifact
  serves `morwalpiz.com`; do not infer deployed SSR from local rendering tests.
- Current relevant .NET manifests target .NET 10. Domain also references
  YouTubeUtilities, so its dependency map is broader than the historical leaf-only
  description. This feature needs no project-reference changes.

## Proposed Design

### 1. Repair and prove public registration first

Connect the existing account screens under the existing public root, add discoverable
sign-in/register navigation, and preserve anonymous content routes. Restore sessions
server-authoritatively. Distinguish session-loading, signed-out and failed-restoration
states. While restoration is unknown, do not mount account-aware comments or a
posting form. Once genuinely anonymous, show approved public comments; an account
service outage must not silently switch a possibly blocked session to anonymous.

Support returning to the originating article after registration/login. Accept only
validated same-application relative destinations, without external/protocol-relative
redirects. Preserve an unsent comment locally while the page remains open, but do not
persist comment bodies or credentials in browser storage.

Do not register readers as BackOffice staff, assign them staff groups, issue admin
cookies, merge accounts by email, or enable public self-registration on BackOffice.
BackOffice manages portal accounts and interactions using its own administrator session.

#### Channel-owned account namespace

Recommend separate account identities per channel, not merely membership tags on a
global identity. Registering the same email/username on a different portal is an
independent registration; credentials, profile, suspension and interactions do not
grant access across channels. This interprets the user's channel-owned registration
requirement; no cross-portal SSO or automatic identity merging is proposed.

Extend `PublicAccount` with immutable nullable channel ownership and an account
revision. A non-empty channel identifies a portal account. Missing/null ownership
is the explicit legacy/non-portal namespace retained for FreezerAnalysis, never an
implicit match for any portal. New portal registrations always get their channel
from the server's configured public-channel resolver. Bodies, query parameters and
public channel headers cannot supply or override ownership.

Make expected identity scope mandatory at service/repository boundaries for
registration, username/email lookup, profile reads/writes, session validation and
revocation. Resolve it from trusted host context: ServerAPI uses its configured
channel; BackOffice uses its authorized selected channel; FreezerAnalysis explicitly
uses the legacy/non-portal namespace. Reuse existing password/session logic, not a
second authentication system. Do not leave a broadly accessible unscoped lookup
overload for convenience.

Session validation loads the account in the expected scope before accepting its
activity/security version. Carry channel ownership in newly created session records
and require it to agree with the referenced account. Legacy sessions are accepted
only in the legacy namespace during transition; mapped portal accounts must sign
in again. Profile/session/logout endpoints need the same scope enforcement as
comments; a valid channel-A cookie must not become channel-B access.

Username/email uniqueness becomes per-channel, with separate uniqueness for the
legacy namespace. Resolve duplicates within scope, never by taking the first global
match. Existing public JSON registration/login URLs and payloads can stay unchanged
because scope is server-owned. Channel ownership cannot be edited or transferred
through reader detail.

Existing accounts lack evidence of portal/channel ownership. Inventory and approve
an explicit mapping before migrating any account. Do not assign every legacy account
to the currently configured channel or infer ownership from email. FreezerAnalysis
identities and feature references must remain usable. The migration decision for
accounts used by both applications is a release prerequisite.

### 2. Keep interaction persistence separate from article content and identity

Add three Models-owned aggregates and focused Domain repository/service behavior:

| Aggregate | Required responsibilities |
| --- | --- |
| Blog comment | Stable ID, channel ID, post ID, public account ID, plain-text body, moderation state, creation/update UTC times, revision, submission request ID, latest moderation actor/time/reason |
| Blog reaction | Channel ID, post ID, public account ID, one current value (like/dislike), update UTC time and revision |
| Reader comment restriction | Channel ID, public account ID, blocked state, revision, latest administrative actor/time/reason |

Use ObjectId-compatible entity identifiers. Resolve slug to stable post ID before
persisting interactions. Do not attach unbounded arrays to `BlogPost`, `PublicAccount`
or staff `User`; do not make each reaction increment the editorial revision.

Persist restrictions outside `PublicAccount`: current profile updates replace that
document, and its service is also used by FreezerAnalysis. Blog restrictions must
not change another application's login behavior or be lost during profile saves.

All queries push channel filters, projections, ordering and bounds into MongoDB.
Use cursor pages ordered by immutable creation time plus ID for comments. Default
page size 20, maximum 50; reject invalid bounds/cursors with explicit errors.
Admin lists include totals and continuation metadata, never silently truncate
"all interactions" to one page.

Initial counts are database aggregations over authoritative reaction/comment records,
not independently maintained counters. Batch summaries for up to 50 article IDs in
an admin page rather than one query per row. Count only approved comments publicly.
Performance evidence, not speculative scale, should justify later cached counters.

### 3. Reactions are one reversible choice per reader/article

Everyone can read like and dislike totals. An authenticated public reader can select
one value, switch it, or clear it. Provide set/clear semantics rather than an ambiguous
toggle endpoint. Return authoritative totals and the caller's state separately from
cached article content.

Enforce uniqueness on channel/post/account at the database boundary. Use revision
checks to reject competing stale changes, and reconcile after conflict. Handle
duplicate upsert races explicitly; retries must never create a second vote.
Do not expose a public voter directory or email addresses.

A comment block does not stop reactions. Channel-account deactivation is a separate
reader administration action and does stop authenticated interactions and login.
A reaction-only restriction is outside this first scope.

### 4. Comments and moderation

- New comments contain trimmed plain text of 1-2000 characters. No HTML, markdown,
  uploads, executable embeds or automatic link previews. React must render escaped
  text, including hostile text and reader display names.
- New comments are pending. Anonymous visitors see only approved comments. Allowed
  signed-in readers additionally see their own pending submissions through a
  separate private query; public responses never include pending/rejected/hidden text.
  Rejected/hidden own submissions expose status, not private moderation reasons.
- Moderators approve pending comments, reject pending comments, and hide approved
  comments. Restoring a hidden/rejected comment requires explicit approval.
  Revision checks prevent conflicting moderation decisions.
- Allow authors to withdraw their own comments. Withdrawal is terminal and removes
  the body from ordinary public/admin responses; preserve only a tombstone and
  necessary moderation metadata according to the approved retention policy.
  Moderators cannot restore withdrawn content.
- Editing, replies, mentions and comment reactions are deferred. Do not let a
  moderator silently rewrite a reader's text.
- Require a client-generated submission request ID, unique per channel/account.
  Replaying identical content returns the original submission; reusing the key with
  different post/body returns conflict. Preserve a tombstone so withdrawal does not
  allow a retry to recreate the comment.
- Rate-limit submissions by account and IP using the existing limiter infrastructure.
  Proposed initial limits: 5 submissions/account/minute and 20/IP/minute, no queue.
  Return `429` with retry guidance; configure through validated host options.

Article details show likes/dislikes, approved count, pending count and paginated
comments with filters and moderation actions. Add a channel-wide pending queue so
moderators do not have to open every article. No social-network credentials or AI
moderation provider is needed.

### 5. Reader details and comment blocking

Add a separate BackOffice Community/Readers surface; do not append public reader
controls to staff `/rbac/users/:id`. Query channel-owned accounts directly, not the
union of interaction authors. A registration appears immediately even with zero
activity. Include inactive and comment-blocked accounts, bounded server-side search,
status filters, registration-date ordering with ID tie-break and real pagination.

Reader detail shows registration date, account status, profile, comment restriction,
paginated comments with article/status/date, current likes/dislikes with article/date
and totals. A reader with no interactions has a clear empty state. Link article
commenters to reader details and interactions back to article moderation.

Use separate permission families for portal-account administration and editorial
moderation. Proposed lowercase keys: `portalaccounts.view`, `portalaccounts.update`,
`portalaccounts.manage`, `blog.interactions.view`, `blog.comments.moderate`,
`blog.readers.moderate`, `blog.interactions.manage`. Expand account-manage to
view/update; update implies account-view. Expand interaction-manage to its three
leaves, with moderation/restriction leaves implying interaction-view.
Do not infer these grants from staff `users.manage` or content `pages.update`.
Keep the existing `backoffice.manageall` override and channel checks.

Account-view grants the owned portal's directory/profile, including email; account
update grants bounded username/email/display-name editing and deactivation/reactivation.
Blog-only moderators receive minimal display identity, never contact email or account
controls. Compose detail tabs by their permissions; interaction data requires
interaction-view, and account administration requires the account leaves.
An authorized channel administrator can inspect any account registered in that
channel, regardless of whether it has ever interacted.

All profile/status changes are revision-protected, scope-filtered and audited.
Validate normalized uniqueness within the selected channel. Use focused atomic
updates and increment the account revision for public self-service edits as well as
admin edits; return conflict rather than overwrite concurrent changes. Preserve
legal acknowledgements and channel ownership. No endpoint exposes credential hashes,
raw sessions, staff permissions or editable security-version fields.

Deactivation increments security version atomically and prevents login and session
use for that channel account. Reactivation does not revive old sessions; require
sign-in again. Preserve historical interactions without automatically hiding them.
Comment-only blocking is distinct and leaves login/reactions available. No manual
staff password assignment, permanent deletion or impersonation is added.

Block and unblock are audited revision-protected administrative actions with a
required internal reason. No public reason text. A block prevents authenticated
comment reads, submissions and withdrawals in that channel; the signed-in UI removes
the entire comments heading/list/form, not merely disables the submit button. Provide a
non-comment-specific account/support notice if needed.

Anonymous approved-comment reads use a credentialless public endpoint. They are
intentionally still usable by someone who logs out after a block; no secrecy,
anti-evasion or hidden-content guarantee is claimed.

For signed-in readers, an access response must be read before mounting or fetching
the comments section. Recheck on
article entry, window focus, sign-in/session change and after relevant errors.
After a block is acknowledged, subsequent authenticated comment requests return forbidden.
An already-open tab clears loaded comments at its next access revalidation; no
WebSocket infrastructure or instantaneous remote erasure is promised.

Blocking does not automatically delete or hide historical approved comments.
Moderators manage those separately. Unblocking restores access without changing
comment statuses. Account creation under another identity cannot be absolutely
prevented by this feature; premoderation and rate limits reduce abuse.

### 6. API and cache boundaries

Create distinct controllers for anonymous approved comments/counts, account-aware
access/own-comment/reaction operations and staff reader management/moderation.
Do not put writes in the current class-level anonymous `BlogController`.
Anonymous comment reads use public transport with credentials omitted. Account
access and own-pending lists use account transport; never blend private fields into
the public comment response.

New JSON routes use `/api/v1`. Public interaction paths resolve the configured
public channel; administrative paths use `RequireChannelScope` and the existing
staff authorization pipeline. New BackOffice route prefixes must be added to the
shared transport's channel-scope classification.

Account-aware calls explicitly select the PublicAccount scheme and credentialed
account CORS policy. Validate CSRF for every unsafe account operation. BackOffice
uses its existing staff cookie/CSRF flow, never the public account cookie. Account
registration, profile, session and logout also enforce the trusted channel scope. Bearer,
fake development and API-key principals must not substitute for a reader session
on production interaction endpoints.

Keep the existing 300-second public article cache unchanged. Interaction totals,
access, viewer state, comment lists and admin lists initially have no output cache;
account-specific responses use private/no-store cache policy and are excluded from
service-worker caching. No personalized field enters `PublicBlogPost`.
Do not purge article content on each vote. Existing publish/unpublish/delete cache
coordination remains untouched.

Every public article interaction operation checks that the target is currently published in the
resolved channel. Unpublication/deletion makes interaction endpoints unavailable,
even while old article HTML remains cached. Requests already authorized before a
concurrent block/unpublish may finish; subsequent authenticated comment requests must
honor a block, and all public interaction requests must honor unpublication.
Anonymous approved reads remain allowed after a comment block. This is the bounded
consistency model, not a cross-document transactional guarantee. Administrative
history/moderation queries can still inspect unpublished or unavailable articles.

Unpublication retains interactions for republishing. Hard article deletion retains
channel-scoped moderation history with an unavailable-article marker; it never
reattaches those records if the slug is reused by a different post ID. Permanent
purge follows the separately approved retention process, not request-time cascading
deletion across collections.

### 7. Share the article, not credentials or personal activity

Extract the homepage's app-local sharing control for reuse by homepage and article
detail. Keep it out of the shared layout package unless another app actually needs
the same behavior. Reuse existing Facebook and WhatsApp integrations; add copy-link
and progressive native Web Share where supported.

Use the canonical article URL without tracking/query fragments; fix the touched
canonical/share derivation to remain correct on alternate hosts. Native share
cancellation is not an error; actual clipboard/share failures get accessible
feedback and a usable link fallback.

A share button cannot prove publication on a social network. Do not count clicks
as completed shares or promise a complete per-reader sharing history. If analytics
is later approved, label it share intent and respect the existing consent boundary.
Verify canonical and Open Graph metadata in the deployed crawler-visible HTML;
add X/Twitter card metadata only as a focused sharing enhancement, not an SEO rewrite.

### 8. UI planning and optional article enhancements

Use the installed Impeccable workflow at implementation time, not as an architecture
redesign. Narrow commands: `shape` for the interaction layout, then `harden` for
states and permission feedback. This is an incumbent refinement.

- Public article mode: Read; preserve body/SEO/renderer and the dark editorial
  identity recorded in [public DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md).
  Put sharing/reactions after the article and comments after the interaction row.
- BackOffice mode: Operate; preserve React Bootstrap/Lucide, existing page headers,
  channel selection, data routes and permission-aware action conventions. Directory
  and detail include newly registered/no-activity, active, inactive and blocked states.
- No public PRODUCT.md or BackOffice PRODUCT.md/DESIGN.md was found. Use current
  source as the incumbent, not permission to replace its visual world.
- Include anonymous/loading/error/blocked/pending/empty/conflict/rate-limited states.
  Keyboard support, labeled reaction buttons with pressed state, status announcements,
  validation associations and mobile wrapping are acceptance criteria.
- Minimum visual evidence: public article and admin article/reader/queue captures at
  narrow mobile and desktop sizes, with long names/text and empty/populated states;
  check focus, contrast, overflow and screen-reader behavior. No design artifacts
  or browser work were produced in this analysis.

Recommended additions inside the baseline are copy-link, a pending moderation queue,
withdrawal, rate limiting and explicit moderation reasons. These close operational
or user-safety gaps in the requested behavior.

Optional follow-ups, not part of the implementation boundary:

1. Email verification, password reset and self-service account deletion: important
   before a broad public launch, but require a bounded account-lifecycle plan,
   delivery provider decisions and cross-consumer review.
2. Per-article close-comments control for sensitive/old articles.
3. Report-comment action and a moderator abuse queue.
4. Reader comment editing with reapproval; threaded replies and notifications.
5. Related articles and tag filtering, reusing existing published metadata.
6. Reading time/table of contents derived from the canonical document.
7. Bookmarks and a reader-facing interaction history.

Do not add all optional features automatically. Email verification can reduce
throwaway-account abuse, but email ownership alone does not establish trust.

## Impacted Projects

| Project/surface | Impact |
| --- | --- |
| MorWalPizVideo.Models | Channel-owned account/session schema, revision, interaction aggregates, collections and permissions |
| MorWalPizVideo.Domain | Scope-aware account/session operations, paginated directory/admin updates, interaction invariants and permission expansion |
| MorWalPiz.Contracts | Safe admin account and interaction DTOs; preserve public account request URLs/body compatibility |
| MorWalPizVideo.ServerAPI | Trusted channel registration/login/profile/session behavior, public comments and authenticated interactions |
| MorWalPizVideo.BackOffice | All-registrant directory, account administration, moderation, audit and scoped index transition |
| MorWalPizVideo.BackOffice.Tests | Domain/API behavior, metadata, tenant/auth, mock and concurrency coverage |
| frontend/fe-packages/models | Additive interaction DTO exports |
| frontend/fe-packages/services | Typed public/account/admin functions, endpoints and scoped-prefix handling |
| frontend/morwalpizvideo.client | Route repair, account error states, article interactions, sharing, targeted SEO |
| frontend/back-office-spa | All-registrant channel directory, profile/status controls, article interactions, pending queue and reader details |
| frontend/fe-packages/layout | Verify only: existing renderer/presentation remains unchanged |
| FreezerAnalysis.Api and FreezerAnalysis.Api.Tests | Modify shared-account callers/composition to explicit legacy scope; prove old identities and feature references remain usable |
| frontend/FreezerAnalysis.spa | Verify only: existing account JSON/session transport stays compatible; no portal registration or blog behavior |
| frontend/ask.client and frontend/shooting-ita-frontend | Verify only: shared services/model consumers, no feature changes |
| frontend/morwalpiz-shop.client | Frozen verify-only consumer; no shop implementation or activation |
| MvcHelpers | Scope-aware cookie validation with host-owned account scope; retain separate cookie schemes |
| AppHost and ServiceDefaults | Verify orchestration/telemetry reuse; no topology change |
| Other .NET services, WPF apps, standalone tools and remaining frontend workspaces | No behavior change; relevant project/package manifests show no need for blog-specific work |
| API/public/admin and Freezer deployment pipelines, Mongo operations | Coordinated scope-safe reader rollout, index replacement, approved account migration and browser/crawler gates |

## Impacted Files

Paths below are repository-relative. New file names are recommendations; keep the
existing owning directory patterns rather than creating a new project.

### Modify

- `MorWalPizVideo.Models/Constraints/DbCollections.cs`: new collection constants.
- `MorWalPizVideo.Models/Models/PublicAccount.cs`: immutable portal channel scope,
  account revision and session scope, with explicit legacy BSON compatibility.
- `MorWalPizVideo.Domain/Accounts/PublicAccountService.cs`,
  `MorWalPizVideo.Domain/Interfaces/IRepository.cs`, `Repository.cs` and `MockRepository.cs`:
  required expected scope on identity/session operations, per-channel uniqueness,
  paginated registered-account queries and atomic revision/status updates.
- `MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs` and
  `MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs`:
  trusted host scope on registration/login/profile/session/logout and cookie validation.
- `MorWalPizVideo.Models/Constraints/AuthorizationKeys.cs` and
  `MorWalPizVideo.Domain/Security/AuthorizationPermissionExpander.cs`: moderation
  account-administration and moderation leaves, umbrellas and tested implications.
- `MorWalPizVideo.ServerAPI/Program.cs`: focused services/repositories in both
  production and mock branches, trusted portal-account scope and interaction limiters.
- `MorWalPizVideo.BackOffice/Program.cs`: same interaction repositories/services,
  plus scoped public-account administration without public authentication in this host.
- `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`: reviewed
  interaction/directory indexes and reviewed replacement of global account uniqueness.
- `FreezerAnalysis.Api/Controllers/FreezerAccountController.cs`,
  `FreezerAnalysis.Api/Services/FreezerAnalysisMongoRegistration.cs` and
  `FreezerAnalysis.Api/Program.cs`: explicit legacy namespace in account callers,
  authentication and mock/production composition; keep feature data/cookies unchanged.
- `FreezerAnalysis.Api.Tests/FreezerAnalysisApiTests.cs`: scope composition and
  compatibility coverage for legacy identities.
- `MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs`: coherent synthetic
  channel registrants (including zero-activity), two-channel collisions, legacy
  accounts and interaction/restriction fixtures for scope-aware mocks.
- `frontend/fe-packages/models/src/index.ts` and
  `frontend/fe-packages/services/src/index.ts`: additive exports.
- `MorWalPiz.Contracts/Contracts/PublicAccountContracts.cs` and
  `frontend/fe-packages/models/src/publicAccount.ts`: compatible revision support
  for public profile updates; do not accept client ownership/security fields.
- `frontend/fe-packages/services/src/endpoints.ts`,
  `frontend/fe-packages/services/src/endpoints-frontend.ts`,
  `frontend/fe-packages/services/src/apiTransport.ts`: endpoint ownership, isolated
  clients and channel-scope prefix classification.
- `frontend/morwalpizvideo.client/src/routes/config.tsx`: connect account routes.
- `frontend/morwalpizvideo.client/src/routes/account/Components.tsx`,
  `frontend/morwalpizvideo.client/src/routes/layout/account.tsx` and
  `frontend/morwalpizvideo.client/src/layouts/title-header.tsx`: discoverable account
  flow, validated article return path, explicit restoration failure.
- `frontend/morwalpizvideo.client/src/routes/blog/index.tsx` and `style.scss`:
  interaction composition without blocking anonymous article rendering.
- `frontend/morwalpizvideo.client/src/routes/home/Component.tsx` and
  `frontend/morwalpizvideo.client/src/utils/seo.tsx`: reuse share control and keep
  canonical sharing metadata consistent.
- `frontend/back-office-spa/src/routes/blog/index.tsx`: batched interaction summaries
  and moderation links; keep editorial actions on their existing permissions.
- `frontend/back-office-spa/src/router/routes/lazy-index.ts`,
  `frontend/back-office-spa/src/authorization/permissions.ts` and
  `frontend/back-office-spa/src/layouts/adminMenu.ts`: new routes and capability mapping.
- Existing adjacent public blog, admin blog transport/index, admin menu/permission,
  account endpoint and permission-expansion tests: preserve surrounding behavior and
  cover every scoped account/session boundary.
- `.github/workflows/deploy-serverapi.yml` and
  `.github/workflows/deploy-morwalpizvideo-client.yml`: extend only the focused release
  proof that cannot be established by existing gates.

### Add

- Models under `MorWalPizVideo.Models/Models/`: comment, reaction and reader restriction.
- Focused service/repository interface, Mongo and mock implementation under
  `MorWalPizVideo.Domain/`: keep blog interaction persistence apart from identity CRUD.
- `MorWalPiz.Contracts/Contracts/BlogInteractionContracts.cs`: distinct request,
  public access/count/viewer, comment page and admin reader/moderation responses.
- A portal-account administration contract under `MorWalPiz.Contracts/Contracts/`
  and matching shared TS model/service: safe directory/detail, revision-protected
  profile/status requests and permission-dependent contact projection.
- A small trusted account-scope resolver/context under the existing Domain account
  ownership, with host composition for portal versus legacy scope. Reuse configured
  public-channel resolution rather than duplicate it across handlers/controllers.
- Focused versioned controllers under `MorWalPizVideo.ServerAPI/Controllers/` and
  `MorWalPizVideo.BackOffice/Controllers/`: separate anonymous comments/counts,
  public account operations, owned account management and moderation boundaries.
- `frontend/fe-packages/models/src/blogInteractions.ts` and
  `frontend/fe-packages/services/src/blogInteractionService.ts`: shared typed contracts
  and explicit transport contexts.
- Public app-local share component under
  `frontend/morwalpizvideo.client/src/components/`, and interaction components/hooks
  beside `src/routes/blog/`: keep public UI app-owned.
- Admin article interaction/queue routes beside
  `frontend/back-office-spa/src/routes/blog/` and reader list/detail routes under a
  separate Community/Readers feature directory.
- Focused backend tests under `MorWalPizVideo.BackOffice.Tests/Features/`, public
  account route/provider and interaction tests beside their owners, admin moderation/
  reader tests beside new routes, shared-service transport tests.

### Generated/Migration

No EF migration, generated source or lockfile change is expected. Mongo account/session
scope expansion, approved legacy-to-portal mapping and global-uniqueness replacement
are now required operational transitions, alongside additive interaction collections.
Use the existing reviewed Mongo operations and a bounded owner-approved migration
artifact after mapping is settled; never imply automatic migration. Document
inventory, idempotency, conflicts and rollback limits.

### Documentation

- This handoff: retain decisions, actual changed files, validation and residual risks
  after implementation.
- `docs/architecture/structured-blog.md`, `backend.md`, `frontend.md`, `security.md`,
  `domain-model.md`, `api-design.md` and the Mongo operations/index inventory: update
  the relevant sections, including corrected account-route/release status.
- `docs/architecture/adr/ADR-018-public-account-boundary.md`: document the proposed
  channel-owned account namespace and migration, preserving staff/public separation.
- New focused ADR under `docs/architecture/adr/`: record approved moderation
  ownership, anonymous visibility and restriction scope; add it to the architecture index.
- FreezerAnalysis account-boundary documentation: record explicit legacy namespace
  compatibility without adding portal/channel features to that application.

### Verify Only

- `MorWalPizVideo.Models/Models/BlogPost.cs` and
  `MorWalPizVideo.Domain/BlogService.cs`: preserve article data/publication revisions;
  no embedded interaction arrays.
- `MorWalPiz.Contracts/Contracts/BlogContracts.cs`: existing article payloads remain compatible.
- `MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs` and `UserController.cs`,
  `frontend/back-office-spa/src/routes/rbac/UsersDetailPage.tsx`: preserve editorial
  mutations and staff identity workflows.
- BackOffice authorization/channel pipeline and `Services/Interfaces/IAuditService.cs`:
  reuse rather than duplicate or weaken.
- `frontend/morwalpizvideo.client/vite.config.ts`, `src/entry-server.tsx`,
  server/container hosting files: prove API/cache isolation and crawler HTML.
- `MorWalPizVideo.AppHost/Program.cs`, relevant .NET manifests and frontend package
  manifests: no new deployable, package dependency or API-to-API reference.
- `.github/workflows/deploy-backoffice-api.yml`, `deploy-back-office-spa.yml`,
  `deploy-freezer-analysis-api.yml`, `ci.yml`: gates and trigger paths must cover
  changed shared sources; deploy all account readers before scoped data is written.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | ServerAPI owns approved reader writes; BackOffice owns moderation; Domain shares invariants |
| Frontend/UI | Public account route repair plus incumbent article/admin refinement |
| Database/Data | Scoped account/session transition, per-channel uniqueness, all-registrant queries and three interaction collections |
| Contracts/Models | Account/session scope and account revision; safe admin DTOs; cached article remains unchanged |
| Authentication/Authorization | Trusted scope on every account/session operation, separate staff sessions and account/moderation leaves |
| Validation | Plain text/body limits, cursor bounds, revision/idempotency, ownership and publication checks |
| Cache | Existing content cache unchanged; interaction/account responses uncached and private where applicable |
| Configuration/Secrets | Validated interaction rate-limit/activation options; no new secrets or social credentials |
| Logging/Telemetry | Structured operation/result metrics, moderation audit and no body/email/token logging |
| Background Jobs | Not required for initial synchronous behavior; retention/notifications remain separately approved |
| Deployment/Operations | Scope-safe readers first, approved account mapping and index transition, then portal activation |
| Backward Compatibility | Anonymous articles/comments, staff RBAC and push preserved; explicit legacy namespace protects Freezer identities |

## Reuse Opportunities

- Existing account registration service, password hashing, sessions and cookie
  schemes, extended with required scope; no second password/authentication system.
- Existing BlogService for publication and configured-channel lookup behavior.
  Reuse/extract only the small channel resolver needed by new public controllers.
- Existing public/account/admin transport instances, URL composition, parsed API
  field errors and channel bootstrap lifecycle.
- Existing permission expansion, `AllowUser`, `RequireChannelScope`, protected data
  routes, page headers, React Bootstrap/Lucide and audit display components.
- Existing `react-share`, homepage control behavior, SEO helper and BlogRenderer.
- Existing Mongo index audit/apply service and scenario-backed mocks.
- Existing API factories/test auth, blog tests and shared transport-isolation tests.

## Risks

| Rank | Cause / affected surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| High | Unwired account routes and uncertain deployed CSRF/cross-site-cookie support prevent any interaction | Repair route flow; collect deployed bootstrap and real browser session evidence before activation | Direct account URLs render; register/login survives reload on canonical origin in supported browsers |
| High | Global account lookups/session validation cross portal boundaries | Trusted scope on every account operation, scoped uniqueness and explicit legacy namespace | Same username/email can belong to two channels; cookies/profile/logout/admin actions cannot cross scope |
| High | Staff/public identity conflation or unrestricted reader lookup leaks data/escalates access | Separate reader UI/API; query channel registration; account-view gates email; blog-only roles get minimal identity | All owned registrants visible; foreign-channel detail denied; no session/credential/RBAC disclosure |
| High | Authenticated block bypass or mixed public/private response leaks pending comments | Separate public approved endpoint and restricted private operations; explicit UI access state | Anonymous approved reads work, authenticated blocked writes fail, pending text never appears publicly |
| High | Guessing legacy ownership or old unscoped binaries corrupts migration | Owner-approved mapping, scope-aware reader rollout and write freeze before index replacement | Unmapped accounts do not appear in portals; legacy Freezer login/references work; migration conflicts stop visibly |
| High | Duplicate/concurrent reactions, submissions or moderation corrupt state | Unique indexes, request IDs, atomic revisions and explicit conflict handling | Parallel requests yield one reaction/submission and stale moderation fails |
| High | Abuse and unacceptable reader data retention | Premoderation, bounded submissions, withdrawal and legal/retention approval | Pending text never public; body never logged; retention and escalation owner signed off |
| Medium | Slug reuse, unpublication, deletion or cached HTML misroutes interactions | Stable post ID and live publication checks; retained unavailable marker | New post with old slug inherits nothing; unpublished/deleted interaction routes unavailable |
| Medium | Ban or unpublish overlaps an already authorized request | Explicit in-flight consistency boundary and access revalidation | Requests started after acknowledgment are denied; no promise of remote erasure |
| Medium | Account-store/database mismatch between API hosts | Verify both hosts read the intended store with the same channel ownership | A synthetic registrant appears in owned BackOffice before any interaction |
| Medium | Raw comment/display-name content produces unsafe UI | Plain text and escaped rendering; no HTML/link preview processing | Hostile text renders inert in both applications |
| Medium | Share preview absent on actual public host despite SSR tests | Inspect deployed crawler-visible canonical/OG HTML and absolute accessible cover URL | Preview fetch obtains article title/description/image without JS or account |
| Medium | Directory/search and interaction aggregation become expensive | Query channel-owned accounts with supporting indexes and batched summaries | No full collection reads or per-row summary query fanout |
| Medium | Concurrent profile/status changes undo suspension or edits | Revision-protected focused updates; atomic security-version changes | Stale admin/self-service changes fail; reactivation never restores revoked sessions |
| Low | Generic audit sink fails after moderation persistence | Persist latest actor/time/reason within changed document; reuse logged audit behavior | Mutation remains attributable; audit failure is logged and monitored |

## Technical Debt

- Account UI is currently disconnected from routing: blocks this feature; repair now.
- Account restoration silently hides failures: tightly coupled to permission UX;
  address now with explicit failed state and retry.
- Documentation conflicts with source and deployment hosting: correct touched
  statements, preserve unknown deployed facts, do not certify release from source.
- Existing public accounts are unscoped and indexed globally: blocks channel-owned
  registration; address now across every service/repository/auth consumer.
- Existing public-account profile update replaces the entire account with no revision.
  Scoped account management makes atomic revision-safe profile/status updates directly
  necessary. Keep comment restrictions separately persisted; no wider identity redesign.
- Existing BlogMockRepository owns an independent list rather than scenario state.
  Preserve existing blog fixtures; new interaction mocks use coherent scenario state.
  Host-to-host mock state is not automatically shared; the cross-host test needs an
  explicitly shared synthetic fixture or equivalent isolated store.
- Existing audit read methods filter through generic repositories and cap at 50;
  not suitable for "all reader interactions." Query interaction collections directly
  with real pagination. Reuse audit recording/display only.
- General cache reliability work, shop lifecycle and repository-wide cleanup remain
  out of scope. Do not make them prerequisites for uncached interaction endpoints.

## Implementation Roadmap

1. **Finalize release prerequisites.** Treat anonymous approved comments, channel-owned
   registration and the all-registrant directory as settled. Approve premoderation,
   retention/privacy and legacy-account ownership mapping; establish production
   account/canonical-host evidence. Owners: this handoff and architecture documents.
   Gate: no migration/identity decision delegated to implementation.
2. **Expand account schema and make all consumers scope-safe.** Add account/session
   scope, revisions, scope-aware service/repository operations and host-owned context;
   update ServerAPI, MvcHelpers, FreezerAnalysis callers and mocks. Owners: Models,
   Domain, both public hosts and their tests. Gate: legacy Freezer behavior plus
   two-channel account/session/profile/logout isolation, before live scoped writes.
3. **Transition account indexes/data and deliver portal account administration.**
   Apply the approved migration and uniqueness replacement, add direct scoped
   directory/profile/status APIs and account permission leaves. Owners: Mongo
   operations, BackOffice, Domain, Contracts and shared models/services.
   Depends on steps 1-2. Gate: retry-safe migration, all owned registrants visible,
   zero-activity detail and revision-safe suspension/reactivation.
4. **Complete existing public account UI.** Wire account routes/navigation and safe
   article return; expose restoration failure/retry and refresh revisions after
   profile updates. Owners: public route/account/header files. Depends on step 2.
   Gate: channel registration appears immediately in step 3's directory;
   login/profile/logout and anonymous blog regressions.
5. **Define interaction contracts and persistence.** Add three aggregates, focused
   repositories/DTOs/indexes, idempotency and deterministic fixtures. Owners: Models,
   Domain, Contracts, shared TS models. Depends on scoped account design in step 2.
   Gate: domain transition, duplicate, pagination and mock parity tests.
6. **Deliver ServerAPI interaction behavior.** Add anonymous approved reads and isolated
   account operations, live publication/channel checks, CSRF and limits. Owners:
   ServerAPI and Domain. Depends on steps 2 and 5.
   Gate: anonymous approved-only reads; blocked authenticated operations; actual host
   401/403/404/409/429 behavior; no draft/private-identity leaks.
7. **Deliver BackOffice moderation APIs and both UIs.** Add scoped article summaries/
   queue, interaction tabs in reader detail, revision-safe moderation/restrictions,
   audit, account profile/status controls and transport/route/menu permissions.
   Public UI adds sharing, reactions and anonymous versus account-aware comments.
   Owners: BackOffice, shared packages and both SPAs. Depends on steps 3-6.
   Gate: role/channel matrices, registration-to-admin workflow, block/unblock,
   deactivation/reactivation, keyboard/error/cancellation and private cache exclusion.
8. **Validate and document the shipped boundary.** Follow the test runbook, build
   shared packages in dependency order, perform targeted API/frontend checks, then
   real canonical-origin browser and visual evidence. Update this handoff/ADR and
   owning architecture sections with actual results. Gate: complete acceptance and
   explicit residual risks; stop before optional follow-ups.
9. **Activate after operational proof.** Use a single default-off host-owned blog
   interaction activation flag, surfaced through an explicit availability response;
   no frontend build-time flag is the authority. Both API hosts must honor it.
   Deploy scope-aware readers, account administration, approved indexes/data and
   management/backend support before public controls. The interaction flag does not
   permit unsafe legacy account behavior; account transition has its own maintenance
   window. Gate: moderation owner available, cookies working, no private caching,
   foreign-scope access, unresolved migration conflicts or duplicate identities.

An independent post-validation review is warranted because the change crosses
identity, tenant authorization, reader-generated content and concurrent persistence.
The user may select MorWalPiz Code Reviewer; the architect does not execute that review.

## Testing Strategy

- **Unit / Domain:** plain-text bounds (0/1/2000/2001), whitespace/Unicode behavior,
  moderation/withdrawal transitions, missing account/post, cursor validation,
  reaction set/switch/clear, revision conflicts and idempotent submission replay.
- **API behavior:** extend `MorWalPizVideo.BackOffice.Tests` using existing factories.
  Verify cookie scheme, absent/wrong CSRF, active/inactive/expired/revoked accounts,
  wrong channel/post, unpublished/deleted articles and explicit errors.
  Anonymous articles/counts and approved comments must work. Public pending/hidden/
  rejected content never appears. Block/unblock affects authenticated comments without
  changing reactions or staff login; channel-account suspension separately prevents
  login/interactions and invalidates existing sessions.
- **Authorization:** inventory new endpoint metadata; verify no class-level anonymous
  write bypass. Test view-only, moderation-only, restriction-only, umbrella and
  manage-all with owned/foreign channels. Add account-view/update/umbrella cases;
  detail requires registered channel ownership, not prior interaction. Account-view
  may expose owned email, while blog-only views redact it; session/credential/staff
  permission fields are always absent.
- **Account isolation/lifecycle:** same username/email can register independently in
  channels A and B; a duplicate within A fails. Unknown/foreign channel input cannot
  choose registration scope. Test session/profile/logout and administrative account
  updates across channels, public/admin profile races, stale revisions, suspension
  and reactivation without session revival. A newly registered account is listed
  before any article request and shows zero-activity detail.
- **Database/concurrency:** mock tests are insufficient for index/atomicity proof.
  Use an approved disposable Mongo environment for duplicate indexes, simultaneous
  first votes, competing switches, duplicate submission IDs, withdrawal replay,
  competing moderation/restriction/account revisions and scoped username/email
  uniqueness. Assert exact persisted cardinality and counts, not merely successful
  responses. Never use deployed personal data.
- **Component/route:** public direct account URLs and safe return path; restoration
  errors; anonymous/allowed/blocked states; own pending versus another user's pending;
  like/dislike pressed state; failed requests, conflict and `429`; withdrawal.
  Admin directory includes new/inactive/blocked users without interactions; search,
  filtering, pages, permission-specific contact/profile/status controls, no-activity
  detail and account revision conflict feedback. Cover counts, queue and block/unblock.
  Keep existing blog renderer/editor behavior unchanged.
- **Contract/transport:** validate DTO shapes, private-field absence, comment page
  continuation and count semantics. Test account requests include credentials/CSRF
  but no staff bearer/channel context; admin routes include the selected channel.
  Preserve public credentials omission and other app service behavior.
- **Security scenarios:** inert malicious text, forged account/channel/body identity,
  unsafe return destinations, blocked direct API access, credentialed disallowed
  origins, cache contamination and no sensitive logging. This is an implementation
  test plan, not a completed vulnerability audit.
- **Migration/rollout:** inspect mapping/index evidence, disabled-flag behavior and
  scope-aware reader deployment order. Test absent/null legacy ownership, legacy
  sessions, migrated portal accounts requiring sign-in, partial/replayed migration
  and conflicts. Verify FreezerAnalysis legacy registration/login/session and feature
  ownership references in `FreezerAnalysis.Api.Tests`; portal credentials are not
  accepted there. Old clients continue reading articles.
- **End-to-end/browser:** a synthetic reader registers from an article, restores
  session, and appears in BackOffice before interacting. An administrator edits the
  profile; the reader submits pending text and changes/clears a reaction. The admin
  sees it in article/reader views, approves/hides, blocks/unblocks comments and
  deactivates/reactivates the account. Anonymous approved viewing still works after
  logout; pending text does not. Include cross-tab focus refresh, account switching,
  expired sessions and channel changes.
  Test actual cross-site cookie behavior on supported desktop/mobile browsers.
- **Sharing/SSR/accessibility:** native supported/unsupported/cancel/failure and
  clipboard failure; correct canonical URL, deployed OG HTML, accessible image,
  narrow/desktop layout, keyboard/focus/contrast and screen-reader announcements.

## Migration And Rollback

Channel-owned registration now requires a Mongo account/session compatibility
transition. Article documents and SQLite remain unchanged; interactions/restrictions
are new collections. Do not migrate readers into staff users, transfer ownership
through public/admin profile APIs or expose account credential material.

### Expand and establish scope-safe readers

First deploy schema-tolerant, scope-aware account/session readers and writers to
ServerAPI, MvcHelpers consumers, FreezerAnalysis and BackOffice. Legacy missing/null
scope stays explicitly non-portal. Verify that every account consumer is updated;
older unscoped binaries must not run against newly scoped identities.
Keep existing uniqueness indexes while expanding and do not activate portal
registration yet. Default legacy revision values must be handled explicitly by
atomic first-update filters, not silently accepted as unrestricted writes.

### Approve ownership and migrate

Inventory existing account/session counts and application references in a protected
operator context. An approved per-account/per-channel mapping is required, with
ambiguous/unmapped rows reported and left unchanged. No automatic attribution from
the current public host, email or display name.

Recommended preservation path: retain the original legacy identity and references
for FreezerAnalysis; create a new channel-owned identity for each approved portal
registration using a new account ID. Preserve legal evidence and original creation
time, and record migration provenance separately from an asserted new acceptance.
Credential continuity, if approved, is handled only inside the protected migration,
never by exporting hashes to clients/docs/logs. No live synchronization of passwords
or profiles across the independent identities is introduced.

An idempotent migration mapping must bind source account and destination channel
to the created destination account ID. Replays must verify the existing destination,
not create duplicates or overwrite later profile/status changes. Conflicting
username/email or ownership evidence stops that row with explicit operator reporting.
Do not clone all Freezer accounts into portals. New portal accounts sign in again;
old public sessions bound to legacy identities do not authenticate the portal.
Freeze affected account writes for the bounded snapshot/index/mapping transition
so password/profile changes cannot silently race it.

Required indexes: unique channel/post/account reaction; unique channel/account/
submission-request comment; comment pages by channel/post/state/creation/ID and
channel/account/creation/ID; channel/state/creation/ID moderation queue; reaction
reader-history index; unique channel/account restriction. Verify actual serialized
field names. Audit/apply with approved Mongo operations, not request-time index
creation. Add channel/registration-time/ID directory indexes and indexes matching
bounded account search/status queries. Retain the session TTL.

Create replacement per-channel username/email unique indexes plus distinct
missing/null-legacy username/email uniqueness. Validate partial-index eligibility
against actual stored types; reject empty/invalid scope rather than treating it as
legacy. Audit duplicates first, prove replacement definitions with disposable Mongo
tests, and use the existing approved index apply/removal workflow.
Only after scope-safe hosts and replacement indexes are proven may the old global
username/email indexes be removed in the maintenance window. Their removal is an
explicitly approved compatibility contraction; do not perform it as request-time
startup or migrate blindly. Apply portal copies after global uniqueness no longer
prevents the approved per-channel registrations, with all affected writes frozen.

Deploy shared account support and all consuming hosts before scoped data, then
approved indexes/mapping, BackOffice account administration and the public account
UI; deliver/activate article interactions after those prerequisites.
Activate only after account, moderation and canonical-host gates pass. Backend
availability is authoritative during mixed frontend versions.
Observe auth/CSRF failures, forbidden comments, `429`, conflicts, queue backlog,
Mongo latency and audit failure metrics without logging content or tokens.

Rollback first disables interactions/portal registration or uses the approved account
maintenance mode as appropriate, and removes controls. Retain collections, scoped
indexes, destination mappings, accounts and restrictions. Do not delete registrations
or change their channel to make an old binary work.

Before scoped data/index contraction, an ordinary artifact rollback is possible.
Afterward, rollback must stay on scope-aware binaries. Reinstating old global unique
indexes may be impossible once two channels use the same username/email; restoring
them requires a separately approved conflict/recovery plan, not data deletion.
Retained legacy identities protect existing Freezer feature references, but new
portal identities are not automatically downgraded to that namespace.
Never reopen an older comment endpoint lacking restriction enforcement.
Approved comments remain public by design; blocking controls authenticated use,
not anonymous viewing or remotely erasing HTML.

Retention duration, rejected/hidden body handling, privacy-request recovery and
permanent deletion must be approved before live data collection. Withdrawal's
tombstone/request-ID retention must preserve idempotency for the approved window.
No automatic TTL deletion of comments/reactions is proposed without that decision.
No cache migration, new cookie scheme, social credential rollout or Hangfire job is
needed. Scoped session validation and reauthentication are nevertheless required.

## Open Questions

Anonymous approved viewing, selected-channel ownership (including registration),
and the directory of every channel registrant are settled and must not be reopened.

1. Which existing legacy accounts have verified registrations to which portals?
   Approve the preservation/copy mapping and credential-continuity decision before
   migration; current records do not contain channel or registration-host evidence.
2. Confirm pending-first moderation and author withdrawal. Who owns queue review,
   abuse escalation, retention periods and privacy requests?
3. Confirm the proposed profile editing/deactivation/reactivation management scope.
   Password recovery and permanent account deletion need separately approved flows;
   they are not implied by a generic staff password/delete button.
4. Does "all user interaction" mean comments and current reactions, or an immutable
   timeline of every reaction change? Completed shares cannot be reliably observed;
   an event timeline would be additional persistence/retention scope.
5. Which deployed artifact serves the canonical domain, and do current supported
   browsers successfully retain the public account cookie? If third-party cookies
   fail, an API custom-domain/proxy change requires a separate approved deployment
   decision before activation.
6. Are email verification/password recovery required before launch, or accepted as
   a separately scheduled account-lifecycle slice?
