using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;
public class ShortLinkRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ShortLinkController : ApplicationController
{
    private readonly IMongoDatabase database;
    private readonly IHttpClientFactory client;
    private readonly IConfiguration configuration;
    private readonly IDiscordService discordService;
    private readonly ITelegramService telegramService;
    public ShortLinkController(IMongoDatabase _database, ITelegramService _telegramService, IHttpClientFactory _clientFactory, IConfiguration _configuration,
        IDiscordService _discordService)
    {
        database = _database;
        client = _clientFactory;
        configuration = _configuration;
        discordService = _discordService;
        telegramService = _telegramService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShortLink()
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);

        var shortlinks = (await shortLinkCollection.FindAsync(x => true)).ToList();

        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(shortlinks.Select(item => $"{siteUrl}sl/{item.Code}   Counts: {item.ClicksCount}   VideoId: {item.VideoId}   QueryString: {item.QueryString}"));
    }

    [HttpGet("{videoId}")]
    public async Task<IActionResult> GetShortLink(string videoId)
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);

        var shortlinks = (await shortLinkCollection.FindAsync(x => x.VideoId == videoId)).ToList();

        if (shortlinks.Count == 0)
        {
            return BadRequest("No shortlink found for this video");
        }
        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(shortlinks.Select(item => $"{siteUrl}sl/{item.Code}   QueryString: {item.QueryString}"));
    }
    [HttpPost]
    public async Task<IActionResult> CreateShortLink(ShortLinkRequest request)
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);
        var matchCollection = database.GetCollection<Match>(DbCollections.Matches);

        var existingMatch = matchCollection.Find(x => x.ThumbnailUrl == request.VideoId || x.Videos.Any(v => v.YoutubeId == request.VideoId)).FirstOrDefault();
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }

        var shortLinkCode = await CalculateShortLink(shortLinkCollection);
        var shortlink = new ShortLink(shortLinkCode, request.VideoId, request.QueryString ?? string.Empty);

        await shortLinkCollection.InsertOneAsync(shortlink);

        using var client = this.client.CreateClient("MorWalPiz");
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.ShortLinks}");

        var siteUrl = configuration.GetValue<string>("SiteUrl");

        if (!string.IsNullOrEmpty(request.Message))
        {
            await discordService.CreatePost(shortLinkCode, request.Message);
            await telegramService.CreatePost(shortLinkCode, request.Message);
        }

        return Ok($"{siteUrl}sl/{shortlink.Code}");

        async Task<string> CalculateShortLink(IMongoCollection<ShortLink> shortLinkCollection)
        {
            var shortlinks = (await shortLinkCollection.FindAsync(_ => true)).ToList();

            var sl = shortlinks.Select(x => x.Code).ToList();

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
                string uniqueString = hash.Substring(0, 5);
                while (strings.Contains(uniqueString))
                {
                    uniqueString = GetUniqueValue([.. strings, uniqueString]);
                }

                return uniqueString;
            }
        }
    }
}
