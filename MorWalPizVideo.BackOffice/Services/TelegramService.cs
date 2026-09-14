using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Services;

public class TelegramServiceMock(ISelectedChannelPublishingConfigurationAccessor configurationAccessor) : ITelegramService
{
    public Task<string> CreatePost(string shortLink, string message)
    {
        configurationAccessor.Get("telegram");
        return Task.FromResult("");
    }
}
public class TelegramService : ITelegramService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly ISelectedChannelPublishingConfigurationAccessor configurationAccessor;
    private readonly string siteUrl;
    public TelegramService(
        IHttpClientFactory clientFactory,
        ISelectedChannelPublishingConfigurationAccessor configurationAccessor,
        IConfiguration configuration)
    {
        this.clientFactory = clientFactory;
        this.configurationAccessor = configurationAccessor;
        siteUrl = configuration["SiteUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(siteUrl))
            throw new InvalidOperationException("SiteUrl is empty");
    }
    public async Task<string> CreatePost(string shortLink, string message)
    {
        var settings = configurationAccessor.Get("telegram");
        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var request = new
        {
            chat_id = settings.DestinationId,
            text = $"{message} {youtubeUrl}"
        };

        var client = clientFactory.CreateClient(HttpClientNames.Telegram);
        var response = await client.PostAsJsonAsync(
            $"https://api.telegram.org/bot{settings.Credential}/sendMessage",
            request);

        return response.IsSuccessStatusCode ? string.Empty
                    : await response.Content.ReadAsStringAsync();
    }
}