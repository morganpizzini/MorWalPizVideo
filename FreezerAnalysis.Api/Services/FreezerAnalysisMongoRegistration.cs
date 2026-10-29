using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Domain.FreezerAnalysis;
using MorWalPizVideo.Server.Services.Interfaces;

namespace FreezerAnalysis.Api.Services;

public static class FreezerAnalysisMongoRegistration
{
    public static void AddRepositories(IServiceCollection services, IConfiguration configuration)
    {
        var platformSettings = configuration.GetSection("MorWalPizDatabase").Get<MorWalPizDatabaseSettings>();
        var applicationDatabaseName = configuration["FreezerAnalysis:ApplicationDatabaseName"];
        if (platformSettings is null ||
            string.IsNullOrWhiteSpace(platformSettings.ConnectionString) ||
            string.IsNullOrWhiteSpace(platformSettings.DatabaseName))
            throw new InvalidOperationException("Platform MongoDB configuration is required for public-account authentication.");
        if (string.IsNullOrWhiteSpace(applicationDatabaseName))
            throw new InvalidOperationException("FreezerAnalysis:ApplicationDatabaseName is required.");
        if (string.Equals(
                platformSettings.DatabaseName.Trim(),
                applicationDatabaseName.Trim(),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("FreezerAnalysis application data must use a database distinct from the platform account database.");

        var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(platformSettings.ConnectionString));
        var mongoClient = new MongoClient(mongoSettings);
        var platformDatabase = mongoClient.GetDatabase(platformSettings.DatabaseName);
        var analysisDatabase = mongoClient.GetDatabase(applicationDatabaseName.Trim());
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton<IPublicAccountRepository>(_ => new PublicAccountRepository(platformDatabase));
        services.AddScoped<IPublicAccountService, PublicAccountService>();
        services.AddSingleton(new FreezerAnalysisDatabase(analysisDatabase));
        services.AddScoped<IFreezerAnalysisRepository, FreezerAnalysisMongoRepository>();
        services.AddHealthChecks().AddCheck(
            "freezer-mongodb",
            () =>
            {
                try
                {
                    analysisDatabase.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                    return HealthCheckResult.Healthy();
                }
                catch (Exception exception) when (exception is MongoException or TimeoutException)
                {
                    return HealthCheckResult.Unhealthy(
                        "FreezerAnalysis MongoDB is unavailable.", exception);
                }
            },
            ["ready", "startup"]);
    }
}
