using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents a video from YouTube or another source
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record VideoContent : BaseEntity
    {
        /// <summary>
        /// YouTube or platform-specific video identifier
        /// </summary>
        [DataMember]
        [BsonElement("videoId")]
        public string VideoId { get; init; }

        /// <summary>
        /// Title of the video
        /// </summary>
        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Description of the video
        /// </summary>
        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// URL of the video thumbnail
        /// </summary>
        [DataMember]
        [BsonElement("thumbnailUrl")]
        public string ThumbnailUrl { get; init; } = string.Empty;

        /// <summary>
        /// URL of the video 
        /// </summary>
        [DataMember]
        [BsonElement("url")]
        public string Url { get; init; } = string.Empty;

        /// <summary>
        /// Categories associated with this video
        /// </summary>
        [DataMember]
        [BsonElement("categories")]
        public string[] Categories { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Create a new VideoContent with specified ID and category
        /// </summary>
        public VideoContent(string videoId, string category) 
            : this(videoId, string.Empty, string.Empty, videoId, string.Empty, new[] { category })
        {
        }

        /// <summary>
        /// Create a new VideoContent with detailed information
        /// </summary>
        public VideoContent(string videoId, string title, string description, string thumbnailUrl, string url, string[] categories)
        {
            VideoId = videoId;
            Title = title;
            Description = description;
            ThumbnailUrl = thumbnailUrl;
            Url = url;
            Categories = categories;
        }
    }
}
