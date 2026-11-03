using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public sealed record VideoImporterChannelSettings : BaseEntity
{
    [BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; init; }

    [BsonElement("payloadJson")]
    public string PayloadJson { get; init; } = string.Empty;

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; init; }
}
