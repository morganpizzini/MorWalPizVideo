using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MorWalPiz.Contracts.Contracts.FreezerAnalysis;
using MorWalPizVideo.Domain.FreezerAnalysis;
using MorWalPizVideo.Models.FreezerAnalysis;
using MongoDB.Bson;

namespace FreezerAnalysis.Api.Services;

public sealed class FreezerAnalysisCoordinator(
    IFreezerAnalysisRepository repository,
    IFreezerPrivateObjectStorage objectStorage,
    FreezerCsvParser csvParser,
    FreezerDocumentRenderer documentRenderer)
{
    public async Task<FreezerProfileResponse> GetProfileAsync(string accountId, CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(accountId, cancellationToken);
        return profile is null
            ? new FreezerProfileResponse(accountId, null, null, null, FreezerAnalysisEngine.CurrentParameters.Performance, DateTime.UnixEpoch)
            : ToResponse(profile);
    }

    public async Task<FreezerProfileResponse> SaveProfileAsync(
        string accountId,
        FreezerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var identityId = NormalizeOptional(request.IdentityId);
        if (identityId is not null &&
            await repository.GetIdentityAsync(accountId, identityId, cancellationToken) is null)
            throw new ArgumentException("The selected shooter identity does not belong to this account.");

        var now = DateTime.UtcNow;
        var current = await repository.GetProfileAsync(accountId, cancellationToken);
        var profile = new FreezerProfile
        {
            Id = current?.Id ?? ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            IdentityId = identityId,
            Division = NormalizeOptional(request.Division)?.ToUpperInvariant(),
            PowerFactor = NormalizeOptional(request.PowerFactor),
            Performance = request.Performance,
            CreatedAtUtc = current?.CreatedAtUtc ?? now,
            UpdatedAtUtc = now
        };
        await repository.SaveProfileAsync(profile, cancellationToken);
        return ToResponse(profile);
    }

    public async Task<FreezerIdentityResponse> CreateIdentityAsync(
        string accountId,
        FreezerIdentityCreateRequest request,
        CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        var normalizedName = FreezerIdentityNormalizer.NormalizeName(displayName);
        if (normalizedName.Length == 0)
            throw new ArgumentException("The display name must contain at least one letter or number.");
        var suppliedAliases = request.Aliases ?? [];
        if (suppliedAliases.Count > 20)
            throw new ArgumentException("An identity can have at most 20 aliases.");
        if (suppliedAliases.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Every alias must contain at least one letter or number.");
        var aliasValues = suppliedAliases.Select(x => x.Trim()).ToArray();
        if (aliasValues.Any(x => x.Length > 100))
            throw new ArgumentException("Each alias must be at most 100 characters.");
        var normalizedAliases = aliasValues
            .Select(FreezerIdentityNormalizer.NormalizeName)
            .ToArray();
        if (normalizedAliases.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Every alias must contain at least one letter or number.");
        var uniqueAliases = new List<string> { displayName };
        var seenAliases = new HashSet<string>(StringComparer.Ordinal) { normalizedName };
        for (var index = 0; index < aliasValues.Length; index++)
        {
            if (seenAliases.Add(normalizedAliases[index]))
                uniqueAliases.Add(aliasValues[index]);
        }

        var identity = await repository.CreateIdentityAsync(new FreezerShooterIdentity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            NormalizedName = normalizedName,
            Aliases = uniqueAliases,
            NormalizedAliases = seenAliases.Where(x => x != normalizedName).Order(StringComparer.Ordinal).ToArray(),
            CreatedByAccountId = accountId,
            Revision = 1
        }, cancellationToken);
        return ToResponse(identity);
    }

    public async Task<FreezerIdentityResponse?> GetIdentityAsync(
        string accountId,
        string identityId,
        CancellationToken cancellationToken)
    {
        var identity = await repository.GetIdentityAsync(accountId, identityId, cancellationToken);
        return identity is null ? null : ToResponse(identity);
    }

    public async Task<IReadOnlyList<FreezerIdentityResponse>> ListIdentitiesAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        var identities = await repository.ListIdentitiesAsync(accountId, cancellationToken);
        return identities.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<FreezerIdentityReviewEntryResponse>> GetIdentityReviewAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken)
    {
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        if (match?.ActiveRevisionId is null)
            throw new KeyNotFoundException("The published match was not found.");
        var revision = await repository.GetRevisionAsync(matchId, match.ActiveRevisionId, cancellationToken);
        if (revision is null)
            throw new InvalidDataException("The active match revision is missing.");

        var results = await repository.GetResultsAsync(revision.ResultIds, cancellationToken);
        var resolutions = await repository.ListIdentityResolutionsAsync(accountId, matchId, cancellationToken);
        var resolutionByResultId = resolutions.ToDictionary(x => x.ResultId, StringComparer.Ordinal);
        var candidates = await repository.FindIdentityCandidatesAsync(
            accountId,
            results.Select(x => x.NormalizedName).Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal),
            cancellationToken);
        var candidatesByName = candidates
            .GroupBy(x => x.NormalizedName, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Select(ToCandidateResponse).ToArray(), StringComparer.Ordinal);

        return results.OrderBy(x => x.DisplayName, StringComparer.Ordinal)
            .ThenBy(x => x.CompetitorId, StringComparer.Ordinal)
            .Select(result =>
            {
                var resolution = resolutionByResultId.GetValueOrDefault(result.Id);
                var resultCandidates = candidatesByName.GetValueOrDefault(result.NormalizedName) ?? [];
                return new FreezerIdentityReviewEntryResponse(
                    result.Id,
                    result.CompetitorId,
                    result.DisplayName,
                    result.Division,
                    resolution?.IdentityId,
                    resolution?.Revision ?? 0,
                    resultCandidates);
            })
            .ToArray();
    }

    public async Task<FreezerIdentityResolutionResponse?> ResolveIdentityAsync(
        string accountId,
        string matchId,
        string resultId,
        FreezerIdentityResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        if (match?.ActiveRevisionId is null)
            throw new KeyNotFoundException("The published match was not found.");
        var revision = await repository.GetRevisionAsync(matchId, match.ActiveRevisionId, cancellationToken);
        if (revision is null)
            throw new InvalidDataException("The active match revision is missing.");
        if (!revision.ResultIds.Contains(resultId, StringComparer.Ordinal))
            throw new KeyNotFoundException("The competitor result was not found in the active match revision.");
        if (await repository.GetIdentityAsync(accountId, request.IdentityId, cancellationToken) is null)
            throw new ArgumentException("The selected shooter identity does not belong to this account.");

        var current = await repository.GetIdentityResolutionAsync(accountId, matchId, resultId, cancellationToken);
        if (current?.IdentityId == request.IdentityId && current.Revision == request.ExpectedRevision)
            return ToResponse(current);

        var resolution = await repository.SaveIdentityResolutionAsync(new FreezerIdentityResolution
        {
            Id = current?.Id ?? ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            MatchId = matchId,
            ResultId = resultId,
            IdentityId = request.IdentityId,
            ResolvedAtUtc = DateTime.UtcNow
        }, request.ExpectedRevision, cancellationToken);
        return resolution is null ? null : ToResponse(resolution);
    }

    public async Task<bool> RemoveIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        if (match?.ActiveRevisionId is null)
            throw new KeyNotFoundException("The published match was not found.");
        var revision = await repository.GetRevisionAsync(matchId, match.ActiveRevisionId, cancellationToken);
        if (revision is null)
            throw new InvalidDataException("The active match revision is missing.");
        if (!revision.ResultIds.Contains(resultId, StringComparer.Ordinal))
            throw new KeyNotFoundException("The competitor result was not found in the active match revision.");

        return await repository.DeleteIdentityResolutionAsync(
            accountId,
            matchId,
            resultId,
            expectedRevision,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FreezerMatchResponse>> ListMatchesAsync(
        string accountId,
        int limit,
        CancellationToken cancellationToken)
    {
        var matches = await repository.ListPublishedMatchesAsync(Math.Clamp(limit, 1, 100), cancellationToken);
        var response = new List<FreezerMatchResponse>(matches.Count);
        foreach (var match in matches)
        {
            var item = await ToMatchResponseAsync(accountId, match, cancellationToken);
            if (item is not null)
                response.Add(item);
        }
        return response;
    }

    public async Task<FreezerMatchResponse?> GetMatchSummaryAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken)
    {
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        return match is null
            ? null
            : await ToMatchResponseAsync(accountId, match, cancellationToken);
    }

    private async Task<FreezerMatchResponse?> ToMatchResponseAsync(
        string accountId,
        FreezerMatch match,
        CancellationToken cancellationToken)
    {
        if (match.ActiveRevisionId is null)
            return null;

        var revision = await repository.GetRevisionAsync(match.Id, match.ActiveRevisionId, cancellationToken);
        if (revision is null)
            return null;

        var results = await repository.GetResultsAsync(revision.ResultIds, cancellationToken);
        var isFinal = revision.Completeness == FreezerImportCompleteness.Final;
        var canVerify = isFinal &&
            await repository.GetPublishedImportForRevisionAsync(accountId, revision.Id, cancellationToken) is not null;
        return new FreezerMatchResponse(
            match.Id,
            match.Name,
            match.Location,
            DateOnly.ParseExact(match.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            match.TimeZoneId,
            match.RevisionNumber,
            isFinal,
            revision.Stages.Count,
            results.Count,
            revision.Id,
            canVerify);
    }

    public async Task<FreezerImportPreviewResponse> PreviewScoresAsync(
        ReadOnlyMemory<byte> source,
        string matchName,
        DateOnly matchDate,
        CancellationToken cancellationToken)
    {
        var parsed = csvParser.ParseScores(source, matchName, matchDate, cancellationToken);
        var existingMatch = await repository.FindMatchByKeyAsync(parsed.MatchKey, cancellationToken);
        return new FreezerImportPreviewResponse(
            parsed.SourceHash,
            parsed.MatchName,
            parsed.LocalMatchDate,
            "Europe/Rome",
            "archive-csv",
            parsed.Completeness.ToString(),
            parsed.Stages.Count,
            parsed.Results.Count,
            parsed.Results.Count(x => !x.IdentityResolved),
            0,
            parsed.Diagnostics,
            true,
            existingMatch?.RevisionNumber ?? 0);
    }

    public async Task<FreezerImportPreviewResponse> PublishScoresAsync(
        string accountId,
        ReadOnlyMemory<byte> source,
        string matchName,
        DateOnly matchDate,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        var parsed = csvParser.ParseScores(source, matchName, matchDate, cancellationToken);
        var storedSource = await objectStorage.PutAsync(
            "source",
            parsed.SourceHash,
            source,
            "text/csv; charset=utf-8",
            cancellationToken);

        var now = DateTime.UtcNow;
        var existingMatch = await repository.FindMatchByKeyAsync(parsed.MatchKey, cancellationToken);
        var matchId = existingMatch?.Id ?? ObjectId.GenerateNewId().ToString();
        var match = existingMatch ?? new FreezerMatch
        {
            Id = matchId,
            MatchKey = parsed.MatchKey,
            Name = parsed.MatchName,
            LocalMatchDate = parsed.LocalMatchDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeZoneId = "Europe/Rome",
            OwnerAccountId = accountId,
            RevisionNumber = 0,
            CreatedAtUtc = now
        };
        var results = parsed.Results.Select(result => result with
        {
            MatchId = matchId,
            ContentHash = HashForMatch(parsed.MatchKey, result.ContentHash)
        }).ToArray();
        var revision = new FreezerMatchRevision
        {
            Id = ObjectId.GenerateNewId().ToString(),
            MatchId = matchId,
            RevisionNumber = expectedRevision + 1,
            SourceHash = parsed.SourceHash,
            SourceFormat = "archive-csv",
            ParserVersion = FreezerCsvParser.Version,
            Completeness = parsed.Completeness,
            Stages = parsed.Stages,
            ResultIds = results.Select(x => x.Id).ToArray(),
            CreatedAtUtc = now
        };
        var manifest = new FreezerImportManifest
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            MatchId = matchId,
            SourceHash = parsed.SourceHash,
            SourceObjectKey = storedSource.Key,
            Format = "archive-csv",
            Completeness = parsed.Completeness,
            State = FreezerImportState.Staged,
            AvailableBeforeStartCompetitorIds = parsed.AvailableBeforeStartCompetitorIds,
            ExcludedStageIds = parsed.ExcludedStageIds,
            Diagnostics = parsed.Diagnostics,
            CreatedAtUtc = now
        };

        var publication = await repository.PublishImportAsync(
            match,
            revision,
            results,
            manifest,
            expectedRevision,
            cancellationToken);
        var publishedMatch = publication.Match;
        return new FreezerImportPreviewResponse(
            publication.Manifest.Id,
            publishedMatch?.Name ?? parsed.MatchName,
            parsed.LocalMatchDate,
            "Europe/Rome",
            "archive-csv",
            publication.Manifest.Completeness.ToString(),
            parsed.Stages.Count,
            parsed.Results.Count,
            parsed.Results.Count(x => !x.IdentityResolved),
            parsed.Results.Count(x => x.IdentityResolved),
            publication.Manifest.Diagnostics,
            !publication.IsConflict,
            publishedMatch?.RevisionNumber ?? expectedRevision);
    }

    public async Task<FreezerDraftResponse> GetDraftAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken)
    {
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        if (match is null)
            throw new KeyNotFoundException("The published match was not found.");
        var draft = await repository.GetDraftAsync(accountId, matchId, cancellationToken);
        if (draft is null)
        {
            var now = DateTime.UtcNow;
            var adoption = await repository.GetLatestParameterAdoptionAsync(accountId, cancellationToken);
            var parameters = adoption?.Parameters;
            draft = await repository.CreateDraftAsync(new FreezerAnalysisDraft
            {
                Id = ObjectId.GenerateNewId().ToString(),
                AccountId = accountId,
                MatchId = matchId,
                Revision = 0,
                ParameterVersion = adoption?.ParameterVersion ?? FreezerAnalysisEngine.CurrentParameters.Version,
                Beta = parameters?.Beta ?? FreezerAnalysisEngine.CurrentParameters.Beta,
                DriftPercent = parameters?.DriftPercent ?? FreezerAnalysisEngine.CurrentParameters.DriftPercent,
                OwnVariability = parameters?.OwnVariability ?? FreezerAnalysisEngine.CurrentParameters.OwnVariability,
                DistancePenalty = parameters?.DistancePenalty ?? FreezerAnalysisEngine.CurrentParameters.DistancePenalty,
                Performance = FreezerAnalysisEngine.CurrentParameters.Performance,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }, cancellationToken);
        }
        return ToResponse(draft);
    }

    public async Task<FreezerDraftResponse?> SaveDraftAsync(
        string accountId,
        string matchId,
        FreezerDraftRequest request,
        CancellationToken cancellationToken)
    {
        var current = await repository.GetDraftAsync(accountId, matchId, cancellationToken);
        if (current is null)
        {
            await GetDraftAsync(accountId, matchId, cancellationToken);
            current = await repository.GetDraftAsync(accountId, matchId, cancellationToken);
        }
        if (current is null || current.StartedAtUtc.HasValue)
            return null;
        var identityId = NormalizeOptional(request.IdentityId);
        if (identityId is not null &&
            await repository.GetIdentityAsync(accountId, identityId, cancellationToken) is null)
            throw new ArgumentException("The selected shooter identity does not belong to this account.");
        var savedParameters = new FreezerParameterValues
        {
            Beta = request.Beta,
            DriftPercent = request.DriftPercent,
            OwnVariability = request.OwnVariability,
            DistancePenalty = request.DistancePenalty
        };
        var currentParameters = new FreezerParameterValues
        {
            Beta = current.Beta,
            DriftPercent = current.DriftPercent,
            OwnVariability = current.OwnVariability,
            DistancePenalty = current.DistancePenalty
        };
        var updated = current with
        {
            Revision = current.Revision + 1,
            ImportManifestId = NormalizeOptional(request.ImportManifestId),
            IdentityId = identityId,
            Division = NormalizeOptional(request.Division)?.ToUpperInvariant(),
            ParameterVersion = savedParameters == currentParameters
                ? current.ParameterVersion
                : ParameterVersion(savedParameters),
            Beta = request.Beta,
            DriftPercent = request.DriftPercent,
            OwnVariability = request.OwnVariability,
            DistancePenalty = request.DistancePenalty,
            Performance = request.Performance,
            ApplyAfternoon = request.ApplyAfternoon,
            ApplyOneHand = request.ApplyOneHand,
            ExcludedStageIds = request.ExcludedStageIds.Distinct(StringComparer.Ordinal).ToArray(),
            RivalIdentityIds = request.RivalIdentityIds.Distinct(StringComparer.Ordinal).ToArray(),
            StrategyByStage = ValidateStrategies(request.StrategyByStage),
            UpdatedAtUtc = DateTime.UtcNow
        };
        var saved = await repository.UpdateDraftAsync(updated, request.ExpectedRevision, cancellationToken);
        return saved is null ? null : ToResponse(saved);
    }

    public async Task<FreezerEstimateResponse> EstimateAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken) =>
        (await ComposeEstimateAsync(accountId, matchId, cancellationToken)).Estimate;

    private async Task<EstimateComposition> ComposeEstimateAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken)
    {
        var draft = await repository.GetDraftAsync(accountId, matchId, cancellationToken);
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        if (draft is null || match?.ActiveRevisionId is null)
            throw new KeyNotFoundException("The draft or its published match was not found.");
        var revision = await repository.GetRevisionAsync(matchId, match.ActiveRevisionId, cancellationToken);
        if (revision is null)
            throw new InvalidDataException("The active match revision is missing.");

        var targetMatchDate = DateOnly.ParseExact(match.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var priorMatches = await repository.ListPublishedMatchesBeforeAsync(targetMatchDate, 1_001, cancellationToken);
        var history = new List<HistoricalMatchResults>();
        var stageScores = new List<ShooterStageScore>();
        var historicalInputs = new List<HistoricalInputProvenance>();
        var diagnostics = new List<string>();
        if (priorMatches.Count > 1_000)
            diagnostics.Add("history_limit_reached: only the 1,000 most recent earlier matches are included.");
        priorMatches = priorMatches.Take(1_000).ToArray();
        var missingAvailabilitySnapshots = 0;
        var unresolvedResults = 0;

        foreach (var historicalMatch in priorMatches
                     .OrderBy(x => x.LocalMatchDate, StringComparer.Ordinal)
                     .ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (historicalMatch.ActiveRevisionId is null)
                continue;
            var historicalRevision = await repository.GetRevisionAsync(
                historicalMatch.Id,
                historicalMatch.ActiveRevisionId,
                cancellationToken);
            if (historicalRevision is null)
            {
                diagnostics.Add($"history_revision_missing: published match {historicalMatch.Id} has no active revision document.");
                continue;
            }

            var import = await repository.GetPublishedImportForRevisionAsync(
                accountId,
                historicalRevision.Id,
                cancellationToken);
            if (import is null)
            {
                missingAvailabilitySnapshots++;
                historicalInputs.Add(new HistoricalInputProvenance(
                    historicalMatch.Id,
                    historicalMatch.LocalMatchDate,
                    historicalRevision.Id,
                    historicalRevision.RevisionNumber,
                    historicalRevision.SourceHash,
                    historicalRevision.ParserVersion,
                    historicalRevision.Completeness,
                    null,
                    false,
                    "availability_manifest_missing",
                    [],
                    [],
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    []));
                continue;
            }
            if (historicalRevision.Completeness == FreezerImportCompleteness.Provisional)
                diagnostics.Add($"provisional_history: match {historicalMatch.Id} contains a provisional import.");

            var results = await repository.GetResultsAsync(historicalRevision.ResultIds, cancellationToken);
            var resolutions = await repository.ListIdentityResolutionsAsync(accountId, historicalMatch.Id, cancellationToken);
            var identityByResultId = resolutions
                .OrderBy(x => x.ResultId, StringComparer.Ordinal)
                .ToDictionary(x => x.ResultId, x => x.IdentityId, StringComparer.Ordinal);
            var availableCompetitorIds = import.AvailableBeforeStartCompetitorIds.ToHashSet(StringComparer.Ordinal);
            var excludedStageIds = import.ExcludedStageIds.ToHashSet(StringComparer.Ordinal);
            var historicalShooters = new List<HistoricalShooterResult>();

            foreach (var result in results.OrderBy(x => x.CompetitorId, StringComparer.Ordinal))
            {
                if (!identityByResultId.TryGetValue(result.Id, out var identityId))
                {
                    unresolvedResults++;
                    continue;
                }

                var availableBeforeStart = availableCompetitorIds.Contains(result.CompetitorId);
                var stageHitFactors = result.Stages
                    .Where(stage => stage.Status == FreezerResultStatus.Scored &&
                                    stage.HitFactor is { } hitFactor &&
                                    double.IsFinite(hitFactor) &&
                                    hitFactor > 0)
                    .OrderBy(stage => stage.StageId, StringComparer.Ordinal)
                    .ToDictionary(stage => stage.StageId, stage => stage.HitFactor!.Value, StringComparer.Ordinal);
                var isTargetShooter = identityId == draft.IdentityId;
                foreach (var stage in result.Stages.OrderBy(stage => stage.StageId, StringComparer.Ordinal))
                {
                    if (stage.Status != FreezerResultStatus.Scored ||
                        stage.HitFactor is not { } hitFactor ||
                        !double.IsFinite(hitFactor) ||
                        hitFactor <= 0)
                        continue;
                    stageScores.Add(new ShooterStageScore(
                        identityId,
                        stage.StageId,
                        hitFactor,
                        availableBeforeStart,
                        isTargetShooter && excludedStageIds.Contains(stage.StageId)));
                }

                if (!availableBeforeStart)
                    continue;
                historicalShooters.Add(new HistoricalShooterResult(
                    identityId,
                    result.Division,
                    stageHitFactors,
                    isTargetShooter ? excludedStageIds : new HashSet<string>(StringComparer.Ordinal)));
            }

            historicalInputs.Add(new HistoricalInputProvenance(
                historicalMatch.Id,
                historicalMatch.LocalMatchDate,
                historicalRevision.Id,
                historicalRevision.RevisionNumber,
                historicalRevision.SourceHash,
                historicalRevision.ParserVersion,
                historicalRevision.Completeness,
                import.Id,
                true,
                null,
                import.AvailableBeforeStartCompetitorIds.Order(StringComparer.Ordinal).ToArray(),
                import.ExcludedStageIds.Order(StringComparer.Ordinal).ToArray(),
                identityByResultId,
                results.OrderBy(x => x.CompetitorId, StringComparer.Ordinal)
                    .Select(result => new HistoricalResultProjection(
                        result.Id,
                        result.CompetitorId,
                        result.ContentHash,
                        identityByResultId.GetValueOrDefault(result.Id),
                        result.Division,
                        result.IsDisqualified,
                        result.IsZeroed,
                        result.Stages.OrderBy(stage => stage.StageId, StringComparer.Ordinal)
                            .Select(stage => new HistoricalStageProjection(
                                stage.StageId,
                                stage.Status,
                                stage.MinimumRounds,
                                stage.HitFactor,
                                stage.TimeSeconds,
                                stage.NetPoints,
                                stage.ClassificationPoints,
                                stage.Alpha,
                                stage.Charlie,
                                stage.Delta,
                                stage.Miss,
                                stage.NoShoot,
                                stage.Procedural))
                            .ToArray()))
                    .ToArray()));

            if (historicalShooters.Count > 0)
            {
                history.Add(new HistoricalMatchResults(
                    historicalMatch.Id,
                    DateOnly.ParseExact(historicalMatch.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    historicalShooters));
            }
        }

        if (missingAvailabilitySnapshots > 0)
            diagnostics.Add($"history_availability_missing: {missingAvailabilitySnapshots} prior shared match(es) have no availability snapshot owned by this account and were excluded.");
        if (unresolvedResults > 0)
            diagnostics.Add($"history_identity_unresolved: {unresolvedResults} historical competitor result(s) lack an explicit account identity mapping.");

        var forecastParameters = AnalysisParameters(draft);
        var response = CalculateEstimate(
            targetMatchDate,
            draft.IdentityId,
            draft.Division,
            history,
            stageScores,
            revision.Stages,
            forecastParameters,
            draft.ApplyAfternoon,
            draft.ApplyOneHand,
            diagnostics);
        return new EstimateComposition(response, history, stageScores, historicalInputs);
    }

    private static FreezerEstimateResponse CalculateEstimate(
        DateOnly targetMatchDate,
        string? targetIdentityId,
        string? division,
        IReadOnlyList<HistoricalMatchResults> history,
        IReadOnlyList<ShooterStageScore> stageScores,
        IReadOnlyList<FreezerMatchStage> stages,
        FreezerAnalysisParameters forecastParameters,
        bool applyAfternoon,
        bool applyOneHand,
        ICollection<string> diagnostics)
    {
        var fit = FreezerAnalysisEngine.FitStageProfiles(stageScores);
        foreach (var diagnostic in fit.Diagnostics)
            diagnostics.Add(FormatDiagnostic(diagnostic));
        if (string.IsNullOrWhiteSpace(targetIdentityId))
            diagnostics.Add("identity_unselected: choose an account-owned shooter identity before estimating.");
        if (string.IsNullOrWhiteSpace(division))
            diagnostics.Add("division_unselected: select the target match division before estimating.");

        UserLevelEstimate userLevel;
        if (string.IsNullOrWhiteSpace(targetIdentityId) || string.IsNullOrWhiteSpace(division))
        {
            userLevel = UserLevelEstimate.Unavailable(
                [new EngineDiagnostic("identity_or_division_unselected", "An account identity and target division are required.")]);
        }
        else
        {
            var registry = FreezerAnalysisEngine.BuildRegistry(targetMatchDate, division, targetIdentityId, history);
            foreach (var diagnostic in registry.Diagnostics)
                diagnostics.Add(FormatDiagnostic(diagnostic));
            var informants = registry.Entries
                .Where(entry => entry.IsInCandidatePool &&
                                string.Equals(entry.Division, division, StringComparison.OrdinalIgnoreCase) &&
                                fit.Shooters.TryGetValue(entry.IdentityId, out _))
                .Select(entry =>
                {
                    var shooter = fit.Shooters[entry.IdentityId];
                    return new InformantEstimateInput(
                        entry.IdentityId,
                        shooter.LogLevel,
                        entry.LogScarto,
                        Math.Sqrt(entry.Variance));
                })
                .ToArray();
            userLevel = FreezerAnalysisEngine.EstimateUserLevel(informants, forecastParameters);
            foreach (var diagnostic in userLevel.Diagnostics)
                diagnostics.Add(FormatDiagnostic(diagnostic));
        }

        var stageEstimates = stages.OrderBy(stage => stage.StageNumber).Select(stage =>
        {
            if (!fit.Stages.TryGetValue(stage.StageId, out var stageProfile))
            {
                var profileDiagnostics = new[]
                {
                    "stage_profile_unavailable: no valid, available historical hit factors were found for this stage."
                };
                return new FreezerStageEstimateResponse(
                    stage.StageId,
                    stage.StageNumber,
                    stage.MinimumRounds,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    profileDiagnostics);
            }

            var forecast = FreezerAnalysisEngine.ForecastStage(
                userLevel,
                stageProfile,
                forecastParameters,
                applyAfternoon,
                applyOneHand);
            var forecastDiagnostics = forecast.Diagnostics.Select(FormatDiagnostic).ToArray();
            return new FreezerStageEstimateResponse(
                stage.StageId,
                stage.StageNumber,
                stage.MinimumRounds,
                forecast.IsAvailable ? FreezerAnalysisEngine.RhythmSymbol(forecast.Center) : null,
                forecast.IsAvailable ? forecast.Center : null,
                forecast.IsAvailable ? forecast.Lower : null,
                forecast.IsAvailable ? forecast.Upper : null,
                null,
                null,
                forecastDiagnostics);
        }).ToArray();
        var isAvailable = stageEstimates.Length > 0 && stageEstimates.All(stage => stage.Center.HasValue);
        if (!isAvailable && diagnostics.Count == 0)
            diagnostics.Add("estimate_unavailable: one or more required historical inputs are missing.");

        return new FreezerEstimateResponse(
            FreezerAnalysisEngine.Version,
            FreezerAnalysisEngine.DispersionConvention,
            isAvailable,
            stageEstimates,
            diagnostics.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static FreezerAnalysisParameters AnalysisParameters(FreezerAnalysisDraft draft) =>
        FreezerAnalysisEngine.CurrentParameters with
        {
            Version = draft.ParameterVersion,
            Beta = draft.Beta,
            DriftPercent = draft.DriftPercent,
            OwnVariability = draft.OwnVariability,
            DistancePenalty = draft.DistancePenalty,
            Performance = draft.Performance
        };

    private static FreezerSnapshotReplayResponse ReplayUnavailable(
        FreezerFrozenSnapshot snapshot,
        string diagnostic) =>
        new(snapshot.Id, snapshot.EngineVersion, false, false, [], [diagnostic]);

    public async Task<FreezerSnapshotResponse?> FreezeAsync(
        string accountId,
        string matchId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var draft = await repository.GetDraftAsync(accountId, matchId, cancellationToken);
        var match = await repository.GetMatchAsync(matchId, cancellationToken);
        if (draft is null || match?.ActiveRevisionId is null)
            throw new KeyNotFoundException("The draft or its published match was not found.");
        var revision = await repository.GetRevisionAsync(matchId, match.ActiveRevisionId, cancellationToken);
        if (revision is null)
            throw new InvalidDataException("The active match revision is missing.");

        var import = draft.ImportManifestId is null
            ? null
            : await repository.GetImportAsync(accountId, draft.ImportManifestId, cancellationToken);
        if (draft.ImportManifestId is not null &&
            (import is null || import.MatchId != matchId || import.State != FreezerImportState.Published))
            throw new InvalidDataException("The draft import is missing, belongs to another match, or is not published.");

        var composition = await ComposeEstimateAsync(accountId, matchId, cancellationToken);
        var estimate = composition.Estimate;
        var appliedParameters = AnalysisParameters(draft);
        var forecasts = estimate.Stages.Select(stage => new FreezerStageForecast
        {
            StageId = stage.StageId,
            Center = stage.Center,
            Lower = stage.Lower,
            Upper = stage.Upper,
            Diagnostics = stage.Diagnostics
        }).ToArray();
        var package = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 2,
            target = new
            {
                matchId = match.Id,
                match.MatchKey,
                match.LocalMatchDate,
                match.TimeZoneId,
                revisionId = revision.Id,
                revision.RevisionNumber,
                stages = revision.Stages
                    .OrderBy(stage => stage.StageNumber)
                    .Select(stage => new
                    {
                        stage.StageId,
                        stage.StageNumber,
                        stage.MinimumRounds,
                        stage.MaximumPoints
                    })
                    .ToArray()
            },
            draft = new
            {
                draft.Id,
                draft.Revision,
                draft.IdentityId,
                draft.Division,
                draft.ParameterVersion,
                draft.Beta,
                draft.DriftPercent,
                draft.OwnVariability,
                draft.DistancePenalty,
                draft.Performance,
                draft.ApplyAfternoon,
                draft.ApplyOneHand,
                excludedStageIds = draft.ExcludedStageIds.Order(StringComparer.Ordinal).ToArray(),
                rivalIdentityIds = draft.RivalIdentityIds.Order(StringComparer.Ordinal).ToArray(),
                strategyByStage = draft.StrategyByStage
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
            },
            appliedParameters,
            historicalMatches = composition.History
                .OrderBy(item => item.LocalMatchDate)
                .ThenBy(item => item.MatchId, StringComparer.Ordinal)
                .Select(item => new
                {
                    item.MatchId,
                    item.LocalMatchDate,
                    results = item.Results
                        .OrderBy(result => result.IdentityId, StringComparer.Ordinal)
                        .ThenBy(result => result.Division, StringComparer.Ordinal)
                        .Select(result => new
                        {
                            result.IdentityId,
                            result.Division,
                            stageHitFactors = result.StageHitFactors
                                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                            excludedStageIds = result.ExcludedStageIds.Order(StringComparer.Ordinal).ToArray(),
                            result.IsIdentityResolved
                        })
                        .ToArray()
                })
                .ToArray(),
            historicalStageScores = composition.StageScores
                .OrderBy(score => score.ShooterId, StringComparer.Ordinal)
                .ThenBy(score => score.StageId, StringComparer.Ordinal)
                .ToArray(),
            historicalInputs = composition.HistoricalInputs
                .OrderBy(input => input.LocalMatchDate, StringComparer.Ordinal)
                .ThenBy(input => input.MatchId, StringComparer.Ordinal)
                .ToArray(),
            estimate
        });
        if (package.Length > 8 * 1024 * 1024)
            throw new InvalidDataException("The reproducible frozen input package exceeds the 8 MiB feature limit.");

        using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            await gzip.WriteAsync(package, cancellationToken);
        var packageBytes = compressed.ToArray();
        var packageHash = Convert.ToHexString(SHA256.HashData(packageBytes)).ToLowerInvariant();
        var storedPackage = await objectStorage.PutAsync(
            "input",
            packageHash,
            packageBytes,
            "application/gzip",
            cancellationToken);
        var historicalAvailabilityIds = composition.HistoricalInputs
            .SelectMany(input => input.AvailableBeforeStartCompetitorIds
                .Select(competitorId => $"{input.MatchId}:{competitorId}"))
            .Concat(import?.AvailableBeforeStartCompetitorIds.Select(competitorId => $"{matchId}:{competitorId}") ?? [])
            .Order(StringComparer.Ordinal)
            .ToArray();
        var historicalExclusions = composition.HistoricalInputs
            .SelectMany(input => input.ExcludedStageIds.Select(stageId => $"{input.MatchId}:{stageId}"))
            .Concat(import?.ExcludedStageIds.Select(stageId => $"{matchId}:{stageId}") ?? [])
            .Order(StringComparer.Ordinal)
            .ToArray();
        var availabilitySetHash = HashStrings(historicalAvailabilityIds, historicalExclusions);
        var now = DateTime.UtcNow;
        var snapshot = new FreezerFrozenSnapshot
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            DraftId = draft.Id,
            DraftRevision = expectedRevision,
            MatchId = matchId,
            MatchRevisionId = revision.Id,
            InputPackageHash = packageHash,
            InputPackageObjectKey = storedPackage.Key,
            InputPackageLength = storedPackage.Length,
            EngineVersion = estimate.EngineVersion,
            ParameterVersion = draft.ParameterVersion,
            Parameters = new FreezerSnapshotParameters
            {
                Beta = appliedParameters.Beta,
                DriftPercent = appliedParameters.DriftPercent,
                OwnVariability = appliedParameters.OwnVariability,
                DistancePenalty = appliedParameters.DistancePenalty,
                DistancePenaltyConfirmed = appliedParameters.DistancePenaltyConfirmed,
                Performance = appliedParameters.Performance,
                AfternoonFactor = appliedParameters.AfternoonFactor,
                OneHandFactor = appliedParameters.OneHandFactor,
                ApplyAfternoon = draft.ApplyAfternoon,
                ApplyOneHand = draft.ApplyOneHand
            },
            Forecast = forecasts,
            StrategyByStage = draft.StrategyByStage,
            AvailabilitySetHash = availabilitySetHash,
            CreatedAtUtc = now
        };
        var publication = await repository.FreezeAsync(snapshot, expectedRevision, cancellationToken);
        return publication.IsConflict || publication.Snapshot is null
            ? null
            : await ToSnapshotResponseAsync(publication.Snapshot, cancellationToken);
    }

    public async Task<FreezerSnapshotResponse?> GetSnapshotAsync(
        string accountId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken);
        return snapshot is null ? null : await ToSnapshotResponseAsync(snapshot, cancellationToken);
    }

    public async Task<IReadOnlyList<FreezerSnapshotSelectionResponse>> ListSnapshotsAsync(
        string accountId,
        int limit,
        CancellationToken cancellationToken)
    {
        var snapshots = await repository.ListSnapshotsAsync(accountId, limit, cancellationToken);
        return snapshots.Select(snapshot => new FreezerSnapshotSelectionResponse(
            snapshot.Id,
            snapshot.MatchId,
            snapshot.MatchRevisionId,
            snapshot.EngineVersion,
            snapshot.ParameterVersion,
            snapshot.CreatedAtUtc)).ToArray();
    }

    public async Task<FreezerSnapshotReplayResponse?> ReplayAsync(
        string accountId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken);
        if (snapshot is null)
            return null;

        var packageBytes = await objectStorage.GetAsync(
            new PrivateObjectReference(
                snapshot.InputPackageObjectKey,
                snapshot.InputPackageHash,
                snapshot.InputPackageLength,
                "application/gzip"),
            cancellationToken);
        using var packageStream = new MemoryStream(packageBytes, writable: false);
        using var gzip = new GZipStream(packageStream, CompressionMode.Decompress);
        var package = await JsonSerializer.DeserializeAsync<FrozenReplayPackage>(
            gzip,
            cancellationToken: cancellationToken);
        if (package is null)
            throw new InvalidDataException("The frozen input package is empty or invalid.");

        if (package.SchemaVersion != 2 ||
            package.Target is null ||
            package.Draft is null ||
            package.HistoricalMatches is null ||
            package.HistoricalStageScores is null ||
            package.AppliedParameters is null ||
            package.Estimate is null)
            return ReplayUnavailable(snapshot, "snapshot_replay_inputs_unavailable: the frozen package does not contain all required replay inputs.");
        if (!FreezerAnalysisEngine.IsSupportedVersion(snapshot.EngineVersion) ||
            package.Estimate.EngineVersion != snapshot.EngineVersion)
            return ReplayUnavailable(snapshot, "snapshot_replay_engine_unsupported: this engine version cannot replay the frozen estimate.");
        if (package.Target.MatchId != snapshot.MatchId ||
            package.Target.RevisionId != snapshot.MatchRevisionId ||
            package.AppliedParameters.Version != snapshot.ParameterVersion)
            return ReplayUnavailable(snapshot, "snapshot_replay_provenance_mismatch: frozen package provenance does not match the stored snapshot.");

        var historicalMatches = package.HistoricalMatches.Select(match =>
            new HistoricalMatchResults(
                match.MatchId,
                DateOnly.ParseExact(match.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                match.Results.Select(result => new HistoricalShooterResult(
                    result.IdentityId,
                    result.Division,
                    result.StageHitFactors,
                    result.ExcludedStageIds.ToHashSet(StringComparer.Ordinal),
                    result.IsIdentityResolved)).ToArray())).ToArray();
        var replayDiagnostics = new List<string>();
        var replayedEstimate = CalculateEstimate(
            DateOnly.ParseExact(package.Target.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            package.Draft.IdentityId,
            package.Draft.Division,
            historicalMatches,
            package.HistoricalStageScores,
            package.Target.Stages,
            package.AppliedParameters,
            package.Draft.ApplyAfternoon,
            package.Draft.ApplyOneHand,
            replayDiagnostics);
        var replayStages = replayedEstimate.Stages
            .OrderBy(stage => stage.StageNumber)
            .Select(replayed =>
            {
                var frozen = package.Estimate.Stages.FirstOrDefault(stage => stage.StageId == replayed.StageId);
                var matches = frozen is not null &&
                    frozen.StageNumber == replayed.StageNumber &&
                    frozen.Center == replayed.Center &&
                    frozen.Lower == replayed.Lower &&
                    frozen.Upper == replayed.Upper;
                return new FreezerSnapshotReplayStageResponse(
                    replayed.StageId,
                    replayed.StageNumber,
                    frozen?.Center,
                    replayed.Center,
                    frozen?.Lower,
                    replayed.Lower,
                    frozen?.Upper,
                    replayed.Upper,
                    matches);
            }).ToArray();
        var storedSnapshotStages = snapshot.Forecast.ToDictionary(stage => stage.StageId, StringComparer.Ordinal);
        var snapshotMatchesPackage = replayStages.Length == snapshot.Forecast.Count &&
            replayStages.All(stage =>
                storedSnapshotStages.TryGetValue(stage.StageId, out var stored) &&
                stored.Center == stage.FrozenCenter &&
                stored.Lower == stage.FrozenLower &&
                stored.Upper == stage.FrozenUpper);
        var matchesFrozenForecast =
            package.Estimate.IsAvailable == replayedEstimate.IsAvailable &&
            package.Estimate.Stages.Count == replayedEstimate.Stages.Count &&
            replayStages.Length == replayedEstimate.Stages.Count &&
            replayStages.All(stage => stage.MatchesFrozenForecast) &&
            snapshotMatchesPackage;
        if (!snapshotMatchesPackage)
            replayDiagnostics.Add("snapshot_replay_storage_mismatch: persisted forecast values differ from the frozen package.");
        if (!matchesFrozenForecast)
            replayDiagnostics.Add("snapshot_replay_forecast_mismatch: replayed forecast values differ from the frozen estimate.");

        return new FreezerSnapshotReplayResponse(
            snapshot.Id,
            snapshot.EngineVersion,
            true,
            matchesFrozenForecast,
            replayStages,
            replayDiagnostics.Distinct(StringComparer.Ordinal).ToArray());
    }

    public async Task<FreezerVerificationResponse?> VerifyAsync(
        string accountId,
        string snapshotId,
        FreezerVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken);
        if (snapshot is null)
            return null;

        var finalRevision = await repository.GetRevisionAsync(
            snapshot.MatchId,
            request.FinalRevisionId,
            cancellationToken);
        if (finalRevision is null)
            throw new KeyNotFoundException("The selected final revision was not found for this match.");
        if (finalRevision.Completeness != FreezerImportCompleteness.Final ||
            finalRevision.Id == snapshot.MatchRevisionId)
            throw new ArgumentException("Verification requires a distinct, final published revision.");
        if (await repository.GetPublishedImportForRevisionAsync(
                accountId,
                finalRevision.Id,
                cancellationToken) is null)
            return null;

        var resultHash = HashForMatch(finalRevision.Id, finalRevision.SourceHash);
        var existing = await repository.FindVerificationAsync(
            accountId,
            snapshotId,
            resultHash,
            cancellationToken);
        if (existing is not null)
            return ToResponse(existing);

        var packageBytes = await objectStorage.GetAsync(
            new PrivateObjectReference(
                snapshot.InputPackageObjectKey,
                snapshot.InputPackageHash,
                snapshot.InputPackageLength,
                "application/gzip"),
            cancellationToken);
        using var packageStream = new MemoryStream(packageBytes, writable: false);
        using var gzip = new GZipStream(packageStream, CompressionMode.Decompress);
        using var package = await JsonDocument.ParseAsync(gzip, cancellationToken: cancellationToken);
        var root = package.RootElement;
        if (!root.TryGetProperty("draft", out var frozenDraft) ||
            !frozenDraft.TryGetProperty("IdentityId", out var frozenIdentityElement))
            throw new InvalidDataException("The frozen input package does not contain its target identity.");
        var frozenIdentityId = frozenIdentityElement.GetString();
        if (string.IsNullOrWhiteSpace(frozenIdentityId))
            throw new InvalidDataException("The frozen input package has no target identity.");

        string? frozenDivision = null;
        if (frozenDraft.TryGetProperty(nameof(FrozenReplayDraft.Division), out var divisionElement))
            frozenDivision = divisionElement.GetString();
        double? frozenBeta = null;
        if (root.TryGetProperty("appliedParameters", out var appliedParametersElement) &&
            appliedParametersElement.TryGetProperty(nameof(FreezerAnalysisParameters.Beta), out var betaElement) &&
            betaElement.TryGetDouble(out var frozenBetaValue))
            frozenBeta = frozenBetaValue;
        DateOnly? frozenMatchDate = null;
        if (root.TryGetProperty("target", out var targetElement) &&
            targetElement.TryGetProperty(nameof(FrozenReplayTarget.LocalMatchDate), out var matchDateElement) &&
            DateOnly.TryParseExact(
                matchDateElement.GetString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedMatchDate))
            frozenMatchDate = parsedMatchDate;

        var historicalScores = root.TryGetProperty("historicalStageScores", out var scoreElement)
            ? JsonSerializer.Deserialize<ShooterStageScore[]>(scoreElement.GetRawText()) ?? []
            : [];
        var fit = FreezerAnalysisEngine.FitStageProfiles(historicalScores);
        var diagnostics = fit.Diagnostics.Select(FormatDiagnostic).ToList();
        if (!root.TryGetProperty("historicalStageScores", out _))
            diagnostics.Add("frozen_history_unavailable: this snapshot predates frozen numerical history inputs.");
        var historicalMatches = root.TryGetProperty("historicalMatches", out var historicalMatchesElement)
            ? JsonSerializer.Deserialize<FrozenReplayHistoricalMatch[]>(historicalMatchesElement.GetRawText())?
                .Select(match => new HistoricalMatchResults(
                    match.MatchId,
                    DateOnly.ParseExact(match.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    match.Results.Select(result => new HistoricalShooterResult(
                        result.IdentityId,
                        result.Division,
                        result.StageHitFactors,
                        result.ExcludedStageIds.ToHashSet(StringComparer.Ordinal),
                        result.IsIdentityResolved)).ToArray()))
                .ToArray() ?? []
            : [];
        if (historicalMatches.Length == 0)
            diagnostics.Add("frozen_registry_history_unavailable: this snapshot has no frozen historical match records.");

        var results = await repository.GetResultsAsync(finalRevision.ResultIds, cancellationToken);
        var resolutions = await repository.ListIdentityResolutionsAsync(
            accountId,
            snapshot.MatchId,
            cancellationToken);
        var identityByResultId = resolutions.ToDictionary(
            resolution => resolution.ResultId,
            resolution => resolution.IdentityId,
            StringComparer.Ordinal);
        var informantDrift = CalculateInformantDrift(
            frozenMatchDate,
            frozenDivision,
            frozenIdentityId,
            historicalMatches,
            fit,
            frozenBeta,
            finalRevision.Stages,
            results,
            identityByResultId);
        diagnostics.AddRange(informantDrift.Diagnostics);
        var targetResults = results
            .Where(result => identityByResultId.GetValueOrDefault(result.Id) == frozenIdentityId)
            .OrderBy(result => result.Id, StringComparer.Ordinal)
            .ToArray();
        FreezerCompetitorResult? targetResult = targetResults.Length == 1 ? targetResults[0] : null;
        if (targetResults.Length == 0)
            diagnostics.Add("final_identity_unresolved: no final result is explicitly mapped to the frozen shooter identity.");
        else if (targetResults.Length > 1)
            diagnostics.Add("final_identity_ambiguous: multiple final results are mapped to the frozen shooter identity.");

        var resultStages = targetResult?.Stages.ToDictionary(stage => stage.StageId, StringComparer.Ordinal)
                           ?? new Dictionary<string, FreezerStageResult>(StringComparer.Ordinal);
        var forecasts = snapshot.Forecast.ToDictionary(forecast => forecast.StageId, StringComparer.Ordinal);
        var stageInputs = new List<VerificationStageInput>();
        var stageReports = new List<FreezerVerificationStageResult>();
        var missingStageCount = 0;
        var disqualifiedStageCount = 0;
        var zeroedStageCount = 0;

        foreach (var stage in finalRevision.Stages.OrderBy(stage => stage.StageNumber))
        {
            var stageDiagnostics = new List<string>();
            var stageGroup = FreezerAnalysisEngine.StageGroup(stage.MinimumRounds);
            if (stageGroup is null)
                stageDiagnostics.Add("Minimum rounds are missing or invalid; this stage is excluded from grouped metrics.");
            var forecast = forecasts.GetValueOrDefault(stage.StageId);
            resultStages.TryGetValue(stage.StageId, out var actualStage);
            var status = "scored";
            double? actualHitFactor = null;
            double? center = forecast?.Center;
            double? lower = forecast?.Lower;
            double? upper = forecast?.Upper;
            double? relativeError = null;
            bool? withinInterval = null;

            if (targetResult is null)
            {
                status = "identity_unresolved";
                stageDiagnostics.Add("No unique final result is mapped to the frozen shooter identity.");
            }
            else if (targetResult.IsDisqualified || actualStage?.Status == FreezerResultStatus.Disqualified)
            {
                status = "disqualified";
                disqualifiedStageCount++;
                stageDiagnostics.Add("The final official result is disqualified; no hit factor was verified.");
            }
            else if (targetResult.IsZeroed || actualStage?.Status == FreezerResultStatus.Zeroed)
            {
                status = "zeroed";
                zeroedStageCount++;
                stageDiagnostics.Add("The final official result is zeroed; no hit factor was verified.");
            }
            else if (actualStage is null || actualStage.Status == FreezerResultStatus.Missing)
            {
                status = "missing";
                missingStageCount++;
                stageDiagnostics.Add("No scored final result exists for this stage.");
            }
            else if (actualStage.Status != FreezerResultStatus.Scored ||
                     actualStage.HitFactor is not { } actual ||
                     !double.IsFinite(actual) ||
                     actual <= 0)
            {
                status = "invalid_actual";
                stageDiagnostics.Add("The final stage result has no positive finite scored hit factor.");
            }
            else if (forecast?.Center is not { } forecastCenter ||
                     forecast.Lower is not { } forecastLower ||
                     forecast.Upper is not { } forecastUpper ||
                     !double.IsFinite(forecastCenter) ||
                     !double.IsFinite(forecastLower) ||
                     !double.IsFinite(forecastUpper) ||
                     forecastCenter <= 0)
            {
                status = "forecast_unavailable";
                stageDiagnostics.Add("The frozen forecast is unavailable for this stage.");
            }
            else if (!fit.Stages.TryGetValue(stage.StageId, out var stageProfile) ||
                     !double.IsFinite(stageProfile.LogDifficulty))
            {
                status = "stage_difficulty_unavailable";
                stageDiagnostics.Add("The frozen history cannot reconstruct this stage's difficulty.");
            }
            else
            {
                var calculatedError = actualStage.HitFactor!.Value / forecastCenter - 1;
                if (!double.IsFinite(calculatedError))
                {
                    status = "invalid_actual";
                    stageDiagnostics.Add("The relative verification error is not finite.");
                }
                else
                {
                    actualHitFactor = actualStage.HitFactor;
                    relativeError = calculatedError;
                    withinInterval = actualHitFactor >= forecastLower && actualHitFactor <= forecastUpper;
                    stageInputs.Add(new VerificationStageInput(
                        stage.StageId,
                        actualHitFactor.Value,
                        stageProfile.LogDifficulty,
                        new StageForecast(true, forecastCenter, forecastLower, forecastUpper, 0, [])));
                }
            }

            if (stageDiagnostics.Count > 0)
                diagnostics.AddRange(stageDiagnostics.Select(message => $"{status}: {message}"));
            stageReports.Add(new FreezerVerificationStageResult
            {
                StageId = stage.StageId,
                StageNumber = stage.StageNumber,
                StageGroup = stageGroup,
                Status = status,
                ActualHitFactor = actualHitFactor,
                ForecastCenter = center,
                ForecastLower = lower,
                ForecastUpper = upper,
                RelativeError = relativeError,
                IsWithinPredictionInterval = withinInterval,
                Diagnostics = stageDiagnostics
            });
        }

        var summary = FreezerAnalysisEngine.Verify(stageInputs);
        diagnostics.AddRange(summary.Diagnostics.Select(FormatDiagnostic));
        diagnostics.Add($"stage_grouping_convention: {FreezerAnalysisEngine.StageGroupingConvention}.");
        var ungroupedStageCount = stageReports.Count(stage => stage.StageGroup is null);
        if (ungroupedStageCount > 0)
            diagnostics.Add($"stage_group_unavailable: {ungroupedStageCount} stages lack valid minimum-round metadata.");
        var groupedMetrics = BuildVerificationGroups(stageReports);
        var report = new FreezerVerificationReport
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            SnapshotId = snapshotId,
            FinalRevisionId = finalRevision.Id,
            ResultHash = resultHash,
            StageCount = summary.StageCount,
            MissingStageCount = missingStageCount,
            DisqualifiedStageCount = disqualifiedStageCount,
            ZeroedStageCount = zeroedStageCount,
            MeanRelativeError = summary.IsAvailable && double.IsFinite(summary.MeanRelativeError)
                ? summary.MeanRelativeError
                : null,
            ErrorSampleStandardDeviation = summary.IsAvailable && double.IsFinite(summary.ErrorSampleStandardDeviation)
                ? summary.ErrorSampleStandardDeviation
                : null,
            IntervalCoverageCount = summary.IntervalCoverageCount,
            ObservedBeta = summary.ObservedBeta is { } beta && double.IsFinite(beta) ? beta : null,
            StageGroupingConvention = FreezerAnalysisEngine.StageGroupingConvention,
            Groups = groupedMetrics,
            InformantDriftComparisonCount = informantDrift.ComparisonCount,
            InformantDriftMissingStageCount = informantDrift.MissingStageCount,
            InformantDriftDisqualifiedStageCount = informantDrift.DisqualifiedStageCount,
            InformantDriftZeroedStageCount = informantDrift.ZeroedStageCount,
            InformantDriftUnverifiableStageCount = informantDrift.UnverifiableStageCount,
            InformantDriftMeanRelativeError = informantDrift.MeanRelativeError,
            InformantDriftErrorSampleStandardDeviation = informantDrift.ErrorSampleStandardDeviation,
            Stages = stageReports,
            Diagnostics = diagnostics.Distinct(StringComparer.Ordinal).ToArray(),
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.SaveVerificationAsync(report, cancellationToken);
        var saved = await repository.FindVerificationAsync(accountId, snapshotId, resultHash, cancellationToken);
        return saved is null
            ? throw new InvalidOperationException("The verification report was not persisted.")
            : ToResponse(saved);
    }

    public async Task<FreezerParameterBacktestResponse> BacktestParametersAsync(
        string accountId,
        FreezerParameterBacktestRequest request,
        CancellationToken cancellationToken)
    {
        var evaluationKeys = request.Evaluations
            .Select(item => $"{item.SnapshotId}\0{item.FinalRevisionId}")
            .ToArray();
        if (evaluationKeys.Distinct(StringComparer.Ordinal).Count() != evaluationKeys.Length)
            throw new ArgumentException("Backtest evaluations must not contain duplicate snapshot and final-revision pairs.");

        var parameterValues = new FreezerParameterValues
        {
            Beta = request.Parameters.Beta,
            DriftPercent = request.Parameters.DriftPercent,
            OwnVariability = request.Parameters.OwnVariability,
            DistancePenalty = request.Parameters.DistancePenalty
        };
        var parameterVersion = ParameterVersion(parameterValues);
        var parameters = FreezerAnalysisEngine.CurrentParameters with
        {
            Version = parameterVersion,
            Beta = parameterValues.Beta,
            DriftPercent = parameterValues.DriftPercent,
            OwnVariability = parameterValues.OwnVariability,
            DistancePenalty = parameterValues.DistancePenalty
        };
        var evaluations = new List<FreezerParameterBacktestEvaluation>(request.Evaluations.Count);
        var allStageInputs = new List<VerificationStageInput>();
        var diagnostics = new List<string>();

        foreach (var selection in request.Evaluations)
        {
            var snapshot = await repository.GetSnapshotAsync(accountId, selection.SnapshotId, cancellationToken);
            if (snapshot is null)
                throw new KeyNotFoundException("A selected frozen snapshot was not found.");

            var finalRevision = await repository.GetRevisionAsync(
                snapshot.MatchId,
                selection.FinalRevisionId,
                cancellationToken);
            if (finalRevision is null)
                throw new KeyNotFoundException("A selected final revision was not found for its frozen match.");
            if (finalRevision.Completeness != FreezerImportCompleteness.Final ||
                finalRevision.Id == snapshot.MatchRevisionId)
                throw new ArgumentException("Backtests require a distinct, final published revision for each frozen snapshot.");
            if (await repository.GetPublishedImportForRevisionAsync(
                    accountId,
                    finalRevision.Id,
                    cancellationToken) is null)
                throw new KeyNotFoundException("A selected final revision is not published to this account.");

            var package = await ReadFrozenReplayPackageAsync(snapshot, cancellationToken);
            if (package is null ||
                package.SchemaVersion != 2 ||
                package.Target is null ||
                package.Draft is null ||
                package.HistoricalMatches is null ||
                package.HistoricalStageScores is null ||
                package.AppliedParameters is null ||
                package.Estimate is null ||
                package.Target.MatchId != snapshot.MatchId ||
                package.Target.RevisionId != snapshot.MatchRevisionId ||
                package.AppliedParameters.Version != snapshot.ParameterVersion ||
                !FreezerAnalysisEngine.IsSupportedVersion(snapshot.EngineVersion) ||
                package.Estimate.EngineVersion != snapshot.EngineVersion)
            {
                var unavailable = new FreezerParameterBacktestEvaluation
                {
                    SnapshotId = snapshot.Id,
                    MatchId = snapshot.MatchId,
                    FinalRevisionId = finalRevision.Id,
                    Status = "unavailable",
                    Diagnostics = ["backtest_frozen_inputs_unavailable: the selected snapshot does not contain supported frozen replay inputs."]
                };
                evaluations.Add(unavailable);
                diagnostics.AddRange(unavailable.Diagnostics);
                continue;
            }

            var historicalMatches = package.HistoricalMatches.Select(match =>
                new HistoricalMatchResults(
                    match.MatchId,
                    DateOnly.ParseExact(match.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    match.Results.Select(result => new HistoricalShooterResult(
                        result.IdentityId,
                        result.Division,
                        result.StageHitFactors,
                        result.ExcludedStageIds.ToHashSet(StringComparer.Ordinal),
                        result.IsIdentityResolved)).ToArray())).ToArray();
            var forecastDiagnostics = new List<string>();
            var candidateEstimate = CalculateEstimate(
                DateOnly.ParseExact(package.Target.LocalMatchDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                package.Draft.IdentityId,
                package.Draft.Division,
                historicalMatches,
                package.HistoricalStageScores,
                package.Target.Stages,
                parameters,
                package.Draft.ApplyAfternoon,
                package.Draft.ApplyOneHand,
                forecastDiagnostics);
            var fit = FreezerAnalysisEngine.FitStageProfiles(package.HistoricalStageScores);
            var results = await repository.GetResultsAsync(finalRevision.ResultIds, cancellationToken);
            var resolutions = await repository.ListIdentityResolutionsAsync(accountId, snapshot.MatchId, cancellationToken);
            var identityByResultId = resolutions.ToDictionary(
                resolution => resolution.ResultId,
                resolution => resolution.IdentityId,
                StringComparer.Ordinal);
            var targetResults = results
                .Where(result => identityByResultId.GetValueOrDefault(result.Id) == package.Draft.IdentityId)
                .OrderBy(result => result.Id, StringComparer.Ordinal)
                .ToArray();
            var targetResult = targetResults.Length == 1 ? targetResults[0] : null;
            var finalStages = targetResult?.Stages.ToDictionary(stage => stage.StageId, StringComparer.Ordinal)
                              ?? new Dictionary<string, FreezerStageResult>(StringComparer.Ordinal);
            var candidateStages = candidateEstimate.Stages.ToDictionary(stage => stage.StageId, StringComparer.Ordinal);
            var stageReports = new List<FreezerParameterBacktestStage>(finalRevision.Stages.Count);
            var stageInputs = new List<VerificationStageInput>();
            var missingCount = 0;
            var disqualifiedCount = 0;
            var zeroedCount = 0;
            var evaluationDiagnostics = new List<string>(forecastDiagnostics);
            if (targetResults.Length == 0)
                evaluationDiagnostics.Add("final_identity_unresolved: no final result is explicitly mapped to the frozen shooter identity.");
            else if (targetResults.Length > 1)
                evaluationDiagnostics.Add("final_identity_ambiguous: multiple final results are mapped to the frozen shooter identity.");

            foreach (var stage in finalRevision.Stages.OrderBy(stage => stage.StageNumber))
            {
                candidateStages.TryGetValue(stage.StageId, out var forecast);
                finalStages.TryGetValue(stage.StageId, out var actualStage);
                var stageStatus = "scored";
                double? actualHitFactor = null;
                double? forecastCenter = forecast?.Center;
                double? forecastLower = forecast?.Lower;
                double? forecastUpper = forecast?.Upper;
                double? relativeError = null;
                bool? withinInterval = null;

                if (targetResult is null)
                    stageStatus = "identity_unresolved";
                else if (targetResult.IsDisqualified || actualStage?.Status == FreezerResultStatus.Disqualified)
                {
                    stageStatus = "disqualified";
                    disqualifiedCount++;
                }
                else if (targetResult.IsZeroed || actualStage?.Status == FreezerResultStatus.Zeroed)
                {
                    stageStatus = "zeroed";
                    zeroedCount++;
                }
                else if (actualStage is null || actualStage.Status == FreezerResultStatus.Missing)
                {
                    stageStatus = "missing";
                    missingCount++;
                }
                else if (actualStage.Status != FreezerResultStatus.Scored ||
                         actualStage.HitFactor is not { } actual ||
                         !double.IsFinite(actual) ||
                         actual <= 0)
                    stageStatus = "invalid_actual";
                else if (forecast?.Center is not { } center ||
                         forecast.Lower is not { } lower ||
                         forecast.Upper is not { } upper ||
                         !double.IsFinite(center) ||
                         !double.IsFinite(lower) ||
                         !double.IsFinite(upper) ||
                         center <= 0)
                    stageStatus = "forecast_unavailable";
                else if (!fit.Stages.TryGetValue(stage.StageId, out var stageProfile) ||
                         !double.IsFinite(stageProfile.LogDifficulty))
                    stageStatus = "stage_difficulty_unavailable";
                else
                {
                    actualHitFactor = actualStage.HitFactor;
                    relativeError = actualHitFactor.Value / center - 1;
                    if (!double.IsFinite(relativeError.Value))
                    {
                        stageStatus = "invalid_actual";
                        actualHitFactor = null;
                        relativeError = null;
                    }
                    else
                    {
                        withinInterval = actualHitFactor.Value >= lower && actualHitFactor.Value <= upper;
                        stageInputs.Add(new VerificationStageInput(
                            stage.StageId,
                            actualHitFactor.Value,
                            stageProfile.LogDifficulty,
                            new StageForecast(true, center, lower, upper, 0, [])));
                    }
                }

                stageReports.Add(new FreezerParameterBacktestStage
                {
                    StageId = stage.StageId,
                    StageNumber = stage.StageNumber,
                    Status = stageStatus,
                    ActualHitFactor = actualHitFactor,
                    ForecastCenter = forecastCenter,
                    ForecastLower = forecastLower,
                    ForecastUpper = forecastUpper,
                    RelativeError = relativeError,
                    IsWithinPredictionInterval = withinInterval
                });
            }

            var summary = FreezerAnalysisEngine.Verify(stageInputs);
            allStageInputs.AddRange(stageInputs);
            evaluationDiagnostics.AddRange(summary.Diagnostics.Select(FormatDiagnostic));
            var evaluation = new FreezerParameterBacktestEvaluation
            {
                SnapshotId = snapshot.Id,
                MatchId = snapshot.MatchId,
                FinalRevisionId = finalRevision.Id,
                Status = "evaluated",
                VerifiableStageCount = summary.StageCount,
                MissingStageCount = missingCount,
                DisqualifiedStageCount = disqualifiedCount,
                ZeroedStageCount = zeroedCount,
                MeanRelativeError = summary.IsAvailable ? summary.MeanRelativeError : null,
                ErrorSampleStandardDeviation = summary.IsAvailable ? summary.ErrorSampleStandardDeviation : null,
                IntervalCoverageCount = summary.IntervalCoverageCount,
                Stages = stageReports,
                Diagnostics = evaluationDiagnostics.Distinct(StringComparer.Ordinal).ToArray()
            };
            evaluations.Add(evaluation);
            diagnostics.AddRange(evaluation.Diagnostics);
        }

        var aggregate = FreezerAnalysisEngine.Verify(allStageInputs);
        diagnostics.AddRange(aggregate.Diagnostics.Select(FormatDiagnostic));
        var report = new FreezerParameterBacktest
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            EngineVersion = FreezerAnalysisEngine.Version,
            ParameterVersion = parameterVersion,
            Parameters = parameterValues,
            EvaluationCount = evaluations.Count,
            VerifiableStageCount = aggregate.StageCount,
            MeanRelativeError = aggregate.IsAvailable ? aggregate.MeanRelativeError : null,
            ErrorSampleStandardDeviation = aggregate.IsAvailable ? aggregate.ErrorSampleStandardDeviation : null,
            IntervalCoverageCount = aggregate.IntervalCoverageCount,
            Evaluations = evaluations,
            Diagnostics = diagnostics.Distinct(StringComparer.Ordinal).ToArray(),
            CreatedAtUtc = DateTime.UtcNow
        };
        await repository.SaveParameterBacktestAsync(report, cancellationToken);
        return ToResponse(report);
    }

    public async Task<FreezerParameterAdoptionResponse?> GetLatestParameterAdoptionAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        var adoption = await repository.GetLatestParameterAdoptionAsync(accountId, cancellationToken);
        return adoption is null ? null : ToResponse(adoption);
    }

    public async Task<FreezerParameterBacktestResponse?> GetParameterBacktestAsync(
        string accountId,
        string backtestId,
        CancellationToken cancellationToken)
    {
        var backtest = await repository.GetParameterBacktestAsync(accountId, backtestId, cancellationToken);
        return backtest is null ? null : ToResponse(backtest);
    }

    public async Task<FreezerParameterAdoptionResponse> AdoptParametersAsync(
        string accountId,
        FreezerParameterAdoptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirm)
            throw new ArgumentException("Parameter adoption requires explicit confirmation.");
        if (request.ExpectedRevision == long.MaxValue)
            throw new ArgumentException("The parameter adoption revision is at its maximum value.");
        var backtest = await repository.GetParameterBacktestAsync(accountId, request.BacktestId, cancellationToken);
        if (backtest is null)
            throw new KeyNotFoundException("The selected parameter backtest was not found.");
        if (!FreezerAnalysisEngine.IsSupportedVersion(backtest.EngineVersion) ||
            backtest.Evaluations.Count == 0 ||
            backtest.Evaluations.Any(evaluation =>
                evaluation.Status != "evaluated" || evaluation.VerifiableStageCount == 0) ||
            backtest.VerifiableStageCount == 0)
            throw new ArgumentException("Only a supported backtest with verifiable stage results can be adopted.");

        var adoption = await repository.SaveParameterAdoptionAsync(new FreezerParameterAdoption
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            BacktestId = backtest.Id,
            ParameterVersion = backtest.ParameterVersion,
            Parameters = backtest.Parameters,
            CreatedAtUtc = DateTime.UtcNow
        }, request.ExpectedRevision, cancellationToken);
        return adoption is null
            ? throw new InvalidOperationException("The parameter adoption revision is stale.")
            : ToResponse(adoption);
    }

    private async Task<FrozenReplayPackage?> ReadFrozenReplayPackageAsync(
        FreezerFrozenSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var bytes = await objectStorage.GetAsync(
            new PrivateObjectReference(
                snapshot.InputPackageObjectKey,
                snapshot.InputPackageHash,
                snapshot.InputPackageLength,
                "application/gzip"),
            cancellationToken);
        using var packageStream = new MemoryStream(bytes, writable: false);
        using var gzip = new GZipStream(packageStream, CompressionMode.Decompress);
        return await JsonSerializer.DeserializeAsync<FrozenReplayPackage>(
            gzip,
            cancellationToken: cancellationToken);
    }

    private static string ParameterVersion(FreezerParameterValues parameters)
    {
        var canonical = string.Join(
            "|",
            parameters.Beta.ToString("R", CultureInfo.InvariantCulture),
            parameters.DriftPercent.ToString("R", CultureInfo.InvariantCulture),
            parameters.OwnVariability.ToString("R", CultureInfo.InvariantCulture),
            parameters.DistancePenalty.ToString("R", CultureInfo.InvariantCulture));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"candidate-{Convert.ToHexString(hash.AsSpan(0, 6)).ToLowerInvariant()}";
    }

    public async Task<FreezerFieldEventResponse?> AppendFieldEventAsync(
        string accountId,
        string snapshotId,
        FreezerFieldEventRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.FindFieldEventAsync(accountId, request.ClientOperationId, cancellationToken);
        if (existing is not null)
            return existing.SnapshotId == snapshotId ? ToResponse(existing) : null;

        var snapshot = await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken);
        if (snapshot is null)
            return null;
        var match = await repository.GetMatchAsync(snapshot.MatchId, cancellationToken);
        var revision = match?.ActiveRevisionId is null
            ? null
            : await repository.GetRevisionAsync(snapshot.MatchId, snapshot.MatchRevisionId, cancellationToken);
        if (revision is null)
            throw new InvalidDataException("The frozen snapshot references a missing stage revision.");
        var stageIds = revision.Stages.Select(stage => stage.StageId).ToHashSet(StringComparer.Ordinal);
        if (request.StartStage is { } startStage && !revision.Stages.Any(stage => stage.StageNumber == startStage))
            throw new ArgumentException("The start stage is not present in the frozen match.", nameof(request));
        if (request.SquadSize is < 1 or > 500 ||
            request.StartTurn is { } startTurn && (request.SquadSize is null || startTurn < 1 || startTurn > request.SquadSize))
            throw new ArgumentException("The turn and squad size are inconsistent.", nameof(request));
        if (request.DisplayOrder.Count > 0 &&
            (request.DisplayOrder.Count != stageIds.Count ||
             request.DisplayOrder.Distinct(StringComparer.Ordinal).Count() != request.DisplayOrder.Count ||
             request.DisplayOrder.Any(stageId => !stageIds.Contains(stageId))))
            throw new ArgumentException("Display order must contain each frozen stage exactly once.", nameof(request));
        if (request.SupersedesEventId is not null)
        {
            var events = await repository.GetFieldEventsAsync(accountId, snapshotId, cancellationToken);
            if (!events.Any(item => item.Id == request.SupersedesEventId))
                throw new ArgumentException("Only an event in this snapshot can be superseded.", nameof(request));
        }

        var fieldEvent = new FreezerFieldEvent
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            SnapshotId = snapshotId,
            ClientOperationId = request.ClientOperationId,
            Kind = request.Kind,
            Content = request.Content.Trim(),
            ServerReceivedAtUtc = DateTime.UtcNow,
            ObservedAtLocal = request.ObservedAtLocal,
            ObservedOffsetMinutes = request.ObservedOffsetMinutes,
            SupersedesEventId = request.SupersedesEventId,
            RenderingContext = new FreezerRenderingContext
            {
                StartStage = request.StartStage,
                StartTurn = request.StartTurn,
                SquadSize = request.SquadSize,
                DisplayOrder = request.DisplayOrder
            }
        };
        var saved = await repository.AppendFieldEventAsync(fieldEvent, snapshot.DraftId, cancellationToken);
        return saved is null ? null : ToResponse(saved);
    }

    public async Task<FreezerCardPayload?> GetCardPayloadAsync(
        string accountId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken);
        if (snapshot is null)
            return null;
        var match = await repository.GetMatchAsync(snapshot.MatchId, cancellationToken);
        var revision = await repository.GetRevisionAsync(snapshot.MatchId, snapshot.MatchRevisionId, cancellationToken);
        if (match is null || revision is null)
            throw new InvalidDataException("The frozen snapshot references a missing match revision.");
        var draft = await repository.GetDraftAsync(accountId, snapshot.MatchId, cancellationToken);
        var profile = await repository.GetProfileAsync(accountId, cancellationToken);
        var events = await repository.GetFieldEventsAsync(accountId, snapshotId, cancellationToken);
        var context = events.LastOrDefault()?.RenderingContext ?? draft?.RenderingContext ?? new FreezerRenderingContext();
        var forecasts = snapshot.Forecast.ToDictionary(item => item.StageId, StringComparer.Ordinal);
        var stages = revision.Stages.Select(stage =>
        {
            forecasts.TryGetValue(stage.StageId, out var forecast);
            return new FreezerCardStage(
                stage.StageNumber,
                stage.MinimumRounds,
                forecast?.Center,
                forecast?.Lower,
                forecast?.Upper,
                snapshot.StrategyByStage.GetValueOrDefault(stage.StageId));
        }).OrderBy(stage => stage.StageNumber).ToArray();
        return new FreezerCardPayload(
            match.Name,
            match.LocalMatchDate,
            draft?.Division ?? profile?.Division ?? string.Empty,
            profile?.PowerFactor ?? string.Empty,
            stages,
            context.StartStage,
            context.StartTurn,
            context.SquadSize,
            context.DisplayOrder,
            events.Select(item => $"{item.Kind}: {item.Content}").ToArray());
    }

    public async Task<(FreezerArtifactManifest Manifest, byte[] Archive)?> CreateOrGetArtifactAsync(
        string accountId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken);
        if (snapshot is null)
            return null;
        var payload = await GetCardPayloadAsync(accountId, snapshotId, cancellationToken);
        if (payload is null)
            return null;

        var canonicalPayload = JsonSerializer.SerializeToUtf8Bytes(payload);
        var inputKey = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{snapshot.InputPackageHash}\n{FreezerDocumentRenderer.RendererVersion}\n")
                .Concat(canonicalPayload).ToArray())).ToLowerInvariant();
        var existing = await repository.GetArtifactAsync(accountId, snapshotId, inputKey, cancellationToken);
        if (existing is not null)
            return (existing, await ReadArtifactAsync(existing, cancellationToken));

        var documents = await documentRenderer.RenderAsync(payload, cancellationToken);
        var archive = CreateArchive(snapshotId, documents);
        var archiveHash = Convert.ToHexString(SHA256.HashData(archive)).ToLowerInvariant();
        var reference = await objectStorage.PutAsync("artifact", archiveHash, archive, "application/zip", cancellationToken);
        var manifest = new FreezerArtifactManifest
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = accountId,
            SnapshotId = snapshotId,
            InputKey = inputKey,
            ObjectKey = reference.Key,
            Sha256 = reference.Sha256,
            Length = reference.Length,
            ContentType = reference.ContentType,
            RendererVersion = documents.RendererVersion,
            CreatedAtUtc = DateTime.UtcNow
        };
        var persisted = await repository.SaveArtifactAsync(manifest, cancellationToken);
        return (persisted, await ReadArtifactAsync(persisted, cancellationToken));
    }

    public async Task<(FreezerArtifactManifest Manifest, byte[] Archive)?> GetArtifactAsync(
        string accountId,
        string artifactId,
        CancellationToken cancellationToken)
    {
        var artifact = await repository.GetArtifactByIdAsync(accountId, artifactId, cancellationToken);
        if (artifact is null ||
            await repository.GetSnapshotAsync(accountId, artifact.SnapshotId, cancellationToken) is null)
            return null;
        return (artifact, await ReadArtifactAsync(artifact, cancellationToken));
    }

    public async Task<IReadOnlyList<FreezerFieldEventResponse>?> GetFieldEventsAsync(
        string accountId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        if (await repository.GetSnapshotAsync(accountId, snapshotId, cancellationToken) is null)
            return null;
        var events = await repository.GetFieldEventsAsync(accountId, snapshotId, cancellationToken);
        return events.Select(ToResponse).ToArray();
    }

    private async Task<FreezerSnapshotResponse> ToSnapshotResponseAsync(
        FreezerFrozenSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var match = await repository.GetMatchAsync(snapshot.MatchId, cancellationToken);
        var revision = await repository.GetRevisionAsync(snapshot.MatchId, snapshot.MatchRevisionId, cancellationToken);
        if (match is null || revision is null)
            throw new InvalidDataException("The frozen snapshot references a missing match revision.");
        var stages = snapshot.Forecast.Select(forecast =>
        {
            var stage = revision.Stages.FirstOrDefault(item => item.StageId == forecast.StageId);
            if (stage is null)
                throw new InvalidDataException($"Frozen forecast stage {forecast.StageId} is absent from the source revision.");
            return new FreezerStageEstimateResponse(
                stage.StageId,
                stage.StageNumber,
                stage.MinimumRounds,
                forecast.Center is { } center ? FreezerAnalysisEngine.RhythmSymbol(center) : null,
                forecast.Center,
                forecast.Lower,
                forecast.Upper,
                null,
                null,
                forecast.Diagnostics);
        }).ToArray();
        return new FreezerSnapshotResponse(
            snapshot.Id,
            snapshot.MatchId,
            snapshot.EngineVersion,
            snapshot.ParameterVersion,
            snapshot.CreatedAtUtc,
            stages);
    }

    private static FreezerFieldEventResponse ToResponse(FreezerFieldEvent fieldEvent) =>
        new(
            fieldEvent.Id,
            fieldEvent.SnapshotId,
            fieldEvent.Sequence,
            fieldEvent.Kind,
            fieldEvent.Content,
            fieldEvent.ServerReceivedAtUtc,
            fieldEvent.ObservedAtLocal,
            fieldEvent.ObservedOffsetMinutes,
            fieldEvent.RenderingContext.StartStage,
            fieldEvent.RenderingContext.StartTurn,
            fieldEvent.RenderingContext.SquadSize,
            fieldEvent.RenderingContext.DisplayOrder);

    private static FreezerProfileResponse ToResponse(FreezerProfile profile) =>
        new(profile.AccountId, profile.IdentityId, profile.Division, profile.PowerFactor, profile.Performance, profile.UpdatedAtUtc);

    private static FreezerParameterBacktestResponse ToResponse(FreezerParameterBacktest backtest) =>
        new(
            backtest.Id,
            backtest.EngineVersion,
            backtest.ParameterVersion,
            ToRequest(backtest.Parameters),
            backtest.EvaluationCount,
            backtest.VerifiableStageCount,
            backtest.MeanRelativeError,
            backtest.ErrorSampleStandardDeviation,
            backtest.IntervalCoverageCount,
            backtest.Evaluations.Select(evaluation => new FreezerParameterBacktestEvaluationResponse(
                evaluation.SnapshotId,
                evaluation.MatchId,
                evaluation.FinalRevisionId,
                evaluation.Status,
                evaluation.VerifiableStageCount,
                evaluation.MissingStageCount,
                evaluation.DisqualifiedStageCount,
                evaluation.ZeroedStageCount,
                evaluation.MeanRelativeError,
                evaluation.ErrorSampleStandardDeviation,
                evaluation.IntervalCoverageCount,
                evaluation.Stages.Select(stage => new FreezerParameterBacktestStageResponse(
                    stage.StageId,
                    stage.StageNumber,
                    stage.Status,
                    stage.ActualHitFactor,
                    stage.ForecastCenter,
                    stage.ForecastLower,
                    stage.ForecastUpper,
                    stage.RelativeError,
                    stage.IsWithinPredictionInterval)).ToArray(),
                evaluation.Diagnostics)).ToArray(),
            backtest.Diagnostics,
            backtest.CreatedAtUtc);

    private static FreezerParameterAdoptionResponse ToResponse(FreezerParameterAdoption adoption) =>
        new(
            adoption.Revision,
            adoption.BacktestId,
            adoption.ParameterVersion,
            ToRequest(adoption.Parameters),
            adoption.CreatedAtUtc);

    private static FreezerParameterValuesRequest ToRequest(FreezerParameterValues parameters) =>
        new()
        {
            Beta = parameters.Beta,
            DriftPercent = parameters.DriftPercent,
            OwnVariability = parameters.OwnVariability,
            DistancePenalty = parameters.DistancePenalty
        };

    private static string FormatDiagnostic(EngineDiagnostic diagnostic) =>
        $"{diagnostic.Code}: {diagnostic.Message}";

    private static FreezerIdentityResponse ToResponse(FreezerShooterIdentity identity) =>
        new(identity.Id, identity.NormalizedName, identity.Aliases, identity.Revision);

    private static FreezerIdentityCandidateResponse ToCandidateResponse(FreezerShooterIdentity identity) =>
        new(identity.Id, identity.NormalizedName, identity.Aliases);

    private static FreezerIdentityResolutionResponse ToResponse(FreezerIdentityResolution resolution) =>
        new(resolution.ResultId, resolution.IdentityId, resolution.Revision, resolution.ResolvedAtUtc);

    private static FreezerVerificationResponse ToResponse(FreezerVerificationReport report) =>
        new(
            report.Id,
            report.SnapshotId,
            report.FinalRevisionId,
            report.StageCount,
            report.MissingStageCount,
            report.DisqualifiedStageCount,
            report.ZeroedStageCount,
            report.MeanRelativeError,
            report.ErrorSampleStandardDeviation,
            report.IntervalCoverageCount,
            report.ObservedBeta,
            report.StageGroupingConvention,
            report.InformantDriftComparisonCount,
            report.InformantDriftMissingStageCount,
            report.InformantDriftDisqualifiedStageCount,
            report.InformantDriftZeroedStageCount,
            report.InformantDriftUnverifiableStageCount,
            report.InformantDriftMeanRelativeError,
            report.InformantDriftErrorSampleStandardDeviation,
            report.Groups.Select(group => new FreezerVerificationGroupResponse(
                group.StageGroup,
                group.StageCount,
                group.VerifiableStageCount,
                group.MissingStageCount,
                group.DisqualifiedStageCount,
                group.ZeroedStageCount,
                group.UnverifiableStageCount,
                group.MeanRelativeError,
                group.ErrorSampleStandardDeviation,
                group.IntervalCoverageCount,
                group.IntervalCoverageRate)).ToArray(),
            report.Stages.Select(stage => new FreezerVerificationStageResponse(
                stage.StageId,
                stage.StageNumber,
                stage.Status,
                stage.ActualHitFactor,
                stage.ForecastCenter,
                stage.ForecastLower,
                stage.ForecastUpper,
                stage.RelativeError,
                stage.IsWithinPredictionInterval,
                stage.Diagnostics,
                stage.StageGroup)).ToArray(),
            report.Diagnostics);

    private static IReadOnlyList<FreezerVerificationGroupResult> BuildVerificationGroups(
        IReadOnlyList<FreezerVerificationStageResult> stages) =>
        stages.Where(stage => stage.StageGroup is not null)
            .GroupBy(stage => stage.StageGroup!, StringComparer.Ordinal)
            .OrderBy(group => group.Key switch
            {
                "short" => 1,
                "medium" => 2,
                "long" => 3,
                _ => 4
            })
            .Select(group =>
            {
                var members = group.ToArray();
                var errors = members
                    .Where(stage => stage.RelativeError is { } error && double.IsFinite(error))
                    .Select(stage => stage.RelativeError!.Value)
                    .ToArray();
                var missingCount = members.Count(stage => stage.Status == "missing");
                var disqualifiedCount = members.Count(stage => stage.Status == "disqualified");
                var zeroedCount = members.Count(stage => stage.Status == "zeroed");
                var coverageCount = members.Count(stage => stage.IsWithinPredictionInterval == true);
                double? meanRelativeError = errors.Length > 0 ? errors.Average() : null;
                double? errorSampleStandardDeviation = errors.Length > 1
                    ? Math.Sqrt(errors.Sum(error => Math.Pow(error - errors.Average(), 2)) / (errors.Length - 1))
                    : null;
                return new FreezerVerificationGroupResult
                {
                    StageGroup = group.Key,
                    StageCount = members.Length,
                    VerifiableStageCount = errors.Length,
                    MissingStageCount = missingCount,
                    DisqualifiedStageCount = disqualifiedCount,
                    ZeroedStageCount = zeroedCount,
                    UnverifiableStageCount = members.Length - errors.Length - missingCount - disqualifiedCount - zeroedCount,
                    MeanRelativeError = meanRelativeError,
                    ErrorSampleStandardDeviation = errorSampleStandardDeviation,
                    IntervalCoverageCount = coverageCount,
                    IntervalCoverageRate = errors.Length > 0 ? coverageCount / (double)errors.Length : null
                };
            })
            .ToArray();

    private static InformantDriftSummary CalculateInformantDrift(
        DateOnly? matchDate,
        string? division,
        string targetIdentityId,
        IReadOnlyList<HistoricalMatchResults> historicalMatches,
        TwoWayFitResult fit,
        double? beta,
        IReadOnlyList<FreezerMatchStage> finalStages,
        IReadOnlyList<FreezerCompetitorResult> finalResults,
        IReadOnlyDictionary<string, string> identityByResultId)
    {
        var diagnostics = new List<string>
        {
            "informant_drift_method: mean and sample SD of (final HF / exp(frozen informant log-level + frozen beta x frozen stage log-difficulty)) - 1; denominator is mapped scored informant-stage comparisons."
        };
        var errors = new List<double>();
        var missingCount = 0;
        var disqualifiedCount = 0;
        var zeroedCount = 0;
        var unverifiableCount = 0;

        if (matchDate is null || string.IsNullOrWhiteSpace(division) || historicalMatches.Count == 0)
        {
            diagnostics.Add("informant_drift_unavailable: the frozen match date, division, or historical registry inputs are unavailable.");
            return new(0, 0, 0, 0, 0, null, null, diagnostics);
        }

        var registry = FreezerAnalysisEngine.BuildRegistry(matchDate.Value, division, targetIdentityId, historicalMatches);
        var informantIds = registry.Entries
            .Where(entry =>
                entry.IsInCandidatePool &&
                string.Equals(entry.Division, division, StringComparison.OrdinalIgnoreCase) &&
                fit.Shooters.ContainsKey(entry.IdentityId))
            .Select(entry => entry.IdentityId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (informantIds.Length == 0)
            diagnostics.Add("informant_drift_unavailable: no same-division frozen registry candidates have an available historical shooter profile.");

        foreach (var informantId in informantIds)
        {
            var informantResults = finalResults
                .Where(result =>
                    identityByResultId.GetValueOrDefault(result.Id) == informantId &&
                    string.Equals(result.Division, division, StringComparison.OrdinalIgnoreCase))
                .OrderBy(result => result.Id, StringComparer.Ordinal)
                .ToArray();

            if (informantResults.Length != 1)
            {
                if (informantResults.Length == 0)
                    missingCount += finalStages.Count;
                else
                    unverifiableCount += finalStages.Count;
                continue;
            }

            var informantResult = informantResults[0];
            var actualStages = informantResult.Stages.ToDictionary(stage => stage.StageId, StringComparer.Ordinal);
            foreach (var stage in finalStages)
            {
                actualStages.TryGetValue(stage.StageId, out var actualStage);
                if (informantResult.IsDisqualified || actualStage?.Status == FreezerResultStatus.Disqualified)
                {
                    disqualifiedCount++;
                    continue;
                }
                if (informantResult.IsZeroed || actualStage?.Status == FreezerResultStatus.Zeroed)
                {
                    zeroedCount++;
                    continue;
                }
                if (actualStage is null || actualStage.Status == FreezerResultStatus.Missing)
                {
                    missingCount++;
                    continue;
                }
                if (actualStage.Status != FreezerResultStatus.Scored ||
                    actualStage.HitFactor is not { } actualHitFactor ||
                    !double.IsFinite(actualHitFactor) ||
                    actualHitFactor <= 0 ||
                    beta is not { } frozenBeta ||
                    !double.IsFinite(frozenBeta) ||
                    !fit.Shooters.TryGetValue(informantId, out var shooterProfile) ||
                    !fit.Stages.TryGetValue(stage.StageId, out var stageProfile) ||
                    !double.IsFinite(shooterProfile.LogLevel) ||
                    !double.IsFinite(stageProfile.LogDifficulty))
                {
                    unverifiableCount++;
                    continue;
                }

                var expectedHitFactor = Math.Exp(shooterProfile.LogLevel + frozenBeta * stageProfile.LogDifficulty);
                var relativeError = actualHitFactor / expectedHitFactor - 1;
                if (!double.IsFinite(expectedHitFactor) ||
                    expectedHitFactor <= 0 ||
                    !double.IsFinite(relativeError))
                {
                    unverifiableCount++;
                    continue;
                }
                errors.Add(relativeError);
            }
        }

        if (missingCount + disqualifiedCount + zeroedCount + unverifiableCount > 0)
        {
            diagnostics.Add(
                $"informant_drift_exclusions: missing={missingCount}, disqualified={disqualifiedCount}, zeroed={zeroedCount}, unverifiable={unverifiableCount} informant-stage comparisons.");
        }
        if (errors.Count == 0 && informantIds.Length > 0)
            diagnostics.Add("informant_drift_unavailable: no informant-stage comparisons had both a mapped scored result and a finite frozen-model expectation.");

        double? mean = errors.Count > 0
            ? errors.Sum(error => error / errors.Count)
            : null;
        if (mean is { } meanValue && !double.IsFinite(meanValue))
            mean = null;
        double? standardDeviation = null;
        if (errors.Count > 1 && mean is { } finiteMean)
        {
            var variance = errors.Sum(error => Math.Pow(error - finiteMean, 2)) / (errors.Count - 1);
            if (double.IsFinite(variance) && variance >= 0)
                standardDeviation = Math.Sqrt(variance);
            else
                diagnostics.Add("informant_drift_dispersion_unavailable: the sample variance is not finite.");
        }
        return new(
            errors.Count,
            missingCount,
            disqualifiedCount,
            zeroedCount,
            unverifiableCount,
            mean,
            standardDeviation,
            diagnostics);
    }

    private static FreezerDraftResponse ToResponse(FreezerAnalysisDraft draft) =>
        new(
            draft.Id,
            draft.MatchId,
            draft.Revision,
            draft.ImportManifestId,
            draft.IdentityId,
            draft.Division,
            draft.ParameterVersion,
            draft.Beta,
            draft.DriftPercent,
            draft.OwnVariability,
            draft.DistancePenalty,
            FreezerAnalysisEngine.CurrentParameters.DistancePenaltyConfirmed,
            draft.Performance,
            draft.ApplyAfternoon,
            draft.ApplyOneHand,
            draft.ExcludedStageIds,
            draft.RivalIdentityIds,
            draft.StrategyByStage,
            draft.StartedAtUtc.HasValue,
            draft.FrozenSnapshotId);

    private static string HashForMatch(string matchKey, string resultHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{matchKey}\n{resultHash}"))).ToLowerInvariant();

    private static string HashStrings(params IReadOnlyList<string>[] values)
    {
        var canonical = string.Join('\n', values.SelectMany(x => x).Order(StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private async Task<byte[]> ReadArtifactAsync(
        FreezerArtifactManifest artifact,
        CancellationToken cancellationToken) =>
        await objectStorage.GetAsync(
            new PrivateObjectReference(
                artifact.ObjectKey,
                artifact.Sha256,
                artifact.Length,
                artifact.ContentType),
            cancellationToken);

    private static byte[] CreateArchive(string snapshotId, FreezerRenderedDocuments documents)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "card-a5.pdf", documents.A5Pdf);
            AddEntry(archive, "card-a4-fold.pdf", documents.A4FoldPdf);
            AddEntry(archive, "phone-front.png", documents.PhoneFrontPng);
            AddEntry(archive, "phone-back.png", documents.PhoneBackPng);
            var manifest = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1,
                snapshotId,
                rendererVersion = documents.RendererVersion,
                phoneWidth = FreezerDocumentRenderer.PhoneWidth,
                phoneHeight = FreezerDocumentRenderer.PhoneHeight,
                artifactNames = new[] { "card-a5.pdf", "card-a4-fold.pdf", "phone-front.png", "phone-back.png" }
            });
            AddEntry(archive, "manifest.json", manifest);
        }
        return buffer.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var stream = entry.Open();
        stream.Write(content);
    }

    private sealed record FrozenReplayPackage(
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("target")] FrozenReplayTarget? Target,
        [property: JsonPropertyName("draft")] FrozenReplayDraft? Draft,
        [property: JsonPropertyName("appliedParameters")] FreezerAnalysisParameters? AppliedParameters,
        [property: JsonPropertyName("historicalMatches")] IReadOnlyList<FrozenReplayHistoricalMatch>? HistoricalMatches,
        [property: JsonPropertyName("historicalStageScores")] IReadOnlyList<ShooterStageScore>? HistoricalStageScores,
        [property: JsonPropertyName("estimate")] FreezerEstimateResponse? Estimate);

    private sealed record FrozenReplayTarget(
        [property: JsonPropertyName("matchId")] string MatchId,
        string LocalMatchDate,
        [property: JsonPropertyName("revisionId")] string RevisionId,
        [property: JsonPropertyName("stages")] IReadOnlyList<FreezerMatchStage> Stages);

    private sealed record FrozenReplayDraft(
        string? IdentityId,
        string? Division,
        bool ApplyAfternoon,
        bool ApplyOneHand);

    private sealed record FrozenReplayHistoricalMatch(
        string MatchId,
        string LocalMatchDate,
        [property: JsonPropertyName("results")] IReadOnlyList<FrozenReplayShooterResult> Results);

    private sealed record FrozenReplayShooterResult(
        string IdentityId,
        string Division,
        [property: JsonPropertyName("stageHitFactors")] IReadOnlyDictionary<string, double> StageHitFactors,
        [property: JsonPropertyName("excludedStageIds")] IReadOnlyList<string> ExcludedStageIds,
        bool IsIdentityResolved);

    private sealed record EstimateComposition(
        FreezerEstimateResponse Estimate,
        IReadOnlyList<HistoricalMatchResults> History,
        IReadOnlyList<ShooterStageScore> StageScores,
        IReadOnlyList<HistoricalInputProvenance> HistoricalInputs);

    private sealed record InformantDriftSummary(
        int ComparisonCount,
        int MissingStageCount,
        int DisqualifiedStageCount,
        int ZeroedStageCount,
        int UnverifiableStageCount,
        double? MeanRelativeError,
        double? ErrorSampleStandardDeviation,
        IReadOnlyList<string> Diagnostics);

    private sealed record HistoricalInputProvenance(
        string MatchId,
        string LocalMatchDate,
        string RevisionId,
        int RevisionNumber,
        string SourceHash,
        string ParserVersion,
        FreezerImportCompleteness Completeness,
        string? ImportManifestId,
        bool IsIncluded,
        string? ExclusionReason,
        IReadOnlyList<string> AvailableBeforeStartCompetitorIds,
        IReadOnlyList<string> ExcludedStageIds,
        IReadOnlyDictionary<string, string> IdentityByResultId,
        IReadOnlyList<HistoricalResultProjection> Results);

    private sealed record HistoricalResultProjection(
        string ResultId,
        string CompetitorId,
        string ContentHash,
        string? IdentityId,
        string Division,
        bool IsDisqualified,
        bool IsZeroed,
        IReadOnlyList<HistoricalStageProjection> Stages);

    private sealed record HistoricalStageProjection(
        string StageId,
        FreezerResultStatus Status,
        int MinimumRounds,
        double? HitFactor,
        double? TimeSeconds,
        int? NetPoints,
        double? ClassificationPoints,
        int? Alpha,
        int? Charlie,
        int? Delta,
        int? Miss,
        int? NoShoot,
        int? Procedural);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyDictionary<string, string> ValidateStrategies(IReadOnlyDictionary<string, string> strategies)
    {
        if (strategies.Count > 34)
            throw new ArgumentOutOfRangeException(nameof(strategies), "At most one strategy may be supplied per stage.");
        foreach (var strategy in strategies.Values)
        {
            if (strategy.Length > 260 || System.Text.RegularExpressions.Regex.Split(strategy, @"(?<=[.!?])\s+")
                    .Count(sentence => !string.IsNullOrWhiteSpace(sentence)) > 2)
                throw new ArgumentException("Each stage strategy is limited to two sentences and 260 characters.", nameof(strategies));
        }
        return strategies.ToDictionary(pair => pair.Key, pair => pair.Value.Trim(), StringComparer.Ordinal);
    }
}
