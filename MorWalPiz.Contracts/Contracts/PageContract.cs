using System.Runtime.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class PageImageContract
{
    [DataMember] public string PublicUrl { get; set; } = string.Empty;
    [DataMember] public string ContentType { get; set; } = string.Empty;
    [DataMember] public int Width { get; set; }
    [DataMember] public int Height { get; set; }
    [DataMember] public string AltText { get; set; } = string.Empty;
}

[DataContract]
public sealed class PageContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string ThumbnailUrl { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public string VideoId { get; set; } = string.Empty;
    [DataMember] public PageStatus Status { get; set; }
    [DataMember] public IReadOnlyList<PageImageContract> InlineImages { get; set; } = [];
    [DataMember] public DateTime CreationDateTime { get; set; }
    [DataMember] public DateTime UpdatedDateTime { get; set; }
}

[DataContract]
public sealed class PagePublicContract
{
    [DataMember] public string ThumbnailUrl { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public string VideoId { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<PageImageContract> InlineImages { get; set; } = [];
}

[DataContract]
public sealed class NavigationMenuItemContract
{
    [DataMember] public NavigationItemType Type { get; set; }
    [DataMember] public string? PageId { get; set; }
    [DataMember] public string TargetUrl { get; set; } = string.Empty;
    [DataMember] public string DisplayText { get; set; } = string.Empty;
    [DataMember] public int Column { get; set; }
    [DataMember] public int DisplayOrder { get; set; }
    [DataMember] public bool OpenInNewTab { get; set; }
}

[DataContract]
public sealed class ChannelNavigationContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public bool IsActive { get; set; }
    [DataMember] public IReadOnlyList<NavigationMenuItemContract> HeaderItems { get; set; } = [];
    [DataMember] public int FooterColumnCount { get; set; }
    [DataMember] public IReadOnlyList<NavigationMenuItemContract> FooterItems { get; set; } = [];
}

[DataContract]
public sealed class PublicNavigationContract
{
    [DataMember] public IReadOnlyList<NavigationMenuItemContract> HeaderItems { get; set; } = [];
    [DataMember] public int FooterColumnCount { get; set; }
    [DataMember] public IReadOnlyList<NavigationMenuItemContract> FooterItems { get; set; } = [];
}