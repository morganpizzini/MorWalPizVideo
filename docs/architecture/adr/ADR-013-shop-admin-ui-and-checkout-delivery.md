# ADR-013: Shop Admin UI And Checkout Delivery Are Out Of Scope

- **Status:** Proposed
- **Date:** 2026-08-02

## Context

ADR-001 moved digital-product/category admin CRUD into BackOffice (`DigitalProductsController`, `DigitalProductCategoriesController`, both authenticated via `ApplicationControllerBase`, backed by existing `DataService` methods) and moved anonymous shop auth/cart/catalog exclusively onto ServerAPI. Two gaps remain unaddressed by that work:

1. `back-office-spa` has no routes/pages calling the new `DigitalProductsController`/`DigitalProductCategoriesController` endpoints, and no UI or contract exists for admin cart visibility/management (`shopService.ts` has no admin cart-listing functions; `DataService` cart methods are unexposed to any controller for admin use).
2. ServerAPI's `ShopCartController.Checkout` returns `{success, message, orderId}`, while the frontend `CheckoutResponse` model expects `{orderId, downloadLinks, totalAmount}`. No blob-storage download-link generation exists for completed orders; a customer currently receives no way to retrieve purchased digital content after checkout.

## Decision

Treat both gaps as explicitly out of scope until scoped separately. Do not build placeholder UI pages or a placeholder checkout-delivery mechanism as a side effect of unrelated work. Admin management of digital products/categories and carts remains a manual/API-direct operation until `back-office-spa` pages are designed. Checkout remains a cart-clearing operation without content delivery until digital-delivery design is approved.

## Alternatives

- Build minimal admin CRUD pages now by copying the existing `products`/`productCategories` route pattern: rejected for this ADR because admin cart-management UX (view-only vs. refund/cancel/mark-completed) is undefined and would be guessed.
- Return a fake or empty `downloadLinks` array from checkout now to satisfy the contract shape: rejected because it would silently ship non-functional purchases without signaling the gap.

## Consequences

Digital product/category creation and editing require direct API calls (e.g., via BackOffice Swagger/API client) until `back-office-spa` UI ships. Admin has no cart visibility. Customers completing checkout receive an order confirmation but no working delivery of purchased content; this blocks shop launch until resolved.

## Migration And Rollback

Not applicable; no code changes are introduced by this ADR. Future work implementing either gap should land as additive `back-office-spa` routes and an additive checkout-response extension, versioned per ADR-003 if the `CheckoutResponse` contract shape changes.

## Validation

None yet. Follow-up ADRs or specs must define acceptance criteria before implementation: admin cart UX requirements, and the digital-delivery mechanism (blob SAS URLs vs. redirect endpoint vs. email delivery) and its expiry/security model.