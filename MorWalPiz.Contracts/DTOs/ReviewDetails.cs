using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class ReviewDetails
  {
    [Required]
    [Description("Il nome del file originale")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Description("Titolo del video in italiano")]
    public string TitleItalian { get; set; } = string.Empty;

    [Required]
    [Description("Descrizione del video in italiano")]
    public string DescriptionItalian { get; set; } = string.Empty;

    [Required]
    [Description("Descrizione del video in inglese")]
    public string TitleEnglish { get; set; } = string.Empty;

    [Required]
    [Description("Descrizione del video in inglese")]
    public string DescriptionEnglish { get; set; } = string.Empty;
  }
}
