using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public sealed class RecordingCrossApiService : ICrossApiService
{
    public List<string> ResetKeys { get; } = [];
    public List<string> PurgedTags { get; } = [];

    public Task<string> ResetCache(string key)
    {
        ResetKeys.Add(key);
        return Task.FromResult(string.Empty);
    }

    public Task<string> PurgeCache(string key)
    {
        PurgedTags.Add(key);
        return Task.FromResult(string.Empty);
    }

    public Task<string> ReloadCache() => Task.FromResult(string.Empty);

    public void Clear()
    {
        ResetKeys.Clear();
        PurgedTags.Clear();
    }
}
