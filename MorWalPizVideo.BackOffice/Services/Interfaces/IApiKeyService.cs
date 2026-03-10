using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IApiKeyService
{
    Task<(ApiKey apiKey, string unhashedKey)> CreateApiKeyAsync(string name, string description, int? rateLimitPerMinute = null, List<string>? allowedIpAddresses = null, DateTime? expiresAt = null);
    Task<ApiKey?> ValidateApiKeyAsync(string key);
    string HashApiKey(string key);
    string GenerateApiKey();
    Task<bool> UpdateLastUsedAsync(string apiKeyId);
}
