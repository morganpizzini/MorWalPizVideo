using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [DataContract]
    public enum LinkType
    {
        [EnumMember]
        YouTubeVideo = 0,
        
        [EnumMember]
        YouTubeChannel = 1,
        
        [EnumMember]
        YouTubePlaylist = 2,
        
        [EnumMember]
        Instagram = 3,
        
        [EnumMember]
        Facebook = 4,
        
        [EnumMember]
        CustomUrl = 5
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record ShortLink(
        [property: DataMember][property: BsonElement("code")] string Code,
        [property: DataMember][property: BsonElement("target")] string Target,
        [property: DataMember][property: BsonElement("queryString")] string QueryString) : BaseEntity
    {
        [DataMember]
        [BsonElement("shortLinkId")]
        public string ShortLinkId => Code;

        [DataMember]
        [BsonElement("clicksCount")]
        public int ClicksCount { get; set; }
        
        [DataMember]
        [BsonElement("linkType")]
        public LinkType LinkType { get; set; } = LinkType.YouTubeVideo;
        
        /// <summary>
        /// Proprietà di retrocompatibilità con il vecchio modello
        /// </summary>
        [DataMember]
        [BsonElement("videoId")]
        public string VideoId => Target;
    }
}
