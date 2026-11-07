# Cover blog: crop locale, alt automatico e upload esplicito

Stato: implementata nel worktree il 2026-11-07; non deployata.

## Outcome

Selezione immagine -> crop locale -> alt modificabile/precompilato -> bottone
esplicito "Upload". Nessuna richiesta di upload o salvataggio draft durante
selezione, crop, conferma crop o modifica alt. Solo l'Upload autorizza il
save-before-upload esistente.

Scope verificato: cover nel form blog. "Titolo dell'immagine" viene interpretato
come nome del file senza estensione, poiche non esiste un campo titolo immagine
separato in questo workflow. Non utilizzare il titolo dell'articolo per default.
Se la segnalazione riguarda anche logo, news o altri uploader, indicarli prima
di ampliare lo scope e senza estendere a essi il vincolo cover 1920 x 1080.

## Current Evidence

- [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx):
  effetto su preparedUpload/uploadAltText invia fetcher.submit appena entrambi
  sono validi; autoSubmittedUpload evita ripetizioni ma non rende l'azione esplicita.
- Lo stesso form mostra il bottone Upload solo quando alt manca o c'e errore:
  con crop e alt validi il controllo puo scomparire proprio quando serve.
- [test form](../../frontend/back-office-spa/src/routes/blog/form.test.tsx)
  richiedono esplicitamente upload immediato e auto-submit dopo inserimento alt.
  Il comportamento e quindi intenzionale nel checkout, ma contrario alla nuova richiesta.
- [BlogCoverCropper](../../frontend/back-office-spa/src/routes/blog/BlogCoverCropper.tsx)
  prepara un File locale; [coverImageCrop](../../frontend/back-office-spa/src/routes/blog/coverImageCrop.ts)
  mantiene 16:9 e output JPEG 1920 x 1080. Non serve introdurre un cropper nuovo.
- Action form salva draft nuovo/dirty prima di upload, usa la revisione ricevuta,
  invia purpose=cover e seleziona cover senza pubblicare.
- [Handoff cover precedente](blog-cover-image-crop-handoff.md) registra il crop
  gia implementato; questa proposta sostituisce solo il trigger "immediately".
- [BlogPostsController](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs)
  e [blogService](../../frontend/fe-packages/services/src/blogService.ts)
  sono il boundary media/revision/alt esistente, non ImageUploadController globale.

## Implemented Design

1. Rimosso l'effetto auto-submit e il ref usato esclusivamente per esso. Il
   submit del form upload tramite il bottone visibile **Upload** è l'unico
   punto che avvia la mutazione. Conferma crop e modifica alt non fanno rete.
2. Selezione nuova: crop non confermato; alt precompilato dal filename stem con
   separatori underscore/trattino convertiti in spazi, trim, limite 300 caratteri.
   Conservare lettere/numeri/Unicode del nome, senza "descrizioni AI".
   Filename senza stem significativo: default esplicito "Immagine"; rendere
   visibile che la descrizione puo essere migliorata.
3. Alt digitato dall'utente deve prevalere. Per ogni nuova selezione generare
   il proprio default, senza trascinare l'alt del file precedente. Non ricalcolarlo
   a ogni render, crop/export o cambio titolo articolo.
4. Alt vuoto o whitespace prima del submit: usare il default del file selezionato
   e renderlo visibile; non usare required per bloccare un fallback previsto.
   La API continua a ricevere sempre alt non vuoto e validato; nessun cambio
   della regola server cover-alt.
5. Conferma crop prepara JPEG e anteprima soltanto. Il bottone
   Upload, abilitato solo con crop confermato, autorizzazioni e stato idle.
   Messaggio locale "Crop pronto. Conferma l'upload." e label che non promettono
   upload immediato. Conferma crop e Upload sono azioni semanticamente distinte.
6. Modifica crop invalida prepared file e disabilita Upload; modifica alt non
   invalida crop e non fa rete. Upload usa alt corrente/default e il prepared
   file, mai la sorgente.
