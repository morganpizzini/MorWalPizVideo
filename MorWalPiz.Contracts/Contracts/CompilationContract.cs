using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class CompilationContract
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
    public VideoRef[] Videos { get; set; } = [];

    [DataMember]
    public string ChannelId { get; set; } = string.Empty;
}