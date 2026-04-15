using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Services;

public class FacebookServiceMock : IFacebookService
{
    public Task<string> CreatePost(string shortLink, string message)
    {
        return Task.FromResult("");
    }
}

public class FacebookService : IFacebookService, IDisposable
{
    private readonly HttpClient client;
    private readonly string pageId;
    private readonly string siteUrl;
    
    public FacebookService(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        client = _clientFactory.CreateClient(HttpClientNames.Facebook);
        siteUrl = _configuration["SiteUrl"] ?? string.Empty;
        if (string.IsNullOrEmpty(siteUrl))
            throw new NullReferenceException("SiteUrl is empty");

        pageId = _configuration.GetSection("FacebookSettings").Get<FacebookSettings>()?.PageId ?? string.Empty;
        if (string.IsNullOrEmpty(pageId))
            throw new NullReferenceException("PageId is not found in the configuration file");
    }

    public async Task<string> CreatePost(string shortLink, string message)
    {
        var youtubeUrl = $"{siteUrl}sl/{shortLink}";

        var requestMessage = !string.IsNullOrEmpty(message) ? message : "Guarda il mio ultimo video:";

        var request = new
        {
            message = $"{requestMessage} {youtubeUrl}"
        };

        var response = await client.PostAsJsonAsync($"{pageId}/feed", request);

        return response.IsSuccessStatusCode ? string.Empty
                    : await response.Content.ReadAsStringAsync();
    }

    public void Dispose()
    {
        client?.Dispose();
    }
}

public class FacebookSettings
{
    public string PageId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
