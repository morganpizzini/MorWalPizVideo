# ADR-013: Shop Admin UI And Manual Checkout Delivery

- **Status:** Accepted
- **Date:** 2026-08-02

## Context

ADR-001 moved digital-product/category admin CRUD into BackOffice (`DigitalProductsController`, `DigitalProductCategoriesController`, both authenticated via `ApplicationControllerBase`, backed by existing `DataService` methods) and moved anonymous shop auth/cart/catalog exclusively onto ServerAPI. Two gaps remain unaddressed by that work:

1. `back-office-spa` has no routes/pages calling the new `DigitalProductsController`/`DigitalProductCategoriesController` endpoints, and no UI or contract exists for admin cart visibility/management (`shopService.ts` has no admin cart-listing functions; `DataService` cart methods are unexposed to any controller for admin use).
2. ServerAPI's `ShopCartController.Checkout` returns `{success, message, orderId}`, while the frontend `CheckoutResponse` model expects `{orderId, downloadLinks, totalAmount}`. No blob-storage download-link generation exists for completed orders; a customer currently receives no way to retrieve purchased digital content after checkout.

## Decision

BackOffice owns catalog/category management and order list/detail plus manual payment confirmation, cancellation, and access restoration. ServerAPI owns the customer session, persistent checkout, order history, and authenticated delivery. Checkout never invokes a payment provider. Delivery uses private originals and read-only SAS URLs with a 24-hour lifetime; authenticated customers may regenerate them and downloads are unlimited.

## Alternatives

- Build minimal admin CRUD pages now by copying the existing `products`/`productCategories` route pattern: rejected for this ADR because admin cart-management UX (view-only vs. refund/cancel/mark-completed) is undefined and would be guessed.
- Return a fake or empty `downloadLinks` array from checkout now to satisfy the contract shape: rejected because it would silently ship non-functional purchases without signaling the gap.

## Consequences

The SPA work is an operational surface over these APIs. No refund or provider workflow is implied by cancellation. A zero-price artifact remains an order and is not an acquisition bypass.

## Migration And Rollback

Mongo additions are additive; existing product fields remain readable during rollout. New contracts omit storage keys and use the existing endpoint aliases until consumers migrate.

## Validation

Acceptance covers manual payment state transitions, order idempotency, customer isolation, cookie session expiry, 24-hour SAS expiry, unlimited regeneration/downloads, and admin authorization.