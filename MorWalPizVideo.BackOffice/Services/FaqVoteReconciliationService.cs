using System.Diagnostics.Metrics;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class FaqVoteReconciliationService(
    IFaqAnswerRepository answerRepository,
    IFaqVoteRepository voteRepository,
    ILogger<FaqVoteReconciliationService> logger) : IFaqVoteReconciliationService
{
    private const int MaxStabilityAttempts = 3;
    private static readonly Meter Meter = new("MorWalPizVideo.Faq");
    private static readonly Counter<long> AnswersScannedCounter = Meter.CreateCounter<long>("faq_vote_reconciliation_answers_scanned");
    private static readonly Counter<long> AnswersRepairedCounter = Meter.CreateCounter<long>("faq_vote_reconciliation_answers_repaired");
    private static readonly Counter<long> ConcurrentChangesCounter = Meter.CreateCounter<long>("faq_vote_reconciliation_concurrent_changes");

    public async Task<FaqVoteReconciliationResult> ReconcileAsync(
        IReadOnlyCollection<string>? answerIds = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedIds = answerIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var answers = normalizedIds is null or { Length: 0 }
            ? await answerRepository.GetItemsAsync()
            : await answerRepository.GetItemsAsync(answer => normalizedIds.Contains(answer.Id, StringComparer.Ordinal));

        var result = new FaqVoteReconciliationResult(0, 0, 0, 0);
        foreach (var answer in answers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = await ReconcileAnswerAsync(answer, cancellationToken);
            result = result with
            {
                AnswersScanned = result.AnswersScanned + 1,
                AnswersRepaired = result.AnswersRepaired + outcome.Repaired,
                AuthoritativeVotes = result.AuthoritativeVotes + outcome.AuthoritativeVotes,
                ConcurrentChanges = result.ConcurrentChanges + outcome.ConcurrentChanges
            };
        }

        AnswersScannedCounter.Add(result.AnswersScanned);
        AnswersRepairedCounter.Add(result.AnswersRepaired);
        ConcurrentChangesCounter.Add(result.ConcurrentChanges);
        logger.LogInformation(
            "FAQ vote reconciliation completed for {AnswersScanned} answers; repaired {AnswersRepaired} counters, observed {AuthoritativeVotes} authoritative votes, and retried {ConcurrentChanges} concurrent changes",
            result.AnswersScanned,
            result.AnswersRepaired,
            result.AuthoritativeVotes,
            result.ConcurrentChanges);
        return result;
    }

    private async Task<AnswerReconciliationResult> ReconcileAnswerAsync(FaqAnswer answer, CancellationToken cancellationToken)
    {
        var repaired = false;
        var concurrentChanges = 0;
        FaqVoteCountSnapshot? stableSnapshot = null;

        for (var attempt = 0; attempt < MaxStabilityAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = await GetSnapshotAsync(answer.Id);
            if (answer.HelpfulVotes != snapshot.HelpfulVotes || answer.NotHelpfulVotes != snapshot.NotHelpfulVotes)
            {
                if (await answerRepository.SetVoteCountsAsync(answer.Id, snapshot.HelpfulVotes, snapshot.NotHelpfulVotes))
                    repaired = true;
            }

            var verification = await GetSnapshotAsync(answer.Id);
            if (verification == snapshot)
            {
                stableSnapshot = verification;
                break;
            }

            concurrentChanges++;
        }

        stableSnapshot ??= await GetSnapshotAsync(answer.Id);
        return new AnswerReconciliationResult(repaired ? 1 : 0, stableSnapshot.TotalVotes, concurrentChanges);
    }

    private async Task<FaqVoteCountSnapshot> GetSnapshotAsync(string answerId)
    {
        var counts = await voteRepository.GetCountsByAnswerIdsAsync([answerId]);
        return counts.FirstOrDefault() ?? new FaqVoteCountSnapshot(answerId, 0, 0, default);
    }

    private sealed record AnswerReconciliationResult(int Repaired, int AuthoritativeVotes, int ConcurrentChanges);
}