using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public sealed class RecordingCrossApiService : ICrossApiService
{
    public List<string> ResetKeys { get; } = [];
    public List<string> PurgedTags { get; } = [];
    public List<string> RefreshedVideoIds { get; } = [];

    public Task<string> ResetCache(string key)
    {
        lock (ResetKeys)
        {
            ResetKeys.Add(key);
        }
        return Task.FromResult(string.Empty);
    }

    public Task<string> PurgeCache(string key)
    {
        lock (PurgedTags)
        {
            PurgedTags.Add(key);
        }
        return Task.FromResult(string.Empty);
    }

    public Task<string> RefreshVideoCache(string matchId)
    {
        lock (RefreshedVideoIds)
        {
            RefreshedVideoIds.Add(matchId);
        }
        return Task.FromResult(string.Empty);
    }

    public Task<string> ReloadCache() => Task.FromResult(string.Empty);

    public void Clear()
    {
        ResetKeys.Clear();
        PurgedTags.Clear();
        RefreshedVideoIds.Clear();
    }
}
