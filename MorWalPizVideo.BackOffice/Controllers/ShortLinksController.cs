using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateShortLinkRequest
{
    [Required]
    public string Target { get; set; } = string.Empty;
    public LinkType LinkType { get; set; } = LinkType.YouTubeVideo;
    public string[] QueryLinkIds { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}
public class UpdateShortLinkRequest
{
    [Required]
    public string Target { get; set; } = string.Empty;
    public LinkType LinkType { get; set; } = LinkType.YouTubeVideo;
    public string[] QueryLinkIds { get; set; } = [];
}
[RequireChannelScope]
public class ShortLinksController : ApplicationControllerBase
{
    private readonly ILinksService _linksService;
    private readonly IContentService _contentService;
    private readonly ICrossApiService client;
    private readonly IConfiguration configuration;
    private readonly IDiscordService discordService;
    private readonly ITelegramService telegramService;
    private readonly IVideoAuthorizationService _authorization;
    public ShortLinksController(ILinksService linksService, IContentService contentService, ITelegramService telegramService, ICrossApiService clientFactory, IConfiguration configuration,
        IDiscordService discordService, IVideoAuthorizationService authorization)
    {
        _linksService = linksService;
        _contentService = contentService;
        client = clientFactory;
        this.configuration = configuration;
        this.discordService = discordService;
        this.telegramService = telegramService;
        _authorization = authorization;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksView, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<IActionResult> FetchShortLinks()
    {
        var siteUrl = configuration.GetValue<string>("SiteUrl");
        var standaloneLinks = await _linksService.GetShortLinksAsync();
        var matches = await _contentService.GetAllMatchesAsync();
        var channels = await _contentService.GetChannelsAsync();
        var selectedChannelId = HttpContext.GetChannelContext().ChannelId;
        var scopedMatches = matches.Where(match => IsMatchInSelectedChannel(match, selectedChannelId)).ToList();
        var scopedChannels = channels.Where(channel => channel.ChannelId == selectedChannelId).ToList();
        var embeddedLinks = scopedMatches.SelectMany(match => match.ShortLinks)
            .Where(link => link.LinkType == LinkType.YouTubeVideo &&
                matchHasVideoTarget(scopedMatches, link))
            .ToList();
        standaloneLinks = standaloneLinks
            .Where(link => link.LinkType != LinkType.YouTubeVideo &&
                IsStandaloneLinkInSelectedChannel(link, selectedChannelId, scopedMatches))
            .ToList();
        var visibleLinks = await FilterAuthorizedLinksAsync(
            standaloneLinks.Concat(embeddedLinks).ToList(), scopedMatches, scopedChannels);

        return Ok(visibleLinks.Select(x => ContractUtils.Convert(x, $"{siteUrl}")).ToList());

        static bool matchHasVideoTarget(IEnumerable<YouTubeContent> content, ShortLink link) =>
            content.Any(match => match.ShortLinks.Any(candidate => candidate.MatchesCode(link.Code)) &&
                match.VideoRefs.Any(video => video.YoutubeId == link.Target));
    }

    [HttpGet("{code}")]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksView, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<IActionResult> GetShortLink(string code)
    {
        var (shortLink, sourceType, owningEntity) = await FindShortLinkAsync(code);

        if (shortLink == null || !await CanAccessShortLinkAsync(shortLink))
        {
            return NotFound("No shortlink found for this video");
        }
        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(ContractUtils.Convert(shortLink, $"{siteUrl}"));
    }
    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksCreate, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<IActionResult> CreateShortLink(CreateShortLinkRequest request)
    {
        if (request.LinkType is not LinkType.YouTubeVideo and not LinkType.YouTubeChannel && !await _authorization.IsAdminAsync(User))
        {
            return Forbid();
        }

        var shortLinkCode = await CalculateShortLink();
        if (!await _linksService.IsCodeAvailableAsync(shortLinkCode))
        {
            return Conflict("A shortlink with this code already exists");
        }
        var existingQueryLink =
                await _linksService.GetQueryLinksAsync(request.QueryLinkIds);

        if (!IsSafeShortLinkTarget(request.Target, request.LinkType))
        {
            return BadRequest("Target must be a safe URL or supported reference");
        }

        var newShortLink = new ShortLink(ShortLink.NormalizeCode(shortLinkCode), request.Target, existingQueryLink)
        {
            ManagementChannelId = HttpContext.GetChannelContext().ChannelId
        };
        var siteUrl = configuration.GetValue<string>("SiteUrl");

        switch (request.LinkType)
        {
            case LinkType.YouTubeVideo:
                var existingMatch = await _contentService.FindMatchAsync(request.Target);
                if (existingMatch == null || !await _authorization.CanAccessAsync(User, existingMatch))
                {
                    return BadRequest("Match do not exists");
                }
                var existingContentShortLink = existingMatch.ShortLinks
                    .FirstOrDefault(x => x.LinkType == LinkType.YouTubeVideo
                        && x.Target == newShortLink.Target
                        && x.QueryString == newShortLink.QueryString);
                if (existingContentShortLink != null)
                {
                    return Ok($"{siteUrl}{existingContentShortLink.Code}");
                }
                newShortLink.LinkType = LinkType.YouTubeVideo;
                await _contentService.UpdateMatchAsync(existingMatch.AddShortLink(newShortLink));
                break;
            case LinkType.YouTubeChannel:
                var existingChannel = await _contentService.FindChannelAsync(request.Target);
                if (existingChannel == null || existingChannel.ChannelId != HttpContext.GetChannelContext().ChannelId ||
                    !await _authorization.CanManageChannelAsync(User, existingChannel.ChannelId))
                {
                    return BadRequest("Channel do not exists");
                }
                var exisintgShortLink = (await _linksService.GetShortLinksAsync())
                    .FirstOrDefault(x => x.LinkType == LinkType.YouTubeChannel
                                                    && x.ChannelId == existingChannel.ChannelId
                                                    && x.Target == newShortLink.Target
                                                    && x.QueryString == newShortLink.QueryString);
                if (exisintgShortLink != null)
                {
                    return Ok($"{siteUrl}{exisintgShortLink.Code}");
                }
                newShortLink.LinkType = LinkType.YouTubeChannel;
                newShortLink.ChannelId = existingChannel.ChannelId;
                newShortLink = await _linksService.SaveShortLinkAsync(newShortLink);
                break;
            case LinkType.CustomUrl:
                if (!IsSafeAbsoluteHttpUrl(request.Target))
                {
                    return BadRequest("Target must be an absolute http or https URL");
                }
                newShortLink = await _linksService.SaveShortLinkAsync(newShortLink);
                break;
            default:
                newShortLink = await _linksService.SaveShortLinkAsync(newShortLink);
                break;
        }
        var json = await client.ResetCache(CacheKeys.ShortLinks);
        if (request.LinkType == LinkType.YouTubeVideo)
        {
            await client.ResetCache(CacheKeys.Matches);
            await client.PurgeCache(ApiTagCacheKeys.Matches);
        }

        if (!string.IsNullOrEmpty(request.Message))
        {
            await discordService.CreatePost(shortLinkCode, request.Message);
            await telegramService.CreatePost(shortLinkCode, request.Message);
        }

        // Ensure the code is not null or empty before building the URL
        if (string.IsNullOrEmpty(newShortLink.Code))
        {
            return StatusCode(500, "Failed to generate short link code");
        }

        return Ok($"{siteUrl}{newShortLink.Code}");

        async Task<string> CalculateShortLink()
        {
            var shortlinks = await _linksService.GetShortLinksAsync();
            var matches = await _contentService.GetAllMatchesAsync();
            var sl = shortlinks
                .Select(x => x.NormalizedCode)
                .Concat(matches.SelectMany(match => match.ShortLinks)
                    .Where(x => x.LinkType == LinkType.YouTubeVideo)
                    .Select(x => x.NormalizedCode))
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
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksUpdate, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<IActionResult> UpdateShortLink(BaseRequestId<UpdateShortLinkRequest> request)
    {
        // Search across all sources to find the existing short link
        var (existingShortLink, sourceType, owningEntity) = await FindShortLinkAsync(request.Id);

        if (existingShortLink == null || !await CanAccessShortLinkAsync(existingShortLink))
        {
            return NotFound("Short link not found");
        }

        if (request.Body.LinkType is not LinkType.YouTubeVideo and not LinkType.YouTubeChannel && !await _authorization.IsAdminAsync(User))
        {
            return Forbid();
        }

        var existingQueryLink =
            await _linksService.GetQueryLinksAsync(request.Body.QueryLinkIds);

        if (!IsSafeShortLinkTarget(request.Body.Target, request.Body.LinkType))
        {
            return BadRequest("Target must be a safe URL or supported reference");
        }

        var updatedShortLink = existingShortLink with
        {
            Target = request.Body.Target,
            QueryLinks = existingQueryLink,
            LinkType = request.Body.LinkType
        };

        // All link types now converge on the canonical standalone collection (ADR-004): links still
        // embedded on a match/channel are migrated out on update rather than re-embedded.
        switch (request.Body.LinkType)
        {
            case LinkType.YouTubeVideo:
                var existingMatch = await _contentService.FindMatchAsync(request.Body.Target);
                if (existingMatch == null || !await _authorization.CanAccessAsync(User, existingMatch))
                {
                    return BadRequest("Match do not exists");
                }
                updatedShortLink.LinkType = LinkType.YouTubeVideo;
                updatedShortLink.ContentId = existingMatch.Id;
                updatedShortLink.ChannelId = null;
                if (sourceType == ShortLinkSourceType.Standalone)
                {
                    await _linksService.DeleteShortLinkAsync(existingShortLink.Id);
                    await _contentService.UpdateMatchAsync(existingMatch.AddShortLink(updatedShortLink));
                }
                else
                {
                    await MigrateToCanonicalAsync(existingShortLink.Code, sourceType, owningEntity, updatedShortLink);
                }
                break;

            case LinkType.YouTubeChannel:
                var existingChannel = await _contentService.FindChannelAsync(request.Body.Target);
                if (existingChannel == null || existingChannel.ChannelId != HttpContext.GetChannelContext().ChannelId ||
                    !await _authorization.CanManageChannelAsync(User, existingChannel.ChannelId))
                {
                    return BadRequest("Channel do not exists");
                }
                updatedShortLink.LinkType = LinkType.YouTubeChannel;
                updatedShortLink.ChannelId = existingChannel.ChannelId;
                updatedShortLink.ContentId = null;
                await MigrateToCanonicalAsync(existingShortLink.Code, sourceType, owningEntity, updatedShortLink);
                break;

            default:
                if (request.Body.LinkType == LinkType.CustomUrl && !IsSafeAbsoluteHttpUrl(request.Body.Target))
                {
                    return BadRequest("Target must be an absolute http or https URL");
                }
                updatedShortLink.ContentId = null;
                updatedShortLink.ChannelId = null;
                await MigrateToCanonicalAsync(existingShortLink.Code, sourceType, owningEntity, updatedShortLink);
                break;
        }

        var json = await client.ResetCache(CacheKeys.ShortLinks);
        if (sourceType == ShortLinkSourceType.Match || request.Body.LinkType == LinkType.YouTubeVideo)
        {
            await client.ResetCache(CacheKeys.Matches);
            await client.PurgeCache(ApiTagCacheKeys.Matches);
        }

        var siteUrl = configuration.GetValue<string>("SiteUrl");

        return Ok($"{siteUrl}{updatedShortLink.Code}");
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ShortLinksDelete, AuthorizationPermissionKeys.ShortLinksManage)]
    public async Task<IActionResult> DeleteShortLink(string id)
    {
        // Search across all sources to find the short link
        var (existingShortLink, sourceType, owningEntity) = await FindShortLinkAsync(id);

        if (existingShortLink == null || !await CanAccessShortLinkAsync(existingShortLink))
            return NotFound("Short link not found");

        await RemoveShortLinkFromSourceAsync(existingShortLink.Code, sourceType, owningEntity);

        var json = await client.ResetCache(CacheKeys.ShortLinks);
        if (sourceType == ShortLinkSourceType.Match)
        {
            await client.ResetCache(CacheKeys.Matches);
            await client.PurgeCache(ApiTagCacheKeys.Matches);
        }

        return NoContent();
    }

    #region Helper Methods

    private enum ShortLinkSourceType { Standalone, Match }

    /// <summary>Finds a short link in the canonical standalone or legacy embedded collections.</summary>
    private async Task<(ShortLink? shortLink, ShortLinkSourceType sourceType, object? owningEntity)> FindShortLinkAsync(string code)
    {
        var selectedChannelId = HttpContext.GetChannelContext().ChannelId;
        var standaloneLink = await _linksService.GetByCodeAsync(code);
        if (standaloneLink != null && standaloneLink.LinkType != LinkType.YouTubeVideo && IsStandaloneLinkInSelectedChannel(
                standaloneLink,
                selectedChannelId,
                await _contentService.GetAllMatchesAsync()))
        {
            return (standaloneLink, ShortLinkSourceType.Standalone, null);
        }

        var match = (await _contentService.GetAllMatchesAsync())
            .FirstOrDefault(item => IsMatchInSelectedChannel(item, selectedChannelId) &&
                                    item.ShortLinks.Any(link => link.LinkType == LinkType.YouTubeVideo &&
                                        link.MatchesCode(code) && item.VideoRefs.Any(video => video.YoutubeId == link.Target)));
        if (match != null)
        {
            return (match.ShortLinks.First(link => link.LinkType == LinkType.YouTubeVideo &&
                link.MatchesCode(code) && match.VideoRefs.Any(video => video.YoutubeId == link.Target)),
                ShortLinkSourceType.Match, match);
        }

        return (null, ShortLinkSourceType.Standalone, null);
    }

    private async Task<IList<ShortLink>> FilterAuthorizedLinksAsync(
        IList<ShortLink> links,
        IList<YouTubeContent> matches,
        IList<YTChannel> channels)
    {
        if (await _authorization.IsAdminAsync(User))
        {
            return links;
        }

        var selectedChannelId = HttpContext.GetChannelContext().ChannelId;
        var visibleLinks = new List<ShortLink>();
        foreach (var link in links)
        {
            if (link.LinkType == LinkType.YouTubeVideo)
            {
                var match = matches.FirstOrDefault(candidate =>
                    IsMatchInSelectedChannel(candidate, selectedChannelId) &&
                    candidate.ShortLinks.Any(candidateLink => candidateLink.MatchesCode(link.Code) &&
                        candidate.VideoRefs.Any(video => video.YoutubeId == candidateLink.Target)));
                if (match != null && await _authorization.CanAccessAsync(User, match))
                {
                    visibleLinks.Add(link);
                }
            }
            else if (link.LinkType == LinkType.YouTubeChannel && !string.IsNullOrWhiteSpace(link.ChannelId))
            {
                if (await _authorization.CanManageChannelAsync(User, link.ChannelId))
                {
                    visibleLinks.Add(link);
                }
            }
            else if (string.Equals(link.ManagementChannelId, selectedChannelId, StringComparison.Ordinal) &&
                await _authorization.CanManageChannelAsync(User, selectedChannelId))
            {
                visibleLinks.Add(link);
            }
        }

        return visibleLinks;
    }

    private async Task<bool> CanAccessShortLinkAsync(ShortLink link)
    {
        var selectedChannelId = HttpContext.GetChannelContext().ChannelId;
        var matches = await _contentService.GetAllMatchesAsync();
        var isInSelectedChannel = link.LinkType != LinkType.YouTubeVideo &&
            IsStandaloneLinkInSelectedChannel(link, selectedChannelId, matches) ||
            matches.Any(match => IsMatchInSelectedChannel(match, selectedChannelId) &&
                match.ShortLinks.Any(candidate => candidate.LinkType == LinkType.YouTubeVideo &&
                    candidate.MatchesCode(link.Code) && match.VideoRefs.Any(video => video.YoutubeId == candidate.Target)));
        if (!isInSelectedChannel)
        {
            return false;
        }

        if (await _authorization.IsAdminAsync(User))
        {
            return true;
        }

        if (link.LinkType == LinkType.YouTubeChannel && !string.IsNullOrWhiteSpace(link.ChannelId))
        {
            return await _authorization.CanManageChannelAsync(User, link.ChannelId);
        }

        if (link.LinkType != LinkType.YouTubeVideo)
        {
            return await _authorization.CanManageChannelAsync(User, selectedChannelId);
        }

        var match = (await _contentService.GetAllMatchesAsync())
            .FirstOrDefault(item => IsMatchInSelectedChannel(item, selectedChannelId) &&
                item.ShortLinks.Any(candidate => candidate.LinkType == LinkType.YouTubeVideo &&
                    candidate.MatchesCode(link.Code) && item.VideoRefs.Any(video => video.YoutubeId == candidate.Target)));
        return match != null && await _authorization.CanAccessAsync(User, match);
    }

    private async Task MigrateToCanonicalAsync(string code, ShortLinkSourceType sourceType, object? owningEntity, ShortLink updatedShortLink)
    {
        switch (sourceType)
        {
            case ShortLinkSourceType.Standalone:
                await _linksService.UpdateShortLinkAsync(updatedShortLink);
                break;
            case ShortLinkSourceType.Match when owningEntity is YouTubeContent match:
                if (updatedShortLink.LinkType == LinkType.YouTubeVideo &&
                    match.VideoRefs.Any(video => video.YoutubeId == updatedShortLink.Target))
                {
                    await _contentService.UpdateMatchAsync(match.UpdateShortLink(code, updatedShortLink));
                }
                else
                {
                    await _contentService.UpdateMatchAsync(match.RemoveShortLink(code));
                    await _linksService.SaveShortLinkAsync(updatedShortLink with
                    {
                        ManagementChannelId = HttpContext.GetChannelContext().ChannelId
                    });
                }
                break;
        }
    }

    private static bool IsSafeAbsoluteHttpUrl(string target) =>
        Uri.TryCreate(target, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsStandaloneLinkInSelectedChannel(
        ShortLink link,
        string selectedChannelId,
        IEnumerable<YouTubeContent> matches)
    {
        if (string.Equals(link.ManagementChannelId, selectedChannelId, StringComparison.Ordinal))
        {
            return true;
        }

        if (link.LinkType == LinkType.YouTubeChannel)
        {
            return string.Equals(link.ChannelId, selectedChannelId, StringComparison.Ordinal);
        }

        return link.LinkType != LinkType.YouTubeVideo;
    }

    private static bool IsMatchInSelectedChannel(YouTubeContent match, string selectedChannelId) =>
        string.Equals(match.OwnerChannelId, selectedChannelId, StringComparison.Ordinal) ||
        match.VideoRefs.Any(video => video.ChannelIds.Contains(selectedChannelId, StringComparer.Ordinal));

    private static bool IsSafeShortLinkTarget(string target, LinkType linkType)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        if (linkType is LinkType.YouTubeVideo or LinkType.YouTubeChannel)
        {
            return true;
        }

        if (linkType == LinkType.CustomUrl)
        {
            return IsSafeAbsoluteHttpUrl(target);
        }

        if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        return !target.Any(char.IsWhiteSpace);
    }


    /// <summary>
    /// Removes a short link from its source location.
    /// </summary>
    private async Task RemoveShortLinkFromSourceAsync(string code, ShortLinkSourceType sourceType, object? owningEntity)
    {
        if (sourceType == ShortLinkSourceType.Standalone)
        {
            var standaloneLink = await _linksService.GetByCodeAsync(code);
            if (standaloneLink != null)
            {
                await _linksService.DeleteShortLinkAsync(standaloneLink.Id);
            }
            return;
        }

        switch (sourceType)
        {
            case ShortLinkSourceType.Match when owningEntity is YouTubeContent match:
                await _contentService.UpdateMatchAsync(match with
                {
                    ShortLinks = match.ShortLinks.Where(link => !link.MatchesCode(code)).ToArray()
                });
                break;
        }
    }

    #endregion
}
