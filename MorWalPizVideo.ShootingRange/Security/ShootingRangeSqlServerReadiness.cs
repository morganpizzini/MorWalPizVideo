using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MorWalPizVideo.ShootingRange.Repositories;
using Microsoft.Data.SqlClient;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeSqlServerReadiness(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<SqlServerShootingRangeRepositorySet>()
                .VerifyReadyAsync(deadline.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("SQL Server readiness could not be established.");
        }
    }
}
