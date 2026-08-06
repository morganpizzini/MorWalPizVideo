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

        [DataMember]
        [BsonElement("shortLinkUrl")]
        public string? ShortLinkUrl { get; init; }

        [DataMember]
        [BsonElement("directVideoUrl")]
        public string? DirectVideoUrl { get; init; }

        public YouTubeVideoLink() { }

        public YouTubeVideoLink(
            string contentCreatorName,
            string youtubeVideoId,
            string imageName,
            ShortLink? shortLink = null,
            string? shortLinkUrl = null,
            string? directVideoUrl = null)
        {
            ContentCreatorName = contentCreatorName;
            YouTubeVideoId = youtubeVideoId;
            ImageName = imageName;
            ShortLink = shortLink;
            ShortLinkUrl = shortLinkUrl;
            DirectVideoUrl = directVideoUrl;
        }
    }
}
