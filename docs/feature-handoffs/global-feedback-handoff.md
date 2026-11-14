# Global bug and feedback reporting handoff

Status: Implementation complete; production activation remains gated by operational review.
Source inspection: 2026-11-14.

## Outcome

Deliver an integrated, always-available feedback modal with **one editable field:
description**. Automatically attach a small, privacy-safe reproduction context,
including the reporting page and server-verified logged-in identity when available.
Submit to **MorWalPizVideo.ServerAPI**; list, inspect, triage, and delete reports
through **MorWalPizVideo.BackOffice** and its SPA.

Feedback is a **global platform capability**, not a Workshop domain aggregate and
not channel-owned. There must be no channel ID in its entity, DTOs, repository
filters, permissions, endpoint requirements, or frontend request context.

Explicit assumptions for review:

- The tagged Workshop entry point identifies the first reporting application.
  "All pages" means every Workshop page, including login, session restoration,
  empty/error states, and nested detail pages. This does not silently authorize
  rollout to every application in the monorepo.
- "Feedbacks like and manage" means **list and manage**, not public likes/voting.
- Anonymous reporting is supported so login/session problems can be reported.
  An active public-account session is linked automatically; login is not required.
- Initial management consists of a paginated inbox, detail, status changes, and
  deletion. No assignments, replies, public report listing, attachments,
  screenshots, recording/replay SDK, automatic issue creation, or notifications.

Completion requires the real reporting-to-management path, not just a rendered
modal or mocked successful submission. Reproduction metadata improves diagnosis;
it cannot guarantee reproduction without the user's description or server evidence.

## Current Evidence

Verified in current source:

| Evidence | Implication |
| --- | --- |
| [Workshop main](../../frontend/shooter-workshop.client/src/main.tsx) nests RouterProvider inside AuthProvider; [route config](../../frontend/shooter-workshop.client/src/routes/config.tsx) places login outside AppShell. | AppShell alone cannot provide all-page reporting. Mount a persistent reporting host above route shells. |
| [AppShell](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx) gates operational pages on public-account restoration; [AuthProvider](../../frontend/shooter-workshop.client/src/auth/AuthProvider.tsx) restores the session through shared account services. | Reuse that session for display, not as the identity authority for submitted reports. Feedback must not wait for successful account restoration. |
| [Workshop page implementations](../../frontend/shooter-workshop.client/src/routes/pages.tsx) own component-level forms, data loading, and error states; [States](../../frontend/shooter-workshop.client/src/components/States.tsx) is the local state-component seam. | Add contextual report affordances without extracting Workshop product behavior into BackOffice or duplicating the modal per page. |
| [Workshop styles](../../frontend/shooter-workshop.client/src/main.scss) define dark surfaces, yellow accent, buttons, focus rings, and responsive breakpoints. | Refine the incumbent UI rather than redesigning it. |
| [PublicAccountController](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs) provides anonymous CSRF acquisition/session restoration and account mutations; [PublicAccount authentication](../../MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs) owns the dedicated cookie scheme and validates sessions server-side. | Support optional identity using this scheme, not ServerAPI's default JWT/development principal or caller-supplied user IDs. |
| [ServerAPI composition](../../MorWalPizVideo.ServerAPI/Program.cs) registers exact credentialed public-account origins, rate limiting, public-account services, and Mongo/mock branches. | Reuse its policy and infrastructure; introduce an explicit anonymous submission exception, not a broader fallback-authentication change. |
| [Shared transport](../../frontend/fe-packages/services/src/apiTransport.ts) has isolated account/admin modes, account CSRF acquisition, and an explicit list of channel-scoped BackOffice URL prefixes. | Account submission and admin management need distinct helpers; feedback must stay outside the channel-prefix list. |
| [PublicAccountsController](../../MorWalPizVideo.BackOffice/Controllers/PublicAccountsController.cs) is an existing global administrative read protected by AllowUser without RequireChannelScope. | Follow this ownership/auth boundary rather than a channel-scoped content controller. |
| [Account repository contract](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs), [Mongo implementation](../../MorWalPizVideo.Domain/Interfaces/Repository.cs), and [mock implementation](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs) already serve both hosts. | Both APIs can access the same Domain-owned global store without referencing or calling one another. |
| [Permission keys](../../MorWalPizVideo.Models/Constraints/AuthorizationKeys.cs), [permission expansion](../../MorWalPizVideo.Domain/Security/AuthorizationPermissionExpander.cs), and [SPA permissions](../../frontend/back-office-spa/src/authorization/permissions.ts) define server-expanded capability checks and the global manage-all bypass. | Add explicit feedback capabilities; staff shell access alone is insufficient. |
| [BackOffice router](../../frontend/back-office-spa/src/router.ts) hydrates session, feature flags, and accessible channels; [active lazy route manifest](../../frontend/back-office-spa/src/router/routes/lazy-index.ts) owns protected feature registration. | Add routes to the active lazy manifest, not the unused legacy definitions in the compatibility route index. Zero accessible channels must not block feedback management. |
| [Admin menu](../../frontend/back-office-spa/src/layouts/adminMenu.ts), [sidebar](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx), and [PrimaryLayout](../../frontend/back-office-spa/src/layouts/PrimaryLayout.tsx) own management navigation and shell. | Integrate the inbox with permission-filtered platform navigation; do not make channel selection a prerequisite. |
| [Mongo operational manifest](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs) supports unique and TTL indexes and reviewed audit/apply. | Use the established index operation path; no EF migration or new startup index subsystem is needed. |
| [Workshop manifest](../../frontend/shooter-workshop.client/package.json) has React, Bootstrap styles, and Vitest, but no React Bootstrap, Testing Library, or jsdom; [Vitest config](../../frontend/shooter-workshop.client/vitest.config.ts) currently uses node. | Do not assume BackOffice component/testing dependencies are available to Workshop. |
| [Public-account endpoint tests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs), [ServerAPI factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/ServerApiWebApplicationFactory.cs), and [BackOffice factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs) provide the existing API test seams. | Reuse the mixed-host test project; no new backend test project. |
| [ServerAPI project](../../MorWalPizVideo.ServerAPI/MorWalPizVideo.ServerAPI.csproj), [BackOffice project](../../MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj), and [test project](../../MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj) target .NET 10 and reference the established shared projects. | Shared DTOs belong in MorWalPiz.Contracts, not a newly invented contracts project or an API-to-API reference. |

The initial implementation now includes dedicated feedback controllers in both
APIs; the post-implementation review below supersedes the earlier absence finding.
The [architecture guide](../architecture/README.md),
[frontend guide](../architecture/frontend.md),
[security guide](../architecture/security.md),
[Workshop baseline](../architecture/shooter-workshop.md), and
[accounts feature record](../features/accounts-and-channels.md) were consulted.
Documentation-only production gates were not treated as fresh deployment proof.

### Post-implementation review findings

The implementation preserves the intended global ownership boundary: no feedback
entity, DTO, repository, route, or transport is channel-owned, and account-mode
submission strips authorization/channel headers. The following defects must be
resolved before production activation:

