# Preview social degli articoli: HTML iniziale e metadata

Stato: implementato lato sorgente SSR il 2026-11-07; deploy non eseguito.

## Outcome

Condividendo un articolo pubblicato, i crawler ricevono nel primo HTML, senza
JavaScript e senza login, cover/thumbnail dell'articolo, titolo e recap.
Canonical e link condiviso concordano su `https://morwalpiz.com/blog/:slug`.
Non basta vedere i metadata dopo il rendering browser.

Assunzione: la preview social usa titolo e summary editoriali; seoTitle e
seoDescription possono restare per title/description SEO ordinari.
Articolo senza cover: fallback dichiarato al logo del sito, non la prova positiva
del requisito thumbnail. Articolo con summary vuoto: description omessa o vuota,
senza testo inventato; testarlo come edge case.

## Current Evidence

- [BlogArticle](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx)
  passa gia cover a SEO; titolo/descrizione preferiscono override SEO.
  Lo share Facebook esiste e usa il canonical morwalpiz.com.
- [SEO](../../frontend/morwalpizvideo.client/src/utils/seo.tsx) produce OG, non
  Twitter card dinamiche; sceglie base da window quando presente.
- [index.html](../../frontend/morwalpizvideo.client/index.html) contiene title,
  canonical, OG e Twitter statici con logo; l'iniezione SSR aggiunge head senza
  rimuovere metadata generici: duplicati e Twitter generico sono difetti verificati.
- [server.mjs](../../frontend/morwalpizvideo.client/server.mjs) installa window
  shim con protocol=https, host=localhost prima dell'import SSR. Il ramo SEO
  "isServer" puo quindi risolvere canonical su localhost.
- [entry-server](../../frontend/morwalpizvideo.client/src/entry-server.tsx)
  raccoglie Helmet title/meta/link ma non script; React 19/Helmet e shim DOM
  richiedono prova sul documento completo, non assunzione sul posizionamento head.
- Server render error -> shell statica con status 200 in server.mjs; anche
  status errori dal router non vengono propagati dal contratto render attuale.
- Evidenza HTTP fornita dal parent: `GET https://morwalpiz.com/blog/road-to-national`
  risponde da Express/SSR (`X-Powered-By: Express`). La risposta conteneva
  metadata statici generici duplicati e canonical `localhost`; quindi non serve
  una migrazione di routing. La documentazione architetturale continua a citare
  Aruba mentre il workflow costruisce e pubblica il container SSR Azure: questa
  è una discrepanza documentale/operativa, non modificata qui.
- [BlogController](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs)
  offre solo snapshot pubblicati, cache 300 secondi/tag blog e 404 per assenti.
- [index.test](../../frontend/morwalpizvideo.client/src/routes/blog/index.test.tsx)
  verifica metadata nel markup del componente, non head HTML servito al crawler.

## Implementazione

**Gate di infrastruttura prima dell'implementazione.** Identificare origine,
artefatto e risposta HTML della deep-link pubblica, redirect inclusi. Se il dominio
serve Node SSR, correggere quel percorso. Se serve solo file statici Aruba,
questa scheda non autorizza una migrazione silenziosa: occorre approvare routing
del dominio al servizio SSR esistente o una strategia prerender aggiornata sulle
pubblicazioni. Il solo client-side SEO non risolve il caso statico.
Preferenza: utilizzare SSR gia presente; prerender comporta una nuova pipeline,
invalidazione publish/unpublish e fallback da progettare dopo la decisione owner.

**Metadata, unica autorita.** Mantenere statici solo metadata invarianti
(viewport/favicon/theme/site_name/font). Head title/canonical/description/OG/
Twitter devono essere forniti una sola volta dal rendering di pagina.
Default globali restano per pagine che non specificano SEO e per shell offline,
senza coesistere con duplicati per articolo SSR.
Per articolo: og:type article, og:title titolo editoriale, og:description summary,
og:image cover assoluta, og:url canonical; Twitter summary_large_image con gli
stessi dati. Aggiungere alt cover dove supportato, senza usare URL firmati privati.
Mantenere i campi SEO ordinari distinti dagli override social.

