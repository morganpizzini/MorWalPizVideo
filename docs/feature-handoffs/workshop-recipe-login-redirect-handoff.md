# Workshop recipe login redirect handoff

## Implementation — 2026-11-26

The shared account transport now invokes its unauthorized callback only for
protected HTTP 401 responses. Other HTTP errors retain their status and error
details and are surfaced by the existing callers. Authentication endpoint
exclusions and CSRF retry remain unchanged.

Changed files:

- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts):
  restrict the callback to 401.
- [apiTransport.test.ts](../../frontend/fe-packages/services/src/apiTransport.test.ts):
  verify 400, 403, 404, 409, 429 and 500 do not invoke the callback, while
  401, empty 401, authentication exclusions and CSRF retry remain covered.
- [component.test.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.test.tsx):
  verify a journal error is shown inside the selected recipe dialog.

Validation:

- Before the fix, the six non-401 regression cases failed because each called
  the unauthorized handler.
- `cd frontend && yarn workspace @morwalpizvideo/services build` — passed.
- `cd frontend && yarn workspace @morwalpizvideo/services test src/apiTransport.test.ts src/publicAccountService.test.ts src/workshopService.test.ts` — 31 tests passed.
- `cd frontend && yarn workspace shooter-workshop.client test src/routes/Recipes/component.test.tsx src/routes/Ammunition/route.test.ts` — 13 tests passed.

Residual verification: the original failing request and status remain unknown.
Verify the rebuilt client on the deployed origin and investigate any underlying
journal or other API error; a genuine 401 still redirects to login. Browser
visual verification and broad build, lint and test suites were not run.

## Outcome

Opening a recipe must not send an authenticated user to login because an
unrelated API request failed. Preserve login redirection for an expired
session. The exact failing request and status in the reported environment are
not yet known; the reported URL may be a browser URL or a network request.

## Current Evidence

- [Recipes/component.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.tsx)
  opens details by setting the `recipeId` query parameter. The current browser
  route is `/ammunition?tab=recipes&recipeId=...`, not a recipe detail path.
  The distinct production route is `/recipes/:recipeId/produce`.
- [Ammunition/loader.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts)
  revalidates when selection changes and starts recipe, batch, inventory and
  production reads; for a selected owned recipe it also reads its journal.
- [workshopService.ts](../../frontend/fe-packages/services/src/workshopService.ts)
  sends Workshop requests through the shared account client. The journal path
  defined in [endpoints-frontend.ts](../../frontend/fe-packages/services/src/endpoints-frontend.ts)
  matches the route in [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs).
- In [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts),
  the account-mode error branch calls the unauthorized handler for **any**
  unsuccessful protected request except `csrf_invalid`, not only 401. Thus a
  400, 403, 404, 409, 429 or 500 can cause the redirect even with a valid
  session. The response retains its status.
- [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx) installs a
  handler that replaces the page with login, explaining the hard refresh.
  [shared.tsx](../../frontend/shooter-workshop.client/src/routes/shared.tsx)
  separately redirects on a deferred `ApiResponseError` with status 401.
- Existing [apiTransport.test.ts](../../frontend/fe-packages/services/src/apiTransport.test.ts)
  checks protected 401, authentication endpoint exclusion and CSRF retry,
  but not the non-401 redirect regression.

The transport defect is verified in source. It explains the symptom **if**
any revalidation request returned a non-401 error; it does not establish which
request failed or why. A genuine 401, deployment mismatch, session/cookie
issue, or an unexpected URL remains possible until browser evidence is obtained.

## Proposed Design

In the existing account-mode error branch, invoke the unauthorized handler only
for an actual protected-request HTTP 401. Preserve authentication endpoint
exclusions, response status and error propagation, CSRF refresh and retry,
cookie credentials, and all other transport modes. Do not mask failures or
remove server authorization. Existing deferred error UI should show a failed
journal read while retaining recipe details; confirm this with a focused test.

This is a behavior correction, not a visual redesign. If visual validation is
needed, use the installed Impeccable workflow as a narrow `harden` refinement
of the Workshop Operate surface, preserving the existing UI; check the dialog,
error presentation and keyboard access at desktop and mobile widths. The recipe
journal handoff records no Workshop-specific PRODUCT.md or DESIGN.md.

## Impacted Projects

- `@morwalpizvideo/services`: shared account transport and regression tests.
- `shooter-workshop.client`: recipe flow verification and a focused regression
  test; no route or layout redesign.
- Other consumers of shared account transport, including the public client and
  FreezerAnalysis SPA: verify compatibility. FreezerAnalysis creates its own
  account client; Workshop registers the identified account callback.
- ServerAPI and deployment: verify the deployed journal endpoint and release
  versions only; no server change is justified without further evidence.

## Impacted Files

### Modify

- [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts):
  restrict account unauthorized callbacks to 401.
