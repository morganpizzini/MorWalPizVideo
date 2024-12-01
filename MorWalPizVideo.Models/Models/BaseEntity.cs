using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    public abstract record BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; init; }
        [DataMember]
        [BsonElement("creationDateTime")]
        public DateTime CreationDateTime { get; init; } = DateTime.Now;
    }
}
