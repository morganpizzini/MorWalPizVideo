# Puck editor interaction and layout feedback

Status: implementation completed in the admin SPA. Focused editor, preview,
adapter, and renderer tests, TypeScript build-mode checking, and targeted ESLint
pass. Browser geometry and toolbar hit testing remain unverified.
Initial design evidence checked: 2026-11-05. That architecture-phase review was
static only; implementation checks are recorded below.
Approval recorded: 2026-11-05, following the user's approval of inline content
editing, contextual media settings, and a separate clean Preview toggle, with
the sidebar optional.

Implementation was completed in `Editor.tsx`, `Editor.css`, `Editor.test.tsx`,
`form.test.tsx`, and the shared Vitest setup; `form.tsx` and the canonical
adapters remain unchanged. The existing unsaved-draft Preview path is covered
at the form level. The blog-route suite passes all 28 tests after adding a
test-only `localStorage` implementation. Chromium is downloaded, but launching
it fails because the host is missing `libatk-1.0.so.0`; no Playwright test
configuration or browser tests exist in BackOffice yet.

## Outcome

Resolve three BackOffice article-composition problems:

1. The first block's clone/delete toolbar must remain visible and clickable.
2. Typing in the right-hand fields must preserve focus and caret across updates.
3. Newly inserted or incomplete blocks must have visible, selectable geometry,
   a small type label, and a subtle boundary against the white canvas.

Approved scope also changes the primary editing interaction: add a text/heading
block and edit its content directly on the canvas; configure media through a
small contextual panel attached to the block; inspect the current unsaved draft
using the existing clean Preview toggle. No required authoring task may depend
on opening the right sidebar.

This is a refinement, not a redesign. Preserve Puck selection,
dragging, history, field formatting, canonical documents, save/publish behavior,
and public article rendering. Changes belong to the admin editor only.

## Current Evidence

### Verified ownership and flow

- [Editor.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.tsx)
  owns Puck configuration, custom fields, Tiptap integration, and block previews.
  The manifest pins Puck 0.23.0 and Tiptap 3.31.4 in
  [package.json](../../frontend/back-office-spa/package.json).
- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx)
  passes canonical draft state to `BlogEditor`. Every editor update calls
  `setDraft`, rerendering the form and editor. The editor key depends on post
  identity/revision, not text, so ordinary typing does not intentionally change
  that key. For an unsaved article, the images fallback creates a new empty array
  each render.
- [adapter.ts](../../frontend/back-office-spa/src/routes/blog/adapter.ts)
  maps canonical blocks to Puck props and back, preserving Puck block IDs.
- `BlogEditor` rebuilds all component definitions, custom-field render
  functions, preview render functions, and the overrides object on every render.
  Each custom-field function closes over the current images.
- Installed Puck 0.23.0 `AutoFieldInternal` mounts the selected custom-field
  render function as a React component. Its render-map memo depends on overrides.
  Replacing these function identities can therefore replace the field subtree,
  destroying the active input/Tiptap instance. This is the leading source-backed
  focus-loss mechanism, not a browser-confirmed reproduction.
- [BlogRenderer.tsx](../../frontend/fe-packages/layout/src/components/BlogRenderer.tsx)
  intentionally renders no video for an absent/invalid YouTube ID, no image
  without a resolved image, no empty carousel, and no absent rich text.
  Empty headings, galleries, and columns also lack reliable content height.
  `BlogEditor` renders this public renderer directly without an editor frame.
  A default block supplies a type and ID, not completed content, so an inserted
  block can have no visible content or useful hit area.
- Installed Puck 0.23.0 exposes inline `contentEditable` field support, but its
  built-in custom-field transform handles string values, not the current
  object-valued canonical `block` field. Enabling that flag on the existing
  custom field alone cannot provide this interaction.
- Puck's public typed hooks expose `getItemById`, `getSelectorForId`, and
  `dispatch`; its public replace action supplies the integration needed to
  update a canonical block from an editor-local canvas component. No fork or
  private store import is required.
