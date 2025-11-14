using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record YTChannel(
        [property: DataMember][property: BsonElement("channelId")] string ChannelId,
        [property: DataMember][property: BsonElement("channelName")] string ChannelName) : BaseEntity
    {
        [DataMember]
        [BsonElement("videos")]
        public List<YouTubeVideo> Videos { get; init; } = new List<YouTubeVideo>();
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record YouTubeVideo
    {
        [DataMember]
        [BsonElement("videoId")]
        public string VideoId { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("lastCommentDate")]
        public DateTime LastCommentDate { get; init; } = DateTime.MinValue;

        [DataMember]
        [BsonElement("videoIdeas")]
        public List<VideoIdea> VideoIdeas { get; init; } = new List<VideoIdea>();
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record VideoIdea
    {
        [DataMember]
        [BsonElement("idea")]
        public string Idea { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("commentExcerpt")]
        public string CommentExcerpt { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("creationDate")]
        public DateTime CreationDate { get; init; } = DateTime.Now;

        [DataMember]
        [BsonElement("sentiment")]
        public string Sentiment { get; init; } = string.Empty;
    }
}
