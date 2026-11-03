using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed class VideoImporterChannelSettingsRepository(IMongoDatabase database)
    : BaseRepository<VideoImporterChannelSettings>(database, DbCollections.VideoImporterChannelSettings),
        IVideoImporterChannelSettingsRepository
{
    public Task<VideoImporterChannelSettings?> GetByChannelIdAsync(
        string channelId,
        CancellationToken cancellationToken = default) =>
        _collection.Find(item => item.ChannelId == channelId).FirstOrDefaultAsync(cancellationToken);

    public async Task<VideoImporterChannelSettings> UpsertByChannelIdAsync(
        VideoImporterChannelSettings settings,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<VideoImporterChannelSettings>.Update
            .Set(item => item.ChannelId, settings.ChannelId)
            .Set(item => item.SchemaVersion, settings.SchemaVersion)
            .Set(item => item.PayloadJson, settings.PayloadJson)
            .Set(item => item.UpdatedAtUtc, settings.UpdatedAtUtc)
            .SetOnInsert(item => item.Id, ObjectId.GenerateNewId().ToString())
            .SetOnInsert(item => item.CreationDateTime, settings.CreationDateTime);
        var options = new FindOneAndUpdateOptions<VideoImporterChannelSettings>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        try
        {
            return await _collection.FindOneAndUpdateAsync(
                item => item.ChannelId == settings.ChannelId,
                update,
                options,
                cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            var retryOptions = new FindOneAndUpdateOptions<VideoImporterChannelSettings>
            {
                ReturnDocument = ReturnDocument.After
            };
            return await _collection.FindOneAndUpdateAsync(
                item => item.ChannelId == settings.ChannelId,
                update,
                retryOptions,
                cancellationToken);
        }
    }
}
