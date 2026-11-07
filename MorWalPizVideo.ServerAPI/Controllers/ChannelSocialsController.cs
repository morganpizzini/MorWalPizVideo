using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/channel/socials")]
public sealed class ChannelSocialsController(
    IContentService content,
    IConfiguration configuration,
    ILogger<ChannelSocialsController> logger) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 300, Tags = [CacheKeys.Channels])]
    public async Task<IActionResult> Get()
    {
        var configured = configuration["YouTubeChannelId"]?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogError("Public channel socials are missing YouTubeChannelId configuration");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Public channel is not configured.");
        }

        var channel = await content.GetChannelByIdAsync(configured);
        if (channel is null)
        {
            logger.LogError("Public channel socials cannot resolve configured channel {ConfiguredChannelId}", configured);
            return NotFound();
        }

        var links = ChannelSocialLinks.Resolve(channel.Socials)
            .Select(item => new PublicChannelSocialLinkContract(item.Provider, item.Url))
            .ToArray();
        return Ok(new PublicChannelSocialsContract(links));
    }
}
