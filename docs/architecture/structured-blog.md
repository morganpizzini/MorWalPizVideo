# Structured Blog

Delivery verification: completion of the 2026-09-30 handoff, with the final
ever-published slug UI repair requested on 2026-10-01.
**Implemented in the worktree, not declared deployed or fully release-gated.**
The repository expert was consulted by the parent implementation session; consultation
is not an outstanding blocker. Current source and executable checks remain authoritative.

## Ownership and Routes

Blog is separate from Page HTML and ChannelNews. `BlogPost` lives in Models;
`BlogService`, Mongo/mock repositories and validation live in Domain; shared responses
and requests live in `MorWalPiz.Contracts/Contracts/BlogContracts.cs`. Each API registers
the service and its existing `EnableMock` repository branch without API-to-API references.

BackOffice owns `/api/blogposts`. All operations require the existing authorization
and channel-scope pipeline. ChannelContext supplies ownership from `X-Channel-Id`;
request bodies cannot choose the channel. Shared transport recognizes `/api/blogposts`
as channel-scoped, using the existing channel-header/token provider and credentials flow.

| Method | Route | Permission (or `pages.manage`) | Result |
| --- | --- | --- | --- |
| GET | `/api/blogposts?page=1` | `pages.view` | Up to 50 admin posts; page 1-10000. |
| GET | `/api/blogposts/{id}` | `pages.view` | Owned draft, revision, publication state, images. |
| POST | `/api/blogposts` | `pages.create` | Create draft; 201 with location. |
| PUT | `/api/blogposts/{id}` | `pages.update` | Save `{slug, revision, draft}`. |
| POST | `/api/blogposts/{id}/publish` | `pages.manage` only | Publish stored revision. |
| POST | `/api/blogposts/{id}/unpublish` | `pages.manage` only | Remove public snapshot. |
| DELETE | `/api/blogposts/{id}?revision=...` | `pages.delete` | Delete aggregate; 204. |
| POST | `/api/blogposts/{id}/images` | `pages.update` | Multipart `file`, `revision`, `altText`; updated admin response. |

Validation errors return 400; revision/slug conflicts return 409; missing or foreign-owned
posts return 404. Existing authorization determines 401/403 responses.

ServerAPI exposes anonymous published-only `GET /api/blog?page=1&pageSize=12` and
`GET /api/blog/{slug}`. Pages are limited to 1-10000 and page sizes to 1-50; list response
is `{items, page, pageSize, hasMore}`. Public ownership resolves configured
`YouTubeChannelId` through `IContentService.GetChannelByIdAsync` to `YTChannel.ChannelId`.
It is not treated as a Mongo ObjectId. Missing configuration/channel logs an error and
returns 404; a visitor cannot override the channel. Reads exclude Draft at repository
projection and responses expose only published metadata/body and referenced image records,
not the draft, revision or an explicit channel field. Image IDs currently use storage keys;
they must be treated as opaque post-owned references, not arbitrary upload URLs.

## Schema and Publication

Mongo collection is `blogPosts`. The aggregate has `ChannelId`, channel-unique `Slug`,
`Revision` (starting at 1), `Draft`, optional `Published`, `FirstPublishedAt`, `PublishedAt`,
creation time and owned image metadata. Each snapshot includes title, summary, author,
tags, cover image ID/alt, SEO title/description, document and updated time.

Canonical JSON is independent of Puck/Tiptap storage formats:

```json
{
  "version": 1,
  "blocks": [
    { "id": "intro", "type": "richText", "richText": {
      "type": "doc", "content": [{ "type": "paragraph", "content": [
        { "type": "text", "text": "Article introduction" }
      ] }]
    } },
    { "id": "section", "type": "heading", "text": "Section", "level": 2 }
  ]
}
```

Block types are `richText`, `heading`, `image`, `gallery`, `carousel`, `video`, `columns`.
IDs/order survive the adapters. Headings are H2-H4; images reference owned `imageIds`;
videos accept only an 11-character YouTube ID, not arbitrary iframe/embed URLs. Columns
contain 1-3 block arrays with no nested columns. Limits include 100 blocks, unique
1-80-character IDs, 20 images per gallery/carousel, rich-text depth 12, 200 children per
node, 20000 characters per text node and a 256000-byte serialized snapshot ceiling.
The regular JSON request limit is 512000 bytes.

