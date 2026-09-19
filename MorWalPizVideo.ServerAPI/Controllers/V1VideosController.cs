using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/videos")]
[Produces("application/json")]
public sealed class V1VideosController(IContentService contentService, ILinksService linksService, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<V1VideoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<V1VideoResponse>>> GetAll()
    {
        var channelId = configuration["YouTubeChannelId"]?.Trim();
        if (string.IsNullOrWhiteSpace(channelId))
            return Ok(Array.Empty<V1VideoResponse>());

        var matches = await contentService.GetPublicMatchesForChannelAsync(channelId, 0, 200);
        var links = await linksService.GetShortLinksAsync();
        return Ok(matches.Select(match => ToResponse(match, links)).ToArray());
    }

    [HttpGet("{url}")]
    [ProducesResponseType(typeof(V1VideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<V1VideoResponse>> Get(string url)
    {
        var match = await contentService.GetMatchByUrlAsync(url, includePrivate: false);
        if (match is null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Video not found",
                type: "https://tools.ietf.org/html/rfc9457");

        var channelId = configuration["YouTubeChannelId"]?.Trim();
        if (string.IsNullOrWhiteSpace(channelId) || !match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)))
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Video not found",
                type: "https://tools.ietf.org/html/rfc9457");

        var links = await linksService.GetShortLinksAsync();
        return Ok(ToResponse(match, links));
    }

    private static V1VideoResponse ToResponse(YouTubeContent match, IList<ShortLink> links) => new(
        match.Id,
        match.Url,
        match.Title,
        match.Description,
        match.ContentType.ToString(),
        match.VideoRefs.Select(video => new V1VideoReferenceResponse(
            video.YoutubeId,
            video.Title,
            video.Description,
            video.ChannelIds.ToArray(),
            links.FirstOrDefault(link => link.LinkType == LinkType.YouTubeVideo &&
                                         link.ContentId == match.Id &&
                                         link.Target == video.YoutubeId)?.NormalizedCode)).ToArray());
}