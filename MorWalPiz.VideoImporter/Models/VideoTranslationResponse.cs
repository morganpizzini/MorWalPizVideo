namespace MorWalPiz.VideoImporter.Models
{
    public class VideoTranslationResponse
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string TranslatedTitle { get; set; } = string.Empty;
        public string TranslatedDescription { get; set; } = string.Empty;
    }
}
