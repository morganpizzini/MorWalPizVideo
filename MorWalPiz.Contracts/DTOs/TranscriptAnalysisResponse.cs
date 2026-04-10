namespace MorWalPiz.Contracts.DTOs
{
    public class TranscriptAnalysisResponse
    {
        public string SeoDescription { get; set; } = string.Empty;
        public List<string> Titles { get; set; } = new();
        public List<string> Descriptions { get; set; } = new();
        public List<string> Hashtags { get; set; } = new();
    }
}