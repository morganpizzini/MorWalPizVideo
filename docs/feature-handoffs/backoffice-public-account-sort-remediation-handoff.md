# BackOffice public account directory: Mongo-compatible ordering

Status: implemented and focused-test validated, 2026-11-09. No deployed-provider
or database-environment verification was available.

## Outcome

Restore `/public-accounts` without requiring a new compound-sort index. Follow the
already implemented single-field sorting approach. Preserve search, bounded
server-side pagination, directory permissions, and the response contract.

Implemented by removing the unsupported secondary ID ordering from both the Mongo
and mock public-account directory queries. The Mongo query now uses an inspectable
single ascending `username` sort definition; filtering, skip/limit pagination,
cancellation, count behavior, controller permissions, and the response contract
remain unchanged.

The user reports a composite-index error. The source proves a matching failure
mechanism; the exact deployed provider, artifact and index policy remain unverified.

## Current Evidence

Paths are relative to the repository root.

- [PublicAccountsController.cs](../../MorWalPizVideo.BackOffice/Controllers/PublicAccountsController.cs),
  `List`, delegates to `IPublicAccountRepository.ListAccountsAsync` and
  `CountAccountsAsync`. Page is bounded to 1-10000 and page size to 1-50.
  Directory access requires `PublicAccountsView`; it is not channel-scoped.
- [Repository.cs](../../MorWalPizVideo.Domain/Interfaces/Repository.cs),
  `PublicAccountRepository.ListAccountsAsync`, filters username/display name,
  sorts by `Username` only, then applies skip/limit in MongoDB. Before this
  implementation it sorted by `Username` followed by `Id`; that multi-field
  database sort was the source-proven likely trigger, not frontend sorting.
- [MockRepository.cs](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs),
  `MockPublicAccountRepository.ListAccountsAsync`, now matches the primary-only
  ordering; mock tests cannot reproduce a provider composite-index failure.
- [BlogService.cs](../../MorWalPizVideo.Domain/BlogService.cs),
  `BlogRepository.ListSortKeys`, already sorts on `publishedAt` alone.
  [ADR-022](../architecture/adr/ADR-022-startup-blog-index-bootstrap.md)
  explicitly documents removing a secondary `_id` sort to avoid Cosmos
  composite-index requirements. This is the verified prior art requested.
- [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs)
  declares unique normalized-username/email indexes, not a username-plus-ID
  directory sort index. Manifest presence does not prove deployed indexes.
- [PublicAccountService.cs](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs)
  enforces normalized-username uniqueness for new registrations. It does not prove
  that every legacy document is unique or that a username sort index exists.

Evidence-ranked diagnosis: the secondary database sort is the high-confidence
cause matching the supplied error. Missing support for the remaining single-field
sort is a separate, unverified operational possibility. Do not mask either with
in-memory sorting, an empty successful result, or a catch-and-retry query.

## Implemented Design

Remove the secondary ID ordering from the Mongo directory query and its mock
counterpart. Keep username ascending as the primary ordering; preserve existing
search and pagination behavior. Do not introduce a normalization/backfill change
or promise new case/collation parity between Mongo and mocks.

Keep all filtering, ordering and pagination database-side. Equal primary keys
have no guaranteed relative ordering; this is the same deliberate tradeoff as the
existing blog fix. Do not add a blanket startup index initializer or composite
index policy for this bug.

## Impacted Projects

- `MorWalPizVideo.Domain` (.NET 10): Mongo/mock directory implementation.
- `MorWalPizVideo.BackOffice.Tests` (.NET 10): query-shape and directory regressions.
- `MorWalPizVideo.BackOffice`: verify-only directory host and database deployment.
- `frontend/back-office-spa`: verify-only directory UI.
- Models, Contracts, ServerAPI, shared frontend packages, other SPAs, Windows tools,
  ShortLinks and AppHost: no required changes.

## Impacted Files

### Modify
- [Repository.cs](../../MorWalPizVideo.Domain/Interfaces/Repository.cs): directory sort only.
- [MockRepository.cs](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs): same primary-only ordering.

### Add
- [PublicAccountRepositoryTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountRepositoryTests.cs):
  focused assertions for the actual Mongo sort definition and mock behavior.

### Generated/Migration
None. No entity or index migration is planned.

### Documentation
- This handoff: record implementation and real-provider proof after delivery.

