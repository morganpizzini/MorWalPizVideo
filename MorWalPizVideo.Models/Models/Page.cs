using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    public enum PageStatus
    {
        Draft,
        Published
    }

    [DataContract]
    public sealed record PageImage
    {
        [DataMember]
        [BsonElement("storageKey")]
        public string StorageKey { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("publicUrl")]
        public string PublicUrl { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("contentType")]
        public string ContentType { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("width")]
        public int Width { get; init; }

        [DataMember]
        [BsonElement("height")]
        public int Height { get; init; }

        [DataMember]
        [BsonElement("altText")]
        public string AltText { get; init; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record Page(
        [property: DataMember, BsonElement("thumbnailUrl")] string ThumbnailUrl,
        [property: DataMember, BsonElement("title")] string Title,
        [property: DataMember, BsonElement("content")] string Content,
        [property: DataMember, BsonElement("url")] string Url,
        [property: DataMember, BsonElement("videoUrl")] string VideoId) : BaseEntity
    {
        [DataMember]
        [BsonElement("channelId")]
        public string ChannelId { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("status")]
        public PageStatus Status { get; init; } = PageStatus.Draft;

        [DataMember]
        [BsonElement("videoReelIds")]
        public IList<string> VideoReelIds { get; init; } = [];

        [DataMember]
        [BsonElement("shortReelIds")]
        public IList<string> ShortReelIds { get; init; } = [];

        [DataMember]
        [BsonElement("inlineImages")]
        public IReadOnlyList<PageImage> InlineImages { get; init; } = [];

        [DataMember]
        [BsonElement("updatedDateTime")]
        public DateTime UpdatedDateTime { get; init; } = DateTime.UtcNow;

        [BsonIgnore]
        public string ShortContent => !string.IsNullOrEmpty(Content) ? Content[..Math.Min(Content.Length, 120)] : string.Empty;
    }
}
