using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;
using MongoDB.Bson;

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
            // Try to parse the string ID as ObjectId first
            if (ObjectId.TryParse(id, out var objectId))
            {
                await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", objectId));
            }
            else
            {
                // Fallback to string ID if not a valid ObjectId
                await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", id));
            }
        }

        public async Task<T> GetItemAsync(string id)
        {
            // Try to parse the string ID as ObjectId first
            if (ObjectId.TryParse(id, out var objectId))
            {
                return await _collection.Find(Builders<T>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync();
            }

            // Fallback to string ID if not a valid ObjectId
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

        public async Task UpdateItemAsync(T item)
        {
            // If item.Id is a string representation of an ObjectId, convert it
            if (ObjectId.TryParse(item.Id, out var objectId))
            {
                await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", objectId), item);
            }
            else
            {
                await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", item.Id), item);
            }
        }
    }
}