1. **High — admin transport errors are success-shaped.** The four management
   helpers cast admin transport envelopes without `requireSuccessfulResponse`.
   A failed list can crash rendering, a failed status update can replace the
   report with an error envelope, and a failed delete can still navigate away.
2. **High — idempotency is incomplete.** Replay equality compares only description
   and reporter account. Reusing a request ID with changed page, component,
   application, release, or browser context is incorrectly accepted.
3. **High — expired reports remain directly readable/mutable.** List/count hide
   expiry, but detail/status/delete do not enforce it before Mongo TTL deletion.
4. **High — mock uniqueness does not model Mongo uniqueness.** Concurrent mock
   submissions can create duplicate request IDs, preventing the normal API test
   infrastructure from proving the production invariant.
5. **Medium — privacy/audit policy is incomplete.** Neither controller marks
   responses no-store, and status changes do not persist the effective staff actor.
6. **Medium — receipt validation is incomplete.** A receipt missing required
   `receivedAtUtc` is accepted.
7. **Medium — Workshop modal hardening is incomplete.** Background inertness,
   robust focus recovery, assertive errors, retained drafts, stale-success-timer
   cancellation, error differentiation, and the serialized 16 KiB budget are not
   implemented or DOM-tested.
8. **Medium — BackOffice management is below the established route standard.**
   It lacks application filtering, pagination controls, loader/action ownership,
   mutation pending states, retryable in-page failures, and safe failed-delete
   behavior.
9. **Low/operational — query indexes and limiter topology are incomplete.**
   Status/application compound indexes are absent; trusted proxy and effective
   multi-replica rate limiting require deployment evidence.

### BackOffice SPA failure classification

The historical result was 14 failures. The feature introduced one navigation
catalog count failure, which is already corrected by the current 34-item
assertion. The archived JUnit report from that earlier run contained **416 tests
and 13 failures**; all 13 were outside feedback code and have since been repaired:

| Failures | Existing suites | Evidence-based cause and required preservation |
| ---: | --- | --- |
| 3 | `src/router/asyncData.test.tsx` | The memory router remains at its hydration boundary. Initialize/settle the router explicitly while preserving resolved, pending, and error assertions. |
| 1 | `src/components/ToastNotification/index.test.tsx` | Real navigation remains on the explicit loading fallback. Fix the route test helper; retain the navigation/toast assertion. |
| 9 | navigation, insights/news, pages/form, sponsors/form, and videos/translate action tests | `request.formData()` receives incompatible Request/FormData implementations under jsdom. Introduce one shared test request/form-data fixture and keep all business assertions unchanged. |

Do not lower expected route/menu counts, remove hydration assertions, skip tests,
or convert rejected/error envelopes into successful defaults. The exact upstream
Node/jsdom/undici regression is not proven statically; implementation must
diagnose the active runtime before choosing the narrow fixture correction.

## Proposed Design

### Ownership and flow

1. A persistent Workshop feedback provider/host opens one modal from a global
   launcher or a contextual component action.
2. The client freezes the reporting page/component context **when opened**, not
   after navigation or when a retry happens.
3. A shared feedback submission helper uses the existing **account-mode client**
   to send the description and typed context to ServerAPI.
4. ServerAPI validates CSRF, size, metadata, optional public-account identity, and
   rate limits; a Domain-owned Feedback service persists one report.
5. BackOffice exposes only protected management operations over the same Domain
   repository and Mongo collection.
6. The BackOffice SPA uses the **admin-mode client**, guarded route loaders/actions,
   and existing management components.

There is no ServerAPI-to-BackOffice HTTP hop, new external integration, channel
resolver, Workshop ledger write, or public feedback-read endpoint.

### Modal and component context

- Mount one provider and feedback host inside AuthProvider but outside
  RouterProvider at the application bootstrap. The host must not require router
  hooks or a successful AppShell render. Read browser location only when opening.
  Route-level errors must leave the launcher usable.
- Use an unobtrusive, labelled "Report an issue / Send feedback" launcher at a
  consistent viewport position, with safe-area spacing and no overlap with
  page actions. It must remain reachable on login, narrow screens, long pages,
  loading states, and failed API screens.
- Modal content: title, a labelled description textarea, short privacy/context
  explanation, read-only page/component summary, Cancel, and Send.
  No required category, email, severity, title, or reproduction-step field.
- Provide one app-local open-feedback hook accepting a stable optional component
  key. Connect it to the common error state and contextual actions for named
  operational panels/forms, including the failing-component case. A general
  launcher supplies page context only; it must not pretend to identify the
  precise broken component automatically.
- Component keys are source-defined identifiers such as a form/panel role, not
  DOM text, CSS selectors, field contents, resource names, or React internals.
  The description can explain an issue in an otherwise uninstrumented component.
- Use native dialog behavior with the application's existing styles rather than
  importing a new UI framework into Workshop. Verify focus containment, initial
  focus, Escape/Cancel, focus restoration, background inertness, scroll handling,
  stacked-overlay behavior, and the supported browser matrix.
- Keep the draft in memory while closed during this application lifetime; do not
  persist descriptions/context in localStorage, IndexedDB, URLs, or analytics.
  Successful submission clears it. Signing in/out clears identity-sensitive
  drafts. Never overwrite another page's frozen context silently.
- While sending, prevent duplicate sends and accidental dismissal. Failure keeps
  the description/context and exposes an actionable error. Navigation must not
  silently recapture a different page; starting a fresh report resets context.
- Distinguish invalid description, expired CSRF, throttling, offline/network,
  and server failure. A missing/malformed receipt is not success. No background
  offline queue or automatic unbounded resubmission.
- The always-available guarantee covers a functioning application runtime, not a
  failed script download, total browser crash, or broken root React runtime.

### Reproduction context and trust

Use explicit typed fields, not an arbitrary JSON metadata bag.

| Field | Source and handling |
| --- | --- |
| Description | Only editable field; trim, require 1-4000 characters; render as plain text everywhere. |
| Submission request ID | Client-generated random UUID, retained with a frozen submission during retries. Not an account or channel ID. |
| Application ID | Stable client identifier, initially Workshop; server accepts an explicit supported application allowlist, not a tenant selector. |
| Client release | Build-produced non-secret release identifier, maximum 100 characters; nullable when genuinely unavailable. Not an arbitrary environment dump. |
| Page location | HTTP/HTTPS origin and pathname, maximum 2048 characters overall; remove userinfo, query, and fragment before transmission. Server repeats validation/sanitization. |
| Route/component key | Optional source-defined bounded identifiers, maximum 120 characters each; never infer identity or authorization from them. |
| Captured time | Client UTC timestamp, diagnostic/untrusted; server receipt time is authoritative. |
| Browser environment | Browser-reported user agent maximum 512 characters, language maximum 35, IANA timezone maximum 100, viewport width/height bounded 1-20000, online boolean. Missing optional capabilities remain explicitly absent. |
| Reporter | Server-verified PublicAccount ID and username snapshot, or anonymous. Do not send a user object, accept a client user ID, or store account email/password/security data. |
| Server correlation | Server-created feedback ID, received UTC time, request trace ID, schema version. Client-supplied traces must never replace server authority. |

