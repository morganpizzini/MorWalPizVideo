using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs;

public sealed class VideoImportCandidatesRequest
{
    public string? ChannelId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public sealed class VideoBulkImportRequest
{
    [Required]
    [MinLength(1)]
    public VideoBulkImportItemRequest[] Items { get; set; } = [];
}

public sealed class VideoBulkImportItemRequest
{
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one category is required")]
    public string[] Categories { get; set; } = [];

    // Existing content ID, or the video ID of an earlier item in this batch.
    public string? Target { get; set; }
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
    string? Error = null);