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

        /// <summary>Discipline type: IDPA or IPSC.</summary>
        [DataMember]
        [BsonElement("type")]
        public CompetitionType Type { get; set; } = CompetitionType.IDPA;

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
        /// <summary>Unique identifier for this stage (UUID).</summary>
        [DataMember]
        [BsonElement("stageId")]
        public string StageId { get; init; } = Guid.NewGuid().ToString();

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

        /// <summary>URLs of images (maps, photos) stored in Blob Storage.</summary>
        [DataMember]
        [BsonElement("images")]
        public List<string> Images { get; init; } = new();

        /// <summary>Community evaluations embedded in the stage document.</summary>
        [DataMember]
        [BsonElement("evaluations")]
        public List<StageEvaluation> Evaluations { get; init; } = new();

        /// <summary>Pre-computed aggregated stats for fast frontend rendering.</summary>
        [DataMember]
        [BsonElement("stats")]
        public StageStats Stats { get; init; } = new();
    }

    /// <summary>A single user evaluation/rating for a stage.</summary>
    public record StageEvaluation
    {
        [BsonElement("userId")]
        public string UserId { get; init; } = string.Empty;

        /// <summary>Denormalised username for fast display without a JOIN.</summary>
        [BsonElement("username")]
        public string Username { get; init; } = string.Empty;

        /// <summary>Star rating 0-5. 0 = comment only, no star vote.</summary>
        [BsonElement("rating")]
        public int Rating { get; init; }

        [BsonElement("comment")]
        public string? Comment { get; init; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>Computed statistics aggregated from Evaluations.</summary>
    public record StageStats
    {
        [BsonElement("averageRating")]
        public double AverageRating { get; init; }

        [BsonElement("totalReviews")]
        public int TotalReviews { get; init; }
    }

    /// <summary>
    /// Competition discipline type
    /// </summary>
    public enum CompetitionType
    {
        IDPA = 0,
        IPSC = 1
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