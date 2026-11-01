# Installazione della BackOffice SPA

## Outcome

Rendere installabile la BackOffice SPA dal browser e mostrare agli amministratori un invito non invasivo all'installazione quando il browser lo consente. Assunzione: la richiesta riguarda l'installazione della SPA esistente, non una modalita offline o l'invio di notifiche push aggiuntive. Questa e una verifica del sorgente e della pipeline, non una certificazione dell'istanza Azure attualmente pubblicata.

## Current Evidence

- `frontend/back-office-spa/package.json` e `vite.config.ts`: Vite/React 19, senza `vite-plugin-pwa` o generazione di manifest. `index.html` non collega alcun manifest e mostra ancora l'icona Vite; `public/` contiene soltanto `vite.svg` e `push-sw.js`. Non ci sono nel sorgente i metadati e le icone necessari per presentare l'app come installabile. Il solo service worker non basta a dimostrare l'installabilita.
- `src/main.tsx` registra `push-sw.js` con scope `/` prima del router; `public/push-sw.js` gestisce eventi push e click sulle notifiche, non la cache delle pagine. `src/routes/profile/PushSettings.tsx` usa `navigator.serviceWorker.ready` per la sottoscrizione push. Una nuova registrazione PWA allo stesso scope rischierebbe di sostituire il worker delle push.
- `src/router.ts` monta `PrimaryLayout` soltanto dopo `authLoader` e verifica di sessione/permessi; le rotte di login sono separate. `src/layouts/PrimaryLayout.tsx` compone header, contenuto e footer; `src/layouts/Header.tsx` contiene i controlli dell'utente. L'invito deve appartenere alla shell protetta, non alla schermata di login.
- `frontend/back-office-spa/Dockerfile` copia `src` e `index.html` nel builder ma **non** `public`: l'immagine distribuita non include quindi `push-sw.js` ne le eventuali future icone o un manifest statico. `nginx.conf` usa il fallback SPA per i file mancanti e assegna cache immutabile ai `.js`, incluso il futuro worker se servito come file statico. `.github/workflows/deploy-back-office-spa.yml` costruisce e pubblica questa immagine dopo test e build verificata.
- `frontend/morwalpizvideo.client/vite.config.ts` usa gia `vite-plugin-pwa` e un worker push importato in Workbox; e un precedente utile ma non va copiato alla cieca nella SPA amministrativa, dove il worker push e gia registrato. `frontend/back-office-spa/src/components/ToastNotification/index.tsx` offre toast temporanei, inadatti come unico accesso a una scelta installativa che l'utente puo rinviare.
- Non risultano `PRODUCT.md` o `DESIGN.md` per `frontend/back-office-spa` nel sorgente esaminato: la verita visiva e la shell React Bootstrap (`PrimaryLayout`, `Header`, `index.css`). Le guide `docs/architecture/frontend.md`, `deployment.md` e `admin-dashboard.md` descrivono rispettivamente ownership, distribuzione e shell responsiva; non certificano la disponibilita dell'installazione PWA.

## Proposed Design

1. Aggiungere un manifest specifico BackOffice, referenziato dall'HTML, con nome, `short_name`, `start_url` e `scope` coerenti con `/`, `display: standalone`, colori e icone amministrative reali di dimensioni e formati verificabili (almeno 192 e 512 px, includendo il trattamento maskable se offerto). Sostituire il placeholder Vite; non riutilizzare asset pubblici senza verificare identita e licenza. Mantenere l'installazione separata dalle push: nessun requisito di permesso notifiche.
2. Conservare **un solo** service worker di scope `/`, quello delle push. Non introdurre automaticamente Workbox o un secondo worker: per l'obiettivo di installazione bastano i metadati e le verifiche di installabilita richieste dal browser; nessuna cache di API autenticate, cookie, HTML amministrativo o dati personali, nessuna promessa offline. Se un browser target richiede un handler aggiuntivo per installare, verificarlo prima con prova reale e progettare l'estensione nello stesso worker senza intercettare richieste amministrative.
3. Far consegnare a Docker tutti gli asset pubblici, e configurare nginx per distinguere asset PWA mancanti da route SPA: manifest, icone e worker devono tornare con MIME corretto, mai HTML con status 200; il worker non deve ricevere cache immutabile di un anno. Non modificare la configurazione delle API o dell'autenticazione.
4. Ascoltare `beforeinstallprompt` fin dall'avvio della pagina, prima del bootstrap asincrono, trattenendo l'evento soltanto quando disponibile. Nella shell protetta mostrare una callout React Bootstrap con azioni **Installa** e **Non ora**; chiamare il prompt nativo soltanto dopo il click. Usare `appinstalled` e il display mode standalone per nascondere la callout dopo installazione, inclusi riavvio e navigazione. Non affermare che l'installazione e riuscita se l'utente rifiuta il dialogo. Salvare il rinvio per origine/browser in storage locale, senza informazioni utente; mantenere nel menu utente un'azione discreta per riaprire l'invito quando il prompt e disponibile. Se `beforeinstallprompt` non esiste, non mostrare un pulsante che non puo installare: su iOS/Safari offrire solo istruzioni contestuali verificate nell'interfaccia, senza simulare un prompt nativo.
5. Superficie Impeccable: **Operate**, raffinamento della shell amministrativa, non redesign. Per l'implementazione scegliere il passaggio `harden` sull'invito di installazione: stati supportato/non supportato, rinviato, accettato, installato e gia standalone; copy coerente con la UI amministrativa senza confondere installazione e notifiche push. Evidenza visiva minima: schermate browser desktop/mobile con e senza invito, sotto e sopra 992 px, focus e navigazione tastiera, contrasto, lettore di schermo e nessuna sovrapposizione ai controlli del menu.

