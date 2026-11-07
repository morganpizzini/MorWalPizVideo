# Terminologia canale: compatibilita null e diagnostica

Stato: implementata in working tree; sorgente e stack trace esaminati il 2026-11-07.
La forma Mongo del canale che fallisce non e stata letta.

## Outcome

GET channels/terminology non genera NullReferenceException per una configurazione
legacy assente/null/vuota. Restituisce sempre due array per una configurazione
validamente vuota. Una configurazione realmente corrotta deve produrre errore
esplicito e diagnostica, non una falsa lista vuota o perdita silenziosa.
Preservare RBAC e channel scope e proteggere anche i consumer AI dello stesso dato.

## Current Evidence

- Stack trace fornito: NullReferenceException in ChannelTerminologyRules.ToContract,
  riga 47 dell'artefatto CI; corrisponde al metodo nel
  [sorgente](../../MorWalPizVideo.BackOffice/Services/ChannelTerminology.cs).
- [ChannelsController](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs),
  GetTerminology, passa Channel.Terminology del channel context direttamente.
  GET/PUT/DELETE hanno RequireChannelScope e permessi view/update/manage.
- [YTChannel](../../MorWalPizVideo.Models/Models/YTChannel.cs) inizializza Terminology
  e le liste con valori non null, ma dati BSON esplicitamente null non sono
  protetti dalle annotazioni non-nullable. Non e provato che il campo mancante
  da solo causi il difetto: va verificata la deserializzazione.
- ToContract dereferenzia configurazione e ogni mapping; le liste sono enumerate
  con Select. Un mapping null puo produrre NRE, mentre una lista null normalmente
  causa ArgumentNullException LINQ: lo stack non basta a scegliere una sola causa.
- [AiWorkflowService](../../MorWalPizVideo.BackOffice/Services/Ai/AiWorkflowService.cs),
  BuildTerminology, e [LegacyAiWorkflowService](../../MorWalPizVideo.BackOffice/Services/Ai/LegacyAiWorkflowService.cs),
  SerializeTerminology, tollerano configurazione null ma non liste null.
  Entrambi usano default quando nessuna lista contiene dati.
- [Richiesta](../../MorWalPizVideo.BackOffice/DTOs/ChannelTerminologyRequests.cs)
  ha Source required; TryNormalize usa request.Source.Trim, quindi elementi null
  nelle liste vanno provati al boundary, senza fidarsi della sola annotation.
- [Detail SPA](../../frontend/back-office-spa/src/routes/channels/detail/Component.tsx)
  carica in effetto e mostra errore generico, senza azzerare sempre stato/errore
  al cambio entity/channel. Il transport usa channel context selezionato.
- [ChannelTerminologyTests](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTerminologyTests.cs)
  copre CRUD normalizzato e channel denial, non BSON legacy/null.

Ipotesi ordinate: Terminology esplicitamente null (alta plausibilita);
mapping null in una lista (media); lista null/forma legacy non conforme
(possibile ma tipo eccezione da confermare); solo campo assente (non dimostrato).

## Proposed Design

**Contratto di compatibilita.** Campo Terminology assente/null -> configurazione
vuota; lista assente/null -> array vuoto; target assente/null -> stringa vuota
(gia significato compatibile per invariantEnglish).
Oggetto mapping null, source assente/null/vuota, tipi BSON non conformi o termini
fuori limite -> corruzione: errore applicativo esplicito, non filtraggio silenzioso.
Assenza/null delle sezioni sono casi documentati, non eccezioni ingoiate.

**Boundary comune.** Piccolo normalizzatore puro nel progetto Models, accanto
al modello Terminology, cosi la compatibilita e riusata da API e AI senza
dipendenza verso BackOffice o Contracts. Esporre esito valido/configurazione
normalizzata o difetto tipizzato di struttura; nessun DTO HTTP nel modello.
Regole CRUD MaxMappings/MaxTermLength restano nel boundary admin; non spostare
tutte le validazioni o creare un servizio/repository nuovo.
Il modello puro non logga: gli owner API/AI registrano esito legacy o corruzione
con channel ID e categoria, mai termini/prompt o payload BSON completo.

**API.** Controller normalizza prima della proiezione. ToContract accetta solo
valori validati, oppure integra la stessa normalizzazione senza duplicate regole.
Dato corrotto -> 409 ProblemDetails con codice stabile `channel_terminology_invalid`,
messaggio sicuro e log; PUT/DELETE rimangono percorsi di riparazione autorizzati.
GET non scrive mai. Errori di deserializzazione devono essere segnalati al boundary
che carica il canale, non convertiti indiscriminatamente in NotFound o [].
TryNormalize respinge mapping null/source null con 400 e messaggio, mantiene
case-insensitive duplicate check e limiti 100/80.

**AI.** Applicare lo stesso esito prima di Count/Select in entrambi i workflow.
Configurazione legacy vuota continua a usare default esistenti; configurazione
corrotta ferma il workflow con errore esplicito, senza ripiegare sui default.
Non cambiare prompt, flag, scheduling o provider.

