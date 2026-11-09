# Link social pubblici del canale e provider Facebook

Stato: implementata in working tree; evidenza sorgente del 2026-11-07.

## Outcome

Header e footer mostrano solo profili social valorizzati nel canale pubblico.
Aggiungere Facebook ai provider configurabili; rendere anche Telegram configurabile,
altrimenti non e possibile sostituire il link statico attuale. Preservare Discord
come provider pubblico configurabile, senza inferirlo dai menu.
Condivisione articolo e publishing restano concetti separati.

## Current Evidence

- [SocialLinks](../../frontend/morwalpizvideo.client/src/components/SocialLinks.tsx)
  ha Telegram/Instagram/YouTube statici; Discord viene cercato in headerItems.
  E usato da [header](../../frontend/morwalpizvideo.client/src/layouts/title-header.tsx)
  e [footer](../../frontend/morwalpizvideo.client/src/layouts/footer.tsx).
- [YTChannel](../../MorWalPizVideo.Models/Models/YTChannel.cs) contiene `Socials`
  come provider/handler string; Facebook non richiede un nuovo campo persistito.
  `SocialPublishing.Facebook` esiste gia ma contiene destinazione/ciphertext:
  non e una sorgente per link pubblici.
- [ChannelsController](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs),
  `NormalizeSocials`, ammette instagram/youtube/reddit/x/patreon; trim/lowercase,
  duplicati last-wins. [Form admin](../../frontend/back-office-spa/src/routes/channels/form/Component.tsx)
  propone gli stessi cinque provider.
- [ChannelContract](../../MorWalPiz.Contracts/Contracts/ChannelContract.cs) e
  [channel.ts](../../frontend/fe-packages/models/src/channel.ts) hanno gia socials.
- [Navigation](../../MorWalPizVideo.Domain/PageService.cs), `GetPublicAsync`,
  seleziona una sola navigazione attiva globalmente; non usa il canale configurato
  del blog. Estendere questo DTO per ricavarne il canale sarebbe un accoppiamento
  errato e puo fallire quando nessun menu e attivo.
- [BlogController](../../MorWalPizVideo.ServerAPI/Controllers/BlogController.cs)
  risolve `YouTubeChannelId` mediante IContentService: e il confine pubblico da seguire.
- [QuickLinksService](../../MorWalPizVideo.Domain/QuickLinksService.cs) ha gia
  validazione HTTP/host per Facebook/Telegram/Instagram/video; riusare le regole
  comuni dove corrispondono, senza modificare le semantiche QuickLinks.
- La condivisione Facebook dell'articolo esiste gia in
  [BlogArticle](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx).

## Proposed Design

**Ownership.** Channel.Socials e la fonte unica dei profili; Domain possiede
policy provider/risoluzione URL, Contracts la proiezione pubblica, ServerAPI
la lettura anonima del canale configurato. Nessuna lista canali admin dal client.

**Policy.** Conservare i cinque provider e aggiungere facebook/telegram/discord.
Ordine pubblico deterministico: telegram, discord, instagram, youtube, facebook,
x, reddit, patreon; assenti omessi. Preservare last-wins nei duplicati del salvataggio.
Handler vuoto significa rimozione della riga prima dell'invio admin, non un URL
inventato. Provider normalizzato lowercase invariant.

- Handle semplici: rimuovere il solo prefisso @, validare identificatore e
  costruire path sul dominio provider (instagram.com, youtube.com con @,
  facebook.com, t.me, x.com, reddit.com/user, patreon.com).
- Discord: codice invito -> discord.gg; consentire URL discord.com/invite.
- URL completi: solo HTTPS, host provider esatto o sottodominio legittimo,
  niente userinfo, caratteri controllo o schema alternativo. Conservarne path,
  query/fragment pubblici compatibili con la piattaforma; non accettare host
  lookalike. Questa compatibilita serve anche a Facebook profile.php con ID.