Authenticated user association is evaluated **at submission**, not trusted from
the earlier UI snapshot. On an explicitly anonymous endpoint, invoke the dedicated
PublicAccount authentication scheme; do not assume default authentication
populates a public-account principal.

A missing account cookie is normal anonymous reporting. An expired/revoked
account cookie must never create a trusted account link: accept as anonymous
with explicit attribution in the receipt and a visible client warning if the UI
expected a logged-in report. Record only a safe attribution outcome in telemetry.
Infrastructure errors during identity validation are real failures, not an
anonymous-success fallback.

Do not capture cookies, auth headers, CSRF values, local/session storage,
request/response bodies, DOM snapshots, form values, clipboard, console history,
IP addresses in report documents, or Workshop inventory/recipe/ledger snapshots.
Do not install session replay or fingerprinting. Free-text descriptions may
still contain personal data; warn users not to include credentials/private details.

Query and fragment values are excluded by default, including return URLs and
tokens. A future query parameter requires an explicit route-level safe allowlist
and tests; it is not part of this implementation. Page/resource identifiers are
diagnostic data, never permission to access the underlying Workshop record.
BackOffice must not gain Workshop business-data APIs through this feature.

### API contracts

All new JSON routes use the approved v1 segment. Define dedicated request,
receipt, list-item, detail, paginated-list, and status-update contracts in the
existing shared .NET and TypeScript contract owners.

| Host | Operation | Behavior |
| --- | --- | --- |
| ServerAPI | POST `/api/v1/feedback` | Explicit anonymous interaction; exact public-account CORS policy; CSRF for every submission, including anonymous; validate and durably create. First create returns 201 with a receipt containing feedback ID, received time, and authenticated/anonymous attribution. Do not return an inaccessible management Location. |
| BackOffice | GET `/api/v1/feedback` | Require `feedback.view`; validated optional status/application filters, page 1-10000, page size 1-50, default 20. Return items, page, pageSize, total. Newest first with ID tie-breaker. |
| BackOffice | GET `/api/v1/feedback/{id}` | Require `feedback.view`; typed diagnostic detail including revision; 404 for absent/expired/deleted records. |
| BackOffice | PATCH `/api/v1/feedback/{id}/status` | Require `feedback.manage`; accept status and expected revision only. Atomic update returns current detail; stale revision returns 409. |
| BackOffice | DELETE `/api/v1/feedback/{id}` | Require `feedback.manage`; hard-delete for removal/privacy purposes, 204 on deletion, 404 if absent. SPA asks for confirmation. |

Receipt exposes no reporter ID, username, description, or management link.
ServerAPI has no list/detail/status/delete route. BackOffice has no report-create
route in this scope. Management DTOs exclude low-level persistence fields and
session/security data.

Statuses are stable string values: new, reviewing, resolved, dismissed. Creation
is always new. Authorized managers may reopen by selecting new/reviewing.
Persist last change time and effective staff ID; distinguish primary actor from
effective user in safe audit events if the existing impersonation flow is active.
Do not allow modification of the original report, reporter, or captured context.

Use `feedback.manage` implying `feedback.view` through the server expander.
Do not manufacture unused CRUD permissions or rely on staff shell/channel access.
Retain the existing manage-all override. API keys do not gain this browser
management surface.

Return standard validation/Problem Details errors: 400 invalid fields, 403 invalid
CSRF, 409 conflicting replay/stale revision, 413 oversized request, 429 throttled,
and non-detailed 5xx for persistence/dependency failure. Protected management
retains normal 401/403 behavior. Never return an empty successful list on failure.
All report/management responses are no-store.

### Persistence, retries, and operations

- Add a global `feedback` Mongo collection. Model follows BaseEntity conventions
  with immutable submitted fields, schema version, request ID, trusted reporter
  attribution, status, revision, created/updated metadata, and expiry.
- A focused Domain Feedback module owns validation, creation/idempotency,
  listing/detail, revision-checked status updates, and deletion. Reuse IMongoDatabase,
  TimeProvider, existing scenario infrastructure, and host registration patterns.
  A new general-purpose workflow framework or generic DataService expansion is
  unnecessary.
- Store normalized creation-content fingerprint with the request ID. A unique
  request-ID index resolves concurrent insert races. Exact replay with the same
  trusted attribution and frozen normalized payload returns the original minimal
  receipt with 200; different content/identity returns generic 409 without report
  data. Do not reset status, receipt time, or retention on replay.
- The client freezes the submitted payload for uncertain-network retries. Editing
  it starts a new submission identity. Double-clicks and concurrent requests must
  still create only one document. Reuse the existing narrowly handled duplicate
  insert approach, not a broad database exception catch.
- Proposed retention: expire the entire report **90 days after receipt**, including
  unresolved reports. Updates do not extend expiry. This is a reviewable product/
  privacy assumption, not an existing approved policy. Obtain owner approval
  before applying the destructive TTL index.
- Add indexes through the existing operational manifest: unique request ID;
  created time/ID; status with created time/ID; application with created time/ID;
  expiry TTL. Push filters, sort, limits, and counts into Mongo. Do not add
  description full-text search or load all reports into memory.
- Both hosts must point this repository at the same intended application database.
  Validate this with redacted deployment evidence; sharing Domain code alone is
  not proof of shared persistence.
- Add a dedicated feedback limiter, independent of login/newsletter limits:
  initial 5 submissions per minute per trusted connection IP, queue zero, 429.
  Do not persist that IP with reports or trust arbitrary forwarded headers.
  Use the existing ASP.NET rate-limiting mechanism; return a bounded retry delay.
  Per-process limits multiply across replicas: release must document the effective
  ingress/replica cap or provide an existing edge/global limit before anonymous
  production exposure. Do not pretend CORS or CSRF is bot protection.
- Enforce a 16 KiB UTF-8 request-body ceiling at the endpoint in addition to
  field bounds; the UI blocks an over-budget serialized payload before sending.
  Handle oversized multibyte descriptions explicitly, not by silent truncation.
- Use structured safe logs for operation, outcome, report ID, trace ID, duration,
  and status transition. Never log the body, page URL/path, user agent, description,
  cookies, or account snapshot. Infrastructure errors remain observable.
- No Hangfire job, new HTTP client, blob store, mail integration, or cache purge.
  TTL is approximate physical deletion; queries must also exclude expired records.
  Backups/log retention need separate operator confirmation.

### BackOffice integration

Create a global `/feedback` inbox and `/feedback/:id` detail route. Add a
permission-filtered Feedback entry to the existing platform/administration menu
group, not a channel-owned content group.

Inbox: status/application filters, created time, safe description excerpt,
reporter username or Anonymous, application/page path, and detail action.
Detail: immutable description/context, identity attribution, server/client times,
release/correlation, status control, and delete confirmation. Render all untrusted
content as escaped text. Display captured URLs as text initially; do not turn
arbitrary supplied origins into privileged links.

