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

    [HttpPost("create")]
    public async Task<IActionResult> CreateYouTubeVideoLink([FromBody] CreateYouTubeVideoLinkRequest request)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.MatchId) ||
                string.IsNullOrWhiteSpace(request.ContentCreatorName) ||
                string.IsNullOrWhiteSpace(request.YouTubeVideoId))
            {
                return BadRequest("MatchId, ContentCreatorName, and YouTubeVideoId are required");
            }

            // Find the match
            var match = await _dataService.FindMatch(request.MatchId);
            if (match == null)
            {
                return NotFound($"Match with ID {request.MatchId} not found");
            }

            // Check if video link already exists for this match
            var existingLink = match.YouTubeVideoLinks?.FirstOrDefault(x => x.YouTubeVideoId == request.YouTubeVideoId);
            if (existingLink != null)
            {
                return BadRequest($"YouTube video link for video {request.YouTubeVideoId} already exists in this match");
            }

            // Generate the creator image
            var imageName = await _imageGenerationService.GenerateCreatorImageAsync(
                request.ContentCreatorName,
                request.FontStyle,
                request.FontSize,
                request.TextColor,
                request.OutlineColor,
                request.OutlineThickness);

            // Generate a short link code (simple implementation)
            var shortLinkCode = GenerateShortLinkCode();
            var shortLink = new ShortLink(
                shortLinkCode,
                request.YouTubeVideoId,
                string.Empty)
            {
                LinkType = LinkType.YouTubeVideo
            };

            // Create the YouTube video link
            var youtubeVideoLink = new YouTubeVideoLink(
                request.ContentCreatorName,
                request.YouTubeVideoId,
                imageName,
                shortLink);

            // Add the link to the match
            var updatedMatch = match.AddYouTubeVideoLink(youtubeVideoLink);
            await _dataService.UpdateMatch(updatedMatch);

            // Return the response
            var response = new YouTubeVideoLinkResponse
            {
                ContentCreatorName = youtubeVideoLink.ContentCreatorName,
                YouTubeVideoId = youtubeVideoLink.YouTubeVideoId,
                ImageName = youtubeVideoLink.ImageName,
                ShortLinkCode = youtubeVideoLink.ShortLink?.Code,
                ShortLinkTarget = youtubeVideoLink.ShortLink?.Target
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating YouTube video link: {ex.Message}");
        }
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
                ShortLinkCode = link.ShortLink?.Code,
                ShortLinkTarget = link.ShortLink?.Target
            }).ToList() ?? new List<YouTubeVideoLinkResponse>();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving YouTube video links: {ex.Message}");
        }
    }

    [HttpDelete("{matchId}/links/{youtubeVideoId}")]
    public async Task<IActionResult> RemoveYouTubeVideoLink(string matchId, string youtubeVideoId)
    {
        try
        {
            var match = await _dataService.FindMatch(matchId);
            if (match == null)
            {
                return NotFound($"Match with ID {matchId} not found");
            }

            var existingLink = match.YouTubeVideoLinks.FirstOrDefault(x => x.YouTubeVideoId == youtubeVideoId);
            if (existingLink == null)
            {
                return NotFound($"YouTube video link for video {youtubeVideoId} not found in this match");
            }

            var updatedMatch = match.RemoveYouTubeVideoLink(youtubeVideoId);
            await _dataService.UpdateMatch(updatedMatch);

            return Ok($"YouTube video link for video {youtubeVideoId} removed successfully");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error removing YouTube video link: {ex.Message}");
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

    private string GenerateShortLinkCode()
    {
        // Generate a random 8-character alphanumeric code
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
