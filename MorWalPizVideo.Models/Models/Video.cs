using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    [method: JsonConstructor]
    public record Video(
        [property: DataMember][property: BsonElement("youtubeId")] string YoutubeId,
        [property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Description,
        [property: DataMember][property: BsonElement("views")] int Views,
        [property: DataMember][property: BsonElement("likes")] int Likes,
        [property: DataMember][property: BsonElement("comments")] int Comments,
        [property: DataMember][property: BsonElement("publishedAt")] DateOnly PublishedAt,
        [property: DataMember][property: BsonElement("thumbnail")] string Thumbnail,
        [property: DataMember][property: BsonElement("duration")] string Duration,
        [property: DataMember][property: BsonElement("category")] string Category = "") : BaseEntity
    {
        public Video(string youtubeId, string category) : this(youtubeId, string.Empty, string.Empty,0,0,0,DateOnly.MinValue, string.Empty,string.Empty, category)
        {
        }
    }
}
