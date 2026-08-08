using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts.Videos;
using MorWalPiz.Contracts.Contracts;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Domain.Interfaces;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

public class VideosController : ApplicationControllerBase
{
    private readonly IContentService _contentService;
    private readonly ILinksService _linksService;
    private readonly ICrossApiService client;
    private readonly IYTService yTService;
    private readonly IExternalDataService externalDataService;
    private readonly ITelegramService telegramService;
    private readonly IDiscordService discordService;
    private readonly IFacebookService facebookService;
    private readonly IUserRepository userRepository;
    private readonly IConfiguration configuration;
    private readonly IVideoAuthorizationService authorization;
    
    public VideosController(IContentService contentService, ILinksService linksService, ICrossApiService _clientFactory,
        IYTService _yTService, IExternalDataService _externalDataService,
        ITelegramService _telegramService, IDiscordService _discordService,
        IFacebookService _facebookService, IUserRepository _userRepository,
        IConfiguration _configuration, IVideoAuthorizationService _authorization)
    {
        _contentService = contentService;
        _linksService = linksService;
        client = _clientFactory;
        yTService = _yTService;
        externalDataService = _externalDataService;
        telegramService = _telegramService;
        discordService = _discordService;
        facebookService = _facebookService;
        userRepository = _userRepository;
        configuration = _configuration;
        authorization = _authorization;
    }

