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
            reviewRequest.Context == null ? string.Empty : $"Il contesto più specifico sarà: {reviewRequest.Context.Trim()}.",
            string.Join(", ", reviewRequest.Languages)
            );
        
        var aggregatedReview = new Review
        {
            Videos = allResults.SelectMany(r => r.Videos).ToList()
        };

        return Ok(aggregatedReview);
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

    private async Task<List<Review>> ProcessFileNamesRecursively(IList<string> fileNames, string context, string languages)
    {
        const int chunkSize = 3;
        var results = new List<Review>();

        if (!fileNames.Any())
            return results;

        // Take the first chunk of files (up to 3)
        var currentChunk = fileNames.Take(chunkSize).ToList();
        var remainingFiles = fileNames.Skip(chunkSize).ToList();

        // Process current chunk
        var chunkResult = await ProcessFileNamesChunk(currentChunk, context,languages);
        results.Add(chunkResult);

        // Recursively process remaining files
        if (remainingFiles.Any())
        {
            var remainingResults = await ProcessFileNamesRecursively(remainingFiles, context,languages);
            results.AddRange(remainingResults);
        }

        return results;
    }

    private async Task<Review> ProcessFileNamesChunk(List<string> fileNames, string context,string languages)
    {
        var fileNamesString = string.Join("\n -", fileNames);

        var prompt = @$"Sei un esperto di armi, del tiro dinamico, IPSC e IDPA.
                    {context}
                    Ti fornirò una lista, ogni elemento è una serie di parole chiavi. Il tuo compito è quello di
                    elaborare una frase di senso compiuto esaustiva del concetto espresso da quelle parole chiavi.
                    Dai più priorità alle parole chiave rispetto al contesto, ma non ignorarlo.
                    Ecco l'elenco:
                    {fileNamesString}
                    Un volta ottenuto il risultato, e conoscendo le meccaniche di engagement di Youtube,
                    il funzionamento del suo algoritmo e le regole SEO,elabora un titolo e una descrizione per ogni elemento seguendo anche le istruzioni seguenti. 
                Il titolo deve:
                – Riflettere chiaramente l'argomento del video
                – Non superare i 100 caratteri.
                – Includere parole chiave rilevanti per il pubblico di riferimento
                – Incoraggiare il clic (essere coinvolgente ma non clickbait)
                La descrizione deve:
                – Spiegare chiaramente di cosa parla il video, la tipologia del video è di intrattenimento ed anche la descrizione deve tenere toni leggeri
                - non utilizzare costrutti simili a 'questo video', 'questo video', 'nel video', che saranno dati come sottintesi
                – Includere parole chiave rilevanti per il pubblico e l'algoritmo di ricerca, evidenziarle utilizzando hashtag
                – Includere se possibile un breve riassunto dei punti principali trattati
                - Se alcune parole chiave non vengono incluse nel contesto della descrizione, aggiungile alla fine del testo senza scrivere altro.
                – Avere una lunghezza compresa tra 200 e 300 caratteri.
                Traduci anche in Inglese.Non tradurre: No Shoot, A Zone, Double Alpha, Charlie, Double Charlie.
                Dizionario per termini specifici: Hit factor > Fattore, match > gara, Failure to engage > Mancato ingaggio, Topolino > Mickey mouse, Consizione 1/2/3 > Condition 1/2/3,mano forte/debole > strong/weak hand
                Utilizzando la traduzione inglese, crea le traduzioni anche per queste lingue: {languages}.
                Elabora le informazioni e dammi un risultato seguento il JSON schema fornito.";

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings = new AzureOpenAIPromptExecutionSettings()
        {
            ResponseFormat = typeof(Review)
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

        var review = JsonSerializer.Deserialize<Review>(result.ToString());
        return review ?? new Review();
    }

    private async Task<List<VideoTranslationResponse>> ProcessVideoTranslation(string title, string description, List<string> languages)
    {
        var languagesString = string.Join(", ", languages);

        // Placeholder prompt - user mentioned they will edit this later
        var prompt = @$"You are a professional translator specializing in YouTube content translation.
                    
                    Please translate the following YouTube video content:
                    
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
