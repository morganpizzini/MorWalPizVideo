using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MorWalPizVideo.Server.Models;
using System.Linq.Expressions;
using System.Text.Json;

namespace MorWalPizVideo.Server.Services.Interfaces
{
    public abstract class BaseMockRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly string _fileName;
        private readonly IHostEnvironment _environment;
        public BaseMockRepository(IHostEnvironment environment, string fileName)
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

        public Task UpdateItemAsync(T item)
        {
            return Task.CompletedTask;
        }
        private IList<K> ReadJson<K>(string jsonFileName) where K : T
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", $"{jsonFileName}.json");

            if (!File.Exists(filePath))
            {
                return new List<K>();
            }

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
