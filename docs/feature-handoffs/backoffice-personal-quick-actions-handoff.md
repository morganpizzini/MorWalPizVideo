# Personal BackOffice Quick Actions

## Outcome

**Status: Implemented and locally verified; Mongo/deployed activation gates remain open.**

## Implementation Record

Implemented on 2026-11-10 without changing public routes, analytics endpoints,
existing permission semantics, or other users' worktree changes.

- Models/Domain own a new typed aggregate, pair-keyed Mongo/mock repositories and
  empty scenario collection. BackOffice composes both repository branches and
  defines the unique user/channel index in its existing operational manifest.
- MorWalPiz.Contracts owns typed requests/responses. The focused BackOffice
  controller/service owns GET/PUT/DELETE, effective-user/channel identity,
  catalog/default/projection logic, structural and 64 KiB request checks,
  API-key denial, no-store responses, and read-only impersonation.
- Shared frontend models/services add wire types, typed admin adapters and only
  the new versioned channel-scoped prefix. Cookies, CSRF and public/account modes
  retain their existing transport.
- The SPA adds a guarded lazy `/dashboard-actions` module, Administration item
  and breadcrumb label. Its editor supports explicit save/cancel/reset,
  add/edit/remove/reorder, read-only and error states, draft preservation on
  failed writes, and selected-channel locking during mutations.
- A local resource hook binds reads to root-loader validated session identity,
  permissions and selected channel, ignores obsolete responses and refetches on
  revalidation. The protected root has a stable `backoffice` route ID for that
  loader access. No preference data or identity authority is added to localStorage.
- Home replaces only its fixed shortcuts with DashboardQuickActions; custom
  links use literal native-anchor hrefs and escaped labels. Analytics remain
  independently loaded and usable when preference reads fail.
- Scope-safe draft initialization prevents editing before the saved selection
  is installed. Channel changes explicitly discard dirty drafts. Remove/reorder
  maintains a usable focus target. Cancel also clears obsolete save errors.

Actual new production files are the aggregate, three repository files, DTO file,
controller/service, shared TypeScript model/service, resource hook,
DashboardQuickActions component and route module listed below. Existing files
changed are the collection/scenario/composition/index seams, shared barrels/
endpoint/transport, root route ID, permission/lazy-route/menu registration,
ChannelContext/AdminSidebar, Home, scoped styles, and
[Breadcrumbs](../../frontend/back-office-spa/src/components/Breadcrumbs/index.tsx).
Focused tests were added in `DashboardQuickActionsTests.cs`, the shared service,
the Home action component and editor; existing Home/menu/channel/transport tests,
authorization metadata, cookie-CSRF and impersonation tests were extended.
The test factory needed no production or fixture-policy changes.

### Validation performed

All commands ran from the repository root. No dependencies were added or installed.

1. `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore -m:1 -p:BuildInParallel=false`
   passed, then
   `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --no-build --filter 'FullyQualifiedName~DashboardQuickActionsTests|FullyQualifiedName~DashboardControllerTests|FullyQualifiedName~AdminRouteAuthorizationMetadataTests|FullyQualifiedName~CookieAntiforgeryTests|FullyQualifiedName~ImpersonationTests' --logger 'console;verbosity=minimal'`
   passed **38 tests**. This includes exact 65,536-byte acceptance/65,537-byte
   rejection, private ownership, retired entries, literal strings, empty/reset,
   real cookie-CSRF middleware, API-key denial and impersonation restrictions.
2. `yarn --cwd frontend build:models` and `yarn --cwd frontend build:services`
   passed; `yarn --cwd frontend workspace back-office-spa build` passed the
   TypeScript/Vite production build. A transient unrelated channel declaration
   mismatch during concurrent workspace work was resolved by rebuilding existing
   shared models/services, without editing that feature's source.
3. `yarn --cwd frontend workspace back-office-spa exec vitest run src/routes/Home.test.tsx src/components/DashboardQuickActions.test.tsx src/routes/dashboardActions/index.test.tsx src/layouts/adminMenu.test.ts src/layouts/AdminSidebar.test.tsx src/contexts/ChannelContext.test.tsx src/router/routes/index.test.ts --reporter=dot`
   passed **36 tests**. Node 26/jsdom tests use native Node abort controllers
   locally in the two new route-loader test files to match native Request.
4. `yarn --cwd frontend workspace @morwalpizvideo/services test src/backOfficeQuickActionsService.test.ts src/apiTransport.readiness.test.ts`
   passed **24 tests**, including exact payloads, CSRF/channel headers, readiness,
   explicit failures and public/account isolation.
5. Headless Chromium exercised the production SPA against intercepted local
   synthetic API responses: save and return to Home, exact custom href, failed
   save retaining a draft across real router revalidation, explicit empty save,
   analytics surviving preference failure, and no horizontal overflow at
   1440px/390px. Desktop/mobile Home/editor and empty/error screenshots were
   inspected. This is browser UI proof, not a deployed API or Mongo test.
6. Impeccable's detector returned `[]` for the action component, editor and
   scoped stylesheet. Focused formatting was applied to edited feature UI/tests.

Existing ImageSharp advisory warnings and Vite's large-chunk warning remain.
Broader/full suites and repository-wide formatting/lint are left to the normal
downstream validation procedure; they were not represented as passing here.

### Remaining release gates and policy

- Apply/audit the unique index using the existing authorized operator process,
  verify first-write concurrency against the actual Mongo/Cosmos provider, then
  expose the feature. No database or deployed environment was mutated locally.