Alternativa scartata: adottare direttamente il plugin PWA del client pubblico. Aggiungerebbe una nuova registrazione e policy di precache al worker gia impiegato dalle push, con rischio di sostituzione del worker e cache di contenuto sensibile senza beneficio necessario per l'installazione.

## Impacted Projects

- `frontend/back-office-spa`: unico workspace applicativo da modificare, con asset di installazione, invito e test.
- `frontend/fe-packages/models`, `services`, `layout`: **verify only** per regressioni di build; nessun contratto condiviso da cambiare.
- `MorWalPizVideo.BackOffice` e `MorWalPizVideo.BackOffice.Tests`: **non applicabile** ai cambiamenti funzionali; login, cookie, autorizzazioni e API restano invariati.
- Container nginx dell'admin SPA e relativo workflow di deploy Azure: cambiano gli asset contenuti nell'immagine, non la topologia o la configurazione dell'API. Public SPA, shop, Shooting ITA e strumenti standalone non sono interessati.

## Impacted Files

**Modify**

- `frontend/back-office-spa/index.html`: collegamento manifest, metadati e icona non-placeholder.
- `frontend/back-office-spa/Dockerfile`: inclusione di `public/` prima della build Vite.
- `frontend/back-office-spa/nginx.conf`: risposte corrette per asset PWA e policy di aggiornamento del worker, preservando il fallback delle rotte.
- `frontend/back-office-spa/src/main.tsx`: cattura tempestiva dell'evento installativo e mantenimento della registrazione push esistente; rendere osservabile un eventuale errore di registrazione.
- `frontend/back-office-spa/src/layouts/PrimaryLayout.tsx` e/o `Header.tsx`: collocare l'invito e un accesso persistente e discreto nella shell protetta.
- `frontend/back-office-spa/src/index.css`: solo eventuali ritocchi responsivi dell'invito, coerenti con la shell esistente.
- `frontend/back-office-spa/src/layouts/PrimaryLayout.test.tsx`, `Header.test.tsx`: estendere i test per accessibilita e composizione della shell.

**Add**

- `frontend/back-office-spa/public/manifest.webmanifest` e icone del BackOffice in `public/`: metadati e asset statici della stessa origine.
- Modulo o hook locale di installazione e relativo test sotto `frontend/back-office-spa/src/`: ownership di evento, disponibilita, rinvio e transizioni; nominare file in base alle convenzioni osservate durante l'implementazione.
- Componente di invito e suo test sotto `src/components/` o `src/layouts/`, secondo il punto esatto di montaggio scelto: CTA e feedback accessibili.

**Generated/Migration**

- Solo output Vite/container non versionato; nessuna migrazione di database.

**Documentation**

- Questo handoff, poi `docs/architecture/frontend.md` e `docs/architecture/deployment.md` per registrare lo stato realmente implementato e la consegna degli asset. ADR non necessario finche si conserva il worker e non si introduce una nuova politica di caching/offline; aggiungerlo se questa decisione cambia.

**Verify Only**

- `frontend/back-office-spa/public/push-sw.js`, `src/routes/profile/PushSettings.tsx`, `src/router.ts`, `.github/workflows/deploy-back-office-spa.yml`: non cambiare senza motivo; verificare worker unico, push funzionanti, shell protetta e gate di distribuzione.

## Impact Matrix

