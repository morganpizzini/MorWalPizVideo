using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class ChannelContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string ChannelId { get; set; } = string.Empty;

    [DataMember]
    public string ChannelName { get; set; } = string.Empty;

    [DataMember]
    public bool Mine { get; set; }

    [DataMember]
    public ChannelVideoContract[] Videos { get; set; } = [];
}