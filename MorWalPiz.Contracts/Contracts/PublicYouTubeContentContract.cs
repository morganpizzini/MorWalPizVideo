using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class PublicYouTubeContentContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string ContentId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string Url { get; set; } = string.Empty;
    [DataMember] public string ThumbnailVideoId { get; set; } = string.Empty;
    [DataMember] public VideoRefContract[] VideoRefs { get; set; } = [];
    [DataMember] public CategoryRefContract[] Categories { get; set; } = [];
    [DataMember] public YoutubeContentType ContentType { get; set; }
    [DataMember] public YouTubeVideoLinkContract[] YouTubeVideoLinks { get; set; } = [];
    [DataMember] public ShortLinkContract[] ShortLinks { get; set; } = [];
    [DataMember] public bool IsLink { get; set; }
    [DataMember] public DateTime CreationDateTime { get; set; }
}

[DataContract]
public sealed class VideoRefContract
{
    [DataMember] public string YoutubeId { get; set; } = string.Empty;
    [DataMember] public CategoryRefContract[] Categories { get; set; } = [];
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public DateTime PublishedAt { get; set; }
    [DataMember] public string[] ChannelIds { get; set; } = [];
    [DataMember] public DateTime CreationDateTime { get; set; }
}

[DataContract]
public sealed class CategoryRefContract
{
    [DataMember] public string Id { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
}

[DataContract]
public sealed class YouTubeVideoLinkContract
{
    [DataMember] public string ContentCreatorName { get; set; } = string.Empty;
    [DataMember] public string YouTubeVideoId { get; set; } = string.Empty;
    [DataMember] public string ImageName { get; set; } = string.Empty;
    [DataMember] public ShortLinkContract? ShortLink { get; set; }
    [DataMember] public string? ShortLinkUrl { get; set; }
    [DataMember] public string? DirectVideoUrl { get; set; }
}