using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.Models.Responses;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Controllers
{
    [ApiKeyAuth]
    public class ChatController : ApplicationControllerBase
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
                reviewRequest.Context,
                string.Join(", ", reviewRequest.Languages)
                );

            return Ok(allResults);
        }

        [HttpPost("translate")]
        public async Task<IActionResult> TranslateVideoContent([FromBody] VideoTranslationRequest translationRequest)
        {
            var translations = await ProcessVideoTranslation(
                translationRequest.Title,
                translationRequest.Description,
                translationRequest.Languages
            );

            return Ok(translations);
        }

        private async Task<IList<ReviewApiVideoResponse>> ProcessFileNamesRecursively(IList<string> fileNames, string context, string languages)
        {
            const int chunkSize = 10;
            List<ReviewApiVideoResponse> results = [];

            if (!fileNames.Any())
                return results;

            var currentChunk = fileNames.Take(chunkSize).ToList();
            var remainingFiles = fileNames.Skip(chunkSize).ToList();

            // Process current chunk
            var chunkResult = await ProcessFileNamesChunk(currentChunk, context, languages);
            results.AddRange(chunkResult);

            // Recursively process remaining files
            if (remainingFiles.Any())
            {
                var remainingResults = await ProcessFileNamesRecursively(remainingFiles, context, languages);
                results.AddRange(remainingResults);
            }

            return results;
        }

        private async Task<IList<ReviewApiVideoResponse>> ProcessFileNamesChunk(List<string> fileNames, string context, string languages)
        {
            var fileNamesString = $"- {string.Join("\n - ", fileNames)}";

            var requestContext = string.IsNullOrEmpty(context) ? string.Empty : $"Il contesto più specifico è: {context.Trim()}.";

            var prompt = @$"
                    Ruolo e Obiettivo:
                    Sei un Content Strategist per YouTube, specializzato nel settore delle armi da fuoco e del tiro sportivo dinamico in particolare IPSC e IDPA. Il tuo compito è trasformare una serie di parole chiave in metadati ottimizzati (Titolo, Descrizione, tags) per YouTube Shorts, massimizzando la visibilità e l'engagement. Devi agire con la competenza di un tiratore esperto e la furbizia di un esperto di marketing digitale.
                    Contesto del Canale:
                    Il canale si rivolge a un pubblico di appassionati di tiro dinamico, neofiti e tiratori esperti. I video sono brevi, dinamici e devono intrattenere, pur offrendo spunti tecnici e informativi. Il tono di voce è autorevole ma amichevole e passionale, mai arrogante.
                    {requestContext}
                    Dati di Input:
                    Lista Parole Chiave: Ti fornirò una lista di stringhe. Ogni stringa è un insieme di parole chiave tematicamente collegata o la descrizione che descrive il contenuto di un video.
                    {fileNamesString}
                    Lingue per la Traduzione: Una lista di lingue target.
                    {languages}
                    Processo Dettagliato:
Per ogni set di parole chiave nella lista, esegui i seguenti passaggi:
Step 1: Sintesi del Concetto Base
Analizza le parole chiave e sintetizzale in una singola frase che ne catturi l'essenza. Questa frase deve essere il concetto centrale e deve suonare come la parlerebbe un esperto del settore. Poni maggiore enfasi sulle parole chiave rispetto al contesto, ma usa il contesto per affinare il messaggio.
Crea anche una serie di tag separati da virgola specifici e pertinenti (massimo 5) che riflettano accuratamente il contenuto e l'argomento del video.
Step 2: Creazione dei Metadati (Italiano)
Basandoti sul concetto elaborato, crea un titolo, una descrizione e dei tag specifici per YouTube seguendo queste direttive strategiche.
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
Step 3: Formattazione dell'Output
Infine, assembla tutte le informazioni generate seguendo scrupolosamente lo schema JSON fornito. Assicurati che ogni campo sia compilato correttamente.";

            // Dividi il testo in righe
            var trimmedPrompt = PrettifyString(prompt);

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(Review)
            };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var result = await _kernel.InvokePromptAsync(trimmedPrompt, new KernelArguments(executionSettings));

            var parsedResults = JsonSerializer.Deserialize<Review>(result.ToString()) ?? new Review();
            var customObjectForTranslation = JsonSerializer.Serialize(parsedResults.Videos.Select(x=> new
            {
                Title = x.Title,
                Description = x.Description,
                Tags = x.Tags
            }));

            var prompt1 = @$"
                    Ruolo e Obiettivo:
                    Sei un Content Strategist per YouTube, specializzato nel settore delle armi da fuoco e del tiro sportivo dinamico in particolare IPSC e IDPA. Il tuo compito è dato in input una lista di oggetti json rappresentante una lista di metadati di video tradurli nelle seguenti lingue {languages}
                    Dati di Input:
                    Lista di oggetti che rappresentano titolo,descrizione e tags in formato json. ogni riga è la rappresentazione di un singolo video, il pattern è il seguente {{ title: "", description: "", tags: "" }}. La lista è la seguente
                    {customObjectForTranslation}
                    Processo Dettagliato:
