using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class InsightTopicContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Description { get; set; } = string.Empty;

    [DataMember]
    public string[] SeedArguments { get; set; } = [];

    [DataMember]
    public string[] PreferredSources { get; set; } = [];

    [DataMember]
    public string ChannelId { get; set; } = string.Empty;
}