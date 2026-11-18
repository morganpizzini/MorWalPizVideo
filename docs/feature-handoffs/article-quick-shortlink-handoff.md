# Article quick short link handoff

Status: implemented in the worktree; deployment gates remain. Source reviewed
2026-11-18.

## Outcome

Add a quick action beside **Indietro** after the article's first save, including
drafts. It creates a short link owned by the article's channel; show the link
and a copy control immediately and when the article is reopened.

Decisions from the user: persistence is required, the link uses the same channel
ID as the article, and the action is available after first save. Recommended
behavior: one reusable link per article; creation never saves or publishes the
article. A link for a draft is visible to the editor but does not resolve
publicly until the article is published.

## Current Evidence

- [Blog form](../../frontend/back-office-spa/src/routes/blog/form.tsx) owns the
  route action, article state and header. The tagged
  [Editor.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.tsx) is
  only the block composition editor. [PageHeader](../../frontend/back-office-spa/src/components/PageHeader/index.tsx)
  has an `actions` slot immediately before **Indietro**.
- [BlogService](../../MorWalPizVideo.Domain/BlogService.cs) reads articles by
  channel and ID; a never-published draft's slug is editable. Once published,
  the slug is locked even after unpublication. Its writes use revision-based
  compare-and-swap and distinguish editorial saves from other mutations.
- [BlogPostResponse](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs) and
  [BlogPostAdmin](../../frontend/fe-packages/models/src/blog.ts) have no
  short-link association. Public reads in
  [BlogController](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs)
  serve published records from one configured channel. The public route is
  [blog/:slug](../../frontend/morwalpizvideo.client/src/routes/config.tsx).
- [ShortLink](../../MorWalPizVideo.Models/Models/ShortLink.cs) is a standalone
  canonical aggregate with typed targets, management channel and globally
  normalized code. `AskCampaign` already illustrates an ID-based typed target:
  [LinksService.EnsureAskShortLinkAsync](../../MorWalPizVideo.Domain/FeatureServices.cs)
  creates it and the
  [redirect controller](../../MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs)
  resolves it dynamically. The canonical code index is an explicit operations
  responsibility, not automatically guaranteed at startup.
- Generic [legacy short-link creation](../../MorWalPizVideo.BackOffice/Controllers/ShortLinksController.cs)
  requires `shortlinks.create`/`shortlinks.manage` and administrator status for
  non-YouTube links. It returns an `Ok` string, while
  [shared transport](../../frontend/fe-packages/services/src/apiTransport.ts)
  parses successful responses as JSON. The new article-specific response
  should instead be an explicit JSON object.
- A channel has `PublicBaseUrl`. Existing
  [QuickLinks detail](../../frontend/back-office-spa/src/routes/quickLinks/detail/Component.tsx)
  validates that origin, but neither a channel ID nor the origin proves that the
  public site's configured ServerAPI resolves the same channel.

## Delivered Implementation

### Canonical association and redirect

Append an `Article` link type without renumbering existing values, and add an
optional article ID to the standalone short-link record. Store the article's
stable ID, channel ID and management channel ID; the association lives only in
the canonical short-link collection, not in the blog document. Existing
documents lack the new field and remain readable.

Use the existing ASK ensure pattern, with a deterministic normalized code
derived from the article ID and a dedicated prefix. Require the existing
globally unique code index before activation. Concurrent create attempts insert
one record; duplicate-key recovery must reload the canonical code and verify
its type, article ID and channel before returning it. A collision with any
different record is an explicit conflict, never a successful fallback. The mock
repository must provide equivalent atomic behavior. No generic URL matching,
automatic adoption of old arbitrary links, or second article association store.

On redirect, look up the article by stored channel and ID using the blog
repository, then require a published snapshot. Resolve that channel's validated
HTTP(S) public origin and the article's current persisted slug. Return 404 for
draft, unpublished, deleted, foreign/missing article or invalid/missing
channel origin; do not expose draft content or redirect to a stale slug. Use
the existing counting/classification path only after resolution succeeds.
Preserve the same code across draft slug changes, publication and unpublication.
The host must be configured so that the resolved public site actually serves
the article's channel; see rollout gate below.

Block generic creation or conversion into this typed link through both legacy
and v1 generic APIs, and block generic update/deletion of article-owned links
while the association exists. This prevents accidental retargeting or removal
behind the editor. If the article is deleted, retain its canonical link as an
inactive record returning 404; cleanup/reuse is outside this change. Preserve
unrelated existing short-link types and their behavior.

### Management and UI

