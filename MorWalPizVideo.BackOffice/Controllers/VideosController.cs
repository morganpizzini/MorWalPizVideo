using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class VideosController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly ICrossApiService client;
    private readonly IYTService yTService;
    private readonly IExternalDataService externalDataService;
    public VideosController(DataService dataService, ICrossApiService _clientFactory,
        IYTService _yTService, IExternalDataService _externalDataService)
    {
        _dataService = dataService;
        client = _clientFactory;
        yTService = _yTService;
        externalDataService = _externalDataService;
    }

    [HttpGet()]
    public async Task<IActionResult> Fetch()
    {
        var matches = await _dataService.FetchMatches();
        return Ok(matches);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(BaseRequestId request)
    {
        var match = await _dataService.GetMatch(request.Id);
        if(match == null)
        {
            return NotFound();
        }
        return Ok(match);
    }

    [HttpPost("Translate")]
    public async Task TranslateShort(IList<string> videoIds)
    {
        await yTService.TranslateYoutubeVideo(videoIds);
    }
    [HttpPost("ImportVideo")]
    public async Task<IActionResult> Import(VideoImportRequest request)
    {
        var matchCollection = await _dataService.FetchMatches();

        // Fetch categories and convert to CategoryRef objects
        var categories = (await _dataService.FetchCategories(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        await _dataService.SaveMatch(YouTubeContent.CreateSingleVideo(request.VideoId, categories));

        // Populate metadata by calling ExternalDataService.FetchMatches()
        // This will fetch YouTube metadata and update the VideoRef with title, description, publishedAt
        await externalDataService.FetchMatches();

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();
        return NoContent();
    }
    [HttpPost("ConvertIntoRoot")]
    public async Task<IActionResult> ConvertIntoRoot(RootCreationRequest request)
    {
        var existingMatch = await _dataService.FindMatch(request.VideoId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        if (!existingMatch.IsLink)
        {
            return BadRequest("Match is already a root");
        }

        // Preserve existing metadata from the single video's VideoRef
        var existingVideoRef = existingMatch.VideoRefs?.FirstOrDefault();
        
        // Fetch categories and convert to CategoryRef objects
        var categories = (await _dataService.FetchCategories(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        var newMatch = YouTubeContent.CreateCollection(
            existingMatch.Id,
            request.Title,
            request.Description,
            request.Url,
            existingMatch.ThumbnailVideoId,
            categories
        );

        // Add video with preserved metadata if available
        if (existingVideoRef != null)
        {
            newMatch = newMatch.AddVideo(
                existingVideoRef.YoutubeId,
                existingVideoRef.Categories,
                existingVideoRef.Title,
                existingVideoRef.Description,
                existingVideoRef.PublishedAt
            );
        }
        else
        {
            // Fallback to basic method if no metadata available
            newMatch = newMatch.AddVideo(existingMatch.ThumbnailVideoId, existingMatch.Categories);
        }

        await _dataService.UpdateMatch(newMatch);

        // Populate any missing metadata by calling ExternalDataService.FetchMatches()
        await externalDataService.FetchMatches();

        return NoContent();
    }

    [HttpPost("SwapThumbnailId")]
    public async Task<IActionResult> SwapThumbnailUrl(SwapRootThumbnailRequest request)
    {
        var existingMatch = await _dataService.FindMatch(request.CurrentVideoId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        if (existingMatch.IsLink)
        {
            return BadRequest("Match is not a root match");
        }

        var updatedMatch = existingMatch.WithThumbnail(request.NewVideoId);

        await _dataService.UpdateMatch(updatedMatch);
        return NoContent();
    }

    [HttpPost("RootCreation")]
    public async Task<IActionResult> RootCreation(RootCreationRequest request)
    {
        var matchCollection = await _dataService.FetchMatches();

        // Fetch categories and convert to CategoryRef objects
        var categories = (await _dataService.FetchCategories(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        await _dataService.SaveMatch(YouTubeContent.CreateCollection(
            request.VideoId,
            request.Title,
            request.Description,
            request.Url,
            request.VideoId,
            categories
        ));
        return NoContent();
    }
    [HttpPost("ImportSubCreation")]
    public async Task<IActionResult> SubVideoCreation(SubVideoCrationRequest request)
    {
        var existingMatch = await _dataService.FindMatch(request.MatchId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }

        // Fetch category and convert to CategoryRef object
        CategoryRef[] categories;
        if (string.IsNullOrEmpty(request.Category))
        {
            categories = Array.Empty<CategoryRef>();
        }
        else
        {
            var categoryEntities = await _dataService.FetchCategories(new[] { request.Category });
            categories = categoryEntities
                .Select(x => new CategoryRef(x.Id, x.Title))
                .ToArray();
        }

        var updatedMatch = existingMatch.AddVideo(request.VideoId, categories);
        await _dataService.UpdateMatch(updatedMatch);

        // Populate metadata by calling ExternalDataService.FetchMatches()
        // This will fetch YouTube metadata and update the VideoRef with title, description, publishedAt
        await externalDataService.FetchMatches();

        return NoContent();
    }
}
