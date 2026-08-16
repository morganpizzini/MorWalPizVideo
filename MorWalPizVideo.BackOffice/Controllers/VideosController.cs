using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts.Videos;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Utils;
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
    private readonly IConfiguration configuration;
    private readonly IVideoAuthorizationService authorization;
    private readonly ILogger<VideosController> logger;

    public VideosController(IContentService contentService, ILinksService linksService, ICrossApiService _clientFactory,
        IYTService _yTService, IExternalDataService _externalDataService,
        ITelegramService _telegramService, IDiscordService _discordService,
        IFacebookService _facebookService, IConfiguration _configuration,
        IVideoAuthorizationService _authorization, ILogger<VideosController> _logger)
    {
        _contentService = contentService;
        _linksService = linksService;
        client = _clientFactory;
        yTService = _yTService;
        externalDataService = _externalDataService;
        telegramService = _telegramService;
        discordService = _discordService;
        facebookService = _facebookService;
        configuration = _configuration;
        authorization = _authorization;
        logger = _logger;
    }

    [HttpGet("import-status")]
    [AllowUser(AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosManage)]
    public async Task<IActionResult> ImportStatus([FromServices] Microsoft.FeatureManagement.IFeatureManager featureManager)
    {
        return Ok(new { enabled = await featureManager.IsEnabledAsync(MyFeatureFlags.EnableVideoBulkImport) });
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
        if (categories.Length != request.Categories.Distinct(StringComparer.Ordinal).Count())
        {
            return BadRequest("One or more categories were not found");
        }

        if (request.VideoRefs is not null && !await ValidateVideoReferenceChannelsAsync(existingMatch, request.VideoRefs))
        {
            return BadRequest("One or more video channel assignments are invalid or unauthorized");
        }

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
        await client.PurgeCache(CacheKeys.Matches);
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
        await client.PurgeCache(CacheKeys.Matches);
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
        try
        {
            await yTService.TranslateYoutubeVideo(videoIds);
        }
        catch (NotSupportedException exception)
        {
            logger.LogError(exception, "YouTube translation is unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "translation_unavailable"
            });
        }
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

        var videoId = request.VideoId.Trim();
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var existingMatch = await _contentService.FindMatchAsync(videoId);
        if (existingMatch is not null)
        {
            if (!await authorization.CanMutateInChannelAsync(User, existingMatch, channelId))
            {
                return NotFound("Video not found");
            }

            return Conflict(new VideoImportResponse(videoId, "alreadyExists", Error: "Video is already imported"));
        }

        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();
        if (categories.Length != request.Categories.Distinct(StringComparer.Ordinal).Count())
        {
            return BadRequest("One or more categories were not found");
        }

        var importedMatch = YouTubeContent.CreateSingleVideo(videoId, categories) with
        {
            CreatorUserId = creatorUserId,
            OwnerChannelId = channelId,
            VideoRefs = [new VideoRef(videoId, categories, channelIds: [channelId])]
        };
        if (!await _contentService.SaveMatchAsync(importedMatch))
        {
            return Conflict(new VideoImportResponse(videoId, "error", Error: "Video could not be persisted"));
        }

        await externalDataService.RefreshMatch(videoId);

        // Auto-create shortlink for the imported video
        var shortLink = await CreateVideoShortLinkAsync(videoId);

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(CacheKeys.Matches);
        await client.ReloadCache();

        return Ok(new VideoImportResponse(
            videoId,
            "imported",
            shortLink is null ? "failed" : "created",
            shortLink is null ? "Video imported, but automatic short-link creation failed" : null));
    }

    [HttpGet("import-candidates")]
    [FeatureGate(MyFeatureFlags.EnableVideoBulkImport)]
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
    [FeatureGate(MyFeatureFlags.EnableVideoBulkImport)]
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
    [FeatureGate(MyFeatureFlags.EnableVideoBulkImport)]
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
                    results.Add(new(videoId, "error", Error: "One or more categories were not found"));
                    continue;
                }

                YouTubeContent? target = null;
                var targetKey = item.Target?.Trim();
                if (!string.IsNullOrWhiteSpace(targetKey))
                {
                    target = createdTargets.GetValueOrDefault(targetKey) ?? targetsById.GetValueOrDefault(targetKey);
                    if (target is null || !await authorization.CanMutateInChannelAsync(User, target, channelId))
                    {
                        results.Add(new(videoId, "error", Error: "Target content was not found or is not accessible"));
                        continue;
                    }
                }

                var videos = await yTService.FetchFromYoutube([videoId]);
                var video = videos.FirstOrDefault();
                if (video is null)
                {
                    results.Add(new(videoId, "error", Error: "YouTube video metadata was not found"));
                    continue;
                }

                var videoRef = new VideoRef(
                    video.YoutubeId,
                    categories,
                    video.Title,
                    video.Description,
                    video.PublishedAt,
                    [channelId]);

                if (target is not null && !string.IsNullOrWhiteSpace(targetKey))
                {
                    var updatedTarget = target with { VideoRefs = target.VideoRefs.Append(videoRef).ToArray() };
                    await _contentService.UpdateMatchAsync(updatedTarget);
                    if (createdTargets.ContainsKey(targetKey))
                    {
                        createdTargets[targetKey] = updatedTarget;
                    }
                    targetsById[targetKey] = updatedTarget;
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
                    if (!await _contentService.SaveMatchAsync(importedMatch))
                    {
                        results.Add(new(videoId, "error", Error: "Video could not be persisted"));
                        continue;
                    }
                    var shortLink = await CreateVideoShortLinkAsync(videoId);
                    createdTargets[videoId] = importedMatch;
                    importedIds.Add(videoId);
                    results.Add(new(
                        videoId,
                        "imported",
                        shortLink is null ? "failed" : "created",
                        shortLink is null ? "Video imported, but automatic short-link creation failed" : null));
                    continue;
                }

                importedIds.Add(videoId);
                results.Add(new(videoId, "imported", "notAttempted"));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Bulk video import failed for video {VideoId}, target {Target}, channel {ChannelId}",
                    videoId, item.Target, channelId);
                results.Add(new(videoId, "error", Error: "Video import failed"));
            }
        }

        if (results.Any(result => result.Status == "imported"))
        {
            await client.ResetCache(CacheKeys.Matches);
            await client.PurgeCache(CacheKeys.Matches);
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
        await client.PurgeCache(CacheKeys.Matches);
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

        var youtubeId = match.VideoRefs.FirstOrDefault(video => video.YoutubeId == id)?.YoutubeId ??
            (match.ThumbnailVideoId == id ? id : null);
        var shortLink = youtubeId is null ? null : await _linksService.GetCanonicalVideoShortLinkAsync(match.Id, youtubeId);

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
                logger.LogWarning("Telegram publishing returned an unsuccessful response for short link {ShortLink}", shortLink.Code);
                errors.Add("Telegram publishing failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Telegram publishing failed for short link {ShortLink}", shortLink.Code);
            errors.Add("Telegram publishing failed");
        }

        // Publish to Discord
        try
        {
            var discordError = await discordService.CreatePost(shortLink.Code, request.Message);
            if (!string.IsNullOrEmpty(discordError))
            {
                logger.LogWarning("Discord publishing returned an unsuccessful response for short link {ShortLink}", shortLink.Code);
                errors.Add("Discord publishing failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discord publishing failed for short link {ShortLink}", shortLink.Code);
            errors.Add("Discord publishing failed");
        }

        // Publish to Facebook
        try
        {
            var facebookError = await facebookService.CreatePost(shortLink.Code, request.Message);
            if (!string.IsNullOrEmpty(facebookError))
            {
                logger.LogWarning("Facebook publishing returned an unsuccessful response for short link {ShortLink}", shortLink.Code);
                errors.Add("Facebook publishing failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Facebook publishing failed for short link {ShortLink}", shortLink.Code);
            errors.Add("Facebook publishing failed");
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
        if (existingMatch is null || !existingMatch.VideoRefs.Any(video => video.YoutubeId == youtubeId))
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found in any channel or match" });
        }

        if (!await authorization.CanMutateInChannelAsync(User, existingMatch, HttpContext.GetChannelContext().ChannelId))
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found" });
        }

        var updatedRefs = existingMatch.VideoRefs.Select(video =>
            video.YoutubeId == youtubeId
                ? video with { ChannelIds = video.ChannelIds.Append(payload.ChannelId).Distinct().ToArray() }
                : video).ToArray();
        var updatedMatch = existingMatch with { VideoRefs = updatedRefs };

        try
        {
            await _contentService.UpdateMatchAsync(updatedMatch);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AssignChannel failed while updating match {MatchId} for video {YoutubeId} and channel {ChannelId}",
                existingMatch.Id, youtubeId, payload.ChannelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Video channel assignment failed before the target channel was updated",
                matchUpdated = false,
                targetChannelUpdated = false
            });
        }

        var targetVideos = targetChannel.Videos?.ToList() ?? [];
        if (!targetVideos.Any(video => video.VideoId == youtubeId))
        {
            targetVideos.Add(new YouTubeVideo { VideoId = youtubeId, LastCommentDate = DateTime.UtcNow });
        }

        try
        {
            await _contentService.UpdateChannelAsync(targetChannel with { Videos = targetVideos });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AssignChannel partially completed after match {MatchId} update; target channel {ChannelId} failed for video {YoutubeId}",
                existingMatch.Id, payload.ChannelId, youtubeId);
            try
            {
                await client.ResetCache(CacheKeys.Matches);
                await client.PurgeCache(CacheKeys.Matches);
            }
            catch (Exception cacheException)
            {
                logger.LogError(cacheException,
                    "AssignChannel partial failure could not invalidate match cache for {MatchId}",
                    existingMatch.Id);
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Video channel assignment partially completed; reconcile the target channel membership",
                matchUpdated = true,
                targetChannelUpdated = false
            });
        }

        await client.ResetCache(CacheKeys.Channels);
        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(CacheKeys.Matches);
        await client.ReloadCache();

        return Ok(new { youtubeId, channelId = payload.ChannelId });
    }

    #region ShortLink Helper

    private async Task<IList<YouTubeContent>> GetAuthorizedMatchesAsync()
    {
        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var channelId = HttpContext.GetChannelContext().ChannelId;
        var matches = await _contentService.GetAuthorizedMatchesAsync(userId, await authorization.IsAdminAsync(User), channelId);
        var authorizedMatches = new List<YouTubeContent>();
        foreach (var match in matches)
        {
            if (await authorization.CanReadInChannelAsync(User, match, channelId))
            {
                authorizedMatches.Add(match);
            }
        }

        return await _linksService.MergeCanonicalVideoShortLinksAsync(authorizedMatches);
    }

    private async Task<YouTubeContent?> FindAuthorizedMatchAsync(string id)
    {
        var matches = await GetAuthorizedMatchesAsync();
        return matches.FirstOrDefault(match => match.ThumbnailVideoId == id ||
            match.Id == id || match.VideoRefs.Any(video => video.YoutubeId == id));
    }

    private async Task<YouTubeContent?> FindManageableMatchAsync(string id)
    {
        var match = await FindAuthorizedMatchAsync(id);
        return match is not null && await authorization.CanMutateInChannelAsync(
            User, match, HttpContext.GetChannelContext().ChannelId)
            ? match
            : null;
    }

    private async Task<bool> ValidateVideoReferenceChannelsAsync(
        YouTubeContent existingMatch,
        IReadOnlyCollection<VideoRef> requestedReferences)
    {
        if (requestedReferences
            .GroupBy(reference => reference.YoutubeId, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            return false;
        }

        var existingReferences = existingMatch.VideoRefs
            .ToDictionary(reference => reference.YoutubeId, StringComparer.Ordinal);
        var requestedVideoIds = requestedReferences
            .Select(reference => reference.YoutubeId)
            .ToHashSet(StringComparer.Ordinal);
        var channelIdsToValidate = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requestedReference in requestedReferences)
        {
            if (existingReferences.TryGetValue(requestedReference.YoutubeId, out var existingReference) &&
                existingReference.ChannelIds.ToHashSet(StringComparer.Ordinal).SetEquals(requestedReference.ChannelIds))
            {
                continue;
            }

            channelIdsToValidate.UnionWith(requestedReference.ChannelIds);
            if (existingReferences.TryGetValue(requestedReference.YoutubeId, out existingReference))
            {
                channelIdsToValidate.UnionWith(existingReference.ChannelIds);
            }
        }

        foreach (var removedReference in existingMatch.VideoRefs.Where(reference =>
                     !requestedVideoIds.Contains(reference.YoutubeId)))
        {
            channelIdsToValidate.UnionWith(removedReference.ChannelIds);
        }

        foreach (var channelId in channelIdsToValidate)
        {
            if (string.IsNullOrWhiteSpace(channelId) ||
                await _contentService.GetChannelByIdAsync(channelId) is null ||
                !await authorization.CanManageChannelAsync(User, channelId))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string?> CreateVideoShortLinkAsync(string videoId)
    {
        try
        {
            var shortLink = await _linksService.EnsureVideoShortLinkAsync(
                videoId, HttpContext.GetChannelContext().ChannelId);
            return shortLink is null ? null : BuildShortLinkUrl(shortLink.Code);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Automatic short-link creation failed for imported video {VideoId}", videoId);
            return null;
        }
    }

    private string BuildShortLinkUrl(string code)
    {
        var siteUrl = configuration["SiteUrl"] ?? string.Empty;
        return $"{siteUrl}{code}";
    }

    #endregion
}
