using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Jobs;

/// <summary>
/// Hangfire recurring job that refreshes YouTube video metadata (title, description,
/// thumbnail, viewCount, etc.) for all stored YouTubeContent documents.
/// Schedule: daily (or every N hours via appsettings "YouTubeSyncCron").
/// </summary>
public class YouTubeSyncJob
{
  private readonly IExternalDataService _externalDataService;
  private readonly ILogger<YouTubeSyncJob> _logger;

  public YouTubeSyncJob(IExternalDataService externalDataService, ILogger<YouTubeSyncJob> logger)
  {
    _externalDataService = externalDataService;
    _logger = logger;
  }

  /// <summary>
  /// Fetches all YouTubeContent from MongoDB, resolves missing or stale
  /// YouTube metadata via the YouTube Data API, and persists any changes.
  /// </summary>
  public async Task ExecuteAsync()
  {
    _logger.LogInformation("YouTubeSyncJob started at {Time}", DateTimeOffset.UtcNow);
    try
    {
      var matches = await _externalDataService.FetchMatches();
      _logger.LogInformation("YouTubeSyncJob completed. Synced {Count} content items.", matches.Count);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "YouTubeSyncJob failed.");
      throw;
    }
  }
}
