using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Controllers;
public class VideoImportRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
}

public class SwapRootThumbnailRequest
{
    [Required]
    public string CurrentVideoId { get; set; } = string.Empty;
    [Required]
    public string NewVideoId { get; set; } = string.Empty;
}

public class RootCreationRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string Url { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
}
public class SubVideoCrationRequest
{
    [Required]
    public string MatchId { get; set; } = string.Empty;
    [Required]
    public string VideoId { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
}


public class Review
{
    [Required]
    [Description("Lista delle degli elementi elaborati sulla base dei nomi dei file forniti")]
    public IList<ReviewDetails> TitleItalian { get; set; } = new List<ReviewDetails>();
}

public class ReviewDetails
{
    [Required]
    [Description("Il nome del file originale")]
    public string Name { get; set; } = string.Empty;
    [Required]
    [Description("Titolo del video in italiano")]
    public string TitleItalian { get; set; } = string.Empty;
    [Required]
    [Description("Descrizione del video in italiano")]
    public string DescriptionItalian { get; set; } = string.Empty;

    [Required]
    [Description("Descrizione del video in inglese")]
    public string TitleEnglish { get; set; } = string.Empty;
    [Required]
    [Description("Descrizione del video in inglese")]
    public string DescriptionEnglish { get; set; } = string.Empty;
}


public class ReviewRequest
{
    [Required]
    public IList<string> Names { get; set; } = new List<string>();

    public string Context { get; set; } = string.Empty;
}


public class ChatController : ApplicationController
{
    private Kernel _kernel;

    public ChatController(Kernel kernel)
    {
        _kernel = kernel;
    }

    [HttpPost]
    public async Task<IActionResult> GetReviewDetails([FromBody] ReviewRequest reviewRequest)
    {
        var fileNames = string.Join("\n", reviewRequest.Names).OrderBy(x=>x).ToList();

        if(reviewRequest.Context == null)
        {
            reviewRequest.Context = string.Empty;
        }
        else
        {
            reviewRequest.Context = "Nel dettaglio: " + reviewRequest.Context.Trim();
        }

        var prompt = @$"Sei un esperto di armi, del tiro dinamico, IPSC e IDPA.
                        {reviewRequest.Context}
                        Ti fornirò una lista, ogni elemento è una serie di parole chiavi. Il tuo compito è quello di
                        elaborare una frase di senso compiuto esaustiva del concetto espresso da quelle parole chiavi.
                        Ecco l'elenco dei nomi:
                        {fileNames}
                        Un volta ottenuto il risultato, e conoscendo le meccaniche di engagement di Youtube,
                        il funzionamento del suo algoritmo e le regole SEO,elabora un titolo e una descrizione per ogni elemento seguendo anche le istruzioni seguenti. 
                    Il titolo deve:
                    – Riflettere chiaramente l'argomento del video
                    – Non superare i 100 caratteri.
                    – Includere parole chiave rilevanti per il pubblico di riferimento
                    – Incoraggiare il clic (essere coinvolgente ma non clickbait)
                    La descrizione deve:
                    – Spiegare chiaramente di cosa parla il video
                    – Includere parole chiave rilevanti per il pubblico e l'algoritmo di ricerca, evidenziarle utilizzando hashtag
                    – Includere se possibile un breve riassunto dei punti principali trattati
                    - Se alcune parole chiave non sono incluse nella descrizione, aggiungerle a fine della descrizione
                    – Avere una lunghezza compresa tra 200 e 800 caratteri.
                    Forniscimi anche la traduzione in inglese.
                    Come regola generale per le traduzioni, non tradurre: No Shoot, A Zone, Double Alpha, Charlie, Double Charlie.
                    Questo è il dizionario di traduzione per alcuni termini specifici: Hit factor > Fattore, match > gara, Failure to engage > Mancato ingaggio.
                    Elabora le informazioni e dammi un risultato seguento il JSON schema fornito.";

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings1 = new AzureOpenAIPromptExecutionSettings()
        {
            ResponseFormat = typeof(Review),
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings1));

        var resultString = result.ToString();
        Console.WriteLine(resultString);

        var review1 = JsonSerializer.Deserialize<Review>(resultString);
        return Ok(review1);
    }
}

public class VideoController : ApplicationController
{
    private readonly DataService dataService;
    private readonly IHttpClientFactory client;
    private readonly IYTService yTService;
    public VideoController(DataService _dataService, IHttpClientFactory _clientFactory,
        IYTService _yTService)
    {
        dataService = _dataService;
        client = _clientFactory;
        yTService = _yTService;
    }

    [HttpGet()]
    public async Task<IActionResult> GetAllVideos()
    {
        var matches = await dataService.GetMatches();
        return Ok(matches);
    }

    [HttpPost("Translate")]
    public async Task TranslateShort(IList<string> videoIds)
    {
        await yTService.TranslateYoutubeVideo(videoIds);
    }
    [HttpPost("ImportVideo")]
    public async Task<IActionResult> Import(VideoImportRequest request)
    {
        var matchCollection = await dataService.GetMatches();

        await dataService.SaveMatch(new Match(request.VideoId, true, request.Category.ToLower()));

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);

        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.Matches}");
        json = await client.GetStringAsync($"cache/purge/{ApiTagCacheKeys.Matches}");
        json = await client.GetStringAsync("matches");
        return NoContent();
    }
    [HttpPost("ConvertIntoRoot")]
    public async Task<IActionResult> ConvertIntoRoot(RootCreationRequest request)
    {
        var existingMatch = await dataService.FindMatch(request.VideoId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        if (!existingMatch.IsLink)
        {
            return BadRequest("Match is already a root");
        }
        existingMatch = existingMatch with { Title = request.Title, Description = request.Description, Url = request.Url, Videos = new[] { new Server.Models.Video(existingMatch.ThumbnailUrl, existingMatch.Category) }, Category = request.Category, IsLink = false };

        await dataService.UpdateMatch(existingMatch);

        return NoContent();
    }

    [HttpPost("SwapThumbnailId")]
    public async Task<IActionResult> SwapThumbnailUrl(SwapRootThumbnailRequest request)
    {
        var existingMatch = await dataService.FindMatch(request.CurrentVideoId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        if (existingMatch.IsLink)
        {
            return BadRequest("Match is not a root match");
        }
        existingMatch = existingMatch with { ThumbnailUrl = request.NewVideoId };

        await dataService.UpdateMatch(existingMatch);
        return NoContent();
    }

    [HttpPost("RootCreation")]
    public async Task<IActionResult> RootCreation(RootCreationRequest request)
    {
        var matchCollection = await dataService.GetMatches();
        await dataService.SaveMatch(new Match(request.VideoId, request.Title, request.Description, request.Url, [], request.Category.ToLower()));
        return NoContent();
    }
    [HttpPost("ImportSubCreation")]
    public async Task<IActionResult> SubVideoCreation(SubVideoCrationRequest request)
    {
        var existingMatch = await dataService.FindMatch(request.MatchId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        existingMatch = existingMatch with { Videos = [.. existingMatch.Videos, new Server.Models.Video(request.VideoId, request.Category.ToLower())] };

        await dataService.UpdateMatch(existingMatch);

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);

        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.Matches}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.Matches}");

        json = await client.GetStringAsync("matches");
        return NoContent();
    }
}
