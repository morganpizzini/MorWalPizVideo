# Banner cookie mobile e layout di lettura articolo

Stato: B1 cookie e B2 layout articolo implementati il 2026-11-07.

## Outcome

Banner cookie ancorato al fondo con altezza determinata dal contenuto, non quasi
fullscreen. Nel dettaglio articolo desktop la cover ha la stessa colonna del
testo; rimuovere il recap visibile sopra la cover e cambiare "Commenti approvati"
in "Commenti". Preservare il layout mobile articolo gia giudicato corretto.
Recap e moderazione restano nei dati: cambia solo la presentazione.

## Current Evidence

- [ConsentBanner](../../frontend/morwalpizvideo.client/src/analytics/ConsentBanner.tsx)
  ha gia aside fisso al fondo e tre azioni; gestisce prima scelta e riapertura.
- [SCSS banner](../../frontend/morwalpizvideo.client/src/analytics/consent-banner.scss):
  `.consent-banner__copy` usa `flex: 1 1 24rem`; sotto 768 px il panel diventa
  column ma il flex-basis resta 24rem. In verticale quella base agisce
  sull'altezza: meccanismo credibile dell'eccesso, da misurare nel browser.
- [BlogArticle](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx):
  recap lead sopra img `img-fluid w-100`, senza limite di colonna sulla testata.
- [Stile blog](../../frontend/morwalpizvideo.client/src/routes/blog/style.scss)
  e [stile globale](../../frontend/morwalpizvideo.client/src/main.scss):
  documento/share/interazioni max 75ch, mentre la superficie puo arrivare
  alla larghezza del contenitore sito (1540 px).
- [Interactions](../../frontend/morwalpizvideo.client/src/routes/blog/Interactions.tsx)
  e [test articolo](../../frontend/morwalpizvideo.client/src/routes/blog/index.test.tsx)
  contengono l'etichetta "Commenti approvati".

## Proposed Design

Due sottointerventi indipendenti:

**B1 Cookie.** Al breakpoint mobile la copy non deve espandersi usando la base
flex orizzontale. Il pannello resta ancorato in basso, con altezza limitata al
45% del viewport dinamico (fallback `vh`) e scroll interno quando il contenuto
lo richiede. I controlli mantengono un target minimo di 44px e la safe area
esterna resta invariata.
Obiettivo di accettazione proposto: a font standard il primo banner occupa al
massimo il 40% di 360 x 800 e 390 x 844; preferenze al massimo il 50%.
Su 667 x 375 e zoom 200% non pretendere questi rapporti: niente clipping,
scroll interno e almeno parte della pagina visibile.

**B2 Articolo.** Introdurre un vincolo route-local della colonna di testata,
cover e metadati uguale ai 75ch del documento, con centro/allineamento comuni.
La cover mantiene proporzioni naturali, non viene ricroppata e non supera la
colonna. Non cambiare card/lista blog o il renderer condiviso.
Rimuovere il paragrafo recap dalla testata per tutti i viewport; conservarlo
nella lista e nel payload usato da metadata/preview.
Etichetta pubblica "Commenti" con conteggio attuale; non cambiare filtri approved,
messaggi sul commento pending o autorizzazioni.

Impeccable in implementazione: refinement; cookie Operate `adapt`, articolo Read
`adapt`/`clarify`. Autorita: [DESIGN](../../frontend/morwalpizvideo.client/DESIGN.md)
e componenti correnti; nessun PRODUCT locale trovato. Niente redesign, palette
nuova o artefatti di design obbligatori per la patch.

## Impacted Projects

Solo public client e suoi test. Models/services/layout, API e BackOffice verify-only:
nessun contratto, renderer, media o dato cambia. Nessun effetto WPF/AppHost/deployment
topology; workflow client esistente sufficiente.

## Implementazione B2

- `BlogArticle` ora usa la stessa misura `75ch` per testata, cover e corpo.
- La cover mantiene proporzioni naturali (`height: auto`) e non viene ricrop­pata.
- Il summary resta nel payload e nei metadata SEO/social, ma non è più mostrato
  sopra la cover.
- L'etichetta pubblica è `Commenti`; conteggio, filtri approved, moderazione e
  autorizzazioni non sono cambiati.
- Non sono stati modificati banner, account flow, API, modelli o renderer condiviso.

## Impacted Files

- **Modify:** [blog/index.tsx](../../frontend/morwalpizvideo.client/src/routes/blog/index.tsx),
  [blog/style.scss](../../frontend/morwalpizvideo.client/src/routes/blog/style.scss),
  [Interactions.tsx](../../frontend/morwalpizvideo.client/src/routes/blog/Interactions.tsx),
  [index.test.tsx](../../frontend/morwalpizvideo.client/src/routes/blog/index.test.tsx).
