# Shooter Workshop Localization Handoff

## Outcome

Make the Shooter Workshop client bilingual, with Italian as the product default and
English as the secondary language. On a browser's first visit, resolve the culture
from the client's language preferences; when no supported client language is found,
fall back to Italian. A user's explicit language choice is client-owned, applies
immediately, and survives reloads.

This handoff assumes:

- the tagged application is `frontend/shooter-workshop.client`;
- supported UI cultures are `it-IT` and `en-GB`;
- browser detection matches the primary language subtag, so `it`, `it-CH`, `en`,
  `en-US`, and similar variants select the matching supported catalog;
- preference precedence is valid persisted choice, then the first supported entry in
  the browser language list, then `it-IT`;
- the preference is browser-local and does not roam between devices;
- routes remain culture-neutral and do not gain locale prefixes;
- API payload values, user-entered content, identifiers, and stored dates are not
  translated or rewritten.

`en-GB` is selected to keep English formatting aligned with the application's European
operating context. If US English formatting is required, change this decision before
implementation because it affects date and number acceptance tests.

The implementation boundary is the Workshop client and its directly related
documentation. Server-side culture negotiation, account preference persistence,
localized API exception text, other MorWalPiz applications, and shared design-system
localization are outside this delivery.

## Current Evidence

- [`main.tsx`](../../frontend/shooter-workshop.client/src/main.tsx) is the browser-only
  composition root above both anonymous and protected routes. It currently mounts
  `AuthProvider` and the router without a culture provider.
- [`config.tsx`](../../frontend/shooter-workshop.client/src/routes/config.tsx) places
  `/account/login` outside the protected `AppShell`; localization therefore must wrap
  the router rather than live only in the authenticated shell.
- [`AppShell.tsx`](../../frontend/shooter-workshop.client/src/layouts/AppShell.tsx)
  owns persistent authenticated navigation, account actions, and hardcoded English
  labels.
- [`pages.tsx`](../../frontend/shooter-workshop.client/src/routes/pages.tsx) contains
  most page titles, form labels, actions, table headings, empty states, statuses, and
  native date defaults as English literals.
- [`States.tsx`](../../frontend/shooter-workshop.client/src/components/States.tsx) and
  [`WorkshopForm.tsx`](../../frontend/shooter-workshop.client/src/components/WorkshopForm.tsx)
  own reusable loading, error, retry, conflict, pending, and fallback copy.
- [`pages.tsx`](../../frontend/shooter-workshop.client/src/routes/pages.tsx) formats
  dates and some counts with parameterless `toLocaleDateString`,
  `toLocaleString`, while other quantities remain unformatted. Output therefore varies
  with browser defaults instead of the selected application culture.
- [`workshop.ts`](../../frontend/fe-packages/models/src/workshop.ts) defines stable
  wire values for tracking modes, statuses, maintenance event types, reconciliation
  states, and related domain values. Several are currently displayed verbatim.
- [`publicAccountService.ts`](../../frontend/fe-packages/services/src/publicAccountService.ts)
  contains Italian account error messages. Combined with the Workshop client's English
  copy and raw server messages, the present experience can mix languages.
- [`index.html`](../../frontend/shooter-workshop.client/index.html) fixes the document
  language to English and supplies a static English title.
- [`package.json`](../../frontend/shooter-workshop.client/package.json) has no
  localization dependency. The client also has no language preference in React state,
  browser storage, account models, or account contracts.
- [`config.test.tsx`](../../frontend/shooter-workshop.client/src/routes/config.test.tsx)
  is the only current Workshop client test. [`vitest.config.ts`](../../frontend/shooter-workshop.client/vitest.config.ts)
  uses a Node environment rather than jsdom.
- [`frontend.md`](../architecture/frontend.md) defines Shooter Workshop as a
  client-rendered private SPA and an Impeccable **Operate** surface. This change is a
  refinement of the incumbent visual system, not a redesign.
