using System.Diagnostics;
using MorWalPizVideo.Domain.FreezerAnalysis;

namespace FreezerAnalysis.Api.Tests;

[CollectionDefinition("FreezerAnalysis performance", DisableParallelization = true)]
public sealed class FreezerAnalysisPerformanceCollection
{
}

[Collection("FreezerAnalysis performance")]
public sealed class FreezerAnalysisPerformanceTests
{
    [Fact]
    public void Fit_handles_a_500_competitor_34_stage_match_deterministically()
    {
        const int competitorCount = 500;
        const int stageCount = 34;
        var observations = Enumerable.Range(0, competitorCount)
            .SelectMany(competitor => Enumerable.Range(1, stageCount)
                .Select(stage =>
                {
                    var shooterLevel = 0.6 + competitor * 0.0002;
                    var stageDifficulty = (stage - 17.5) * 0.004;
                    return new ShooterStageScore(
                        $"shooter-{competitor:D4}",
                        $"stage-{stage:D2}",
                        Math.Exp(shooterLevel + stageDifficulty));
                }))
            .ToArray();

        _ = FreezerAnalysisEngine.FitStageProfiles(observations);
        var durations = new List<double>(20);
        TwoWayFitResult? result = null;
        for (var iteration = 0; iteration < 20; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            result = FreezerAnalysisEngine.FitStageProfiles(observations);
            stopwatch.Stop();
            durations.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        var orderedDurations = durations.Order().ToArray();
        var p95Index = (int)Math.Ceiling(orderedDurations.Length * 0.95) - 1;
        Console.WriteLine(
            $"Synthetic 500x34 engine fit: p95={orderedDurations[p95Index]:F2}ms, " +
            $"min={orderedDurations[0]:F2}ms, max={orderedDurations[^1]:F2}ms");

        Assert.NotNull(result);
        Assert.True(result.IsAvailable);
        Assert.Equal(competitorCount, result.Shooters.Count);
        Assert.Equal(stageCount, result.Stages.Count);
        Assert.Equal(competitorCount, result.Stages["stage-17"].ObservationCount);
        Assert.Equal(-0.002, result.Stages["stage-17"].LogDifficulty, 12);
        Assert.Equal(result.Stages["stage-01"].LogDifficulty, result.Stages["stage-34"].LogDifficulty * -1, 12);
    }
}