- **Add:** nessun file.
- **Generated/Migration:** nessuno.
- **Documentation:** questa scheda; aggiornare le prove visive nell'
  [handoff interazioni](blog-reader-interactions-handoff.md) dopo implementazione.
- **Verify Only:** [consent.ts](../../frontend/morwalpizvideo.client/src/analytics/consent.ts),
  [main.scss](../../frontend/morwalpizvideo.client/src/main.scss),
  [Interactions.test.tsx](../../frontend/morwalpizvideo.client/src/routes/blog/Interactions.test.tsx),
  [workflow](../../.github/workflows/deploy-morwalpizvideo-client.yml).

## Impact Matrix

| Area | Impatto |
| --- | --- |
| Backend/API | Nessuno; endpoint comments invariati |
| Frontend/UI | Dimensioni responsive, gerarchia cover/testo, etichetta |
| Database/Data | Recap e stato approved conservati |
| Contracts/Models | Nessuno |
| Authentication/Authorization | Nessuno; azioni consenso equivalenti e pubbliche |
| Validation | Misure viewport reali, contenuto lungo e accessibilita |
| Cache | Solo normale aggiornamento asset/PWA; niente nuova cache |
| Configuration/Secrets | Nessuno; non aggiornare versione consenso per una patch grafica |
| Logging/Telemetry | Analytics restano disattivate senza consenso |
| Background Jobs | Non applicabile |
| Deployment/Operations | Release bundle client, verifica anche PWA aggiornata |
| Backward Compatibility | Layout mobile preservato; nessun cambiamento moderazione |

## Reuse Opportunities

Colonna 75ch, breakpoint Bootstrap, token magazine, banner e stato consenso
esistenti, renderer blog e conteggio approvedComments senza rinomine wire.

## Risks

- **Medium:** CSS globale sovrascrive patch; usare selettori route-local e
  screenshot/bounding boxes a 360/390/768/1440 px.
- **Medium:** banner taglia azioni in landscape/zoom; scroll e focus testati,
  nessun controllo irraggiungibile.
- **Low:** rimozione recap anche da SEO o moderazione; test lista/payload/share
  e assert che commenti pending non diventino pubblici.

## Technical Debt

Duplicazione stili colonna blog tra SCSS locale/globale; non consolidarla ora.
Riutilizzare il valore vigente e verificare la cascade, senza refactoring layout.

## Implementation Roadmap

1. Misurare banner e cover correnti insieme su mobile/desktop.
2. B1: correggere dimensionamento nel breakpoint; gate sui rapporti e safe area.
3. B2: allineare testata/cover, rimuovere recap e cambiare etichetta; gate sulla
   larghezza cover uguale alla colonna entro 2 px a 1440 px.
4. Aggiornare test e raccogliere screenshot prima/dopo nella sessione, non nel
   codice generato. Coordinare con [metadata](blog-social-preview-remediation-handoff.md).
5. Build/typecheck client e test mirati; annotare risultati e fermarsi.
   Review indipendente facoltativa, proporzionata alla patch UI.

## Testing Strategy

Vitest client: lista conserva summary, articolo non mostra lead summary ma
metadata lo mantengono, etichetta Commenti e conteggio presenti, interazioni
pending/anonime invariate. Consenso: accetta/rifiuta/riapri e policy version invariati.
Browser: 360 x 800, 390 x 844, 768 px, 1440 px e 667 x 375; font standard/200%,
testo lungo, iOS safe area, tab order, focus, touch target almeno 44 px.
Registrare bounding boxes reali: non basta una snapshot DOM per provare dimensioni.
Unit/backend/contract/migration test nuovi non necessari.

## Migration And Rollback

Nessuna migrazione account, Mongo, API o consenso. Rilascio del solo client e
normale aggiornamento PWA; rollback bundle ripristina il layout precedente.
Non cancellare localStorage consenso e non forzare nuove accettazioni.

## Validation And Residual Risks

Validation eseguita: test Vitest mirati `blog/index.test.tsx` e
`blog/Interactions.test.tsx` (5 test passati complessivamente). Il test SSR
verifica che il summary non sia un lead visibile, che la label sia `Commenti` e
che la moderazione resti leggibile. Nessuna prova browser/bounding-box è stata
eseguita; il downstream final procedure può eseguire il build workspace e la
verifica visuale serializzata. `npm --prefix frontend/morwalpizvideo.client run
build:all` ha completato build client e SSR. Per B1, il CSS imposta una massima
altezza di 45dvh (con fallback 45vh), scroll verticale contenuto e rimuove la
base flex mobile dalla copy.

Restano da verificare in browser le misure 360/390/768/1440 px, il caso
landscape/zoom 200% e il comportamento PWA dopo il rilascio.

## Open Questions

Nessuna decisione architetturale bloccante. I rapporti di altezza sono criteri
proposti per approvazione, non una misurazione gia eseguita.
