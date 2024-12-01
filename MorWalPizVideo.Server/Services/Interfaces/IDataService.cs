using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;
using System.Text.Json;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T> GetItemAsync(string id);
        Task<IList<T>> GetItemsAsync();
        Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);
        Task AddItemAsync(T item);
        Task UpdateItemAsync(string id, T item);
        Task DeleteItemAsync(string id);
    }

    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        public MatchRepository(IMongoDatabase database) : base(database, "matches")
        {
        }
    }
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(IMongoDatabase database) : base(database, "products")
        {
        }
    }
    public class SponsorRepository : BaseRepository<Sponsor>, ISponsorRepository
    {
        public SponsorRepository(IMongoDatabase database) : base(database, "sponsors")
        {
        }
    }
    public class PageRepository : BaseRepository<Page>, IPageRepository
    {
        public PageRepository(IMongoDatabase database) : base(database, "pages")
        {
        }
    }

    public class MatchMockRepository : BaseMockRepository<Match>, IMatchRepository
    {
        public MatchMockRepository(IWebHostEnvironment environment) : base(environment, "matches")
        {
        }
    }
    public class ProductMockRepository : BaseMockRepository<Product>, IProductRepository
    {
        public ProductMockRepository(IWebHostEnvironment environment) : base(environment, "products")
        {
        }
    }
    public class SponsorMockRepository : BaseMockRepository<Sponsor>, ISponsorRepository
    {
        public SponsorMockRepository(IWebHostEnvironment environment) : base(environment, "sponsors")
        {
        }
    }
    public class PageMockRepository : BaseMockRepository<Page>, IPageRepository
    {
        public PageMockRepository(IWebHostEnvironment environment) : base(environment, "pages")
        {
        }
    }
    public interface IMatchRepository : IRepository<Match> { }
    public interface IProductRepository : IRepository<Product> { }
    public interface ISponsorRepository : IRepository<Sponsor> { }
    public interface IPageRepository : IRepository<Page> { }

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
    public abstract class BaseMockRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly string _fileName;
        private readonly IWebHostEnvironment _environment;
        public BaseMockRepository(IWebHostEnvironment environment, string fileName)
        {
            _environment = environment;
            _fileName = fileName;
        }

        public Task AddItemAsync(T item)
        {
            return Task.CompletedTask;
        }

        public Task DeleteItemAsync(string id)
        {
            return Task.CompletedTask;
        }

        public Task<T> GetItemAsync(string id) =>
            Task.FromResult(
                ReadJson<T>(_fileName)
                    .OrderByDescending(x => x.CreationDateTime)
                    .FirstOrDefault(x => x.Id == id));


        public Task<IList<T>> GetItemsAsync() =>

                Task.FromResult(ReadJson<T>(_fileName));

        public Task<IList<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IList<T> result = ReadJson<T>(_fileName)
                    .Where(predicate.Compile()).ToList();
            return Task.FromResult(result);
        }

        public Task UpdateItemAsync(string id, T item)
        {
            return Task.CompletedTask;
        }

        private IList<K> ReadJson<K>(string jsonFileName) where K : T
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", $"{jsonFileName}.json");
            var jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<IList<K>>(jsonString, options)?.OrderByDescending(x => x.CreationDateTime).ToList() ?? new List<K>();
        }
    }
}