- [apiTransport.test.ts](../../frontend/fe-packages/services/src/apiTransport.test.ts):
  cover non-401 errors, preserved status and existing authentication behavior.
- [component.test.tsx](../../frontend/shooter-workshop.client/src/routes/Recipes/component.test.tsx)
  or an existing route-level Workshop test: cover selected recipe and journal
  error presentation with the real relevant boundaries.

### Add

None identified.

### Generated/Migration

None; no schema or contract change.

### Documentation

- Update this handoff with actual implementation, test results and outstanding
  deployed-environment evidence.

### Verify Only

- [Ammunition/loader.ts](../../frontend/shooter-workshop.client/src/routes/Ammunition/loader.ts),
  [shared.tsx](../../frontend/shooter-workshop.client/src/routes/shared.tsx)
  and [main.tsx](../../frontend/shooter-workshop.client/src/main.tsx): ensure
  error and expired-session behavior remain correct.
- [WorkshopController.cs](../../MorWalPizVideo.ServerAPI/Controllers/Workshop/WorkshopController.cs)
  and [ADR-030](../architecture/adr/ADR-030-shooter-workshop-recipe-journal.md):
  compare deployed API availability to the additive journal contract if a
  journal request fails.

## Impact Matrix

| Area | Impact |
| --- | --- |
| Backend/API | No source change proposed; investigate actual response separately. |
| Frontend/UI | Non-401 failures remain on the page and surface as errors. |
| Database/Data | None; do not alter or delete recipes or journal data. |
| Contracts/Models | No request, response or model change. |
| Authentication/Authorization | Preserve real 401 redirect and server enforcement. |
| Validation | Surface 400 and other errors rather than treating them as expiry. |
| Cache | No change. |
| Configuration/Secrets | No changes; do not collect cookies or tokens during investigation. |
| Logging/Telemetry | Existing errors remain visible; inspect sanitized status/path evidence. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Deploy shared service build with Workshop; verify server/client versions and actual browser behavior. |
| Backward Compatibility | Corrects callback behavior across account-mode clients; non-401 status payloads remain unchanged. |

## Reuse Opportunities

Reuse account-mode transport, `ApiResponseError`, existing deferred error state,
authentication exclusions, CSRF retry and established Workshop route tests.

## Risks

- **High — underlying request failure remains:** fixing false redirects does
  not fix a 404/500, old server or genuine 401. Capture the failing request's
  sanitized path and status and verify the corresponding deployed endpoint.
- **Medium — expired sessions stop redirecting:** protect 401 behavior with
  tests, including non-JSON responses and authentication endpoint exclusions.
- **Medium — shared transport regression:** verify 400/403/404/409/429/500
  retain error status and do not call the handler; check CSRF retry and account
  consumers without changing admin/public modes.
- **Low — recipe content lost on journal failure:** verify the dialog and
  already-loaded recipe remain visible while the journal displays an error.

## Technical Debt

The global hard redirect and duplicated 401 handling are existing behavior;
redesigning authentication navigation is out of scope. Broad loader
revalidation can generate several requests on selection; optimization is
separate work unless evidence proves it is necessary for this fix.

## Implementation Roadmap

1. Capture the actual browser address and sanitized network statuses for recipe
   selection. Identify the first failing request; never capture credentials.
2. Add focused account transport regression cases and restrict the callback to
   401. Preserve exclusions, error payloads and CSRF behavior.
3. Verify the Workshop recipe remains open with a journal error and that a real
   401 still navigates to login with a safe return URL.
4. Build the shared services package before Workshop, run targeted tests and
   build, and document results here. If the deployed request still fails,
   investigate its status separately rather than hiding it.

Stop after this correction and its verification. Independent review is optional
for this narrow change; shared authentication behavior makes focused security
regression checks essential.

## Testing Strategy

- Services unit tests: protected 401 invokes the handler once; account
  authentication 401 remains excluded; non-401 errors do not invoke it and
  preserve their error/status; CSRF invalid still refreshes and retries once.
- Workshop route/component test: selection preserves tab/query and visible
  recipe details when journal returns a non-401 error; the error is surfaced.
  Preserve existing success and expired-session coverage.
- Browser verification: authenticated selection on the actual origin,
  successful journal load, simulated non-401 failure without navigation, and
  genuine session expiry with login return path. Check mobile and keyboard
  behavior where the dialog is involved.
- Server contract/security, migration and full end-to-end testing are not
  otherwise changed; check the deployed journal route if it is the failure.

## Migration And Rollback

No MongoDB, SQLite, API contract, auth or configuration migration is needed.
Release the rebuilt services dependency with the Workshop client and verify on
the target origin. Roll back the client artifact if needed; rollback restores
the known false-redirect behavior. Preserve all journal and recipe data.

## Open Questions

- Is the reported string the browser address or a request URL, and which
  request returned which HTTP status at the moment of redirect?
- Are the deployed Workshop client and ServerAPI versions journal-compatible?