    [HttpGet()]
    [AllowUser(AuthorizationPermissionKeys.VideosView, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> Fetch()
    {
        var matches = await GetAuthorizedMatchesAsync();
        return Ok(matches.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.VideosView, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> Get(BaseRequestId request)
    {
        var match = await FindAuthorizedMatchAsync(request.Id);
        if (match == null)
        {
            return NotFound();
        }
        return Ok(ContractUtils.Convert(match));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.VideosUpdate, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> Update(string id, [FromBody] VideoUpdateRequest request)
    {
        var existingMatch = await FindAuthorizedMatchAsync(id);
        if (existingMatch == null)
        {
            return NotFound("Video not found");
        }

        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        // Update the match using immutable record pattern
        var updatedMatch = existingMatch with
        {
            Title = request.Title,
            Description = request.Description,
            Url = request.Url,
            ThumbnailVideoId = request.ThumbnailVideoId,
            Categories = categories,
            VideoRefs = request.VideoRefs ?? existingMatch.VideoRefs
        };

        await _contentService.UpdateMatchAsync(updatedMatch);

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.VideosDelete, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> Delete(string id)
    {
        var match = await FindAuthorizedMatchAsync(id);
        if (match == null)
        {
            return NotFound();
        }

        await _contentService.DeleteMatchAsync(match.Id);
        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();
        return NoContent();
    }

    [HttpPost("Translate")]
    [AllowUser(AuthorizationPermissionKeys.VideosTranslate, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> TranslateShort(IList<string> videoIds)
    {
        foreach (var videoId in videoIds)
        {
            if (await FindAuthorizedMatchAsync(videoId) == null)
            {
                return NotFound("Video not found");
            }
        }

        await yTService.TranslateYoutubeVideo(videoIds);
        return NoContent();
    }
    [HttpPost("ImportVideo")]
    [AllowUser(AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> Import(VideoImportRequest request)
    {
        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(creatorUserId))
        {
            return Unauthorized();
        }

        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        var importedMatch = YouTubeContent.CreateSingleVideo(request.VideoId, categories) with
        {
            CreatorUserId = creatorUserId
        };
        await _contentService.SaveMatchAsync(importedMatch);

        // Populate metadata by calling ExternalDataService.FetchMatches()
        // This will fetch YouTube metadata and update the VideoRef with title, description, publishedAt
        await externalDataService.FetchMatches();

        // Auto-create shortlink for the imported video
        await CreateVideoShortLinkAsync(request.VideoId);

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();

        return NoContent();
    }

    [HttpPost("{id}/refresh-youtube")]
    [AllowUser(AuthorizationPermissionKeys.VideosUpdate, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> RefreshYouTubeData(string id)
    {
        var existingMatch = await FindAuthorizedMatchAsync(id);
        if (existingMatch == null)
        {
            return NotFound("Video not found");
        }

        var updatedMatch = await externalDataService.RefreshMatch(id);
        if (updatedMatch == null || !await authorization.CanAccessAsync(User, updatedMatch))
        {
            return NotFound("Video not found");
        }

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();

        return Ok(ContractUtils.Convert(updatedMatch));
    }

    [HttpPost("{id}/publish-social")]
    [AllowUser(AuthorizationPermissionKeys.VideosPublish, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> PublishToSocialMedia(string id, [FromBody] PublishSocialRequest request)
    {
        var match = await _contentService.FindMatchAsync(id);
        if (match == null || !await authorization.CanAccessAsync(User, match))
        {
            return NotFound("Video not found");
        }

        // Get the shortlink for this video
        var shortLink = (await _linksService.GetShortLinksAsync())
            .FirstOrDefault(x => x.LinkType == LinkType.YouTubeVideo &&
                                 x.ContentId == match.Id &&
                                 x.Target == id &&
                                 string.IsNullOrEmpty(x.QueryString));

        if (shortLink == null)
        {
            return BadRequest("No shortlink found for this video");
        }

        var errors = new List<string>();

        // Publish to Telegram
        try
        {
            var telegramError = await telegramService.CreatePost(shortLink.Code, request.Message);
            if (!string.IsNullOrEmpty(telegramError))
            {
                errors.Add($"Telegram: {telegramError}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Telegram: {ex.Message}");
        }

        // Publish to Discord
        try
        {
            var discordError = await discordService.CreatePost(shortLink.Code, request.Message);
            if (!string.IsNullOrEmpty(discordError))
            {
                errors.Add($"Discord: {discordError}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Discord: {ex.Message}");
        }

        // Publish to Facebook
        try
        {
            var facebookError = await facebookService.CreatePost(shortLink.Code, request.Message);
            if (!string.IsNullOrEmpty(facebookError))
            {
                errors.Add($"Facebook: {facebookError}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Facebook: {ex.Message}");
        }

        if (errors.Any())
        {
            return BadRequest(new { errors });
        }

        return Ok(new { message = "Successfully published to all platforms" });
    }

    [HttpPost("{youtubeId}/channel")]
    [AllowUser(AuthorizationPermissionKeys.VideosUpdate, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> AssignChannel(string youtubeId, [FromBody] VideoChannelAssignmentPayload payload)
    {
        if (string.IsNullOrWhiteSpace(youtubeId))
        {
            return BadRequest(new { error = "youtubeId is required" });
        }
        if (payload is null || string.IsNullOrWhiteSpace(payload.ChannelId))
        {
            return BadRequest(new { error = "channelId is required" });
        }

        var targetChannel = await _contentService.GetChannelByIdAsync(payload.ChannelId);
        if (targetChannel is null)
        {
            return BadRequest(new { error = $"Unknown channelId '{payload.ChannelId}'" });
        }

        if (!await authorization.CanManageChannelAsync(User, payload.ChannelId))
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found" });
        }

        var existingMatch = await _contentService.FindMatchAsync(youtubeId);
        if (existingMatch != null && !await authorization.CanAccessAsync(User, existingMatch))
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found" });
        }

        if (existingMatch is null)
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found in any channel or match" });
        }

        if (existingMatch != null)
        {
            var updatedRefs = existingMatch.VideoRefs.Select(video =>
                video.YoutubeId == youtubeId
                    ? video with { ChannelIds = video.ChannelIds.Append(payload.ChannelId).Distinct().ToArray() }
                    : video).ToArray();
            await _contentService.UpdateMatchAsync(existingMatch with { VideoRefs = updatedRefs });
        }

        // Ensure on target channel.
        var targetVideos = targetChannel.Videos?.ToList() ?? new List<YouTubeVideo>();
        if (!targetVideos.Any(v => v.VideoId == youtubeId))
        {
            targetVideos.Add(new YouTubeVideo { VideoId = youtubeId, LastCommentDate = DateTime.UtcNow });
            var updatedTarget = targetChannel with { Videos = targetVideos };
            await _contentService.UpdateChannelAsync(updatedTarget);
        }

        await client.ResetCache(CacheKeys.Channels);
        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();

        return Ok(new { youtubeId, channelId = payload.ChannelId });
    }

    #region ShortLink Helper

    private async Task<IList<YouTubeContent>> GetAuthorizedMatchesAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId)
            ? []
            : await _contentService.GetAuthorizedMatchesAsync(userId, await authorization.IsAdminAsync(User));
    }

    private async Task<YouTubeContent?> FindAuthorizedMatchAsync(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await _contentService.FindAuthorizedMatchAsync(id, userId, await authorization.IsAdminAsync(User));
    }

    /// <summary>
    /// Auto-creates a shortlink for a video (similar to ShortLinksController logic).
    /// </summary>
    private async Task<string?> CreateVideoShortLinkAsync(string videoId)
    {
        var existingMatch = await _contentService.FindMatchAsync(videoId);
        if (existingMatch == null)
        {
            return null;
        }

        // Check if a canonical shortlink already exists for this video (ADR-004: standalone aggregate).
        var existingShortLink = (await _linksService.GetShortLinksAsync())
            .FirstOrDefault(x => x.LinkType == LinkType.YouTubeVideo
                && x.ContentId == existingMatch.Id
                && x.Target == videoId
                && string.IsNullOrEmpty(x.QueryString));

        if (existingShortLink != null)
        {
            return BuildShortLinkUrl(existingShortLink.Code);
        }

        // Generate unique shortlink code
        var shortLinkCode = await CalculateShortLinkAsync();

        // Create new canonical shortlink referencing the owning match instead of embedding it.
        var newShortLink = new ShortLink(shortLinkCode, videoId, Array.Empty<QueryLink>())
        {
            LinkType = LinkType.YouTubeVideo,
            ContentId = existingMatch.Id
        };

        await _linksService.SaveShortLinkAsync(newShortLink);

        // Reset shortlink cache
        await client.ResetCache(CacheKeys.ShortLinks);

        return BuildShortLinkUrl(newShortLink.Code);
    }

    private string BuildShortLinkUrl(string code)
    {
        var siteUrl = configuration["SiteUrl"] ?? string.Empty;
        return $"{siteUrl}{code}";
    }

    /// <summary>
    /// Generates a unique shortlink code (mirrors ShortLinksController logic).
    /// </summary>
    private async Task<string> CalculateShortLinkAsync()
    {
        var shortlinks = await _linksService.GetShortLinksAsync();
        var sl = shortlinks.Select(x => x.Code.ToLower()).ToList();

        return GetUniqueValue(sl);

        string GetUniqueValue(IEnumerable<string> strings)
        {
            // Sort and concatenate the input strings
            string concatenated = string.Join("", strings.OrderBy(s => s));

            // Hash the concatenated string
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatenated));

            // Convert hash bytes to a hexadecimal string
            string hash = Convert.ToHexString(hashBytes);

            // Check if the truncated hash conflicts with inputs
            string uniqueString = hash.Substring(0, 5).ToLower();
            while (strings.Contains(uniqueString))
            {
                uniqueString = GetUniqueValue([.. strings, uniqueString]);
            }

            return uniqueString;
        }
    }

    #endregion
}
