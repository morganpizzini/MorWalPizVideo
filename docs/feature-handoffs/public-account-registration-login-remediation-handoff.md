# Registrazione, destinazione login ed errori account pubblici

Stato: implementata nel client pubblico e nei servizi condivisi; verifica live
credentialed non eseguita per assenza di credenziali sintetiche disponibili.

## Outcome

Dopo registrazione riuscita navigare direttamente a `/account/login`, mostrando
un banner italiano "Registrazione completata. Ora puoi accedere.", senza pagina
intermedia e senza ulteriori click. Login, errori e ripristino sessione devono
funzionare sul browser reale. Nessuna password viene trasportata alla destinazione.

Assunzione di compatibilita: la richiesta cambia la navigazione, non vieta
l'attuale emissione di una sessione alla registrazione. Conservarla; non fare
logout artificiale per rendere credibile il banner. Se si vuole una registrazione
senza sessione, occorre approvare separatamente la modifica condivisa con FreezerAnalysis.

## Current Evidence

- [Register e Login](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx):
  entrambi chiamano il servizio, `refresh` e navigano al profilo; catch locali
  esistono gia. Il difetto osservato non prova l'assenza di qualsiasi gestione errori.
- [Provider](../../frontend/morwalpizvideo.client/src/routes/layout/account.tsx):
  `refresh` cattura l'errore e risolve comunque. Login puo quindi navigare al
  profilo dopo un ripristino fallito o una sessione anonima.
- [Servizio account](../../frontend/fe-packages/services/src/publicAccountService.ts):
  register/login acquisiscono CSRF e richiedono un envelope autenticato;
  `getPublicAccountSession` restituisce direttamente il risultato del trasporto
  senza validazione strutturale. Un oggetto errore non deve diventare una sessione.
- [Trasporto](../../frontend/fe-packages/services/src/apiTransport.ts):
  400 ha fieldErrors; 401/429 JSON possono essere restituiti grezzi; il servizio
  puo perdere il motivo reale e mostrare "The account request was not accepted".
- [API](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs):
  CSRF, registrazione, login e session lookup anonimi espliciti; mutazioni
  protette da CSRF, profile protetto dallo schema PublicAccount.
- [Domain](../../MorWalPizVideo.Domain/Accounts/PublicAccountService.cs):
  registra l'account prima di creare la sessione; login verifica hash/salt,
  identita normalizzata e account attivo. Account e sessioni non sono channel-owned.
- [Cookie](../../MorWalPizVideo.MvcHelpers/Authentication/PublicAccountAuthentication.cs):
  HttpOnly, Secure/SameSite=None in produzione, host-only. La
  [composition root](../../MorWalPizVideo.ServerAPI/Program.cs) ha CORS credentialed
  per gli esatti domini pubblici.
- La [guida frontend](../architecture/frontend.md) riporta una precedente CSRF
  pubblicata con 401; e un indizio storico, non una prova corrente.
- Il [router](../../frontend/morwalpizvideo.client/src/routes/config.tsx) non ha
  pagina di successo; un artefatto pubblicato diverso resta possibile.

Flusso: form -> account client include/CSRF -> ServerAPI -> Domain ->
repository account/sessione -> cookie -> session lookup -> UI.

## Proposed Design

1. Registrazione riuscita: destinazione login con sostituzione della voce history
   e indicatore di successo nella navigation state, consumato una volta dalla
   pagina login. Banner `role=status`; nessuna query con dati personali.
   Rientro normale alla login non deve simulare una registrazione riuscita.
2. Conservare envelope e session cookie esistenti. Login resta utilizzabile anche
   se la registrazione ha gia emesso il cookie: nessun redirect automatico al
   profilo che impedisca la destinazione richiesta.
3. Distinguere successo della registrazione da esito del ripristino sessione:
   se l'account e stato creato, non invitare a ripetere la registrazione per un
   fallimento successivo. Mostrare eventuale warning di sessione separato.
4. Login naviga al profilo solo dopo verifica server-authoritative di sessione
   autenticata e account valido. Rendere l'esito di `refresh` esplicito e aggiornare
   tutti i suoi chiamanti; nessun throw non gestito dall'effetto iniziale.
