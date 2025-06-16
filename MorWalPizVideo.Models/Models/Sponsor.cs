using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Sponsor([property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("url")] string Url,
        [property: DataMember][property: BsonElement("imgSrc")] string ImgSrc) : BaseEntity
    {
    }
}
