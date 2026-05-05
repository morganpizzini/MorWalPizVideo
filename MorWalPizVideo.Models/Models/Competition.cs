using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models
{
    /// <summary>
    /// Represents a shooting competition/match
    /// </summary>
    public record Competition : BaseEntity
    {
        [DataMember]
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [DataMember]
        [BsonElement("description")]
        public string? Description { get; set; }

        [DataMember]
        [BsonElement("location")]
        public string? Location { get; set; }

        [DataMember]
        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [DataMember]
        [BsonElement("endDate")]
        public DateTime? EndDate { get; set; }

        [DataMember]
        [BsonElement("organizerId")]
        public string? OrganizerId { get; set; }

        [DataMember]
        [BsonElement("status")]
        public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;

        [DataMember]
        [BsonElement("maxParticipants")]
        public int? MaxParticipants { get; set; }

        [DataMember]
        [BsonElement("registrationDeadline")]
        public DateTime? RegistrationDeadline { get; set; }

        [DataMember]
        [BsonElement("rules")]
        public string? Rules { get; set; }

        [DataMember]
        [BsonElement("stages")]
        public List<Stage> Stages { get; set; } = new();

        [DataMember]
        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [DataMember]
        [BsonElement("websiteUrl")]
        public string? WebsiteUrl { get; set; }
    }

    /// <summary>
    /// Represents a stage within a competition (embedded document)
    /// </summary>
    public record Stage
    {
        [DataMember]
        [BsonElement("stageNumber")]
        public int StageNumber { get; init; }

        [DataMember]
        [BsonElement("name")]
        public string Name { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("description")]
        public string? Description { get; init; }

        [DataMember]
        [BsonElement("targetCount")]
        public int TargetCount { get; init; }

        [DataMember]
        [BsonElement("roundCount")]
        public int RoundCount { get; init; }

        [DataMember]
        [BsonElement("minScore")]
        public int MinScore { get; init; }

        [DataMember]
        [BsonElement("maxScore")]
        public int MaxScore { get; init; }

        [DataMember]
        [BsonElement("timeLimitSeconds")]
        public int? TimeLimitSeconds { get; init; }

        [DataMember]
        [BsonElement("briefing")]
        public string? Briefing { get; init; }

        [DataMember]
        [BsonElement("order")]
        public int Order { get; init; }
    }

    /// <summary>
    /// Competition status enum
    /// </summary>
    public enum CompetitionStatus
    {
        Draft = 0,
        Published = 1,
        RegistrationOpen = 2,
        RegistrationClosed = 3,
        InProgress = 4,
        Completed = 5,
        Cancelled = 6
    }
}