# Blog authoring controls and cover-media workflow

Status: implemented in the worktree; browser visual verification remains outstanding.
The user confirmed that primary Save publishes and Save as draft only persists, and
approved article-local media. Implementation and automated validation completed
2026-11-05.

## Outcome

Address four BackOffice article-authoring inconsistencies: primary Save with a
Save as draft dropdown, discoverable cover upload/selection, removable comma-created
tag pills matching video editing, and Create article in the index header's right side.
This is a refinement, not a redesign.

Confirmed behavior: for publishers, **Save saves the current article and publishes
it**; Save as draft saves without changing the live snapshot. A non-publisher receives
an explicitly labelled Save as draft action rather than a Save control that implies
publication.

The initial delivery uses an article-local media box, not a site-wide WordPress
media library. It preserves current post-owned images, API contracts, permissions,
public reads, canonical documents, and permanent ever-published slug locking.

## Delivered Implementation

- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx) now saves the
  current revision before publishing and publishes only the returned revision. A
  failed publish after a successful save reports the partial success and retains the
  saved post so the author can retry. Save as draft never publishes. Non-publishers
  only see the draft-save action. A selected-channel consistency check prevents the
  follow-up API call if the channel changes between sequential operations.
- The same form now has a post-local cover box on create and edit, with an empty state,
  upload input and limits, required image alt text, thumbnail selection, cover preview,
  remove action, and visible unsaved selection state. For a new or dirty article,
  uploading explicitly saves first; successful upload selects the image as cover,
  requiring another Save or Save as draft to persist that reference. A failed upload
  after a save retains the confirmed article revision and the selected local file.
  Update/manage permission is required for upload, including on create.
- The blog metadata form reuses [TagInput](../../frontend/back-office-spa/src/components/TagInput/index.tsx)
  with 20 tags and 60 characters per tag. The shared input now accumulates tags from
  a single comma-separated paste without dropping earlier tags; Enter, blur, removal,
  keyboard behavior, and video defaults remain shared.
- [index.tsx](../../frontend/back-office-spa/src/routes/blog/index.tsx) places the
  permission-gated Create article action in the existing PageHeader actions slot.
- No API, model, persistence, RBAC, cache, shared-service, or public-client change.

Changed code/test files: `frontend/back-office-spa/src/routes/blog/form.tsx`,
`form.test.tsx`, `index.tsx`, new `index.test.tsx`,
`frontend/back-office-spa/src/components/TagInput/index.tsx`, and
`frontend/back-office-spa/src/components/__tests__/TagInput.test.tsx`.
Updated feature documentation: this handoff and
`docs/architecture/structured-blog.md`.

Validation:

- `yarn --cwd frontend workspace back-office-spa build` — passed (`tsc -b` and
  production Vite build); Vite emitted a >500 kB chunk-size warning.
- Expanded final regressions — 64 tests passed across the blog routes, video edit,
  and TagInput.
- Targeted ESLint — passed with 3 `react-refresh/only-export-components` warnings in
  form/index modules; no lint errors.
- Targeted Prettier check — passed.
- Impeccable detector on changed UI sources — no findings (`[]`).
- No live browser was available. Desktop/mobile layout, dropdown clipping, focus and
  touch behavior, real image upload, and public published-cover smoke remain unverified.

## Baseline Evidence

Paths below are relative to the repository root.

