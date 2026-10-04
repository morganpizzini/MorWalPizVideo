# ADR: Web Push endpoint security and recipient scoping

Web Push subscriptions remain anonymous and channel-based for the public applications
(`morwalpizvideo` and `shooting-ita`). BackOffice delivery is platform-scoped and uses
the explicit `backoffice` application discriminator on dispatches.

Subscription endpoints are accepted only when they are HTTPS and DNS resolution
contains no loopback, link-local, private, site-local, or unspecified address.
The same check runs immediately before delivery. The named Web Push `HttpClient`
disables redirects. Endpoint values are never written to logs; only the existing
redacted endpoint helper may be logged.

This is application-layer SSRF mitigation. Deployments must additionally restrict
BackOffice egress to approved Web Push provider networks because DNS can change
between validation and connection establishment; the current WebPush package does
not expose a connection-pinning hook compatible with the factory-owned client.

Legacy subscription documents with an empty application discriminator are excluded
from BackOffice platform fan-out and are not promoted automatically.
