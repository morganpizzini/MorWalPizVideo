using MongoDB.Driver;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public class ApiKeyRepository : BaseRepository<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(IMongoDatabase database) : base(database, DbCollections.ApiKeys)
    {
    }

    public async Task<ApiKey?> GetByKeyAsync(string key)
    {
        var filter = Builders<ApiKey>.Filter.Eq(x => x.Key, key);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<ApiKey?> GetByNameAsync(string name)
    {
        var filter = Builders<ApiKey>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ApiKey>> GetActiveKeysAsync()
    {
        var filter = Builders<ApiKey>.Filter.Eq(x => x.IsActive, true);
        return await _collection.Find(filter).ToListAsync();
    }
}
