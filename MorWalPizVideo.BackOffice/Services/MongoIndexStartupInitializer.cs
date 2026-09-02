using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MorWalPizVideo.BackOffice.Services;

/// <summary>
/// Applies indexes required by request paths before the BackOffice host accepts traffic.
/// The operation is idempotent and safe when several instances start together.
/// </summary>
public sealed class MongoIndexStartupInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<MongoIndexStartupInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var operationsService = scope.ServiceProvider.GetRequiredService<IMongoIndexOperationsService>();
                await operationsService.ApplyAsync(["shortlinks.code.unique"], cancellationToken);
                return;
            }
            catch (Exception exception) when (
                attempt < 2 &&
                (exception is MongoException || exception is MongoIndexOperationException))
            {
                logger.LogWarning(exception,
                    "Short-link unique index initialization failed on attempt {Attempt}; retrying",
                    attempt + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
            }
        }

        throw new InvalidOperationException("Short-link unique index initialization failed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
