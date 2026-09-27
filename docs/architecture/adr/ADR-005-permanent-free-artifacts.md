# ADR-005: Commercial Digital Artifacts With Manual Payment

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

The shop sells digital artifacts. Payment providers are deliberately deferred, but every checkout is commercial, including a valid zero price. Orders, customer access, and delivery must remain durable and auditable.

## Decision

Model catalog items with a mandatory non-negative price. Checkout always persists an order with item price snapshots and a manual payment status. Customer identity is derived from a persisted server-side session referenced by an HttpOnly, Secure, SameSite cookie; caller-supplied customer IDs are not authority. Orders are idempotent by customer and client idempotency key, and the cart is cleared only after order persistence succeeds.

Original blobs remain private. Authenticated delivery issues read-only SAS URLs valid for 24 hours and may be regenerated without a download counter. Public and customer DTOs never expose the storage key.

The order state and manual payment state are separate. The initial workflow is PendingPayment -> PaymentConfirmed, with Cancelled and access restoration handled by BackOffice. No provider, refund, payment-intent, or on-hold state is part of this perimeter.

## Alternatives

- Public originals with no acquisition: rejected because cart semantics and future continuity disappear.
- Full customer login now: rejected because identity and analytics are deferred.
- Simulated checkout: rejected because it creates false commerce semantics.

## Consequences

The customer area exposes order history and authenticated download regeneration. Zero-price orders still follow the same persistence, payment confirmation, and delivery rules.

## Migration And Rollback

Introduce new DTOs and acquisition storage before removing payment fields. Compatibility adapters may serve the experimental client temporarily.

## Validation

Test zero-price orders, price snapshots, idempotency, cross-customer denial, false/expired sessions, manual payment transitions, SAS expiry, repeated downloads, and storage-key omission.