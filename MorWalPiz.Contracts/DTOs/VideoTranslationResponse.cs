using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs
{
    public class VideoTranslationResponse
    {
        [Required]
        [Description("Codice della lingua")]
        public string LanguageCode { get; set; } = string.Empty;
        
        [Required]
        [Description("Titolo tradotto")]
        public string TranslatedTitle { get; set; } = string.Empty;
        
        [Required]
        [Description("Descrizione tradotta")]
        public string TranslatedDescription { get; set; } = string.Empty;
    }
}
