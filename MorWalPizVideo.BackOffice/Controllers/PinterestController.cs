using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services.Interfaces;

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

public class PinterestController : ApplicationControllerBase
{
    private readonly IPinterestService pinterestService;
    private readonly string channelName;
    private readonly string siteUrl;
    private readonly PinterestSettings pinterestSettings;
    private readonly string scope = "pins:read_write";
    public PinterestController(IPinterestService _pinterestService, IConfiguration _configuration)
    {
        pinterestService = _pinterestService;

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
        var redirectUri = $"https://{Request.Host}/api/pinterest/callback";
        var token = await pinterestService.ExchangeCodeForTokenAsync(code, redirectUri);
        return Ok(token);
    }
    [HttpPost]
    public async Task<IActionResult> CreatePin(CreatePinterestPinRequest request)
    {
        var responseContent = await pinterestService.CreatePinAsync(
            request.Token, request.BoardId, request.Link, request.Title, request.Description, request.ImageUrl);

        return Ok(responseContent);
    }
}
