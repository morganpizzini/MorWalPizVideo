using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public class BackOfficeWebApplicationFactory : WebApplicationFactory<MorWalPizVideo.BackOffice.Program>
{
    public YTChannelMockRepository? YTChannelRepository => Services.GetRequiredService<IYTChannelRepository>() as YTChannelMockRepository;
    public ShortLinkMockRepository? ShortLinkRepository => Services.GetRequiredService<IShortLinkRepository>() as ShortLinkMockRepository;
    public CompilationMockRepository? CompilationRepository => Services.GetRequiredService<ICompilationRepository>() as CompilationMockRepository;
    public MatchMockRepository? MatchRepository => Services.GetRequiredService<IYouTubeContentRepository>() as MatchMockRepository;

    public HttpClient CreateClientWithPermissions(params string[] permissions)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Test-Permissions",
            string.Join(',', permissions.Select(permission => permission.ToLowerInvariant())));
        return client;
    }

    static BackOfficeWebApplicationFactory()
    {
        // Program.cs reads feature flags eagerly during WebApplication.CreateBuilder(),
        // BEFORE ConfigureAppConfiguration callbacks run. Environment variables are picked up
        // by the default configuration sources at builder creation time, so this is the only
        // reliable way to force mock mode (and to disable Swagger/Hangfire/KeyVault/CORS) end-to-end.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableMock", "true");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableSwagger", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableHangFire", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableKeyVault", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableCors", "false");
        Environment.SetEnvironmentVariable("JwtSettings__Secret", "test-secret-key-for-testing-purposes-only-min-32-chars");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "MorWalPizVideo.BackOffice.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "MorWalPizVideo.BackOffice.Tests");
        Environment.SetEnvironmentVariable("SiteUrl", "http://localhost/");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:EnableMock"] = "true",
                ["FeatureManagement:EnableSwagger"] = "false",
                ["FeatureManagement:EnableKeyVault"] = "false",
                ["FeatureManagement:EnableHangFire"] = "false",
                ["FeatureManagement:EnableCors"] = "false",
                ["SiteUrl"] = "http://localhost/",
                // JWT settings required for authentication services
                ["JwtSettings:Secret"] = "test-secret-key-for-testing-purposes-only-min-32-chars",
                ["JwtSettings:Issuer"] = "MorWalPizVideo.BackOffice.Tests",
                ["JwtSettings:Audience"] = "MorWalPizVideo.BackOffice.Tests"
                ,
                ["BootstrapSettings:Secret"] = "test-bootstrap-secret"
            });
        });

        // Use ConfigureTestServices to override services AFTER Program.cs registration
        builder.ConfigureTestServices(services =>
        {
            // Mock external services to prevent real HTTP calls during tests

            services.AddScoped<IDiscordService, DiscordServiceMock>();
            services.AddScoped<ITelegramService, TelegramServiceMock>();
            services.AddScoped<ICrossApiService, MockCrossApiService>();

            // Remove existing authentication services
            var authDescriptors = services
                .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.AspNetCore.Authentication") == true)
                .ToList();
            foreach (var descriptor in authDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });

        builder.UseEnvironment("Test");
    }
}

/// <summary>
/// Test authentication handler that authenticates requests with an optional test role.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-Anonymous"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = "test-user-id";
        if (Request.Headers.TryGetValue("X-Test-UserId", out var headerUserId) && !string.IsNullOrWhiteSpace(headerUserId))
        {
            userId = headerUserId.ToString();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.NameIdentifier, userId)
        };
        if (Request.Headers.TryGetValue("X-Test-Role", out var role) && !string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-Permissions", out var permissionsHeader) && !string.IsNullOrWhiteSpace(permissionsHeader))
        {
            foreach (var permission in permissionsHeader
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}