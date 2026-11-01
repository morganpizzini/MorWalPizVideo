using System.Runtime.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class EditorialProductionCardContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string EditorialAngle { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<string> SourceUrls { get; set; } = [];
    [DataMember] public string Brief { get; set; } = string.Empty;
    [DataMember] public string Script { get; set; } = string.Empty;
    [DataMember] public EditorialProductionCardStatus Status { get; set; }
    [DataMember] public string? OwnerUserId { get; set; }
    [DataMember] public DateTime? DueDateUtc { get; set; }
    [DataMember] public string? TopicId { get; set; }
    [DataMember] public string? NewsItemId { get; set; }
    [DataMember] public string? ContentPlanId { get; set; }
    [DataMember] public string? YoutubeVideoId { get; set; }
    [DataMember] public string? BackOfficeContentId { get; set; }
    [DataMember] public long Revision { get; set; }
    [DataMember] public DateTime CreationDateTime { get; set; }
    [DataMember] public DateTime UpdatedDateTime { get; set; }
}