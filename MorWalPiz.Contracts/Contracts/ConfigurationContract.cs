using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class ConfigurationContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string Key { get; set; } = string.Empty;

    [DataMember]
    public object Value { get; set; } = string.Empty;

    [DataMember]
    public string Type { get; set; } = string.Empty;

    [DataMember]
    public string Description { get; set; } = string.Empty;
}