using MorWalPiz.Contracts.DTOs;
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

        /// <summary>
        /// Classifies a raw social post collected by the scanner as news or not, relative to the given topic
        /// </summary>
        /// <param name="topic">The topic the post was scanned for</param>
        /// <param name="post">The raw post collected from the public source</param>
        /// <returns>Classification result including decision, score, reason and a suggested title/summary</returns>
        Task<PostClassificationResult> ClassifyPostAsync(InsightTopic topic, RawSocialPostDto post);

        /// <summary>
        /// Analyzes a batch of YouTube video comments and derives ShortContent ideas/hints relevant to the topic
        /// </summary>
        Task<IList<InsightNewsItem>> AnalyzeVideoCommentsAsync(
            InsightTopic topic,
            string videoId,
            string videoTitle,
            string videoUrl,
            string channelName,
            IList<VideoCommentDto> comments,
            InsightSourceKind sourceKind = InsightSourceKind.ShortContent,
            string description = "");

        Task<IList<InsightNewsItem>> CondenseCommentNewsAsync(InsightTopic topic, IList<InsightNewsItem> candidates);
    }

    /// <summary>
    /// AI classification outcome for a single scanned social post
    /// </summary>
    public class PostClassificationResult
    {
        public bool IsNews { get; set; }
        public double RelevanceScore { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string SuggestedTitle { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}