Use existing React Bootstrap controls, table responsiveness, alerts, loaders,
fetchers/actions, and server-effective permissions. Handle empty/loading/error/
pending/conflict states explicitly. Revalidate list/detail after mutations.
Do not refetch reports merely because the channel selector changes.

A staff user with feedback permission and **zero accessible channels** must
reach and use the inbox. Preserve authentication and explicit channel-loading
errors; do not introduce a success-shaped empty-channel fallback or redesign the
whole protected shell.

### Impeccable implementation workflow

This is a **refinement**, not an explicit redesign. Both the Workshop reporting
surface and BackOffice inbox are mode **Operate**.

No incumbent PRODUCT.md or DESIGN.md was found in these two workspaces.
Use the [Workshop functional baseline](../architecture/shooter-workshop.md)
and current Workshop/BackOffice source styles as primary context.
The public client's [DESIGN.md](../../frontend/morwalpizvideo.client/DESIGN.md)
is supporting brand context, not permission to copy app-local source or restyle
the admin shell.

During implementation use the installed Impeccable workflow, with the narrow
`harden` command for modal states, accessibility, and responsive integration.
Do not execute that design workflow as part of this planning handoff.
Minimum visual evidence: desktop and 360px mobile modal over a domain page and
login, visible failure/success states, contextual-component launch, and desktop/
mobile BackOffice inbox/detail. Check keyboard-only operation, focus return,
200% zoom, reduced motion, contrast, long descriptions, soft keyboard, and safe
overlay stacking. No new design-system documentation is required for this slice.

## Impacted Projects

| Project/surface | Impact |
| --- | --- |
| MorWalPizVideo.Models | Global report entity, status, collection name, permission keys. |
| MorWalPizVideo.Domain | Focused feedback service and repository interface/Mongo/mock implementations. |
| MorWalPiz.Contracts | Shared request/response DTOs; retain current dependency direction. |
| MorWalPizVideo.ServerAPI | Anonymous CSRF-protected optional-account intake, limits, DI. |
| MorWalPizVideo.BackOffice | Global permission-protected management endpoints, DI, index manifest. |
| MorWalPizVideo.BackOffice.Tests | Domain, API, authorization, concurrency, and repository behavior coverage. |
| @morwalpizvideo/models | Additive feedback TypeScript contracts and exports. |
| @morwalpizvideo/services | Isolated account intake/admin management helpers, endpoint constants, tests. |
| shooter-workshop.client | Global host, modal, context capture, contextual entry points, styles, DOM tests. |
| back-office-spa | Inbox/detail routes, menu, capability mapping, management tests. |
| Deployment/operations | Ordered API/client rollout, same-database proof, reviewed indexes, rate cap, privacy/browser gates. Existing deployment units remain unchanged. |
| @morwalpiz/layout | Verify only. Only one reporting UI consumer exists in this slice; do not publish the modal here yet. |
| Other SPAs, FreezerAnalysis, Range, Windows tools, ShortLinks, ServiceDefaults, AppHost | No implementation changes. Shared contract/transport regression verification only where consumed. Shop remains on hold. |

## Impacted Files

Paths below are concrete existing files unless explicitly marked as new.
New names describe the bounded feature, not permission to reorganize unrelated code.

### Modify

- [ServerAPI Program](../../MorWalPizVideo.ServerAPI/Program.cs): feedback DI and
  dedicated limiter; retain exact account CORS and existing host security.
- [BackOffice Program](../../MorWalPizVideo.BackOffice/Program.cs): global feedback DI
  in Mongo/mock branches.
- [DbCollections](../../MorWalPizVideo.Models/Constraints/DbCollections.cs):
  global collection constant.
- [AuthorizationKeys](../../MorWalPizVideo.Models/Constraints/AuthorizationKeys.cs)
  and [AuthorizationPermissionExpander](../../MorWalPizVideo.Domain/Security/AuthorizationPermissionExpander.cs):
  explicit feedback capabilities and one-way implication.
- [MongoIndexOperationsService](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs):
  reviewed feedback index definitions.
- [TypeScript model exports](../../frontend/fe-packages/models/src/index.ts),
  [service exports](../../frontend/fe-packages/services/src/index.ts),
  [admin endpoints](../../frontend/fe-packages/services/src/endpoints.ts), and
  [frontend endpoints](../../frontend/fe-packages/services/src/endpoints-frontend.ts):
  additive DTO/helper/endpoint exports.
- [Workshop main](../../frontend/shooter-workshop.client/src/main.tsx):
  persistent feedback wrapper/host.
- [Workshop States](../../frontend/shooter-workshop.client/src/components/States.tsx)
  and [pages](../../frontend/shooter-workshop.client/src/routes/pages.tsx):
  stable contextual launch integration without rewriting domain forms.
- [Workshop styles](../../frontend/shooter-workshop.client/src/main.scss):
  narrow launcher/dialog additions using current tokens.
- [Workshop manifest](../../frontend/shooter-workshop.client/package.json) and
  [Vitest config](../../frontend/shooter-workshop.client/vitest.config.ts):
  add direct test-only Testing Library/jsdom dependencies and a DOM environment
  only for the new component tests; retain existing node-test behavior.
- [BackOffice lazy routes](../../frontend/back-office-spa/src/router/routes/lazy-index.ts),
  [permissions](../../frontend/back-office-spa/src/authorization/permissions.ts),
  and [adminMenu](../../frontend/back-office-spa/src/layouts/adminMenu.ts):
  global inbox/detail and capability-aware navigation.
- [BackOffice test setup](../../frontend/back-office-spa/src/test/setup.ts),
  [async data tests](../../frontend/back-office-spa/src/router/asyncData.test.tsx),
  [toast navigation tests](../../frontend/back-office-spa/src/components/ToastNotification/index.test.tsx),
  [navigation action tests](../../frontend/back-office-spa/src/routes/navigation/action.test.ts),
  [insights news action tests](../../frontend/back-office-spa/src/routes/insights/news/action.test.ts),
  [page form action tests](../../frontend/back-office-spa/src/routes/pages/form/action.test.ts),
  [sponsor form action tests](../../frontend/back-office-spa/src/routes/sponsors/form/action.test.ts),
  and [video translation action tests](../../frontend/back-office-spa/src/routes/videos/translate/action.test.ts):
  align the Request/FormData and router lifecycle test harness without weakening
  behavior.
- [AdminSidebar tests](../../frontend/back-office-spa/src/layouts/AdminSidebar.test.tsx),
  [permission-expansion tests](../../MorWalPizVideo.BackOffice.Tests/Features/AuthorizationPermissionExpanderTests.cs),
  [admin controller metadata tests](../../MorWalPizVideo.BackOffice.Tests/Features/AdminRouteAuthorizationMetadataTests.cs),
  [host containment tests](../../MorWalPizVideo.BackOffice.Tests/Features/HostSecurityContainmentTests.cs),
  and [index tests](../../MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs):
  focused additions for the changed boundaries.

