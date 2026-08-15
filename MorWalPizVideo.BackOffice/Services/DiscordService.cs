using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services.Configuration;
using MorWalPizVideo.BackOffice.Services.Factories;

namespace MorWalPizVideo.BackOffice.Services;
public class DiscordServiceMock : IDiscordService
{
    public Task<string> CreatePost(string shortLink, string message)
    {
        return Task.FromResult("");
    }
}
public class DiscordService : IDiscordService
{
    private readonly HttpClient client;
    private readonly string channelName;
    private readonly string siteUrl;
    public DiscordService(
        IDiscordHttpClientFactory clientFactory,
        IDiscordConfigurationService configurationService,
        IConfiguration configuration)
    {
        client = clientFactory.CreateClient();
        siteUrl = configuration["SiteUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(siteUrl))
            throw new InvalidOperationException("SiteUrl is empty");

        channelName = configurationService.GetDiscordSettings().ChannelName;
        if (string.IsNullOrWhiteSpace(channelName))
            throw new InvalidOperationException("Discord channel name is not configured");
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
}
