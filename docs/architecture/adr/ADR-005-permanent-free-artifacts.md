# ADR-005: Permanent-Free Digital Artifacts

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

The shop is an experimental catalog for digital images/artifacts. Current APIs contain payment-shaped contracts, incompatible checkout routes, unvalidated tokens, and no durable acquisition/download behavior. Products will remain free permanently.

## Decision

Model catalog items as permanently free digital artifacts. A server-owned anonymous cart cookie identifies the current cart. Adding an artifact creates an idempotent durable free acquisition. Download requires that acquisition. No payment method, payment intent, or mutable price exists in public contracts.

Future verified customer accounts may claim anonymous acquisitions. If paid products are ever introduced, they are separate editions and never convert an acquired free artifact.

## Alternatives

- Public originals with no acquisition: rejected because cart semantics and future continuity disappear.
- Full customer login now: rejected because identity and analytics are deferred.
- Simulated checkout: rejected because it creates false commerce semantics.

## Consequences

Anonymous cookie loss means users add the free artifact again. Durable acquisitions support future account attachment without requiring current personal data.

## Migration And Rollback

Introduce new DTOs and acquisition storage before removing payment fields. Compatibility adapters may serve the experimental client temporarily.

## Validation

Test idempotent acquisition, cookie tampering, cross-cart denial, permanent free semantics, and future owner attachment rules.