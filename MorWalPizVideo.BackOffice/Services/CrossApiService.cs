using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Services;

public interface ICrossApiService
{
    Task<string> ResetCache(string key);
    Task<string> PurgeCache(string key);
    Task<string> ReloadCache();
}
public class MockCrossApiService : ICrossApiService
{
    public Task<string> ResetCache(string key)
    {
        return Task.FromResult(string.Empty);
    }
    public Task<string> PurgeCache(string key)
    {
        return Task.FromResult(string.Empty);
    }
    public Task<string> ReloadCache()
    {
        return Task.FromResult(string.Empty);
    }
}
public class CrossApiService : ICrossApiService
{
    private readonly IHttpClientFactory client;
    public CrossApiService(IHttpClientFactory _clientFactory)
    {
        client = _clientFactory;
    }
    public Task<string> ResetCache(string key)
    {
        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);

        return client.GetStringAsync($"cache/reset?k={key}");
    }
    public Task<string> PurgeCache(string key)
    {
        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        return client.GetStringAsync($"cache/purge/{key}");
    }
    public Task<string> ReloadCache()
    {
        using var client = this.client.CreateClient(HttpClientNames.MorWalPiz);
        return client.GetStringAsync($"matches");
    }
}
