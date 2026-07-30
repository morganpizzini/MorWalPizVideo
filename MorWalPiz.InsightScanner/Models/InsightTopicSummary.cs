namespace MorWalPiz.InsightScanner.Models
{
    /// <summary>
    /// Lightweight client-side projection of the server's InsightTopic entity.
    /// Only the fields the scanner needs to operate are declared here.
    /// </summary>
    public class InsightTopicSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string[] PreferredSources { get; set; } = Array.Empty<string>();
    }
}