- The form already switches between `BlogEditor` and a clean `BlogRenderer`
  using local Preview state. The clean view consumes `draft.document`, not
  the last published snapshot, and does not require saving or publishing.

### Toolbar evidence and uncertainty

- The supplied screenshot shows the first block close to the preview's top edge.
  There is no editor-specific top gutter or root preview wrapper in `BlogEditor`.
- Installed Puck styles give the canvas scrolling overflow and its layout
  clipping overflow. Its action bar uses an above-block offset, and
  `syncActionsPosition` corrects negative browser-viewport coordinates, not every
  ancestor scrollport boundary. With iframe disabled, the browser viewport and
  editor canvas top are different boundaries.
- This supports clipping at the canvas boundary as the leading hypothesis.
  The exact clipping ancestor and toolbar rectangle must be measured in the
  browser before a layout fix is accepted; a stacking-order problem has not been
  established. Raising global z-index cannot reliably fix overflow clipping.

### Existing proof gaps

- [Editor.test.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.test.tsx)
  mounts real Puck/Tiptap and exercises link controls, but does not feed editor
  changes back through a stateful parent or type a multi-character sequence.
  It accounts for Puck's duplicate desktop/mobile fields and pending timers.
- [renderer.test.tsx](../../frontend/back-office-spa/src/routes/blog/renderer.test.tsx)
  checks canonical rendering, safe links, video filtering, columns, and carousel
  behavior. It does not prove browser geometry or toolbar hit testing.
- [vitest.config.ts](../../frontend/back-office-spa/vitest.config.ts)
  uses jsdom. No owning-workspace browser-test configuration was found.
- Relevant current documentation:
  [structured-blog.md](../architecture/structured-blog.md),
  [frontend.md](../architecture/frontend.md), and
  [testing.md](../architecture/testing.md).

## Proposed Design

### Stable editor component identity

Keep Puck configuration, custom-field component types, preview component types,
and overrides stable across document updates. Define these at module scope.
Supply current image records through an editor-local React context with a typed
provider in `BlogEditor`; stable render components read that context rather than
capturing images in newly created functions. Keep the existing public props.

Do not merely memoize configuration by `document`, callbacks, or an unstable
images array: these dependencies can recreate the same failure. Do not copy
images into stale configuration or ignore successful uploads. Keep the normal
canonical change flow; no debouncing, blur-only commits, forced focus,
event suppression, timeout-based refocusing, or changing keys per edit.
Preserve the intentional identity/revision reset in the parent form.

Keep `TextEditor` content reconciliation non-emitting and equality-guarded.
Reuse that reconciliation in both inline and optional sidebar text surfaces.
Only change it further if the stateful regression still fails after component
identities are stable.

### Approved inline authoring and contextual settings

Retain the canonical object-valued block field and existing adapters. Do not
flatten all block content into new Puck fields or switch persisted rich text to
Puck's native rich-text format. Instead, use stable editor-local canvas
components to reuse the existing restricted Tiptap editor and native heading
input, styled to read as article text rather than a sidebar form.
Non-text content continues to use `BlogRenderer`.

Use the public typed Puck hook and replace action for all canvas and contextual
updates. Resolve the current item and selector by the Puck ID at update time,
merge the changed property into the latest canonical block, and preserve its
type and authoritative Puck ID. Do not capture positional indices across reorder
or clone operations, mutate history snapshots, or write directly to parent
draft state from a second independent editor state store.

The minimum interaction contract is:

- Inserting a rich-text or heading block gives a visible inline writing target.
  A click/tap in the content edits it directly; the frame header remains a
  reliable selection/drag target. An explicit insertion may focus the new text
  target once, but ordinary parent rerenders must never steal or reset focus.
- Rich text retains the existing allowed nodes, marks, HTTPS/safe-link rules,
  lists, and local undo/redo controls. Show a compact formatting toolbar for the
  active text block, without requiring the sidebar.
- Headings edit directly on the canvas, retaining their existing 2000-character
  input limit. Heading level is available in the block's contextual settings.
