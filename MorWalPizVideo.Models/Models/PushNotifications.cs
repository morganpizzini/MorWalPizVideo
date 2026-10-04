using MongoDB.Bson.Serialization.Attributes;

namespace MorWalPizVideo.Server.Models;

/// <summary>Lifecycle of a push dispatch aggregate.</summary>
public enum PushDispatchState { Draft, Queued, Sending, Sent, Failed, Cancelled }

/// <summary>Per-endpoint delivery state inside a dispatch snapshot.</summary>
public enum PushDeliveryStatus { Pending, Sending, Sent, Suppressed, Failed }

/// <summary>Which BackOffice section originated the dispatch.</summary>
public enum PushDispatchScope { Platform, Channel }
public enum PushSubscriptionScope { Channel, Platform }

/// <summary>Browser-supplied encryption material for a Web Push endpoint (RFC 8291).</summary>
[BsonIgnoreExtraElements]
public sealed record PushSubscriptionKeys(string P256dh, string Auth);

/// <summary>
/// Anonymous, per-channel Web Push subscription. Not bound to a <see cref="MorWalPizVideo.Models.Models.User"/>:
/// the visitor proves ownership with a high-entropy credential whose SHA-256 hash is the only stored copy.
/// </summary>
[BsonIgnoreExtraElements]
public sealed record PushChannelSubscription(
    string EndpointHash,
    string Endpoint,
    PushSubscriptionKeys Keys,
    string CredentialHash,
    IReadOnlyList<string> ChannelIds) : BaseEntity
{
    /// <summary>Public application that captured the consent (for example <c>morwalpizvideo</c> or <c>shooting-ita</c>).</summary>
    [BsonElement("applicationKey")]
    public string ApplicationKey { get; init; } = string.Empty;
    [BsonElement("scope")]
    public PushSubscriptionScope Scope { get; init; } = PushSubscriptionScope.Channel;

    /// <summary>False once revoked by the visitor or pruned after a permanent delivery failure.</summary>
    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("consentedAt")]
    public DateTime ConsentedAt { get; init; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; init; }

    [BsonElement("language")]
    public string? Language { get; init; }
}

/// <summary>
/// A named, separately-defined collection of channels used as a platform push audience.
/// Audiences are content targeting groups and are intentionally unrelated to RBAC groups.
/// </summary>
[BsonIgnoreExtraElements]
public sealed record PushAudience(
    string Code,
    string Name,
    IReadOnlyList<string> ChannelIds) : BaseEntity
{
    [BsonElement("description")]
    public string? Description { get; init; }

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Optional notification action button. <see cref="Destination"/> is a same-origin relative path,
/// validated before persistence.
/// </summary>
[BsonIgnoreExtraElements]
public sealed record PushNotificationAction(string Action, string Title, string? Destination);

[BsonIgnoreExtraElements]
public sealed record PushNotificationTemplate(
    string Name,
    string Title,
    string Body,
    string Destination,
    IReadOnlyList<PushNotificationAction> Actions,
    int Version = 1) : BaseEntity
{
    [BsonElement("isActive")] public bool IsActive { get; init; } = true;
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    [BsonElement("createdBy")] public string CreatedBy { get; init; } = string.Empty;
}

/// <summary>Durable push campaign. Recipients are snapshotted at queue time into <see cref="PushDispatchRecipient"/>.</summary>
[BsonIgnoreExtraElements]
public sealed record PushDispatch(
    PushDispatchScope Scope,
    string Title,
    string Body,
    IReadOnlyList<string> ChannelIds,
    PushDispatchState State = PushDispatchState.Draft) : BaseEntity
{
    /// <summary>Recipient application discriminator. Legacy records remain unscoped and are never platform recipients.</summary>
    [BsonElement("applicationKey")]
    public string ApplicationKey { get; init; } = "backoffice";
    /// <summary>Explicit recipient application scopes. Missing on legacy records, which remain BackOffice-only.</summary>
    [BsonElement("applicationKeys")]
    public IReadOnlyList<string> ApplicationKeys { get; init; } = [];
    [BsonElement("templateId")] public string? TemplateId { get; init; }
    [BsonElement("templateVersion")] public int? TemplateVersion { get; init; }
    /// <summary>Same-origin relative destination used as the notification body-click fallback.</summary>
    [BsonElement("destination")]
    public string Destination { get; init; } = "/";

    [BsonElement("actions")]
    public IReadOnlyList<PushNotificationAction> Actions { get; init; } = [];

    [BsonElement("audienceIds")]
    public IReadOnlyList<string> AudienceIds { get; init; } = [];

    /// <summary>Channel that owns a <see cref="PushDispatchScope.Channel"/> dispatch; null for platform dispatches.</summary>
    [BsonElement("ownerChannelId")]
    public string? OwnerChannelId { get; init; }

    [BsonElement("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [BsonElement("recipientCount")]
    public int RecipientCount { get; init; }

    [BsonElement("queuedAt")]
    public DateTime? QueuedAt { get; init; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; init; }
}

/// <summary>One snapshotted endpoint of a dispatch. <see cref="IdempotencyKey"/> makes provisioning repeatable.</summary>
[BsonIgnoreExtraElements]
public sealed record PushDispatchRecipient(
    string DispatchId,
    string SubscriptionId,
    string EndpointHash,
    PushDeliveryStatus Status = PushDeliveryStatus.Pending) : BaseEntity
{
    [BsonElement("idempotencyKey")]
    public string IdempotencyKey { get; init; } = string.Empty;

    [BsonElement("attemptCount")]
    public int AttemptCount { get; init; }

    [BsonElement("lastAttemptAt")]
    public DateTime? LastAttemptAt { get; init; }

    [BsonElement("sentAt")]
    public DateTime? SentAt { get; init; }

    [BsonElement("failedAt")]
    public DateTime? FailedAt { get; init; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; init; }
}
