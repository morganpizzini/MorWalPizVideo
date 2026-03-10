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
            DateTime startDate,
            DateTime endDate,
            CategoryRef[] categories,
            string matchId = "")
        {
            Title = title;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            Categories = categories;
            MatchId = matchId;
        }

        [DataMember]
        [BsonElement("title")]
        public string Title { get; init; }

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        [DataMember]
        [BsonElement("startDate")]
        public DateTime StartDate { get; init; }

        [DataMember]
        [BsonElement("endDate")]
        public DateTime EndDate { get; init; }

        [DataMember]
        [BsonElement("categories")]
        public CategoryRef[] Categories { get; init; }

        [DataMember]
        [BsonElement("matchId")]
        public string MatchId { get; init; } = "";

        [BsonIgnore]
        public string MatchUrl { get; set; } = string.Empty;

        [BsonIgnore]
        public bool OldEvent => DateTime.Now.Date > EndDate.Date;
    }
}