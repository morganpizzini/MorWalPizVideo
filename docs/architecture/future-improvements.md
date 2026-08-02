# Future Improvements

| Improvement | Motivation | Expected benefits | Complexity | Priority | Risks |
|---|---|---|---|---|---|
| Secret-management remediation | Tracked credentials are untrusted | Restored credential integrity and auditable configuration | Medium | High | Rotation may interrupt integrations |
| Explicit API ownership cleanup | Shop and shared controller behavior cross boundaries | Clear exposure, simpler auth, less duplication | Medium | High | Consumer route migration |
| Free-artifact acquisition/download | Shop cannot deliver originals safely | Complete useful shop workflow and future account attachment | High | High | Cookie loss and SAS sharing window |
| Canonical short links | Three stores prevent uniqueness and efficient lookup | Safe branded URLs, compact query tracking, reliable metrics | High | High | Conflict resolution and backfill |
| Versioned DTO APIs | Entities and routes are unstable contracts | Safer evolution and generated documentation | High | High | Temporary dual-contract maintenance |
| Mongo index manifest | Queries lack source-owned performance guarantees | Predictable latency and enforced uniqueness | Medium | High | Index build load and duplicate cleanup |
| Feature-focused application services | `DataService` is broad and coupled | Smaller constructors, focused tests, reusable use cases | High | Medium | Incremental migration complexity |
| Separate form responses | Embedded arrays grow without bound | Scalable submissions and reporting | High | Medium | Dual-write/reconciliation period |
| Cookie-only BackOffice auth | Local-storage token is XSS-accessible | Reduced browser token exposure | Medium | Medium | CSRF and cross-origin configuration |
| Complete mock/fake platform | External dependencies slow local work | Offline development, deterministic UI/E2E tests | Medium | Medium | Fakes drifting from provider behavior |
| Desktop Generic Host/DI | Static services resist testing | Managed clients, isolation, maintainable MVVM evolution | Medium | Medium | Legacy UI migration effort |
| Blob metadata and health | Current abstraction loses protocol details | Correct content delivery, caching, diagnostics | Medium | Medium | API contract changes |
| Distributed throttling | In-memory limits do not scale | Consistent abuse protection across instances | Medium | Medium | New infrastructure dependency |
| Durable jobs and audit | Background execution needs stronger guarantees | Retry safety, traceability, operational confidence | Medium | Medium | Storage/retention overhead |
| Frontend contract convergence | Direct calls and mismatched environment variables exist | Consistent auth, errors, configuration, and testing | Medium | Medium | Coordinated package builds |
| Customer identity and analytics | Deferred product need | Cross-device recovery and acquisition insight | High | Low | Privacy, retention, email deliverability |
| Transactional email | Future identity/notifications need delivery | Verification and lifecycle notifications | Medium | Low | Provider lock-in and sender reputation |
| Detailed short-link analytics | Aggregate counts provide limited insight | Campaign and destination effectiveness | Medium | Low | Privacy and event-volume costs |

## Recommendation Principles

- Complete security, ownership, and delivery correctness before adding analytics.
- Add abstractions only where a real provider or repeated consumer requires them.
- Keep customer identity optional until a workflow requires cross-device continuity.
- Preserve permanent-free acquisition semantics if paid products are ever introduced as separate editions.