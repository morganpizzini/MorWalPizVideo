using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPiz.Contracts;
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
    public string Target { get; set; } = string.Empty;
    public LinkType LinkType { get; set; } = LinkType.YouTubeVideo;
    public string QueryString { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // Per retrocompatibilità
    public string VideoId 
    { 
        get => Target;
        set => Target = value; 
    }
}
    public class ShortLinksController : ApplicationControllerBase
{
    private readonly IMongoDatabase database;
    private readonly IHttpClientFactory client;
    private readonly IConfiguration configuration;
    private readonly IDiscordService discordService;
    private readonly ITelegramService telegramService;
    public ShortLinksController(IMongoDatabase _database, ITelegramService _telegramService, IHttpClientFactory _clientFactory, IConfiguration _configuration,
        IDiscordService _discordService)
    {
        database = _database;
        client = _clientFactory;
        configuration = _configuration;
        discordService = _discordService;
        telegramService = _telegramService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShortLinks()
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);

        var shortlinks = (await shortLinkCollection.FindAsync(x => true)).ToList();

        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(shortlinks.Select(x=>ContractUtils.Convert(x,$"{siteUrl}sl")).ToList());
    }

    [HttpGet("{videoId}")]
    public async Task<IActionResult> GetShortLink(string videoId)
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);

        var shortlink = (await shortLinkCollection.FindAsync(x => x.Code == videoId)).FirstOrDefault();

        if (shortlink== null)
        {
            return NotFound("No shortlink found for this video");
        }
        var siteUrl = configuration.GetValue<string>("SiteUrl");
        return Ok(ContractUtils.Convert(shortlink, $"{siteUrl}sl"));
    }
    [HttpPost]
    public async Task<IActionResult> CreateShortLink(ShortLinkRequest request)
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);
        var matchCollection = database.GetCollection<YouTubeContent>(DbCollections.YouTubeContent);

        var existingMatch = matchCollection.Find(x => x.ContentId == request.VideoId || x.VideoRefs.Any(v => v.YoutubeId == request.VideoId)).FirstOrDefault();
        if (existingMatch == null)
        {
            return BadRequest("Match do not exists");
        }

        var shortLinkCode = await CalculateShortLink(shortLinkCollection);
        var shortlink = new ShortLink(shortLinkCode, request.VideoId, request.QueryString ?? string.Empty);

        await shortLinkCollection.InsertOneAsync(shortlink);

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
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

    [HttpPut("{shortLinkCode}")]
    public async Task<IActionResult> UpdateShortLink(string shortLinkCode, ShortLinkRequest request)
    {
        var shortLinkCollection = database.GetCollection<ShortLink>(DbCollections.ShortLinks);

        var existingShortLink = await shortLinkCollection.Find(x => x.Code == shortLinkCode).FirstOrDefaultAsync();
        if (existingShortLink == null)
        {
            return NotFound("Short link not found");
        }
        existingShortLink = existingShortLink with { Target = request.VideoId, QueryString = request.QueryString ?? string.Empty };
        
        var updateDefinition = Builders<ShortLink>.Update
            .Set(sl => sl.VideoId, existingShortLink.VideoId)
            .Set(sl => sl.QueryString, existingShortLink.QueryString);

        await shortLinkCollection.UpdateOneAsync(x => x.Code == shortLinkCode, updateDefinition);

        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        var json = await client.GetStringAsync($"cache/reset?k={CacheKeys.ShortLinks}");

        var siteUrl = configuration.GetValue<string>("SiteUrl");

        return Ok($"{siteUrl}sl/{existingShortLink.Code}");
    }
}
