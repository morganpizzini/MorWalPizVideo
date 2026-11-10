using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public sealed record BackOfficeQuickActionPreferences : BaseEntity
{
    [BsonElement("userId")]
    public string UserId { get; init; } = string.Empty;

    [BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [BsonElement("actions")]
    public List<BackOfficeQuickActionEntry> Actions { get; init; } = [];

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed record BackOfficeQuickActionEntry
{
    public string Id { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string? ActionKey { get; init; }
    public string? Label { get; init; }
    public string? Url { get; init; }
}
