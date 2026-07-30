namespace MorWalPiz.InsightScanner.Models
{
    public class ScannerAppSettings
    {
        public string ApiEndpoint { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public int DefaultMaxPostsPerSource { get; set; } = 5;
    }
}
