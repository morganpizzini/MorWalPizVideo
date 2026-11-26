using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class MongoIndexStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<MongoIndexStartupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var verifiedCount = 0;
        var failedCount = 0;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var operations = scope.ServiceProvider.GetRequiredService<IMongoIndexOperationsService>();

            foreach (var entry in MongoIndexOperationsService.Manifest)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    if (await InitializeIndexAsync(operations, entry, stoppingToken))
                        verifiedCount++;
                    else
                        failedCount++;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    logger.LogError(
                        exception,
                        "Could not initialize Mongo index {IndexKey} on {Collection}; startup will continue",
                        entry.Key,
                        entry.Collection);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("Mongo index startup initialization was cancelled");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not start Mongo index initialization; the application will continue");
        }
        finally
        {
            var unverifiedCount = MongoIndexOperationsService.Manifest.Count - verifiedCount - failedCount;
            logger.LogInformation(
                "Mongo index startup initialization finished with {VerifiedCount} verified, {FailedCount} failed, and {UnverifiedCount} unverified indexes",
                verifiedCount,
                failedCount,
                unverifiedCount);
        }
    }

    private async Task<bool> InitializeIndexAsync(
        IMongoIndexOperationsService operations,
        MongoIndexManifestEntry entry,
        CancellationToken cancellationToken)
    {
        Exception? applyFailure = null;
        string? action = null;

        try
        {
            var results = await operations.ApplyAsync([entry.Key], cancellationToken);
            action = results.SingleOrDefault()?.Action;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            applyFailure = exception;
        }

        try
        {
            var audit = await operations.AuditAsync([entry.Key], cancellationToken);
            var item = audit.SingleOrDefault();
            if (item?.Exists == true)
            {
                if (applyFailure is not null)
                {
                    logger.LogWarning(
                        applyFailure,
                        "Mongo index {IndexKey} was verified after its apply operation reported an error",
                        entry.Key);
                }
                else
                {
                    logger.LogInformation(
                        "Mongo startup index {IndexKey} on {Collection} verified with action {Action}",
                        item.Key,
                        item.Collection ?? entry.Collection,
                        action);
                }

                return true;
            }

            if (applyFailure is not null)
            {
                logger.LogError(
                    applyFailure,
                    "Mongo index {IndexKey} on {Collection} is not present with its expected definition; startup will continue",
                    item?.Key ?? entry.Key,
                    item?.Collection ?? entry.Collection);
            }
            else
            {
                logger.LogError(
                    "Mongo index {IndexKey} was not verified with its expected definition; startup will continue",
                    entry.Key);
            }

            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception auditFailure)
        {
            var failure = applyFailure is null
                ? auditFailure
                : new AggregateException("Mongo index apply and follow-up audit both failed.", applyFailure, auditFailure);
            logger.LogError(
                failure,
                "Could not verify Mongo index {IndexKey} after startup apply; startup will continue",
                entry.Key);
            return false;
        }
    }
}