- Deploy API before SPA and verify real cross-origin cookies/CSRF plus the same
  user/channel on a second device. Private ownership is covered in local HTTP
  tests, but deployed cross-device persistence is not claimed.
- Literal custom links intentionally have no destination/content validation;
  browser/React restrictions, broken links and blank-label accessibility limits
  remain as specified. Last successful write wins; no merge/history is added.
- Routine rollback retains the new collection/index. See
  [Mongo rollout](../architecture/mongo-operations.md#personal-quick-action-rollout).

The sections below retain the source evidence and bounded specification that
guided implementation. Future-tense file/test suggestions are not additional
unimplemented product scope or claims of deployed verification.

## Accepted Outcome

Replace the fixed **Attività rapide** section with ordered quick actions owned by
the current user and selected channel. Persist preferences through BackOffice API
so they follow the user across devices. Add **Attività rapide** under the existing
Administration navigation section, where each user configures their own actions.

Confirmed requirements:

- Support predefined actions and custom actions with the user's own label and URL.
- Custom label and URL strings are literal user data. Do not validate route
  existence, permissions for the destination, URL syntax, schemes, domains, or
  label content; do not trim, normalize, prefix, encode, or rewrite their values.
  An entered `/videos` remains `/videos`, not an API URL or an absolute URL.
- Home renders the saved selection dynamically for the selected channel.
- Preferences are private to each user/channel pair, not channel-wide settings.
- Existing destination authorization still applies when a link is followed.

Accepted design decisions:

- Users can add, edit, remove, and reorder actions with explicit Save/Cancel.
- Missing preferences preserve the current three default actions, filtered by
  permission. An explicitly saved empty list means no action buttons.
- Predefined actions are permission-filtered; custom actions are not inspected
  or permission-filtered by destination.
- This is a refinement in **Operate** mode, not a dashboard redesign.
- Last successful write wins across devices; no merge or revision UI in this slice.

Scope ends at personal quick-action configuration and rendering. Do not introduce
configurable KPI widgets, arbitrary dashboard layouts, executable actions,
automatic publishing, administrator editing of other users' preferences, new RBAC
semantics, or changes to public QuickLinks.

## Current Evidence

All source paths below are repository-relative; links resolve from this document.
Initial source inspection was performed on 2026-11-10 during the read-only
planning pass. Implementation validation is recorded above.

| Source | Verified behavior and consequence |
| --- | --- |
| [Architecture guide](../architecture/README.md), [dashboard feature](../architecture/admin-dashboard.md), [frontend architecture](../architecture/frontend.md) | BackOffice owns administrative workflows; app-specific UI stays in the SPA. The dashboard combines quick actions and existing analytics. |
| [Home](../../frontend/back-office-spa/src/routes/Home.tsx) | Three fixed links: `/videos`, `/blogposts/create`, `/blogposts`. Permission checks distinguish article creation from article viewing. Analytics refresh on selected-channel changes and suppress stale responses. Shortcuts are independent of analytics loading/failure. |
| [Menu catalog](../../frontend/back-office-spa/src/layouts/adminMenu.ts), [sidebar](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx) | Administration is a stable navigation group. Items are individually permission-filtered; no separate administrator-only guard wraps that group. One responsive Offcanvas renders the navigation. |
| [Active route definitions](../../frontend/back-office-spa/src/router/routes/lazy-index.ts), [route export](../../frontend/back-office-spa/src/router/routes/index.ts), [guards](../../frontend/back-office-spa/src/router/guards.ts), [permissions](../../frontend/back-office-spa/src/authorization/permissions.ts) | Protected routes are exported from the lazy catalog. New protected module names need an explicit permission mapping. Unknown modules throw rather than silently granting access. |
| [Channel provider](../../frontend/back-office-spa/src/contexts/ChannelContext.tsx), [app store](../../frontend/back-office-spa/src/state/appStore.ts), [root loader](../../frontend/back-office-spa/src/router.ts) | Channel switching persists transport context, updates the shell store, and revalidates routes. Root loader data includes the validated session's user ID; display identity in the store can come from localStorage and is not an ownership authority. |
| [Dashboard controller](../../MorWalPizVideo.BackOffice/Controllers/DashboardController.cs), [host-neutral controller base](../../MorWalPizVideo.MvcHelpers/Controllers/ApplicationControllerBase.cs) | Dashboard reads explicitly require `backoffice.access` and resolved channel scope. Inheriting the controller base alone does not establish authorization. |
| [Channel resolution and middleware](../../MorWalPizVideo.BackOffice/Services/ChannelContext.cs) | `X-Channel-Id` resolves an accessible channel. Non-administrators need an active channel-owner relationship. Missing context is 400; nonexistent/inaccessible context is 404. This is separate from user-wide module permissions, not a channel-specific permission matrix. |
| [Authorization evaluator](../../MorWalPizVideo.BackOffice/Authorization/AllowUserAuthorization.cs), [impersonation identity helper](../../MorWalPizVideo.BackOffice/Authentication/ImpersonationClaimsTransformation.cs) | Server authorization resolves the effective user, expands permissions, and supports `backoffice.manageall`. Impersonation has an explicit target identity and a reusable hard-block attribute/middleware. |
| [User entity](../../MorWalPizVideo.Models/Models/User.cs), [base entity](../../MorWalPizVideo.Models/Models/BaseEntity.cs) | No quick-action preference exists on User. Generic aggregate IDs have ObjectId representation. Avoid unrelated whole-user writes or incompatible deterministic string IDs. |
| [Repository interfaces](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs), [channel-settings Mongo repository](../../MorWalPizVideo.Domain/Interfaces/VideoImporterChannelSettingsRepository.cs), [mock equivalent](../../MorWalPizVideo.Domain/Interfaces/VideoImporterChannelSettingsMockRepository.cs) | Focused keyed reads and atomic upserts already have prior art. Concurrent insert races are handled narrowly. Reuse the pattern, not the API-key-only VideoImporter endpoint or its opaque payload contract. |
| [Mock scenario base](../../MorWalPizVideo.Domain/Scenarios/BaseScenario.cs), [Primary scenario](../../MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs), [named scenarios](../../MorWalPizVideo.Domain/Scenarios/NamedScenarios.cs) | Collections must be explicitly initialized. Named scenarios inherit Primary. Reads clone records; mock persistence must survive scoped repository recreation and reset cleanly. |
| [Collection names](../../MorWalPizVideo.Models/Constraints/DbCollections.cs), [Mongo index operations](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs), [API composition root](../../MorWalPizVideo.BackOffice/Program.cs) | Collection constants and a source-owned index manifest exist. Mock and Mongo registrations are separate. General indexes are operator-applied; startup bootstrap is currently a narrow blog-interaction exception. |
| [Shared transport](../../frontend/fe-packages/services/src/apiTransport.ts), [service exports](../../frontend/fe-packages/services/src/index.ts), [endpoint constants](../../frontend/fe-packages/services/src/endpoints.ts), [typed service prior art](../../frontend/fe-packages/services/src/scriptStudioService.ts) | Admin transport owns credentials, CSRF, channel bootstrap/header injection, and explicit error handling. Scoped endpoint prefixes currently include `/api/dashboard`, not the proposed versioned route. |
| [SPA manifest](../../frontend/back-office-spa/package.json), [Models](../../MorWalPizVideo.Models/MorWalPizVideo.Models.csproj), [Domain](../../MorWalPizVideo.Domain/MorWalPizVideo.Domain.csproj), [Contracts](../../MorWalPiz.Contracts/MorWalPiz.Contracts.csproj), [BackOffice](../../MorWalPizVideo.BackOffice/MorWalPizVideo.BackOffice.csproj), [test project](../../MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj) | Owning .NET projects target .NET 10. React 19, React Router 7, React Bootstrap, Lucide, Zustand, Vitest/Testing Library, and Playwright are already declared; no new dependencies are needed. |
| [Home tests](../../frontend/back-office-spa/src/routes/Home.test.tsx), [endpoint test prior art](../../MorWalPizVideo.BackOffice.Tests/Features/VideoImporterSettingsEndpointTests.cs), [test factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs) | Existing component and HTTP fixtures can cover the new flow without live external dependencies. Mock tests do not establish Mongo uniqueness or deployed cookie behavior. |

No BackOffice PRODUCT.md or DESIGN.md was found in the frontend tree.
[Current styles](../../frontend/back-office-spa/src/index.css) and existing
components are the visual authority. The earlier
[navigation/Home handoff](backoffice-navigation-home-handoff.md) prohibited
personalization only within its own slice; this new feature intentionally adds it
without revising that historical delivery record.

## Implementation Specification

### Ownership and persistence

Create one additive `backofficeQuickActionPreferences` MongoDB collection.
Each aggregate contains an ordinary ObjectId-compatible ID, user ID, channel ID,
ordered action entries, creation time, and server-generated UTC update time.
User IDs are opaque, case-preserved identifiers, not usernames or emails.

A unique compound index on user ID and channel ID enforces exactly one document
per pair. Repository operations filter by both fields and perform atomic upsert,
read, or delete; never enumerate all preferences or replace the User document.
Use insert-only ID/creation fields and narrowly handle duplicate-key upsert races
using the established keyed-settings pattern. Initialize an empty collection in
PrimaryScenario and mirror atomic pair-level behavior in the mock repository.

Each entry has a stable client-generated ID and one of two explicit kinds:

- **Predefined:** a stable catalog action key. Label, destination, icon token,
  and required permissions come from the server-owned catalog, not saved copies.
- **Custom:** exactly the entered label and URL strings. No permission metadata,
  route lookup, icon customization, HTML, or executable callbacks.

Array position is the display order. Allow repeated destinations and labels;
entry IDs, not URLs or labels, identify rows. Do not add a separate enabled flag:
membership in the saved list is selection. Removing an action deselects it.

Do not delete saved predefined entries when permission is lost. Exclude them from
Home's visible projection and mark them unavailable in the editor; users can
remove or reorder them. Restoring permission makes them visible again.
Unknown/retired saved catalog keys are also unavailable, not converted to custom.

### API contract and authorization

Add a dedicated controller and focused BackOffice service at
**`/api/v1/dashboard/quick-actions`**. Keep the existing unversioned analytics
endpoints unchanged, following [ADR-003](../architecture/adr/ADR-003-versioned-dto-apis.md).

| Operation | Contract |
| --- | --- |
| GET | Return `isConfigured`, `channelId`, nullable `updatedAtUtc`, the ordered editable `actions`, permission-filtered `availableActions` catalog, and ordered resolved `visibleActions` for Home. No database document means default actions and `isConfigured=false`; do not persist a document on read. |
| PUT | Replace the whole ordered selection for the current pair, including an empty list. Return the same authoritative envelope with `isConfigured=true`. Last completed atomic write wins. No user ID or channel ID is accepted in the mutation payload. |
| DELETE | Remove only the current pair's document and return the GET envelope with defaults and `isConfigured=false`. Repeated reset is harmless. |

Shared .NET request/response DTOs belong in MorWalPiz.Contracts, not the similarly
named legacy Contracts project. Keep Mongo IDs and ownership internals out of the
response. TypeScript wire types belong in the shared models package; transport
functions belong in shared services. No generic preference framework is needed.

All operations require an authenticated interactive user, `backoffice.access`,
and `RequireChannelScope`. Reject API-key principals explicitly: they do not
represent a user's personal preferences. Resolve ownership exclusively from the
effective authenticated user and middleware-resolved channel. Global administrators
may select accessible channels but cannot supply another user's ID.

Use the effective impersonated user's preferences for GET, consistent with the
existing identity resolver. Block PUT and DELETE during impersonation using the
existing hard-block attribute; expose read-only configuration UI in that state.
The response includes a server-derived `canEdit` boolean so the editor does not
invent or infer impersonation policy.

Return private/no-store responses and preserve browser-cookie CSRF protection for
PUT and DELETE. Authentication failure remains 401; missing BackOffice access,
API-key callers, and impersonated mutations are denied; channel failures preserve
the existing 400/404 convention. Use Problem Details for feature-owned structural
errors and explicit non-success responses for persistence failures.

Structural checks are distinct from custom-content validation:

- Require the expected object/array/string shapes, known entry kind, nonempty
  unique entry IDs, and a known key for newly added predefined entries.
- Existing unavailable or retired keys may round-trip unchanged for the current
  pair. Reject newly forged or newly selected inaccessible predefined keys.
- Custom labels and URLs may be any strings, including empty strings or
  whitespace. Use text inputs, not native URL inputs or required/pattern rules.
- Apply a 64 KiB total request-body limit as a storage/transport bound, returning
  an explicit 413. Do not add per-string business limits or silently truncate.

### Initial predefined catalog

Use one server-owned catalog in the new feature service, reusing existing
permission constants and the server authorization evaluator rather than building
another hierarchy. Catalog availability requires the current channel scope and
the existing destination permissions.

| Stable key | Label | SPA destination | Permission alternatives |
| --- | --- | --- | --- |
| `videos.manage` | Gestisci video | `/videos` | `videos.view`, `videos.manage` |
| `articles.create` | Scrivi articolo | `/blogposts/create` | `pages.create`, `pages.manage` |
| `articles.list` | Tutti gli articoli | `/blogposts` | `pages.view`, `pages.manage` |
| `videos.import` | Importa video | `/videos/import` | `videos.import`, `videos.manage` |
| `calendar.open` | Calendario | `/calendarevents` | `calendar.view`, `calendar.manage` |
| `editorial.open` | Produzione editoriale | `/editorialproductioncards` | `editorialproductioncards.view`, `editorialproductioncards.manage` |
| `shortlinks.open` | Short link | `/shortlinks` | `shortlinks.view`, `shortlinks.manage` |
| `scripts.open` | Script Studio | `/script-studio` | `scripts.studio` |

These are navigation shortcuts, not mutations. The default ordered selection is
the first three entries only, matching existing Home behavior. The established
global administrator override still applies. Match route permissions in contract
tests so frontend route metadata and the backend catalog cannot drift unnoticed.
No frozen shop feature is added.

### Literal custom links

Render the label as ordinary escaped React text, never HTML. Render custom
destinations with a native anchor supplied the saved URL string, not React
Router's relative-path composer or an API endpoint helper. Do not add a base
prefix, placeholder interpolation, URL parser, reachability probe, permission
check, autocomplete rewrite, server redirect, URL preview request, or proxy.
Links navigate in the same tab; no automatic fetch runs while editing.

Test exact strings through input, save request, persistence, GET, and anchor
attribute for ordinary paths, queries, fragments, absolute URLs, whitespace, and
Unicode. Browser URL resolution/serialization and React's built-in unsafe-link
protections are not application validation and must not be bypassed to force a
navigation. Do not promise that every arbitrary string will navigate successfully.
Unsupported destinations may produce the normal browser/router error; broken
links are intentionally the user's responsibility.

This is deliberately not the validated public QuickLinks or safe-external-report
feature. Do not reuse their sanitization rules or broaden this literal-link
exception to either surface.

### Configuration and Home UX

- Add `/dashboard-actions` as a lazy protected route, mapped to `backoffice.access`
  for both reads and actions. Add **Attività rapide** under Administration with
  that same permission, not `configurations.manage` or an administrator role.
  It remains reachable for a BackOffice user who has no other Administration item.
- The editor names the selected channel and explains that changes are personal.
  Reuse the shell's channel selector; do not add another independent selector.
- Show available predefined choices with an Add control and an ordered selected
  list. **Aggiungi link personalizzato** adds literal label/URL text inputs.
  Each row has accessible Move up, Move down, and Remove controls. Drag-and-drop
  is not required and must not be the only reordering mechanism.
- Save submits the complete draft. Cancel restores the last loaded/saved state.
  **Ripristina predefinite** is an explicit reset with confirmation. Announce
  pending, success, and failure; retain drafts after failed saves.
- Show unavailable predefined entries in the editor without making them actionable.
  Show a neutral read-only explanation when `canEdit=false`.
- Home keeps **Attività rapide** above analytics and adds **Personalizza** linking
  to the editor. Render ordered `visibleActions`; predefined links retain router
  navigation and catalog icons, custom links use the native-anchor behavior above.
- Keep the section and Personalizza visible for an empty selection. Distinguish
  “no actions selected” from “no saved predefined action is currently available.”
  A user with no predefined permissions can still configure personal custom links.
- No selected channel: explain that a channel must be selected and issue no scoped
  API call. Do not show a previous channel's buttons or an enabled editor form.
- Quick-action fetching is independent of analytics. Its error has a local Retry;
  never replace a failed preferences read with defaults or hide analytics.
- Bind data and asynchronous results to validated session user ID plus channel ID.
  Read session identity from authenticated root loader data, not cached display
  identity. No cross-route global preference cache or new app-store slice.
  Refetch on channel/session identity changes, permission changes, editor return,
  and route revalidation; suppress stale responses and hide old-scope data immediately.
- A dirty draft belongs to its captured channel/user scope. On a shell channel
  change, discard it with an explicit notice and reload the new scope. Disable save
  until loaded. Once submission starts, do not allow channel selection to change
  the request header's scope: gate the existing selector while that scoped mutation
  is pending, then re-enable on success/failure. Do not submit a previous-channel
  draft using whichever header happens to be current later.

Impeccable implementation brief: narrow **shape** refinement for Home's action
section and the new editor; **harden** for state/error/accessibility coverage.
Preserve Bootstrap/Lucide styling, the existing sidebar and 992px breakpoint,
analytics content, Italian dashboard copy, and current navigation destinations.
No design-system extraction or mandatory new PRODUCT.md/DESIGN.md is needed.
Before UI edits, the implementer should load the installed workflow's target
context and craft guidance. Minimum visual proof: desktop and narrow/mobile
captures of Home and editor, plus empty, unavailable, and error states; check
keyboard order, focus after row removal/reordering, status announcements, long
labels/URLs, and no horizontal overflow. Do not render a misleading unlabeled
fallback for an intentionally empty custom label.

## Impacted Projects

| Project/surface | Impact |
| --- | --- |
| MorWalPizVideo.Models | Add persistence aggregate and collection constant; preserve existing User and channel entities. |
| MorWalPizVideo.Domain | Add focused Mongo/mock repository interface and implementations; initialize scenario collection. |
| MorWalPiz.Contracts | Add typed feature API contracts; keep existing contracts compatible. |
| MorWalPizVideo.BackOffice | New controller/service, catalog/projection, DI registrations, unique-index manifest entry. |
| MorWalPizVideo.BackOffice.Tests | HTTP ownership/auth/round-trip tests, repository/catalog/metadata coverage. |
| `frontend/fe-packages/models` | Add wire types and exports. |
| `frontend/fe-packages/services` | Add typed admin calls, endpoint constant, versioned channel-scoped prefix and tests. |
| `frontend/back-office-spa` | Add editor route/menu item; dynamic Home section and focused components/state; scope-safe channel selector gating. |
| Existing deployment surfaces | BackOffice API and SPA releases, additive Mongo collection/index provisioning; no new host. |

Shared services consumers verified in manifests: BackOffice, public client,
Shooting ITA, Ask, FreezerAnalysis SPA, and frozen shop. They receive additive
exports only; transport regression coverage must prove no change to public/account/
legacy modes. No application changes are planned for those consumers. Shared
layout, ServerAPI, ShortLinks, ServiceDefaults, AppHost, Windows tools, and
standalone frontend tools need no feature implementation.

## Impacted Files

Paths in this section are repository-relative. New filenames are proposed within
verified owning directories, not claims that these files already exist.

### Modify

- [DbCollections.cs](../../MorWalPizVideo.Models/Constraints/DbCollections.cs):
  declare the new collection.
- [PrimaryScenario.cs](../../MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs):
  initialize the empty typed collection; inherited named scenarios receive it.
- [Program.cs](../../MorWalPizVideo.BackOffice/Program.cs): register focused service
  and both repository branches.
- [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs):
  declare the unique user/channel compound index in the existing manifest.
- [Models exports](../../frontend/fe-packages/models/src/index.ts),
  [service exports](../../frontend/fe-packages/services/src/index.ts),
  [endpoints](../../frontend/fe-packages/services/src/endpoints.ts):
  additive types/functions/endpoint export.
- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts):
  recognize only the new feature's versioned prefix as admin-channel-scoped.
