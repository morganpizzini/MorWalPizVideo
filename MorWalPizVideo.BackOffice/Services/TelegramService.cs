using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Services;

public class TelegramService : ITelegramService,IDisposable
{
    private readonly HttpClient client;
    private readonly string channelName;
    private readonly string siteUrl;
    public TelegramService(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        client = _clientFactory.CreateClient(HttpClientNames.Telegram);
        siteUrl = _configuration["SiteUrl"] ?? string.Empty;
        if (siteUrl == null)
            throw new NullReferenceException("SiteUrl is empty");

        channelName = _configuration.GetSection("TelegramSettings").Get<TelegramSettings>()?.ChannelName ?? string.Empty;
        if (channelName == null)
            throw new NullReferenceException("Channel name is not found in the configuration file");
    }
    public async Task<string> CreatePost(string shortLink, string message)
    {
        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var request = new
        {
            chat_id = channelName,
            text = $"{message} {youtubeUrl}"
        };

        var response = await client.PostAsJsonAsync("", request);

        return response.IsSuccessStatusCode ? string.Empty
                    : await response.Content.ReadAsStringAsync();
    }

    public void Dispose()
    {
        client?.Dispose();
    }
}