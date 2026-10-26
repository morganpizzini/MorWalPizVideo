# BackOffice And ServerAPI Consistency Audit

**Reviewed:** 2026-10-24  
**Scope:** Current-source baseline, not a pull-request diff. Documentation only; no implementation or operational changes.  
**Status:** Findings verified statically; remediation proposals are not implemented or release-approved.

## Evidence And Limits

The review compared both API composition roots, selected controller/service/repository paths, shared contracts, relevant tests and the ServerAPI deployment filter against the architecture guide and existing debt. Repository Expert evidence established ownership; a Senior Developer consultation clarified legacy Push and form-submission intent, with the core claims checked against source.

This is a risk-led sample, not an exhaustive endpoint, consumer, configuration or security inventory. No builds, tests, HTTP probes, database queries, browser checks or deployed-environment checks were run. Test references below identify existing coverage and the owner of missing regressions, not current passing results. Runtime predictions require the stated regression checks before closure. No historical attribution is claimed.

## Common Standard

These rules consolidate existing architecture targets. The cancellation and composition-check details are proposed refinements, not claims of universal implementation. Identical standards do not mean identical host responsibilities.

| Concern | Standard and owner | Current alignment / bounded next step |
|---|---|---|
| Host boundaries | BackOffice owns administrative writes; ServerAPI owns public projections and approved interactions. No API-to-API project reference. | Both hosts reference shared libraries, not each other. Preserve this; do not create a shared host or copy controllers between hosts. |
| Controller responsibilities | Controllers bind, authorize, resolve host/channel context and map outcomes. Shared domain invariants belong in focused Domain services. | Focused services exist; form submissions still duplicate decision logic. Extract only the shared use case, not a generic controller framework. |
| Dependencies | Inject the smallest owning service. Retain `DataService`/`IGenericDataService` only as compatibility surfaces. | TD-013 remains partial. Use existing focused-service dependency tests for each migrated controller; do not rewrite the entire facade. |
| DTOs and compatibility | Shared request/response DTOs belong in `MorWalPiz.Contracts`; entities and storage details remain internal. Version externally observable changes additively. | Competitions v1 is a maintained example, not evidence every legacy route is safe. Preserve client compatibility until migration is evidenced. |
| Authorization | Host-owned authenticated fallback; explicit public/API-key/internal-service exceptions. Domain record protection must hold across every route. | Shared base is already neutral (TD-003 closed). Push has a cross-route protection gap; forms' anonymous metadata is not an administrative override. |
| Validation | Request shape at binding; lifecycle, eligibility, option identity and cross-record rules at the owning service boundary. | Form rules differ by host. Same input and domain context should yield the same domain outcome unless an explicit, tested override exists. |
| Errors and logging | Incrementally adopt Problem Details with stable codes; structured logs; no sensitive exception bodies. Separate expected domain rejection from infrastructure failure. | TD-014 remains partial. Do not change every legacy error envelope in one release or mask a committed write as a failed write. |
| Persistence | Focused queries push predicates, ordering and bounds to Mongo; preserve legacy serializer behavior and Mongo/mock parity. | Keep TD-011 operational index proof and TD-015 additive response compatibility separate from service cleanup. |
| Cache and HTTP | Central lowercase-invariant keys/tags, authenticated internal calls, factory-owned HTTP clients; do not dispose factory clients. | Existing internal authentication is implemented. Cache maintenance still uses GET; a future command migration needs client compatibility. Reliability remains deferred under TD-006. |
| Async work | Propagate cancellation through touched read/pre-commit I/O boundaries; define post-commit completion separately. | Generic repositories, forms and cross-API methods lack token parameters. Extend one feature slice compatibly, not every interface at once. |
| Composition | Every supported flag combination must resolve reachable controller dependencies; offline workflows need deterministic fakes. | Chat mock activation and cache-disabled activation need focused checks. Keep host policy/options local; share only proven reusable registrations. |
| Verification | Tests follow behavior ownership, not the assembly name; deployment filters must select required security/contract suites. | Tests in BackOffice.Tests can exercise ServerAPI. A nonempty filtered suite does not prove the correct tests were selected. |

## Critical Issues

None identified within the inspected scope. Endpoint discovery and deployed exploitability were not established; this is not a security certification.

## Major Issues

### API-01: Legacy Push bypasses subscription credentials (corrected in source)

