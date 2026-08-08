using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // ADR-002: explicit anonymous access (login/logout/validate must remain reachable without a prior token)
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IUserAccessResolver _userAccessResolver;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IJwtService jwtService,
        IRateLimitingService rateLimitingService,
        IUserAccessResolver userAccessResolver,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _rateLimitingService = rateLimitingService;
        _userAccessResolver = userAccessResolver;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Input validation
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username ?? "", false, userAgent, "Missing credentials");
            return BadRequest(new { message = "Username and password are required" });
        }

        // Check rate limiting before attempting authentication
        var rateLimitResult = await _rateLimitingService.CheckRateLimitAsync(ipAddress, request.Username);
        if (!rateLimitResult.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IpAddress} and user {Username}. Reason: {Reason}",
                ipAddress, request.Username, rateLimitResult.Reason);

            return new ObjectResult(new
            {
                message = rateLimitResult.Reason,
                retryAfter = rateLimitResult.RetryAfter?.TotalSeconds,
                remainingAttempts = 0
            })
            {
                StatusCode = 429 // Too Many Requests
            };
        }

        // Attempt authentication
        var user = await _userRepository.AuthenticateAsync(request.Username, request.Password);

        if (user == null)
        {
            await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username, false, userAgent, "Invalid credentials");

            return Unauthorized(new
            {
                message = "Invalid credentials",
                remainingAttempts = rateLimitResult.RemainingAttempts - 1
            });
        }

        if (!user.IsActive)
        {
            await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username, false, userAgent, "Account disabled");

            return Unauthorized(new
            {
                message = "Account is disabled",
                remainingAttempts = rateLimitResult.RemainingAttempts - 1
            });
        }

        var accessProfile = await _userAccessResolver.ResolveAsync(user.Id);
        if (accessProfile?.EffectivePermissions.Contains(AuthorizationPermissionKeys.BackofficeAccess) != true)
        {
            await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username, false, userAgent, "Missing backoffice permission");

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "You do not have access to the backoffice"
            });
        }

        // Successful authentication
        await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username, true, userAgent, "");

        var token = _jwtService.GenerateToken(user);

        // Update last login
        var updatedUser = user with { LastLogin = DateTime.UtcNow };
        await _userRepository.UpdateItemAsync(updatedUser);

        _logger.LogInformation("Successful login for user {Username} from IP {IpAddress}", request.Username, ipAddress);

        var expirationDays = double.Parse(HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["JwtSettings:ExpirationDays"] ?? "7");

        // The SPA and API use distinct HTTPS origins, so credentialed browser requests require SameSite=None.
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays),
            Path = "/"
        };
        Response.Cookies.Append("auth_token", token, cookieOptions);

        return Ok(new LoginResponse
        {
            User = new UserInfo
            {
                Id = user.Id!,
                Username = user.Username,
                Email = user.Email
            }
        });
    }

    [HttpGet("csrf")]
    public IActionResult IssueCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("logout")]
    [RequireCookieAntiforgery]
    public IActionResult Logout()
    {
        // Clear the auth cookie
        Response.Cookies.Delete("auth_token", new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });

        _logger.LogInformation("User logged out");

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("validate")]
    [RequireCookieAntiforgery]
    public async Task<IActionResult> ValidateToken()
    {
        var token = Request.Cookies["auth_token"];
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = _jwtService.ValidateToken(token);

        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var accessProfile = await _userAccessResolver.ResolveAsync(userId);

        return Ok(new
        {
            userId,
            effectivePermissions = accessProfile?.EffectivePermissions
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? []
        });
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

public record LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public UserInfo User { get; init; } = new();
}

public record ShopLoginResponse
{
    public string CustomerId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SessionToken { get; init; } = string.Empty;
}

public record UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
