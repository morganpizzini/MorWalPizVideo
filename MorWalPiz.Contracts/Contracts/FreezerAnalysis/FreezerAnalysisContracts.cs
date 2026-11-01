using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.Contracts.Contracts.FreezerAnalysis;

public sealed record FreezerProfileRequest
{
    [StringLength(100)]
    public string? IdentityId { get; init; }

    [StringLength(32)]
    public string? Division { get; init; }

    [RegularExpression("^(Major|Minor)?$", ErrorMessage = "Power factor must be Major or Minor.")]
    public string? PowerFactor { get; init; }

    [Range(0.01, 1)]
    public double Performance { get; init; } = 0.935;
}

public sealed record FreezerProfileResponse(
    string AccountId,
    string? IdentityId,
    string? Division,
    string? PowerFactor,
    double Performance,
    DateTime UpdatedAtUtc);

public sealed record FreezerDivisionOptionResponse(string Code, string Label);

public sealed record FreezerReferenceDataResponse(
    IReadOnlyList<FreezerDivisionOptionResponse> Divisions);

public sealed record FreezerResultSummaryResponse(
    string ResultId,
    string CompetitorId,
    string DisplayName,
    string Division,
    string? Class,
    string? PowerFactor,
    string? Squad,
    double? OfficialTotal,
    string Status,
    int StageResultCount);

public sealed record FreezerResultStageResponse(
    string StageId,
    int StageNumber,
    string StageName,
    int MinimumRounds,
    string Status,
    double? HitFactor,
    double? TimeSeconds,
    int? NetPoints,
    double? ClassificationPoints,
    int? Alpha,
    int? Charlie,
    int? Delta,
    int? Misses,
    int? NoShoots,
    int? Procedurals);

public sealed record FreezerResultDetailResponse(
    FreezerResultSummaryResponse Summary,
    IReadOnlyList<FreezerResultStageResponse> Stages);

public sealed record FreezerIdentityCreateRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; init; } = string.Empty;

    [MaxLength(20)]
    public IReadOnlyList<string> Aliases { get; init; } = [];
}

public sealed record FreezerIdentityResponse(
    string Id,
    string NormalizedName,
    IReadOnlyList<string> Aliases,
    int Revision);

public sealed record FreezerIdentityCandidateResponse(
    string Id,
    string NormalizedName,
    IReadOnlyList<string> Aliases);

public sealed record FreezerIdentityReviewEntryResponse(
    string ResultId,
    string CompetitorId,
    string DisplayName,
    string Division,
    string? IdentityId,
    long ResolutionRevision,
    IReadOnlyList<FreezerIdentityCandidateResponse> Candidates);

public sealed record FreezerIdentityResolutionRequest
{
    [Required, StringLength(100)]
    public string IdentityId { get; init; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }
}

public sealed record FreezerIdentityResolutionResponse(
    string ResultId,
    string IdentityId,
    long Revision,
    DateTime ResolvedAtUtc);

public sealed record FreezerMatchResponse(
    string Id,
    string Name,
    string? Location,
    DateOnly LocalMatchDate,
    string TimeZoneId,
    int RevisionNumber,
    bool IsFinal,
    int StageCount,
    int CompetitorCount,
    string ActiveRevisionId,
    bool CanVerify);

public sealed record FreezerImportPreviewResponse(
    string ImportId,
    string MatchName,
    DateOnly LocalMatchDate,
    string TimeZoneId,
    string Format,
    string Completeness,
    int StageCount,
    int CompetitorCount,
    int UnresolvedIdentityCount,
    int MatchedResultCount,
    IReadOnlyList<string> Diagnostics,
    bool CanPublish,
    int ExpectedRevision = 0);

public sealed record FreezerDraftRequest
{
    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }

    [StringLength(100)]
    public string? ImportManifestId { get; init; }

    [StringLength(100)]
    public string? IdentityId { get; init; }

    [StringLength(32)]
    public string? Division { get; init; }

    [Range(0.1, 3)]
    public double Beta { get; init; } = 1.2;

    [Range(0, 0.5)]
    public double DriftPercent { get; init; } = 0.04;

    [Range(0, 1)]
    public double OwnVariability { get; init; } = 0.105;

    [Range(0, 2)]
    public double DistancePenalty { get; init; } = 0.25;

    [Range(0.01, 1)]
    public double Performance { get; init; } = 0.935;

    public bool ApplyAfternoon { get; init; }
    public bool ApplyOneHand { get; init; }

    public IReadOnlyList<string> ExcludedStageIds { get; init; } = [];
    public IReadOnlyList<string> RivalIdentityIds { get; init; } = [];
    public IReadOnlyDictionary<string, string> StrategyByStage { get; init; } = new Dictionary<string, string>();
}

