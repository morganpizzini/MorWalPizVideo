using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

public class DiscordController : ApplicationController
{
    private readonly IHttpClientFactory client;
    private readonly string channelName;
    private readonly string siteUrl;
    public DiscordController(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        client = _clientFactory;

        siteUrl = _configuration["SiteUrl"] ?? string.Empty;
        if (siteUrl == null)
            throw new NullReferenceException("SiteUrl is empty");

        channelName = _configuration.GetSection("DiscordSettings").Get<TelegramSettings>()?.ChannelName ?? string.Empty;
        if (channelName == null)
            throw new NullReferenceException("Channel name is not found in the configuration file");
    }

    [HttpGet("{shortLink}")]
    public async Task<IActionResult> CreatePost(string shortLink, [FromQuery(Name = "message")] string message)
    {
        using var client = this.client.CreateClient("Discord");

        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var requestMessage = !string.IsNullOrEmpty(message) ? message : "Guarda il mio ultimo video:";

        var request = new
        {
            content = $"{requestMessage} {youtubeUrl} 🔥🔫"
        };

        var response = await client.PostAsJsonAsync($"channels/{channelName}/messages", request);

        return response.IsSuccessStatusCode ? NoContent()
            : BadRequest(await response.Content.ReadAsStringAsync());

    }
}
