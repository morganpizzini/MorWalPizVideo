using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class SponsorContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Url { get; set; } = string.Empty;

    [DataMember]
    public string ImgSrc { get; set; } = string.Empty;
}