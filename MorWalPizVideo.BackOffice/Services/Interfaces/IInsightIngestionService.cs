using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces
{
  public interface IInsightIngestionService
  {
    /// <summary>
    /// Processes a manually triggered scan run: dedups against already stored news items and
    /// each source's cursor, classifies each new candidate post and persists it as AutoDetected when relevant.
    /// </summary>
    Task<ManualScanResponseDto> ProcessManualScanAsync(InsightTopic topic, ManualScanRequest request);

    /// <summary>
    /// Fetches new YouTube comments for a channel since the last processed comment per video,
    /// derives ShortContent ideas/hints via AI analysis and persists them as insight items for the topic.
    /// </summary>
    Task<ScanShortContentResponseDto> ProcessShortContentScanAsync(InsightTopic topic, string channelName, int videos, int commentsNumber);
  }
}
