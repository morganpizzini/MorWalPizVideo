# Editorial and AI

Consolidated 2026-11-09 from AI orchestration, editorial production, and creator
partnership handoffs. **Recorded implementation is not deployed qualification**;
checks below were not rerun. See the [filename map](../feature-handoffs/README.md#former-handoff-map).

## Chat orchestration

Foundation recorded implemented 2026-11-02; production activation remains gated.
[ADR-020](../architecture/adr/ADR-020-backoffice-ai-prompt-orchestration.md)
owns the accepted boundary.

- Preserve three existing Chat routes/wire DTOs; X-AI-Execution-Id is additive.
  Nonmock orchestration defaults off; mock always runs deterministic workflows
  with five baseline stages. Compatibility fallback uses its separate service/
  gateway/baseline prompts, retains terminology defaults, and does not record
  new orchestration executions.
- Stable opaque IDs across ten-item chunks and translation; validate exact
  identity/language sets, bounds, and transcript output in application code.
- Immutable revisions/evaluations/experiment evidence/audit, Mongo and mock
  persistence, CAS active/canary pointers, candidate-only optimization, rollback,
  versioned administrative endpoints and SPA were recorded delivered.
  Separate view/edit/evaluate/activate permissions; blocked in impersonation.
- Activation defaults false and cannot operate without orchestration flag.
  Minimum reviewed cases, default 80% pass threshold, baseline/latency
  nonregression and bounded canary counts/failure ceiling are required.
  Human promotion only; token/cost capture is absent and cannot be evidence.
- Statically registered tools only; initial pilot registers none. Terminology
  is preloaded. Execution records are metadata-only with explicit expiry;
  evaluation cases require consent/redaction confirmation and retention.

Owners: BackOffice ChatController/AiPromptsController,
[AI services](../../MorWalPizVideo.BackOffice/Services/Ai/), Models/Domain/
Contracts orchestration boundaries, shared frontend services/models, admin
AI-workflow routes, and AiOrchestrationTests.

**Still proposed/unconfirmed:** recursive DataAnnotations plus workflow
validation with stable issue paths/codes; protected short-lived redacted failure
artifacts/curation; typed read-tool allowlist; desktop Accepted/Edited/Rejected
feedback and persisted execution IDs. Full protected failure-artifact delivery
is not established. DSpyNet is only a possible external candidate optimizer:
license, .NET 10/provider/cancellation/reproducibility/Linux suitability need
a spike. A deterministic optimizer fake proves no quality gain.
Generation is on demand, not a scheduled job.

Evidence: BackOffice build; **8/8 AiOrchestration tests**, including three mock
HTTP routes, compatibility/lifecycle/canary/consent-redaction; shared build;
**3 services**, **2 SPA permission/component tests**; admin build and targeted
lint/format/detector passed. Index manifest parsed, but no indexes were applied.

Release gates: governed index audit/apply, explicit protected production
bootstrap, live provider, representative reviewed dataset, privacy/retention
and quality approval, token/cost telemetry, measured canary/rollback rehearsal,
deployed route/browser evidence, activation-role ownership and scope/feedback
decisions. FAQ/Ask/Insights/Script Studio remain on existing integrations.
Independent review is warranted before broad activation.

Rollback changes the pointer atomically to the previous immutable revision,
cancels canary/disables activation/optimizer, or uses retained compatibility
fallback. Retain all evidence; content disclosed to a provider is not recalled.
Tools need separate security/scope approval. See
[terminology compatibility](accounts-and-channels.md#terminology-null-compatibility).

## Editorial production cards and analytics

Original broad proposal has a **partial recorded P0 implementation**, not full
delivery. [ADR-019](../architecture/adr/ADR-019-editorial-production-cards.md)
owns the accepted card boundary.

- Private channel-owned cards have CRUD/revisions/transitions, Mongo/mock,
  contracts, and SPA. Cards reference authoritative records, not duplicate
  Blog/video/archive. Status changes never publish/upload.
- Optional HTTPS YouTube Studio/Google Analytics links are safe external links.
  No private analytics proxy, OAuth/dashboard, automatic publishing, or CRM.
- Later completion notes claim server ownership/coherence checks for
  topic/news/plan/video/blog; foreign references give 400 before persistence.
  `POST /api/editorial-production-cards/from-plan/{planId}` copies title/outline/
  references/source URLs without mutating/publishing the plan. The Insight
  command is distinct from Create article. These later claims go beyond the
  ADR's earlier manual-only baseline and lack fresh HTTP proof.
- Optional persistent associations are explicit, not router prefill.
  VideoImporter remains operational Shorts-only uploader, no card/file tracking.
  Long-form upload remains manual YouTube/browser with optional association.
- Reports remain Google-owned; optional integration defaults disabled.
  A first-party default-deny adapter replaced the CDN banner and existing calls.
  Full consent/revocation/storage cleanup, event taxonomy, route-local reCAPTCHA,
  third-party consent/click-load remain unfinished/privacy-owner-gated.

Proposed analytics taxonomy: page_view, navigation_select, select_content,
filter_change, media_load (not playback), share, outbound_click, successful
form_submit, newsletter_action, push_action, file_download, gallery_interaction,
stream_interaction, error_retry. Only allowlisted public/opaque metadata; no PII,
full URLs/query, text, answers, filenames, or error detail. No account/API-key/
consent-choice behavioral events. Centralize navigation/pageview; GA is no-op
before consent and after revocation. Old banner dismissal is not valid consent.

Owners: editorial Models/Domain/contracts/controller/admin routes, Insights
detail, channel config/sidebar, and
[public analytics](../../frontend/morwalpizvideo.client/src/analytics/).
Initial BackOffice build and later admin builds passed; static diagnostics clean.
Later backend attempts were cancelled/timed out without reliable exits;
no fresh HTTP/from-plan/permission frontend/mock E2E proof.

Before expansion, measure 5-10 real productions; confirm states/published
semantics/permissions and indexes. AI plan generation receives IDs rather than
source text; uncertain URLs and globally deduplicated Insights saves cannot
guarantee an automated queue. Preserve these as debt, not implied delivery.
Rollback hides routes/permissions and retains additive cards; no SQLite
migration. Never restore pre-consent GA initialization.

## Creator partnerships and campaigns

Workspace recorded implemented; runtime endpoint proof remains unresolved after
the 2026-10-26 follow-up.

- Channel-scoped `/api/Partners` and `/api/Campaigns`; partner contacts, one
  partner per campaign, embedded typed timeline. Default excludes archived
  partners and Completed/Archived campaigns; includeArchived opts in.
  Referenced partners cannot be deleted. URLs/content references remain strings;
  no public endpoint/dashboard/CRM.
- Timeline sorts due then occurrence; generated entry IDs.
  Missing external design document means lifecycle names/timeline kinds still
  need product reconciliation before production data.
- CI's two failures lacked top-level ID because upstream status/body was not
  checked. Original response/revision remains unknown; nullable optional-contact
  validation is a hypothesis, not a confirmed production bug.
  A global-empty shared-fixture assertion was independently order-dependent.
- Incident repair changed tests only: check statuses before parsing, assert the
  test's own Completed ID exclusion/inclusion. No production route/casing/
  serializer/auth change or scenario clearing.

Owners: CreatorPartnership model/contracts/converters, DataService/repositories,
Partners/Campaigns controllers, shared endpoints/types, admin partners/campaigns
routes/menu/permissions, and CreatorPartnershipEndpointTests.
Contracts/BackOffice/test builds and shared models/services/TypeScript checks
passed in recorded iterations. SPA build before smoke harness passed;
post-harness TypeScript and **2 files/2 index smoke tests** passed.
Final Release test build passed, but focused execution exceeded 120 seconds
before VSTest startup and produced no summary. Earlier post-assertion build
also hit MSB4242. Optional-contact omitted/valid/invalid tests remain proposed.
Calendar Mongo skips are unrelated.

Resolve the [test runner proposal](delivery-and-tooling.md#test-runner-timeout)
before claiming endpoint runtime success. The later
[transport correction](accounts-and-channels.md#administrative-channel-transport)
scopes these routes but does not close their behavior tests.
Schema is additive; test-only incident repair needs no data rollback.
Do not broaden permissions or reset shared scenarios to make tests pass.
