using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Types of content display in the app
    /// </summary>
    public enum ContentType
    {
        /// <summary>
        /// A single video
        /// </summary>
        SingleVideo,
        
        /// <summary>
        /// A collection of videos
        /// </summary>
        Playlist
    }

    /// <summary>
    /// Represents a media item to display in the app (either a single video or a collection)
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record VideoDisplayItem : BaseEntity
    {
    /// <summary>
        /// Unique identifier for this item
        /// </summary>
        [DataMember]
        [BsonElement("displayId")]
        public string DisplayId { get; init; } = string.Empty;

        /// <summary>
        /// Type of content (single video or playlist)
        /// </summary>
        [DataMember]
        [BsonElement("contentType")]
        public ContentType ContentType { get; init; }

        /// <summary>
        /// Title to display in the UI
        /// </summary>
        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Description to display in the UI
        /// </summary>
        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Thumbnail URL to display in the UI
        /// </summary>
        [DataMember]
        [BsonElement("thumbnailUrl")]
        public string ThumbnailUrl { get; init; } = string.Empty;

        /// <summary>
        /// Main category of this item
        /// </summary>
        [DataMember]
        [BsonElement("category")]
        public string Category { get; init; } = string.Empty;

        /// <summary>
        /// For single videos, this is the ID of the video
        /// </summary>
        [DataMember]
        [BsonElement("primaryVideoId")]
        public string PrimaryVideoId { get; init; } = string.Empty;

        /// <summary>
        /// For playlists, this contains the videos in the playlist
        /// For single videos, this is empty
        /// </summary>
        [DataMember]
        [BsonElement("videoIds")]
        public string[] VideoIds { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Create a new single video display item
        /// </summary>
        public static VideoDisplayItem CreateSingleVideo(string videoId, string title, string description, string thumbnailUrl, string category)
        {
            return new VideoDisplayItem
            {
                DisplayId = videoId,
                ContentType = ContentType.SingleVideo,
                Title = title,
                Description = description,
                ThumbnailUrl = thumbnailUrl,
                Category = category,
                PrimaryVideoId = videoId,
                VideoIds = Array.Empty<string>()
            };
        }

        /// <summary>
        /// Create a new playlist display item
        /// </summary>
        public static VideoDisplayItem CreatePlaylist(string displayId, string title, string description, string thumbnailUrl, string category, string primaryVideoId, string[] videoIds)
        {
            return new VideoDisplayItem
            {
                DisplayId = displayId,
                ContentType = ContentType.Playlist,
                Title = title,
                Description = description,
                ThumbnailUrl = thumbnailUrl,
                Category = category,
                PrimaryVideoId = primaryVideoId,
                VideoIds = videoIds
            };
        }
        
        /// <summary>
        /// Create a basic single video display item with minimal information
        /// </summary>
        public static VideoDisplayItem CreateBasicSingleVideo(string videoId, string category)
        {
            return new VideoDisplayItem
            {
                DisplayId = videoId,
                ContentType = ContentType.SingleVideo,
                Title = string.Empty,
                Description = string.Empty,
                ThumbnailUrl = videoId,
                Category = category,
                PrimaryVideoId = videoId,
                VideoIds = Array.Empty<string>()
            };
        }
    }
}
