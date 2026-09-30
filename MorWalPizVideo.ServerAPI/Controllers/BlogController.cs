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
[Route("api/blog")]
public sealed class BlogController(BlogService service, IContentService content, IConfiguration configuration,
    ILogger<BlogController> logger) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 300, Tags = [ApiTagCacheKeys.Blog], VaryByQueryKeys = ["page", "pageSize"])]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 50) return BadRequest("Invalid pagination.");
        var channelId = await ResolveChannelAsync();
        if (channelId is null) return NotFound();
        var posts = await service.ListAsync(channelId, true, page, pageSize, 1);
        return Ok(new PublicBlogPage(posts.Take(pageSize).Select(PublicBlogSummary.From).ToArray(), page, pageSize, posts.Count > pageSize));
    }

    [HttpGet("{slug}")]
    [OutputCache(Duration = 300, Tags = [ApiTagCacheKeys.Blog], VaryByRouteValueNames = ["slug"])]
    public async Task<IActionResult> Get(string slug)
    {
        var channelId = await ResolveChannelAsync();
        if (channelId is null) return NotFound();
        var post = await service.GetPublishedAsync(channelId, slug);
        return post is null ? NotFound() : Ok(PublicBlogPost.From(post));
    }

    private async Task<string?> ResolveChannelAsync()
    {
        var configured = configuration["YouTubeChannelId"]?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogError("Public blog is missing YouTubeChannelId configuration");
            return null;
        }
        var channelId = (await content.GetChannelByIdAsync(configured))?.ChannelId;
        if (string.IsNullOrWhiteSpace(channelId))
        {
            logger.LogError("Public blog cannot resolve configured channel {ConfiguredChannelId}", configured);
            return null;
        }
        return channelId;
    }
}