# Blog Editorial Revision Standardization Handoff

## Outcome

Preserve a user-visible article revision number with standardized editorial semantics:

- The first persisted draft or public article is editorial revision `0`.
- Every later successful explicit **Save** or **Save as draft** advances the editorial revision by exactly one.
- Image upload/deletion, deletion recovery, publish-state-only actions, cache work, and other internal aggregate writes do not advance the editorial revision.
- The existing aggregate `Revision` remains a separate, non-visible optimistic-concurrency token and continues advancing on every persisted aggregate mutation.

This plan assumes that “revision” means the sequence of successful editorial saves, not retained historical snapshots. Comparison, restoration, and full revision history remain outside scope.

Accepted compatibility decisions:

- Existing articles keep their current displayed aggregate revision as the initial editorial-revision baseline. Their visible number does not reset during rollout.
- If image upload creates an article before its first manual Save, creation establishes editorial revision 0 and that later explicit Save advances it to revision 1.

## Current Evidence

- `MorWalPizVideo.Models/Models/BlogPost.cs` currently has one `Revision` field initialized to 1.
- `MorWalPizVideo.Domain/BlogService.cs` uses that field as an optimistic-concurrency token. Its private replacement path validates the expected value, persists `revision + 1`, and is shared by:
  - draft save;
  - save-and-publish;
  - publish and unpublish;
  - image metadata upload;
  - image-deletion reservation;
  - image-deletion finalization.
- `MorWalPizVideo.Domain/BlogService.cs` creates draft and public articles at aggregate revision 1 without using the replacement path.
- `MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs` returns that aggregate token through `BlogPostResponse` and accepts it on every revision-aware mutation.
- `frontend/back-office-spa/src/routes/blog/form.tsx` passes the token through hidden request data and uses server-returned values for subsequent saves, uploads, publication changes, and image deletion.
- The same form displays the token as `Revision` in the editor status and operation-success message.
- `frontend/back-office-spa/src/routes/blog/index.tsx` displays it in a `Revision` column.
- The new/dirty cover workflow in `frontend/back-office-spa/src/routes/blog/form.tsx` may create or save an article, then upload image metadata, then require another save to apply cover selection. Those valid aggregate writes explain visible jumps.
- `MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs` verifies that create-and-publish currently returns aggregate revision 1 and the next save-and-publish returns 2.
- `MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs` verifies aggregate compare-and-swap behavior and one aggregate revision increment per replacement.
- Existing persisted aggregate revisions cannot be converted into exact historical editorial-save counts because aggregate revisions also include media and publication-state writes.

## Proposed Design

### Separate the two meanings

Keep the current `BlogPost.Revision` as the aggregate concurrency token. Do not change its initialization, compare-and-swap behavior, request validation, or increment rules.

Add `BlogPost.EditorialRevision`, exposed as `BlogPostResponse.EditorialRevision` and `BlogPostAdmin.editorialRevision`.

Use the following rules:

| Operation | Aggregate `Revision` | `EditorialRevision` |
| --- | --- | --- |
| Create first draft | Starts at current value 1 | Starts at 0 |
| Create and publish | Starts at current value 1 | Starts at 0 |
| Save existing draft | +1 | +1 |
| Save and publish existing article | +1 | +1 |
| Publish stored draft | +1 | unchanged |
| Unpublish | +1 | unchanged |
| Upload image metadata | +1 | unchanged |
| Reserve/finalize image deletion | +1 per write | unchanged |
| Delete whole article | no surviving value | not applicable |

An explicit save increments the editorial revision even if the submitted content serializes identically. This keeps the rule aligned with successful Save actions and avoids introducing deep-equivalence semantics that do not exist today.

The server, not the SPA, owns the increment:

- Existing-article `SaveAsync` and `SaveAndPublishAsync` request the next editorial revision as part of the same atomic aggregate replacement.
- `CreateAsync` and `CreateAndPublishAsync` initialize it to 0.
- Media and publication-state-only service methods preserve it unchanged.
- The SPA renders `editorialRevision` but continues submitting aggregate `revision` for concurrency.

### Existing article compatibility

Exact historical editorial revisions cannot be reconstructed. Existing documents
with no `editorialRevision` read as their current aggregate `Revision`. The first
successful aggregate replacement persists that baseline atomically; an explicit
save uses baseline + 1, while all other mutations preserve the baseline. This
avoids a visible reset of mature articles to 0 without requiring a batch backfill,
while acknowledging that the pre-rollout baseline may include non-editorial writes.