- Non derivare YouTube da ChannelId o Telegram da una costante se socials manca.
  Nessun parametro di iscrizione YouTube aggiunto arbitrariamente.
- Input nuovo invalido: errore 400 esplicito. Legacy sconosciuto/invalido:
  omettere solo la voce non sicura, warning strutturato provider/channel senza
  handler e avviso nel dettaglio admin; non sostituirla con link statici.

**API.** Lettura nuova `GET /api/v1/channel/socials`, anonima esplicita, risolve
il canale con la stessa configurazione del blog e IContentService.
DTO minimo: elenco `links` di provider e URL pubblico risolto; nessuna entita
completa, ID account, publishing, token, destinationId o ciphertext.
Canale valido senza socials -> 200 elenco vuoto; configurazione mancante/canale
inesistente -> errore esplicito e log, non successo-shaped fallback.
Cache output con durata 300 secondi e tag centralizzato `CacheKeys.Channels`
gia lowercase. Mutazioni canale gia resettano/purgano channels: testare il giro reale.

**Client.** Helper shared per endpoint tramite publicApiService; provider dati
app-local nel layout root, distinto dalla navigazione. Un caricamento condiviso
header/footer, stati loading/error distinti da elenco vuoto. Errore visibile
accessibile con retry, nessun fallback statico; le altre parti del menu funzionano.
Icone Font Awesome gia caricate, label leggibili, rel noopener noreferrer.
Nessuna modifica ai link Facebook/WhatsApp/X di condivisione articolo.

Impeccable: refinement, header/footer Operate, `harden` per stati e `adapt`
per wrapping; [DESIGN pubblico](../../frontend/morwalpizvideo.client/DESIGN.md).
Admin Bootstrap/Lucide incumbent, nessun PRODUCT/DESIGN locale trovato.
Screenshot 360/1440 px con zero, uno e otto provider; tastiera e zoom 200%.

## Impacted Projects

Domain, Contracts, ServerAPI, BackOffice, shared models/services, public client,
BackOffice SPA e test BackOffice. Models verify-only salvo estrazione strettamente
necessaria di policy comune: nessuna modifica schema YTChannel prevista.
Tutti i workspace nel [manifest](../../frontend/package.json) verify-only per
export additivi; Shooting ITA e VideoImporter mantengono le loro semantiche.
Nessun lavoro su publishing, WPF, AppHost, shop o tools standalone.
Per Contracts, verificare anche MvcHelpers, FreezerAnalysis API e InsightScanner
come consumer diretti o transitivi dei project reference, senza cambiare i loro
DTO o workflow. Per frontend concentrare i gate sui consumer shared effettivi
elencati nell'[indice](public-client-admin-remediation-handoff.md).

## Impacted Files

- **Modify:** [ChannelsController](../../MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs),
  [form Component](../../frontend/back-office-spa/src/routes/channels/form/Component.tsx),
  [form action](../../frontend/back-office-spa/src/routes/channels/form/action.ts),
  [detail](../../frontend/back-office-spa/src/routes/channels/detail/Component.tsx)
  per feedback legacy; [SocialLinks](../../frontend/morwalpizvideo.client/src/components/SocialLinks.tsx),
  [root](../../frontend/morwalpizvideo.client/src/routes/layout/root.tsx);
  [endpoints-frontend](../../frontend/fe-packages/services/src/endpoints-frontend.ts),
  [services index](../../frontend/fe-packages/services/src/index.ts),
  [models index](../../frontend/fe-packages/models/src/index.ts),
  [ServerAPI Program](../../MorWalPizVideo.ServerAPI/Program.cs) per DI se necessaria.
- **Add:** policy URL nella directory Domain proprietaria; DTO pubblico in
  Contracts/Contracts; controller ServerAPI; servizio e tipo frontend accanto
  agli endpoint channel; provider app-local e relativi test. I nomi nuovi
  seguano i pattern di queste directory, senza un package nuovo.
