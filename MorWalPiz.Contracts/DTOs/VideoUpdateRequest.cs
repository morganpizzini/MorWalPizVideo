using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class VideoUpdateRequest
  {
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = true)]
    public string Description { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = true)]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string ThumbnailVideoId { get; set; } = string.Empty;

    [Required]
    public IList<string> Categories { get; set; } = [];

    public VideoRef[]? VideoRefs { get; set; }
  }
}