namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IApiKeyRateLimitingService
{
    Task<bool> CheckRateLimitAsync(string apiKeyId, int rateLimitPerMinute);
    Task RecordRequestAsync(string apiKeyId);
}