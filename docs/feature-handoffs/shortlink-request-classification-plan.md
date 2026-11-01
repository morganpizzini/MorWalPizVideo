# Piano: distinguere aperture e anteprime degli short link

## Status

In implementation. Classifier, additive counters, classified event persistence,
and the protected BackOffice analytics read are implemented; frontend summary
surfaces, operational index/TTL rollout, and production activation remain.

## Summary

Separare, per le nuove richieste agli short link, le aperture probabilmente
umane dalle richieste automatiche per anteprime social e dalle altre
automazioni. Conservare i conteggi esistenti senza reinterpretarli.
La classificazione e' una stima tecnica, non una prova di click umano,
di utente unico o di visualizzazione del post.

Assunzione di perimetro: classificazione sul server, redirect immediato
invariato, nessuna pagina intermedia e nessun nuovo tracciamento nel browser.

## Evidenze e comportamento attuale

| Evidenza | Fonte |
| --- | --- |
| Una GET a un codice risolvibile incrementa il totale prima del redirect. Lo User-Agent serve solo a scegliere il redirect mobile. | `MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs`, metodi `Index` e `UpdateShortLinkClickCount` |
| Mongo incrementa `ClicksCount` e aggiunge `ClickTimestamps` nella stessa operazione. | `MorWalPizVideo.Domain/Interfaces/Repository.cs`, `ShortLinkRepository.IncrementClicksAsync` |
| Il mock replica totale e timestamp. | `MorWalPizVideo.Domain/Interfaces/MockRepository.cs`, `ShortLinkMockRepository.IncrementClicksAsync` |
| Il grafico del dettaglio usa i timestamp, non eventi classificati. | `MorWalPizVideo.BackOffice/Controllers/ShortLinksController.cs`, `GetShortLinkClicks` |
| La card della dashboard somma i timestamp degli ultimi sette giorni nel canale selezionato. | `MorWalPizVideo.BackOffice/Services/DashboardService.cs`, `GetSummaryAsync` |
| La pubblicazione social usa `SiteUrl + sl/{code}`; il codice di pubblicazione non incrementa direttamente il contatore. | `MorWalPizVideo.BackOffice/Services/FacebookService.cs`, `TelegramService.cs`, `DiscordService.cs` |
| I dettagli delle visite devono avere una collection separata e una retention approvata. | `docs/architecture/adr/ADR-004-canonical-short-links.md` |

La configurazione distribuita di `/sl/{code}` va verificata: la lettura del
repository non prova che il percorso pubblico raggiunga il servizio ShortLinks.
La visualizzazione di una preview gia' memorizzata dal social non genera
necessariamente una richiesta al servizio e non e' osservabile da esso.

## Impacted Areas

- Backend/API: ShortLinks per classificazione e conteggio; BackOffice per letture analytics.
- Frontend/UI: elenco e dettaglio short link, dashboard BackOffice.
- Database/Data: campi additivi sul documento canonico e collection separata per eventi classificati.
- Contracts/Models: DTO analytics dedicati e modelli TypeScript corrispondenti.
- Auth/Security: redirect anonimo invariato; analytics protetti dagli attuali permessi e scope di canale.
- Cache/Configuration: flag di attivazione, retention e indici applicati manualmente; nessuna cache sul redirect.
- Tests/Validation: classifier, repository, redirect, compatibilita', analytics e interfaccia.

## Expected Change

### 1. Semantica delle categorie

Introdurre un classifier locale, deterministico e senza chiamate esterne.
Applicare regole ordinate e confronti case-insensitive allo User-Agent:

| Categoria proposta | Significato | Esempio di regola |
| --- | --- | --- |
| `SocialPreview` | Client noto per recuperare anteprime social. | Token esplicito `facebookexternalhit`; altri token solo dopo verifica e test. |
| `OtherAutomation` | Crawler, scanner o strumenti automatici riconoscibili, non identificati come anteprima social. | Token specifici di crawler o scanner conosciuti. |
| `HumanCandidate` | User-Agent compatibile con un browser, senza segnali automatici riconosciuti. | Browser desktop/mobile, incluso browser in-app. |
| `Unclassified` | Richiesta nuova con informazioni insufficienti. | User-Agent assente, vuoto o non riconosciuto. |

