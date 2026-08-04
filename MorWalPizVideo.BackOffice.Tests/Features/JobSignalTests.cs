using Microsoft.Extensions.Logging;
using MorWalPizVideo.BackOffice.Jobs;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class JobSignalTests
{
  [Fact]
  public async Task News_job_emits_stable_started_and_completed_signals()
  {
    var logger = new CapturingLogger<NewsJobs>();
    var job = new NewsJobs(logger);

    await job.ExecuteAsync();

    Assert.Equal([JobSignals.Started, JobSignals.Completed], logger.EventIds);
    Assert.All(logger.States, state => Assert.Contains(NewsJobs.JobId, state));
    Assert.Contains(logger.States, state => state.Contains("started", StringComparison.Ordinal));
    Assert.Contains(logger.States, state => state.Contains("completed", StringComparison.Ordinal));
  }

  private sealed class CapturingLogger<T> : ILogger<T>
  {
    public List<EventId> EventIds { get; } = [];
    public List<string> States { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
      EventIds.Add(eventId);
      States.Add(formatter(state, exception));
    }
  }
}