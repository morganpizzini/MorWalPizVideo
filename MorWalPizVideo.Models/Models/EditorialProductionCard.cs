using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum EditorialProductionCardStatus
{
    Idea,
    Planned,
    InProgress,
    Ready,
    Published,
    Archived
}

[BsonIgnoreExtraElements]
[DataContract]
public record EditorialProductionCard : BaseEntity
{
    [DataMember]
    [BsonElement("channelId")]
    public string ChannelId { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("title")]
    public string Title { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("editorialAngle")]
    public string EditorialAngle { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("sourceUrls")]
    public IReadOnlyList<string> SourceUrls { get; init; } = [];

    [DataMember]
    [BsonElement("brief")]
    public string Brief { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("script")]
    public string Script { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("status")]
    public EditorialProductionCardStatus Status { get; init; } = EditorialProductionCardStatus.Idea;

    [DataMember]
    [BsonElement("ownerUserId")]
    public string? OwnerUserId { get; init; }

    [DataMember]
    [BsonElement("dueDateUtc")]
    public DateTime? DueDateUtc { get; init; }

    [DataMember]
    [BsonElement("topicId")]
    public string? TopicId { get; init; }

    [DataMember]
    [BsonElement("newsItemId")]
    public string? NewsItemId { get; init; }

    [DataMember]
    [BsonElement("contentPlanId")]
    public string? ContentPlanId { get; init; }

    [DataMember]
    [BsonElement("youtubeVideoId")]
    public string? YoutubeVideoId { get; init; }

    [DataMember]
    [BsonElement("backOfficeContentId")]
    public string? BackOfficeContentId { get; init; }

    [DataMember]
    [BsonElement("revision")]
    public long Revision { get; init; }

    [DataMember]
    [BsonElement("updatedDateTime")]
    public DateTime UpdatedDateTime { get; init; } = DateTime.UtcNow;
}