using System.Collections.Concurrent;
using MorWalPizVideo.Models.FreezerAnalysis;

namespace MorWalPizVideo.Domain.FreezerAnalysis;

public sealed class InMemoryFreezerAnalysisRepository : IFreezerAnalysisRepository
{
    private readonly object gate;
    private readonly ConcurrentDictionary<string, FreezerProfile> profiles;
    private readonly ConcurrentDictionary<string, FreezerMatch> matches;
    private readonly ConcurrentDictionary<string, FreezerMatchRevision> revisions;
    private readonly ConcurrentDictionary<string, FreezerCompetitorResult> resultsByHash;
    private readonly ConcurrentDictionary<string, FreezerShooterIdentity> identities;
    private readonly ConcurrentDictionary<string, FreezerIdentityResolution> identityResolutions;
    private readonly ConcurrentDictionary<string, FreezerImportManifest> importsById;
    private readonly ConcurrentDictionary<string, FreezerAnalysisDraft> drafts;
    private readonly ConcurrentDictionary<string, FreezerFrozenSnapshot> snapshots;
    private readonly ConcurrentDictionary<string, FreezerFieldEvent> eventsByOperation;
    private readonly ConcurrentDictionary<string, FreezerVerificationReport> verifications;
    private readonly ConcurrentDictionary<string, FreezerParameterBacktest> parameterBacktests;
    private readonly ConcurrentDictionary<string, FreezerParameterAdoption> parameterAdoptions;
    private readonly ConcurrentDictionary<string, FreezerArtifactManifest> artifacts;

    public InMemoryFreezerAnalysisRepository(InMemoryFreezerAnalysisState state)
    {
        gate = state.Gate;
        profiles = state.Profiles;
        matches = state.Matches;
        revisions = state.Revisions;
        resultsByHash = state.ResultsByHash;
        identities = state.Identities;
        identityResolutions = state.IdentityResolutions;
        importsById = state.ImportsById;
        drafts = state.Drafts;
        snapshots = state.Snapshots;
        eventsByOperation = state.EventsByOperation;
        verifications = state.Verifications;
        parameterBacktests = state.ParameterBacktests;
        parameterAdoptions = state.ParameterAdoptions;
        artifacts = state.Artifacts;
    }

    public Task<FreezerProfile?> GetProfileAsync(string accountId, CancellationToken cancellationToken) =>
        Task.FromResult(profiles.GetValueOrDefault(accountId));

    public Task SaveProfileAsync(FreezerProfile profile, CancellationToken cancellationToken)
    {
        profiles[profile.AccountId] = profile;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FreezerMatch>> ListPublishedMatchesAsync(int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FreezerMatch>>(matches.Values
            .Where(x => x.ActiveRevisionId is not null)
            .OrderByDescending(x => x.LocalMatchDate, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArray());

    public Task<IReadOnlyList<FreezerMatch>> ListPublishedMatchesBeforeAsync(
        DateOnly localMatchDate,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FreezerMatch>>(matches.Values
            .Where(x => x.ActiveRevisionId is not null && x.LocalMatchDate.CompareTo(
                localMatchDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)) < 0)
            .OrderByDescending(x => x.LocalMatchDate, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 1_001))
            .ToArray());

    public Task<FreezerMatch?> GetMatchAsync(string matchId, CancellationToken cancellationToken) =>
        Task.FromResult(matches.TryGetValue(matchId, out var match) && match.ActiveRevisionId is not null ? match : null);

    public Task<FreezerMatch?> FindMatchByKeyAsync(string matchKey, CancellationToken cancellationToken) =>
        Task.FromResult(matches.Values.FirstOrDefault(x => x.MatchKey == matchKey));

    public Task<FreezerMatchRevision?> GetRevisionAsync(string matchId, string revisionId, CancellationToken cancellationToken) =>
        Task.FromResult(revisions.TryGetValue(revisionId, out var revision) && revision.MatchId == matchId ? revision : null);

    public Task<IReadOnlyList<FreezerCompetitorResult>> GetResultsAsync(IEnumerable<string> resultIds, CancellationToken cancellationToken)
    {
        var ids = resultIds.ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<FreezerCompetitorResult> found = resultsByHash.Values.Where(x => ids.Contains(x.Id)).ToArray();
        return Task.FromResult(found);
    }

    public Task<FreezerShooterIdentity?> GetIdentityAsync(
        string accountId,
        string identityId,
        CancellationToken cancellationToken) =>
        Task.FromResult(identities.TryGetValue(identityId, out var identity) &&
                        identity.CreatedByAccountId == accountId
            ? identity
            : null);

    public Task<IReadOnlyList<FreezerShooterIdentity>> ListIdentitiesAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FreezerShooterIdentity> found = identities.Values
            .Where(x => x.CreatedByAccountId == accountId)
            .OrderBy(x => x.NormalizedName, StringComparer.Ordinal)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Take(1_000)
            .ToArray();
        return Task.FromResult(found);
    }

    public Task<FreezerShooterIdentity> CreateIdentityAsync(
        FreezerShooterIdentity identity,
        CancellationToken cancellationToken)
    {
        identities[identity.Id] = identity;
        return Task.FromResult(identity);
    }

    public Task<IReadOnlyList<FreezerShooterIdentity>> FindIdentityCandidatesAsync(
        string accountId,
        IEnumerable<string> normalizedNames,
        CancellationToken cancellationToken)
    {
        var names = normalizedNames.ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<FreezerShooterIdentity> found = identities.Values
            .Where(x => x.CreatedByAccountId == accountId &&
                        (names.Contains(x.NormalizedName) || x.NormalizedAliases.Any(names.Contains)))
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<FreezerIdentityResolution>> ListIdentityResolutionsAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FreezerIdentityResolution>>(identityResolutions.Values
            .Where(x => x.AccountId == accountId && x.MatchId == matchId)
            .ToArray());

    public Task<FreezerIdentityResolution?> GetIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        CancellationToken cancellationToken) =>
        Task.FromResult(identityResolutions.GetValueOrDefault(IdentityResolutionKey(accountId, matchId, resultId)));

    public Task<FreezerIdentityResolution?> SaveIdentityResolutionAsync(
        FreezerIdentityResolution resolution,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var key = IdentityResolutionKey(resolution.AccountId, resolution.MatchId, resolution.ResultId);
            var current = identityResolutions.GetValueOrDefault(key);
            if ((current?.Revision ?? 0) != expectedRevision)
                return Task.FromResult<FreezerIdentityResolution?>(null);

            var saved = resolution with { Revision = expectedRevision + 1 };
            identityResolutions[key] = saved;
            return Task.FromResult<FreezerIdentityResolution?>(saved);
        }
    }