5. Validare l'envelope sessione nel servizio. Normalizzare gli errori solo nel
   ramo account del trasporto, mantenendo gli altri modi e le firme pubbliche.
   Preservare status, fieldErrors e messaggi sicuri anche con body vuoto/non JSON.
6. UI in italiano: credenziali errate 401 senza account enumeration; CSRF 403
   con indicazione di riprovare; conflitto 409 generico; 429 con attesa; rete/5xx
   con retry. Stato pending e protezione da doppio invio. Nessun redirect admin.
7. Prima di attribuire il guasto al codice verificare, con evidenza redatta,
   base URL/artefatto, preflight, CSRF cookie/header, session cookie e successivo
   GET session. Cookie third-party bloccati sono una possibile causa, non accertata.
   Non usare wildcard CORS, bearer localStorage, cookie JS o disabilitazione CSRF.

Impeccable da applicare in implementazione: refinement, modo Operate, `harden`
e `clarify`; [DESIGN](../../frontend/morwalpizvideo.client/DESIGN.md) e stile account
incumbent. Nessun PRODUCT locale trovato. Screenshot login/register/errori a
360 e 1440 px; focus, banner annunciato, tastiera e zoom 200%.

## Impacted Projects

- Public client e servizi frontend: comportamento e validazione account.
- ServerAPI/Domain/MvcHelpers: verify-only finche una riproduzione non identifica
  un difetto backend; nessun cambio speculativo a cookie/password/repository.
- Test BackOffice: copertura ServerAPI; FreezerAnalysis API/SPA verify-only
  per il protocollo condiviso e il ramo account del trasporto.
- Tutti i workspace del [manifest](../../frontend/package.json) verify-only
  se si tocca il core shared; shop resta immutato. Nessun impatto AppHost/WPF.

## Impacted Files

- **Modify:** [Components.tsx](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx),
  [account.tsx](../../frontend/morwalpizvideo.client/src/routes/layout/account.tsx),
  [publicAccountService.ts](../../frontend/fe-packages/services/src/publicAccountService.ts),
  [apiTransport.ts](../../frontend/fe-packages/services/src/apiTransport.ts);
  [title-header](../../frontend/morwalpizvideo.client/src/layouts/title-header.tsx)
  e [Interactions](../../frontend/morwalpizvideo.client/src/routes/blog/Interactions.tsx)
  solo se il cambiamento del provider richiede adeguamento dei chiamanti.
- **Add:** test route/provider nella directory account/layout; test del servizio
  account nel package services, secondo i pattern Vitest esistenti.
- **Generated/Migration:** nessuno.
- **Documentation:** questa scheda, [handoff account precedente](public-accounts-anonymous-push-handoff.md)
  e [guida frontend](../architecture/frontend.md) con prove reali dopo delivery.
- **Verify Only:** [API account](../../MorWalPizVideo.ServerAPI/Controllers/PublicAccountController.cs),
  [PublicAccountEndpointTests](../../MorWalPizVideo.BackOffice.Tests/Features/PublicAccountEndpointTests.cs),
  [trasporto readiness](../../frontend/fe-packages/services/src/apiTransport.readiness.test.ts),
  [FreezerAccountController](../../FreezerAnalysis.Api/Controllers/FreezerAccountController.cs),
  [workflow client](../../.github/workflows/deploy-morwalpizvideo-client.yml).

## Impact Matrix

| Area | Impatto |
| --- | --- |
| Backend/API | Protocollo invariato; diagnosi prima di eventuale patch aggiuntiva |
| Frontend/UI | Redirect diretto, banner, pending, errori e sessione verificata |
| Database/Data | Nessuna modifica account/sessioni; niente registrazioni automatiche ripetute |
| Contracts/Models | Envelope esistente, guardie runtime; nessun nuovo account type |
| Authentication/Authorization | Preservare cookie/CSRF/CORS e isolamento public/admin |
| Validation | Form, errori HTTP e shape della sessione |
| Cache | Nessuna cache di richieste o risposte autenticate |
| Configuration/Secrets | Verificare base URL e cookie senza copiarne i valori |
| Logging/Telemetry | Status/correlazione; niente password, email, cookie o token |
| Background Jobs | Non applicabile |
| Deployment/Operations | Gate browser cross-site sul vero dominio, non solo localhost |
| Backward Compatibility | Freezer e trasporti non account invariati; sessione register preservata |

