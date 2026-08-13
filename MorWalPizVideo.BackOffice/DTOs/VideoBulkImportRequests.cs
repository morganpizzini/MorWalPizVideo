using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs;

public sealed class VideoImportCandidatesRequest
{
    [Required]
    public string ChannelId { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }
}

public sealed class VideoBulkImportRequest
{
    [Required]
    [MinLength(1)]
    public string[] VideoIds { get; set; } = [];

    [Required]
    [MinLength(1, ErrorMessage = "At least one category is required")]
    public string[] Categories { get; set; } = [];

    public string? TargetContentId { get; set; }
}

public sealed record VideoImportCandidateResponse(
    string VideoId,
    string Title,
    DateTime PublishedAt,
    bool AlreadyImported);

public sealed record VideoBulkImportItemResponse(
    string VideoId,
    string Status,
    string? Error = null);