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
        public VideoRef(string youtubeId, string category = "")
        {
            YoutubeId = youtubeId;
            Category = category;
        }

        [DataMember]
        [BsonElement("youtubeId")]
        public string YoutubeId { get; init; }

        [DataMember]
        [BsonElement("category")]
        public string Category { get; init; } = "";
        
        // Convert VideoRef to VideoDisplayItem for display in UI
        public VideoDisplayItem ToDisplayItem()
        {
            return VideoDisplayItem.CreateBasicSingleVideo(YoutubeId, Category);
        }
        
        // Create VideoRef from VideoDisplayItem
        public static VideoRef FromDisplayItem(VideoDisplayItem displayItem)
        {
            return new VideoRef(displayItem.PrimaryVideoId, displayItem.Category);
        }
    }
}