public sealed record FreezerDraftResponse(
    string Id,
    string MatchId,
    long Revision,
    string? ImportManifestId,
    string? IdentityId,
    string? Division,
    string ParameterVersion,
    double Beta,
    double DriftPercent,
    double OwnVariability,
    double DistancePenalty,
    bool DistancePenaltyConfirmed,
    double Performance,
    bool ApplyAfternoon,
    bool ApplyOneHand,
    IReadOnlyList<string> ExcludedStageIds,
    IReadOnlyList<string> RivalIdentityIds,
    IReadOnlyDictionary<string, string> StrategyByStage,
    bool IsStarted,
    string? FrozenSnapshotId);

public sealed record FreezerEstimateResponse(
    string EngineVersion,
    string DispersionConvention,
    bool IsAvailable,
    IReadOnlyList<FreezerStageEstimateResponse> Stages,
    IReadOnlyList<string> Diagnostics);

public sealed record FreezerStageEstimateResponse(
    string StageId,
    int StageNumber,
    int MinimumRounds,
    string? RhythmSymbol,
    double? Center,
    double? Lower,
    double? Upper,
    double? MissCostSeconds,
    double? CharlieCostSeconds,
    IReadOnlyList<string> Diagnostics);

public sealed record FreezerFreezeRequest
{
    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }
}

public sealed record FreezerSnapshotResponse(
    string Id,
    string MatchId,
    string EngineVersion,
    string ParameterVersion,
    DateTime CreatedAtUtc,
    IReadOnlyList<FreezerStageEstimateResponse> Stages);

public sealed record FreezerSnapshotSelectionResponse(
    string Id,
    string MatchId,
    string MatchRevisionId,
    string EngineVersion,
    string ParameterVersion,
    DateTime CreatedAtUtc);

public sealed record FreezerVerificationRequest
{
    [Required, StringLength(100)]
    public string FinalRevisionId { get; init; } = string.Empty;
}

public sealed record FreezerVerificationResponse(
    string Id,
    string SnapshotId,
    string FinalRevisionId,
    int VerifiableStageCount,
    int MissingStageCount,
    int DisqualifiedStageCount,
    int ZeroedStageCount,
    double? MeanRelativeError,
    double? ErrorSampleStandardDeviation,
    int IntervalCoverageCount,
    double? ObservedBeta,
    string StageGroupingConvention,
    int InformantDriftComparisonCount,
    int InformantDriftMissingStageCount,
    int InformantDriftDisqualifiedStageCount,
    int InformantDriftZeroedStageCount,
    int InformantDriftUnverifiableStageCount,
    double? InformantDriftMeanRelativeError,
    double? InformantDriftErrorSampleStandardDeviation,
    IReadOnlyList<FreezerVerificationGroupResponse> Groups,
    IReadOnlyList<FreezerVerificationStageResponse> Stages,
    IReadOnlyList<string> Diagnostics);

public sealed record FreezerVerificationGroupResponse(
    string StageGroup,
    int StageCount,
    int VerifiableStageCount,
    int MissingStageCount,
    int DisqualifiedStageCount,
    int ZeroedStageCount,
    int UnverifiableStageCount,
    double? MeanRelativeError,
    double? ErrorSampleStandardDeviation,
    int IntervalCoverageCount,
    double? IntervalCoverageRate);

public sealed record FreezerVerificationStageResponse(
    string StageId,
    int StageNumber,
    string Status,
    double? ActualHitFactor,
    double? ForecastCenter,
    double? ForecastLower,
    double? ForecastUpper,
    double? RelativeError,
    bool? IsWithinPredictionInterval,
    IReadOnlyList<string> Diagnostics,
    string? StageGroup = null);

public sealed record FreezerSnapshotReplayResponse(
    string SnapshotId,
    string EngineVersion,
    bool IsReplayAvailable,
    bool MatchesFrozenForecast,
    IReadOnlyList<FreezerSnapshotReplayStageResponse> Stages,
    IReadOnlyList<string> Diagnostics);

public sealed record FreezerSnapshotReplayStageResponse(
    string StageId,
    int StageNumber,
    double? FrozenCenter,
    double? ReplayedCenter,
    double? FrozenLower,
    double? ReplayedLower,
    double? FrozenUpper,
    double? ReplayedUpper,
    bool MatchesFrozenForecast);

public sealed record FreezerParameterValuesRequest
{
    [Range(0.1, 5)]
    public double Beta { get; init; } = 1.2;
    [Range(0, 0.5)]
    public double DriftPercent { get; init; } = 0.04;
    [Range(0.001, 1)]
    public double OwnVariability { get; init; } = 0.105;
    [Range(0, 2)]
    public double DistancePenalty { get; init; } = 0.25;
}

public sealed record FreezerParameterBacktestEvaluationRequest
{
    [Required, StringLength(100)]
    public string SnapshotId { get; init; } = string.Empty;
    [Required, StringLength(100)]
    public string FinalRevisionId { get; init; } = string.Empty;
}

public sealed record FreezerParameterBacktestRequest
{
    [Required]
    public FreezerParameterValuesRequest Parameters { get; init; } = new();
    [Required, MinLength(1), MaxLength(100)]
    public IReadOnlyList<FreezerParameterBacktestEvaluationRequest> Evaluations { get; init; } = [];
}

