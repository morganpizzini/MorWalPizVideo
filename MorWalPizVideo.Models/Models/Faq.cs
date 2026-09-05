using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum FaqLifecycleStatus { Draft, Published, Archived }
public enum FaqCandidateStatus { Pending, Approved, Rejected }
public enum FaqVoteValue { Helpful = 1, NotHelpful = -1 }

[BsonIgnoreExtraElements]
[DataContract]
public record FaqCategory : BaseEntity
{
    [DataMember][BsonElement("slug")] public string Slug { get; init; } = string.Empty;
    [DataMember][BsonElement("name")] public string Name { get; init; } = string.Empty;
    [DataMember][BsonElement("description")] public string Description { get; init; } = string.Empty;
    [DataMember][BsonElement("sortOrder")] public int SortOrder { get; init; }
    [DataMember][BsonElement("isActive")] public bool IsActive { get; init; } = true;
}

[BsonIgnoreExtraElements]
[DataContract]
public record Faq : BaseEntity
{
    [DataMember][BsonElement("question")] public string Question { get; init; } = string.Empty;
    [DataMember][BsonElement("categoryId")] public string CategoryId { get; init; } = string.Empty;
    [DataMember][BsonElement("status")] public FaqLifecycleStatus Status { get; init; } = FaqLifecycleStatus.Draft;
    [DataMember][BsonElement("publishedAt")] public DateTime? PublishedAt { get; init; }
    [DataMember][BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    [DataMember][BsonElement("sourceMetadata")] public FaqSourceMetadata SourceMetadata { get; init; } = new();
}

[BsonIgnoreExtraElements]
[DataContract]
public record FaqAnswer : BaseEntity
{
    [DataMember][BsonElement("faqId")] public string FaqId { get; init; } = string.Empty;
    [DataMember][BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [DataMember][BsonElement("content")] public string Content { get; init; } = string.Empty;
    [DataMember][BsonElement("status")] public FaqLifecycleStatus Status { get; init; } = FaqLifecycleStatus.Draft;
    [DataMember][BsonElement("publishedAt")] public DateTime? PublishedAt { get; init; }
    [DataMember][BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    [DataMember][BsonElement("helpfulVotes")] public int HelpfulVotes { get; init; }
    [DataMember][BsonElement("notHelpfulVotes")] public int NotHelpfulVotes { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public record FaqSourceMetadata
{
    [DataMember][BsonElement("sourceType")] public string SourceType { get; init; } = "Manual";
    [DataMember][BsonElement("campaignIds")] public IReadOnlyList<string> CampaignIds { get; init; } = [];
    [DataMember][BsonElement("generatedAt")] public DateTime? GeneratedAt { get; init; }
    [DataMember][BsonElement("provider")] public string Provider { get; init; } = string.Empty;
}

[BsonIgnoreExtraElements]
[DataContract]
public record FaqCandidate : BaseEntity
{
    [DataMember][BsonElement("question")] public string Question { get; init; } = string.Empty;
    [DataMember][BsonElement("answer")] public string Answer { get; init; } = string.Empty;
    [DataMember][BsonElement("campaignIds")] public IReadOnlyList<string> CampaignIds { get; init; } = [];
    [DataMember][BsonElement("sourceSubmissionCount")] public int SourceSubmissionCount { get; init; }
    [DataMember][BsonElement("relevance")] public double Relevance { get; init; }
    [DataMember][BsonElement("duplicateOfFaqId")] public string? DuplicateOfFaqId { get; init; }
    [DataMember][BsonElement("status")] public FaqCandidateStatus Status { get; init; } = FaqCandidateStatus.Pending;
    [DataMember][BsonElement("failureReason")] public string? FailureReason { get; init; }
    [DataMember][BsonElement("createdBy")] public string CreatedBy { get; init; } = string.Empty;
}

[BsonIgnoreExtraElements]
[DataContract]
public record FaqVote : BaseEntity
{
    [DataMember][BsonElement("answerId")] public string AnswerId { get; init; } = string.Empty;
    [DataMember][BsonElement("userId")] public string UserId { get; init; } = string.Empty;
    [DataMember][BsonElement("value")] public FaqVoteValue Value { get; init; }
    [DataMember][BsonElement("createdAt")] public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    [DataMember][BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}