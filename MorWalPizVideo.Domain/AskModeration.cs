using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain;

public enum AskModerationDecision
{
    Approve,
    Reject,
    Review
}

public sealed record AskModerationResult(
    double Score,
    IReadOnlyList<string> Categories,
    AskModerationDecision Decision,
    string Reason);

public interface IAskModerationProvider
{
    Task<AskModerationResult?> AssessAsync(string content, CancellationToken cancellationToken = default);
}

public sealed class UnavailableAskModerationProvider : IAskModerationProvider
{
    public Task<AskModerationResult?> AssessAsync(string content, CancellationToken cancellationToken = default)
        => Task.FromResult<AskModerationResult?>(null);
}