    public Task<bool> DeleteIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var key = IdentityResolutionKey(accountId, matchId, resultId);
            var current = identityResolutions.GetValueOrDefault(key);
            if (current is null)
                return Task.FromResult(expectedRevision == 0);
            if (current.Revision != expectedRevision)
                return Task.FromResult(false);
            identityResolutions.TryRemove(key, out _);
            return Task.FromResult(true);
        }
    }

    public Task<FreezerImportManifest?> FindImportByHashAsync(string accountId, string sourceHash, CancellationToken cancellationToken) =>
        Task.FromResult(importsById.Values.FirstOrDefault(x => x.AccountId == accountId && x.SourceHash == sourceHash));

    public Task<FreezerImportManifest?> GetPublishedImportForRevisionAsync(
        string accountId,
        string revisionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(importsById.Values.FirstOrDefault(x =>
            x.AccountId == accountId &&
            x.RevisionId == revisionId &&
            x.State == FreezerImportState.Published));

    public Task<FreezerImportManifest?> GetImportAsync(string accountId, string importId, CancellationToken cancellationToken) =>
        Task.FromResult(importsById.TryGetValue(importId, out var manifest) && manifest.AccountId == accountId ? manifest : null);

    public Task<ImportPublicationResult> PublishImportAsync(
        FreezerMatch match,
        FreezerMatchRevision revision,
        IReadOnlyList<FreezerCompetitorResult> importedResults,
        FreezerImportManifest manifest,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var duplicate = importsById.Values.FirstOrDefault(x => x.AccountId == manifest.AccountId && x.SourceHash == manifest.SourceHash);
            if (duplicate is not null)
                return Task.FromResult(new ImportPublicationResult(duplicate, matches.GetValueOrDefault(duplicate.MatchId), false));

            var existing = matches.Values.FirstOrDefault(x => x.MatchKey == match.MatchKey);
            if (existing is not null && existing.OwnerAccountId != manifest.AccountId)
            {
                var staged = manifest with
                {
                    State = FreezerImportState.Staged,
                    Diagnostics = ["A match with this key is owned by another uploader; owner resolution is required."]
                };
                importsById[staged.Id] = staged;
                return Task.FromResult(new ImportPublicationResult(staged, null, true));
            }

            existing ??= match with { ActiveRevisionId = null, RevisionNumber = 0 };
            if (existing.RevisionNumber != expectedRevision)
            {
                var staged = manifest with { State = FreezerImportState.Staged, Diagnostics = ["The match has changed since this import preview was created."] };
                importsById[staged.Id] = staged;
                return Task.FromResult(new ImportPublicationResult(staged, existing, true));
            }

            var ids = new List<string>(importedResults.Count);
            foreach (var result in importedResults)
            {
                if (!resultsByHash.TryGetValue(result.ContentHash, out var stored))
                {
                    stored = result;
                    resultsByHash[result.ContentHash] = stored;
                }
                ids.Add(stored.Id);
            }

            var completedRevision = revision with { ResultIds = ids };
            revisions[completedRevision.Id] = completedRevision;
            var publishedMatch = existing with
            {
                ActiveRevisionId = completedRevision.Id,
                RevisionNumber = expectedRevision + 1
            };
            matches[publishedMatch.Id] = publishedMatch;
            var published = manifest with { RevisionId = completedRevision.Id, State = FreezerImportState.Published };
            importsById[published.Id] = published;
            return Task.FromResult(new ImportPublicationResult(published, publishedMatch, false));
        }
    }

    public Task<FreezerAnalysisDraft?> GetDraftAsync(string accountId, string matchId, CancellationToken cancellationToken) =>
        Task.FromResult(drafts.Values.FirstOrDefault(x => x.AccountId == accountId && x.MatchId == matchId));

    public Task<FreezerAnalysisDraft> CreateDraftAsync(FreezerAnalysisDraft draft, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var existing = drafts.Values.FirstOrDefault(x => x.AccountId == draft.AccountId && x.MatchId == draft.MatchId);
            if (existing is not null)
                return Task.FromResult(existing);
            drafts[draft.Id] = draft;
            return Task.FromResult(draft);
        }
    }

    public Task<FreezerAnalysisDraft?> UpdateDraftAsync(FreezerAnalysisDraft draft, long expectedRevision, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (!drafts.TryGetValue(draft.Id, out var current) ||
                current.AccountId != draft.AccountId || current.Revision != expectedRevision ||
                current.StartedAtUtc is not null || current.FrozenSnapshotId is not null)
                return Task.FromResult<FreezerAnalysisDraft?>(null);
            var updated = draft with { Revision = expectedRevision + 1, UpdatedAtUtc = DateTime.UtcNow };
            drafts[draft.Id] = updated;
            return Task.FromResult<FreezerAnalysisDraft?>(updated);
        }
    }

    public Task<FreezePublicationResult> FreezeAsync(FreezerFrozenSnapshot snapshot, long expectedRevision, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (!drafts.TryGetValue(snapshot.DraftId, out var draft) || draft.AccountId != snapshot.AccountId)
                return Task.FromResult(new FreezePublicationResult(null, null, true));
            if (draft.FrozenSnapshotId is { } existingId)
                return Task.FromResult(snapshots.TryGetValue(existingId, out var existing)
                    ? new FreezePublicationResult(draft, existing, false)
                    : new FreezePublicationResult(draft, null, true));
            if (draft.Revision != expectedRevision || draft.StartedAtUtc is not null)
                return Task.FromResult(new FreezePublicationResult(draft, null, true));
            snapshots[snapshot.Id] = snapshot;
            var frozen = draft with { FrozenSnapshotId = snapshot.Id, UpdatedAtUtc = DateTime.UtcNow };
            drafts[draft.Id] = frozen;
            return Task.FromResult(new FreezePublicationResult(frozen, snapshot, false));
        }
    }

    public Task<FreezerFrozenSnapshot?> GetSnapshotAsync(string accountId, string snapshotId, CancellationToken cancellationToken) =>
        Task.FromResult(snapshots.TryGetValue(snapshotId, out var snapshot) && snapshot.AccountId == accountId ? snapshot : null);

    public Task<IReadOnlyList<FreezerFrozenSnapshot>> ListSnapshotsAsync(
        string accountId,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FreezerFrozenSnapshot>>(snapshots.Values
            .Where(item => item.AccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArray());

    public Task<FreezerFieldEvent?> FindFieldEventAsync(string accountId, string clientOperationId, CancellationToken cancellationToken) =>
        Task.FromResult(eventsByOperation.TryGetValue($"{accountId}:{clientOperationId}", out var item) && item.AccountId == accountId ? item : null);

    public Task<FreezerFieldEvent?> AppendFieldEventAsync(FreezerFieldEvent fieldEvent, string draftId, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var key = $"{fieldEvent.AccountId}:{fieldEvent.ClientOperationId}";
            if (eventsByOperation.TryGetValue(key, out var existing))
                return Task.FromResult(existing.SnapshotId == fieldEvent.SnapshotId ? existing : null);
            if (!drafts.TryGetValue(draftId, out var draft) || draft.AccountId != fieldEvent.AccountId ||
                draft.FrozenSnapshotId != fieldEvent.SnapshotId)
                return Task.FromResult<FreezerFieldEvent?>(null);

            var persisted = fieldEvent with { Sequence = draft.LastEventSequence + 1 };
            eventsByOperation[key] = persisted;
            drafts[draftId] = draft with
            {
                LastEventSequence = persisted.Sequence,
                RenderingContext = fieldEvent.RenderingContext ?? draft.RenderingContext
            };
            return Task.FromResult<FreezerFieldEvent?>(persisted);
        }
    }

    public Task<IReadOnlyList<FreezerFieldEvent>> GetFieldEventsAsync(string accountId, string snapshotId, CancellationToken cancellationToken)
    {
        IReadOnlyList<FreezerFieldEvent> result = eventsByOperation.Values
            .Where(x => x.AccountId == accountId && x.SnapshotId == snapshotId)
            .OrderBy(x => x.Sequence)
            .Take(1000)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<FreezerVerificationReport?> FindVerificationAsync(string accountId, string snapshotId, string resultHash, CancellationToken cancellationToken) =>
        Task.FromResult(verifications.Values.FirstOrDefault(x =>
            x.AccountId == accountId && x.SnapshotId == snapshotId && x.ResultHash == resultHash));

    public Task SaveVerificationAsync(FreezerVerificationReport report, CancellationToken cancellationToken)
    {
        verifications.TryAdd($"{report.AccountId}:{report.SnapshotId}:{report.ResultHash}", report);
        return Task.CompletedTask;
    }

    public Task<FreezerParameterBacktest?> GetParameterBacktestAsync(
        string accountId,
        string backtestId,
        CancellationToken cancellationToken) =>
        Task.FromResult(parameterBacktests.TryGetValue(backtestId, out var item) && item.AccountId == accountId
            ? item
            : null);

    public Task SaveParameterBacktestAsync(FreezerParameterBacktest backtest, CancellationToken cancellationToken)
    {
        parameterBacktests[backtest.Id] = backtest;
        return Task.CompletedTask;
    }

    public Task<FreezerParameterAdoption?> GetLatestParameterAdoptionAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        Task.FromResult(parameterAdoptions.Values
            .Where(item => item.AccountId == accountId)
            .OrderByDescending(item => item.Revision)
            .FirstOrDefault());

    public Task<FreezerParameterAdoption?> SaveParameterAdoptionAsync(
        FreezerParameterAdoption adoption,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var current = parameterAdoptions.Values
                .Where(item => item.AccountId == adoption.AccountId)
                .OrderByDescending(item => item.Revision)
                .FirstOrDefault();
            if ((current?.Revision ?? 0) != expectedRevision)
                return Task.FromResult<FreezerParameterAdoption?>(null);

            var saved = adoption with { Revision = expectedRevision + 1 };
            parameterAdoptions[saved.Id] = saved;
            return Task.FromResult<FreezerParameterAdoption?>(saved);
        }
    }

    public Task<FreezerArtifactManifest?> GetArtifactAsync(
        string accountId,
        string snapshotId,
        string inputKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(artifacts.GetValueOrDefault($"{accountId}:{snapshotId}:{inputKey}"));

    public Task<FreezerArtifactManifest> SaveArtifactAsync(
        FreezerArtifactManifest artifact,
        CancellationToken cancellationToken) =>
        Task.FromResult(artifacts.GetOrAdd(
            $"{artifact.AccountId}:{artifact.SnapshotId}:{artifact.InputKey}",
            artifact));

    public Task<FreezerArtifactManifest?> GetArtifactByIdAsync(
        string accountId,
        string artifactId,
        CancellationToken cancellationToken) =>
        Task.FromResult(artifacts.Values.FirstOrDefault(x => x.AccountId == accountId && x.Id == artifactId));

    private static string IdentityResolutionKey(string accountId, string matchId, string resultId) =>
        string.Join('\0', accountId, matchId, resultId);
}

public sealed class InMemoryFreezerAnalysisState
{
    public object Gate { get; } = new();
    public ConcurrentDictionary<string, FreezerProfile> Profiles { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerMatch> Matches { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerMatchRevision> Revisions { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerCompetitorResult> ResultsByHash { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerShooterIdentity> Identities { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerIdentityResolution> IdentityResolutions { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerImportManifest> ImportsById { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerAnalysisDraft> Drafts { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerFrozenSnapshot> Snapshots { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerFieldEvent> EventsByOperation { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerVerificationReport> Verifications { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerParameterBacktest> ParameterBacktests { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerParameterAdoption> ParameterAdoptions { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, FreezerArtifactManifest> Artifacts { get; } = new(StringComparer.Ordinal);
}