| Surface | Verified behavior and cause |
| --- | --- |
| Save/publish | [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx), `BlogForm`, always renders Save draft. Publish renders only when a persisted `post` exists and the session has `pages.manage`; dirty drafts disable it. Creation redirects to the edit route. Thus a new article has no Publish control even for a publisher. On a persisted article its absence can instead be permission-related; the user's actual session was not inspected. |
| Publication semantics | [BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs), `Create`, `Save`, `Publish`, and `Unpublish`, expose separate operations. [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs), `SaveAsync` and `PublishAsync`, preserve the old published snapshot on draft save and publish the stored draft, not unsaved browser content. Every replacement increments the expected revision. |
| Cover selection | `BlogForm` renders a select populated only from `post.images`, plus separate cover alt text. There is no upload affordance in that select and no thumbnail preview. |
| Upload discovery | `BlogForm` renders a separate upload form only after `post` exists. Its fieldset is disabled while busy, dirty, or lacking `canSave`. On a new article it is absent; after editing an existing article it becomes disabled without an adjacent explanation. Upload does not select the new image as cover. |
| Upload authorization | `BlogPostsController.Upload` requires `pages.update` or `pages.manage`; create permission alone is insufficient. The UI currently uses `canSave` for this gate, which is not the same capability on a create workflow. |
| Media constraints | `Upload` checks nonempty files up to 10,000,000 bytes, alt text of 1-300 characters, expected revision, decoded single-frame images up to 25 megapixels, and channel/post ownership. It uses `ChannelNewsMediaProcessor.PrepareImageAsync`, Blob storage, and `BlogService.AddImageAsync`; the latter caps images at 100. The browser advertises JPEG, PNG, and WebP. |
| Image references | [BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs) uses the storage key as the opaque image ID. `BlogService.ValidateSnapshot` requires cover ownership and nonblank cover alt text. Upload alt text and snapshot-specific cover alt text are separate fields. |
| Tags | `BlogForm` joins the tag array with commas and splits every input change. It never uses the existing pill control. Blog validation permits 20 tags of up to 60 characters; it does not apply the video tag normalizer. |
| Existing video interaction | [TagInput/index.tsx](../../frontend/back-office-spa/src/components/TagInput/index.tsx) supports comma, Enter, blur, removable pills, empty-field Backspace, case-insensitive duplicate prevention, and optional keyboard suggestions. [tagRules.ts](../../frontend/back-office-spa/src/components/TagInput/tagRules.ts) defaults to 20 tags of 32 characters. [videos/edit/Component.tsx](../../frontend/back-office-spa/src/routes/videos/edit/Component.tsx) already consumes it. |
| Tag batch edge case | The shared input processes multiple comma-delimited segments by repeatedly calling `commitTag`, each reading the same render's `value`. A single paste can therefore replace earlier additions. Existing tests type comma sequences one character at a time; they do not prove one-event paste handling. |
| Index header | [blog/index.tsx](../../frontend/back-office-spa/src/routes/blog/index.tsx) renders Create article after `PageHeader`. [PageHeader/index.tsx](../../frontend/back-office-spa/src/components/PageHeader/index.tsx) already accepts an `actions` slot aligned right, as well as `createLink`. The slot preserves the current English label and Lucide icon without altering shared header copy. |
| Transport/public consumption | [blogService.ts](../../frontend/fe-packages/services/src/blogService.ts) already provides create, save, publish, and multipart upload using shared admin transport. Public contracts project cover URL/alt only from Published; the public blog and home consume them. No public UI change is required. |

The current frontend manifest verifies React 19, React Router 7, React Bootstrap 2,
Puck 0.23.0, Tiptap 3.31.4, and Vitest 3.2.6. Relevant backend project manifests
target .NET 10. The frontend workspace manifest verifies Yarn Classic ownership.

Owning guidance:
[structured-blog.md](../architecture/structured-blog.md),
[frontend.md](../architecture/frontend.md), and
[testing.md](../architecture/testing.md).
The prior [Puck handoff](puck-editor-layout-feedback-handoff.md) owns inline
composition and toolbar work; that scope and its unverified browser evidence remain
separate. No root or BackOffice-local PRODUCT.md or DESIGN.md was found in the
targeted context search. Existing components and Bootstrap styling are the incumbent
authority; this is not justification for a new visual system.

## Design and Preserved Boundaries

### 1. One primary save group, explicit publication semantics

- For `pages.manage`, show a primary **Save** action and adjacent dropdown containing
  **Save as draft**. Explain beside the group that Save publishes the current changes.
  Keep Prettify and Preview secondary. Preserve a distinct Unpublish action.
