using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class ReviewRequest
  {
    [Required]
    public IList<string> Names { get; set; } = new List<string>();

    public string Context { get; set; } = string.Empty;
  }
}
