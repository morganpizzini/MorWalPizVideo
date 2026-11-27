# ADR-031: Shared public-account recovery and email confirmation

- **Status:** Accepted; implemented locally, deployed delivery unverified
- **Scope:** ServerAPI public accounts, Shooter Workshop, public site
- **Related:** [ADR-018](ADR-018-public-account-boundary.md), [ADR-011](ADR-011-email-boundary.md)

## Context

The two public-account clients need password recovery and email confirmation.
New registrations must confirm before login; existing accounts temporarily retain
unverified access. The user selected configured SMTP and best-effort delivery.
Freezer uses a separate host, database, and account-cookie boundary.

## Decision

- Domain owns purpose-separated random-token issuance, hashing, expiry, and
  conditional persistence. Mongo and mock repositories implement atomic
  last-issuance-wins and single-consumption operations. Reset changes the password
  and security version; confirmation only binds the current normalized email.
- Add an explicit new-account confirmation gate. Missing legacy fields do not
  lock out existing users. ServerAPI registration creates no session; Freezer's
  existing registration path remains unchanged.
- ServerAPI owns an `IAccountMailSender` SMTP/mock adapter and bounded in-memory
  queue. This selects ADR-011's SMTP option for this workflow; it does not add
  customer/shop workflows or require other hosts to send mail.
- Four anonymous account-mail POST endpoints retain account CSRF/CORS, no-store,
  bounded input, and mandatory per-IP limits independent of login throttling.
  Valid requests return account-independent queue admission, not delivery proof.
- Closed identifiers map only to the configured canonical Workshop/public-site
  origins and fixed reset/confirmation paths. Tokens travel in fragments, are
  removed by an early shared import, and remain only in module memory.
- Models and services share contracts/transport; layout shares forms, notices,
  and a 60-second client-only resend timer. Apps retain local routes and styling.
  Sensitive public routes bypass SSR hydration and third-party widgets.

## Alternatives

- Workshop-only reset destination: superseded by the user's requesting-site rule.
- Mandatory legacy-account lockout: rejected for compatibility.
- Immediate new-registration sessions: rejected because confirmation is required.
- Durable outbox/provider integration: deferred; restart loss and explicit resend
  are accepted. No per-account backend cooldown tracking is introduced.

## Consequences

SMTP requires protected `PublicAccount:Mail` configuration, authenticated STARTTLS,
and a usable sender/mailbox. `SmtpClient` does not support implicit SMTPS.
Queue loss on restart is possible. Sanitized failures are observable, but actual
mailbox delivery requires deployed verification. Offline data already on a device
is not remotely erased by server-session invalidation.

## Migration And Rollback

State is additive; no backfill is required. Deploy both clients and the backend
in a coordinated registration cutover and drain older backend instances. ServerAPI
requires the registration site, while the shared DTO keeps it optional for
Freezer. Optional login site preserves older callers without automatic mail.

After gated accounts exist, keep a policy-aware backend or forward-fix. Returning
to an old ungated backend would bypass the confirmation requirement. Preserve
password/security-version and confirmation state; do not restore stale documents.
No deployed cutover or rollback was performed by this implementation.

## Validation

Focused backend API/state/worker tests and disposable MongoDB atomicity tests,
shared transport/component tests, both client builds, and controlled
desktop/mobile Chromium flows are recorded in the
[delivery handoff](../../feature-handoffs/workshop-password-recovery-handoff.md).
Real SMTP, Key Vault, and deployed-origin CSRF/cookie proof remain release gates.