### Add Or Complete

- MorWalPizVideo.Models/Models/Feedback.cs: report aggregate and status.
- MorWalPiz.Contracts/Contracts/FeedbackContracts.cs: dedicated public/admin DTOs.
- MorWalPizVideo.Domain/Feedback/: focused service, repository interface,
  Mongo implementation, and scenario-backed mock implementation; keep ownership
  explicit without extending the large generic repository files.
- MorWalPizVideo.ServerAPI/Controllers/FeedbackController.cs: submission only.
- MorWalPizVideo.BackOffice/Controllers/FeedbackController.cs: management only.
- frontend/fe-packages/models/src/feedback.ts: typed DTOs.
- frontend/fe-packages/services/src/feedbackService.ts and adjacent tests:
  intake/admin helpers with isolated clients, strict success-envelope handling,
  complete receipt validation, and transport isolation coverage.
- frontend/shooter-workshop.client/src/components/feedback/: provider/host,
  dialog, context capture helper, and focused tests.
- frontend/back-office-spa/src/routes/feedback/: inbox/detail route modules,
  loader/action behavior, filtering, pagination, pending/conflict/delete states,
  and tests following the active lazy-route convention.
- frontend/back-office-spa/src/test/: one shared Request/FormData fixture only if
  runtime diagnosis confirms the mismatch; do not add per-test polyfills.
- MorWalPizVideo.BackOffice.Tests/Features/: focused Feedback-named service,
  endpoint, and repository tests, using existing factories/scenarios.

### Generated/Migration

- The existing frontend lockfile changes only for the explicit test dependency
  additions, generated by the repository's Yarn version.
- No EF migration, Mongo backfill, generated client, or checked-in build output.
- Mongo index audit/apply is an operator-reviewed deployment operation, not an
  automatic schema migration. TTL application waits for retention approval.

### Documentation

- This handoff is the editable source of truth for review and implementation results.
- During implementation update the backend, frontend, security, and Workshop
  architecture documents linked above. Clarify that feedback is platform
  administration, **not** a new Workshop management capability.
- Add a platform-feedback ADR with the next available identifier for global
  ownership, public intake/admin management, optional account attribution,
  minimization, and approved retention. Do not predeclare an unreviewed ADR accepted.

### Verify Only

- [PublicAccount authentication](../../MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs)
  and [public-account helper](../../frontend/fe-packages/services/src/publicAccountService.ts):
  reuse cookie/CSRF behavior unchanged.
- [Transport](../../frontend/fe-packages/services/src/apiTransport.ts) and its
  [readiness tests](../../frontend/fe-packages/services/src/apiTransport.readiness.test.ts):
  feedback is unscoped, never waits for channel readiness, and uses the correct host.
- [BackOffice root router](../../frontend/back-office-spa/src/router.ts):
  verify zero-channel access. Modify only if the focused regression demonstrates
  a feedback blocker, preserving unrelated shell behavior.
- [Workshop routes](../../frontend/shooter-workshop.client/src/routes/config.tsx)
  and [route tests](../../frontend/shooter-workshop.client/src/routes/config.test.tsx):
  all current navigation/authentication remains unchanged.
- [ServerAPI factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/ServerApiWebApplicationFactory.cs)
  and [BackOffice factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs):
  isolate tests; use a deliberately shared feature repository for cross-host proof,
  not an assumption that independently hosted mock lifecycles share state.
- Existing deployment workflows and [AppHost](../../MorWalPizVideo.AppHost/):
  retain current applications/origins/API configuration; no new service required.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | Add ServerAPI submission and separate BackOffice management over shared Domain persistence; no API project reference. |
| Frontend/UI | All-page Workshop host and component context; separate admin inbox/detail. Refinement of incumbent styles. |
| Database/Data | One additive global Mongo collection, request uniqueness, optimistic revision, approved retention indexes. |
| Contracts/Models | Additive dedicated DTOs; no entity serialization or channel field. |
| Authentication/Authorization | Optional verified PublicAccount intake; mandatory CSRF; staff feedback permissions for management; no API-key/channel authorization inheritance. |
| Validation | One description plus bounded typed context; server authoritative; unknown metadata not persisted; unsafe location values rejected/sanitized. |
| Cache | No-store; no OutputCache, client persistent report cache, cache tags, or cross-API cache invalidation. |
| Configuration/Secrets | Reuse existing origins and database providers. No credentials in payload/config/docs. Release identity is non-secret. |
| Logging/Telemetry | Safe outcome/correlation/duration and administrative status audit only; exclude submitted content. |
| Background Jobs | Not applicable; synchronous acknowledgement after persistence, native TTL retention. |
| Deployment/Operations | APIs/indexes before clients, explicit permission grants, shared-store and real-origin browser proof. |
| Backward Compatibility | Additive routes/contracts; preserve current sessions, public content, Workshop ownership/ledger, and frozen shop. |

## Reuse Opportunities

- Existing PublicAccount cookie validator, CSRF acquisition/validation, and exact
  credentialed CORS policy; no new authentication provider.
- Existing account/admin client instances, URL resolution, error helpers, and
  endpoint exports; no direct app-local fetch or singleton reconfiguration.
- BaseEntity, IMongoDatabase, TimeProvider, and scenario lifecycle.
- Existing narrowly handled duplicate-insert and revision patterns in
  [BlogInteractionService](../../MorWalPizVideo.Domain/BlogInteractions/BlogInteractionService.cs),
  but **not** its channel ownership, article moderation, or portal restrictions.
- AllowUser, permission expander, lazy routes, route guards, React Bootstrap,
  admin menu filtering, and normal action revalidation.
- Existing API factories, mock scenarios, Vitest/Testing Library patterns, and
  Mongo operational manifest. Share the presentation component only after a
  second actual reporting consumer exists.

## Risks

| Rank | Cause / affected surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| High | Sensitive values leak through URLs, free text, captured UI state, or logs. | Typed minimal context; strip query/fragment; no DOM/storage/body capture; plain-text display; bounded retention and explicit notice. | Token-seeded URL and description tests, request/log inspection, approved privacy review. |
| High | Spoofed user association or anonymous intake abuse. | Dedicated scheme only, no client identity, CSRF and exact origins, body cap, dedicated limit plus ingress/replica proof. | Forged identity/bearer tests, stale-cookie attribution warning, denied-origin and load-limit checks. |
| High | Accidental channel filtering or Workshop data access through BackOffice. | No channel fields/scope/header/readiness dependency; diagnostic IDs grant no underlying access. | Zero-channel staff can manage; changing channels has no effect; no Workshop data query from feedback detail. |
| Medium | Timeout/double-send or concurrent triage loses integrity. | Unique request ID, frozen payload, atomic replay resolution and revision compare-and-set. | Concurrent Mongo insert/status tests demonstrate one report and explicit 409. |
| Medium | Different host databases or isolated mocks conceal failed end-to-end integration. | Shared persistence deployment proof; intentionally shared feature test store. | ServerAPI receipt appears once in BackOffice under the same ID. |
| Medium | Logged-in identity unavailable because browser cookies are blocked or session expired. | Anonymous support with explicit receipt attribution; never fabricate the link or weaken cookie policy. | Real Workshop-origin tests with allowed/blocked cookies and expired sessions. |
| Medium | Modal disappears on login/error routes or blocks mobile controls. | Host outside route shells; explicit context actions; keyboard/mobile hardening. | All-route UI coverage, screenshots, focus/soft-keyboard checks. |
| Medium | TTL deletes unresolved reports or retains backup copies beyond expectations. | Explicit 90-day proposal and owner approval; display expiry to staff; document backups and irreversible deletion. | Approved policy, real TTL audit, expired-query tests, backup-retention evidence. |
| Low | Stale caches expose reports or hide triage results. | No-store and normal mutation revalidation; no new cache tags. | Header tests, two-session updates, and browser network inspection. |
| Low | New external service dependency disrupts delivery. | None introduced. | No outbound integration, session replay, or notification dependency added. |

