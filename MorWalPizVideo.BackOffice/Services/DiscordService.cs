using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using System.Net.Http.Headers;

namespace MorWalPizVideo.BackOffice.Services;
public class DiscordServiceMock(ISelectedChannelPublishingConfigurationAccessor configurationAccessor) : IDiscordService
{
    public Task<string> CreatePost(string shortLink, string message)
    {
        configurationAccessor.Get("discord");
        return Task.FromResult("");
    }
}
public class DiscordService : IDiscordService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly ISelectedChannelPublishingConfigurationAccessor configurationAccessor;
    private readonly string siteUrl;
    public DiscordService(
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
        var settings = configurationAccessor.Get("discord");
        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var requestMessage = !string.IsNullOrEmpty(message) ? message : "Guarda il mio ultimo video:";

        var request = new
        {
            content = $"{requestMessage} {youtubeUrl}"
        };

        var client = clientFactory.CreateClient(HttpClientNames.Discord);
        using var requestMessageBody = new HttpRequestMessage(
            HttpMethod.Post,
            $"channels/{settings.DestinationId}/messages")
        {
            Content = JsonContent.Create(request)
        };
        requestMessageBody.Headers.Authorization = new AuthenticationHeaderValue("Bot", settings.Credential);
        var response = await client.SendAsync(requestMessageBody);

        return response.IsSuccessStatusCode ? string.Empty
                : await response.Content.ReadAsStringAsync();

    }
}
