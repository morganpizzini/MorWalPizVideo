using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class VideosController : ApplicationControllerBase
{
    private readonly DataService dataService;
    private readonly IHttpClientFactory client;
    private readonly IYTService yTService;
    public VideosController(DataService _dataService, IHttpClientFactory _clientFactory,
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

        await dataService.SaveMatch(YouTubeContent.CreateSingleVideo(request.VideoId, request.Category.ToLower()));

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

        var newMatch = YouTubeContent.CreateCollection(
            existingMatch.Id,
            request.Title,
            request.Description,
            request.Url,
            existingMatch.ThumbnailVideoId,
            request.Category
        ).AddVideo(existingMatch.ThumbnailVideoId, existingMatch.Category);

        await dataService.UpdateMatch(newMatch);

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

        var updatedMatch = existingMatch.WithThumbnail(request.NewVideoId);

        await dataService.UpdateMatch(updatedMatch);
        return NoContent();
    }

    [HttpPost("RootCreation")]
    public async Task<IActionResult> RootCreation(RootCreationRequest request)
    {
        var matchCollection = await dataService.GetMatches();

        await dataService.SaveMatch(YouTubeContent.CreateCollection(
            request.VideoId,
            request.Title,
            request.Description,
            request.Url,
            request.VideoId,
            request.Category.ToLower()
        ));
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

        var updatedMatch = existingMatch.AddVideo(request.VideoId, request.Category.ToLower());
        await dataService.UpdateMatch(updatedMatch);

        return NoContent();
    }
}