## Technical Debt

- Workshop has only node-based route tests today. Add the minimum DOM test
  prerequisites for the new behavior; do not turn this into a whole-app testing
  rewrite.
- The BackOffice compatibility route index contains unused legacy definitions.
  Defer cleanup; register only in the active lazy manifest.
- Root BackOffice hydration includes accessible channels even for global pages.
  Isolate this feature from empty selection; only change hydration if its focused
  zero-channel test fails. Do not conceal real channel-bootstrap failures.
- Existing public-account release/cookie gates are unresolved documentation
  claims until checked in deployment. They block claims of trusted logged-in
  attribution, not permission to weaken auth/CORS or to invent identity.
- Existing per-process rate-limit infrastructure is not a distributed abuse
  guarantee. Resolve the production ingress/replica gate for this endpoint;
  a repository-wide limiter replacement is separate work.
- No unrelated refactor of Workshop pages, generic repositories, account
  lifecycle, permission catalog, or other application shells is authorized.

## Implementation Roadmap

1. **Restore the BackOffice SPA baseline.** Owners: BackOffice test setup and
   affected tests only. Diagnose the active Request/FormData implementations,
   introduce one shared form-request fixture, and migrate the nine failing action
   tests without changing assertions. Explicitly initialize/settle the memory
   router used by async-data and toast navigation tests while preserving pending,
   resolved, error, and real-navigation behavior.
   Gate: the unchanged full 416-test suite passes with no skips and the 34-item
   feedback navigation assertion remains.
2. **Make feedback transport fail closed.** Owner: shared services. Apply
   `requireSuccessfulResponse` to list/detail/status/delete, validate the complete
   intake receipt including authoritative time, and define deletion success only
   for the expected successful response.
   Gate: focused transport tests prove methods/URLs/bodies, account CSRF, admin
   cookie mode, no bearer/channel header, no channel-readiness wait, error-envelope
   rejection, 204 deletion, and malformed-receipt rejection.
3. **Complete Domain and repository invariants.** Owners: Models and Domain.
   Compare a normalized complete submission fingerprint on both replay paths;
   preserve immutable receipt/expiry/status fields; enforce expiry on detail,
   status, and delete; make mock request-ID uniqueness atomic and equivalent to
   Mongo; record the effective staff actor on status changes if the accepted audit
   contract remains in scope.
   Gate: deterministic service/repository tests cover exact replay, changed
   context conflict, concurrent uniqueness, stale revision, expiry-before-TTL,
   deletion, and Mongo/mock parity.
4. **Harden both API boundaries.** Owners: ServerAPI, BackOffice, Contracts, and
   mixed backend tests. Add no-store, complete body/metadata bounds, safe structured
   outcome logging, actor propagation, and missing compound index definitions.
   Extend authorization implication, controller metadata, index, endpoint, and
   cross-host tests.
   Gate: CSRF, optional verified identity, permission split, no public reads,
   no admin create, no channel scope, replay/conflict, expiry, request limits,
   no-store, and deliberately shared-store lifecycle tests pass.
5. **Harden Workshop as an Operate-mode refinement.** Owners: Workshop-local
   provider/styles/test setup. Add only the test dependencies needed for local
   jsdom/Testing Library coverage; keep existing node tests in their current
   environment. Use the incumbent visual system, not a redesign. Correct
   background inertness, focus containment/recovery, Escape and focus return,
   retained drafts, timer cancellation, pending dismissal prevention, serialized
   body budget, and actionable 400/403/409/429/network/server errors.
   Gate: login/domain/error-route availability, one editable field, frozen
   component context, success/failure/retry lifecycle, safe escaped text, 360px,
   200% zoom, keyboard-only, and reduced-motion checks.
6. **Bring BackOffice management to the maintained data-route pattern.** Owners:
   feedback route modules and focused UI tests. Move reads/mutations to
   loaders/actions/fetchers, add application/status filters and pagination,
   loading/empty/retry states, pending controls, conflict recovery, and failed
   delete preservation. Keep it global and independent of channel selection.
   Gate: view/manage/denied/manage-all, direct route guards, zero-channel access,
   channel-switch independence, pagination/filtering, status conflict, and delete
   failure tests pass.
7. **Run progressively broader validation.** Build shared packages in order;
   run focused new tests, complete shared-service and Workshop suites, the full
   BackOffice SPA suite, checked SPA builds, then one serial backend build and
   relevant `--no-build` category filters from the
   [test runbook](../architecture/testing.md). Update this handoff with exact fresh
   results; historical JUnit files are not current proof.
   Gate: no affected suite is red and no empty test selection passes.
8. **Complete operational gates and roll out.** Audit/apply unique and query
   indexes before writes; keep TTL unapplied until retention approval. Verify
   same-database configuration, exact Workshop origin, cookie/CSRF behavior,
   trusted proxy handling, effective aggregate rate limit, explicit staff grants,
   and one real report lifecycle. Release APIs first, BackOffice second, Workshop
   intake last.
   Gate: a known anonymous and authenticated report can be submitted, retrieved,
   triaged, and deleted without a selected channel.

An independent post-validation review by MorWalPiz Code Reviewer is recommended
because anonymous intake, personal diagnostic data, and two authentication
boundaries are involved. The user decides whether to request it.

**Stop boundary:** all Workshop pages have usable one-field reporting; safe
context/trusted optional identity reaches ServerAPI; authorized staff can manage
the same global records without channel context; tests and release evidence
cover those claims. Further application rollout, likes, notifications, replies,
attachments, and automatic error collection require separate approval.

## Testing Strategy

Planning-only status: none of these checks has been run for this handoff.
No build/test is required to validate this documentation-only creation.

