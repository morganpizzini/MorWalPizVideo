using MorWalPizVideo.Domain;

namespace MorWalPizVideo.BackOffice.Jobs;

public sealed class AskRetentionJob(IAskService askService, ILogger<AskRetentionJob> logger)
{
    public const string JobId = "ask-retention-job";

    public async Task ExecuteAsync()
    {
        var deleted = await askService.DeleteExpiredAsync();
        logger.LogInformation("Ask retention completed. Deleted {DeletedCount} expired submissions", deleted);
    }
}