- [Home.tsx](../../frontend/back-office-spa/src/routes/Home.tsx):
  replace hard-coded actions with the scoped preference projection.
- [adminMenu.ts](../../frontend/back-office-spa/src/layouts/adminMenu.ts),
  [lazy-index.ts](../../frontend/back-office-spa/src/router/routes/lazy-index.ts),
  [permissions.ts](../../frontend/back-office-spa/src/authorization/permissions.ts):
  editor reachability and permission mapping.
- [ChannelContext.tsx](../../frontend/back-office-spa/src/contexts/ChannelContext.tsx)
  and [AdminSidebar.tsx](../../frontend/back-office-spa/src/layouts/AdminSidebar.tsx):
  expose/reuse a narrowly scoped mutation-pending guard for channel switching.
  This is transient UI state, not preference persistence.
- [index.css](../../frontend/back-office-spa/src/index.css): only necessary scoped
  wrapping/layout styles for dynamic actions and editor.
- [Home tests](../../frontend/back-office-spa/src/routes/Home.test.tsx),
  [menu tests](../../frontend/back-office-spa/src/layouts/adminMenu.test.ts),
  [sidebar tests](../../frontend/back-office-spa/src/layouts/AdminSidebar.test.tsx),
  [channel context tests](../../frontend/back-office-spa/src/contexts/ChannelContext.test.tsx),
  [route tests](../../frontend/back-office-spa/src/router/routes/index.test.ts),
  [metadata inventory](../../MorWalPizVideo.BackOffice.Tests/Features/AdminRouteAuthorizationMetadataTests.cs),
  [index tests](../../MorWalPizVideo.BackOffice.Tests/Features/MongoIndexOperationsTests.cs):
  cover the changed integration seams.
