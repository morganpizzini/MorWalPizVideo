using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class ChannelVideoContract
{
    [DataMember]
    public string VideoId { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public DateTime LastCommentDate { get; set; }
}