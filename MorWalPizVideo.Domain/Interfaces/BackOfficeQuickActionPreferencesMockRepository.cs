using MongoDB.Bson;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed class BackOfficeQuickActionPreferencesMockRepository(IMockScenario scenario)
    : BaseMockRepository<BackOfficeQuickActionPreferences>(scenario, DbCollections.BackOfficeQuickActionPreferences),
        IBackOfficeQuickActionPreferencesRepository
{
    private static readonly object Sync = new();

    public Task<BackOfficeQuickActionPreferences?> GetAsync(string userId, string channelId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Sync)
            return Task.FromResult(Find(userId, channelId));
    }

    public Task UpsertAsync(BackOfficeQuickActionPreferences preferences, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Sync)
        {
            var existing = Find(preferences.UserId, preferences.ChannelId);
            var saved = preferences with
            {
                Id = existing?.Id ?? ObjectId.GenerateNewId().ToString(),
                CreationDateTime = existing?.CreationDateTime ?? preferences.CreationDateTime
            };
            if (existing is null)
                scenario.Add(_fileName, saved);
            else
                scenario.Replace(_fileName, saved);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId, string channelId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Sync)
        {
            var existing = Find(userId, channelId);
            if (existing is not null)
                scenario.Delete<BackOfficeQuickActionPreferences>(_fileName, existing.Id);
        }
        return Task.CompletedTask;
    }

    private BackOfficeQuickActionPreferences? Find(string userId, string channelId) =>
        scenario.Read<BackOfficeQuickActionPreferences>(_fileName)
            .FirstOrDefault(item => item.UserId == userId && item.ChannelId == channelId);
}
