using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class FeedbackMongoIndexStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<FeedbackMongoIndexStartupService> logger) : IHostedService
{
    private static readonly string[] StartupIndexKeys = ["feedback.createdat_desc"];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var operations = scope.ServiceProvider.GetRequiredService<IMongoIndexOperationsService>();
        var results = await operations.ApplyAsync(StartupIndexKeys, cancellationToken);
        foreach (var result in results)
        {
            logger.LogInformation(
                "Mongo startup index {IndexKey} on {Collection} completed with action {Action}",
                result.Key,
                result.Collection,
                result.Action);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
