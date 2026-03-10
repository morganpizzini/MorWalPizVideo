using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Lightweight reference to a video used in collections
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record VideoRef
    {
        [JsonConstructor]
        public VideoRef(string youtubeId, CategoryRef[]? categories = null, string title = "", string description = "", DateTime publishedAt = default)
        {
            YoutubeId = youtubeId;
            Categories = categories ?? Array.Empty<CategoryRef>();
            Title = title;
            Description = description;
            PublishedAt = publishedAt;
        }

        [DataMember]
        [BsonElement("youtubeId")]
        public string YoutubeId { get; init; }

        [DataMember]
        [BsonElement("categories")]
        public CategoryRef[] Categories { get; init; } = Array.Empty<CategoryRef>();

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; } = "";

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; } = "";

        [DataMember]
        [BsonElement("publishedAt")]
        public DateTime PublishedAt { get; init; } = DateTime.MinValue;

        [DataMember]
        [BsonElement("creationDateTime")]
        public DateTime CreationDateTime { get; init; } = DateTime.Now;
        

        // Convert VideoRef to VideoDisplayItem for display in UI
        public VideoDisplayItem ToDisplayItem()
        {
            return VideoDisplayItem.CreateBasicSingleVideo(YoutubeId, Categories);
        }
        
        // Create VideoRef from VideoDisplayItem
        public static VideoRef FromDisplayItem(VideoDisplayItem displayItem)
        {
            return new VideoRef(displayItem.PrimaryVideoId, displayItem.Categories);
        }
    }
}