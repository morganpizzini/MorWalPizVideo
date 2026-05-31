using Microsoft.Extensions.Logging;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MorWalPizVideo.BackOffice.Tests.Features;

/// <summary>
/// Telemetry tests for ApiKey LastUsed updates (FR-018, FR-019).
/// Verifies that swallowed exceptions in the fire-and-forget UpdateLastUsedAsync path produce
/// structured Error logs (so silent failures can be observed in production).
/// </summary>
public class ApiKeyTelemetryTests
{
    [Fact]
    public async Task UpdateLastUsedAsync_logs_error_when_repository_throws()
    {
        // Arrange
        var repository = new ThrowingApiKeyRepository();
        var logger = new ListLogger<ApiKeyService>();
        var service = new ApiKeyService(repository, Options.Create(new ApiKeySettings()), logger);

        // Act
        var result = await service.UpdateLastUsedAsync("api-key-id-123");

        // Assert: swallowed (returns false) but error logged with the key id
        Assert.False(result);
        var errors = logger.Entries.Where(e => e.Level == LogLevel.Error).ToList();
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Message.Contains("api-key-id-123"));
        Assert.Contains(errors, e => e.Exception is InvalidOperationException);
    }

    private sealed class ThrowingApiKeyRepository : IApiKeyRepository
    {
        public Task<ApiKey> GetItemAsync(string id) => throw new InvalidOperationException("simulated repository failure");
        public Task<IList<ApiKey>> GetItemsAsync() => throw new NotSupportedException();
        public Task<IList<ApiKey>> GetItemsAsync(System.Linq.Expressions.Expression<Func<ApiKey, bool>> predicate) => throw new NotSupportedException();
        public Task<ApiKey> AddItemAsync(ApiKey item) => throw new NotSupportedException();
        public Task UpdateItemAsync(ApiKey item) => throw new NotSupportedException();
        public Task DeleteItemAsync(string id) => throw new NotSupportedException();
        public Task<ApiKey?> GetByKeyAsync(string key) => throw new NotSupportedException();
        public Task<ApiKey?> GetByNameAsync(string name) => throw new NotSupportedException();
        public Task<IEnumerable<ApiKey>> GetActiveKeysAsync() => throw new NotSupportedException();
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }
}
