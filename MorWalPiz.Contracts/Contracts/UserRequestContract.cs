using MorWalPizVideo.Models.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class UserRequestContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public string Email { get; set; } = string.Empty;

    [DataMember]
    public string Topic { get; set; } = string.Empty;

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public UserRequestStatus Status { get; set; }

    [DataMember]
    public string? AdminNote { get; set; }

    [DataMember]
    public int Votes { get; set; }
}