# BackOffice article navigation, breadcrumbs and not-found shell

Status: implemented, 2026-11-09. Application and focused test changes are
complete. Browser/deployed verification was not performed.

## Outcome

Make interaction article links reach the existing editor, show the article title
in the editor breadcrumb instead of its ID, and render unknown admin URLs inside
the existing authenticated base layout. This is a refinement, not a redesign.

Assume the user's `blogposts/<id>` breadcrumb report refers to the editor reached
through the article workflow: the current router has no detail page at that bare
URL. Do not invent a separate read-only article management surface.

## Current Evidence

- [blogInteractions/index.tsx](../../frontend/back-office-spa/src/routes/blogInteractions/index.tsx)
  links article titles to `/blogposts/{postId}`.
- [lazy-index.ts](../../frontend/back-office-spa/src/router/routes/lazy-index.ts)
  registers `/blogposts`, `/blogposts/create` and `/blogposts/:id/edit`, but no
  `/blogposts/:id`. The interaction URL is therefore unmatched in current source.
- [router/routes/index.ts](../../frontend/back-office-spa/src/router/routes/index.ts)
  contains legacy declarations but imports/exports the active lazy protected
  routes. Fix the active manifest, not an unused parallel definition.
- [Breadcrumbs/index.tsx](../../frontend/back-office-spa/src/components/Breadcrumbs/index.tsx)
  reads the last match's raw data and expects a flat `breadcrumbIdentifier`.
  It does not label `blogposts` as a known segment.
- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx), `loader`,
  returns a `BlogPostAdmin` whose title lives at `draft.title`, not at a flat
  `breadcrumbIdentifier`.
- [guards.ts](../../frontend/back-office-spa/src/router/guards.ts),
  `withDeferredPermission`, wraps loader results as a `{ data: Promise }` envelope.
  [asyncData.tsx](../../frontend/back-office-spa/src/router/asyncData.tsx) resolves it
  through `Await`. The shell's `useMatches()` data is therefore not the resolved
  article object. Adding a property to the loader alone is insufficient.
- [router.ts](../../frontend/back-office-spa/src/router.ts) owns the authenticated
  root and `PrimaryLayout`; the active protected manifest has no wildcard.
  [PrimaryLayout.tsx](../../frontend/back-office-spa/src/layouts/PrimaryLayout.tsx)
  requires successful auth-loader channels and supplies sidebar, header,
  breadcrumbs, outlet and footer.
- [permissions.ts](../../frontend/back-office-spa/src/authorization/permissions.ts),
  `getRoutePermissions`, throws on unknown protected modules. A wildcard inserted
  through `protectRoute` without an explicit mapping would break route construction.
- [ErrorBoundary.tsx](../../frontend/back-office-spa/src/components/ErrorBoundary.tsx)
  is a class React error boundary, not a status-aware React Router error view.
  Rewriting all application error handling is unnecessary for an unmatched URL.

The link mismatch and breadcrumb envelope/shape mismatch are source-proven.
The exact appearance of the deployed default 404 was not independently inspected.

## Implemented Design

### Article destinations

Interaction links now target the existing `/blogposts/{encodedId}/edit` route
only when the session has `pages.update` or `pages.manage`; other users see
plain article text. A protected `/blogposts/:id` compatibility component
redirects to the canonical editor without a second fetch. Existing editor links
remain valid.
No public blog URL is changed.

Interaction access and page-edit access are separate permissions. Render the
editor link only for sessions with `pages.update` or `pages.manage`; otherwise show
the title as plain text rather than a misleading link. The destination's existing
server/route checks remain authoritative. Do not grant edit permission to moderators
or change the existing editor gate to solve the 404.

### Deferred-aware title breadcrumbs

The editor route now uses the existing route-handle facility to declare article-specific breadcrumb
metadata on the canonical editor route. Keep its title resolver typed against
`BlogPostAdmin` and derive the persisted `draft.title`, with slug/generic Article
fallback for absent data. Do not introduce a network request from breadcrumbs.

The app-local Breadcrumbs renderer now resolves the existing deferred
envelope with `Await` inside a small `Suspense` boundary for the label. Retain the
existing flat `breadcrumbIdentifier` behavior for other routes; do not cast the
entire raw match envelope to a string record. Render a stable generic Article
label while pending or on breadcrumb-resolution failure; the content boundary
still displays the actual page-fetch error. Do not expose a failed request as an
apparently loaded article.

Add a fixed `blogposts` label, Blog articles. Apply entity titles only to the
matching dynamic ID segment, never all unknown segments. Nonlast ID breadcrumbs
may link through the compatibility alias. Create has no entity ID and retains
Create; Edit remains Edit. After successful save/revalidation, use the new persisted
title. Unsaved keystrokes need not change the shell breadcrumb.

### Not-found layout

