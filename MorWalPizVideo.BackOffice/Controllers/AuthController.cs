using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository, 
        IJwtService jwtService, 
        IRateLimitingService rateLimitingService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _rateLimitingService = rateLimitingService;
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
            
            return Unauthorized(new { 
                message = "Invalid credentials",
                remainingAttempts = rateLimitResult.RemainingAttempts - 1
            });
        }

        if (!user.IsActive)
        {
            await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username, false, userAgent, "Account disabled");
            
            return Unauthorized(new { 
                message = "Account is disabled",
                remainingAttempts = rateLimitResult.RemainingAttempts - 1
            });
        }

        // Successful authentication
        await _rateLimitingService.RecordLoginAttemptAsync(ipAddress, request.Username, true, userAgent, "");

        var token = _jwtService.GenerateToken(user);
        
        // Update last login
        var updatedUser = user with { LastLogin = DateTime.UtcNow };
        await _userRepository.UpdateItemAsync(updatedUser);

        _logger.LogInformation("Successful login for user {Username} from IP {IpAddress}", request.Username, ipAddress);

        // Set HttpOnly Secure SameSite cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Requires HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(24) // Match JWT expiry
        };
        Response.Cookies.Append("auth_token", token, cookieOptions);

        return Ok(new LoginResponse
        {
            Token = token, // Still return token for backward compatibility during transition
            User = new UserInfo
            {
                Id = user.Id!,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Clear the auth cookie
        Response.Cookies.Delete("auth_token");
        
        _logger.LogInformation("User logged out");
        
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("validate")]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var userId = _jwtService.ValidateToken(request.Token);
        
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        return Ok(new { userId });
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
    public string Token { get; init; } = string.Empty;
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
    public string Role { get; init; } = string.Empty;
}

public record ValidateTokenRequest
{
    public string Token { get; init; } = string.Empty;
}
