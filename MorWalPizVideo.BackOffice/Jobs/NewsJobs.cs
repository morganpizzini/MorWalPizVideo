using System.Diagnostics;

namespace MorWalPizVideo.BackOffice.Jobs
{
    public class NewsJobs
    {
        public const string JobId = "news-job";
        public const string CronSchedule = "0 18 * * 0";
        private readonly ILogger<NewsJobs> _logger;

        public NewsJobs(ILogger<NewsJobs> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            JobSignals.LogStarted(_logger, JobId, DateTimeOffset.UtcNow);
            // retrieve short published in the current week
            // retrieve video published in the current week
            // compose site page
            // create a message pointing site page, populate VideoReelIds and ShortReelIds
            // publish message to telegram/discord
            JobSignals.LogCompleted(_logger, JobId, DateTimeOffset.UtcNow, stopwatch.ElapsedMilliseconds);
            return Task.CompletedTask;
        }
    }
}