Add an article-specific read and idempotent ensure operation to BackOffice,
scoped by the selected channel and article ID. The server loads the article
using that channel, ignores client-supplied target/channel ownership, and
returns a typed JSON object with the short URL/code and publication status.
Require blog read permission for the read; require blog update/manage **and**
the existing short-link create/manage permission plus the existing non-YouTube
administrator policy for ensure. Enforce all checks server-side. UI permission
checks are only hints: `IsAdminAsync` also recognizes the admin group, so a
403 must be displayed rather than guessed away.

The blog form uses the read operation on an existing article and an independent
fetcher for ensure; it never creates automatically on load. Hide the action on
the unsaved create route and show it after a successful first save creates the
post. If no link exists, show create; if it exists, show the URL and accessible
copy control, including after reload. Explain that a draft/unpublished link
becomes public only on publication. Preserve unsaved form state and save,
upload and publish fetcher behavior; suppress stale results after channel/post
switch, avoid double-click submissions, and expose errors and clipboard failure.
The URL remains selectable when clipboard access fails.

Build the displayed URL from the existing configured short-link base and the
persisted code on the server, validating the configured base as an absolute
HTTP(S) URL. Report invalid configuration rather than inventing a host. Do not
emit success before the canonical record can be read back; an ambiguous network
failure is resolved by a subsequent read or idempotent retry.

Visual direction: existing BackOffice **Operate** refinement, not redesign.
Use Impeccable `harden` narrowly during implementation. No BackOffice-local
`PRODUCT.md` or `DESIGN.md` was found. Verify desktop and narrow header
overflow, keyboard/focus, copy status and failure with real browser evidence.

## Impacted Projects

- `MorWalPizVideo.Models`, `Domain`, `Contracts`: typed association, repository
  operations, safe ensure and admin response contracts.
- `MorWalPizVideo.BackOffice`: channel-scoped article link operations, existing
  authorization, audit, cache and generic-management protections.
- `MorWalPizVideo.ShortLinks`: article resolution and blog-repository DI in
  production and mock branches.
- `frontend/fe-packages/models`, `services`, `back-office-spa`: typed contract,
  shared transport, article header UI and tests. Verify shared-package consumers.
- `MorWalPizVideo.BackOffice.Tests`, `MorWalPizVideo.ShortLinks.Tests`: API,
  repository/concurrency, authorization and redirect scenarios.
- Public frontend and ServerAPI: verify-only public landing behavior.
  AppHost and deployments: verify rollout order and configuration; no topology
  change. Other workspaces and standalone tools are unaffected.

## Impacted Files

### Modify

- [ShortLink.cs](../../MorWalPizVideo.Models/Models/ShortLink.cs): append type
  and additive article reference.
- [FeatureServices.cs](../../MorWalPizVideo.Domain/FeatureServices.cs),
  [IRepository.cs](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs),
  [Repository.cs](../../MorWalPizVideo.Domain/Interfaces/Repository.cs) and
  [MockRepository.cs](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs):
  deterministic, atomic, collision-safe ensure/read and ownership protection.
- [BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs):
  article-scoped read/ensure and authorization.
- [ShortLinksController.cs](../../MorWalPizVideo.BackOffice/Controllers/ShortLinksController.cs)
  and [V1ShortLinksController.cs](../../MorWalPizVideo.BackOffice/Controllers/V1ShortLinksController.cs):
  prevent generic creation/conversion and destructive modification of article links.
- [ShortLinkController.cs](../../MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs)
  and [Program.cs](../../MorWalPizVideo.ShortLinks/Program.cs): published-only,
  channel-bound dynamic resolution and repository registration. Existing
  `BlogMockRepository` is in-memory per host, so separate mock applications do
  not automatically share blog data; use explicitly seeded/shared test fixtures
  and do not claim a cross-host mock works without proving its data path.
- [BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs),
  [blog.ts](../../frontend/fe-packages/models/src/blog.ts) and
  [blogService.ts](../../frontend/fe-packages/services/src/blogService.ts):
  additive article-short-link JSON contract and transport.
- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx) and
  [form.test.tsx](../../frontend/back-office-spa/src/routes/blog/form.test.tsx):
  create/read/copy and state/permission/error coverage.
- Existing focused tests in
  [BackOffice.Tests](../../MorWalPizVideo.BackOffice.Tests/) and
  [ShortLinks.Tests](../../MorWalPizVideo.ShortLinks.Tests/): endpoint,
  concurrency, compatibility and redirect behavior.

### Add

- Focused tests or a feature-local service only if existing test/service files
  cannot reasonably contain the behavior; follow local naming conventions.

