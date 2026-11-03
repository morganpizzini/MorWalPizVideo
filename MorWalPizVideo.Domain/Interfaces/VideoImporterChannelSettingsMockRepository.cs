using MongoDB.Bson;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed class VideoImporterChannelSettingsMockRepository(IMockScenario scenario)
    : BaseMockRepository<VideoImporterChannelSettings>(scenario, "videoImporterChannelSettings"),
        IVideoImporterChannelSettingsRepository
{
    private static readonly object Sync = new();

    public Task<VideoImporterChannelSettings?> GetByChannelIdAsync(
        string channelId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            scenario.Read<VideoImporterChannelSettings>(_fileName)
                .FirstOrDefault(item => item.ChannelId == channelId));
    }

    public Task<VideoImporterChannelSettings> UpsertByChannelIdAsync(
        VideoImporterChannelSettings settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Sync)
        {
            var existing = scenario.Read<VideoImporterChannelSettings>(_fileName)
                .FirstOrDefault(item => item.ChannelId == settings.ChannelId);
            var saved = settings with
            {
                Id = existing?.Id ?? ObjectId.GenerateNewId().ToString(),
                CreationDateTime = existing?.CreationDateTime ?? settings.CreationDateTime
            };

            if (existing is null)
                scenario.Add(_fileName, saved);
            else
                scenario.Replace(_fileName, saved);

            return Task.FromResult(saved);
        }
    }
}
