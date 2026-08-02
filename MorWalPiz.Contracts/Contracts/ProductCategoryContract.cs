using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class ProductCategoryContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Description { get; set; } = string.Empty;

    [DataMember]
    public DateTime CreationDateTime { get; set; }
}