### Generated/Migration

- No bulk backfill. Old articles have no association until explicitly requested.
  The existing global unique normalized-code index is a prerequisite; verify
  operationally and audit duplicates before rollout.

### Documentation

- Update [blog authoring](../features/blog-authoring.md),
  [structured blog](../architecture/structured-blog.md),
  [ADR-004](../architecture/adr/ADR-004-canonical-short-links.md), and this
  handoff with delivered behavior, evidence and residual risks.

### Verify Only

- [PageHeader](../../frontend/back-office-spa/src/components/PageHeader/index.tsx),
  [BlogService](../../MorWalPizVideo.Domain/BlogService.cs),
  [public blog controller](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs),
  public route, channel origin validation, short-link index operations and
  [deployment guidance](../architecture/deployment.md).

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Add scoped article read/ensure; redirect dynamically resolves published article. |
| Frontend/UI | Saved-post action, persisted URL and copy feedback; composition unchanged. |
| Database/Data | Add optional reference/type to canonical short links; no blog document mutation or backfill. |
| Contracts/Models | Append enum value and additive fields/typed response; preserve numeric values and older records. |
| Authentication/Authorization | Existing cookie, CSRF, channel, blog permissions, short-link permissions and admin restriction all remain authoritative. |
| Validation | Reject foreign article, collisions, invalid origin, malformed persisted records and untrusted target input. |
| Cache | Reuse existing short-link creation eviction; dynamic article state must not be cached in a way that serves stale slug/publication. No blog mutation. |
| Configuration/Secrets | Channel public origin and short-link base must be valid; no new secret. Origin/channel deployment mapping is a release gate. |
| Logging/Telemetry | Preserve canonical create audit, click counting and classified event recording; log resolution/config failures without exposing drafts. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Confirm unique index; deploy redirect support before permitting new link creation; verify channel/public mapping. |
| Backward Compatibility | Older records remain valid; old redirect instances cannot safely handle the new type. Coordinated deployment and rollback limits apply. |

## Reuse Opportunities

Reuse `PageHeader.actions`, blog `GetAsync`, the selected channel context,
existing permission resolver, `ILinksService`, canonical short-link repository,
ASK dynamic redirect/ensure patterns, cache eviction and audit, shared admin
transport and existing test fixtures. Reuse channel-origin validation behavior,
but enforce it on the server that constructs the redirect.

## Risks

1. **High — incorrect destination:** origin may serve a different configured
   public channel, despite matching article ownership. Verify site-to-channel
   mapping and smoke-test the published article for every enabled channel.
2. **High — unsupported redirect deployment:** older redirect nodes can fall
   into the YouTube default for a new enum. Deploy and drain redirect nodes
   before any article link can be created; test mixed-version sequencing.
3. **High — duplicate/colliding creation:** concurrent requests and response loss
   can otherwise produce inconsistent records. Require the unique code index,
   atomic ensure and identity-checked duplicate recovery; test concurrent calls
   and foreign-record collisions in Mongo and mock behavior.
4. **Medium — mutable drafts and publication:** static URL links would become
   stale or reveal draft paths. Resolve by ID and channel at request time and
   return 404 until published; test slug changes, unpublish and deletion.
5. **Medium — permission or channel leakage:** require server-side joint
   permission checks and channel-scoped article lookup; test non-admin,
   insufficient permission and foreign IDs.
6. **Medium — generic mutation:** legacy management can change targets or delete
   links. Reject type conversion, retargeting and deletion through every generic
   mutation entry point; test both existing and new types.
7. **Low — clipboard restrictions:** keep URL selectable and display copy
   failure; test unavailable and rejected clipboard APIs.

## Technical Debt

- The generic short-link service has legacy success-shaped collision paths;
  isolate the new ensure operation from them rather than expanding cleanup.
- One configured public blog channel per ServerAPI is a deployment constraint;
  the article channel ID alone does not establish a working public destination.
- Deleted articles leave inactive short-link records; lifecycle cleanup is
  deferred and must not be silently added to article deletion.

## Original Implementation Roadmap

1. Establish unique-code index and public site/channel configuration evidence;
   review existing type consumers and generic short-link mutation paths.
2. Add additive type/reference and typed response. Implement atomic ensure with
   deterministic code, identity-checked duplicate recovery and mock parity.
   Prove existing short-link behavior and Mongo concurrency.
3. Implement scoped BackOffice read/ensure, combined authorization, audit,
   error handling and generic endpoint/service protection. Verify ID/channel
   ownership, validated returned URL and no article revision changes.
