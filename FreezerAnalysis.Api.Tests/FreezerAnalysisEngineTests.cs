using MorWalPizVideo.Domain.FreezerAnalysis;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerAnalysisEngineTests
{
    [Fact]
    public void Two_way_log_fit_recovers_known_shooter_and_centered_stage_effects()
    {
        var levels = new Dictionary<string, double>
        {
            ["shooter-a"] = 1.1,
            ["shooter-b"] = 1.5,
            ["shooter-c"] = 0.7
        };
        var difficulties = new Dictionary<string, double>
        {
            ["stage-1"] = -0.2,
            ["stage-2"] = 0.1,
            ["stage-3"] = 0.1
        };
        var observations = levels.SelectMany(shooter =>
            difficulties.Select(stage => new ShooterStageScore(
                shooter.Key,
                stage.Key,
                Math.Exp(shooter.Value + stage.Value))));

        var fit = FreezerAnalysisEngine.FitStageProfiles(observations);

        Assert.True(fit.IsAvailable);
        Assert.Equal(FreezerAnalysisEngine.AlternatingMeanIterations, 200);
        foreach (var (stage, expected) in difficulties)
            Assert.InRange(Math.Abs(fit.Stages[stage].LogDifficulty - expected), 0, 1e-12);
        foreach (var (shooter, expected) in levels)
            Assert.InRange(Math.Abs(fit.Shooters[shooter].LogLevel - expected), 0, 1e-12);
    }

    [Fact]
    public void Fit_excludes_unavailable_excluded_and_non_positive_scores_with_diagnostics()
    {
        var input = new[]
        {
            new ShooterStageScore("a", "1", 1),
            new ShooterStageScore("b", "1", 2),
            new ShooterStageScore("a", "2", 0),
            new ShooterStageScore("a", "3", 3, IsAvailableBeforeStart: false),
            new ShooterStageScore("b", "4", 4, IsExcluded: true)
        };

        var fit = FreezerAnalysisEngine.FitStageProfiles(input);

        Assert.Single(fit.Stages);
        Assert.Contains(fit.Diagnostics, x => x.Code == "non_positive_hf");
        Assert.DoesNotContain(fit.Stages.Keys, x => x is "3" or "4");
    }

    [Fact]
    public void Registry_uses_only_strictly_earlier_matches_and_excludes_zero_variance_weights()
    {
        var scores = Enumerable.Range(1, 6).ToDictionary(x => $"s{x}", _ => 5d);
        var history = new[]
        {
            Match("earlier", new DateOnly(2026, 10, 28), scores),
            Match("same-day", new DateOnly(2026, 10, 29), scores)
        };

        var registry = FreezerAnalysisEngine.BuildRegistry(
            new DateOnly(2026, 10, 29),
            "S",
            "user",
            history);

        Assert.Empty(registry.Entries);
        Assert.Contains(registry.Diagnostics, x => x.Code == "zero_variance_pairing");
    }

    [Fact]
    public void Error_cost_matches_independent_long_and_short_examples()
    {
        Assert.Equal(2.22m, FreezerAnalysisEngine.ErrorCostSeconds(7.5m, 32, 0.935m, 15m));
        Assert.Equal(2.73m, FreezerAnalysisEngine.ErrorCostSeconds(7.5m, 12, 0.935m, 15m));
    }

    [Fact]
    public void Error_cost_reports_undefined_finite_equivalence()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            FreezerAnalysisEngine.ErrorCostSeconds(7.5m, 1, 0.935m, 15m));

        Assert.Contains("undefined", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Charlie_range_uses_half_up_tenths_and_clamps_to_zero_and_half()
    {
        Assert.Equal("4–5", FreezerAnalysisEngine.AllowedCharlieRange(6.84m, 32));
        Assert.Equal("0", FreezerAnalysisEngine.AllowedCharlieRange(0, 32));
        Assert.Equal("16", FreezerAnalysisEngine.AllowedCharlieRange(100, 32));
    }

    [Theory]
    [InlineData(5, "0")]
    [InlineData(7, "0–1")]
    [InlineData(13, "1")]
    public void Charlie_range_rounds_half_tenths_away_from_zero(int minimumRounds, string expected)
    {
        Assert.Equal(expected, FreezerAnalysisEngine.AllowedCharlieRange(5m, minimumRounds));
    }

    [Fact]
    public void Turn_order_wraps_stage_numbers_without_continuing_turn_sequence_past_last_stage()
    {
        var turns = FreezerAnalysisEngine.CalculateTurnOrder(12, 8, 7, 4);

        Assert.Equal(new[] { 4, 3, 2, 1, 8, 7 }, Enumerable.Range(7, 6).Select(stage => turns[stage]));
        Assert.Equal(new[] { 2, 1, 8, 7, 6, 5 }, Enumerable.Range(1, 6).Select(stage => turns[stage]));
    }

    [Fact]
    public void Forecast_returns_unavailable_for_non_finite_model_inputs()
    {
        var estimate = new UserLevelEstimate(true, 1, 0.1, 2, []);
        var stage = new StageProfile(0, double.NaN, 0.1, 10);

        var forecast = FreezerAnalysisEngine.ForecastStage(
            estimate,
            stage,
            FreezerAnalysisEngine.CurrentParameters);

        Assert.False(forecast.IsAvailable);
        Assert.Contains(forecast.Diagnostics, x => x.Code == "insufficient_data");
        Assert.DoesNotContain(forecast.Diagnostics, x => x.Message.Contains("NaN", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(4d, "○")]
    [InlineData(6d, "◔")]
    [InlineData(7.5d, "◑")]
    [InlineData(8.5d, "◕")]
    [InlineData(9.5d, "●")]
    [InlineData(9.5001d, "◆")]
    public void Rhythm_symbols_use_inclusive_documented_boundaries(double hitFactor, string expected) =>
        Assert.Equal(expected, FreezerAnalysisEngine.RhythmSymbol(hitFactor));

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "short")]
    [InlineData(12, "short")]
    [InlineData(13, "medium")]
    [InlineData(24, "medium")]
    [InlineData(25, "long")]
    public void Stage_groups_use_explicit_minimum_round_boundaries(int minimumRounds, string? expected) =>
        Assert.Equal(expected, FreezerAnalysisEngine.StageGroup(minimumRounds));

    [Theory]
    [InlineData("freezer-log-two-way-200-sample-v1", true)]
    [InlineData(FreezerAnalysisEngine.Version, true)]
    [InlineData("unsupported-engine", false)]
    [InlineData(null, false)]
    public void Engine_version_support_preserves_only_numerically_compatible_snapshots(
        string? version,
        bool expected) =>
        Assert.Equal(expected, FreezerAnalysisEngine.IsSupportedVersion(version));

    [Fact]
    public void User_level_combines_adjusted_informants_by_inverse_variance()
    {
        var parameters = FreezerAnalysisEngine.CurrentParameters with
        {
            DriftPercent = 0,
            DistancePenalty = 0
        };
        var estimate = FreezerAnalysisEngine.EstimateUserLevel(
        [
            new InformantEstimateInput("a", 0.4, 0.1, 0.2),
            new InformantEstimateInput("b", 0.8, 0.2, 0.4)
        ],
        parameters);

        Assert.True(estimate.IsAvailable);
        Assert.Equal(0.36, estimate.LogLevel, 12);
        Assert.Equal(0.032, estimate.Variance, 12);
        Assert.Equal(2, estimate.InformantCount);
    }

    [Fact]
    public void Forecast_adjustments_apply_exactly_once_to_center_and_interval()
    {
        var parameters = FreezerAnalysisEngine.CurrentParameters with
        {
            Beta = 1,
            OwnVariability = 0
        };
        var user = new UserLevelEstimate(true, 0, 0, 1, []);
        var stage = new StageProfile(0, 0, 0, 10);

        var baseEstimate = FreezerAnalysisEngine.ForecastStage(user, stage, parameters);
        var afternoon = FreezerAnalysisEngine.ForecastStage(user, stage, parameters, applyAfternoon: true);
        var oneHand = FreezerAnalysisEngine.ForecastStage(user, stage, parameters, applyOneHand: true);
        var combined = FreezerAnalysisEngine.ForecastStage(user, stage, parameters, applyAfternoon: true, applyOneHand: true);

        Assert.Equal(1, baseEstimate.Center);
        Assert.Equal(0.94, afternoon.Center, 12);
        Assert.Equal(0.89, oneHand.Center, 12);
        Assert.Equal(0.94 * 0.89, combined.Center, 12);
        Assert.Equal(combined.Center, combined.Lower);
        Assert.Equal(combined.Center, combined.Upper);
    }

    [Fact]
    public void Verification_reports_hand_calculated_relative_error_dispersion_coverage_and_beta()
    {
        var interval = new StageForecast(true, 10, 8, 12, 0, []);
        var summary = FreezerAnalysisEngine.Verify(
        [
            new VerificationStageInput("stage-1", 9, 0, interval),
            new VerificationStageInput("stage-2", 11, Math.Log(2), interval)
        ]);

        Assert.True(summary.IsAvailable);
        Assert.Equal(2, summary.StageCount);
        Assert.Equal(0, summary.MeanRelativeError, 12);
        Assert.Equal(Math.Sqrt(0.02), summary.ErrorSampleStandardDeviation, 12);
        Assert.Equal(2, summary.IntervalCoverageCount);
        Assert.NotNull(summary.ObservedBeta);
        Assert.Equal(Math.Log(11d / 9d) / Math.Log(2), summary.ObservedBeta.Value, 12);
    }

    [Fact]
    public void Registry_uses_only_latest_division_when_a_competitor_changes_division()
    {
        var scores = Enumerable.Range(1, 6).ToDictionary(index => $"s{index}", _ => 1d);
        var competitorScores = Enumerable.Range(1, 6)
            .ToDictionary(index => $"s{index}", index => 1 + index / 10d);
        var history = new[]
        {
            new HistoricalMatchResults("older", new DateOnly(2026, 10, 1),
            [
                new HistoricalShooterResult("user", "PROD", scores, new HashSet<string>()),
                new HistoricalShooterResult("competitor", "OPEN", competitorScores, new HashSet<string>())
            ]),
            new HistoricalMatchResults("newer", new DateOnly(2026, 10, 2),
            [
                new HistoricalShooterResult("user", "PROD", scores, new HashSet<string>()),
                new HistoricalShooterResult("competitor", "PROD", competitorScores, new HashSet<string>())
            ])
        };

        var registry = FreezerAnalysisEngine.BuildRegistry(
            new DateOnly(2026, 10, 3),
            "PROD",
            "user",
            history);

        var competitor = Assert.Single(registry.Entries);
        Assert.Equal("PROD", competitor.Division);
        Assert.Equal(new DateOnly(2026, 10, 2), competitor.LastMatchDate);
        Assert.Equal(1, competitor.MatchCount);
    }

    [Fact]
    public void Registry_requires_the_larger_of_five_or_user_stage_count_minus_three_overlaps()
    {
        var userScores = Enumerable.Range(1, 8).ToDictionary(index => $"s{index}", _ => 1d);
        var fiveStageOpponent = Enumerable.Range(1, 5)
            .ToDictionary(index => $"s{index}", index => 1 + index / 10d);
        var fourStageOpponent = Enumerable.Range(1, 4)
            .ToDictionary(index => $"s{index}", index => 1 + index / 10d);
        var match = new HistoricalMatchResults("overlap", new DateOnly(2026, 10, 1),
        [
            new HistoricalShooterResult("user", "PROD", userScores, new HashSet<string>()),
            new HistoricalShooterResult("five", "PROD", fiveStageOpponent, new HashSet<string>()),
            new HistoricalShooterResult("four", "PROD", fourStageOpponent, new HashSet<string>())
        ]);

        var registry = FreezerAnalysisEngine.BuildRegistry(
            new DateOnly(2026, 10, 2),
            "PROD",
            "user",
            [match]);

        var entry = Assert.Single(registry.Entries);
        Assert.Equal("five", entry.IdentityId);
        Assert.Equal(5, entry.StageCount);
        Assert.Contains(registry.Diagnostics, diagnostic => diagnostic.Code == "insufficient_overlap");
    }

    private static HistoricalMatchResults Match(
        string matchId,
        DateOnly date,
        IReadOnlyDictionary<string, double> scores) =>
        new(matchId, date,
        [
            new HistoricalShooterResult("user", "S", scores, new HashSet<string>()),
            new HistoricalShooterResult("competitor", "S", scores, new HashSet<string>())
        ]);
}
