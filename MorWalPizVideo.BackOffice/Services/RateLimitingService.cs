using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services;

public class RateLimitingService : IRateLimitingService
{
    private readonly ILoginAttemptRepository _loginAttemptRepository;
    private readonly SecuritySettings _settings;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(
        ILoginAttemptRepository loginAttemptRepository,
        IOptions<SecuritySettings> settings,
        ILogger<RateLimitingService> logger)
    {
        _loginAttemptRepository = loginAttemptRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string ipAddress, string username)
    {
        var result = new RateLimitResult { IsAllowed = true };

        // Check IP-based rate limiting
        var ipWindow = TimeSpan.FromMinutes(_settings.IpRateLimitWindowMinutes);
        var ipFailedAttempts = await _loginAttemptRepository.GetFailedAttemptsCountByIpAsync(ipAddress, ipWindow);
        
        if (ipFailedAttempts >= _settings.MaxFailedAttemptsPerIp)
        {
            var lastIpAttempt = await _loginAttemptRepository.GetLastFailedAttemptTimeByIpAsync(ipAddress);
            if (lastIpAttempt.HasValue)
            {
                var lockoutDuration = CalculateProgressiveDelay(ipFailedAttempts - _settings.MaxFailedAttemptsPerIp + 1);
                var unlockTime = lastIpAttempt.Value.Add(lockoutDuration);
                
                if (DateTime.UtcNow < unlockTime)
                {
                    result.IsAllowed = false;
                    result.Reason = "IP address temporarily blocked due to too many failed attempts";
                    result.RetryAfter = unlockTime - DateTime.UtcNow;
                    _logger.LogWarning("IP {IpAddress} is rate limited. Failed attempts: {FailedAttempts}", ipAddress, ipFailedAttempts);
                    return result;
                }
            }
        }

        // Check username-based rate limiting
        var userWindow = TimeSpan.FromMinutes(_settings.UsernameRateLimitWindowMinutes);
        var userFailedAttempts = await _loginAttemptRepository.GetFailedAttemptsCountByUsernameAsync(username, userWindow);
        
        if (userFailedAttempts >= _settings.MaxFailedAttemptsPerUser)
        {
            var lastUserAttempt = await _loginAttemptRepository.GetLastFailedAttemptTimeByUsernameAsync(username);
            if (lastUserAttempt.HasValue)
            {
                var lockoutDuration = CalculateProgressiveDelay(userFailedAttempts - _settings.MaxFailedAttemptsPerUser + 1);
                var unlockTime = lastUserAttempt.Value.Add(lockoutDuration);
                
                if (DateTime.UtcNow < unlockTime)
                {
                    result.IsAllowed = false;
                    result.Reason = "Account temporarily locked due to too many failed attempts";
                    result.RetryAfter = unlockTime - DateTime.UtcNow;
                    _logger.LogWarning("User {Username} is rate limited. Failed attempts: {FailedAttempts}", username, userFailedAttempts);
                    return result;
                }
            }
        }

        // Calculate remaining attempts
        result.RemainingAttempts = Math.Min(
            _settings.MaxFailedAttemptsPerIp - ipFailedAttempts,
            _settings.MaxFailedAttemptsPerUser - userFailedAttempts);

        return result;
    }

    public async Task RecordLoginAttemptAsync(string ipAddress, string username, bool isSuccessful, string userAgent, string failureReason = "")
    {
        var attempt = new LoginAttempt
        {
            IpAddress = ipAddress,
            Username = username,
            IsSuccessful = isSuccessful,
            AttemptTime = DateTime.UtcNow,
            UserAgent = userAgent,
            FailureReason = failureReason
        };

        await _loginAttemptRepository.AddItemAsync(attempt);

        if (isSuccessful)
        {
            _logger.LogInformation("Successful login for user {Username} from IP {IpAddress}", username, ipAddress);
        }
        else
        {
            _logger.LogWarning("Failed login attempt for user {Username} from IP {IpAddress}. Reason: {Reason}", username, ipAddress, failureReason);
        }
    }

    public async Task<bool> IsIpLockedOutAsync(string ipAddress)
    {
        var window = TimeSpan.FromMinutes(_settings.IpRateLimitWindowMinutes);
        var failedAttempts = await _loginAttemptRepository.GetFailedAttemptsCountByIpAsync(ipAddress, window);
        
        if (failedAttempts < _settings.MaxFailedAttemptsPerIp)
            return false;

        var lastAttempt = await _loginAttemptRepository.GetLastFailedAttemptTimeByIpAsync(ipAddress);
        if (!lastAttempt.HasValue)
            return false;

        var lockoutDuration = CalculateProgressiveDelay(failedAttempts - _settings.MaxFailedAttemptsPerIp + 1);
        var unlockTime = lastAttempt.Value.Add(lockoutDuration);
        
        return DateTime.UtcNow < unlockTime;
    }

