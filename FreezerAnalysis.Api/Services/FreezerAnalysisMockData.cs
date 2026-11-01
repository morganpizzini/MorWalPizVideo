using MorWalPizVideo.Domain.FreezerAnalysis;
using MorWalPizVideo.Models.FreezerAnalysis;

namespace FreezerAnalysis.Api.Services;

internal static class FreezerAnalysisMockData
{
    private static readonly DateTime SeededAtUtc = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    public static void Seed(InMemoryFreezerAnalysisState state, string accountId)
    {
        const string selfIdentityId = "mock-identity-alessia";
        const string rivalIdentityId = "mock-identity-marco";
        const string secondRivalIdentityId = "mock-identity-chiara";

        state.Identities.TryAdd(selfIdentityId, Identity(
            selfIdentityId, accountId, "Alessia Bianchi", ["A. Bianchi"], "ITA-1001"));
        state.Identities.TryAdd(rivalIdentityId, Identity(
            rivalIdentityId, accountId, "Marco Rossi", ["M. Rossi"], "ITA-1002"));
        state.Identities.TryAdd(secondRivalIdentityId, Identity(
            secondRivalIdentityId, accountId, "Chiara Verdi", ["C. Verdi"], "ITA-1003"));

        state.Profiles.TryAdd(accountId, new FreezerProfile
        {
            Id = "mock-profile",
            AccountId = accountId,
            IdentityId = selfIdentityId,
            Division = "PRODUCTION",
            PowerFactor = "Minor",
            Performance = 0.94,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        });

        SeedHistoricalMatch(state, accountId, "mock-match-spring", "Trofeo Appennino", "2026-03-15", 1.02);
        SeedHistoricalMatch(state, accountId, "mock-match-summer", "Coppa dei Laghi", "2026-06-21", 1.08);
        SeedTargetMatch(state, accountId, selfIdentityId);
    }

    private static void SeedHistoricalMatch(
        InMemoryFreezerAnalysisState state,
        string accountId,
        string matchId,
        string name,
        string date,
        double selfBaseHitFactor)
    {
        var revisionId = $"{matchId}-revision";
        var stages = Stages(matchId);
        var results = new[]
        {
            Result($"{matchId}-alessia", matchId, "Alessia Bianchi", "mock-identity-alessia", stages, selfBaseHitFactor),
            Result($"{matchId}-marco", matchId, "Marco Rossi", "mock-identity-marco", stages, selfBaseHitFactor - 0.03),
            Result($"{matchId}-chiara", matchId, "Chiara Verdi", "mock-identity-chiara", stages, selfBaseHitFactor - 0.05)
        };

        PublishMatch(state, accountId, matchId, revisionId, name, date, stages, results);
        foreach (var result in results)
        {
            state.IdentityResolutions.TryAdd(
                $"{accountId}\0{matchId}\0{result.Id}",
                new FreezerIdentityResolution
                {
                    Id = $"{result.Id}-resolution",
                    AccountId = accountId,
                    MatchId = matchId,
                    ResultId = result.Id,
                    IdentityId = result.IdentityId!,
                    Revision = 1,
                    ResolvedAtUtc = SeededAtUtc
                });
        }
    }

    private static void SeedTargetMatch(InMemoryFreezerAnalysisState state, string accountId, string selfIdentityId)
    {
        const string matchId = "mock-match-target";
        const string revisionId = "mock-match-target-revision";
        var stages = Stages(matchId);
        var results = new[]
        {
            Result("mock-target-alessia", matchId, "Alessia Bianchi", null, stages, 1.16),
            Result("mock-target-marco", matchId, "Marco Rossi", null, stages, 0.98),
            Result("mock-target-unknown", matchId, "Luca Neri", null, stages, 0.87, FreezerResultStatus.Disqualified)
        };

        PublishMatch(state, accountId, matchId, revisionId, "Memoriale delle Colline", "2026-10-25", stages, results);
        state.Drafts.TryAdd("mock-target-draft", new FreezerAnalysisDraft
        {
            Id = "mock-target-draft",
            AccountId = accountId,
            MatchId = matchId,
            Revision = 0,
            IdentityId = selfIdentityId,
            Division = "PRODUCTION",
            ParameterVersion = FreezerAnalysisEngine.CurrentParameters.Version,
            Beta = FreezerAnalysisEngine.CurrentParameters.Beta,
            DriftPercent = FreezerAnalysisEngine.CurrentParameters.DriftPercent,
            OwnVariability = FreezerAnalysisEngine.CurrentParameters.OwnVariability,
            DistancePenalty = FreezerAnalysisEngine.CurrentParameters.DistancePenalty,
            Performance = 0.94,
            StrategyByStage = new Dictionary<string, string>
            {
                ["stage-1"] = "Conservativa",
                ["stage-2"] = "Standard",
                ["stage-3"] = "Aggressiva",
                ["stage-4"] = "Standard",
                ["stage-5"] = "Conservativa"
            },
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        });
    }

