using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Type of content to be generated
    /// </summary>
    public enum ContentPlanType
    {
        /// <summary>
        /// Article or blog post
        /// </summary>
        Article,
        
        /// <summary>
        /// Podcast episode
        /// </summary>
        Podcast,
        
        /// <summary>
        /// Social media post (Twitter, Facebook, etc.)
        /// </summary>
        SocialPost,
        
        /// <summary>
        /// Video script
        /// </summary>
        VideoScript,
        
        /// <summary>
        /// Newsletter content
        /// </summary>
        Newsletter
    }

    /// <summary>
    /// Represents a generated content plan based on news items
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record InsightContentPlan : BaseEntity
    {
        [JsonConstructor]
        public InsightContentPlan(
            string topicId,
            string title,
            ContentPlanType type,
            string outline,
            string[] generatedFromNewsItemIds,
            string[] targetPlatforms,
            DateTime? generatedAt = null)
        {
            TopicId = topicId;
            Title = title;
            Type = type;
            Outline = outline;
            GeneratedFromNewsItemIds = generatedFromNewsItemIds ?? Array.Empty<string>();
            TargetPlatforms = targetPlatforms ?? Array.Empty<string>();
            GeneratedAt = generatedAt ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Reference to the InsightTopic this plan belongs to
        /// </summary>
        [DataMember]
        [BsonElement("topicId")]
        public string TopicId { get; init; }

        /// <summary>
        /// Title of the content piece
        /// </summary>
        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        /// <summary>
        /// Type of content to generate
        /// </summary>
        [DataMember]
        [BsonElement("type")]
        public ContentPlanType Type { get; init; }

        /// <summary>
        /// Detailed outline or structure of the content
        /// </summary>
        [DataMember]
        [BsonElement("outline")]
        public string Outline { get; init; }

        /// <summary>
        /// IDs of news items this content plan is based on
        /// </summary>
        [DataMember]
        [BsonElement("generatedFromNewsItemIds")]
        public string[] GeneratedFromNewsItemIds { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Target platforms for this content (e.g., "YouTube", "Twitter", "Blog")
        /// </summary>
        [DataMember]
        [BsonElement("targetPlatforms")]
        public string[] TargetPlatforms { get; init; } = Array.Empty<string>();

        /// <summary>
        /// When this content plan was generated
        /// </summary>
        [DataMember]
        [BsonElement("generatedAt")]
        public DateTime GeneratedAt { get; init; }

        /// <summary>
        /// Add a target platform
        /// </summary>
        public InsightContentPlan AddTargetPlatform(string platform)
        {
            var newPlatforms = TargetPlatforms.Append(platform).ToArray();
            return this with { TargetPlatforms = newPlatforms };
        }

        /// <summary>
        /// Remove a target platform
        /// </summary>
        public InsightContentPlan RemoveTargetPlatform(string platform)
        {
            var newPlatforms = TargetPlatforms.Where(p => p != platform).ToArray();
            return this with { TargetPlatforms = newPlatforms };
        }

        /// <summary>
        /// Update the outline
        /// </summary>
        public InsightContentPlan UpdateOutline(string newOutline)
        {
            return this with { Outline = newOutline };
        }

        /// <summary>
        /// Update the title
        /// </summary>
        public InsightContentPlan UpdateTitle(string newTitle)
        {
            return this with { Title = newTitle };
        }
    }
}