An app-local not-found route component is registered as the protected root's
wildcard child.
It renders only page content using the incumbent PageHeader/card/button conventions,
with a clear Not found heading and a Home link. `PrimaryLayout` provides the shell;
do not mount a second layout, ChannelProvider, sidebar or auth loader.

Add an explicit wildcard permission mapping to `backoffice.access` before unknown
module rejection. Keep rejection for other accidentally unmapped feature modules.
This child route lets authentication run normally: anonymous unknown URLs redirect
to login; authenticated backoffice users see the styled not-found page.

This fixes unmatched paths. Preserve existing route-level loader error handling:
400/401/403/409/500 must not become not-found pages. A known entity endpoint's 404
remains in its existing in-shell route error boundary. Do not expand this task into
a global ErrorBoundary/root-auth-error rewrite or unauthenticated shell extraction.

### UI planning lens

Impeccable mode: Operate. Narrow implementation workflow: `harden` for navigation,
pending labels and the 404 state; no redesign. No incumbent BackOffice
`PRODUCT.md`/`DESIGN.md` was found in the inspected frontend tree. Use the existing
Bootstrap/Lucide admin shell and component styles as visual authority. Implementation
should capture desktop/mobile shell evidence and keyboard/focus/contrast checks;
no new visual-world artifacts are required for planning.

## Impacted Projects

- `frontend/back-office-spa`: active manifest, permission mapping, breadcrumbs,
  interaction links, app-local 404 content and focused tests.
- `MorWalPizVideo.BackOffice`, Contracts, Domain and shared frontend services/models:
  verify-only editor response and existing permission contracts.
- Public client and shared layout: no changes; admin breadcrumbs are app-local.
- Database, ServerAPI, other SPAs, Windows tools, ShortLinks, AppHost and deployment
  topology: no required changes.

## Impacted Files

### Modify
- [blogInteractions/index.tsx](../../frontend/back-office-spa/src/routes/blogInteractions/index.tsx):
  canonical editor destination and permission-aware link.
- [lazy-index.ts](../../frontend/back-office-spa/src/router/routes/lazy-index.ts):
  compatibility alias, editor breadcrumb handle and protected wildcard.
- [permissions.ts](../../frontend/back-office-spa/src/authorization/permissions.ts):
  wildcard backoffice-access mapping only.
- [Breadcrumbs/index.tsx](../../frontend/back-office-spa/src/components/Breadcrumbs/index.tsx):
  deferred-aware, segment-specific labels and existing flat-data compatibility.

### Add
- App-local `notFound` route module under
  [routes/](../../frontend/back-office-spa/src/routes/), matching existing lazy module
  export conventions; no separate layout.
- Focused route and component tests under the existing
  [router/](../../frontend/back-office-spa/src/router/),
  [components/](../../frontend/back-office-spa/src/components/) and
  [blogInteractions/](../../frontend/back-office-spa/src/routes/blogInteractions/)
  test ownership.

### Generated/Migration
None. Do not manually edit frontend build output.

### Documentation
- This handoff and [structured-blog.md](../architecture/structured-blog.md):
  canonical editor, compatibility alias and breadcrumb behavior.
- [frontend.md](../architecture/frontend.md): document authenticated wildcard
  placement without changing root authentication behavior.

### Verify Only
- [router.ts](../../frontend/back-office-spa/src/router.ts).
- [PrimaryLayout.tsx](../../frontend/back-office-spa/src/layouts/PrimaryLayout.tsx).
- [asyncData.tsx](../../frontend/back-office-spa/src/router/asyncData.tsx)
  and [asyncData.test.tsx](../../frontend/back-office-spa/src/router/asyncData.test.tsx).
- [guards.test.ts](../../frontend/back-office-spa/src/router/guards.test.ts).
- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx).
- [ErrorBoundary.tsx](../../frontend/back-office-spa/src/components/ErrorBoundary.tsx).

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No new endpoint or response shape. |
| Frontend/UI | Valid article destinations, resolved title breadcrumb, in-shell unmatched page. |
| Database/Data | No impact. |
| Contracts/Models | Local typed route metadata only; shared DTO unchanged. |
| Authentication/Authorization | Preserve editor permission; wildcard requires backoffice access and normal root auth. |
| Validation | Unknown paths distinct from known-route server/permission errors. |
| Cache | No API caching; loader/revalidation behavior preserved. |
| Configuration/Secrets | No settings or secret changes. |
| Logging/Telemetry | Preserve content-error visibility; no broad error-to-404 conversion. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Existing admin bundle deployment; verify direct URL fallback on the actual host. |
| Backward Compatibility | Existing edit paths work; bare article URLs become aliases instead of 404s. |

## Reuse Opportunities

Reuse `PrimaryLayout`, PageHeader, React Router route handles, `Await`/`Suspense`,
current permission checks and existing memory-router test helpers. Keep reusable
behavior inside the admin application; no second SPA requires this breadcrumb
change, so moving it to shared layout is unjustified.

## Risks