- Save validates and captures the current canonical draft. On create, create the
  draft then publish using the returned ID and revision. On dirty edit, save then
  publish using the returned revision. A clean existing draft can publish directly.
  Do not simply rename the existing draft-save button or publish the stale revision.
- Check publication permission before the first write in the composite action.
  Existing server authorization remains authoritative for every operation.
- Save as draft never publishes or unpublishes. For an already-live article, display
  “Draft saved; live article unchanged.” Preserve the permanently locked slug.
- Non-publishers with appropriate create/update permission retain Save draft and
  an explanation of restricted publishing. Do not broaden RBAC to satisfy a layout request.
- Use one route-owned sequential workflow with existing service functions. No new
  backend endpoint is necessary for this scope. Return typed, phase-specific results:
  latest confirmed post plus success, partial success, or failure and failed phase.
  If publication fails after saving, say “Draft saved; publication failed,” retain
  ID/revision, and offer a deliberate publication retry.
- Disable duplicate submissions and conflicting editing/prettify/upload operations
  while a workflow runs. Bind the workflow to its captured channel; channel changes
  must not send later steps into a different channel. Stop and require recovery.
  Do not navigate after create until its intended sequence finishes; retain the
  created ID and reach its edit URL on success or known partial success.

This is a two-request workflow, not an atomic transaction. An atomic save-and-publish
API would add contracts, authorization, persistence, and compatibility work; defer it
unless product requirements demand all-or-nothing publication.

### 2. A visible, article-local Cover image box

- Show the box on both create and edit, near article metadata. Include an empty
  state, Upload image, existing-media thumbnails, selected-cover preview, Replace,
  Remove cover, and a labelled cover-alt field.
- Reuse `post.images` for selection and the block editor; do not introduce a separate
  media store or unrelated upload API. Remove cover only clears the draft reference;
  it does not delete media needed by published content.
- Give upload a distinct `pages.update`/`pages.manage` capability check. If a
  create-only user cannot upload, explain that restriction instead of showing an
  apparently usable control. Base create-versus-update save capability on the current
  persisted post, not permanently on the initial loader value.
- For an authorized user with a new or dirty article, Upload image explicitly saves
  a draft first, without publishing, then uploads with the returned ID/revision.
  State this side effect before activation: “Uploading saves your current draft.”
  Require valid title/slug and draft content; do not silently create an empty article
  or invent a title. Keep selected file/alt text available across this sequence.
- Collect image alt text with the upload, communicate supported browser formats and
  limits, and show upload progress/busy state, validation failures, and retry guidance.
  Server decoding and validation remain authoritative.
- After upload, reconcile confirmed post/revision/images without erasing local
  composition. Select the uploaded image as the cover and seed cover alt from its
  image alt text. Mark that selection as unsaved until Save or Save as draft succeeds.
  Selecting existing media also seeds alt text, editable independently afterward.
- If upload fails after draft save, explicitly report both outcomes and retain the
  saved revision and file for a deliberate retry. Do not blindly replay a create or
  upload after an ambiguous network failure.
- Show the selected cover in the existing unsaved article preview as well as the
  media box, so authors can verify it without publishing.

Scope stops at a visual library for this article. Global media search/reuse, media
deletion, crop tools, drag-and-drop, multi-upload, direct Blob uploads, and asset
lifecycle redesign are not prerequisites.

### 3. Reuse the video tag control without changing the blog contract

- Replace the plain text field with app-local `TagInput`, keeping canonical `string[]`.
  Support comma/Enter commits, blur, removal, keyboard interaction, and clear help.
- Pass the existing blog limits explicitly: 20 tags and 60 characters per tag.
  Matching interaction does not justify silently lowering valid blog tags to 32.
  Preserve existing loaded values; do not silently normalize old records on load.
- Reuse the existing normalizer for newly submitted tag lists: trim, remove empties,
  case-insensitive deduplication retaining the first display casing.
- Fix the tightly coupled batch-commit defect in the shared input by accumulating
  one validated next list per input event. Preserve video defaults and suggestions.
  Add an actual paste-event regression, including mixed duplicates and the limit.
