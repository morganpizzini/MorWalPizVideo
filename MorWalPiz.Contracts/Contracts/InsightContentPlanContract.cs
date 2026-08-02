using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class InsightContentPlanContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string TopicId { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public ContentPlanType Type { get; set; }

    [DataMember]
    public string Outline { get; set; } = string.Empty;

    [DataMember]
    public string[] GeneratedFromNewsItemIds { get; set; } = [];

    [DataMember]
    public string[] TargetPlatforms { get; set; } = [];

    [DataMember]
    public DateTime GeneratedAt { get; set; }
}