    public async Task<bool> IsUserLockedOutAsync(string username)
    {
        var window = TimeSpan.FromMinutes(_settings.UsernameRateLimitWindowMinutes);
        var failedAttempts = await _loginAttemptRepository.GetFailedAttemptsCountByUsernameAsync(username, window);
        
        if (failedAttempts < _settings.MaxFailedAttemptsPerUser)
            return false;

        var lastAttempt = await _loginAttemptRepository.GetLastFailedAttemptTimeByUsernameAsync(username);
        if (!lastAttempt.HasValue)
            return false;

        var lockoutDuration = CalculateProgressiveDelay(failedAttempts - _settings.MaxFailedAttemptsPerUser + 1);
        var unlockTime = lastAttempt.Value.Add(lockoutDuration);
        
        return DateTime.UtcNow < unlockTime;
    }

    public async Task<TimeSpan?> GetLockoutTimeRemainingAsync(string ipAddress, string username)
    {
        var ipLocked = await IsIpLockedOutAsync(ipAddress);
        var userLocked = await IsUserLockedOutAsync(username);

        if (!ipLocked && !userLocked)
            return null;

        TimeSpan? ipRemaining = null;
        TimeSpan? userRemaining = null;

        if (ipLocked)
        {
            var lastIpAttempt = await _loginAttemptRepository.GetLastFailedAttemptTimeByIpAsync(ipAddress);
            if (lastIpAttempt.HasValue)
            {
                var ipWindow = TimeSpan.FromMinutes(_settings.IpRateLimitWindowMinutes);
                var ipFailedAttempts = await _loginAttemptRepository.GetFailedAttemptsCountByIpAsync(ipAddress, ipWindow);
                var lockoutDuration = CalculateProgressiveDelay(ipFailedAttempts - _settings.MaxFailedAttemptsPerIp + 1);
                var unlockTime = lastIpAttempt.Value.Add(lockoutDuration);
                ipRemaining = unlockTime - DateTime.UtcNow;
            }
        }

        if (userLocked)
        {
            var lastUserAttempt = await _loginAttemptRepository.GetLastFailedAttemptTimeByUsernameAsync(username);
            if (lastUserAttempt.HasValue)
            {
                var userWindow = TimeSpan.FromMinutes(_settings.UsernameRateLimitWindowMinutes);
                var userFailedAttempts = await _loginAttemptRepository.GetFailedAttemptsCountByUsernameAsync(username, userWindow);
                var lockoutDuration = CalculateProgressiveDelay(userFailedAttempts - _settings.MaxFailedAttemptsPerUser + 1);
                var unlockTime = lastUserAttempt.Value.Add(lockoutDuration);
                userRemaining = unlockTime - DateTime.UtcNow;
            }
        }

        // Return the longer of the two lockout times
        if (ipRemaining.HasValue && userRemaining.HasValue)
            return ipRemaining > userRemaining ? ipRemaining : userRemaining;
        
        return ipRemaining ?? userRemaining;
    }

    public async Task CleanupOldAttemptsAsync()
    {
        var cleanupThreshold = TimeSpan.FromDays(_settings.CleanupThresholdDays);
        await _loginAttemptRepository.CleanupOldAttemptsAsync(cleanupThreshold);
        _logger.LogInformation("Cleaned up login attempts older than {Days} days", _settings.CleanupThresholdDays);
    }

    private TimeSpan CalculateProgressiveDelay(int attemptNumber)
    {
        // Progressive delay: 1s, 5s, 15s, 60s, 300s (5min), 900s (15min), 1800s (30min), 3600s (1h)
        var delaySeconds = attemptNumber switch
        {
            1 => 1,
            2 => 5,
            3 => 15,
            4 => 60,
            5 => 300,
            6 => 900,
            7 => 1800,
            _ => 3600
        };

        return TimeSpan.FromSeconds(delaySeconds);
    }
}

public class SecuritySettings
{
    public int MaxFailedAttemptsPerIp { get; set; } = 5;
    public int MaxFailedAttemptsPerUser { get; set; } = 3;
    public int IpRateLimitWindowMinutes { get; set; } = 15;
    public int UsernameRateLimitWindowMinutes { get; set; } = 10;
    public int CleanupThresholdDays { get; set; } = 30;
    public bool EnableProgressiveDelays { get; set; } = true;
}