## Reuse Opportunities

PublicAccountProvider, getApiFieldErrors, ApiResponseError, trasporto account,
navigation state React Router, API/domain e fixture esistenti.

## Risks

- **High:** cookie cross-site/CSRF pubblicato fallisce; mitigazione diagnosi richiesta
  per richiesta e prova desktop/mobile; segnale: login -> session -> profile autenticati.
- **Medium:** una risposta errore viene accettata come sessione; guardie runtime;
  segnale: 401/429/HTML non aggiornano stato autenticato.
- **Medium:** registrazione persiste ma creazione sessione fallisce; non riprovare
  automaticamente POST accounts; verificare login del nuovo account e messaggio corretto.
- **Low:** banner riappare su back/reload; consumare navigation state e testare history.

## Technical Debt

`refresh` maschera il proprio fallimento; errori account non uniformi; CSRF
acquisito sia manualmente sia dal core in percorsi diversi. Correggere i primi
due; non riscrivere tutti i trasporti o il meccanismo CSRF.

## Implementation Roadmap

1. Raccogliere riproduzione HTTP/browser e testare le shape auth; gate: causa
   della login pubblicata identificata o blocco operativo esplicitamente registrato.
2. Servizi: validazione sessione e errori account; gate sui modi non account.
3. Provider: esito refresh esplicito, effetto iniziale gestito; gate sulle chiamate esistenti.
4. Form: redirect/banner, pending, errori; gate sul ciclo register/login completo.
5. Se il guasto e operativo, correggere solo la policy/artefatto identificati con
   approvazione dell'owner; non dichiarare completato il modulo senza browser reale.
6. Aggiornare documentazione e prove; revisione indipendente raccomandata sul confine auth.

## Testing Strategy

- Services Vitest: 400 field errors, 401 JSON/vuoto, 403, 409, 429, 5xx,
  rete, sessione malformata; assert status e nessuna recovery admin.
- Public client: redirect diretto e banner una volta, nessuna pagina intermedia,
  submit doppio, refresh fallito/anonimo, successo login solo con sessione valida,
  profile/logout e interazioni articolo preservati.
- Runner pubblico e Node: [config](../../frontend/morwalpizvideo.client/vitest.config.ts).
  Non imporre jsdom globale: usare prove statiche/pure e, per eventi DOM, runner
  gia disponibile solo dopo verificarne il supporto.
- Backend: estendere PublicAccountEndpointTests per login username/email, password
  errata, CSRF mancante, cookie/session restore e identita condivisa.
- E2E browser: account sintetico, dominio reale, Chrome e Safari mobile,
  cookie bloccati e rete offline. Nessuna credenziale in screenshot o report.

## Migration And Rollback

Nessuna migrazione: schema, API, auth e dati invariati. Rilasciare bundle client/
services insieme; ripristinare bundle precedente se il regression gate fallisce.
Una correzione operativa deve conservare allowlist e controlli CSRF.
Limite: rollback non annulla account gia registrati e non giustifica eliminarli.

## Open Questions

- Quali URL/status/errore browser si osservano nel caso fallito?
- Esiste una policy di hosting che intercetta CSRF/sessioni prima di ASP.NET?
- Registrazione senza alcuna sessione non e richiesta qui; se desiderata, serve
  una decisione distinta prima di cambiare il protocollo condiviso.

## Delivery Evidence (2026-11-07)

### Implemented

- `publicAccountService.ts` ora valida anche la shape della sessione anonima e
  rifiuta envelope di errore o risposte malformate. Gli errori account vengono
  tradotti in messaggi sicuri per status (401/403/409/429/5xx), senza esporre
  dettagli sensibili.
