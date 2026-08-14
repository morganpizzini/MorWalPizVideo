using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Responses;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[AllowAnonymous]
[Route("api/shit")]
public sealed class ShootingItaController(
    IContentService contentService,
    IQuickLinksService quickLinksService,
    IChannelNewsService channelNewsService) : ControllerBase
{
    [HttpGet("channels")]
    [OutputCache(Tags = [CacheKeys.Channels])]
    public async Task<IActionResult> Channels()
    {
        var channels = await contentService.GetChannelsAsync();
        return Ok(channels.Where(channel => channel.IsSHIT).Select(ContractUtils.Convert));
    }

    [HttpGet("matches")]
    [OutputCache(Tags = [CacheKeys.Matches])]
    public async Task<IActionResult> Matches(int skip = 0, int take = 50)
    {
        var channelIds = await GetShootingItaChannelIdsAsync();
        var includePrivate = User.Identity?.IsAuthenticated ?? false;
        var count = await contentService.CountPublicMatchesForChannelsAsync(channelIds, includePrivate);
        var matches = await contentService.GetPublicMatchesForChannelsAsync(channelIds, includePrivate, skip, take);
        var safeTake = Math.Clamp(take, 1, 200);
        return Ok(new BaseResponse<IList<YouTubeContent>>(matches, count, $"skip={Math.Max(0, skip) + safeTake}&take={safeTake}"));
    }

    [HttpGet("quicklinks/{url}")]
    [OutputCache(Tags = [CacheKeys.QuickLinks], VaryByRouteValueNames = ["url"])]
    public async Task<IActionResult> QuickLinks(string url)
    {
        var entity = await quickLinksService.GetByUrlAsync(url, await GetShootingItaChannelIdsAsync());
        return entity is null ? NotFound() : Ok(ContractUtils.ConvertPublic(entity));
    }

    [HttpGet("channelnews")]
    [OutputCache(Tags = [ApiTagCacheKeys.ChannelNews])]
    public async Task<IActionResult> ChannelNews(int skip = 0, int take = 20)
    {
        var channels = (await contentService.GetChannelsAsync())
            .Where(channel => channel.IsSHIT)
            .ToDictionary(channel => channel.ChannelId, StringComparer.Ordinal);
        var items = await channelNewsService.GetPublicAsync(channels.Keys.ToArray(), DateTime.UtcNow);
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take, 1, 100);
        var result = items
            .Where(item => channels.ContainsKey(item.ChannelId))
            .OrderByDescending(item => item.PublicationTimeUtc ?? item.UpdatedDateTime)
            .ThenBy(item => item.DisplayOrder)
            .Skip(safeSkip)
            .Take(safeTake)
            .Select(item => ContractUtils.ConvertPublic(item, channels[item.ChannelId], "/images/logo-150.png"))
            .ToArray();
        return Ok(result);
    }

    [HttpGet("channelnews/{idOrSlug}")]
    [OutputCache(Tags = [ApiTagCacheKeys.ChannelNews], VaryByRouteValueNames = ["idOrSlug"])]
    public async Task<IActionResult> ChannelNewsDetail(string idOrSlug)
    {
        var channels = (await contentService.GetChannelsAsync())
            .Where(channel => channel.IsSHIT)
            .ToDictionary(channel => channel.ChannelId, StringComparer.Ordinal);
        var items = await channelNewsService.GetPublicAsync(channels.Keys.ToArray(), DateTime.UtcNow);
        var item = items.FirstOrDefault(candidate =>
            candidate.Id == idOrSlug || candidate.Slug.Equals(idOrSlug, StringComparison.OrdinalIgnoreCase));
        return item is null
            ? NotFound()
            : Ok(ContractUtils.ConvertPublic(item, channels[item.ChannelId], "/images/logo-150.png"));
    }

    private async Task<string[]> GetShootingItaChannelIdsAsync()
        => (await contentService.GetChannelsAsync())
            .Where(channel => channel.IsSHIT)
            .Select(channel => channel.ChannelId)
            .Where(channelId => !string.IsNullOrWhiteSpace(channelId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}