**SPA.** Stati load/success/error e retry separati; azzerare liste/errore all'inizio
del caricamento del nuovo canale, ignorare risultati tardivi e impedire Save
prima di un caricamento valido. Il parametro della detail route non deve
silenziosamente prevalere sul channel context: se differiscono, richiedere
selezione esplicita del canale visualizzato prima di caricare/salvare terminologia.
Mostrare diagnostica sicura per 409, non stack trace; mantenere eventuali
azioni di riparazione sotto gli stessi permessi.

Prima della patch raccogliere shape redatta del solo campo terminologia e
versione artefatto; riprodurre con BSON sintetico. Non copiare documenti canale
interi: possono contenere credentialCiphertext.
Impeccable solo per la SPA: refinement Operate `harden`, Bootstrap incumbent;
nessun PRODUCT/DESIGN admin trovato. Screenshot loading/error/retry e test focus.

## Impacted Projects

Models (normalizzazione pura), BackOffice API (CRUD/proiezione/AI), BackOffice SPA
e BackOffice.Tests. Domain verify-only per deserializzazione repository/channel
context; Contracts e servizi/models frontend conservano gli array wire.
ServerAPI/WPF/FreezerAnalysis verify-only come consumer Models: nessuna nuova
dipendenza, nessuna modifica auth o schema. AppHost/jobs/deployment topology invariati.
Includere Contracts/MvcHelpers e i consumer transitivi VideoImporter/InsightScanner
nell'inventario di compatibilita del build; non richiedere test Windows su Linux
e non modificare i workflow desktop per questo helper.

## Impacted Files

- **Modify:** [ChannelTerminology](../../MorWalPizVideo.BackOffice/Services/ChannelTerminology.cs),
  [ChannelsController](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs),
  [AiWorkflowService](../../MorWalPizVideo.BackOffice/Services/Ai/AiWorkflowService.cs),
  [LegacyAiWorkflowService](../../MorWalPizVideo.BackOffice/Services/Ai/LegacyAiWorkflowService.cs),
  [detail SPA](../../frontend/back-office-spa/src/routes/channels/detail/Component.tsx),
  [ChannelTerminologyTests](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTerminologyTests.cs),
  [detail tests](../../frontend/back-office-spa/src/routes/channels/__tests__/detail.test.tsx).
- **Add:** normalizzatore puro accanto a
  [YTChannel](../../MorWalPizVideo.Models/Models/YTChannel.cs); unit/BSON/AI regression
  test nel progetto BackOffice.Tests, secondo fixture esistenti.
- **Generated/Migration:** nessuno obbligatorio; nessuna migrazione automatica Mongo.
- **Documentation:** questa scheda, [backend](../architecture/backend.md) per
  compatibilita legacy; [handoff AI](ai-prompt-orchestration-handoff.md) per il
  comportamento vuoto/corrotto se cambia dopo implementazione.
- **Verify Only:** [DTO](../../MorWalPizVideo.BackOffice/DTOs/ChannelTerminologyRequests.cs),
  [ChannelContext](../../MorWalPizVideo.BackOffice/Services/ChannelContext.cs),
  [repository Mongo](../../MorWalPizVideo.Domain/Interfaces/Repository.cs),
  [mock](../../MorWalPizVideo.Domain/Interfaces/MockRepository.cs),
  [PrimaryScenario](../../MorWalPizVideo.Domain/Scenarios/PrimaryScenario.cs),
  [channel.ts](../../frontend/fe-packages/models/src/channel.ts),
  [apiTransport](../../frontend/fe-packages/services/src/apiTransport.ts).

## Impact Matrix

| Area | Impatto |
| --- | --- |
| Backend/API | GET tollerante, PUT validato, errore corruzione tipizzato |
| Frontend/UI | Load/error/retry, channel mismatch e stale data protetti |
| Database/Data | Compatibilita read-time; nessuna scrittura GET |
| Contracts/Models | Helper puro Models; response due array invariata |
| Authentication/Authorization | Scope/RBAC identici; nessun anonymous nuovo |
| Validation | Assente/null distinti da mapping corrotto; 100/80 e duplicati invariati |
| Cache | Nessun nuovo output cache/eviction; preservare lookup channel corrente |
| Configuration/Secrets | Nessun campo segreto letto/esposto/loggato per diagnosticare |
| Logging/Telemetry | Categoria legacy/corruzione e channel ID; no termini/transcript |
| Background Jobs | Nessuno; workflow AI non si avvia su configurazione corrotta |
| Deployment/Operations | API prima della UI error handling; monitorare 500/409 senza dati |
| Backward Compatibility | BSON legacy vuoto leggibile, default AI preservati, corruzione esplicita |

## Reuse Opportunities

YTChannel, ChannelTerminologyConfiguration, context/ownership, CRUD, ProblemDetails,
due workflow AI e fixture mock/WebApplicationFactory/BSON disponibili nel backend.