- `apiTransport.ts` conserva per il solo modo `account` status, field errors e
  body JSON anche per 401/403/429 e body vuoti; gli altri modi di trasporto non
  sono stati modificati intenzionalmente. Cookie HttpOnly, `credentials:
  include`, CSRF e isolamento da Authorization/channel headers restano invariati.
- `layout/account.tsx` rende `refresh` esplicito (`Promise<boolean>`), azzera la
  sessione su errore e non maschera più un ripristino fallito come successo.
- `account/Components.tsx` naviga la registrazione direttamente a
  `/account/login` con banner `role="status"` consumato una sola volta; conserva
  la sessione emessa dalla registrazione, mostra un warning separato se il
  ripristino fallisce, blocca il doppio invio e porta al profilo solo dopo una
  sessione autenticata verificata.
- Aggiunti test di trasporto/servizio in
  `frontend/fe-packages/services/src/publicAccountService.test.ts` e un test
  statico del banner in `account/Components.test.tsx`.

### Validation

- `cd frontend/fe-packages/services && npm test -- --run src/publicAccountService.test.ts`
  — PASS, 3 test.
- `cd frontend/fe-packages/services && npm run build` — PASS.
- `cd frontend/morwalpizvideo.client && npx vitest run
  src/routes/account/Components.test.tsx` — PASS, 1 test.
- Impeccable detector sui file UI account — PASS, nessun finding.
- Il typecheck client mirato (`npx tsc -p tsconfig.app.json --noEmit`) resta
  bloccato da un errore preesistente/non account: `PublicChannelSocialLink`
  non esportato da `@morwalpizvideo/models` in `src/routes/layout/socials.tsx`.
- Il primo tentativo del test UI con `jsdom` non è stato usato: il workspace
  non contiene `frontend/node_modules/jsdom`; il test consegnato è statico.

## Follow-up Delivery Evidence (2026-11-08)

### Implemented

- Normalized API paths before identifying anonymous login/registration
  requests. The account endpoint constants omit a leading slash, so the prior
  check missed them and fetched a second CSRF token; transport could overwrite
  the freshly supplied header with its cached token. Session logout remains
  subject to automatic CSRF because only the exact anonymous session endpoint
  is exempted.
- Marked CSRF responses `Cache-Control: no-store` and made frontend CSRF GETs
  bypass the browser cache.
- Applied the same no-cache policy to session restoration so an earlier
  anonymous result cannot override the authenticated cookie after login.
- Added safe `returnUrl` handling from the header account action through the
  registration/login route, returning to the requested local page after a
  verified login. External and authentication-loop destinations are rejected.
- Displayed the authenticated account's display name in the header account
  action, retaining username as the fallback.
- Updated the ServerAPI controller, shared account transport/service, public
  account routes, header, and their focused tests.

### Validation

- `cd frontend/fe-packages/services && npm test -- --run
  src/publicAccountService.test.ts` — PASS, 6 tests.
- `cd frontend/fe-packages/services && npm run build` — PASS.
- `cd frontend/morwalpizvideo.client && npm test -- --run
  src/routes/account/Components.test.tsx src/routes/account/returnUrl.test.ts
  src/layouts/title-header.test.tsx` — PASS, 8 tests.
- `cd frontend/morwalpizvideo.client && npm run build` — PASS.
- `dotnet build MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj
  --configuration Release --no-restore -m:1 -p:BuildInParallel=false` — PASS.
- Focused `PublicAccountEndpointTests` run with `--no-build` — PASS, 4 tests.
- The .NET build reports existing SixLabors.ImageSharp vulnerability advisories.
  The client build reports existing `env-config.js` bundling and stale
  Browserslist-data warnings.

### Residual blocker and operational boundary

Il controllo live anonimo comunicato dall'owner ha restituito CSRF `200` con
cookie `Secure; SameSite=None`; la precedente nota di `401` è quindi stale per
questa verifica. Non è stata eseguita login/sessione credentialed live e non è
stata usata alcuna credenziale. L'origine del precedente fallimento login non
è dimostrata essere React: resta necessaria una prova sintetica sul dominio
reale (preflight, cookie CSRF/sessione e successivo GET session) prima di
attribuire il problema a hosting, browser policy o artefatto distribuito.
