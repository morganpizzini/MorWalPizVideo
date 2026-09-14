using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.DTOs;
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
    public SocialPublishingConfigurationRequest SocialPublishing { get; set; } = new();
}

public class UpdateChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;

    public string? ShortLinkUrl { get; set; }

    [JsonPropertyName("isSHIT")]
    public bool IsSHIT { get; set; }

    public List<ChannelSocialRequest> Socials { get; set; } = [];
    public SocialPublishingConfigurationRequest SocialPublishing { get; set; } = new();
}

public class ChannelSocialRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
}

public class SocialPublishingConfigurationRequest
{
    public SocialPublishingProviderRequest? Telegram { get; set; } = new();
    public SocialPublishingProviderRequest? Discord { get; set; } = new();
    public SocialPublishingProviderRequest? Facebook { get; set; } = new();
}

public class SocialPublishingProviderRequest
{
    public string? DestinationId { get; set; }
    public string? Credential { get; set; }
    public bool ClearCredential { get; set; }
}

public class ChannelsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly IYTService ytService;
    private readonly IChannelContextResolver channelContextResolver;
    private readonly IVideoAuthorizationService channelAuthorization;
    private readonly ICrossApiService crossApiService;
    private readonly IBlobService blobService;
    private readonly ILogger<ChannelsController> logger;
    private readonly ISocialPublishingSecretProtector secretProtector;

    public ChannelsController(
        IYTService _ytService,
        DataService dataService,
        IChannelContextResolver channelContextResolver,
        IVideoAuthorizationService channelAuthorization,
        ICrossApiService crossApiService,
        IBlobService blobService,
        ISocialPublishingSecretProtector secretProtector,
        ILogger<ChannelsController> logger)
    {
        ytService = _ytService;
        _dataService = dataService;
        this.channelContextResolver = channelContextResolver;
        this.channelAuthorization = channelAuthorization;
        this.crossApiService = crossApiService;
        this.blobService = blobService;
        this.secretProtector = secretProtector;
        this.logger = logger;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ChannelsAdmin)]
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
        return Ok(ConvertManagement(existing));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ChannelsAdmin)]
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
        var socialPublishing = BuildSocialPublishing(request.SocialPublishing, null, channelId, out var publishingError);
        if (publishingError is not null) return BadRequest(publishingError);
        await _dataService.SaveChannel(new YTChannel(channelId, request.ChannelName.Trim())
        {
            ShortLinkUrl = shortLinkUrl,
            Socials = socials,
            SocialPublishing = socialPublishing,
            IsSHIT = request.IsSHIT
        });
        var cacheInvalidation = await TryInvalidatePublicShootingItaCachesAsync();
        return Ok(new { success = true, cacheInvalidation });
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
        var socialPublishing = BuildSocialPublishing(request.SocialPublishing, existing.SocialPublishing, existing.ChannelId, out var publishingError);
        if (publishingError is not null) return BadRequest(publishingError);
        await _dataService.UpdateChannel(existing with
        {
            ChannelName = request.ChannelName.Trim(),
            ShortLinkUrl = shortLinkUrl,
            Socials = socials,
            SocialPublishing = socialPublishing,
            IsSHIT = request.IsSHIT
        });
        var cacheInvalidation = await TryInvalidatePublicShootingItaCachesAsync();
        return Ok(new { success = true, cacheInvalidation });
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
        var cacheInvalidation = await TryInvalidatePublicShootingItaCachesAsync();
        return Ok(new { success = true, cacheInvalidation });
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
            var cacheInvalidation = await TryInvalidatePublicShootingItaCachesAsync();
            return Ok(new
            {
                success = true,
                channel = ContractUtils.Convert(existing with
                {
                    ChannelLogoStorageKey = storageKey,
                    ChannelLogoUrl = blobService.GetImageUrl(storageKey)
                }),
                cacheInvalidation
            });
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
        var cacheInvalidation = await TryInvalidatePublicShootingItaCachesAsync();
        return Ok(new { success = true, cacheInvalidation });
    }

    private async Task<CacheInvalidationResult> TryInvalidatePublicShootingItaCachesAsync()
    {
        try
        {
            await crossApiService.ResetCache(CacheKeys.Channels);
            await crossApiService.ResetCache(CacheKeys.Matches);
            await crossApiService.ResetCache(CacheKeys.QuickLinks);
            await crossApiService.ResetCache(CacheKeys.ChannelNews);
            await crossApiService.PurgeCache(CacheKeys.Channels);
            await crossApiService.PurgeCache(CacheKeys.Matches);
            await crossApiService.PurgeCache(CacheKeys.QuickLinks);
            await crossApiService.PurgeCache(ApiTagCacheKeys.ChannelNews);
            return new CacheInvalidationResult("completed", null, null);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public shooting ITA cache invalidation failed after channel mutation. CacheInvalidationStatus={CacheInvalidationStatus} WarningCode={WarningCode}", "failed", "public_cache_invalidation_failed");
            return new CacheInvalidationResult(
                "failed",
                "public_cache_invalidation_failed",
                "The operation completed, but the public cache was not reset.");
        }
    }

    private sealed record CacheInvalidationResult(string Status, string? WarningCode, string? Message);

    private ChannelManagementContract ConvertManagement(YTChannel entity)
    {
        var contract = ContractUtils.Convert(entity);
        var socialPublishing = entity.SocialPublishing ?? new SocialPublishingConfiguration();
        return new ChannelManagementContract
        {
            Id = contract.Id,
            ChannelId = contract.ChannelId,
            YTChannelId = contract.YTChannelId,
            ChannelName = contract.ChannelName,
            ShortLinkUrl = contract.ShortLinkUrl,
            IsSHIT = contract.IsSHIT,
            ChannelLogoUrl = contract.ChannelLogoUrl,
            Socials = contract.Socials,
            Videos = contract.Videos,
            SocialPublishing = new SocialPublishingManagementContract
            {
                Telegram = ConvertManagement(socialPublishing.Telegram ?? new SocialPublishingProviderConfiguration()),
                Discord = ConvertManagement(socialPublishing.Discord ?? new SocialPublishingProviderConfiguration()),
                Facebook = ConvertManagement(socialPublishing.Facebook ?? new SocialPublishingProviderConfiguration())
            }
        };
    }

    private static SocialPublishingProviderManagementContract ConvertManagement(
        SocialPublishingProviderConfiguration configuration) => new()
        {
            DestinationId = configuration.DestinationId,
            CredentialConfigured = !string.IsNullOrWhiteSpace(configuration.CredentialCiphertext)
        };

    private SocialPublishingConfiguration BuildSocialPublishing(
        SocialPublishingConfigurationRequest? request,
        SocialPublishingConfiguration? existing,
        string channelId,
        out string? error)
    {
        request ??= new SocialPublishingConfigurationRequest();
        existing ??= new SocialPublishingConfiguration();
        request.Telegram ??= new SocialPublishingProviderRequest();
        request.Discord ??= new SocialPublishingProviderRequest();
        request.Facebook ??= new SocialPublishingProviderRequest();
        error = ValidateProviderRequest(request.Telegram, "Telegram")
            ?? ValidateProviderRequest(request.Discord, "Discord")
            ?? ValidateProviderRequest(request.Facebook, "Facebook");
        if (error is not null)
        {
            return existing;
        }

        return new SocialPublishingConfiguration
        {
            Telegram = BuildProvider(request.Telegram, existing.Telegram ?? new SocialPublishingProviderConfiguration(), channelId, "telegram"),
            Discord = BuildProvider(request.Discord, existing.Discord ?? new SocialPublishingProviderConfiguration(), channelId, "discord"),
            Facebook = BuildProvider(request.Facebook, existing.Facebook ?? new SocialPublishingProviderConfiguration(), channelId, "facebook")
        };
    }

    private SocialPublishingProviderConfiguration BuildProvider(
        SocialPublishingProviderRequest request,
        SocialPublishingProviderConfiguration existing,
        string channelId,
        string provider)
    {
        var credentialCiphertext = request.ClearCredential
            ? string.Empty
            : string.IsNullOrWhiteSpace(request.Credential)
                ? existing.CredentialCiphertext
                : secretProtector.Protect(request.Credential.Trim(), channelId, provider);

        return new SocialPublishingProviderConfiguration
        {
            DestinationId = request.DestinationId?.Trim() ?? existing.DestinationId,
            CredentialCiphertext = credentialCiphertext
        };
    }

    private static string? ValidateProviderRequest(SocialPublishingProviderRequest request, string provider) =>
        request.ClearCredential && !string.IsNullOrWhiteSpace(request.Credential)
            ? $"{provider} credential cannot be replaced and cleared in the same request."
            : null;

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
