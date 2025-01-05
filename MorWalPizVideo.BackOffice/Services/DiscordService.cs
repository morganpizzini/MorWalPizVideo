using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;
public class DiscordService : IDiscordService, IDisposable
{
    private readonly HttpClient client;
    private readonly string channelName;
    private readonly string siteUrl;
    public DiscordService(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        client = _clientFactory.CreateClient("Discord");
        siteUrl = _configuration["SiteUrl"] ?? string.Empty;
        if (siteUrl == null)
            throw new NullReferenceException("SiteUrl is empty");

        channelName = _configuration.GetSection("DiscordSettings").Get<TelegramSettings>()?.ChannelName ?? string.Empty;
        if (channelName == null)
            throw new NullReferenceException("Channel name is not found in the configuration file");
    }

    public async Task<string> CreatePost(string shortLink, string message)
    {
        
        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var requestMessage = !string.IsNullOrEmpty(message) ? message : "Guarda il mio ultimo video:";

        var request = new
        {
            content = $"{requestMessage} {youtubeUrl}"
        };

        var response = await client.PostAsJsonAsync($"channels/{channelName}/messages", request);

        return response.IsSuccessStatusCode ? string.Empty
                : await response.Content.ReadAsStringAsync();

    }

    public void Dispose()
    {
        client?.Dispose();
    }
}
