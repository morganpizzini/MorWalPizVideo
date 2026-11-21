using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.ShootingRange.Models;

public abstract record BaseEntity
{
    public string Id { get; init; } = string.Empty;

    [JsonIgnore]
    public string TenantId { get; init; } = string.Empty;

    [DataMember]
    public DateTime CreationDateTime { get; init; } = DateTime.Now;
}