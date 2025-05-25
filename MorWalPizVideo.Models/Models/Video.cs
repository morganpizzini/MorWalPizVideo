using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents a complete YouTube video with all details fetched from API
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Video : BaseEntity
    {
        [JsonConstructor]
        public Video(
            string youtubeId,
            string title,
            string description,
            int views,
            int likes,
            int comments,
            DateOnly publishedAt,
            string thumbnail,
            string duration,
            string category = "")
        {
            YoutubeId = youtubeId;
            Title = title;
            Description = description;
            Views = views;
            Likes = likes;
            Comments = comments;
            PublishedAt = publishedAt;
            Thumbnail = thumbnail;
            Duration = duration;
            Category = category;
        }

        [DataMember]
        [BsonElement("youtubeId")]
        public string YoutubeId { get; init; }

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }
        
        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }
        
        [DataMember]
        [BsonElement("views")]
        public int Views { get; init; }
        
        [DataMember]
        [BsonElement("likes")]
        public int Likes { get; init; }
        
        [DataMember]
        [BsonElement("comments")]
        public int Comments { get; init; }
        
        [DataMember]
        [BsonElement("publishedAt")]
        public DateOnly PublishedAt { get; init; }
        
        [DataMember]
        [BsonElement("thumbnail")]
        public string Thumbnail { get; init; }
        
        [DataMember]
        [BsonElement("duration")]
        public string Duration { get; init; }
        
        [DataMember]
        [BsonElement("category")]
        public string Category { get; init; } = "";
        
        // Constructor to create minimal video from a video reference
        public Video(string youtubeId, string category) 
            : this(youtubeId, string.Empty, string.Empty, 0, 0, 0, DateOnly.MinValue, string.Empty, string.Empty, category)
        {
        }
        
        // Convert to VideoRef for lightweight references
        public VideoRef ToVideoRef() => new VideoRef(YoutubeId, Category);
    }
}