7. Pending impedisce doppio invio; errori mantengono crop/alt per retry esplicito.
   Cancel, cambio file/post/canale e unmount invalidano operazioni tardive e URL
   locali. Successo svuota i dati del file ma conserva cover appena selezionata.
8. Preservare save-before-upload, revisioni, esiti parziali, permessi, channel
   guard, 10 MB/25 MP, export 1920 x 1080 e save/publish separati.

Impeccable in implementazione: refinement Operate `clarify`/`harden`;
Bootstrap/Lucide e cropper incumbent, nessun PRODUCT/DESIGN admin trovato.
Screenshot 360/1440 px per source/crop-ready/uploading/error, input alt con
label, stato annunciato, focus e crop da tastiera gia disponibili.

## Implementation Details

- **Modified:** `frontend/back-office-spa/src/routes/blog/form.tsx` and
  `frontend/back-office-spa/src/routes/blog/form.test.tsx` only.
- Added the route-local `getUploadAltDefault` helper: it removes the final
  extension, converts underscore/hyphen runs to spaces, trims to 300
  characters, and uses the visible deterministic fallback `Immagine` when no
  stem remains.
- Each file selection resets crop preparation and replaces the editable alt
  with that file's default. A blank/whitespace alt is made visible and
  resolved to the same default at explicit submission time.
- The prepared crop remains available after errors for an explicit retry. The
  existing action still performs draft-save-before-upload, uses the returned
  revision, preserves channel and permission guards, and sends only the
  prepared crop with `purpose=cover`.
- No image endpoint, server validation, crop geometry/export, other uploader,
  lockfile, or generated file was changed.

## Validation Performed

- `cd frontend/back-office-spa && yarn vitest run src/routes/blog/form.test.tsx`
  — passed, 1 file / 23 tests before the final filename-fallback assertion was
  added. A final rerun was attempted but the existing
  `frontend/node_modules/jsdom` installation was incomplete (`index.js` /
  package metadata missing), so Vitest could not start; no dependency or
  lockfile was changed.
- `cd frontend/back-office-spa && yarn build` — passed TypeScript project build
  and Vite production bundle (existing large-router-chunk warning only).
- Impeccable context was run once for the form surface; `clarify`, `harden`,
  `operate`, and craft-floor guidance were read. The mechanical detector
  passed with no findings.

Browser screenshot/real viewport validation was not run. The focused tests
cover explicit interaction, no request before Upload, filename-derived and
fallback alt, prepared-crop retry, and existing save/revision/action behavior.
The final test rerun is the only validation blocker and is environmental.

## Impacted Projects

BackOffice SPA e test feature. Servizi/model frontend e BackOffice API/Domain
verify-only; nessun contratto media, formato, persistenza o policy globale cambia.
Nessun effetto pubblico, VideoImporter, AppHost o altri uploader.

## Impacted Files

- **Modify:** [form.tsx](../../frontend/back-office-spa/src/routes/blog/form.tsx),
  [form.test.tsx](../../frontend/back-office-spa/src/routes/blog/form.test.tsx).
  [BlogCoverCropper](../../frontend/back-office-spa/src/routes/blog/BlogCoverCropper.tsx)
  solo per copy locale se richiesto.
- **Add:** helper puro filename-alt e test route-local solo se non basta una
  funzione piccola nel modulo esistente; nessun package nuovo.
- **Generated/Migration:** nessuno.
- **Documentation:** questa scheda e [handoff crop](blog-cover-image-crop-handoff.md)
  aggiornato dopo implementazione con nuovo trigger e prove.
- **Verify Only:** [crop geometry](../../frontend/back-office-spa/src/routes/blog/coverImageCrop.ts),
  [crop tests](../../frontend/back-office-spa/src/routes/blog/coverImageCrop.test.ts),
  [cropper tests](../../frontend/back-office-spa/src/routes/blog/BlogCoverCropper.test.tsx),
  [transport tests](../../frontend/back-office-spa/src/routes/blog/transport.test.ts),
  [BlogControllerTests](../../MorWalPizVideo.BackOffice.Tests/Features/BlogControllerTests.cs),
  [BlogPostsController](../../MorWalPizVideo.BackOffice/Controllers/BlogPostsController.cs),
  [workflow SPA](../../.github/workflows/deploy-back-office-spa.yml).