- Existing shared transport tests
  [apiService.test.ts](../../frontend/fe-packages/services/src/apiService.test.ts)
  and [apiTransport.readiness.test.ts](../../frontend/fe-packages/services/src/apiTransport.readiness.test.ts):
  assert channel bootstrap, exact scope and transport isolation.

### Add

- `MorWalPizVideo.Models/Models/BackOfficeQuickActionPreferences.cs`:
  feature aggregate and typed saved entries.
- `MorWalPizVideo.Domain/Interfaces/IBackOfficeQuickActionPreferencesRepository.cs`,
  `BackOfficeQuickActionPreferencesRepository.cs`, and
  `BackOfficeQuickActionPreferencesMockRepository.cs` in that same directory:
  pair-keyed persistence contract and implementations.
- `MorWalPiz.Contracts/Contracts/BackOfficeQuickActionsContracts.cs`:
  explicit save/read/catalog/resolved-action DTOs.
- `MorWalPizVideo.BackOffice/Controllers/DashboardQuickActionsController.cs` and
  `MorWalPizVideo.BackOffice/Services/DashboardQuickActionsService.cs`:
  HTTP boundary, scoped orchestration, catalog, defaults and projection.
- `frontend/fe-packages/models/src/backOfficeQuickActions.ts` and
  `frontend/fe-packages/services/src/backOfficeQuickActionsService.ts`:
  typed wire contract and existing-admin-transport adapters.
