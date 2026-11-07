# Correzioni public client e BackOffice: indice degli handoff

Stato: implementazione e verifiche focalizzate completate; restano verifiche
visuali e operative dopo il deployment.
Evidenza sorgente e controllo HTTP pubblico raccolti il 2026-11-07.

## Outcome

Trasformare gli appunti in interventi indipendenti, verificabili e senza redesign.
`backoffice-spa-admin` viene interpretato come workspace `back-office-spa`.
Le osservazioni dell'utente descrivono il comportamento pubblicato; il checkout
contiene anche implementazioni recenti che non provano lo stato in produzione.

| ID | Handoff | Priorita | Confine di completamento |
| --- | --- | --- | --- |
| A | [Account pubblici](public-account-registration-login-remediation-handoff.md) | P1 | Registrazione -> login con banner; login e ripristino sessione verificati; errori gestiti |
| B | [Layout mobile e articolo](public-cookie-article-layout-remediation-handoff.md) | P2 | Cookie compatti; cover allineata al testo; recap rimosso dalla testata; etichetta Commenti |
| C | [Social del canale](channel-public-social-links-handoff.md) | P2 | Header/footer derivati da socials; Facebook configurabile; nessuna credenziale pubblica |
| D | [Preview degli articoli](blog-social-preview-remediation-handoff.md) | P2 | HTML iniziale sul dominio pubblico con thumbnail, titolo e recap dell'articolo |
| E | [Upload cover](blog-cover-explicit-upload-remediation-handoff.md) | P1 | Crop e alt locali; nessun upload senza conferma esplicita |
| F | [Terminologia](channel-terminology-null-compatibility-handoff.md) | P1 | GET e consumer AI compatibili con configurazioni legacy vuote; dati corrotti segnalati |

## Progress

- **B1 — completato:** banner cookie mobile con copy non espansa dalla base flex,
  pannello limitato al 45% del viewport dinamico (fallback `vh`), scroll interno
  accessibile e target azioni minimo 44px; safe area esterna preservata. Verifica
  browser alle dimensioni e zoom richiesti ancora da eseguire.
- **B2/D — completati dall'agente:** colonna articolo limitata a 75ch, summary
  visibile rimosso ma conservato nei metadata, label "Commenti", metadata statici
  duplicati rimossi, metadata Twitter/social title editoriali, canonical SSR
  corretto e status route/error propagati.
- **B1/B2/D — validazione integrata:** `npm --prefix
  frontend/morwalpizvideo.client run build:all` passato per client e SSR; i 5 test
  mirati B2/D risultavano passati. Nessuna verifica visuale browser effettuata.
- **Integrazione public client:** il bundle segnala il warning preesistente che
  `/env-config.js` non è un module e il dato Browserslist obsoleto; entrambi i
  bundle sono stati prodotti correttamente.
- **E — completato dall'agente:** upload cover manuale, alt precompilato dal
  filename, crop locale e retry esplicito; flusso di revisione/permessi preservato.
- **E — validazione integrata:** riparata l'installazione locale incompleta di
  `jsdom` con `yarn --cwd frontend install --frozen-lockfile --force` (nessuna
  modifica ai manifest/lockfile); `form.test.tsx` e `BlogCoverCropper.test.tsx`
  passati, 25/25 test. L'agente aveva inoltre riportato build/typecheck SPA
  passato e detector Impeccable senza rilievi.
- **C/F — completati dall'agente:** lettura social pubblica dal canale, provider
  aggiunti e normalizzazione null/corruzione terminologia condivisa con i due
  workflow AI.
- **C/F — validazione integrata:** build Release del progetto
  `MorWalPizVideo.BackOffice.Tests` passata; `ChannelTerminologyTests` e
  `ChannelBoundaryTests` passati, 4/4. Restano i warning preesistenti NuGet per
  `SixLabors.ImageSharp` 3.1.12. La build solution completa non è stata eseguita:
  l'agente aveva rilevato errori di targeting Windows e asset mancanti su Linux.
- **A — completato dall'agente:** banner di successo one-shot e navigazione diretta
  alla login; validazione della sessione, errori, pending e doppio invio gestiti.
- **A — verifiche riportate dall'agente:** 3 service test, 1 UI test, build
  TypeScript services e detector Impeccable passati. Login autenticato live non
  testato; una compilazione client iniziale ha visto un export shared stale,
  ma la build integrata successiva e passata dopo la build dei package condivisi.
