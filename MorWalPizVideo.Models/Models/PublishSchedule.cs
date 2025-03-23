using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    [method: JsonConstructor]
    public record PublishSchedule(
        [property: DataMember][property: BsonElement("videoId")] string VideoId,
        [property: DataMember][property: BsonElement("queryStringIds")] string[] QueryStringIds,
        [property: DataMember][property: BsonElement("message")] string Message,
        [property: DataMember][property: BsonElement("date")] DateTime Date) : BaseEntity
    {
        [DataMember]
        [BsonElement("publishScheduleId")]
        public string PublishScheduleId => Id;
    }
}
