using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class ProductContract
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
    public CategoryRef[] Categories { get; set; } = [];

    [DataMember]
    public DateTime CreationDateTime { get; set; }
}