Alternative rejected:

- Treat missing values as 0. This is mechanically simple but would reset every existing article and falsely imply it has never been revised.
- Reuse the aggregate `Revision` but suppress increments for media operations. This weakens optimistic concurrency because concurrent media, save, publish, and deletion operations would no longer always change the compare-and-swap token.
- Store complete editorial snapshots. That is a separate history/versioning feature with storage, retention, comparison, and restoration decisions.

### Frontend presentation

- Keep the visible label `Revision`.
- Change the index and editor to render `editorialRevision`.
- Append the editorial revision to success feedback only if retaining the current message pattern remains useful; use the value returned by the completed request, never a client-side increment.
- Keep all existing Draft/Published status language and controls.

Impeccable planning lens:

- Surface: BackOffice blog list and article editor.
- Mode: Operate.
- Change type: refinement, not redesign.
- No BackOffice-local `PRODUCT.md` or `DESIGN.md` was found; current Bootstrap, `PageHeader`, table, status, and alert conventions remain visual authority.
- Narrow implementation workflow: `clarify`, because the task corrects the meaning of displayed operational metadata without changing layout or interaction.
- Minimum visual evidence: populated list and saved editor at desktop and narrow widths, showing `Revision 0`, a later linear revision, and unchanged Draft/Published hierarchy.
- Accessibility: retain the existing status region, ensure the revision label is understandable with adjacent state, and preserve table header/data association.

## Impacted Projects

- `MorWalPizVideo.Models`: add the persisted editorial revision to the blog aggregate with legacy-compatible deserialization behavior.
- `MorWalPizVideo.Domain`: initialize, increment, and preserve editorial revisions at the owning service/repository boundary.
- `MorWalPiz.Contracts`: expose the editorial revision in the admin response without replacing the concurrency token.
- `MorWalPizVideo.BackOffice`: existing response projection returns both values; no composition-root change.
- `MorWalPizVideo.BackOffice.Tests`: add service and HTTP coverage for initialization, increments, non-editorial preservation, stale concurrency, and legacy compatibility.
- `frontend/fe-packages/models`: add the admin response property.
- `frontend/fe-packages/services`: verify only unless a response-normalization compatibility helper is needed; mutation requests must continue carrying aggregate revision.
- `frontend/back-office-spa`: display editorial revision while using aggregate revision internally.
- ServerAPI and public frontend: no contract or UI impact because public blog responses do not expose revision.
- AppHost/deployment: API-before-SPA deployment order and old-writer drain are required; topology is unchanged.
- Shop, Shooting ITA, desktop applications, ShortLinks, Shooting Range, and FreezerAnalysis: not affected.

## Impacted Files

### Modify

- `MorWalPizVideo.Models/Models/BlogPost.cs`: add persisted `EditorialRevision`.
- `MorWalPizVideo.Domain/BlogService.cs`: initialize at 0, increment only in explicit save methods, preserve through all other mutations, and support legacy baseline handling at the repository/service boundary.
- `MorWalPiz.Contracts/Contracts/BlogContracts.cs`: add `EditorialRevision` to `BlogPostResponse`; retain `Revision`.
- `MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs`: cover the complete two-counter transition table and legacy documents.
- `MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs`: verify draft/public creation at editorial revision 0, subsequent explicit saves at 1, media operations not changing it, and aggregate revision still changing.
- `frontend/fe-packages/models/src/blog.ts`: add `editorialRevision` to `BlogPostAdmin`.
- `frontend/back-office-spa/src/routes/blog/form.tsx`: render editorial revision; retain aggregate revision in hidden values, mutation arguments, response state, and editor remount key unless a separate reason exists to change that key.
- `frontend/back-office-spa/src/routes/blog/index.tsx`: render editorial rather than aggregate revision.
- `frontend/back-office-spa/src/routes/blog/form.test.tsx`: distinguish displayed editorial revision from submitted aggregate revision.
- `frontend/back-office-spa/src/routes/blog/index.test.tsx`: cover populated rows at revisions 0 and greater.

### Add

- None. Legacy baseline persistence is lazy and occurs atomically on the first successful aggregate replacement.

### Generated/Migration

- No batch MongoDB backfill or EF Core/SQLite migration.

### Documentation

