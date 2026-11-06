using MorWalPizVideo.Models.FreezerAnalysis;

namespace MorWalPizVideo.Domain.FreezerAnalysis;

public sealed record ImportPublicationResult(
    FreezerImportManifest Manifest,
    FreezerMatch? Match,
    bool IsConflict);

public sealed record FreezePublicationResult(
    FreezerAnalysisDraft? Draft,
    FreezerFrozenSnapshot? Snapshot,
    bool IsConflict);

public interface IFreezerAnalysisRepository
{
    Task<FreezerProfile?> GetProfileAsync(string accountId, CancellationToken cancellationToken);
    Task SaveProfileAsync(FreezerProfile profile, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerMatch>> ListPublishedMatchesAsync(int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerMatch>> ListPublishedMatchesBeforeAsync(
        DateOnly localMatchDate,
        int limit,
        CancellationToken cancellationToken);
    Task<FreezerMatch?> GetMatchAsync(string matchId, CancellationToken cancellationToken);
    Task<FreezerMatch?> FindMatchByKeyAsync(string matchKey, CancellationToken cancellationToken);
    Task<FreezerMatchRevision?> GetRevisionAsync(string matchId, string revisionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerCompetitorResult>> GetResultsAsync(IEnumerable<string> resultIds, CancellationToken cancellationToken);
    Task<FreezerShooterIdentity?> GetIdentityAsync(string accountId, string identityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerShooterIdentity>> ListIdentitiesAsync(string accountId, CancellationToken cancellationToken);
    Task<FreezerShooterIdentity> CreateIdentityAsync(FreezerShooterIdentity identity, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerShooterIdentity>> FindIdentityCandidatesAsync(
        string accountId,
        IEnumerable<string> normalizedNames,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerIdentityResolution>> ListIdentityResolutionsAsync(
        string accountId,
        string matchId,
        CancellationToken cancellationToken);
    Task<FreezerIdentityResolution?> GetIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        CancellationToken cancellationToken);
    Task<FreezerIdentityResolution?> SaveIdentityResolutionAsync(
        FreezerIdentityResolution resolution,
        long expectedRevision,
        CancellationToken cancellationToken);
    Task<bool> DeleteIdentityResolutionAsync(
        string accountId,
        string matchId,
        string resultId,
        long expectedRevision,
        CancellationToken cancellationToken);
    Task<FreezerImportManifest?> FindImportByHashAsync(string accountId, string sourceHash, CancellationToken cancellationToken);
    Task<FreezerImportManifest?> GetPublishedImportForRevisionAsync(
        string accountId,
        string revisionId,
        CancellationToken cancellationToken);
    Task<FreezerImportManifest?> GetImportAsync(string accountId, string importId, CancellationToken cancellationToken);
    Task<ImportPublicationResult> PublishImportAsync(
        FreezerMatch match,
        FreezerMatchRevision revision,
        IReadOnlyList<FreezerCompetitorResult> results,
        FreezerImportManifest manifest,
        int expectedRevision,
        CancellationToken cancellationToken);
    Task<FreezerAnalysisDraft?> GetDraftAsync(string accountId, string matchId, CancellationToken cancellationToken);
    Task<FreezerAnalysisDraft> CreateDraftAsync(FreezerAnalysisDraft draft, CancellationToken cancellationToken);
    Task<FreezerAnalysisDraft?> UpdateDraftAsync(FreezerAnalysisDraft draft, long expectedRevision, CancellationToken cancellationToken);
    Task<FreezerAnalysisDraft?> ReopenDraftAsync(
        string accountId,
        string matchId,
        long expectedRevision,
        CancellationToken cancellationToken);
    Task<FreezePublicationResult> FreezeAsync(FreezerFrozenSnapshot snapshot, long expectedRevision, CancellationToken cancellationToken);
    Task<FreezerFrozenSnapshot?> GetSnapshotAsync(string accountId, string snapshotId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerFrozenSnapshot>> ListSnapshotsAsync(string accountId, int limit, CancellationToken cancellationToken);
    Task<FreezerFieldEvent?> FindFieldEventAsync(string accountId, string clientOperationId, CancellationToken cancellationToken);
    Task<FreezerFieldEvent?> AppendFieldEventAsync(FreezerFieldEvent fieldEvent, string draftId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FreezerFieldEvent>> GetFieldEventsAsync(string accountId, string snapshotId, CancellationToken cancellationToken);
    Task<FreezerVerificationReport?> FindVerificationAsync(string accountId, string snapshotId, string resultHash, CancellationToken cancellationToken);
    Task SaveVerificationAsync(FreezerVerificationReport report, CancellationToken cancellationToken);
    Task<FreezerParameterBacktest?> GetParameterBacktestAsync(string accountId, string backtestId, CancellationToken cancellationToken);
    Task SaveParameterBacktestAsync(FreezerParameterBacktest backtest, CancellationToken cancellationToken);
    Task<FreezerParameterAdoption?> GetLatestParameterAdoptionAsync(string accountId, CancellationToken cancellationToken);
    Task<FreezerParameterAdoption?> SaveParameterAdoptionAsync(
        FreezerParameterAdoption adoption,
        long expectedRevision,
        CancellationToken cancellationToken);
    Task<FreezerArtifactManifest?> GetArtifactAsync(string accountId, string snapshotId, string inputKey, CancellationToken cancellationToken);
    Task<FreezerArtifactManifest?> GetArtifactByIdAsync(string accountId, string artifactId, CancellationToken cancellationToken);
    Task<FreezerArtifactManifest> SaveArtifactAsync(FreezerArtifactManifest artifact, CancellationToken cancellationToken);
}
