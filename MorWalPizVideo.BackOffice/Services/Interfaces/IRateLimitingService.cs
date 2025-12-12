namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IRateLimitingService
{
    Task<RateLimitResult> CheckRateLimitAsync(string ipAddress, string username);
    Task RecordLoginAttemptAsync(string ipAddress, string username, bool isSuccessful, string userAgent, string failureReason = "");
    Task<bool> IsIpLockedOutAsync(string ipAddress);
    Task<bool> IsUserLockedOutAsync(string username);
    Task<TimeSpan?> GetLockoutTimeRemainingAsync(string ipAddress, string username);
    Task CleanupOldAttemptsAsync();
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TimeSpan? RetryAfter { get; set; }
    public int RemainingAttempts { get; set; }
}
