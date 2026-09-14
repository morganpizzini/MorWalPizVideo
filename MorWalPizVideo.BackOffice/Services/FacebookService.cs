using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using System.Net.Http.Headers;

namespace MorWalPizVideo.BackOffice.Services;

public class FacebookServiceMock(ISelectedChannelPublishingConfigurationAccessor configurationAccessor) : IFacebookService
{
    public Task<string> CreatePost(string shortLink, string message)
    {
        configurationAccessor.Get("facebook");
        return Task.FromResult("");
    }
}

public class FacebookService : IFacebookService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly ISelectedChannelPublishingConfigurationAccessor configurationAccessor;
    private readonly string siteUrl;
    
    public FacebookService(
        IHttpClientFactory clientFactory,
        ISelectedChannelPublishingConfigurationAccessor configurationAccessor,
        IConfiguration configuration)
    {
        this.clientFactory = clientFactory;
        this.configurationAccessor = configurationAccessor;
        siteUrl = configuration["SiteUrl"] ?? string.Empty;
        if (string.IsNullOrEmpty(siteUrl))
            throw new InvalidOperationException("SiteUrl is empty");
    }

    public async Task<string> CreatePost(string shortLink, string message)
    {
        var settings = configurationAccessor.Get("facebook");
        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var requestMessage = !string.IsNullOrEmpty(message) ? message : "Guarda il mio ultimo video:";

        var request = new
        {
            message = $"{requestMessage} {youtubeUrl}"
        };

        var client = clientFactory.CreateClient(HttpClientNames.Facebook);
        using var requestMessageBody = new HttpRequestMessage(HttpMethod.Post, $"{settings.DestinationId}/feed")
        {
            Content = JsonContent.Create(request)
        };
        requestMessageBody.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.Credential);
        var response = await client.SendAsync(requestMessageBody);

        return response.IsSuccessStatusCode ? string.Empty
                    : await response.Content.ReadAsStringAsync();
    }
}