- A labeled Settings control opens a small in-flow contextual panel within the
  selected frame. Reuse the relevant existing field controls for video ID/title,
  image choice/caption, gallery/carousel image selection and ordering, heading
  level, and column count. Keep panel identity stable while typing.
- Every panel control has a label, keyboard access, and visible focus.
  Opening settings moves focus into the panel; closing explicitly or via Escape
  restores focus to its trigger. Do not dismiss on every field change.
  Panel width must fit the canvas at narrow sizes, not cover unrelated blocks.
- Media panels use the current owned-image collection. Upload remains in the
  existing form; do not introduce a new upload endpoint or silently bypass the
  current save-before-upload rule. Empty media offers a clear settings action.
- Column text children are also editable in place. Reuse the existing
  field-based add/move/remove and media settings controls in a contextual column
  panel. Child edits immutably update the latest enclosing top-level columns
  block by child ID. Do not introduce nested Puck zones or nested columns.
- Keep Puck's existing sidebar available as an optional alternate control
  surface, hidden initially using its public UI-state API. Inline editing and
  contextual controls must cover the existing authoring fields; toggling the
  sidebar must not affect data or reset the active canvas input.

Constrain event routing only where an active editor or settings control needs
to receive input instead of a drag gesture. Do not blanket-disable Puck overlays,
pointer events, selection, or keyboard shortcuts. Formatting shortcuts apply to
the focused text editor; Puck block history remains available for composition
changes. Browser checks must prove both histories remain usable.

### Clean preview and returning to edit

Reuse the existing form-level Preview toggle and shared renderer. Preview
contains no block frames, settings panels, inline inputs, formatting toolbars,
Puck controls, or placeholder labels. It reflects the latest unsaved canonical
document immediately, including the final character typed before the toggle.
Persist changes through Puck on update, not only on blur or panel close.

The current form unmounts Puck when previewing. Preserve this simple lifecycle:
returning to edit must restore document content and media, but preserving Puck
selection, caret, open panels, or its undo stack across a Preview round trip is
not promised. Within uninterrupted editing, history and focus must remain intact.
Do not add a hidden mounted editor or a second preview state store to extend
that guarantee in this task.

### Editor-only block frame

Wrap each top-level block preview in one stable, admin-only frame containing:

- A compact human-readable block-type label; keep the label noninteractive
  while exposing a separately labeled Settings button where applicable.
- A subtle neutral outline/border and distinct light header surface, with a
  stronger visible state supplied by Puck's existing selection treatment.
- A minimum total height of 72 CSS pixels for empty/incomplete blocks, including
  the header. Populated content grows naturally without fixed-height clipping.
- Brief type-specific setup guidance for incomplete media, rather than a blank
  white area. Invalid or missing media must not create an unsafe embed.

Keep the frame inside Puck's existing selectable block geometry. Clicking its
header or empty body must select that block using Puck's own selection mechanism.
Do not introduce a second custom drag/delete/clone system or nested button-like
semantics. Preserve Puck keyboard/outline selection.

Continue using `BlogRenderer` for populated non-text content and the clean
preview; canvas text uses the inline editors described above. Do not put labels, empty
states, borders, or minimum heights in the shared public renderer or canonical
document. Do not change the renderer's defensive media filtering.
Column children retain their canonical ownership and existing add/move/remove
controls, with inline text and contextual settings as described above; do not
introduce nested Puck zones or a new Puck column-selection model.

### First-block toolbar clearance

Use the supported, stable Puck preview override to add an editor-only internal
canvas gutter around its existing children, not a replacement preview renderer.
Start with 48 CSS pixels of top clearance and 16 pixels inline; the acceptance
criterion is the actual toolbar rectangle and clickability, not the padding value.
Preserve canvas scrolling, Puck overlays, and all preview children.

Verify the first block at the canvas's topmost scroll position. If the measured
toolbar is still clipped, adjust clearance or the editor-local integration
against the identified clipping boundary before delivery. Do not disable overflow
throughout Puck or depend on hashed package class names. No package upgrade or
library patch is part of the planned solution.

