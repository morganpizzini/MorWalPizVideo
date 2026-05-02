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
        public string Name { get; init; } = string.Empty;

        [DataMember]
        [BsonElement("description")]
        public string? Description { get; init; }

        [DataMember]
        [BsonElement("location")]
        public string? Location { get; init; }

        [DataMember]
        [BsonElement("startDate")]
        public DateTime StartDate { get; init; }

        [DataMember]
        [BsonElement("endDate")]
        public DateTime? EndDate { get; init; }

        [DataMember]
        [BsonElement("organizerId")]
        public string? OrganizerId { get; init; }

        [DataMember]
        [BsonElement("status")]
        public CompetitionStatus Status { get; init; } = CompetitionStatus.Draft;

        [DataMember]
        [BsonElement("maxParticipants")]
        public int? MaxParticipants { get; init; }

        [DataMember]
        [BsonElement("registrationDeadline")]
        public DateTime? RegistrationDeadline { get; init; }

        [DataMember]
        [BsonElement("rules")]
        public string? Rules { get; init; }

        [DataMember]
        [BsonElement("stages")]
        public List<Stage> Stages { get; init; } = new();

        [DataMember]
        [BsonElement("imageUrl")]
        public string? ImageUrl { get; init; }

        [DataMember]
        [BsonElement("websiteUrl")]
        public string? WebsiteUrl { get; init; }
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