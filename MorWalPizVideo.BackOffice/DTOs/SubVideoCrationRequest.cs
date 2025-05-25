using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class SubVideoCrationRequest
  {
    [Required]
    public string MatchId { get; set; } = string.Empty;

    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;
  }
}
