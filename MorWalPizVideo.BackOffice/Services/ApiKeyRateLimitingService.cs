using MorWalPizVideo.BackOffice.Services.Interfaces;
using System.Collections.Concurrent;

namespace MorWalPizVideo.BackOffice.Services;

public class ApiKeyRateLimitingService : IApiKeyRateLimitingService
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);

    public async Task<bool> CheckRateLimitAsync(string apiKeyId, int rateLimitPerMinute)
    {
        if (string.IsNullOrEmpty(apiKeyId))
            return false;

        await CleanupOldEntriesAsync(apiKeyId);

        var requests = _requestLog.GetOrAdd(apiKeyId, _ => new List<DateTime>());

        lock (requests)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var recentRequests = requests.Count(r => r > oneMinuteAgo);

            return recentRequests < rateLimitPerMinute;
        }
    }

    public Task RecordRequestAsync(string apiKeyId)
    {
        if (string.IsNullOrEmpty(apiKeyId))
            return Task.CompletedTask;

        var requests = _requestLog.GetOrAdd(apiKeyId, _ => new List<DateTime>());

        lock (requests)
        {
            requests.Add(DateTime.UtcNow);
        }

        return Task.CompletedTask;
    }

    private async Task CleanupOldEntriesAsync(string apiKeyId)
    {
        await _cleanupLock.WaitAsync();
        try
        {
            if (_requestLog.TryGetValue(apiKeyId, out var requests))
            {
                lock (requests)
                {
                    var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
                    requests.RemoveAll(r => r <= oneMinuteAgo);
                }
            }
        }
        finally
        {
            _cleanupLock.Release();
        }
    }
}