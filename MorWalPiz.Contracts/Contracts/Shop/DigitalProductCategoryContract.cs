using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts.Shop;

[DataContract]
public class DigitalProductCategoryContract
{
    [DataMember]
    public string DigitalProductCategoryId { get; set; } = string.Empty;
    [DataMember]
    public string Name { get; set; } = string.Empty;
    [DataMember]
    public string Description { get; set; } = string.Empty;
    [DataMember]
    public int? DisplayOrder { get; set; }
}
