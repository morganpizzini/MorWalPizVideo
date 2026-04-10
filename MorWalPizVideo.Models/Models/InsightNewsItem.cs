using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Status of a news item in the insight workflow
    /// </summary>
    public enum InsightNewsStatus
    {
        /// <summary>
        /// Newly discovered, pending review
        /// </summary>
        Pending,
        
        /// <summary>
        /// Accepted by user for content generation
        /// </summary>
        Accepted,
        
        /// <summary>
        /// Rejected by user
        /// </summary>
        Rejected,
        
        /// <summary>
        /// Content has been generated from this news item
        /// </summary>
        Generated
    }

    /// <summary>
    /// Represents a discovered news item related to an insight topic
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record InsightNewsItem : BaseEntity
    {
        [JsonConstructor]
        public InsightNewsItem(
            string topicId, 
            string title, 
            string summary, 
            string sourceUrl, 
            string sourceName,
            InsightNewsStatus status = InsightNewsStatus.Pending,
            int starRating = 0,
            double aiRelevanceScore = 0.0,
            DateTime? discoveredAt = null)
        {
            TopicId = topicId;
            Title = title;
            Summary = summary;
            SourceUrl = sourceUrl;
            SourceName = sourceName;
            Status = status;
            StarRating = starRating;
            AIRelevanceScore = aiRelevanceScore;
            DiscoveredAt = discoveredAt ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Reference to the InsightTopic this news item belongs to
        /// </summary>
        [DataMember]
        [BsonElement("topicId")]
        public string TopicId { get; init; }

        /// <summary>
        /// Title of the news item
        /// </summary>
        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        /// <summary>
        /// Summary or excerpt of the news content
        /// </summary>
        [DataMember]
        [BsonElement("summary")]
        public string Summary { get; init; }

        /// <summary>
        /// URL to the original news source
        /// </summary>
        [DataMember]
        [BsonElement("sourceUrl")]
        public string SourceUrl { get; init; }

        /// <summary>
        /// Name of the news source
        /// </summary>
        [DataMember]
        [BsonElement("sourceName")]
        public string SourceName { get; init; }

        /// <summary>
        /// Current status of the news item in the workflow
        /// </summary>
        [DataMember]
        [BsonElement("status")]
        public InsightNewsStatus Status { get; init; }

        /// <summary>
        /// User's star rating (0-5) indicating interest/preference
        /// </summary>
        [DataMember]
        [BsonElement("starRating")]
        public int StarRating { get; init; }

        /// <summary>
        /// AI-calculated relevance score (0.0-1.0)
        /// </summary>
        [DataMember]
        [BsonElement("aiRelevanceScore")]
        public double AIRelevanceScore { get; init; }

        /// <summary>
        /// When this news item was discovered
        /// </summary>
        [DataMember]
        [BsonElement("discoveredAt")]
        public DateTime DiscoveredAt { get; init; }

        /// <summary>
        /// Update the status of the news item
        /// </summary>
        public InsightNewsItem UpdateStatus(InsightNewsStatus newStatus)
        {
            return this with { Status = newStatus };
        }

        /// <summary>
        /// Update the star rating
        /// </summary>
        public InsightNewsItem UpdateStarRating(int rating)
        {
            // Clamp rating between 0 and 5
            var clampedRating = Math.Max(0, Math.Min(5, rating));
            return this with { StarRating = clampedRating };
        }

        /// <summary>
        /// Calculate a composite ranking score based on multiple factors
        /// </summary>
        public double CalculateRankingScore(double sourceTrustScore = 0.5)
        {
            // Calculate recency score (decays over 30 days)
            var daysSinceDiscovery = (DateTime.UtcNow - DiscoveredAt).TotalDays;
            var recencyScore = Math.Max(0, 1 - (daysSinceDiscovery / 30.0));

            // Normalize star rating to 0-1 range
            var userPreferenceScore = StarRating / 5.0;

            // Weighted combination: 45% AI, 20% recency, 20% source trust, 15% user preference
            var compositeScore = 
                (0.45 * AIRelevanceScore) + 
                (0.20 * recencyScore) + 
                (0.20 * sourceTrustScore) + 
                (0.15 * userPreferenceScore);

            return compositeScore;
        }
    }
}