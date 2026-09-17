# Social publishing

La finestra `Pubblicazione social` compone post con foto o video locali, caption, hashtag, thumbnail, formato e data di invio. Gli hashtag usati sono salvati in SQLite con chiave tenant/canale e deduplicazione case-insensitive; `Settings.DefaultHashtags` non e la fonte della cronologia.

## Prerequisiti

- Configurare per ogni tenant/canale l'account della piattaforma e un token OAuth con gli scope richiesti dalla documentazione Meta.
- Salvare il token solo nella configurazione del canale e proteggerne il database secondo le policy della macchina. Il valore non viene scritto nei log o nelle richieste di telemetria.
- Usare un'app Meta approvata e account Business/Creator quando richiesto dalla piattaforma.
- Per Instagram Business e Threads, configurare il client BackOffice con `ApiEndpoint`, `ApiKey` e il canale selezionato. Il client carica i file su `POST /api/social-assets/upload` usando `multipart/form-data`, `X-API-Key`, `X-Channel-Id` e `Idempotency-Key`.
- Il BackOffice salva gli asset nel container privato `BlobStorage:SocialAssetContainerName` e restituisce solo una URL SAS read-only a scadenza. La firma richiede `BlobStorage:StorageAccountName` e `BlobStorage:StorageAccountKey`; managed identity da sola non è sufficiente con l'SDK/configurazione attuale.

## Capability e limiti

- Facebook Page e l'unico target rappresentato come schedulabile dal provider Graph, quando l'account e i permessi lo consentono.
- Instagram Business e Threads ricevono la SAS privata dell'asset caricato. Se la configurazione di firma SAS manca, l'upload fallisce esplicitamente; non viene usato alcun URL pubblico di fallback.
- Facebook personal profile non ha una API ufficiale generale per pubblicazione automatica da questo client: viene mostrato come non supportato.
- Il client non esegue scheduler locali: la data viene inviata solo quando la capability del provider la dichiara supportata.
- I formati sono limitati alla lista restituita dal provider. Non viene dichiarato supporto per combinazioni non esposte dalla capability.

Le configurazioni del canale sono predisposte dalla tabella SQLite `SocialChannelConfigurations`; il flusso di configurazione OAuth/UI delle credenziali resta un'estensione separata, perché nel repository non esistono client ID, redirect URI o segreti Meta concreti.