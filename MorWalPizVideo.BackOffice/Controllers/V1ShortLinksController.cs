using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/v1/short-links")]
[RequireChannelScope]
[Produces("application/json")]
public sealed class V1ShortLinksController(
    ILinksService linksService,
    IContentService contentService,
    IVideoAuthorizationService authorization) : ApplicationControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<V1ShortLinkResponse>), StatusCodes.Status200OK)]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksView, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<ActionResult<IReadOnlyList<V1ShortLinkResponse>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var links = await linksService.GetShortLinksAsync();
        return Ok(links
            .Where(link => link.ManagementChannelId == channelId || link.ChannelId == channelId)
            .OrderByDescending(link => link.CreationDateTime)
            .Select(ToResponse)
            .ToArray());
    }

    [HttpGet("{code}")]
    [ProducesResponseType(typeof(V1ShortLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksView, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<ActionResult<V1ShortLinkResponse>> Get(string code)
    {
        var link = await linksService.GetByCodeAsync(ShortLink.NormalizeCode(code));
        var channelId = HttpContext.GetChannelContext().ChannelId;
        if (link is null || (link.ManagementChannelId != channelId && link.ChannelId != channelId))
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Short link not found",
                type: "https://tools.ietf.org/html/rfc9457");

        return Ok(ToResponse(link));
    }

    [HttpPost]
    [ProducesResponseType(typeof(V1ShortLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksCreate, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<ActionResult<V1ShortLinkResponse>> Create(V1CreateShortLinkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Target) ||
            !Enum.TryParse<LinkType>(request.LinkType, true, out var linkType))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid short-link request",
                Detail = "target and a supported linkType are required",
                Type = "https://httpstatuses.com/400"
            });

        var channelId = HttpContext.GetChannelContext().ChannelId;
        if (linkType == LinkType.YouTubeVideo)
        {
            var match = await contentService.FindMatchAsync(request.Target.Trim());
            if (match is null || !await authorization.CanMutateInChannelAsync(User, match, channelId))
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Video is not available in the selected channel",
                    Detail = "The requested video does not belong to the selected channel.",
                    Type = "https://httpstatuses.com/400"
                });

            var ensured = await linksService.EnsureVideoShortLinkAsync(match.Id, request.Target.Trim(), channelId);
            return ensured is null
                ? BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Video short link could not be created",
                    Type = "https://httpstatuses.com/400"
                })
                : Ok(ToResponse(ensured));
        }

        if (linkType != LinkType.YouTubeChannel && !await authorization.IsAdminAsync(User))
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Short-link management permission required",
                type: "https://tools.ietf.org/html/rfc9457");

        var queryLinks = await linksService.GetQueryLinksAsync(request.QueryLinkIds ?? []);
        var link = await linksService.SaveShortLinkAsync(new ShortLink(CreateCode(request.Target), request.Target.Trim(), queryLinks)
        {
            LinkType = linkType,
            ManagementChannelId = channelId
        });
        return Ok(ToResponse(link));
    }

    private static V1ShortLinkResponse ToResponse(ShortLink link) => new(
        link.NormalizedCode,
        link.Target,
        link.LinkType.ToString(),
        link.ContentId,
        link.ChannelId,
        link.ManagementChannelId,
        link.ClicksCount,
        link.CreationDateTime);

    private static string CreateCode(string target)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(target.Trim()));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}