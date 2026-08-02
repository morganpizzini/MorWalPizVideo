# ADR-011: Provider-Neutral Transactional Email

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Customer identity and email notifications are deferred. A `@morwalpiz.com` mailbox will provide sender identity, but no application email transport or provider exists.

## Decision

When a workflow requires email, introduce a provider-neutral transactional sender interface. Prefer an HTTP API adapter following named HttpClient/options/mock/health patterns; SMTP remains an adapter option. Verify a `@morwalpiz.com` sender and configure SPF, DKIM, DMARC, bounces, complaints, suppression, and signed webhooks.

Customer workflow ownership determines the host. Share the abstraction only when multiple hosts genuinely send mail.

## Alternatives

- Bind directly to one vendor SDK: rejected because no provider precedent exists and portability is inexpensive at the boundary.
- Use mailbox SMTP unconditionally: rejected because delivery telemetry and webhook handling may be limited.
- Implement now: rejected because no current required workflow justifies it.

## Consequences

Email remains deferred without blocking future verification. Operational deliverability is recognized as more than mailbox creation.

## Migration And Rollback

Add one adapter and mock when selected. A second adapter can replace it behind the same interface.

## Validation

Test template data, retries, idempotency, webhook signatures, bounce/suppression behavior, and safe logging.