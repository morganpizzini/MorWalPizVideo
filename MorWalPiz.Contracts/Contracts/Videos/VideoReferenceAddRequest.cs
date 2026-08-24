using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.Contracts.Contracts.Videos;

public sealed class VideoReferenceAddRequest
{
    [Required]
    public string YoutubeId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one category is required")]
    public string[] Categories { get; set; } = [];
}