- Prove that clicking Save immediately after typing an uncommitted tag includes that
  tag in the captured payload. Do not rely on an untested blur/state-update ordering.
  Reject invalid new tokens visibly rather than silently losing text.
- No new suggestions endpoint or tagging taxonomy is needed.

### 4. Put creation in the existing index-header action slot

Move the current Create article link into `PageHeader.actions`; retain its target,
icon, label, and `canCreate` gate. Remove the duplicate below the header. Do not
change the shared header for a blog-only placement fix.

### UX implementation lens

Use the installed Impeccable `shape` workflow during implementation confirmation,
with **Operate** mode targeting the blog index and create/edit form. It is a bounded
refinement of the current Bootstrap admin UI, not a redesign or public-blog restyle.
No Impeccable scripts or design-artifact generation were executed in this analysis.
Require desktop and narrow-mobile evidence for header placement, split-button
discoverability, media states, wrapped tag pills, dropdown clipping, focus order,
keyboard operation, error announcements, and visible busy/partial-success messages.

## Impacted Projects

- **frontend/back-office-spa:** implementation owner; article forms/index, reused
  local tag component, local tests. Video editing is a regression consumer of TagInput.
- **frontend/fe-packages/services:** reuse/verify only; existing transport functions
  are sufficient. No package API change.
- **frontend/fe-packages/models:** verify only; existing blog DTOs suffice.
- **frontend/fe-packages/layout:** verify only; preserve canonical BlogRenderer.
  Cover preview belongs in the admin form, not a global renderer change.
- **MorWalPizVideo.BackOffice, Domain, Models, MorWalPiz.Contracts:** verify only;
  keep current authorization, upload processing, revisions, persistence, and contracts.
- **MorWalPizVideo.BackOffice.Tests:** existing behavior suites are contract gates;
  no production backend change is proposed.
- **MorWalPizVideo.ServerAPI and public client:** verify published cover/draft isolation;
  no implementation changes expected.
- **Deployment:** admin SPA release only. AppHost, ShortLinks, Windows applications,
  other SPAs, frozen shop, and standalone tools have no implementation impact.

## Impacted Files

### Modify

- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx):
  save-group intents, sequential workflows, typed outcomes, permissions, cover box,
  preview cover, and TagInput integration.
- [form.test.tsx](../../frontend/back-office-spa/src/routes/blog/form.test.tsx):
  replace old two-step-only expectations and cover composite/partial-success flows.
- [index.tsx](../../frontend/back-office-spa/src/routes/blog/index.tsx):
  use the header actions slot.
- [TagInput/index.tsx](../../frontend/back-office-spa/src/components/TagInput/index.tsx):
  bounded batch-commit correction and any demonstrated submit-commit synchronization.
- [TagInput.test.tsx](../../frontend/back-office-spa/src/components/__tests__/TagInput.test.tsx):
  actual paste coverage and preservation of existing video behavior.

### Add

- A blog-index component test beside the existing blog-route tests, following the
  established test naming convention, for header placement and permission gating.
- A small article-local media component/test beside the blog form only if separating
  the media box is needed for clarity; do not create a shared media-library package.

### Generated/Migration

None. No schema, generated contract, lockfile, dependency, or data migration required.

### Documentation

- This handoff: update with accepted semantics, actual files, validation, and residual risks.
- [structured-blog.md](../architecture/structured-blog.md): after implementation,
  replace the manual save-before-upload/publication description with the sequential
  UX, partial-success behavior, and unchanged server boundary.
- A new ADR is not required for composition of existing operations. Reconsider only
  if atomic publication or shared media ownership is separately approved.

### Verify Only

- [PageHeader/index.tsx](../../frontend/back-office-spa/src/components/PageHeader/index.tsx):
  right-aligned slot, mobile overflow, and no global regression.
- [tagRules.ts](../../frontend/back-office-spa/src/components/TagInput/tagRules.ts) and
  [videos/edit/Component.tsx](../../frontend/back-office-spa/src/routes/videos/edit/Component.tsx):
  reuse normalizer and preserve video limits.
