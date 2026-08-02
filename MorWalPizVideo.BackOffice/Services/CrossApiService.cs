using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
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
    private readonly InternalServiceSettings internalServiceSettings;
    public CrossApiService(IHttpClientFactory _clientFactory, IOptions<InternalServiceSettings> _internalServiceSettings)
    {
        client = _clientFactory;
        internalServiceSettings = _internalServiceSettings.Value;
    }

    // ADR-002: authenticate as a trusted service identity when calling ServerAPI's internal cache endpoints.
    private HttpClient CreateAuthenticatedClient()
    {
        var httpClient = client.CreateClient(HttpClientNames.MorWalPiz);
        if (!string.IsNullOrEmpty(internalServiceSettings.Secret))
        {
            httpClient.DefaultRequestHeaders.Remove(internalServiceSettings.HeaderName);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(internalServiceSettings.HeaderName, internalServiceSettings.Secret);
        }
        return httpClient;
    }

    public Task<string> ResetCache(string key)
    {
        var httpClient = CreateAuthenticatedClient();

        return httpClient.GetStringAsync($"cache/reset?k={Uri.EscapeDataString(key)}");
    }
    public Task<string> PurgeCache(string key)
    {
        var httpClient = CreateAuthenticatedClient();
        return httpClient.GetStringAsync($"cache/purge?k={Uri.EscapeDataString(key)}");
    }
    public Task<string> ReloadCache()
    {
        var httpClient = client.CreateClient(HttpClientNames.MorWalPiz);
        return httpClient.GetStringAsync($"matches");
    }
}

