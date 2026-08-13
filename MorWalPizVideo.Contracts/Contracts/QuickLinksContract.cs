using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class QuickLinkContract
{
    [DataMember] public QuickLinkKind Kind { get; set; }
    [DataMember] public string TargetUrl { get; set; } = string.Empty;
    [DataMember] public string? Title { get; set; }
    [DataMember] public string? Subtitle { get; set; }
    [DataMember] public string? Label { get; set; }
    [DataMember] public string? ImageUrl { get; set; }
    [DataMember] public string? Icon { get; set; }
    [DataMember] public string? Provider { get; set; }
}

[DataContract]
public sealed class QuickLinksContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string? Subtitle { get; set; }
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public QuickLinkContract[] Links { get; set; } = [];
    [DataMember] public DateTime CreationDateTime { get; set; }
}

[DataContract]
public sealed class QuickLinksPublicContract
{
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string? Subtitle { get; set; }
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public QuickLinkContract[] Links { get; set; } = [];
}