## Risks

- **High:** fallback [] nasconde termini perduti e AI usa default sbagliati;
  separare empty/corrupt; test nessun workflow provider chiamato su corruzione.
- **Medium:** patch solo proiezione lascia Count AI vulnerabile; normalizzatore
  condiviso e regressioni su entrambi i percorsi/flag.
- **Medium:** forma BSON non deserializzabile fallisce prima del normalizzatore;
  test deserializzazione prima patch; se presente, patch al caricamento ristretto
  con errore esplicito, non serializer globale permissivo.
- **Medium:** canale detail diverso dal selezionato; blocco UI e scope server;
  test mismatch e risposta tardiva.
- **Low:** warning ripetuti; categoria strutturata e diagnosi/backfill eventuale,
  senza introdurre un sistema di logging diverso.

## Technical Debt

Inizializzatori C# scambiati per garanzia sui dati persistiti; DTO terminologia
locali BackOffice e default AI duplicati. Correggere compatibilita condivisa;
spostamento DTO in Contracts e unificazione prompt/default sono fuori scope.

## Implementation Roadmap

1. Shape/versione e BSON sintetico: distinguere NRE top-level/elementi da null
   liste e deserializzazione; gate con riproduzione discriminante.
2. Normalizzatore puro e unit/BSON test; nessuna scrittura o dipendenza inversa.
3. API CRUD/proiezione e AI entrambi; gate empty/corrupt/permessi.
4. SPA stati/channel mismatch/retry; gate nessun save di dati non caricati.
5. Build backend una sola volta, test focalizzati senza rebuild, mai concorrenti;
   poi test detail SPA/build checked. Registrare esiti e deployment.
6. Review indipendente raccomandata per compatibilita dati e integrita AI.

## Testing Strategy

BackOffice.Tests: deserializzare BSON sintetico con campo assente, null, {},
liste assenti/null, lista valida, mapping null, source null/vuota, target null
e tipo inatteso; nessun dato reale.
GET deve produrre esattamente italianToEnglish/invariantEnglish array sui casi
vuoti compatibili; corruzione -> 409, GET senza update repository.
CRUD: liste request null secondo contratto vuoto, elementi/source null -> 400,
duplicati case-insensitive, limiti 100/80, GET/PUT/DELETE autorizzazione,
header assente 400 e altro canale 404 preservati.
AI: configurazione vuota -> default attuali; liste valide conservate; corruzione
-> errore prima gateway, per workflow nuovo e legacy.
SPA: load fallito, retry, nuovo canale senza vecchie righe/errori, mismatch,
late response e Save disabilitato. E2E canale sintetico legacy con GET/update/delete.

## Migration And Rollback

Read-time tolerance: nessun backfill richiesto per campi assenti/null, niente
nuovo indice. Eventuale pulizia dati e un'operazione separata approvata, idempotente,
con copia protetta e preservazione termini; non necessaria per questa patch.
Deploy API poi SPA. Rollback UI non modifica dati; rollback API puo riaprire
NRE su dati legacy: mantenere la patch di compatibilita o non ripristinare quella
versione. PUT/DELETE autorizzati sono le sole scritture di riparazione previste.

## Open Questions

La configurazione fallita ha Terminology null, mapping null o una forma BSON
non deserializzabile? Serve la shape redatta/versione, non il payload canale
completo. Se emerge un formato legacy strutturalmente diverso, il relativo
mapping deve essere approvato prima di aggiungere una conversione automatica.

## Implementation Handoff

Aggiunto `ChannelTerminologyNormalizer` in Models: configurazione/lista assente o
null viene trattata come vuota, mentre mapping null, source mancante e duplicati
sono invalidi. `GET /api/Channels/terminology` restituisce ProblemDetails 409 con
codice `channel_terminology_invalid` senza scrivere; entrambi i workflow AI
normalizzano prima di contare/selezionare e non usano i default su dati corrotti.
Il boundary PUT continua a validare input e mantiene scope/RBAC esistenti.

File modificati: `MorWalPizVideo.Models/Models/YTChannel.cs`,
`MorWalPizVideo.Models/Models/ChannelTerminologyNormalizer.cs`,
`MorWalPizVideo.BackOffice/Services/ChannelTerminology.cs`,
`MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs`,
`MorWalPizVideo.BackOffice/Services/Ai/{AiWorkflowService,LegacyAiWorkflowService}.cs`,
`MorWalPizVideo.BackOffice.Tests/Features/{ChannelTerminologyTests,ChannelBoundaryTests}.cs`.

Validazione: `dotnet test` Release focalizzato su `ChannelTerminologyTests`, 2/2
verdi; la compilazione ha incluso Models, Contracts, Domain, BackOffice,
ServerAPI e test. La solution completa non è stata portata a termine per i
progetti Windows su Linux e asset di test non ripristinati. Non è stata raccolta
la shape BSON reale; la forma non deserializzabile resta un rischio da verificare.