    private static void PublishMatch(
        InMemoryFreezerAnalysisState state,
        string accountId,
        string matchId,
        string revisionId,
        string name,
        string date,
        IReadOnlyList<FreezerMatchStage> stages,
        IReadOnlyList<FreezerCompetitorResult> results)
    {
        state.Matches[matchId] = new FreezerMatch
        {
            Id = matchId,
            MatchKey = matchId,
            Name = name,
            Location = "Italia",
            LocalMatchDate = date,
            TimeZoneId = "Europe/Rome",
            OwnerAccountId = accountId,
            ActiveRevisionId = revisionId,
            RevisionNumber = 1,
            CreatedAtUtc = SeededAtUtc
        };
        state.Revisions[revisionId] = new FreezerMatchRevision
        {
            Id = revisionId,
            MatchId = matchId,
            RevisionNumber = 1,
            SourceHash = $"{matchId}-source",
            SourceFormat = "archive-csv",
            ParserVersion = "mock-seed",
            Completeness = FreezerImportCompleteness.Final,
            Stages = stages,
            ResultIds = results.Select(result => result.Id).ToArray(),
            CreatedAtUtc = SeededAtUtc
        };
        foreach (var result in results)
            state.ResultsByHash.TryAdd(result.ContentHash, result);

        state.ImportsById[$"{matchId}-import"] = new FreezerImportManifest
        {
            Id = $"{matchId}-import",
            AccountId = accountId,
            MatchId = matchId,
            RevisionId = revisionId,
            SourceHash = $"{matchId}-source",
            Format = "archive-csv",
            Completeness = FreezerImportCompleteness.Final,
            State = FreezerImportState.Published,
            AvailableBeforeStartCompetitorIds = results.Select(result => result.CompetitorId).ToArray(),
            CreatedAtUtc = SeededAtUtc
        };
    }

    private static IReadOnlyList<FreezerMatchStage> Stages(string matchId) =>
    [
        new() { StageId = "stage-1", StageNumber = 1, Name = "Precisione", MinimumRounds = 12, MaximumPoints = 60 },
        new() { StageId = "stage-2", StageNumber = 2, Name = "Transizioni", MinimumRounds = 18, MaximumPoints = 90 },
        new() { StageId = "stage-3", StageNumber = 3, Name = "Finale", MinimumRounds = 24, MaximumPoints = 120 },
        new() { StageId = "stage-4", StageNumber = 4, Name = "Precisione dinamica", MinimumRounds = 18, MaximumPoints = 90 },
        new() { StageId = "stage-5", StageNumber = 5, Name = "Chiusura", MinimumRounds = 12, MaximumPoints = 60 }
    ];

    private static FreezerCompetitorResult Result(
        string id,
        string matchId,
        string displayName,
        string? identityId,
        IReadOnlyList<FreezerMatchStage> stages,
        double baseHitFactor,
        FreezerResultStatus status = FreezerResultStatus.Scored) => new()
    {
        Id = id,
        MatchId = matchId,
        CompetitorId = id,
        ContentHash = $"{id}-hash",
        IdentityId = identityId,
        IdentityResolved = identityId is not null,
        DisplayName = displayName,
        NormalizedName = displayName.ToUpperInvariant(),
        Division = "PRODUCTION",
        Class = "A",
        PowerFactor = "Minor",
        OfficialTotal = status == FreezerResultStatus.Scored ? baseHitFactor * 100 : 0,
        IsDisqualified = status == FreezerResultStatus.Disqualified,
        IsZeroed = status == FreezerResultStatus.Zeroed,
        Stages = stages.Select((stage, index) => new FreezerStageResult
        {
            StageId = stage.StageId,
            MinimumRounds = stage.MinimumRounds,
            HitFactor = status == FreezerResultStatus.Scored ? baseHitFactor - index * 0.06 : null,
            TimeSeconds = status == FreezerResultStatus.Scored ? stage.MinimumRounds / (baseHitFactor - index * 0.06) : null,
            NetPoints = status == FreezerResultStatus.Scored ? stage.MinimumRounds * 5 : null,
            ClassificationPoints = status == FreezerResultStatus.Scored ? baseHitFactor * 10 : null,
            Alpha = status == FreezerResultStatus.Scored ? stage.MinimumRounds : null,
            Status = status
        }).ToArray()
    };

    private static FreezerShooterIdentity Identity(
        string id,
        string accountId,
        string displayName,
        IReadOnlyList<string> aliases,
        string license) => new()
    {
        Id = id,
        NormalizedName = displayName.ToUpperInvariant(),
        Aliases = new[] { displayName }.Concat(aliases).ToArray(),
        NormalizedAliases = aliases.Select(alias => alias.ToUpperInvariant()).ToArray(),
        Federation = "FITDS",
        License = license,
        CreatedByAccountId = accountId,
        Revision = 1
    };
}