- **Generated/Migration:** nessuno; backfill non obbligatorio.
- **Documentation:** questa scheda, [frontend](../architecture/frontend.md),
  [API design](../architecture/api-design.md) per nuovo endpoint pubblico.
  ADR solo se l'implementazione cambia la policy pubblica oltre questo confine.
- **Verify Only:** [YTChannel](../../MorWalPizVideo.Models/Models/YTChannel.cs),
  [ContractUtils](../../MorWalPiz.Contracts/ContractUtils.cs),
  [QuickLinksService](../../MorWalPizVideo.Domain/QuickLinksService.cs),
  [CacheKeys](../../MorWalPizVideo.Models/Constraints/CacheKeys.cs),
  [routing tests](../../frontend/back-office-spa/src/routes/channels/__tests__/routing.test.ts),
  [detail tests](../../frontend/back-office-spa/src/routes/channels/__tests__/detail.test.tsx),
  [ChannelTenancyAuthorizationTests](../../MorWalPizVideo.BackOffice.Tests/Features/ChannelTenancyAuthorizationTests.cs),
  workflow ServerAPI/BackOffice/public/admin esistenti.

## Impact Matrix

| Area | Impatto |
| --- | --- |
| Backend/API | Nuova lettura pubblica minima; policy riusata dall'admin |
| Frontend/UI | Social dinamici, provider admin aggiunti, loading/error |
| Database/Data | Socials gia esistente; nessuna collection/enum BSON nuova |
| Contracts/Models | DTO pubblico additivo provider/url; management invariato |
| Authentication/Authorization | GET anonimo; scritture canale con RBAC/ownership/CSRF attuali |
| Validation | Allowlist provider/host e identificatori; dati legacy esplicitamente segnalati |
| Cache | Tag channels, invalidazione cross-API e durata limitata |
| Configuration/Secrets | YouTubeChannelId esistente; publishing completamente escluso |
| Logging/Telemetry | Warning per voce legacy non sicura; errore risoluzione canale |
| Background Jobs | Non applicabile; nessuna API social chiamata per risolvere link |
| Deployment/Operations | Backend prima del client; verificare base URL e purge |
| Backward Compatibility | Aggiunte provider string; zero fallback statico e nessuna modifica publishing |

## Reuse Opportunities

ChannelSocial, IContentService, proiezioni Contracts, publicApiService, cache
channels e helper invalidazione esistenti. Estrarre soltanto guardie URI comuni
da QuickLinks se il riuso non cambia il comportamento del suo validatore.

## Risks

- **High:** esposizione publishing o collegamento a host ostile; DTO minimo e
  allowlist; test negativi con ciphertext sentinella e host lookalike.
- **Medium:** canale navigazione diverso da canale blog; usare configurazione
  ServerAPI, non headerItems; test due canali e navigazione assente.
- **Medium:** social vecchi restano in cache; gate update -> purge -> GET nuovi.
- **Medium:** URL legacy prima accettati diventano non validi; warning e correzione
  manuale, niente riscrittura automatica o perdita silenziosa al salvataggio.

## Technical Debt

Allowlist nel controller, handler finora non validato come URL e Discord inferito
dal menu. Sono in scope per garantire sicurezza/coerenza; non unificare publishing
e profili pubblici, non riscrivere la selezione navigazione globale.

## Implementation Roadmap

1. Domain: policy e test su handle/URL/legacy; gate compatibilita QuickLinks.
2. Contracts e tipo frontend additivi; gate export/consumer.
3. API e cache: risoluzione configurata, DTO minimo, errori e test; gate anonimo
   senza accesso al management.
4. Admin: provider/validazione/feedback e round-trip; gate ownership e publishing invariati.
5. Client: caricamento condiviso e icone; gate solo socials valorizzati.
6. Backend prima dei bundle; compilazione shared in ordine models/services/layout,
   build consumer e verifica pubblicata cache. Review indipendente raccomandata.

## Testing Strategy