- [blogService.ts](../../frontend/fe-packages/services/src/blogService.ts),
  [blog.ts](../../frontend/fe-packages/models/src/blog.ts), and
  [transport.test.ts](../../frontend/back-office-spa/src/routes/blog/transport.test.ts):
  unchanged wire bodies, selected channel, expected revisions, and errors.
- [BlogPostsController.cs](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs),
  [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs),
  [BlogContracts.cs](../../MorWalPiz.Contracts/Contracts/BlogContracts.cs), and
  [BlogPost.cs](../../MorWalPizVideo.Models/Models/BlogPost.cs):
  existing server invariants.
- [BlogControllerTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs),
  [BlogServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs),
  [public blog](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx), and
  [public home](../../frontend/morwalpizvideo.client/src/routes/home/Component.tsx):
  published-only visibility and cover behavior.

## Impact Matrix

| Area | Decision |
| --- | --- |
| Backend/API | No new routes; compose existing create/save/publish/upload operations sequentially. |
| Frontend/UI | Local controls, media selection, status/errors, tags, header placement; no editor redesign. |
| Database/Data | Unchanged Mongo aggregate and owned image records; no backfill. |
| Contracts/Models | Existing wire types preserved; local typed workflow results only. |
| Authentication/Authorization | Keep shared cookie/CSRF/channel transport and server RBAC; distinguish create, update/upload, and publish. |
| Validation | Current metadata/content/image/ownership/revision checks remain authoritative; add matching client guidance. |
| Cache | Draft save/upload stay private at API projection; publish/unpublish keep existing Blog tag/reset invalidation and cache-warning behavior. |
| Configuration/Secrets | No configuration or secret additions. |
| Logging/Telemetry | Preserve server logging; show phase-specific client failures. No new telemetry subsystem. |
| Background Jobs | Not applicable; user-initiated synchronous workflows. |
| Deployment/Operations | Admin SPA release; no coordinated backend deploy needed. Browser acceptance required. |
| Backward Compatibility | API/data compatible; primary Save intentionally publishes, as confirmed by the user. |

## Reuse Opportunities

Reuse `PageHeader.actions`, app-local TagInput and tag normalizer, React Bootstrap
dropdown/button-group controls, existing Lucide icons, router action/fetcher patterns,
shared blog service functions, canonical snapshots, current image metadata,
BlogRenderer for body preview, app-store permissions, and existing mock/test fixtures.
Keep orchestration local to the article workflow, not in global app state.

## Risks

| Level | Cause and affected surface | Mitigation and verification signal |
| --- | --- | --- |
| High | Primary Save intentionally publishes the current draft. | User confirmed this workflow; the button has adjacent “Save publishes this article” copy and Save as draft remains explicit. Tests cover create-and-publish, update-and-publish, and draft-only save. |
| High | Save succeeds but publication/upload fails or times out. | Phase-specific outcomes, retained confirmed ID/revision, no blind replay; failure-injection tests prove no false success or duplicate create. |
| High | Older response or channel switch overwrites current content or misroutes a later write. | Serialize operations, freeze conflicting changes, capture channel, retain latest confirmed state; delayed-response and channel-change tests. |
| Medium | Create permission is mistaken for upload/update permission. | Separate capability gates, keep server checks; create-only and update-only negative tests. |
| Medium | Reusing video defaults truncates valid blog tags or batch paste loses tags. | Explicit blog limits, accumulator correction; 32/33/60/61 boundaries, 20/21 tags, actual paste and immediate-save tests. |
| Medium | Upload appears to publish the cover, or media is mistaken for private storage. | Show unsaved cover state and published-snapshot isolation. Public image URLs already exist; do not promise draft-file confidentiality or introduce new storage claims. |
| Medium | Public view is briefly stale after publication due to cache eviction failure. | Preserve existing eviction/log/header behavior and documented 300-second TTL; verify warning is not converted into “publication failed.” |
| Medium | New media layout or split dropdown clips on narrow screens. | Desktop/mobile browser evidence, keyboard focus and status-announcement checks; jsdom is not geometry proof. |
| Low | Scope expands into a global media library or unrelated Puck fixes. | Article-local ownership and explicit stop boundary; separate approval for expanded work. |