- **Evidence:** Anonymous [legacy subscribe/unsubscribe](../../MorWalPizVideo.ServerAPI/Controllers/PushController.cs) return `410 Gone` and no longer resolve `IPushSubscriptionRepository`. The [shared subscription service](../../MorWalPizVideo.Domain/Push/PushSubscriptionService.cs#L46) remains credential-protected for existing records.
- **Impact:** The previously verified endpoint-only mutation path is removed in source. Deployed legacy-consumer traffic and endpoint discovery remain unverified.
- **Correction:** Retire the legacy mutation contract and preserve only the credential-protected `api/push/subscriptions/*` mutation surface. Keep the read-only public-key alias during consumer verification.
- **Tracking:** The source defect is corrected; TD-027 remains open only for deployment/consumer rollout evidence.
- **Validation owner:** BackOffice.Tests `PushSubscriptionEndpointTests`; the focused suite passes 6/6, including cross-route attempts that leave the protected subscription unchanged.

### API-02: Duplicate ServerAPI health route

- **Evidence:** [ServerAPI startup](../../MorWalPizVideo.ServerAPI/Program.cs#L419) calls `MapDefaultEndpoints()` and then maps `/health` again. [ServiceDefaults](../../MorWalPizVideo.ServiceDefaults/Extensions.cs#L132) already maps the same route.
- **Impact:** Two equally matching endpoints predict ambiguous routing and a failed general health probe. Runtime reproduction and actual deployment probe selection remain unverified.
- **Minimal correction:** Keep one authoritative `/health` mapping with the intended response and anonymous metadata; preserve live, ready and startup probes.
- **Tracking:** TD-029; new baseline finding, not an introduced-change claim.
- **Validation owner:** BackOffice.Tests ServerAPI host fixture; request all four health paths and assert one general-health endpoint. Check expected healthy/unhealthy status and response rather than assuming every dependency is healthy.

### API-03: Form submission invariants differ between hosts

- **Evidence:** [BackOffice submission](../../MorWalPizVideo.BackOffice/Controllers/CustomFormsController.cs#L313) is anonymous but channel-scope-gated, checks `Active`, and matches answers positionally. [ServerAPI submission](../../MorWalPizVideo.ServerAPI/Controllers/CustomFormsController.cs#L118) checks effective lifecycle, survey eligibility and selected option IDs. [FormsService](../../MorWalPizVideo.Domain/FeatureServices.cs#L647) persists without these invariants. The [feature contract](frontend-custom-forms.md) requires lifecycle and active-survey eligibility.
- **Impact:** A caller with a resolvable channel context can submit through a weaker route, including bypassing SurveyOnly eligibility and accepting invalid option IDs. `[AllowAnonymous]` does not mean channel-scope resolution accepts every anonymous request. No documented/tested administrative override was found.
- **Minimal correction:** Establish ServerAPI as the public submission owner and check legacy consumers before restricting/retiring the BackOffice alias. If both remain, use one focused Domain submission operation for shared rules while keeping host-specific access checks. An operator override needs explicit permission and tests, not accidental weaker validation.
- **Tracking:** TD-030, related to partial TD-013/TD-014. Preserve TD-015 dual-write/backfill compatibility.
- **Validation owner:** BackOffice.Tests forms integration tests; Draft/Disabled/Deleted, legacy effective lifecycle, missing/expired/wrong-channel survey, reordered/duplicate/missing question IDs, invalid options and required Email/TextInput answers. Assert rejection does not persist a response.

### API-04: Chat dependency is absent in mock mode

- **Evidence:** [ChatController](../../MorWalPizVideo.BackOffice/Controllers/ChatController.cs#L15) requires `Kernel`; [BackOffice composition](../../MorWalPizVideo.BackOffice/Program.cs#L190) registers it only when mock mode is off. The route has API-key authorization; missing registration is reached after successful authorization.
- **Impact:** Offline review/translation/transcript workflows fail during controller activation instead of returning deterministic fake results.
- **Minimal correction:** Supply a fake at the owning AI boundary for supported mock workflows, or explicitly mark the workflow unsupported and return a deliberate unavailable outcome. Do not instantiate a real external provider merely to satisfy DI.
- **Tracking:** TD-031; gap against the external-fakes target in [Development](development.md).
- **Validation owner:** BackOffice.Tests mock-host HTTP tests with an authorized API-key identity; execute each Chat action without external network access. Existing Swagger metadata checks do not prove activation.

### API-05: ServerAPI deployment omits relevant security/contract suites

- **Evidence:** [HostSecurityContainmentTests](../../MorWalPizVideo.BackOffice.Tests/Features/HostSecurityContainmentTests.cs#L21) and [CompetitionV1ControllerTests](../../MorWalPizVideo.BackOffice.Tests/Features/CompetitionV1ControllerTests.cs#L10) use `TestGroup:BackOffice` despite exercising ServerAPI. The [ServerAPI deployment filter](../../.github/workflows/deploy-serverapi.yml#L41) selects only ServerAPI, Shared and CrossApi groups.
- **Impact:** These regressions do not directly gate ServerAPI deployment; a nonempty test count cannot detect their omission. Full CI runs the mixed project separately, so this is not a claim that the tests never run.
- **Minimal correction:** Align behavior ownership traits or the deployment selection, preserving BackOffice and cross-host gates; verify exact required suites, not just a positive test count.
- **Tracking:** Specific open instance of TD-009, not a new parallel delivery backlog.
- **Validation owner:** Backend test/deployment maintainers; prove both suites are discovered by the ServerAPI deployment selection and that the full CI gate remains intact.

## Minor Issues

### API-06: Cancellation cannot cross several shared boundaries

- **Evidence:** [IFormsService](../../MorWalPizVideo.Domain/FeatureServices.cs#L562), [IRepository](../../MorWalPizVideo.Domain/Interfaces/IRepository.cs#L7) and [ICrossApiService](../../MorWalPizVideo.BackOffice/Services/CrossApiService.cs#L8) expose no cancellation parameter. The Push service/repository provides a token-aware local precedent.
- **Impact:** Request cancellation cannot propagate through these APIs; database/network work may continue after disconnection. No measured resource exhaustion is claimed.
- **Minimal correction:** Add compatible token support to one touched feature and its real/mock implementations. Do not cancel required consistency work blindly after a write commits; define that boundary first.
- **Tracking:** TD-032; follow-up, not grounds for a global repository rewrite.
- **Validation owner:** BackOffice.Tests forms and cross-API transport tests; cancel pre-commit work, verify provider propagation and separately verify post-commit behavior.

## Suggested Improvements

- **API-07 / TD-006, deferred:** [CacheController](../../MorWalPizVideo.ServerAPI/Controllers/CacheController.cs#L16) always requires `IOutputCacheStore`, but [startup registration](../../MorWalPizVideo.ServerAPI/Program.cs#L125) is conditional. Cache-disabled internal reset requests predict an activation failure. When cache work is explicitly resumed, align registration and dependencies, and test enabled/disabled combinations with the actual host. Existing direct-controller purge tests cannot establish composition correctness. This is a concrete deferred defect, not a claim of cache closure.
- **API-08 / TD-006 and TD-014, deferred cache aspect:** [Form creation](../../MorWalPizVideo.BackOffice/Controllers/CustomFormsController.cs#L177) commits data before remote invalidation inside the same catch-all. A transport failure can return `500` for an already committed write. Preserve existing contracts until a bounded fix defines observable degraded-cache success/failure and retry semantics. Inject a failing cross-API service and assert both persisted state and HTTP outcome. Do not introduce a new queue/outbox merely for uniformity.
- **API-09 / TD-014 compatibility/privacy follow-up:** [Legacy competition routes](../../MorWalPizVideo.ServerAPI/Controllers/CompetitionsController.cs#L20) return persistence entities, including [evaluation user identifiers and comments](../../MorWalPizVideo.Models/Models/Competition.cs#L127); [v1 DTOs](../../MorWalPiz.Contracts/Contracts/CompetitionContract.cs#L53) omit evaluations. Confirm intended public review fields and active legacy consumers before migration; the v1 exclusion test does not make the old route safe. Publication/privacy policy and live data remain unknown, so this is not asserted as a proven unauthorized disclosure.

## Refactoring Opportunities

1. **Forms first, in its correction slice:** consolidate lifecycle, eligibility and answer validation in the existing Domain feature boundary, with request/response shapes in Contracts. Create/update question validation is duplicated in the same BackOffice controller too; reuse a small feature-owned validator only when its semantics match. Keep tenancy resolution in each host.
2. **Focused service migration, follow-up:** choose one remaining `DataService` consumer at a time and protect outcomes plus constructor dependencies. Existing Calendar/content extraction is precedent; do not split `FeatureServices.cs` or rename shared namespaces solely for cosmetic consistency.
3. **Composition simplification, follow-up:** ServerAPI registers `ISurveyRepository` twice in the mock branch and a fallback newsletter sender before the production sender. Document deliberate fallback ordering and remove redundant registrations only with provider-selection tests; duplication alone does not prove runtime failure.
4. **Legacy client cleanup, follow-up:** TD-028 covers ServerAPI's directly constructed `WebPushClient`. Verify a live caller before claiming active connection exhaustion; reuse the factory-managed BackOffice sender pattern if the legacy path remains required.

## Missing Tests

Approval-blocking for the affected correction/release slices: cross-route Push tampering; host health-route uniqueness/probes; form eligibility/answer parity; authorized mock Chat execution; exact ServerAPI deployment-suite selection. These belong in the existing BackOffice.Tests cross-host infrastructure, not new test projects or generated Reqnroll code.

Follow-up/deferred: cache-disabled host activation, committed-write/invalidation failure, cancellation propagation, and legacy competition projection/consumer compatibility. Existing `FormsMigrationSafetyTests`, `CustomFormsAuthorizationTests`, `PushSubscriptionEndpointTests`, `OutputCachePurgeTests`, `CrossApiServiceContractTests`, `CompetitionV1ControllerTests` and `FocusedServiceDependencyTests` are starting points, not evidence of the missing scenarios passing. No tests were run in this audit.

## Performance Observations

Confirmed structural risks: generic uncancellable repository/network calls and form list methods returning full scoped lists without paging parameters. No latency, collection-size, allocation or load measurements were collected. Bound queries when changing their actual readers, keep Mongo/mock semantics aligned, and require representative query/index evidence under TD-011 before making performance claims. Do not prescribe blanket memoization, generic caching or a new persistence layer.

## Security Observations

API-01 is a verified record-protection inconsistency, conditional on knowledge of an endpoint URL. API-03 is a weaker domain-validation path, not evidence that all anonymous callers pass channel resolution. Legacy competition publication scope needs policy verification. Current host-neutral controller behavior, explicit internal-service cache authentication and host-owned fallback policies should be preserved. Existing TD-001 credential rotation/artifact proof, CSRF/CORS/browser gates and operational risks remain open; no secret values were inspected for inclusion or recorded here.

## Prioritized Execution And Stop Conditions

| Order | Bounded slice / owner | Exit evidence and compatibility constraint |
|---|---|---|
| 1 | API-01 / ServerAPI + Domain Push | Agree legacy compatibility policy; cross-route tampering fails without changing a protected record; legitimate consent/settings/revocation work. No subscription deletion or credential rotation in this audit. |
| 2 | API-02 / ServerAPI composition | Exactly one `/health`; all probe contracts exercised in supported host environments. No deployment or health-policy redesign bundled in. |
| 3 | API-05 / test and deployment owners | Required ServerAPI suites selected by its deployment gate; retain full CI and cross-host coverage. |
| 4 | API-03 / both forms controllers + Domain | One authoritative invariant implementation or explicit retirement/override; negative paths do not write; migration-safety tests retain additive behavior. Consumer inventory precedes route removal. |
| 5 | API-04 / BackOffice AI composition | Authorized mock requests resolve and return deterministic results without network; real-provider registration remains environment-controlled. |
| 6 | API-06 / next touched feature owner | Token-aware pre-commit I/O and explicit post-commit completion contract, real/mock parity, no unrelated interface churn. |
| Deferred | API-07/08 cache work; API-09 DTO/privacy compatibility | No reopening TD-006 or silently changing legacy responses. Require explicit scope approval and targeted evidence before implementation. |

One reversible behavior slice per implementation change. Preserve routes/payloads until explicit consumer migration; add an ADR only if a future correction chooses a new boundary or compatibility policy. Do not reactivate the shop backlog. The API Design shop section now labels its commercial contract as unresolved design context under the existing hold; reconciling that target with historical free-artifact targets remains a separate portfolio decision, not authorization inferred from documentation.

## Final Approval Status

**CHANGES REQUESTED**

This status applies to convergence of the inspected API baseline, not to approval of a supplied code diff or deployment. API-01 through API-05 need bounded correction and regression evidence before those areas can be considered aligned; deferred and operational items retain their existing status. The documentation update itself makes no application changes and grants no release approval.