using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed class BackOfficeQuickActionPreferencesRepository(IMongoDatabase database)
    : BaseRepository<BackOfficeQuickActionPreferences>(database, DbCollections.BackOfficeQuickActionPreferences),
        IBackOfficeQuickActionPreferencesRepository
{
    public Task<BackOfficeQuickActionPreferences?> GetAsync(string userId, string channelId, CancellationToken cancellationToken = default) =>
        _collection.Find(item => item.UserId == userId && item.ChannelId == channelId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpsertAsync(BackOfficeQuickActionPreferences preferences, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BackOfficeQuickActionPreferences>.Filter.Where(
            item => item.UserId == preferences.UserId && item.ChannelId == preferences.ChannelId);
        var update = Builders<BackOfficeQuickActionPreferences>.Update
            .Set(item => item.UserId, preferences.UserId)
            .Set(item => item.ChannelId, preferences.ChannelId)
            .Set(item => item.Actions, preferences.Actions)
            .Set(item => item.UpdatedAtUtc, preferences.UpdatedAtUtc)
            .SetOnInsert(item => item.Id, ObjectId.GenerateNewId().ToString())
            .SetOnInsert(item => item.CreationDateTime, preferences.CreationDateTime);
        try
        {
            await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            if (result.MatchedCount == 0)
                throw;
        }
    }

    public Task DeleteAsync(string userId, string channelId, CancellationToken cancellationToken = default) =>
        _collection.DeleteOneAsync(item => item.UserId == userId && item.ChannelId == channelId, cancellationToken);
}
