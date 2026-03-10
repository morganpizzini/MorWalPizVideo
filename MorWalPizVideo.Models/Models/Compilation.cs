using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents a compilation of videos with a title, description, and URL
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Compilation : BaseEntity
    {
        [JsonConstructor]
        public Compilation(string title, string description, string url, VideoRef[] videos)
        {
            Title = title;
            Description = description;
            Url = url;
            Videos = videos ?? Array.Empty<VideoRef>();
        }

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        [DataMember]
        [BsonElement("url")]
        public string Url { get; init; }

        [DataMember]
        [BsonElement("videos")]
        public VideoRef[] Videos { get; init; } = Array.Empty<VideoRef>();
    }
}