Unit Domain: @, handle/URL, Facebook numerico/profile.php, YouTube @ e URL /channel,
inviti Discord, Telegram, provider ignoto, duplicati, blank, javascript/http,
userinfo, host lookalike e input con caratteri controllo.
Backend BackOffice.Tests: GET anonimo minimo, canale corretto/assente, nessun
publishing, save Facebook/Telegram/Discord, ownership/RBAC/CSRF e cache channels.
Frontend: routing/detail admin, helper public credentials omit senza bearer o
X-Channel-Id, loading/error/retry, lista vuota, stessa lista header/footer,
nessun URL statico; build dei consumer shared senza modifiche shop.
E2E: modifica admin di un profilo -> lettura pubblica dopo invalidazione.

## Migration And Rollback

Nessuna migrazione Mongo: provider string e socials esistono gia. Nessun backfill
da costanti senza consenso editoriale; valorizzare i profili manualmente nell'admin.
Deploy backend prima del client. Rollback client possibile; vecchio backend non
riconosce nuovi provider in scrittura: evitare edit canali con quella versione
finche non viene ripristinata la policy aggiornata. Non eliminare socials nuovi.

## Open Questions

Confermare gli handle/URL editoriali da valorizzare, in particolare Facebook,
Telegram e Discord. Non bloccano lo sviluppo con fixture sintetiche, ma bloccano
la prova dei collegamenti reali; non dedurli dalle credenziali publishing.

## Implementation Handoff

Implementati `ChannelSocialLinks` in Domain, il DTO minimo `PublicChannelSocialsContract`,
`GET /api/v1/channel/socials` in ServerAPI con risoluzione `YouTubeChannelId`, output
cache di 300 secondi e tag `channels`, oltre ai provider Facebook/Telegram/Discord
nel salvataggio BackOffice. Il client pubblico carica una sola volta i profili
configurati tramite `publicApiService` (credentials omit) e li usa in header/footer;
non esistono più fallback statici o inferenze dalla navigazione. Aggiunti test di
proiezione sicura e round-trip provider nel percorso BackOffice.

File modificati: `MorWalPizVideo.Domain/ChannelSocialLinks.cs`,
`MorWalPiz.Contracts/Contracts/PublicChannelSocialsContract.cs`,
`MorWalPizVideo.ServerAPI/Controllers/ChannelSocialsController.cs`,
`MorWalPizVideo.BackOffice/Controllers/ChannelsController.cs`,
`frontend/fe-packages/services/src/endpoints-frontend.ts`,
`frontend/fe-packages/models/src/channel.ts`,
`frontend/morwalpizvideo.client/src/routes/layout/{root,socials}.tsx`,
`frontend/morwalpizvideo.client/src/components/SocialLinks.tsx`,
`frontend/back-office-spa/src/routes/channels/form/Component.tsx` e
`MorWalPizVideo.BackOffice.Tests/Features/ChannelBoundaryTests.cs`.

## Icone social header/footer

Font Awesome e aggiornato alla versione 6.7.2 per supportare `fa-x-twitter`,
prima assente nella versione 5 caricata dal client. I pulsanti Facebook e X
hanno ora sfondi circolari dedicati, coerenti con Instagram e YouTube; la
modalita compatta dell'header mantiene i suoi colori esistenti.

File aggiornati: `frontend/morwalpizvideo.client/index.html` e
`frontend/morwalpizvideo.client/src/layouts/title-header.scss`. Verificati
il test SocialLinks (1/1), il test header pubblico (3/3), il build di
produzione del client e la presenza di `fa-x-twitter` nel CSS CDN caricato.

Validazione: build/test focalizzato BackOffice Release compilato i progetti
interessati; `ChannelTerminologyTests` 2/2 verdi. Il build dell'intera solution
è stato interrotto per progetti Windows e asset di test preesistenti mancanti.
Restano da eseguire il build TypeScript del workspace e la verifica reale dei
profili editoriali; nessun dato di produzione è stato letto.
