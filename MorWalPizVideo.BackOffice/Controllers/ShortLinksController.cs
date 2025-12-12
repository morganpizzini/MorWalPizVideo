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
        var shortlinks = await _dataService.FetchShortLinks();

        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(shortlinks.Select(x => ContractUtils.Convert(x, $"{siteUrl}")).ToList());
    }

    [HttpGet("{videoId}")]
    public async Task<IActionResult> GetShortLink(string videoId)
    {
        var shortlink = await _dataService.GetShortLinkByCode(videoId);

        if (shortlink == null)
        {
            return NotFound("No shortlink found for this video");
        }
        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(ContractUtils.Convert(shortlink, $"{siteUrl}"));
    }
    [HttpPost]
    public async Task<IActionResult> CreateShortLink(CreateShortLinkRequest request)
    {
        var existingMatch = await _dataService.FindMatch(request.Target);
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }

        var existingQueryLink =
            await _dataService.FetchQueryLinks(request.QueryLinkIds);

        var shortLinkCode = await CalculateShortLink();
        var shortlink = new ShortLink(shortLinkCode, request.Target,
            existingQueryLink);

        await _dataService.SaveShortLink(shortlink);

        var json = await client.ResetCache(CacheKeys.ShortLinks);

        var siteUrl = configuration.GetValue<string>("SiteUrl");

        if (!string.IsNullOrEmpty(request.Message))
        {
            await discordService.CreatePost(shortLinkCode, request.Message);
            await telegramService.CreatePost(shortLinkCode, request.Message);
        }

        return Ok($"{siteUrl}{shortlink.Code}");

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
        var existingShortLink = await _dataService.GetShortLink(request.Id);
        if (existingShortLink == null)
        {
            return NotFound("Short link not found");
        }
        
        var existingQueryLink = 
            await _dataService.FetchQueryLinks(request.Body.QueryLinkIds);
        
        var updatedShortLink = existingShortLink with { Target = request.Body.Target, 
            QueryLinks = existingQueryLink, 
            LinkType = request.Body.LinkType };

        await _dataService.UpdateShortlink(updatedShortLink);

        var json = await client.ResetCache(CacheKeys.ShortLinks);

        var siteUrl = configuration.GetValue<string>("SiteUrl");

        return Ok($"{siteUrl}{updatedShortLink.Code}");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShortLink(string id)
    {
        var entity = await _dataService.GetShortLink(id);
        if (entity == null)
            return BadRequest("Shortlink not found");

        await _dataService.DeleteShortLink(id);
        return NoContent();
    }
}
