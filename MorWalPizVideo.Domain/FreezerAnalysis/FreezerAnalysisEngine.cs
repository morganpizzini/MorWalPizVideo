namespace MorWalPizVideo.Domain.FreezerAnalysis;

public static class FreezerAnalysisEngine
{
    public const string LegacyVersion = "freezer-log-two-way-200-sample-v1";
    public const string Version = "freezer-log-two-way-200-sample-v1-groups-r1-12-r13-24-r25plus";
    public const int AlternatingMeanIterations = 200;
    public const string DispersionConvention = "sample-standard-deviation; natural-log hit-factor ratios";
    public const string StageGroupingConvention = "minimum rounds: short=1-12, medium=13-24, long=25+";

    public static readonly FreezerAnalysisParameters CurrentParameters = new(
        Version: "defaults-2026-10",
        Beta: 1.2,
        DriftPercent: 0.04,
        OwnVariability: 0.105,
        DistancePenalty: 0.25,
        Performance: 0.935,
        AfternoonFactor: 0.94,
        OneHandFactor: 0.89,
        DistancePenaltyConfirmed: false);

    public static TwoWayFitResult FitStageProfiles(IEnumerable<ShooterStageScore> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var diagnostics = new List<EngineDiagnostic>();
        var observations = new List<LogObservation>();
        foreach (var score in source.OrderBy(x => x.ShooterId, StringComparer.Ordinal)
                     .ThenBy(x => x.StageId, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(score.ShooterId) || string.IsNullOrWhiteSpace(score.StageId))
            {
                diagnostics.Add(new("invalid_identity", "A score without shooter or stage identity was excluded."));
                continue;
            }

            if (!score.IsAvailableBeforeStart)
                continue;
            if (score.IsExcluded)
                continue;
            if (!double.IsFinite(score.HitFactor) || score.HitFactor <= 0)
            {
                diagnostics.Add(new("non_positive_hf", "A non-positive or non-finite HF was excluded from the logarithmic fit."));
                continue;
            }

            observations.Add(new(score.ShooterId, score.StageId, Math.Log(score.HitFactor)));
        }

        if (observations.Count == 0)
            return TwoWayFitResult.Unavailable(diagnostics.Append(
                new EngineDiagnostic("no_positive_hf", "No positive HF observations are available.")));

        var observationsByShooter = observations.GroupBy(x => x.ShooterId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var observationsByStage = observations.GroupBy(x => x.StageId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var shooterEffects = observationsByShooter.ToDictionary(
            group => group.Key,
            group => group.Value.Average(x => x.LogHitFactor),
            StringComparer.Ordinal);
        var stageEffects = observationsByStage.Keys
            .ToDictionary(stage => stage, _ => 0d, StringComparer.Ordinal);

        for (var iteration = 0; iteration < AlternatingMeanIterations; iteration++)
        {
            foreach (var stage in stageEffects.Keys.Order(StringComparer.Ordinal))
            {
                var matching = observationsByStage[stage];
                stageEffects[stage] = matching.Average(x => x.LogHitFactor - shooterEffects[x.ShooterId]);
            }

            var stageCenter = stageEffects.Values.Average();
            foreach (var stage in stageEffects.Keys.ToArray())
                stageEffects[stage] -= stageCenter;

            foreach (var shooter in shooterEffects.Keys.Order(StringComparer.Ordinal))
            {
                var matching = observationsByShooter[shooter];
                shooterEffects[shooter] = matching.Average(x => x.LogHitFactor - stageEffects[x.StageId]);
            }
        }

        var residuals = observations.Select(x => x.LogHitFactor - shooterEffects[x.ShooterId] - stageEffects[x.StageId]).ToArray();
        var degreesOfFreedom = observations.Count - shooterEffects.Count - stageEffects.Count + 1;
        var residualVariance = degreesOfFreedom > 0
            ? residuals.Sum(x => x * x) / degreesOfFreedom
            : double.NaN;

        if (!double.IsFinite(residualVariance))
            diagnostics.Add(new("undefined_fit_variance", "The two-way fit has insufficient degrees of freedom to estimate uncertainty."));

        var shooterProfiles = shooterEffects.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                var count = observationsByShooter[pair.Key].Length;
                var standardError = double.IsFinite(residualVariance) && count > 0
                    ? Math.Sqrt(residualVariance / count)
                    : double.NaN;
                return new ShooterProfile(pair.Value, standardError, count);
            },
            StringComparer.Ordinal);

        var stageProfiles = stageEffects.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                var values = observationsByStage[pair.Key]
                    .Select(x => x.LogHitFactor - shooterEffects[x.ShooterId] - pair.Value)
                    .ToArray();
                var count = values.Length;
                var standardError = double.IsFinite(residualVariance) && count > 0
                    ? Math.Sqrt(residualVariance / count)
                    : double.NaN;
                var dispersion = SampleStandardDeviation(values);
                return new StageProfile(pair.Value, standardError, dispersion, count);
            },
            StringComparer.Ordinal);

        return new TwoWayFitResult(
            IsAvailable: true,
            Version,
            DispersionConvention,
            shooterProfiles,
            stageProfiles,
            diagnostics);
    }

    public static RegistryResult BuildRegistry(
        DateOnly targetMatchDate,
        string division,
        string userIdentityId,
        IEnumerable<HistoricalMatchResults> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var diagnostics = new List<EngineDiagnostic>();
        var matchEntries = new List<RegistryMatchEntry>();

        foreach (var match in history
                     .Where(x => x.LocalMatchDate < targetMatchDate)
                     .OrderBy(x => x.LocalMatchDate)
                     .ThenBy(x => x.MatchId, StringComparer.Ordinal))
        {
            var user = match.Results.FirstOrDefault(x =>
                x.IdentityId == userIdentityId &&
                string.Equals(x.Division, division, StringComparison.OrdinalIgnoreCase));
            if (user is null)
                continue;

            var validUserScores = user.StageHitFactors
                .Where(x => !user.ExcludedStageIds.Contains(x.Key) && double.IsFinite(x.Value) && x.Value > 0)
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            var minimumOverlap = Math.Max(5, validUserScores.Count - 3);

            foreach (var opponent in match.Results
                         .Where(x => x.IdentityId != userIdentityId &&
                                     string.Equals(x.Division, division, StringComparison.OrdinalIgnoreCase) &&
                                     x.IsIdentityResolved)
                         .OrderBy(x => x.IdentityId, StringComparer.Ordinal))
            {
                var ratios = validUserScores
                    .Where(x => opponent.StageHitFactors.TryGetValue(x.Key, out var hf) &&
                                double.IsFinite(hf) && hf > 0)
                    .Select(x => Math.Log(opponent.StageHitFactors[x.Key] / x.Value))
                    .Where(double.IsFinite)
                    .ToArray();
                if (ratios.Length < minimumOverlap)
                {
                    diagnostics.Add(new("insufficient_overlap", $"A historical pairing in match {match.MatchId} had fewer than {minimumOverlap} valid common stages."));
                    continue;
                }

                var dispersion = SampleStandardDeviation(ratios);
                var weight = ratios.Length / (dispersion * dispersion);
                if (!double.IsFinite(weight) || weight <= 0)
                {
                    diagnostics.Add(new(
                        dispersion == 0 ? "zero_variance_pairing" : "invalid_pairing_weight",
                        "A historical pairing with zero variance or a non-finite weight was excluded."));
                    continue;
                }

                matchEntries.Add(new RegistryMatchEntry(
                    opponent.IdentityId,
                    opponent.Division,
                    match.LocalMatchDate,
                    ratios.Average(),
                    dispersion,
                    ratios.Length,
                    weight));
            }
        }

        var entries = new List<RegistryShooterEntry>();
        foreach (var group in matchEntries.GroupBy(x => x.IdentityId, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var ordered = group.OrderByDescending(x => x.LocalMatchDate)
                .ThenBy(x => x.Division, StringComparer.Ordinal)
                .ToArray();
            var divisionHistory = ordered.Select(x => x.Division).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var included = divisionHistory.Length > 1
                ? ordered.Take(1).ToArray()
                : ordered;
            var totalWeight = included.Sum(x => x.Weight);
            if (!double.IsFinite(totalWeight) || totalWeight <= 0)
            {
                diagnostics.Add(new("invalid_registry_weight", "A shooter registry entry had no finite positive weight."));
                continue;
            }

            var scarto = included.Sum(x => x.LogScarto * x.Weight) / totalWeight;
            var dispersion = Math.Sqrt(included.Sum(x => x.Dispersion * x.Dispersion * x.Weight) / totalWeight);
            var percentScarto = Math.Exp(scarto) - 1;
            var dispersionPercent = Math.Exp(dispersion) - 1;
            entries.Add(new RegistryShooterEntry(
                group.Key,
                included[0].Division,
                scarto,
                dispersion,
                1d / totalWeight,
                included.Sum(x => x.StageCount),
                included.Select(x => x.LocalMatchDate).Max(),
                Math.Abs(percentScarto) <= 0.083 && dispersionPercent <= 0.15,
                included.Length));
        }

        return new RegistryResult(entries, diagnostics);
    }

    public static UserLevelEstimate EstimateUserLevel(IEnumerable<InformantEstimateInput> source, FreezerAnalysisParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var diagnostics = new List<EngineDiagnostic>();
        var weighted = new List<(double Level, double Weight)>();
        var drift = Math.Log(1 + parameters.DriftPercent);
        foreach (var informant in source.OrderBy(x => x.IdentityId, StringComparer.Ordinal))
        {
            var variance = informant.RegistryStandardError * informant.RegistryStandardError +
                           drift * drift +
                           Math.Pow(parameters.DistancePenalty * informant.LogScarto, 2);
            var weight = 1 / variance;
            if (!double.IsFinite(weight) || weight <= 0 ||
                !double.IsFinite(informant.ShooterLogLevel) ||
                !double.IsFinite(informant.LogScarto))
            {
                diagnostics.Add(new("invalid_informant_weight", "An informant with a non-finite or non-positive weight was excluded."));
                continue;
            }

            weighted.Add((informant.ShooterLogLevel - informant.LogScarto, weight));
        }

        if (weighted.Count == 0)
            return UserLevelEstimate.Unavailable(diagnostics.Append(
                new EngineDiagnostic("no_eligible_informants", "No eligible same-division informants have a usable estimate.")));

        var totalWeight = weighted.Sum(x => x.Weight);
        var level = weighted.Sum(x => x.Level * x.Weight) / totalWeight;
        var varianceLevel = 1 / totalWeight;
        if (!double.IsFinite(level) || !double.IsFinite(varianceLevel) || varianceLevel <= 0)
            return UserLevelEstimate.Unavailable(diagnostics.Append(
                new EngineDiagnostic("invalid_user_level", "The weighted user level is not finite.")));

        return new UserLevelEstimate(true, level, varianceLevel, weighted.Count, diagnostics);
    }

    public static StageForecast ForecastStage(
        UserLevelEstimate userLevel,
        StageProfile stage,
        FreezerAnalysisParameters parameters,
        bool applyAfternoon = false,
        bool applyOneHand = false)
    {
        var diagnostics = new List<EngineDiagnostic>(userLevel.Diagnostics);
        if (!userLevel.IsAvailable || !double.IsFinite(stage.LogDifficulty) ||
            !double.IsFinite(stage.StandardError) || !double.IsFinite(stage.Dispersion))
        {
            diagnostics.Add(new("insufficient_data", "A stage estimate is unavailable because its inputs are incomplete."));
            return StageForecast.Unavailable(diagnostics);
        }

        var standardDeviationSquared =
            userLevel.Variance +
            Math.Pow(parameters.Beta * stage.StandardError, 2) +
            Math.Pow(parameters.OwnVariability, 2);
        if (!double.IsFinite(standardDeviationSquared) || standardDeviationSquared < 0)
            return StageForecast.Unavailable(diagnostics.Append(
                new EngineDiagnostic("invalid_interval_variance", "The forecast interval variance is not finite and non-negative.")));

        var factor = (applyAfternoon ? parameters.AfternoonFactor : 1) *
                     (applyOneHand ? parameters.OneHandFactor : 1);
        var center = Math.Exp(userLevel.LogLevel + parameters.Beta * stage.LogDifficulty) * factor;
        var width = center * Math.Sinh(Math.Sqrt(standardDeviationSquared));
        var lower = center - width;
        var upper = center + width;
        if (!double.IsFinite(center) || center <= 0 || !double.IsFinite(lower) || !double.IsFinite(upper))
            return StageForecast.Unavailable(diagnostics.Append(
                new EngineDiagnostic("non_finite_forecast", "The forecast or uncertainty interval is not finite.")));

        return new StageForecast(true, center, Math.Max(0, lower), upper, Math.Sqrt(standardDeviationSquared), diagnostics);
    }

    public static decimal ErrorCostSeconds(
        decimal hitFactor,
        int minimumRounds,
        decimal performance,
        decimal lostPoints)
    {
        if (hitFactor <= 0 || minimumRounds <= 0 || performance <= 0 || lostPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(hitFactor), "HF, rounds, performance, and lost points must all be positive.");

        var maximumPoints = minimumRounds * 5m;
        var expectedPoints = performance * maximumPoints;
        var denominator = expectedPoints - lostPoints;
        if (denominator <= 0)
            throw new InvalidOperationException("Finite error equivalence is undefined because expected points do not exceed the lost points.");

        var exact = (lostPoints / hitFactor) * (expectedPoints / denominator);
        return decimal.Round(exact, 2, MidpointRounding.AwayFromZero);
    }

    public static string AllowedCharlieRange(decimal hitFactor, int minimumRounds)
    {
        if (hitFactor < 0 || minimumRounds <= 0)
            throw new ArgumentOutOfRangeException(nameof(hitFactor), "HF must be non-negative and minimum rounds must be positive.");

        var percentage = Math.Clamp(5m * hitFactor - 20m, 0m, 50m);
        var count = decimal.Round(minimumRounds * percentage / 100m, 1, MidpointRounding.AwayFromZero);
        var whole = (int)decimal.Floor(count);
        var tenths = (int)((count - whole) * 10m);
        return tenths switch
        {
            <= 3 => whole.ToString(System.Globalization.CultureInfo.InvariantCulture),
            <= 6 => $"{whole}–{whole + 1}",
            _ => (whole + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public static string RhythmSymbol(double hitFactor)
    {
        if (!double.IsFinite(hitFactor) || hitFactor < 0)
            throw new ArgumentOutOfRangeException(nameof(hitFactor));
        return hitFactor <= 4 ? "○" :
            hitFactor <= 6 ? "◔" :
            hitFactor <= 7.5 ? "◑" :
            hitFactor <= 8.5 ? "◕" :
            hitFactor <= 9.5 ? "●" : "◆";
    }

    public static string? StageGroup(int minimumRounds) =>
        minimumRounds switch
        {
            <= 0 => null,
            <= 12 => "short",
            <= 24 => "medium",
            _ => "long"
        };

    public static bool IsSupportedVersion(string? version) =>
        string.Equals(version, Version, StringComparison.Ordinal) ||
        string.Equals(version, LegacyVersion, StringComparison.Ordinal);

    public static IReadOnlyDictionary<int, int> CalculateTurnOrder(int stageCount, int squadSize, int startStage, int startTurn)
    {
        if (stageCount <= 0 || squadSize <= 0 || startStage < 1 || startStage > stageCount ||
            startTurn < 1 || startTurn > squadSize)
            throw new ArgumentOutOfRangeException(nameof(stageCount), "Stage count, squad size, start stage, and start turn must be valid.");

        return Enumerable.Range(1, stageCount).ToDictionary(
            stage => stage,
            stage =>
            {
                var offset = stage - startStage;
                var zeroBasedTurn = ((startTurn - 1 - offset) % squadSize + squadSize) % squadSize;
                return zeroBasedTurn + 1;
            });
    }

    public static VerificationSummary Verify(IEnumerable<VerificationStageInput> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var values = source.Where(x => double.IsFinite(x.ActualHitFactor) && x.ActualHitFactor > 0 &&
                                      double.IsFinite(x.Forecast.Center) && x.Forecast.Center > 0 &&
                                      x.Forecast.IsAvailable)
            .OrderBy(x => x.StageId, StringComparer.Ordinal)
            .ToArray();
        var diagnostics = new List<EngineDiagnostic>();
        if (values.Length == 0)
            return VerificationSummary.Unavailable(
                [new EngineDiagnostic("no_verifiable_stages", "No stages have both a valid actual HF and a frozen estimate.")]);

        var errors = values.Select(x => x.ActualHitFactor / x.Forecast.Center - 1).ToArray();
        var stageDifficulties = values.Select(x => x.FinalLogDifficulty).ToArray();
        var actualLogs = values.Select(x => Math.Log(x.ActualHitFactor)).ToArray();
        var beta = LinearRegressionSlope(stageDifficulties, actualLogs);
        if (!double.IsFinite(beta))
            diagnostics.Add(new("undefined_beta", "Observed beta is unavailable because stage difficulty has insufficient variance."));

        var covered = values.Count(x => x.ActualHitFactor >= x.Forecast.Lower && x.ActualHitFactor <= x.Forecast.Upper);
        return new VerificationSummary(
            true,
            values.Length,
            errors.Average(),
            SampleStandardDeviation(errors),
            covered,
            double.IsFinite(beta) ? beta : null,
            diagnostics);
    }

    private static double LinearRegressionSlope(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return double.NaN;
        var meanX = x.Average();
        var meanY = y.Average();
        var denominator = x.Sum(value => Math.Pow(value - meanX, 2));
        if (denominator <= 0 || !double.IsFinite(denominator))
            return double.NaN;
        return x.Select((value, index) => (value - meanX) * (y[index] - meanY)).Sum() / denominator;
    }

    private static double SampleStandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return double.NaN;
        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1);
        return double.IsFinite(variance) && variance >= 0 ? Math.Sqrt(variance) : double.NaN;
    }

    private sealed record LogObservation(string ShooterId, string StageId, double LogHitFactor);
}

public sealed record FreezerAnalysisParameters(
    string Version,
    double Beta,
    double DriftPercent,
    double OwnVariability,
    double DistancePenalty,
    double Performance,
    double AfternoonFactor,
    double OneHandFactor,
    bool DistancePenaltyConfirmed);

public sealed record ShooterStageScore(
    string ShooterId,
    string StageId,
    double HitFactor,
    bool IsAvailableBeforeStart = true,
    bool IsExcluded = false);

public sealed record ShooterProfile(double LogLevel, double StandardError, int ObservationCount);
public sealed record StageProfile(double LogDifficulty, double StandardError, double Dispersion, int ObservationCount);
public sealed record EngineDiagnostic(string Code, string Message);

public sealed record TwoWayFitResult(
    bool IsAvailable,
    string Version,
    string DispersionConvention,
    IReadOnlyDictionary<string, ShooterProfile> Shooters,
    IReadOnlyDictionary<string, StageProfile> Stages,
    IReadOnlyList<EngineDiagnostic> Diagnostics)
{
    public static TwoWayFitResult Unavailable(IEnumerable<EngineDiagnostic> diagnostics) =>
        new(false, FreezerAnalysisEngine.Version, FreezerAnalysisEngine.DispersionConvention,
            new Dictionary<string, ShooterProfile>(), new Dictionary<string, StageProfile>(), diagnostics.ToArray());
}

public sealed record HistoricalMatchResults(
    string MatchId,
    DateOnly LocalMatchDate,
    IReadOnlyList<HistoricalShooterResult> Results);

public sealed record HistoricalShooterResult(
    string IdentityId,
    string Division,
    IReadOnlyDictionary<string, double> StageHitFactors,
    IReadOnlySet<string> ExcludedStageIds,
    bool IsIdentityResolved = true);

public sealed record RegistryMatchEntry(
    string IdentityId,
    string Division,
    DateOnly LocalMatchDate,
    double LogScarto,
    double Dispersion,
    int StageCount,
    double Weight);

public sealed record RegistryShooterEntry(
    string IdentityId,
    string Division,
    double LogScarto,
    double Dispersion,
    double Variance,
    int StageCount,
    DateOnly LastMatchDate,
    bool IsInCandidatePool,
    int MatchCount)
{
    public double ScartoPercent => (Math.Exp(LogScarto) - 1) * 100;
    public double DispersionPercent => (Math.Exp(Dispersion) - 1) * 100;
}

public sealed record RegistryResult(
    IReadOnlyList<RegistryShooterEntry> Entries,
    IReadOnlyList<EngineDiagnostic> Diagnostics);

public sealed record InformantEstimateInput(
    string IdentityId,
    double ShooterLogLevel,
    double LogScarto,
    double RegistryStandardError);

public sealed record UserLevelEstimate(
    bool IsAvailable,
    double LogLevel,
    double Variance,
    int InformantCount,
    IReadOnlyList<EngineDiagnostic> Diagnostics)
{
    public static UserLevelEstimate Unavailable(IEnumerable<EngineDiagnostic> diagnostics) =>
        new(false, double.NaN, double.NaN, 0, diagnostics.ToArray());
}

public sealed record StageForecast(
    bool IsAvailable,
    double Center,
    double Lower,
    double Upper,
    double StandardDeviation,
    IReadOnlyList<EngineDiagnostic> Diagnostics)
{
    public static StageForecast Unavailable(IEnumerable<EngineDiagnostic> diagnostics) =>
        new(false, double.NaN, double.NaN, double.NaN, double.NaN, diagnostics.ToArray());
}

public sealed record VerificationStageInput(
    string StageId,
    double ActualHitFactor,
    double FinalLogDifficulty,
    StageForecast Forecast);

public sealed record VerificationSummary(
    bool IsAvailable,
    int StageCount,
    double MeanRelativeError,
    double ErrorSampleStandardDeviation,
    int IntervalCoverageCount,
    double? ObservedBeta,
    IReadOnlyList<EngineDiagnostic> Diagnostics)
{
    public static VerificationSummary Unavailable(IReadOnlyList<EngineDiagnostic> diagnostics) =>
        new(false, 0, double.NaN, double.NaN, 0, null, diagnostics);
}