public sealed record FreezerParameterBacktestResponse(
    string Id,
    string EngineVersion,
    string ParameterVersion,
    FreezerParameterValuesRequest Parameters,
    int EvaluationCount,
    int VerifiableStageCount,
    double? MeanRelativeError,
    double? ErrorSampleStandardDeviation,
    int IntervalCoverageCount,
    IReadOnlyList<FreezerParameterBacktestEvaluationResponse> Evaluations,
    IReadOnlyList<string> Diagnostics,
    DateTime CreatedAtUtc);

public sealed record FreezerParameterBacktestEvaluationResponse(
    string SnapshotId,
    string MatchId,
    string FinalRevisionId,
    string Status,
    int VerifiableStageCount,
    int MissingStageCount,
    int DisqualifiedStageCount,
    int ZeroedStageCount,
    double? MeanRelativeError,
    double? ErrorSampleStandardDeviation,
    int IntervalCoverageCount,
    IReadOnlyList<FreezerParameterBacktestStageResponse> Stages,
    IReadOnlyList<string> Diagnostics);

public sealed record FreezerParameterBacktestStageResponse(
    string StageId,
    int StageNumber,
    string Status,
    double? ActualHitFactor,
    double? ForecastCenter,
    double? ForecastLower,
    double? ForecastUpper,
    double? RelativeError,
    bool? IsWithinPredictionInterval);

public sealed record FreezerParameterAdoptionRequest
{
    [Required, StringLength(100)]
    public string BacktestId { get; init; } = string.Empty;
    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }
    public bool Confirm { get; init; }
}

public sealed record FreezerParameterAdoptionResponse(
    long Revision,
    string BacktestId,
    string ParameterVersion,
    FreezerParameterValuesRequest Parameters,
    DateTime CreatedAtUtc);

public sealed record FreezerFieldEventRequest
{
    [Required, StringLength(100)]
    public string ClientOperationId { get; init; } = string.Empty;

    [Required, RegularExpression("^(turn|reconnaissance|midday|correction)$")]
    public string Kind { get; init; } = string.Empty;

    [StringLength(2000)]
    public string Content { get; init; } = string.Empty;

    public DateTime? ObservedAtLocal { get; init; }
    [Range(-840, 840)]
    public int? ObservedOffsetMinutes { get; init; }
    public string? SupersedesEventId { get; init; }
    public int? StartStage { get; init; }
    public int? StartTurn { get; init; }
    public int? SquadSize { get; init; }
    public IReadOnlyList<string> DisplayOrder { get; init; } = [];
}

public sealed record FreezerFieldEventResponse(
    string Id,
    string SnapshotId,
    long Sequence,
    string Kind,
    string Content,
    DateTime ServerReceivedAtUtc,
    DateTime? ObservedAtLocal,
    int? ObservedOffsetMinutes,
    int? StartStage = null,
    int? StartTurn = null,
    int? SquadSize = null,
    IReadOnlyList<string>? DisplayOrder = null);

public sealed record FreezerPdfPairPreviewResponse(
    int ClassificationPageCount,
    int ClassificationCompetitorCount,
    int DisqualifiedCompetitorCount,
    int VerificationPageCount,
    int VerificationCompetitorCount,
    int VerificationStageResultCount,
    int MatchedCompetitorCount,
    int UnmatchedClassificationCount,
    int UnmatchedVerificationCount,
    int AmbiguousNameDivisionGroupCount,
    int MappedDisqualifiedCompetitorCount,
    int UnmatchedDisqualifiedCompetitorCount,
    bool CanPublish,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<FreezerPdfClassificationPreview> ClassificationEntries,
    IReadOnlyList<FreezerPdfVerificationPreview> VerificationEntries,
    int ParsedVerificationStageResultCount,
    int HitFactorTimeMismatchCount,
    IReadOnlyList<FreezerPdfDisqualificationPreview>? DisqualifiedEntries = null);

public sealed record FreezerPdfDisqualificationPreview(
    string DisplayName,
    string Division,
    int PageNumber,
    string PairingStatus);

public sealed record FreezerPdfClassificationPreview(
    string DisplayName,
    string Division,
    int Rank,
    double Percent,
    double Points,
    int PageNumber,
    string PairingStatus,
    bool? IsDisqualified);

public sealed record FreezerPdfVerificationPreview(
    string CompetitorNumber,
    string DisplayName,
    string Division,
    string? PowerFactor,
    int PageNumber,
    string PairingStatus,
    bool? IsDisqualified,
    IReadOnlyList<FreezerPdfStagePreview> Stages);

public sealed record FreezerPdfStagePreview(
    int StageNumber,
    double? HitFactor,
    int? ReportedPoints,
    int? Alpha,
    int? Charlie,
    int? Delta,
    int? Deduction,
    int? Misses,
    int? NoShoots,
    int? Procedurals,
    string? OvertimeValue,
    double? TimeSeconds,
    int PageNumber,
    bool? HitFactorMatchesReportedPointsAndTime,
    IReadOnlyList<string> Diagnostics);
