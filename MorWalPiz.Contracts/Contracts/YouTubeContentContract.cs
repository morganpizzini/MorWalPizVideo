using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

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
    public VideoRef[] VideoRefs { get; set; } = [];

    [DataMember]
    public CategoryRef[] Categories { get; set; } = [];

    [DataMember]
    public YoutubeContentType ContentType { get; set; }

    [DataMember]
    public YouTubeVideoLink[]? YouTubeVideoLinks { get; set; } = [];

    [DataMember]
    public ShortLink[] ShortLinks { get; set; } = [];

    [DataMember]
    public bool IsPrivate { get; set; }
}