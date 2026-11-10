using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.Server.Services.Interfaces;

public interface IBackOfficeQuickActionPreferencesRepository : IRepository<BackOfficeQuickActionPreferences>
{
    Task<BackOfficeQuickActionPreferences?> GetAsync(string userId, string channelId, CancellationToken cancellationToken = default);
    Task UpsertAsync(BackOfficeQuickActionPreferences preferences, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, string channelId, CancellationToken cancellationToken = default);
}
