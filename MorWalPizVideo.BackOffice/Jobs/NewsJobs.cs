namespace MorWalPizVideo.BackOffice.Jobs
{
    public class NewsJobs
    {
        private readonly ILogger<NewsJobs> _logger;

        public NewsJobs(ILogger<NewsJobs> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync()
        {
            // retrieve short published in the current week
            // retrieve video published in the current week
            // compose site page
            // create a message pointing site page, populate VideoReelIds and ShortReelIds
            // publish message to telegram/discord
            _logger.LogInformation("ExampleJob executed at: {Time}", DateTime.Now);
            return Task.CompletedTask;
        }
    }
}