**SSR affidabile.** Base canonical esplicita dal dominio canonico/configurazione
non segreta, non dall'esistenza di window shim ne da host forwarded arbitrario.
Un solo helper app-local per canonical usato da SEO e share; niente mutazione
globale window.location per richiesta. Verificare la raccolta head sotto Node/
React 19, includendo JSON-LD senza script duplicati e senza XSS.
Loader continua a usare transport public con base URL assoluta disponibile nel
server. L'HTML articolo deve contenere il corpo e i metadata in head.

**Errori.** Propagare status 404 del router per articoli mancanti/non pubblicati,
5xx per fetch/render falliti; non tornare 200 con logo come finto articolo.
Error page senza metadata di un altro articolo; log errore strutturato senza
cookie/token. Preservare redirect e shell offline PWA, senza cambiare globalmente
gli altri percorsi oltre gli adeguamenti necessari al contratto render.

**Cache e rilascio.** Conservare invalidazione API blog. Non introdurre cache SSR
finche non c'e un bisogno misurato; verificare cache/rewrite gia presenti davanti
al dominio e l'accessibilita anonima della cover. Dopo release richiedere nuova
scansione della singola URL nei debugger social consentiti dall'operatore:
vecchia preview nella cache del social non prova un difetto del nuovo HTML.
Non inviare contenuti privati a servizi terzi durante sviluppo/test.

Impeccable: refinement Read, `harden` per edge state; non e un redesign.
[DESIGN](../../frontend/morwalpizvideo.client/DESIGN.md) resta invariato. Evidenza
minima: risposta raw HTML piu screenshot desktop/mobile dell'articolo per
escludere regressioni di [layout](public-cookie-article-layout-remediation-handoff.md).

## Impacted Projects

Public client (SEO, SSR server, entry, template, test) e hosting pubblico.
ServerAPI/BackOffice/Domain/Contracts/media verify-only: i dati necessari sono
gia disponibili. Nessun cambiamento Mongo, Models, shared layout, WPF o AppHost.
Workflow/Docker verify-only nel ramo SSR gia operativo; eventuale cambio topologia
richiede approvazione e aggiornamento ADR, non e presunto.

Sono stati applicati questi punti senza ridisegnare SSR o routing:

- SEO usa `import.meta.env.SSR` e base canonica configurabile, evitando il
  browser shim `localhost`; aggiunge Twitter dinamico e consente title/summary
  social editoriali distinti dagli override SEO.
- Il template non contiene più title/canonical/OG/Twitter dinamici generici:
  `og:site_name` e metadata invarianti restano statici.
- `entry-server` porta lo status del router e, con React 19, estrae dal markup
  i tag Helmet hoisted nel vero head; `server.mjs` restituisce 404/5xx invece di
  una risposta 200 success-shaped per gli errori.
- L'articolo fornisce agli OG/Twitter title, summary e cover pubblicati.

## Impacted Files

- **Modify:** [SEO](../../frontend/morwalpizvideo.client/src/utils/seo.tsx),
  [blog index](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx),
  [index.html](../../frontend/morwalpizvideo.client/index.html),
  [entry-server](../../frontend/morwalpizvideo.client/src/entry-server.tsx),
  [server.mjs](../../frontend/morwalpizvideo.client/server.mjs),
  [index.test](../../frontend/morwalpizvideo.client/src/routes/blog/index.test.tsx).
- **Add:** helper canonical e test SEO/SSR nella struttura app-local esistente;
  test HTTP del server con API mock, senza dipendenza da servizi social.
- **Generated/Migration:** nessuno; dist/dist-ssr rigenerati dalla build, non editati.
- **Documentation:** questa scheda, [frontend](../architecture/frontend.md),
  [deployment](../architecture/deployment.md) con origine effettiva; ADR solo per
  cambio serving/routing approvato.
- **Verify Only:** [BlogController](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs),
  [blogService](../../frontend/fe-packages/services/src/blogService.ts),
  [Dockerfile](../../frontend/morwalpizvideo.client/Dockerfile),
  [workflow](../../.github/workflows/deploy-morwalpizvideo-client.yml),
  [vite.config](../../frontend/morwalpizvideo.client/vite.config.ts),
  [main](../../frontend/morwalpizvideo.client/src/main.tsx).

## Impact Matrix

