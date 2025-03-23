using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MongoDB.Driver;
using MorWalPizVideo.Domain;
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


public class ReviewDetails
{
    [Required]
    [Description("Italian version, at the end add one or more related icon and apply hashtag strategy related to the title context")]
    public string TitleItalian { get; set; } = string.Empty;

    [Required]
    [Description("English version, at the end add one or more related icon and apply hashtag strategy related to the title context")]
    public string TitleEnglish { get; set; } = string.Empty;
}

public class ChatController : ApplicationController
{
    private Kernel _kernel;

    public ChatController(Kernel kernel)
    {
        _kernel = kernel;
    }

    [HttpPost]
    public async Task<ReviewDetails> GetReviewDetails([FromBody] string reviewText)
    {
        //This is an example of workload:
        //    `prova armi ipsc test sicurezza fattore`
        //    when asked for Italian, you should answer: 'Test Armi IPSC: Sicurezza e Fattore da Non Sottovalutare! 🔥🔫 #IPSC #Sicurezza #ArmiSportive'
        //    when asked for English, you should answer: 'IPSC Gun Test: Safety & Power Factor Matter! ⚡🔫 #IPSC #GunTest #SafetyFirst'
        //    This is another example            
        //    `non tutto e oro mirare importante`
        //    when asked for Italian, you should answer: 'Non Tutto è Oro! 🎯 Mirare Bene è Più Importante! #TiroDinamico #Precisione #IPSC'
        //    when asked for English, you should answer: 'Not Everything That Glitters is Gold! 🎯 Accuracy is Key! #ShootingSports #IPSC #Accuracy'
        string prompt = string.Format(
            @"You are an expert Youtube shorts title creator about guns IPSC Dynamic shooting world.
                You will be prompted with a series of keywords in Italian.
                You have to elaborate them following the provided JSON schema.
                As general rule do not translate 'No shoot, A zone, Double Alpha, Charlie, Double Charlie'.
                this is the dictionary: Hit factor > Fattore, match > gara, Failure to engage > Mancato ingaggio
            This are the words to working with:
            `{0}`", reviewText);

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var executionSettings = new AzureOpenAIPromptExecutionSettings()
        {
            ResponseFormat = typeof(ReviewDetails),
        };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


        var result = await _kernel.InvokePromptAsync(reviewText, new KernelArguments(executionSettings));

        Console.WriteLine(result.ToString());

        var review = JsonSerializer.Deserialize<ReviewDetails>(result.ToString());

        return review;

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
        existingMatch = existingMatch with { Title = request.Title, Description = request.Description, Url = request.Url, Videos = new[] { new Video(existingMatch.ThumbnailUrl, existingMatch.Category) }, Category = request.Category, IsLink = false };

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
        existingMatch = existingMatch with { Videos = [.. existingMatch.Videos, new Video(request.VideoId, request.Category.ToLower())] };

        await dataService.UpdateMatch(existingMatch);

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);

        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.Matches}");
        json = await client.GetStringAsync($"cache/purge?k={ApiTagCacheKeys.Matches}");

        json = await client.GetStringAsync("matches");
        return NoContent();
    }
}
