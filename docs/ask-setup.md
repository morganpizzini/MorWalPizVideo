# Ask setup and privacy

## Public host

Deploy `frontend/ask.client` as a separate Node SSR application at `ask.morwalpiz.com`. The versioned delivery path is `.github/workflows/deploy-ask-client.yml` plus `frontend/ask.client/Dockerfile`; set the repository variable `ASK_CLIENT_APP_NAME`. Build locally with `yarn build:ask-client` and run it with the app's `serve` script. Set `API_BASE_URL` (or `VITE_API_BASE_URL`) to ServerAPI, configure `VITE_SITE_KEY`, terminate TLS at the edge, and forward `/api` requests to ServerAPI. The app expects `X-Forwarded-Host` and `X-Forwarded-Proto` to be preserved for canonical metadata.

## Policy defaults

Server defaults are maximum message length 2000, 90-day retention, five submissions per campaign per hour, 60-minute duplicate detection, manual moderation, and required reCAPTCHA. Campaign policy can tighten or relax these values through Backoffice, while ServerAPI remains authoritative. `EnableMock` is permitted only in Development/Test and is never a production fallback.

## Privacy and operations

Submission text and an optional name are stored for moderation until `retentionUntil`. Do not log CAPTCHA tokens or raw submissions. When `FeatureManagement:EnableHangFire` is enabled, `ask-retention-job` runs daily at `0 2 * * *`; override it with `Ask:RetentionCron`. Backoffice provides permission- and channel-scoped QR/share, response visibility, aggregate analytics and escaped CSV export; names are excluded by default. Public API responses expose only approved questions and explicitly public responses.

When `moderationMode` is `AiAssisted`, ServerAPI invokes the existing Semantic Kernel chat provider through `IAskModerationProvider`. The adapter requests structured `AskModerationResult` JSON containing score, categories, decision, and a short generic reason. It uses the existing server-side `AzureConfig:OpenAi` configuration, never exposes credentials to `ask.client`, and applies a five-second linked timeout. Missing configuration, invalid output, timeout, or provider failure leaves the submission `Pending` for manual review; no raw content is logged or placed in audit data. Automatic expiry changes a published campaign to `Closed` with `ClosedAt` before public reads return not-found.

Validation status: `AskServiceTests` contains six passing backend unit tests for lifecycle, isolation, policy variants, duplicate/idempotency/rate-limit, moderation fallback, responses, reactions, analytics, and retention. The existing BackOffice Ask permission test passes, and `yarn build:ask-client` completes both browser and SSR builds. Endpoint-level CAPTCHA/controller authorization and SSR runtime tests remain follow-up coverage.