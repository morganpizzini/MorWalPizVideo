using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class YouTubeContentImageContract
{
    [DataMember] public string PublicUrl { get; set; } = string.Empty;
    [DataMember] public string ContentType { get; set; } = string.Empty;
    [DataMember] public int Width { get; set; }
    [DataMember] public int Height { get; set; }
    [DataMember] public string AltText { get; set; } = string.Empty;
}

[DataContract]
public class YouTubeContentContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string ContentId { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Description { get; set; } = string.Empty;

    [DataMember]
    public string Url { get; set; } = string.Empty;

    [DataMember]
    public string ThumbnailVideoId { get; set; } = string.Empty;

    [DataMember]
    public VideoRefContract[] VideoRefs { get; set; } = [];

    [DataMember]
    public CategoryRefContract[] Categories { get; set; } = [];

    [DataMember]
    public YoutubeContentType ContentType { get; set; }

    [DataMember]
    public YouTubeVideoLinkContract[]? YouTubeVideoLinks { get; set; } = [];

    [DataMember]
    public ShortLinkContract[] ShortLinks { get; set; } = [];

    [DataMember]
    public string[] Tags { get; set; } = [];

    [DataMember]
    public bool IsPrivate { get; set; }

    [DataMember]
    public string CreatorUserId { get; set; } = string.Empty;

    [DataMember]
    public string OwnerChannelId { get; set; } = string.Empty;

    [DataMember]
    public IReadOnlyList<YouTubeContentImageContract> Images { get; set; } = [];
}