4. Add published-only dynamic redirect and register blog repositories in both
   ShortLinks DI branches. Verify current slug/origin, analytics and failures.
5. Add shared model/service and independent form fetcher/UI. Gate on saved post;
   read existing link on reopen; preserve draft edits, report errors and copy.
6. Run focused backend builds/tests serially, shared-package checks and focused
   SPA tests; browser-check narrow/desktop, copy, reopen and real redirects.
   Update documentation and this handoff. Stop at this feature boundary.

## Testing Strategy

- **Unit/repository:** deterministic idempotent ensure; concurrent calls;
  duplicate code owned by a different article; legacy documents; invalid
  channel/origin; mock parity and Mongo unique-index behavior.
- **BackOffice integration/security:** saved draft creation, repeat/read/reopen,
  lost-response retry, foreign channel/article, missing article, non-admin,
  missing either permission, CSRF and generic create/update/delete bypasses.
  Assert no editorial or aggregate article revision changes.
- **Redirect integration:** draft 404, publication succeeds, changed draft slug
  before publication routes to new slug, unpublish/deletion returns 404, republish
  restores same URL; foreign ownership/missing origin fail; no click counted
  on failed resolution and existing link types remain unchanged.
- **SPA/component/contract:** new article hides action; saved draft and published
  article show it; reload restores link; no implicit save or publish; pending,
  channel switch, errors and clipboard success/failure; JSON object shape and
  error propagation.
- **End-to-end:** verify actual channel public site after publish, click the
  branded link, copy and reopen, at desktop and narrow widths. Test missing
  index and staged rollout operational gates.
- Independent post-validation review is warranted: this adds a redirect type,
  authorization surface and persistent cross-project contract.

## Migration And Rollback

No Mongo backfill: absent article reference is tolerated; existing records keep
their types. Before release, audit/apply the global normalized-code unique index.
Deploy and drain all ShortLinks instances supporting the new type, then deploy
BackOffice, then the SPA. Confirm the public origin serves the same article
channel and verify drafts return 404 and published links resolve correctly.

SPA rollback leaves links and redirects intact. BackOffice rollback removes
creation/read UI support but existing links must continue working. Do not roll
back the redirect service to a version unaware of the new type while article
links exist; retain a compatible redirect or explicitly disable/migrate those
links with an approved recovery plan. Do not delete canonical links as rollback.
No auth, secret or blog-data migration is introduced.

## Open Questions

No product questions remain from the user's answers. Operational verification
remains mandatory: channel `PublicBaseUrl` and the public site's configured
ServerAPI must correspond to the same channel; a channel ID alone cannot prove
this. If that is false for a target channel, do not enable the action there
until an authoritative routing solution is approved.

## Implementation Record

Delivered behavior:

- Added the `Article` short-link type and optional article ID on the canonical
  record. `LinksService` creates a deterministic, idempotent code and validates
  article/channel ownership on reuse. Generic create/update/delete paths reject
  article-owned links.
- BackOffice exposes `POST /api/blogposts/{id}/short-link`, requires both
  article update/manage and short-link create/manage permissions plus the
  existing administrator check, audits new records, and adds nullable
  `articleShortLink` metadata to article reads.
- ShortLinks resolves the stored article ID and channel at request time. Draft,
  unpublished, deleted, mismatched-channel, and invalid-origin cases do not
  redirect or count a click. A published article redirects to its current slug.
- The SPA offers the action after first save, retains the URL on reopen, displays
  draft-inactive status, and provides copy feedback/failure messaging without
  saving or publishing article content.
- Updated the blog authoring, structured-blog, and canonical-short-link ADR docs.

Validation completed:

- BackOffice/Domain Release build succeeded; focused BackOffice API and
  `LinksServiceTests` passed **33/33**. Existing ImageSharp vulnerability
  advisories and pre-existing nullable warnings appeared during build.
- ShortLinks Release build succeeded; complete ShortLinks test project passed
  **17/17**, including draft 404, current-slug redirect, unpublish 404, and
  success-only click counting.
- BackOffice SPA TypeScript/Vite production build succeeded; targeted blog form
  tests passed **34/34**. Existing large-chunk warning remains.

Remaining release gates: verify and apply the globally unique normalized-code
Mongo index; set valid `SiteUrl` and article channel `PublicBaseUrl`; ensure the
ShortLinks deployment's `YouTubeChannelId` matches; deploy redirect support
before enabling the BackOffice action. No browser visual capture or production
configuration/index verification was performed. No schema backfill is required.
