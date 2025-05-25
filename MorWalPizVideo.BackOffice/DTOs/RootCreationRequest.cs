using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class RootCreationRequest
  {
    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;
  }
}
