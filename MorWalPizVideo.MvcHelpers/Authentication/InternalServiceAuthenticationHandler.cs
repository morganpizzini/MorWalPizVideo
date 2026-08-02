using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MorWalPizVideo.MvcHelpers.Authentication;

public class InternalServiceAuthenticationOptions : AuthenticationSchemeOptions
{
}

// Validates a shared-secret header for trusted service-to-service calls (ADR-002: internal cache operations).
public class InternalServiceAuthenticationHandler : AuthenticationHandler<InternalServiceAuthenticationOptions>
{
    public const string SchemeName = "InternalService";

    private readonly InternalServiceSettings _settings;

    public InternalServiceAuthenticationHandler(
        IOptionsMonitor<InternalServiceAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<InternalServiceSettings> settings)
        : base(options, logger, encoder)
    {
        _settings = settings.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(_settings.Secret))
        {
            return Task.FromResult(AuthenticateResult.Fail("Internal service secret is not configured"));
        }

        if (!Request.Headers.TryGetValue(_settings.HeaderName, out var providedValues))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {_settings.HeaderName} header"));
        }

        var provided = providedValues.ToString();
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(_settings.Secret);
        if (providedBytes.Length != expectedBytes.Length || !CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal service key"));
        }

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "InternalService") }, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
