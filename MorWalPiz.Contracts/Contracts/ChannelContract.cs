using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class ChannelContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string ChannelId { get; set; } = string.Empty;

    [DataMember]
    [JsonPropertyName("yTChannelId")]
    public string YTChannelId { get; set; } = string.Empty;

    [DataMember]
    public string ChannelName { get; set; } = string.Empty;

    [DataMember]
    public string ShortLinkUrl { get; set; } = string.Empty;

    [DataMember]
    public List<ChannelSocialContract> Socials { get; set; } = [];

    [DataMember]
    public ChannelVideoContract[] Videos { get; set; } = [];
}

[DataContract]
public class ChannelSocialContract
{
    [DataMember]
    public string Provider { get; set; } = string.Empty;

    [DataMember]
    public string Handler { get; set; } = string.Empty;
}