- `frontend/back-office-spa/src/routes/dashboardActions/`:
  route-local Component/loader/action and tests following existing lazy module
  exports. Keep form-specific state here.
- `frontend/back-office-spa/src/components/DashboardQuickActions.tsx` and a focused
  test: Home presentation and independent fetch lifecycle.
- `MorWalPizVideo.BackOffice.Tests/Features/DashboardQuickActionsTests.cs`,
  focused repository/service tests in the existing test project, and a shared
  service test beside the new transport adapter.

### Generated/Migration

No EF migrations or generated client files. Additive Mongo collection and unique
compound index are provisioned through the source-owned index operations process.
No user/channel backfill. Do not hand-edit compiled frontend outputs.

### Documentation

- This handoff: implementation decisions, actual files/checks, residual risks.
- [admin-dashboard.md](../architecture/admin-dashboard.md): replace the current
  fixed-shortcut description after implementation, including editor and API.
- [mongo-operations.md](../architecture/mongo-operations.md): exact feature index
  key, audit/apply evidence, release order and rollback retention.
- Existing [ADR-003](../architecture/adr/ADR-003-versioned-dto-apis.md),
  [ADR-008](../architecture/adr/ADR-008-mongo-index-migrations.md), and
  [ADR-015](../architecture/adr/ADR-015-backoffice-app-store.md) remain applicable;
  this slice does not change their policies. A new infrastructure/auth ADR is not
  needed unless implementation departs from these boundaries.