- [`shooter-workshop.md`](../architecture/shooter-workshop.md) owns the functional
  application baseline but does not currently define culture or localization behavior.
- [`deploy-shooter-workshop-client.yml`](../../.github/workflows/deploy-shooter-workshop-client.yml)
  and central CI already run the Workshop test and build gates; locale catalogs need no
  deployment secret or runtime environment value.

Current request flow remains:

1. The browser mounts `main.tsx`.
2. `AuthProvider` restores the public-account session.
3. Route components call the shared Workshop service.
4. The account transport sends cookie/CSRF-authenticated requests to ServerAPI.
5. ServerAPI returns culture-neutral DTO values plus currently free-form problem text.

Localization belongs before step 2 for UI availability, but it does not change steps
3-5 in this scope.

## Proposed Design

### Culture ownership and first-load algorithm

Add an app-local localization module and provider at the root of `main.tsx`. Keep
culture ownership separate from authentication so login, session restoration,
protected routes, and error states use the same selected culture.

Resolve the initial culture synchronously before the first React render:

1. Read the Workshop-specific persisted culture key.
2. Accept it only when it exactly matches a supported culture.
3. Otherwise inspect `navigator.languages` in order, followed by
   `navigator.language` when necessary.
4. Match supported cultures by case-insensitive primary subtag.
5. Select `it-IT` for Italian, `en-GB` for English, and `it-IT` for unsupported,
   missing, malformed, or storage-inaccessible values.

This avoids an English or Italian flash while culture state initializes. Reading
browser language or storage failure must not block application startup. Storage access
failure means the in-memory culture still works for the current page lifetime.

Persist only an explicit user selection, not the automatically detected result. This
preserves true browser-language detection on a later first visit after browser storage
is cleared while keeping user intent authoritative after selection.

Use one namespaced, versioned storage key owned by Shooter Workshop. Do not reuse
authentication storage, store culture in cookies, or place it in URLs.

### Translation resources and API

Keep the implementation app-local because only Shooter Workshop is in scope and no
second consumer currently needs the same catalogs or culture state. Use an established
React localization library with:

- typed or statically verifiable translation keys;
- interpolation and pluralization;
- React context integration;
- synchronous bundled resources for both supported cultures;
- no network-loaded catalogs and no runtime translation service.

Organize catalogs by stable feature namespaces such as shell, account, common states,
dashboard, firearms, components, maintenance, ammunition, recipes, sessions, and
reconciliation. Italian and English catalogs must have identical key sets. Italian is
the source/default catalog for fallback behavior.

Do not use source English sentences as keys. Do not concatenate translated fragments.
Translate complete messages with interpolation and plural rules.

### Domain values and errors

Create presentation mappings from stable Workshop wire values to translation keys.
The API values themselves must remain unchanged in requests, responses, filtering,
tests, and persistence. User-entered names, notes, identifiers, calibers, and recipe
data remain verbatim.

Client-owned loading, empty, retry, validation summary, conflict, authentication, and
generic request messages become localized. Known stable field names and HTTP/problem
categories receive localized labels.

The current API returns some free-form exception and reconciliation text without
stable localization codes. Do not infer business meaning by matching English message
text. Present a localized, actionable summary based on the stable HTTP/problem shape
and preserve any server detail as secondary technical detail where it is necessary for
diagnosis. A later API contract can introduce stable problem codes if full translation
of server-authored detail becomes required.

The shared public-account error helper must not be changed globally for this client.
Workshop should map account failure categories to its own localized copy at the app
boundary so other consumers retain their current behavior.

### Locale-aware formatting

Centralize app-local formatters for:

- integer and decimal quantities;
- dates;
- date-time history values;
- future pluralized counts.

Every formatter receives the selected culture explicitly. Do not rely on parameterless
`toLocale*` calls. Keep API timestamps and submitted instants in their existing ISO/UTC
forms; localization affects presentation only.