| Rank | Cause/surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Wildcard lacks a permission mapping and crashes router construction. | Explicit mapping plus a test importing/building the actual active route tree. |
| Medium | Breadcrumb reads unresolved data or labels static segments as the title. | Deferred envelope test; exact Home / Blog articles / Title / Edit hierarchy. |
| Medium | Title belongs to previous post/channel after navigation. | Route-scoped resolution; pending replacement label and no stale title after switching. |
| Medium | Moderator can view interactions but cannot edit pages. | Permission-aware link; no privilege expansion; direct alias still enforces destination access. |
| Medium | Mounting full layout as an error view requires missing loader data. | Wildcard child under successful auth root, not a new root error layout. |
| Low | Deployed host returns its own 404 before SPA routing. | Hard-refresh unknown and canonical/alias URLs in the existing deployment; report host fallback separately if broken. |

## Technical Debt

Legacy route declarations duplicate the lazy manifest, and ErrorBoundary is not a
proper Router-status renderer. Both are deferred; neither must be rewritten to fix
this request. Existing generic breadcrumbs have loose flat-data typing; improve only
the touched resolution/label boundary while retaining other feature behavior.

## Implementation Delivered

1. Added the canonical editor link, page-edit permission guard, bare-ID alias,
   deferred title handle/renderer, wildcard permission mapping, and in-shell
   not-found component in the active lazy manifest.
2. Added focused interaction permission/link tests and active-manifest alias,
   wildcard, and permission tests.
3. Preserved the pre-existing removal of the `Article activity` heading.
4. Validation completed: the targeted admin form, blog-interaction, and active
   route-manifest tests passed (34 tests); the BackOffice SPA TypeScript and
   production build passed. The UI detector reported no findings.
5. A broader run that also included `asyncData.test.tsx` failed its 3 generic
   route tests with an undici/DOM `AbortSignal` mismatch; the changed feature tests
   passed in isolation. Browser direct-navigation, responsive, accessibility,
   and deployed-host fallback checks remain deferred.

Stop at these admin surfaces. No read-only article detail feature, shared layout
redesign, router rewrite or global error-system migration. Independent review is
optional; recommend focused review of wildcard permission and deferred-data handling
if implementation extends beyond this plan.

## Testing Strategy

- Routing/component: interaction click reaches `/edit`; bare alias redirects once;
  ID encoding preserved; no second article fetch due to alias.
- Permissions: editor-authorized links versus plain title for moderation-only
  users; direct editor/alias keeps existing denial; wildcard does not grant feature
  permissions or suppress unknown-module mistakes.
- Breadcrumb contract: wrapped real loader promise, direct flat data, pending
  state, rejected request, Create, Edit, title update after persisted save,
  post/channel navigation and long-title wrapping. ID is replaced only in the
  entity segment; Home/Blog articles/Edit retain their labels.
- Shell/end-to-end: unknown top-level and nested URLs show exactly one header,
  sidebar, footer and Home action after auth; anonymous unknown URL goes to login;
  known entity 404 and 403/500 remain distinct from unmatched paths.
- Accessibility/responsive: one page heading, functional navigation links,
  keyboard focus visibility, no horizontal overflow on mobile, readable long
  titles, and no shell layout shift caused by resolving breadcrumb text.
- Deployment: actual-host hard refresh of editor, alias and unknown URLs.
  No data-migration or new API-contract tests are required.

## Migration And Rollback

SPA-only additive routing/label changes; no schema, auth, cache or configuration
migration. Existing routes stay valid. Roll back the admin bundle to restore old
behavior; data remains untouched, though legacy interaction links and bare URLs
will fail again. No backfill, feature flag or backend deployment is necessary.
If actual-host history fallback is missing, obtain explicit approval for that
separate deployment change rather than silently editing hosting configuration.

## Open Questions

None blocking under the stated existing-editor destination. A read-only admin
article detail page for moderators would be a separate feature, not part of fixing
the broken editor link.

## Changed Files

- `frontend/back-office-spa/src/routes/blogInteractions/index.tsx`: canonical
  editor link with page-edit permission awareness; existing heading deletion
  preserved.
- `frontend/back-office-spa/src/routes/blogInteractions/index.test.tsx`: link
  destination and denied-link coverage.
- `frontend/back-office-spa/src/router/routes/lazy-index.ts`: bare-ID alias,
  editor breadcrumb metadata, and authenticated wildcard.
- `frontend/back-office-spa/src/router/routes/index.test.ts`: active route and
  wildcard permission coverage.
- `frontend/back-office-spa/src/authorization/permissions.ts`: explicit
  `backoffice.access` mapping for the wildcard.
- `frontend/back-office-spa/src/components/Breadcrumbs/index.tsx`: deferred,
  segment-specific article title resolution with stable fallback labels.
- `frontend/back-office-spa/src/routes/blog/alias.tsx`: compatibility redirect.
- `frontend/back-office-spa/src/routes/notFound.tsx`: authenticated-shell
  not-found content.
