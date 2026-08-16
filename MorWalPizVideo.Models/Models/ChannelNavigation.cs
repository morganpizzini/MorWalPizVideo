using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum NavigationItemType
{
    Page,
    Internal,
    External
}

[DataContract]
public sealed record NavigationMenuItem
{
    [DataMember]
    [BsonElement("type")]
    public NavigationItemType Type { get; init; }

    [DataMember]
    [BsonElement("pageId")]
    public string? PageId { get; init; }

    [DataMember]
    [BsonElement("targetUrl")]
    public string TargetUrl { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("displayText")]
    public string DisplayText { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("column")]
    public int Column { get; init; }

    [DataMember]
    [BsonElement("displayOrder")]
    public int DisplayOrder { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public sealed record ChannelNavigation : BaseEntity
{
    [DataMember]
    [BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [DataMember]
    [BsonElement("headerItems")]
    public IReadOnlyList<NavigationMenuItem> HeaderItems { get; init; } = [];

    [DataMember]
    [BsonElement("footerColumnCount")]
    public int FooterColumnCount { get; init; } = 1;

    [DataMember]
    [BsonElement("footerItems")]
    public IReadOnlyList<NavigationMenuItem> FooterItems { get; init; } = [];

    [DataMember]
    [BsonElement("updatedDateTime")]
    public DateTime UpdatedDateTime { get; init; } = DateTime.UtcNow;
}

public sealed record PublicNavigation(
    IReadOnlyList<PublicNavigationItem> HeaderItems,
    int FooterColumnCount,
    IReadOnlyList<PublicNavigationItem> FooterItems);

public sealed record PublicNavigationItem(
    string DisplayText,
    string TargetUrl,
    bool OpenInNewTab,
    int Column,
    int DisplayOrder);