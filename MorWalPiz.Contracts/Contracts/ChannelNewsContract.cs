using System.Runtime.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class ChannelNewsImageContract
{
    [DataMember] public string StorageKey { get; set; } = string.Empty;
    [DataMember] public string PublicUrl { get; set; } = string.Empty;
    [DataMember] public string ContentType { get; set; } = string.Empty;
    [DataMember] public int Width { get; set; }
    [DataMember] public int Height { get; set; }
    [DataMember] public string AltText { get; set; } = string.Empty;
    [DataMember] public int DisplayOrder { get; set; }
}

[DataContract]
public sealed class ChannelNewsContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Subtitle { get; set; } = string.Empty;
    [DataMember] public string DescriptionHtml { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<ChannelNewsImageContract> Images { get; set; } = [];
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public ChannelNewsStatus Status { get; set; }
    [DataMember] public DateTime? PublicationTimeUtc { get; set; }
    [DataMember] public int DisplayOrder { get; set; }
    [DataMember] public DateTime CreationDateTime { get; set; }
    [DataMember] public DateTime UpdatedDateTime { get; set; }
}

[DataContract]
public sealed class ChannelNewsPublicImageContract
{
    [DataMember] public string PublicUrl { get; set; } = string.Empty;
    [DataMember] public string ContentType { get; set; } = string.Empty;
    [DataMember] public int Width { get; set; }
    [DataMember] public int Height { get; set; }
    [DataMember] public string AltText { get; set; } = string.Empty;
    [DataMember] public int DisplayOrder { get; set; }
}

[DataContract]
public sealed class ChannelNewsPublicContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public string ChannelName { get; set; } = string.Empty;
    [DataMember] public string ChannelLogoUrl { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Subtitle { get; set; } = string.Empty;
    [DataMember] public string DescriptionHtml { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<ChannelNewsPublicImageContract> Images { get; set; } = [];
    [DataMember] public ChannelNewsStatus Status { get; set; }
    [DataMember] public DateTime? PublicationTimeUtc { get; set; }
}