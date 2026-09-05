using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class FaqCandidateGenerationService(
    IAskSubmissionRepository submissionRepository,
    IFaqCandidateRepository candidateRepository,
    IFaqCandidateAiProvider aiProvider,
    ILogger<FaqCandidateGenerationService> logger) : IFaqCandidateGenerationService
{
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(30);
    private const int MaxAttempts = 2;

    public async Task<IReadOnlyList<FaqCandidate>> GenerateAsync(IReadOnlyList<string> campaignIds, string createdBy, CancellationToken cancellationToken = default)
    {
        var selectedCampaignIds = campaignIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (selectedCampaignIds.Length == 0)
            throw new FaqCandidateGenerationException("faq_campaign_selection_required", "Select at least one Campaign.");

        var submissionsByCampaign = await Task.WhenAll(selectedCampaignIds.Select(async campaignId =>
            (CampaignId: campaignId, Submissions: await submissionRepository.GetByCampaignIdAsync(campaignId, 5000))));
        var sources = submissionsByCampaign
            .Select(item => new FaqCandidateSource(
                item.CampaignId,
                item.Submissions.Select(submission => submission.Text.Trim()).Where(text => text.Length > 0).ToArray()))
            .Where(source => source.Messages.Count > 0)
            .ToArray();

        if (sources.Length == 0) return [];

        IReadOnlyList<FaqCandidateDraft> drafts;
        try
        {
            drafts = await GenerateWithRetryAsync(sources, cancellationToken);
        }
        catch (FaqCandidateProviderTimeoutException)
        {
            await RecordFailureAsync(selectedCampaignIds, submissionsByCampaign.Sum(item => item.Submissions.Count), createdBy, "faq_campaign_ai_timeout");
            throw new FaqCandidateGenerationException("faq_campaign_ai_timeout", "FAQ candidate generation timed out. A pending failure candidate was recorded.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await RecordFailureAsync(selectedCampaignIds, submissionsByCampaign.Sum(item => item.Submissions.Count), createdBy, "faq_campaign_ai_timeout");
            throw new FaqCandidateGenerationException("faq_campaign_ai_timeout", "FAQ candidate generation timed out. A pending failure candidate was recorded.");
        }
        catch (Exception exception) when (exception is not FaqCandidateGenerationException)
        {
            logger.LogError(exception, "FAQ candidate generation failed for {CampaignCount} selected campaigns", selectedCampaignIds.Length);
            await RecordFailureAsync(selectedCampaignIds, submissionsByCampaign.Sum(item => item.Submissions.Count), createdBy, "faq_campaign_ai_failed");
            throw new FaqCandidateGenerationException("faq_campaign_ai_failed", "FAQ candidate generation failed. A pending failure candidate was recorded.");
        }

        var selectedIds = selectedCampaignIds.ToHashSet(StringComparer.Ordinal);
        var sourceCounts = sources.ToDictionary(source => source.CampaignId, source => source.Messages.Count, StringComparer.Ordinal);
        var candidates = drafts
            .Select(draft =>
            {
                var sourceCampaignIds = draft.CampaignIds.Where(selectedIds.Contains).Distinct(StringComparer.Ordinal).ToArray();
                var sourceSubmissionCount = sourceCampaignIds.Sum(id => sourceCounts.GetValueOrDefault(id));
                return new FaqCandidate
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Question = draft.Question.Trim(),
                    Answer = draft.Answer.Trim(),
                    CampaignIds = sourceCampaignIds,
                    SourceSubmissionCount = sourceSubmissionCount,
                    Relevance = Math.Clamp(draft.Relevance, 0, 1),
                    DuplicateOfFaqId = string.IsNullOrWhiteSpace(draft.DuplicateOfFaqId) ? null : draft.DuplicateOfFaqId.Trim(),
                    Status = FaqCandidateStatus.Pending,
                    CreatedBy = createdBy.Trim()
                };
            })
            .Where(candidate => candidate.CampaignIds.Count > 0 && candidate.Question.Length > 0 && candidate.Answer.Length > 0)
            .ToArray();

        foreach (var candidate in candidates) await candidateRepository.AddItemAsync(candidate);
        logger.LogInformation("Created {CandidateCount} pending FAQ candidates from {CampaignCount} selected campaigns", candidates.Length, selectedCampaignIds.Length);
        return candidates;
    }

    private async Task<IReadOnlyList<FaqCandidateDraft>> GenerateWithRetryAsync(IReadOnlyList<FaqCandidateSource> sources, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProviderTimeout);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await aiProvider.GenerateAsync(sources, timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxAttempts)
            {
                logger.LogWarning("FAQ AI provider timed out on attempt {Attempt}; retrying", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new FaqCandidateProviderTimeoutException();
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(exception, "FAQ AI provider failed on attempt {Attempt}; retrying", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
    }

    private async Task RecordFailureAsync(IReadOnlyList<string> campaignIds, int sourceSubmissionCount, string createdBy, string reason)
    {
        await candidateRepository.AddItemAsync(new FaqCandidate
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CampaignIds = campaignIds,
            SourceSubmissionCount = sourceSubmissionCount,
            Status = FaqCandidateStatus.Pending,
            FailureReason = reason,
            CreatedBy = createdBy.Trim()
        });
    }
}
