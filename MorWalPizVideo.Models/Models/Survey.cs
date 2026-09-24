using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

[BsonIgnoreExtraElements]
[DataContract]
public record Survey : BaseEntity
{
    public Survey(string title, string description, string url, string channelId, DateTime fromUtc, DateTime toUtc, string[] formIds, SurveyLifecycle lifecycle = SurveyLifecycle.Draft)
    {
        Title = title;
        Description = description;
        Url = url;
        ChannelId = channelId;
        FromUtc = fromUtc.ToUniversalTime();
        ToUtc = toUtc.ToUniversalTime();
        FormIds = formIds ?? [];
        Lifecycle = lifecycle;
    }

    [DataMember, BsonElement("title")] public string Title { get; init; }
    [DataMember, BsonElement("description")] public string Description { get; init; }
    [DataMember, BsonElement("url")] public string Url { get; init; }
    [DataMember, BsonElement("channelId")] public string ChannelId { get; init; }
    [DataMember, BsonElement("fromUtc")] public DateTime FromUtc { get; init; }
    [DataMember, BsonElement("toUtc")] public DateTime ToUtc { get; init; }
    [DataMember, BsonElement("formIds")] public string[] FormIds { get; init; }
    [DataMember, BsonElement("lifecycle")] public SurveyLifecycle Lifecycle { get; init; }

    public bool IsPubliclyEligible(DateTime utcNow) => Lifecycle == SurveyLifecycle.Online && FromUtc <= utcNow && utcNow < ToUtc;
}

public enum SurveyLifecycle { Draft, Online, Archived, Closed }