- `docs/feature-handoffs/blog-revision-display-remediation-handoff.md`: this revised implementation source of truth.
- `docs/features/blog-authoring.md`: record the two revision meanings and delivered validation.
- `docs/architecture/structured-blog.md`: update the persisted schema, admin response, save semantics, and rollout.

### Verify Only

- `MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs`: response projection should acquire the additive field through the shared contract; request concurrency behavior remains unchanged.
- `frontend/fe-packages/services/src/blogService.ts`: transport already returns typed admin responses and should continue sending aggregate revision.
- BackOffice composition-root files, including `MorWalPizVideo.BackOffice/Program.cs`: no registration or operational command is needed.
- Deployment workflows and manifests: API-before-SPA and draining old writers are rollout gates; no workflow change is required for this code change.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Additive admin response field. Existing aggregate revision request/response remains intact. |
| Frontend/UI | Visible Revision uses editorial semantics; hidden concurrency behavior is unchanged. |
| Database/Data | Additive MongoDB field; legacy baseline is persisted lazily in the first atomic replacement. No batch migration. |
| Contracts/Models | Add `EditorialRevision`/`editorialRevision`; do not rename or remove aggregate `Revision`. |
| Authentication/Authorization | No permission change. Backfill execution must use the established trusted operational boundary. |
| Validation | Editorial revision must be nonnegative; aggregate expected revision remains at least 1. |
| Cache | No public response change. Save-and-publish invalidation behavior remains unchanged. |
| Configuration/Secrets | No new secret. Backfill uses existing environment-managed MongoDB access. |
| Logging/Telemetry | Backfill must report matched, modified, skipped, and failed counts without document content. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Deploy compatible API before SPA; complete or activate legacy baseline handling before displaying the new field. |
| Backward Compatibility | Additive field supports rolling deployment. Old SPAs ignore it; new SPAs require a compatible API or explicit normalization during the compatibility window. |

## Reuse Opportunities

- Reuse the existing immutable record `with` transitions in `BlogService`.
- Reuse the existing atomic repository replacement; update both counters in the same replacement rather than performing a second write.
- Reuse `BlogPostResponse.From` for centralized admin projection.
- Reuse existing BackOffice WebApplicationFactory, mock repository, test authentication, and blog service/controller fixtures.
- Reuse current frontend `BlogPostAdmin`, shared service calls, React Router fetcher state, and component test setup.
- Reuse the repository's established MongoDB operational mechanism after verifying its current pattern; do not add a startup scan by default.

## Risks

### High — conflating editorial and concurrency revisions

- Cause: both counters coexist on the same aggregate and response.
- Affected surface: every save, publish, upload, and deletion mutation.
- Mitigation: use explicit names in Models/Contracts/frontend; aggregate `Revision` remains the only expected concurrency value.
- Verification signal: media writes advance aggregate revision while editorial revision remains unchanged, and stale aggregate tokens still produce conflict.

### High — inconsistent mixed-version deployment

- Cause: a new SPA could display a field absent from an older API.
- Affected surface: BackOffice article list/editor during rolling deployment.
- Mitigation: deploy additive API first, verify responses, then deploy shared frontend artifacts and SPA.
- Verification signal: pre-SPA smoke response contains both revision values for new and legacy articles.

### Medium — inaccurate legacy baseline

- Cause: historical aggregate revision includes non-editorial writes.
- Affected surface: existing article revision numbers.
- Mitigation: document the rollout baseline, preserve monotonicity, and do not claim exact pre-rollout editorial history.
- Verification signal: every existing article retains a nondecreasing visible value after rollout; future transitions follow the operation table.

### Medium — missed mutation resets or increments the wrong counter

- Cause: blog mutations share the replacement helper.
- Affected surface: media/deletion workflows and explicit saves.
- Mitigation: pass the desired editorial revision explicitly into the replacement candidate and test every mutation family.
- Verification signal: transition-table service and HTTP tests cover save, save-and-publish, publish, unpublish, upload, reserve, and finalize.

### Medium — legacy baseline compatibility

- Cause: old documents do not contain the new field.
- Affected surface: legacy document display.
- Mitigation: use tolerant response fallback and persist a missing baseline during the first successful aggregate replacement.
- Verification signal: legacy response uses the current aggregate revision, a non-save mutation preserves that value, and a later save increments from it.

### Low — number overflow

