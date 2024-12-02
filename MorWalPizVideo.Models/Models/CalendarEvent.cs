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
        [property: DataMember][property: BsonElement("url")] string MatchId,
        [property: DataMember][property: BsonElement("category")] string Category) : BaseEntity
    {
        [DataMember]
        [BsonElement("calendarEvent")]
        public string CalendarEventId => Title;
    }
}
