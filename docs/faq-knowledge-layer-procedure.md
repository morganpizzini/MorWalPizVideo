# FAQ knowledge layer operating procedure

1. Create or select a domain-managed FAQ category in BackOffice. Use a stable lowercase slug; never encode channel ownership in the category.
2. Create the global FAQ in `Draft`, edit the canonical question, and save it without selecting AI. Manual creation and publication do not require an AI provider.
3. Ask each channel owner to create or edit their own answer under the FAQ. Select `Published` on an answer only after editorial review. A channel owner must keep the active channel context selected.
4. Publish the FAQ only when its question and category are ready. Archive the FAQ or an individual answer independently; archival of a Campaign never changes FAQ content.
5. For AI assistance, select Campaigns explicitly and call candidate generation. Only submission text belonging to those selected Campaigns is analyzed; names, metadata, and unselected Campaigns are excluded from the AI source.
6. Review generated candidates. They remain `Pending` and are never public until a person edits and approves the candidate by creating or updating the FAQ and its channel answer separately. A provider failure or timeout leaves a pending diagnostic candidate and an operational error for retry or investigation. Do not pass user names, IPs, headers, tokens, or unrelated technical data to AI.
7. Monitor FAQ cache invalidation, category/answer index health, vote-counter drift, request rate limits, and FAQ query volume. Vote records are authoritative; answer counters are an eventually consistent cache. Hangfire runs the idempotent `faq-vote-reconciliation-job` every 15 minutes by default (`Faq:VoteReconciliationCron` controls the schedule).

8. To repair counters immediately, an administrator with `backoffice.manageall` may call `POST /api/faq-maintenance/votes/reconcile`. Send `{}` to scan all answers or `{ "answerIds": ["<answer-id>"] }` for a focused repair. The endpoint is BackOffice-only and must not be proxied through the public ServerAPI.

9. Recovery procedure: confirm Mongo connectivity and the unique `faqvotes_answerid_userid` index, run a focused reconciliation for affected answers, then rerun it after active voting subsides. Compare the structured reconciliation counts and public FAQ output. If concurrent-change observations remain high, keep the scheduled job active and investigate vote traffic or deployment contention; do not manually edit denormalized counters or delete authoritative vote records.

Public readers receive only published FAQs, active categories, and published answers. Voting is available only after authentication; duplicate and changed votes are handled per answer.