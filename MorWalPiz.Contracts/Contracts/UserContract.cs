using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class UserContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Username { get; set; } = string.Empty;

    [DataMember]
    public string Email { get; set; } = string.Empty;

    [DataMember]
    public bool IsActive { get; set; }

    [DataMember]
    public DateTime? LastLogin { get; set; }
}