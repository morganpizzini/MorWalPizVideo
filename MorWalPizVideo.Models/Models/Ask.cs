using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum AskCampaignStatus { Draft, Published, Closed, Archived }
public enum AskModerationStatus { Pending, Approved, Rejected, Spam, Deleted }
public enum AskModerationMode { Manual, AiAssisted }
public enum AskResponseVisibility { Internal, Public }

[BsonIgnoreExtraElements]
[DataContract]
public record AskPolicy
{
    [DataMember][BsonElement("maxSubmissionLength")] public int MaxSubmissionLength { get; init; } = 2000;
    [DataMember][BsonElement("retentionDays")] public int RetentionDays { get; init; } = 90;
    [DataMember][BsonElement("rateLimitPerHour")] public int RateLimitPerHour { get; init; } = 5;
    [DataMember][BsonElement("duplicateWindowMinutes")] public int DuplicateWindowMinutes { get; init; } = 60;
    [DataMember][BsonElement("moderationMode")] public AskModerationMode ModerationMode { get; init; } = AskModerationMode.Manual;
    [DataMember][BsonElement("recaptchaRequired")] public bool RecaptchaRequired { get; init; } = true;
    [DataMember][BsonElement("allowNamedSubmissions")] public bool AllowNamedSubmissions { get; init; }
    [DataMember][BsonElement("nameRequired")] public bool NameRequired { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public record AskCampaign : BaseEntity
{
    [DataMember][BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [DataMember][BsonElement("title")] public string Title { get; init; } = string.Empty;
    [DataMember][BsonElement("description")] public string Description { get; init; } = string.Empty;
    [DataMember][BsonElement("startAt")] public DateTime? StartAt { get; init; }
    [DataMember][BsonElement("endAt")] public DateTime? EndAt { get; init; }
    [DataMember][BsonElement("slug")] public string Slug { get; init; } = string.Empty;
    [DataMember][BsonElement("status")] public AskCampaignStatus Status { get; init; } = AskCampaignStatus.Draft;
    [DataMember][BsonElement("policy")] public AskPolicy Policy { get; init; } = new();
    [DataMember][BsonElement("publishedAt")] public DateTime? PublishedAt { get; init; }
    [DataMember][BsonElement("closedAt")] public DateTime? ClosedAt { get; init; }
    [DataMember][BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
[DataContract]
public record AskSubmission : BaseEntity
{
    [DataMember][BsonElement("campaignId")] public string CampaignId { get; init; } = string.Empty;
    [DataMember][BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [DataMember][BsonElement("text")] public string Text { get; init; } = string.Empty;
    [DataMember][BsonElement("name")] public string Name { get; init; } = string.Empty;
    [DataMember][BsonElement("moderationStatus")] public AskModerationStatus ModerationStatus { get; init; } = AskModerationStatus.Pending;
    [DataMember][BsonElement("submittedAt")] public DateTime SubmittedAt { get; init; } = DateTime.UtcNow;
    [DataMember][BsonElement("retentionUntil")] public DateTime RetentionUntil { get; init; }
    [DataMember][BsonElement("idempotencyKey")] public string IdempotencyKey { get; init; } = string.Empty;
    [DataMember][BsonElement("contentHash")] public string ContentHash { get; init; } = string.Empty;
    [DataMember][BsonElement("moderationScore")] public double? ModerationScore { get; init; }
    [DataMember][BsonElement("moderationCategories")] public IReadOnlyList<string> ModerationCategories { get; init; } = [];
    [DataMember][BsonElement("moderationNote")] public string ModerationNote { get; init; } = string.Empty;
    [DataMember][BsonElement("responseContent")] public string ResponseContent { get; init; } = string.Empty;
    [DataMember][BsonElement("responseAuthor")] public string ResponseAuthor { get; init; } = string.Empty;
    [DataMember][BsonElement("responseCreatedAt")] public DateTime? ResponseCreatedAt { get; init; }
    [DataMember][BsonElement("responseVisibility")] public AskResponseVisibility? ResponseVisibility { get; init; }
    [DataMember][BsonElement("reactionCount")] public int ReactionCount { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public record AskReaction : BaseEntity
{
    [DataMember][BsonElement("submissionId")] public string SubmissionId { get; init; } = string.Empty;
    [DataMember][BsonElement("fingerprint")] public string Fingerprint { get; init; } = string.Empty;
    [DataMember][BsonElement("createdAt")] public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}