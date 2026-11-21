using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeManagementAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "ShootingRangeManagement";
    public const string HeaderName = "X-Shooting-Range-Management-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = configuration["ShootingRange:ManagementKey"];
        if (string.IsNullOrEmpty(configuredKey))
            return Task.FromResult(AuthenticateResult.Fail("Tenant management is not configured."));

        if (!Request.Headers.TryGetValue(HeaderName, out var suppliedValues) || suppliedValues.Count != 1)
            return Task.FromResult(AuthenticateResult.NoResult());

        var suppliedKey = suppliedValues[0];
        if (string.IsNullOrEmpty(suppliedKey) || suppliedKey.Length > 512)
            return Task.FromResult(AuthenticateResult.Fail("Tenant management authentication failed."));

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKey));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
            return Task.FromResult(AuthenticateResult.Fail("Tenant management authentication failed."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "shooting-range-tenant-provisioner")], AuthenticationScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
