using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class ReviewRequest
  {
    [Required]
    public IList<string> Names { get; set; } = new List<string>();

    public IList<ReviewVideoRequest> Videos { get; set; } = new List<ReviewVideoRequest>();

    public string Context { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = new List<string>();

    }

  public class ReviewVideoRequest
  {
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Context { get; set; } = string.Empty;
  }
}
