using MorWalPizVideo.Models.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class PartnerContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string ContactName { get; set; } = string.Empty;
    [DataMember] public string ContactEmail { get; set; } = string.Empty;
    [DataMember] public string ContactPhone { get; set; } = string.Empty;
    [DataMember] public string WebsiteUrl { get; set; } = string.Empty;
    [DataMember] public string Notes { get; set; } = string.Empty;
    [DataMember] public PartnerStatus Status { get; set; }
    [DataMember] public DateTime UpdatedAt { get; set; }
}

[DataContract]
public sealed class CampaignContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string PartnerId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public CampaignStatus Status { get; set; }
    [DataMember] public DateTime? StartAt { get; set; }
    [DataMember] public DateTime? EndAt { get; set; }
    [DataMember] public IReadOnlyList<string> ContentReferences { get; set; } = [];
    [DataMember] public IReadOnlyList<TimelineEntryContract> TimelineEntries { get; set; } = [];
    [DataMember] public DateTime UpdatedAt { get; set; }
}

[DataContract]
public sealed class TimelineEntryContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public TimelineEntryKind Kind { get; set; }
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public DateTime? OccurredAt { get; set; }
    [DataMember] public DateTime? DueAt { get; set; }
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public bool IsComplete { get; set; }
}