| Layer / owner | Focused proof |
| --- | --- |
| Unit / Domain and shared services | Blank/whitespace, 1/4000/4001 characters; multibyte body budget at/over 16 KiB; location parsing and token stripping; context bounds; fixed context on navigation/retry; schema mapping; valid/invalid status. |
| Repository / BackOffice.Tests | Mongo predicates/sort/page/count stay server-side; unique simultaneous creation; same/different replay content and reporter; revision collision; hard delete; expired records absent before TTL sweep. Mock parity, including atomicity, uses deterministic time. |
| ServerAPI behavior / BackOffice.Tests | Anonymous and active-account submission; forged client reporter/default JWT cannot impersonate PublicAccount; absent/invalid CSRF rejected; expired/revoked cookie yields explicit anonymous receipt; identity dependency failure is 5xx; no public reads/admin writes; request limits and 429; persistence failure never succeeds. |
| BackOffice behavior / BackOffice.Tests | 401 anonymous, 403 insufficient permission, read-only viewer, manage-implies-view, manage-all; API-key denied; list/detail/status/delete, malformed IDs/filters/page bounds, stale revision, no reporter/context edits; absent or arbitrary X-Channel-Id never scopes data. |
| Cross-host contract / BackOffice.Tests | Intake receipt ID is retrievable in BackOffice using deliberately shared test persistence; private feedback cannot be read from ServerAPI; list/detail expose only allowed fields. Independent default mock hosts are not shared-store proof. |
| Workshop component / local Vitest DOM tests | One host across routes; login and domain page open; only one editable description; stable component context; pending disable; successful clear; failure/dismiss/reopen draft; stale-identity warning; long text; keyboard/focus restoration; no changes to underlying unsaved forms. |
| BackOffice component / existing Vitest setup | Permission-filtered menu/route guards, loader/action errors, filters/pagination, empty results, status conflict, confirmed deletion and revalidation; zero accessible channels and channel switching do not affect feedback data. |
| Transport regression / services Vitest | Submission targets ServerAPI account mode, management targets BackOffice admin mode; credentials/CSRF isolation; no X-Channel-Id, bearer, bootstrap wait, silent error envelopes, or malformed-receipt success. |
| Security/privacy / focused tests and browser evidence | Query/fragment with seeded tokens never transmitted; arbitrary metadata discarded/not persisted; HTML/script descriptions display as text; URL not auto-linked; report body/cookies absent from logs; denied-origin browser requests cannot act as the allowed application. |
| Migration/operations / controlled Mongo | Audit/apply approved definitions, duplicate race with real unique index, physical expiry behavior, no old collection changes; approved backup/TTL policy and effective global abuse cap. |
| End-to-end / real Workshop and admin origins | Logged-in and anonymous page/component reports become one global inbox item; staff status update persists; logout/blocked cookies produces explicit anonymous attribution; zero-channel manager works; report deletion is reflected; no Workshop resource access is granted. |

Build shared packages in existing dependency order before consuming apps.
Verify both touched SPAs and additive-model/service consumers using their existing
checks; do not expand the frozen shop. Run .NET builds/tests serially, build once,
then select tests without implicit rebuilds. Preserve test output/results as
implementation evidence rather than claiming this proposed matrix already passes.

## Migration And Rollback

- **MongoDB:** additive collection only; no existing-document rewrite, backfill,
  tenant reassignment, transaction requirement, or EF migration. Review/apply
  unique/query indexes before accepting writes. Retention TTL is applied only
  after policy approval. Expiry is server-created and never refreshed on retry.
- **Accounts/auth:** no credential/session migration. Use the existing host-owned
  PublicAccount cookie; do not add shared-cookie SSO, staff-cookie acceptance in
  ServerAPI, or localStorage bearer fallback. Existing account host/browser release
  gates remain mandatory for claims of logged-in attribution.
- **Contracts:** additive v1 routes and exports preserve existing consumers.
  Release server capability before clients; incompatible intake receipts are
  explicit errors. Stored reports carry schema version for future tolerant reads.
- **Permissions:** explicitly grant feedback access to intended staff/groups.
  Do not auto-grant it to all contributors or channel owners. Existing global
  manage-all bypass remains unchanged.
- **Configuration/deployment:** reuse current Workshop API origin and independent
  BackOffice base URL. Verify shared application database and exact-origin CORS.
  No new feature flag is required for this bounded slice; server-first/client-last
  rollout and targeted endpoint containment provide the release boundary.
- **Cache:** no migration or invalidation; report responses must be no-store.
- **Rollback:** remove/roll back reporting UI first, contain new submission if
  necessary, and roll back management/API capability coherently. Preserve reports,
  indexes, permission data, and prior security posture; do not drop the collection
  as application rollback. No retry queue is left behind.
- **Retention incident:** TTL and explicit deletion are irreversible without an
  approved backup recovery process. Application rollback does not stop a Mongo TTL
  index or recover deleted reports. If expiry policy was wrong, an operator must
  suspend the specific TTL behavior through reviewed operations and assess recovery
  separately; never promise recovery by simply deploying an older API.
- **Monitoring:** safe error/latency/429/duplicate/attribution outcomes plus one
  redacted known-report lifecycle check. No real user description in rollout logs.

## Open Questions

These are review decisions, not implementation choices to make silently:

1. Does "all pages" mean the tagged **Workshop application** as assumed, or all
   active public applications? If broader, name the applications before expanding
   shell integrations, identity schemes, exact origins, and shared UI ownership.
2. Confirm **list/manage**, rather than a feedback likes/voting feature.
3. Confirm anonymous reporting and **90-day expiry including unresolved reports**;
   name the owner approving privacy notice, access grants, and backup retention
   before production activation.

## Implementation Status

The initial end-to-end implementation is present in the repository:

- Workshop mounts a global feedback provider above routing, so the launcher is
  available on login, routed pages, and error states. The modal accepts only a
  description and submits automatically captured page, component, browser,
  viewport, language, timezone, online-state, and request-id context.
- ServerAPI exposes anonymous-capable `POST /api/v1/feedback`, validates the
  request and CSRF token, optionally attributes a verified PublicAccount
  identity, rate-limits intake, and provides duplicate-safe request handling.
- The shared Models, Contracts, and Domain layers contain the channel-independent
  feedback document, repository implementations, validation, expiry, and
  optimistic status handling. No `channelId` or channel header is required.
- BackOffice exposes protected list, detail, status, and delete operations and
  the BackOffice SPA includes permission-filtered navigation, inbox, and detail
  routes. `feedback.manage` implies `feedback.view`.
- Mongo index definitions cover unique request ids, creation ordering, and the
  proposed expiry field. Applying the TTL index remains gated on retention
  approval.

The initial implementation gaps and the historical 14-failure result have now
been addressed in source and focused validation. The checked-in historical JUnit
artifact remains an archive only; it is superseded by the fresh full-suite result
below and must not be used as current pass/fail evidence.

## Backend implementation update (roadmap steps 2–4)

Implemented on 2026-11-14 for the backend/domain/API scope only:

- Feedback documents now persist a normalized full-submission SHA-256 fingerprint
  and status-change actor identity (`UpdatedByAccountId`/`UpdatedByUsername`).
  Replay matching uses that fingerprint on both the initial lookup and the
  concurrent unique-request race path; the mock repository performs the same
  request-ID uniqueness check atomically.
