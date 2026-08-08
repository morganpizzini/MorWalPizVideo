using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public record ImpersonationGrant : BaseEntity
{
    [BsonElement("grantHash")]
    public string GrantHash { get; init; } = string.Empty;

    [BsonElement("actorUserId")]
    public string ActorUserId { get; init; } = string.Empty;

    [BsonElement("targetUserId")]
    public string TargetUserId { get; init; } = string.Empty;

    [BsonElement("issuedAt")]
    public DateTime IssuedAt { get; init; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; init; }

    [BsonElement("redeemedAt")]
    public DateTime? RedeemedAt { get; init; }

    [BsonElement("sessionId")]
    public string? SessionId { get; init; }
}

public record ImpersonationSession : BaseEntity
{
    [BsonElement("sessionHash")]
    public string SessionHash { get; init; } = string.Empty;

    [BsonElement("actorUserId")]
    public string ActorUserId { get; init; } = string.Empty;

    [BsonElement("targetUserId")]
    public string TargetUserId { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; init; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; init; }

    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; init; }

    [BsonElement("endReason")]
    public string? EndReason { get; init; }
}

public record ImpersonationAuditEvent : BaseEntity
{
    [BsonElement("eventType")]
    public string EventType { get; init; } = string.Empty;

    [BsonElement("actorUserId")]
    public string? ActorUserId { get; init; }

    [BsonElement("targetUserId")]
    public string? TargetUserId { get; init; }

    [BsonElement("sessionId")]
    public string? SessionId { get; init; }

    [BsonElement("requestPath")]
    public string? RequestPath { get; init; }

    [BsonElement("reason")]
    public string? Reason { get; init; }

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; init; }
}