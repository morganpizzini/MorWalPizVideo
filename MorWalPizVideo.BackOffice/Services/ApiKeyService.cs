using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Models;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ApiKeySettings _settings;

    public ApiKeyService(IApiKeyRepository apiKeyRepository, IOptions<ApiKeySettings> settings)
    {
        _apiKeyRepository = apiKeyRepository;
        _settings = settings.Value;
    }

    public async Task<(ApiKey apiKey, string unhashedKey)> CreateApiKeyAsync(
        string name, 
        string description, 
        int? rateLimitPerMinute = null, 
        List<string>? allowedIpAddresses = null, 
        DateTime? expiresAt = null)
    {
        var unhashedKey = GenerateApiKey();
        var hashedKey = HashApiKey(unhashedKey);

        var apiKey = new ApiKey
        {
            Key = hashedKey,
            Name = name,
            Description = description,
            IsActive = true,
            RateLimitPerMinute = rateLimitPerMinute ?? _settings.DefaultRateLimitPerMinute,
            AllowedIpAddresses = allowedIpAddresses ?? new List<string>(),
            ExpiresAt = expiresAt,
            CreationDateTime = DateTime.UtcNow
        };

        await _apiKeyRepository.AddItemAsync(apiKey);

        return (apiKey, unhashedKey);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var hashedKey = HashApiKey(key);
        var apiKey = await _apiKeyRepository.GetByKeyAsync(hashedKey);

        if (apiKey == null)
            return null;

        // Check if key is active
        if (!apiKey.IsActive)
            return null;

        // Check if key has expired
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        return apiKey;
    }

    public string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public string GenerateApiKey()
    {
        // Generate a 32-byte (256-bit) random key
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        // Convert to base64 and make it URL-safe
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public async Task<bool> UpdateLastUsedAsync(string apiKeyId)
    {
        try
        {
            var apiKey = await _apiKeyRepository.GetItemAsync(apiKeyId);
            if (apiKey == null)
                return false;

            var updatedKey = apiKey with { LastUsedAt = DateTime.UtcNow };
            await _apiKeyRepository.UpdateItemAsync(updatedKey);
            return true;
        }
        catch
        {
            return false;
        }
    }
}