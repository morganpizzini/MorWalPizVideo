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
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public class BackOfficeWebApplicationFactory : WebApplicationFactory<Program>
{
    // Expose mock repositories as singleton instances for direct test access
    public YTChannelMockRepository? YTChannelRepository { get; private set; }
    public ShortLinkMockRepository? ShortLinkRepository { get; private set; }
    public CompilationMockRepository? CompilationRepository { get; private set; }
    public MatchMockRepository? MatchRepository { get; private set; }

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
            });
        });

        // Use ConfigureTestServices to override services AFTER Program.cs registration
        builder.ConfigureTestServices(services =>
        {
            // Remove all MongoDB-related repository registrations if they exist
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.Name.Contains("Repository") && 
                           !d.ServiceType.Name.Contains("Mock") &&
                           d.ServiceType.Namespace == "MorWalPizVideo.Domain.Interfaces")
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Get IHostEnvironment for mock repository construction
            var serviceProvider = services.BuildServiceProvider();
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();

            // Create singleton instances of frequently-accessed mock repositories
            YTChannelRepository = new YTChannelMockRepository(hostEnvironment);
            ShortLinkRepository = new ShortLinkMockRepository(hostEnvironment);
            CompilationRepository = new CompilationMockRepository(hostEnvironment);
            MatchRepository = new MatchMockRepository(hostEnvironment);

            // Register exposed repositories as singletons
            services.AddSingleton<IYTChannelRepository>(YTChannelRepository);
            services.AddSingleton<IShortLinkRepository>(ShortLinkRepository);
            services.AddSingleton<ICompilationRepository>(CompilationRepository);
            services.AddSingleton<IYouTubeContentRepository>(MatchRepository);

            // Register remaining repositories as scoped (no need for direct test access)
            services.AddSingleton<IProductRepository, ProductMockRepository>();
            services.AddSingleton<ISponsorRepository, SponsorMockRepository>();
            services.AddSingleton<ISponsorApplyRepository, SponsorApplyMockRepository>();
            services.AddSingleton<IPageRepository, PageMockRepository>();
            services.AddSingleton<ICalendarEventRepository, CalendarEventMockRepository>();
            services.AddSingleton<IBioLinkRepository, BioLinkMockRepository>();
            services.AddSingleton<ICategoryRepository, CategoryMockRepository>();
            services.AddSingleton<IQueryLinkRepository, QueryLinkMockRepository>();
            services.AddSingleton<IUserRepository, UserMockRepository>();
            services.AddSingleton<ILoginAttemptRepository, LoginAttemptMockRepository>();
            services.AddSingleton<IProductCategoryRepository, ProductCategoryMockRepository>();
            services.AddSingleton<IPublishScheduleRepository, PublishScheduleMockRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationMockRepository>();
            services.AddSingleton<ICustomFormRepository, CustomFormMockRepository>();
            services.AddSingleton<IApiKeyRepository, ApiKeyMockRepository>();
            services.AddSingleton<IDigitalProductRepository, DigitalProductMockRepository>();
            services.AddSingleton<IDigitalProductCategoryRepository, DigitalProductCategoryMockRepository>();
            services.AddSingleton<ICustomerRepository, CustomerMockRepository>();
            services.AddSingleton<ICartRepository, CartMockRepository>();

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
/// Test authentication handler that automatically authenticates all requests
/// with a fixed single-user identity (no roles or groups).
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
        // Create a fixed authenticated identity
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}