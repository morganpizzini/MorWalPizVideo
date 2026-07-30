namespace MorWalPiz.Contracts.DTOs
{
    /// <summary>
    /// A single publicly visible social post collected by the scanner, not yet classified.
    /// </summary>
    public class RawSocialPostDto
    {
        public string PostUrl { get; set; } = string.Empty;
        public string PostId { get; set; } = string.Empty;
        public string PlatformSource { get; set; } = "Instagram";
        public string Author { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
    }

    /// <summary>
    /// Posts collected from a single registered source (e.g. one Instagram profile URL), newest first.
    /// </summary>
    public class SourceScanBatchDto
    {
        public string SourceUrl { get; set; } = string.Empty;
        public List<RawSocialPostDto> Posts { get; set; } = new();
    }

    /// <summary>
    /// Request sent by the interactive scanner app after collecting posts for a manually triggered run.
    /// </summary>
    public class ManualScanRequest
    {
        public List<SourceScanBatchDto> Sources { get; set; } = new();

        /// <summary>
        /// Maximum number of newest posts to process per source. Defaults to 5 when not provided.
        /// </summary>
        public int? MaxPostsPerSource { get; set; }
    }

    public class SourceScanSummaryDto
    {
        public string SourceUrl { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedDuplicateCount { get; set; }
        public int SkippedNotNewsCount { get; set; }
        public string? Error { get; set; }
    }

    public class ManualScanResponseDto
    {
        public List<SourceScanSummaryDto> SourceSummaries { get; set; } = new();
        public List<string> CreatedNewsItemIds { get; set; } = new();
    }
}
