using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record CalendarEvent : BaseEntity
    {
        [JsonConstructor]
        public CalendarEvent(
            string title,
            string description,
            DateOnly date,
            string category,
            string matchId = "")
        {
            Title = title;
            Description = description;
            Date = date;
            Category = category;
            MatchId = matchId;
        }

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        [DataMember]
        [BsonElement("date")]
        public DateOnly Date { get; init; }

        [DataMember]
        [BsonElement("category")]
        public string Category { get; init; }

        [DataMember]
        [BsonElement("matchId")]
        public string MatchId { get; init; } = "";

        [DataMember]
        [BsonElement("calendarEventId")]
        public string CalendarEventId => Title;

        [BsonIgnore]
        public string MatchUrl { get; set; } = string.Empty;

        [BsonIgnore]
        public bool OldEvent => DateTime.Now.Date > Date.ToDateTime(new TimeOnly(0, 0));    }
}
