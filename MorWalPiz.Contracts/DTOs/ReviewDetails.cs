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

        [Required]
        [Description("Tags del video")]
        public string Tags { get; set; } = string.Empty;
    }
    public class ReviewDetails
    {
        [Required]
        [Description("Titolo originale")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Description("Lista delle traduzioni effettuate")]
        public IList<ReviewTranslationDetail> Translations { get; set; } = new List<ReviewTranslationDetail>();
    }

    public class ReviewDetails1
    {
        [Required]
        [Description("Parole chiavi o descrizione originale")]
        public string Name { get; set; } = string.Empty;
        [Required]
        [Description("Elemento elaborato")]
        public string ProcessElement { get; set; } = string.Empty;
        [Required]
        [Description("Tag specifici e pertinenti")]
        public string Tags { get; set; } = string.Empty;

        [Required]
        [Description("Titolo del video")]
        public string Title { get; set; } = string.Empty;
        [Required]
        [Description("Descrizione del video")]
        public string Description { get; set; } = string.Empty;
    }
}