Le regole per anteprime e automazioni precedono quelle per browser:
un crawler puo' dichiarare anche `Mozilla`. Non classificare come bot un
browser umano solo perche' contiene `Facebook`, `Instagram` o altri nomi di app.
Non usare il referrer social come prova di anteprima: puo' accompagnare un
click reale. Evitare una regex generica che tratti qualsiasi token come prova.

Restituire categoria, codice della regola e versione del classifier.
Non aggiungere una libreria esterna o un servizio di bot detection per questa
prima versione. Le firme iniziali devono essere verificate durante
l'implementazione; gli esempi qui non costituiscono una lista esaustiva.

### 2. Totali compatibili e atomicita'

Preservare il significato di `ClicksCount`: totale grezzo delle richieste
conteggiate, incluse le automazioni. Non convertirlo retroattivamente in
"click umani".

Aggiungere quattro contatori persistenti, uno per categoria, inizializzati
a zero quando assenti. Per una richiesta classificata eseguire una sola
operazione atomica sul documento canonico:

1. Incrementare `ClicksCount`.
2. Incrementare esattamente il contatore della categoria.
3. Aggiungere a `ClickTimestamps` lo stesso istante UTC usato per l'evento.

Il servizio passa al repository categoria e timestamp; il repository non
interpreta header HTTP. Allineare Mongo, mock e gli eventuali chiamanti della
vecchia operazione di incremento, mantenendo il percorso non classificato
compatibile durante rollout e rollback.

Definire nel DTO:

```text
classifiedRequests =
    socialPreviewRequests + otherAutomationRequests
    + humanCandidateRequests + unclassifiedRequests

unsegmentedRequests = clicksCount - classifiedRequests
```

`unsegmentedRequests` comprende lo storico e le richieste di periodi senza
classificazione, non "persone". `unclassifiedRequests` riguarda invece
richieste elaborate dal nuovo classifier senza elementi sufficienti.
Non confondere queste due grandezze.

Non fare snapshot iniziali con read-modify-write ne' backfill dei timestamp.
La differenza consente di mantenere lo storico senza attribuzioni inventate
e senza una migrazione che interrompa il traffico. Se l'invariante diventa
negativa, segnalare esplicitamente l'incoerenza: niente clamp silenzioso.

Proteggere i contatori dalle modifiche amministrative: gli aggiornamenti di
target, codice e ownership devono aggiornare solo i campi gestionali, non
riscrivere contatori da una copia letta prima di un click concorrente.
Verificare anche aggiornamenti sponsor/campagne, creazione idempotente e
`ShortLinkMigrationService`: replacement e merge non devono azzerare i nuovi
campi o perdere timestamp e incrementi concorrenti.
Non ampliare il lavoro a un refactor generale dei repository.

### 3. Dettaglio temporale separato

Usare una nuova collection, nome proposto `shortLinkRequestEvents`, per
gli eventi classificati. Non aggiungere altre liste illimitate a `ShortLink`.

Campi minimi proposti:

- ID evento generato una sola volta per richiesta.
- ID canonico dello short link, stabile anche se cambia il codice.
- Timestamp UTC.
- Categoria, codice regola e versione classifier.
- Scadenza calcolata dalla policy di retention.

Non salvare IP, cookie, identificativi di utenti, User-Agent completo,
referrer completo o query string. I dati necessari al classifier rimangono
in memoria durante la richiesta.

I totali sul documento canonico sono la fonte autorevole e non scadono.
Gli eventi servono al grafico classificato e hanno retention limitata.
Durata proposta: 90 giorni, da approvare come policy operativa prima
dell'attivazione. Verificare supporto TTL sull'ambiente Mongo/Cosmos effettivo;
se non disponibile, definire una pulizia con il meccanismo operativo esistente
prima del rilascio. Nessun job schedulato nel servizio ShortLinks.

Prevedere indice per `(shortLinkId, timestamp)` e unicita' dell'ID evento.
Query e aggregazioni temporali devono essere eseguite sul database,
con filtri e intervalli limitati; non caricare l'intera collection.

### 4. Errori, coerenza e conteggio esattamente una volta