- Cause: unbounded repeated saves.
- Affected surface: a single long-lived article.
- Mitigation: use `long`, reject invalid negative data, and use checked increment behavior consistent with aggregate revision safeguards.
- Verification signal: boundary test rejects or safely handles the maximum value without wrapping.

Security, cache coherence, and external-service risks otherwise remain unchanged.

## Technical Debt

- One field currently carries concurrency and presentation semantics. This blocks the requested standardization and must be split now.
- The feature does not retain revision snapshots. This does not block a monotonic save counter and remains deferred.
- Existing history is not reconstructable from current data. Isolate that limitation in the rollout baseline rather than fabricating counts.
- The upload-before-save flow creates an aggregate before the cover can be stored. Under the proposed rule that creation is editorial revision 0, a later explicit save becomes revision 1. Changing that product flow or introducing provisional articles is separate scope.

## Implementation Roadmap

1. Add the persisted editorial revision.
   - Owners: Models and Domain.
   - Add a nonnegative `long` initialized to 0 for new aggregates.
   - Keep aggregate `Revision` untouched.
   - Gate: serialization covers present and missing fields.

2. Define atomic transition ownership.
   - Owner: `BlogService`.
   - `CreateAsync` and `CreateAndPublishAsync` set editorial revision 0.
   - Existing-article `SaveAsync` and `SaveAndPublishAsync` advance it once in the same aggregate replacement.
   - Publish/unpublish, image upload, and both image-deletion phases preserve it.
   - Gate: service transition-table tests pass, including stale concurrency and maximum-value validation.

3. Extend the additive admin contract.
   - Owners: Contracts and BackOffice projection.
   - Return both aggregate `Revision` and `EditorialRevision`.
   - Do not accept client-selected editorial revision in save requests; server ownership prevents skipped or forged counts.
   - Gate: HTTP responses expose both values and requests still use aggregate revision.

4. Implement legacy compatibility without a batch migration.
   - Owners: Domain/BackOffice operational boundary.
   - Tolerantly interpret missing editorial revision using aggregate revision.
   - On the first successful aggregate replacement, persist the legacy baseline in the same CAS write; explicit save advances from that baseline.
   - Gate: fixture with a missing field reads consistently, a non-save mutation persists the same baseline, and the next explicit save increments once.

5. Update shared frontend typing and presentation.
   - Owners: shared models and BackOffice SPA.
   - Render `editorialRevision` in the list, status line, and success feedback.
   - Continue sending `revision` for all concurrency-aware service calls.
   - Gate: component tests use deliberately different values, such as aggregate revision 7 and editorial revision 2, so accidental conflation fails.

6. Validate the end-to-end sequence.
   - New draft returns editorial 0.
   - New public article returns editorial 0.
   - Next explicit draft or publish save returns editorial 1.
   - Upload and deletion operations leave editorial revision unchanged while aggregate revision advances.
   - Stale aggregate writes still conflict.
   - Apply the Impeccable `clarify` review to the Operate-mode list/editor and verify desktop/narrow rendering and status announcement.

7. Roll out in compatibility order and update documentation.
   - Deploy API and compatible reads first.
   - Drain old API writers before new writes rely on the additive field.
   - Deploy shared frontend artifacts and SPA.
   - Update this handoff and the owning architecture/feature records with actual files, checks, and residual risks.
   - Stop at a counter only; do not add snapshot history, diff, restore, or audit-log UI.

An independent post-validation review is warranted because the change splits a concurrency-sensitive persisted concept and introduces a MongoDB compatibility transition.

## Implementation Record

Implemented in the worktree on 2026-11-18:

- Added nullable persisted `EditorialRevision` to the blog model for legacy BSON compatibility. New draft/public articles initialize it to 0.
- Explicit draft saves and save-and-publish advance it once. Publish/unpublish, image upload, and image deletion reserve/finalize preserve it.
- Admin responses expose `editorialRevision`, falling back to the existing aggregate `Revision` for legacy articles. The first successful write persists that baseline in the same compare-and-swap replacement; explicit saves advance from it.
- BackOffice editor and index now display the editorial counter. All hidden fields, API request payloads, and service arguments continue using aggregate `revision`.
- Updated the feature handoff, blog authoring record, and Structured Blog architecture document. No batch MongoDB backfill or schema migration is required.

Modified files:

