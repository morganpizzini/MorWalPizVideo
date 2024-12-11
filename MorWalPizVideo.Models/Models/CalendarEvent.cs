using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record CalendarEvent(
        [property: DataMember][property: BsonElement("title")] string Title,
        [property: DataMember][property: BsonElement("description")] string Description,
        [property: DataMember][property: BsonElement("date")] DateOnly Date,
        [property: DataMember][property: BsonElement("category")] string Category,
        [property: DataMember][property: BsonElement("matchId")] string MatchId = "") : BaseEntity
    {
        [DataMember]
        [BsonElement("calendarEventId")]
        public string CalendarEventId => Title;
        [BsonIgnore]
        public string MatchUrl { get; set; } = string.Empty;
        [BsonIgnore]
        public bool OldEvent => DateTime.Now.Date > Date.ToDateTime(new TimeOnly(0, 0));
    }
}
