namespace MorWalPiz.VideoImporter.Models
{
    public class VideoTranslationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new List<string>();
    }
}
