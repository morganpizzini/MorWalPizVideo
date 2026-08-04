namespace MorWalPizVideo.BackOffice.Jobs;

public static class JobSignals
{
  public static readonly EventId Started = new(5100, "BackgroundJobStarted");
  public static readonly EventId Completed = new(5101, "BackgroundJobCompleted");
  public static readonly EventId Failed = new(5102, "BackgroundJobFailed");

  public static void LogStarted(ILogger logger, string jobId, DateTimeOffset startedAtUtc) =>
      logger.LogInformation(
          Started,
          "Background job {JobId} has status {JobStatus} at {TimestampUtc}",
          jobId,
          "started",
          startedAtUtc);

  public static void LogCompleted(
      ILogger logger,
      string jobId,
      DateTimeOffset completedAtUtc,
      long durationMilliseconds,
      int? itemCount = null) =>
      logger.LogInformation(
          Completed,
          "Background job {JobId} has status {JobStatus} at {TimestampUtc} after {DurationMilliseconds} ms with {ItemCount} items",
          jobId,
          "completed",
          completedAtUtc,
          durationMilliseconds,
          itemCount);

  public static void LogFailed(
      ILogger logger,
      string jobId,
      DateTimeOffset failedAtUtc,
      long durationMilliseconds,
      Exception exception) =>
      logger.LogError(
          Failed,
          exception,
          "Background job {JobId} has status {JobStatus} at {TimestampUtc} after {DurationMilliseconds} ms",
          jobId,
          "failed",
          failedAtUtc,
          durationMilliseconds);
}