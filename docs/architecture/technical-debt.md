# Technical Debt Backlog

## Prioritization

Priority combines security, correctness, production impact, architectural leverage, and implementation dependency. Status is Open unless stated otherwise.

| ID | Priority | Debt | Impact | Recommended action | Complexity |
|---|---|---|---|---|---|
| TD-001 | Critical | Tracked credential material and unconfirmed rotation | Credential compromise and unauthorized access | Revoke/rotate, remove current files/seeds, review history/artifacts, enable scanning | Medium |
| TD-002 | Critical | Shop tokens are not persisted or validated; cart trusts caller IDs | Customer/cart impersonation | Replace caller identity with server-owned anonymous cart cookie; later add customer policy | High |
| TD-003 | High | Shared controller base applies BackOffice authorization to public hosts | Public endpoint failures and unclear exposure | Split host-neutral base behavior from explicit authorization policies | Medium |
| TD-004 | High | BackOffice duplicates anonymous shop controllers | Divergent contracts and ownership | Add authenticated management surface; remove duplicate public auth/cart/catalog endpoints | Medium |
| TD-005 | High | Public product responses expose storage keys | Private artifact disclosure | Introduce public DTOs and private-original download contract | Medium |
| TD-006 | High | Cache eviction lacks a reliable authenticated service contract | Stale public data | Replace maintenance GETs with authenticated internal commands and telemetry | Medium |
| TD-007 | High | Development flags and deployed CORS are inconsistent | Insecure or nonfunctional environments | Enforce only Dev/Swagger locally and fail-closed explicit deployed origins | Low |
| TD-008 | High | Docker runtime versions do not match .NET 10 | Failed or misleading builds | Align SDK/runtime images and referenced-project restore inputs | Low |
| TD-009 | High | CI skips actual backend tests and omits supported apps | Regressions reach deployment | Build/test complete supported matrix | Medium |
| TD-010 | High | Short links are split across three storage locations | No global uniqueness, scans, lost counts | Migrate to canonical collection and atomic counters | High |
| TD-011 | High | No source-managed Mongo indexes | Unbounded latency and duplicate data | Audit, normalize, define and apply index manifest | Medium |
| TD-012 | High | Free checkout does not persist acquisition or produce download | Core shop workflow incomplete | Add permanent-free acquisition and private download delivery | High |
| TD-013 | Medium | Broad `DataService` has excessive dependencies and responsibility | Coupling and difficult tests | Extract focused feature services incrementally | High |
| TD-014 | Medium | APIs return persistence entities and inconsistent errors | Contract leakage and unsafe evolution | Adopt versioned DTOs and Problem Details feature by feature | High |
| TD-015 | Medium | CustomForm embeds unbounded responses | Mongo document-size and contention risk | Move responses to separate collection with dual-write migration | High |
| TD-016 | Medium | ShortLinks uses read-modify-replace counting and full scans | Lost updates and scaling failure | Indexed lookup and atomic increment | Medium |
| TD-017 | Medium | BackOffice browser tokens remain in local storage | XSS token exposure | Complete HttpOnly cookie and CSRF design | Medium |
| TD-018 | Medium | WPF applications use static service location/direct HttpClient | Testability and connection-management issues | Adopt Generic Host, DI, typed clients incrementally | Medium |
| TD-019 | Medium | Frontends contain direct Fetch/Axios and route ownership leaks | Inconsistent auth/config and duplicate APIs | Consolidate in shared services; remove public management routes | Medium |
| TD-020 | Medium | Blob abstraction loses metadata and swallows failures | Incorrect media responses and weak diagnostics | Return typed blob metadata/result and classify failures | Medium |
| TD-021 | Medium | Private content authorization is coarse and inconsistent | Asset authorization bypass | Add visibility policies and enforce across metadata/images/downloads | High |
| TD-022 | Medium | API-key throttling is process-local | Limits bypassed when scaled | Use distributed rate limiting or gateway enforcement | Medium |
| TD-023 | Medium | Hangfire production durability/dashboard protection need verification | Lost/duplicated work and admin exposure | Durable storage, protected dashboard, idempotent jobs | Medium |
| TD-024 | Low | Shared namespaces reference Server/BackOffice ownership | Boundary confusion | Rename after behavioral boundaries stabilize | Medium |
| TD-025 | Low | Legacy domains and obsolete frontend modules remain | SEO and maintenance ambiguity | Correct canonical metadata; decide retain/remove per module | Low |

## Backlog Rules

- Security and data-integrity items block feature expansion in their area.
- A debt item closes only after executable validation and documentation update.
- Do not close an item merely because a plan or partial abstraction exists.
- Record pre-existing unrelated failures separately rather than hiding them.