Rich-text nodes allow doc, paragraph, text, lists/listItem, blockquote and hardBreak;
marks allow bold, italic, strike, code and safe links. No raw HTML, arbitrary CSS or
executable embeds are rendered. The server validates content and owned references; the
React renderer escapes text and defensively filters link/video destinations.

Slugs trim whitespace/slashes and lowercase invariant; valid ASCII slugs are 1-120
characters. Creation can derive one from title; saving a title does not regenerate it.
Once ever published, the slug cannot change, including after unpublication. Save, image
upload, publish, unpublish and deletion use an expected revision. Mongo replacement/deletion
filters by channel, ID and revision atomically. Publish copies the stored draft into
Published; later draft saves leave that snapshot unchanged. Unpublish clears Published
but retains first-publication history and media.

The admin response additively exposes nullable `firstPublishedAt` directly from the
aggregate's `FirstPublishedAt`; it is not inferred from current publication state.
The existing response constructor, save request and public response shapes are unchanged.
Deploy the compatible API projection before the updated admin client; no data migration
is required.

## Editor and Public Rendering

Admin routes are `/blogposts`, `/blogposts/create`, `/blogposts/:id/edit`, guarded with
the existing Pages permissions. Blog is an admin menu entry, not a new permission family.
The editor uses Puck 0.23.0 custom fields and Tiptap 3.31.4 with React 19, canonical
adapters, metadata fields, image upload/selection/order, composition and shared preview.
Dirty drafts must be saved before publication, unpublication or upload. Publication
controls require `pages.manage`; server checks remain authoritative. Revision conflicts
are surfaced without silently overwriting content. The slug input is read-only whenever
the admin response has `firstPublishedAt`, including after unpublish and reopen. Save
retains the unchanged slug; never-published drafts remain editable. Publish/unpublish
responses update the same history-driven lock. The server's permanent slug enforcement
is unchanged and remains authoritative.

Puck mounts both its mobile Fields plugin and desktop sidebar, hiding the inactive
surface via CSS. The production mounting test scopes actions to the desktop sidebar,
not an arbitrary first matching button. Test teardown drains Puck's canvas-loader timer.
The Puck jsdom geometry warning (`NaN` top) remains; it is not browser visual evidence.

Shared models, transport and `BlogRenderer` are exported from the existing models,
services and layout packages. Public routes are `/blog` and `/blog/:slug`; the existing
public shell is retained. Columns stack on mobile; the carousel has named buttons,
arrow-key navigation, a live counter and no autoplay. Preview and public use the same
React renderer. Editor dependencies are admin-only, not imported into public rendering.
Public SSR tests cover published body, absolute cover image, canonical URL and Article
JSON-LD; pagination validation, empty list and missing/unpublished 404 are covered too.

Navigation remains manual: configure an `Internal` link with target `/blog`. There is no
new navigation type or automatic entry. Page URL uniqueness and Page/ChannelNews routes,
including ShootingIta channel-news behavior, are unchanged.

## Media, Cache and Mongo Rollout

Images use `BlobStorage:PageContainerName` under `blog/{channelId}/{postId}/...`.
Uploads require nonempty alt text (maximum 300 characters), at most 100 images per post,
maximum 10000000 input bytes, a single frame and at most 25 megapixels. The existing
ChannelNews media processor auto-orients/re-encodes/resizes; the endpoint limit is
12000000 bytes. Failed metadata persistence attempts to roll back the new blob.

Draft changes never delete images referenced by the published snapshot. Retention is
deliberately conservative: there is no individual image-delete endpoint, no garbage
collector, and aggregate deletion currently retains blobs too. Operators must not delete
media on draft-reference removal. Durable orphan cleanup/retry is not implemented; a
rollback delete failure needs operational recovery. Real HTTP multipart upload, blob
rollback and production storage permissions remain unverified in this continuation.

Public cache TTL is 300 seconds with lowercase `ApiTagCacheKeys.Blog = "tag-blog"`;
list varies by page/pageSize and detail by slug. Publish/unpublish/delete invoke existing
cross-API purge (`tag-blog`) and reset (`CacheKeys.Blog = "blog"`). Each eviction waits
at most five seconds. Failures after persistence are logged and return
`X-Blog-Cache-Warning`, not a fictitious failed write; bounded public staleness is 300
seconds. Draft save/upload do not invalidate the unchanged public snapshot.

Mongo repository creates the unique `blog_channel_slug_unique` ascending ChannelId+Slug
index before create/replace. This is lazy first-write provisioning, not the central Page
index audit/apply manifest or a startup migration. Runtime Mongo credentials need index
creation plus normal read/write privileges. Insufficient permissions or existing duplicate
channel/slugs block writes; do not disable uniqueness to bypass that failure.

