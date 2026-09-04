# Ask

Ask is the public, channel-scoped campaign and submission feature. A campaign always belongs to one `YTChannel`; its public URL is `https://ask.morwalpiz.com/<channel-name>/<campaign-slug>` (the host is supplied by deployment/runtime configuration).

## Data model

`AskCampaign` is stored in `askCampaigns`. `AskSubmission` is stored separately in `askSubmissions`, rather than embedded, because submissions are unbounded and require moderation, retention and indexed operational queries. This leaves room for later archival/export without changing the campaign document. A declarative unique compound index `{ channelId: 1, slug: 1 }` prevents collisions inside a channel while allowing the same slug in different channels.

Campaign lifecycle is `Draft`, `Published`, `Closed`, `Archived`. Submissions are `Pending`, `Approved`, `Rejected`, `Spam`, or `Deleted`. Public reads expose only published campaigns and approved questions; a response is included only when its visibility is `Public`. Names, moderation state and retention data are never public.

## Baseline policy

Defaults are conservative and overridable from `Ask:*` configuration or campaign policy: maximum text length 2000, retention 90 days, five submissions per campaign per hour, duplicate window 60 minutes, manual moderation, and reCAPTCHA required. Retention is represented by `retentionUntil` and should be executed by an idempotent scheduled job calling `DeleteExpiredAsync`; no raw submission, CAPTCHA token, or sensitive request data belongs in logs.

## API and security

BackOffice owns channel-scoped CRUD, lifecycle and moderation at `/api/Ask`; the existing `RequireChannelScope` and `AllowUser` permission model are authoritative. ServerAPI owns anonymous public `GET /api/ask/{channelName}/{campaignSlug}` and `POST /api/ask/{channelName}/{campaignSlug}/submissions`. Server-side validation, reCAPTCHA, length checks, duplicate/idempotency checks and rate limits apply regardless of client behavior.

Mongo repositories are registered only in the production branch and scenario repositories only when `FeatureManagement:EnableMock` is enabled. Reactions use a unique `(submissionId, fingerprint)` index and a short server-side limiter. Backoffice export is channel-scoped, escaped CSV, and excludes names unless explicitly requested. Cache identities and output tags are lowercase invariant (`ask`, `tag-ask`).

## Deployment and privacy

The standalone `frontend/ask.client` app is the only owner of `ask.morwalpiz.com/<channel-name>/<campaign-slug>`; the main `morwalpizvideo.client` no longer mounts this route. It uses the same shared services package, runtime `API_BASE_URL`/`VITE_API_BASE_URL`, Vite SSR build, Express SSR host and Helmet metadata conventions as the existing public app. Provision DNS, TLS, a reverse proxy, Node.js 18+, and a process serving `npm run build:all && npm run serve` before launch. The proxy must forward `/api` to ServerAPI and preserve `Host`/`X-Forwarded-Proto` for canonical URLs.

Configure `VITE_SITE_KEY` at build time and the reCAPTCHA secret through the existing ServerAPI secret/configuration providers. Configure `Ask:*` in every environment: `MaxSubmissionLength` 2000, `RetentionDays` 90, `RateLimitPerHour` 5, `DuplicateWindowMinutes` 60, and `RecaptchaRequired` true are the defaults. Mock repositories are enabled only by `FeatureManagement:EnableMock` in Development/Test; production always uses Mongo repositories. Publish the applicable privacy notice, deletion/access procedures, and retention contact because names and user-authored text are stored until expiry.

## Verification status

`frontend/ask.client` is a first-class Yarn workspace and Aspire resource (`ask`, port 5176 in development). CI builds its browser and SSR bundles. The deployment workflow builds its SSR Node image and deploys it to `ASK_CLIENT_APP_NAME`; the edge must route `ask.morwalpiz.com` and `/api` as described in the setup guide. `morwalpizvideo.client` no longer mounts Ask and its orphaned route sources were removed.

Backoffice forms expose start/end dates and the detail screen consumes analytics, share/QR, CSV, response visibility, moderation actions, and text/status filters. Expired published campaigns are persisted as `Closed` on the first public read or submission attempt with `ClosedAt` set; the public projection remains published-only.

Dedicated backend coverage now exists in `MorWalPizVideo.BackOffice.Tests/Services/AskServiceTests.cs`: six executable tests cover lifecycle/expiry, channel isolation and normalized slugs, named/anonymous policy, duplicate/idempotency/rate-limit, AI reject plus provider failure fallback, and response/reaction/analytics/retention behavior. The BackOffice permission test covers Ask route permissions. The standalone Ask browser and SSR bundles also compile successfully.

## Known limitations

QR generation/share, CSV export, reactions, aggregate analytics and admin responses are backed by the Ask APIs. The current rate limit is repository-backed and campaign-wide; the reaction limiter is fingerprint-based and should move to a distributed limiter for multi-node deployments. AI-assisted moderation is wired through the existing Semantic Kernel `IChatCompletionService` abstraction in ServerAPI. It returns `AskModerationResult` (score/categories/decision/reason), sends no client secret, bounds execution to five seconds, and leaves submissions `Pending` on missing, invalid, timed-out, or failed provider responses. The AI assessment is not written to audit logs and the prompt forbids echoing raw content; moderation policy remains stored separately on the campaign.

The remaining release-gate gap is endpoint-level integration coverage for ServerAPI CAPTCHA failure, BackOffice controller authorization/actions, CSV privacy, and SSR runtime loader/error rendering. These paths are compiled and existing permission/build checks pass, but they are not represented by dedicated executable tests yet. Closed/archived/expired public responses map to not-found rather than a distinct `410`.