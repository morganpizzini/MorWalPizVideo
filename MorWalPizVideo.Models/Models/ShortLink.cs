using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record ShortLink(
        [property: DataMember][property: BsonElement("code")] string Code,
        [property: DataMember][property: BsonElement("videoId")] string VideoId,
        [property: DataMember][property: BsonElement("queryString")] string QueryString) : BaseEntity
    {
        [DataMember]
        [BsonElement("shortLinkId")]
        public string shortLinkId => Code;

        [DataMember]
        [BsonElement("clicksCount")]
        public int ClicksCount { get; set; }
    }
}
