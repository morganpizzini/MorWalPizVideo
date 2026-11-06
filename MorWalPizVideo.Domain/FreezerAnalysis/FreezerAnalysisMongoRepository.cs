using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.FreezerAnalysis;

namespace MorWalPizVideo.Domain.FreezerAnalysis;

public sealed class FreezerAnalysisMongoRepository(FreezerAnalysisDatabase applicationDatabase)
    : IFreezerAnalysisRepository
{
    private readonly IMongoCollection<FreezerMatch> matches =
        applicationDatabase.Database.GetCollection<FreezerMatch>(DbCollections.FreezerMatches);
    private readonly IMongoCollection<FreezerMatchRevision> revisions =
        applicationDatabase.Database.GetCollection<FreezerMatchRevision>(DbCollections.FreezerMatchRevisions);
    private readonly IMongoCollection<FreezerCompetitorResult> results =
        applicationDatabase.Database.GetCollection<FreezerCompetitorResult>(DbCollections.FreezerResults);
    private readonly IMongoCollection<FreezerShooterIdentity> identities =
        applicationDatabase.Database.GetCollection<FreezerShooterIdentity>(DbCollections.FreezerIdentities);
    private readonly IMongoCollection<FreezerIdentityResolution> identityResolutions =
        applicationDatabase.Database.GetCollection<FreezerIdentityResolution>(DbCollections.FreezerIdentityResolutions);
    private readonly IMongoCollection<FreezerProfile> profiles =
        applicationDatabase.Database.GetCollection<FreezerProfile>(DbCollections.FreezerProfiles);
    private readonly IMongoCollection<FreezerImportManifest> imports =
        applicationDatabase.Database.GetCollection<FreezerImportManifest>(DbCollections.FreezerImports);
    private readonly IMongoCollection<FreezerAnalysisDraft> drafts =
        applicationDatabase.Database.GetCollection<FreezerAnalysisDraft>(DbCollections.FreezerDrafts);
    private readonly IMongoCollection<FreezerFrozenSnapshot> snapshots =
        applicationDatabase.Database.GetCollection<FreezerFrozenSnapshot>(DbCollections.FreezerSnapshots);
    private readonly IMongoCollection<FreezerFieldEvent> events =
        applicationDatabase.Database.GetCollection<FreezerFieldEvent>(DbCollections.FreezerFieldEvents);
    private readonly IMongoCollection<FreezerVerificationReport> verifications =
        applicationDatabase.Database.GetCollection<FreezerVerificationReport>(DbCollections.FreezerVerifications);
    private readonly IMongoCollection<FreezerParameterBacktest> parameterBacktests =
        applicationDatabase.Database.GetCollection<FreezerParameterBacktest>(DbCollections.FreezerParameterCandidates);
    private readonly IMongoCollection<FreezerParameterAdoption> parameterAdoptions =
        applicationDatabase.Database.GetCollection<FreezerParameterAdoption>(DbCollections.FreezerParameterAdoptions);
    private readonly IMongoCollection<FreezerArtifactManifest> artifacts =
        applicationDatabase.Database.GetCollection<FreezerArtifactManifest>(DbCollections.FreezerArtifacts);

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await matches.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<FreezerMatch>(
                Builders<FreezerMatch>.IndexKeys.Ascending(x => x.MatchKey),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_match_key" }),
            new CreateIndexModel<FreezerMatch>(
                Builders<FreezerMatch>.IndexKeys.Ascending(x => x.LocalMatchDate).Descending(x => x.ActiveRevisionId),
                new CreateIndexOptions { Name = "ix_freezer_match_date_active" })
        ], cancellationToken);
        await results.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerCompetitorResult>(
                Builders<FreezerCompetitorResult>.IndexKeys.Ascending(x => x.ContentHash),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_result_content_hash" }),
            cancellationToken: cancellationToken);
        await identities.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerShooterIdentity>(
                Builders<FreezerShooterIdentity>.IndexKeys.Ascending(x => x.CreatedByAccountId)
                    .Ascending(x => x.NormalizedName)
                    .Ascending(x => x.NormalizedAliases),
                new CreateIndexOptions { Name = "ix_freezer_identity_owner_alias" }),
            cancellationToken: cancellationToken);
        await identityResolutions.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerIdentityResolution>(
                Builders<FreezerIdentityResolution>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.MatchId).Ascending(x => x.ResultId),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_identity_resolution_owner_result" }),
            cancellationToken: cancellationToken);
        await profiles.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerProfile>(
                Builders<FreezerProfile>.IndexKeys.Ascending(x => x.AccountId),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_profile_account" }),
            cancellationToken: cancellationToken);
        await imports.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<FreezerImportManifest>(
                Builders<FreezerImportManifest>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.SourceHash),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_import_account_hash" }),
            new CreateIndexModel<FreezerImportManifest>(
                Builders<FreezerImportManifest>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.MatchId).Descending(x => x.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_freezer_import_account_match" }),
            new CreateIndexModel<FreezerImportManifest>(
                Builders<FreezerImportManifest>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.RevisionId),
                new CreateIndexOptions { Name = "ix_freezer_import_account_revision" })
        ], cancellationToken);
        await drafts.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerAnalysisDraft>(
                Builders<FreezerAnalysisDraft>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.MatchId),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_draft_account_match" }),
            cancellationToken: cancellationToken);
        await snapshots.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerFrozenSnapshot>(
                Builders<FreezerFrozenSnapshot>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.DraftId).Ascending(x => x.DraftRevision),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_snapshot_draft_revision" }),
            cancellationToken: cancellationToken);
        await snapshots.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerFrozenSnapshot>(
                Builders<FreezerFrozenSnapshot>.IndexKeys.Ascending(x => x.AccountId).Descending(x => x.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_freezer_snapshot_account_created" }),
            cancellationToken: cancellationToken);
        await events.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<FreezerFieldEvent>(
                Builders<FreezerFieldEvent>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.ClientOperationId),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_event_account_operation" }),
            new CreateIndexModel<FreezerFieldEvent>(
                Builders<FreezerFieldEvent>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.SnapshotId).Ascending(x => x.Sequence),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_event_snapshot_sequence" })
        ], cancellationToken);
        await verifications.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerVerificationReport>(
                Builders<FreezerVerificationReport>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.SnapshotId).Ascending(x => x.ResultHash),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_verification_result" }),
            cancellationToken: cancellationToken);
        await parameterBacktests.Indexes.CreateOneAsync(
            new CreateIndexModel<FreezerParameterBacktest>(
                Builders<FreezerParameterBacktest>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.Id),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_parameter_backtest_owner_id" }),
            cancellationToken: cancellationToken);
        await parameterAdoptions.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<FreezerParameterAdoption>(
                Builders<FreezerParameterAdoption>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.Revision),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_parameter_adoption_owner_revision" }),
            new CreateIndexModel<FreezerParameterAdoption>(
                Builders<FreezerParameterAdoption>.IndexKeys.Ascending(x => x.AccountId).Descending(x => x.Revision),
                new CreateIndexOptions { Name = "ix_freezer_parameter_adoption_latest" })
        ], cancellationToken);
        await artifacts.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<FreezerArtifactManifest>(
                Builders<FreezerArtifactManifest>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.SnapshotId).Ascending(x => x.InputKey),
                new CreateIndexOptions { Unique = true, Name = "ux_freezer_artifact_snapshot_input" }),
            new CreateIndexModel<FreezerArtifactManifest>(
                Builders<FreezerArtifactManifest>.IndexKeys.Ascending(x => x.AccountId).Ascending(x => x.Sha256),
                new CreateIndexOptions { Name = "ix_freezer_artifact_account_hash" })
        ], cancellationToken);
    }

    public Task<FreezerProfile?> GetProfileAsync(string accountId, CancellationToken cancellationToken) =>
        profiles.Find(x => x.AccountId == accountId).FirstOrDefaultAsync(cancellationToken);

    public Task SaveProfileAsync(FreezerProfile profile, CancellationToken cancellationToken) =>
        profiles.ReplaceOneAsync(x => x.AccountId == profile.AccountId, profile, new ReplaceOptions { IsUpsert = true }, cancellationToken);

    public async Task<IReadOnlyList<FreezerMatch>> ListPublishedMatchesAsync(int limit, CancellationToken cancellationToken) =>
        await matches.Find(x => x.ActiveRevisionId != null)
            .SortByDescending(x => x.LocalMatchDate)
            .Limit(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FreezerMatch>> ListPublishedMatchesBeforeAsync(
        DateOnly localMatchDate,
        int limit,
        CancellationToken cancellationToken)
    {
        var date = localMatchDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var filter = Builders<FreezerMatch>.Filter.And(
            Builders<FreezerMatch>.Filter.Ne(x => x.ActiveRevisionId, null),
            Builders<FreezerMatch>.Filter.Lt(x => x.LocalMatchDate, date));
        return await matches.Find(filter)
            .SortByDescending(x => x.LocalMatchDate)
            .Limit(Math.Clamp(limit, 1, 1_001))
            .ToListAsync(cancellationToken);
    }

    public Task<FreezerMatch?> GetMatchAsync(string matchId, CancellationToken cancellationToken) =>
        matches.Find(x => x.Id == matchId && x.ActiveRevisionId != null).FirstOrDefaultAsync(cancellationToken);

    public Task<FreezerMatch?> FindMatchByKeyAsync(string matchKey, CancellationToken cancellationToken) =>
        matches.Find(x => x.MatchKey == matchKey).FirstOrDefaultAsync(cancellationToken);

    public Task<FreezerMatchRevision?> GetRevisionAsync(string matchId, string revisionId, CancellationToken cancellationToken) =>
        revisions.Find(x => x.Id == revisionId && x.MatchId == matchId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FreezerCompetitorResult>> GetResultsAsync(
        IEnumerable<string> resultIds,
        CancellationToken cancellationToken)
    {
        var ids = resultIds.Distinct(StringComparer.Ordinal).Take(20_000).ToArray();
        return ids.Length == 0
            ? []
            : await results.Find(Builders<FreezerCompetitorResult>.Filter.In(x => x.Id, ids))
                .Limit(20_000)
                .ToListAsync(cancellationToken);
    }

    public Task<FreezerShooterIdentity?> GetIdentityAsync(
        string accountId,
        string identityId,
        CancellationToken cancellationToken) =>
        identities.Find(x => x.CreatedByAccountId == accountId && x.Id == identityId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FreezerShooterIdentity>> ListIdentitiesAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        await identities.Find(x => x.CreatedByAccountId == accountId)
            .SortBy(x => x.NormalizedName)
            .ThenBy(x => x.Id)
            .Limit(1_000)
            .ToListAsync(cancellationToken);

    public async Task<FreezerShooterIdentity> CreateIdentityAsync(
        FreezerShooterIdentity identity,
        CancellationToken cancellationToken)
    {
        await identities.InsertOneAsync(identity, cancellationToken: cancellationToken);
        return identity;
    }

    public async Task<IReadOnlyList<FreezerShooterIdentity>> FindIdentityCandidatesAsync(
        string accountId,
        IEnumerable<string> normalizedNames,
        CancellationToken cancellationToken)
    {
        var names = normalizedNames.Distinct(StringComparer.Ordinal).Take(20_000).ToArray();
        if (names.Length == 0)
            return [];

        var ownerFilter = Builders<FreezerShooterIdentity>.Filter.Eq(x => x.CreatedByAccountId, accountId);
        var nameFilter = Builders<FreezerShooterIdentity>.Filter.In(x => x.NormalizedName, names);
        var aliasFilter = Builders<FreezerShooterIdentity>.Filter.AnyIn(x => x.NormalizedAliases, names);
        return await identities.Find(Builders<FreezerShooterIdentity>.Filter.And(
                ownerFilter,
                Builders<FreezerShooterIdentity>.Filter.Or(nameFilter, aliasFilter)))
            .SortBy(x => x.Id)
            .Limit(10_000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FreezerIdentityResolution>> ListIdentityResolutionsAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken) =>
        await identityResolutions.Find(x => x.AccountId == accountId && x.MatchId == matchId)
            .Limit(20_000)
            .ToListAsync(cancellationToken);

    public Task<FreezerIdentityResolution?> GetIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        CancellationToken cancellationToken) =>
        identityResolutions.Find(x =>
            x.AccountId == accountId &&
            x.MatchId == matchId &&
            x.ResultId == resultId).FirstOrDefaultAsync(cancellationToken);

    public async Task<FreezerIdentityResolution?> SaveIdentityResolutionAsync(
        FreezerIdentityResolution resolution,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var current = await GetIdentityResolutionAsync(
            resolution.AccountId,
            resolution.MatchId,
            resolution.ResultId,
            cancellationToken);
        if ((current?.Revision ?? 0) != expectedRevision)
            return null;

        var saved = resolution with { Revision = expectedRevision + 1 };
        if (current is null)
        {
            try
            {
                await identityResolutions.InsertOneAsync(saved, cancellationToken: cancellationToken);
                return saved;
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return null;
            }
        }

        var result = await identityResolutions.ReplaceOneAsync(
            x => x.AccountId == resolution.AccountId &&
                 x.MatchId == resolution.MatchId &&
                 x.ResultId == resolution.ResultId &&
                 x.Revision == expectedRevision,
            saved,
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1 ? saved : null;
    }

    public async Task<bool> DeleteIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var current = await GetIdentityResolutionAsync(accountId, matchId, resultId, cancellationToken);
        if (current is null)
            return expectedRevision == 0;
        if (current.Revision != expectedRevision)
            return false;

        var result = await identityResolutions.DeleteOneAsync(
            x => x.AccountId == accountId &&
                 x.MatchId == matchId &&
                 x.ResultId == resultId &&
                 x.Revision == expectedRevision,
            cancellationToken);
        return result.DeletedCount == 1;
    }

    public Task<FreezerImportManifest?> FindImportByHashAsync(string accountId, string sourceHash, CancellationToken cancellationToken) =>
        imports.Find(x => x.AccountId == accountId && x.SourceHash == sourceHash).FirstOrDefaultAsync(cancellationToken);

    public Task<FreezerImportManifest?> GetPublishedImportForRevisionAsync(
        string accountId,
        string revisionId,
        CancellationToken cancellationToken) =>
        imports.Find(x =>
            x.AccountId == accountId &&
            x.RevisionId == revisionId &&
            x.State == FreezerImportState.Published).FirstOrDefaultAsync(cancellationToken);

    public Task<FreezerImportManifest?> GetImportAsync(string accountId, string importId, CancellationToken cancellationToken) =>
        imports.Find(x => x.AccountId == accountId && x.Id == importId).FirstOrDefaultAsync(cancellationToken);

    public async Task<ImportPublicationResult> PublishImportAsync(
        FreezerMatch match,
        FreezerMatchRevision revision,
        IReadOnlyList<FreezerCompetitorResult> importedResults,
        FreezerImportManifest manifest,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        var existingImport = await FindImportByHashAsync(manifest.AccountId, manifest.SourceHash, cancellationToken);
        if (existingImport is not null)
        {
            var importedMatch = await GetMatchAsync(existingImport.MatchId, cancellationToken);
            return new(existingImport, importedMatch, false);
        }

        var existingMatch = await matches.Find(x => x.MatchKey == match.MatchKey).FirstOrDefaultAsync(cancellationToken);
        if (existingMatch is not null && existingMatch.OwnerAccountId != manifest.AccountId)
        {
            var staged = manifest with { State = FreezerImportState.Staged, Diagnostics = ["A match with this key is owned by another uploader; owner resolution is required."] };
            await imports.InsertOneAsync(staged, cancellationToken: cancellationToken);
            return new(staged, null, true);
        }

        if (existingMatch is null)
        {
            try
            {
                await matches.InsertOneAsync(match with { ActiveRevisionId = null, RevisionNumber = 0 }, cancellationToken: cancellationToken);
                existingMatch = match with { ActiveRevisionId = null, RevisionNumber = 0 };
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                existingMatch = await matches.Find(x => x.MatchKey == match.MatchKey).FirstOrDefaultAsync(cancellationToken);
                if (existingMatch is null || existingMatch.OwnerAccountId != manifest.AccountId)
                    throw new InvalidOperationException("The match key was claimed by another import.", exception);
            }
        }

        if (existingMatch.RevisionNumber != expectedRevision)
        {
            var staged = manifest with { State = FreezerImportState.Staged, Diagnostics = ["The match has changed since this import preview was created."] };
            await imports.InsertOneAsync(staged, cancellationToken: cancellationToken);
            return new(staged, existingMatch, true);
        }

        var committedResultIds = new List<string>(importedResults.Count);
        foreach (var result in importedResults)
        {
            var previous = await results.Find(x => x.ContentHash == result.ContentHash).FirstOrDefaultAsync(cancellationToken);
            if (previous is not null)
            {
                committedResultIds.Add(previous.Id);
                continue;
            }

            try
            {
                await results.InsertOneAsync(result, cancellationToken: cancellationToken);
                committedResultIds.Add(result.Id);
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                var deduplicated = await results.Find(x => x.ContentHash == result.ContentHash).FirstOrDefaultAsync(cancellationToken);
                if (deduplicated is null)
                    throw new InvalidOperationException("A duplicate result hash could not be resolved.", exception);
                committedResultIds.Add(deduplicated.Id);
            }
        }

        var completedRevision = revision with { ResultIds = committedResultIds };
        await revisions.InsertOneAsync(completedRevision, cancellationToken: cancellationToken);

        var filter = Builders<FreezerMatch>.Filter.And(
            Builders<FreezerMatch>.Filter.Eq(x => x.Id, existingMatch.Id),
            Builders<FreezerMatch>.Filter.Eq(x => x.OwnerAccountId, manifest.AccountId),
            Builders<FreezerMatch>.Filter.Eq(x => x.RevisionNumber, expectedRevision));
        var updatedMatch = await matches.FindOneAndUpdateAsync(
            filter,
            Builders<FreezerMatch>.Update
                .Set(x => x.ActiveRevisionId, completedRevision.Id)
                .Set(x => x.RevisionNumber, expectedRevision + 1),
            new FindOneAndUpdateOptions<FreezerMatch> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (updatedMatch is null)
        {
            var staged = manifest with { State = FreezerImportState.Staged, Diagnostics = ["The match changed during publication; staged records remain invisible."] };
            await imports.InsertOneAsync(staged, cancellationToken: cancellationToken);
            return new(staged, existingMatch, true);
        }

        var published = manifest with
        {
            RevisionId = completedRevision.Id,
            State = FreezerImportState.Published
        };
        await imports.InsertOneAsync(published, cancellationToken: cancellationToken);
        return new(published, updatedMatch, false);
    }

    public Task<FreezerAnalysisDraft?> GetDraftAsync(string accountId, string matchId, CancellationToken cancellationToken) =>
        drafts.Find(x => x.AccountId == accountId && x.MatchId == matchId).FirstOrDefaultAsync(cancellationToken);

    public async Task<FreezerAnalysisDraft> CreateDraftAsync(FreezerAnalysisDraft draft, CancellationToken cancellationToken)
    {
        var existing = await GetDraftAsync(draft.AccountId, draft.MatchId, cancellationToken);
        if (existing is not null)
            return existing;
        try
        {
            await drafts.InsertOneAsync(draft, cancellationToken: cancellationToken);
            return draft;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return await GetDraftAsync(draft.AccountId, draft.MatchId, cancellationToken)
                ?? throw new InvalidOperationException("A concurrent draft could not be read after its unique-key conflict.", exception);
        }
    }

    public async Task<FreezerAnalysisDraft?> UpdateDraftAsync(
        FreezerAnalysisDraft draft,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FreezerAnalysisDraft>.Filter.And(
            Builders<FreezerAnalysisDraft>.Filter.Eq(x => x.Id, draft.Id),
            Builders<FreezerAnalysisDraft>.Filter.Eq(x => x.AccountId, draft.AccountId),
            Builders<FreezerAnalysisDraft>.Filter.Eq(x => x.Revision, expectedRevision),
            Builders<FreezerAnalysisDraft>.Filter.Eq(x => x.StartedAtUtc, null),
            Builders<FreezerAnalysisDraft>.Filter.Eq(x => x.FrozenSnapshotId, null));
        var updated = draft with { Revision = expectedRevision + 1, UpdatedAtUtc = DateTime.UtcNow };
        var result = await drafts.ReplaceOneAsync(filter, updated, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1 ? updated : null;
    }

    public Task<FreezerAnalysisDraft?> ReopenDraftAsync(
        string accountId,
        string matchId,
        long expectedRevision,
        CancellationToken cancellationToken) =>
        drafts.FindOneAndUpdateAsync(
            x => x.AccountId == accountId && x.MatchId == matchId &&
                 x.Revision == expectedRevision && x.StartedAtUtc == null &&
                 x.FrozenSnapshotId != null,
            Builders<FreezerAnalysisDraft>.Update
                .Set(x => x.FrozenSnapshotId, null)
                .Inc(x => x.Revision, 1)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            new FindOneAndUpdateOptions<FreezerAnalysisDraft> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

    public async Task<FreezePublicationResult> FreezeAsync(
        FreezerFrozenSnapshot snapshot,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var draft = await GetDraftByIdAsync(snapshot.AccountId, snapshot.DraftId, cancellationToken);
        if (draft?.FrozenSnapshotId is { } existingSnapshotId)
        {
            var existing = await GetSnapshotAsync(snapshot.AccountId, existingSnapshotId, cancellationToken);
            return existing is null
                ? new(draft, null, true)
                : new(draft, existing, false);
        }
        if (draft is null || draft.Revision != expectedRevision || draft.StartedAtUtc is not null)
            return new(draft, null, true);

        try
        {
            await snapshots.InsertOneAsync(snapshot, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var winner = await GetDraftByIdAsync(snapshot.AccountId, snapshot.DraftId, cancellationToken);
            if (winner?.FrozenSnapshotId is { } winnerSnapshotId)
            {
                var existing = await GetSnapshotAsync(snapshot.AccountId, winnerSnapshotId, cancellationToken);
                return existing is null ? new(winner, null, true) : new(winner, existing, false);
            }
            return new(winner, null, true);
        }
        var updated = await drafts.FindOneAndUpdateAsync(
            x => x.Id == draft.Id && x.AccountId == snapshot.AccountId &&
                 x.Revision == expectedRevision && x.StartedAtUtc == null &&
                 x.FrozenSnapshotId == null,
            Builders<FreezerAnalysisDraft>.Update
                .Set(x => x.FrozenSnapshotId, snapshot.Id)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            new FindOneAndUpdateOptions<FreezerAnalysisDraft> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        return updated is null
            ? new(draft, null, true)
            : new(updated, snapshot, false);
    }

    public Task<FreezerFrozenSnapshot?> GetSnapshotAsync(string accountId, string snapshotId, CancellationToken cancellationToken) =>
        snapshots.Find(x => x.AccountId == accountId && x.Id == snapshotId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FreezerFrozenSnapshot>> ListSnapshotsAsync(
        string accountId,
        int limit,
        CancellationToken cancellationToken) =>
        await snapshots.Find(x => x.AccountId == accountId)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

    public Task<FreezerFieldEvent?> FindFieldEventAsync(string accountId, string clientOperationId, CancellationToken cancellationToken) =>
        events.Find(x => x.AccountId == accountId && x.ClientOperationId == clientOperationId).FirstOrDefaultAsync(cancellationToken);

    public async Task<FreezerFieldEvent?> AppendFieldEventAsync(
        FreezerFieldEvent fieldEvent,
        string draftId,
        CancellationToken cancellationToken)
    {
        var previous = await FindFieldEventAsync(fieldEvent.AccountId, fieldEvent.ClientOperationId, cancellationToken);
        if (previous is not null)
            return previous.SnapshotId == fieldEvent.SnapshotId ? previous : null;
        var historicalSnapshot = await snapshots.Find(x =>
            x.Id == fieldEvent.SnapshotId &&
            x.AccountId == fieldEvent.AccountId &&
            x.DraftId == draftId).AnyAsync(cancellationToken);

        var eventUpdate = Builders<FreezerAnalysisDraft>.Update.Inc(x => x.LastEventSequence, 1);
        if (fieldEvent.RenderingContext is { } context)
        {
            eventUpdate = eventUpdate.Set(x => x.RenderingContext, context);
        }

        var draft = await drafts.FindOneAndUpdateAsync(
            historicalSnapshot
                ? x => x.Id == draftId && x.AccountId == fieldEvent.AccountId
                : x => x.Id == draftId && x.AccountId == fieldEvent.AccountId &&
                       x.FrozenSnapshotId == fieldEvent.SnapshotId,
            eventUpdate,
            new FindOneAndUpdateOptions<FreezerAnalysisDraft> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        if (draft is null)
            return null;

        var persisted = fieldEvent with { Sequence = draft.LastEventSequence };
        try
        {
            await events.InsertOneAsync(persisted, cancellationToken: cancellationToken);
            return persisted;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return await FindFieldEventAsync(fieldEvent.AccountId, fieldEvent.ClientOperationId, cancellationToken)
                ?? throw new InvalidOperationException("A field event duplicate could not be resolved.", exception);
        }
    }

    public async Task<IReadOnlyList<FreezerFieldEvent>> GetFieldEventsAsync(
        string accountId,
        string snapshotId,
        CancellationToken cancellationToken) =>
        await events.Find(x => x.AccountId == accountId && x.SnapshotId == snapshotId)
            .SortBy(x => x.Sequence)
            .Limit(1000)
            .ToListAsync(cancellationToken);

    public Task<FreezerVerificationReport?> FindVerificationAsync(
        string accountId,
        string snapshotId,
        string resultHash,
        CancellationToken cancellationToken) =>
        verifications.Find(x => x.AccountId == accountId && x.SnapshotId == snapshotId && x.ResultHash == resultHash)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveVerificationAsync(FreezerVerificationReport report, CancellationToken cancellationToken)
    {
        try
        {
            await verifications.InsertOneAsync(report, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await FindVerificationAsync(report.AccountId, report.SnapshotId, report.ResultHash, cancellationToken);
            if (existing is null)
                throw new InvalidOperationException("A duplicate verification could not be resolved.", exception);
        }
    }

    public Task<FreezerParameterBacktest?> GetParameterBacktestAsync(
        string accountId,
        string backtestId,
        CancellationToken cancellationToken) =>
        parameterBacktests.Find(x => x.AccountId == accountId && x.Id == backtestId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveParameterBacktestAsync(
        FreezerParameterBacktest backtest,
        CancellationToken cancellationToken) =>
        parameterBacktests.InsertOneAsync(backtest, cancellationToken: cancellationToken);

    public Task<FreezerParameterAdoption?> GetLatestParameterAdoptionAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        parameterAdoptions.Find(x => x.AccountId == accountId)
            .SortByDescending(x => x.Revision)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<FreezerParameterAdoption?> SaveParameterAdoptionAsync(
        FreezerParameterAdoption adoption,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var current = await GetLatestParameterAdoptionAsync(adoption.AccountId, cancellationToken);
        if ((current?.Revision ?? 0) != expectedRevision)
            return null;

        try
        {
            var revision = expectedRevision + 1;
            var deterministicId = Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes($"{adoption.AccountId}\0{revision}")).AsSpan(0, 12)).ToLowerInvariant();
            var saved = adoption with { Id = deterministicId, Revision = revision };
            await parameterAdoptions.InsertOneAsync(saved, cancellationToken: cancellationToken);
            return saved;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }
    }

    public Task<FreezerArtifactManifest?> GetArtifactAsync(
        string accountId,
        string snapshotId,
        string inputKey,
        CancellationToken cancellationToken) =>
        artifacts.Find(x => x.AccountId == accountId && x.SnapshotId == snapshotId && x.InputKey == inputKey)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<FreezerArtifactManifest?> GetArtifactByIdAsync(
        string accountId,
        string artifactId,
        CancellationToken cancellationToken) =>
        artifacts.Find(x => x.AccountId == accountId && x.Id == artifactId).FirstOrDefaultAsync(cancellationToken);

    public async Task<FreezerArtifactManifest> SaveArtifactAsync(
        FreezerArtifactManifest artifact,
        CancellationToken cancellationToken)
    {
        try
        {
            await artifacts.InsertOneAsync(artifact, cancellationToken: cancellationToken);
            return artifact;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return await GetArtifactAsync(artifact.AccountId, artifact.SnapshotId, artifact.InputKey, cancellationToken)
                ?? throw new InvalidOperationException("A duplicate artifact manifest could not be resolved.", exception);
        }
    }

    private Task<FreezerAnalysisDraft?> GetDraftByIdAsync(string accountId, string draftId, CancellationToken cancellationToken) =>
        drafts.Find(x => x.AccountId == accountId && x.Id == draftId).FirstOrDefaultAsync(cancellationToken);
}