| Area | Impatto |
|---|---|
| Backend/API | Nessuna modifica; API e sessione restano server-authoritative. |
| Frontend/UI | Manifest, icone, invito protetto e stato installazione per browser/origine. |
| Database/Data | Nessuna persistenza server; solo preferenza non sensibile nel browser. |
| Contracts/Models | Nessun DTO o modello condiviso nuovo. |
| Authentication/Authorization | Non esporre CTA nel login o in una shell non autenticata; l'app installata richiede ancora sessione valida. |
| Validation | Convalidare struttura manifest, dimensioni/file icone, MIME, HTTPS e evento nativo prima di offrire il prompt. |
| Cache | Nessuna cache offline dei dati amministrativi; worker aggiornabile; asset PWA mancanti non devono ricevere HTML fallback. |
| Configuration/Secrets | Nessun segreto o valore di ambiente aggiuntivo. |
| Logging/Telemetry | Rendere visibile il fallimento della registrazione del worker senza registrare token o dati sensibili; non serve nuovo backend analytics. |
| Background Jobs | Non applicabile. |
| Deployment/Operations | Immagine Docker deve includere gli asset e servirli correttamente sull'origine HTTPS amministrativa. |
| Backward Compatibility | Conservare sottoscrizioni push, login/CSRF, deep link React Router e avvio normale nel browser. |

## Reuse Opportunities

- `PrimaryLayout`, `Header`, React Bootstrap e test con `src/test/test-utils.tsx` per inserire la CTA senza nuovi framework UI.
- `push-sw.js` esistente e `PushSettings`: conservare registrazione, scope e sottoscrizioni; il worker della SPA pubblica e solo riferimento per il rischio di ownership, non una dipendenza.
- `frontend/back-office-spa/src/components/ToastNotification`: feedback di esito eventualmente riutilizzabile, non sostituto della CTA persistente.
- Workflow corrente con test e build verificata prima del deploy; aggiungere controlli mirati agli asset prodotti invece di creare un nuovo pipeline owner.

## Risks

| Priorita | Causa e superficie | Mitigazione | Segnale di verifica |
|---|---|---|---|
| High | `public/` non copiato nell'immagine: worker/manifest/icone assenti in Azure. | Copia nel builder e controllo degli URL dell'immagine servita. | Risposte 200 con MIME atteso, non `index.html`; manifest e icone leggibili. |
| High | Nuovo worker o caching di richieste con cookie: push interrotte o contenuti amministrativi persistiti. | Unico worker push e nessuna cache di API/HTML amministrativo. | Sottoscrizione push ancora attiva; storage browser senza risposte sensibili. |
| Medium | Evento `beforeinstallprompt` perso durante bootstrap e auth loader. | Listener prima del bootstrap, stato condiviso solo nella SPA; CTA montata dopo auth. | Su browser supportato il click apre il prompt dopo accesso. |
| Medium | Browser non supportato/utente gia installato o dismissal: invito ingannevole o ripetitivo. | Gating tramite evento, display mode e `appinstalled`; rinvio persistente e riapertura volontaria. | Stati distinti coperti in test e prova reale Android/desktop/iOS. |
| Medium | Cache immutabile del worker e fallback nginx per file mancanti. | Esclusione worker dalla cache lunga e risposte 404 per asset mancanti. | Update del worker dopo nuovo rilascio; asset assenti non risolvono all'HTML. |
| Low | Logout o sessione scaduta mentre l'app e installata. | Nessun bypass della route protetta; `start_url` attraversa il loader esistente. | Avvio senza cookie apre login, poi shell una volta autenticati. |

## Technical Debt

- `index.html` e `public/vite.svg` contengono branding di template: sostituire il placeholder referenziato dall'HTML nell'ambito delle icone d'installazione; evitare il resto della revisione grafica.
- La registrazione del worker in `main.tsx` non segnala attualmente un errore: trattarlo nel cambiamento per rendere diagnosticabili le push e l'installabilita.
- La build container usa `build-uncheck` dopo il gate TypeScript del workflow: mantenere il gate, non espandere il lavoro a correzioni TypeScript preesistenti non collegate.
- Offline amministrativo e policy di cache/restauro sono un'altra decisione, differita esplicitamente.

## Implementation Roadmap

1. Consolidare il contratto dell'installazione nella SPA: manifest, icone e metadati HTML della stessa origine; verificare identita visiva, URL e differenza tra browser installabile e worker push.
2. Rendere distribuibili gli asset via Docker/nginx senza modificare il fallback delle rotte; verificare MIME, 404 reali e aggiornabilita del worker nell'immagine, oltre alla presenza del worker push.
3. Integrare la cattura precoce dell'evento e la macchina a stati locale alla SPA; conservare la registrazione push unica, il consenso al prompt solo su click, e coprire i casi negativi.
4. Integrare CTA accessibile in `PrimaryLayout` e azione di recupero in `Header`, senza alterare la login route; affinare sul mobile con il passaggio Impeccable `harden` e confrontare gli screenshot.
5. Completare test unitari/component e build verificata; controllare installazione/rinuncia/reinstallazione e push in browser reali contro la build distribuita. Aggiornare `frontend.md`, `deployment.md` e questo handoff con evidenza e rischi rimasti; rilasciare soltanto dopo la prova dell'immagine HTTPS.

