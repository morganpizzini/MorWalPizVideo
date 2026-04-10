using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces
{
    public interface IInsightAgentService
    {
        /// <summary>
        /// Discovers news items for a specific topic using AI-powered search
        /// </summary>
        /// <param name="topic">The topic to search for</param>
        /// <returns>List of discovered news items</returns>
        Task<IList<InsightNewsItem>> DiscoverNewsAsync(InsightTopic topic);

        /// <summary>
        /// Ranks a list of news items using the composite ranking algorithm
        /// </summary>
        /// <param name="newsItems">News items to rank</param>
        /// <returns>Ranked list of news items</returns>
        Task<IList<InsightNewsItem>> RankNewsItemsAsync(IList<InsightNewsItem> newsItems);

        /// <summary>
        /// Generates a content plan based on accepted news items
        /// </summary>
        /// <param name="topicId">The topic ID</param>
        /// <param name="newsItemIds">IDs of news items to base the plan on</param>
        /// <param name="contentType">Type of content to generate</param>
        /// <param name="targetPlatforms">Target platforms for the content</param>
        /// <returns>Generated content plan</returns>
        Task<InsightContentPlan> GenerateContentPlanAsync(
            string topicId,
            IList<string> newsItemIds,
            ContentPlanType contentType,
            IList<string> targetPlatforms);
    }
}