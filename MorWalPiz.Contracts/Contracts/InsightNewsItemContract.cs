using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class InsightNewsItemContract
{
    [DataMember]
    public string Id { get; set; } = string.Empty;

    [DataMember]
    public string TopicId { get; set; } = string.Empty;

    [DataMember]
    public string Title { get; set; } = string.Empty;

    [DataMember]
    public string Summary { get; set; } = string.Empty;

    [DataMember]
    public string SourceUrl { get; set; } = string.Empty;

    [DataMember]
    public string SourceName { get; set; } = string.Empty;

    [DataMember]
    public InsightNewsStatus Status { get; set; }

    [DataMember]
    public int StarRating { get; set; }

    [DataMember]
    public double AIRelevanceScore { get; set; }

    [DataMember]
    public DateTime DiscoveredAt { get; set; }

    [DataMember]
    public string PlatformSource { get; set; } = string.Empty;

    [DataMember]
    public string PostId { get; set; } = string.Empty;

    [DataMember]
    public string VideoId { get; set; } = string.Empty;

    [DataMember]
    public string AnalysisReason { get; set; } = string.Empty;

    [DataMember]
    public string ReviewReason { get; set; } = string.Empty;

    [DataMember]
    public InsightSourceKind SourceKind { get; set; }

    [DataMember]
    public string CommentExcerpt { get; set; } = string.Empty;

    [DataMember]
    public string Sentiment { get; set; } = string.Empty;

    [DataMember]
    public string ChannelId { get; set; } = string.Empty;
}