### Verify Only

- [User.cs](../../MorWalPizVideo.Models/Models/User.cs), existing RBAC/channel
  ownership and analytics endpoints: unchanged behavior.
- [Root loader](../../frontend/back-office-spa/src/router.ts),
  [app store](../../frontend/back-office-spa/src/state/appStore.ts),
  [auth service](../../frontend/back-office-spa/src/services/authService.ts):
  use authoritative session identity; no preference data in localStorage.
- [Test factory](../../MorWalPizVideo.BackOffice.Tests/Infrastructure/BackOfficeWebApplicationFactory.cs):
  reuse DI access and test identity/channel helpers; change only if a concrete
  test fixture seam is missing.
- [BackOffice API deployment](../../.github/workflows/deploy-backoffice-api.yml)
  and [SPA deployment](../../.github/workflows/deploy-back-office-spa.yml):
  existing path filters already cover affected projects and shared packages.
  Verify actual release order; no workflow modification planned.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | Add versioned personal-preferences endpoint and focused service; leave analytics unchanged. |
| Frontend/UI | New Administration editor and dynamic Home actions; keep remaining dashboard/shell intact. |
| Database/Data | One additive Mongo aggregate per user/channel, atomic full-list replacement, unique compound index. |
| Contracts/Models | Add explicit .NET DTOs, persistence types, and TypeScript wire types; no breaking existing contract. |
| Authentication/Authorization | Existing cookie session, CSRF, BackOffice access and channel resolver. No new permission key or channel RBAC matrix. API keys denied; impersonation read-only. |
| Validation | Structural DTO and total request-size checks only for custom entries; predefined selection checks use existing permissions. No custom URL/label content validation. |
| Cache | Private/no-store responses; no OutputCache, cross-API eviction, browser persistence, or PWA response caching. |
| Configuration/Secrets | No new secret, provider credential, environment value, or feature flag. |
| Logging/Telemetry | Structured failures and operation/user/channel metadata only; never log custom URLs/labels, which may contain sensitive text or query values. |
| Background Jobs | Not applicable; synchronous personal settings only. |
| Deployment/Operations | Apply and verify index, deploy API, then SPA; existing hosts/pipelines. |
| Backward Compatibility | Old SPA ignores new API; new SPA requires API deployed first. Missing preferences preserve defaults; explicit empty lists remain empty. |