Native date input values must remain ISO `yyyy-MM-dd` because that is the control's
value contract. Separately correct the current UTC-derived “today” default to use the
client's local calendar date so users near midnight do not receive the previous or
next day. This is a tightly coupled correctness fix, not a culture-specific storage
change.

No currency, unit conversion, time-zone selector, or alternate calendar system is
introduced.

### Language controls and document metadata

Provide a compact language selector:

- on the anonymous login surface, so culture can be changed before authentication;
- in the authenticated header utility area beside account actions;
- with Italian and English names that remain understandable in either culture;
- keyboard-operable, visibly focused, and announced with a localized accessible label;
- usable below the existing 560px compact breakpoint.

Changing the selection updates the provider, visible copy, formatters,
`document.documentElement.lang`, document title, and persisted preference immediately
without reloading or navigating. Both supported cultures are left-to-right, so this
scope does not introduce RTL layout behavior.

The static HTML language should use the Italian fallback for no-script and pre-bootstrap
semantics; runtime startup then sets the resolved culture.

### Impeccable UI direction

This is an Impeccable **Operate** refinement. Implementation should apply the
`harden` workflow to the existing Workshop surface, preserving the incumbent layout,
Manrope typography, dark graphite surfaces, and selective signal-yellow accent.
Localization must not trigger a visual redesign.

Visual evidence must cover login and the authenticated shell in both cultures at a
desktop viewport and below the 560px compact breakpoint. Verify 200% zoom, keyboard
focus, long English and Italian labels, no clipped actions or headings, and correct
document language. A final focused `audit` is appropriate after implementation.

## Impacted Projects

| Project or surface | Impact |
|---|---|
| `frontend/shooter-workshop.client` | Owns culture resolution, provider, catalogs, selector, formatters, translated UI, and tests. |
| `frontend/fe-packages/models` | Verify only; stable wire values become inputs to app-local label mappings. |
| `frontend/fe-packages/services` | Verify only; account and Workshop transport contracts remain unchanged. |
| `@morwalpiz/layout` | Verify only; shared components continue receiving already translated props. |
| `MorWalPizVideo.ServerAPI` | No implementation change; existing DTOs and problem shapes remain authoritative. |
| `MorWalPiz.Contracts` / `MorWalPizVideo.Models` / `MorWalPizVideo.Domain` | No implementation change; culture is not persisted and domain values remain stable. |
| `MorWalPizVideo.AppHost` | No change; browser language is client state and requires no orchestration setting. |
| CI and Workshop deployment | Existing test/build gates validate the new code; no new secret, variable, image behavior, or deployment order. |
| Other frontend applications | Not applicable; this delivery must not alter their language behavior. |

## Impacted Files

### Modify

- `frontend/shooter-workshop.client/package.json` — add the chosen localization
  runtime and test-environment support only if required by focused component tests.
- `frontend/yarn.lock` — generated dependency resolution after the manifest change.
- `frontend/shooter-workshop.client/index.html` — set Italian fallback document
  metadata.
