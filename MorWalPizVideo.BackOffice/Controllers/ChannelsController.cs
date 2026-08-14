using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.BackOffice.Controllers;

public class AddChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("yTChannelId")]
    public string? YTChannelId { get; set; }

    public string? ShortLinkUrl { get; set; }

    [JsonPropertyName("isSHIT")]
    public bool IsSHIT { get; set; }

    public List<ChannelSocialRequest> Socials { get; set; } = [];
}

public class UpdateChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;

    public string? ShortLinkUrl { get; set; }

    [JsonPropertyName("isSHIT")]
    public bool IsSHIT { get; set; }

    public List<ChannelSocialRequest> Socials { get; set; } = [];
}

public class ChannelSocialRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
}

public class ChannelsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly IYTService ytService;
    private readonly IChannelContextResolver channelContextResolver;
    private readonly IVideoAuthorizationService channelAuthorization;
    private readonly ICrossApiService crossApiService;
    private readonly IBlobService blobService;

    public ChannelsController(
        IYTService _ytService,
        DataService dataService,
        IChannelContextResolver channelContextResolver,
        IVideoAuthorizationService channelAuthorization,
        ICrossApiService crossApiService,
        IBlobService blobService)
    {
        ytService = _ytService;
        _dataService = dataService;
        this.channelContextResolver = channelContextResolver;
        this.channelAuthorization = channelAuthorization;
        this.crossApiService = crossApiService;
        this.blobService = blobService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> GetChannels()
    {
        var entities = await _dataService.GetChannels();
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("accessible")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<IActionResult> GetAccessibleChannels()
    {
        var entities = await channelContextResolver.GetAccessibleChannelsAsync(User);
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> GetChannel(string id)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
        {
            return NotFound();
        }

        var existing = await _dataService.GetChannelById(id);
        if (existing == null)
        {
            return NotFound();
        }
        return Ok(ContractUtils.Convert(existing));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ChannelsCreate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> AddChannel(AddChannelRequest request)
    {
        var channelId = request.YTChannelId is null
            ? await ytService.GetChannelId(request.ChannelName.Trim())
            : request.YTChannelId.Trim();

        if (string.IsNullOrWhiteSpace(channelId))
        {
            return BadRequest("YouTube channel ID is required");
        }
        var shortLinkUrl = NormalizeShortLinkUrl(request.ShortLinkUrl);
        if (shortLinkUrl is null)
        {
            return BadRequest("Short link URL must be an absolute HTTP or HTTPS base URL without a query or fragment.");
        }
        var socials = NormalizeSocials(request.Socials);
        if (socials is null) return BadRequest("Only Instagram, YouTube, Reddit, X, and Patreon providers are allowed.");
        await _dataService.SaveChannel(new YTChannel(channelId, request.ChannelName.Trim())
        {
            ShortLinkUrl = shortLinkUrl,
            Socials = socials,
            IsSHIT = request.IsSHIT
        });
        await InvalidatePublicShootingItaCachesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsUpdate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> UpdateChannel(string id, UpdateChannelRequest request)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
        {
            return NotFound();
        }

        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
        {
            return NotFound();
        }

        var shortLinkUrl = NormalizeShortLinkUrl(request.ShortLinkUrl);
        if (shortLinkUrl is null)
        {
            return BadRequest("Short link URL must be an absolute HTTP or HTTPS base URL without a query or fragment.");
        }
        var socials = NormalizeSocials(request.Socials);
        if (socials is null) return BadRequest("Only Instagram, YouTube, Reddit, X, and Patreon providers are allowed.");
        await _dataService.UpdateChannel(existing with
        {
            ChannelName = request.ChannelName.Trim(),
            ShortLinkUrl = shortLinkUrl,
            Socials = socials,
            IsSHIT = request.IsSHIT
        });
        await InvalidatePublicShootingItaCachesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsDelete, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> RemoveChannel(string id)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
        {
            return NotFound();
        }

        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _dataService.RemoveChannelById(id);
        await InvalidatePublicShootingItaCachesAsync();
        return NoContent();
    }

    [HttpPost("{id}/logo")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsUpdate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> UploadLogo(string id, IFormFile logo)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
            return NotFound();
        if (logo is null || logo.Length == 0)
            return BadRequest("A PNG logo is required.");

        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
            return NotFound();

        try
        {
            await using var input = logo.OpenReadStream();
            var prepared = await ChannelNewsMediaProcessor.PrepareLogoAsync(input);
            var storageKey = $"channel-logos/{existing.ChannelId}/{Guid.NewGuid():N}{prepared.Extension}";
            await blobService.UploadImagesAsync(storageKey, prepared.Content, false);
            await _dataService.UpdateChannel(existing with
            {
                ChannelLogoStorageKey = storageKey,
                ChannelLogoUrl = blobService.GetImageUrl(storageKey)
            });
            await InvalidatePublicShootingItaCachesAsync();
            return Ok(ContractUtils.Convert(existing with
            {
                ChannelLogoStorageKey = storageKey,
                ChannelLogoUrl = blobService.GetImageUrl(storageKey)
            }));
        }
        catch (Exception exception)
        {
            return BadRequest($"The channel logo is not a valid PNG: {exception.Message}");
        }
    }

    [HttpDelete("{id}/logo")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsUpdate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> RemoveLogo(string id)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
            return NotFound();

        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
            return NotFound();

        await _dataService.UpdateChannel(existing with { ChannelLogoStorageKey = string.Empty, ChannelLogoUrl = string.Empty });
        if (!string.IsNullOrWhiteSpace(existing.ChannelLogoStorageKey))
        {
            await blobService.DeleteImageAsync(existing.ChannelLogoStorageKey);
        }
        await InvalidatePublicShootingItaCachesAsync();
        return NoContent();
    }

    private async Task InvalidatePublicShootingItaCachesAsync()
    {
        await crossApiService.ResetCache(CacheKeys.Channels);
        await crossApiService.ResetCache(CacheKeys.Matches);
        await crossApiService.ResetCache(CacheKeys.QuickLinks);
        await crossApiService.ResetCache(CacheKeys.ChannelNews);
        await crossApiService.PurgeCache(CacheKeys.Channels);
        await crossApiService.PurgeCache(CacheKeys.Matches);
        await crossApiService.PurgeCache(CacheKeys.QuickLinks);
        await crossApiService.PurgeCache(ApiTagCacheKeys.ChannelNews);
    }

    private static List<ChannelSocial>? NormalizeSocials(IEnumerable<ChannelSocialRequest>? requests)
    {
        var allowed = new[] { "instagram", "youtube", "reddit", "x", "patreon" };
        var result = new List<ChannelSocial>();
        foreach (var request in requests ?? [])
        {
            var provider = request.Provider.Trim().ToLowerInvariant();
            if (!allowed.Contains(provider, StringComparer.Ordinal) || string.IsNullOrWhiteSpace(request.Handler)) return null;
            result.Add(new ChannelSocial { Provider = provider, Handler = request.Handler.Trim() });
        }
        return result.GroupBy(s => s.Provider, StringComparer.Ordinal).Select(g => g.Last()).ToList();
    }

    private static string? NormalizeShortLinkUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return null;
        }

        return uri.AbsoluteUri.TrimEnd('/');
    }
}
