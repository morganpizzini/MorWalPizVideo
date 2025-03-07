using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record YTChannel(
        [property: DataMember][property: BsonElement("channelId")] string ChannelId,
        [property: DataMember][property: BsonElement("channelName")] string ChannelName) : BaseEntity
    {
        [DataMember]
        [BsonElement("ytChannelId")]
        public string YTChannelId => ChannelId;
    }
}
