using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public record LoginAttempt : BaseEntity
{
    [BsonElement("ipAddress")]
    public string IpAddress { get; init; } = string.Empty;

    [BsonElement("username")]
    public string Username { get; init; } = string.Empty;

    [BsonElement("isSuccessful")]
    public bool IsSuccessful { get; init; }

    [BsonElement("attemptTime")]
    public DateTime AttemptTime { get; init; } = DateTime.UtcNow;

    [BsonElement("userAgent")]
    public string UserAgent { get; init; } = string.Empty;

    [BsonElement("failureReason")]
    public string FailureReason { get; init; } = string.Empty;
}
