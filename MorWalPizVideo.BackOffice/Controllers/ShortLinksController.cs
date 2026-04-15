using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
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
public class ShortLinksController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly ICrossApiService client;
    private readonly IConfiguration configuration;
    private readonly IDiscordService discordService;
    private readonly ITelegramService telegramService;
    public ShortLinksController(DataService dataService, ITelegramService telegramService, ICrossApiService clientFactory, IConfiguration configuration,
        IDiscordService discordService)
    {
        _dataService = dataService;
        client = clientFactory;
        this.configuration = configuration;
        this.discordService = discordService;
        this.telegramService = telegramService;
    }

    [HttpGet]
    public async Task<IActionResult> FetchShortLinks()
    {
        var allShortLinks = new List<ShortLink>();
        var siteUrl = configuration.GetValue<string>("SiteUrl");

        // Fetch short links from matches (YouTubeContent)
        var matches = await _dataService.FetchMatches();
        foreach (var match in matches)
        {
            allShortLinks.AddRange(match.ShortLinks);
        }

        // Fetch short links from channels (YTChannel)
        var channels = await _dataService.FetchChannels();
        foreach (var channel in channels)
        {
            allShortLinks.AddRange(channel.ShortLinks);
        }

        // Fetch standalone short links (exclude YouTubeVideo and YouTubeChannel to avoid duplication)
        var standaloneLinks = await _dataService.FetchShortLinks();
        allShortLinks.AddRange(standaloneLinks.Where(x => 
            x.LinkType != LinkType.YouTubeVideo && 
            x.LinkType != LinkType.YouTubeChannel));

        return Ok(allShortLinks.Select(x => ContractUtils.Convert(x, $"{siteUrl}")).ToList());
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetShortLink(string code)
    {
        var (shortLink, sourceType, owningEntity) = await FindShortLinkAsync(code);

        if (shortLink == null)
        {
            return NotFound("No shortlink found for this video");
        }
        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(ContractUtils.Convert(shortLink, $"{siteUrl}"));
    }
    [HttpPost]
    public async Task<IActionResult> CreateShortLink(CreateShortLinkRequest request)
    {
        var shortLinkCode = await CalculateShortLink();
        var existingQueryLink =
                    await _dataService.FetchQueryLinks(request.QueryLinkIds);
        var newShortLink = new ShortLink(shortLinkCode, request.Target, existingQueryLink);
        var siteUrl = configuration.GetValue<string>("SiteUrl");

        switch (request.LinkType)
        {
            case LinkType.YouTubeVideo:
                var existingMatch = await _dataService.FindMatch(request.Target);
                if (existingMatch == null)
                {
                    return BadRequest("Match do not exists");
                }
                var existingContentShortLink = existingMatch.ShortLinks
                    .FirstOrDefault(x => x.Target == newShortLink.Target 
                                                    && x.QueryString == newShortLink.QueryString);
                if (existingContentShortLink != null)
                {
                    return Ok($"{siteUrl}{existingContentShortLink.Code}");
                }
                newShortLink.LinkType = LinkType.YouTubeVideo;
                existingMatch = existingMatch.AddShortLink(newShortLink);
                await _dataService.UpdateMatch(existingMatch);
                break;
            case LinkType.YouTubeChannel:
                var existingChannel = await _dataService.FindChannel(request.Target);
                if (existingChannel == null)
                {
                    return BadRequest("Channel do not exists");
                }
                var exisintgShortLink = existingChannel.ShortLinks.FirstOrDefault(x => x.Target == newShortLink.Target
                                                    && x.QueryString == newShortLink.QueryString);
                if (exisintgShortLink != null)
                {
                    return Ok($"{siteUrl}{exisintgShortLink.Code}");
                }
                newShortLink.LinkType = LinkType.YouTubeChannel;
                existingChannel = existingChannel.AddShortLink(newShortLink);
                await _dataService.UpdateChannel(existingChannel);
                var channels = await _dataService.FetchChannels();
                break;
            default:
                newShortLink = await _dataService.SaveShortLink(newShortLink);
                break;
        }
        var json = await client.ResetCache(CacheKeys.ShortLinks);

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
            var shortlinks = await _dataService.FetchShortLinks();
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
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShortLink(BaseRequestId<UpdateShortLinkRequest> request)
    {
        // Search across all sources to find the existing short link
        var (existingShortLink, sourceType, owningEntity) = await FindShortLinkAsync(request.Id);
        
        if (existingShortLink == null)
        {
            return NotFound("Short link not found");
        }
        
        var existingQueryLink = 
            await _dataService.FetchQueryLinks(request.Body.QueryLinkIds);
        
        var updatedShortLink = existingShortLink with { 
            Target = request.Body.Target, 
            QueryLinks = existingQueryLink, 
            LinkType = request.Body.LinkType 
        };

        // Update based on the new LinkType and handle migration between types
        switch (request.Body.LinkType)
        {
            case LinkType.YouTubeVideo:
                updatedShortLink.LinkType = LinkType.YouTubeVideo;
                var existingMatch = await _dataService.FindMatch(request.Body.Target);
                if (existingMatch == null)
                {
                    return BadRequest("Match do not exists");
                }
                
                // Remove from old location if it's being migrated
                if (sourceType != ShortLinkSourceType.Match)
                {
                    await RemoveShortLinkFromSourceAsync(existingShortLink.Code, sourceType, owningEntity);
                    existingMatch.AddShortLink(updatedShortLink);
                }
                else
                {
                    existingMatch = (YouTubeContent)owningEntity!;
                    existingMatch = existingMatch.UpdateShortLink(existingShortLink.Code, updatedShortLink);
                }
                await _dataService.UpdateMatch(existingMatch);
                break;
                
            case LinkType.YouTubeChannel:
                updatedShortLink.LinkType = LinkType.YouTubeChannel;
                var existingChannel = await _dataService.GetChannel(request.Body.Target);
                if (existingChannel == null)
                {
                    return BadRequest("Channel do not exists");
                }
                
                // Remove from old location if it's being migrated
                if (sourceType != ShortLinkSourceType.Channel)
                {
                    await RemoveShortLinkFromSourceAsync(existingShortLink.Code, sourceType, owningEntity);
                    existingChannel.AddShortLink(updatedShortLink);
                }
                else
                {
                    existingChannel = (YTChannel)owningEntity!;
                    existingChannel = existingChannel.UpdateShortLink(existingShortLink.Code, updatedShortLink);
                }
                await _dataService.UpdateChannel(existingChannel);
                break;
                
            default:
                // Remove from old location if it's being migrated from match or channel
                if (sourceType != ShortLinkSourceType.Standalone)
                {
                    await RemoveShortLinkFromSourceAsync(existingShortLink.Code, sourceType, owningEntity);
                    await _dataService.SaveShortLink(updatedShortLink);
                }
                else
                {
                    await _dataService.UpdateShortlink(updatedShortLink);
                }
                break;
        }

        var json = await client.ResetCache(CacheKeys.ShortLinks);

        var siteUrl = configuration.GetValue<string>("SiteUrl");

        return Ok($"{siteUrl}{updatedShortLink.Code}");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShortLink(string id)
    {
        // Search across all sources to find the short link
        var (existingShortLink, sourceType, owningEntity) = await FindShortLinkAsync(id);
        
        if (existingShortLink == null)
            return NotFound("Short link not found");

        await RemoveShortLinkFromSourceAsync(existingShortLink.Code, sourceType, owningEntity);

        var json = await client.ResetCache(CacheKeys.ShortLinks);

        return NoContent();
    }

    #region Helper Methods

    private enum ShortLinkSourceType
    {
        Standalone,
        Match,
        Channel
    }

    /// <summary>
    /// Finds a short link by code across all sources (standalone, matches, channels).
    /// </summary>
    /// <returns>Tuple of (ShortLink, SourceType, OwningEntity)</returns>
    private async Task<(ShortLink? shortLink, ShortLinkSourceType sourceType, object? owningEntity)> FindShortLinkAsync(string code)
    {
        // Search in matches
        var matches = await _dataService.FetchMatches();
        foreach (var match in matches)
        {
            var matchShortLink = match.ShortLinks.FirstOrDefault(sl => sl.Code == code);
            if (matchShortLink != null)
            {
                return (matchShortLink, ShortLinkSourceType.Match, match);
            }
        }

        // Search in channels
        var channels = await _dataService.FetchChannels();
        foreach (var channel in channels)
        {
            var channelShortLink = channel.ShortLinks.FirstOrDefault(sl => sl.Code == code);
            if (channelShortLink != null)
            {
                return (channelShortLink, ShortLinkSourceType.Channel, channel);
            }
        }

        // Search in standalone repository
        var standaloneLink = await _dataService.GetShortLinkByCode(code);
        if (standaloneLink != null)
        {
            return (standaloneLink, ShortLinkSourceType.Standalone, null);
        }

        return (null, ShortLinkSourceType.Standalone, null);
    }

   
    /// <summary>
    /// Removes a short link from its source location.
    /// </summary>
    private async Task  RemoveShortLinkFromSourceAsync(string code, ShortLinkSourceType sourceType, object? owningEntity)
    {
        switch (sourceType)
        {
            case ShortLinkSourceType.Match:
                if (owningEntity is YouTubeContent match)
                {
                    var updatedMatch = match.RemoveShortLink(code);
                    await _dataService.UpdateMatch(updatedMatch);
                }
                break;
                
            case ShortLinkSourceType.Channel:
                if (owningEntity is YTChannel channel)
                {
                    var updatedChannel = channel.RemoveShortLink(code);
                    await _dataService.UpdateChannel(updatedChannel);
                }
                break;
                
            case ShortLinkSourceType.Standalone:
                // For standalone, we need the actual ID from the short link
                var standaloneLink = await _dataService.GetShortLinkByCode(code);
                if (standaloneLink != null)
                {
                    await _dataService.DeleteShortLink(standaloneLink.Id);
                }
                break;
        }
    }

    #endregion
}
