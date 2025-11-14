using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record PublishSchedule : BaseEntity
    {
        [JsonConstructor]
        public PublishSchedule(
            string videoId,
            string[] queryStringIds,
            string message,
            DateTime date)
        {
            VideoId = videoId;
            QueryStringIds = queryStringIds;
            Message = message;
            Date = date;
        }

        [DataMember]
        [BsonElement("videoId")]
        public string VideoId { get; init; }

        [DataMember]
        [BsonElement("queryStringIds")]
        public string[] QueryStringIds { get; init; }

        [DataMember]
        [BsonElement("message")]
        public string Message { get; init; }

        [DataMember]
        [BsonElement("date")]
        public DateTime Date { get; init; }
    }
}
