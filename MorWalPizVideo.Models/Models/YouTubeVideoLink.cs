using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record YouTubeVideoLink
    {
        [DataMember]
        [BsonElement("contentCreatorName")]
        public string ContentCreatorName { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("youtubeVideoId")]
        public string YouTubeVideoId { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("imageName")]
        public string ImageName { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("shortLink")]
        public ShortLink? ShortLink { get; init; }

        public YouTubeVideoLink() { }

        public YouTubeVideoLink(string contentCreatorName, string youtubeVideoId, string imageName, ShortLink? shortLink = null)
        {
            ContentCreatorName = contentCreatorName;
            YouTubeVideoId = youtubeVideoId;
            ImageName = imageName;
            ShortLink = shortLink;
        }
    }
}