Non assumere transazioni tra collection su Cosmos Mongo RU.
Incrementare prima i totali atomici, quindi salvare l'evento.

Se fallisce il salvataggio del dettaglio dopo il conteggio, mantenere il
redirect, emettere log strutturato e metrica di errore e indicare copertura
incompleta nei dati analytics. E' una scelta esplicita: non bloccare
l'apertura del link per il solo dettaglio statistico.
Non rilanciare l'intero conteggio nel retry dell'evento; riusare l'ID evento.
Non usare task fire-and-forget o una coda in memoria che perda dati al riavvio.

Se fallisce l'aggiornamento autorevole dei totali, preservare la gestione
dell'errore del servizio: non dichiarare il conteggio riuscito.
Un documento eliminato tra risoluzione e incremento deve avere un esito
esplicito, non un finto incremento a zero.

Il grafico deve distinguere eventi registrati da copertura completa:
confrontare, nel periodo disponibile, il totale dei timestamp grezzi con gli
eventi classificati. Il residuo non attribuibile e' "richieste senza dettaglio",
che puo' comprendere storico, flag disattivo o errori di persistenza.
Fuori retention non rappresentare assenza di eventi come zero traffico.
Se la query eventi fallisce, esporre l'errore; non sostituirla con una serie vuota.

La scadenza degli eventi non riduce i contatori cumulativi. Il valore di un
intervallo e il totale cumulativo sono metriche diverse e vanno etichettati.

### 5. API e BackOffice

Mantenere i contratti esistenti e `GET /api/ShortLinks/{code}/clicks`
con il significato grezzo attuale. Non cambiare da array a oggetto la risposta
di quell'endpoint.

Aggiungere una lettura analytics dedicata, percorso proposto:
`GET /api/ShortLinks/{code}/analytics?range=24h`.
Riutilizzare risoluzione del link, autorizzazione, scope e validazione degli
intervalli esistenti. Rispondere con un DTO, non con documenti Mongo.

Contenuto previsto: totali cumulativi e residuo non segmentato, bucket
temporali per categoria e totale grezzo, intervallo realmente disponibile,
retention e indicazione esplicita della copertura. Supportare `1h`, `24h`,
`7d` e `all`; per `all` dichiarare che il dettaglio classificato e' limitato
alla retention e il totale cumulativo non lo e'.

Aggiornare tutte le superfici amministrative pertinenti:

- Dettaglio: totale grezzo, aperture probabilmente umane, anteprime social,
  altre automazioni, richieste nuove non classificabili e storico/non segmentato.
- Grafico: serie separate, legenda, copertura e limite di retention.
- Elenco: rinominare l'attuale colonna come totale richieste e aggiungere una
  sintesi classificata, evitando una chiamata analytics per ogni riga.
- Dashboard: conservare l'attuale metrica dei sette giorni come totale grezzo;
  aggiungere il confronto classificato nello stesso periodo, non la somma
  dei contatori cumulativi. Gestire esplicitamente dati parziali.

Usare il trasporto e i modelli esistenti; aggiornare sia il modello locale
BackOffice sia quello condiviso quando consumato. I nuovi campi analytics
sono di sola lettura, esclusi dai DTO di creazione e modifica.
Le risposte v1 e le proiezioni pubbliche mantengono `ClicksCount` grezzo:
non esporre header, regole interne o eventi attraverso API pubbliche.

Testo informativo: "Le aperture sono stimate dal tipo di client. Le anteprime
e le altre automazioni sono conteggiate separatamente. I dati precedenti
all'attivazione non sono classificabili."

### 6. Confini da preservare

Tutti i tipi di link canonici usano lo stesso classifier, senza modificare
URL, schema mobile, status HTTP o validazione dei target.
Link inesistenti, video non autorizzati per il canale pubblico e campagne
Ask non risolvibili restano esclusi dal conteggio.
`last` e `clear` restano fuori dal conteggio canonico come oggi.
HEAD e OPTIONS non diventano click: verificare il routing reale e aggiungere
una guardia sul metodo se necessaria, senza alterare la risposta prevista.

