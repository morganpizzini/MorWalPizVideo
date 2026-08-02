using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts.Shop;

// Public-facing shape of DigitalProduct. ContentStorageKey is intentionally omitted (ADR-006: public DTOs never expose storage keys).
[DataContract]
public class DigitalProductContract
{
    [DataMember]
    public string DigitalProductId { get; set; } = string.Empty;
    [DataMember]
    public string Name { get; set; } = string.Empty;
    [DataMember]
    public string Description { get; set; } = string.Empty;
    [DataMember]
    public string PreviewImageUrl { get; set; } = string.Empty;
    [DataMember]
    public List<string> CategoryIds { get; set; } = new();
    [DataMember]
    public decimal? Price { get; set; }
    [DataMember]
    public bool IsActive { get; set; }
    [DataMember]
    public DateTime UpdatedAt { get; set; }
}