### Impeccable implementation brief

Surface: BackOffice blog composition canvas, contextual settings, optional right
fields, and clean preview; mode: **Operate**.
This is a narrow refinement using the existing Puck chrome, Bootstrap utilities,
Lucide controls, typography, and neutral/blue selection conventions.
No BackOffice-local `PRODUCT.md` or `DESIGN.md` was found in the frontend tree;
the incumbent source and supplied screenshot are the visual baseline.

During implementation, use the installed Impeccable `harden` workflow for
interaction/empty-state reliability, with focused layout checks. This architect
handoff does not execute the UI workflow. Capture desktop and narrow/mobile
states in one inspection batch, apply one coherent correction batch if needed,
then confirm. Do not create a replacement design system or redesign the shell.

## Impacted Projects

- `frontend/back-office-spa`: editor component stability, canvas affordances,
  inline authoring, contextual settings, preview regression coverage, focused
  tests, and owning-workspace build/type validation.
- `frontend/fe-packages/layout`: verify-only; shared renderer must stay unchanged.
- `frontend/fe-packages/models` and `frontend/fe-packages/services`: verify-only;
  no shared schema, endpoint, transport, or dependency changes.
- `frontend/morwalpizvideo.client`: public renderer consumer; verify compatibility
  through unchanged rendering, not new public editor UI.
- .NET projects, other SPAs, standalone tools, AppHost, and deployment workflows:
  no changes required. The existing admin artifact is the sole rollout surface.

## Impacted Files

### Modify

- [Editor.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.tsx):
  stabilize render identities; provide current images; integrate inline editing
  and Puck updates; add contextual settings, local frames, and gutter.
- [Editor.css](../../frontend/back-office-spa/src/routes/blog/Editor.css):
  scope frame, inline text, contextual panel, and gutter styling to the editor.
- [Editor.test.tsx](../../frontend/back-office-spa/src/routes/blog/Editor.test.tsx):
  controlled-parent typing, inline editing, contextual controls, identity,
  selection, history, and frame regressions.
- [setup.ts](../../frontend/back-office-spa/src/test/setup.ts):
  deterministic test-only localStorage for jsdom environments.
- [form.test.tsx](../../frontend/back-office-spa/src/routes/blog/form.test.tsx):
  verify clean preview uses unsaved edits and survives returning to edit;
  preserve save, permission, and revision assertions.

### Add

None.

### Generated/Migration

None. No generated assets, data migrations, or lockfile changes are expected.

### Documentation

- [structured-blog.md](../architecture/structured-blog.md): describe editor-only
  affordances and identity-preservation behavior after implementation.
- This handoff: record actual files, checks, browser evidence, and residual risks.
  No new ADR is warranted because ownership and external contracts are unchanged.

### Verify Only

- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx): parent
  feedback and intentional revision resets, including new-post empty images.
- [adapter.ts](../../frontend/back-office-spa/src/routes/blog/adapter.ts) and
  [adapter.test.ts](../../frontend/back-office-spa/src/routes/blog/adapter.test.ts):
  stable canonical IDs, order, and round trips.
- [renderer.test.tsx](../../frontend/back-office-spa/src/routes/blog/renderer.test.tsx)
  and [BlogRenderer.tsx](../../frontend/fe-packages/layout/src/components/BlogRenderer.tsx):
  no public chrome or altered unsafe-media behavior.
- [package.json](../../frontend/back-office-spa/package.json) and
  [vitest.config.ts](../../frontend/back-office-spa/vitest.config.ts):
  use existing runner and validation tooling; no dependency changes.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | None; existing save/upload/publish endpoints unchanged. |
| Frontend/UI | Admin-only inline content editing, contextual media settings, optional sidebar, selectable empty frames, toolbar clearance, and clean draft preview. |
| Database/Data | None; canonical documents remain unchanged. |
| Contracts/Models | None; labels and setup guidance are presentation only. |
| Authentication/Authorization | None; existing route permissions and server authority retained. |
| Validation | Existing field/server rules and safe link/video filtering retained. |
| Cache | None; no tags, public content, or eviction paths change. |
| Configuration/Secrets | None; retain iframe-disabled integration and dependency versions. |
| Logging/Telemetry | No new pipeline; investigate browser errors rather than suppress them. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Redeploy existing admin SPA only. |
| Backward Compatibility | Existing drafts and published snapshots require no conversion. |

