using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Page([property: DataMember]
    [property: BsonElement("thumbnailUrl")]string ThumbnailUrl,
        [property: DataMember]
    [property: BsonElement("title")]string Title,
        [property: DataMember]
    [property: BsonElement("content")]string Content,
        [property: DataMember]
    [property: BsonElement("url")]string Url) : BaseEntity {
        [DataMember]
        [BsonElement("pageId")]
        public string PageId => Title;
        [BsonIgnore]
        public string ShortContent => Content.Substring(0,20);
    }
}
