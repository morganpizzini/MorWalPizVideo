using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public enum SocialAssetStatus
{
    Available,
    Consumed,
    Expired
}

public sealed record SocialAsset : BaseEntity
{
    [BsonElement("assetId")] public string AssetId { get; init; } = string.Empty;
    [BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [BsonElement("storageKey")] public string StorageKey { get; init; } = string.Empty;
    [BsonElement("contentType")] public string ContentType { get; init; } = string.Empty;
    [BsonElement("size")] public long Size { get; init; }
    [BsonElement("sha256")] public string Sha256 { get; init; } = string.Empty;
    [BsonElement("mediaKind")] public string MediaKind { get; init; } = string.Empty;
    [BsonElement("createdAt")] public DateTimeOffset CreatedAt { get; init; }
    [BsonElement("expiresAt")] public DateTimeOffset ExpiresAt { get; init; }
    [BsonElement("status")] public SocialAssetStatus Status { get; init; }
    [BsonElement("consumedAt")] public DateTimeOffset? ConsumedAt { get; init; }
    [BsonElement("idempotencyKey")] public string IdempotencyKey { get; init; } = string.Empty;
}
