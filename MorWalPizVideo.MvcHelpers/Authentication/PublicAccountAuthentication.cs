using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Domain.Accounts;

namespace MorWalPizVideo.MvcHelpers.Authentication;

public sealed class PublicAccountAuthenticationOptions : AuthenticationSchemeOptions
{
    public string SessionCookieName { get; set; } = PublicAccountSecurity.SessionCookie;
}

public static class PublicAccountSecurity
{
    public const string Scheme = "PublicAccount";
    public const string SessionCookie = "public_account_session";
    public const string CsrfCookie = "public_account_csrf";
    public const string CsrfHeader = "X-CSRF-TOKEN";

    public static bool HasValidCsrf(
        HttpRequest request,
        string csrfCookieName = CsrfCookie,
        string csrfHeaderName = CsrfHeader)
    {
        if (!request.Headers.TryGetValue(csrfHeaderName, out var header) ||
            !request.Cookies.TryGetValue(csrfCookieName, out var cookie))
            return false;

        var headerBytes = Encoding.UTF8.GetBytes(header.ToString());
        var cookieBytes = Encoding.UTF8.GetBytes(cookie);
        return headerBytes.Length == cookieBytes.Length &&
               CryptographicOperations.FixedTimeEquals(headerBytes, cookieBytes);
    }

    public static CookieOptions CreateCookieOptions(
        bool secure,
        SameSiteMode? sameSite = null) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = sameSite ?? (secure ? SameSiteMode.None : SameSiteMode.Lax),
        IsEssential = true,
        Path = "/"
    };
}

public static class PublicAccountAuthenticationExtensions
{
    public static AuthenticationBuilder AddPublicAccountAuthentication(
        this AuthenticationBuilder builder,
        string scheme,
        string sessionCookieName)
    {
        if (string.IsNullOrWhiteSpace(scheme))
            throw new ArgumentException("An authentication scheme is required.", nameof(scheme));
        if (string.IsNullOrWhiteSpace(sessionCookieName))
            throw new ArgumentException("A session cookie name is required.", nameof(sessionCookieName));

        return builder.AddScheme<PublicAccountAuthenticationOptions, PublicAccountAuthenticationHandler>(
            scheme,
            options => options.SessionCookieName = sessionCookieName);
    }
}

public sealed class PublicAccountAuthenticationHandler(
    IOptionsMonitor<PublicAccountAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IPublicAccountService accountService)
    : AuthenticationHandler<PublicAccountAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(Options.SessionCookieName, out var token))
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
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
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
