using MorWalPizVideo.Server.Models;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public sealed class CompetitionContract
{
    [DataMember]
    public string Id { get; init; } = string.Empty;

    [DataMember]
    public string Name { get; init; } = string.Empty;

    [DataMember]
    public string? Description { get; init; }

    [DataMember]
    public string? Location { get; init; }

    [DataMember]
    public DateTime StartDate { get; init; }

    [DataMember]
    public DateTime? EndDate { get; init; }

    [DataMember]
    public CompetitionStatus Status { get; init; }

    [DataMember]
    public CompetitionType Type { get; init; }

    [DataMember]
    public int? MaxParticipants { get; init; }

    [DataMember]
    public DateTime? RegistrationDeadline { get; init; }

    [DataMember]
    public string? Rules { get; init; }

    [DataMember]
    public IReadOnlyList<CompetitionStageContract> Stages { get; init; } = [];

    [DataMember]
    public string? ImageUrl { get; init; }

    [DataMember]
    public string? WebsiteUrl { get; init; }
}

[DataContract]
public sealed class CompetitionStageContract
{
    [DataMember]
    public string StageId { get; init; } = string.Empty;

    [DataMember]
    public int StageNumber { get; init; }

    [DataMember]
    public string Name { get; init; } = string.Empty;

    [DataMember]
    public string? Description { get; init; }

    [DataMember]
    public int TargetCount { get; init; }

    [DataMember]
    public int RoundCount { get; init; }

    [DataMember]
    public int MinScore { get; init; }

    [DataMember]
    public int MaxScore { get; init; }

    [DataMember]
    public int? TimeLimitSeconds { get; init; }

    [DataMember]
    public string? Briefing { get; init; }

    [DataMember]
    public int Order { get; init; }

    [DataMember]
    public IReadOnlyList<string> Images { get; init; } = [];

    [DataMember]
    public CompetitionStageStatsContract Stats { get; init; } = new();
}

[DataContract]
public sealed class CompetitionStageStatsContract
{
    [DataMember]
    public double AverageRating { get; init; }

    [DataMember]
    public int TotalReviews { get; init; }
}
