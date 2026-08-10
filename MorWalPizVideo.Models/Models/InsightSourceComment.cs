using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

[BsonIgnoreExtraElements]
[DataContract]
public record InsightSourceComment
{
    [DataMember]
    [BsonElement("fullText")]
    public string FullText { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("highlightText")]
    public string HighlightText { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("author")]
    public string Author { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("publishedAt")]
    public DateTime PublishedAt { get; init; }
}