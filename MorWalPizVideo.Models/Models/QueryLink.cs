using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record QueryLink(
        [property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Value) : BaseEntity
    {
        [DataMember]
        [BsonElement("queryLinkId")]
        public string QueryLinkId => Title;
    }
}
