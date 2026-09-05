using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public sealed record FaqCandidateSource(string CampaignId, IReadOnlyList<string> Messages);
public sealed record FaqCandidateDraft(string Question, string Answer, IReadOnlyList<string> CampaignIds, double Relevance, string? DuplicateOfFaqId);

public interface IFaqCandidateAiProvider
{
    Task<IReadOnlyList<FaqCandidateDraft>> GenerateAsync(IReadOnlyList<FaqCandidateSource> sources, CancellationToken cancellationToken = default);
}

public interface IFaqCandidateGenerationService
{
    Task<IReadOnlyList<FaqCandidate>> GenerateAsync(IReadOnlyList<string> campaignIds, string createdBy, CancellationToken cancellationToken = default);
}

public sealed class FaqCandidateGenerationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class FaqCandidateProviderTimeoutException : Exception { }