- Detail, status mutation, and deletion all apply the expiry predicate before
  returning or mutating a document. Status updates remain revision-checked and
  preserve the original receipt, status, and expiry while recording the
  effective authenticated staff actor.
- Submission metadata is validated against the declared bounds without silent
  truncation. Operational outcomes log only structured request identifiers and
  result categories; submission payload values are not logged.
- Public and BackOffice feedback controllers declare `no-store`; the BackOffice
  contract includes status actor metadata. The index manifest now includes the
  status/application/creation ordering compound index in addition to the unique
  request-ID, creation ordering, and TTL definitions.

Files changed for this update: `MorWalPizVideo.Models/Models/Feedback.cs`,
`MorWalPiz.Contracts/Contracts/FeedbackContracts.cs`,
`MorWalPizVideo.Domain/Feedback/FeedbackService.cs`,
`MorWalPizVideo.Domain/Interfaces/IRepository.cs`,
`MorWalPizVideo.Domain/Interfaces/Repository.cs`,
`MorWalPizVideo.Domain/Interfaces/MockRepository.cs`,
`MorWalPizVideo.ServerAPI/Controllers/FeedbackController.cs`,
`MorWalPizVideo.BackOffice/Controllers/FeedbackController.cs`, and
`MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`.

Historical validation before the contract-aligned test correction: the
`AdminEditWorkflowTests.QuickLinks_create_persists_and_returns_the_additive_youtube_kind`
test expected `OK` while the controller correctly returned `NoContent`. The test
now asserts `204 NoContent`; the current full backend result is recorded below.
Two pre-existing Calendar Mongo tests remain skipped by their existing setup.

## Roadmap step 1 update: BackOffice SPA test harness

Implemented on 2026-11-14 within the BackOffice SPA test harness and existing
failing tests only:

- Added `src/test/request-fixtures.ts`, a shared URL-encoded form request
  fixture. This avoids passing jsdom's `FormData` implementation to Node's
  `Request` while preserving each action's submitted values and repeated keys.
- Updated the failing sponsor, navigation, page, insight-news, and video
  translation action tests to use that fixture.
- Updated `src/test/setup.ts` to normalize router-created requests when jsdom's
  `AbortSignal` is passed to Node's `Request`.
- Explicitly initialized and disposed memory routers in the async route tests,
  and initialized the toast navigation router before settling its assertions.
  Pending, resolved, rejected, toast, navigation, and action assertions remain
  unchanged.

Validation:

- Focused run: `cd frontend/back-office-spa && yarn vitest run
  src/router/asyncData.test.tsx src/components/ToastNotification/index.test.tsx
  src/routes/sponsors/form/action.test.ts src/routes/navigation/action.test.ts
  src/routes/pages/form/action.test.ts src/routes/insights/news/action.test.ts
  src/routes/videos/translate/action.test.ts --reporter=verbose
  --passWithNoTests=false` — **7 files passed, 15 tests passed**.
- Full run: `cd frontend/back-office-spa && yarn vitest run --reporter=verbose
  --passWithNoTests=false` — **88 files passed**.

No feedback production implementation or backend files were changed for this
roadmap step. The full run still emits existing jsdom CSS parse diagnostics and
the intentional sponsor exception log, but has no failing tests.

## Backend feedback regression coverage update

Added `MorWalPizVideo.BackOffice.Tests/Features/FeedbackEndpointTests.cs`
using the existing ServerAPI and BackOffice WebApplicationFactory/mock-scenario
patterns. The focused coverage verifies:

- anonymous intake is CSRF-protected, accepts a valid anonymous submission, and
  emits no-store response metadata;
- authenticated public-account intake reports authenticated attribution and
  accepts an exact replay while rejecting a changed context payload under the
  same request ID;
- `feedback.view` permits reading while `feedback.manage` is required for
  status and deletion;
- stale status revisions return conflict, successful status changes return
  no-store, and deletion returns 204;
- management reads remain global when an unrelated `X-Channel-Id` is supplied;
- expired feedback is hidden from detail, status, and delete operations.

Validation:

```text
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --no-restore
  --filter 'FullyQualifiedName~FeedbackEndpointTests|FullyQualifiedName~BackOfficeFeedbackEndpointTests'
```

Result: 5 passed, 0 failed, 0 skipped. The existing package vulnerability and
unrelated compiler warnings remain warnings only. No fixture blocker required
weakening an assertion.

## Frontend feedback hardening update

Implemented on 2026-11-14:

- Shared feedback management helpers now reject non-success transport envelopes,
  and intake receipts require an ID, authoritative receipt time, and boolean
  attribution state.
- Workshop feedback now preserves retry drafts, clears identity-sensitive state
  on session changes, enforces the serialized 16 KiB request budget, differentiates
  offline/throttle/session/general failures, restores focus, traps focus, makes
  the application content inert, and cleans up success timers. Workshop-local
  jsdom/Testing Library coverage verifies the modal behavior.
- BackOffice feedback list management now uses route-owned loading, status and
  application filters, pagination, pending/retry states, and a safe empty state.
  Detail mutations expose pending states, preserve failed deletion, and provide
  a reload path for optimistic-revision conflicts.
- The BackOffice jsdom compatibility harness now preserves router abort
  cancellation rather than discarding signals, with a dedicated cancellation
  regression test.

Focused validation:

- Shared feedback transport: 5 passed.
- Workshop feedback DOM tests: 2 passed.
- BackOffice feedback route tests: 3 passed.
- BackOffice request cancellation setup test: passed.

## Final validation update

Fresh integrated validation completed on 2026-11-14:

- Shared frontend packages (`models`, `services`, `layout`) build successfully.
- Shared services: **10 test files, 89 tests passed**.
- Workshop: **3 test files, 7 tests passed**. This includes the new jsdom setup
  and feedback modal accessibility/draft tests.
- BackOffice SPA: **419 tests across 88 test files passed with no failures**. The historical
  navigation-count failure and the 13 unrelated jsdom/router failures are no
  longer present. The run still emits existing `TimeoutNaNWarning`, jsdom CSS
  diagnostics, and the intentional sponsor exception log.
- Workshop production build passed.
- BackOffice SPA production build passed.
- Backend Release build passed with existing ImageSharp vulnerability warnings
  and unrelated nullable warnings.
- Full backend test project: **747 passed, 2 skipped, 0 failed, 749 total**.
- Focused feedback backend tests: **5 passed, 0 skipped, 0 failed**.
- The QuickLinks test now correctly asserts the controller's intentional
  `204 NoContent` response and its focused test passes.

Remaining release gates are operational rather than unimplemented code:

- Real-browser desktop/mobile validation, including exact origin cookies/CSRF,
  focus behavior, 200% zoom, reduced motion, and denied-origin behavior.
- Proof that deployed ServerAPI and BackOffice use the same intended database.
- Review and application of unique/query indexes before accepting writes.
- Retention/privacy approval before applying the destructive 90-day TTL index,
  including backup-retention review.
- Trusted proxy configuration and effective aggregate rate limiting across
  replicas.
- Explicit production staff permission grants and one known-report lifecycle
  through the deployed origins.
