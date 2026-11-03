using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/v1/video-importer-settings")]
[ApiKeyAuth]
[RequireChannelScope]
public sealed class VideoImporterSettingsController(
    VideoImporterChannelSettingsService settingsService,
    ILogger<VideoImporterSettingsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VideoImporterChannelSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        if (!HasScope(AuthorizationPermissionKeys.VideoImporterSettingsRead))
            return Forbid();

        var channelId = HttpContext.GetChannelContext().ChannelId;
        try
        {
            var result = await settingsService.GetAsync(channelId, cancellationToken);
            return result is null ? NoContent() : Ok(result);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Stored video importer settings are invalid for channel {ChannelId}", channelId);
            return ProblemResponse(
                StatusCodes.Status500InternalServerError,
                "settings_document_invalid",
                "The stored channel settings document is invalid.");
        }
    }

    [HttpPut]
    [RequestSizeLimit(VideoImporterChannelSettingsService.MaxPayloadBytes + 1024)]
    public async Task<ActionResult<VideoImporterChannelSettingsResponse>> Put(
        [FromBody] VideoImporterChannelSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasScope(AuthorizationPermissionKeys.VideoImporterSettingsWrite))
            return Forbid();

        var channelId = HttpContext.GetChannelContext().ChannelId;
        try
        {
            var result = await settingsService.SaveAsync(channelId, request, cancellationToken);
            return Ok(result);
        }
        catch (VideoImporterChannelSettingsValidationException exception)
        {
            return ProblemResponse(exception.StatusCode, exception.Code, exception.Message);
        }
    }

    private bool HasScope(string requiredScope) =>
        User.Claims.Any(claim =>
            claim.Type == "scope" &&
            string.Equals(claim.Value, requiredScope, StringComparison.OrdinalIgnoreCase));

    private ObjectResult ProblemResponse(int statusCode, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status413PayloadTooLarge
                ? "The settings payload is too large."
                : "The channel settings request is invalid.",
            Detail = detail
        };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
