using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs;

public sealed class VideoImportCandidatesRequest
{
    public string? ChannelId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool ShowVideo { get; set; } = true;
}

public sealed class VideoBulkImportRequest
{
    [Required]
    [MinLength(1)]
    public VideoBulkImportRootRequest[] Roots { get; set; } = [];
}

public static class VideoBulkImportLimits
{
    public const int MaxTotalVideos = 25;
}

public sealed class VideoBulkImportRootRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one category is required")]
    public string[] Categories { get; set; } = [];

    public VideoBulkImportReferenceRequest[] References { get; set; } = [];
}

public sealed class VideoBulkImportReferenceRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one category is required")]
    public string[] Categories { get; set; } = [];
}

public sealed record VideoImportCandidateResponse(
    string VideoId,
    string Title,
    DateTime PublishedAt,
    bool AlreadyImported);

public sealed record VideoImportResponse(
    string VideoId,
    string Status,
    string? ShortLinkStatus = null,
    string? Error = null);

public sealed record VideoBulkImportItemResponse(
    string VideoId,
    string Status,
    string? ShortLinkStatus = null,
    string? Error = null,
    string Role = "root",
    string? ErrorCode = null,
    string? CacheStatus = null);

public sealed record VideoBulkImportGroupResponse(
    VideoBulkImportItemResponse Root,
    IReadOnlyList<VideoBulkImportItemResponse> References);