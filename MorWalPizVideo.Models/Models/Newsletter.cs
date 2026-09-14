using MongoDB.Bson.Serialization.Attributes;

namespace MorWalPizVideo.Server.Models;

public enum NewsletterState { Draft, Editing, ReadyForPreview, Preview, Approved, Scheduled, Sending, Sent, Failed, Cancelled }
public enum NewsletterUserStatus { PendingConfirmation, Subscribed, Unsubscribed, Suppressed }
public enum NewsletterRecipientStatus { Pending, Sending, Sent, Delivered, Bounced, Suppressed, Failed }
public enum NewsletterEventType { Send, Delivered, Bounce, Click, Unsubscribe }

[BsonIgnoreExtraElements]
public sealed record NewsletterSection(string Type, string? Title, string? Body, string? ImageUrl, string? ShortLinkCode);

[BsonIgnoreExtraElements]
public sealed record NewsletterTemplate(
    string ChannelId,
    string Name,
    int Version,
    IReadOnlyList<NewsletterSection> Sections) : BaseEntity;

[BsonIgnoreExtraElements]
public sealed record Newsletter(
    string ChannelId,
    string Name,
    string SubjectIt,
    string SubjectEng,
    string TemplateId,
    int TemplateVersion,
    IReadOnlyList<NewsletterSection> Sections,
    NewsletterState State = NewsletterState.Draft) : BaseEntity
{
    [BsonElement("approvedAt")]
    public DateTime? ApprovedAt { get; init; }

    [BsonElement("scheduledAtUtc")]
    public DateTime? ScheduledAtUtc { get; init; }
}

[BsonIgnoreExtraElements]
public sealed record NewsletterUser(
    string ChannelId,
    string EmailHash,
    string EmailCiphertext,
    string Language,
    NewsletterUserStatus Status = NewsletterUserStatus.PendingConfirmation) : BaseEntity
{
    public string? ConfirmationTokenHash { get; init; }
    public DateTime? ConfirmationExpiresAt { get; init; }
    public string? UnsubscribeTokenHash { get; init; }
    public string? UnsubscribeTokenCiphertext { get; init; }
    public DateTime? UnsubscribeExpiresAt { get; init; }
}

[BsonIgnoreExtraElements]
public sealed record NewsletterRecipient(
    string ChannelId,
    string NewsletterId,
    string NewsletterUserId,
    string Language,
    NewsletterRecipientStatus Status = NewsletterRecipientStatus.Pending) : BaseEntity
{
    public string? ProviderMessageId { get; init; }
    public string? FailureReason { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public DateTime? BouncedAt { get; init; }
}

[BsonIgnoreExtraElements]
public sealed record NewsletterEvent(
    string ChannelId,
    string NewsletterId,
    NewsletterEventType Type,
    DateTime OccurredAt,
    string? ShortLinkCode = null,
    string? ProviderMessageId = null,
    long Count = 1,
    string? ShortLinkContext = null) : BaseEntity;