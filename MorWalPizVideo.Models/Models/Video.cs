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
            DateTime publishedAt,
            string thumbnail,
            string duration,
            CategoryRef[]? categories = null)
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
            Categories = categories ?? Array.Empty<CategoryRef>();
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
        public DateTime PublishedAt { get; init; }
        
        [DataMember]
        [BsonElement("thumbnail")]
        public string Thumbnail { get; init; }
        
        [DataMember]
        [BsonElement("duration")]
        public string Duration { get; init; }
        
        [DataMember]
        [BsonElement("categories")]
        public CategoryRef[] Categories { get; init; } = Array.Empty<CategoryRef>();
        
        // Constructor to create minimal video from a video reference
        public Video(string youtubeId, CategoryRef[] categories) 
            : this(youtubeId, string.Empty, string.Empty, 0, 0, 0, DateTime.MinValue, string.Empty, string.Empty, categories)
        {
        }
        
        // Convert to VideoRef for lightweight references
        public VideoRef ToVideoRef() => new VideoRef(YoutubeId, Categories, Title, Description, PublishedAt);
    }
}