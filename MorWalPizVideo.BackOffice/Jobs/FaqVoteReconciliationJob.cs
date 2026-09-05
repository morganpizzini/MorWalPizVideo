using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Jobs;

public sealed class FaqVoteReconciliationJob(IFaqVoteReconciliationService reconciliationService, ILogger<FaqVoteReconciliationJob> logger)
{
    public const string JobId = "faq-vote-reconciliation-job";

    public async Task ExecuteAsync()
    {
        var result = await reconciliationService.ReconcileAsync();
        logger.LogInformation(
            "FAQ vote reconciliation job completed for {AnswersScanned} answers with {AnswersRepaired} repairs",
            result.AnswersScanned,
            result.AnswersRepaired);
    }
}