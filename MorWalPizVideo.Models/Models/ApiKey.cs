using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public record ApiKey : BaseEntity
{
    [BsonElement("key")]
    public string Key { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("lastUsedAt")]
    public DateTime? LastUsedAt { get; init; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    [BsonElement("rateLimitPerMinute")]
    public int RateLimitPerMinute { get; init; } = 60;

    [BsonElement("allowedIpAddresses")]
    public List<string> AllowedIpAddresses { get; init; } = new();
}