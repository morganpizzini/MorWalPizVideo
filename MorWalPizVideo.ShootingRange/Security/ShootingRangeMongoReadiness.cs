using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeMongoReadiness(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var hello = await database.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: deadline.Token);
            return IsTransactionTopology(hello) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Transaction-capable Mongo topology is required.");
        }
        catch (Exception error) when (error is MongoException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Mongo readiness could not be established.");
        }
    }

    internal static bool IsTransactionTopology(BsonDocument hello) =>
        hello.GetValue("isWritablePrimary", false).ToBoolean() && hello.Contains("logicalSessionTimeoutMinutes") &&
        (hello.Contains("setName") || hello.GetValue("msg", "").AsString == "isdbgrid");
}