using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record BioLink([property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Description,
        [property: DataMember][property: BsonElement("url")] string Url,
        [property: DataMember][property: BsonElement("icon")] string Icon,
        [property: DataMember][property: BsonElement("order")] int Order) : BaseEntity
    {
        [DataMember]
        [BsonElement("enable")]
        public bool Enable { get; set; } = true;

        [DataMember]
        [BsonElement("bioLinkId")]
        public string BioLinkId => Title;
    }
}