- **A — residuo operativo:** sessione/login cross-site non verificati sul browser
  autenticato in produzione; non sono state usate credenziali reali o sintetiche.
- **D — residuo operativo:** la route live era SSR, ma il canonical `localhost`
  e metadata duplicati rilevati prima della patch non sono stati riverificati
  dopo il deployment. Nessun deploy e stato richiesto o eseguito.
- **C — residuo operativo:** il nuovo endpoint/social configurato non e stato
  verificato dopo deployment sul dominio reale; cache e URL social reali vanno
  controllati con profili valorizzati.

## Current Evidence

### Verifica pubblica prima dell'implementazione

- `GET https://morwalpiz.com/blog/road-to-national` ha restituito HTTP 200 da
  Express/SSR al controllo del 2026-11-07. Il serving HTML server-rendered e
  quindi attivo su questa route; non serve ipotizzare una migrazione di hosting
  per iniziare il modulo D.
- Lo stesso HTML contiene metadata statici homepage con logo prima dei metadata
  dell'articolo. I crawler possono quindi incontrare la preview generica.
- I metadata specifici includono la cover pubblica, ma canonical e `og:url`
  dell'articolo puntano a `https://localhost/blog/road-to-national`: difetto
  concreto del canonical SSR.
- La richiesta anonima live a `/api/v1/account/csrf` ha restituito HTTP 200.
  Questo smentisce il precedente 401 documentato per quell'endpoint, ma non
  verifica login/sessione cross-site; nessun account/credenziale usato.

Non sono stati verificati browser autenticati, cookie policy dei client, o il
comportamento di login con account sintetico sul deployment live.

- Il [form account](../../frontend/morwalpizvideo.client/src/routes/account/Components.tsx)
  registra e naviga al profilo: non esiste una pagina di successo dedicata nel
  [router](../../frontend/morwalpizvideo.client/src/routes/config.tsx).
- Il [form cover](../../frontend/back-office-spa/src/routes/blog/form.tsx)
  invia automaticamente il file preparato appena l'alt diventa non vuoto.
- [SocialLinks](../../frontend/morwalpizvideo.client/src/components/SocialLinks.tsx)
  contiene URL statici; Facebook e Telegram non sono provider ammessi per
  `Channel.Socials` nel [controller canali](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs).
- La [testata HTML](../../frontend/morwalpizvideo.client/index.html) mantiene
  metadata generici anche quando il componente articolo emette metadata specifici.
- [ToContract](../../MorWalPizVideo.BackOffice/Services/ChannelTerminology.cs)
  dereferenzia la configurazione senza guardie. Lo stack trace non identifica da
  solo quale valore Mongo sia nullo.

## Proposed Design

Sei moduli, non un refactoring generale. B contiene due sottointerventi indipendenti.
A conserva l'attuale sessione emessa dall'API alla registrazione: la richiesta
impone la destinazione login, non una modifica del protocollo di autenticazione.
C distingue profili pubblici da condivisione articolo e da social publishing.
D verifica prima quale infrastruttura restituisce realmente l'HTML pubblico.
E corregge solo il workflow cover identificato; un uploader diverso richiede
evidenza aggiuntiva, non l'estensione automatica del requisito 1920 x 1080.

## Impacted Projects

Public client, BackOffice SPA, ServerAPI, BackOffice API, Domain, Contracts,
Models e test BackOffice sono coinvolti secondo le schede.
Models e servizi frontend hanno verifiche di compatibilita su tutti i workspace
dichiarati nel [manifest](../../frontend/package.json).
FreezerAnalysis e strumenti Windows sono verify-only dove condividono confini;
shop resta congelato. Nessun intervento su AppHost o strumenti standalone previsto.

Consumer diretti dei package shared verificati nei manifest: BackOffice SPA,
public client, ask.client, Shooting ITA, shop e FreezerAnalysis SPA, oltre ai
package layout/services. Gli altri workspace non diventano automaticamente
consumer di questi package: verificarne il manifest prima di ampliare i gate.
Consumer .NET diretti Models/Contracts verificati nei project reference:
Domain, Contracts, MvcHelpers, ServerAPI, BackOffice, FreezerAnalysis API,
VideoImporter e InsightScanner. Per i consumer verify-only basta il gate
proporzionato al contratto toccato; non introdurre modifiche funzionali.

## Impacted Files

- **Modify / Add:** elenchi puntuali nelle sei schede; non modificare codice in questa fase.
- **Generated/Migration:** nessuno obbligatorio; nessuna riscrittura di file generati.
- **Documentation:** questo indice e le sei schede; aggiornare le schede con esiti
  reali dopo l'implementazione.
