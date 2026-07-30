using MorWalPiz.Contracts.DTOs;
using MorWalPiz.InsightScanner.Models;

namespace MorWalPiz.InsightScanner.Services
{
    public interface IBackOfficeInsightClient
    {
        Task<List<InsightTopicSummary>> GetTopicsAsync();

        Task<ManualScanResponseDto> SubmitManualScanAsync(string topicId, ManualScanRequest request);
    }
}