Rollout sequence:

1. Resolve duplicate channel/slugs if any; back up data and confirm `blogPosts`, mapped
   index fields, runtime `createIndex` privileges and blob/container access in staging.
2. Create/save one internal draft through BackOffice against real Mongo before exposing
   navigation; confirm the named unique index and same-channel duplicate-slug conflict.
   Pre-provisioning with an operations identity is possible but must use the same driver
   field mapping/name/options. No destructive data migration is required.
3. Verify configured YouTubeChannelId resolves to the intended ChannelId, perform real
   multipart upload and concurrent revision tests, publish/edit draft/unpublish/delete,
   and check cross-API cache invalidation and published media retention.
4. Complete browser desktop/mobile content, keyboard, overflow and hydration checks,
   resolve the admin full-build blockers and assess the workspace audit before release.
5. Deploy compatible API/shared/client artifacts, then manually add the `/blog` link.
   Removing that link and unpublishing posts is the non-destructive visibility rollback.

## Verification Results

Commands are from the repository root. Results below are from this completion pass,
not a claim that all repository tests or production infrastructure passed.

| Command | Result |
| --- | --- |
| `yarn --cwd frontend/back-office-spa vitest run src/routes/blog/Editor.test.tsx` | 1 passed after scoped query and timer cleanup. |
| `yarn --cwd frontend/back-office-spa vitest run src/routes/blog` | Prior completion pass: 5 files, 17 tests passed; no unhandled errors. |
| `yarn --cwd frontend/morwalpizvideo.client test` | 1 file, 4 tests passed. |
| `npm --prefix scripts/blog-editor-feasibility run check` | Strict TypeScript declarations and 4 compatibility/SSR tests passed. |
| `yarn --cwd frontend build:shared` | Models, services, layout builds passed in dependency order. |
| `yarn --cwd frontend/back-office-spa build` | Failed: 8 errors outside blog, listed below. Full gate did not pass. |
| `yarn --cwd frontend/back-office-spa build-uncheck` | Vite bundle passed; editor chunk about 1025 KB (306 KB gzip), size warning. Not a typecheck substitute. |
| `yarn --cwd frontend/morwalpizvideo.client build:all` | Real `build:client` (tsc + Vite) and `build:ssr` passed. Runtime env-script and stale Browserslist warnings remain. |
| `yarn --cwd frontend/back-office-spa eslint src/routes/blog` | 0 errors, 3 Fast Refresh warnings. |
| `yarn --cwd frontend/morwalpizvideo.client eslint src/routes/blog src/utils/seo.tsx` | 0 errors, 2 Fast Refresh warnings. |
| `node .github/skills/impeccable/scripts/detect.mjs --json frontend/back-office-spa/src/routes/blog frontend/fe-packages/layout/src/components/BlogRenderer.tsx frontend/morwalpizvideo.client/src/routes/blog` | Empty findings. Not a browser/a11y certification. |

The eight admin errors were present at the continuation boundary and remain outside its
blog edit group. No unrelated files were repaired:

| File / line (under `frontend/back-office-spa/src`) | Exact diagnostic / count |
| --- | --- |
| `components/ToastNotification/index.test.tsx:33` | TS2322: `VitestUtils` not assignable to `Awaitable<void>` (1). |
| `routes/products/index/Component.behavior.test.tsx:41` | TS2352: Product fixture missing `creationDateTime` (1). |
| `routes/sponsors/edit/index.ts:1,2` | TS2300: duplicate identifier `Component` (2). |
| `routes/sponsors/form/action.test.ts:23,34,44` | TS2345: action args missing `url`, `pattern`, `context` (3). |
| `routes/sponsors/form/action.test.ts:50` | TS2339: property `mock` absent on `updateSponsorWithImage` function type (1). |

Blog test type errors were repaired; the redundant cross-app admin public-test copy was
removed. Public route coverage lives in the public app's own runner, avoiding separate
React Router contexts. No test symlink is used. The isolated probe is retained solely
for strict dependency declarations, adapter fixtures and React SSR/Tiptap compatibility,
not as a prototype application or a security boundary. It now has an authoritative npm
`package-lock.json` and Vitest 4.1.11; application Vitest is 3.2.6. Router manifests for
the affected admin/public apps use the patched 7.18.2 range. The workspace lockfile is
`frontend/yarn.lock`; do not run npm installation inside Yarn workspace apps.

