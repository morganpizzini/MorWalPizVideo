using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Interfaces;

public interface ILoginAttemptRepository : IRepository<LoginAttempt>
{
    Task<List<LoginAttempt>> GetRecentAttemptsByIpAsync(string ipAddress, TimeSpan timeWindow);
    Task<List<LoginAttempt>> GetRecentAttemptsByUsernameAsync(string username, TimeSpan timeWindow);
    Task<int> GetFailedAttemptsCountByIpAsync(string ipAddress, TimeSpan timeWindow);
    Task<int> GetFailedAttemptsCountByUsernameAsync(string username, TimeSpan timeWindow);
    Task<DateTime?> GetLastFailedAttemptTimeByIpAsync(string ipAddress);
    Task<DateTime?> GetLastFailedAttemptTimeByUsernameAsync(string username);
    Task CleanupOldAttemptsAsync(TimeSpan olderThan);
}
