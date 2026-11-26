# Global Feedback

**Status:** Implemented in source; production activation remains gated by
operational, privacy, and real-origin verification.

## Current behavior

Shooter Workshop exposes a global feedback launcher inside its authenticated
route shell. The modal accepts one user-authored description and adds bounded,
typed diagnostic context without capturing DOM content, browser storage,
request bodies, or arbitrary query/fragment values. Failed submissions retain
the draft for retry.

ServerAPI owns anonymous-capable intake at `POST /api/v1/feedback`. Every
submission uses the account-mode CSRF contract, receives a server-created
request identity and receipt, and is persisted before success is returned.
Authentication may enrich attribution but is not required for intake.

BackOffice owns the management surface:

- `GET /api/v1/feedback`
- `GET /api/v1/feedback/{id}`
- `PATCH /api/v1/feedback/{id}/status`
- `DELETE /api/v1/feedback/{id}`

List/detail access requires `feedback.view`; status changes and deletion require
`feedback.manage`, which implies view. The SPA exposes permission-filtered
`/feedback` and `/feedback/:id` routes. Feedback is platform-global: it has no
channel field, does not require an accessible channel, and must not emit or
consume `X-Channel-Id`.

Statuses are `new`, `reviewing`, `resolved`, and `dismissed`. Status updates are
revision-checked and stale updates return a conflict. Deletion is a confirmed
hard delete. ServerAPI exposes no feedback read or management endpoint, and
BackOffice exposes no intake endpoint.

## Persistence and operations

Feedback uses a dedicated global MongoDB collection with:

- a unique client request-ID index for idempotent retries;
- creation-order and status/application query indexes;
- an expiry field used by the declared TTL index.
- BackOffice attempts all four indexes through its non-blocking full-manifest
  startup initializer. Startup does not gate feedback intake or management;
  inspect actual index state through the audit endpoint. Missing unique indexes
  can permit duplicate writes before they are created.

The declared 90-day TTL can delete already-expired documents when created.
Review the privacy notice, unresolved-report policy, existing data and backup
retention before enabling startup creation in an environment. This is an
operational recommendation, not an application startup or readiness gate.
Production should also verify that ServerAPI and BackOffice reach the intended
shared feedback store.

A dedicated limiter protects intake independently from login and newsletter
limits. Trusted-proxy configuration and the effective distributed rate cap
remain operational gates.

## Security and privacy boundary

- Server validation is authoritative; unknown metadata is discarded.
- Diagnostic context is allowlisted, bounded, and displayed as plain text.
- Authentication, account cookies, CSRF tokens, storage values, and page
  content are never copied into a report.
- Management permission is independent of channel membership and API-key
  authorization.
- Responses are `no-store`; logs use server-owned identifiers rather than the
  submitted description.

## Validation record

The 2026-11-14 implementation record reported:

- focused shared feedback transport tests passing;
- Shooter Workshop feedback DOM tests passing;
- BackOffice feedback route tests passing;
- focused backend feedback endpoint tests passing;
- full BackOffice SPA and Shooter Workshop frontend suites/builds passing after
  unrelated router-harness regressions were corrected.

These are historical source-level results, not deployed approval.

## Remaining release checks

1. Confirm the production intake and management hosts share the intended
   collection.
2. Grant `feedback.view` and `feedback.manage` only to reviewed staff groups.
3. Review duplicate data and verify the actual unique/query indexes before
   relying on their guarantees. This is operational risk management, not a
   startup or release gate; missing constraints do not block intake.
4. Review retention and backup policy before enabling automatic TTL creation.
5. Verify real-browser CSRF/CORS behavior from the Workshop origin, including
   authenticated and anonymous intake where enabled.
6. Verify trusted-proxy and effective rate-limit behavior in the deployed
   topology.

Rollback disables client launch and intake before removing permissions. Do not
drop the collection or TTL index as an automatic code rollback; preserve data
for operator review.
