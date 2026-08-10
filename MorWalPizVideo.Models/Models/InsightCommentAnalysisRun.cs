using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum InsightCommentSourceType
{
    StoredChannel = 0,
    StoredVideo = 1,
    DirectVideoId = 2
}

public enum InsightCommentAnalysisRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Rejected = 3
}

[BsonIgnoreExtraElements]
[DataContract]
public record InsightCommentAnalysisRun : BaseEntity
{
    [DataMember, BsonElement("topicId")]
    public string TopicId { get; init; } = string.Empty;

    [DataMember, BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [DataMember, BsonElement("sourceType")]
    public InsightCommentSourceType SourceType { get; init; }

    [DataMember, BsonElement("sourceKind")]
    public InsightSourceKind SourceKind { get; init; } = InsightSourceKind.ShortContent;

    [DataMember, BsonElement("sourceChannelId")]
    public string SourceChannelId { get; init; } = string.Empty;

    [DataMember, BsonElement("videoId")]
    public string VideoId { get; init; } = string.Empty;

    [DataMember, BsonElement("commentsNumber")]
    public int CommentsNumber { get; init; }

    [DataMember, BsonElement("excludeUploaderComments")]
    public bool ExcludeUploaderComments { get; init; } = true;

    [DataMember, BsonElement("creationMode")]
    public InsightTopicCreationMode CreationMode { get; init; } = InsightTopicCreationMode.YouTubeCommentAnalysis;

    [DataMember, BsonElement("status")]
    public InsightCommentAnalysisRunStatus Status { get; init; } = InsightCommentAnalysisRunStatus.Pending;

    [DataMember, BsonElement("queuedAtUtc")]
    public DateTime QueuedAtUtc { get; init; } = DateTime.UtcNow;

    [DataMember, BsonElement("startedAtUtc")]
    public DateTime? StartedAtUtc { get; init; }

    [DataMember, BsonElement("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; init; }

    [DataMember, BsonElement("rejectionReason")]
    public string RejectionReason { get; init; } = string.Empty;

    [DataMember, BsonElement("videosProcessed")]
    public int VideosProcessed { get; init; }

    [DataMember, BsonElement("commentsAnalyzed")]
    public int CommentsAnalyzed { get; init; }

    [DataMember, BsonElement("createdNewsItemCount")]
    public int CreatedNewsItemCount { get; init; }
}
