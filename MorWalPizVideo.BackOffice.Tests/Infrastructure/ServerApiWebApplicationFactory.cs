using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public sealed class ServerApiWebApplicationFactory : WebApplicationFactory<MorWalPizVideo.ServerAPI.Program>
{
    public YTChannelMockRepository? YTChannelRepository => Services.GetRequiredService<IYTChannelRepository>() as YTChannelMockRepository;
    public MatchMockRepository? MatchRepository => Services.GetRequiredService<IYouTubeContentRepository>() as MatchMockRepository;
    public QuickLinksMockRepository? QuickLinksRepository => Services.GetRequiredService<IQuickLinksRepository>() as QuickLinksMockRepository;
    public ChannelNewsMockRepository? ChannelNewsRepository => Services.GetRequiredService<IChannelNewsRepository>() as ChannelNewsMockRepository;
    public AskCampaignMockRepository? AskCampaignRepository => Services.GetRequiredService<IAskCampaignRepository>() as AskCampaignMockRepository;

    static ServerApiWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableMock", "true");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableDev", "true");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableSwagger", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableKeyVault", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableCache", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableOutputCache", "false");
        Environment.SetEnvironmentVariable("FeatureManagement__EnableCors", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:EnableMock"] = "true",
                ["FeatureManagement:EnableDev"] = "true",
                ["FeatureManagement:EnableSwagger"] = "false",
                ["FeatureManagement:EnableKeyVault"] = "false",
                ["FeatureManagement:EnableCache"] = "false",
                ["FeatureManagement:EnableOutputCache"] = "false",
                ["FeatureManagement:EnableCors"] = "false",
                ["YouTubeChannelId"] = PrimaryScenario.ChannelId
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRecaptchaService>();
            services.AddSingleton<IRecaptchaService, TestRecaptchaService>();
            services.RemoveAll<IAskModerationProvider>();
            services.AddScoped<IAskModerationProvider, UnavailableAskModerationProvider>();
        });
    }
}

public sealed class TestRecaptchaService : IRecaptchaService
{
    public Task<bool> VerifyAsync(string token, string remoteIp, string expectedAction, CancellationToken ct = default)
        => Task.FromResult(token == "valid-token");
}
