using MorWalPizVideo.Server.Services;
using System.Diagnostics;

namespace MorWalPizVideo.BackOffice.Jobs;

/// <summary>
/// Hangfire recurring job that refreshes YouTube video metadata (title, description,
/// thumbnail, viewCount, etc.) for all stored YouTubeContent documents.
/// Schedule: daily (or every N hours via appsettings "YouTubeSyncCron").
/// </summary>
public class YouTubeSyncJob
{
  public const string JobId = "youtube-sync-job";
  public const string CronConfigurationKey = "YouTubeSyncCron";
  public const string DefaultCronSchedule = "0 3 * * *";
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
    var stopwatch = Stopwatch.StartNew();
    JobSignals.LogStarted(_logger, JobId, DateTimeOffset.UtcNow);
    try
    {
      var matches = await _externalDataService.FetchMatches();
      JobSignals.LogCompleted(_logger, JobId, DateTimeOffset.UtcNow, stopwatch.ElapsedMilliseconds, matches.Count);
    }
    catch (Exception ex)
    {
      JobSignals.LogFailed(_logger, JobId, DateTimeOffset.UtcNow, stopwatch.ElapsedMilliseconds, ex);
      throw;
    }
  }
}
