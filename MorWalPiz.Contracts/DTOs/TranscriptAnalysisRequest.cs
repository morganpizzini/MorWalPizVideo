namespace MorWalPiz.Contracts.DTOs
{
    public class TranscriptAnalysisRequest
    {
        public string Transcript { get; set; } = string.Empty;
        public string? Context { get; set; }
    }
}