**Confine di implementazione:** non aggiungere offline, precache di dati amministrativi, nuove API, permessi push obbligatori, migrazioni o redesign globale. La feature e finita quando il browser supportato puo installare su gesto esplicito, la CTA non appare quando impropria e le push esistenti continuano a funzionare. Consigliata una review indipendente dopo la validazione: il rischio principale e la compatibilita del worker e del container distribuito, non la complessita della UI.

## Testing Strategy

- **Unit/component (BackOffice SPA Vitest/Testing Library):** evento installabile prima/dopo il mount; CTA solo nella shell autenticata; click utente invoca il prompt una sola volta; accettazione, rifiuto, `appinstalled`, rinvio persistito e riapertura volontaria; modalita standalone, assenza API e storage indisponibile; listener rimossi correttamente. Verificare ruolo, nome, focus e contrasto delle azioni.
- **Contratto asset/build:** manifest valido con scope/start coerenti e icone di dimensioni dichiarate; esistenza nel `dist` e nell'immagine nginx; `Content-Type` corretto, 404 per asset mancanti e policy di aggiornamento del worker.
- **Browser end-to-end:** browser Chromium desktop e Android su HTTPS per `beforeinstallprompt`, gesto nativo, avvio installato e accesso senza sessione; Safari/iOS per eventuali istruzioni manuali e assenza di falsa CTA. Testare sotto/sopra 992 px con tastiera, touch e screen reader; verificare deep link e sottoscrizione/consegna push dopo il deploy.
- **Backend/security/migrazione:** nessun nuovo test API o schema; verificare che worker e cache non conservino risposte amministrative/cookie e che la sessione installata rispetti login, scadenza e CSRF.

## Migration And Rollback

Nessuna migrazione MongoDB, SQLite, DTO, cookie o API. Distribuire l'immagine con manifest/icone e worker push conservato, poi misurare installabilita e push su origine HTTPS. Prima del rollout conservare la precedente immagine indirizzata per SHA; per rollback ridistribuirla, tenendo presente che i service worker gia registrati nei browser possono restare attivi finche non ricevono un aggiornamento valido. Non cancellare registrazioni push o dati utente durante il rollback. La preferenza locale del rinvio non ha bisogno di backfill; un cambio futuro del significato del prompt richiede una nuova chiave/versione.

## Open Questions

## Implementation Evidence

- Implementato `manifest.webmanifest`, icone SVG 192/512 con purpose `any maskable`, favicon e theme color; il placeholder Vite non e piu referenziato dall'HTML.
- Implementato `src/services/installPrompt.ts`: cattura dell'evento prima del bootstrap, prompt solo su gesto esplicito, stati installabile/rinviato/installato/standalone, storage locale versionato e riapertura volontaria dal menu utente.
- La CTA vive in `PrimaryLayout`, quindi solo dopo `authLoader`; `Header` espone l'azione discreta di riapertura quando il browser ha fornito il prompt. Nessuna CTA viene mostrata senza `beforeinstallprompt`.
- Docker copia `public/`; nginx tratta manifest e worker come file reali con MIME esplicito, 404 e `no-cache`, preservando il fallback delle route SPA. Il worker push resta l'unico worker a scope `/`.
- La registrazione push conserva scope e comportamento; gli errori sono ora visibili in console senza token o dati utente.
- Diagnostica editor: nessun errore nei file TypeScript modificati. I test Vitest mirati sono stati avviati ma il runner locale resta bloccato prima della raccolta; la verifica automatica dei test e quindi da ripetere in CI o in un ambiente Vitest funzionante.
- La build Vite locale non ha prodotto un risultato osservabile dal runner prima della chiusura del processo; verificare nel gate CI la presenza di manifest, icone e worker in `dist` e nell'immagine nginx.

## Open Questions

Nessuna decisione architetturale bloccante. Restano obbligatorie la prova su container pubblicato HTTPS, Chromium desktop/Android e Safari/iOS, inclusi MIME, installazione reale, logout/sessione e continuita delle push. Le icone SVG sono intenzionalmente same-origin e dimensionate; se un browser target rifiuta SVG nel manifest, sostituirle con PNG equivalenti senza cambiare il contratto della shell.