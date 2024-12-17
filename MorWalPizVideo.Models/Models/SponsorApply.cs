using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record SponsorApply([property: DataMember][property: BsonElement("name")] string Name,
        [property: DataMember][property: BsonElement("email")] string Email,
        [property: DataMember][property: BsonElement("description")] string Description) : BaseEntity
    {
        [DataMember]
        [BsonElement("sponsorApplyId")]
        public string sponsorApplyId => Email;
    }
}
