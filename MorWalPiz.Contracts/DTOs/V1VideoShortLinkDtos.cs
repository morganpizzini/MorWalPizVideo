using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.DTOs;

[DataContract]
public sealed record V1ShortLinkResponse(
    [property: DataMember] string Code,
    [property: DataMember] string Target,
    [property: DataMember] string LinkType,
    [property: DataMember] string? ContentId,
    [property: DataMember] string? ChannelId,
    [property: DataMember] string? ManagementChannelId,
    [property: DataMember] int Clicks,
    [property: DataMember] DateTime CreatedAtUtc);

[DataContract]
public sealed record V1CreateShortLinkRequest(
    [property: DataMember] string Target,
    [property: DataMember] string LinkType,
    [property: DataMember] string[]? QueryLinkIds);

[DataContract]
public sealed record V1VideoResponse(
    [property: DataMember] string Id,
    [property: DataMember] string Url,
    [property: DataMember] string Title,
    [property: DataMember] string Description,
    [property: DataMember] string ContentType,
    [property: DataMember] V1VideoReferenceResponse[] Videos);

[DataContract]
public sealed record V1VideoReferenceResponse(
    [property: DataMember] string YouTubeId,
    [property: DataMember] string Title,
    [property: DataMember] string Description,
    [property: DataMember] string[] ChannelIds,
    [property: DataMember] string? ShortLinkCode);