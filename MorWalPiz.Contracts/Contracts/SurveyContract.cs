using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class SurveyContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public DateTime FromUtc { get; set; }
    [DataMember] public DateTime ToUtc { get; set; }
    [DataMember] public string[] FormIds { get; set; } = [];
    [DataMember] public SurveyLifecycle Lifecycle { get; set; }
}