## Impact Matrix

| Area | Impatto |
| --- | --- |
| Backend/API | Invariato; stessa purpose cover e validazione |
| Frontend/UI | Trigger unico manuale, alt default e stato locale |
| Database/Data | Save draft solo dopo conferma Upload; media gia salvati invariati |
| Contracts/Models | Nessuna aggiunta titolo immagine |
| Authentication/Authorization | Permessi update/create/manage e channel guard preservati |
| Validation | Alt effettivo non vuoto <=300; file preparato e limiti esistenti |
| Cache | Invariata; nessun publish o purge nuovo |
| Configuration/Secrets | Nessun impatto |
| Logging/Telemetry | Errori action/partial success esistenti, niente silent retry |
| Background Jobs | Non applicabile |
| Deployment/Operations | Solo bundle SPA; nessun backend deploy obbligatorio |
| Backward Compatibility | Crop/export e save-before-upload invariati; cambia trigger voluto |

## Reuse Opportunities

Cropper/geometry/export, fetcher form, action upload, payload purpose cover,
validazione API e test sequenza draft/revision.

## Risks

- **Medium:** resta un percorso di auto-submit; assert fetcher e mutazioni
  zero prima di Upload anche dopo validazione alt/crop.
- **Medium:** alt di un file precedente o crop obsoleto; stato per selezione e
  test replacement/cancel/risultati tardivi.
- **Medium:** doppio invio o draft salvato prima della conferma; pending e gate
  unico; test numero chiamate e stato server prima/dopo click.
- **Low:** filename e un alt poco descrittivo; default modificabile, non equivalente
  a garanzia editoriale/accessibilita; non inventare descrizioni.

## Technical Debt

Test/copy e guardia autoSubmittedUpload codificano il vecchio automatismo:
aggiornarli insieme e non mantenere due modi "auto/manual". Media manager
universale e form di altre entita restano fuori scope.

## Implementation Roadmap

1. Test discriminante: zero submit durante selezione/crop/alt, uno al click.
2. Stato/default alt e bottone sempre visibile; eliminare unico auto-trigger.
3. Verificare retry/cancel/change-channel e conservare action/save sequencing.
4. Test crop/transport/form in un unico set mirato, build checked SPA.
5. Screenshot prima/dopo e aggiornamento handoff crop. Stop al confine cover.
   Review indipendente facoltativa; utile se si tocca action/revision oltre la UI.

## Testing Strategy

Vitest/Testing Library nel workspace back-office-spa:
filename normale/dot/Unicode/separatori/lungo; default vs custom alt;
file replacement; crop confermato senza submit; alt edit/clear senza submit;
Upload un solo submit con JPEG preparato e alt effettivo; pending doppio click;
failure/retry manuale; cancel e cambio canale; stessa sorgente riselezionabile.
Preservare test save dirty/new -> returned revision -> upload -> cover unsaved,
409/partial success e autorizzazioni. Crop tests continuano a provare esattamente
1920 x 1080, non solo aspect ratio. Backend focused regressione solo se vengono
modificati action/transport/payload; nessuna nuova migrazione.
Browser mobile/desktop: crop touch/tastiera e nessuna mutazione di rete prima di Upload.

## Migration And Rollback

Nessuna migrazione dati/media/API. Rollout SPA senza flag aggiuntivo; rollback
riporta l'automatismo precedente, quindi non usarlo come soluzione definitiva.
Upload gia completati restano validi; nessuna cancellazione di immagini o draft.

## Open Questions

Confermare che la schermata segnalata sia cover blog e che "titolo immagine"
significhi filename stem. Se esiste un altro campo titolo o un altro uploader,
serve una scheda mirata prima di estendere il workflow.
