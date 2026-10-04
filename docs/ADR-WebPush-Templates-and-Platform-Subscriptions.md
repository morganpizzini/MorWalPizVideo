# ADR: reusable Web Push templates and platform subscriptions

## Decision

BackOffice notification templates are persisted in MongoDB with a monotonically
increasing version. Sending resolves a template before queueing and copies title,
body, destination, actions, template id, and version into the immutable
`PushDispatch`; delivery never rereads a mutable template.

BackOffice browser subscriptions use the same anonymous credential protocol as
public clients, but are explicitly scoped to `applicationKey=backoffice` and
are managed only by authenticated platform operators. Destinations remain
same-origin relative paths and actions are capped at two with the body
destination as the fallback.

Public opt-in dismissal is namespaced by application key. Settings remain
recoverable anonymously through the stored endpoint credential. Shooting ITA is
production-disabled until its API origin is configured.

## Operations

Configure `WebPush:VapidSubject`, `WebPush:VapidPublicKey`, `WebPush:VapidPrivateKey`,
and (optionally) `WebPush:MaxActions` in secret configuration. No migration is
required: MongoDB creates `pushNotificationTemplates` on first write.

Verify the service-worker assets (`/push-sw.js`) are present in each public
client build and test registration from the deployed origin before enabling
production opt-in.