- **Verify Only:** manifest, workflow, guide, fixture e consumer elencati nelle schede.

## Impact Matrix

| Area | Decisione |
| --- | --- |
| Backend/API | A verifica auth; C aggiunge lettura pubblica minima; F normalizza dati legacy |
| Frontend/UI | A/B/C/E sono refinement, non redesign |
| Database/Data | Nessuna nuova collection; C usa socials; F lettura tollerante senza scritture GET |
| Contracts/Models | C aggiuntivo; A preserva envelope e protocollo |
| Authentication/Authorization | Nessun indebolimento CORS/CSRF/RBAC; nessuna identita staff/pubblica unificata |
| Validation | Errori auth, URL social, media e terminologia testati ai confini proprietari |
| Cache | C riusa channels; D include cache HTML/social; A non deve essere cacheata |
| Configuration/Secrets | Solo configurazione non segreta; dati diagnostici redatti |
| Logging/Telemetry | Correlazione errori senza password, token, contenuti o ciphertext |
| Background Jobs | Nessun nuovo job |
| Deployment/Operations | D ha gate sul vero dominio; A ha gate browser cross-site |
| Backward Compatibility | Preservare contratti auth, media esistenti e consumer non interessati |

## Reuse Opportunities

Trasporti public/account/admin separati, PublicAccountProvider, cropper cover,
test WebApplicationFactory/mock, ChannelSocial e invalidazione cross-API esistenti.
Non creare un nuovo sistema account, media manager o store globale.

## Risks

- **High:** login cross-site e origine HTML non verificati; mitigazione con prove
  browser/HTTP redatte sul dominio effettivo prima della dichiarazione di successo.
- **Medium:** mutazioni nello stesso form cover da precedenti handoff; E sostituisce
  solo il trigger automatico, non crop/export/revisioni.
- **Medium:** dati legacy e cache possono simulare regressioni; verificare forma
  BSON e invalidazione, senza backfill distruttivo o reset globale.
- **Low:** drift della documentazione; distinguere sempre proposta, test e rilascio.

## Technical Debt

Documentazione ospitalita Aruba contro workflow Azure SSR; errori account non
uniformi; nullability persistita non garantita da inizializzatori C#.
Affrontare solo gli aspetti necessari a questi moduli. Nessun cleanup generale.

## Implementation Roadmap

1. Approvare assunzioni/confini e ottenere le evidenze operative A/D/F.
2. Implementare A, E e F come patch indipendenti. Nessuna dipendenza reciproca;
   serializzare le validazioni .NET che condividono output.
3. Implementare B. Coordinare B e D sullo stesso componente articolo.
4. Implementare C: policy Domain -> DTO -> API -> servizi/modelli frontend -> admin/public.
5. Implementare D dopo aver risolto il gate sul serving pubblico; non consegnare
   una soluzione solo client-side.
6. Validare ciascun modulo, poi le superfici condivise interessate; registrare
   risultati, deployment, rollback e rischi nello stesso handoff.
7. Raccomandata revisione indipendente con MorWalPiz Code Reviewer per A/C/D/F
   dopo la validazione: confini auth, esposizione dati, cache e compatibilita.
   Scelta dell'utente, non attivata in questa analisi.

## Testing Strategy

Usare le suite/fixture indicate nelle schede e il
[runbook](../architecture/testing.md). Prima selettori mirati, poi build/typecheck
dei consumer interessati. Nessun risultato passato ereditato vale come prova corrente.
Le prove visive vanno raccolte insieme su desktop/mobile, con al massimo un
secondo passaggio di conferma dopo le correzioni.

## Migration And Rollback

Nessuna migrazione generale. C e additivo; F distingue assenza/null da corruzione.
A mantiene il protocollo esistente; E non modifica media gia caricati.
Rollback per singolo modulo, evitando di ripristinare versioni con errori auth o
perdita di dati. D richiede una decisione operativa se il dominio serve HTML statico.

## Open Questions

1. Quale artefatto/origine serve oggi `morwalpiz.com`, specialmente `/blog/:slug`?
2. Quali URL, status e codici browser compaiono nella login fallita? Solo evidenza redatta.
3. L'upload segnalato e la cover blog? E il caso verificato; se si tratta anche
   di altre immagini occorre indicare le schermate prima di ampliare lo scope.
4. Qual e la forma redatta della terminologia del canale che fallisce?