| Area | Impatto |
| --- | --- |
| Backend/API | Solo verifica snapshot/cache pubblicata |
| Frontend/UI | Metadata/SSR/error page; nessun recap reinserito in testata |
| Database/Data | Nessun cambio; usa cover/title/summary pubblicati |
| Contracts/Models | Contratto interno render deve portare status HTTP |
| Authentication/Authorization | Crawler anonimo; nessun cookie utente inoltrato all'API |
| Validation | Head unico, canonical assoluto sicuro, escaping metadata |
| Cache | API blog e cache HTML/social esistenti da verificare |
| Configuration/Secrets | Origine canonical e API base non segrete, no localhost pubblico |
| Logging/Telemetry | Fetch/render falliti espliciti, non success-shaped fallback |
| Background Jobs | Nessuno nel ramo SSR; prerender non approvato |
| Deployment/Operations | Gate sull'HTML del dominio effettivo, non sul container soltanto |
| Backward Compatibility | SEO delle altre pagine, redirect e PWA da preservare |

## Reuse Opportunities

Loader articolo, public transport, Helmet, SSR Node/Express, metadata cover
pubblicata e workflow build client+SSR gia esistenti.

## Risks

- **High:** dominio statico bypassa SSR; fermare delivery al gate e ottenere
  decisione routing; segnale: raw head specifico sulla URL pubblica senza JS.
- **Medium:** metadata duplicati/shim DOM; parser documento completo e richieste
  concorrenti di due slug, nessun canonical localhost o contaminazione.
- **Medium:** cache sociale vecchia o cover non raggiungibile; verificare HTML
  e immagine anonimi prima della nuova scansione.
- **Medium:** escaping titolo/summary e JSON-LD; test con apici, &, < e Unicode;
  nessuno script eseguibile introdotto.
- **Low:** rimozione template lascia pagine senza SEO; default documentato e
  test homepage/pagina ordinaria.

## Technical Debt

Browser shim nel server, fallback 200 e test SEO limitato al componente sono
in scope per la preview. Non riscrivere tutto SSR, analytics o la PWA.

## Implementation Roadmap

1. Gate serving: origine/artefatto/HTML pubblico e copertura API base.
2. Test prima patch: documento intero con API mock, slug diversi/404/5xx.
3. Canonical condiviso e dati social separati; template senza duplicati.
4. SSR head/status e handling errori; gate su HTML iniziale, non DOM idratato.
5. Build client+SSR e verifica homepage/articolo/PWA; deploy e nuova scansione
   pubblica approvata dall'operatore. Registrare prove nello stesso handoff.
6. Review indipendente raccomandata per serving/status/canonical e compatibilita.

## Testing Strategy

Unit SEO/canonical: URL assolute/relative, override SEO distinti dai dati social.
SSR/HTTP app-local: pubblicato con cover/title/summary, un solo title/canonical/
og:* e twitter:*, tutti in head; nessun localhost/logo come og:image con cover;
404 draft/assente, 5xx fetch, redirect, due richieste concorrenti.
Contratto: input solo snapshot pubblicato, cover anonima valida; test escaping.
E2E: raw HTML dominio e immagine senza cookie/JS, poi browser 360/1440 px,
hydration senza regressioni. Test social live solo su contenuti pubblici e
con autorizzazione operatore; non e un sostituto dei test HTTP.

## Migration And Rollback

Nessuna migrazione Mongo/API/auth. Rilascio SSR e client accoppiato, verificare
base URL runtime e routing. Rollback artefatto non aggiorna automaticamente cache
social/CDN: ripetere verifiche e rescansione. Se cambia l'origine, serve piano
owner di routing reversibile prima della release; nessuna migrazione presunta.

## Validation And Residual Risks

Validation eseguita: test Vitest mirati per blog e interazioni (5 test passati).
Il test dell'articolo verifica summary non visibile, singolo canonical/OG/Twitter,
cover assoluta, title/summary editoriali e assenza di `localhost`; il loader
mantiene la copertura 404. Non sono stati eseguiti build client/SSR concorrenti:
il downstream final procedure deve eseguire un unico build workspace serializzato.

Residual risk: non sono state fatte richieste HTTP aggiuntive, deploy o scansioni
social. L'evidenza parent conferma SSR attivo, ma la discrepanza Aruba/Azure e
la configurazione runtime/API/forwarded headers restano da verificare
operativamente dal release owner. Nessun segreto o contenuto privato è stato
inviato a terzi.

## Open Questions

Nessuna decisione di routing è bloccante per questa patch; l'origine SSR è stata
dimostrata dall'evidenza HTTP parent. Resta la riconciliazione operativa Aruba/
Azure come attività del release owner.
