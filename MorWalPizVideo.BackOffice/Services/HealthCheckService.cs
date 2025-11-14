using Microsoft.Extensions.Diagnostics.HealthChecks;
using MorWalPizVideo.Server.Utils;

namespace MorWalPizVideo.BackOffice.Services;

public static class HealthCheckService
{
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var enableMock = configuration.IsFeatureEnabled(MyFeatureFlags.EnableMock);
        var enableHangfire = configuration.IsFeatureEnabled(MyFeatureFlags.EnableHangFire);

        var healthChecksBuilder = services.AddHealthChecks();

        // Note: Basic liveness check ("self") is already added by AddServiceDefaults()
        // Avoiding duplicate registration to prevent MapHealthChecksCore conflicts

        if (!enableMock)
        {
            // MongoDB Health Check
            var dbConfig = configuration.GetSection("MorWalPizDatabase").Get<MorWalPizDatabaseSettings>();
            if (dbConfig != null)
            {
                healthChecksBuilder.AddMongoDb(
                    mongodbConnectionString: dbConfig.ConnectionString,
                    name: "mongodb",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready", "database"],
                    timeout: TimeSpan.FromSeconds(10));
            }

            // Azure OpenAI Health Check
            var azureConfig = configuration.GetSection("AzureConfig").Get<AzureConfig>();
            if (azureConfig?.OpenAi != null && !string.IsNullOrEmpty(azureConfig.OpenAi.OpenAiEndpoint))
            {
                var openAiHealthUri = new Uri(azureConfig.OpenAi.OpenAiEndpoint).GetLeftPart(UriPartial.Authority);
                healthChecksBuilder.AddUrlGroup(
                    new Uri(openAiHealthUri),
                    name: "azure-openai",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["ready", "external"],
                    timeout: TimeSpan.FromSeconds(15));
            }

            // YouTube API Health Check
            healthChecksBuilder.AddUrlGroup(
                new Uri("https://www.googleapis.com/youtube/v3/"),
                name: "youtube-api",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "external"],
                timeout: TimeSpan.FromSeconds(10));

            // MorWalPiz API Health Check
            var siteUrl = configuration["SiteUrl"];
            if (!string.IsNullOrEmpty(siteUrl))
            {
                healthChecksBuilder.AddUrlGroup(
                    new Uri($"{siteUrl}api/"),
                    name: "morwalpiz-api",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["ready", "internal"],
                    timeout: TimeSpan.FromSeconds(10));
            }
        }
        else
        {
            // Mock mode health checks - always healthy
            healthChecksBuilder.AddCheck("mock-services", () => 
                HealthCheckResult.Healthy("Running in mock mode - all external dependencies are mocked"), 
                ["ready", "mock"]);
        }

        if (enableHangfire)
        {
            // Hangfire Health Check
            if (!enableMock && !string.IsNullOrEmpty(configuration.GetConnectionString("HangfireConnection")))
            {
                // Production - SQL Server for Hangfire
                healthChecksBuilder.AddSqlServer(
                    connectionString: configuration.GetConnectionString("HangfireConnection")!,
                    name: "hangfire-sqlserver",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready", "hangfire", "database"],
                    timeout: TimeSpan.FromSeconds(10));
            }

            // Hangfire processing health check
            healthChecksBuilder.AddHangfire(
                setup => {
                    setup.MinimumAvailableServers = 1;
                },
                name: "hangfire-processing",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "hangfire"],
                timeout: TimeSpan.FromSeconds(5));
        }

        // Feature Management Health Check
        healthChecksBuilder.AddCheck("feature-management", () =>
        {
            try
            {
                var featureEnabled = configuration.IsFeatureEnabled(MyFeatureFlags.EnableSwagger);
                return HealthCheckResult.Healthy("Feature management is working correctly");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Feature management configuration error", ex);
            }
        }, ["startup", "configuration"]);

        return services;
    }
}
