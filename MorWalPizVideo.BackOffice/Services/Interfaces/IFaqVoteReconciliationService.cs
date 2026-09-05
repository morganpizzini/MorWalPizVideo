using MorWalPizVideo.Domain.Interfaces;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public sealed record FaqVoteReconciliationResult(
    int AnswersScanned,
    int AnswersRepaired,
    int AuthoritativeVotes,
    int ConcurrentChanges);

public interface IFaqVoteReconciliationService
{
    Task<FaqVoteReconciliationResult> ReconcileAsync(
        IReadOnlyCollection<string>? answerIds = null,
        CancellationToken cancellationToken = default);
}