## Technical Debt

- Separate draft-save and publish endpoints require partial-success UX. Address locally
  now; atomic API design is deferred, not required.
- Blog and video tag length rules differ. Preserve existing compatibility; defer
  repository-wide taxonomy/validation alignment.
- Shared tag input's multi-segment stale-value behavior is tightly coupled to reuse;
  fix narrowly now with regression coverage, not a general component rewrite.
- Current response handling resets draft state after any successful fetcher response.
  Integrate multi-step results deliberately to prevent media/workflow state loss.
- Prior Puck handoff records incomplete real-browser validation and a host browser
  dependency failure. Treat this as a possible verification blocker, not proof the
  environment is still broken. Do not modify host dependencies without approval.

## Delivery Status

The four requested outcomes are implemented. The targeted admin build, 64-test
expanded regression run, targeted lint, formatting, and UI detector pass.

No new dependency, global library, schema change, public redesign, RBAC widening, or
unrelated Puck cleanup was introduced. A real-browser pass remains recommended for
desktop/mobile layout, dropdown clipping, keyboard/touch behavior, image upload and
public cover smoke. An independent Code Reviewer pass is optional and was not run.

## Testing Strategy

- **Unit/component, BackOffice Vitest:** comma/Enter/blur commit, removal and Backspace,
  actual multi-tag paste, case-insensitive duplicates, empty segments, tag limits and
  long existing tags; verify video defaults unchanged.
- **Form/route integration:** new create-then-publish; dirty save-then-publish; clean
  publish; draft-only save while live; permanently locked slug; returned revision used
  at each step; no subsequent mutation after failed prerequisite.
- **Failure/recovery:** save success plus publish/upload failure, 400/403/409, transport
  timeout, duplicate click, delayed response, conflicting prettify, and channel switch.
  Retain confirmed identity/revision and do not claim publication without confirmation.
- **Media:** visible create empty state; required fields explained; valid upload;
  invalid/oversized image and missing alt; existing cover selection/removal; seeded
  and edited cover alt; unsaved preview cover; body/media references survive updates.
- **Permissions:** publisher, create-only, update-only, no create/update, and server
  denial after UI eligibility. Creation placement remains permission-gated.
- **Contract/security behavior:** reuse blog transport tests for channel and exact
  revision bodies; existing backend BlogServiceTests/BlogControllerTests for ownership,
  foreign images, draft isolation, image validation, slug lock, and cache semantics.
  Expand tests only where a concrete missing gate is discovered.
- **End-to-end/browser:** on desktop and narrow mobile, create an article, add tags,
  upload and select cover, save draft, verify no new public article, then Save and
  verify published cover/title/body. Edit live content and save draft; live version
  must stay unchanged. Check header alignment, dropdown clipping, keyboard access,
  focus, alt labels, disabled explanations, and status announcements.
- **Migration tests:** not applicable; persistence and wire contracts are unchanged.
  Static inspection here is evidence of current behavior, not a passing test result.

## Migration And Rollback

No MongoDB, SQLite, API contract, auth, cache-key, or configuration migration is
required. Existing draft/public snapshots and image references remain valid, with no
backfill or dual-write window. Deploy the admin SPA against the current compatible
API after focused and browser gates. No new feature flag is required for this bounded
change; announce the changed primary Save semantics.

Roll back by restoring the prior SPA artifact. Existing posts and media remain
readable. A UI rollback does not undo already-published articles or uploaded files:
use existing controlled draft edits/publish/unpublish operations for content recovery.
Do not delete uploaded media or auto-unpublish as compensation for a failed sequence.
Existing first-publication slug locks remain permanent. Review partial-operation
messages, authorization/conflict failures, and existing server cache logs during rollout.

## Confirmed Decisions

1. The user confirmed primary **Save** publishes; **Save as draft** persists the draft
   without changing the live snapshot.
2. The media picker is article-local. Cross-article media reuse is not in scope.
