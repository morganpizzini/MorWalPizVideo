using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class YouTubeVideoLinksController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly IImageGenerationService _imageGenerationService;

    public YouTubeVideoLinksController(DataService dataService, IImageGenerationService imageGenerationService)
    {
        _dataService = dataService;
        _imageGenerationService = imageGenerationService;
    }

    [HttpGet("{matchId}/links")]
    public async Task<IActionResult> GetYouTubeVideoLinks([FromRoute]string matchId)
    {
        try
        {
            var match = await _dataService.FindMatch(matchId);
            if (match == null)
            {
                return NotFound($"Match with ID {matchId} not found");
            }

            var response = match.YouTubeVideoLinks?.Select(link => new YouTubeVideoLinkResponse
            {
                ContentCreatorName = link.ContentCreatorName,
                YouTubeVideoId = link.YouTubeVideoId,
                ImageName = link.ImageName,
                ShortLinkUrl = ResolveShortLinkUrl(link.ShortLinkUrl),
                ShortLinkCode = link.ShortLink?.Code,
                ShortLinkTarget = link.ShortLink?.Target,
                DirectVideoUrl = link.DirectVideoUrl
            }).ToList() ?? new List<YouTubeVideoLinkResponse>();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving YouTube video links: {ex.Message}");
        }
    }

    [HttpGet("image/{imageName}")]
    public async Task<IActionResult> GetCreatorImage(string imageName)
    {
        try
        {
            var imageStream = await _imageGenerationService.GetExistingImageAsync(imageName);
            if (imageStream == null)
            {
                return NotFound($"Image {imageName} not found");
            }

            return File(imageStream, "image/png", imageName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving image: {ex.Message}");
        }
    }

    private static string? ResolveShortLinkUrl(string? shortLinkUrl)
    {
        if (string.IsNullOrWhiteSpace(shortLinkUrl))
        {
            return null;
        }

        return shortLinkUrl;
    }
}
