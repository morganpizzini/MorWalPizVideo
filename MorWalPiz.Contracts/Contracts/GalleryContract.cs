using System.Runtime.Serialization;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class GalleryImageContract
{
    [DataMember] public string StorageKey { get; set; } = string.Empty;
    [DataMember] public string PublicUrl { get; set; } = string.Empty;
    [DataMember] public string ContentType { get; set; } = string.Empty;
    [DataMember] public int Width { get; set; }
    [DataMember] public int Height { get; set; }
    [DataMember] public int DisplayOrder { get; set; }
}

[DataContract]
public sealed class GalleryContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string? Description { get; set; }
    [DataMember] public int DownloadCount { get; set; }
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public DateTime Expiration { get; set; }
    [DataMember] public string ChannelId { get; set; } = string.Empty;
    [DataMember] public IReadOnlyList<GalleryImageContract> Images { get; set; } = [];
}

[DataContract]
public sealed class GalleryPublicContract
{
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string? Description { get; set; }
    [DataMember] public string Slug { get; set; } = string.Empty;
    [DataMember] public DateTime Expiration { get; set; }
    [DataMember] public int DownloadCount { get; set; }
    [DataMember] public IReadOnlyList<string> Images { get; set; } = [];
}