## Reuse Opportunities

- Existing channel resolver, effective-user helper, authorization evaluator,
  impersonation hard-block middleware, and CSRF transport.
- Focused keyed upsert and duplicate-key handling from channel-settings repository,
  without borrowing its API-key auth or channel-global ownership.
- Source-owned Mongo index manifest; no general-purpose migration infrastructure.
- Existing Bootstrap forms/buttons/alerts, Lucide icons, shared toast provider,
  route loader/action conventions, and lazy protected route registration.
- Home's current channel-scoped stale-result protection, extended to preferences
  without coupling analytics and configuration failures.
- WebApplicationFactory, scenario lifecycle, route-aware Testing Library helpers,
  shared transport tests and existing authorization metadata inventory.

## Risks

| Severity | Risk / affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Cross-user/channel disclosure or saving a draft under a new channel after a switch | Server-derived pair only; scope-bound UI state; gate channel selection during submission. Two-user/two-channel HTTP matrix and delayed-response UI tests must prove isolation. |
| High | Arbitrary custom destinations may be broken, deceptive, or use browser-sensitive schemes | Explicit user-owned literal-link policy; escaped labels, native anchors, no HTML/script interpolation, no backend fetch or preview, no bypass of React/browser safeguards. Verify exact ordinary href strings and benign markup rendered as text; record remaining navigation risk. |
| Medium | Missing unique index permits duplicate preferences during concurrent first writes | Apply before exposing feature; atomic upsert and duplicate-key retry. Real-Mongo parallel first-save test proves one aggregate per pair. |
| Medium | Permission loss leaves visible predefined links or causes destructive preference pruning | Filter projections using current server permission evaluation and refresh on session/permission changes; retain unavailable entries. Revocation/restoration tests show hidden/restored links without data loss. |
| Medium | Full-list saves on multiple devices overwrite edits | Explicit last-successful-write-wins policy; no autosave or merge claims. Two-client ordering test confirms complete atomic lists and documented overwrite behavior. |
| Medium | New versioned prefix misses channel readiness/header injection or leaks into public clients | Narrow prefix registration and shared transport isolation tests; no edits to public/account request behavior. |
| Medium | Wrong SPA/API deployment order creates missing endpoint errors | API-first release and synthetic GET/PUT smoke test. Preferences failures remain explicit, never success-shaped defaults. |
| Low | Long/empty custom labels or URLs degrade accessibility/layout | Wrap rather than truncate stored values; full values editable; keyboard reordering and mobile evidence. Empty labels intentionally remain empty and carry a documented accessibility limitation rather than an invented label. |

No external-service dependency is added. Storage/logging must not propagate
custom-link content to telemetry or third-party preview services.

## Technical Debt

- Existing module permissions are user-wide while channel membership is separate.
  This does not block the change. Reuse both; defer channel-specific RBAC.
- Existing analytics DTOs are local/unversioned. Do not migrate analytics or
  introduce a unified dashboard API as collateral work.
- The app store's display identity is not authoritative. Use root session user ID
  in this feature; do not refactor all authentication state.
- The route export retains legacy definitions alongside the active lazy catalog.
  Add the new route to the active catalog only; defer route cleanup.
- General Mongo index provisioning is operational, not automatic at startup.
  Keep the existing policy and require release evidence; do not copy the narrowly
  approved blog startup exception.
- Literal custom links deliberately forgo route/content validation and can be
  inaccessible or poorly labeled. Isolate that behavior to this personal feature;
  do not silently “fix” it or weaken other link validators.

## Implementation Roadmap

1. **Contracts and data boundary.** Add aggregate, collection constant, repository
   interface, .NET DTOs and matching TypeScript types. Confirm discriminated entry
   kinds, literal string preservation, absent-versus-empty semantics, default list,
   and private scope. Gate: serialization/contract tests and compatible type builds.
2. **Persistence and operations.** Implement pair-keyed atomic Mongo/mock read,
   upsert and reset; initialize scenario state; add unique index manifest entry.
   Gate: mock lifecycle/parity tests, index-manifest tests and real-Mongo uniqueness/
   concurrent-first-save evidence before production. Depends on step 1.
3. **BackOffice API.** Implement catalog, effective-user/channel ownership,
   permission-filtered projection, structural checks, no-store responses,
   impersonation/API-key rules, explicit failures, and DI in both modes.
   Gate: focused HTTP/auth/ownership/literal-content tests; unchanged analytics.
   Depends on steps 1-2.
4. **Shared admin client.** Add endpoint and typed adapters using existing admin
   transport/error helpers; register the narrow versioned scoped prefix.
   Gate: CSRF, channel readiness, write failure, literal payload and transport-mode
   regression tests. Depends on step 1 and the agreed endpoint in step 3.
5. **Editor and navigation.** Add lazy `/dashboard-actions` route, permission
   mapping and Administration entry; implement explicit draft/save/cancel/reset,
   unavailable states, accessible reordering and channel-submit locking.
   Gate: configuration/menu/route/channel tests and no-selected-channel handling.
   Depends on steps 3-4.
6. **Dynamic Home.** Replace only the fixed action block with ordered resolved
   actions and Personalizza; independently fetch and retry preferences, preserve
   analytics and scope suppression. Gate: Home tests prove defaults, saved empty,
   arbitrary custom links, permissions, stale results and analytics independence.
   Depends on steps 3-4; visual integration completes with step 5.
