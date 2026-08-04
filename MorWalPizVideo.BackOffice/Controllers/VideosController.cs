using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts.Videos;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.Security.Cryptography;
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
    
    public VideosController(IContentService contentService, ILinksService linksService, ICrossApiService _clientFactory,
        IYTService _yTService, IExternalDataService _externalDataService,
        ITelegramService _telegramService, IDiscordService _discordService,
        IFacebookService _facebookService)
    {
        _contentService = contentService;
        _linksService = linksService;
        client = _clientFactory;
        yTService = _yTService;
        externalDataService = _externalDataService;
        telegramService = _telegramService;
        discordService = _discordService;
        facebookService = _facebookService;
    }

    [HttpGet()]
    public async Task<IActionResult> Fetch()
    {
        var matches = await _contentService.GetAllMatchesAsync();
        return Ok(matches.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(BaseRequestId request)
    {
        var match = await _contentService.GetMatchByIdAsync(request.Id);
        if(match == null)
        {
            return NotFound();
        }
        return Ok(ContractUtils.Convert(match));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VideoUpdateRequest request)
    {
        var existingMatch = await _contentService.FindMatchAsync(id);
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
            Categories = categories
        };

        await _contentService.UpdateMatchAsync(updatedMatch);

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();

        return NoContent();
    }

    [HttpPost("Translate")]
    public async Task TranslateShort(IList<string> videoIds)
    {
        await yTService.TranslateYoutubeVideo(videoIds);
    }
    [HttpPost("ImportVideo")]
    public async Task<IActionResult> Import(VideoImportRequest request)
    {
        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        await _contentService.SaveMatchAsync(YouTubeContent.CreateSingleVideo(request.VideoId, categories));

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
    [HttpPost("ConvertIntoRoot")]
    public async Task<IActionResult> ConvertIntoRoot(RootCreationRequest request)
    {
        var existingMatch = await _contentService.FindMatchAsync(request.VideoId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        if (!existingMatch.IsLink)
        {
            return BadRequest("Match is already a root");
        }

        // Preserve existing metadata from the single video's VideoRef
        var existingVideoRef = existingMatch.VideoRefs?.FirstOrDefault();
        
        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        var newMatch = YouTubeContent.CreateCollection(
            existingMatch.Id,
            request.Title,
            request.Description,
            request.Url,
            existingMatch.ThumbnailVideoId,
            categories
        );

        // Add video with preserved metadata if available
        if (existingVideoRef != null)
        {
            newMatch = newMatch.AddVideo(
                existingVideoRef.YoutubeId,
                existingVideoRef.Categories,
                existingVideoRef.Title,
                existingVideoRef.Description,
                existingVideoRef.PublishedAt
            );
        }
        else
        {
            // Fallback to basic method if no metadata available
            newMatch = newMatch.AddVideo(existingMatch.ThumbnailVideoId, existingMatch.Categories);
        }

        await _contentService.UpdateMatchAsync(newMatch);

        // Populate any missing metadata by calling ExternalDataService.FetchMatches()
        await externalDataService.FetchMatches();

        return NoContent();
    }

    [HttpPost("SwapThumbnailId")]
    public async Task<IActionResult> SwapThumbnailUrl(SwapRootThumbnailRequest request)
    {
        var existingMatch = await _contentService.FindMatchAsync(request.CurrentVideoId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }
        if (existingMatch.IsLink)
        {
            return BadRequest("Match is not a root match");
        }

        var updatedMatch = existingMatch.WithThumbnail(request.NewVideoId);

        await _contentService.UpdateMatchAsync(updatedMatch);
        return NoContent();
    }

    [HttpPost("RootCreation")]
    public async Task<IActionResult> RootCreation(RootCreationRequest request)
    {
        // Fetch categories and convert to CategoryRef objects
        var categories = (await _contentService.GetCategoriesAsync(request.Categories))
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();

        await _contentService.SaveMatchAsync(YouTubeContent.CreateCollection(
            request.VideoId,
            request.Title,
            request.Description,
            request.Url,
            request.VideoId,
            categories
        ));
        return NoContent();
    }
    [HttpPost("ImportSubCreation")]
    public async Task<IActionResult> SubVideoCreation(SubVideoCrationRequest request)
    {
        var existingMatch = await _contentService.FindMatchAsync(request.MatchId);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }

        // Fetch category and convert to CategoryRef object
        var categoryEntities = await _contentService.GetCategoriesAsync(request.Categories);
        var categories = categoryEntities
            .Select(x => new CategoryRef(x.Id, x.Title))
            .ToArray();
        

        var updatedMatch = existingMatch.AddVideo(request.VideoId, categories);
        await _contentService.UpdateMatchAsync(updatedMatch);

        // Populate metadata by calling ExternalDataService.FetchMatches()
        // This will fetch YouTube metadata and update the VideoRef with title, description, publishedAt
        await externalDataService.FetchMatches();

        // Auto-create shortlink for the sub-video
        await CreateVideoShortLinkAsync(request.VideoId);

        return NoContent();
    }

    [HttpPost("{id}/refresh-youtube")]
    public async Task<IActionResult> RefreshYouTubeData(string id)
    {
        var updatedMatch = await externalDataService.RefreshMatch(id);
        if (updatedMatch == null)
        {
            return NotFound("Video not found");
        }

        await client.ResetCache(CacheKeys.Matches);
        await client.PurgeCache(ApiTagCacheKeys.Matches);
        await client.ReloadCache();

        return Ok(ContractUtils.Convert(updatedMatch));
    }

    [HttpPost("{id}/publish-social")]
    public async Task<IActionResult> PublishToSocialMedia(string id, [FromBody] PublishSocialRequest request)
    {
        var match = await _contentService.FindMatchAsync(id);
        if (match == null)
        {
            return NotFound("Video not found");
        }

        // Get the shortlink for this video
        var shortLink = match.ShortLinks
            .FirstOrDefault(x => x.Target == id && string.IsNullOrEmpty(x.QueryString));

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

        var allChannels = await _contentService.GetChannelsAsync();
        var owningChannels = allChannels
            .Where(c => c.Videos != null && c.Videos.Any(v => v.VideoId == youtubeId))
            .ToList();
        var existsInMatches = (await _contentService.FindMatchAsync(youtubeId)) != null;

        if (owningChannels.Count == 0 && !existsInMatches)
        {
            return NotFound(new { error = $"Video '{youtubeId}' was not found in any channel or match" });
        }

        // Remove from every other owning channel (idempotent: skips target).
        foreach (var c in owningChannels.Where(c => c.ChannelId != payload.ChannelId))
        {
            var trimmedVideos = c.Videos.Where(v => v.VideoId != youtubeId).ToList();
            var updated = c with { Videos = trimmedVideos };
            await _contentService.UpdateChannelAsync(updated);
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

    /// <summary>
    /// Auto-creates a shortlink for a video (similar to ShortLinksController logic).
    /// </summary>
    private async Task CreateVideoShortLinkAsync(string videoId)
    {
        var existingMatch = await _contentService.FindMatchAsync(videoId);
        if (existingMatch == null)
        {
            return;
        }

        // Check if a canonical shortlink already exists for this video (ADR-004: standalone aggregate).
        var existingShortLink = (await _linksService.GetShortLinksAsync())
            .FirstOrDefault(x => x.LinkType == LinkType.YouTubeVideo
                && x.ContentId == existingMatch.Id
                && x.Target == videoId
                && string.IsNullOrEmpty(x.QueryString));

        if (existingShortLink != null)
        {
            return; // Shortlink already exists
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