Il flusso newsletter oggi registra un click aggiuntivo nello stesso punto:
non presentarlo come metrica umana filtrata mentre continua a essere grezzo.
In questa consegna conservarne semantica e conteggio, documentare il limite
e verificare che non sia duplicato. La separazione delle statistiche
newsletter e' fuori perimetro.

## Sequenza di implementazione e rilascio

1. Definire categorie, DTO, policy di retention e flag, con tracking disattivo.
   Aggiornare ADR-004 per atomicita', retention e comportamento in errore.
2. Implementare classifier e repository, compresi mock e protezione dei
   contatori dai replacement gestionali. Aggiungere le prove di regressione.
3. Collegare il servizio ShortLinks e la registrazione eventi. Applicare
   manualmente gli indici verificati per l'ambiente; nessun backfill storico.
4. Implementare letture analytics e BackOffice, mantenendo compatibilita'
   con risposte prive di classificazione durante un deploy misto.
5. Distribuire prima lettori e writer compatibili, poi abilitare il flag
   nel servizio ShortLinks. Registrare data e versione di attivazione.
6. Verificare nel deployment reale sia il branded host sia `/sl/{code}`,
   osservare errori e categorie, quindi considerare il rilascio completo.

Un rollback sicuro disattiva la classificazione e conserva la versione
compatibile con i nuovi campi. Non eliminare campi o eventi.
Il traffico con flag disattivo torna nel residuo non segmentato.
Il rollback a un vecchio binario che sostituisce documenti puo' eliminare i
nuovi campi: deve essere vietato o preceduto da una verifica specifica.

## User Review Notes

Accettato dall'utente: i conteggi attuali restano non separabili.

Spazio modificabile per annotazioni su categorie, presentazione e policy.
La retention di 90 giorni e' una proposta del piano, non una decisione
gia' approvata. Non e' richiesta una pagina intermedia in questo perimetro.

## Files and Ownership

| Area | Files | Why it matters |
| --- | --- | --- |
| Modify | `MorWalPizVideo.ShortLinks/Controllers/ShortLinkController.cs`, `Services/IShortLinkRedirectService.cs`, `Program.cs` | Classificazione, tracking, metodo HTTP e wiring. |
| Add | Classifier e test sotto `MorWalPizVideo.ShortLinks` e `MorWalPizVideo.ShortLinks.Tests` | Regole pure, versionate e testabili. |
| Modify | `MorWalPizVideo.Models/Models/ShortLink.cs`, `MorWalPizVideo.Domain/Interfaces/IRepository.cs`, `Repository.cs`, `MockRepository.cs` | Contatori atomici e repository eventi. |
| Add | Modello e repository eventi negli attuali progetti Models/Domain | Dettaglio separato, retention e aggregazioni. |
| Modify | `MorWalPizVideo.Domain/FeatureServices.cs`, `ShortLinkMigrationService.cs` e writer canonici pertinenti | Preservazione contatori e concorrenza. |
| Modify | `MorWalPizVideo.BackOffice/Controllers/ShortLinksController.cs`, `Services/DashboardService.cs`, `Program.cs` | DTO analytics, scope e composizione. |
| Modify | `MorWalPiz.Contracts/Contracts/ShortLinkContract.cs`, `ContractUtils.cs`; DTO analytics dedicati | Proiezioni additive e metriche di sola lettura. |
| Modify | `MorWalPizVideo.BackOffice/Services/MongoIndexOperationsService.cs`, `docs/architecture/operations/mongo-index-manifest.phase4.json` | Audit/apply degli indici secondo il meccanismo esistente. |
| Modify | `frontend/back-office-spa/src/routes/shortLinks/`, `src/routes/Home.tsx`, `src/models/shortLink.ts` | Elenco, dettaglio, grafico e dashboard. |
| Modify | `frontend/fe-packages/models/src/shortLink.ts`, `frontend/fe-packages/services/src/endpoints.ts` e servizi pertinenti | Tipi e trasporto coerenti. |
| Modify | Suite ShortLinks e BackOffice, test SPA delle route interessate | Regressioni e compatibilita'. |
| Verify only | Servizi social, v1, proiezioni pubbliche e flusso newsletter | Nessun cambio involontario di URL o semantica. |

I nomi dei nuovi tipi e file si definiscono seguendo le convenzioni locali.
La tabella non autorizza modifiche ad aree non pertinenti.

