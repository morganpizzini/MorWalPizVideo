using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Match([property: DataMember][property: BsonElement("thumbnailUrl")] string ThumbnailUrl,
        [property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Description,
        [property: DataMember][property: BsonElement("url")] string Url,
        [property: DataMember][property: BsonElement("videos")] Video[] Videos,
        [property: DataMember][property: BsonElement("category")] string Category = "",
        [property: DataMember][property: BsonElement("isLink")] bool IsLink = false) : BaseEntity
    {
        [DataMember]
        [BsonElement("matchId")]
        public string MatchId => ThumbnailUrl;

        public Match(string thumbnailUrl,bool isLink,string category) : this(thumbnailUrl, string.Empty, string.Empty, string.Empty, [], category,isLink)
        {
        }
    }
}
