using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class VideoImportRequest
  {
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;
  }
}
