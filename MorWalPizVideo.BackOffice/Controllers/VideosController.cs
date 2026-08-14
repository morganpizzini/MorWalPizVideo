using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts.Videos;
using MorWalPiz.Contracts.Contracts;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Authentication;
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

[RequireChannelScope]
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
        var existingMatch = await FindManageableMatchAsync(id);
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
        var match = await FindManageableMatchAsync(id);
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
            if (await FindManageableMatchAsync(videoId) == null)
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
        var creatorUserId = ImpersonationClaimsTransformation.GetEffectiveUserId(User);
        if (string.IsNullOrWhiteSpace(creatorUserId))
        {
            return Unauthorized();
        }

        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        var channelContext = HttpContext.GetChannelContext();
        var importedMatch = YouTubeContent.CreateSingleVideo(request.VideoId, categories) with
        {
            CreatorUserId = creatorUserId,
            OwnerChannelId = channelContext.ChannelId,
            VideoRefs = [new VideoRef(request.VideoId, categories, channelIds: [channelContext.ChannelId])]
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

    [HttpGet("import-candidates")]
    [AllowUser(AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> ImportCandidates([FromQuery] VideoImportCandidatesRequest request)
    {
        var channelContext = HttpContext.GetChannelContext();
        if ((!string.IsNullOrWhiteSpace(request.ChannelId) &&
             !string.Equals(channelContext.ChannelId, request.ChannelId, StringComparison.Ordinal)) ||
            !await authorization.CanManageChannelAsync(User, channelContext.ChannelId))
        {
            return NotFound();
        }

        var matches = await _contentService.GetAllMatchesAsync();
        var importedIds = matches
            .SelectMany(match => new[] { match.ContentId }.Concat(match.VideoRefs.Select(video => video.YoutubeId)))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        var startDate = request.StartDate.Kind == DateTimeKind.Local
            ? request.StartDate.ToUniversalTime().Date
            : request.StartDate.Date;
        var endDate = request.EndDate is { } requestedEndDate
            ? requestedEndDate.Kind == DateTimeKind.Local
                ? requestedEndDate.ToUniversalTime().Date
                : requestedEndDate.Date
            : DateTime.UtcNow.Date;
        var candidates = await yTService.FetchVideosBetween(
            channelContext.ChannelId,
            DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            DateTime.SpecifyKind(endDate, DateTimeKind.Utc));

        return Ok(candidates
            .Where(candidate => !importedIds.Contains(candidate.Id.VideoId))
            .Select(candidate => new VideoImportCandidateResponse(
                candidate.Id.VideoId,
                candidate.Snippet?.Title ?? string.Empty,
                candidate.Snippet?.PublishedAtDateTimeOffset?.UtcDateTime ?? DateTime.MinValue,
                false))
            .ToArray());
    }

    [HttpGet("import-targets")]
    [AllowUser(AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> ImportTargets()
    {
        var matches = await _contentService.GetAllMatchesAsync();
        var targets = new List<YouTubeContent>();
        foreach (var match in matches)
        {
            if (await authorization.CanAccessAsync(User, match))
            {
                targets.Add(match);
            }
        }

        return Ok(targets.Select(match => new
        {
            contentId = match.ContentId,
            title = match.Title,
            videoCount = match.VideoRefs.Length
        }));
    }

    [HttpPost("bulk-import")]
    [AllowUser(AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> BulkImport(VideoBulkImportRequest request)
    {
        var creatorUserId = ImpersonationClaimsTransformation.GetEffectiveUserId(User);
        if (string.IsNullOrWhiteSpace(creatorUserId))
        {
            return Unauthorized();
        }

        var allMatches = await _contentService.GetAllMatchesAsync();
        var targetsById = allMatches.ToDictionary(match => match.ContentId, StringComparer.Ordinal);
        var importedIds = allMatches
            .SelectMany(match => new[] { match.ContentId }.Concat(match.VideoRefs.Select(video => video.YoutubeId)))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var results = new List<VideoBulkImportItemResponse>();

        var channelId = HttpContext.GetChannelContext().ChannelId;
        var createdTargets = new Dictionary<string, YouTubeContent>(StringComparer.Ordinal);
        var requestedVideoIds = request.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.VideoId))
            .Select(item => item.VideoId.Trim())
            .ToArray();
        if (requestedVideoIds.Length != requestedVideoIds.Distinct(StringComparer.Ordinal).Count())
        {
            return BadRequest(new { error = "A video cannot be selected more than once" });
        }

        foreach (var item in request.Items.Where(item => !string.IsNullOrWhiteSpace(item.VideoId)))
        {
            var videoId = item.VideoId.Trim();
            if (importedIds.Contains(videoId))
            {
                results.Add(new(videoId, "skipped"));
                continue;
            }

            try
            {
                var categories = (await _contentService.GetCategoriesAsync(item.Categories))
                    .Select(category => new CategoryRef(category.Id, category.Title))
                    .ToArray();
                if (categories.Length != item.Categories.Distinct(StringComparer.Ordinal).Count())
                {
                    results.Add(new(videoId, "error", "One or more categories were not found"));
                    continue;
                }

                var videos = await yTService.FetchFromYoutube([videoId]);
                var video = videos.FirstOrDefault();
                if (video is null)
                {
                    results.Add(new(videoId, "error", "YouTube video metadata was not found"));
                    continue;
                }

                var videoRef = new VideoRef(
                    video.YoutubeId,
                    categories,
                    video.Title,
                    video.Description,
                    video.PublishedAt,
                    [channelId]);

                YouTubeContent? target = null;
                if (!string.IsNullOrWhiteSpace(item.Target))
                {
                    target = createdTargets.GetValueOrDefault(item.Target) ?? targetsById.GetValueOrDefault(item.Target);
                    if (target is null || !await authorization.CanAccessAsync(User, target))
                    {
                        results.Add(new(videoId, "error", "Target content was not found or is not accessible"));
                        continue;
                    }
                    var updatedTarget = target with { VideoRefs = target.VideoRefs.Append(videoRef).ToArray() };
                    await _contentService.UpdateMatchAsync(updatedTarget);
                    if (createdTargets.ContainsKey(item.Target))
                    {
                        createdTargets[item.Target] = updatedTarget;
                    }
                    targetsById[item.Target] = updatedTarget;
                }
                else
                {
                    var importedMatch = YouTubeContent.CreateSingleVideo(videoId, categories) with
                    {
                        CreatorUserId = creatorUserId,
                        OwnerChannelId = channelId,
                        VideoRefs = [videoRef],
                        Title = video.Title,
                        Description = video.Description,
                        CreationDateTime = video.PublishedAt
                    };
                    await _contentService.SaveMatchAsync(importedMatch);
                    await CreateVideoShortLinkAsync(videoId);
                    createdTargets[videoId] = importedMatch;
                }

                importedIds.Add(videoId);
                results.Add(new(videoId, "imported"));
            }
            catch (Exception exception)
            {
                results.Add(new(videoId, "error", exception.Message));
            }
        }

        if (results.Any(result => result.Status == "imported"))
        {
            await client.ResetCache(CacheKeys.Matches);
            await client.PurgeCache(ApiTagCacheKeys.Matches);
            await client.ReloadCache();
        }

        return Ok(results);
    }

    [HttpPost("{id}/refresh-youtube")]
    [AllowUser(AuthorizationPermissionKeys.VideosUpdate, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> RefreshYouTubeData(string id)
    {
        var existingMatch = await FindManageableMatchAsync(id);
        if (existingMatch == null)
        {
            return NotFound("Video not found");
        }

        var updatedMatch = await externalDataService.RefreshMatch(id);
        if (updatedMatch == null || !await authorization.CanMutateInChannelAsync(User, updatedMatch, HttpContext.GetChannelContext().ChannelId))
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
        var match = await FindManageableMatchAsync(id);
        if (match == null)
        {
            return NotFound("Video not found");
        }

        // Get the shortlink for this video
        var shortLink = match.ShortLinks.FirstOrDefault(x =>
            x.LinkType == LinkType.YouTubeVideo &&
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

        if (!await authorization.CanManageChannelAsync(User, payload.ChannelId) ||
            payload.ChannelId != HttpContext.GetChannelContext().ChannelId)
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found" });
        }

        var existingMatch = await _contentService.FindMatchAsync(youtubeId);
        if (existingMatch != null && !await authorization.CanMutateInChannelAsync(User, existingMatch, HttpContext.GetChannelContext().ChannelId))
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
        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(User);
        return string.IsNullOrWhiteSpace(userId)
            ? []
            : await _contentService.GetAuthorizedMatchesAsync(userId, await authorization.IsAdminAsync(User), HttpContext.GetChannelContext().ChannelId);
    }

    private async Task<YouTubeContent?> FindAuthorizedMatchAsync(string id)
    {
        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(User);
        return string.IsNullOrWhiteSpace(userId)
            ? null
                : await _contentService.FindAuthorizedMatchAsync(id, userId, await authorization.IsAdminAsync(User), HttpContext.GetChannelContext().ChannelId);
    }

            private async Task<YouTubeContent?> FindManageableMatchAsync(string id)
            {
            var match = await FindAuthorizedMatchAsync(id);
            return match is not null && await authorization.CanMutateInChannelAsync(
                User, match, HttpContext.GetChannelContext().ChannelId)
                ? match
                : null;
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

        var existingShortLink = existingMatch.ShortLinks
            .FirstOrDefault(x => x.LinkType == LinkType.YouTubeVideo
                && x.Target == videoId
                && string.IsNullOrEmpty(x.QueryString));

        if (existingShortLink != null)
        {
            return BuildShortLinkUrl(existingShortLink.Code);
        }

        // Generate unique shortlink code
        var shortLinkCode = await CalculateShortLinkAsync();

        var newShortLink = new ShortLink(shortLinkCode, videoId, Array.Empty<QueryLink>())
        {
            LinkType = LinkType.YouTubeVideo
        };

        await _contentService.UpdateMatchAsync(existingMatch.AddShortLink(newShortLink));

        await client.ResetCache(CacheKeys.ShortLinks);
        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);

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
        var matches = await _contentService.GetAllMatchesAsync();
        var sl = shortlinks.Select(x => x.NormalizedCode)
            .Concat(matches.SelectMany(match => match.ShortLinks).Select(x => x.NormalizedCode))
            .Distinct(StringComparer.Ordinal)
            .ToList();

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
