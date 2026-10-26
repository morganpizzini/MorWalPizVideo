using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Domain.Accounts;

namespace MorWalPizVideo.ServerAPI.Security;

public static class PublicAccountSecurity
{
    public const string Scheme = "PublicAccount";
    public const string SessionCookie = "public_account_session";
    public const string CsrfCookie = "public_account_csrf";
    public const string CsrfHeader = "X-CSRF-TOKEN";

    public static bool HasValidCsrf(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(CsrfHeader, out var header) ||
            !request.Cookies.TryGetValue(CsrfCookie, out var cookie))
            return false;

        var headerBytes = Encoding.UTF8.GetBytes(header.ToString());
        var cookieBytes = Encoding.UTF8.GetBytes(cookie);
        return headerBytes.Length == cookieBytes.Length &&
               CryptographicOperations.FixedTimeEquals(headerBytes, cookieBytes);
    }

    public static CookieOptions CreateCookieOptions(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
        IsEssential = true,
        Path = "/"
    };
}

public sealed class PublicAccountAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IPublicAccountService accountService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(PublicAccountSecurity.SessionCookie, out var token))
            return AuthenticateResult.NoResult();

        var session = await accountService.ValidateSessionAsync(token, Context.RequestAborted);
        if (session is null)
            return AuthenticateResult.Fail("Invalid or expired public account session");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.Account.Id),
            new Claim(ClaimTypes.Name, session.Account.Username),
            new Claim(ClaimTypes.Email, session.Account.Email)
        };
        var identity = new ClaimsIdentity(claims, PublicAccountSecurity.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), PublicAccountSecurity.Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
