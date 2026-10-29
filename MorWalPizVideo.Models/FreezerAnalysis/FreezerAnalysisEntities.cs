using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.FreezerAnalysis;

public enum FreezerImportCompleteness
{
    Provisional,
    Final
}

public enum FreezerImportState
{
    Staged,
    Published,
    Rejected
}

public enum FreezerResultStatus
{
    Scored,
    Missing,
    Disqualified,
    Zeroed
}

public sealed record FreezerMatch : BaseEntity
{
    [BsonElement("matchKey")]
    public string MatchKey { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("location")]
    public string? Location { get; init; }

    [BsonElement("localMatchDate")]
    public string LocalMatchDate { get; init; } = string.Empty;

    [BsonElement("timeZoneId")]
    public string TimeZoneId { get; init; } = "Europe/Rome";

    [BsonElement("ownerAccountId")]
    public string OwnerAccountId { get; init; } = string.Empty;

    [BsonElement("activeRevisionId")]
    public string? ActiveRevisionId { get; init; }

    [BsonElement("revisionNumber")]
    public int RevisionNumber { get; init; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerMatchStage
{
    [BsonElement("stageId")]
    public string StageId { get; init; } = string.Empty;

    [BsonElement("stageNumber")]
    public int StageNumber { get; init; }

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("minimumRounds")]
    public int MinimumRounds { get; init; }

    [BsonElement("maximumPoints")]
    public int MaximumPoints { get; init; }
}

public sealed record FreezerMatchRevision : BaseEntity
{
    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;

    [BsonElement("revisionNumber")]
    public int RevisionNumber { get; init; }

    [BsonElement("sourceHash")]
    public string SourceHash { get; init; } = string.Empty;

    [BsonElement("sourceFormat")]
    public string SourceFormat { get; init; } = string.Empty;

    [BsonElement("parserVersion")]
    public string ParserVersion { get; init; } = string.Empty;

    [BsonElement("completeness")]
    public FreezerImportCompleteness Completeness { get; init; }

    [BsonElement("stages")]
    public IReadOnlyList<FreezerMatchStage> Stages { get; init; } = [];

    [BsonElement("resultIds")]
    public IReadOnlyList<string> ResultIds { get; init; } = [];

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerStageResult
{
    [BsonElement("stageId")]
    public string StageId { get; init; } = string.Empty;

    [BsonElement("minimumRounds")]
    public int MinimumRounds { get; init; }

    [BsonElement("hitFactor")]
    public double? HitFactor { get; init; }

    [BsonElement("timeSeconds")]
    public double? TimeSeconds { get; init; }

    [BsonElement("netPoints")]
    public int? NetPoints { get; init; }

    [BsonElement("classificationPoints")]
    public double? ClassificationPoints { get; init; }

    [BsonElement("alpha")]
    public int? Alpha { get; init; }

    [BsonElement("charlie")]
    public int? Charlie { get; init; }

    [BsonElement("delta")]
    public int? Delta { get; init; }

    [BsonElement("miss")]
    public int? Miss { get; init; }

    [BsonElement("noShoot")]
    public int? NoShoot { get; init; }

    [BsonElement("procedural")]
    public int? Procedural { get; init; }

    [BsonElement("status")]
    public FreezerResultStatus Status { get; init; }
}

public sealed record FreezerCompetitorResult : BaseEntity
{
    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;

    [BsonElement("competitorId")]
    public string CompetitorId { get; init; } = string.Empty;

    [BsonElement("contentHash")]
    public string ContentHash { get; init; } = string.Empty;

    [BsonElement("identityId")]
    public string? IdentityId { get; init; }

    [BsonElement("identityResolved")]
    public bool IdentityResolved { get; init; }