7. **Verification and delivery record.** Run focused backend and frontend checks,
   checked SPA build and shared-package builds; collect desktop/mobile evidence,
   API-first deployment/index proof and cross-device smoke results. Update the
   dashboard/operations documents and this handoff with actual outcomes.
   Depends on steps 2-6.

Completion requires all confirmed requirements and focused gates above, with
remaining deployed proof explicitly marked open. Stop before unrelated dashboard,
RBAC, transport, public links, or shop changes. An independent post-validation
review is recommended because private ownership, literal links and async channel
switching cross persistence/auth/UI boundaries; the user may select MorWalPiz Code
Reviewer after implementation.

## Testing Strategy

- **Unit/service:** exact catalog/default order, existing global permission
  override, create-only article permission, unavailable/retired keys, deterministic
  projection ordering, duplicate row IDs, unknown kinds, null versus empty strings,
  and full-list replacement. No URI/content validator is introduced.
- **HTTP behavior, BackOffice.Tests:** anonymous/no-access denial; API-key denial;
  missing/inaccessible channel; two users sharing one channel; one user across two
  channels; no supplied ownership override; GET default without persistence;
  save/reload/reset; explicit empty; permission loss/return; repeated destinations;
  exact custom string round-trip; malformed structure and 64 KiB size boundary;
  explicit persistence failure; no-store metadata; impersonated reads and blocked
  writes/reset. Include category metadata in deployment-filtered tests.
- **Browser-cookie integration:** safe requests and CSRF-protected PUT/DELETE with
  the real host cookie pipeline, not only test auth. Confirm rejected CSRF requests
  cannot mutate preferences.
- **Repository/migration:** mock reset and scoped-instance persistence; Mongo
  pair-keyed queries, compound unique index audit, parallel first saves, concurrent
  full replacements and repeated deletes. Mock-only success is not uniqueness proof.
- **Shared services:** exact versioned endpoint, proper header after channel
  readiness, no missing-channel request, CSRF/error propagation, exact payload
  strings, no silent fallback. Public/account/legacy transport must remain isolated.
- **SPA component/route:** Administration appears for basic BackOffice users;
  deep-link guard; add/edit/remove/reorder/Save/Cancel/reset; dirty switch notice;
  selector disabled during mutation; unchanged saved draft after failure;
  read-only impersonation; no previous-user/channel flash; stale GET/PUT results
  ignored; retry works; Home analytics work if preferences fail and vice versa.
- **Contract consistency:** all predefined destinations exist in the active route
  catalog and required permission alternatives match existing route guards. Custom
  links bypass that lookup and retain exact `getAttribute('href')` values rather
  than comparing the browser-expanded `.href` property.
- **End-to-end/visual:** configure on device/session A and read on B with the same
  user/channel; switch channel and then user to prove isolation; follow predefined
  and custom internal/external links; save no actions; revoke/restore a predefined
  permission. Capture desktop and narrow/mobile Home/editor states, keyboard-only
  workflow, wrapping, focus, contrast and status announcements.
- **Build/regression selection:** follow the existing test runbook. Build backend
  once serially and run focused tests without rebuilding; do not overlap .NET
  output writers. Build shared models then services, run focused SPA/service tests,
  and perform the checked TypeScript/Vite application build. Escalate to broader
  owning suites only for concrete failures or the normal release gate.

Actual executed checks and their results are recorded in the Implementation Record;
the broader strategy does not imply that every deployment gate has been executed.

## Migration And Rollback

- **MongoDB:** additive empty collection with unique user/channel index. No legacy
  preference documents or content backfill. Before feature exposure, audit/apply
  the named manifest index and record successful uniqueness proof. If unexpected
  duplicates exist from a pre-release build, stop release and resolve them
  deliberately; do not silently discard user preferences.
- **Contracts:** additive `/api/v1` endpoint and exports; existing dashboard routes
  remain. No unversioned alias is needed because no old client uses this feature.
- **Deployment:** provision index, deploy API, verify authenticated scoped reads
  and writes, then deploy SPA. Existing workflows cover the changed paths but do
  not establish cross-workflow sequencing. No feature flag is proposed.
- **Monitoring:** observe feature HTTP errors and failed saves with operation and
  scope metadata; do not log link contents. Confirm real cross-origin cookie/CSRF
  behavior and the same user's second-device read.
- **Rollback:** revert SPA to fixed shortcuts first; retain the additive API and
  preference collection/index. Roll API back only after removing its new client.
  Preserved documents can be reused on re-release. No index/collection deletion
  is part of routine rollback.
- **Auth/cache/configuration:** no identity migration, permission backfill, cache
  purge, new credential, environment setting or SQLite change. Browser/session
  and public transport policies stay intact.
- **Recovery limit:** last-write-wins intentionally has no revision history or
  merge recovery. Reset affects only the current user/channel and cannot recover
  overwritten earlier lists. Operational backup retention stays under existing
  Mongo policy; this slice does not add a backup system.

## Open Questions

None blocking. The user confirmed literal custom labels/URLs and server-persisted
personal configuration under Administration. Catalog breadth, default preservation,
reordering, explicit save/reset, last-write-wins, and read-only impersonation are
the proposed bounded design decisions above, available for review in this file.

The unavoidable browser/React behavior of arbitrary destinations and the
accessibility limitation of intentionally empty labels are explicitly recorded,
not requests to add validation contrary to the confirmed requirement.
