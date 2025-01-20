using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreatePinterestPinRequest
{
    public string Token { get; set; } = string.Empty;
    public string BoardId { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class PinterestController : ApplicationController
{
    private readonly IHttpClientFactory client;
    private readonly string channelName;
    private readonly string siteUrl;
    private readonly PinterestSettings pinterestSettings;
    private readonly string scope = "pins:read_write";
    public PinterestController(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        client = _clientFactory;

        siteUrl = _configuration["SiteUrl"] ?? string.Empty;
        if (siteUrl == null)
            throw new NullReferenceException($"{nameof(siteUrl)} is empty");

        pinterestSettings = _configuration.GetSection("PinterestSettings").Get<PinterestSettings>()!;
        if (pinterestSettings == null)
            throw new Exception("Cannot read configuration for Pinterest");

        channelName = _configuration.GetSection("TelegramSettings").Get<TelegramSettings>()?.ChannelName ?? string.Empty;
        if (channelName == null)
            throw new NullReferenceException("Channel name is not found in the configuration file");
    }

    [HttpGet]
    public IActionResult Login()
    {
        var redirectUri = $"https://{Request.Host}/api/pinterest/callback";
        var authUrl = $"https://www.pinterest.com/oauth/?response_type=code&redirect_uri={redirectUri}&client_id={pinterestSettings.AppId}&scope={scope}";
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code)
    {
        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", pinterestSettings.AppId),
            new KeyValuePair<string, string>("client_secret", pinterestSettings.AppSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", $"https://{Request.Host}/api/pinterest/callback")
        });

        var response = await client.PostAsync("https://api.pinterest.com/v5/oauth/token", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Salva il token di accesso
        var token = JsonSerializer.Deserialize<dynamic>(responseContent)!.access_token;
        return Ok(token);
    }
    [HttpPost]
    public async Task<IActionResult> CreatePin(CreatePinterestPinRequest request)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);

        var content = new StringContent(JsonSerializer.Serialize(new
        {
            board_id = request.BoardId,
            link = request.Link,
            title = request.Title,
            description = request.Description,
            media_source = new
            {
                source_type = "external",
                url = request.ImageUrl
            }
        }), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.pinterest.com/v5/pins", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        return Ok(responseContent);
    }
}