    [BsonElement("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [BsonElement("normalizedName")]
    public string NormalizedName { get; init; } = string.Empty;

    [BsonElement("division")]
    public string Division { get; init; } = string.Empty;

    [BsonElement("class")]
    public string? Class { get; init; }

    [BsonElement("powerFactor")]
    public string? PowerFactor { get; init; }

    [BsonElement("category")]
    public string? Category { get; init; }

    [BsonElement("squad")]
    public string? Squad { get; init; }

    [BsonElement("officialTotal")]
    public double? OfficialTotal { get; init; }

    [BsonElement("isDisqualified")]
    public bool IsDisqualified { get; init; }

    [BsonElement("isZeroed")]
    public bool IsZeroed { get; init; }

    [BsonElement("stages")]
    public IReadOnlyList<FreezerStageResult> Stages { get; init; } = [];
}

public sealed record FreezerShooterIdentity : BaseEntity
{
    [BsonElement("normalizedName")]
    public string NormalizedName { get; init; } = string.Empty;

    [BsonElement("aliases")]
    public IReadOnlyList<string> Aliases { get; init; } = [];

    [BsonElement("normalizedAliases")]
    public IReadOnlyList<string> NormalizedAliases { get; init; } = [];

    [BsonElement("federation")]
    public string? Federation { get; init; }

    [BsonElement("license")]
    public string? License { get; init; }

    [BsonElement("createdByAccountId")]
    public string CreatedByAccountId { get; init; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; init; }
}

public sealed record FreezerIdentityResolution : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;

    [BsonElement("resultId")]
    public string ResultId { get; init; } = string.Empty;

    [BsonElement("identityId")]
    public string IdentityId { get; init; } = string.Empty;

    [BsonElement("revision")]
    public long Revision { get; init; }

    [BsonElement("resolvedAtUtc")]
    public DateTime ResolvedAtUtc { get; init; }
}

public sealed record FreezerProfile : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("identityId")]
    public string? IdentityId { get; init; }

    [BsonElement("division")]
    public string? Division { get; init; }

    [BsonElement("powerFactor")]
    public string? PowerFactor { get; init; }

    [BsonElement("performance")]
    public double Performance { get; init; } = 0.935;

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed record FreezerImportManifest : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;

    [BsonElement("revisionId")]
    public string? RevisionId { get; init; }

    [BsonElement("sourceHash")]
    public string SourceHash { get; init; } = string.Empty;

    [BsonElement("sourceObjectKey")]
    public string? SourceObjectKey { get; init; }

    [BsonElement("format")]
    public string Format { get; init; } = string.Empty;

    [BsonElement("completeness")]
    public FreezerImportCompleteness Completeness { get; init; }

    [BsonElement("state")]
    public FreezerImportState State { get; init; }

    [BsonElement("availableBeforeStartCompetitorIds")]
    public IReadOnlyList<string> AvailableBeforeStartCompetitorIds { get; init; } = [];

    [BsonElement("excludedStageIds")]
    public IReadOnlyList<string> ExcludedStageIds { get; init; } = [];

    [BsonElement("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerAnalysisDraft : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;

    [BsonElement("revision")]
    public long Revision { get; init; }

    [BsonElement("importManifestId")]
    public string? ImportManifestId { get; init; }

    [BsonElement("identityId")]
    public string? IdentityId { get; init; }

    [BsonElement("division")]
    public string? Division { get; init; }

    [BsonElement("parameterVersion")]
    public string ParameterVersion { get; init; } = string.Empty;

    [BsonElement("beta")]
    public double Beta { get; init; } = 1.2;

    [BsonElement("driftPercent")]
    public double DriftPercent { get; init; } = 0.04;

    [BsonElement("ownVariability")]
    public double OwnVariability { get; init; } = 0.105;

    [BsonElement("distancePenalty")]
    public double DistancePenalty { get; init; } = 0.25;

    [BsonElement("performance")]
    public double Performance { get; init; } = 0.935;

    [BsonElement("applyAfternoon")]
    public bool ApplyAfternoon { get; init; }

    [BsonElement("applyOneHand")]
    public bool ApplyOneHand { get; init; }

    [BsonElement("excludedStageIds")]
    public IReadOnlyList<string> ExcludedStageIds { get; init; } = [];

    [BsonElement("rivalIdentityIds")]
    public IReadOnlyList<string> RivalIdentityIds { get; init; } = [];

    [BsonElement("strategyByStage")]
    public IReadOnlyDictionary<string, string> StrategyByStage { get; init; } = new Dictionary<string, string>();

    [BsonElement("startedAtUtc")]
    public DateTime? StartedAtUtc { get; init; }

    [BsonElement("frozenSnapshotId")]
    public string? FrozenSnapshotId { get; init; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; init; }

    [BsonElement("lastEventSequence")]
    public long LastEventSequence { get; init; }

    [BsonElement("renderingContext")]
    public FreezerRenderingContext? RenderingContext { get; init; }
}

public sealed record FreezerFrozenSnapshot : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("draftId")]
    public string DraftId { get; init; } = string.Empty;

