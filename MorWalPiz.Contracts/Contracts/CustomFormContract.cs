using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class CustomFormContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Description { get; set; } = string.Empty;

    [DataMember]
    public string Url { get; set; } = string.Empty;

    [DataMember]
    public bool Active { get; set; }

    [DataMember]
    public CustomFormQuestion[] Questions { get; set; } = [];

    [DataMember]
    public CustomFormResponse[] Responses { get; set; } = [];

    [DataMember]
    public int ResponseCount { get; set; }
}