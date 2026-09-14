using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.ShootingRange.Models;

public abstract record BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = string.Empty;

    [DataMember]
    [BsonElement("creationDateTime")]
    public DateTime CreationDateTime { get; init; } = DateTime.Now;
}