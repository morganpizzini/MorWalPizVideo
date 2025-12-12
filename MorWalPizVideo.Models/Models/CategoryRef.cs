using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Reference to a category with ID and title
    /// </summary>
    [BsonIgnoreExtraElements]
    [DataContract]
    public record CategoryRef(
        [property: DataMember][property: BsonElement("id")] string Id,
        [property: DataMember][property: BsonElement("title")] string Title);
}
