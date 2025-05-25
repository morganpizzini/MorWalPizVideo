using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System.Runtime.Serialization;
using System.Text.Json;

namespace MorWalPizVideo.Server.Models
{    [BsonIgnoreExtraElements]
    [DataContract]
    public record Product(
        [property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Description,
        [property: DataMember][property: BsonElement("url")] string Url,
        [property: DataMember][property: BsonElement("category")] string Category,
        [property: DataMember][property: BsonElement("category2")] string? Category2) : BaseEntity {
        [DataMember]
        [BsonElement("productId")]
        public string ProductId => Title;
    }}
