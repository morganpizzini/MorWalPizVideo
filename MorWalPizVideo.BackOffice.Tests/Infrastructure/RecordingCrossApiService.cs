using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public sealed class RecordingCrossApiService : ICrossApiService
{
    public List<string> ResetKeys { get; } = [];
    public List<string> PurgedTags { get; } = [];
    public List<string> RefreshedVideoIds { get; } = [];
    public bool ShouldFail { get; set; }

    public Task<string> ResetCache(string key)
    {
        if (ShouldFail) throw new InvalidOperationException("simulated cache failure");
        lock (ResetKeys)
        {
            ResetKeys.Add(key);
        }
        return Task.FromResult(string.Empty);
    }

    public Task<string> PurgeCache(string key)
    {
        if (ShouldFail) throw new InvalidOperationException("simulated cache failure");
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
        ShouldFail = false;
    }
}
