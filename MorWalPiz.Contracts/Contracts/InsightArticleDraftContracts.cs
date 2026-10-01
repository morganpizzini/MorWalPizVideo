using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.Contracts.Contracts;

public sealed record CreateInsightArticleDraftRequest
{
    [Required, MaxLength(100)] public string TopicId { get; init; } = string.Empty;
    [MaxLength(100)] public string? ContentPlanId { get; init; }
    [Required, RegularExpression("^(direct|firstDraft|expandContext|additionalInformation)$")]
    public string Mode { get; init; } = "direct";
    [MaxLength(10000)] public string AdditionalInformation { get; init; } = string.Empty;
}

public sealed record InsightArticleDraftResponse(
    string Title,
    string Summary,
    string Body,
    string TopicId,
    string? ContentPlanId);
