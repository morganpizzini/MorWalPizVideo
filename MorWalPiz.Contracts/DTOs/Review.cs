using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class Review
  {
    [Required]
    [Description("Lista delle degli elementi elaborati")]
    public IList<ReviewDetails> Videos { get; set; } = new List<ReviewDetails>();
  }
}
