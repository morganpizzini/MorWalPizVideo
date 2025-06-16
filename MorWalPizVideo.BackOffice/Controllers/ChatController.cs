using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPizVideo.BackOffice.DTOs;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Controllers
{
    public class ChatController : ApplicationController
    {
        private readonly Kernel _kernel;

        public ChatController(Kernel kernel)
        {
            _kernel = kernel;
        }
        [HttpPost]
        public async Task<IActionResult> GetReviewDetails([FromBody] ReviewRequest reviewRequest)
        {
            var allResults = await ProcessFileNamesRecursively(reviewRequest.Names,
                reviewRequest.Context == null ? string.Empty : $"Il contesto più specifico è: {reviewRequest.Context.Trim()}.",
                string.Join(", ", reviewRequest.Languages)
                );

            return Ok(new Review
            {
                Videos = allResults.SelectMany(r => r.Videos).ToList()
            });
        }

        private async Task<List<Review>> ProcessFileNamesRecursively(IList<string> fileNames, string context, string languages)
        {
            const int chunkSize = 5;
            var results = new List<Review>();

            if (!fileNames.Any())
                return results;

            // Take the first chunk of files (up to 3)
            var currentChunk = fileNames.Take(chunkSize).ToList();
            var remainingFiles = fileNames.Skip(chunkSize).ToList();

            // Process current chunk
            var chunkResult = await ProcessFileNamesChunk(currentChunk, context, languages);
            results.Add(chunkResult);

            // Recursively process remaining files
            if (remainingFiles.Any())
            {
                var remainingResults = await ProcessFileNamesRecursively(remainingFiles, context, languages);
                results.AddRange(remainingResults);
            }

            return results;
        }

        private async Task<Review> ProcessFileNamesChunk(List<string> fileNames, string context, string languages)
        {
            var fileNamesString = $"- {string.Join("\n - ", fileNames)}";

            var prompt = @$"
                    Ruolo e Obiettivo:
                    Sei un Content Strategist per YouTube, specializzato nel settore delle armi da fuoco e del tiro sportivo (in particolare IPSC e IDPA). Il tuo compito è trasformare una serie di parole chiave in metadati ottimizzati (Titolo, Descrizione) per YouTube Shorts, massimizzando la visibilità e l'engagement. Devi agire con la competenza di un tiratore esperto e la furbizia di un esperto di marketing digitale.
                    Contesto del Canale:
                    Il canale si rivolge a un pubblico di appassionati di tiro dinamico, neofiti e tiratori esperti. I video sono brevi, dinamici e devono intrattenere, pur offrendo spunti tecnici e informativi. Il tono di voce è autorevole ma amichevole e passionale, mai arrogante.
                    {context}
                    Dati di Input:
                    Lista Parole Chiave: Ti fornirò una lista di stringhe. Ogni stringa è un insieme di parole chiave tematicamente collegate che descrivono il contenuto di un video.
                    {fileNamesString}
                    Lingue per la Traduzione: Una lista di lingue target.
                    {languages}
                    Processo Dettagliato:
Per ogni set di parole chiave nella lista, esegui i seguenti passaggi:
Step 1: Sintesi del Concetto Base
Analizza le parole chiave e sintetizzale in una singola frase che ne catturi l'essenza. Questa frase deve essere il concetto centrale e deve suonare come la parlerebbe un esperto del settore. Poni maggiore enfasi sulle parole chiave rispetto al contesto, ma usa il contesto per affinare il messaggio.
Step 2: Creazione dei Metadati (Italiano)
Basandoti sul concetto elaborato, crea un titolo e una descrizione per YouTube seguendo queste direttive strategiche.
Direttive per il Titolo:
Chiarezza e Impatto: Il titolo deve identificare immediatamente l'argomento. Usa parole d'impatto o formula una domanda per generare curiosità.
Priorità alle Keyword: Inserisci la parola chiave più importante all'inizio del titolo.
Lunghezza Ottimale: Mantieniti sotto i 70 caratteri per una visualizzazione ottimale su tutti i dispositivi (massimo 100 caratteri).
No Clickbait: Sii coinvolgente e accattivante, ma sempre onesto riguardo al contenuto del video. Esempio: ""Il segreto per..."" invece di ""Non crederai mai a..."".
Direttive per la Descrizione:
Hook Iniziale: La prima frase deve riprendere e amplificare la promessa del titolo, catturando subito l'attenzione.
Corpo della Descrizione:
Spiega in modo colloquiale e coinvolgente cosa succede nel video, mantenendo il tono leggero e da intrattenimento.
Evita frasi ridondanti come ""in questo video vedrete"". Dai per scontato che l'utente stia guardando il video.
Struttura il testo in piccoli paragrafi o punti elenco per facilitare la lettura.
Integrazione SEO:
Includi le parole chiave principali in modo naturale nel testo.
Utilizza le parole chiave secondarie e correlate per creare una sezione di hashtag (#parolachiave #altraroba). Utilizza da 3 a 5 hashtag rilevanti.
Se parole chiave importanti non trovano spazio nel testo, inseriscile in fondo, dopo gli hashtag.
Call to Action (CTA): Includi sempre una domanda o un invito all'azione per stimolare i commenti. Esempio: ""Qual è la tua tecnica preferita? Dimmelo nei commenti!""
Lunghezza: La descrizione completa deve avere una lunghezza compresa tra 250 e 400 caratteri per un equilibrio ottimale tra informazioni e leggibilità.
Step 3: Traduzione e Adattamento Culturale
Traduzione Primaria (Inglese):
Traduci il titolo e la descrizione che hai creato in un inglese fluente e naturale.
Non tradurre letteralmente: adatta il tono e le espressioni per il pubblico di lingua inglese. Ciò che è coinvolgente in italiano potrebbe richiedere una formulazione diversa.
Terminologia Specifica:
Usa il dizionario fornito: Hit factor > Factor, match > competition/match, Failure to engage > Failure to Engage, Topolino > Mickey Mouse, Condizione 1/2/3 > Condition 1/2/3, mano forte/debole > strong/weak hand.
Mantieni invariati i seguenti termini: ""No Shoot"", ""A Zone"", ""Double Alpha"", ""Charlie"", ""Double Charlie"".
Traduzione Secondaria (Altre Lingue):
Utilizzando la versione inglese come riferimento, traduci titolo e descrizione nelle altre lingue specificate in {{languages}}. Mantieni la stessa attenzione all'adattamento del tono e delle espressioni idiomatiche.
Step 4: Formattazione dell'Output
Infine, assembla tutte le informazioni generate seguendo scrupolosamente lo schema JSON fornito. Assicurati che ogni campo sia compilato correttamente.";

            // Dividi il testo in righe
            string[] rows = prompt.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Rimuovi gli spazi all'inizio di ogni riga
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = rows[i].TrimStart();
            }

            // Riassembla le righe in una nuova stringa
            string trimmedPrompt = string.Join(Environment.NewLine, rows);

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(Review)
            };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var result = await _kernel.InvokePromptAsync(trimmedPrompt, new KernelArguments(executionSettings));

            return JsonSerializer.Deserialize<Review>(result.ToString()) ?? new Review();
        }
    }
}
