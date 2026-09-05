using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class SemanticKernelFaqCandidateAiProvider(Kernel kernel) : IFaqCandidateAiProvider
{
    public async Task<IReadOnlyList<FaqCandidateDraft>> GenerateAsync(IReadOnlyList<FaqCandidateSource> sources, CancellationToken cancellationToken = default)
    {
        var sourceText = string.Join("\n\n", sources.Select(source =>
            $"Campaign {source.CampaignId}:\n" + string.Join("\n", source.Messages.Select(message => $"- {message}"))));
        var prompt = $"""
You generate FAQ candidates from explicitly selected Campaign submissions.
Use only the submission messages below. Do not infer or add personal data, names, metadata, campaign descriptions, or answers from outside the messages.
Semantically cluster equivalent questions, identify the topic, canonicalize each question, deduplicate overlapping candidates, and score relevance from 0 to 1.
Return only candidates that are useful as a general FAQ. The answer must be a concise draft grounded only in the messages.
For each candidate, return the exact Campaign IDs that support its cluster. Never return an ID not present in the input.

Selected Campaign submissions:
{sourceText}
""";

#pragma warning disable SKEXP0010
        var settings = new AzureOpenAIPromptExecutionSettings { ResponseFormat = typeof(FaqCandidateResponse) };
#pragma warning restore SKEXP0010
        var result = await kernel.InvokePromptAsync(prompt, new KernelArguments(settings), cancellationToken: cancellationToken);
        var response = JsonSerializer.Deserialize<FaqCandidateResponse>(result.ToString())
            ?? throw new InvalidOperationException("The FAQ AI provider returned an empty response.");
        return response.Candidates.Select(candidate => new FaqCandidateDraft(
            candidate.Question ?? string.Empty,
            candidate.Answer ?? string.Empty,
            candidate.CampaignIds ?? [],
            candidate.Relevance,
            candidate.DuplicateOfFaqId)).ToArray();
    }

    public sealed class FaqCandidateResponse
    {
        public List<FaqCandidateResponseItem> Candidates { get; set; } = [];
    }

    public sealed class FaqCandidateResponseItem
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
        public List<string>? CampaignIds { get; set; }
        public double Relevance { get; set; }
        public string? DuplicateOfFaqId { get; set; }
    }
}