Final dependency audits (no dependency changes after these commands):

| Command / lockfile | Info | Low | Moderate | High | Critical | Total |
| --- | --- | --- | --- | --- | --- | --- |
| `npm --prefix scripts/blog-editor-feasibility audit --json` / probe npm lock | 0 | 0 | 0 | 0 | 0 | 0 |
| `yarn --cwd frontend audit --json` / workspace Yarn lock | 0 | 2 | 18 | 26 | 0 | 46 |

The full-workspace counts are dependency-path advisory counts, not 46 distinct packages.
Affected packages: `react-router` 7.16.0 in ask.client and ShootingIta (high/moderate);
`browserslist` 4.28.1 (high), `baseline-browser-mapping` 2.10.10 (moderate) and
`serialize-javascript` 7.1.1 (low) in ShootingIta's PWA/Workbox build chain;
`qs` 6.15.2 via the public app's Express/body-parser chain (moderate).
No audit finding names Puck/Tiptap. The full app/workspace is **not audit-zero**;
public Express findings still require release risk assessment/remediation. Unrelated
workspace upgrades were not performed merely to obtain a green audit.

### Final Slug UI Repair

Only the admin response projection, shared admin type, blog form and their existing
tests changed in this final repair, plus this report. No service enforcement, persistence,
public contracts, dependencies or unrelated admin errors were changed.

The first substantive edit exposed `FirstPublishedAt` and extended the existing backend
publication regression; focused validation was attempted immediately. The editor test
runner did not discover the .NET test, so the filtered `dotnet test` command was used.
The subsequent backend run below reported an actual passing build and test count.
Immediately after the frontend edit, the focused form run passed all nine tests.

| Command | Final repair result |
| --- | --- |
| `yarn --cwd frontend/back-office-spa vitest run src/routes/blog/form.test.tsx` | 9 passed, including reopened-unpublished read-only slug with unchanged save payload, editable never-published draft and publish/unpublish response transitions. |
| `yarn --cwd frontend/back-office-spa vitest run src/routes/blog/form.test.tsx src/routes/blog/Editor.test.tsx` | 2 files, 10 tests passed. Existing Puck jsdom `NaN` top warning remains. |
| `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter 'FullyQualifiedName~BlogServiceTests\|FullyQualifiedName~BlogControllerTests' --verbosity normal` | Build passed; 21 tests passed, 0 failed/skipped. Projection regression checks null history for a draft and retained history after unpublished reopening. Mock/TestServer evidence, not real Mongo or multipart upload verification. |
| `yarn --cwd frontend build:shared` | Models, services and layout passed in dependency order. |
| `yarn --cwd frontend/morwalpizvideo.client build:all` | Client typecheck/Vite and SSR passed; existing runtime env-script and stale Browserslist warnings remain. |
| `yarn --cwd frontend/back-office-spa eslint src/routes/blog/form.tsx src/routes/blog/form.test.tsx` | 0 errors, 2 existing Fast Refresh warnings. |
| `node .github/skills/impeccable/scripts/detect.mjs --json frontend/back-office-spa/src/routes/blog/form.tsx` | Empty findings; not browser verification. |

The eight documented outside-blog admin typecheck blockers and 46 workspace audit
paths above remain inherited release findings; full admin typecheck and dependency
audits were not rerun or repaired in this narrow pass. None of those audit findings
names Puck/Tiptap; the public `qs` moderate finding remains. The admin bundle size warning
is retained, not cleared by these targeted tests. Blobs remain conservatively retained.
Real Mongo, HTTP multipart uploads and browser verification remain unverified.

Local dev servers were started with `dev --host 127.0.0.1 --port ... --strictPort`:
public `http://127.0.0.1:5187/blog`, admin `http://127.0.0.1:5188/blogposts`.
`Invoke-WebRequest` verified HTTP 200 and app mount HTML at both routes. These are Vite
shell checks from the earlier pass only; both existing dev servers were left untouched
in the final repair. Usable data/auth require the configured running APIs. No browser tool
was exposed, and local Playwright/Puppeteer were unavailable. Desktop/mobile screenshots,
nonblank rendered article, interactive editor/media, keyboard/overflow and hydration
acceptance are explicitly unverified. No screenshot evidence was fabricated.

Impeccable found incumbent UI but no PRODUCT.md, DESIGN.md or matching surface brief;
no unrelated design documents were introduced. Unrelated user changes were preserved;
no commits or branches were created.