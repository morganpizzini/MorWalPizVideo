using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Configuration;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MorWalPizVideo.BackOffice.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeyRateLimitingService _rateLimitingService;
    private readonly ApiKeySettings _apiKeySettings;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService,
        IApiKeyRateLimitingService rateLimitingService,
        IOptions<ApiKeySettings> apiKeySettings)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
        _rateLimitingService = rateLimitingService;
        _apiKeySettings = apiKeySettings.Value;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Extract API key from header
        if (!Request.Headers.TryGetValue(_apiKeySettings.HeaderName, out StringValues apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail($"Missing {_apiKeySettings.HeaderName} header");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // Validate API key
        var apiKey = await _apiKeyService.ValidateApiKeyAsync(providedApiKey);
        if (apiKey == null)
        {
            _logger.LogWarning("Invalid or expired API key attempted");
            return AuthenticateResult.Fail("Invalid or expired API Key");
        }

        // Check IP whitelist if enabled
        if (_apiKeySettings.EnableIpWhitelist && apiKey.AllowedIpAddresses.Any())
        {
            var clientIpAddress = GetClientIpAddress();
            if (!apiKey.AllowedIpAddresses.Contains(clientIpAddress))
            {
                _logger.LogWarning("API key {KeyName} attempted from unauthorized IP: {IpAddress}", 
                    apiKey.Name, clientIpAddress);
                return AuthenticateResult.Fail("API Key not authorized from this IP address");
            }
        }

        // Check rate limit
        var isAllowed = await _rateLimitingService.CheckRateLimitAsync(apiKey.Id!, apiKey.RateLimitPerMinute);
        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for API key: {KeyName}", apiKey.Name);
            Response.Headers.Append("Retry-After", "60");
            return AuthenticateResult.Fail("Rate limit exceeded");
        }

        // Record request
        await _rateLimitingService.RecordRequestAsync(apiKey.Id!);

        // Update last used timestamp (fire and forget)
        _ = _apiKeyService.UpdateLastUsedAsync(apiKey.Id!);

        // Create claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey.Id!),
            new Claim(ClaimTypes.Name, apiKey.Name),
            new Claim("ApiKeyId", apiKey.Id!),
            new Claim("ApiKeyName", apiKey.Name)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("API key authenticated: {KeyName}", apiKey.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";
        await Response.WriteAsync("{\"error\": \"Unauthorized\", \"message\": \"Valid API key required\"}");
    }

    private string GetClientIpAddress()
    {
        // Try to get the real IP from X-Forwarded-For header (for load balancers/proxies)
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var ips = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Try X-Real-IP header
        var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // Fallback to remote IP address
        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}