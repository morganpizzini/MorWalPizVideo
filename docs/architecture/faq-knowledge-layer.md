# FAQ knowledge layer architecture

## Scope and boundaries

FAQ is a global knowledge entity. It is separate from `AskCampaign` and `AskSubmission`; the Ask workflow remains the active campaign and anonymous-submission tool. No submission migration, backfill, retention change, or controller change is part of this feature.

An FAQ has a lifecycle of `Draft`, `Published`, or `Archived`. Answers are separate documents with the same lifecycle and are owned by one channel. A FAQ can therefore have one independently managed answer per channel without making the FAQ lifecycle depend on a channel or Campaign lifecycle.

## Mongo model

The model uses five collections:

- `faqs`: global question, category reference, lifecycle, publication timestamp, and non-public source metadata.
- `faqCategories`: extensible slug/name/description/order records managed in BackOffice.
- `faqAnswers`: FAQ/channel/content/lifecycle and vote counters. The unique `(faqId, channelId)` index prevents duplicate channel answers.
- `faqCandidates`: AI or human-review candidates with selected Campaign IDs, relevance, duplicate suggestion, and review status.
- `faqVotes`: authenticated user vote records. The unique `(answerId, userId)` index prevents duplicate votes.

FAQ source metadata contains only source type, selected Campaign IDs, provider, and generation time. It never controls FAQ or answer lifecycle.

## APIs and DTO privacy

ServerAPI exposes `GET /api/faq` and `GET /api/faq/categories`, plus authenticated `POST /api/faq/{faqId}/answers/{channelName}/vote`. Public DTOs contain only question/category and published answers with channel name, content, and vote counters. They do not expose users, submissions, Campaigns, source metadata, moderation, AI, or internal admin data. Answers are sorted by channel name case-insensitively and then by stable ID.

BackOffice exposes FAQ search/filter/list/detail, category management, manual FAQ lifecycle, answer management, candidate listing/review, and voting counters. `RequireChannelScope` remains in force. An owner can mutate only answers whose `channelId` matches the selected channel context; global FAQ editing is permission-controlled.

## AI and candidate workflow

AI generation is explicit and accepts manually selected Campaign IDs only. BackOffice loads submissions only through `IAskSubmissionRepository.GetByCampaignIdAsync` for those IDs and sends only each submission's text to the existing Semantic Kernel/Azure OpenAI infrastructure. The structured prompt asks the provider to semantically cluster, identify topics, canonicalize questions, deduplicate, suggest duplicate FAQ IDs, and score relevance. Candidate records retain selected Campaign IDs and a submission count, not raw submission context.

`POST /api/faq/candidates/generate` creates `Pending` candidates only. Provider calls have a bounded timeout and one retry; provider failure or timeout records a pending diagnostic candidate and returns an explicit `503` operational error. A person must edit and approve or reject a candidate; no endpoint auto-publishes an FAQ or answer, and the AskCampaign/AskSubmission records are never mutated.

## Voting, privacy, and abuse

Voting requires the existing authenticated identity and is per answer. Repeating the same value is a no-op; changing value updates the vote record and atomically increments/decrements the answer counters. Mongo uniqueness handles concurrent duplicate insertion. No IP, email, submission, or technical payload is stored in `faqVotes`; only the authenticated user ID and timestamps are retained. A production rate-limit policy should be applied through the platform's authenticated API limiter before enabling high-volume public voting.

`FaqVote` records are authoritative. `FaqAnswer.HelpfulVotes` and `FaqAnswer.NotHelpfulVotes` are denormalized caches and are therefore eventually consistent. The reconciliation service aggregates persisted votes by answer, atomically sets only the two counter fields, and verifies the vote snapshot. It retries an answer up to three times when a vote changes during reconciliation, without replacing the whole answer document. Reconciliation is idempotent and can be limited to selected answer IDs.

## SSR and SEO

FAQ is public in `frontend/shooting-ita-frontend`. It now has a route loader, browser hydration-safe startup, `entry-server.tsx`, and Express `server.mjs`. Its SSR loader fetches the same public API data as the browser and emits FAQ title, description, and canonical metadata. This resolves the previous mismatch where Shooting ITA was browser-only while `frontend/morwalpizvideo.client` already used SSR. The API client remains credential-omit for this public application.

## Operations and limitations

There is no artificial FAQ aggregate-size limit. FAQs and answers are separate collections so growth does not enlarge one Mongo document, but unbounded FAQ counts still require indexes, pagination, cache monitoring, and BackOffice query limits as content grows. Counter changes are atomic per Mongo update, but MongoDB transactions are intentionally not assumed. A vote can still commit between reconciliation's final verification and the next vote-record read, so the counters remain eventually consistent and the scheduled job is the recovery mechanism. A sustained high-contention incident may require repeated or scoped reconciliation after voting volume drops.

When Hangfire is enabled, `faq-vote-reconciliation-job` runs every 15 minutes by default. The schedule can be changed with `Faq:VoteReconciliationCron`. A BackOffice administrator with `backoffice.manageall` can run `POST /api/faq-maintenance/votes/reconcile`, optionally supplying `answerIds`; the route is not part of ServerAPI and is never public. The service emits structured completion logs and meter counters for answers scanned, counters repaired, and concurrent changes observed; it never logs user IDs.

FAQ cache invalidation uses the independent lowercase `faq` cache key and `tag-faq` API tag. Existing Ask cache behavior is unchanged.

## Testing and deployment

The domain and API projects build in the normal .NET 10 pipeline. Shared frontend packages must build in order: models, services, then layout/consumer. Shooting ITA uses `npm run build` and `npm run build:ssr`; production runs `node server.mjs` after both bundles exist. Focused tests should cover lifecycle/source independence, category filtering, candidate provider failures, cross-channel answer isolation, public DTO leakage, authenticated duplicate/change voting, ordering, SSR loader metadata/error states, vote drift repair including zero-vote and concurrent-change cases, and BackOffice authorization.