## Reuse Opportunities

- Reuse `BlockFields`, `TextEditor`, canonical adapters, and `BlogRenderer`.
- Reuse public typed Puck item/selector lookup and replace dispatch for inline
  updates; no private store APIs or library patches.
- Reuse Puck preview override, selection, overlays, outline, history, and actions.
- Reuse Bootstrap utilities and incumbent Puck neutral/selected styling.
- Extend the existing real-library editor test and its timer cleanup; target
  visible desktop fields rather than the hidden mobile instance.
- Use existing Vitest/Testing Library and user-event dependencies. Do not install
  a browser framework solely to assert layout that can be checked in a browser.

## Risks

| Level | Cause / affected surface | Mitigation | Verification signal |
| --- | --- | --- | --- |
| High | Changed field identities destroy input/Tiptap state during parent updates. | Stable component/config/override identities; current images via context. | Multi-character input retains the same DOM node, focus, selection, and final canonical text. |
| High | Inline updates target a stale index or overwrite another field after reorder/clone. | Resolve latest block/selector by authoritative Puck ID and merge immutable updates. | Edit, reorder, clone, then edit again; only the intended block changes and IDs remain unique. |
| Medium | Puck overlays or drag handling prevent typing and contextual input. | Keep an explicit frame selection target and narrow editor interaction routing. | Pointer caret placement, text selection, IME composition, settings typing, and header dragging all work. |
| Medium | Toolbar coordinates are viewport-based while canvas clips locally. | Internal clearance; measure clipping ancestors before accepting. | All toolbar controls are inside the usable canvas and pointer hit testing reaches them. |
| Medium | Context/config stabilization captures stale media. | Context always supplies latest images; no frozen captured arrays. | Updated image props render and remain selectable without remounting text fields. |
| Medium | Frames interfere with drag, media overlays, or keyboard selection. | Noninteractive labels; keep Puck's event ownership and geometry. | Header/body click, outline selection, reorder, clone/delete, and undo/redo work. |
| Medium | Editor chrome leaks into saved/public content. | Keep frames local to editor render functions. | Canonical round trip and public markup unchanged; preview contains no frame labels. |
| Low | Empty/invalid media becomes an accidental external request. | Retain shared renderer filtering; placeholder is local text only. | Invalid video creates no iframe; unsafe links remain disabled/filtered. |
| Low | Rollout affects existing drafts unexpectedly. | Admin-only artifact; no data transition. | Existing drafts reopen/save with unchanged IDs and content. |

## Technical Debt

- Current editor tests prove initial mounting rather than realistic controlled
  editing. Address this proof gap now because it directly explains the escape.
- Puck's desktop/mobile field duplication and jsdom geometry limitations remain;
  isolate tests to the intended panel and require browser geometry evidence.
- Column children currently use field-based editing, not nested Puck selection.
  Extend them with inline text while preserving that ownership boundary; a
  nested Puck canvas redesign is separate work.
- The current Preview round trip unmounts Puck and resets transient editor
  state/history. Preserve and document that lifecycle; history continuity across
  Preview is deferred, not silently promised by the new inline UI.
- No shared-renderer cleanup, dependency upgrade, auth change, or broader styling
  consolidation belongs in this task.

## Implementation Roadmap

1. **Establish failing interaction proof.** Extend the owning editor test with a
   stateful parent that immediately feeds canonical changes back. Cover new and
   existing articles, including freshly allocated empty image arrays. Confirm
   the focus/identity failure before changing editor behavior.
2. **Stabilize component ownership.** Change only editor configuration/render
   identity and live-image delivery. Keep adapters and parent state flow.
   Gate: multi-character native inputs and Tiptap retain focus, caret, and text;
   switching block selection still shows the correct field value.
