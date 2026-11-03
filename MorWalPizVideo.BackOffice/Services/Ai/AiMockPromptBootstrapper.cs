using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MorWalPizVideo.BackOffice.Services.Ai;

public sealed class AiMockPromptBootstrapper(
    IServiceScopeFactory scopeFactory,
    ILogger<AiMockPromptBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var operations = scope.ServiceProvider.GetRequiredService<IAiPromptOperationsService>();
        var created = await operations.BootstrapAsync("mock-bootstrap", cancellationToken);
        logger.LogInformation("Initialized {PromptCount} deterministic AI prompt baselines for mock mode.", created);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