### Verify Only
- [PublicAccountsController.cs](../../MorWalPizVideo.BackOffice/Controllers/PublicAccountsController.cs).
- [publicAccounts/index.tsx](../../frontend/back-office-spa/src/routes/publicAccounts/index.tsx).
- [MongoIndexOperationsService.cs](../../MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs).
- [BlogServiceTests.cs](../../MorWalPizVideo.BackOffice.Tests/Features/BlogServiceTests.cs).

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | Narrow repository ordering correction; same endpoint and status behavior. |
| Frontend/UI | No source change; directory must load again. |
| Database/Data | Read-query change only; no document writes or collection materialization. |
| Contracts/Models | Unchanged directory item/page shape. |
| Authentication/Authorization | Preserve global directory permission; do not require a channel. |
| Validation | Preserve search escaping, page bounds and page-size bounds. |
| Cache | No new cache or invalidation. |
| Configuration/Secrets | No configuration or secret changes. |
| Logging/Telemetry | Database failures remain visible; inspect existing request telemetry at rollout. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Deploy BackOffice; verify on the actual Mongo-compatible provider. |
| Backward Compatibility | Same API; secondary tie ordering is intentionally unspecified. |

## Reuse Opportunities

Reuse the single-field sort decision and test style in `BlogRepository` and
`BlogServiceTests`, existing repository cancellation tokens, controller projection,
and bounded pagination. Do not reuse the blog-specific startup index exception for
an unrelated collection.

## Risks

| Rank | Cause/surface | Mitigation and verification signal |
| --- | --- | --- |
| Medium | Remaining username sort may lack a usable deployed index. | Read-only index inspection and a real directory request; single-field query succeeds without the supplied error. If not, report the exact remaining index requirement before changing operations policy. |
| Low | Legacy duplicate usernames can reorder between offset pages. | Document unspecified ties; test ordinary unique-name pagination and no count/filter regression. Do not introduce full-collection sorting. |
| Low | Directory data exposure if auth/scope is changed opportunistically. | Keep controller and DTO unchanged; verify denied access and that sensitive account fields remain absent. |

## Technical Debt

Mock ordering uses a case-insensitive comparer whereas Mongo uses its configured
collation. Nonblocking and deferred; this bug does not authorize identity
normalization, collation changes, or a new paging model.

## Implementation Roadmap

1. Capture a regression asserting the actual directory database sort contains only
   the username field; retain the blog sort regression as a control.
2. Update Mongo and mock ordering together. Verify search, count, cancellation and
   skip/limit remain on their existing paths.
3. Validate focused backend regressions, building once serially and running later
   targeted tests without rebuilding, following the test runbook.
4. Deployment verification remains outstanding: deploy BackOffice independently
   of the SPA and capture a successful real-provider directory request, search and
   second page.

Stop when directory fetching works with the existing single-field approach.
No independent review is required for this bounded fix; a reviewer is optional.

## Testing Strategy

- Unit/query contract: actual Mongo sort has exactly one ascending username key.
- Mock behavior: ordered fixtures, empty results, literal regex punctuation in
  search, page boundaries and count consistency.
- API integration: valid directory response; invalid pagination remains 400;
  unauthenticated/unauthorized behavior unchanged; no account secrets projected.
- Real-provider gate: initial/search/second-page requests against the target
  provider. Ordinary in-memory API tests do not establish index compatibility.
- Component/end-to-end: smoke directory search and pagination without changing the
  SPA. No migration or visual redesign testing is required.

## Migration And Rollback

No schema, data, API, auth, cache or configuration migration is required. Roll out
the BackOffice query correction only. Do not drop existing indexes. Rollback is an
application rollback with no data recovery needed, but restores the failing
compound query. No backfill, feature flag or compatibility window is needed.

## Open Questions

None blocking the code change. The deployed provider/index policy and successful
single-field execution are release evidence still to collect, not verified facts.

## Implementation Evidence

Modified files:

- `MorWalPizVideo.Domain/Interfaces/Repository.cs`: replaced the Mongo
  `username` + `id` ordering with the single-field `username` sort definition.
- `MorWalPizVideo.Domain/Interfaces/MockRepository.cs`: removed the mock's
  secondary ID ordering so equal usernames preserve source order like the
  single-field database query.
- `MorWalPizVideo.BackOffice.Tests/Features/PublicAccountRepositoryTests.cs`:
  added focused assertions for the actual Mongo sort definition and mock behavior
  for equal usernames.

Validation:

- `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --filter FullyQualifiedName~PublicAccountRepositoryTests --no-restore`
  — passed, 2 tests, 0 failures.
  The run emitted pre-existing NuGet audit warnings for the repository's
  `SixLabors.ImageSharp` dependency; no test failures occurred.

This account-sort change did not modify frontend, blog, migration, or generated
files.