## Validation Plan

### Criteri di accettazione

1. Fixture con 100 richieste storiche: prima di nuovo traffico mostra totale
   100, classificati zero e non segmentati 100.
2. Su quella fixture, una richiesta per ciascuna delle quattro categorie:
   totale 104, ogni categoria 1, non segmentati ancora 100.
3. Richieste concorrenti: nessun incremento perso e una sola categoria
   incrementata per richiesta. Verificare Mongo reale oltre al mock.
4. Browser desktop, Android, iOS e browser in-app mantengono il redirect
   attuale; crawler con token browser resta automatico.
5. User-Agent mancante o ambiguo non diventa automaticamente umano.
   HEAD/OPTIONS e richieste non risolvibili non incrementano.
6. Modifica gestionale concorrente e migrazione canonica conservano conteggi,
   eventi e ownership, senza attribuire categorie ai dati storici.
7. Inserimento evento fallito: totali corretti, redirect mantenuto, errore
   osservabile e grafico con copertura incompleta. Retry evento senza doppio
   incremento; errore di query non rappresentato come zero.
8. Scadenza eventi: contatori cumulativi invariati, grafico limitato
   esplicitamente alla retention. Bucket UTC, estremi temporali e intervalli
   validati senza doppio conteggio ai confini.
9. Analytics e dashboard rispettano canale e permessi esistenti; nessuna
   esposizione pubblica dei dati interni.
10. Attivazione/disattivazione del flag non riclassifica lo storico.
    UI compatibile con backend precedente e con serie parziali.

### Comandi previsti

Eseguire dalla root, con dipendenze disponibili secondo il runbook:

```bash
dotnet test MorWalPizVideo.ShortLinks.Tests/MorWalPizVideo.ShortLinks.Tests.csproj --configuration Release --filter "Category=TestGroup:ShortLinks"
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --filter "FullyQualifiedName~ShortLink|FullyQualifiedName~DashboardControllerTests"
yarn --cwd frontend workspace back-office-spa test --run src/routes/shortLinks src/routes/Home.test.tsx
yarn --cwd frontend workspace back-office-spa build
```

Estendere il filtro ai nuovi test specifici per indici, repository e writer
canonici effettivamente modificati. Per il gate Mongo usare una fixture
isolata sul motore compatibile con la produzione: il test mock da solo non
prova il supporto di atomicita', query e TTL del deployment.

Verifica nel deployment: usare uno short link dedicato, senza redirect
automatico nel client di prova, con User-Agent browser e crawler distinti.
Confermare delta attesi, destinazione invariata, assenza di dati identificativi
persistiti e percorso `/sl/{code}`. Una prova manuale su Facebook conferma
il comportamento osservato in quel momento, non che tutte le anteprime
future avranno lo stesso identificatore.

## Real Implementation Notes

Non implementato. Aggiornare dopo la consegna con file realmente modificati,
comandi eseguiti, risultati, versione classifier e stato del rollout.
Nessuna prova di produzione o esecuzione dei test e' dichiarata da questo piano.

## Residual Risks

- User-Agent falsificabile; bot sconosciuti possono sembrare browser.
- Il servizio non misura impression del post, utenti unici o visualizzazioni
  del contenuto finale.
- Le due scritture non sono una transazione: il dettaglio puo' essere
  incompleto, mentre i totali per categoria restano atomici.
- `ClickTimestamps` e' gia' una lista crescente: preservarla per compatibilita'
  non risolve quel limite. Non aggiungerne altre; una futura rimozione richiede
  un piano distinto di compatibilita'.
- Retention, TTL e indici richiedono approvazione e verifica operativa.
- Scanner newsletter restano nel conteggio grezzo della newsletter.

## Final Documentation Notes

Durante l'implementazione aggiornare ADR-004, backend, domain model,
API design, dashboard e deployment per descrivere comportamento effettivo,
retention, flag, errori e rollback. Aggiornare il runbook per i nuovi test.
Il documento di audit shortlink continua a descrivere azioni amministrative
e pubblicazioni: non usare `auditEvents` per le richieste anonime.
Non aggiungere connection string, credenziali o configurazioni segrete.