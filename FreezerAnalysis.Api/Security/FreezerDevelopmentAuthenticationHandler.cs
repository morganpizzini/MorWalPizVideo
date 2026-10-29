using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FreezerAnalysis.Api.Security;

public sealed class FreezerDevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "FreezerDevelopment";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var accountId = configuration["FreezerAnalysis:DevelopmentAccountId"];
        if (string.IsNullOrWhiteSpace(accountId))
            accountId = "development-account";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, accountId),
            new Claim(ClaimTypes.Name, "FreezerAnalysis Development")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, AuthenticationScheme)));
    }
}