- `MorWalPizVideo.Models/Models/BlogPost.cs`
- `MorWalPizVideo.Domain/BlogService.cs`
- `MorWalPiz.Contracts/Contracts/BlogContracts.cs`
- `MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs`
- `MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs`
- `frontend/fe-packages/models/src/blog.ts`
- `frontend/back-office-spa/src/routes/blog/form.tsx`
- `frontend/back-office-spa/src/routes/blog/form.test.tsx`
- `frontend/back-office-spa/src/routes/blog/index.tsx`
- `frontend/back-office-spa/src/routes/blog/index.test.tsx`
- `docs/features/blog-authoring.md`
- `docs/architecture/structured-blog.md`
- `docs/feature-handoffs/blog-revision-display-remediation-handoff.md`

Validation:

- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~BlogServiceTests|FullyQualifiedName~BlogControllerTests"` — passed, 30 tests.
- `yarn --cwd frontend workspace back-office-spa test --passWithNoTests=false --run src/routes/blog/form.test.tsx src/routes/blog/index.test.tsx` — passed, 32 tests (JUnit: 29 form, 3 index).
- `yarn --cwd frontend workspace back-office-spa build` — passed (TypeScript and Vite); existing large-chunk warning remains.

Residual risks:

- Legacy editorial baselines intentionally reflect the prior aggregate revision and therefore are not exact historical save counts.
- Drain old API writers before new writes persist `editorialRevision`; an older whole-document writer can discard the additive field.
- No real-browser desktop/narrow visual capture was performed. Component tests and the checked build passed.

## Testing Strategy

- Unit/service in `MorWalPizVideo.BackOffice.Tests`:
  - first draft and first public article initialize editorial revision 0;
  - each later `SaveAsync` and `SaveAndPublishAsync` increments it exactly once;
  - publish, unpublish, image add, image-deletion reserve, and finalization preserve it;
  - aggregate revision increments on every surviving mutation;
  - stale aggregate revision still conflicts;
  - editorial revision cannot wrap or become negative;
  - legacy missing-field documents resolve to the approved baseline.
- HTTP/controller:
  - create draft/public responses expose aggregate revision 1 and editorial revision 0;
  - subsequent save exposes aggregate revision 2 and editorial revision 1 when no intermediate mutation occurs;
  - upload sequence demonstrates aggregate advancement without editorial advancement;
  - additive response remains compatible with existing request shapes.
- Frontend model/contract:
  - TypeScript model requires the field after the API compatibility boundary is established.
- Frontend component:
  - list and editor render editorial revision, not aggregate revision;
  - success feedback uses server-returned editorial revision;
  - save, publish, upload, unpublish, and deletion continue submitting aggregate revision;
  - values intentionally differ in fixtures.
- Security:
  - clients cannot submit or override the next editorial revision;
  - permissions and channel scope remain unchanged.
- Migration:
  - no batch migration; verify lazy baseline persistence for legacy BSON documents.
- End to end:
  - draft-first and public-first flows;
  - cover upload before a later explicit save;
  - stale concurrent editor conflict;
  - legacy article read before its baseline is persisted and after the first mutation.
- Responsive/accessibility:
  - populated list alignment at narrow width;
  - status region announces the returned revision;
  - revision and publication status remain understandable without color.

## Migration And Rollback

- MongoDB compatibility window:
  - API tolerant reads must deploy before SPA dependency.
  - New writes always persist `editorialRevision`.
  - Existing missing values resolve to aggregate revision; the first successful CAS replacement persists that baseline (or increments it for an explicit save).
- Deployment order:
  1. compatible Models/Domain/Contracts/BackOffice API;
  2. verify dual-field responses;
  3. drain old API writers;
  4. shared frontend model/service build;
  5. BackOffice SPA.
- Feature flags:
  - not required if tolerant reads and additive deployment order are used;
  - use an existing rollout mechanism only if operational constraints prevent coordinated API-first deployment.
- Monitoring:
  - API serialization/validation failures;
  - blog 409 conflict rate;
  - missing editorial revision count;
  - frontend response-shape errors.
- Rollback:
  - SPA can roll back independently; older SPA continues displaying aggregate revision.
  - Drain old API writers before the new field is first persisted because an older whole-document replacement can discard it.
  - Do not roll the API back to a writer that does not preserve `editorialRevision` after new-field writes begin; use SPA rollback or restore a compatible API artifact.
  - Do not map the editorial field onto aggregate concurrency semantics.
- SQLite, auth, cache, and secrets: no migration.

## Open Questions

None. The legacy baseline and upload-before-save semantics are approved above.
