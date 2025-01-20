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
    [property: BsonElement("url")]string Url,
        [property: DataMember]
    [property: BsonElement("videoUrl")]string VideoId) : BaseEntity {

        [DataMember]
        [BsonElement("pageId")]
        public string PageId => Title;
        [DataMember]
        [BsonElement("videoReelIds")]
        public IList<string> VideoReelIds { get; set; } = new List<string>();
        [DataMember]
        [BsonElement("shortReelIds")]
        public IList<string> ShortReelIds { get; set; } = new List<string>();
        [BsonIgnore]
        public string ShortContent => Content.Substring(0,20);
    }
}
