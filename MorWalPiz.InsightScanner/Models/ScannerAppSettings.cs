namespace MorWalPiz.InsightScanner.Models
{
    public class ScannerAppSettings
    {
        public string ApiEndpoint { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public string ChannelId { get; set; } = string.Empty;
        public int DefaultMaxPostsPerSource { get; set; } = 5;
    }
}
