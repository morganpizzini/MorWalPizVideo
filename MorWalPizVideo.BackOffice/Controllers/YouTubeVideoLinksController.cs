using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Server.Models;
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

            var channel = string.IsNullOrWhiteSpace(match.OwnerChannelId)
                ? null
                : await _dataService.GetChannelById(match.OwnerChannelId);
            var response = new List<YouTubeVideoLinkResponse>();
            foreach (var link in match.YouTubeVideoLinks ?? Array.Empty<YouTubeVideoLink>())
            {
                response.Add(new YouTubeVideoLinkResponse
                {
                    ContentCreatorName = link.ContentCreatorName,
                    YouTubeVideoId = link.YouTubeVideoId,
                    ImageName = link.ImageName,
                    ShortLinkUrl = ResolveShortLinkUrl(link, channel),
                    ShortLinkCode = link.ShortLink?.Code,
                    ShortLinkTarget = link.ShortLink?.Target,
                    DirectVideoUrl = link.DirectVideoUrl
                });
            }

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

    private static string? ResolveShortLinkUrl(YouTubeVideoLink link, YTChannel? channel)
    {
        var code = link.ShortLink?.Code;
        if (!string.IsNullOrWhiteSpace(channel?.ShortLinkUrl) && !string.IsNullOrWhiteSpace(code))
        {
            return $"{channel.ShortLinkUrl.TrimEnd('/')}/{code.TrimStart('/')}";
        }

        return string.IsNullOrWhiteSpace(link.ShortLinkUrl) ? null : link.ShortLinkUrl;
    }
}
