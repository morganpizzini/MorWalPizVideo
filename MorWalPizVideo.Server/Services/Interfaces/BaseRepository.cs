using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly IMongoCollection<T> _collection;

        public BaseRepository(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<T>(collectionName);
        }

        public async Task AddItemAsync(T item)
        {
            await _collection.InsertOneAsync(item);
        }

        public async Task DeleteItemAsync(string id)
        {
            await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", id));
        }

        public async Task<T> GetItemAsync(string id)
        {
            return await _collection.Find(Builders<T>.Filter.Eq("_id", id)).FirstOrDefaultAsync();
        }

        public async Task<IList<T>> GetItemsAsync()
        {
            return await _collection.Find(Builders<T>.Filter.Empty).ToListAsync();
        }

        public async Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.Find(predicate).ToListAsync();
        }

        public async Task UpdateItemAsync(string id, T item)
        {
            await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq(e => e.Id, item.Id), item);
        }
    }
}
