using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class SwapRootThumbnailRequest
  {
    [Required]
    public string CurrentVideoId { get; set; } = string.Empty;

    [Required]
    public string NewVideoId { get; set; } = string.Empty;
  }
}
