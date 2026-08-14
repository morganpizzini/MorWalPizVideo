using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

[DataContract]
public enum QuickLinkKind
{
    [EnumMember] External = 0,
    [EnumMember] Telegram = 1,
    [EnumMember] Instagram = 2,
    [EnumMember] Facebook = 3,
    [EnumMember] Video = 4
}

[BsonIgnoreExtraElements]
[DataContract]
public sealed record QuickLink(
    [property: DataMember][property: BsonElement("kind")] QuickLinkKind Kind,
    [property: DataMember][property: BsonElement("targetUrl")] string TargetUrl,
    [property: DataMember][property: BsonElement("title")] string? Title = null,
    [property: DataMember][property: BsonElement("subtitle")] string? Subtitle = null,
    [property: DataMember][property: BsonElement("label")] string? Label = null,
    [property: DataMember][property: BsonElement("imageUrl")] string? ImageUrl = null,
    [property: DataMember][property: BsonElement("icon")] string? Icon = null,
    [property: DataMember][property: BsonElement("provider")] string? Provider = null);

[BsonIgnoreExtraElements]
[DataContract]
public sealed record QuickLinks(
    [property: DataMember][property: BsonElement("title")] string Title,
    [property: DataMember][property: BsonElement("subtitle")] string? Subtitle,
    [property: DataMember][property: BsonElement("url")] string Url,
    [property: DataMember][property: BsonElement("links")] QuickLink[] Links) : BaseEntity
{
    [DataMember]
    [BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    public static string NormalizeUrl(string? url) => (url ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
}
