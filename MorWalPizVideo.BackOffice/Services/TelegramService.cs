using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services.Configuration;
using MorWalPizVideo.BackOffice.Services.Factories;

namespace MorWalPizVideo.BackOffice.Services;

public class TelegramServiceMock : ITelegramService
{
    public Task<string> CreatePost(string shortLink, string message)
    {
        return Task.FromResult("");
    }
}
public class TelegramService : ITelegramService
{
    private readonly HttpClient client;
    private readonly string channelName;
    private readonly string siteUrl;
    public TelegramService(
        ITelegramHttpClientFactory clientFactory,
        ITelegramConfigurationService configurationService,
        IConfiguration configuration)
    {
        client = clientFactory.CreateClient();
        siteUrl = configuration["SiteUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(siteUrl))
            throw new InvalidOperationException("SiteUrl is empty");

        channelName = configurationService.GetTelegramSettings().ChannelName;
        if (string.IsNullOrWhiteSpace(channelName))
            throw new InvalidOperationException("Telegram channel name is not configured");
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
}