- `frontend/shooter-workshop.client/src/main.tsx` — resolve culture before render and
  mount the localization provider above authentication and routing.
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx` — translate shell copy
  and expose the authenticated language control.
- `frontend/shooter-workshop.client/src/routes/pages.tsx` — replace UI literals,
  present stable domain values through translation mappings, use explicit formatters,
  and expose the anonymous language control.
- `frontend/shooter-workshop.client/src/components/States.tsx` — localize shared state
  and recovery copy.
- `frontend/shooter-workshop.client/src/components/WorkshopForm.tsx` — localize
  pending, validation, and fallback error presentation.
- `frontend/shooter-workshop.client/src/main.scss` — only the responsive/focus rules
  required for the language selector and translation-safe wrapping.
- `frontend/shooter-workshop.client/vitest.config.ts` — use the narrowest environment
  needed for provider, storage, and document-metadata behavior.
- `frontend/shooter-workshop.client/src/routes/config.test.tsx` — preserve route
  structure assertions under the localized root where relevant.

### Add

- `frontend/shooter-workshop.client/src/i18n/` — culture constants, supported-culture
  type, first-load resolver, provider/hook integration, and matching Italian/English
  catalogs.
- `frontend/shooter-workshop.client/src/i18n/formatters.ts` — explicit locale-aware
  date, date-time, and numeric formatting.
- `frontend/shooter-workshop.client/src/i18n/domainLabels.ts` — exhaustive mapping of
  stable Workshop wire values to translation keys.
- `frontend/shooter-workshop.client/src/components/LanguageSelector.tsx` — reusable
  anonymous/authenticated language control.
- Focused test files beside the localization module and selector, following the
  workspace's existing `src/**/*.test.{ts,tsx}` collection convention.

Exact catalog filenames may follow the chosen library's normal convention, but the
ownership and namespace boundaries above must remain intact.

### Generated/Migration

- `frontend/yarn.lock` is the only generated artifact.
- No MongoDB, SQLite, API, or account-data migration is required.

### Documentation

- `docs/feature-handoffs/shooter-workshop-localization-handoff.md` — this plan and,
  after implementation, the actual files, validation, and residual risks.
- `docs/architecture/shooter-workshop.md` — add the implemented supported-culture,
  first-load precedence, persistence, formatting, and UI acceptance baseline.
- `docs/architecture/frontend.md` — record client-owned Workshop localization without
  implying a repository-wide frontend localization framework.

No ADR is required because the change does not alter service ownership, persistence,
authentication, deployment topology, or an external contract.

### Verify Only

- `frontend/fe-packages/models/src/workshop.ts` — stable domain values covered by
  exhaustive label mappings.
- `frontend/fe-packages/services/src/publicAccountService.ts` — current shared error
  behavior must remain unchanged for other consumers.
- `frontend/fe-packages/services/src/workshopService.ts` — request and response values
  remain culture-neutral.
- `frontend/shooter-workshop.client/src/auth/AuthProvider.tsx` — session state remains
  independent from culture state.
- `.github/workflows/ci.yml` and
  `.github/workflows/deploy-shooter-workshop-client.yml` — existing nonempty test and
  build gates cover the workspace.

## Impact Matrix

| Area | Impact |
|---|---|
| Backend/API | No route, middleware, header, or response contract change. |
| Frontend/UI | Two complete catalogs, language controls, translated shell/routes/states, and locale-aware formatting. |
| Database/Data | No persistence change; explicit preference is browser-local only. |
| Contracts/Models | Stable wire values are mapped for display but never translated on the wire. |
| Authentication/Authorization | Unchanged cookie/CSRF account flow; culture provider must not delay or authorize session restoration. |
| Validation | Client-owned labels and summaries are translated; server details remain explicit secondary technical content when no stable code exists. |
| Cache | No API/output-cache impact. Bundled locale resources are ordinary versioned static assets. |
| Configuration/Secrets | No environment value or secret. Supported cultures are product constants in the client. |
| Logging/Telemetry | No new telemetry contract. Culture values must not include user data. |
| Background Jobs | Not applicable. |
| Deployment/Operations | Existing client test/build/image flow; no culture-specific deployment sequencing. |
| Backward Compatibility | Existing URLs, stored domain data, API payloads, and account sessions remain compatible. Users without a preference receive browser detection, then Italian fallback. |

## Reuse Opportunities

- Reuse the root composition seam in `main.tsx`; do not introduce a second router or
  culture-aware route tree.
- Reuse existing shared `PageTitle` and `EmptyState` components by passing translated
  props; do not move Workshop catalogs into `@morwalpiz/layout`.
- Reuse stable unions from `@morwalpizvideo/models` as exhaustive mapping inputs.
- Reuse current `ErrorState` and `WorkshopForm` presentation boundaries instead of
  translating independently in every page.
- Reuse `Intl.DateTimeFormat` and `Intl.NumberFormat` through app-local formatter
  factories rather than adding separate date/number libraries.
- Reuse the authenticated header utility area and existing compact breakpoint for the
  language control.
- Reuse the workspace's Vitest runner and existing CI/deployment gates.

## Risks

| Rank | Risk, cause, and affected surface | Mitigation | Verification signal |
|---|---|---|---|
| High | Incomplete catalog coverage leaves mixed English/Italian UI, especially errors and raw domain values. | Inventory all literals, use exhaustive domain mappings, compare catalog key sets, and test both cultures. | No client-owned visible literal or unmapped stable enum remains in reviewed surfaces. |
| High | Culture initializes after React render, causing wrong-language flash or inconsistent auth/login copy. | Resolve synchronously before root render and provide culture above `AuthProvider`. | First rendered login and protected loading states use the resolved culture. |
| Medium | Free-form server errors cannot be translated safely. | Translate stable category/status/field presentation; retain server detail only as secondary technical information; never parse English text. | Forced validation, conflict, auth, and 500 scenarios remain actionable in both cultures. |
| Medium | Stale or malformed browser storage prevents startup or selects an unsupported culture. | Validate against the supported set and isolate storage failures with Italian fallback. | Corrupt, unavailable, and unknown preference tests all render successfully in Italian. |
| Medium | Translation expansion breaks compact navigation or forms. | Use adaptive layout, wrapping, and the existing compact breakpoint; avoid fixed text widths. | Login/header/forms pass both cultures at desktop, narrow viewport, and 200% zoom. |
| Medium | Locale formatting accidentally changes API values or native date input contracts. | Keep formatting at presentation boundaries and retain ISO wire/input values. | Request fixtures are unchanged while displayed dates/numbers differ correctly by culture. |
| Low | Changing the shared account error helper affects other applications. | Add Workshop-local error mapping and leave the shared helper contract unchanged. | Shared-service tests and other consumers compile without localization changes. |
| Low | Browser-only preference does not follow the user to another device. | Document this as intentional scope; account persistence requires a separate contract change. | Reload in the same browser retains explicit choice; a clean browser follows detection. |
| Low | Static metadata remains English before startup. | Set Italian fallback metadata and update it at runtime on culture changes. | `html[lang]` and title match the selected culture before and after switching. |

Security risk is low: culture is an allow-listed presentation preference, not an
authorization input. Never interpolate catalog HTML or render server text as HTML.

## Technical Debt

- Mixed app-local English, shared Italian account errors, and raw server messages
  directly block a coherent bilingual experience. Address the app-local and account
  boundary now; isolate server-authored detail without expanding the API contract.
- Parameterless locale formatting directly conflicts with deterministic selected
  culture. Replace it in scope.
- UTC-derived “today” values can be wrong around local midnight. Fix with the
  formatter/date utility because the touched forms otherwise remain incorrect.
- The client has minimal Node-environment tests. Add only the focused DOM capability
  needed for localization behavior; do not redesign the entire test stack.
- Account culture roaming and stable API problem codes are useful future work but do
  not block this browser-owned first delivery.
- Existing deployment-comment and AppHost/Vite port inconsistencies are unrelated and
  must not be folded into localization work.

## Implementation Roadmap

1. **Establish culture policy and dependency**
   - Add the selected localization runtime and supported-culture constants.
   - Encode `it-IT` default, `en-GB` secondary, first-load precedence, storage key,
     and strict allow-list behavior.
   - Validation gate: resolver tests cover persisted, Italian browser, English browser,
     mixed preference order, unsupported, malformed, and unavailable-storage cases.

2. **Create complete catalogs and domain mappings**
   - Inventory all client-owned copy in shell, routes, common states, forms, and
     document metadata.
   - Add matching Italian and English namespaces.
   - Map every displayed stable Workshop status/type value exhaustively.
   - Validation gate: catalog parity and domain-union exhaustiveness fail when a key or
     value is missing.

3. **Mount localization before authentication and routing**
   - Resolve initial culture before the first render.
   - Provide translation and culture state above `AuthProvider`.
   - Synchronize document language and title.
   - Validation gate: login, restoration loading, and protected shell render in the
     first resolved culture without a wrong-language intermediate state.

4. **Translate UI and normalize error presentation**
   - Replace literals in `AppShell`, pages, shared states, and forms.
   - Keep user content and wire values unchanged.
   - Use localized summaries for stable error categories and explicit secondary
     technical server detail where required.
   - Validation gate: login, dashboard, each domain route, loading, empty, validation,
     conflict, unauthorized, network, and generic failure states are coherent in both
     cultures.

5. **Centralize presentation formatting**
   - Replace parameterless locale calls and raw display quantities with selected-culture
     formatters.
   - Preserve ISO API timestamps and native date values.
   - Generate “today” from the client's local calendar date.
   - Validation gate: expected Italian and English date/number examples pass without
     changing request payload snapshots.

6. **Add language controls**
   - Place the selector on login and in the authenticated header.
   - Persist explicit changes and update the application immediately.
   - Preserve keyboard, focus, accessible naming, and narrow-layout behavior.
   - Validation gate: switching in either location updates visible copy, formatting,
     metadata, and reload behavior without navigation or authentication changes.

7. **Harden and document**
   - Apply Impeccable `harden`, then focused `audit`, to the bilingual Operate surface.
   - Update the owning architecture documents and this handoff with actual decisions,
     files, validation, and residual limitations.
   - Validation gate: focused tests, production build, desktop/narrow visual evidence,
     keyboard checks, and 200% zoom checks pass in both cultures.

The implementer must stop after Shooter Workshop client localization and documentation.
Account preference persistence, ServerAPI localization middleware/problem codes, URL
locales, and localization of other applications require separate approval.

An independent post-validation review is warranted because the change touches every
visible route and can silently miss copy, enum mappings, accessibility labels, or
formatting while still compiling. The user may select `MorWalPiz Code Reviewer` after
implementation.

## Testing Strategy

### Unit

Owning workspace: `frontend/shooter-workshop.client`.

- Culture resolver precedence and primary-subtag matching.
- Strict handling of invalid persisted values and storage exceptions.
- Catalog key parity.
- Exhaustive stable domain-value mappings.
- Italian and English date, date-time, integer, decimal, and plural examples.
- Local-calendar “today” behavior around UTC day boundaries.

### Component and integration

- Root initialization renders the detected culture on the first render.
- `html.lang` and document title synchronize on startup and switch.
- Login and authenticated selectors share state, persist explicit choices, and are
  keyboard accessible.
- Reload honors a valid explicit choice over changed browser language.
- Clearing preference restores browser detection.
- Shell navigation, representative forms, tables, empty/loading/error/conflict states,
  and stable domain labels render in both cultures.
- User-entered content and submitted API values remain unchanged.
- Server detail is never interpreted as a translation key or rendered as HTML.

### Contract and backend

- Existing shared-service request tests remain unchanged and passing.
- No `Accept-Language`, route, DTO, cookie, CSRF, CORS, or account contract expectation
  is added.
- Backend behavior tests are not required because no backend behavior changes.

### Visual and accessibility

- Capture login and authenticated shell in both languages at desktop and below 560px.
- Check long labels, form errors, table headings, navigation, and selectors at 200%
  zoom.
- Verify keyboard order, visible focus, accessible selector name, and announced error
  states.
- Verify Italian/English copy does not clip, overlap, or force horizontal page scroll.

### End to end

Manual browser acceptance should prove:

1. clean Italian browser selects Italian;
2. clean English browser selects English;
3. unsupported browser selects Italian;
4. explicit switch survives reload and overrides browser preference;
5. logout/login does not reset culture;
6. clearing the Workshop preference restores first-load detection;
7. domain operations continue sending the same API values.

## Migration And Rollback

No MongoDB, SQLite, API contract, authentication, cache, or server configuration
migration is required.

The only new durable value is an allow-listed browser storage preference. Unknown or
future values fall back safely and can be ignored by older builds. Deploy the client
through its existing validation and image flow; no backend-first ordering or feature
flag is necessary because locale resources are bundled and the API remains unchanged.

Rollback consists of deploying the prior client image. The stored culture key is
harmless to the prior build. A later localized build can reuse it if the key contract
is unchanged. If a catalog defect is discovered, roll back the client rather than
changing API data or account records.

Monitoring is limited to normal client build/test and browser acceptance evidence.
No new operational metric is required, though future product analytics may record only
the allow-listed culture value if separately approved.

## Open Questions

- Confirm whether English formatting should be `en-GB` as recommended or `en-US`. Response: use en-US
- Confirm the final Italian and English product copy with a fluent reviewer before
  release; automated tests can prove key coverage, not translation quality.
- Confirm whether server-authored validation and reconciliation detail must become
  fully translated in a later phase. If yes, that phase needs stable API problem codes,
  shared contract changes, and separate backend planning. Response: yes has to be planned for later phase, store it as technical debt
- Confirm whether culture should eventually roam with the public account. If yes, that
  requires account contract, persistence, privacy, and compatibility design outside
  this handoff. Response: NO

## Implementation Update — 2026-11-14

The first client-owned localization slice is implemented with the handoff defaults:

- `it-IT` is the fallback culture and `en-GB` is the supported secondary culture.
- Initial resolution reads a valid Workshop browser preference, then browser language
  preferences by primary subtag, then falls back to Italian.
- An explicit selection is stored under `shooter-workshop.culture`; storage failures
  do not prevent startup.
- The provider is mounted above authentication and routing, so login and protected
  loading states share the selected culture.
- Language selectors are present on the login screen and authenticated header.
- `document.documentElement.lang` is updated at runtime.
- Number, date, date-time, and local date-input helpers were added. Existing API
  timestamps and request values remain unchanged.
- The UTC-based default date bug was corrected for maintenance and production forms by
  using the browser's local calendar date.
- Shared state/error/form copy and the primary shell/login labels use the new
  localization boundary. Dashboard/activity and selected quantity/date displays now
  use explicit culture formatting.
- No API, account contract, route, authentication, persistence, CORS, or deployment
  changes were made.

Files added:

- `frontend/shooter-workshop.client/src/i18n.tsx`
- `frontend/shooter-workshop.client/src/i18n-formatters.ts`
- `frontend/shooter-workshop.client/src/components/LanguageSelector.tsx`
- `frontend/shooter-workshop.client/src/i18n.test.ts`

Files modified:

- `frontend/shooter-workshop.client/src/main.tsx`
- `frontend/shooter-workshop.client/src/layouts/AppShell.tsx`
- `frontend/shooter-workshop.client/src/routes/pages.tsx`
- `frontend/shooter-workshop.client/src/components/States.tsx`
- `frontend/shooter-workshop.client/src/components/WorkshopForm.tsx`
- `frontend/shooter-workshop.client/src/main.scss`
- `frontend/shooter-workshop.client/index.html`

Validation completed:

- `cd frontend && yarn workspace shooter-workshop.client test --run` — passed,
  2 test files and 5 tests.
- `cd frontend && yarn workspace shooter-workshop.client build` — passed.

Known residual scope:

- The large domain page module still contains additional English literals and raw
  domain labels in less frequently used forms and tables. Those should be migrated
  through the same provider/mapping boundary before bilingual release certification.
- Browser visual checks at desktop, compact width, keyboard navigation, and 200% zoom
  were not run in this environment.
- Server-authored free-form validation/reconciliation details remain untranslated by
  design; stable API problem codes would be needed for complete server-message
  localization. Response: NO
