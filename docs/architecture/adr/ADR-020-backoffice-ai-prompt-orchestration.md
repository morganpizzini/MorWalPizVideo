# ADR-020: BackOffice AI Prompt Orchestration

- Status: Accepted for the BackOffice Chat pilot; production rollout remains gated
- Date: 2026-11-02

## Context

BackOffice Chat originally kept prompt construction, provider invocation, parsing,
and response joining in `ChatController`. Production and mock composition also
diverged: mock mode did not register the Semantic Kernel dependency, so the
API-key-protected review, translation, and transcript routes could not execute
offline. The video-review translation stage joined results by generated title,
which is mutable model output rather than stable identity.

Prompt changes need immutable provenance and a controlled evaluation path. The
model must not promote its own candidate, define executable tools, or receive
unbounded application access. Prompt and evaluation content must remain
separate from execution telemetry and have explicit retention.

## Decision

BackOffice owns the Chat orchestration feature. Its controller maps the
existing `api/chat`, `api/chat/translate`, and
`api/chat/transcript-analysis` contracts to typed workflows. The
`FeatureManagement:EnableAiOrchestration` flag defaults off and selects the
compatibility fallback until operators initialize baseline revisions and
deliberately enable the new path. Both paths use `IAiModelGateway`; workflow
stages resolve immutable stored revisions only when the feature flag is on.
Azure OpenAI credentials continue to come from protected
`AzureConfig:OpenAi` configuration.

Mock mode uses a deterministic implementation of the same gateway and
scenario-backed orchestration repositories. It does not construct an external
AI client. Review items receive opaque internal IDs before chunking; the
metadata/translation join validates exact ID and language sets and leaves the
legacy external video name unchanged. Workflow stages validate structured
outputs in application code.

Definitions, revisions, execution metadata, evaluation cases, experiments, and
prompt lifecycle audit events use additive Mongo collections. Revisions and
completed evaluation evidence are immutable; the definition owns active and
canary pointers. Pointer changes use expected-value compare-and-set operations.
The source-owned Mongo index manifest describes required indexes; deployment
does not create them automatically.

Prompt editing, evaluation, and activation use separate permissions. Candidate
optimization creates only a draft revision. A candidate requires deterministic
baseline comparison before an authorized user may start a bounded canary.
Promotion or rollback remains an explicit human action. Production activation
is disabled by default until the operator configures
`AiOrchestration:ActivationEnabled` after reviewing the data policy, index
state, quality thresholds, and rollout evidence.

Only statically registered tools can be exposed. The initial pilot registers no
model-invokable tools; required channel terminology is loaded before the model
call. No arbitrary database query, URL fetch, filesystem, command, or write
tool is provided.

Execution records contain only workflow/stage/revision identity, selected
channel, status, stable error code, timing, item counts, and explicit expiry.
They do not retain raw prompts, inputs, outputs, tool payloads, or credentials.
Evaluation cases require operator confirmation of consent and redaction, and
carry an explicit retention deadline.

## Consequences

- VideoImporter retains its existing routes and JSON response shapes. An
  execution ID is returned in `X-AI-Execution-Id`, which is additive metadata;
  desktop feedback and execution-ID persistence remain a later slice.
- Production prompt baselines must be initialized by the protected,
  operator-invoked bootstrap endpoint before enabling
  `FeatureManagement:EnableAiOrchestration`. Mock mode initializes
  deterministic baselines on host start and always tests the new workflow.
- `ActivationEnabled` remains false by default. There is no automated
  self-promotion, canary promotion, or quality-threshold override.
- The initial scorer checks structured-output validity, curated deterministic
  checks, minimum reviewed-case count, configured pass-rate threshold (80% by
  default), and latency non-regression. Token/cost usage is not currently
  captured by the gateway. A representative
  reviewed quality dataset and approved thresholds are still required before
  real production activation.
- The SPA exposes operational revision, evaluation, canary, rollback, and
  metadata-only health controls. It does not edit Script Studio's existing
  mutable global prompt.
- FAQ, Ask, Insights, and Script Studio continue to use their existing AI
  integrations; this ADR does not migrate those callers.

## Migration and rollback

Collection additions are backward-compatible. Apply the manifest indexes only
through the reviewed Mongo index operation after auditing any existing
documents. Initialize prompts idempotently; the bootstrap does not overwrite
existing active pointers. There is no automatic production bootstrap.

Cancel a canary before changing the active pointer. Rollback atomically points
the definition to its previous immutable revision and retains experiment and
audit evidence. Disabling `AiOrchestration:ActivationEnabled` blocks new
canary/promote operations without deleting prompt or evaluation data.

## Validation

- BackOffice builds with Semantic Kernel 1.80.0.
- Focused BackOffice tests cover all three mock-host Chat routes, API-key
  authorization, deterministic execution, immutable draft creation,
  evaluation, candidate generation, canary allocation/promotion, rollback,
  prompt validation, and consent/redaction requirements.
- Shared model/service TypeScript builds, service tests pass, and the BackOffice
  SPA build and focused AI route-permission test pass.
- The source index manifest parses as JSON. Index application, live Azure
  provider verification, production canary, privacy approval, and desktop
  feedback are not claimed as completed.
