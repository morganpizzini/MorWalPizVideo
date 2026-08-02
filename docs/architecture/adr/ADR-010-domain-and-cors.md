# ADR-010: Canonical Domains And CORS

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

The public frontend runs on Aruba and calls Azure ServerAPI directly. The admin SPA has a separate Azure origin. Existing policies can fall back to allow-all and contain outdated origins.

## Decision

Canonical public domain is `morwalpiz.com`; branded redirects use `shorts.morwalpiz.com`. Production CORS is least privilege:

- ServerAPI allows `https://morwalpiz.com` for current public browser calls.
- BackOffice allows `https://morwalpiz-admin-spa.azurewebsites.net` with credentials.
- ShortLinks requires no CORS for redirects.

Add other origins only when an actual browser API call requires them. Local Development allows all origins. Configure host filtering, DNS/TLS, and forwarded proxies separately.

## Alternatives

- Wildcard all subdomains: rejected because abandoned/compromised subdomains expand trust.
- Allow all when a flag is false: rejected as fail-open behavior.
- Proxy production API calls through Aruba: rejected because no such topology exists or is required.

## Consequences

Origin changes require configuration updates. Public and admin credential policies remain isolated.

## Migration And Rollback

Deploy policy tests and settings before removing old origins. Rollback restores a previous explicit list, never allow-all production behavior.

## Validation

Test accepted origins, lookalike rejection, preflights, credentials, host filtering, and direct Aruba-to-Azure calls.