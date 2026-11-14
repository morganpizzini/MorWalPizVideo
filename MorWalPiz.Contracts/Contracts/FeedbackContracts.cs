using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.Contracts.Contracts;

public sealed record FeedbackContextRequest
{
    [Required, StringLength(20)]
    public string Application { get; init; } = string.Empty;
    [Required, StringLength(2048)]
    public string PagePath { get; init; } = string.Empty;
    [StringLength(120)]
    public string? ComponentKey { get; init; }
    [StringLength(100)]
    public string? ClientRelease { get; init; }
    [StringLength(512)]
    public string? UserAgent { get; init; }
    [StringLength(35)]
    public string? Language { get; init; }
    [StringLength(100)]
    public string? Timezone { get; init; }
    [Range(1, 20000)]
    public int? ViewportWidth { get; init; }
    [Range(1, 20000)]
    public int? ViewportHeight { get; init; }
    public bool? Online { get; init; }
}

public sealed record SubmitFeedbackRequest
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string RequestId { get; init; } = string.Empty;
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
    [Required]
    public FeedbackContextRequest Context { get; init; } = new();
}

public sealed record FeedbackReceiptContract(string Id, DateTime ReceivedAtUtc, bool Authenticated);

public sealed record FeedbackItemContract(
    string Id,
    string RequestId,
    string Application,
    string Description,
    string PagePath,
    string? ComponentKey,
    string? ClientRelease,
    string? UserAgent,
    string? Language,
    string? Timezone,
    int? ViewportWidth,
    int? ViewportHeight,
    bool? Online,
    string? ReporterUsername,
    string Status,
    long Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime ExpiresAtUtc,
    string? UpdatedByAccountId = null,
    string? UpdatedByUsername = null);

public sealed record FeedbackPageContract(
    IReadOnlyList<FeedbackItemContract> Items,
    int Page,
    int PageSize,
    long Total);

public sealed record UpdateFeedbackStatusRequest
{
    [Required]
    public string Status { get; init; } = string.Empty;
    [Range(1, long.MaxValue)]
    public long ExpectedRevision { get; init; }
}
