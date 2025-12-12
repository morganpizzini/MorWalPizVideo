using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class VideoImportRequest
  {
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one category is required")]
    public string[] Categories { get; set; } = Array.Empty<string>();
  }
}