    [BsonElement("draftRevision")]
    public long DraftRevision { get; init; }

    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;

    [BsonElement("matchRevisionId")]
    public string MatchRevisionId { get; init; } = string.Empty;

    [BsonElement("inputPackageHash")]
    public string InputPackageHash { get; init; } = string.Empty;

    [BsonElement("inputPackageObjectKey")]
    public string InputPackageObjectKey { get; init; } = string.Empty;

    [BsonElement("inputPackageLength")]
    public long InputPackageLength { get; init; }

    [BsonElement("engineVersion")]
    public string EngineVersion { get; init; } = string.Empty;

    [BsonElement("parameterVersion")]
    public string ParameterVersion { get; init; } = string.Empty;

    [BsonElement("parameters")]
    public FreezerSnapshotParameters Parameters { get; init; } = new();

    [BsonElement("forecast")]
    public IReadOnlyList<FreezerStageForecast> Forecast { get; init; } = [];

    [BsonElement("strategyByStage")]
    public IReadOnlyDictionary<string, string> StrategyByStage { get; init; } = new Dictionary<string, string>();

    [BsonElement("availabilitySetHash")]
    public string AvailabilitySetHash { get; init; } = string.Empty;

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerSnapshotParameters
{
    [BsonElement("beta")]
    public double Beta { get; init; }
    [BsonElement("driftPercent")]
    public double DriftPercent { get; init; }
    [BsonElement("ownVariability")]
    public double OwnVariability { get; init; }
    [BsonElement("distancePenalty")]
    public double DistancePenalty { get; init; }
    [BsonElement("distancePenaltyConfirmed")]
    public bool DistancePenaltyConfirmed { get; init; }
    [BsonElement("performance")]
    public double Performance { get; init; }
    [BsonElement("afternoonFactor")]
    public double AfternoonFactor { get; init; }
    [BsonElement("oneHandFactor")]
    public double OneHandFactor { get; init; }
    [BsonElement("applyAfternoon")]
    public bool ApplyAfternoon { get; init; }
    [BsonElement("applyOneHand")]
    public bool ApplyOneHand { get; init; }
}

public sealed record FreezerStageForecast
{
    [BsonElement("stageId")]
    public string StageId { get; init; } = string.Empty;
    [BsonElement("center")]
    public double? Center { get; init; }
    [BsonElement("lower")]
    public double? Lower { get; init; }
    [BsonElement("upper")]
    public double? Upper { get; init; }
    [BsonElement("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record FreezerFieldEvent : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;
    [BsonElement("snapshotId")]
    public string SnapshotId { get; init; } = string.Empty;
    [BsonElement("clientOperationId")]
    public string ClientOperationId { get; init; } = string.Empty;
    [BsonElement("sequence")]
    public long Sequence { get; init; }
    [BsonElement("kind")]
    public string Kind { get; init; } = string.Empty;
    [BsonElement("content")]
    public string Content { get; init; } = string.Empty;
    [BsonElement("serverReceivedAtUtc")]
    public DateTime ServerReceivedAtUtc { get; init; }
    [BsonElement("observedAtLocal")]
    public DateTime? ObservedAtLocal { get; init; }
    [BsonElement("observedOffsetMinutes")]
    public int? ObservedOffsetMinutes { get; init; }
    [BsonElement("supersedesEventId")]
    public string? SupersedesEventId { get; init; }
    [BsonElement("renderingContext")]
    public FreezerRenderingContext RenderingContext { get; init; } = new();
}

public sealed record FreezerRenderingContext
{
    [BsonElement("startStage")]
    public int? StartStage { get; init; }
    [BsonElement("startTurn")]
    public int? StartTurn { get; init; }
    [BsonElement("squadSize")]
    public int? SquadSize { get; init; }
    [BsonElement("displayOrder")]
    public IReadOnlyList<string> DisplayOrder { get; init; } = [];
}

public sealed record FreezerVerificationReport : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;
    [BsonElement("snapshotId")]
    public string SnapshotId { get; init; } = string.Empty;
    [BsonElement("finalRevisionId")]
    public string FinalRevisionId { get; init; } = string.Empty;
    [BsonElement("resultHash")]
    public string ResultHash { get; init; } = string.Empty;
    [BsonElement("stageCount")]
    public int StageCount { get; init; }
    [BsonElement("missingStageCount")]
    public int MissingStageCount { get; init; }
    [BsonElement("disqualifiedStageCount")]
    public int DisqualifiedStageCount { get; init; }
    [BsonElement("zeroedStageCount")]
    public int ZeroedStageCount { get; init; }
    [BsonElement("meanRelativeError")]
    public double? MeanRelativeError { get; init; }
    [BsonElement("errorSampleStandardDeviation")]
    public double? ErrorSampleStandardDeviation { get; init; }
    [BsonElement("intervalCoverageCount")]
    public int IntervalCoverageCount { get; init; }
    [BsonElement("observedBeta")]
    public double? ObservedBeta { get; init; }
    [BsonElement("stageGroupingConvention")]
    public string StageGroupingConvention { get; init; } = string.Empty;
    [BsonElement("groups")]
    public IReadOnlyList<FreezerVerificationGroupResult> Groups { get; init; } = [];
    [BsonElement("informantDriftComparisonCount")]
    public int InformantDriftComparisonCount { get; init; }
    [BsonElement("informantDriftMissingStageCount")]
    public int InformantDriftMissingStageCount { get; init; }
    [BsonElement("informantDriftDisqualifiedStageCount")]
    public int InformantDriftDisqualifiedStageCount { get; init; }
    [BsonElement("informantDriftZeroedStageCount")]
    public int InformantDriftZeroedStageCount { get; init; }
    [BsonElement("informantDriftUnverifiableStageCount")]
    public int InformantDriftUnverifiableStageCount { get; init; }
    [BsonElement("informantDriftMeanRelativeError")]
    public double? InformantDriftMeanRelativeError { get; init; }
    [BsonElement("informantDriftErrorSampleStandardDeviation")]
    public double? InformantDriftErrorSampleStandardDeviation { get; init; }
    [BsonElement("stages")]
    public IReadOnlyList<FreezerVerificationStageResult> Stages { get; init; } = [];
    [BsonElement("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerVerificationGroupResult
{
    [BsonElement("stageGroup")]
    public string StageGroup { get; init; } = string.Empty;
    [BsonElement("stageCount")]
    public int StageCount { get; init; }
    [BsonElement("verifiableStageCount")]
    public int VerifiableStageCount { get; init; }
    [BsonElement("missingStageCount")]
    public int MissingStageCount { get; init; }
    [BsonElement("disqualifiedStageCount")]
    public int DisqualifiedStageCount { get; init; }
    [BsonElement("zeroedStageCount")]
    public int ZeroedStageCount { get; init; }
    [BsonElement("unverifiableStageCount")]
    public int UnverifiableStageCount { get; init; }
    [BsonElement("meanRelativeError")]
    public double? MeanRelativeError { get; init; }
    [BsonElement("errorSampleStandardDeviation")]
    public double? ErrorSampleStandardDeviation { get; init; }
    [BsonElement("intervalCoverageCount")]
    public int IntervalCoverageCount { get; init; }
    [BsonElement("intervalCoverageRate")]
    public double? IntervalCoverageRate { get; init; }
}

public sealed record FreezerVerificationStageResult
{
    [BsonElement("stageId")]
    public string StageId { get; init; } = string.Empty;
    [BsonElement("stageNumber")]
    public int StageNumber { get; init; }
    [BsonElement("stageGroup")]
    public string? StageGroup { get; init; }
    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;
    [BsonElement("actualHitFactor")]
    public double? ActualHitFactor { get; init; }
    [BsonElement("forecastCenter")]
    public double? ForecastCenter { get; init; }
    [BsonElement("forecastLower")]
    public double? ForecastLower { get; init; }
    [BsonElement("forecastUpper")]
    public double? ForecastUpper { get; init; }
    [BsonElement("relativeError")]
    public double? RelativeError { get; init; }
    [BsonElement("isWithinPredictionInterval")]
    public bool? IsWithinPredictionInterval { get; init; }
    [BsonElement("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record FreezerParameterValues
{
    [BsonElement("beta")]
    public double Beta { get; init; }
    [BsonElement("driftPercent")]
    public double DriftPercent { get; init; }
    [BsonElement("ownVariability")]
    public double OwnVariability { get; init; }
    [BsonElement("distancePenalty")]
    public double DistancePenalty { get; init; }
}

public sealed record FreezerParameterBacktest : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;
    [BsonElement("engineVersion")]
    public string EngineVersion { get; init; } = string.Empty;
    [BsonElement("parameterVersion")]
    public string ParameterVersion { get; init; } = string.Empty;
    [BsonElement("parameters")]
    public FreezerParameterValues Parameters { get; init; } = new();
    [BsonElement("evaluationCount")]
    public int EvaluationCount { get; init; }
    [BsonElement("verifiableStageCount")]
    public int VerifiableStageCount { get; init; }
    [BsonElement("meanRelativeError")]
    public double? MeanRelativeError { get; init; }
    [BsonElement("errorSampleStandardDeviation")]
    public double? ErrorSampleStandardDeviation { get; init; }
    [BsonElement("intervalCoverageCount")]
    public int IntervalCoverageCount { get; init; }
    [BsonElement("evaluations")]
    public IReadOnlyList<FreezerParameterBacktestEvaluation> Evaluations { get; init; } = [];
    [BsonElement("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerParameterBacktestEvaluation
{
    [BsonElement("snapshotId")]
    public string SnapshotId { get; init; } = string.Empty;
    [BsonElement("matchId")]
    public string MatchId { get; init; } = string.Empty;
    [BsonElement("finalRevisionId")]
    public string FinalRevisionId { get; init; } = string.Empty;
    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;
    [BsonElement("verifiableStageCount")]
    public int VerifiableStageCount { get; init; }
    [BsonElement("missingStageCount")]
    public int MissingStageCount { get; init; }
    [BsonElement("disqualifiedStageCount")]
    public int DisqualifiedStageCount { get; init; }
    [BsonElement("zeroedStageCount")]
    public int ZeroedStageCount { get; init; }
    [BsonElement("meanRelativeError")]
    public double? MeanRelativeError { get; init; }
    [BsonElement("errorSampleStandardDeviation")]
    public double? ErrorSampleStandardDeviation { get; init; }
    [BsonElement("intervalCoverageCount")]
    public int IntervalCoverageCount { get; init; }
    [BsonElement("stages")]
    public IReadOnlyList<FreezerParameterBacktestStage> Stages { get; init; } = [];
    [BsonElement("diagnostics")]
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record FreezerParameterBacktestStage
{
    [BsonElement("stageId")]
    public string StageId { get; init; } = string.Empty;
    [BsonElement("stageNumber")]
    public int StageNumber { get; init; }
    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;
    [BsonElement("actualHitFactor")]
    public double? ActualHitFactor { get; init; }
    [BsonElement("forecastCenter")]
    public double? ForecastCenter { get; init; }
    [BsonElement("forecastLower")]
    public double? ForecastLower { get; init; }
    [BsonElement("forecastUpper")]
    public double? ForecastUpper { get; init; }
    [BsonElement("relativeError")]
    public double? RelativeError { get; init; }
    [BsonElement("isWithinPredictionInterval")]
    public bool? IsWithinPredictionInterval { get; init; }
}

public sealed record FreezerParameterAdoption : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;
    [BsonElement("revision")]
    public long Revision { get; init; }
    [BsonElement("backtestId")]
    public string BacktestId { get; init; } = string.Empty;
    [BsonElement("parameterVersion")]
    public string ParameterVersion { get; init; } = string.Empty;
    [BsonElement("parameters")]
    public FreezerParameterValues Parameters { get; init; } = new();
    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record FreezerArtifactManifest : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;
    [BsonElement("snapshotId")]
    public string SnapshotId { get; init; } = string.Empty;
    [BsonElement("inputKey")]
    public string InputKey { get; init; } = string.Empty;
    [BsonElement("objectKey")]
    public string ObjectKey { get; init; } = string.Empty;
    [BsonElement("sha256")]
    public string Sha256 { get; init; } = string.Empty;
    [BsonElement("length")]
    public long Length { get; init; }
    [BsonElement("contentType")]
    public string ContentType { get; init; } = string.Empty;
    [BsonElement("rendererVersion")]
    public string RendererVersion { get; init; } = string.Empty;
    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}