3. **Integrate inline text updates.** Reuse restricted Tiptap and heading
   controls in stable canvas components; commit through public Puck lookup and
   replace actions, including column-child updates. Gate: text is editable
   without the sidebar, canonical content survives reorder/clone and ordinary
   rerenders, and typing/formatting/history stay usable.
4. **Add block frames, contextual settings, and canvas clearance.** Implement
   the specified headers, settings panels, empty geometry, setup guidance, and
   stable preview wrapper. Keep the sidebar optional. Gate: every existing
   authoring field is reachable without it; all seven empty block types are
   discoverable/selectable; populated non-text content uses the shared renderer.
5. **Verify preview integration.** Reuse the current toggle and shared renderer.
   Extend form tests with an editor stand-in that emits canonical edits.
   Gate: unsaved final-character edits appear in clean preview with no editor
   chrome, and returning to edit restores content without saving/publishing.
6. **Verify real geometry and interactions.** Inspect clipping ancestors and
   first-block toolbar rectangles in the browser, then check all actions and
   scroll positions. Cover desktop and narrow viewports. Adjust the coherent
   layout batch once if required; do not accept jsdom as geometry proof.
7. **Run focused regression and owning-workspace gates.** Run editor, adapter,
   renderer, and form tests; run relevant lint and the admin TypeScript/build
   checks using existing scripts. Build shared prerequisites only as required
   by the repository's existing workspace order. No backend tests are needed.
8. **Update documentation and deliver evidence.** Update the feature document
   and this handoff with actual changes, results, screenshots, and unresolved
   release risks. Stop when the three reported issues, approved inline/contextual
   interactions, clean preview, and preservation checks pass; broader editor
   features require separate approval.

No contract, model, persistence, observability, or service changes precede these
steps. An independent post-validation review is recommended for the expanded
inline-authoring scope because it combines Puck state/history, Tiptap focus, and
canonical updates. The user may select MorWalPiz Code Reviewer after validation;
review execution is not assigned to the architect.

## Testing Strategy

- **Unit/contract:** preserve adapter round trips, unique IDs, order, safe links,
  invalid-video filtering, and existing deterministic renderer comparisons.
- **Component/integration:** real Puck/Tiptap with a stateful parent; type a
  sentence character by character into rich text and headings, edit in the
  middle, and assert focus/caret and canonical content. Also cover video title,
  video ID, link URL local state, and a rich-text child within columns.
- Assert the active contenteditable/input node is not replaced after ordinary
  updates. Exercise formatting and undo/redo, and ensure the final state has
  all characters rather than only testing that callbacks fired.
- Repeat typing with the sidebar closed. Cover inline caret placement, selecting
  existing text, replacement, paste, and IME composition; preserve the restricted
  text model. Contextual media/title/heading-level/column controls must work
  without the sidebar and have explicit keyboard close/focus restoration.
- After reorder, clone, and deletion, edit another block and verify lookup by ID
  updates only that block. Cover child text edits within columns and preserve
  sibling children when changing one field.
- Test editing, opening Preview immediately, inspecting the latest unsaved
  canonical text/media, and returning to edit. Assert no frames, placeholders,
  inputs, or settings controls in clean preview, and no save/publish API call.
  Do not assert undo-stack or caret restoration across the intentional unmount.
- Test replacing image props and transitioning from no images to uploaded
  images without stale previews. Selecting another block must update fields
  without editing the previous block.
- For all seven types, test an initially empty frame with an accessible text
  label and selectable root; test populated/empty transitions and missing owned
  media references. For video, insert it, select a text block, then reselect the
  video by clicking its visible header/body.
- **Browser/end-to-end:** first block at scroll top and after scrolling back;
  hover and select; clone/delete controls visible and actually clickable;
  reorder and undo/redo; long text and populated video/carousel frames; confirm
  selection overlays do not swallow fields or toolbar controls.
- Measure empty frame height at least 72 CSS pixels. Measure action controls
  inside the visible canvas bounds and use pointer hit testing, not screenshots
  alone, to confirm no sibling/ancestor covers them.
