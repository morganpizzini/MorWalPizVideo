using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Services;

public class PinterestServiceMock : IPinterestService
{
    public Task<string> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        return Task.FromResult("mock-pinterest-access-token");
    }

    public Task<string> CreatePinAsync(string accessToken, string boardId, string link, string title, string description, string imageUrl)
    {
        return Task.FromResult(string.Empty);
    }
}

public class PinterestService : IPinterestService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly PinterestSettings pinterestSettings;

    public PinterestService(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        clientFactory = _clientFactory;

        pinterestSettings = _configuration.GetSection("PinterestSettings").Get<PinterestSettings>()!;
        if (pinterestSettings == null)
            throw new Exception("Cannot read configuration for Pinterest");
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        var httpClient = clientFactory.CreateClient(HttpClientNames.Pinterest);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", pinterestSettings.AppId),
            new KeyValuePair<string, string>("client_secret", pinterestSettings.AppSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var response = await httpClient.PostAsync("oauth/token", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        var token = JsonSerializer.Deserialize<dynamic>(responseContent)!.access_token;
        return token;
    }

    public async Task<string> CreatePinAsync(string accessToken, string boardId, string link, string title, string description, string imageUrl)
    {
        var httpClient = clientFactory.CreateClient(HttpClientNames.Pinterest);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(JsonSerializer.Serialize(new
        {
            board_id = boardId,
            link,
            title,
            description,
            media_source = new
            {
                source_type = "external",
                url = imageUrl
            }
        }), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("pins", content);
        return await response.Content.ReadAsStringAsync();
    }
}
