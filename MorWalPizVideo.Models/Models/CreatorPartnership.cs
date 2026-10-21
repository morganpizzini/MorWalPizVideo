using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Models.Models;

public enum PartnerStatus { Prospect, Active, Paused, Archived }
public enum CampaignStatus { Draft, Planned, Active, Completed, Archived }
public enum TimelineEntryKind { Note, Contact, Meeting, Deliverable, Content, Payment }

[BsonIgnoreExtraElements]
[DataContract]
public sealed record Partner : BaseEntity
{
    [DataMember][BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [DataMember][BsonElement("name")] public string Name { get; init; } = string.Empty;
    [DataMember][BsonElement("contactName")] public string ContactName { get; init; } = string.Empty;
    [DataMember][BsonElement("contactEmail")] public string ContactEmail { get; init; } = string.Empty;
    [DataMember][BsonElement("contactPhone")] public string ContactPhone { get; init; } = string.Empty;
    [DataMember][BsonElement("websiteUrl")] public string WebsiteUrl { get; init; } = string.Empty;
    [DataMember][BsonElement("notes")] public string Notes { get; init; } = string.Empty;
    [DataMember][BsonElement("status")] public PartnerStatus Status { get; init; } = PartnerStatus.Prospect;
    [DataMember][BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
[DataContract]
public sealed record Campaign : BaseEntity
{
    [DataMember][BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [DataMember][BsonElement("partnerId")] public string PartnerId { get; init; } = string.Empty;
    [DataMember][BsonElement("title")] public string Title { get; init; } = string.Empty;
    [DataMember][BsonElement("description")] public string Description { get; init; } = string.Empty;
    [DataMember][BsonElement("status")] public CampaignStatus Status { get; init; } = CampaignStatus.Draft;
    [DataMember][BsonElement("startAt")] public DateTime? StartAt { get; init; }
    [DataMember][BsonElement("endAt")] public DateTime? EndAt { get; init; }
    [DataMember][BsonElement("contentReferences")] public IReadOnlyList<string> ContentReferences { get; init; } = [];
    [DataMember][BsonElement("timelineEntries")] public IReadOnlyList<TimelineEntry> TimelineEntries { get; init; } = [];
    [DataMember][BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

[DataContract]
public sealed record TimelineEntry
{
    [DataMember][BsonElement("id")] public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [DataMember][BsonElement("kind")] public TimelineEntryKind Kind { get; init; } = TimelineEntryKind.Note;
    [DataMember][BsonElement("title")] public string Title { get; init; } = string.Empty;
    [DataMember][BsonElement("description")] public string Description { get; init; } = string.Empty;
    [DataMember][BsonElement("occurredAt")] public DateTime? OccurredAt { get; init; }
    [DataMember][BsonElement("dueAt")] public DateTime? DueAt { get; init; }
    [DataMember][BsonElement("url")] public string Url { get; init; } = string.Empty;
    [DataMember][BsonElement("isComplete")] public bool IsComplete { get; init; }
}