- Capture before/after desktop evidence near the supplied screenshot size and
  narrow/mobile evidence at 375 CSS pixels. Verify sidebar transitions,
  keyboard/outline selection, visible focus, readable labels, and 200% zoom.
  Neutral text must meet normal text contrast requirements; selection cannot be
  conveyed by color alone.
- **Security:** retain existing unsafe-link/unsupported-video tests; no new
  executable embed or persisted HTML path.
- **Migration:** not applicable; data and APIs are unchanged.
- **Verification status:** all blog-route tests pass (27 tests in 17.08
  seconds); `tsc -b --pretty false` and targeted ESLint pass. Chromium launch
  is blocked by missing host library `libatk-1.0.so.0`; no BackOffice
  Playwright tests/configuration exist yet. Browser toolbar geometry, hit
  testing, desktop/narrow screenshots, and history/drag interactions remain
  unverified.

## Implementation and Verification Results

- `Editor.tsx` now keeps Puck config/render identities stable, reads live images
  from editor-local context, edits text and headings inline, updates blocks and
  column children through current Puck IDs and public replace dispatch, and
  exposes contextual settings with focus restoration.
- `Editor.css` adds editor-only selectable frames, empty-state guidance, text
  affordances, contextual panel styling, and a 48px top / 16px inline canvas
  gutter. It selectively re-enables pointer events and text selection for inline
  editors and settings controls because Puck disables block descendants by
  default. The shared renderer and persisted document shape are unchanged.
- `Editor.test.tsx` covers pointer-driven heading typing, rich-text focus,
  settings clicks and typing, controlled-parent updates, media selection, empty
  frames, column-child updates, invalid video handling, and settings focus.
  `form.test.tsx` covers unsaved Preview and returning to edit without saving.
- The shared Vitest `setup.ts` provides a deterministic in-memory `localStorage`
  implementation so service initialization works even when the jsdom/Node
  environment does not expose storage.
- Passed:
  - `yarn --cwd frontend/back-office-spa vitest run src/routes/blog --silent` — 7 files, 28 tests.
  - `yarn --cwd frontend/back-office-spa tsc -b --pretty false`.
  - `yarn --cwd frontend/back-office-spa eslint src/routes/blog/Editor.test.tsx`.
  - `yarn --cwd frontend/back-office-spa eslint src/test/setup.ts`.
- The editor test suite confirms pointer-driven heading and settings input,
  plus rich-text focus. Real-browser rich-text typing, drag, and layout checks
  remain unverified due to the host's missing Chromium system dependency.
- The pointer test disables user-event's CSS hit-test check because jsdom sees
  Puck's injected drag styles but does not apply the editor stylesheet; it
  exercises event handling and content updates, not browser hit testing.
- Playwright 1.64.1 and browser binaries are present, but a headless Chromium
  launch fails because `libatk-1.0.so.0` is absent. Install Playwright's Linux
  browser dependencies on the host before attempting browser checks. The
  BackOffice workspace has no Playwright config/specs yet, so the first-block
  toolbar rectangle, canvas clipping ancestor, pointer hit testing, and
  narrow-screen layout still need a browser test/inspection before release.

## Migration And Rollback

No MongoDB, SQLite, API, authentication, cache, or configuration migration is
needed. Canonical blocks remain version 1 and editor affordances are never
persisted. There is no backfill, dual-write period, or new feature flag.

Deploy only the updated admin SPA after focused tests, build, and browser proof.
Existing BackOffice and public clients remain compatible in either order.
Watch for editor console errors, inability to type/select, stale image choices,
and unexpected draft differences. Roll back the admin artifact if necessary;
no database recovery is needed for editor-only chrome. Drafts genuinely edited
by users are still real saved content and must not be automatically reverted.

## Open Questions

None blocking the local design. The exact toolbar clipping ancestor remains a
required browser measurement, not permission to redesign Puck or alter global
overflow. If the local gutter fails the measurable checks, document that
evidence before expanding implementation beyond the editor integration.
