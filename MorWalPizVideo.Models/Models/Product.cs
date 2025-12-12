using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{    
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Product(
        [property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Description,
        [property: DataMember][property: BsonElement("url")] string Url,
        /// <summary> Categories associated with the product
        /// refers to ProductCategory documents
        /// </summary>
         [property:DataMember]
        [property: BsonElement("categories")]
        CategoryRef[] Categories ) : BaseEntity
{
}

[BsonIgnoreExtraElements]
[DataContract]
public record ProductCategory(
   [property: DataMember][property: BsonElement("title")] string Title,
   [property: DataMember][property: BsonElement("description")] string Description) : BaseEntity
{
}}
