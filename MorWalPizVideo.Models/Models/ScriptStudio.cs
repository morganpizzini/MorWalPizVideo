using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public enum ScriptStudioOperation
{
    Expand,
    Rewrite,
    Structure
}

public record ScriptStudioChannelData : BaseEntity
{
    [BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [BsonElement("script")] public string Script { get; init; } = string.Empty;
    [BsonElement("savedPrompt")] public string SavedPrompt { get; init; } = string.Empty;
    [BsonElement("examples")] public string Examples { get; init; } = string.Empty;
    [BsonElement("style")] public string Style { get; init; } = string.Empty;
    [BsonElement("generalContext")] public string GeneralContext { get; init; } = string.Empty;
    [BsonElement("savedResult")] public string SavedResult { get; init; } = string.Empty;
    [BsonElement("format")] public string Format { get; init; } = "markdown";
}

public record ScriptStudioGlobalPrompt : BaseEntity
{
    [BsonElement("prompt")] public string Prompt { get; init; } = string.Empty;
    [BsonElement("version")] public int Version { get; init; } = 1;
}

public record ScriptStudioAuditEvent : BaseEntity
{
    [BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [BsonElement("operation")] public ScriptStudioOperation Operation { get; init; }
    [BsonElement("success")] public bool Success { get; init; }
    [BsonElement("deniedQuota")] public bool DeniedQuota { get; init; }
    [BsonElement("durationMilliseconds")] public long DurationMilliseconds { get; init; }
    [BsonElement("quotaBefore")] public int QuotaBefore { get; init; }
    [BsonElement("quotaAfter")] public int QuotaAfter { get; init; }
    [BsonElement("deployment")] public string Deployment { get; init; } = string.Empty;
    [BsonElement("correlationId")] public string CorrelationId { get; init; } = string.Empty;
    [BsonElement("promptVersion")] public int? PromptVersion { get; init; }
    [BsonElement("expiresAtUtc")] public DateTime ExpiresAtUtc { get; init; }
}