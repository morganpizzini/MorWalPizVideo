using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
  public class Review
  {
    [Required]
    [Description("Lista delle degli elementi elaborati")]
    public IList<ReviewDetails1> Videos { get; set; } = new List<ReviewDetails1>();
  }

  public class TranslatedReview
  {
    [Required]
    [Description("Lista delle degli elementi elaborati")]
    public IList<ReviewDetails> Videos { get; set; } = new List<ReviewDetails>();
  }

    public class ReviewApiVideoResponse
    {
        public string Name { get; set; } = string.Empty;
        public string ProcessElement { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public IList<ReviewApiVideoTranslation> Translations { get; set; } = new List<ReviewApiVideoTranslation>();
    }
    public class ReviewApiVideoTranslation
    {
        public string Language { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
