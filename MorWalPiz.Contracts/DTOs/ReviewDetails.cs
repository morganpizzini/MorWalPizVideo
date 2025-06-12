using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
    public class ReviewTranslationDetail
    {
        [Required]
        [Description("Lingua della traduzione")]
        public string Language { get; set; } = string.Empty;

        [Required]
        [Description("Titolo del video")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Description("Descrizione del video")]
        public string Description { get; set; } = string.Empty;
    }
  public class ReviewDetails
  {
    [Required]
    [Description("Il nome del file originale")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Description("Lista delle traduzioni effettuate")]
    public IList<ReviewTranslationDetail> Translations { get; set; } = new List<ReviewTranslationDetail>();
    }
}