Step 1: Traduzione e Adattamento Culturale
Traduzione Primaria (Inglese):
Traduci il titolo, descrizione e tag in un inglese fluente e naturale.
Non tradurre letteralmente: adatta il tono e le espressioni per il pubblico di lingua inglese. Ciò che è coinvolgente in italiano potrebbe richiedere una formulazione diversa.
Terminologia Specifica:
Usa il dizionario fornito: Hit factor > Factor, match > competition/match, Failure to engage > Failure to Engage, Topolino > Mickey Mouse, Condizione 1/2/3 > Condition 1/2/3, mano forte/debole > strong/weak hand.
Mantieni invariati i seguenti termini: ""No Shoot"", ""A Zone"", ""Double Alpha"", ""Charlie"", ""Double Charlie"".
Traduzione Secondaria (Altre Lingue):
Utilizzando la versione inglese come riferimento, traduci titolo, descrizione ma non tags nelle altre lingue specificate in {{languages}}. Mantieni la stessa attenzione all'adattamento del tono e delle espressioni idiomatiche.
Step 4: Formattazione dell'Output
Infine, assembla tutte le informazioni generate seguendo scrupolosamente lo schema JSON fornito. Assicurati che ogni campo sia compilato correttamente.";


            trimmedPrompt = PrettifyString(prompt1);


#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(TranslatedReview)
            };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            result = await _kernel.InvokePromptAsync(trimmedPrompt, new KernelArguments(executionSettings));

            var finalResult = JsonSerializer.Deserialize<TranslatedReview>(result.ToString()) ?? new TranslatedReview();

            IList<ReviewApiVideoResponse> computed = [];
            foreach (var translation in finalResult.Videos) {
                var existing = parsedResults.Videos.FirstOrDefault(x => x.Title == translation.Title);
                if (existing == null)
                    continue;

                var tags = translation.Translations.FirstOrDefault(x=>!string.IsNullOrEmpty(x.Tags))?.Tags ?? string.Empty;
                translation.Translations.Add(new ReviewTranslationDetail
                {
                    Title = existing.Title,
                    Description = existing.Description,
                    Language = "IT-it"
                });
                computed.Add(new ReviewApiVideoResponse
                {
                    Name = existing.Name,
                    ProcessElement = existing.ProcessElement,
                    Tags = tags,
                    Translations = translation.Translations.Select(x => new ReviewApiVideoTranslation
                    {
                        Description = x.Description,
                        Language = x.Language,
                        Title = x.Title
                    }).ToList()
                });
            }
            
            //merge result from two request

            return computed;
        }
        private string PrettifyString(string s)
        {
            // Dividi il testo in righe
            string[] rows = s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Rimuovi gli spazi all'inizio di ogni riga
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = rows[i].TrimStart();
            }

            // Riassembla le righe in una nuova stringa
            return string.Join(Environment.NewLine, rows);
        }
        private async Task<List<VideoTranslationResponse>> ProcessVideoTranslation(string title, string description, List<string> languages)
        {
            var languagesString = string.Join(", ", languages);

            // Placeholder prompt - user mentioned they will edit this later
            var prompt = @$"You are a professional translator specializing in YouTube content translation.
                    
                    Translate the following YouTube video content:
                    
                    Title: {title}
                    Description: {description}
                    
                    Translate both the title and description into each of these languages: {languagesString}
                    
                    For each language, provide:
                    - The language code (e.g., 'en' for English, 'es' for Spanish, 'fr' for French)
                    - The translated title
                    - The translated description
                    
                    Ensure translations are natural, culturally appropriate, and optimized for YouTube SEO in each target language.
                    Maintain the original tone and style while adapting to each language's conventions.
                    
                    Return the result as a JSON array following the specified schema.";

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var executionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                ResponseFormat = typeof(List<VideoTranslationResponse>)
            };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

            var translations = JsonSerializer.Deserialize<List<VideoTranslationResponse>>(result.ToString());
            return translations ?? new List<VideoTranslationResponse>();
        }

    }
}
