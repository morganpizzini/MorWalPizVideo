using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Tracks the last post seen for a registered source within a topic, so the scanner
    /// can resume from where it left off and avoid re-processing already scanned posts.
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record InsightSourceCursor : BaseEntity
    {
        [JsonConstructor]
        public InsightSourceCursor(
            string topicId,
            string sourceUrl,
            string? lastSeenPostId = null,
            string? lastSeenPostUrl = null,
            DateTime? lastScanAt = null)
        {
            TopicId = topicId;
            SourceUrl = sourceUrl;
            LastSeenPostId = lastSeenPostId ?? string.Empty;
            LastSeenPostUrl = lastSeenPostUrl ?? string.Empty;
            LastScanAt = lastScanAt ?? DateTime.UtcNow;
        }

        [DataMember]
        [BsonElement("topicId")]
        public string TopicId { get; init; }

        [DataMember]
        [BsonElement("sourceUrl")]
        public string SourceUrl { get; init; }

        [DataMember]
        [BsonElement("lastSeenPostId")]
        public string LastSeenPostId { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("lastSeenPostUrl")]
        public string LastSeenPostUrl { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("lastScanAt")]
        public DateTime LastScanAt { get; init; }

        [DataMember]
        [BsonElement("channelId")]
        public string ChannelId { get; init; } = string.Empty;

        public InsightSourceCursor UpdateCursor(string? postId, string? postUrl, DateTime scanAt) =>
            this with
            {
                LastSeenPostId = string.IsNullOrEmpty(postId) ? LastSeenPostId : postId,
                LastSeenPostUrl = string.IsNullOrEmpty(postUrl) ? LastSeenPostUrl : postUrl,
                LastScanAt = scanAt
            };
    }
}
