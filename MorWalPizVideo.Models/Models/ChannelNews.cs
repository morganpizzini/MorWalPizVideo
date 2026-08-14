using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum ChannelNewsStatus
{
    Draft,
    Scheduled,
    Published,
    Archived
}

[BsonIgnoreExtraElements]
[DataContract]
public record ChannelNewsImage
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

    [DataMember]
    [BsonElement("displayOrder")]
    public int DisplayOrder { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public record ChannelNews : BaseEntity
{
    [DataMember]
    [BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("title")]
    public string Title { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("subtitle")]
    public string Subtitle { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("descriptionHtml")]
    public string DescriptionHtml { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("images")]
    public IReadOnlyList<ChannelNewsImage> Images { get; init; } = [];

    [DataMember]
    [BsonElement("slug")]
    public string Slug { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("status")]
    public ChannelNewsStatus Status { get; init; } = ChannelNewsStatus.Draft;

    [DataMember]
    [BsonElement("publicationTimeUtc")]
    public DateTime? PublicationTimeUtc { get; init; }

    [DataMember]
    [BsonElement("displayOrder")]
    public int DisplayOrder { get; init; }

    [DataMember]
    [BsonElement("updatedDateTime")]
    public DateTime UpdatedDateTime { get; init; } = DateTime.UtcNow;
}