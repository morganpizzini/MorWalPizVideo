# ADR-017: Anonymous Per-Channel Web Push

- **Status:** Accepted
- **Date:** 2026-10-15

## Context

`MorWalPizVideo.ServerAPI` already contained a Web Push surface (`PushController`, `WebPushService`, the `WebPush` 1.0.13 package) whose subscriptions are embedded in `User.PushSubscriptions`. That surface requires an authenticated BackOffice user, so it cannot serve the public applications: `frontend/morwalpizvideo.client` and `frontend/shooting-ita-frontend` have no end-user accounts at all and public API calls run with `credentials: 'omit'`.

The approved feature is anonymous, per-channel notification consent on the public applications, plus two administrative sending surfaces: a platform-wide section and a channel-owner section. Audiences had to be reusable named groups independent of the RBAC model, because an audience answers "which channel populations receive this message" rather than "who may act".

Fanout must survive restarts and repeated execution: a single platform send can address every active subscription of every channel, and browser push endpoints fail individually, transiently, and permanently.

## Decision

Model anonymous subscriptions as a first-class `PushChannelSubscription` document keyed by a SHA-256 `EndpointHash`, with a 256-bit credential minted once at subscription time and persisted only as a hash. The browser keeps the credential in local storage; the server never returns the endpoint or the credential again. Settings reads, channel updates and revocation require a fixed-time credential-hash match. This gives anonymous users durable self-service without accounts, cookies, or server-held identifiers.

Expose the anonymous surface on ServerAPI at `api/push/subscriptions/*` under a dedicated `push-public` rate-limit policy, leaving the legacy user-bound `api/push/*` routes untouched.

Keep sending in BackOffice. `push.platform.send` is the canonical permission for the platform section; the channel section requires only `backoffice.access` and derives its sole target from the existing channel scope, so no recipient picker exists and a channel owner can never address another channel's subscribers.

`PushAudience` is a named, coded collection of channel ids owned by the push feature, not by RBAC. Select-all over channels and over audiences is a client-side convenience that still resolves to an explicit server-side channel set.

Snapshot recipients at dispatch time. `PushDispatchService.QueueAsync` claims the dispatch, writes one `PushDispatchRecipient` per active consenting subscription with idempotency key `{dispatchId}:{endpointHash}`, records `RecipientCount`, and only then enqueues the Hangfire job. The durable queue is the snapshot, not the Hangfire enqueue; a recurring reconciliation job drains anything stalled.

Notification destinations are same-origin relative paths. `PushDestination.Normalize` rejects absolute, protocol-relative and traversal values for the notification body and for every action button. Action buttons are optional and clamped to a configured `WebPush:MaxActions`; the service worker additionally slices to `Notification.maxActions` and falls back to the body destination when no action matched.

Every log or response path that touches an endpoint goes through `PushEndpointProtection.Redact`, including the legacy `WebPushService` logs.

## Alternatives

- Extend `User.PushSubscriptions`: rejected because the public applications have no users and the embedded array cannot be queried per channel or expired per endpoint.
- Derive audiences from roles, groups or permissions: rejected because the request explicitly separates "who receives" from "who may act", and reusing RBAC would make a messaging change an authorization change.
- Let channel owners pick recipients: rejected; the approved behaviour is "broadcast to all subscribers of owned channels", and a picker would expose subscriber-level data that the redaction rule forbids.
- Switch the public applications to `injectManifest`: rejected because it would replace the whole generated-service-worker/offline behaviour. A static `public/push-sw.js` imported through `workbox.importScripts` adds push handling without changing precaching.
- Treat Hangfire availability as a precondition and return `409` like `NewslettersController`: rejected because the approved contract is a durable queue. The snapshot is written first and the controller returns `202 Accepted`; delivery is deferred when Hangfire is absent instead of losing the request.
- Put the opt-in prompt in `@morwalpiz/layout`: rejected because the two public applications have incompatible visual systems. Only the framework-agnostic browser logic is shared, through `@morwalpizvideo/services`.

## Consequences

Four additive MongoDB collections exist: `pushSubscriptions`, `pushAudiences`, `pushDispatches`, `pushDispatchRecipients`. No existing document shape changes, and the legacy embedded subscriptions keep working.

Dispatch is idempotent: re-queuing an already-claimed dispatch is a no-op, and `EnsurePendingAsync` prevents duplicate recipients. `410 Gone` and `404` deactivate the subscription and mark the recipient `Suppressed`; `400/401/403/413` mark it `Rejected`; anything else raises `PushTransientException` so Hangfire retries.

`PushDispatchService` is registered both as its interface and as a concrete type because Hangfire's `AspNetCoreJobActivator` resolves the concrete job type from DI.

A user who clears local storage loses control of that subscription until the browser endpoint rotates or they revoke from the browser itself. This is the accepted cost of having no accounts.

The feature cannot deliver until VAPID material is provisioned out of band: `WebPush:PublicKey`, `WebPush:PrivateKey` and `WebPush:Subject` in BackOffice, and `WebPush:PublicKey` in ServerAPI. All committed values are empty strings; the public-key endpoint returns `503` when unconfigured. `docs/SHOOTING_ITA_PHASE4_ADVANCED_FEATURES.md` describes a `VapidKeys:*` shape that was never implemented; that document is already listed as superseded, and `WebPush:*` in source is authoritative.

`frontend/shooting-ita-frontend` previously built a service worker that was never registered. Registration is now explicit in `main.tsx`, so that application gains its configured PWA behaviour along with push.

## Migration And Rollback

All schema changes are additive; no backfill is required and existing documents remain readable. Deploy ServerAPI and BackOffice before the public clients so that subscribe calls do not 404. Rolling back the clients leaves orphan subscriptions that simply stop being renewed; rolling back the APIs leaves clients unable to subscribe but does not corrupt stored data.

Removing VAPID configuration disables delivery without data loss: queued dispatches remain `Queued` with their snapshot intact, and the reconciliation job resumes them when configuration returns. Revoking VAPID keys invalidates every stored endpoint and requires all users to opt in again.

## Validation

- `MorWalPizVideo.BackOffice.Tests` — `PushSubscriptionEndpointTests` (5) and `PushCampaignTests` (7): credential minted once and never echoed, credential enforcement on re-subscribe/settings/revoke, endpoint validation, `503` when unconfigured, permission gating, audience CRUD with duplicate and unknown-channel rejection, snapshot excluding inactive subscriptions, idempotent re-queue, audience resolution, same-origin destination and action rejection, action clamping, channel-scoped broadcast, and batch completion.
- `@morwalpizvideo/services` — `pushNotifications.test.ts` covers prompt/dismissal persistence, credential storage and the opt-in flow.
- `frontend/shooting-ita-frontend` — `PushOptIn.test.tsx` covers support detection, dismissal and opt-in.
- `frontend/back-office-spa` — `src/routes/push/__tests__/index.test.tsx` covers platform-section permission gating, select-all, audience targeting, the `maxActions` ceiling and the channel broadcast.
- Browser capture of the two public prompts and the BackOffice page was NOT performed; no visual evidence exists for this change.
- Mongo `AnyIn` channel filtering is exercised only against mock repositories; target-database verification remains outstanding.
